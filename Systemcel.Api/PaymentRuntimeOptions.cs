namespace Systemcel.Api;

public sealed class PaymentRuntimeOptions
{
    public string Provider { get; init; } = "Unconfigured";
    public string FakeSecret { get; init; } = string.Empty;
    public string PaytrMerchantId { get; init; } = string.Empty;
    public string PaytrMerchantKey { get; init; } = string.Empty;
    public string PaytrMerchantSalt { get; init; } = string.Empty;
    public bool PaytrTestMode { get; init; } = true;
    public string[] PaytrTrustedProxyIps { get; init; } = [];
    public int[] PaytrTestBusinessIds { get; init; } = [];
    public string PublicBaseUrl { get; init; } = string.Empty;
    public decimal VatRate { get; init; } = 20m;
    public bool FreeTrialEnabled { get; init; }
    public int BusinessTrialDays { get; init; } = 30;
    public int AccountantTrialDays { get; init; } = 14;
    public decimal AccountantPlatformCommissionRate { get; init; } = 10m;

    public bool UsesFakeProvider => string.Equals(Provider, "Fake", StringComparison.OrdinalIgnoreCase);
    public bool UsesPaytrProvider => string.Equals(Provider, "PayTR", StringComparison.OrdinalIgnoreCase) &&
        PaytrTestMode && !string.IsNullOrWhiteSpace(PaytrMerchantId) &&
        !string.IsNullOrWhiteSpace(PaytrMerchantKey) && !string.IsNullOrWhiteSpace(PaytrMerchantSalt) &&
        PaytrTestBusinessIds.Length > 0 &&
        Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var baseUri) &&
        baseUri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(baseUri.UserInfo) &&
        string.IsNullOrEmpty(baseUri.Query) && string.IsNullOrEmpty(baseUri.Fragment);
    public bool AllowsPaytrTestBusiness(int businessId) =>
        UsesPaytrProvider && PaytrTestBusinessIds.Contains(businessId);
}
