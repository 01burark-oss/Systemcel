using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public static class PaymentBillingPeriods
    {
        public const string Monthly = "Aylik";
        public const string Annual = "Yillik";
    }

    public static class PaymentEventTypes
    {
        public const string TrialAuthorized = "trial.authorized";
        public const string PaymentSucceeded = "payment.succeeded";
        public const string PaymentFailed = "payment.failed";
        public const string PaymentRefunded = "payment.refunded";
        public const string SubscriptionCancelled = "subscription.cancelled";
    }

    public static class PaymentTransactionStates
    {
        public const string Preparing = "Hazirlaniyor";
        public const string CheckoutOpen = "CheckoutAcik";
        public const string TrialAuthorized = "DenemeYetkilendirildi";
        public const string Succeeded = "Basarili";
        public const string Failed = "Basarisiz";
        public const string Refunded = "IadeEdildi";
        public const string Cancelled = "IptalEdildi";
    }

    public sealed record PaymentQuote(
        string PlanCode,
        string AccountType,
        string BillingPeriod,
        string Currency,
        decimal NetAmount,
        decimal VatRate,
        decimal VatAmount,
        decimal TotalAmount,
        int TrialDays,
        int ExtraCustomerCredits,
        int IncludedCustomerCount,
        decimal CustomerCreditUnitAmount);

    public sealed record PaymentCheckoutRequest(
        string MerchantReference,
        PaymentQuote Quote,
        string CustomerReference,
        string CustomerEmail,
        Uri SuccessUrl,
        Uri FailureUrl,
        Uri CallbackUrl,
        IReadOnlyDictionary<string, string>? Metadata = null);

    public sealed record PaymentCheckoutSession(
        string Provider,
        string ProviderSessionId,
        Uri CheckoutUrl,
        DateTime ExpiresAt,
        DateTime? FirstChargeAt);

    public sealed record PaymentWebhookEvent(
        string Provider,
        string EventId,
        string EventType,
        string MerchantReference,
        string ProviderTransactionId,
        decimal Amount,
        string Currency,
        DateTime OccurredAt,
        string PayloadHash);

    public sealed record PaymentWebhookEnvelope(string Payload, string Signature);

    public sealed record PaymentWebhookVerificationResult(
        bool IsValid,
        PaymentWebhookEvent? Event,
        string Error)
    {
        public static PaymentWebhookVerificationResult Invalid(string error) => new(false, null, error);
        public static PaymentWebhookVerificationResult Valid(PaymentWebhookEvent paymentEvent) => new(true, paymentEvent, string.Empty);
    }

    public sealed record SubscriptionCheckoutCommand(
        int BusinessId,
        string AccountType,
        string PlanCode,
        string BillingPeriod,
        int ExtraCustomerCredits,
        string IdempotencyKey,
        string UserReference,
        string CustomerEmail,
        string ConsentTextVersion,
        string ConsentText,
        string ClientIp,
        string UserAgent,
        Uri SuccessUrl,
        Uri FailureUrl,
        Uri CallbackUrl);

    public sealed record SubscriptionCheckoutResult(
        int PaymentTransactionId,
        PaymentQuote Quote,
        PaymentCheckoutSession Session,
        bool Reused);

    public sealed record PaymentWebhookProcessingResult(
        bool Accepted,
        bool Duplicate,
        string State,
        string Message);

    public sealed record SubscriptionReconciliationResult(
        int ExpiredTrials,
        int ExpiredSubscriptions,
        int CancelledSubscriptions,
        int GracePeriodsEnded,
        int SevenDayReminders = 0,
        int ThreeDayReminders = 0);

    public sealed record ProviderSubscriptionSnapshot(
        string ProviderSubscriptionId,
        string State,
        string PlanCode,
        DateTime? PeriodEndAt,
        bool CancelAtPeriodEnd);

    public sealed record ProviderSubscriptionLookupResult(
        bool Available,
        ProviderSubscriptionSnapshot? Subscription,
        string Error = "");

    public sealed record ProviderReconciliationResult(
        bool ProviderAvailable,
        int CheckedSubscriptions,
        int DiscrepancyCount,
        int RecordedFindings,
        string Message = "");
}
