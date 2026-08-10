using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Systemcel.Api.Api;

internal static class SubscriptionApi
{
    public static void MapSubscriptionApi(this WebApplication app)
    {
        app.MapGet("/api/public/planlar", async (
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var now = DateTime.UtcNow;
            var usedFounderSlots = await db.KurucuKampanyaHaklari.AsNoTracking().CountAsync(x =>
                x.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu &&
                (x.Durum == "Kazanildi" || (x.Durum == "Rezerve" && x.RezervasyonBitisAt > now)), ct);
            var remainingFounderSlots = Math.Max(0, SubscriptionPlanCatalog.KurucuKampanyaKontenjani - usedFounderSlots);
            var founderActive = remainingFounderSlots > 0;
            var plans = SubscriptionPlanCatalog.Plans
                .Where(x => x.Kod is PlanKodlari.IsletmeBaslangic
                    or PlanKodlari.IsletmeBuyume
                    or PlanKodlari.IsletmeKurumsal
                    or PlanKodlari.MuhasebeciStandart
                    or PlanKodlari.MuhasebeciPro)
                .Select(x => new
                {
                    kod = x.Kod,
                    ad = x.Ad,
                    hesapTipi = x.HesapTipi,
                    aylikTutar = founderActive ? x.KurucuAylikTutar : x.AylikTutar,
                    yillikTutar = founderActive ? x.KurucuYillikTutar : x.YillikTutar,
                    yillikEfektifAylikTutar = (founderActive ? x.KurucuYillikTutar : x.YillikTutar) / 12,
                    normalAylikTutar = x.AylikTutar,
                    normalYillikTutar = x.YillikTutar,
                    kurucuAylikTutar = x.KurucuAylikTutar,
                    kurucuYillikTutar = x.KurucuYillikTutar,
                    kampanyaKodu = founderActive ? SubscriptionPlanCatalog.KurucuKampanyaKodu : string.Empty,
                    kurucuKontenjanKalan = remainingFounderSlots,
                    paraBirimi = "TRY",
                    aiMesajLimiti = x.AiMesajLimiti,
                    kullaniciLimiti = x.KullaniciLimiti,
                    musteriLimiti = x.MusteriLimiti,
                    faturaLimiti = x.FaturaLimiti,
                    isletmeLimiti = x.IsletmeLimiti,
                    gelirGiderIslemLimiti = x.GelirGiderIslemLimiti,
                    cariKartLimiti = x.CariKartLimiti,
                    urunHizmetLimiti = x.UrunHizmetLimiti,
                    bankaMutabakatiAktif = x.BankaMutabakatiAktif,
                    stokRaporAktif = x.StokRaporAktif,
                    muhasebeciErisimiAktif = x.MuhasebeciErisimiAktif,
                    cokluSubeAktif = x.CokluSubeAktif,
                    cokluParaBirimiAktif = x.CokluParaBirimiAktif,
                    apiErisimiAktif = x.ApiErisimiAktif,
                    oncelikliDestekAktif = x.OncelikliDestekAktif,
                    denemeGunSayisi = 0
                });

            return Results.Ok(plans);
        }).AllowAnonymous();

        var endpoint = app.MapGet(
            "/api/abonelik/durum",
            async (
                int? isletmeId,
                string? hesapTipi,
                IIsletmeService isletmeService,
                ISubscriptionEntitlementService entitlementService,
                CancellationToken ct) =>
            {
                var target = isletmeId.HasValue
                    ? await isletmeService.GetByIdAsync(isletmeId.Value)
                    : await isletmeService.GetActiveAsync();

                if (target is null)
                    return Results.NotFound(new { mesaj = "Isletme bulunamadi." });

                var effectiveType = NormalizeHesapTipi(hesapTipi, target.TenantTipi);
                var status = string.Equals(effectiveType, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
                    ? await entitlementService.GetMuhasebeciEntitlementAsync(target.Id, ct: ct)
                    : await entitlementService.GetIsletmeEntitlementAsync(target.Id, ct: ct);

                return Results.Ok(status);
            });

        var clerkOptions = app.Services.GetRequiredService<ClerkAuthenticationOptions>();
        if (clerkOptions.Enabled)
        {
            endpoint.RequireAuthorization();
        }
    }

    private static string NormalizeHesapTipi(string? requested, string? tenantTipi)
    {
        var value = string.IsNullOrWhiteSpace(requested) ? tenantTipi : requested;
        if (string.Equals(value, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase))
            return HesapTipleri.Muhasebeci;

        return HesapTipleri.Isletme;
    }

}
