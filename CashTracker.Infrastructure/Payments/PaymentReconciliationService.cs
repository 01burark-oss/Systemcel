using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        if (_provider is IPaymentStatusQueryProvider paymentQueryProvider)
            return await ReconcilePaymentsAsync(paymentQueryProvider, now, ct);
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

    private async Task<ProviderReconciliationResult> ReconcilePaymentsAsync(
        IPaymentStatusQueryProvider queryProvider, DateTime now, CancellationToken ct)
    {
        var current = now.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(now, DateTimeKind.Utc) : now.ToUniversalTime();
        var dayKey = current.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var checkedCount = 0;
        var unavailable = 0;
        var discrepancies = 0;
        var recorded = 0;
        var cursor = 0;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        if (_provider is IPaymentRefundProvider { RefundsEnabled: true } refundProvider)
        {
            var pending = await db.OdemeIadeTalimatlari.AsNoTracking().Where(x =>
                    x.Durum == "Gonderiliyor" || x.Durum == "SonucBekliyor" || x.Durum == "IncelemeGerekli")
                .Join(db.OdemeIslemleri.Where(x => x.OdemeSaglayici == _provider.Name),
                    refund => refund.OdemeIslemiId, payment => payment.Id, (refund, payment) => new { refund.Id, payment.IsletmeId })
                .ToListAsync(ct);
            var refunds = new PaymentRefundService(_dbFactory, _provider);
            foreach (var item in pending.Where(x => refundProvider.CanRefundBusiness(x.IsletmeId)))
                await refunds.ReconcileAsync(item.Id, ct);
        }
        while (true)
        {
            var payments = await db.OdemeIslemleri.AsNoTracking()
                .Where(x => x.OdemeSaglayici == _provider.Name && x.SaglayiciOturumId != "" && x.Id > cursor)
                .OrderBy(x => x.Id).Take(100).ToListAsync(ct);
            if (payments.Count == 0) break;
            foreach (var selected in payments)
            {
                ct.ThrowIfCancellationRequested();
                cursor = selected.Id;
                var lookup = await queryProvider.GetPaymentAsync(selected.SaglayiciOturumId, ct);
                if (!lookup.Available) { unavailable++; continue; }
                checkedCount++;
                // A callback can commit while the HTTP query is in flight. Compare its current state.
                var local = await db.OdemeIslemleri.AsNoTracking().SingleAsync(x => x.Id == selected.Id, ct);
                var completedRefunds = await db.OdemeIadeTalimatlari.AsNoTracking()
                    .Where(x => x.OdemeIslemiId == local.Id && x.Durum == "Tamamlandi").ToListAsync(ct);
                var refundTotal = completedRefunds.Count == 0 && local.Durum == PaymentTransactionStates.Refunded
                    ? local.ToplamTutar : completedRefunds.Sum(x => x.Tutar);
                var differences = FindPaymentDifferences(local, lookup, queryProvider.ExpectedTestMode, current, refundTotal);
                if (differences.Count == 0) continue;
                discrepancies++;
                var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                    string.Join(";", differences)))).ToLowerInvariant();
                var eventId = $"payment-mutabakat:{dayKey}:{local.Id}:{fingerprint}";
                if (await db.OdemeOlaylari.AsNoTracking().AnyAsync(x =>
                        x.OdemeSaglayici == _provider.Name && x.OlayId == eventId, ct)) continue;
                var finding = new OdemeOlayi
                {
                    OdemeSaglayici = _provider.Name,
                    OlayId = eventId,
                    OlayTipi = "payment.reconciliation.mismatch",
                    CheckoutAnahtari = local.CheckoutAnahtari,
                    SaglayiciIslemId = local.SaglayiciOturumId,
                    IslenmeDurumu = "IncelemeGerekli",
                    PayloadHash = fingerprint,
                    HataMesaji = string.Join("; ", differences),
                    SaglayiciAt = current,
                    AlindiAt = current
                };
                db.OdemeOlaylari.Add(finding);
                try { await db.SaveChangesAsync(ct); recorded++; }
                catch (DbUpdateException)
                {
                    db.Entry(finding).State = EntityState.Detached;
                    if (!await db.OdemeOlaylari.AsNoTracking().AnyAsync(x =>
                            x.OdemeSaglayici == _provider.Name && x.OlayId == eventId, ct)) throw;
                }
            }
        }
        return new ProviderReconciliationResult(checkedCount > 0, 0, discrepancies, recorded,
            unavailable > 0 ? "Bazi odemeler icin saglayici sorgusu kullanilabilir degil." : string.Empty,
            checkedCount, unavailable);
    }

    private static List<string> FindPaymentDifferences(OdemeIslemi local,
        ProviderPaymentLookupResult lookup, bool expectedTestMode, DateTime now, decimal localRefundTotal)
    {
        var result = new List<string>();
        var remote = lookup.Payment;
        if (remote is null)
        {
            if (local.Durum is PaymentTransactionStates.Succeeded or PaymentTransactionStates.Refunded)
                result.Add("Yerel tamamlanan odeme saglayicinin basarili odemelerinde bulunamadi.");
            else if (local.Durum == PaymentTransactionStates.CheckoutOpen &&
                     local.CheckoutExpiresAt is { } expires &&
                     (expires.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(expires, DateTimeKind.Utc) : expires.ToUniversalTime()) <= now)
                result.Add("Checkout suresi doldu; kesin odeme sonucu bildirimle dogrulanmadi.");
            return result;
        }
        if (!string.Equals(local.SaglayiciOturumId, remote.ProviderOrderId, StringComparison.Ordinal))
            result.Add("Saglayici siparis referansi farkli.");
        if (local.ToplamTutar != remote.Amount || local.ToplamTutar != remote.TotalAmount)
            result.Add("Siparis veya tahsilat tutari farkli.");
        if (!string.Equals(local.ParaBirimi, remote.Currency, StringComparison.OrdinalIgnoreCase))
            result.Add("Para birimi farkli.");
        if (remote.IsTestPayment != expectedTestMode)
            result.Add("Test ve canli odeme ortami farkli.");
        if (local.Durum != PaymentTransactionStates.Succeeded && local.Durum != PaymentTransactionStates.Refunded)
            result.Add("Saglayicida basarili tahsilat var; yerel odeme tamamlanmamis.");
        if (remote.RefundedAmount != localRefundTotal)
            result.Add("Saglayici iade toplami yerel odeme durumuyla uyusmuyor.");
        return result;
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
