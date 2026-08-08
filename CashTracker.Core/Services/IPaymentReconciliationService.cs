using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IPaymentReconciliationProvider
{
    Task<ProviderSubscriptionLookupResult> GetSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default);
}

public interface IPaymentReconciliationService
{
    Task<ProviderReconciliationResult> ReconcileAsync(DateTime now, CancellationToken ct = default);
}
