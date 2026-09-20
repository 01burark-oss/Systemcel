using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Systemcel.Api.Services;
using Xunit;

namespace CashTracker.Tests;

public sealed class MarketplaceDemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesFourVerifiedSuppliersOnce()
    {
        await using var fixture = await Fixture.CreateAsync();

        var first = await fixture.SeedAsync();
        var second = await fixture.SeedAsync();

        Assert.True(first.Seeded);
        Assert.Equal(4, first.SupplierCount);
        Assert.Equal(12, first.ProductCount);
        Assert.False(second.Seeded);
        await using var db = fixture.CreateDb();
        Assert.Equal(4, await db.TedarikciProfilleri.CountAsync(x => x.Dogrulandi && x.Yayinda));
        Assert.Equal(12, await db.TedarikciUrunleri.CountAsync(x => x.Aktif));
        Assert.Equal(1, await db.AppSettings.CountAsync(x => x.Key == MarketplaceDemoDataSeeder.SeedMarker));
    }

    [Fact]
    public async Task SeededCatalog_CompletesOnePaymentAcrossMultipleSuppliers()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.SeedAsync();
        var buyer = await fixture.CreateBuyerAsync();
        await using var readDb = fixture.CreateDb();
        var products = await readDb.TedarikciUrunleri
            .OrderBy(x => x.TedarikciIsletmeId)
            .GroupBy(x => x.TedarikciIsletmeId)
            .Select(x => x.First())
            .Take(3)
            .ToListAsync();
        Assert.Equal(3, products.Count);

        var service = fixture.CreateMarketplaceService(buyer);
        var created = await service.CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Systemcel demo teslimat adresi",
            "demo-catalog-order",
            products.Select(x => new PazaryeriSepetKalemiRequest(x.Id, 1)).ToList()));
        var paid = await service.PayOrderAsync(created.Id, new PazaryeriOdemeRequest("demo-catalog-payment"));

        Assert.False(paid.TekrarKullanildi);
        await using var assertDb = fixture.CreateDb();
        Assert.Equal(PazaryeriSiparisDurumlari.Odendi,
            await assertDb.PazaryeriAnaSiparisleri.Where(x => x.Id == created.Id).Select(x => x.Durum).SingleAsync());
        Assert.Equal(3, await assertDb.TedarikciSiparisleri.CountAsync(x => x.AnaSiparisId == created.Id));
        Assert.Equal(3, await assertDb.PazaryeriOdemeDagitimlari.CountAsync());
        Assert.Equal(3, await assertDb.TedarikciHakEdisleri.CountAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<CashTrackerDbContext> _options;

        private Fixture(SqliteConnection connection, DbContextOptions<CashTrackerDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        internal static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
            var fixture = new Fixture(connection, options);
            await using var db = fixture.CreateDb();
            await db.Database.EnsureCreatedAsync();
            return fixture;
        }

        internal CashTrackerDbContext CreateDb() => new(_options);

        internal async Task<MarketplaceDemoSeedResult> SeedAsync()
        {
            await using var db = CreateDb();
            return await MarketplaceDemoDataSeeder.SeedAsync(db);
        }

        internal async Task<Isletme> CreateBuyerAsync()
        {
            await using var db = CreateDb();
            var buyer = new Isletme { Ad = "Demo Alıcı", IsAktif = true };
            db.Isletmeler.Add(buyer);
            await db.SaveChangesAsync();
            return buyer;
        }

        internal TedarikciPazaryeriService CreateMarketplaceService(Isletme buyer) => new(
            new SingleDbContextFactory(_options),
            new FakeIsletmeService(buyer),
            new FakeManagementService(),
            new FakeMarketplacePaymentGateway(),
            new PazaryeriOptions(),
            new AnonymousCurrentUserContext());

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private sealed class FakeIsletmeService(Isletme activeBusiness) : IIsletmeService
    {
        public Task<List<Isletme>> GetAllAsync() => Task.FromResult(new List<Isletme> { activeBusiness });
        public Task<Isletme?> GetByIdAsync(int id) => Task.FromResult<Isletme?>(id == activeBusiness.Id ? activeBusiness : null);
        public Task<Isletme> GetActiveAsync() => Task.FromResult(activeBusiness);
        public Task<int> GetActiveIdAsync() => Task.FromResult(activeBusiness.Id);
        public Task<int> CreateAsync(string ad, bool makeActive = false) => throw new NotSupportedException();
        public Task RenameAsync(int id, string ad) => throw new NotSupportedException();
        public Task UpdateSetupAsync(int id, string ad, string t, string k, bool c, string? h = null, bool? m = null, MuhasebeciProfilKaydetRequest? p = null, string? v = null, string? o = null, string? s = null) => throw new NotSupportedException();
        public Task SetActiveAsync(int id) => throw new NotSupportedException();
        public Task SetActiveCustomerContextAsync(int id) => throw new NotSupportedException();
        public Task ClearActiveCustomerContextAsync() => throw new NotSupportedException();
        public Task<ActiveBusinessAccess> GetActiveAccessAsync() => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
    }

    private sealed class FakeManagementService : ISystemcelYonetimService
    {
        public Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<MuhasebeciBasvuruListeDto> GetMuhasebeciBasvurulariAsync(string? d = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MuhasebeciBasvuruDto> ApproveMuhasebeciBasvurusuAsync(int i, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MuhasebeciBasvuruDto> RejectMuhasebeciBasvurusuAsync(int i, MuhasebeciBasvuruRedRequest r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<YonetimOdemeIncelemeDto> GetOdemeIncelemeAsync(string? d = null, bool s = false, int l = 100, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MuhasebeciAktarimListeDto> GetMuhasebeciAktarimlariAsync(string d, int? i = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MuhasebeciAktarimOzetDto> CompleteMuhasebeciAktarimiAsync(int i, MuhasebeciAktarimTamamlaRequest r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DestekTalebiListeDto> GetDestekTalepleriAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DestekTalebiDto> UpdateDestekTalebiAsync(int i, DestekTalebiGuncelleRequest r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<EntitlementOverrideResult> ApplyEntitlementOverrideAsync(int i, EntitlementOverrideRequest r, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class AnonymousCurrentUserContext : ICurrentUserContext
    {
        public CurrentUserIdentity? GetCurrentUser() => null;
    }
}
