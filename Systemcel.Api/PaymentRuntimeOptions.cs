namespace Systemcel.Api;

public sealed class PaymentRuntimeOptions
{
    public string Provider { get; init; } = "Unconfigured";
    public string FakeSecret { get; init; } = string.Empty;
    public string PublicBaseUrl { get; init; } = string.Empty;
    public decimal VatRate { get; init; } = 20m;

    public bool UsesFakeProvider => string.Equals(Provider, "Fake", StringComparison.OrdinalIgnoreCase);
}
