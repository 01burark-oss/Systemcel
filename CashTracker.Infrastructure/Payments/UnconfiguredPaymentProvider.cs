using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Payments;

/// <summary>
/// Production guard: a missing live provider configuration must fail loudly instead
/// of accidentally accepting a fake checkout.
/// </summary>
public sealed class UnconfiguredPaymentProvider : IPaymentProvider
{
    private const string ErrorMessage =
        "Odeme saglayicisi yapilandirilmadi. Canli ortamda checkout acilamaz.";

    public string Name => "Unconfigured";

    public Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        throw new InvalidOperationException(ErrorMessage);
    }

    public PaymentWebhookVerificationResult VerifyWebhook(PaymentWebhookEnvelope envelope) =>
        PaymentWebhookVerificationResult.Invalid(ErrorMessage);
}
