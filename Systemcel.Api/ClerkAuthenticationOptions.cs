namespace Systemcel.Api;

internal sealed class ClerkAuthenticationOptions
{
    public bool Enabled { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string AuthorizedParties { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string JsUrl { get; set; } = string.Empty;
    public bool RejectPendingOrganizationStatus { get; set; } = true;

    public string[] GetAuthorizedParties()
    {
        return AuthorizedParties
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
