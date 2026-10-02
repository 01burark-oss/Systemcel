namespace Systemcel.Api;

public sealed class PaymentRuntimeOptions
{
    public string Provider { get; init; } = "Unconfigured";
    public string FakeSecret { get; init; } = string.Empty;
    public string PaytrMerchantId { get; init; } = string.Empty;
    public string PaytrMerchantKey { get; init; } = string.Empty;
    public string PaytrMerchantSalt { get; init; } = string.Empty;
    public bool PaytrTestMode { get; init; } = true;
    public bool PaytrLiveEnabled { get; init; }
    public bool PaytrLiveCheckoutPaused { get; init; }
    public int[] PaytrLiveBusinessIds { get; init; } = [];
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
        !string.IsNullOrWhiteSpace(PaytrMerchantId) &&
        !string.IsNullOrWhiteSpace(PaytrMerchantKey) && !string.IsNullOrWhiteSpace(PaytrMerchantSalt) &&
        (PaytrTestMode ? ValidBusinessIds(PaytrTestBusinessIds) : PaytrLiveEnabled && ValidBusinessIds(PaytrLiveBusinessIds)) &&
        Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var baseUri) &&
        baseUri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(baseUri.UserInfo) &&
        string.IsNullOrEmpty(baseUri.Query) && string.IsNullOrEmpty(baseUri.Fragment);
    public bool AllowsPaytrTestBusiness(int businessId) =>
        PaytrTestMode && AllowsPaytrBusiness(businessId);
    public bool AllowsPaytrBusiness(int businessId) => UsesPaytrProvider && businessId > 0 &&
        (PaytrTestMode || !PaytrLiveCheckoutPaused) &&
        (PaytrTestMode ? PaytrTestBusinessIds : PaytrLiveBusinessIds).Contains(businessId);

    private static bool ValidBusinessIds(int[] ids) => ids.Length > 0 && ids.All(id => id > 0);
}
