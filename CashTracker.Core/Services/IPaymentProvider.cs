using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IPaymentProvider
    {
        string Name { get; }

        Task<PaymentCheckoutSession> CreateCheckoutAsync(
            PaymentCheckoutRequest request,
            CancellationToken ct = default);

        PaymentWebhookVerificationResult VerifyWebhook(PaymentWebhookEnvelope envelope);
    }
}
