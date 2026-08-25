using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;

namespace Systemcel.Api.Services;

public static class DeveloperApiRequestContext
{
    internal const string ItemKey = "Systemcel.DeveloperApiIdentity";
    internal const string FailureItemKey = "Systemcel.DeveloperApiAuthenticationFailure";

    public static DeveloperApiIdentity? TryGet(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) ? value as DeveloperApiIdentity : null;

    public static DeveloperApiIdentity GetRequired(HttpContext context) =>
        TryGet(context) ?? throw new InvalidOperationException("Developer API identity is missing.");
}

public sealed class DeveloperApiAuthenticationMiddleware
{
    public const string HeaderName = "X-Systemcel-Api-Key";
    private readonly RequestDelegate _next;

    public DeveloperApiAuthenticationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        DeveloperApiKeyService keyService,
        IEntitlementGuard entitlementGuard)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1") || HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.Pragma = "no-cache";

        if (!context.Request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1)
        {
            context.Items[DeveloperApiRequestContext.FailureItemKey] = new DeveloperApiAuthenticationFailure(
                StatusCodes.Status401Unauthorized, "Geçerli bir API anahtarı gereklidir.");
            await _next(context);
            return;
        }

        var identity = await keyService.AuthenticateAsync(values[0], ct: context.RequestAborted);
        if (identity is null)
        {
            context.Items[DeveloperApiRequestContext.FailureItemKey] = new DeveloperApiAuthenticationFailure(
                StatusCodes.Status401Unauthorized, "Geçerli bir API anahtarı gereklidir.");
            await _next(context);
            return;
        }

        try
        {
            var entitlement = await entitlementGuard.GetAsync(identity.BusinessId, HesapTipleri.Isletme, context.RequestAborted);
            entitlementGuard.EnsureFeature(entitlement, EntitlementFeatures.ApiAccess);
        }
        catch (EntitlementViolationException)
        {
            context.Items[DeveloperApiRequestContext.FailureItemKey] = new DeveloperApiAuthenticationFailure(
                StatusCodes.Status403Forbidden, "Bu işletme için geliştirici API erişimi kullanılamıyor.");
            await _next(context);
            return;
        }

        context.Items[DeveloperApiRequestContext.ItemKey] = identity;
        await _next(context);
    }
}

internal sealed record DeveloperApiAuthenticationFailure(int Status, string Detail);

public sealed class DeveloperApiAuthenticationEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public DeveloperApiAuthenticationEnforcementMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1") ||
            HttpMethods.IsOptions(context.Request.Method) ||
            DeveloperApiRequestContext.TryGet(context) is not null)
        {
            await _next(context);
            return;
        }

        var failure = context.Items.TryGetValue(DeveloperApiRequestContext.FailureItemKey, out var value)
            ? value as DeveloperApiAuthenticationFailure
            : null;
        await RejectAsync(
            context,
            failure?.Status ?? StatusCodes.Status401Unauthorized,
            failure?.Detail ?? "Geçerli bir API anahtarı gereklidir.");
    }

    private static async Task RejectAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        if (status == StatusCodes.Status401Unauthorized)
            context.Response.Headers.WWWAuthenticate = "ApiKey";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://systemcel.app/problems/developer-api-authentication",
            title = status == 401 ? "API anahtarı doğrulanamadı" : "API erişimi kullanılamıyor",
            status,
            detail,
            traceId = context.TraceIdentifier
        }, context.RequestAborted);
    }
}

internal static class DeveloperApiRateLimit
{
    public const int PermitLimit = 60;
    public static string GetPartitionKey(HttpContext context)
    {
        var identity = DeveloperApiRequestContext.TryGet(context);
        return identity is null
            ? $"developer-api:unauthenticated:{context.Connection.RemoteIpAddress}"
            : $"developer-api:key:{identity.KeyId}";
    }
}
