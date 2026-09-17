using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Systemcel.Api.Api;

internal static class TedarikciPazaryeriApi
{
    private sealed record ApiHata(string mesaj);
    public static void MapTedarikciPazaryeriApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/tedarikci-pazaryeri", async (IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
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
            var gelenTeklifler = await (from t in db.TedarikTeklifleri.AsNoTracking() join a in db.TedarikAlimTalepleri on t.TalepId equals a.Id join p in db.TedarikciProfilleri on t.TedarikciIsletmeId equals p.IsletmeId where a.AliciIsletmeId == aktif.Id select new { t.Id, t.TalepId, talepBasligi = a.Baslik, t.BirimFiyat, t.ParaBirimi, t.TerminGun, t.MinimumSiparis, t.Not, t.Durum, tedarikciUnvani = p.Unvan }).OrderBy(x => x.BirimFiyat).ToListAsync(ct);
            var profil = await db.TedarikciProfilleri.AsNoTracking().Where(x => x.IsletmeId == aktif.Id)
                .Select(x => new { x.Id, x.Unvan, x.Kategoriler, x.Sehir, x.Aciklama, x.VergiNo, x.MersisNo, x.KepAdresi, x.Iban, x.Adres, x.YetkiliAdSoyad, x.VergiDurumu, x.SevkiyatBolgeleri, x.IadeKosullari, x.PazaryeriSozlesmeVersiyonu, x.PspAltUyeIsyeriId, x.KomisyonOrani, x.OdemeVadesiGun, x.TevkifatMuaf, x.DogrulamaDurumu, x.DogrulamaNotu, x.Dogrulandi, x.Yayinda }).SingleOrDefaultAsync(ct);
            var urunler = await (from u in db.TedarikciUrunleri.AsNoTracking()
                join p in db.TedarikciProfilleri.AsNoTracking() on u.TedarikciProfilId equals p.Id
                where u.Aktif && p.Dogrulandi && p.Yayinda && u.StokMiktari > u.RezerveMiktar
                orderby u.Kategori, u.Ad
                select new { u.Id, u.TedarikciProfilId, tedarikciUnvani = p.Unvan, tedarikciSehri = p.Sehir, p.SevkiyatBolgeleri, p.IadeKosullari, u.Sku, u.Ad, u.Aciklama, u.Kategori, u.Birim, u.BirimFiyat, u.KdvOrani, u.ParaBirimi, kullanilabilirStok = u.StokMiktari - u.RezerveMiktar, u.MinimumSiparisMiktari, u.TahminiTeslimatGun }).ToListAsync(ct);
            var benimUrunlerim = await db.TedarikciUrunleri.AsNoTracking().Where(x => x.TedarikciIsletmeId == aktif.Id)
                .OrderBy(x => x.Ad).Select(x => new { x.Id, x.KaynakUrunHizmetId, x.Sku, x.Ad, x.Aciklama, x.Kategori, x.Birim, x.BirimFiyat, x.KdvOrani, x.ParaBirimi, x.StokMiktari, x.RezerveMiktar, x.MinimumSiparisMiktari, x.TahminiTeslimatGun, x.Aktif }).ToListAsync(ct);
            var kaynakUrunler = await db.UrunHizmetleri.AsNoTracking().Where(x => x.IsletmeId == aktif.Id && x.Aktif && x.Tip == "Urun")
                .OrderBy(x => x.Ad).Select(x => new { x.Id, x.Ad, x.Barkod, x.Birim, x.KdvOrani, x.SatisFiyati, x.ParaBirimi }).ToListAsync(ct);
            var siparisRows = await (from s in db.TedarikciSiparisleri.AsNoTracking()
                join a in db.PazaryeriAnaSiparisleri.AsNoTracking() on s.AnaSiparisId equals a.Id
                join p in db.TedarikciProfilleri.AsNoTracking() on s.TedarikciProfilId equals p.Id
                where s.AliciIsletmeId == aktif.Id || s.TedarikciIsletmeId == aktif.Id
                orderby s.CreatedAt descending
                select new { s.Id, s.AnaSiparisId, anaSiparisNo = a.SiparisNo, s.SiparisNo, a.TeslimatAdresi, s.AliciIsletmeId, s.TedarikciIsletmeId, tedarikciUnvani = p.Unvan, s.AraToplam, s.KdvToplam, s.GenelToplam, s.ParaBirimi, s.KomisyonTutari, s.KomisyonKdvTutari, s.TevkifatTutari, s.OdemeHizmetiBedeli, s.TedarikciHakEdisi, s.Durum, s.KargoFirmasi, s.KargoTakipNo, s.CreatedAt }).ToListAsync(ct);
            var siparisIds = siparisRows.Select(x => x.Id).ToList();
            var siparisKalemleri = await db.TedarikciSiparisKalemleri.AsNoTracking().Where(x => siparisIds.Contains(x.TedarikciSiparisId))
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.TedarikciSiparisId, x.TedarikciUrunId, x.Sku, x.Ad, x.Birim, x.Miktar, x.BirimFiyat, x.KdvOrani, x.NetTutar, x.KdvTutari, x.ToplamTutar }).ToListAsync(ct);
            var anaSiparisler = await db.PazaryeriAnaSiparisleri.AsNoTracking().Where(x => x.AliciIsletmeId == aktif.Id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.SiparisNo, x.TeslimatAdresi, x.AraToplam, x.KdvToplam, x.GenelToplam, x.ParaBirimi, x.Durum, x.CreatedAt }).ToListAsync(ct);
            var hakedisler = await (from h in db.TedarikciHakEdisleri.AsNoTracking()
                join s in db.TedarikciSiparisleri.AsNoTracking() on h.TedarikciSiparisId equals s.Id
                where h.TedarikciIsletmeId == aktif.Id
                orderby h.CreatedAt descending
                select new { h.Id, h.TedarikciSiparisId, s.SiparisNo, h.BrutTutar, h.KomisyonTutari, h.KomisyonKdvTutari, h.TevkifatTutari, h.OdemeHizmetiBedeli, h.IadeTutari, h.NetTutar, h.OdenenTutar, h.ParaBirimi, h.Durum, h.AktarimReferansi, h.PlanlananAt, h.TamamlandiAt }).ToListAsync(ct);
            return Results.Ok(new { aktifIsletmeId = aktif.Id, profiller, talepler, acikTalepler, gelenTeklifler, profil, urunler, benimUrunlerim, kaynakUrunler, anaSiparisler, siparisler = siparisRows, siparisKalemleri, hakedisler });
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

        app.MapPost("/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/{siparisId:int}/itiraz", async (int siparisId, PazaryeriIptalRequest request, ITedarikciPazaryeriService service, CancellationToken ct) =>
        {
            try { await service.DisputeSupplierOrderAsync(siparisId, request.Neden, ct); return Results.Ok(new { mesaj = "İtiraz kaydedildi. Hakediş inceleme bitene kadar bekletilecek." }); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
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
        });

        app.MapPost("/api/ekran/tedarikci-pazaryeri/talepler/{talepId:int}/teklifler", async (int talepId, TedarikTeklifiOlusturRequest request, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            if (request.BirimFiyat <= 0 || request.TerminGun < 0 || request.MinimumSiparis <= 0) return Results.BadRequest(new ApiHata("Fiyat, termin ve minimum sipariş bilgilerini kontrol edin."));
            var aktif = await isletmeler.GetActiveAsync(); await using var db = await factory.CreateDbContextAsync(ct);
            if (!await db.TedarikciProfilleri.AnyAsync(x => x.IsletmeId == aktif.Id && x.Yayinda, ct)) return Results.Json(new ApiHata("Teklif vermek için yayında bir tedarikçi profili gerekir."), statusCode: 403);
            var talep = await db.TedarikAlimTalepleri.SingleOrDefaultAsync(x => x.Id == talepId && x.AliciIsletmeId != aktif.Id && x.Durum == "Acik" && x.SonTeklifAt > DateTime.UtcNow, ct);
            if (talep is null) return Results.NotFound(new ApiHata("Açık alım talebi bulunamadı."));
            var teklif = await db.TedarikTeklifleri.SingleOrDefaultAsync(x => x.TalepId == talepId && x.TedarikciIsletmeId == aktif.Id, ct);
            if (teklif is null) { teklif = new TedarikTeklifi { TalepId = talepId, TedarikciIsletmeId = aktif.Id }; db.TedarikTeklifleri.Add(teklif); }
            teklif.BirimFiyat = request.BirimFiyat; teklif.ParaBirimi = request.ParaBirimi.Trim().ToUpperInvariant(); teklif.TerminGun = request.TerminGun; teklif.MinimumSiparis = request.MinimumSiparis; teklif.Not = request.Not.Trim();
            await db.SaveChangesAsync(ct); return Results.Ok(new { teklif.Id, mesaj = "Teklif gönderildi." });
        });

        app.MapPost("/api/ekran/tedarikci-pazaryeri/teklifler/{teklifId:int}/kabul", async (int teklifId, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            var aktif = await isletmeler.GetActiveAsync(); await using var db = await factory.CreateDbContextAsync(ct);
            var teklif = await (from t in db.TedarikTeklifleri join a in db.TedarikAlimTalepleri on t.TalepId equals a.Id where t.Id == teklifId && a.AliciIsletmeId == aktif.Id select t).SingleOrDefaultAsync(ct);
            if (teklif is null) return Results.NotFound(new ApiHata("Teklif bulunamadı."));
            var talep = await db.TedarikAlimTalepleri.SingleAsync(x => x.Id == teklif.TalepId, ct);
            teklif.Durum = "KabulEdildi"; talep.Durum = "Sonuclandi";
            await db.TedarikTeklifleri.Where(x => x.TalepId == talep.Id && x.Id != teklif.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Durum, "Reddedildi"), ct);
            await db.SaveChangesAsync(ct); return Results.Ok(new { mesaj = "Teklif kabul edildi." });
        });

    }
}
