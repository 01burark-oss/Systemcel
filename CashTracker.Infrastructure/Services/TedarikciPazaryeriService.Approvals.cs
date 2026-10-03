using System.Data;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed partial class TedarikciPazaryeriService
{
    private static bool NeedsSecondReceiptApproval(TedarikciSiparis order, TedarikciMalKabulRequest? request) =>
        order.GenelToplam > 10_000m || !string.Equals(order.ParaBirimi, "TRY", StringComparison.OrdinalIgnoreCase) ||
        request is { ReddedilenMiktar: > 0m } or { BelgeUyusmazligi: true } or { MiktarDegisikligi: true };

    private static string ReceiptApprovalReason(TedarikciSiparis order, TedarikciMalKabulRequest request)
    {
        var reasons = new List<string>();
        if (!string.Equals(order.ParaBirimi, "TRY", StringComparison.OrdinalIgnoreCase)) reasons.Add("Dövizli sipariş");
        else if (order.GenelToplam > 10_000m) reasons.Add("Sipariş toplamı 10.000 TL üzerinde");
        if (request.ReddedilenMiktar > 0m) reasons.Add("Eksik veya reddedilen teslimat");
        if (request.BelgeUyusmazligi) reasons.Add("Belge uyuşmazlığı");
        if (request.MiktarDegisikligi) reasons.Add("Miktar değişikliği");
        return string.Join("; ", reasons);
    }

    public async Task<TedarikciMalKabulSonucu> ApproveReceiptAsync(
        int receiptId, TedarikciMalKabulOnayRequest request, CancellationToken ct = default)
    {
        var note = (request.Not ?? string.Empty).Trim();
        if (note.Length is < 1 or > 500) throw new ArgumentException("Onay notu 1-500 karakter olmalıdır.");
        var identity = _currentUserContext.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("İkinci onay için oturum açmalısınız.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var receipt = await db.TedarikciMalKabulleri.SingleOrDefaultAsync(x => x.Id == receiptId, ct)
            ?? throw new KeyNotFoundException("Mal kabul kaydı bulunamadı.");
        var actor = await db.Kullanicilar.SingleOrDefaultAsync(x =>
            x.AuthProviderUserId == identity.ProviderUserId && x.Durum == "Aktif", ct)
            ?? throw new UnauthorizedAccessException("Onay için aktif kullanıcı kaydı gerekir.");
        var admin = await _yonetimService.IsCurrentUserAdminAsync(ct);
        if (!admin)
        {
            if (await _isletmeService.GetActiveIdAsync() != receipt.AliciIsletmeId)
                throw new UnauthorizedAccessException("Bu işletmenin mal kabulünü onaylayamazsınız.");
            var membership = await db.IsletmeUyelikleri.SingleOrDefaultAsync(x =>
                x.IsletmeId == receipt.AliciIsletmeId && x.KullaniciId == actor.Id && x.Durum == "Aktif", ct);
            if (membership is null || membership.Rol is not ("isletme_sahibi" or "mal_kabul_onaylayicisi") ||
                (membership.SubeId != null && membership.SubeId != receipt.SubeId) ||
                (membership.DepoId != null && membership.DepoId != receipt.DepoId))
                throw new UnauthorizedAccessException("İkinci onay için işletme sahibi veya atanmış mal kabul onaylayıcısı olmalısınız.");
        }
        var firstActor = await db.Kullanicilar.SingleOrDefaultAsync(x =>
            x.AuthProviderUserId == receipt.IslemYapanKullaniciRef, ct);
        if (receipt.IslemYapanKullaniciRef == identity.ProviderUserId || firstActor?.Id == actor.Id)
            throw new InvalidOperationException("İlk ve ikinci onayı iki farklı kişi vermelidir.");
        var order = await db.TedarikciSiparisleri.SingleAsync(x => x.Id == receipt.TedarikciSiparisId, ct);
        if (receipt.OnayDurumu == "Onaylandi" && receipt.IkinciOnaylayanKullaniciRef == identity.ProviderUserId)
            return new(receipt.Id, order.Durum, true, receipt.OnayDurumu);
        if (receipt.OnayDurumu != "OnayBekliyor")
            throw new InvalidOperationException("Bu kayıtta bekleyen ikinci onay yok.");
        if (order.Durum is PazaryeriSiparisDurumlari.IptalEdildi or PazaryeriSiparisDurumlari.IadeEdildi or PazaryeriSiparisDurumlari.Tamamlandi)
            throw new InvalidOperationException("Kapanmış siparişin mal kabulü onaylanamaz.");
        var row = await (from label in db.TedarikciSevkiyatEtiketleri
                         join line in db.TedarikciSevkiyatKalemleri on label.TedarikciSevkiyatKalemiId equals line.Id
                         join shipment in db.TedarikciSevkiyatlari on line.TedarikciSevkiyatId equals shipment.Id
                         join orderLine in db.TedarikciSiparisKalemleri on line.TedarikciSiparisKalemiId equals orderLine.Id
                         where label.Id == receipt.TedarikciSevkiyatEtiketiId
                         select new { label, shipment, orderLine }).SingleAsync(ct);
        if (row.label.Durum != "OnayBekliyor") throw new InvalidOperationException("Etiket onay durumuyla eşleşmiyor.");
        receipt.OnayDurumu = "Onaylandi";
        receipt.IkinciOnaylayanKullaniciRef = identity.ProviderUserId;
        receipt.IkinciOnayNotu = note;
        receipt.IkinciOnayAt = DateTime.UtcNow;
        await ApplyReceiptDecisionAsync(db, row.label, row.shipment, row.orderLine, order, receipt, ct);
        AddStateHistory(db, order, order.Durum, receipt.AliciIsletmeId,
            $"Mal kabul {receipt.Id}: ikinci onay {identity.ProviderUserId}. {note}");
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await tx.DisposeAsync();
        await DispatchOrderPayoutsAsync(order.Id, ct);
        await using var resultDb = await _dbFactory.CreateDbContextAsync(ct);
        var state = await resultDb.TedarikciSiparisleri.Where(x => x.Id == order.Id).Select(x => x.Durum).SingleAsync(ct);
        return new(receipt.Id, state, OnayDurumu: receipt.OnayDurumu);
    }

    public async Task RecordDisputeProgressAsync(
        int supplierOrderId, PazaryeriItirazIlerlemeRequest request, CancellationToken ct = default)
    {
        if (!await _yonetimService.IsCurrentUserAdminAsync(ct))
            throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekir.");
        var actor = _currentUserContext.GetCurrentUser()?.ProviderUserId;
        if (string.IsNullOrWhiteSpace(actor)) throw new UnauthorizedAccessException("Oturum açmalısınız.");
        var note = (request.Not ?? string.Empty).Trim();
        if (note.Length is < 1 or > 500) throw new ArgumentException("İnceleme notu 1-500 karakter olmalıdır.");
        if (request.Asama is not ("IlkYanit" or "KanitToplandi")) throw new ArgumentException("Geçerli bir inceleme aşaması seçin.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");
        if (order.Durum != PazaryeriSiparisDurumlari.Itirazli) throw new InvalidOperationException("Siparişte açık itiraz yok.");
        if (request.Asama == "IlkYanit")
        {
            if (order.ItirazIlkYanitAt != null) return;
            order.ItirazIlkYanitAt = DateTime.UtcNow;
        }
        else
        {
            if (order.ItirazKanitToplandiAt != null) return;
            order.ItirazKanitToplandiAt = DateTime.UtcNow;
        }
        AddStateHistory(db, order, order.Durum, order.AliciIsletmeId, $"Yönetici {actor}. İtiraz aşaması: {request.Asama}. {note}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<int> EscalateOverdueDisputesAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC zamanı gereklidir.", nameof(nowUtc));
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var orders = await db.TedarikciSiparisleri.Where(x => x.Durum == PazaryeriSiparisDurumlari.Itirazli &&
            x.ItirazYukseltildiAt == null).OrderBy(x => x.UpdatedAt).ToListAsync(ct);
        var escalated = 0;
        foreach (var order in orders)
        {
            var opened = order.ItirazAcildiAt ?? await db.TedarikciSiparisDurumKayitlari
                .Where(x => x.TedarikciSiparisId == order.Id && x.YeniDurum == PazaryeriSiparisDurumlari.Itirazli && x.OncekiDurum != x.YeniDurum)
                .OrderByDescending(x => x.Id).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct) ?? order.UpdatedAt;
            order.ItirazAcildiAt ??= opened;
            if ((order.ItirazIlkYanitAt ?? nowUtc) <= PazaryeriIsGunu.SonTarih(opened, 1, _options.IsGunuTatilTarihleri) &&
                (order.ItirazKanitToplandiAt ?? nowUtc) <= PazaryeriIsGunu.SonTarih(opened, 2, _options.IsGunuTatilTarihleri)) continue;
            order.ItirazYukseltildiAt = nowUtc;
            db.TedarikciSiparisDurumKayitlari.Add(new TedarikciSiparisDurumKaydi
            {
                TedarikciSiparisId = order.Id, OncekiDurum = order.Durum, YeniDurum = order.Durum,
                IslemYapanIsletmeId = order.AliciIsletmeId, CreatedAt = nowUtc,
                Aciklama = "İç itiraz hedef süresi aşıldı; Burak'ın yönetim incelemesine yükseltildi. Aktarım beklemeye devam eder."
            });
            var recipient = _options.ItirazYukseltmeKullaniciRef.Trim();
            if (recipient.Length > 0)
                db.BildirimKayitlari.Add(new BildirimKaydi
                {
                    IsletmeId = order.AliciIsletmeId, KullaniciRef = recipient,
                    KaynakAnahtari = $"marketplace-dispute:{order.Id}:{opened.Ticks}", Tur = "pazaryeri", Onem = "yuksek",
                    Baslik = "İtiraz süresi aşıldı", Mesaj = $"{order.SiparisNo}: yanıt veya kanıt hedefi aşıldı. Aktarım inceleme bitene kadar bekliyor.",
                    Aksiyon = "İtirazı incele", Url = "/app/tedarikci-pazaryeri", CreatedAt = nowUtc, UpdatedAt = nowUtc
                });
            escalated++;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return escalated;
    }
}
