using System.Net;
using System.Text.Json;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Services;
using Xunit;

namespace CashTracker.Tests;

public sealed class OpenAiReceiptOcrServiceTests
{
    [Fact]
    public async Task AnalyzeReceiptAsync_SendsVisionPayloadWithHeaderOnlySecretAndParsesJson()
    {
        const string apiKey = "openai-super-secret";
        var handler = new CapturingHandler();
        var service = new OpenAiReceiptOcrService(
            new HttpClient(handler),
            new ReceiptOcrSettings
            {
                Provider = "OpenAI",
                ApiKey = apiKey,
                BaseUrl = "https://api.openai.com/v1",
                Model = "gpt-5-mini",
                FallbackModel = string.Empty
            });

        var result = await service.AnalyzeReceiptAsync(new ReceiptOcrRequest
        {
            BusinessName = "Test",
            FileName = "receipt.jpg",
            MimeType = "image/jpeg",
            ImageBytes = [0xff, 0xd8, 0xff]
        });

        Assert.Equal("https://api.openai.com/v1/responses", handler.Url);
        Assert.DoesNotContain(apiKey, handler.Url, StringComparison.Ordinal);
        Assert.DoesNotContain("?key=", handler.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(apiKey, handler.AuthorizationParameter);

        using var requestJson = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal("gpt-5-mini", requestJson.RootElement.GetProperty("model").GetString());
        Assert.False(requestJson.RootElement.GetProperty("store").GetBoolean());
        var content = requestJson.RootElement
            .GetProperty("input")[0]
            .GetProperty("content");
        Assert.Equal("input_text", content[0].GetProperty("type").GetString());
        Assert.Equal("input_image", content[1].GetProperty("type").GetString());
        Assert.Equal("data:image/jpeg;base64,/9j/", content[1].GetProperty("image_url").GetString());

        Assert.Equal("Market", result.Merchant);
        Assert.Equal(10m, result.ReceiptTotal);
        var item = Assert.Single(result.Items);
        Assert.Equal("Su", item.RawName);
        Assert.Equal(10m, item.Amount);
    }

    [Fact]
    public async Task AnalyzeReceiptAsync_RejectsInsecureBaseUrlBeforeSending()
    {
        var handler = new CapturingHandler();
        var service = new OpenAiReceiptOcrService(
            new HttpClient(handler),
            new ReceiptOcrSettings
            {
                Provider = "OpenAI",
                ApiKey = "test-key",
                BaseUrl = "http://api.openai.com/v1",
                Model = "gpt-5-mini"
            });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeReceiptAsync(new ReceiptOcrRequest
            {
                MimeType = "image/jpeg",
                ImageBytes = [0xff, 0xd8, 0xff]
            }));

        Assert.Contains("HTTPS", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.SendCount);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int SendCount { get; private set; }
        public string Url { get; private set; } = string.Empty;
        public string AuthorizationScheme { get; private set; } = string.Empty;
        public string AuthorizationParameter { get; private set; } = string.Empty;
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            Url = request.RequestUri?.ToString() ?? string.Empty;
            AuthorizationScheme = request.Headers.Authorization?.Scheme ?? string.Empty;
            AuthorizationParameter = request.Headers.Authorization?.Parameter ?? string.Empty;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            const string body = "{\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"text\":\"{\\\"merchant\\\":\\\"Market\\\",\\\"receiptDate\\\":\\\"2026-08-25\\\",\\\"paymentMethod\\\":\\\"KrediKarti\\\",\\\"receiptTotal\\\":10,\\\"items\\\":[{\\\"rawName\\\":\\\"Su\\\",\\\"amount\\\":10,\\\"candidateKalem\\\":\\\"Mutfak Giderleri\\\",\\\"confidence\\\":0.98,\\\"needsUserInput\\\":false}]}\"}]}]}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };
        }
    }
}
