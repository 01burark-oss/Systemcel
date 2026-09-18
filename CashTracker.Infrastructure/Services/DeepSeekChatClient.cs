using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
            string userIsolationKey,
            bool enableThinking,
            string reasoningEffort = "low",
            CancellationToken ct = default)
        {
            if (!_settings.IsConfigured)
                throw new InvalidOperationException("DeepSeek API anahtari eksik.");

            if (string.IsNullOrWhiteSpace(userIsolationKey))
                throw new ArgumentException("AI kullanıcı izolasyon anahtarı eksik.", nameof(userIsolationKey));

            var payload = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["messages"] = messages.Select(x => new
                {
                    role = x.Role,
                    content = x.Content
                }).ToArray(),
                ["thinking"] = new
                {
                    type = enableThinking ? "enabled" : "disabled"
                },
                ["max_tokens"] = Math.Clamp(maxTokens, 256, 2400),
                ["stream"] = false,
                ["user_id"] = CreateAnonymousUserId(userIsolationKey)
            };
            if (enableThinking)
                payload["reasoning_effort"] = NormalizeReasoningEffort(reasoningEffort);
            else
                payload["temperature"] = Math.Clamp(temperature, 0d, 1d);

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
                var requestId = response.Headers.TryGetValues("x-request-id", out var values)
                    ? values.FirstOrDefault()
                    : null;
                var suffix = string.IsNullOrWhiteSpace(requestId) ? string.Empty : $" RequestId={requestId}.";
                throw new HttpRequestException($"DeepSeek isteği {(int)response.StatusCode} durumuyla başarısız oldu.{suffix}");
            }

            var content = ExtractAssistantContent(body);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("DeepSeek bos yanit dondu.");

            return content.Trim();
        }

        private string CreateAnonymousUserId(string isolationKey)
        {
            var digest = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(_settings.EffectiveApiKey),
                Encoding.UTF8.GetBytes($"systemcel-tenant:{isolationKey.Trim()}"));
            return $"tenant-{Convert.ToHexString(digest)[..32].ToLowerInvariant()}";
        }

        private static string NormalizeReasoningEffort(string value)
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized is "high" or "max" ? normalized : "low";
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
