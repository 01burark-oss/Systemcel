using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;
using Systemcel.Api.Services;

namespace Systemcel.Api.Api;

public static class DeveloperApi
{
    public static void MapDeveloperApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/gelistirici-api/anahtarlar", async (
            HttpContext http,
            ICurrentUserContext currentUser,
            IIsletmeService businessService,
            IEntitlementGuard entitlementGuard,
            DeveloperApiKeyService keyService,
            CancellationToken ct) =>
        {
            SetNoStore(http);
            var access = await ResolveManagementAccessAsync(currentUser, businessService, entitlementGuard, keyService, ct);
            if (access.Error is not null)
                return access.Error;
            var rows = await keyService.ListAsync(access.BusinessId, ct);
            return Results.Ok(new
            {
                anahtarlar = rows.Select(x => new
                {
                    id = x.Id,
                    ad = x.Name,
                    prefix = x.Prefix,
                    scopes = x.Scopes,
                    createdAt = x.CreatedAt,
                    lastUsedAt = x.LastUsedAt,
                    expiresAt = x.ExpiresAt,
                    revokedAt = x.RevokedAt
                })
            });
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/gelistirici-api/anahtarlar", async (
            DeveloperApiManagementCreateRequest request,
            HttpContext http,
            ICurrentUserContext currentUser,
            IIsletmeService businessService,
            IEntitlementGuard entitlementGuard,
            DeveloperApiKeyService keyService,
            CancellationToken ct) =>
        {
            SetNoStore(http);
            var access = await ResolveManagementAccessAsync(currentUser, businessService, entitlementGuard, keyService, ct);
            if (access.Error is not null)
                return access.Error;
            try
            {
                var created = await keyService.CreateAsync(
                    access.BusinessId,
                    access.UserRef,
                    new DeveloperApiKeyCreateRequest(request.Ad, request.Scopes, request.ExpiresInDays),
                    ct: ct);
                return Results.Ok(new
                {
                    id = created.Id,
                    ad = created.Name,
                    prefix = created.Prefix,
                    scopes = created.Scopes,
                    createdAt = created.CreatedAt,
                    lastUsedAt = (DateTime?)null,
                    expiresAt = created.ExpiresAt,
                    revokedAt = (DateTime?)null,
                    anahtar = created.ApiKey
                });
            }
            catch (ArgumentException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }).RequireRateLimiting("sensitive");

        app.MapDelete("/api/ekran/gelistirici-api/anahtarlar/{id:int}", async (
            int id,
            HttpContext http,
            ICurrentUserContext currentUser,
            IIsletmeService businessService,
            IEntitlementGuard entitlementGuard,
            DeveloperApiKeyService keyService,
            CancellationToken ct) =>
        {
            SetNoStore(http);
            var access = await ResolveManagementAccessAsync(currentUser, businessService, entitlementGuard, keyService, ct);
            if (access.Error is not null)
                return access.Error;
            return await keyService.RevokeAsync(access.BusinessId, id, access.UserRef, ct)
                ? Results.NoContent()
                : Results.NotFound();
        }).RequireRateLimiting("sensitive");

        var v1 = app.MapGroup("/api/v1")
            .RequireRateLimiting("developer-api")
            .AddEndpointFilter<DeveloperApiScopeFilter>();

        v1.MapGet("/business", async (HttpContext http, DeveloperApiReadService service, CancellationToken ct) =>
        {
            var identity = DeveloperApiRequestContext.GetRequired(http);
            var result = await service.GetSummaryAsync(identity.BusinessId, ct: ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithMetadata(new DeveloperApiScopeRequirement(DeveloperApiScopes.SummaryRead));

        v1.MapGet("/accounts", async (HttpContext http, DeveloperApiReadService service, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
            await PagedAsync(() => service.GetAccountsAsync(DeveloperApiRequestContext.GetRequired(http).BusinessId, page, pageSize, ct)))
            .WithMetadata(new DeveloperApiScopeRequirement(DeveloperApiScopes.AccountsRead));

        v1.MapGet("/products", async (HttpContext http, DeveloperApiReadService service, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
            await PagedAsync(() => service.GetProductsAsync(DeveloperApiRequestContext.GetRequired(http).BusinessId, page, pageSize, ct)))
            .WithMetadata(new DeveloperApiScopeRequirement(DeveloperApiScopes.ProductsRead));

        v1.MapGet("/invoices", async (HttpContext http, DeveloperApiReadService service, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
            await PagedAsync(() => service.GetInvoicesAsync(DeveloperApiRequestContext.GetRequired(http).BusinessId, page, pageSize, ct)))
            .WithMetadata(new DeveloperApiScopeRequirement(DeveloperApiScopes.InvoicesRead));

        v1.MapGet("/bank-transactions", async (HttpContext http, DeveloperApiReadService service, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
            await PagedAsync(() => service.GetBankTransactionsAsync(DeveloperApiRequestContext.GetRequired(http).BusinessId, page, pageSize, ct)))
            .WithMetadata(new DeveloperApiScopeRequirement(DeveloperApiScopes.BankRead));
    }

    private static async Task<IResult> PagedAsync<T>(Func<Task<DeveloperApiPage<T>>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    private static async Task<ManagementAccess> ResolveManagementAccessAsync(
        ICurrentUserContext currentUser,
        IIsletmeService businessService,
        IEntitlementGuard entitlementGuard,
        DeveloperApiKeyService keyService,
        CancellationToken ct)
    {
        var identity = currentUser.GetCurrentUser();
        if (identity is null)
            return new ManagementAccess(0, string.Empty, Results.Unauthorized());
        var business = await businessService.GetActiveAsync();
        if (!string.Equals(business.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase) ||
            !await keyService.IsOwnerAsync(business.Id, identity.ProviderUserId, ct))
        {
            return new ManagementAccess(0, identity.ProviderUserId, Results.StatusCode(StatusCodes.Status403Forbidden));
        }
        var entitlement = await entitlementGuard.GetAsync(business.Id, HesapTipleri.Isletme, ct);
        entitlementGuard.EnsureFeature(entitlement, EntitlementFeatures.ApiAccess);
        return new ManagementAccess(business.Id, identity.ProviderUserId, null);
    }

    private static IResult ValidationProblem(string detail) => Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Geçersiz istek",
        detail: detail,
        type: "https://systemcel.app/problems/invalid-developer-api-request");

    private static void SetNoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "private, no-store";
        http.Response.Headers.Pragma = "no-cache";
    }

    private sealed record ManagementAccess(int BusinessId, string UserRef, IResult? Error);
}

public sealed class DeveloperApiManagementCreateRequest
{
    public string Ad { get; init; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
    public int ExpiresInDays { get; init; } = 90;
}

internal sealed record DeveloperApiScopeRequirement(string Scope);

internal sealed class DeveloperApiScopeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var identity = DeveloperApiRequestContext.TryGet(context.HttpContext);
        if (identity is null)
            return Results.Unauthorized();
        var requirement = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<DeveloperApiScopeRequirement>();
        if (requirement is null)
            return Results.Problem(statusCode: 500, title: "API kapsamı yapılandırılmamış.");
        if (!identity.HasScope(requirement.Scope))
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Yetersiz API kapsamı",
                detail: $"Bu uç nokta '{requirement.Scope}' okuma kapsamını gerektirir.",
                type: "https://systemcel.app/problems/developer-api-scope-required");
        return await next(context);
    }
}
