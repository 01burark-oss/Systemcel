using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services;

public sealed class OpenAiReceiptOcrService : IReceiptOcrService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _httpClient;
    private readonly ReceiptOcrSettings _settings;

    public OpenAiReceiptOcrService(HttpClient httpClient, ReceiptOcrSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<ReceiptOcrResult> AnalyzeReceiptAsync(
        ReceiptOcrRequest request,
        CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("Receipt OCR ayarlari eksik.");

        if (!string.Equals(_settings.EffectiveProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Desteklenmeyen OCR provider: {_settings.EffectiveProvider}");

        if (request.ImageBytes is null || request.ImageBytes.Length == 0)
            throw new ArgumentException("Receipt image is required.", nameof(request));

        var endpoint = BuildResponsesEndpoint(_settings.EffectiveBaseUrl);
        return await AnalyzeReceiptWithModelAsync(request, endpoint, _settings.EffectiveModel, ct);
    }

    private async Task<ReceiptOcrResult> AnalyzeReceiptWithModelAsync(
        ReceiptOcrRequest request,
        Uri endpoint,
        string model,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("OpenAI OCR modeli eksik.");

        var mimeType = string.IsNullOrWhiteSpace(request.MimeType)
            ? "image/jpeg"
            : request.MimeType.Trim().ToLowerInvariant();
        var dataUrl = $"data:{mimeType};base64,{Convert.ToBase64String(request.ImageBytes)}";
        var payload = new
        {
            model = model.Trim(),
            store = false,
            max_output_tokens = 2048,
            input = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = BuildPrompt(request) },
                        new { type = "input_image", image_url = dataUrl, detail = "high" }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "receipt_expense",
                    strict = true,
                    schema = BuildReceiptSchema()
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _settings.EffectiveApiKey.Trim());

        using var response = await _httpClient.SendAsync(httpRequest, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI OCR failed: {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        var responseText = ExtractOutputText(responseBody);
        if (string.IsNullOrWhiteSpace(responseText))
            throw new InvalidOperationException("OpenAI OCR yaniti bos.");

        var parsed = JsonSerializer.Deserialize<OpenAiReceiptPayload>(
            ExtractJsonObject(responseText),
            JsonOptions);
        if (parsed is null)
            throw new InvalidOperationException("OpenAI OCR JSON yaniti okunamadi.");

        return new ReceiptOcrResult
        {
            Merchant = parsed.Merchant?.Trim() ?? string.Empty,
            ReceiptDate = TryParseReceiptDate(parsed.ReceiptDate),
            PaymentMethod = parsed.PaymentMethod?.Trim() ?? string.Empty,
            ReceiptTotal = parsed.ReceiptTotal,
            Items = parsed.Items?
                .Where(x => !string.IsNullOrWhiteSpace(x.RawName) && x.Amount > 0)
                .Select(x => new ReceiptOcrLineItem
                {
                    RawName = x.RawName!.Trim(),
                    Amount = x.Amount,
                    CandidateKalem = x.CandidateKalem?.Trim() ?? string.Empty,
                    Confidence = x.Confidence,
                    NeedsUserInput = x.NeedsUserInput
                })
                .ToList() ?? []
        };
    }

    private static Uri BuildResponsesEndpoint(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed) ||
            !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parsed.Host) ||
            !string.IsNullOrEmpty(parsed.UserInfo) ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            throw new InvalidOperationException(
                "ReceiptOcr BaseUrl mutlak, kimlik bilgisi icermeyen bir HTTPS adresi olmali.");
        }

        var builder = new UriBuilder(parsed)
        {
            Path = parsed.AbsolutePath.TrimEnd('/') + "/responses",
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }

    private static object BuildReceiptSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            required = new[]
            {
                "merchant",
                "receiptDate",
                "paymentMethod",
                "receiptTotal",
                "items"
            },
            properties = new
            {
                merchant = new { type = "string" },
                receiptDate = new { type = "string" },
                paymentMethod = new { type = "string" },
                receiptTotal = new { type = new[] { "number", "null" } },
                items = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[]
                        {
                            "rawName",
                            "amount",
                            "candidateKalem",
                            "confidence",
                            "needsUserInput"
                        },
                        properties = new
                        {
                            rawName = new { type = "string" },
                            amount = new { type = "number" },
                            candidateKalem = new { type = "string" },
                            confidence = new { type = new[] { "number", "null" } },
                            needsUserInput = new { type = "boolean" }
                        }
                    }
                }
            }
        };
    }

    private static string BuildPrompt(ReceiptOcrRequest request)
    {
        var categories = request.AvailableExpenseCategories.Count == 0
            ? "Genel Gider"
            : string.Join(", ", request.AvailableExpenseCategories
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));

        var captionLine = string.IsNullOrWhiteSpace(request.Caption)
            ? string.Empty
            : $"Kullanici notu: {request.Caption.Trim()}\n";

        return
            "Bu fis fotografini gider taslagi olarak oku. Yalnizca tanimli JSON semasini dondur.\n" +
            "Toplam, KDV, indirim ve para ustu satirlarini items listesine ekleme.\n" +
            "CandidateKalem yalnizca verilen genel gider kalemlerinden tam biri olabilir; emin degilsen bos birak ve needsUserInput true yap.\n" +
            "PaymentMethod yalnizca Nakit, KrediKarti, OnlineOdeme, Havale veya bos olabilir.\n" +
            "Tarih belirsizse receiptDate bos olsun.\n" +
            "Isletme: " + (string.IsNullOrWhiteSpace(request.BusinessName) ? "Bilinmiyor" : request.BusinessName.Trim()) + "\n" +
            captionLine +
            "Mevcut gider kalemleri: " + categories;
    }

    private static string ExtractOutputText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("output_text", out var direct) &&
            direct.ValueKind == JsonValueKind.String)
        {
            return direct.GetString()?.Trim() ?? string.Empty;
        }

        if (!doc.RootElement.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.ToString().Trim();
    }

    private static string ExtractJsonObject(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];

        throw new InvalidOperationException("OpenAI OCR yaniti JSON object icermiyor.");
    }

    private static DateTime? TryParseReceiptDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var raw = value.Trim();
        var formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "dd.MM.yyyy",
            "dd/MM/yyyy",
            "d.M.yyyy",
            "d/M/yyyy",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssK"
        };

        if (DateTime.TryParseExact(
                raw,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var exact))
        {
            return exact.TimeOfDay == TimeSpan.Zero
                ? exact.Date.Add(DateTime.Now.TimeOfDay)
                : exact;
        }

        if (DateTime.TryParse(
                raw,
                CultureInfo.GetCultureInfo("tr-TR"),
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return parsed.TimeOfDay == TimeSpan.Zero
                ? parsed.Date.Add(DateTime.Now.TimeOfDay)
                : parsed;
        }

        return null;
    }

    private sealed class OpenAiReceiptPayload
    {
        public string? Merchant { get; set; }
        public string? ReceiptDate { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? ReceiptTotal { get; set; }
        public OpenAiReceiptItem[]? Items { get; set; }
    }

    private sealed class OpenAiReceiptItem
    {
        public string? RawName { get; set; }
        public decimal Amount { get; set; }
        public string? CandidateKalem { get; set; }
        public decimal? Confidence { get; set; }
        public bool NeedsUserInput { get; set; }
    }
}
