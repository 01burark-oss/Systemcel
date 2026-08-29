using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Systemcel.Api.Import;
using Xunit;

namespace CashTracker.Tests;

public sealed class ExternalDataMigrationTests
{
    [Fact]
    public async Task ParseAsync_AcceptsQuotedSemicolonAndTurkishDecimal()
    {
        using var reader = new StringReader("kayitAnahtari;unvan;tip;acilisBakiyesi\n1;\"Kardeş, Market\";Musteri;1.234,50");

        var result = await MigrationCsvParser.ParseAsync("cari", reader, CancellationToken.None);

        Assert.Single(result.ValidRows);
        Assert.Empty(result.Errors);
        Assert.Equal("Kardeş, Market", result.ValidRows[0].Required("unvan"));
        Assert.Equal(1234.50m, result.ValidRows[0].Decimal("acilisBakiyesi"));
    }

    [Fact]
    public async Task ParseAsync_RejectsDuplicateKeysAndReportsInvalidRows()
    {
        using var reader = new StringReader("kayitAnahtari;ad;tip;acilisStok\naynı;Kalem 1;Urun;2\naynı;Kalem 2;Urun;3\nbozuk;;Urun;x");

        var result = await MigrationCsvParser.ParseAsync("urun", reader, CancellationToken.None);

        Assert.Single(result.ValidRows);
        Assert.Equal(1, result.DuplicateRows);
        Assert.Contains(result.Errors, error => error.Row == 4 && error.Message.Contains("ad", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_RejectsSpreadsheetFormulaContent()
    {
        using var reader = new StringReader("kayitAnahtari;unvan;tip\nkey;=HYPERLINK(\"https://evil.invalid\");Musteri");

        var result = await MigrationCsvParser.ParseAsync("cari", reader, CancellationToken.None);

        Assert.Empty(result.ValidRows);
        Assert.Contains(result.Errors, error => error.Message.Contains("formül", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("-HYPERLINK(\"https://evil.invalid\")")]
    [InlineData("@SUM(1,1)")]
    public async Task ParseAsync_RejectsSpreadsheetFormulaPrefixesInTextFields(string value)
    {
        using var reader = new StringReader($"kayitAnahtari;unvan;tip\nkey;{value};Musteri");

        var result = await MigrationCsvParser.ParseAsync("cari", reader, CancellationToken.None);

        Assert.Empty(result.ValidRows);
        Assert.Contains(result.Errors, error => error.Message.Contains("formül", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_AllowsNegativeNumberInStockQuantity()
    {
        using var reader = new StringReader("kayitAnahtari;ad;barkod;miktar\nstock-1;Örnek Ürün;8690000000000;-2");

        var result = await MigrationCsvParser.ParseAsync("stok", reader, CancellationToken.None);

        Assert.Single(result.ValidRows);
        Assert.Empty(result.Errors);
        Assert.Equal(-2m, result.ValidRows[0].Decimal("miktar"));
    }

    [Fact]
    public async Task ParseAsync_RejectsInvisibleDirectionChangingCharacters()
    {
        using var reader = new StringReader("kayitAnahtari;unvan;tip\nkey;Müşteri\u202Etxt;Musteri");

        var result = await MigrationCsvParser.ParseAsync("cari", reader, CancellationToken.None);

        Assert.Empty(result.ValidRows);
        Assert.Contains(result.Errors, error => error.Message.Contains("Görünmeyen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_ReportsInvalidCategoryTypeBeforeApply()
    {
        using var reader = new StringReader("kayitAnahtari;tip;ad\ncategory-1;Bilinmeyen;Kalem");

        var result = await MigrationCsvParser.ParseAsync("kategori", reader, CancellationToken.None);

        Assert.Empty(result.ValidRows);
        Assert.Contains(result.Errors, error => error.Message.Contains("gelir veya gider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_RejectsUnclosedQuotes()
    {
        using var reader = new StringReader("kayitAnahtari;unvan;tip\nkey;\"Kapanmamış;Musteri");

        var result = await MigrationCsvParser.ParseAsync("cari", reader, CancellationToken.None);

        Assert.Empty(result.ValidRows);
        Assert.Contains(result.Errors, error => error.Message.Contains("tırnak", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_RejectsTooManyRows()
    {
        var content = new StringBuilder("kayitAnahtari;tip;ad\n");
        for (var index = 0; index < 5_001; index++)
            content.Append("key").Append(index).Append(";Gelir;Kalem\n");

        await Assert.ThrowsAsync<MigrationValidationException>(() => MigrationCsvParser.ParseAsync("kategori", new StringReader(content.ToString()), CancellationToken.None));
    }

    [Theory]
    [InlineData("cari")]
    [InlineData("urun")]
    [InlineData("stok")]
    [InlineData("kategori")]
    public async Task Template_IsReadableByParser(string type)
    {
        var result = await MigrationCsvParser.ParseAsync(type, new StringReader(MigrationCsvParser.Template(type)), CancellationToken.None);

        Assert.Single(result.ValidRows);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ApplyAsync_ProductOpeningStock_ForwardsPurchasePriceAsUnitCost()
    {
        await using var fixture = await MigrationFixture.CreateAsync();

        await fixture.PreviewAndApplyAsync("urun", "kayitAnahtari;ad;tip;alisFiyati;acilisStok\nproduct-1;Kablo;Urun;12,50;4");

        var movement = Assert.Single(fixture.Stock.Requests);
        Assert.Equal(4m, movement.Miktar);
        Assert.Equal(12.50m, movement.BirimMaliyet);
    }

    [Fact]
    public async Task ApplyAsync_StockOpeningCost_IsForwardedOnlyWhenSupplied()
    {
        await using var fixture = await MigrationFixture.CreateAsync(new UrunHizmet { Id = 7, Ad = "Kablo", Barkod = "8690000000000", Aktif = true });

        await fixture.PreviewAndApplyAsync("stok", "kayitAnahtari;ad;barkod;miktar;birimMaliyet\nstock-1;Kablo;8690000000000;5;12,50\nstock-2;Kablo;8690000000000;2;");

        Assert.Collection(fixture.Stock.Requests,
            first => Assert.Equal(12.50m, first.BirimMaliyet),
            second => Assert.Null(second.BirimMaliyet));
    }

    private sealed class MigrationFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ExternalDataMigrationService _service;

        private MigrationFixture(SqliteConnection connection, ExternalDataMigrationService service, FakeStokService stock)
        {
            _connection = connection;
            _service = service;
            Stock = stock;
        }

        public FakeStokService Stock { get; }

        public static async Task<MigrationFixture> CreateAsync(params UrunHizmet[] products)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
            var factory = new SingleDbContextFactory(options);
            await using (var db = factory.CreateDbContext())
                await db.Database.EnsureCreatedAsync();

            var business = new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Test" } };
            var stock = new FakeStokService();
            var productsService = new FakeUrunHizmetService(products);
            var service = new ExternalDataMigrationService(
                factory,
                business,
                new CariService(factory, business),
                productsService,
                stock,
                new FakeKalemTanimiService());

            return new MigrationFixture(connection, service, stock);
        }

        public async Task PreviewAndApplyAsync(string type, string csv)
        {
            var bytes = Encoding.UTF8.GetBytes(csv);
            var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "aktarim.csv");
            var preview = await _service.PreviewAsync(type, file, CancellationToken.None);
            Assert.Empty(preview.Errors);
            await _service.ApplyAsync(preview.DraftId, CancellationToken.None);
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}
