using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Models;

namespace CashTracker.Infrastructure.Payments;

public sealed class PaytrPaymentQueryClient(HttpClient httpClient)
{
    private static readonly Uri Endpoint = new("https://www.paytr.com/odeme/durum-sorgu");
    private const int MaxResponseBytes = 65536;

    public async Task<ProviderPaymentLookupResult> GetPaymentAsync(
        string orderId, string merchantId, string merchantKey, string merchantSalt,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderId) || orderId.Length > 64 ||
            orderId.Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException("Invalid PayTR order reference.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(merchantId) || merchantId.Length > 12 ||
            merchantId.Any(c => !char.IsAsciiDigit(c)))
            throw new ArgumentException("Invalid PayTR merchant configuration.", nameof(merchantId));
        if (string.IsNullOrWhiteSpace(merchantKey) || string.IsNullOrWhiteSpace(merchantSalt))
            throw new ArgumentException("PayTR query credentials are required.");

        var signature = Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(merchantKey), Encoding.UTF8.GetBytes(merchantId + orderId + merchantSalt)));
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["merchant_id"] = merchantId, ["merchant_oid"] = orderId, ["paytr_token"] = signature
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var requestToken = deadline.Token;
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestToken);
            if (!response.IsSuccessStatusCode)
                return Unavailable("http_error");
            if (response.Content.Headers.ContentLength is > MaxResponseBytes)
                return Unavailable("response_too_large");

            await using var stream = await response.Content.ReadAsStreamAsync(requestToken);
            var buffer = new byte[MaxResponseBytes + 1];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(length), requestToken);
                if (read == 0) break;
                length += read;
            }
            if (length > MaxResponseBytes)
                return Unavailable("response_too_large");
            using var body = JsonDocument.Parse(buffer.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 8 });
            return Parse(body.RootElement, orderId);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Unavailable("timeout"); }
        catch (HttpRequestException) { return Unavailable("transport_error"); }
        catch (IOException) { return Unavailable("transport_error"); }
        catch (JsonException) { return Unavailable("invalid_response"); }
    }

    private static ProviderPaymentLookupResult Parse(JsonElement body, string orderId)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("status", out var status) ||
            status.ValueKind != JsonValueKind.String)
            return Unavailable("invalid_response");
        if (status.GetString() == "error")
        {
            // 004 says that no successful charge was found. It cannot establish a failed charge.
            return body.TryGetProperty("err_no", out var code) && code.ValueKind == JsonValueKind.String &&
                   code.GetString() == "004"
                ? new ProviderPaymentLookupResult(true, null, "004")
                : Unavailable("provider_error");
        }
        if (status.GetString() != "success" || !TryMoney(body, "payment_amount", out var amount) ||
            !TryMoney(body, "payment_total", out var total) || amount <= 0 || total < amount ||
            !body.TryGetProperty("currency", out var currency) || currency.ValueKind != JsonValueKind.String ||
            !body.TryGetProperty("test_mode", out var mode))
            return Unavailable("invalid_response");
        var modeValue = mode.ValueKind == JsonValueKind.String ? mode.GetString() : mode.GetRawText();
        if (modeValue is not ("0" or "1"))
            return Unavailable("invalid_response");
        var currencyCode = currency.GetString()?.ToUpperInvariant();
        if (currencyCode == "TL") currencyCode = "TRY";
        if (currencyCode is not ("TRY" or "EUR" or "USD" or "GBP" or "RUB"))
            return Unavailable("invalid_response");

        decimal refunded = 0;
        var refundItems = new List<ProviderRefundSnapshot>();
        if (body.TryGetProperty("returns", out var returns) && returns.ValueKind != JsonValueKind.Null)
        {
            if (returns.ValueKind != JsonValueKind.Array)
                return Unavailable("invalid_response");
            foreach (var refund in returns.EnumerateArray())
            {
                if (refund.ValueKind != JsonValueKind.Object || !TryMoney(refund, "return_amount", out var value) ||
                    value > total - refunded)
                    return Unavailable("invalid_response");
                refunded += value;
                var reference = refund.TryGetProperty("reference_no", out var refValue) && refValue.ValueKind == JsonValueKind.String
                    ? refValue.GetString() ?? string.Empty : string.Empty;
                if (reference.Length > 64) return Unavailable("invalid_response");
                refundItems.Add(new ProviderRefundSnapshot(reference, value));
            }
        }
        return new ProviderPaymentLookupResult(true,
            new ProviderPaymentSnapshot(orderId, amount, total, currencyCode, modeValue == "1", refunded, refundItems));
    }

    internal static bool TryMoney(JsonElement body, string key, out decimal amount)
    {
        amount = 0;
        if (!body.TryGetProperty(key, out var element) ||
            element.ValueKind is not (JsonValueKind.String or JsonValueKind.Number)) return false;
        var value = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        if (string.IsNullOrEmpty(value) || value.Length > 20) return false;
        var separator = -1;
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsAsciiDigit(value[i])) continue;
            if (value[i] is not ('.' or ',') || separator >= 0 || i == 0) return false;
            separator = i;
        }
        if (separator >= 0 && value.Length - separator - 1 is not (1 or 2)) return false;
        return decimal.TryParse(value.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out amount);
    }

    private static ProviderPaymentLookupResult Unavailable(string code) => new(false, null, code);
}
