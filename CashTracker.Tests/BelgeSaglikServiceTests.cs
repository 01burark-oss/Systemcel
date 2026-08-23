using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class BelgeSaglikServiceTests
    {
        private static readonly DateTime ReferenceDate = new(2026, 8, 23);

        [Fact]
        public async Task GetAsync_TamBelgeleriHazirOlarakPuanlarVeGibKimliginiDosyaYerineKabulEder()
        {
            using var fixture = await BelgeSaglikFixture.CreateAsync();
            var customerId = await fixture.AddCustomerAsync(1, "Hazır Cari", "1234567890");
            var portalInvoiceId = await fixture.AddInvoiceAsync(
                1,
                customerId,
                new DateTime(2026, 8, 5),
                FaturaDurum.Kesildi,
                portalUuid: "portal-uuid");
            await fixture.AddLineAsync(1, portalInvoiceId);

            var localInvoiceId = await fixture.AddInvoiceAsync(
                1,
                customerId,
                new DateTime(2026, 8, 18),
                FaturaDurum.Kesildi,
                invoiceType: "Alis");
            await fixture.AddLineAsync(1, localInvoiceId);
            await fixture.AddFileAsync(1, localInvoiceId, "XML", "belgeler/fatura.xml");

            var result = await fixture.Service.GetAsync(1, ReferenceDate);

            Assert.Equal(100, result.Skor);
            Assert.Equal(BelgeSaglikDurumlari.Hazir, result.Durum);
            Assert.Equal(new DateTime(2026, 8, 1), result.DonemBaslangic);
            Assert.Equal(new DateTime(2026, 8, 31), result.DonemBitis);
            Assert.Equal(2, result.FaturaSayisi);
            Assert.Equal(2, result.HazirBelgeSayisi);
            Assert.Equal(0, result.EksikBelgeSayisi);
            Assert.Equal(0, result.DosyasiEksikFaturaSayisi);
            Assert.Equal(new DateTime(2026, 8, 18), result.SonBelgeAt);
            Assert.Empty(result.Sorunlar);
        }

        [Fact]
        public async Task GetAsync_EksikleriOranliVeDeterministikOlarakPuandanDuser()
        {
            using var fixture = await BelgeSaglikFixture.CreateAsync();
            var completeCustomerId = await fixture.AddCustomerAsync(1, "Tam Cari", "1234567890");
            var incompleteCustomerId = await fixture.AddCustomerAsync(1, "", "");

            var completeInvoiceId = await fixture.AddInvoiceAsync(
                1,
                completeCustomerId,
                new DateTime(2026, 8, 3),
                FaturaDurum.Kesildi);
            await fixture.AddLineAsync(1, completeInvoiceId);
            await fixture.AddFileAsync(1, completeInvoiceId);

            await fixture.AddInvoiceAsync(
                1,
                incompleteCustomerId,
                new DateTime(2026, 8, 9),
                FaturaDurum.YerelTaslak,
                dueDateMissing: true);

            var result = await fixture.Service.GetAsync(1, ReferenceDate);

            Assert.Equal(54, result.Skor);
            Assert.Equal(BelgeSaglikDurumlari.Eksik, result.Durum);
            Assert.Equal(1, result.HazirBelgeSayisi);
            Assert.Equal(1, result.EksikBelgeSayisi);
            Assert.Equal(1, result.TaslakFaturaSayisi);
            Assert.Equal(1, result.DosyasiEksikFaturaSayisi);
            Assert.Equal(1, result.SatiriEksikFaturaSayisi);
            Assert.Equal(1, result.CariBilgisiEksikFaturaSayisi);
            Assert.Equal(1, result.VadeTarihiEksikFaturaSayisi);
            Assert.Equal(46, result.Sorunlar.Sum(x => x.PuanEtkisi));
            Assert.Contains(result.Sorunlar, x => x.Kod == "CariBilgisiEksik" && x.AksiyonUrl == "/app/cari-hesaplar");
        }

        [Fact]
        public async Task GetAsync_FaturaYoksaSkorUretmez()
        {
            using var fixture = await BelgeSaglikFixture.CreateAsync();

            var result = await fixture.Service.GetAsync(1, ReferenceDate);

            Assert.Null(result.Skor);
            Assert.Equal(BelgeSaglikDurumlari.VeriYok, result.Durum);
            Assert.Equal(0, result.FaturaSayisi);
            Assert.Null(result.SonBelgeAt);
            Assert.Empty(result.Sorunlar);
        }

        [Fact]
        public async Task GetAsync_TumBelgeKaynaklariniIsletmeyeGoreIzoleEder()
        {
            using var fixture = await BelgeSaglikFixture.CreateAsync();
            var tenantOneCustomer = await fixture.AddCustomerAsync(1, "Birinci Cari", "1234567890");
            var tenantTwoCustomer = await fixture.AddCustomerAsync(2, "", "");
            var tenantOneInvoice = await fixture.AddInvoiceAsync(1, tenantOneCustomer, new DateTime(2026, 8, 4), FaturaDurum.Kesildi);
            await fixture.AddLineAsync(1, tenantOneInvoice);
            await fixture.AddFileAsync(1, tenantOneInvoice);

            var tenantTwoInvoice = await fixture.AddInvoiceAsync(2, tenantTwoCustomer, new DateTime(2026, 8, 4), FaturaDurum.YerelTaslak, dueDateMissing: true);
            await fixture.AddLineAsync(2, tenantOneInvoice);
            await fixture.AddFileAsync(2, tenantOneInvoice);
            await fixture.AddPendingDataRequestAsync(2);

            var result = await fixture.Service.GetAsync(1, ReferenceDate);

            Assert.Equal(100, result.Skor);
            Assert.Equal(1, result.FaturaSayisi);
            Assert.Equal(0, result.BekleyenVeriIstegiSayisi);
            Assert.DoesNotContain(result.Sorunlar, x => x.Kod == "BekleyenVeriIstegi");
            Assert.NotEqual(tenantOneInvoice, tenantTwoInvoice);
        }

        [Fact]
        public async Task GetAsync_AktifMuhasebeciBaglantisiniGosterir()
        {
            using var fixture = await BelgeSaglikFixture.CreateAsync();
            await fixture.AddAccountantConnectionAsync(accountantId: 3, customerBusinessId: 1, active: true);
            await fixture.AddAccountantConnectionAsync(accountantId: 4, customerBusinessId: 2, active: false);

            var linked = await fixture.Service.GetAsync(1, ReferenceDate);
            var notLinked = await fixture.Service.GetAsync(2, ReferenceDate);

            Assert.True(linked.MuhasebeciBagli);
            Assert.False(notLinked.MuhasebeciBagli);
        }

        [Fact]
        public async Task GetAsync_BekleyenVeriIstekleriniSayipSkoraVeSorunlaraYansitir()
        {
            using var fixture = await BelgeSaglikFixture.CreateAsync();
            var customerId = await fixture.AddCustomerAsync(1, "İstek Carisi", "1234567890");
            var invoiceId = await fixture.AddInvoiceAsync(1, customerId, new DateTime(2026, 8, 7), FaturaDurum.Kesildi);
            await fixture.AddLineAsync(1, invoiceId);
            await fixture.AddFileAsync(1, invoiceId);
            await fixture.AddPendingDataRequestAsync(1);
            await fixture.AddPendingDataRequestAsync(1);
            await fixture.AddPendingDataRequestAsync(1, MuhasebeciSohbetVeriIstegiDurumlari.Paylasildi);

            var result = await fixture.Service.GetAsync(1, ReferenceDate);

            Assert.Equal(2, result.BekleyenVeriIstegiSayisi);
            Assert.Equal(90, result.Skor);
            Assert.Equal(BelgeSaglikDurumlari.Hazir, result.Durum);
            var issue = Assert.Single(result.Sorunlar);
            Assert.Equal("BekleyenVeriIstegi", issue.Kod);
            Assert.Equal(2, issue.Adet);
            Assert.Equal(10, issue.PuanEtkisi);
            Assert.Equal("/app/sohbetler", issue.AksiyonUrl);
        }

        private sealed class BelgeSaglikFixture : IDisposable
        {
            private BelgeSaglikFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
            {
                DbPath = dbPath;
                Options = options;
                Service = new BelgeSaglikService(new SingleDbContextFactory(options));
            }

            public string DbPath { get; }
            public DbContextOptions<CashTrackerDbContext> Options { get; }
            public BelgeSaglikService Service { get; }

            public static async Task<BelgeSaglikFixture> CreateAsync()
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_belge_sagligi_{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                var fixture = new BelgeSaglikFixture(dbPath, options);

                await using var db = fixture.CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.AddRange(
                    new Isletme { Id = 1, Ad = "Birinci İşletme", IsAktif = true },
                    new Isletme { Id = 2, Ad = "İkinci İşletme", IsAktif = false },
                    new Isletme { Id = 3, Ad = "Birinci Muhasebeci", TenantTipi = HesapTipleri.Muhasebeci },
                    new Isletme { Id = 4, Ad = "İkinci Muhasebeci", TenantTipi = HesapTipleri.Muhasebeci });
                await db.SaveChangesAsync();
                return fixture;
            }

            public async Task<int> AddCustomerAsync(int businessId, string title, string taxNumber)
            {
                await using var db = CreateDbContext();
                var customer = new CariKart
                {
                    IsletmeId = businessId,
                    Tip = "Musteri",
                    Unvan = title,
                    VergiNoTc = taxNumber,
                    CreatedAt = ReferenceDate,
                    UpdatedAt = ReferenceDate
                };
                db.CariKartlari.Add(customer);
                await db.SaveChangesAsync();
                return customer.Id;
            }

            public async Task<int> AddInvoiceAsync(
                int businessId,
                int customerId,
                DateTime invoiceDate,
                string status,
                DateTime? dueDate = default,
                string invoiceType = "Satis",
                string portalUuid = "",
                bool dueDateMissing = false)
            {
                await using var db = CreateDbContext();
                var invoice = new Fatura
                {
                    IsletmeId = businessId,
                    CariKartId = customerId,
                    Tarih = invoiceDate,
                    VadeTarihi = dueDateMissing ? null : dueDate ?? invoiceDate.AddDays(30),
                    FaturaTipi = invoiceType,
                    Durum = status,
                    YerelFaturaNo = $"TEST-{Guid.NewGuid():N}",
                    PortalUuid = portalUuid,
                    GenelToplam = 1_000m,
                    CreatedAt = invoiceDate,
                    UpdatedAt = invoiceDate
                };
                db.Faturalar.Add(invoice);
                await db.SaveChangesAsync();
                return invoice.Id;
            }

            public async Task AddLineAsync(int businessId, int invoiceId)
            {
                await using var db = CreateDbContext();
                db.FaturaSatirlari.Add(new FaturaSatir
                {
                    IsletmeId = businessId,
                    FaturaId = invoiceId,
                    Aciklama = "Hizmet",
                    Miktar = 1,
                    BirimFiyat = 1_000m,
                    SatirNetTutar = 1_000m,
                    SatirToplam = 1_000m
                });
                await db.SaveChangesAsync();
            }

            public async Task AddFileAsync(
                int businessId,
                int invoiceId,
                string documentType = "PDF",
                string path = "belgeler/fatura.pdf")
            {
                await using var db = CreateDbContext();
                db.BelgeDosyalari.Add(new BelgeDosya
                {
                    IsletmeId = businessId,
                    FaturaId = invoiceId,
                    BelgeTipi = documentType,
                    DosyaYolu = path,
                    CreatedAt = ReferenceDate
                });
                await db.SaveChangesAsync();
            }

            public async Task AddPendingDataRequestAsync(
                int targetBusinessId,
                string status = MuhasebeciSohbetVeriIstegiDurumlari.Beklemede)
            {
                await using var db = CreateDbContext();
                db.MuhasebeciSohbetVeriIstekleri.Add(new MuhasebeciSohbetVeriIstegi
                {
                    SohbetId = 1,
                    IsteyenIsletmeId = 3,
                    HedefIsletmeId = targetBusinessId,
                    Durum = status,
                    Baslangic = new DateTime(2026, 8, 1),
                    Bitis = new DateTime(2026, 8, 31),
                    CreatedAt = ReferenceDate,
                    UpdatedAt = ReferenceDate
                });
                await db.SaveChangesAsync();
            }

            public async Task AddAccountantConnectionAsync(int accountantId, int customerBusinessId, bool active)
            {
                await using var db = CreateDbContext();
                db.MuhasebeciMusterileri.Add(new MuhasebeciMusteri
                {
                    MuhasebeciIsletmeId = accountantId,
                    MusteriIsletmeId = customerBusinessId,
                    Durum = active ? "Aktif" : "Pasif",
                    BaslangicAt = new DateTime(2026, 1, 1),
                    CreatedAt = ReferenceDate,
                    UpdatedAt = ReferenceDate
                });
                await db.SaveChangesAsync();
            }

            private CashTrackerDbContext CreateDbContext() => new(Options);

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
