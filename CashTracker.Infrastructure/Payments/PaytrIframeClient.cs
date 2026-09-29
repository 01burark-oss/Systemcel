using System.Text.Json;

namespace CashTracker.Infrastructure.Payments;

public sealed record PaytrIframeCheckoutRequest(
    PaytrIframeTokenInput TokenInput,
    string CustomerName,
    string CustomerAddress,
    string CustomerPhone,
    Uri SuccessUrl,
    Uri FailureUrl,
    int TimeoutMinutes = 30,
    string Language = "tr");

/// <summary>
/// Creates only the hosted payment form URL. Payment is final only after the
/// separately verified server callback has been matched and committed.
/// </summary>
public sealed class PaytrIframeClient(HttpClient httpClient)
{
    private static readonly Uri TokenEndpoint = new("https://www.paytr.com/odeme/api/get-token");
    private const int MaxResponseBytes = 8192;

    public async Task<Uri> CreateCheckoutUrlAsync(
        PaytrIframeCheckoutRequest request,
        string merchantKey,
        string merchantSalt,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fields = BuildTokenFields(request, merchantKey, merchantSalt);
        using var content = new FormUrlEncodedContent(fields);
        using var message = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint) { Content = content };
        using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("PayTR token request failed.", null, response.StatusCode);
        if (response.Content.Headers.ContentLength is > MaxResponseBytes)
            throw new InvalidOperationException("PayTR token response is too large.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[MaxResponseBytes + 1];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), ct);
            if (read == 0) break;
            length += read;
        }
        if (length > MaxResponseBytes)
            throw new InvalidOperationException("PayTR token response is too large.");
        using var body = JsonDocument.Parse(buffer.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 4 });
        if (body.RootElement.ValueKind != JsonValueKind.Object ||
            !body.RootElement.TryGetProperty("status", out var status) ||
            status.ValueKind != JsonValueKind.String || status.GetString() != "success" ||
            !body.RootElement.TryGetProperty("token", out var tokenElement) ||
            tokenElement.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("PayTR did not issue a checkout token.");

        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512 ||
            token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new InvalidOperationException("PayTR returned an invalid checkout token.");

        return new Uri("https://www.paytr.com/odeme/guvenli/" + token);
    }

    public static IReadOnlyDictionary<string, string> BuildTokenFields(
        PaytrIframeCheckoutRequest request,
        string merchantKey,
        string merchantSalt)
    {
        ArgumentNullException.ThrowIfNull(request);
        var input = request.TokenInput ?? throw new ArgumentException("Payment input is required.", nameof(request));
        var signature = PaytrIframeProtocol.CreateToken(input, merchantKey, merchantSalt);
        ValidateRequired(request.CustomerName, 60, nameof(request.CustomerName));
        ValidateRequired(request.CustomerAddress, 400, nameof(request.CustomerAddress));
        ValidateRequired(request.CustomerPhone, 20, nameof(request.CustomerPhone));
        ValidateRedirect(request.SuccessUrl, nameof(request.SuccessUrl));
        ValidateRedirect(request.FailureUrl, nameof(request.FailureUrl));
        if (request.TimeoutMinutes is < 1 or > 120)
            throw new ArgumentOutOfRangeException(nameof(request.TimeoutMinutes));
        if (request.Language is not ("tr" or "en"))
            throw new ArgumentException("Language must be tr or en.", nameof(request));

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["merchant_id"] = input.MerchantId,
            ["user_ip"] = input.UserIp,
            ["merchant_oid"] = input.MerchantOrderId,
            ["email"] = input.Email,
            ["payment_amount"] = input.AmountKurus.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["paytr_token"] = signature,
            ["user_basket"] = input.BasketBase64,
            ["no_installment"] = input.NoInstallment ? "1" : "0",
            ["max_installment"] = input.MaxInstallment.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["currency"] = input.Currency,
            ["test_mode"] = input.TestMode ? "1" : "0",
            ["user_name"] = request.CustomerName,
            ["user_address"] = request.CustomerAddress,
            ["user_phone"] = request.CustomerPhone,
            ["merchant_ok_url"] = request.SuccessUrl.AbsoluteUri,
            ["merchant_fail_url"] = request.FailureUrl.AbsoluteUri,
            ["timeout_limit"] = request.TimeoutMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["lang"] = request.Language,
            ["debug_on"] = "0"
        };
    }

    private static void ValidateRequired(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl))
            throw new ArgumentException("Required PayTR customer field is invalid.", name);
    }

    private static void ValidateRedirect(Uri uri, string name)
    {
        if (uri is null || !uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps ||
            uri.AbsoluteUri.Length > 400 || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Payment return URL must be an absolute HTTPS URL.", name);
    }
}
