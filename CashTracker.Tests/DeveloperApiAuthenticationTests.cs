using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Systemcel.Api.Services;
using Xunit;

namespace CashTracker.Tests;

public sealed class DeveloperApiAuthenticationTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_devapi_auth_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly DeveloperApiKeyService _keys;
    private readonly int _tenant;

    public DeveloperApiAuthenticationTests()
    {
        _factory = new Factory(_dbPath);
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        var business = new Isletme { Ad = "A", TenantTipi = HesapTipleri.Isletme, IsAktif = true };
        db.Isletmeler.Add(business);
        db.SaveChanges();
        _tenant = business.Id;
        _keys = new DeveloperApiKeyService(_factory);
    }

    [Fact]
    public async Task Middleware_UsesDedicatedHeaderAndDoesNotAcceptMissingOrAlteredKey()
    {
        var created = await _keys.CreateAsync(_tenant, "user", new DeveloperApiKeyCreateRequest("ERP", new[] { DeveloperApiScopes.SummaryRead }, 90));
        var nextCalled = false;
        var enforcement = new DeveloperApiAuthenticationEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var middleware = new DeveloperApiAuthenticationMiddleware(context => enforcement.InvokeAsync(context));

        var missing = CreateContext();
        await middleware.InvokeAsync(missing, _keys, new EnabledGuard());
        Assert.Equal(StatusCodes.Status401Unauthorized, missing.Response.StatusCode);
        Assert.False(nextCalled);

        var altered = CreateContext();
        altered.Request.Headers[DeveloperApiAuthenticationMiddleware.HeaderName] = created.ApiKey[..^1] + "x";
        await middleware.InvokeAsync(altered, _keys, new EnabledGuard());
        Assert.Equal(StatusCodes.Status401Unauthorized, altered.Response.StatusCode);
        Assert.False(nextCalled);

        var valid = CreateContext();
        valid.Request.Headers[DeveloperApiAuthenticationMiddleware.HeaderName] = created.ApiKey;
        await middleware.InvokeAsync(valid, _keys, new EnabledGuard());
        Assert.True(nextCalled);
        Assert.Equal(_tenant, DeveloperApiRequestContext.GetRequired(valid).BusinessId);
        Assert.Equal("private, no-store", valid.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task Middleware_DeniesValidKeyWhenTenantEntitlementIsDisabled()
    {
        var created = await _keys.CreateAsync(_tenant, "user", new DeveloperApiKeyCreateRequest("ERP", new[] { DeveloperApiScopes.ReadAll }, 90));
        var context = CreateContext();
        context.Request.Headers[DeveloperApiAuthenticationMiddleware.HeaderName] = created.ApiKey;
        var enforcement = new DeveloperApiAuthenticationEnforcementMiddleware(_ => throw new Xunit.Sdk.XunitException("Pipeline devam etmemeliydi."));
        var middleware = new DeveloperApiAuthenticationMiddleware(context => enforcement.InvokeAsync(context));

        await middleware.InvokeAsync(context, _keys, new DisabledGuard());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public void RateLimiter_PartitionsByAuthenticatedKeyAndRejectsAfterSixtyRequests()
    {
        var first = CreateContext();
        var second = CreateContext();
        first.Items[DeveloperApiRequestContext.ItemKey] = new DeveloperApiIdentity(7, _tenant, "prefix-a", new HashSet<string>());
        second.Items[DeveloperApiRequestContext.ItemKey] = new DeveloperApiIdentity(8, _tenant, "prefix-b", new HashSet<string>());
        Assert.NotEqual(DeveloperApiRateLimit.GetPartitionKey(first), DeveloperApiRateLimit.GetPartitionKey(second));

        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = DeveloperApiRateLimit.PermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = false
        });
        for (var i = 0; i < DeveloperApiRateLimit.PermitLimit; i++)
            Assert.True(limiter.AttemptAcquire().IsAcquired);
        Assert.False(limiter.AttemptAcquire().IsAcquired);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/business";
        context.Response.Body = new MemoryStream();
        return context;
    }

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    private class EnabledGuard : IEntitlementGuard
    {
        public virtual Task<SubscriptionEntitlementStatus> GetAsync(int businessId, string accountType, CancellationToken ct = default) =>
            Task.FromResult(new SubscriptionEntitlementStatus { IsletmeId = businessId, PlanAdi = "Büyüme", ApiErisimiAktif = true });
        public void EnsureFeature(SubscriptionEntitlementStatus entitlement, string featureName)
        {
            if (!entitlement.ApiErisimiAktif) throw new EntitlementViolationException(EntitlementErrorCodes.FeatureNotAvailable, "Kapalı");
        }
        public void EnsureLimit(SubscriptionEntitlementStatus entitlement, string limitName, int currentCount, int requestedCount = 1) { }
        public void EnsureWritable(SubscriptionEntitlementStatus entitlement) { }
    }

    private sealed class DisabledGuard : EnabledGuard
    {
        public override Task<SubscriptionEntitlementStatus> GetAsync(int businessId, string accountType, CancellationToken ct = default) =>
            Task.FromResult(new SubscriptionEntitlementStatus { IsletmeId = businessId, PlanAdi = "Başlangıç", ApiErisimiAktif = false });
    }

    private sealed class Factory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        public Factory(string path) => _options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
        public CashTrackerDbContext CreateDbContext() => new(_options);
        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
