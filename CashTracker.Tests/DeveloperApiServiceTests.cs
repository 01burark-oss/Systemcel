using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CashTracker.Tests;

public sealed class DeveloperApiServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_devapi_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly DeveloperApiKeyService _keys;
    private readonly int _tenantA;
    private readonly int _tenantB;

    public DeveloperApiServiceTests()
    {
        _factory = new Factory(_dbPath);
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        var businesses = new[]
        {
            new Isletme { Ad = "A", TenantTipi = "Isletme", IsAktif = true },
            new Isletme { Ad = "B", TenantTipi = "Isletme" }
        };
        db.Isletmeler.AddRange(businesses);
        db.SaveChanges();
        _tenantA = businesses[0].Id;
        _tenantB = businesses[1].Id;
        _keys = new DeveloperApiKeyService(_factory);
    }

    [Fact]
    public async Task Create_ReturnsPlaintextOnceButPersistsOnlyHashAndPrefix()
    {
        var created = await _keys.CreateAsync(
            _tenantA,
            "user_a",
            new DeveloperApiKeyCreateRequest("ERP bağlantısı", new[] { DeveloperApiScopes.ReadAll }, 90));

        Assert.StartsWith("sys_live_", created.ApiKey);
        await using var db = _factory.CreateDbContext();
        var row = await db.GelistiriciApiAnahtarlari.SingleAsync();
        Assert.DoesNotContain(created.ApiKey, row.Prefix, StringComparison.Ordinal);
        Assert.DoesNotContain(created.ApiKey, Convert.ToHexString(row.AnahtarHash), StringComparison.OrdinalIgnoreCase);

        var listed = await _keys.ListAsync(_tenantA);
        Assert.Equal(created.Prefix, Assert.Single(listed).Prefix);
        Assert.DoesNotContain(created.ApiKey, System.Text.Json.JsonSerializer.Serialize(listed), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authenticate_IsTenantScopedAndRejectsWrongSecretRevokedAndExpiredKeys()
    {
        var created = await _keys.CreateAsync(
            _tenantA,
            "user_a",
            new DeveloperApiKeyCreateRequest("Rapor", new[] { DeveloperApiScopes.InvoicesRead }, 90));

        var valid = await _keys.AuthenticateAsync(created.ApiKey);
        Assert.NotNull(valid);
        Assert.Equal(_tenantA, valid!.BusinessId);
        Assert.True(valid.HasScope(DeveloperApiScopes.InvoicesRead));
        Assert.False(valid.HasScope(DeveloperApiScopes.ProductsRead));

        var altered = created.ApiKey[..^1] + (created.ApiKey[^1] == 'A' ? "B" : "A");
        Assert.Null(await _keys.AuthenticateAsync(altered));
        Assert.False(await _keys.RevokeAsync(_tenantB, created.Id, "user_b"));
        Assert.True(await _keys.RevokeAsync(_tenantA, created.Id, "user_a"));
        Assert.Null(await _keys.AuthenticateAsync(created.ApiKey));

        var expired = await _keys.CreateAsync(
            _tenantA,
            "user_a",
            new DeveloperApiKeyCreateRequest("Eski", new[] { DeveloperApiScopes.ReadAll }, 30),
            new DateTime(2026, 1, 1));
        Assert.Null(await _keys.AuthenticateAsync(expired.ApiKey, new DateTime(2026, 2, 1)));
    }

    [Fact]
    public async Task ReadService_AlwaysFiltersTenantAndBoundsPagination()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.CariKartlari.AddRange(
                new CariKart { IsletmeId = _tenantA, Unvan = "A cari" },
                new CariKart { IsletmeId = _tenantB, Unvan = "B cari" });
            await db.SaveChangesAsync();
        }

        var service = new DeveloperApiReadService(_factory);
        var page = await service.GetAccountsAsync(_tenantA, 1, 100);

        Assert.Equal("A cari", Assert.Single(page.Items).Name);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetAccountsAsync(_tenantA, 1, 101));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetAccountsAsync(_tenantA, 10_001, 10));
    }

    [Fact]
    public async Task OwnerLookup_IsTenantScopedAndWriteScopesAreRejected()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var user = new Kullanici { AuthProvider = "clerk", AuthProviderUserId = "owner-a", Eposta = "a@example.test", AdSoyad = "A" };
            db.Kullanicilar.Add(user);
            await db.SaveChangesAsync();
            db.IsletmeUyelikleri.Add(new IsletmeUyelik
            {
                IsletmeId = _tenantA, KullaniciId = user.Id, Rol = "isletme_sahibi", Durum = "Aktif", DavetEposta = user.Eposta
            });
            await db.SaveChangesAsync();
        }
        Assert.True(await _keys.IsOwnerAsync(_tenantA, "owner-a"));
        Assert.False(await _keys.IsOwnerAsync(_tenantB, "owner-a"));
        await Assert.ThrowsAsync<ArgumentException>(() => _keys.CreateAsync(
            _tenantA, "owner-a", new DeveloperApiKeyCreateRequest("Yazma", new[] { "invoices:write" }, 90)));
    }

    [Fact]
    public void PostgreSqlSnapshot_DeveloperApiMatchesCurrentModel()
    {
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseNpgsql("Host=localhost;Database=systemcel_migration_metadata;Username=test;Password=test")
            .Options;
        using var db = new CashTrackerDbContext(options);
        var assembly = db.GetService<IMigrationsAssembly>();
        var initializer = db.GetService<IModelRuntimeInitializer>();
        var differ = db.GetService<IMigrationsModelDiffer>();
        var snapshot = initializer.Initialize(assembly.ModelSnapshot!.Model, designTime: true);
        var current = db.GetService<IDesignTimeModel>().Model;
        var differences = differ.GetDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel())
            .Where(x => x switch
            {
                CreateTableOperation table => table.Name == "GelistiriciApiAnahtari",
                DropTableOperation table => table.Name == "GelistiriciApiAnahtari",
                AlterColumnOperation column => column.Table == "GelistiriciApiAnahtari",
                CreateIndexOperation index => index.Table == "GelistiriciApiAnahtari",
                DropIndexOperation index => index.Table == "GelistiriciApiAnahtari",
                AddForeignKeyOperation foreignKey => foreignKey.Table == "GelistiriciApiAnahtari",
                DropForeignKeyOperation foreignKey => foreignKey.Table == "GelistiriciApiAnahtari",
                _ => false
            }).ToList();
        Assert.Empty(differences);
    }

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    private sealed class Factory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        public Factory(string path) => _options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
        public CashTrackerDbContext CreateDbContext() => new(_options);
        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
