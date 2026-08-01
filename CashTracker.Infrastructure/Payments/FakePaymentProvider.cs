using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Payments
{
    public sealed class FakePaymentProvider : IPaymentProvider
    {
        private readonly byte[] _secret;

        public FakePaymentProvider(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16)
                throw new ArgumentException("Sahte odeme saglayicisi anahtari en az 16 karakter olmalidir.", nameof(secret));

            _secret = Encoding.UTF8.GetBytes(secret);
        }

        public string Name => "Fake";

        public Task<PaymentCheckoutSession> CreateCheckoutAsync(
            PaymentCheckoutRequest request,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (request.Quote.TotalAmount <= 0)
                throw new InvalidOperationException("Odeme oturumu sifir veya negatif tutarla acilamaz.");

            var sessionId = $"fake_{ComputeSha256(request.MerchantReference)[..24]}";
            var checkoutUrl = new Uri(
                request.CallbackUrl,
                $"/api/odeme/test/checkout/{sessionId}?merchantReference={Uri.EscapeDataString(request.MerchantReference)}");
            return Task.FromResult(new PaymentCheckoutSession(
                Name,
                sessionId,
                checkoutUrl,
                DateTime.UtcNow.AddMinutes(30),
                DateTime.UtcNow.AddDays(request.Quote.TrialDays)));
        }

        public PaymentWebhookVerificationResult VerifyWebhook(PaymentWebhookEnvelope envelope)
        {
            if (string.IsNullOrWhiteSpace(envelope.Payload) || string.IsNullOrWhiteSpace(envelope.Signature))
                return PaymentWebhookVerificationResult.Invalid("Webhook govdesi ve imzasi zorunludur.");

            byte[] suppliedSignature;
            try
            {
                suppliedSignature = Convert.FromBase64String(envelope.Signature);
            }
            catch (FormatException)
            {
                return PaymentWebhookVerificationResult.Invalid("Webhook imza bicimi gecersiz.");
            }

            using var hmac = new HMACSHA256(_secret);
            var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(envelope.Payload));
            if (suppliedSignature.Length != expectedSignature.Length ||
                !CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
                return PaymentWebhookVerificationResult.Invalid("Webhook imzasi dogrulanamadi.");

            try
            {
                var payload = JsonSerializer.Deserialize<FakeWebhookPayload>(envelope.Payload, JsonOptions);
                if (payload is null ||
                    string.IsNullOrWhiteSpace(payload.EventId) ||
                    string.IsNullOrWhiteSpace(payload.EventType) ||
                    string.IsNullOrWhiteSpace(payload.MerchantReference))
                    return PaymentWebhookVerificationResult.Invalid("Webhook zorunlu alanlari eksik.");

                var paymentEvent = new PaymentWebhookEvent(
                    Name,
                    payload.EventId,
                    payload.EventType,
                    payload.MerchantReference,
                    payload.ProviderTransactionId ?? string.Empty,
                    payload.Amount,
                    string.IsNullOrWhiteSpace(payload.Currency) ? "TRY" : payload.Currency,
                    payload.OccurredAt == default ? DateTime.UtcNow : payload.OccurredAt,
                    ComputeSha256(envelope.Payload));
                return PaymentWebhookVerificationResult.Valid(paymentEvent);
            }
            catch (JsonException)
            {
                return PaymentWebhookVerificationResult.Invalid("Webhook JSON bicimi gecersiz.");
            }
        }

        public string SignPayload(string payload)
        {
            using var hmac = new HMACSHA256(_secret);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private static string ComputeSha256(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        private sealed record FakeWebhookPayload(
            string EventId,
            string EventType,
            string MerchantReference,
            string? ProviderTransactionId,
            decimal Amount,
            string Currency,
            DateTime OccurredAt);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
