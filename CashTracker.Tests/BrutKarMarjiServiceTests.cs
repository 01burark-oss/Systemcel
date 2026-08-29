using System;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
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

    private sealed class MarginFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SingleDbContextFactory _factory;
        public BrutKarMarjiService Service { get; }
        public int ProductId { get; }

        private MarginFixture(SqliteConnection connection, SingleDbContextFactory factory, int productId)
        {
            _connection = connection;
            _factory = factory;
            ProductId = productId;
            Service = new BrutKarMarjiService(factory, new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Test" } });
        }

        public static async Task<MarginFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
            var factory = new SingleDbContextFactory(options);
            await using var db = factory.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            var product = new UrunHizmet { IsletmeId = 1, Tip = "Urun", Ad = "Kablo", Aktif = true };
            db.UrunHizmetleri.Add(product);
            await db.SaveChangesAsync();
            return new MarginFixture(connection, factory, product.Id);
        }

        public async Task AddInvoiceAsync(string type, DateTime date, decimal rate, decimal quantity, decimal netAmount)
        {
            await using var db = _factory.CreateDbContext();
            var invoice = new Fatura { IsletmeId = 1, CariKartId = 1, Tarih = date, FaturaTipi = type, Durum = FaturaDurum.Kesildi, KurSnapshot = rate, ParaBirimi = "USD" };
            db.Faturalar.Add(invoice);
            await db.SaveChangesAsync();
            db.FaturaSatirlari.Add(new FaturaSatir { IsletmeId = 1, FaturaId = invoice.Id, UrunHizmetId = ProductId, Miktar = quantity, SatirNetTutar = netAmount, StokEtkilesin = true });
            await db.SaveChangesAsync();
        }

        public async Task AddManualMovementAsync(DateTime date, decimal quantity, decimal costTry)
        {
            await using var db = _factory.CreateDbContext();
            db.StokHareketleri.Add(new StokHareket { IsletmeId = 1, UrunHizmetId = ProductId, Tarih = date, Miktar = quantity, BirimMaliyetTry = costTry, Kaynak = "Manuel", HareketTipi = "Giris" });
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
}
