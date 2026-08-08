using System.Globalization;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed class PaymentReconciliationService : IPaymentReconciliationService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IPaymentProvider _provider;

    public PaymentReconciliationService(IDbContextFactory<CashTrackerDbContext> dbFactory, IPaymentProvider provider)
    {
        _dbFactory = dbFactory;
        _provider = provider;
    }

    public async Task<ProviderReconciliationResult> ReconcileAsync(DateTime now, CancellationToken ct = default)
    {
        if (_provider is not IPaymentReconciliationProvider reconciliationProvider)
            return new ProviderReconciliationResult(false, 0, 0, 0, "Saglayici mutabakat sorgusunu desteklemiyor.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var subscriptions = await db.Abonelikler.AsNoTracking()
            .Where(x => x.OdemeSaglayici == _provider.Name && x.SaglayiciAbonelikId != "")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        var checkedCount = 0;
        var discrepancies = 0;
        var recorded = 0;
        var providerAvailable = false;
        var dayKey = now.ToUniversalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        foreach (var local in subscriptions)
        {
            ct.ThrowIfCancellationRequested();
            var lookup = await reconciliationProvider.GetSubscriptionAsync(local.SaglayiciAbonelikId, ct);
            if (!lookup.Available)
                continue;

            providerAvailable = true;
            checkedCount++;
            var differences = FindDifferences(local, lookup.Subscription);
            if (differences.Count == 0)
                continue;

            discrepancies++;
            var providerState = lookup.Subscription?.State ?? "SaglayicidaYok";
            var eventId = $"mutabakat:{dayKey}:{local.SaglayiciAbonelikId}:{local.Durum}:{providerState}";
            var exists = await db.OdemeOlaylari.AsNoTracking().AnyAsync(x => x.OdemeSaglayici == _provider.Name && x.OlayId == eventId, ct);
            if (exists)
                continue;

            var checkoutKey = await db.OdemeIslemleri.AsNoTracking()
                .Where(x => x.IsletmeId == local.IsletmeId && x.OdemeSaglayici == _provider.Name)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => x.CheckoutAnahtari)
                .FirstOrDefaultAsync(ct) ?? string.Empty;
            db.OdemeOlaylari.Add(new OdemeOlayi
            {
                OdemeSaglayici = _provider.Name,
                OlayId = eventId,
                OlayTipi = "subscription.reconciliation.mismatch",
                CheckoutAnahtari = checkoutKey,
                SaglayiciIslemId = local.SaglayiciAbonelikId,
                IslenmeDurumu = "IncelemeGerekli",
                PayloadHash = string.Empty,
                HataMesaji = string.Join("; ", differences),
                SaglayiciAt = now,
                AlindiAt = now
            });
            recorded++;
        }

        if (recorded > 0)
            await db.SaveChangesAsync(ct);

        return new ProviderReconciliationResult(providerAvailable, checkedCount, discrepancies, recorded,
            providerAvailable ? string.Empty : "Saglayici mutabakat verisi kullanilabilir degil.");
    }

    private static List<string> FindDifferences(Abonelik local, ProviderSubscriptionSnapshot? remote)
    {
        if (remote is null)
            return new List<string> { $"Saglayicida bulunamadi; yerel durum={local.Durum}" };

        var result = new List<string>();
        if (!string.Equals(local.Durum, remote.State, StringComparison.OrdinalIgnoreCase))
            result.Add($"Durum farki: yerel={local.Durum}, saglayici={remote.State}");
        if (!string.IsNullOrWhiteSpace(remote.PlanCode) && !string.Equals(local.PlanKodu, remote.PlanCode, StringComparison.OrdinalIgnoreCase))
            result.Add($"Plan farki: yerel={local.PlanKodu}, saglayici={remote.PlanCode}");
        if (local.DonemSonundaIptal != remote.CancelAtPeriodEnd)
            result.Add($"Donem sonu iptal farki: yerel={local.DonemSonundaIptal}, saglayici={remote.CancelAtPeriodEnd}");
        if (local.DonemBitisAt.HasValue && remote.PeriodEndAt.HasValue && Math.Abs((local.DonemBitisAt.Value - remote.PeriodEndAt.Value).TotalMinutes) > 5)
            result.Add($"Donem bitisi farki: yerel={local.DonemBitisAt:O}, saglayici={remote.PeriodEndAt:O}");
        return result;
    }
}
