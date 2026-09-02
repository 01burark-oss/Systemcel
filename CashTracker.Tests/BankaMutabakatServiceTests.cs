using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CashTracker.Tests;

public sealed class BankaMutabakatServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_bank_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly BankaMutabakatService _service;
    private readonly int _tenantA;
    private readonly int _tenantB;

    public BankaMutabakatServiceTests()
    {
        _factory = new Factory(_dbPath);
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        var businesses = new[]
        {
            new Isletme { Ad = "A", TenantTipi = "Isletme", IsAktif = true },
            new Isletme { Ad = "B", TenantTipi = "Isletme" }
        };
        db.Isletmeler.AddRange(businesses);
        db.SaveChanges();
        _tenantA = businesses[0].Id;
        _tenantB = businesses[1].Id;
        _service = new BankaMutabakatService(_factory);
    }

    [Fact]
    public async Task CsvImport_TurkishHeadersAreAcceptedAndMovementHashIsTenantScopedIdempotent()
    {
        const string csv = "Tarih;Açıklama;Borç;Alacak;Para Birimi\n24.08.2026;ABC LTD;;1.250,00;try\n24.08.2026;ABC LTD;;1.250,00;try";

        var first = await ImportAsync(_tenantA, csv);
        var repeated = await ImportAsync(_tenantA, csv);
        var otherTenant = await ImportAsync(_tenantB, csv);

        Assert.Equal(new BankaCsvImportSonucu(1, 1, 2), first);
        Assert.Equal(new BankaCsvImportSonucu(0, 2, 2), repeated);
        Assert.Equal(new BankaCsvImportSonucu(1, 1, 2), otherTenant);
        Assert.Equal(1250m, Assert.Single(await _service.ListeleAsync(_tenantA)).Tutar);
        await using var db = _factory.CreateDbContext();
        Assert.Equal(2, await db.BankaHareketleri.CountAsync());
    }

    [Fact]
    public async Task CandidateLookupAndMatch_AreTenantScopedAndNeverMutateFinancialRecordWithoutExplicitApproval()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.Faturalar.AddRange(new Fatura
            {
                IsletmeId = _tenantA, CariKartId = 0, Tarih = new DateTime(2026, 8, 24),
                FaturaTipi = "Satis", YerelFaturaNo = "F-12", GenelToplam = 1000m,
                Durum = "Kesildi", OdenenTutar = 125m, Aciklama = "ABC LTD"
            }, new Fatura
            {
                IsletmeId = _tenantB, CariKartId = 0, Tarih = new DateTime(2026, 8, 24),
                FaturaTipi = "Satis", YerelFaturaNo = "B-99", GenelToplam = 1000m,
                Durum = "Kesildi", Aciklama = "ABC LTD"
            });
            await db.SaveChangesAsync();
        }
        await ImportAsync(_tenantA, "Tarih;Açıklama;Tutar\n24.08.2026;ABC LTD;1000,00");
        var movement = Assert.Single(await _service.ListeleAsync(_tenantA));
        var candidate = Assert.Single(await _service.AdaylariGetirAsync(_tenantA, movement.Id));
        Assert.Equal(100, candidate.Skor);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AdaylariGetirAsync(_tenantB, movement.Id));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.EslesmeOnaylaAsync(
            _tenantA, movement.Id, new BankaEslesmeIstek(candidate.KaynakTuru, candidate.KaynakId, false)));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.EslesmeOnaylaAsync(
            _tenantB, movement.Id, new BankaEslesmeIstek(candidate.KaynakTuru, candidate.KaynakId, true)));
        int foreignInvoiceId;
        await using (var db = _factory.CreateDbContext())
            foreignInvoiceId = await db.Faturalar.Where(x => x.IsletmeId == _tenantB).Select(x => x.Id).SingleAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => _service.EslesmeOnaylaAsync(
            _tenantA, movement.Id, new BankaEslesmeIstek(BankaEslesmeKaynakTurleri.Fatura, foreignInvoiceId, true)));

        await using (var db = _factory.CreateDbContext())
        {
            var invoice = await db.Faturalar.SingleAsync(x => x.IsletmeId == _tenantA);
            Assert.Equal(125m, invoice.OdenenTutar);
            Assert.Equal("Kesildi", invoice.Durum);
        }

        await _service.EslesmeOnaylaAsync(_tenantA, movement.Id, new BankaEslesmeIstek(candidate.KaynakTuru, candidate.KaynakId, true));
        await using (var db = _factory.CreateDbContext())
        {
            var invoice = await db.Faturalar.SingleAsync(x => x.IsletmeId == _tenantA);
            var bank = await db.BankaHareketleri.SingleAsync();
            Assert.Equal(125m, invoice.OdenenTutar);
            Assert.Equal("Kesildi", invoice.Durum);
            Assert.Equal(BankaHareketDurumlari.Eslesti, bank.Durum);
            Assert.Equal(candidate.KaynakId, bank.EslesenKaynakId);
        }
    }

    [Theory]
    [InlineData("Tarih;Açıklama;Tutar\n24.08.2026;=HYPERLINK(\"https://evil\");10")]
    [InlineData("PK\u0003\u0004fake")]
    public async Task CsvImport_RejectsSpreadsheetFormulaAndNonCsvSignature(string csv)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => ImportAsync(_tenantA, csv));
    }

    [Fact]
    public async Task CsvImport_RejectsOversizedOrInvalidUtf8Input()
    {
        await using var small = new MemoryStream(Encoding.UTF8.GetBytes("Tarih;Açıklama;Tutar"));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CsvIceAktarAsync(
            _tenantA, small, "hareketler.csv", BankaMutabakatService.AzamiDosyaBoyutu + 1));

        var invalidUtf8 = new byte[] { 0x54, 0x61, 0x72, 0x69, 0x68, 0x3B, 0xFF };
        await using var invalid = new MemoryStream(invalidUtf8);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => _service.CsvIceAktarAsync(
            _tenantA, invalid, "hareketler.csv", invalidUtf8.Length));
        Assert.Contains("Bankanızdan hareketleri yeniden indirip tekrar deneyin.", error.Message);
        Assert.DoesNotContain("UTF", error.Message);
    }

    [Fact]
    public void PostgreSqlSnapshot_BankReconciliationMatchesCurrentModel()
    {
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseNpgsql("Host=localhost;Database=systemcel_migration_metadata;Username=test;Password=test")
            .Options;
        using var db = new CashTrackerDbContext(options);
        var assembly = db.GetService<IMigrationsAssembly>();
        var initializer = db.GetService<IModelRuntimeInitializer>();
        var differ = db.GetService<IMigrationsModelDiffer>();
        var snapshot = initializer.Initialize(assembly.ModelSnapshot!.Model, designTime: true);
        var current = db.GetService<IDesignTimeModel>().Model;
        var bankDifferences = differ.GetDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel())
            .Where(x => x switch
            {
                CreateTableOperation table => table.Name == "BankaHareketi",
                DropTableOperation table => table.Name == "BankaHareketi",
                AlterColumnOperation column => column.Table == "BankaHareketi",
                CreateIndexOperation index => index.Table == "BankaHareketi",
                DropIndexOperation index => index.Table == "BankaHareketi",
                AddForeignKeyOperation foreignKey => foreignKey.Table == "BankaHareketi",
                DropForeignKeyOperation foreignKey => foreignKey.Table == "BankaHareketi",
                _ => false
            })
            .ToList();
        Assert.Empty(bankDifferences);
    }

    private async Task<BankaCsvImportSonucu> ImportAsync(int tenant, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        await using var stream = new MemoryStream(bytes);
        return await _service.CsvIceAktarAsync(tenant, stream, "hareketler.csv", bytes.Length);
    }

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    private sealed class Factory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        public Factory(string path) => _options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
        public CashTrackerDbContext CreateDbContext() => new(_options);
        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
