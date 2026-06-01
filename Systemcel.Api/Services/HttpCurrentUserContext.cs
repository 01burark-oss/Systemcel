using System.Security.Claims;
using CashTracker.Core.Services;

namespace Systemcel.Api.Services;

internal sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserIdentity? GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var providerUserId = FirstNonEmpty(
            principal.FindFirst("sub")?.Value,
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            principal.FindFirst("user_id")?.Value);

        if (string.IsNullOrWhiteSpace(providerUserId))
            return null;

        return new CurrentUserIdentity(
            providerUserId.Trim(),
            FirstNonEmpty(
                principal.FindFirst(ClaimTypes.Email)?.Value,
                principal.FindFirst("email")?.Value,
                principal.FindFirst("email_address")?.Value,
                principal.FindFirst("primary_email_address")?.Value),
            FirstNonEmpty(
                principal.FindFirst(ClaimTypes.Name)?.Value,
                principal.FindFirst("name")?.Value,
                principal.FindFirst("full_name")?.Value));
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
