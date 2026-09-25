using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Security;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Systemcel.Api.Api;

internal static class TedarikciPazaryeriApi
{
    private sealed record ApiHata(string mesaj);
    private sealed record YonetimSiparisiSatiri(
        int Id,
        string SiparisNo,
        string TedarikciUnvani,
        string Durum,
        decimal GenelToplam,
        string ParaBirimi,
        string HakedisDurumu,
        DateTime? PlanlananAt,
        string YonetimNedeni);

    internal sealed record SikayetZamanSatiri(
        int TedarikciSiparisId,
        int SikayetId,
        DateTime CreatedAt,
        DateTime? YanitlandiAt);

    internal static (decimal? ItirazYasiSaat, decimal? TedarikciIlkYanitSuresiSaat) HesaplaItirazSureleri(
        DateTime? sonItirazGecisiAt,
        IEnumerable<SikayetZamanSatiri> sikayetler,
        int tedarikciSiparisId,
        DateTime utcNow)
    {
        static decimal Saat(DateTime baslangic, DateTime bitis) =>
            Math.Round(Math.Max(0m, (decimal)(bitis - baslangic).TotalHours), 1, MidpointRounding.AwayFromZero);

        var siparisSikayetleri = sikayetler
            .Where(x => x.TedarikciSiparisId == tedarikciSiparisId)
            .ToArray();
        var ilkSikayet = siparisSikayetleri
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.SikayetId)
            .FirstOrDefault();
        var ilkYanitlananSikayet = siparisSikayetleri
            .Where(x => x.YanitlandiAt is not null)
            .OrderBy(x => x.YanitlandiAt)
            .ThenBy(x => x.SikayetId)
            .FirstOrDefault();
        var sikayetOlusturulduAt = ilkSikayet?.CreatedAt;
        var itirazBaslangici = sonItirazGecisiAt ?? sikayetOlusturulduAt;
        decimal? itirazYasiSaat = itirazBaslangici is null ? null : Saat(itirazBaslangici.Value, utcNow);
        decimal? tedarikciIlkYanitSuresiSaat = ilkYanitlananSikayet is not null
            ? Saat(ilkYanitlananSikayet.CreatedAt, ilkYanitlananSikayet.YanitlandiAt!.Value)
            : null;
        return (itirazYasiSaat, tedarikciIlkYanitSuresiSaat);
    }

    public static void MapTedarikciPazaryeriApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/tedarikci-pazaryeri", async (IIsletmeService isletmeler, ISystemcelYonetimService yonetim, ICurrentUserContext currentUser, IMarketplacePaymentGateway paymentGateway, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            var aktif = await isletmeler.GetActiveAsync();
            await using var db = await factory.CreateDbContextAsync(ct);
            var profiller = await db.TedarikciProfilleri.AsNoTracking().Where(x => x.Yayinda)
                .OrderByDescending(x => x.Dogrulandi).ThenBy(x => x.Unvan)
                .Select(x => new { x.Id, x.Unvan, x.Kategoriler, x.Sehir, x.Aciklama, x.Dogrulandi }).ToListAsync(ct);
            var talepler = await db.TedarikAlimTalepleri.AsNoTracking().Where(x => x.AliciIsletmeId == aktif.Id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.Baslik, x.Kategori, x.UrunHizmet, x.Miktar, x.Birim, x.TeslimatSehri, x.SonTeklifAt, x.Aciklama, x.Durum, teklifSayisi = db.TedarikTeklifleri.Count(t => t.TalepId == x.Id) }).ToListAsync(ct);
            var acikTalepler = await db.TedarikAlimTalepleri.AsNoTracking().Where(x => x.AliciIsletmeId != aktif.Id && x.Durum == "Acik" && x.SonTeklifAt > DateTime.UtcNow)
                .OrderBy(x => x.SonTeklifAt).Select(x => new { x.Id, x.Baslik, x.Kategori, x.UrunHizmet, x.Miktar, x.Birim, x.TeslimatSehri, x.SonTeklifAt, x.Aciklama, teklifVerildi = db.TedarikTeklifleri.Any(t => t.TalepId == x.Id && t.TedarikciIsletmeId == aktif.Id) }).ToListAsync(ct);
            var gelenTeklifler = await (from t in db.TedarikTeklifleri.AsNoTracking() join a in db.TedarikAlimTalepleri on t.TalepId equals a.Id join p in db.TedarikciProfilleri on t.TedarikciIsletmeId equals p.IsletmeId where a.AliciIsletmeId == aktif.Id select new { t.Id, t.TalepId, talepBasligi = a.Baslik, t.BirimFiyat, t.KdvOrani, t.ParaBirimi, t.TerminGun, t.MinimumSiparis, t.Not, t.Durum, tedarikciUnvani = p.Unvan }).OrderBy(x => x.BirimFiyat).ToListAsync(ct);
            var profil = await db.TedarikciProfilleri.AsNoTracking().Where(x => x.IsletmeId == aktif.Id)
                .Select(x => new { x.Id, x.Unvan, x.Kategoriler, x.Sehir, x.Aciklama, x.VergiNo, x.MersisNo, x.KepAdresi, x.Iban, x.Adres, x.YetkiliAdSoyad, x.VergiDurumu, x.SevkiyatBolgeleri, x.IadeKosullari, x.PazaryeriSozlesmeVersiyonu, x.PspAltUyeIsyeriId, x.KomisyonOrani, x.OdemeVadesiGun, x.TevkifatMuaf, x.DogrulamaDurumu, x.DogrulamaNotu, x.Dogrulandi, x.Yayinda }).SingleOrDefaultAsync(ct);
            var urunler = await (from u in db.TedarikciUrunleri.AsNoTracking()
                join p in db.TedarikciProfilleri.AsNoTracking() on u.TedarikciProfilId equals p.Id
                where u.Aktif && p.Dogrulandi && p.Yayinda && u.StokMiktari > u.RezerveMiktar
                orderby u.Kategori, u.Ad
                select new { u.Id, u.TedarikciProfilId, tedarikciUnvani = p.Unvan, tedarikciSehri = p.Sehir, p.SevkiyatBolgeleri, p.IadeKosullari, u.Sku, u.Ad, u.Aciklama, u.Kategori, u.Birim, u.BirimFiyat, u.KdvOrani, u.ParaBirimi, kullanilabilirStok = u.StokMiktari - u.RezerveMiktar, u.MinimumSiparisMiktari, u.TahminiTeslimatGun }).ToListAsync(ct);
            var benimUrunlerim = await db.TedarikciUrunleri.AsNoTracking().Where(x => x.TedarikciIsletmeId == aktif.Id && !x.Sku.StartsWith("RFQ-"))
                .OrderBy(x => x.Ad).Select(x => new { x.Id, x.KaynakUrunHizmetId, x.Sku, x.Ad, x.Aciklama, x.Kategori, x.Birim, x.BirimFiyat, x.KdvOrani, x.ParaBirimi, x.StokMiktari, x.RezerveMiktar, x.MinimumSiparisMiktari, x.TahminiTeslimatGun, x.Aktif }).ToListAsync(ct);
            var kaynakUrunler = await db.UrunHizmetleri.AsNoTracking().Where(x => x.IsletmeId == aktif.Id && x.Aktif && x.Tip == "Urun")
                .OrderBy(x => x.Ad).Select(x => new { x.Id, x.Ad, x.Barkod, x.Birim, x.KdvOrani, x.SatisFiyati, x.ParaBirimi }).ToListAsync(ct);
            var siparisRows = await (from s in db.TedarikciSiparisleri.AsNoTracking()
                join a in db.PazaryeriAnaSiparisleri.AsNoTracking() on s.AnaSiparisId equals a.Id
                join p in db.TedarikciProfilleri.AsNoTracking() on s.TedarikciProfilId equals p.Id
                where s.AliciIsletmeId == aktif.Id || s.TedarikciIsletmeId == aktif.Id
                orderby s.CreatedAt descending
                select new { s.Id, s.AnaSiparisId, anaSiparisNo = a.SiparisNo, s.SiparisNo, a.TeslimatAdresi, s.AliciIsletmeId, s.TedarikciIsletmeId, tedarikciUnvani = p.Unvan, s.AraToplam, s.KdvToplam, s.GenelToplam, s.ParaBirimi, s.KomisyonTutari, s.KomisyonKdvTutari, s.TevkifatTutari, s.OdemeHizmetiBedeli, s.TedarikciHakEdisi, s.Durum, s.KargoFirmasi, s.KargoTakipNo, s.CreatedAt, malKabulVar = s.TeslimEdildiAt != null || db.TedarikciMalKabulleri.Any(r => r.TedarikciSiparisId == s.Id) }).ToListAsync(ct);
            var siparisIds = siparisRows.Select(x => x.Id).ToList();
            var siparisKalemleri = await db.TedarikciSiparisKalemleri.AsNoTracking().Where(x => siparisIds.Contains(x.TedarikciSiparisId))
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.TedarikciSiparisId, x.TedarikciUrunId, x.Sku, x.Ad, x.Birim, x.Miktar, x.SevkEdilenMiktar, x.KabulEdilenMiktar, x.ReddedilenMiktar, x.BirimFiyat, x.KdvOrani, x.NetTutar, x.KdvTutari, x.ToplamTutar }).ToListAsync(ct);
            var sevkiyatlar = await db.TedarikciSevkiyatlari.AsNoTracking().Where(x => siparisIds.Contains(x.TedarikciSiparisId))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.TedarikciSiparisId, x.SevkiyatNo, x.TasimaTipi, x.Tasiyici, x.BelgeNo, x.BelgeUuid, x.BelgeDosyaYolu, x.SevkAt, x.AracPlaka, x.SurucuAdi, x.CikisDeposu, x.VarisDeposu, x.RandevuAt, x.PlanlananTeslimAt, x.Not, x.Durum, x.CreatedAt,
                    etiketSayisi = (from k in db.TedarikciSevkiyatKalemleri where k.TedarikciSevkiyatId == x.Id join e in db.TedarikciSevkiyatEtiketleri on k.Id equals e.TedarikciSevkiyatKalemiId select e).Count(),
                    okutulanEtiketSayisi = (from k in db.TedarikciSevkiyatKalemleri where k.TedarikciSevkiyatId == x.Id join e in db.TedarikciSevkiyatEtiketleri on k.Id equals e.TedarikciSevkiyatKalemiId where e.Durum != "Hazir" select e).Count() }).ToListAsync(ct);
            var anaSiparisler = await db.PazaryeriAnaSiparisleri.AsNoTracking().Where(x => x.AliciIsletmeId == aktif.Id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.SiparisNo, x.TeslimatAdresi, x.AraToplam, x.KdvToplam, x.GenelToplam, x.ParaBirimi, x.Durum, x.CreatedAt }).ToListAsync(ct);
            var hakedisler = await (from h in db.TedarikciHakEdisleri.AsNoTracking()
                join s in db.TedarikciSiparisleri.AsNoTracking() on h.TedarikciSiparisId equals s.Id
                where h.TedarikciIsletmeId == aktif.Id || s.AliciIsletmeId == aktif.Id
                orderby h.CreatedAt descending
                select new { h.Id, h.TedarikciSiparisId, s.SiparisNo, h.BrutTutar, h.KomisyonTutari, h.KomisyonKdvTutari, h.TevkifatTutari, h.OdemeHizmetiBedeli, h.IadeTutari, h.NetTutar, h.OdenenTutar, h.ParaBirimi, h.Durum, h.AktarimReferansi, h.PlanlananAt, h.TamamlandiAt }).ToListAsync(ct);
            var malKabulHareketleri = await (from receipt in db.TedarikciMalKabulleri.AsNoTracking()
                join receiptOrder in db.TedarikciSiparisleri.AsNoTracking() on receipt.TedarikciSiparisId equals receiptOrder.Id
                where receiptOrder.TedarikciIsletmeId == aktif.Id
                orderby receipt.CreatedAt descending
                select new { receipt.Id, receipt.TedarikciSiparisId, receipt.KabulEdilenMiktar, receipt.ReddedilenMiktar,
                    receipt.KabulBrutTutar, receipt.SerbestBirakilanNetTutar, receipt.HakEdisAktarimReferansi,
                    receipt.HakEdisAktarimHatasi, receipt.CreatedAt }).ToListAsync(ct);
            var sikayetler = await (from complaint in db.TedarikciSiparisSikayetleri.AsNoTracking()
                join order in db.TedarikciSiparisleri.AsNoTracking() on complaint.TedarikciSiparisId equals order.Id
                join profileRow in db.TedarikciProfilleri.AsNoTracking() on complaint.TedarikciIsletmeId equals profileRow.IsletmeId
                where complaint.AliciIsletmeId == aktif.Id || complaint.TedarikciIsletmeId == aktif.Id
                orderby complaint.UpdatedAt descending
                select new { complaint.Id, complaint.TedarikciSiparisId, order.SiparisNo, tedarikciUnvani = profileRow.Unvan,
                    complaint.AliciIsletmeId, complaint.TedarikciIsletmeId, complaint.Kategori, complaint.Aciklama,
                    complaint.Talep, complaint.Durum, complaint.TedarikciYaniti, complaint.KapanisNotu,
                    complaint.YanitlandiAt, complaint.KapatildiAt, complaint.CreatedAt, complaint.UpdatedAt }).ToListAsync(ct);
            var degerlendirmeler = await db.TedarikciDegerlendirmeleri.AsNoTracking()
                .Where(x => x.AliciIsletmeId == aktif.Id)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new { x.Id, x.TedarikciSiparisId, x.TedarikciIsletmeId, x.UrunUygunluguPuani,
                    x.EksiksizTeslimatPuani, x.HasarsizTeslimatPuani, x.ZamanindaTeslimatPuani,
                    x.SorunCozmePuani, x.OrtalamaPuan, x.Yorum, x.UpdatedAt }).ToListAsync(ct);
            var subeler = await db.Subeler.AsNoTracking().Where(x => x.IsletmeId == aktif.Id && x.Aktif)
                .OrderByDescending(x => x.Varsayilan).ThenBy(x => x.Ad)
                .Select(x => new { x.Id, x.Ad, x.Kod, x.Varsayilan }).ToListAsync(ct);
            var depolar = await db.StokDepolari.AsNoTracking().Where(x => x.IsletmeId == aktif.Id && x.Aktif)
                .OrderByDescending(x => x.Varsayilan).ThenBy(x => x.Ad)
                .Select(x => new { x.Id, x.SubeId, x.Ad, x.Kod, x.Varsayilan }).ToListAsync(ct);
            var identity = currentUser.GetCurrentUser();
            var malKabulUyelik = identity is null ? null : await (
                from membership in db.IsletmeUyelikleri.AsNoTracking()
                join user in db.Kullanicilar.AsNoTracking() on membership.KullaniciId equals user.Id
                where membership.IsletmeId == aktif.Id && membership.Durum == "Aktif" &&
                      user.AuthProviderUserId == identity.ProviderUserId &&
                      (membership.Rol == "isletme_sahibi" || membership.Rol == "yonetici" ||
                       membership.Rol == "depo_sorumlusu" || membership.Rol == "mal_kabul_onaylayicisi")
                select new { membership.Id, membership.SubeId, membership.DepoId }).SingleOrDefaultAsync(ct);
            var malKabulYetkisi = malKabulUyelik is not null;
            if (malKabulUyelik is not null && (malKabulUyelik.SubeId is not null || malKabulUyelik.DepoId is not null))
            {
                subeler = subeler.Where(x => malKabulUyelik.SubeId == null || x.Id == malKabulUyelik.SubeId).ToList();
                depolar = depolar.Where(x => (malKabulUyelik.SubeId == null || x.SubeId == malKabulUyelik.SubeId) &&
                                             (malKabulUyelik.DepoId == null || x.Id == malKabulUyelik.DepoId)).ToList();
            }
            var tedarikciPerformanslari = await db.TedarikciProfilleri.AsNoTracking().Where(x => x.Yayinda)
                .Select(x => new
                {
                    tedarikciProfilId = x.Id,
                    tedarikciIsletmeId = x.IsletmeId,
                    puan = (from rating in db.TedarikciDegerlendirmeleri
                            join ratingOrder in db.TedarikciSiparisleri on rating.TedarikciSiparisId equals ratingOrder.Id
                            where rating.TedarikciIsletmeId == x.IsletmeId && ratingOrder.Durum != PazaryeriSiparisDurumlari.IptalEdildi && ratingOrder.Durum != PazaryeriSiparisDurumlari.IadeEdildi
                            select (decimal?)rating.OrtalamaPuan).Average() ?? 0m,
                    degerlendirmeSayisi = (from rating in db.TedarikciDegerlendirmeleri
                                           join ratingOrder in db.TedarikciSiparisleri on rating.TedarikciSiparisId equals ratingOrder.Id
                                           where rating.TedarikciIsletmeId == x.IsletmeId && ratingOrder.Durum != PazaryeriSiparisDurumlari.IptalEdildi && ratingOrder.Durum != PazaryeriSiparisDurumlari.IadeEdildi
                                           select rating.Id).Count(),
                    sorunBildirimiSayisi = db.TedarikciSiparisSikayetleri.Count(s => s.TedarikciIsletmeId == x.IsletmeId)
                }).ToListAsync(ct);
            var kabulZamanlari = await (from receipt in db.TedarikciMalKabulleri.AsNoTracking()
                join label in db.TedarikciSevkiyatEtiketleri.AsNoTracking() on receipt.TedarikciSevkiyatEtiketiId equals label.Id
                join shipmentLine in db.TedarikciSevkiyatKalemleri.AsNoTracking() on label.TedarikciSevkiyatKalemiId equals shipmentLine.Id
                where siparisIds.Contains(receipt.TedarikciSiparisId)
                group receipt by shipmentLine.TedarikciSevkiyatId into receipts
                select new { sevkiyatId = receipts.Key, ilkKabulAt = receipts.Min(x => x.CreatedAt) }).ToListAsync(ct);
            var kabulZamaniBySevkiyat = kabulZamanlari.ToDictionary(x => x.sevkiyatId, x => x.ilkKabulAt);
            var kabulEdilenToplam = siparisKalemleri.Sum(x => x.KabulEdilenMiktar);
            var reddedilenToplam = siparisKalemleri.Sum(x => x.ReddedilenMiktar);
            var sevkEdilenToplam = siparisKalemleri.Sum(x => x.SevkEdilenMiktar);
            var zamanliAdaylar = sevkiyatlar.Where(x => x.PlanlananTeslimAt != null && kabulZamaniBySevkiyat.ContainsKey(x.Id)).ToList();
            var kabulSureleri = sevkiyatlar.Where(x => kabulZamaniBySevkiyat.ContainsKey(x.Id))
                .Select(x => (kabulZamaniBySevkiyat[x.Id] - x.CreatedAt).TotalHours).Where(x => x >= 0).ToList();
            var hakedisSureleri = await (from receipt in db.TedarikciMalKabulleri.AsNoTracking()
                join settlement in db.TedarikciHakEdisleri.AsNoTracking() on receipt.TedarikciSiparisId equals settlement.TedarikciSiparisId
                where siparisIds.Contains(receipt.TedarikciSiparisId) && settlement.TamamlandiAt != null
                select new { receipt.CreatedAt, settlement.TamamlandiAt }).ToListAsync(ct);
            var operasyonMetrikleri = new
            {
                zamanindaTeslimOrani = zamanliAdaylar.Count == 0 ? 0m : Math.Round(100m * zamanliAdaylar.Count(x => kabulZamaniBySevkiyat[x.Id] <= x.PlanlananTeslimAt) / zamanliAdaylar.Count, 1),
                kabulOrani = sevkEdilenToplam <= 0 ? 0m : Math.Round(100m * kabulEdilenToplam / sevkEdilenToplam, 1),
                eksikOrani = sevkEdilenToplam <= 0 ? 0m : Math.Round(100m * Math.Max(0m, sevkEdilenToplam - kabulEdilenToplam - reddedilenToplam) / sevkEdilenToplam, 1),
                hasarOrani = siparisRows.Count == 0 ? 0m : Math.Round(100m * sikayetler.Count(x => x.Kategori == TedarikciSikayetKategorileri.Hasarli) / siparisRows.Count, 1),
                itirazOrani = siparisRows.Count == 0 ? 0m : Math.Round(100m * siparisRows.Count(x => x.Durum == PazaryeriSiparisDurumlari.Itirazli) / siparisRows.Count, 1),
                ortalamaKabulSuresiSaat = kabulSureleri.Count == 0 ? 0m : Math.Round((decimal)kabulSureleri.Average(), 1),
                ortalamaHakedisSuresiSaat = hakedisSureleri.Count == 0 ? 0m : Math.Round((decimal)hakedisSureleri.Average(x => (x.TamamlandiAt!.Value - x.CreatedAt).TotalHours), 1)
            };
            var yonetici = await yonetim.IsCurrentUserAdminAsync(ct);
            var yonetimProfilleri = yonetici
                ? await db.TedarikciProfilleri.AsNoTracking().Where(x => x.DogrulamaDurumu == "Incelemede")
                    .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.Unvan, x.VergiNo, x.Iban, x.Adres, x.YetkiliAdSoyad, x.Kategoriler, x.Sehir, x.DogrulamaDurumu }).ToListAsync(ct)
                : [];
            var yonetimSiparisler = Array.Empty<object>();
            if (yonetici)
            {
                var now = DateTime.UtcNow;
                var yonetimSiparisiSatirlari = await (from s in db.TedarikciSiparisleri.AsNoTracking()
                    join p in db.TedarikciProfilleri.AsNoTracking() on s.TedarikciProfilId equals p.Id
                    join h in db.TedarikciHakEdisleri.AsNoTracking() on s.Id equals h.TedarikciSiparisId into hg
                    from h in hg.DefaultIfEmpty()
                    where s.Durum == PazaryeriSiparisDurumlari.Itirazli ||
                          (s.Durum == PazaryeriSiparisDurumlari.MalKabulBekliyor && db.TedarikciSevkiyatlari.Any(v => v.TedarikciSiparisId == s.Id && v.PlanlananTeslimAt != null && v.PlanlananTeslimAt <= DateTime.UtcNow)) ||
                          (h != null && (h.Durum == "AktarimBasarisiz" || h.Durum == "AktarimBekliyor" || (h.Durum == "Bekliyor" && h.PlanlananAt <= DateTime.UtcNow)))
                    orderby s.UpdatedAt
                    select new YonetimSiparisiSatiri(
                        s.Id,
                        s.SiparisNo,
                        p.Unvan,
                        s.Durum,
                        s.GenelToplam,
                        s.ParaBirimi,
                        h == null ? "" : h.Durum,
                        h == null ? (DateTime?)null : h.PlanlananAt,
                        s.Durum == PazaryeriSiparisDurumlari.Itirazli ? "Açık itiraz" : h != null && h.Durum == "AktarimBasarisiz" ? "Başarısız aktarım" : h != null && h.Durum == "AktarimBekliyor" ? "Aktarım yeniden denenecek" : s.Durum == PazaryeriSiparisDurumlari.MalKabulBekliyor ? "Geciken mal kabul" : "Geciken hakediş")).ToListAsync(ct);
                var itirazSiparisIds = yonetimSiparisiSatirlari
                    .Where(x => x.Durum == PazaryeriSiparisDurumlari.Itirazli)
                    .Select(x => x.Id)
                    .ToArray();
                var itirazGecisleri = await db.TedarikciSiparisDurumKayitlari.AsNoTracking()
                        .Where(x => itirazSiparisIds.Contains(x.TedarikciSiparisId) && x.YeniDurum == PazaryeriSiparisDurumlari.Itirazli)
                        .OrderByDescending(x => x.CreatedAt)
                        .ThenByDescending(x => x.Id)
                        .Select(x => new { x.TedarikciSiparisId, x.CreatedAt })
                        .ToListAsync(ct);
                var sonItirazGecisiByOrder = itirazGecisleri
                    .GroupBy(x => x.TedarikciSiparisId)
                    .ToDictionary(x => x.Key, x => (DateTime?)x.First().CreatedAt);
                var itirazSikayetleri = await db.TedarikciSiparisSikayetleri.AsNoTracking()
                        .Where(x => itirazSiparisIds.Contains(x.TedarikciSiparisId))
                        .OrderBy(x => x.CreatedAt)
                        .ThenBy(x => x.Id)
                        .Select(x => new SikayetZamanSatiri(x.TedarikciSiparisId, x.Id, x.CreatedAt, x.YanitlandiAt))
                        .ToListAsync(ct);
                yonetimSiparisler = yonetimSiparisiSatirlari.Select(s =>
                {
                    var itirazSureleri = s.Durum == PazaryeriSiparisDurumlari.Itirazli
                        ? HesaplaItirazSureleri(
                            sonItirazGecisiByOrder.GetValueOrDefault(s.Id), itirazSikayetleri, s.Id, now)
                        : (null, null);
                    return (object)new
                    {
                        id = s.Id,
                        siparisNo = s.SiparisNo,
                        tedarikciUnvani = s.TedarikciUnvani,
                        durum = s.Durum,
                        genelToplam = s.GenelToplam,
                        paraBirimi = s.ParaBirimi,
                        hakedisDurumu = s.HakedisDurumu,
                        planlananAt = s.PlanlananAt,
                        yonetimNedeni = s.YonetimNedeni,
                        itirazYasiSaat = itirazSureleri.Item1,
                        tedarikciIlkYanitSuresiSaat = itirazSureleri.Item2
                    };
                }).ToArray();
            }
            return Results.Ok(new { aktifIsletmeId = aktif.Id, guvenliOdemeHazir = paymentGateway.IsConfigured, malKabulYetkisi, subeler, depolar, profiller, talepler, acikTalepler, gelenTeklifler, profil, urunler, benimUrunlerim, kaynakUrunler, anaSiparisler, siparisler = siparisRows, siparisKalemleri, sevkiyatlar, hakedisler, malKabulHareketleri, sikayetler, degerlendirmeler, tedarikciPerformanslari, operasyonMetrikleri, yonetici, yonetimProfilleri, yonetimSiparisler });
        });

        app.MapPut("/api/ekran/tedarikci-pazaryeri/profil", async (TedarikciProfilKaydetRequest request, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Unvan) || string.IsNullOrWhiteSpace(request.Kategoriler)) return Results.BadRequest(new ApiHata("Unvan ve kategori zorunludur."));
            var aktif = await isletmeler.GetActiveAsync(); await using var db = await factory.CreateDbContextAsync(ct);
            var profil = await db.TedarikciProfilleri.SingleOrDefaultAsync(x => x.IsletmeId == aktif.Id, ct);
            if (profil is null) { profil = new TedarikciProfil { IsletmeId = aktif.Id }; db.TedarikciProfilleri.Add(profil); }
            profil.Unvan = request.Unvan.Trim(); profil.Kategoriler = request.Kategoriler.Trim(); profil.Sehir = request.Sehir.Trim(); profil.Aciklama = request.Aciklama.Trim(); profil.Yayinda = request.Yayinda && profil.Dogrulandi; profil.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct); return Results.Ok(new { profil.Id, mesaj = "Tedarikçi profili kaydedildi." });
        });

        app.MapPut("/api/ekran/tedarikci-pazaryeri/profil/basvuru", async (TedarikciOnboardingRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var profile = await service.SaveSupplierProfileAsync(request, ct); return Results.Ok(new { profile.Id, profile.DogrulamaDurumu, profile.Dogrulandi, profile.Yayinda, mesaj = profile.Dogrulandi ? "Tedarikçi profili kaydedildi." : "Başvuru incelemeye gönderildi." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/urunler", async (TedarikciUrunKaydetRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var product = await service.SaveProductAsync(null, request, ct); return Results.Ok(new { product.Id, mesaj = "Ürün yayınlandı." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPut("/api/ekran/tedarikci-pazaryeri/urunler/{urunId:int}", async (int urunId, TedarikciUrunKaydetRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var product = await service.SaveProductAsync(urunId, request, ct); return Results.Ok(new { product.Id, mesaj = "Ürün güncellendi." }); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/siparisler", async (PazaryeriSiparisOlusturRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.CreateOrderAsync(request, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/siparisler/{siparisId:int}/odeme", async (int siparisId, PazaryeriOdemeRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.PayOrderAsync(siparisId, request, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/siparisler/{siparisId:int}/iptal", async (int siparisId, PazaryeriIptalRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.CancelOrderAsync(siparisId, request, ct); return Results.Ok(new { mesaj = "Sipariş iptal edildi." }); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/iptal", async (int siparisId, PazaryeriIptalRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.CancelSupplierOrderAsync(siparisId, request, ct); return Results.Ok(new { mesaj = "Tedarikçi siparişi iptal edildi." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/durum", async (int siparisId, TedarikciSiparisDurumRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.UpdateSupplierOrderStateAsync(siparisId, request, ct); return Results.Ok(new { mesaj = "Sipariş durumu güncellendi." }); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/sevkiyatlar", async (int siparisId, TedarikciSevkiyatOlusturRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.CreateShipmentAsync(siparisId, request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/sevkiyatlar/{sevkiyatId:int}/belge", async (int sevkiyatId, HttpContext http, AppRuntimeOptions runtime, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            try
            {
                var businessId = await isletmeler.GetActiveIdAsync();
                await using var db = await factory.CreateDbContextAsync(ct);
                var shipment = await db.TedarikciSevkiyatlari.SingleOrDefaultAsync(x => x.Id == sevkiyatId && x.TedarikciIsletmeId == businessId, ct)
                    ?? throw new KeyNotFoundException("Sevkiyat bulunamadı.");
                if (!http.Request.HasFormContentType)
                    return Results.BadRequest(new ApiHata("Belge multipart/form-data olarak gönderilmelidir."));
                var form = await http.Request.ReadFormAsync(ct);
                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
                if (file is null || file.Length == 0)
                    return Results.BadRequest(new ApiHata("İrsaliye fotoğrafı veya PDF seçilmedi."));
                await using var input = file.OpenReadStream();
                var inspection = await SecureFileInspector.InspectAsync(input, file.FileName, file.Length, SecureFilePurpose.ChatAttachment, ct);
                if (!inspection.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && inspection.ContentType != "application/pdf")
                    return Results.BadRequest(new ApiHata("İrsaliye yalnız JPG, PNG, WEBP veya PDF olabilir."));

                var directory = GetShipmentDocumentDirectory(runtime, businessId, sevkiyatId);
                Directory.CreateDirectory(directory);
                var storedName = $"{Guid.NewGuid():N}{inspection.Extension}";
                var path = Path.Combine(directory, storedName);
                await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                    await SecureFileInspector.CopyBoundedAsync(input, output, file.Length, 5L * 1024 * 1024, ct);
                shipment.BelgeDosyaYolu = ShipmentDocumentUrl(sevkiyatId, storedName);
                shipment.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { yol = shipment.BelgeDosyaYolu, dosyaAdi = inspection.DisplayFileName });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("upload");

        app.MapGet("/api/ekran/tedarikci-pazaryeri/sevkiyatlar/{sevkiyatId:int}/belge/{dosyaAdi}", async (int sevkiyatId, string dosyaAdi, AppRuntimeOptions runtime, IIsletmeService isletmeler, ISystemcelYonetimService yonetim, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            var isAdmin = await yonetim.IsCurrentUserAdminAsync(ct);
            var businessId = isAdmin ? 0 : await isletmeler.GetActiveIdAsync();
            await using var db = await factory.CreateDbContextAsync(ct);
            var shipment = await db.TedarikciSevkiyatlari.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sevkiyatId && (isAdmin || x.AliciIsletmeId == businessId || x.TedarikciIsletmeId == businessId), ct);
            if (shipment is null)
                return Results.NotFound(new ApiHata("Sevkiyat bulunamadı."));
            var safeName = Path.GetFileName(dosyaAdi);
            var directory = GetShipmentDocumentDirectory(runtime, shipment.TedarikciIsletmeId, sevkiyatId);
            var path = Path.Combine(directory, safeName);
            if (!string.Equals(safeName, dosyaAdi, StringComparison.Ordinal) || !SecureFileInspector.IsPathInside(path, directory) || !File.Exists(path))
                return Results.NotFound(new ApiHata("İrsaliye dosyası bulunamadı."));
            return Results.File(path, EvidenceContentType(Path.GetExtension(path)), enableRangeProcessing: false);
        }).RequireRateLimiting("sensitive");

        app.MapGet("/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{kod}", async (string kod, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ResolveShipmentQrAsync(kod, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapGet("/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{kod}/gorsel", async (string kod, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try
            {
                var qr = await service.ResolveShipmentQrAsync(kod, ct);
                return Results.Content(MarketplaceQrSvgRenderer.Render($"https://systemcel.app/app/tedarikci-pazaryeri?qr={qr.Kod}"), "image/svg+xml", System.Text.Encoding.UTF8);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{kod}/kanit", async (string kod, HttpContext http, AppRuntimeOptions runtime, IIsletmeService isletmeler, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try
            {
                if (!http.Request.HasFormContentType)
                    return Results.BadRequest(new ApiHata("Kanıt dosyası multipart/form-data olarak gönderilmelidir."));
                var form = await http.Request.ReadFormAsync(ct);
                var branchId = ParseOptionalFormInt(form["subeId"].ToString(), "Şube");
                var warehouseId = ParseOptionalFormInt(form["depoId"].ToString(), "Depo");
                await service.ValidateReceiptEvidenceAccessAsync(kod, branchId, warehouseId, ct);
                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
                if (file is null || file.Length == 0)
                    return Results.BadRequest(new ApiHata("Kanıt fotoğrafı veya PDF seçilmedi."));

                await using var input = file.OpenReadStream();
                var inspection = await SecureFileInspector.InspectAsync(input, file.FileName, file.Length, SecureFilePurpose.ChatAttachment, ct);
                if (!inspection.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && inspection.ContentType != "application/pdf")
                    return Results.BadRequest(new ApiHata("Kanıt yalnız JPG, PNG, WEBP veya PDF olabilir."));

                var businessId = await isletmeler.GetActiveIdAsync();
                var directory = GetReceiptEvidenceDirectory(runtime, businessId, kod);
                Directory.CreateDirectory(directory);
                var storedName = $"{Guid.NewGuid():N}{inspection.Extension}";
                var path = Path.Combine(directory, storedName);
                await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                    await SecureFileInspector.CopyBoundedAsync(input, output, file.Length, 5L * 1024 * 1024, ct);
                return Results.Ok(new { yol = ReceiptEvidenceUrl(kod, storedName), dosyaAdi = inspection.DisplayFileName });
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("upload");

        app.MapGet("/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{kod}/kanit/{dosyaAdi}", async (string kod, string dosyaAdi, AppRuntimeOptions runtime, IIsletmeService isletmeler, ISystemcelYonetimService yonetim, ITedarikciPazaryeriService service, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            try
            {
                var isAdmin = await yonetim.IsCurrentUserAdminAsync(ct);
                int businessId;
                if (isAdmin)
                {
                    var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeEvidenceQrCode(kod)))).ToLowerInvariant();
                    await using var db = await factory.CreateDbContextAsync(ct);
                    businessId = await (from label in db.TedarikciSevkiyatEtiketleri.AsNoTracking()
                        join line in db.TedarikciSevkiyatKalemleri.AsNoTracking() on label.TedarikciSevkiyatKalemiId equals line.Id
                        join shipment in db.TedarikciSevkiyatlari.AsNoTracking() on line.TedarikciSevkiyatId equals shipment.Id
                        where label.KodHash == codeHash
                        select shipment.AliciIsletmeId).SingleOrDefaultAsync(ct);
                    if (businessId == 0)
                        return Results.NotFound(new ApiHata("Kanıt dosyası bulunamadı."));
                }
                else
                {
                    _ = await service.ResolveShipmentQrAsync(kod, ct);
                    businessId = await isletmeler.GetActiveIdAsync();
                }
                var safeName = Path.GetFileName(dosyaAdi);
                if (!string.Equals(safeName, dosyaAdi, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(safeName))
                    return Results.BadRequest(new ApiHata("Kanıt dosya adı geçersiz."));
                var directory = GetReceiptEvidenceDirectory(runtime, businessId, kod);
                var path = Path.Combine(directory, safeName);
                if (!SecureFileInspector.IsPathInside(path, directory) || !File.Exists(path))
                    return Results.NotFound(new ApiHata("Kanıt dosyası bulunamadı."));
                return Results.File(path, EvidenceContentType(Path.GetExtension(path)), enableRangeProcessing: false);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{kod}/mal-kabul", async (string kod, TedarikciMalKabulRequest request, HttpContext http, AppRuntimeOptions runtime, IIsletmeService isletmeler, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try
            {
                var evidenceUrl = NormalizeReceiptEvidenceUrl(request.FotoKanitiYolu, kod);
                var evidenceHash = string.IsNullOrWhiteSpace(evidenceUrl)
                    ? string.Empty
                    : await ComputeReceiptEvidenceHashAsync(runtime, await isletmeler.GetActiveIdAsync(), kod, evidenceUrl, ct);
                var auditedRequest = request with
                {
                    IslemYapanKullaniciRef = null,
                    IpAdresi = http.Connection.RemoteIpAddress?.ToString(),
                    FotoKanitiYolu = evidenceUrl,
                    BelgeKarmasi = evidenceHash
                };
                return Results.Ok(await service.ReceiveShipmentQrAsync(kod, auditedRequest, ct));
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/itiraz", async (int siparisId, PazaryeriIptalRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.DisputeSupplierOrderAsync(siparisId, request.Neden, ct); return Results.Ok(new { mesaj = "İtiraz kaydedildi. Hakediş inceleme bitene kadar bekletilecek." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/sikayetler", async (int siparisId, TedarikciSikayetOlusturRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var complaint = await service.CreateComplaintAsync(siparisId, request, ct); return Results.Ok(new { complaint.Id, complaint.Durum, mesaj = "Sorun tedarikçiye iletildi." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/sikayetler/{sikayetId:int}/yanit", async (int sikayetId, TedarikciSikayetYanitRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var complaint = await service.RespondToComplaintAsync(sikayetId, request, ct); return Results.Ok(new { complaint.Id, complaint.Durum, mesaj = "Yanıt alıcıya iletildi." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/sikayetler/{sikayetId:int}/kapat", async (int sikayetId, TedarikciSikayetKapatRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var complaint = await service.CloseComplaintAsync(sikayetId, request, ct); return Results.Ok(new { complaint.Id, complaint.Durum, mesaj = "Sorunun sonucu kaydedildi." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPut("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/degerlendirme", async (int siparisId, TedarikciDegerlendirmeKaydetRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var rating = await service.SaveSupplierRatingAsync(siparisId, request, ct); return Results.Ok(new { rating.Id, rating.OrtalamaPuan, mesaj = "Değerlendirmeniz kaydedildi." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/yonetim/tedarikci-siparisler/{siparisId:int}/itiraz-coz", async (int siparisId, PazaryeriItirazCozRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.ResolveDisputeAsync(siparisId, request, ct); return Results.Ok(new { mesaj = "İtiraz kapatıldı." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/yonetim/tedarikci-mal-kabulleri/{malKabulId:int}/stok-uzlastir", async (int malKabulId, TedarikciMalKabulStokUzlastirmaRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ReconcileRejectedReceiptStockAsync(malKabulId, request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapGet("/api/ekran/yonetim/tedarikci-siparisler/{siparisId:int}/inceleme", async (int siparisId, ISystemcelYonetimService yonetim, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            if (!await yonetim.IsCurrentUserAdminAsync(ct))
                return Results.Json(new ApiHata("Bu işlem için yönetici yetkisi gerekir."), statusCode: StatusCodes.Status403Forbidden);
            await using var db = await factory.CreateDbContextAsync(ct);
            var order = await db.TedarikciSiparisleri.AsNoTracking().SingleOrDefaultAsync(x => x.Id == siparisId, ct);
            if (order is null)
                return Results.NotFound(new ApiHata("Tedarikçi siparişi bulunamadı."));
            var shipments = await db.TedarikciSevkiyatlari.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.SevkiyatNo, x.BelgeNo, x.BelgeUuid, x.BelgeDosyaYolu, x.SevkAt, x.RandevuAt, x.PlanlananTeslimAt, x.AracPlaka, x.SurucuAdi, x.Durum }).ToListAsync(ct);
            var receipts = await db.TedarikciMalKabulleri.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.KabulEdilenMiktar, x.ReddedilenMiktar, x.StokUzlastirmaDurumu, x.StokUzlastirmaAnahtari, x.StokUzlastiranKullaniciRef, x.StokUzlastirmaNotu, x.StokUzlastirmaAt, x.RedNedeni, x.IkinciRedNedeni, x.Not, x.SubeId, x.DepoId, x.IslemYapanKullaniciRef, x.CihazRef, x.IpAdresi, x.BelgeKarmasi, x.FotoKanitiYolu, x.OlculenAgirlik, x.OlculenSicaklik, x.KabulBrutTutar, x.SerbestBirakilanNetTutar, x.HakEdisAktarimReferansi, x.HakEdisAktarimHatasi, x.CreatedAt }).ToListAsync(ct);
            var complaints = await db.TedarikciSiparisSikayetleri.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.CreatedAt).Select(x => new { x.Kategori, x.Aciklama, x.Talep, x.Durum, x.TedarikciYaniti, x.KapanisNotu, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
            var history = await db.TedarikciSiparisDurumKayitlari.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.CreatedAt).Select(x => new { x.OncekiDurum, x.YeniDurum, x.IslemYapanIsletmeId, x.Aciklama, x.CreatedAt }).ToListAsync(ct);
            var paymentAllocations = await (from allocation in db.PazaryeriOdemeDagitimlari.AsNoTracking()
                                            join payment in db.PazaryeriOdemeleri.AsNoTracking() on allocation.PazaryeriOdemeId equals payment.Id
                                            where allocation.TedarikciSiparisId == siparisId && payment.AnaSiparisId == order.AnaSiparisId
                                            select new { payment.Id, payment.Saglayici, payment.SaglayiciIslemId, payment.Durum,
                                                payment.Tutar, allocation.BrutTutar, allocation.IadeTutari }).ToListAsync(ct);
            var invoiceMatch = await db.TedarikciFaturaEslesmeleri.AsNoTracking()
                .Where(x => x.TedarikciSiparisId == siparisId)
                .Select(x => new { x.AliciFaturaId, x.SaticiFaturaId, x.TedarikciBelgeNo, x.TedarikciBelgeUuid })
                .SingleOrDefaultAsync(ct);
            var invoices = invoiceMatch is null
                ? []
                : await db.Faturalar.AsNoTracking()
                    .Where(x => x.Id == invoiceMatch.AliciFaturaId || x.Id == invoiceMatch.SaticiFaturaId)
                    .Select(x => new { x.Id, x.IsletmeId, x.FaturaTipi, x.YerelFaturaNo, x.PortalBelgeNo, x.GenelToplam, x.Durum })
                    .ToArrayAsync(ct);
            var settlement = await db.TedarikciHakEdisleri.AsNoTracking()
                .Where(x => x.TedarikciSiparisId == siparisId)
                .Select(x => new { x.Id, x.Durum, x.NetTutar, x.OdenenTutar, x.AktarimReferansi })
                .SingleOrDefaultAsync(ct);
            var stockMovements = await db.StokHareketleri.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.IsletmeId, x.TedarikciSevkiyatId, x.TedarikciMalKabulId, x.HareketTipi, x.Kaynak, x.Miktar, x.RezerveMiktar, x.Aciklama }).ToListAsync(ct);
            var cariMovements = await db.CariHareketleri.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.IsletmeId, x.TedarikciMalKabulId, x.HareketTipi, x.Tutar }).ToListAsync(ct);
            var paymentMovements = await db.TahsilatOdemeleri.AsNoTracking().Where(x => x.TedarikciSiparisId == siparisId)
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.FaturaId, x.TedarikciMalKabulId, x.Tip, x.Tutar }).ToListAsync(ct);
            return Results.Ok(new { order.Id, order.SiparisNo, order.Durum, order.GenelToplam, order.ParaBirimi,
                shipments, receipts, complaints, history,
                references = new { paymentAllocations, invoiceMatch, invoices, settlement, stockMovements, cariMovements, paymentMovements } });
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/fatura", async (int siparisId, TedarikciBelgeEsleRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.MatchSupplierInvoiceAsync(siparisId, request, ct); return Results.Ok(new { mesaj = "Fatura siparişle eşleştirildi." }); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/yonetim/tedarikci-profilleri/{profilId:int}/dogrula", async (int profilId, TedarikciDogrulamaRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { var profile = await service.VerifySupplierAsync(profilId, request, ct); return Results.Ok(new { profile.Id, profile.DogrulamaDurumu, mesaj = request.Onaylandi ? "Tedarikçi doğrulandı." : "Başvuru reddedildi." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/yonetim/tedarikci-siparisler/{siparisId:int}/hakedis", async (int siparisId, PazaryeriHakEdisTamamlaRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.CompleteSettlementAsync(siparisId, request, ct); return Results.Ok(new { mesaj = "Hakediş tamamlandı." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/talepler", async (TedarikAlimTalebiOlusturRequest request, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Baslik) || string.IsNullOrWhiteSpace(request.UrunHizmet) || request.Miktar <= 0 || request.SonTeklifAt <= DateTime.UtcNow) return Results.BadRequest(new ApiHata("Talep bilgilerini ve ileri bir son teklif tarihini girin."));
            var aktif = await isletmeler.GetActiveAsync(); await using var db = await factory.CreateDbContextAsync(ct);
            var talep = new TedarikAlimTalebi { AliciIsletmeId = aktif.Id, Baslik = request.Baslik.Trim(), Kategori = request.Kategori.Trim(), UrunHizmet = request.UrunHizmet.Trim(), Miktar = request.Miktar, Birim = request.Birim.Trim(), TeslimatSehri = request.TeslimatSehri.Trim(), SonTeklifAt = request.SonTeklifAt, Aciklama = request.Aciklama.Trim() };
            db.TedarikAlimTalepleri.Add(talep); await db.SaveChangesAsync(ct); return Results.Ok(new { talep.Id, mesaj = "Alım talebi yayınlandı." });
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/talepler/{talepId:int}/teklifler", async (int talepId, TedarikTeklifiOlusturRequest request, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            if (request.BirimFiyat <= 0 || request.KdvOrani is < 0 or > 100 || request.TerminGun < 0 || request.MinimumSiparis <= 0) return Results.BadRequest(new ApiHata("Fiyat, KDV, termin ve minimum sipariş bilgilerini kontrol edin."));
            var aktif = await isletmeler.GetActiveAsync(); await using var db = await factory.CreateDbContextAsync(ct);
            if (!await db.TedarikciProfilleri.AnyAsync(x => x.IsletmeId == aktif.Id && x.Yayinda, ct)) return Results.Json(new ApiHata("Teklif vermek için yayında bir tedarikçi profili gerekir."), statusCode: 403);
            var talep = await db.TedarikAlimTalepleri.SingleOrDefaultAsync(x => x.Id == talepId && x.AliciIsletmeId != aktif.Id && x.Durum == "Acik" && x.SonTeklifAt > DateTime.UtcNow, ct);
            if (talep is null) return Results.NotFound(new ApiHata("Açık alım talebi bulunamadı."));
            var teklif = await db.TedarikTeklifleri.SingleOrDefaultAsync(x => x.TalepId == talepId && x.TedarikciIsletmeId == aktif.Id, ct);
            if (teklif is null) { teklif = new TedarikTeklifi { TalepId = talepId, TedarikciIsletmeId = aktif.Id }; db.TedarikTeklifleri.Add(teklif); }
            teklif.BirimFiyat = request.BirimFiyat; teklif.KdvOrani = request.KdvOrani; teklif.ParaBirimi = request.ParaBirimi.Trim().ToUpperInvariant(); teklif.TerminGun = request.TerminGun; teklif.MinimumSiparis = request.MinimumSiparis; teklif.Not = request.Not.Trim();
            await db.SaveChangesAsync(ct); return Results.Ok(new { teklif.Id, mesaj = "Teklif gönderildi." });
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/tedarikci-pazaryeri/teklifler/{teklifId:int}/kabul", async (int teklifId, TedarikTeklifKabulRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.AcceptOfferAsync(teklifId, request, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

    }

    private static string GetReceiptEvidenceDirectory(AppRuntimeOptions runtime, int businessId, string code)
    {
        var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()))).ToLowerInvariant();
        return Path.Combine(runtime.AppDataPath, "uploads", "marketplace-receipts", businessId.ToString(), codeHash);
    }

    private static string GetShipmentDocumentDirectory(AppRuntimeOptions runtime, int supplierBusinessId, int shipmentId) =>
        Path.Combine(runtime.AppDataPath, "uploads", "marketplace-shipments", supplierBusinessId.ToString(), shipmentId.ToString());

    private static string ShipmentDocumentUrl(int shipmentId, string fileName) =>
        $"/api/ekran/tedarikci-pazaryeri/sevkiyatlar/{shipmentId}/belge/{Uri.EscapeDataString(fileName)}";

    private static string ReceiptEvidenceUrl(string code, string fileName) =>
        $"/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{Uri.EscapeDataString(code.Trim())}/kanit/{Uri.EscapeDataString(fileName)}";

    private static string NormalizeEvidenceQrCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim();
        const string prefix = "systemcel:sevkiyat:";
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[prefix.Length..];
        if (!normalized.StartsWith("scq1_", StringComparison.Ordinal) || normalized.Length != 53)
            throw new ArgumentException("Geçersiz sevkiyat QR kodu.");
        return normalized;
    }

    private static string NormalizeReceiptEvidenceUrl(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var prefix = $"/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/{Uri.EscapeDataString(code.Trim())}/kanit/";
        var normalized = value.Trim();
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal) || normalized[prefix.Length..].Contains('/'))
            throw new ArgumentException("Kanıt dosyası bu sevkiyat etiketiyle eşleşmiyor.");
        return normalized;
    }

    private static int? ParseOptionalFormInt(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
            throw new ArgumentException($"{label} seçimi geçersiz.");
        return parsed;
    }

    private static async Task<string> ComputeReceiptEvidenceHashAsync(
        AppRuntimeOptions runtime,
        int businessId,
        string code,
        string evidenceUrl,
        CancellationToken ct)
    {
        var fileName = Uri.UnescapeDataString(evidenceUrl[(evidenceUrl.LastIndexOf('/') + 1)..]);
        var safeName = Path.GetFileName(fileName);
        var directory = GetReceiptEvidenceDirectory(runtime, businessId, code);
        var path = Path.Combine(directory, safeName);
        if (string.IsNullOrWhiteSpace(safeName) || !string.Equals(safeName, fileName, StringComparison.Ordinal) ||
            !SecureFileInspector.IsPathInside(path, directory) || !File.Exists(path))
            throw new ArgumentException("Kanıt dosyası bulunamadı veya bu sevkiyat etiketiyle eşleşmiyor.");
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Convert.ToHexString(await SHA256.HashDataAsync(input, ct)).ToLowerInvariant();
    }

    private static string EvidenceContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
