using System.Data.Common;
using System.Diagnostics;
using CashTracker.Core.Entities;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CashTracker.Tests;

public sealed class StokServicePerformanceTests
{
    [Fact]
    [Trait("Category", "Performance")]
    public async Task GetCurrentStockAsync_AggregatesOneHundredThousandRowsInDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_stock_perf_{Guid.NewGuid():N}.db");
        var sqlCapture = new AggregateSqlCapture();
        try
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .AddInterceptors(sqlCapture)
                .Options;

            await using (var db = new CashTrackerDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.Database.ExecuteSqlRawAsync("""
                    WITH digits(d) AS (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)),
                    numbers(n) AS (
                        SELECT d0.d + d1.d * 10 + d2.d * 100 + d3.d * 1000 + d4.d * 10000
                        FROM digits d0 CROSS JOIN digits d1 CROSS JOIN digits d2 CROSS JOIN digits d3 CROSS JOIN digits d4
                    )
                    INSERT INTO "StokHareket" (
                        "IsletmeId", "UrunHizmetId", "Tarih", "Miktar", "RezerveMiktar", "BirimMaliyet",
                        "MaliyetParaBirimi", "MaliyetKurSnapshot", "BirimMaliyetTry", "HareketTipi", "Kaynak", "CreatedAt")
                    SELECT CASE WHEN n % 2 = 0 THEN 1 ELSE 2 END, 42, CURRENT_TIMESTAMP, 0.001, 0, 0,
                        'TRY', 1, 0, 'Giris', 'Performance fixture', CURRENT_TIMESTAMP
                    FROM numbers;
                    """);
            }

            var service = new StokService(
                new SingleDbContextFactory(options),
                new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Performance fixture", IsAktif = true } });

            var stopwatch = Stopwatch.StartNew();
            var currentStock = await service.GetCurrentStockAsync(42);
            stopwatch.Stop();

            Assert.Equal(50m, currentStock);
            Assert.Contains(sqlCapture.Commands, command =>
                command.Contains("SUM(", StringComparison.OrdinalIgnoreCase) &&
                command.Contains("StokHareket", StringComparison.OrdinalIgnoreCase));
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3),
                $"100k movement stock aggregate took {stopwatch.Elapsed.TotalMilliseconds:F0} ms.");

            Console.WriteLine($"PERF|Scenario=StockCurrentBalance|rows=100000|elapsed_ms={stopwatch.Elapsed.TotalMilliseconds:F2}");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    private sealed class AggregateSqlCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
