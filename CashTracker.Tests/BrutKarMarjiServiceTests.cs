using System;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class BrutKarMarjiServiceTests
{
    [Fact]
    public async Task GetAsync_UsesVatExcludedDiscountedSalesAndMovingAveragePurchaseCost()
    {
        await using var fixture = await MarginFixture.CreateAsync();
        await fixture.AddInvoiceAsync("Alis", new DateTime(2026, 8, 1), 2m, 10m, 1_000m);
        await fixture.AddInvoiceAsync("Satis", new DateTime(2026, 8, 2), 2m, 6m, 1_800m);

        var result = await fixture.Service.GetAsync(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        Assert.True(result.Guvenilir);
        Assert.Equal("Hazir", result.Durum);
        Assert.Equal(3_600m, result.SatisGeliriTry);
        Assert.Equal(1_200m, result.SatisMaliyetiTry);
        Assert.Equal(2_400m, result.BrutKarTry);
        Assert.Equal(66.7m, result.BrutKarOrani);
    }

    [Fact]
    public async Task GetAsync_DoesNotInventMarginWhenHistoricalManualEntryHasNoCost()
    {
        await using var fixture = await MarginFixture.CreateAsync();
        await fixture.AddManualMovementAsync(new DateTime(2026, 8, 1), 10m, 0m);
        await fixture.AddInvoiceAsync("Satis", new DateTime(2026, 8, 2), 1m, 2m, 400m);

        var result = await fixture.Service.GetAsync(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        Assert.False(result.Guvenilir);
        Assert.Equal("EksikMaliyet", result.Durum);
        Assert.Equal(400m, result.SatisGeliriTry);
        Assert.Equal(1, result.EksikMaliyetliSatisSatiri);
        Assert.Equal(0m, result.BrutKarTry);
    }

    [Fact]
    public async Task GetAsync_UsesCostFromBeforeRequestedPeriodWithoutCountingItsPurchaseAsRevenue()
    {
        await using var fixture = await MarginFixture.CreateAsync();
        await fixture.AddInvoiceAsync("Alis", new DateTime(2026, 7, 28), 1m, 10m, 1_000m);
        await fixture.AddInvoiceAsync("Satis", new DateTime(2026, 8, 3), 1m, 4m, 800m);

        var result = await fixture.Service.GetAsync(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        Assert.True(result.Guvenilir);
        Assert.Equal(800m, result.SatisGeliriTry);
        Assert.Equal(400m, result.SatisMaliyetiTry);
        Assert.Equal(400m, result.BrutKarTry);
    }

    [Fact]
    public async Task GetAsync_ExcludesOtherBusinessesInvoicesAndCosts()
    {
        await using var fixture = await MarginFixture.CreateAsync();
        await fixture.AddInvoiceAsync("Alis", new DateTime(2026, 8, 1), 1m, 10m, 1_000m);
        await fixture.AddInvoiceAsync("Satis", new DateTime(2026, 8, 2), 1m, 2m, 600m);
        await fixture.AddOtherBusinessInvoicesAsync();

        var result = await fixture.Service.GetAsync(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        Assert.True(result.Guvenilir);
        Assert.Equal(600m, result.SatisGeliriTry);
        Assert.Equal(200m, result.SatisMaliyetiTry);
        Assert.Equal(400m, result.BrutKarTry);
        Assert.Equal(1, result.SatisSatiri);
    }

    [Fact]
    public async Task GetAsync_DoesNotCountMarketplaceReceiptStockMovementTwice()
    {
        await using var fixture = await MarginFixture.CreateAsync();
        await fixture.AddInvoiceAsync("Alis", new DateTime(2026, 8, 1), 1m, 10m, 1_000m);
        await fixture.AddManualMovementAsync(new DateTime(2026, 8, 1), 10m, 100m, "PazaryeriMalKabul");
        await fixture.AddInvoiceAsync("Alis", new DateTime(2026, 8, 2), 1m, 10m, 2_000m);
        await fixture.AddInvoiceAsync("Satis", new DateTime(2026, 8, 3), 1m, 10m, 4_000m);

        var result = await fixture.Service.GetAsync(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        Assert.Equal("Hazir", result.Durum);
        Assert.Equal(1_500m, result.SatisMaliyetiTry);
        Assert.Equal(2_500m, result.BrutKarTry);
    }

    [Fact]
    public async Task GetAsync_CarriesMovingAverageCostAcrossBranchWarehouseTransfer()
    {
        await using var fixture = await MarginFixture.CreateAsync();
        var (sourceBranchId, destinationBranchId, sourceWarehouseId, destinationWarehouseId) = await fixture.AddBranchesAndWarehousesAsync();
        fixture.SelectBranch(destinationBranchId);
        await fixture.AddInvoiceAsync("Alis", new DateTime(2026, 8, 1), 1m, 10m, 1_000m, sourceBranchId);
        await fixture.AddTransferAsync(new DateTime(2026, 8, 2), sourceBranchId, destinationBranchId, sourceWarehouseId, destinationWarehouseId, 5m);
        await fixture.AddInvoiceAsync("Satis", new DateTime(2026, 8, 3), 1m, 5m, 1_000m, destinationBranchId);

        var result = await fixture.Service.GetAsync(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        Assert.Equal("Hazir", result.Durum);
        Assert.Equal(1_000m, result.SatisGeliriTry);
        Assert.Equal(500m, result.SatisMaliyetiTry);
        Assert.Equal(500m, result.BrutKarTry);
        Assert.Equal(1, result.SatisSatiri);
    }

    private sealed class MarginFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SingleDbContextFactory _factory;
        public BrutKarMarjiService Service { get; private set; }
        public int ProductId { get; }

        private MarginFixture(SqliteConnection connection, SingleDbContextFactory factory, int productId, ISubeKurService? branchService)
        {
            _connection = connection;
            _factory = factory;
            ProductId = productId;
            Service = new BrutKarMarjiService(factory, new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Test" } }, branchService);
        }

        public static async Task<MarginFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
            var factory = new SingleDbContextFactory(options);
            await using var db = factory.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.Add(new Isletme { Id = 1, Ad = "Test" });
            var product = new UrunHizmet { IsletmeId = 1, Tip = "Urun", Ad = "Kablo", Aktif = true };
            db.UrunHizmetleri.Add(product);
            await db.SaveChangesAsync();
            return new MarginFixture(connection, factory, product.Id, null);
        }

        public void SelectBranch(int branchId) => Service = new BrutKarMarjiService(
            _factory,
            new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Test" } },
            new FixedBranchService(new SubeDto { Id = branchId, Ad = "Seçili Şube", Varsayilan = false, Aktif = true }));

        public async Task AddInvoiceAsync(string type, DateTime date, decimal rate, decimal quantity, decimal netAmount, int? branchId = null)
        {
            await using var db = _factory.CreateDbContext();
            var invoice = new Fatura { IsletmeId = 1, SubeId = branchId, CariKartId = 1, Tarih = date, FaturaTipi = type, Durum = FaturaDurum.Kesildi, KurSnapshot = rate, ParaBirimi = "USD" };
            db.Faturalar.Add(invoice);
            await db.SaveChangesAsync();
            db.FaturaSatirlari.Add(new FaturaSatir { IsletmeId = 1, FaturaId = invoice.Id, UrunHizmetId = ProductId, Miktar = quantity, SatirNetTutar = netAmount, StokEtkilesin = true });
            await db.SaveChangesAsync();
        }

        public async Task<(int SourceBranchId, int DestinationBranchId, int SourceWarehouseId, int DestinationWarehouseId)> AddBranchesAndWarehousesAsync()
        {
            await using var db = _factory.CreateDbContext();
            var sourceBranch = new Sube { IsletmeId = 1, Ad = "Şube A", Kod = "A", OlusturmaAnahtari = "margin-test-branch-a", IcerikOzeti = new string('A', 64), Aktif = true };
            var destinationBranch = new Sube { IsletmeId = 1, Ad = "Şube B", Kod = "B", OlusturmaAnahtari = "margin-test-branch-b", IcerikOzeti = new string('B', 64), Aktif = true };
            db.Subeler.AddRange(sourceBranch, destinationBranch);
            await db.SaveChangesAsync();
            var sourceWarehouse = new StokDepo { IsletmeId = 1, SubeId = sourceBranch.Id, Ad = "Depo A", Kod = "DEPO-A", Aktif = true };
            var destinationWarehouse = new StokDepo { IsletmeId = 1, SubeId = destinationBranch.Id, Ad = "Depo B", Kod = "DEPO-B", Aktif = true };
            db.StokDepolari.AddRange(sourceWarehouse, destinationWarehouse);
            await db.SaveChangesAsync();
            return (sourceBranch.Id, destinationBranch.Id, sourceWarehouse.Id, destinationWarehouse.Id);
        }

        public async Task AddTransferAsync(DateTime date, int sourceBranchId, int destinationBranchId, int sourceWarehouseId, int destinationWarehouseId, decimal quantity)
        {
            await using var db = _factory.CreateDbContext();
            var operation = new StokDefterIslemi
            {
                IsletmeId = 1,
                IslemAnahtari = "branch-transfer-1",
                IcerikOzeti = new string('A', 64),
                IslemTipi = "Transfer",
                CreatedAt = date
            };
            db.StokDefterIslemleri.Add(operation);
            await db.SaveChangesAsync();
            db.StokHareketleri.AddRange(
                new StokHareket { IsletmeId = 1, SubeId = sourceBranchId, UrunHizmetId = ProductId, DepoId = sourceWarehouseId, StokDefterIslemiId = operation.Id, Tarih = date, Miktar = -quantity, HareketTipi = "TransferCikis", Kaynak = "GelismisStok" },
                new StokHareket { IsletmeId = 1, SubeId = destinationBranchId, UrunHizmetId = ProductId, DepoId = destinationWarehouseId, StokDefterIslemiId = operation.Id, Tarih = date, Miktar = quantity, HareketTipi = "TransferGiris", Kaynak = "GelismisStok" });
            await db.SaveChangesAsync();
        }

        public async Task AddManualMovementAsync(DateTime date, decimal quantity, decimal costTry, string source = "Manuel")
        {
            await using var db = _factory.CreateDbContext();
            db.StokHareketleri.Add(new StokHareket { IsletmeId = 1, UrunHizmetId = ProductId, Tarih = date, Miktar = quantity, BirimMaliyetTry = costTry, Kaynak = source, HareketTipi = "Giris" });
            await db.SaveChangesAsync();
        }

        public async Task AddOtherBusinessInvoicesAsync()
        {
            await using var db = _factory.CreateDbContext();
            var product = new UrunHizmet { IsletmeId = 2, Tip = "Urun", Ad = "Yabancı ürün", Aktif = true };
            db.UrunHizmetleri.Add(product);
            await db.SaveChangesAsync();

            var purchase = new Fatura { IsletmeId = 2, CariKartId = 2, Tarih = new DateTime(2026, 8, 1), FaturaTipi = "Alis", Durum = FaturaDurum.Kesildi, KurSnapshot = 1m };
            var sale = new Fatura { IsletmeId = 2, CariKartId = 2, Tarih = new DateTime(2026, 8, 2), FaturaTipi = "Satis", Durum = FaturaDurum.Kesildi, KurSnapshot = 1m };
            db.Faturalar.AddRange(purchase, sale);
            await db.SaveChangesAsync();

            db.FaturaSatirlari.AddRange(
                new FaturaSatir { IsletmeId = 2, FaturaId = purchase.Id, UrunHizmetId = product.Id, Miktar = 10m, SatirNetTutar = 10m, StokEtkilesin = true },
                new FaturaSatir { IsletmeId = 2, FaturaId = sale.Id, UrunHizmetId = product.Id, Miktar = 10m, SatirNetTutar = 10_000m, StokEtkilesin = true });
            await db.SaveChangesAsync();
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private sealed class FixedBranchService(SubeDto activeBranch) : ISubeKurService
    {
        public Task<SubeKurDurumuDto> GetContextAsync(CancellationToken ct = default) => Task.FromResult(new SubeKurDurumuDto { AktifSube = activeBranch, Subeler = [activeBranch], CokluSubeAktif = true });
        public Task<SubeFinansOzetiDto> GetFinancialSummaryAsync(int? branchId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SubeOlusturResult> CreateBranchAsync(SubeOlusturRequest request, string idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetActiveBranchAsync(int branchId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KurKaydetResult> SaveRateAsync(DovizKuruKaydetRequest request, string idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IslemKurSnapshot> ResolveSnapshotAsync(string? currency, decimal originalAmount, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
