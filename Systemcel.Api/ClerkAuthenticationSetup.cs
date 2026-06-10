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
        options.AuthorizedParties = CombineCsv(
            FirstNonEmpty(
                Environment.GetEnvironmentVariable("SYSTEMCEL_CLERK_AUTHORIZED_PARTIES"),
                options.AuthorizedParties),
            FirstNonEmpty(
                Environment.GetEnvironmentVariable("SYSTEMCEL_ALLOWED_ORIGINS"),
                configuration["Systemcel:AllowedOrigins"]));
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
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrWhiteSpace(accessToken) &&
                            path.StartsWithSegments("/hubs/muhasebeci-sohbet"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var authorizedPartyError = ValidateAuthorizedParty(context.Principal, authorizedParties);
                        if (!string.IsNullOrWhiteSpace(authorizedPartyError))
                        {
                            context.Fail(authorizedPartyError);
                            return Task.CompletedTask;
                        }

                        var organizationError = ValidateOrganizationStatus(context.Principal, clerkOptions.RejectPendingOrganizationStatus);
                        if (!string.IsNullOrWhiteSpace(organizationError))
                        {
                            context.Fail(organizationError);
                            return Task.CompletedTask;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    private static string ValidateAuthorizedParty(System.Security.Claims.ClaimsPrincipal? principal, string[] allowed)
    {
        if (allowed.Length == 0)
            return string.Empty;

        var azp = principal?.FindFirst("azp")?.Value;
        if (string.IsNullOrWhiteSpace(azp) || allowed.Contains(azp, StringComparer.OrdinalIgnoreCase))
            return string.Empty;

        if (allowed.Any(x => OriginMatches(azp, x)))
        {
            return string.Empty;
        }

        return "Clerk azp claim izin verilen origin listesinde degil.";
    }

    private static string ValidateOrganizationStatus(
        System.Security.Claims.ClaimsPrincipal? principal,
        bool rejectPending)
    {
        if (!rejectPending)
            return string.Empty;

        var status = principal?.FindFirst("sts")?.Value;
        if (!string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return "Clerk organizasyon durumu pending.";
    }

    private static string NormalizeAuthority(string authority)
    {
        return string.IsNullOrWhiteSpace(authority)
            ? string.Empty
            : authority.Trim().TrimEnd('/');
    }

    private static string NormalizeOrigin(string origin)
    {
        var trimmed = origin.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed.ToLowerInvariant();

        var builder = new UriBuilder(uri.Scheme.ToLowerInvariant(), uri.Host.ToLowerInvariant());
        if (!uri.IsDefaultPort)
            builder.Port = uri.Port;

        return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static bool OriginMatches(string azp, string allowed)
    {
        var normalizedAzp = NormalizeOrigin(azp);
        var normalizedAllowed = NormalizeOrigin(allowed);
        if (string.IsNullOrWhiteSpace(normalizedAzp) || string.IsNullOrWhiteSpace(normalizedAllowed))
            return false;

        if (string.Equals(normalizedAzp, normalizedAllowed, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!Uri.TryCreate(normalizedAzp, UriKind.Absolute, out var azpUri))
            return false;

        if (!Uri.TryCreate(normalizedAllowed, UriKind.Absolute, out var allowedUri))
            return string.Equals(azpUri.Host, normalizedAllowed, StringComparison.OrdinalIgnoreCase);

        return IsWebScheme(azpUri.Scheme) &&
            IsWebScheme(allowedUri.Scheme) &&
            string.Equals(azpUri.Host, allowedUri.Host, StringComparison.OrdinalIgnoreCase) &&
            EffectivePort(azpUri) == EffectivePort(allowedUri);
    }

    private static bool IsWebScheme(string scheme)
    {
        return string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase);
    }

    private static int EffectivePort(Uri uri)
    {
        if (!uri.IsDefaultPort)
            return uri.Port;

        return string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
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

    private static string CombineCsv(params string?[] values)
    {
        return string.Join(
            ",",
            values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => x!.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
