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
    public sealed class GelismisStokServiceTests
    {
        [Fact]
        public async Task TransferAsync_SameKey_ReplaysWithoutDuplicateRows()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();
            var request = new StokTransferRequest
            {
                UrunHizmetId = fixture.ProductId,
                KaynakDepoId = fixture.DefaultWarehouseId,
                HedefDepoId = fixture.SecondWarehouseId,
                Miktar = 3,
                Aciklama = "Raf aktarımı"
            };

            var first = await fixture.Service.TransferAsync(request, "transfer-1");
            var replay = await fixture.Service.TransferAsync(request, "transfer-1");

            await using var db = fixture.CreateDbContext();
            Assert.False(first.Tekrarlandi);
            Assert.True(replay.Tekrarlandi);
            Assert.Equal(first.IslemId, replay.IslemId);
            Assert.Equal(2, await db.StokHareketleri.CountAsync(x => x.StokDefterIslemiId == first.IslemId));
            Assert.Single(await db.StokDefterIslemleri.ToListAsync());
        }

        [Fact]
        public async Task TransferAsync_SameKeyWithDifferentPayload_IsRejected()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();
            await fixture.Service.TransferAsync(new StokTransferRequest
            {
                UrunHizmetId = fixture.ProductId,
                KaynakDepoId = fixture.DefaultWarehouseId,
                HedefDepoId = fixture.SecondWarehouseId,
                Miktar = 1
            }, "same-key");

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.TransferAsync(new StokTransferRequest
            {
                UrunHizmetId = fixture.ProductId,
                KaynakDepoId = fixture.DefaultWarehouseId,
                HedefDepoId = fixture.SecondWarehouseId,
                Miktar = 2
            }, "same-key"));

            await using var db = fixture.CreateDbContext();
            Assert.Single(await db.StokDefterIslemleri.ToListAsync());
            Assert.Equal(2, await db.StokHareketleri.CountAsync(x => x.StokDefterIslemiId != null));
        }

        [Fact]
        public async Task TransferAsync_ForeignTenantWarehouse_RollsBackAtomically()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.TransferAsync(
                new StokTransferRequest
                {
                    UrunHizmetId = fixture.ProductId,
                    KaynakDepoId = fixture.DefaultWarehouseId,
                    HedefDepoId = fixture.ForeignWarehouseId,
                    Miktar = 2
                },
                "foreign-transfer"));

            await using var db = fixture.CreateDbContext();
            Assert.False(await db.StokDefterIslemleri.AnyAsync());
            Assert.Single(await db.StokHareketleri.ToListAsync());
        }

        [Fact]
        public async Task TransferAsync_InsufficientStock_WritesNeitherLeg()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.TransferAsync(
                new StokTransferRequest
                {
                    UrunHizmetId = fixture.ProductId,
                    KaynakDepoId = fixture.DefaultWarehouseId,
                    HedefDepoId = fixture.SecondWarehouseId,
                    Miktar = 11
                },
                "too-much"));

            await using var db = fixture.CreateDbContext();
            Assert.False(await db.StokDefterIslemleri.AnyAsync());
            Assert.Equal(10m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar));
        }

        [Fact]
        public async Task CountAndReverse_CreateImmutableDeltas_AndSecondReverseIsRejected()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();
            var count = await fixture.Service.CountAsync(new StokSayimRequest
            {
                UrunHizmetId = fixture.ProductId,
                DepoId = fixture.DefaultWarehouseId,
                SayilanMiktar = 7,
                Onaylandi = true,
                Aciklama = "Aylık sayım"
            }, "count-1");

            var reversed = await fixture.Service.ReverseAsync(count.IslemId, new StokTersKayitRequest
            {
                Aciklama = "Hatalı sayım"
            }, "reverse-1");

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ReverseAsync(
                count.IslemId,
                new StokTersKayitRequest { Aciklama = "İkinci deneme" },
                "reverse-2"));

            await using var db = fixture.CreateDbContext();
            var rows = await db.StokHareketleri.OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(3, rows.Count);
            Assert.Equal(10m, rows.Sum(x => x.Miktar));
            Assert.Equal(-3m, rows[1].Miktar);
            Assert.Equal(3m, rows[2].Miktar);
            Assert.Equal(count.IslemId, reversed.TersKayitKaynakIslemId);
            Assert.Single(await db.StokDefterIslemleri.Where(x => x.TersKayitKaynakIslemId == count.IslemId).ToListAsync());
        }

        [Fact]
        public async Task CountAsync_RequiresExplicitUserConfirmation()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.CountAsync(new StokSayimRequest
            {
                UrunHizmetId = fixture.ProductId,
                DepoId = fixture.DefaultWarehouseId,
                SayilanMiktar = 8,
                Onaylandi = false
            }, "unconfirmed-count"));

            await using var db = fixture.CreateDbContext();
            Assert.False(await db.StokDefterIslemleri.AnyAsync());
        }

        [Fact]
        public async Task ReservationAndRelease_AreLedgerDeltas_AndCannotExceedPhysicalStock()
        {
            using var fixture = await StockLedgerFixture.CreateAsync();
            await fixture.Service.CreateMovementAsync(new StokHareketIslemRequest
            {
                UrunHizmetId = fixture.ProductId,
                DepoId = fixture.DefaultWarehouseId,
                IslemTipi = StokDefterIslemTipleri.Rezervasyon,
                Miktar = 4
            }, "reserve-1");
            await fixture.Service.CreateMovementAsync(new StokHareketIslemRequest
            {
                UrunHizmetId = fixture.ProductId,
                DepoId = fixture.DefaultWarehouseId,
                IslemTipi = StokDefterIslemTipleri.RezervasyonBirakma,
                Miktar = 1
            }, "release-1");

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateMovementAsync(new StokHareketIslemRequest
            {
                UrunHizmetId = fixture.ProductId,
                DepoId = fixture.DefaultWarehouseId,
                IslemTipi = StokDefterIslemTipleri.Cikis,
                Miktar = 8
            }, "consume-reserved"));

            await using var db = fixture.CreateDbContext();
            Assert.Equal(10m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.Miktar));
            Assert.Equal(3m, (await db.StokHareketleri.ToListAsync()).Sum(x => x.RezerveMiktar));
        }

        [Fact]
        public async Task FeatureGuard_WhenAdvancedStockIsNotIncluded_DeniesAccess()
        {
            using var fixture = await StockLedgerFixture.CreateAsync(advancedStock: false);

            var error = await Assert.ThrowsAsync<EntitlementViolationException>(() => fixture.Service.GetAsync());

            Assert.Equal(EntitlementErrorCodes.FeatureNotAvailable, error.Code);
        }

        private sealed class StockLedgerFixture : IDisposable
        {
            private StockLedgerFixture(string path, DbContextOptions<CashTrackerDbContext> options, bool? advancedStock)
            {
                Path = path;
                Options = options;
                IEntitlementGuard? guard = advancedStock is null
                    ? null
                    : new EntitlementGuard(new StaticEntitlementService(new SubscriptionEntitlementStatus
                    {
                        HesapTipi = HesapTipleri.Isletme,
                        PlanKodu = advancedStock.Value ? PlanKodlari.IsletmeBuyume : PlanKodlari.IsletmeUcretsiz,
                        PlanAdi = advancedStock.Value ? "Büyüme" : "Ücretsiz",
                        StokRaporAktif = advancedStock.Value
                    }));
                Service = new GelismisStokService(
                    new SingleDbContextFactory(options),
                    new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Tenant 1", IsAktif = true } },
                    guard);
            }

            public string Path { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }
            public GelismisStokService Service { get; }
            public int ProductId { get; private set; }
            public int DefaultWarehouseId { get; private set; }
            public int SecondWarehouseId { get; private set; }
            public int ForeignWarehouseId { get; private set; }

            public static async Task<StockLedgerFixture> CreateAsync(bool? advancedStock = null)
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"cashtracker_stock_ledger_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={path}")
                    .Options;
                var fixture = new StockLedgerFixture(path, options, advancedStock);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.AddRange(
                    new Isletme { Id = 1, Ad = "Tenant 1", IsAktif = true },
                    new Isletme { Id = 2, Ad = "Tenant 2", IsAktif = true });
                var product = new UrunHizmet
                {
                    IsletmeId = 1,
                    Tip = "Urun",
                    Ad = "Defter ürünü",
                    Birim = "Adet",
                    SatisFiyati = 100,
                    Aktif = true
                };
                var defaultWarehouse = new StokDepo { IsletmeId = 1, Ad = "Merkez", Kod = "MRK", Varsayilan = true };
                var secondWarehouse = new StokDepo { IsletmeId = 1, Ad = "Şube", Kod = "SUB" };
                var foreignWarehouse = new StokDepo { IsletmeId = 2, Ad = "Yabancı", Kod = "YBN", Varsayilan = true };
                db.UrunHizmetleri.Add(product);
                db.StokDepolari.AddRange(defaultWarehouse, secondWarehouse, foreignWarehouse);
                await db.SaveChangesAsync();
                db.StokHareketleri.Add(new StokHareket
                {
                    IsletmeId = 1,
                    UrunHizmetId = product.Id,
                    Miktar = 10,
                    HareketTipi = "Giris",
                    Kaynak = "Legacy başlangıç"
                });
                await db.SaveChangesAsync();

                fixture.ProductId = product.Id;
                fixture.DefaultWarehouseId = defaultWarehouse.Id;
                fixture.SecondWarehouseId = secondWarehouse.Id;
                fixture.ForeignWarehouseId = foreignWarehouse.Id;
                return fixture;
            }

            public CashTrackerDbContext CreateDbContext() => new(Options);

            public void Dispose()
            {
                try
                {
                    if (File.Exists(Path)) File.Delete(Path);
                }
                catch
                {
                }
            }
        }

        private sealed class StaticEntitlementService : ISubscriptionEntitlementService
        {
            private readonly SubscriptionEntitlementStatus _status;

            public StaticEntitlementService(SubscriptionEntitlementStatus status) => _status = status;

            public Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(int isletmeId, DateTime? now = null, System.Threading.CancellationToken ct = default) => Task.FromResult(_status);
            public Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(int muhasebeciIsletmeId, DateTime? now = null, System.Threading.CancellationToken ct = default) => Task.FromResult(_status);
        }
    }
}
