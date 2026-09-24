using System.Text.Json;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class AiAssistantConversationToken
{
    private const string Purpose = "systemcel.ai.follow-up.v2";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    internal sealed class Context
    {
        public string Purpose { get; set; } = string.Empty;
        public int BusinessId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public DateTimeOffset IssuedAt { get; set; }
    }

    public static string Create(
        ISecretProtector protector,
        int businessId,
        string userId,
        string question,
        string answer,
        DateTimeOffset? now = null)
    {
        var context = new Context
        {
            Purpose = Purpose,
            BusinessId = businessId,
            UserId = userId,
            Question = question[..Math.Min(question.Length, 500)],
            Answer = answer[..Math.Min(answer.Length, 2500)],
            IssuedAt = now ?? DateTimeOffset.UtcNow
        };
        return protector.Protect(JsonSerializer.Serialize(context));
    }

    public static bool TryRead(
        ISecretProtector protector,
        string? token,
        int businessId,
        string userId,
        out Context context,
        DateTimeOffset? now = null)
    {
        context = new Context();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 12_000 ||
            !(token.StartsWith("aesgcm1:", StringComparison.Ordinal) ||
              token.StartsWith("dpapi1:", StringComparison.Ordinal)) ||
            !protector.TryUnprotect(token, out var clear))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<Context>(clear);
            if (parsed is null)
                return false;

            var age = (now ?? DateTimeOffset.UtcNow) - parsed.IssuedAt;
            if (parsed.Purpose != Purpose ||
                parsed.BusinessId != businessId || parsed.UserId != userId ||
                string.IsNullOrWhiteSpace(parsed.Question) ||
                string.IsNullOrWhiteSpace(parsed.Answer) ||
                parsed.Question.Length > 500 || parsed.Answer.Length > 2500 ||
                age < TimeSpan.Zero || age > Lifetime)
                return false;

            context = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
