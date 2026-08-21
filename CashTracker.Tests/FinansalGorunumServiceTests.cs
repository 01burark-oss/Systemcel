using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class FinansalGorunumServiceTests
{
    private static readonly DateTime ReferenceDate = new(2026, 9, 30);

    [Fact]
    public async Task GetAsync_ReconstructsHistoricalAgingAtEveryBoundary()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var customerId = await fixture.AddCustomerAsync(1, "Sınır Müşterisi");

        await fixture.AddInvoiceAsync(1, customerId, 100m, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));
        await fixture.AddInvoiceAsync(1, customerId, 200m, new DateTime(2026, 9, 1), ReferenceDate);

        var historicallyPartial = await fixture.AddInvoiceAsync(
            1,
            customerId,
            500m,
            new DateTime(2026, 8, 1),
            new DateTime(2026, 9, 29),
            durum: FaturaDurum.Odendi,
            odenenTutar: 500m);
        await fixture.AddCollectionAsync(1, customerId, historicallyPartial, 200m, ReferenceDate);
        await fixture.AddCollectionAsync(1, customerId, historicallyPartial, 300m, ReferenceDate.AddDays(1));

        await fixture.AddInvoiceAsync(1, customerId, 400m, new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));
        await fixture.AddInvoiceAsync(1, customerId, 500m, new DateTime(2026, 8, 1), new DateTime(2026, 8, 30));
        await fixture.AddInvoiceAsync(1, customerId, 600m, new DateTime(2026, 7, 1), new DateTime(2026, 8, 1));
        await fixture.AddInvoiceAsync(1, customerId, 700m, new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));
        await fixture.AddInvoiceAsync(1, customerId, 800m, new DateTime(2026, 7, 1), new DateTime(2026, 7, 2));
        await fixture.AddInvoiceAsync(1, customerId, 900m, new DateTime(2026, 6, 1), new DateTime(2026, 7, 1));

        await fixture.AddInvoiceAsync(1, customerId, 10_000m, ReferenceDate.AddDays(1), new DateTime(2026, 6, 1));
        await fixture.AddInvoiceAsync(1, customerId, 20_000m, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1), durum: FaturaDurum.YerelTaslak);
        await fixture.AddInvoiceAsync(1, customerId, 30_000m, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1), durum: FaturaDurum.Iptal);
        await fixture.AddInvoiceAsync(1, customerId, 40_000m, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1), faturaTipi: "Alis");

        var result = await fixture.Service.GetAsync(ReferenceDate);

        Assert.Equal(4_500m, result.AcikAlacakToplami);
        Assert.Equal(4_200m, result.VadesiGecmisAlacakToplami);
        Assert.Collection(
            result.Yaslandirma,
            row => AssertAging(row, "VadesiGelmedi", 300m, 2, 6.7m),
            row => AssertAging(row, "Gun1_30", 700m, 2, 15.6m),
            row => AssertAging(row, "Gun31_60", 1_100m, 2, 24.4m),
            row => AssertAging(row, "Gun61_90", 1_500m, 2, 33.3m),
            row => AssertAging(row, "Gun91Uzeri", 900m, 1, 20m));
        Assert.Equal(100m, result.Yaslandirma.Sum(x => x.Oran));
        var customerAging = Assert.Single(result.CariYaslandirma);
        Assert.Equal(customerId, customerAging.CariKartId);
        Assert.Equal(4_500m, customerAging.Toplam);
        Assert.Equal(300m, customerAging.VadesiGelmemis);
        Assert.Equal(700m, customerAging.Gun1Ila30);
        Assert.Equal(1_100m, customerAging.Gun31Ila60);
        Assert.Equal(1_500m, customerAging.Gun61Ila90);
        Assert.Equal(900m, customerAging.Gun91VeUzeri);
        Assert.Equal(9, customerAging.AcikFaturaAdedi);
        Assert.Equal(91, customerAging.EnUzunGecikmeGunu);
        Assert.Equal(100m, customerAging.ToplamdakiOrani);
    }

    [Fact]
    public async Task GetAsync_UsesInvoiceDateWhenDueDateIsMissing()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var customerId = await fixture.AddCustomerAsync(1, "Vadesiz Müşteri");
        await fixture.AddInvoiceAsync(
            1,
            customerId,
            250m,
            ReferenceDate.AddDays(-31),
            dueDate: null);

        var result = await fixture.Service.GetAsync(ReferenceDate);

        var bucket = Assert.Single(result.Yaslandirma, x => x.Kod == "Gun31_60");
        Assert.Equal(250m, bucket.Tutar);
        Assert.Equal(1, bucket.FaturaAdedi);
    }

    [Fact]
    public async Task GetAsync_ClassifiesPaymentRhythmAtSevenDayThresholds()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var worseningId = await fixture.AddCustomerAsync(1, "Kötüleşen");
        var improvingId = await fixture.AddCustomerAsync(1, "İyileşen");
        var stableId = await fixture.AddCustomerAsync(1, "Dengeli");
        var insufficientId = await fixture.AddCustomerAsync(1, "Yeni Müşteri");

        await fixture.AddOpenReceivableAsync(1, worseningId, 100m, ReferenceDate.AddDays(-10));
        await fixture.AddOpenReceivableAsync(1, improvingId, 100m, ReferenceDate.AddDays(-10));
        await fixture.AddOpenReceivableAsync(1, stableId, 100m, ReferenceDate.AddDays(-10));
        await fixture.AddOpenReceivableAsync(1, insufficientId, 100m, ReferenceDate.AddDays(-10));

        await fixture.AddPaymentHistoryAsync(1, worseningId, [8, 9, 10], [0, 1, 2]);
        await fixture.AddPaymentHistoryAsync(1, improvingId, [0, 1, 2], [8, 9, 10]);
        await fixture.AddPaymentHistoryAsync(1, stableId, [5, 6, 7], [0, 1, 2]);
        await fixture.AddSettledInvoiceAsync(1, insufficientId, new DateTime(2026, 9, 20), delayDays: 4);

        var result = await fixture.Service.GetAsync(ReferenceDate);

        var worsening = result.CariRiskleri.Single(x => x.CariKartId == worseningId);
        Assert.Equal(6, worsening.TamamlananOdemeAdedi);
        Assert.Equal(8m, worsening.SonDonemDegisimiGunu);
        Assert.Equal("Kotulesiyor", worsening.RitimDurumu);

        var improving = result.CariRiskleri.Single(x => x.CariKartId == improvingId);
        Assert.Equal(-8m, improving.SonDonemDegisimiGunu);
        Assert.Equal("Iyilesiyor", improving.RitimDurumu);

        var stable = result.CariRiskleri.Single(x => x.CariKartId == stableId);
        Assert.Equal(5m, stable.SonDonemDegisimiGunu);
        Assert.Equal("Dengeli", stable.RitimDurumu);

        var insufficient = result.CariRiskleri.Single(x => x.CariKartId == insufficientId);
        Assert.Equal(1, insufficient.TamamlananOdemeAdedi);
        Assert.Null(insufficient.SonDonemDegisimiGunu);
        Assert.Equal("YetersizVeri", insufficient.RitimDurumu);
    }

    [Fact]
    public async Task GetAsync_UsesThePaymentThatActuallySettlesASplitInvoice()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var customerId = await fixture.AddCustomerAsync(1, "Parçalı Ödeme");
        await fixture.AddOpenReceivableAsync(1, customerId, 100m, ReferenceDate.AddDays(-10));
        var invoiceId = await fixture.AddInvoiceAsync(
            1,
            customerId,
            100m,
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 31),
            durum: FaturaDurum.Odendi,
            odenenTutar: 100m);
        await fixture.AddCollectionAsync(1, customerId, invoiceId, 40m, new DateTime(2026, 8, 30));
        await fixture.AddCollectionAsync(1, customerId, invoiceId, 60m, new DateTime(2026, 9, 10));

        var result = await fixture.Service.GetAsync(ReferenceDate);

        var rhythm = result.CariRiskleri.Single(x => x.CariKartId == customerId);
        Assert.Equal(10m, rhythm.OrtalamaOdemeSapmasiGunu);
        Assert.Equal(1, rhythm.TamamlananOdemeAdedi);
    }

    [Fact]
    public async Task GetAsync_CalculatesConcentrationAndHandlesNoReceivables()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var first = await fixture.AddCustomerAsync(1, "A");
        var second = await fixture.AddCustomerAsync(1, "B");
        var third = await fixture.AddCustomerAsync(1, "C");
        await fixture.AddOpenReceivableAsync(1, first, 600m, ReferenceDate.AddDays(1));
        await fixture.AddOpenReceivableAsync(1, second, 300m, ReferenceDate.AddDays(1));
        await fixture.AddOpenReceivableAsync(1, third, 100m, ReferenceDate.AddDays(1));

        var concentrated = await fixture.Service.GetAsync(ReferenceDate);

        Assert.Equal(60m, concentrated.Yogunlasma.EnBuyukCariOrani);
        Assert.Equal(100m, concentrated.Yogunlasma.IlkUcCariOrani);
        Assert.Equal(100m, concentrated.Yogunlasma.IlkBesCariOrani);
        Assert.Equal(4_600m, concentrated.Yogunlasma.Hhi);
        Assert.Equal("Yuksek", concentrated.Yogunlasma.RiskSeviyesi);

        fixture.Isletme.Active = new Isletme { Id = 2, Ad = "Boş İşletme", IsAktif = true };
        var empty = await fixture.Service.GetAsync(ReferenceDate);
        Assert.Equal(0m, empty.Yogunlasma.EnBuyukCariOrani);
        Assert.Equal(0m, empty.Yogunlasma.IlkUcCariOrani);
        Assert.Equal(0m, empty.Yogunlasma.IlkBesCariOrani);
        Assert.Equal(0m, empty.Yogunlasma.Hhi);
        Assert.Equal("VeriYok", empty.Yogunlasma.RiskSeviyesi);
    }

    [Fact]
    public async Task GetAsync_ReportsLowHhiForEvenlyDistributedReceivables()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        for (var index = 1; index <= 10; index++)
        {
            var customerId = await fixture.AddCustomerAsync(1, $"Müşteri {index:00}");
            await fixture.AddOpenReceivableAsync(1, customerId, 100m, ReferenceDate.AddDays(1));
        }

        var result = await fixture.Service.GetAsync(ReferenceDate);

        Assert.Equal(10m, result.Yogunlasma.EnBuyukCariOrani);
        Assert.Equal(30m, result.Yogunlasma.IlkUcCariOrani);
        Assert.Equal(50m, result.Yogunlasma.IlkBesCariOrani);
        Assert.Equal(1_000m, result.Yogunlasma.Hhi);
        Assert.Equal("Dusuk", result.Yogunlasma.RiskSeviyesi);
    }

    [Fact]
    public async Task GetAsync_BuildsThirteenContiguousProjectionWeeksWithPlannedCash()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var customerId = await fixture.AddCustomerAsync(1, "Projeksiyon Carisi");
        await fixture.AddCashAsync(1, "Gelir", 3_000m, ReferenceDate);
        await fixture.AddCashAsync(1, "Gider", 500m, ReferenceDate);
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Planlanan gelir",
            Tip = "Gelir",
            Tutar = 200m,
            IlkTarih = new DateTime(2026, 10, 2),
            TekrarTipi = "TekSefer"
        });
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Planlanan gider",
            Tip = "Gider",
            Tutar = 100m,
            IlkTarih = new DateTime(2026, 10, 3),
            TekrarTipi = "TekSefer"
        });
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Büyük planlanan gider",
            Tip = "Gider",
            Tutar = 3_000m,
            IlkTarih = new DateTime(2026, 10, 15),
            TekrarTipi = "TekSefer"
        });

        await fixture.AddOpenReceivableAsync(1, customerId, 500m, ReferenceDate.AddDays(-5));
        await fixture.AddOpenReceivableAsync(1, customerId, 700m, new DateTime(2026, 10, 7));
        await fixture.AddInvoiceAsync(1, customerId, 1_000m, new DateTime(2026, 9, 1), new DateTime(2026, 10, 8), faturaTipi: "Alis");
        await fixture.AddInvoiceAsync(1, customerId, 100m, new DateTime(2026, 9, 1), new DateTime(2026, 12, 30));
        await fixture.AddInvoiceAsync(1, customerId, 999m, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31));

        var result = await fixture.Service.GetAsync(ReferenceDate, 13);

        Assert.Equal(2_500m, result.KasaBakiyesi);
        Assert.Equal(13, result.NakitProjeksiyonu.Count);
        Assert.Equal(new DateTime(2026, 10, 1), result.NakitProjeksiyonu[0].Baslangic);
        Assert.Equal(new DateTime(2026, 12, 30), result.NakitProjeksiyonu[^1].Bitis);
        for (var index = 1; index < result.NakitProjeksiyonu.Count; index++)
        {
            Assert.Equal(result.NakitProjeksiyonu[index - 1].Bitis.AddDays(1), result.NakitProjeksiyonu[index].Baslangic);
            Assert.Equal(result.NakitProjeksiyonu[index - 1].KapanisBakiyesi, result.NakitProjeksiyonu[index].AcilisBakiyesi);
        }

        var weekOne = result.NakitProjeksiyonu[0];
        Assert.Equal(1_200m, weekOne.BeklenenTahsilat);
        Assert.Equal(200m, weekOne.PlanlananGelir);
        Assert.Equal(0m, weekOne.BeklenenOdeme);
        Assert.Equal(100m, weekOne.PlanlananGider);
        Assert.Equal(3_800m, weekOne.KapanisBakiyesi);

        var weekTwo = result.NakitProjeksiyonu[1];
        Assert.Equal(1_000m, weekTwo.BeklenenOdeme);
        Assert.Equal(2_800m, weekTwo.KapanisBakiyesi);

        var weekThree = result.NakitProjeksiyonu[2];
        Assert.Equal(3_000m, weekThree.PlanlananGider);
        Assert.Equal(-200m, weekThree.KapanisBakiyesi);
        Assert.Equal(3, result.IlkNegatifHafta);

        var weekThirteen = result.NakitProjeksiyonu[^1];
        Assert.Equal(100m, weekThirteen.BeklenenTahsilat);
        Assert.DoesNotContain(result.NakitProjeksiyonu, x => x.BeklenenTahsilat == 999m);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(13, 13)]
    [InlineData(20, 13)]
    public async Task GetAsync_ClampsProjectionWeekCount(int requested, int expected)
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();

        var result = await fixture.Service.GetAsync(ReferenceDate, requested);

        Assert.Equal(expected, result.NakitProjeksiyonu.Count);
    }

    [Fact]
    public async Task GetAsync_ExpandsOneOffWeeklyAndMonthlyPlanItems()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Beklenen destek",
            Tip = "Gelir",
            Tutar = 200m,
            IlkTarih = new DateTime(2026, 10, 2),
            TekrarTipi = "TekSefer",
            Kategori = "Diğer"
        });
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Haftalık sabit gider",
            Tip = "Gider",
            Tutar = 100m,
            IlkTarih = new DateTime(2026, 10, 3),
            TekrarTipi = "Haftalik",
            TekrarAraligi = 1,
            Kategori = "Sabit gider"
        });
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Vergi / SGK",
            Tip = "Gider",
            Tutar = 300m,
            IlkTarih = new DateTime(2026, 10, 5),
            TekrarTipi = "Aylik",
            TekrarAraligi = 1,
            Kategori = "Vergi"
        });
        await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "Pasif kalem",
            Tip = "Gider",
            Tutar = 9_999m,
            IlkTarih = new DateTime(2026, 10, 4),
            TekrarTipi = "TekSefer",
            Aktif = false
        });

        var result = await fixture.Service.GetAsync(ReferenceDate);

        Assert.Equal(200m, result.NakitProjeksiyonu[0].PlanlananGelir);
        Assert.Equal(400m, result.NakitProjeksiyonu[0].PlanlananGider);
        Assert.Equal(100m, result.NakitProjeksiyonu[1].PlanlananGider);
        Assert.Equal(400m, result.NakitProjeksiyonu[5].PlanlananGider);
        Assert.Equal(400m, result.NakitProjeksiyonu[9].PlanlananGider);
        Assert.DoesNotContain(result.NakitProjeksiyonu, x => x.PlanlananGider >= 9_999m);
    }

    [Fact]
    public async Task PlanItemMutations_AreTenantScoped()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var id = await fixture.Service.CreatePlanItemAsync(new NakitPlanKalemiKaydetRequest
        {
            Ad = "  Kira  ",
            Tip = "Gider",
            Tutar = 1_000m,
            IlkTarih = new DateTime(2026, 10, 1),
            TekrarTipi = "Aylik",
            TekrarAraligi = 1,
            Kategori = "  Sabit  "
        });

        var tenantOneRows = await fixture.Service.GetPlanItemsAsync();
        var created = Assert.Single(tenantOneRows);
        Assert.Equal(id, created.Id);
        Assert.Equal(1, created.IsletmeId);
        Assert.Equal("Kira", created.Ad);
        Assert.Equal("Sabit", created.Kategori);

        fixture.Isletme.Active = new Isletme { Id = 2, Ad = "İkinci", IsAktif = true };
        Assert.Empty(await fixture.Service.GetPlanItemsAsync());
        Assert.False(await fixture.Service.UpdatePlanItemAsync(id, new NakitPlanKalemiKaydetRequest
        {
            Ad = "Başkasının kirası",
            Tip = "Gider",
            Tutar = 2_000m,
            IlkTarih = new DateTime(2026, 10, 1),
            TekrarTipi = "Aylik"
        }));
        Assert.False(await fixture.Service.DeletePlanItemAsync(id));

        fixture.Isletme.Active = new Isletme { Id = 1, Ad = "Birinci", IsAktif = true };
        Assert.True(await fixture.Service.DeletePlanItemAsync(id));
        Assert.Empty(await fixture.Service.GetPlanItemsAsync());
    }

    [Fact]
    public async Task GetAsync_IsolatesEveryFinancialSectionByActiveBusiness()
    {
        using var fixture = await FinansalGorunumFixture.CreateAsync();
        var tenantOneCustomer = await fixture.AddCustomerAsync(1, "Birinci Kiracı");
        var tenantTwoCustomer = await fixture.AddCustomerAsync(2, "İkinci Kiracı");
        await fixture.AddOpenReceivableAsync(1, tenantOneCustomer, 100m, ReferenceDate.AddDays(-1));
        await fixture.AddOpenReceivableAsync(2, tenantTwoCustomer, 999_999m, ReferenceDate.AddDays(-91));
        await fixture.AddCashAsync(1, "Gelir", 1_000m, ReferenceDate);
        await fixture.AddCashAsync(2, "Gelir", 999_999m, ReferenceDate);

        var tenantOne = await fixture.Service.GetAsync(ReferenceDate);

        Assert.Equal(100m, tenantOne.AcikAlacakToplami);
        Assert.Equal(1_000m, tenantOne.KasaBakiyesi);
        var tenantOneRisk = Assert.Single(tenantOne.CariRiskleri);
        Assert.Equal("Birinci Kiracı", tenantOneRisk.Unvan);
        Assert.DoesNotContain(tenantOne.CariRiskleri, x => x.CariKartId == tenantTwoCustomer);

        fixture.Isletme.Active = new Isletme { Id = 2, Ad = "İkinci", IsAktif = true };
        var tenantTwo = await fixture.Service.GetAsync(ReferenceDate);

        Assert.Equal(999_999m, tenantTwo.AcikAlacakToplami);
        Assert.Equal(999_999m, tenantTwo.KasaBakiyesi);
        var tenantTwoRisk = Assert.Single(tenantTwo.CariRiskleri);
        Assert.Equal("İkinci Kiracı", tenantTwoRisk.Unvan);
        Assert.DoesNotContain(tenantTwo.CariRiskleri, x => x.CariKartId == tenantOneCustomer);
    }

    private static void AssertAging(
        AlacakYaslandirmaDilimi actual,
        string code,
        decimal amount,
        int count,
        decimal ratio)
    {
        Assert.Equal(code, actual.Kod);
        Assert.Equal(amount, actual.Tutar);
        Assert.Equal(count, actual.FaturaAdedi);
        Assert.Equal(ratio, actual.Oran);
    }

    private sealed class FinansalGorunumFixture : IDisposable
    {
        private FinansalGorunumFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
        {
            DbPath = dbPath;
            Options = options;
            Factory = new SingleDbContextFactory(options);
            Isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 1, Ad = "Birinci", IsAktif = true }
            };
            Service = new FinansalGorunumService(Factory, Isletme);
        }

        public string DbPath { get; }
        public DbContextOptions<CashTrackerDbContext> Options { get; }
        public SingleDbContextFactory Factory { get; }
        public FakeIsletmeService Isletme { get; }
        public FinansalGorunumService Service { get; }

        public static async Task<FinansalGorunumFixture> CreateAsync()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_finansal_gorunum_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var fixture = new FinansalGorunumFixture(dbPath, options);

            await using var db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.AddRange(
                new Isletme { Id = 1, Ad = "Birinci", IsAktif = true },
                new Isletme { Id = 2, Ad = "İkinci", IsAktif = false });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async Task<int> AddCustomerAsync(int businessId, string name)
        {
            await using var db = CreateDbContext();
            var customer = new CariKart
            {
                IsletmeId = businessId,
                Tip = "Musteri",
                Unvan = name,
                CreatedAt = ReferenceDate.AddYears(-1),
                UpdatedAt = ReferenceDate.AddYears(-1)
            };
            db.CariKartlari.Add(customer);
            await db.SaveChangesAsync();
            return customer.Id;
        }

        public async Task<int> AddInvoiceAsync(
            int businessId,
            int customerId,
            decimal amount,
            DateTime invoiceDate,
            DateTime? dueDate,
            string durum = FaturaDurum.Kesildi,
            decimal odenenTutar = 0m,
            string faturaTipi = "Satis")
        {
            await using var db = CreateDbContext();
            var invoice = new Fatura
            {
                IsletmeId = businessId,
                CariKartId = customerId,
                Tarih = invoiceDate,
                VadeTarihi = dueDate,
                FaturaTipi = faturaTipi,
                Durum = durum,
                YerelFaturaNo = $"TEST-{Guid.NewGuid():N}",
                GenelToplam = amount,
                OdenenTutar = odenenTutar,
                CreatedAt = invoiceDate,
                UpdatedAt = invoiceDate
            };
            db.Faturalar.Add(invoice);
            await db.SaveChangesAsync();
            return invoice.Id;
        }

        public Task<int> AddOpenReceivableAsync(int businessId, int customerId, decimal amount, DateTime dueDate)
        {
            return AddInvoiceAsync(
                businessId,
                customerId,
                amount,
                dueDate.AddDays(-30),
                dueDate);
        }

        public async Task AddCollectionAsync(
            int businessId,
            int customerId,
            int invoiceId,
            decimal amount,
            DateTime date)
        {
            await using var db = CreateDbContext();
            db.TahsilatOdemeleri.Add(new TahsilatOdeme
            {
                IsletmeId = businessId,
                FaturaId = invoiceId,
                CariKartId = customerId,
                Tarih = date,
                Tip = "Tahsilat",
                Tutar = amount,
                OdemeYontemi = "Nakit",
                CreatedAt = date
            });
            await db.SaveChangesAsync();
        }

        public async Task AddPaymentHistoryAsync(
            int businessId,
            int customerId,
            IReadOnlyList<int> recentDelays,
            IReadOnlyList<int> previousDelays)
        {
            var paidDates = new[]
            {
                new DateTime(2026, 9, 20),
                new DateTime(2026, 9, 10),
                new DateTime(2026, 8, 31),
                new DateTime(2026, 8, 20),
                new DateTime(2026, 8, 10),
                new DateTime(2026, 7, 31)
            };
            var delays = recentDelays.Concat(previousDelays).ToArray();
            for (var index = 0; index < delays.Length; index++)
                await AddSettledInvoiceAsync(businessId, customerId, paidDates[index], delays[index]);
        }

        public async Task AddSettledInvoiceAsync(
            int businessId,
            int customerId,
            DateTime paidAt,
            int delayDays)
        {
            var dueDate = paidAt.AddDays(-delayDays);
            var invoiceId = await AddInvoiceAsync(
                businessId,
                customerId,
                100m,
                dueDate.AddDays(-30),
                dueDate,
                durum: FaturaDurum.Odendi,
                odenenTutar: 100m);
            await AddCollectionAsync(businessId, customerId, invoiceId, 100m, paidAt);
        }

        public async Task AddCashAsync(int businessId, string type, decimal amount, DateTime date)
        {
            await using var db = CreateDbContext();
            db.Kasalar.Add(new Kasa
            {
                IsletmeId = businessId,
                Tip = type,
                Tutar = amount,
                Tarih = date,
                OdemeYontemi = "Nakit",
                CreatedAt = date
            });
            await db.SaveChangesAsync();
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
}
