using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Systemcel.Api;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Services
{
    internal sealed class DashboardSnapshotService : IDashboardSnapshotService
    {
        private readonly IKasaService _kasaService;
        private readonly IIsletmeService _isletmeService;

        public DashboardSnapshotService(
            IKasaService kasaService,
            IIsletmeService isletmeService)
        {
            _kasaService = kasaService;
            _isletmeService = isletmeService;
        }

        public async Task<DashboardSnapshot> GetSnapshotAsync(
            DateTime referenceDate,
            string primaryRangeCode,
            string secondaryRangeCode,
            int month,
            int monthYear,
            int year)
        {
            var today = referenceDate.Date;
            var (primaryFrom, primaryTo) = SummaryRangeCatalog.GetRange(primaryRangeCode, today);
            var (secondaryFrom, secondaryTo) = SummaryRangeCatalog.GetRange(secondaryRangeCode, today);
            var monthFrom = new DateTime(monthYear, month, 1);
            var monthTo = monthFrom.AddMonths(1).AddDays(-1);
            var yearFrom = new DateTime(year, 1, 1);
            var yearTo = new DateTime(year, 12, 31);

            var minDate = new[] { today, primaryFrom, secondaryFrom, monthFrom, yearFrom }.Min();
            var maxDate = new[] { today, primaryTo, secondaryTo, monthTo, yearTo }.Max();

            var activeBusiness = await _isletmeService.GetActiveAsync();
            var rows = await _kasaService.GetAllAsync(minDate, maxDate);

            var todayRows = rows.Where(x => x.Tarih.Date == today).ToList();

            return new DashboardSnapshot
            {
                ActiveBusinessName = activeBusiness.Ad?.Trim() ?? string.Empty,
                DailySummary = BuildSummary(todayRows, today, today),
                PrimaryRangeSummary = BuildSummary(rows, primaryFrom, primaryTo),
                SecondaryRangeSummary = BuildSummary(rows, secondaryFrom, secondaryTo),
                MonthlySummary = BuildSummary(rows, monthFrom, monthTo),
                YearlySummary = BuildSummary(rows, yearFrom, yearTo),
                DailyPaymentMethodBreakdowns = BuildPaymentMethodBreakdowns(todayRows)
            };
        }

        private static PeriodSummary BuildSummary(IEnumerable<Kasa> rows, DateTime from, DateTime to)
        {
            var selected = rows
                .Where(x => x.Tarih.Date >= from.Date && x.Tarih.Date <= to.Date)
                .ToList();

            var incomeRows = selected.Where(x => IsIncomeTip(x.Tip)).ToList();
            var expenseRows = selected.Where(x => IsExpenseTip(x.Tip)).ToList();

            return new PeriodSummary
            {
                From = from.Date,
                To = to.Date,
                IncomeTotal = incomeRows.Sum(x => x.Tutar),
                ExpenseTotal = expenseRows.Sum(x => x.Tutar),
                IncomeCount = incomeRows.Count,
                ExpenseCount = expenseRows.Count
            };
        }

        private static List<DailyPaymentMethodBreakdown> BuildPaymentMethodBreakdowns(IReadOnlyCollection<Kasa> rows)
        {
            var byMethod = rows
                .GroupBy(x => NormalizeOdemeYontemi(x.OdemeYontemi), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Where(x => IsIncomeTip(x.Tip)).Sum(x => x.Tutar),
                        Expense = g.Where(x => IsExpenseTip(x.Tip)).Sum(x => x.Tutar)
                    },
                    StringComparer.OrdinalIgnoreCase);

            var methods = new[] { "Nakit", "KrediKarti", "OnlineOdeme", "Havale" };
            var result = new List<DailyPaymentMethodBreakdown>(methods.Length);
            foreach (var method in methods)
            {
                var income = byMethod.TryGetValue(method, out var values) ? values.Income : 0m;
                var expense = byMethod.TryGetValue(method, out values) ? values.Expense : 0m;
                result.Add(new DailyPaymentMethodBreakdown
                {
                    Method = method,
                    IncomeTotal = income,
                    ExpenseTotal = expense
                });
            }

            return result;
        }

        private static bool IsIncomeTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gelir" or "giris" or "income";
        }

        private static bool IsExpenseTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gider" or "cikis" or "expense";
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            var normalized = NormalizeAscii(value);
            return normalized switch
            {
                "nakit" => "Nakit",
                "cash" => "Nakit",
                "kredikarti" => "KrediKarti",
                "kredi karti" => "KrediKarti",
                "kart" => "KrediKarti",
                "creditcard" => "KrediKarti",
                "credit card" => "KrediKarti",
                "online" => "OnlineOdeme",
                "onlineodeme" => "OnlineOdeme",
                "online odeme" => "OnlineOdeme",
                "online payment" => "OnlineOdeme",
                "havale" => "Havale",
                "transfer" => "Havale",
                "bank transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string NormalizeAscii(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('\u0131', 'i')
                .Replace('\u015f', 's')
                .Replace('\u011f', 'g')
                .Replace('\u00fc', 'u')
                .Replace('\u00f6', 'o')
                .Replace('\u00e7', 'c');
        }
    }
}
