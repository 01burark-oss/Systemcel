using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class OcrSpaceDeepSeekReceiptOcrService : IReceiptOcrService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly HttpClient _httpClient;
        private readonly ReceiptOcrSettings _ocrSettings;
        private readonly DeepSeekSettings _deepSeekSettings;
        private readonly DeepSeekChatClient _deepSeek;

        public OcrSpaceDeepSeekReceiptOcrService(
            HttpClient httpClient,
            ReceiptOcrSettings ocrSettings,
            DeepSeekSettings deepSeekSettings,
            DeepSeekChatClient deepSeek)
        {
            _httpClient = httpClient;
            _ocrSettings = ocrSettings;
            _deepSeekSettings = deepSeekSettings;
            _deepSeek = deepSeek;
        }

        public async Task<ReceiptOcrResult> AnalyzeReceiptAsync(ReceiptOcrRequest request, CancellationToken ct = default)
        {
            if (!_ocrSettings.IsConfigured)
                throw new InvalidOperationException("Receipt OCR ayarlari eksik.");

            if (!_deepSeekSettings.IsConfigured)
                throw new InvalidOperationException("DeepSeek API anahtari eksik.");

            if (request.ImageBytes is null || request.ImageBytes.Length == 0)
                throw new ArgumentException("Receipt image is required.", nameof(request));

            if (request.ImageBytes.Length > 1_000_000)
                throw new InvalidOperationException("OCR.space free limit 1 MB. Telegram fotografini daha dusuk boyutta tekrar gonderin.");

            var rawText = await ReadTextAsync(request, ct);
            var answer = await _deepSeek.CompleteAsync(
                _deepSeekSettings.EffectiveFlashModel,
                new[]
                {
                    new DeepSeekChatMessage("system", BuildSystemPrompt()),
                    new DeepSeekChatMessage("user", BuildUserPrompt(request, rawText))
                },
                0.1,
                1600,
                ct);

            var parsed = JsonSerializer.Deserialize<ReceiptPayload>(ExtractJsonObject(answer), JsonOptions)
                ?? throw new InvalidOperationException("DeepSeek OCR JSON yaniti okunamadi.");

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

        private async Task<string> ReadTextAsync(ReceiptOcrRequest request, CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_ocrSettings.EffectiveLanguage), "language");
            form.Add(new StringContent("false"), "isOverlayRequired");
            form.Add(new StringContent("true"), "scale");
            form.Add(new StringContent("true"), "detectOrientation");
            form.Add(new StringContent("2"), "OCREngine");
            form.Add(new StringContent("true"), "isTable");

            var imageContent = new ByteArrayContent(request.ImageBytes);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
                string.IsNullOrWhiteSpace(request.MimeType) ? "image/jpeg" : request.MimeType.Trim());
            form.Add(imageContent, "file", string.IsNullOrWhiteSpace(request.FileName) ? "receipt.jpg" : request.FileName);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.ocr.space/parse/image");
            httpRequest.Headers.TryAddWithoutValidation("apikey", _ocrSettings.EffectiveApiKey);
            httpRequest.Content = form;

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OCR.space failed: {(int)response.StatusCode} {response.ReasonPhrase}. {Trim(body)}");

            var parsed = JsonSerializer.Deserialize<OcrSpaceResponse>(body, JsonOptions)
                ?? throw new InvalidOperationException("OCR.space yaniti okunamadi.");

            if (parsed.IsErroredOnProcessing)
                throw new InvalidOperationException("OCR.space hata dondu: " + string.Join(" | ", parsed.ErrorMessage ?? []));

            var text = string.Join(
                "\n",
                parsed.ParsedResults?
                    .Select(x => x.ParsedText)
                    .Where(x => !string.IsNullOrWhiteSpace(x)) ?? []);

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("OCR.space bos metin dondu.");

            return text.Trim();
        }

        private static string BuildSystemPrompt()
        {
            return
                "Sen OCR metnini Systemcel gider fis JSON'una ceviren motorsun. Sadece gecerli JSON dondur. Markdown yok.\n" +
                "{\"merchant\":\"\",\"receiptDate\":\"yyyy-MM-dd veya bos\",\"paymentMethod\":\"Nakit|KrediKarti|OnlineOdeme|Havale veya bos\",\"receiptTotal\":0.0,\"items\":[{\"rawName\":\"\",\"amount\":0.0,\"candidateKalem\":\"\",\"confidence\":0.0,\"needsUserInput\":true}]}\n" +
                "Toplam, KDV, indirim, para ustu satirlarini items icine koyma. Emin degilsen candidateKalem bos ve needsUserInput true olsun.";
        }

        private static string BuildUserPrompt(ReceiptOcrRequest request, string rawText)
        {
            var categories = request.AvailableExpenseCategories.Count == 0
                ? "Genel Gider"
                : string.Join(", ", request.AvailableExpenseCategories.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));

            return
                "Isletme: " + (string.IsNullOrWhiteSpace(request.BusinessName) ? "Bilinmiyor" : request.BusinessName.Trim()) + "\n" +
                (string.IsNullOrWhiteSpace(request.Caption) ? "" : "Kullanici notu: " + request.Caption.Trim() + "\n") +
                "Mevcut gider kalemleri: " + categories + "\n\n" +
                "OCR metni:\n" +
                rawText;
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

            throw new InvalidOperationException("DeepSeek OCR yaniti JSON object icermiyor.");
        }

        private static DateTime? TryParseReceiptDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var formats = new[] { "yyyy-MM-dd", "yyyy/MM/dd", "dd.MM.yyyy", "dd/MM/yyyy", "d.M.yyyy", "d/M/yyyy" };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var exact))
                return exact.Date.Add(DateTime.Now.TimeOfDay);

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out var parsed)
                ? parsed
                : null;
        }

        private static string Trim(string value) => value.Length > 800 ? value[..800] : value;

        private sealed class OcrSpaceResponse
        {
            public OcrSpaceParsedResult[]? ParsedResults { get; set; }
            public bool IsErroredOnProcessing { get; set; }
            public string[]? ErrorMessage { get; set; }
        }

        private sealed class OcrSpaceParsedResult
        {
            public string? ParsedText { get; set; }
        }

        private sealed class ReceiptPayload
        {
            public string? Merchant { get; set; }
            public string? ReceiptDate { get; set; }
            public string? PaymentMethod { get; set; }
            public decimal? ReceiptTotal { get; set; }
            public ReceiptItem[]? Items { get; set; }
        }

        private sealed class ReceiptItem
        {
            public string? RawName { get; set; }
            public decimal Amount { get; set; }
            public string? CandidateKalem { get; set; }
            public decimal? Confidence { get; set; }
            public bool NeedsUserInput { get; set; }
        }
    }
}
