using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api;

internal static class SubscriptionApi
{
    public static void MapSubscriptionApi(this WebApplication app)
    {
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
            endpoint.RequireAuthorization();
    }

    private static string NormalizeHesapTipi(string? requested, string? tenantTipi)
    {
        var value = string.IsNullOrWhiteSpace(requested) ? tenantTipi : requested;
        if (string.Equals(value, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase))
            return HesapTipleri.Muhasebeci;

        return HesapTipleri.Isletme;
    }
}
