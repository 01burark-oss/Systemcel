using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Systemcel.Api.Api;

internal static class SubscriptionApi
{
    public static void MapSubscriptionApi(this WebApplication app)
    {
        app.MapGet("/api/public/planlar", () =>
        {
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
                    aylikTutar = x.AylikTutar,
                    yillikTutar = x.YillikTutar > 0 ? x.YillikTutar : (decimal?)null,
                    yillikEfektifAylikTutar = x.YillikTutar > 0 ? x.YillikTutar / 12 : (decimal?)null,
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
                    denemeGunSayisi = x.HesapTipi == HesapTipleri.Muhasebeci ? 14 : 30
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
