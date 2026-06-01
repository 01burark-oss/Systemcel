using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class FaturaServiceTests
    {
        [Fact]
        public async Task CreateDraftAsync_CalculatesTotals_WithoutCariOrStockMovement()
        {
            using var fixture = await FaturaFixture.CreateAsync();
            var service = fixture.CreateFaturaService();

            var id = await service.CreateDraftAsync(new FaturaCreateRequest
            {
                CariKartId = fixture.CariId,
                FaturaTipi = "Satis",
                Tarih = new DateTime(2026, 4, 20),
                Satirlar =
                [
                    new FaturaSatirRequest
                    {
                        UrunHizmetId = fixture.UrunId,
                        Aciklama = "Test urun",
                        Miktar = 2,
                        BirimFiyat = 100,
                        IskontoOrani = 10,
                        KdvOrani = 20
                    }
                ]
            });

            await using var db = fixture.CreateDbContext();
            var fatura = await db.Faturalar.SingleAsync(x => x.Id == id);

            Assert.Equal(FaturaDurum.YerelTaslak, fatura.Durum);
            Assert.Equal(166.67m, fatura.AraToplam);
            Assert.Equal(16.67m, fatura.IskontoToplam);
            Assert.Equal(30m, fatura.KdvToplam);
            Assert.Equal(180m, fatura.GenelToplam);
            Assert.False(await db.CariHareketleri.AnyAsync());
            Assert.False(await db.StokHareketleri.AnyAsync());
        }

        [Fact]
        public async Task MarkAsIssuedAsync_ForSale_CreatesCariReceivableAndStockOut()
        {
            using var fixture = await FaturaFixture.CreateAsync();
            var service = fixture.CreateFaturaService();
            var id = await service.CreateDraftAsync(new FaturaCreateRequest
            {
                CariKartId = fixture.CariId,
                FaturaTipi = "Satis",
                Satirlar =
                [
                    new FaturaSatirRequest
                    {
                        UrunHizmetId = fixture.UrunId,
                        Aciklama = "Test urun",
                        Miktar = 3,
                        BirimFiyat = 50,
                        KdvOrani = 10
                    }
                ]
            });

            await service.MarkAsIssuedAsync(id);

            await using var db = fixture.CreateDbContext();
            var fatura = await db.Faturalar.SingleAsync(x => x.Id == id);
            var cari = await db.CariHareketleri.SingleAsync();
            var stok = await db.StokHareketleri.SingleAsync();

            Assert.Equal(FaturaDurum.Kesildi, fatura.Durum);
            Assert.Equal("Alacak", cari.HareketTipi);
            Assert.Equal(150, cari.Tutar);
            Assert.Equal(-3, stok.Miktar);
            Assert.Equal("Cikis", stok.HareketTipi);
        }

        [Fact]
        public async Task TahsilatOdemeService_FullPayment_UpdatesInvoiceCariAndKasa()
        {
            using var fixture = await FaturaFixture.CreateAsync();
            var faturaService = fixture.CreateFaturaService();
            var paymentService = new TahsilatOdemeService(fixture.Factory, fixture.Isletme);
            var id = await faturaService.CreateDraftAsync(new FaturaCreateRequest
            {
                CariKartId = fixture.CariId,
                FaturaTipi = "Satis",
                Satirlar =
                [
                    new FaturaSatirRequest
                    {
                        Aciklama = "Hizmet",
                        Miktar = 1,
                        BirimFiyat = 100,
                        KdvOrani = 20,
                        StokEtkilesin = false
                    }
                ]
            });
            await faturaService.MarkAsIssuedAsync(id);

            await paymentService.CreateAsync(new TahsilatOdemeRequest
            {
                FaturaId = id,
                Tutar = 100,
                OdemeYontemi = "Nakit"
            });

            await using var db = fixture.CreateDbContext();
            var fatura = await db.Faturalar.SingleAsync(x => x.Id == id);

            Assert.Equal(FaturaDurum.Odendi, fatura.Durum);
            Assert.Equal(100, fatura.OdenenTutar);
            Assert.Equal(2, await db.CariHareketleri.CountAsync());
            Assert.Equal("Gelir", (await db.Kasalar.SingleAsync()).Tip);
        }

        [Fact]
        public async Task TahsilatOdemeService_PaymentForDraft_IssuesInvoiceAndAddsPayment()
        {
            using var fixture = await FaturaFixture.CreateAsync();
            var faturaService = fixture.CreateFaturaService();
            var paymentService = new TahsilatOdemeService(fixture.Factory, fixture.Isletme);
            var id = await faturaService.CreateDraftAsync(new FaturaCreateRequest
            {
                CariKartId = fixture.CariId,
                FaturaTipi = "Satis",
                Satirlar =
                [
                    new FaturaSatirRequest
                    {
                        UrunHizmetId = fixture.UrunId,
                        Aciklama = "Urun",
                        Miktar = 2,
                        BirimFiyat = 50,
                        KdvOrani = 20
                    }
                ]
            });

            await paymentService.CreateAsync(new TahsilatOdemeRequest
            {
                FaturaId = id,
                Tutar = 100,
                OdemeYontemi = "Nakit"
            });

            await using var db = fixture.CreateDbContext();
            var fatura = await db.Faturalar.SingleAsync(x => x.Id == id);

            Assert.Equal(FaturaDurum.Odendi, fatura.Durum);
            Assert.Equal(100, fatura.OdenenTutar);
            Assert.Equal(2, await db.CariHareketleri.CountAsync());
            Assert.Equal(1, await db.StokHareketleri.CountAsync());
            Assert.Equal("Gelir", (await db.Kasalar.SingleAsync()).Tip);
        }

        [Fact]
        public async Task CreateDraftAsync_TreatsUnitPriceAsVatIncluded()
        {
            using var fixture = await FaturaFixture.CreateAsync();
            var service = fixture.CreateFaturaService();

            var id = await service.CreateDraftAsync(new FaturaCreateRequest
            {
                CariKartId = fixture.CariId,
                FaturaTipi = "Satis",
                Satirlar =
                [
                    new FaturaSatirRequest
                    {
                        UrunHizmetId = fixture.UrunId,
                        Aciklama = "KDV dahil urun",
                        Miktar = 20,
                        BirimFiyat = 50,
                        KdvOrani = 20
                    }
                ]
            });

            await using var db = fixture.CreateDbContext();
            var fatura = await db.Faturalar.SingleAsync(x => x.Id == id);
            var satir = await db.FaturaSatirlari.SingleAsync(x => x.FaturaId == id);

            Assert.Equal(1000m, fatura.GenelToplam);
            Assert.Equal(166.67m, fatura.KdvToplam);
            Assert.Equal(833.33m, fatura.AraToplam);
            Assert.Equal(1000m, satir.SatirToplam);
            Assert.Equal(833.33m, satir.SatirNetTutar);
        }

        private sealed class FaturaFixture : IDisposable
        {
            private FaturaFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
            {
                DbPath = dbPath;
                Options = options;
                Factory = new SingleDbContextFactory(options);
                Isletme = new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Test", IsAktif = true } };
            }

            public string DbPath { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }
            public SingleDbContextFactory Factory { get; }
            public FakeIsletmeService Isletme { get; }
            public int CariId { get; private set; }
            public int UrunId { get; private set; }

            public static async Task<FaturaFixture> CreateAsync()
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_fatura_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                var fixture = new FaturaFixture(dbPath, options);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.Add(new Isletme { Id = 1, Ad = "Test", IsAktif = true, CreatedAt = DateTime.Now });
                var cari = new CariKart { IsletmeId = 1, Tip = "Musteri", Unvan = "Test Cari", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
                var urun = new UrunHizmet { IsletmeId = 1, Tip = "Urun", Ad = "Test Urun", Barkod = "869", Birim = "Adet", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
                db.CariKartlari.Add(cari);
                db.UrunHizmetleri.Add(urun);
                await db.SaveChangesAsync();
                fixture.CariId = cari.Id;
                fixture.UrunId = urun.Id;
                return fixture;
            }

            public CashTrackerDbContext CreateDbContext()
            {
                return new CashTrackerDbContext(Options);
            }

            public FaturaService CreateFaturaService()
            {
                return new FaturaService(Factory, Isletme);
            }

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
    }
}
