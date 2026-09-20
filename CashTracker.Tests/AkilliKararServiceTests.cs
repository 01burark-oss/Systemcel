using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class AkilliKararServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private CashTrackerDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(_connection).Options;
        _db = new CashTrackerDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        _db.Isletmeler.AddRange(
            new Isletme { Id = 1, Ad = "İşletme 1", IsAktif = true },
            new Isletme { Id = 2, Ad = "İşletme 2", IsAktif = true });
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task ConfirmedSupplierAlias_IsTenantScopedAndReusedWithoutProvider()
    {
        var product = new UrunHizmet { IsletmeId = 1, Ad = "Kablo", Birim = "Adet", AlisFiyati = 10 };
        var otherProduct = new UrunHizmet { IsletmeId = 2, Ad = "Kablo B", Birim = "Adet", AlisFiyati = 20 };
        await _db.UrunHizmetleri.AddRangeAsync(product, otherProduct);
        await _db.SaveChangesAsync();
        var fake = new FakeJev((key, _) => key == "urun" ? Choice("urun_" + product.Id, .9) : JevChoiceResult.Unavailable);
        var service = CreateService(fake);
        await service.UrunEslesmesiniOnaylaAsync(1, new UrunEslesmeOnayIstek { KaynakMetin = "Kablo tedarikçi", UrunHizmetId = product.Id });

        var reused = await service.UrunEsleAsync(1, new UrunEslesmeIstek { KaynakMetin = "Kablo tedarikçi", Birim = "Adet" });
        var foreign = await service.UrunEsleAsync(2, new UrunEslesmeIstek { KaynakMetin = "Kablo tedarikçi", Birim = "Adet" });

        Assert.True(reused.KayitliEslesme);
        Assert.Equal(product.Id, reused.UrunHizmetId);
        Assert.Equal(1, fake.Calls);
        Assert.Null(foreign.UrunHizmetId);
        Assert.NotEqual(otherProduct.Id, foreign.UrunHizmetId);
    }

    [Fact]
    public async Task ProductSuggestion_CarriesDeterministicUnitAndCostWarnings()
    {
        var product = new UrunHizmet { IsletmeId = 1, Ad = "Kablo", Birim = "Adet", AlisFiyati = 10 };
        _db.UrunHizmetleri.Add(product);
        await _db.SaveChangesAsync();
        var service = CreateService(new FakeJev((key, _) => Choice($"urun_{product.Id}", .9)));

        var result = await service.UrunEsleAsync(1, new UrunEslesmeIstek
        {
            KaynakMetin = "Kablo",
            Birim = "Kutu",
            BirimFiyat = 20
        });

        Assert.Contains("birim Kutu", result.BirimUyarisi);
        Assert.Contains("%100", result.MaliyetUyarisi);
    }

    [Fact]
    public async Task InvoiceDuplicateResult_IdentifiesExistingInvoiceWithoutMutation()
    {
        var cari = new CariKart { IsletmeId = 1, Tip = "Tedarikci", Unvan = "ABC" };
        _db.CariKartlari.Add(cari);
        await _db.SaveChangesAsync();
        var invoice = new Fatura { IsletmeId = 1, CariKartId = cari.Id, Tarih = DateTime.Today, GenelToplam = 100, Durum = "Kesildi", YerelFaturaNo = "A-1", Aciklama = "Kablo" };
        _db.Faturalar.Add(invoice);
        await _db.SaveChangesAsync();
        var service = CreateService(new FakeJev((key, _) => Choice($"ayni_{invoice.Id}", .88)));

        var result = await service.FaturaKontrolEtAsync(1, new FaturaKontrolIstek { CariKartId = cari.Id, Tarih = DateTime.Today, GenelToplam = 100, Aciklama = "Kablo" });

        Assert.Equal("ayni_" + invoice.Id, result.Iliski);
        Assert.Equal(invoice.Id, result.IliskiliFaturaId);
        Assert.Equal(100, (await _db.Faturalar.SingleAsync()).GenelToplam);
    }

    [Fact]
    public async Task InvoiceRelation_LowConfidenceFallsBackToReview()
    {
        var cari = new CariKart { IsletmeId = 1, Tip = "Tedarikci", Unvan = "ABC" };
        _db.CariKartlari.Add(cari);
        await _db.SaveChangesAsync();
        var invoice = new Fatura { IsletmeId = 1, CariKartId = cari.Id, Tarih = DateTime.Today, GenelToplam = 100, Durum = "Kesildi", YerelFaturaNo = "A-1" };
        _db.Faturalar.Add(invoice);
        await _db.SaveChangesAsync();
        var service = CreateService(new FakeJev((key, _) => Choice($"ayni_{invoice.Id}", .31)));

        var result = await service.FaturaKontrolEtAsync(1, new FaturaKontrolIstek
        {
            CariKartId = cari.Id,
            Tarih = DateTime.Today,
            GenelToplam = 100
        });

        Assert.Equal("incele", result.Iliski);
        Assert.Null(result.IliskiliFaturaId);
    }

    [Fact]
    public async Task DailyTasks_ReturnAtMostThreeAndGroupOpenBankMovements()
    {
        _db.Faturalar.AddRange(
            new Fatura { IsletmeId = 1, CariKartId = 1, Tarih = DateTime.Today.AddDays(-3), VadeTarihi = DateTime.Today.AddDays(-1), GenelToplam = 10, Durum = "Kesildi", YerelFaturaNo = "A" },
            new Fatura { IsletmeId = 1, CariKartId = 1, Tarih = DateTime.Today.AddDays(-4), VadeTarihi = DateTime.Today.AddDays(-2), GenelToplam = 20, Durum = "Kesildi", YerelFaturaNo = "B" },
            new Fatura { IsletmeId = 1, CariKartId = 1, Tarih = DateTime.Today.AddDays(-5), VadeTarihi = DateTime.Today.AddDays(-3), GenelToplam = 30, Durum = "Kesildi", YerelFaturaNo = "C" });
        _db.BankaHareketleri.AddRange(
            new BankaHareketi { IsletmeId = 1, Tarih = DateTime.Today, Tutar = 1, KaynakHash = "banka-1" },
            new BankaHareketi { IsletmeId = 1, Tarih = DateTime.Today, Tutar = 2, KaynakHash = "banka-2" });
        await _db.SaveChangesAsync();

        var tasks = await CreateService(new FakeJev((_, _) => JevChoiceResult.Unavailable)).BugununIsleriniGetirAsync(1);

        Assert.True(tasks.Count <= 3);
        Assert.Equal(1, tasks.Count(x => x.Kimlik == "banka:acik"));
        Assert.Contains("2 hareket", tasks.Single(x => x.Kimlik == "banka:acik").Metrik);
    }

    [Fact]
    public async Task ReceiptEnrichment_MapsCategorySupplierAndRelatedRecord()
    {
        var supplier = new CariKart { IsletmeId = 1, Tip = "Tedarikci", Unvan = "ABC" };
        _db.CariKartlari.Add(supplier);
        await _db.SaveChangesAsync();
        var cash = new Kasa { IsletmeId = 1, Tip = "Gider", Tarih = DateTime.Today, Tutar = 42, Kalem = "Ofis", Aciklama = "ABC" };
        _db.Kasalar.Add(cash);
        await _db.SaveChangesAsync();
        var fake = new FakeJev((key, _) => key switch
        {
            "kalem_0" => Choice("Kırtasiye", .9),
            "cari" => Choice("cari_" + supplier.Id, .91),
            "kayit" => Choice("kayit_" + cash.Id, .92),
            _ => JevChoiceResult.Unavailable
        });
        var result = await CreateService(fake).FisiZenginlestirAsync(1, new ReceiptOcrResult
        {
            Merchant = "ABC", ReceiptDate = DateTime.Today, ReceiptTotal = 42,
            Items = [new ReceiptOcrLineItem { RawName = "Defter", Amount = 42 }]
        }, ["Kırtasiye", "Ofis"]);

        Assert.Equal("Kırtasiye", result.Items[0].CandidateKalem);
        Assert.Equal(supplier.Id, result.SuggestedCariId);
        Assert.Equal("kayit_" + cash.Id, result.ExistingRecordRelation);
        Assert.Equal(cash.Id, result.RelatedRecordId);
    }

    private AkilliKararService CreateService(IJevDecisionService jev) => new(
        new CashTracker.Tests.Support.SingleDbContextFactory(_db.Database.GetDbConnection() is SqliteConnection
            ? new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(_connection).Options
            : throw new InvalidOperationException()), jev, new JevSettings { ApiKey = "configured" });

    private static JevChoiceResult Choice(string value, double confidence) => new(
        true,
        value,
        confidence,
        new Dictionary<string, double> { [value] = confidence },
        "test");

    private sealed class FakeJev(Func<string, IReadOnlyDictionary<string, JevChoiceQuestion>, JevChoiceResult> choose) : IJevDecisionService
    {
        public int Calls { get; private set; }
        public bool IsConfigured => true;
        public Task<IReadOnlyDictionary<string, JevChoiceResult>> ChooseAsync(object state, IReadOnlyDictionary<string, JevChoiceQuestion> questions, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyDictionary<string, JevChoiceResult>>(questions.ToDictionary(x => x.Key, x => choose(x.Key, questions), StringComparer.Ordinal));
        }
    }
}
