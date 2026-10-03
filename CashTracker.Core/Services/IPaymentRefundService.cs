using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IPaymentRefundProvider : IPaymentStatusQueryProvider
{
    bool RefundsEnabled => ExpectedTestMode;
    bool CanRefundBusiness(int businessId) => RefundsEnabled && businessId > 0;
    Task<ProviderRefundResult> RefundAsync(string orderId, decimal amount, string referenceNo, CancellationToken ct = default);
}

public interface IPaymentRefundService
{
    Task<OdemeIadeTalimati> ApproveCancellationAsync(int subscriptionId, string approvedBy, CancellationToken ct = default);
    Task<OdemeIadeTalimati> RequestAsync(int businessId, int paymentId, decimal amount, string idempotencyKey, CancellationToken ct = default);
    Task<OdemeIadeTalimati> DispatchAsync(long instructionId, CancellationToken ct = default, bool liveRefundConfirmed = false);
    Task<OdemeIadeTalimati> ReconcileAsync(long instructionId, CancellationToken ct = default);
}
