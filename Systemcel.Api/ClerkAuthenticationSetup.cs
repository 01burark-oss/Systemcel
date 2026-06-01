using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Systemcel.Api;

internal static class ClerkAuthenticationSetup
{
    public static ClerkAuthenticationOptions Resolve(IConfiguration configuration)
    {
        var options = configuration.GetSection("Authentication:Clerk").Get<ClerkAuthenticationOptions>()
            ?? new ClerkAuthenticationOptions();

        options.Enabled = ReadBool(
            Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_ENABLED"),
            options.Enabled);
        options.Authority = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_AUTHORITY"),
            options.Authority) ?? string.Empty;
        options.Audience = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_AUDIENCE"),
            options.Audience) ?? string.Empty;
        options.AuthorizedParties = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_AUTHORIZED_PARTIES"),
            options.AuthorizedParties) ?? string.Empty;
        options.PublishableKey = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_PUBLISHABLE_KEY"),
            options.PublishableKey) ?? string.Empty;
        options.JsUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_JS_URL"),
            options.JsUrl) ?? string.Empty;

        return options;
    }

    public static void AddClerkAuthentication(
        this IServiceCollection services,
        ClerkAuthenticationOptions clerkOptions)
    {
        services.AddSingleton(clerkOptions);

        if (!clerkOptions.Enabled)
            return;

        var authority = NormalizeAuthority(clerkOptions.Authority);
        if (string.IsNullOrWhiteSpace(authority))
            throw new InvalidOperationException("Clerk auth aktif ama Authentication:Clerk:Authority bos.");

        var authorizedParties = clerkOptions.GetAuthorizedParties();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = !string.IsNullOrWhiteSpace(clerkOptions.Audience),
                    ValidAudience = string.IsNullOrWhiteSpace(clerkOptions.Audience)
                        ? null
                        : clerkOptions.Audience,
                    NameClaimType = "sub"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        ValidateAuthorizedParty(context.Principal, authorizedParties);
                        ValidateOrganizationStatus(context.Principal, clerkOptions.RejectPendingOrganizationStatus);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    private static void ValidateAuthorizedParty(System.Security.Claims.ClaimsPrincipal? principal, string[] allowed)
    {
        if (allowed.Length == 0)
            return;

        var azp = principal?.FindFirst("azp")?.Value;
        if (string.IsNullOrWhiteSpace(azp) || allowed.Contains(azp, StringComparer.OrdinalIgnoreCase))
            return;

        throw new SecurityTokenValidationException("Clerk azp claim izin verilen origin listesinde degil.");
    }

    private static void ValidateOrganizationStatus(
        System.Security.Claims.ClaimsPrincipal? principal,
        bool rejectPending)
    {
        if (!rejectPending)
            return;

        var status = principal?.FindFirst("sts")?.Value;
        if (!string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
            return;

        throw new SecurityTokenValidationException("Clerk organizasyon durumu pending.");
    }

    private static string NormalizeAuthority(string authority)
    {
        return string.IsNullOrWhiteSpace(authority)
            ? string.Empty
            : authority.Trim().TrimEnd('/');
    }

    private static bool ReadBool(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
