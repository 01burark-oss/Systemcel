namespace CashTracker.Core.Models;

public sealed class JevSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.typesafe.ai/v1";
    public string Model { get; set; } = "jev-latest";
    public int TimeoutSeconds { get; set; } = 20;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    public string EffectiveBaseUrl => string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.typesafe.ai/v1" : BaseUrl.TrimEnd('/');
    public string EffectiveModel => string.IsNullOrWhiteSpace(Model) ? "jev-latest" : Model.Trim();
    public int EffectiveTimeoutSeconds => Math.Clamp(TimeoutSeconds, 3, 60);
}

public sealed record JevChoiceQuestion(
    string Instructions,
    IReadOnlyDictionary<string, string?> Options);

public sealed record JevChoiceResult(
    bool Available,
    string Choice,
    double Confidence,
    IReadOnlyDictionary<string, double> Probabilities,
    string Model)
{
    public static JevChoiceResult Unavailable { get; } = new(
        false,
        string.Empty,
        0,
        new Dictionary<string, double>(),
        string.Empty);
}
