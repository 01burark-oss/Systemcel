using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IPaymentRefundProvider : IPaymentStatusQueryProvider
{
    Task<ProviderRefundResult> RefundAsync(string orderId, decimal amount, string referenceNo, CancellationToken ct = default);
}

public interface IPaymentRefundService
{
    Task<OdemeIadeTalimati> RequestAsync(int businessId, int paymentId, decimal amount, string idempotencyKey, CancellationToken ct = default);
    Task<OdemeIadeTalimati> DispatchAsync(long instructionId, CancellationToken ct = default);
    Task<OdemeIadeTalimati> ReconcileAsync(long instructionId, CancellationToken ct = default);
}
