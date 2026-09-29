using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Payments;

/// <summary>
/// Starts a single-charge PayTR iFrame checkout. Only the signed, persisted callback
/// can complete the subscription; the browser return page has no payment authority.
/// </summary>
public sealed class PaytrSubscriptionProvider : IPaymentProvider
{
    private readonly PaytrIframeClient _client;
    private readonly string _merchantId;
    private readonly string _merchantKey;
    private readonly string _merchantSalt;
    private readonly bool _testMode;

    public PaytrSubscriptionProvider(
        PaytrIframeClient client,
        string merchantId,
        string merchantKey,
        string merchantSalt,
        bool testMode)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _merchantId = merchantId;
        _merchantKey = merchantKey;
        _merchantSalt = merchantSalt;
        _testMode = testMode;
        // Validate configuration before the provider can accept checkout traffic.
        PaytrIframeProtocol.CreateToken(new PaytrIframeTokenInput(
            merchantId, "127.0.0.1", "ConfigurationCheck", "check@example.com", 1,
            Convert.ToBase64String(Encoding.UTF8.GetBytes("[]")), true, 0, "TL", testMode),
            merchantKey, merchantSalt);
    }

    public string Name => "PayTR";

    public async Task<PaymentCheckoutSession> CreateCheckoutAsync(
        PaymentCheckoutRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Quote.TrialDays != 0)
            throw new InvalidOperationException("PayTR tek çekim akışı deneme süresiyle kullanılamaz.");
        if (!string.Equals(request.Quote.Currency, "TRY", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PayTR abonelik tahsilatı yalnız TL için yapılandırılmıştır.");

        var orderId = CreateOrderId(request.CustomerReference, request.MerchantReference);
        var amountKurus = PaytrIframeProtocol.ToKurus(request.Quote.TotalAmount);
        var basket = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new object[][]
        {
            ["Systemcel abonelik", request.Quote.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture), 1]
        })));
        var input = new PaytrIframeTokenInput(
            _merchantId, request.CustomerIp ?? string.Empty, orderId,
            request.CustomerEmail, amountKurus, basket, true, 0, "TL", _testMode);
        var paytrFormUrl = await _client.CreateCheckoutUrlAsync(new PaytrIframeCheckoutRequest(
            input,
            request.CustomerName ?? string.Empty,
            request.CustomerAddress ?? string.Empty,
            request.CustomerPhone ?? string.Empty,
            new Uri(request.CallbackUrl, "/api/odeme/paytr/donus?sonuc=basarili"),
            new Uri(request.CallbackUrl, "/api/odeme/paytr/donus?sonuc=basarisiz")),
            _merchantKey, _merchantSalt, ct);

        var token = paytrFormUrl.Segments[^1];
        var displayUrl = new Uri(request.CallbackUrl,
            "/api/odeme/paytr/form?token=" + Uri.EscapeDataString(token));
        return new PaymentCheckoutSession(Name, orderId, displayUrl,
            DateTime.UtcNow.AddMinutes(30), DateTime.UtcNow);
    }

    public PaymentWebhookVerificationResult VerifyWebhook(PaymentWebhookEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.Payload) || string.IsNullOrWhiteSpace(envelope.Signature) ||
            envelope.Payload.Length > 4096)
            return PaymentWebhookVerificationResult.Invalid("PayTR bildirimi eksik veya çok büyük.");

        PaytrCallbackPayload? payload;
        try { payload = JsonSerializer.Deserialize<PaytrCallbackPayload>(envelope.Payload); }
        catch (JsonException) { return PaymentWebhookVerificationResult.Invalid("PayTR bildirim biçimi geçersiz."); }
        if (payload is null || !PaytrIframeProtocol.VerifyCallback(new PaytrIframeCallback(
                payload.MerchantOrderId, payload.Status, payload.TotalAmountKurus, envelope.Signature),
                _merchantKey, _merchantSalt))
            return PaymentWebhookVerificationResult.Invalid("PayTR bildirim imzası geçersiz.");

        if (!long.TryParse(payload.TotalAmountKurus, NumberStyles.None, CultureInfo.InvariantCulture, out var amountKurus))
            return PaymentWebhookVerificationResult.Invalid("PayTR bildirim tutarı geçersiz.");
        var eventType = payload.Status == "success"
            ? PaymentEventTypes.PaymentSucceeded : PaymentEventTypes.PaymentFailed;
        var eventId = $"paytr-{payload.MerchantOrderId}-{payload.Status}";
        var eventHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            envelope.Payload + envelope.Signature))).ToLowerInvariant();
        return PaymentWebhookVerificationResult.Valid(new PaymentWebhookEvent(
            Name, eventId, eventType, payload.MerchantOrderId, payload.MerchantOrderId,
            amountKurus / 100m, "TRY", DateTime.UtcNow, eventHash));
    }

    public static string CreateOrderId(string customerReference, string merchantReference)
    {
        if (string.IsNullOrWhiteSpace(customerReference) || string.IsNullOrWhiteSpace(merchantReference))
            throw new ArgumentException("Payment references are required.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            customerReference + ":" + merchantReference))).ToLowerInvariant();
    }

    private sealed record PaytrCallbackPayload(
        string MerchantOrderId,
        string Status,
        string TotalAmountKurus);
}
