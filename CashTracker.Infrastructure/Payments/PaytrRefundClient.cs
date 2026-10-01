using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Models;

namespace CashTracker.Infrastructure.Payments;

public sealed class PaytrRefundClient(HttpClient httpClient)
{
    public async Task<ProviderRefundResult> RefundAsync(string orderId, decimal amount, string referenceNo,
        string merchantId, string merchantKey, string merchantSalt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderId) || orderId.Length > 64 || orderId.Any(c => !char.IsAsciiLetterOrDigit(c)) ||
            string.IsNullOrWhiteSpace(referenceNo) || referenceNo.Length > 64 || referenceNo.Any(c => !char.IsAsciiLetterOrDigit(c)) ||
            amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Invalid refund order, amount or reference.");
        if (string.IsNullOrWhiteSpace(merchantId) || merchantId.Length > 12 || merchantId.Any(c => !char.IsAsciiDigit(c)) ||
            string.IsNullOrWhiteSpace(merchantKey) || string.IsNullOrWhiteSpace(merchantSalt))
            throw new ArgumentException("Invalid refund merchant configuration.");
        var value = amount.ToString("0.00", CultureInfo.InvariantCulture);
        var token = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(merchantKey),
            Encoding.UTF8.GetBytes(merchantId + orderId + value + merchantSalt)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.paytr.com/odeme/iade")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["merchant_id"] = merchantId, ["merchant_oid"] = orderId, ["return_amount"] = value,
                ["reference_no"] = referenceNo, ["paytr_token"] = token
            })
        };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode) return Unknown("http_error");
            const int maxBytes = 65536;
            if (response.Content.Headers.ContentLength is > maxBytes) return Unknown("response_too_large");
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var buffer = new byte[maxBytes + 1];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(length), deadline.Token);
                if (read == 0) break;
                length += read;
            }
            if (length > maxBytes) return Unknown("response_too_large");
            using var json = JsonDocument.Parse(buffer.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 8 });
            var body = json.RootElement;
            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("status", out var status) ||
                status.ValueKind != JsonValueKind.String) return Unknown("invalid_response");
            if (status.GetString() == "failed") return new(false, true, "order_not_found");
            if (status.GetString() == "error")
            {
                var overAmount = body.TryGetProperty("err_no", out var code) && code.ValueKind == JsonValueKind.String && code.GetString() == "006";
                return new(false, overAmount, overAmount ? "refund_exceeds_payment" : "provider_error");
            }
            if (status.GetString() != "success" || !body.TryGetProperty("merchant_oid", out var order) ||
                order.ValueKind != JsonValueKind.String || order.GetString() != orderId ||
                !PaytrPaymentQueryClient.TryMoney(body, "return_amount", out var returned) || returned != amount ||
                !body.TryGetProperty("reference_no", out var reference) || reference.ValueKind != JsonValueKind.String || reference.GetString() != referenceNo ||
                !body.TryGetProperty("is_test", out var mode) ||
                (mode.ValueKind == JsonValueKind.String ? mode.GetString() : mode.GetRawText()) != "1")
                return Unknown("refund_response_mismatch");
            return new(true, true);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Unknown("timeout"); }
        catch (HttpRequestException) { return Unknown("transport_error"); }
        catch (IOException) { return Unknown("transport_error"); }
        catch (JsonException) { return Unknown("invalid_response"); }
    }

    private static ProviderRefundResult Unknown(string code) => new(false, false, code);
}
