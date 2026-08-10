using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class HizliSatisServiceTests
    {
        [Fact]
        public async Task CreateAsync_RecordsPaidSaleIncomeAndStockOut()
        {
            using var fixture = await QuickSaleFixture.CreateAsync();
            var result = await fixture.Service.CreateAsync(new HizliSatisCreateRequest
            {
                IslemAnahtari = "sale-1",
                OdemeYontemi = "KrediKarti",
                Satirlar =
                [
                    new HizliSatisSatirRequest { UrunHizmetId = fixture.ProductId, Miktar = 2 }
                ]
            });

            await using var db = fixture.CreateDbContext();
            var invoice = await db.Faturalar.SingleAsync();
            var cash = await db.Kasalar.SingleAsync();
            var stock = (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar);

            Assert.Equal(invoice.Id, result.FaturaId);
            Assert.Equal(FaturaDurum.Odendi, invoice.Durum);
            Assert.Equal(240m, invoice.GenelToplam);
            Assert.Equal(240m, invoice.OdenenTutar);
            Assert.Equal("Gelir", cash.Tip);
            Assert.Equal("KrediKarti", cash.OdemeYontemi);
            Assert.Equal(240m, cash.Tutar);
            Assert.Equal(8m, stock);
            Assert.Equal(2, await db.CariHareketleri.CountAsync());
            Assert.Single(await db.TahsilatOdemeleri.ToListAsync());
        }

        [Fact]
        public async Task CreateAsync_WithSameKey_DoesNotDuplicateSale()
        {
            using var fixture = await QuickSaleFixture.CreateAsync();
            var request = new HizliSatisCreateRequest
            {
                IslemAnahtari = "same-sale",
                Satirlar =
                [
                    new HizliSatisSatirRequest { UrunHizmetId = fixture.ProductId, Miktar = 1 }
                ]
            };

            var first = await fixture.Service.CreateAsync(request);
            var second = await fixture.Service.CreateAsync(request);

            await using var db = fixture.CreateDbContext();
            Assert.False(first.Tekrarlandi);
            Assert.True(second.Tekrarlandi);
            Assert.Equal(first.FaturaId, second.FaturaId);
            Assert.Equal(1, await db.Faturalar.CountAsync());
            Assert.Equal(1, await db.Kasalar.CountAsync());
            Assert.Equal(9m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar));
        }

        [Fact]
        public async Task CreateAsync_WithInsufficientStock_RollsBackEverything()
        {
            using var fixture = await QuickSaleFixture.CreateAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAsync(new HizliSatisCreateRequest
            {
                IslemAnahtari = "too-much",
                Satirlar =
                [
                    new HizliSatisSatirRequest { UrunHizmetId = fixture.ProductId, Miktar = 11 }
                ]
            }));

            await using var db = fixture.CreateDbContext();
            Assert.False(await db.Faturalar.AnyAsync());
            Assert.False(await db.Kasalar.AnyAsync());
            Assert.Equal(10m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar));
        }

        [Fact]
        public async Task CreateAsync_WhenInvoiceLimitReached_RollsBackEverything()
        {
            using var fixture = await QuickSaleFixture.CreateAsync(new SubscriptionEntitlementStatus
            {
                HesapTipi = HesapTipleri.Isletme,
                PlanKodu = PlanKodlari.IsletmeUcretsiz,
                PlanAdi = "Ücretsiz",
                Kaynak = EntitlementKaynaklari.Ucretsiz,
                FaturaLimiti = 0,
                CariKartLimiti = 20
            });

            var error = await Assert.ThrowsAsync<EntitlementViolationException>(() =>
                fixture.Service.CreateAsync(new HizliSatisCreateRequest
                {
                    IslemAnahtari = "free-plan-sale",
                    Satirlar =
                    [
                        new HizliSatisSatirRequest { UrunHizmetId = fixture.ProductId, Miktar = 1 }
                    ]
                }));

            Assert.Equal(EntitlementLimits.Invoice, error.LimitName);
            await using var db = fixture.CreateDbContext();
            Assert.False(await db.Faturalar.AnyAsync());
            Assert.False(await db.Kasalar.AnyAsync());
            Assert.False(await db.CariKartlari.AnyAsync());
            Assert.Equal(10m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar));
        }

        [Fact]
        public async Task CreateAsync_WhenDefaultCustomerWouldExceedLimit_RollsBackEverything()
        {
            using var fixture = await QuickSaleFixture.CreateAsync(new SubscriptionEntitlementStatus
            {
                HesapTipi = HesapTipleri.Isletme,
                PlanKodu = PlanKodlari.IsletmeUcretsiz,
                PlanAdi = "Ücretsiz",
                Kaynak = EntitlementKaynaklari.Ucretsiz,
                FaturaLimiti = null,
                CariKartLimiti = 0
            });

            var error = await Assert.ThrowsAsync<EntitlementViolationException>(() =>
                fixture.Service.CreateAsync(new HizliSatisCreateRequest
                {
                    IslemAnahtari = "no-customer-capacity",
                    Satirlar =
                    [
                        new HizliSatisSatirRequest { UrunHizmetId = fixture.ProductId, Miktar = 1 }
                    ]
                }));

            Assert.Equal(EntitlementLimits.CurrentAccount, error.LimitName);
            await using var db = fixture.CreateDbContext();
            Assert.False(await db.Faturalar.AnyAsync());
            Assert.False(await db.CariKartlari.AnyAsync());
            Assert.Equal(10m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar));
        }

        private sealed class QuickSaleFixture : IDisposable
        {
            private QuickSaleFixture(
                string dbPath,
                DbContextOptions<CashTrackerDbContext> options,
                SubscriptionEntitlementStatus? entitlement)
            {
                DbPath = dbPath;
                Options = options;
                Factory = new SingleDbContextFactory(options);
                Isletme = new FakeIsletmeService
                {
                    Active = new Isletme { Id = 1, Ad = "Test", IsAktif = true }
                };
                var guard = entitlement is null
                    ? null
                    : new EntitlementGuard(new StaticEntitlementService(entitlement));
                Service = new HizliSatisService(Factory, Isletme, guard);
            }

            public string DbPath { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }
            public SingleDbContextFactory Factory { get; }
            public FakeIsletmeService Isletme { get; }
            public HizliSatisService Service { get; }
            public int ProductId { get; private set; }

            public static async Task<QuickSaleFixture> CreateAsync(
                SubscriptionEntitlementStatus? entitlement = null)
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_quick_sale_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                var fixture = new QuickSaleFixture(dbPath, options, entitlement);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.Add(new Isletme { Id = 1, Ad = "Test", IsAktif = true });
                var product = new UrunHizmet
                {
                    IsletmeId = 1,
                    Tip = "Urun",
                    Ad = "Barkodlu Urun",
                    Barkod = "8690001",
                    Birim = "Adet",
                    KdvOrani = 20,
                    SatisFiyati = 120,
                    Aktif = true
                };
                db.UrunHizmetleri.Add(product);
                await db.SaveChangesAsync();
                fixture.ProductId = product.Id;
                db.StokHareketleri.Add(new StokHareket
                {
                    IsletmeId = 1,
                    UrunHizmetId = product.Id,
                    Miktar = 10,
                    HareketTipi = "Giris",
                    Kaynak = "Test"
                });
                await db.SaveChangesAsync();
                return fixture;
            }

            public CashTrackerDbContext CreateDbContext() => new(Options);

            public void Dispose()
            {
                try
                {
                    if (File.Exists(DbPath))
                        File.Delete(DbPath);
                }
                catch
                {
                }
            }
        }

        private sealed class StaticEntitlementService : ISubscriptionEntitlementService
        {
            private readonly SubscriptionEntitlementStatus _entitlement;

            public StaticEntitlementService(SubscriptionEntitlementStatus entitlement)
            {
                _entitlement = entitlement;
            }

            public Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(
                int isletmeId,
                DateTime? now = null,
                System.Threading.CancellationToken ct = default) => Task.FromResult(_entitlement);

            public Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(
                int muhasebeciIsletmeId,
                DateTime? now = null,
                System.Threading.CancellationToken ct = default) => Task.FromResult(_entitlement);
        }
    }
}
