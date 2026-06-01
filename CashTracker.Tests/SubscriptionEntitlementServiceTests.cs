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
    public sealed class SubscriptionEntitlementServiceTests
    {
        private static readonly DateTime Now = new(2026, 5, 13, 12, 0, 0);

        [Fact]
        public async Task IsletmeEntitlement_KendiUcretliPlanlardaYuksekPlanKazanir()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.Abonelikler.Add(CreateAbonelik(1, HesapTipleri.Isletme, PlanKodlari.IsletmeBaslangic));
                db.Abonelikler.Add(CreateAbonelik(1, HesapTipleri.Isletme, PlanKodlari.IsletmeIsletme));
                db.IsletmeDenemeleri.Add(CreateDeneme(1));
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(2, 1));
                db.Abonelikler.Add(CreateAbonelik(2, HesapTipleri.Muhasebeci, PlanKodlari.MuhasebeciPro));
            });

            var actual = await fixture.Service.GetIsletmeEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.IsletmeIsletme, actual.PlanKodu);
            Assert.Equal(EntitlementKaynaklari.KendiAboneligi, actual.Kaynak);
            Assert.True(actual.OcrAktif);
            Assert.True(actual.GibAktif);
            Assert.True(actual.TelegramAktif);
            Assert.True(actual.AiSinirsiz);
            Assert.Equal(3, actual.KullaniciLimiti);
            Assert.Null(actual.SponsorMuhasebeciIsletmeId);
            await AssertNoPersistedEntitlementAsync(fixture);
        }

        [Fact]
        public async Task IsletmeEntitlement_DenemeSponsorunOnundeBaslangicHakkiVerir()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.IsletmeDenemeleri.Add(CreateDeneme(1));
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(2, 1));
                db.Abonelikler.Add(CreateAbonelik(2, HesapTipleri.Muhasebeci, PlanKodlari.MuhasebeciPro));
            });

            var actual = await fixture.Service.GetIsletmeEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.IsletmeBaslangic, actual.PlanKodu);
            Assert.Equal(EntitlementKaynaklari.IsletmeDenemesi, actual.Kaynak);
            Assert.True(actual.OcrAktif);
            Assert.True(actual.GibAktif);
            Assert.True(actual.TelegramAktif);
            Assert.True(actual.AiAktif);
            Assert.Equal(50, actual.AiMesajLimiti);
            Assert.Equal(Now.AddDays(20), actual.GecerliBitisAt);
            Assert.Null(actual.SponsorMuhasebeciIsletmeId);
            await AssertNoPersistedEntitlementAsync(fixture);
        }

        [Fact]
        public async Task IsletmeEntitlement_MuhasebeciProSponsorBaslangicHakkiVerir()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(2, 1));
                db.Abonelikler.Add(CreateAbonelik(2, HesapTipleri.Muhasebeci, PlanKodlari.MuhasebeciPro));
            });

            var actual = await fixture.Service.GetIsletmeEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.IsletmeBaslangic, actual.PlanKodu);
            Assert.Equal(EntitlementKaynaklari.MuhasebeciProSponsor, actual.Kaynak);
            Assert.Equal(2, actual.SponsorMuhasebeciIsletmeId);
            Assert.True(actual.AiAktif);
            Assert.Equal(50, actual.AiMesajLimiti);
            await AssertNoPersistedEntitlementAsync(fixture);
        }

        [Fact]
        public async Task IsletmeEntitlement_AktifKaynakYokkenUcretsizHakDondurur()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.IsletmeDenemeleri.Add(CreateDeneme(1, odemeYontemiEklendi: false));
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(2, 1));
                db.Abonelikler.Add(CreateAbonelik(
                    2,
                    HesapTipleri.Muhasebeci,
                    PlanKodlari.MuhasebeciPro,
                    donemBitisAt: Now.AddDays(-1)));
            });

            var actual = await fixture.Service.GetIsletmeEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.IsletmeUcretsiz, actual.PlanKodu);
            Assert.Equal(EntitlementKaynaklari.Ucretsiz, actual.Kaynak);
            Assert.False(actual.OcrAktif);
            Assert.False(actual.GibAktif);
            Assert.False(actual.TelegramAktif);
            Assert.False(actual.AiAktif);
            Assert.Equal(0, actual.AiMesajLimiti);
            Assert.Equal(1, actual.KullaniciLimiti);
            await AssertNoPersistedEntitlementAsync(fixture);
        }

        [Fact]
        public async Task MuhasebeciEntitlement_UcretsizFallbackMusteriLimitiniDondurur()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(1, 10));
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(1, 11));
            });

            var actual = await fixture.Service.GetMuhasebeciEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.MuhasebeciUcretsiz, actual.PlanKodu);
            Assert.Equal(EntitlementKaynaklari.Ucretsiz, actual.Kaynak);
            Assert.Equal(2, actual.AktifMusteriSayisi);
            Assert.Equal(3, actual.MusteriLimiti);
            Assert.True(actual.MuhasebeciPaneliAktif);
            Assert.False(actual.AiAktif);
            Assert.False(actual.MuhasebeciProOnerilir);
        }

        [Fact]
        public async Task MuhasebeciEntitlement_StandartFiyatiMusteriSayisinaGoreHesaplarVeProOnerir()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.Abonelikler.Add(CreateAbonelik(1, HesapTipleri.Muhasebeci, PlanKodlari.MuhasebeciStandart));

                foreach (var musteriId in Enumerable.Range(10, 20))
                    db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(1, musteriId));

                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(1, 99, bitisAt: Now.AddDays(-1)));
            });

            var actual = await fixture.Service.GetMuhasebeciEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.MuhasebeciStandart, actual.PlanKodu);
            Assert.Equal(20, actual.AktifMusteriSayisi);
            Assert.Equal(1199, actual.AylikTutar);
            Assert.Equal(1199, actual.MuhasebeciStandartAylikTutar);
            Assert.True(actual.MuhasebeciProOnerilir);
            Assert.True(actual.AiAktif);
            Assert.Equal(100, actual.AiMesajLimiti);
            Assert.Equal(10, actual.MusteriLimiti);
        }

        [Fact]
        public async Task MuhasebeciEntitlement_ProSinirsizHaklariDondurur()
        {
            using var fixture = await SubscriptionFixture.CreateAsync();
            await fixture.SeedAsync(db =>
            {
                db.Abonelikler.Add(CreateAbonelik(1, HesapTipleri.Muhasebeci, PlanKodlari.MuhasebeciPro));
                db.MuhasebeciMusterileri.Add(CreateMuhasebeciMusteri(1, 10));
            });

            var actual = await fixture.Service.GetMuhasebeciEntitlementAsync(1, Now);

            Assert.Equal(PlanKodlari.MuhasebeciPro, actual.PlanKodu);
            Assert.Equal(1199, actual.AylikTutar);
            Assert.True(actual.AiSinirsiz);
            Assert.True(actual.MusteriSinirsiz);
            Assert.True(actual.OneCikmaAktif);
            Assert.True(actual.DonemOtomasyonuAktif);
            Assert.True(actual.MusteriSaglikSkoruAktif);
            Assert.False(actual.MuhasebeciProOnerilir);
        }

        private static Abonelik CreateAbonelik(
            int isletmeId,
            string hesapTipi,
            string planKodu,
            string durum = "Aktif",
            DateTime? donemBaslangicAt = null,
            DateTime? donemBitisAt = null)
        {
            var plan = SubscriptionPlanCatalog.Plans.Single(x => x.Kod == planKodu);

            return new Abonelik
            {
                IsletmeId = isletmeId,
                HesapTipi = hesapTipi,
                PlanKodu = planKodu,
                Durum = durum,
                AylikTutar = plan.AylikTutar,
                ParaBirimi = "TRY",
                DonemBaslangicAt = donemBaslangicAt ?? Now.AddDays(-10),
                DonemBitisAt = donemBitisAt ?? Now.AddDays(30),
                CreatedAt = Now.AddDays(-10),
                UpdatedAt = Now.AddDays(-10)
            };
        }

        private static IsletmeDeneme CreateDeneme(int isletmeId, bool odemeYontemiEklendi = true)
        {
            return new IsletmeDeneme
            {
                IsletmeId = isletmeId,
                PlanKodu = PlanKodlari.IsletmeBaslangic,
                Durum = "Aktif",
                BaslangicAt = Now.AddDays(-10),
                BitisAt = Now.AddDays(20),
                OdemeYontemiEklendi = odemeYontemiEklendi,
                CreatedAt = Now.AddDays(-10),
                UpdatedAt = Now.AddDays(-10)
            };
        }

        private static MuhasebeciMusteri CreateMuhasebeciMusteri(
            int muhasebeciIsletmeId,
            int musteriIsletmeId,
            DateTime? bitisAt = null)
        {
            return new MuhasebeciMusteri
            {
                MuhasebeciIsletmeId = muhasebeciIsletmeId,
                MusteriIsletmeId = musteriIsletmeId,
                Durum = "Aktif",
                BaslangicAt = Now.AddDays(-10),
                BitisAt = bitisAt,
                CreatedAt = Now.AddDays(-10),
                UpdatedAt = Now.AddDays(-10)
            };
        }

        private static async Task AssertNoPersistedEntitlementAsync(SubscriptionFixture fixture)
        {
            await using var db = fixture.CreateDbContext();
            Assert.False(await db.IsletmeEntitlementlari.AnyAsync());
        }

        private sealed class SubscriptionFixture : IDisposable
        {
            private SubscriptionFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
            {
                DbPath = dbPath;
                Options = options;
                Service = new SubscriptionEntitlementService(new SingleDbContextFactory(options));
            }

            public string DbPath { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }
            public SubscriptionEntitlementService Service { get; }

            public static async Task<SubscriptionFixture> CreateAsync()
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_entitlement_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                var fixture = new SubscriptionFixture(dbPath, options);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                return fixture;
            }

            public CashTrackerDbContext CreateDbContext()
            {
                return new CashTrackerDbContext(Options);
            }

            public async Task SeedAsync(Action<CashTrackerDbContext> seed)
            {
                await using var db = CreateDbContext();
                seed(db);
                await db.SaveChangesAsync();
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
