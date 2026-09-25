using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.Extensions.Logging;

namespace CashTracker.Infrastructure.Services;

public sealed class JevDecisionService : IJevDecisionService
{
    private const int MaxQuestions = 64;
    private const int MaxOptions = 255;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly JevSettings _settings;
    private readonly ILogger<JevDecisionService>? _logger;

    public JevDecisionService(
        HttpClient httpClient,
        JevSettings settings,
        ILogger<JevDecisionService>? logger = null)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        ValidateSettings(settings);
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task<IReadOnlyDictionary<string, JevChoiceResult>> ChooseAsync(
        object state,
        IReadOnlyDictionary<string, JevChoiceQuestion> questions,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateQuestions(questions);
        if (!IsConfigured)
            return Unavailable(questions);

        var payload = new JevRequest(
            state,
            _settings.EffectiveModel,
            questions.ToDictionary(
                x => x.Key,
                x => new JevQuestion("choice", x.Value.Instructions.Trim(), x.Value.Options),
                StringComparer.Ordinal));

        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.EffectiveBaseUrl}/systemone")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (HttpRequestException)
            {
                _logger?.LogWarning("Jev decision request failed due to a transport error.");
                return Unavailable(questions);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger?.LogWarning("Jev decision request timed out.");
                return Unavailable(questions);
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return await ReadResultsAsync(response, questions, ct);
                    }
                    catch (JsonException)
                    {
                        _logger?.LogWarning("Jev decision response was not valid JSON.");
                        return Unavailable(questions);
                    }
                }

                if (attempt < 2 && response.StatusCode is HttpStatusCode.TooManyRequests or (HttpStatusCode)529)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(200 * (attempt + 1));
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(retryAfter.TotalMilliseconds, 1500)), ct);
                    continue;
                }

                _logger?.LogWarning("Jev decision request failed. StatusCode={StatusCode}", (int)response.StatusCode);
                return Unavailable(questions);
            }
        }

        return Unavailable(questions);
    }

    private static IReadOnlyDictionary<string, JevChoiceResult> Unavailable(
        IReadOnlyDictionary<string, JevChoiceQuestion> questions) =>
        questions.Keys.ToDictionary(x => x, _ => JevChoiceResult.Unavailable, StringComparer.Ordinal);

    private static async Task<IReadOnlyDictionary<string, JevChoiceResult>> ReadResultsAsync(
        HttpResponseMessage response,
        IReadOnlyDictionary<string, JevChoiceQuestion> questions,
        CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var body = await JsonSerializer.DeserializeAsync<JevResponse>(stream, JsonOptions, ct);
        if (body?.Answers is null)
            return Unavailable(questions);

        var results = new Dictionary<string, JevChoiceResult>(StringComparer.Ordinal);
        foreach (var pair in questions)
        {
            if (!body.Answers.TryGetValue(pair.Key, out var answer) ||
                string.IsNullOrWhiteSpace(answer.Choice) ||
                !pair.Value.Options.ContainsKey(answer.Choice))
            {
                results[pair.Key] = JevChoiceResult.Unavailable;
                continue;
            }

            var probabilities = answer.Probabilities?
                .Where(x => pair.Value.Options.ContainsKey(x.Key) && double.IsFinite(x.Value))
                .ToDictionary(x => x.Key, x => Math.Clamp(x.Value, 0, 1), StringComparer.Ordinal)
                ?? new Dictionary<string, double>();
            results[pair.Key] = new JevChoiceResult(
                true,
                answer.Choice,
                Math.Clamp(answer.Confidence, 0, 1),
                probabilities,
                body.Model ?? string.Empty);
        }
        return results;
    }

    private static void ValidateSettings(JevSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Uri.TryCreate(settings.EffectiveBaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Jev base URL must use HTTPS.", nameof(settings));
    }

    private static void ValidateQuestions(IReadOnlyDictionary<string, JevChoiceQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        if (questions.Count is 0 or > MaxQuestions)
            throw new ArgumentException($"Jev requests must contain 1-{MaxQuestions} questions.", nameof(questions));

        foreach (var pair in questions)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value.Instructions))
                throw new ArgumentException("Jev question ids and instructions are required.", nameof(questions));
            if (pair.Value.Options.Count is < 2 or > MaxOptions || pair.Value.Options.Keys.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException($"Each Jev choice must contain 2-{MaxOptions} named options.", nameof(questions));
        }
    }

    private sealed record JevRequest(
        [property: JsonPropertyName("state")] object State,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("questions")] IReadOnlyDictionary<string, JevQuestion> Questions);

    private sealed record JevQuestion(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("instructions")] string Instructions,
        [property: JsonPropertyName("criteria")] IReadOnlyDictionary<string, string?> Criteria);

    private sealed class JevResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("answers")]
        public Dictionary<string, JevAnswer>? Answers { get; init; }
    }

    private sealed class JevAnswer
    {
        [JsonPropertyName("choice")]
        public string Choice { get; init; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("probabilities")]
        public Dictionary<string, double>? Probabilities { get; init; }
    }
}
