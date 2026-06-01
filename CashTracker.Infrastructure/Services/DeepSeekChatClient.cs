using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Infrastructure.Services
{
    public sealed class DeepSeekChatClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly DeepSeekSettings _settings;

        public DeepSeekChatClient(HttpClient httpClient, DeepSeekSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<string> CompleteAsync(
            string model,
            IEnumerable<DeepSeekChatMessage> messages,
            double temperature,
            int maxTokens,
            CancellationToken ct = default)
        {
            if (!_settings.IsConfigured)
                throw new InvalidOperationException("DeepSeek API anahtari eksik.");

            var payload = new
            {
                model,
                messages = messages.Select(x => new
                {
                    role = x.Role,
                    content = x.Content
                }).ToArray(),
                thinking = new
                {
                    type = "disabled"
                },
                temperature,
                max_tokens = Math.Max(256, maxTokens),
                stream = false
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_settings.EffectiveBaseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.EffectiveApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                var safeBody = body.Length > 800 ? body[..800] : body;
                throw new InvalidOperationException($"DeepSeek yaniti basarisiz: {(int)response.StatusCode} {response.ReasonPhrase}. {safeBody}");
            }

            var content = ExtractAssistantContent(body);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("DeepSeek bos yanit dondu.");

            return content.Trim();
        }

        private static string ExtractAssistantContent(string body)
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var first = choices[0];
            if (!first.TryGetProperty("message", out var message) ||
                message.ValueKind != JsonValueKind.Object ||
                !message.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.String)
            {
                return string.Empty;
            }

            return content.GetString() ?? string.Empty;
        }
    }

    public sealed record DeepSeekChatMessage(string Role, string Content);
}
