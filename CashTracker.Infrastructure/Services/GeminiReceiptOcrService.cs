using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class GeminiReceiptOcrService : IReceiptOcrService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly HttpClient _httpClient;
        private readonly ReceiptOcrSettings _settings;

        public GeminiReceiptOcrService(HttpClient httpClient, ReceiptOcrSettings settings)
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

            if (!string.Equals(_settings.EffectiveProvider, "Gemini", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Desteklenmeyen OCR provider: {_settings.EffectiveProvider}");

            if (request.ImageBytes is null || request.ImageBytes.Length == 0)
                throw new ArgumentException("Receipt image is required.", nameof(request));

            ReceiptOcrResult? primaryResult = null;
            Exception? primaryError = null;

            try
            {
                primaryResult = await AnalyzeReceiptWithModelAsync(request, _settings.EffectiveModel, ct);
                if (!ShouldRetryWithFallback(primaryResult))
                    return primaryResult;
            }
            catch (Exception ex)
            {
                primaryError = ex;
            }

            var fallbackModel = _settings.EffectiveFallbackModel;
            if (!string.IsNullOrWhiteSpace(fallbackModel) &&
                !string.Equals(fallbackModel, _settings.EffectiveModel, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return await AnalyzeReceiptWithModelAsync(request, fallbackModel, ct);
                }
                catch when (primaryResult is not null)
                {
                    return primaryResult;
                }
                catch (Exception ex) when (primaryError is not null)
                {
                    throw new InvalidOperationException("Gemini OCR birincil ve yedek modelle basarisiz oldu.", ex);
                }
            }

            if (primaryError is not null)
                throw primaryError;

            return primaryResult ?? throw new InvalidOperationException("Gemini OCR yaniti bos.");
        }

        private async Task<ReceiptOcrResult> AnalyzeReceiptWithModelAsync(
            ReceiptOcrRequest request,
            string model,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("Gemini OCR modeli eksik.");

            var generationConfig = new Dictionary<string, object>
            {
                ["responseMimeType"] = "application/json",
                ["temperature"] = 0,
                ["maxOutputTokens"] = 2048
            };

            if (IsFlashModel(model))
            {
                generationConfig["thinkingConfig"] = new
                {
                    thinkingBudget = 0
                };
            }

            var payload = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = BuildPrompt(request) },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = string.IsNullOrWhiteSpace(request.MimeType) ? "image/jpeg" : request.MimeType.Trim(),
                                    data = Convert.ToBase64String(request.ImageBytes)
                                }
                            }
                        }
                    }
                },
                generationConfig
            };

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model.Trim())}:generateContent" +
                $"?key={Uri.EscapeDataString(_settings.EffectiveApiKey)}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gemini OCR failed: {response.StatusCode} - {responseBody}");

            var responseText = ExtractGeminiText(responseBody);
            if (string.IsNullOrWhiteSpace(responseText))
                throw new InvalidOperationException("Gemini OCR yaniti bos.");

            var jsonText = ExtractJsonObject(responseText);
            var parsed = JsonSerializer.Deserialize<GeminiReceiptPayload>(jsonText, JsonOptions);
            if (parsed is null)
                throw new InvalidOperationException("Gemini OCR JSON yaniti okunamadi.");

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

        private bool ShouldRetryWithFallback(ReceiptOcrResult result)
        {
            if (string.IsNullOrWhiteSpace(_settings.EffectiveFallbackModel))
                return false;

            return result.ReceiptTotal is null || result.Items.Count == 0;
        }

        private static bool IsFlashModel(string model)
        {
            return model.Contains("flash", StringComparison.OrdinalIgnoreCase);
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
                : $"Kullanici notu: {request.Caption!.Trim()}\n";

            return
                "Sen bir fis OCR ve gider kalemi esleme motorusun.\n" +
                "Gorevin sadece GIDER fislerini okumak ve yalnizca gecerli JSON dondurmektir.\n" +
                "Markdown, aciklama, kod blogu veya ek metin dondurme.\n" +
                "Cevap su sekilde bir JSON object olmali:\n" +
                "{\n" +
                "  \"merchant\": \"\",\n" +
                "  \"receiptDate\": \"yyyy-MM-dd\" veya \"\",\n" +
                "  \"paymentMethod\": \"Nakit|KrediKarti|OnlineOdeme|Havale\" veya \"\",\n" +
                "  \"receiptTotal\": 0.0 veya null,\n" +
                "  \"items\": [\n" +
                "    {\n" +
                "      \"rawName\": \"\",\n" +
                "      \"amount\": 0.0,\n" +
                "      \"candidateKalem\": \"\",\n" +
                "      \"confidence\": 0.0,\n" +
                "      \"needsUserInput\": true\n" +
                "    }\n" +
                "  ]\n" +
                "}\n" +
                "Kurallar:\n" +
                "- Sadece satis/gider satirlarini item olarak dondur.\n" +
                "- Toplam, KDV, indirim, para ustu gibi satirlari item listesine ekleme.\n" +
                "- Bir item mevcut gider kalemlerinden birine guvenilir sekilde eslesiyorsa candidateKalem alanina TAM kalem adini koy.\n" +
                "- CandidateKalem genel bir isletme gider grubu olmali; urun adi, marka adi veya tekil fis satiri candidateKalem olamaz.\n" +
                "- Ornek genel kalemler: Mutfak Giderleri, Personel Giderleri, Temizlik Giderleri, Kira Giderleri, Fatura Giderleri.\n" +
                "- Emin degilsen candidateKalem bos olsun ve needsUserInput true olsun.\n" +
                "- Payment method yalnizca izin verilen 4 degerden biri olsun; emin degilsen bos birak.\n" +
                "- Tarih belirsizse bos birak.\n" +
                "- Isletme: " + (string.IsNullOrWhiteSpace(request.BusinessName) ? "Bilinmiyor" : request.BusinessName.Trim()) + "\n" +
                captionLine +
                "Mevcut gider kalemleri: " + categories;
        }

        private static string ExtractGeminiText(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Object ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
                    sb.Append(textNode.GetString());
            }

            return sb.ToString().Trim();
        }

        private static string ExtractJsonObject(string responseText)
        {
            var trimmed = responseText.Trim();
            if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
                return trimmed;

            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                return trimmed[start..(end + 1)];

            throw new InvalidOperationException("Gemini OCR yaniti JSON object icermiyor.");
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

            if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out var parsed))
            {
                return parsed.TimeOfDay == TimeSpan.Zero
                    ? parsed.Date.Add(DateTime.Now.TimeOfDay)
                    : parsed;
            }

            return null;
        }

        private sealed class GeminiReceiptPayload
        {
            public string? Merchant { get; set; }
            public string? ReceiptDate { get; set; }
            public string? PaymentMethod { get; set; }
            public decimal? ReceiptTotal { get; set; }
            public GeminiReceiptItem[]? Items { get; set; }
        }

        private sealed class GeminiReceiptItem
        {
            public string? RawName { get; set; }
            public decimal Amount { get; set; }
            public string? CandidateKalem { get; set; }
            public decimal? Confidence { get; set; }
            public bool NeedsUserInput { get; set; }
        }
    }
}
