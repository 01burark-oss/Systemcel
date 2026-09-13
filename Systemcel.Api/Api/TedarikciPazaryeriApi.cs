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
                .Select(x => new { x.Id, x.Unvan, x.Kategoriler, x.Sehir, x.Aciklama, x.Dogrulandi, x.Yayinda }).SingleOrDefaultAsync(ct);
            return Results.Ok(new { profiller, talepler, acikTalepler, gelenTeklifler, profil });
        });

        app.MapPut("/api/ekran/tedarikci-pazaryeri/profil", async (TedarikciProfilKaydetRequest request, IIsletmeService isletmeler, IDbContextFactory<CashTrackerDbContext> factory, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Unvan) || string.IsNullOrWhiteSpace(request.Kategoriler)) return Results.BadRequest(new ApiHata("Unvan ve kategori zorunludur."));
            var aktif = await isletmeler.GetActiveAsync(); await using var db = await factory.CreateDbContextAsync(ct);
            var profil = await db.TedarikciProfilleri.SingleOrDefaultAsync(x => x.IsletmeId == aktif.Id, ct);
            if (profil is null) { profil = new TedarikciProfil { IsletmeId = aktif.Id }; db.TedarikciProfilleri.Add(profil); }
            profil.Unvan = request.Unvan.Trim(); profil.Kategoriler = request.Kategoriler.Trim(); profil.Sehir = request.Sehir.Trim(); profil.Aciklama = request.Aciklama.Trim(); profil.Yayinda = request.Yayinda; profil.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct); return Results.Ok(new { profil.Id, mesaj = "Tedarikçi profili kaydedildi." });
        });

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
