using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace Systemcel.Api.Printing
{
    internal static class PrintReportComposer
    {
        public static IReadOnlyList<PrintTemplateOption> CreateTemplateOptions()
        {
            return
            [
                new PrintTemplateOption
                {
                    Template = PrintReportTemplate.ExecutiveSummary,
                    Display = AppLocalization.T("print.template.executive")
                },
                new PrintTemplateOption
                {
                    Template = PrintReportTemplate.AccountingReport,
                    Display = AppLocalization.T("print.template.accounting")
                }
            ];
        }

        public static PrintReportData Compose(
            PrintReportRequest request,
            string businessName,
            PeriodSummary summary,
            IEnumerable<Kasa> records)
        {
            var orderedRecords = records
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToList();

            var visibleRows = request.RecordLimit.HasValue
                ? orderedRecords.Take(request.RecordLimit.Value).ToList()
                : orderedRecords;
            var aggregateRows = request.RecordLimit.HasValue && !request.IsPreview
                ? visibleRows
                : orderedRecords;
            var effectiveSummary = request.RecordLimit.HasValue && !request.IsPreview
                ? BuildSummary(
                    aggregateRows,
                    summary.From == default ? request.From : summary.From,
                    summary.To == default ? request.To : summary.To)
                : CloneSummary(summary, request.From, request.To);

            return new PrintReportData
            {
                Template = request.Template,
                ReportTitle = request.Template == PrintReportTemplate.ExecutiveSummary
                    ? AppLocalization.T("print.template.executive")
                    : AppLocalization.T("print.template.accounting"),
                BusinessName = string.IsNullOrWhiteSpace(businessName)
                    ? AppLocalization.T("common.unknown")
                    : businessName.Trim(),
                RangeDisplay = request.RangeDisplay,
                Note = request.Note.Trim(),
                GeneratedAt = request.GeneratedAt,
                DocumentCode = request.GeneratedAt.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture),
                IsPreview = request.IsPreview,
                RecordLimit = request.RecordLimit,
                VisibleRecordCount = visibleRows.Count,
                TotalRecordCount = orderedRecords.Count,
                Summary = effectiveSummary,
                PaymentMethods = BuildMethodSummaries(aggregateRows),
                IncomeCategories = BuildCategorySummaries(aggregateRows, "Gelir"),
                ExpenseCategories = BuildCategorySummaries(aggregateRows, "Gider"),
                Records = BuildRecordRows(visibleRows)
            };
        }

        private static PeriodSummary CloneSummary(PeriodSummary summary, DateTime fallbackFrom, DateTime fallbackTo)
        {
            return new PeriodSummary
            {
                From = summary.From == default ? fallbackFrom : summary.From,
                To = summary.To == default ? fallbackTo : summary.To,
                IncomeTotal = summary.IncomeTotal,
                ExpenseTotal = summary.ExpenseTotal,
                IncomeCount = summary.IncomeCount,
                ExpenseCount = summary.ExpenseCount
            };
        }

        private static PeriodSummary BuildSummary(IEnumerable<Kasa> records, DateTime from, DateTime to)
        {
            var rows = records.ToList();
            return new PeriodSummary
            {
                From = from,
                To = to,
                IncomeTotal = rows.Where(x => IsIncome(x.Tip)).Sum(x => x.Tutar),
                ExpenseTotal = rows.Where(x => IsExpense(x.Tip)).Sum(x => x.Tutar),
                IncomeCount = rows.Count(x => IsIncome(x.Tip)),
                ExpenseCount = rows.Count(x => IsExpense(x.Tip))
            };
        }

        private static IReadOnlyList<PrintMethodSummary> BuildMethodSummaries(IEnumerable<Kasa> records)
        {
            return records
                .GroupBy(x => NormalizePaymentMethod(x.OdemeYontemi))
                .Select(g => new PrintMethodSummary
                {
                    MethodKey = g.Key,
                    DisplayName = GetPaymentMethodDisplay(g.Key),
                    Income = g.Where(x => IsIncome(x.Tip)).Sum(x => x.Tutar),
                    Expense = g.Where(x => IsExpense(x.Tip)).Sum(x => x.Tutar)
                })
                .OrderBy(x => GetPaymentOrder(x.MethodKey))
                .ToArray();
        }

        private static IReadOnlyList<PrintCategorySummary> BuildCategorySummaries(IEnumerable<Kasa> records, string tip)
        {
            return records
                .Where(x => string.Equals(AppLocalization.NormalizeTip(x.Tip), tip, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => ResolveCategoryName(x, tip))
                .Select(g => new PrintCategorySummary
                {
                    Tip = tip,
                    CategoryName = g.Key,
                    Total = g.Sum(x => x.Tutar),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.CategoryName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }

        private static IReadOnlyList<PrintRecordRow> BuildRecordRows(IEnumerable<Kasa> records)
        {
            return records
                .Select(x =>
                {
                    var normalizedTip = AppLocalization.NormalizeTip(x.Tip);
                    return new PrintRecordRow
                    {
                        Date = x.Tarih,
                        IsIncome = string.Equals(normalizedTip, "Gelir", StringComparison.OrdinalIgnoreCase),
                        TypeDisplay = AppLocalization.GetTipDisplay(normalizedTip),
                        MethodDisplay = GetPaymentMethodDisplay(NormalizePaymentMethod(x.OdemeYontemi)),
                        CategoryDisplay = ResolveCategoryName(x, normalizedTip),
                        Description = string.IsNullOrWhiteSpace(x.Aciklama)
                            ? ResolveCategoryName(x, normalizedTip)
                            : x.Aciklama.Trim(),
                        Amount = x.Tutar
                    };
                })
                .ToArray();
        }

        private static string ResolveCategoryName(Kasa row, string normalizedTip)
        {
            var raw = string.IsNullOrWhiteSpace(row.Kalem) ? row.GiderTuru : row.Kalem;
            if (!string.IsNullOrWhiteSpace(raw))
                return raw.Trim();

            return string.Equals(normalizedTip, "Gider", StringComparison.OrdinalIgnoreCase)
                ? AppLocalization.T("main.telegram.defaultExpenseCategory")
                : AppLocalization.T("main.telegram.defaultIncomeCategory");
        }

        private static bool IsIncome(string? tip)
        {
            return string.Equals(AppLocalization.NormalizeTip(tip), "Gelir", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExpense(string? tip)
        {
            return string.Equals(AppLocalization.NormalizeTip(tip), "Gider", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePaymentMethod(string? value)
        {
            var normalized = NormalizeAscii(value);
            return normalized switch
            {
                "nakit" => "cash",
                "cash" => "cash",
                "kredikarti" => "card",
                "kredi karti" => "card",
                "kart" => "card",
                "creditcard" => "card",
                "credit card" => "card",
                "online" => "online",
                "onlineodeme" => "online",
                "online odeme" => "online",
                "online payment" => "online",
                "havale" => "transfer",
                "transfer" => "transfer",
                "bank transfer" => "transfer",
                _ => "cash"
            };
        }

        private static string GetPaymentMethodDisplay(string key)
        {
            return key switch
            {
                "card" => AppLocalization.T("payment.card"),
                "online" => AppLocalization.T("payment.online"),
                "transfer" => AppLocalization.T("payment.transfer"),
                _ => AppLocalization.T("payment.cash")
            };
        }

        private static int GetPaymentOrder(string key)
        {
            return key switch
            {
                "cash" => 0,
                "card" => 1,
                "online" => 2,
                "transfer" => 3,
                _ => 9
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
