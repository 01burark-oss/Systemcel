using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ISubscriptionLifecycleService
    {
        Task<SubscriptionCheckoutResult> BeginCheckoutAsync(
            SubscriptionCheckoutCommand command,
            CancellationToken ct = default);

        Task<PaymentWebhookProcessingResult> ProcessWebhookAsync(
            PaymentWebhookEnvelope envelope,
            CancellationToken ct = default);

        Task CancelAtPeriodEndAsync(int businessId, CancellationToken ct = default);

        Task<SubscriptionPlanChangeResult> SchedulePlanChangeAsync(
            SubscriptionCheckoutCommand command,
            CancellationToken ct = default);

        Task CancelScheduledPlanChangeAsync(int businessId, CancellationToken ct = default);

        Task<SubscriptionReconciliationResult> ReconcileAsync(
            DateTime now,
            CancellationToken ct = default);
    }
}
