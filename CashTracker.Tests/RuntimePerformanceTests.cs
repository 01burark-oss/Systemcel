using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
    public sealed class RuntimePerformanceTests
    {
        private const string PerfFlag = "CASHTRACKER_RUN_PERF";

        [Fact]
        [Trait("Category", "Performance")]
        public async Task TelegramSummaryCommand_Benchmark()
        {
            if (!IsPerformanceRunEnabled())
                return;

            var rows = GenerateRows(25_000);
            var summary = BuildSummary(rows);
            var kasa = new FakeKasaService(rows);
            var kalem = new FakeKalemTanimiService();
            var summaryService = new FakeSummaryService { SummaryToReturn = summary };
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 1, Ad = "Perf Isletme", IsAktif = true }
            };

            var (_, handler, service) = BuildTelegramService(kasa, kalem, summaryService, isletme);
            var update = new TelegramUpdate
            {
                UpdateId = 100,
                ChatId = 123,
                UserId = 42,
                Text = "/ozet 30"
            };

            await service.ProcessUpdateAsync(update); // Warm-up
            handler.Requests.Clear();

            const int runs = 20;
            var timings = await MeasureAsync(runs, () => service.ProcessUpdateAsync(update));

            var avg = timings.Average();
            var p95 = Percentile(timings, 95);
            var max = timings.Max();

            Console.WriteLine(
                $"PERF|Scenario=TelegramSummary|records={rows.Count}|runs={runs}|avg_ms={avg:F2}|p95_ms={p95:F2}|max_ms={max:F2}");

            Assert.True(avg < 250, $"TelegramSummary average too high: {avg:F2} ms");
            Assert.True(p95 < 400, $"TelegramSummary p95 too high: {p95:F2} ms");
        }

        [Fact]
        [Trait("Category", "Performance")]
        public async Task TelegramIncomeCommand_Benchmark()
        {
            if (!IsPerformanceRunEnabled())
                return;

            var kalemRows = new List<KalemTanimi>();
            for (var i = 1; i <= 120; i++)
            {
                kalemRows.Add(new KalemTanimi
                {
                    Id = i,
                    Tip = "Gelir",
                    Ad = $"Gelir Kalem {i}"
                });
            }

            kalemRows.Add(new KalemTanimi { Id = 999, Tip = "Gelir", Ad = "Online Satis" });

            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(kalemRows);
            var summaryService = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 1, Ad = "Perf Isletme", IsAktif = true }
            };

            var (_, handler, service) = BuildTelegramService(kasa, kalem, summaryService, isletme);
            var update = new TelegramUpdate
            {
                UpdateId = 200,
                ChatId = 123,
                UserId = 42,
                Text = "/gelir 1499.90 Online Satis hafta sonu kampanyasi"
            };

            await service.ProcessUpdateAsync(update); // Warm-up
            handler.Requests.Clear();

            const int runs = 300;
            var timings = await MeasureAsync(runs, () => service.ProcessUpdateAsync(update));

            var avg = timings.Average();
            var p95 = Percentile(timings, 95);
            var max = timings.Max();

            Console.WriteLine(
                $"PERF|Scenario=TelegramIncome|runs={runs}|avg_ms={avg:F2}|p95_ms={p95:F2}|max_ms={max:F2}");

            Assert.True(avg < 20, $"TelegramIncome average too high: {avg:F2} ms");
            Assert.True(p95 < 40, $"TelegramIncome p95 too high: {p95:F2} ms");
        }

        [Fact]
        [Trait("Category", "Performance")]
        public async Task SummaryServiceSqlite_Benchmark()
        {
            if (!IsPerformanceRunEnabled())
                return;

            var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_perf_{Guid.NewGuid():N}.db");
            try
            {
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                await using (var db = new CashTrackerDbContext(options))
                {
                    await db.Database.EnsureCreatedAsync();

                    var batch = new List<Kasa>(2_500);
                    var now = DateTime.Today;
                    for (var i = 0; i < 50_000; i++)
                    {
                        var isIncome = (i % 2) == 0;
                        batch.Add(new Kasa
                        {
                            IsletmeId = 1,
                            Tarih = now.AddDays(-(i % 45)).AddMinutes(i % 1_440),
                            Tip = isIncome ? "Gelir" : "Gider",
                            Tutar = (i % 300) + 1,
                            Kalem = isIncome ? $"Gelir Kalem {(i % 15) + 1}" : $"Gider Kalem {(i % 20) + 1}",
                            GiderTuru = isIncome ? null : $"Gider Kalem {(i % 20) + 1}",
                            CreatedAt = now
                        });

                        if (batch.Count >= 2_500)
                        {
                            db.Kasalar.AddRange(batch);
                            await db.SaveChangesAsync();
                            batch.Clear();
                        }
                    }

                    if (batch.Count > 0)
                    {
                        db.Kasalar.AddRange(batch);
                        await db.SaveChangesAsync();
                    }
                }

                var factory = new SingleDbContextFactory(options);
                var isletme = new FakeIsletmeService
                {
                    Active = new Isletme { Id = 1, Ad = "Perf Isletme", IsAktif = true }
                };

                var service = new SummaryService(factory, isletme);
                var from = DateTime.Today.AddDays(-29);
                var to = DateTime.Today;

                await service.GetSummaryAsync(from, to); // Warm-up

                const int runs = 10;
                var timings = await MeasureAsync(runs, () => service.GetSummaryAsync(from, to));

                var avg = timings.Average();
                var p95 = Percentile(timings, 95);
                var max = timings.Max();

                Console.WriteLine(
                    $"PERF|Scenario=SummaryServiceSqlite|rows=50000|runs={runs}|avg_ms={avg:F2}|p95_ms={p95:F2}|max_ms={max:F2}");

                Assert.True(avg < 1_200, $"SummaryServiceSqlite average too high: {avg:F2} ms");
                Assert.True(p95 < 2_000, $"SummaryServiceSqlite p95 too high: {p95:F2} ms");
            }
            finally
            {
                try
                {
                    if (File.Exists(dbPath))
                        File.Delete(dbPath);
                }
                catch
                {
                }
            }
        }

        private static bool IsPerformanceRunEnabled()
        {
            var value = Environment.GetEnvironmentVariable(PerfFlag);
            return string.Equals(value, "1", StringComparison.Ordinal);
        }

        private static async Task<List<double>> MeasureAsync(int runs, Func<Task> action)
        {
            var timings = new List<double>(runs);
            var sw = new Stopwatch();

            for (var i = 0; i < runs; i++)
            {
                sw.Restart();
                await action();
                sw.Stop();
                timings.Add(sw.Elapsed.TotalMilliseconds);
            }

            return timings;
        }

        private static double Percentile(IReadOnlyList<double> source, double percentile)
        {
            if (source.Count == 0)
                return 0;

            var ordered = source.OrderBy(x => x).ToArray();
            var rank = (percentile / 100d) * (ordered.Length - 1);
            var low = (int)Math.Floor(rank);
            var high = (int)Math.Ceiling(rank);
            if (low == high)
                return ordered[low];

            var weight = rank - low;
            return ordered[low] + (ordered[high] - ordered[low]) * weight;
        }

        private static PeriodSummary BuildSummary(IReadOnlyList<Kasa> rows)
        {
            var incomeRows = rows.Where(x => string.Equals(x.Tip, "Gelir", StringComparison.OrdinalIgnoreCase)).ToArray();
            var expenseRows = rows.Where(x => string.Equals(x.Tip, "Gider", StringComparison.OrdinalIgnoreCase)).ToArray();

            return new PeriodSummary
            {
                From = DateTime.Today.AddDays(-29),
                To = DateTime.Today,
                IncomeTotal = incomeRows.Sum(x => x.Tutar),
                ExpenseTotal = expenseRows.Sum(x => x.Tutar),
                IncomeCount = incomeRows.Length,
                ExpenseCount = expenseRows.Length
            };
        }

        private static List<Kasa> GenerateRows(int count)
        {
            var rows = new List<Kasa>(count);
            var now = DateTime.Today;

            for (var i = 0; i < count; i++)
            {
                var isIncome = (i % 2) == 0;
                rows.Add(new Kasa
                {
                    Id = i + 1,
                    IsletmeId = 1,
                    Tarih = now.AddDays(-(i % 30)).AddMinutes(i % 1_440),
                    Tip = isIncome ? "Gelir" : "Gider",
                    Tutar = (i % 200) + 1,
                    Kalem = isIncome ? $"Gelir Kalem {(i % 12) + 1}" : $"Gider Kalem {(i % 18) + 1}",
                    GiderTuru = isIncome ? null : $"Gider Kalem {(i % 18) + 1}",
                    Aciklama = "Perf"
                });
            }

            return rows;
        }

        private static (TelegramBotService Bot, RecordingHttpMessageHandler Handler, TelegramCommandService Service) BuildTelegramService(
            FakeKasaService kasa,
            FakeKalemTanimiService kalem,
            FakeSummaryService summary,
            FakeIsletmeService isletme)
        {
            var handler = new RecordingHttpMessageHandler();
            var http = new HttpClient(handler);
            var bot = new TelegramBotService(http, "test-token");
            var settings = new TelegramSettings
            {
                BotToken = "test-token",
                ChatId = "123",
                EnableCommands = true,
                AllowedUserIds = "42"
            };

            var backup = new BackupReportService(
                bot,
                settings,
                new FakeDailyReportService(),
                new DatabaseBackupService(new DatabasePaths(Path.Combine(
                    Path.GetTempPath(),
                    $"cashtracker_perf_backup_{Guid.NewGuid():N}.db"))));

            var receiptOcrSettings = new ReceiptOcrSettings
            {
                Provider = "OpenAI",
                ApiKey = "test-key",
                Model = "gpt-5-mini",
                SessionTimeoutMinutes = 30
            };

            var service = new TelegramCommandService(
                bot,
                settings,
                kasa,
                kalem,
                summary,
                isletme,
                new FakeAppSecurityService(),
                backup,
                new FakeTelegramApprovalService(),
                new FakeReceiptOcrService(),
                new FakeTelegramReceiptSessionStore(),
                receiptOcrSettings,
                new FakeUrunHizmetService(),
                new FakeStokService(),
                new FakeBarcodeReaderService(),
                new FakeTelegramStockSessionStore());

            return (bot, handler, service);
        }
    }
}
