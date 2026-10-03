using System.Data;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed partial class TedarikciPazaryeriService
{
    private static Task<bool> HasCorrectionHoldAsync(CashTrackerDbContext db, int orderId, CancellationToken ct) =>
        db.TedarikciMalKabulDuzeltmeleri.AnyAsync(x => x.TedarikciSiparisId == orderId &&
            (x.Durum == "OnayBekliyor" || (x.Durum == "Uygulandi" &&
                ((x.ParaDurumu != "Uzlasti" && x.ParaDurumu != "Uygulanmaz") || x.BelgeDurumu != "Tamamlandi" || x.StokDurumu != "Tamamlandi"))), ct);

    private async Task<string> RequireCorrectionAdminAsync(CashTrackerDbContext db, CancellationToken ct)
    {
        if (!await _yonetimService.IsCurrentUserAdminAsync(ct))
            throw new UnauthorizedAccessException("Kabul düzeltmesi için yönetici yetkisi gerekir.");
        var identity = _currentUserContext.GetCurrentUser();
        if (identity is null || !await db.Kullanicilar.AnyAsync(x => x.AuthProviderUserId == identity.ProviderUserId && x.Durum == "Aktif", ct))
            throw new UnauthorizedAccessException("Kabul düzeltmesi için aktif kullanıcı kaydı gerekir.");
        return identity.ProviderUserId;
    }

    private static string CorrectionNote(string? note)
    {
        var value = (note ?? string.Empty).Trim();
        if (value.Length is < 1 or > 500) throw new ArgumentException("Düzeltme gerekçesi 1-500 karakter olmalıdır.");
        return value;
    }

    public async Task<TedarikciMalKabulDuzeltmeSonucu> RequestReceiptCorrectionAsync(
        int receiptId, TedarikciMalKabulDuzeltmeRequest request, CancellationToken ct = default)
    {
        var note = CorrectionNote(request.Not);
        var key = (request.IdempotencyKey ?? string.Empty).Trim();
        if (key.Length is < 1 or > 100) throw new ArgumentException("Geçerli bir işlem anahtarı gerekir.");
        if (request.Miktar <= 0 || decimal.Round(request.Miktar, 3) != request.Miktar)
            throw new ArgumentException("Düzeltilecek miktar pozitif ve en fazla üç ondalıklı olmalıdır.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var actor = await RequireCorrectionAdminAsync(db, ct);
        var receipt = await db.TedarikciMalKabulleri.SingleOrDefaultAsync(x => x.Id == receiptId, ct)
            ?? throw new KeyNotFoundException("Mal kabul kaydı bulunamadı.");
        var existing = await db.TedarikciMalKabulDuzeltmeleri.SingleOrDefaultAsync(x =>
            x.AliciIsletmeId == receipt.AliciIsletmeId && x.IdempotencyAnahtari == key, ct);
        if (existing != null)
        {
            if (existing.TedarikciMalKabulId != receiptId || existing.Miktar != request.Miktar || existing.Not != note || existing.IslemYapanKullaniciRef != actor)
                throw new InvalidOperationException("Bu işlem anahtarı başka bir düzeltme için kullanıldı.");
            return new(existing.Id, existing.Durum, true);
        }
        if (receipt.OnayDurumu != "Onaylandi" || receipt.MuhasebelestiAt == null || receipt.KabulEdilenMiktar <= 0)
            throw new InvalidOperationException("Yalnız muhasebeleşmiş ve kesinleşmiş kabul düzeltilebilir.");
        var order = await db.TedarikciSiparisleri.SingleAsync(x => x.Id == receipt.TedarikciSiparisId, ct);
        if (order.Durum is PazaryeriSiparisDurumlari.IptalEdildi or PazaryeriSiparisDurumlari.IadeEdildi)
            throw new InvalidOperationException("İptal veya iade edilmiş sipariş için kabul düzeltmesi açılamaz.");
        if (await db.TedarikciMalKabulDuzeltmeleri.AnyAsync(x => x.TedarikciSiparisId == order.Id && x.Durum == "OnayBekliyor", ct))
            throw new InvalidOperationException("Bu siparişin bekleyen düzeltme kararı var.");
        if (await db.PazaryeriParaTalimatlari.AnyAsync(x => x.Tur == "Iade" && db.PazaryeriOdemeleri.Any(p => p.Id == x.PazaryeriOdemeId && p.AnaSiparisId == order.AnaSiparisId), ct) ||
            await db.PazaryeriOdemeDagitimlari.AnyAsync(x => x.TedarikciSiparisId == order.Id && x.IadeTutari > 0, ct))
            throw new InvalidOperationException("Mevcut ödeme iadesi önce uzlaştırılmalıdır.");
        var applied = await db.TedarikciMalKabulDuzeltmeleri.Where(x => x.TedarikciMalKabulId == receiptId && x.Durum == "Uygulandi")
            .Select(x => x.Miktar).ToListAsync(ct);
        if (request.Miktar > receipt.KabulEdilenMiktar - applied.Sum())
            throw new InvalidOperationException("Düzeltilecek miktar kalan kabulü aşamaz.");
        var priorReductions = await db.TedarikciMalKabulDuzeltmeleri.Where(x => x.TedarikciSiparisId == order.Id && x.Durum == "Uygulandi")
            .Select(x => x.HakEdisAzaltimi).ToListAsync(ct);
        var currentSettlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        if (currentSettlement == null && await db.PazaryeriOdemeleri.AnyAsync(x => x.AnaSiparisId == order.AnaSiparisId, ct))
            throw new InvalidOperationException("Ödemenin hakediş bağlantısı eksik; önce referansları uzlaştırın.");
        if (currentSettlement != null && currentSettlement.NetTutar != Money(order.TedarikciHakEdisi - priorReductions.Sum()))
            throw new InvalidOperationException("Önceki mali karar hakedişi değiştirmiş; kabul düzeltmesi öncesinde bu kararı uzlaştırın.");
        var correction = new TedarikciMalKabulDuzeltme
        {
            TedarikciMalKabulId = receiptId, TedarikciSiparisId = order.Id, AliciIsletmeId = receipt.AliciIsletmeId,
            IdempotencyAnahtari = key, Miktar = request.Miktar, Not = note, IslemYapanKullaniciRef = actor
        };
        db.TedarikciMalKabulDuzeltmeleri.Add(correction);
        await db.SaveChangesAsync(ct);
        AddStateHistory(db, order, order.Durum, order.AliciIsletmeId, $"Kabul #{receiptId}, düzeltme #{correction.Id}: ikinci kişi onayı bekleniyor. {note}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(correction.Id, correction.Durum);
    }

    public async Task<TedarikciMalKabulDuzeltmeSonucu> DecideReceiptCorrectionAsync(
        int correctionId, TedarikciMalKabulDuzeltmeKararRequest request, CancellationToken ct = default)
    {
        var note = CorrectionNote(request.Not);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var actor = await RequireCorrectionAdminAsync(db, ct);
        var correction = await db.TedarikciMalKabulDuzeltmeleri.SingleOrDefaultAsync(x => x.Id == correctionId, ct)
            ?? throw new KeyNotFoundException("Kabul düzeltmesi bulunamadı.");
        if (actor == correction.IslemYapanKullaniciRef) throw new InvalidOperationException("Düzeltmeyi iki farklı kişi onaylamalıdır.");
        var desiredState = request.Onaylandi ? "Uygulandi" : "Reddedildi";
        if (correction.Durum != "OnayBekliyor")
        {
            if (correction.Durum == desiredState && correction.OnaylayanKullaniciRef == actor && correction.OnayNotu == note)
                return new(correction.Id, correction.Durum, true);
            throw new InvalidOperationException("Bu düzeltme için karar verilmiş.");
        }
        var order = await db.TedarikciSiparisleri.SingleAsync(x => x.Id == correction.TedarikciSiparisId, ct);
        if (request.Onaylandi) await ApplyReceiptCorrectionAsync(db, order, correction, ct);
        else
        {
            correction.ParaDurumu = correction.BelgeDurumu = correction.StokDurumu = "Uygulanmaz";
            // A provider response may have arrived while this request was awaiting its second person.
            if (!await db.TedarikciMalKabulDuzeltmeleri.AnyAsync(x => x.Id != correction.Id && x.TedarikciSiparisId == order.Id &&
                    (x.Durum == "OnayBekliyor" || x.Durum == "Uygulandi"), ct))
            {
                var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
                if (settlement?.Durum == "DuzeltmeIncelemesi")
                {
                    var payouts = await db.PazaryeriParaTalimatlari.Where(x => x.TedarikciSiparisId == order.Id && x.Tur == "Aktarim").ToListAsync(ct);
                    if (payouts.Any(x => x.Durum is "Gonderiliyor" or "SonucBekliyor" or "IncelemeGerekli")) settlement.Durum = "MutabakatFarki";
                    else if (settlement.OdenenTutar >= settlement.NetTutar && settlement.NetTutar > 0)
                    {
                        settlement.Durum = "SerbestBirakildi";
                        settlement.TamamlandiAt = DateTime.UtcNow;
                        if (order.Durum == PazaryeriSiparisDurumlari.HakEdisBekliyor && !await db.TedarikciSiparisSikayetleri.AnyAsync(x => x.TedarikciSiparisId == order.Id && x.Durum != "Cozuldu", ct))
                            AddStateHistory(db, order, PazaryeriSiparisDurumlari.Tamamlandi, order.AliciIsletmeId, "Kabul düzeltmesi reddedildi; doğrulanmış aktarım tamamlandı.");
                    }
                    else settlement.Durum = payouts.Any(x => x.Durum == "Hazir") ? "AktarimBekliyor" : payouts.Any(x => x.Durum == "KesinBasarisiz") ? "AktarimBasarisiz" : "KismenSerbest";
                    settlement.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        correction.Durum = desiredState;
        correction.OnaylayanKullaniciRef = actor;
        correction.OnayNotu = note;
        correction.OnayAt = DateTime.UtcNow;
        AddStateHistory(db, order, order.Durum, order.AliciIsletmeId, $"Kabul düzeltmesi #{correction.Id}: {desiredState}, yönetici {actor}. {note}");
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(correction.Id, correction.Durum);
    }

    private static async Task ApplyReceiptCorrectionAsync(CashTrackerDbContext db, TedarikciSiparis order,
        TedarikciMalKabulDuzeltme correction, CancellationToken ct)
    {
        var receipt = await db.TedarikciMalKabulleri.SingleAsync(x => x.Id == correction.TedarikciMalKabulId, ct);
        var line = await (from label in db.TedarikciSevkiyatEtiketleri
                          join shipmentLine in db.TedarikciSevkiyatKalemleri on label.TedarikciSevkiyatKalemiId equals shipmentLine.Id
                          join orderLine in db.TedarikciSiparisKalemleri on shipmentLine.TedarikciSiparisKalemiId equals orderLine.Id
                          where label.Id == receipt.TedarikciSevkiyatEtiketiId && orderLine.TedarikciSiparisId == order.Id
                          select orderLine).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Kabulün sipariş kalemi bağlantısı eksik; önce referansları uzlaştırın.");
        var prior = await db.TedarikciMalKabulDuzeltmeleri.Where(x => x.TedarikciSiparisId == order.Id && x.Durum == "Uygulandi").ToListAsync(ct);
        var receiptPrior = prior.Where(x => x.TedarikciMalKabulId == receipt.Id).ToList();
        var cumulativeQuantity = receiptPrior.Sum(x => x.Miktar) + correction.Miktar;
        if (receipt.OnayDurumu != "Onaylandi" || receipt.MuhasebelestiAt == null || cumulativeQuantity > receipt.KabulEdilenMiktar || correction.Miktar > line.KabulEdilenMiktar)
            throw new InvalidOperationException("Düzeltme kalan kesin kabul miktarıyla eşleşmiyor.");
        var originalStock = await db.StokHareketleri.SingleOrDefaultAsync(x => x.TedarikciMalKabulId == receipt.Id && x.IsletmeId == order.AliciIsletmeId && x.Kaynak == "PazaryeriMalKabul", ct)
            ?? throw new InvalidOperationException("Kabulün stok bağlantısı eksik; önce referansları uzlaştırın.");
        var defaultDepot = await db.StokDepolari.Where(x => x.IsletmeId == originalStock.IsletmeId && x.Varsayilan && x.Aktif)
            .Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        var depot = originalStock.DepoId ?? defaultDepot;
        var includeLegacy = depot == null || depot == defaultDepot;
        var quantities = await db.StokHareketleri.Where(x => x.IsletmeId == originalStock.IsletmeId && x.UrunHizmetId == originalStock.UrunHizmetId &&
            ((depot != null && x.DepoId == depot) || (includeLegacy && x.DepoId == null && x.SubeId == originalStock.SubeId)))
            .Select(x => new { x.Miktar, x.RezerveMiktar }).ToListAsync(ct);
        if (quantities.Sum(x => x.Miktar - x.RezerveMiktar) < correction.Miktar)
            throw new InvalidOperationException("Kabulün deposunda düzeltme için yeterli kullanılabilir stok yok; önce stok konumunu uzlaştırın.");
        var originalNet = Money(line.BirimFiyat * receipt.KabulEdilenMiktar);
        correction.NetTutar = Money(originalNet * cumulativeQuantity / receipt.KabulEdilenMiktar) - receiptPrior.Sum(x => x.NetTutar);
        correction.BrutTutar = Money(receipt.KabulBrutTutar * cumulativeQuantity / receipt.KabulEdilenMiktar) - receiptPrior.Sum(x => x.BrutTutar);
        correction.KdvTutar = correction.BrutTutar - correction.NetTutar;
        var previousGross = prior.Sum(x => x.BrutTutar);
        if (order.GenelToplam <= 0 || previousGross + correction.BrutTutar > order.GenelToplam)
            throw new InvalidOperationException("Düzeltme tutarı sipariş tutarıyla eşleşmiyor.");
        var previousNet = prior.Sum(x => x.NetTutar);
        decimal DeltaGross(decimal original) => Money(original * (previousGross + correction.BrutTutar) / order.GenelToplam) - Money(original * previousGross / order.GenelToplam);
        decimal DeltaNet(decimal original, decimal basis)
        {
            if (original == 0) return 0;
            if (basis <= 0 || previousNet + correction.NetTutar > basis)
                throw new InvalidOperationException("Düzeltmenin vergi ve komisyon matrahı eşleşmiyor; mali inceleme gerekiyor.");
            return Money(original * (previousNet + correction.NetTutar) / basis) - Money(original * previousNet / basis);
        }
        var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        var commission = settlement == null ? 0 : DeltaNet(order.KomisyonTutari, order.KomisyonMatrahi);
        var commissionVat = settlement == null ? 0 : DeltaNet(order.KomisyonKdvTutari, order.KomisyonMatrahi);
        var withholding = settlement == null ? 0 : DeltaNet(order.TevkifatTutari, order.TevkifatMatrahi);
        var supplierFee = settlement == null ? 0 : DeltaGross(order.OdemeHizmetiBedeli);
        correction.HakEdisAzaltimi = settlement == null ? 0m : Money(correction.BrutTutar - commission - commissionVat - withholding - supplierFee);
        if (settlement != null && Money(order.GenelToplam - order.KomisyonTutari - order.KomisyonKdvTutari - order.TevkifatTutari - order.OdemeHizmetiBedeli) != order.TedarikciHakEdisi)
            throw new InvalidOperationException("Hakedişin başlangıç tutarı mali kayıtlarla eşleşmiyor; önce referansları uzlaştırın.");
        if (correction.HakEdisAzaltimi < 0)
            throw new InvalidOperationException("Bu miktarın kuruş yuvarlaması mali inceleme gerektiriyor.");
        if (settlement == null && await db.PazaryeriOdemeleri.AnyAsync(x => x.AnaSiparisId == order.AnaSiparisId, ct))
            throw new InvalidOperationException("Ödemenin hakediş bağlantısı eksik; önce referansları uzlaştırın.");
        if (settlement != null && (settlement.NetTutar != Money(order.TedarikciHakEdisi - prior.Sum(x => x.HakEdisAzaltimi)) || correction.HakEdisAzaltimi > settlement.NetTutar))
            throw new InvalidOperationException("Hakediş düzeltmesi önceki mali kararla çakışıyor; uzlaştırma gerekir.");
        var match = await db.TedarikciFaturaEslesmeleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct)
            ?? throw new InvalidOperationException("Kabulün fatura bağlantısı eksik; önce referansları uzlaştırın.");
        var invoices = await db.Faturalar.Where(x => x.Id == match.AliciFaturaId || x.Id == match.SaticiFaturaId).ToListAsync(ct);
        if (invoices.Count != 2 || !invoices.Any(x => x.Id == match.AliciFaturaId && x.IsletmeId == order.AliciIsletmeId && x.ParaBirimi == order.ParaBirimi) ||
            !invoices.Any(x => x.Id == match.SaticiFaturaId && x.IsletmeId == order.TedarikciIsletmeId && x.ParaBirimi == order.ParaBirimi))
            throw new InvalidOperationException("Fatura bağlantısı siparişle eşleşmiyor; önce referansları uzlaştırın.");
        correction.AliciFaturaId = match.AliciFaturaId;
        correction.SaticiFaturaId = match.SaticiFaturaId;
        var originalCari = await db.CariHareketleri.Where(x => x.TedarikciMalKabulId == receipt.Id && x.Kaynak == "PazaryeriMalKabul").ToListAsync(ct);
        foreach (var businessId in new[] { order.AliciIsletmeId, order.TedarikciIsletmeId })
        {
            var sources = originalCari.Where(x => x.IsletmeId == businessId).ToList();
            if (sources.Count != 1) throw new InvalidOperationException("Kabulün cari bağlantısı belirsiz; önce referansları uzlaştırın.");
            var source = sources[0];
            var sourceInvoice = invoices.Single(x => x.IsletmeId == businessId);
            if (source.Tutar != receipt.KabulBrutTutar || source.CariKartId != sourceInvoice.CariKartId || source.ParaBirimi != order.ParaBirimi ||
                source.HareketTipi != (businessId == order.AliciIsletmeId ? "Borc" : "Alacak"))
                throw new InvalidOperationException("Kabulün cari ve fatura bağlantısı eşleşmiyor; önce referansları uzlaştırın.");
            db.CariHareketleri.Add(new CariHareket
            {
                IsletmeId = businessId, SubeId = source.SubeId, CariKartId = source.CariKartId, TedarikciSiparisId = order.Id,
                TedarikciMalKabulId = receipt.Id, TedarikciMalKabulDuzeltmeId = correction.Id, Tarih = DateTime.Now,
                HareketTipi = source.HareketTipi == "Borc" ? "Alacak" : "Borc", Tutar = correction.BrutTutar,
                ParaBirimi = source.ParaBirimi, KurSnapshot = source.KurSnapshot, TryKarsiligi = Money(correction.BrutTutar * source.KurSnapshot),
                Kaynak = "PazaryeriKabulDuzeltme", Aciklama = $"Kabul #{receipt.Id}, düzeltme #{correction.Id}. {correction.Not}"
            });
        }
        db.StokHareketleri.Add(new StokHareket
        {
            IsletmeId = originalStock.IsletmeId, SubeId = originalStock.SubeId, DepoId = originalStock.DepoId,
            UrunHizmetId = originalStock.UrunHizmetId, TedarikciSiparisId = order.Id, TedarikciMalKabulId = receipt.Id,
            TedarikciMalKabulDuzeltmeId = correction.Id, Tarih = DateTime.Now, Miktar = -correction.Miktar, HareketTipi = "Cikis",
            BirimMaliyet = originalStock.BirimMaliyet, MaliyetParaBirimi = originalStock.MaliyetParaBirimi,
            MaliyetKurSnapshot = originalStock.MaliyetKurSnapshot, BirimMaliyetTry = originalStock.BirimMaliyetTry,
            Kaynak = "PazaryeriKabulDuzeltme", Aciklama = $"Kabul #{receipt.Id}, düzeltme #{correction.Id}. İade konumu uzlaştırılacak."
        });
        void Reverse(string account, string direction, decimal amount)
        {
            if (amount <= 0) return;
            db.PazaryeriDefterKayitlari.Add(new PazaryeriDefterKaydi { TedarikciSiparisId = order.Id,
                TedarikciMalKabulDuzeltmeId = correction.Id, Hesap = account, Yon = direction, Tutar = amount,
                ParaBirimi = order.ParaBirimi, Aciklama = $"Kabul düzeltmesi #{correction.Id}; ödeme sonucu değildir." });
        }
        if (settlement != null)
        {
            Reverse("BrutSatis", "Borc", correction.BrutTutar);
            Reverse("TedarikciHakEdisi", "Borc", correction.HakEdisAzaltimi);
            Reverse("Komisyon", "Alacak", commission);
            Reverse("KomisyonKdv", "Alacak", commissionVat);
            Reverse("Tevkifat", "Alacak", withholding);
            Reverse("OdemeHizmeti", "Alacak", supplierFee);
            correction.TedarikcidenGeriAlinacakTutar = Money(Math.Max(0, settlement.OdenenTutar - (settlement.NetTutar - correction.HakEdisAzaltimi)) - Math.Max(0, settlement.OdenenTutar - settlement.NetTutar));
            settlement.NetTutar = Money(settlement.NetTutar - correction.HakEdisAzaltimi);
            settlement.Durum = "DuzeltmeIncelemesi";
            settlement.TamamlandiAt = null;
            settlement.UpdatedAt = DateTime.UtcNow;
        }
        var hasInvoicePayment = invoices.Any(x => x.OdenenTutar > 0) || await db.TahsilatOdemeleri.AnyAsync(x => x.Tutar > 0 &&
            (x.FaturaId == match.AliciFaturaId || x.FaturaId == match.SaticiFaturaId), ct);
        correction.ParaDurumu = settlement == null && !hasInvoicePayment ? "Uygulanmaz" : "IncelemeBekliyor";
        correction.BelgeDurumu = "BelgeBekliyor";
        correction.StokDurumu = "IadeKonumuBekliyor";
        line.KabulEdilenMiktar -= correction.Miktar;
        line.ReddedilenMiktar += correction.Miktar;
        if (order.Durum != PazaryeriSiparisDurumlari.Itirazli)
        {
            order.ItirazAcildiAt = DateTime.UtcNow;
            order.ItirazIlkYanitAt = order.ItirazKanitToplandiAt = order.ItirazYukseltildiAt = null;
        }
        AddStateHistory(db, order, PazaryeriSiparisDurumlari.Itirazli, order.AliciIsletmeId, $"Kabul düzeltmesi #{correction.Id}: stok/cari ters kayıtları uygulandı; belge, iade konumu ve ödeme uzlaştırması bekliyor.");
    }
}
