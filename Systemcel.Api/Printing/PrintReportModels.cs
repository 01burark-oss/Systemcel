using System;
using System.Collections.Generic;
using CashTracker.Core.Models;

namespace Systemcel.Api.Printing
{
    internal enum PrintReportTemplate
    {
        ExecutiveSummary = 0,
        AccountingReport = 1
    }

    internal sealed class PrintTemplateOption
    {
        public PrintReportTemplate Template { get; init; }
        public string Display { get; init; } = string.Empty;
    }

    internal sealed class PrintReportRequest
    {
        public PrintReportTemplate Template { get; init; }
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public string RangeDisplay { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public DateTime GeneratedAt { get; init; }
        public int? RecordLimit { get; init; }
        public bool IsPreview { get; init; }
    }

    internal sealed class PrintMethodSummary
    {
        public string MethodKey { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public decimal Income { get; init; }
        public decimal Expense { get; init; }
        public decimal Net => Income - Expense;
    }

    internal sealed class PrintCategorySummary
    {
        public string CategoryName { get; init; } = string.Empty;
        public string Tip { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public int Count { get; init; }
    }

    internal sealed class PrintRecordRow
    {
        public DateTime Date { get; init; }
        public bool IsIncome { get; init; }
        public string TypeDisplay { get; init; } = string.Empty;
        public string MethodDisplay { get; init; } = string.Empty;
        public string CategoryDisplay { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
    }

    internal sealed class PrintReportData
    {
        public PrintReportTemplate Template { get; init; }
        public string ReportTitle { get; init; } = string.Empty;
        public string BusinessName { get; init; } = string.Empty;
        public string RangeDisplay { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public string DocumentCode { get; init; } = string.Empty;
        public DateTime GeneratedAt { get; init; }
        public bool IsPreview { get; init; }
        public int? RecordLimit { get; init; }
        public int VisibleRecordCount { get; init; }
        public int TotalRecordCount { get; init; }
        public PeriodSummary Summary { get; init; } = new();
        public IReadOnlyList<PrintMethodSummary> PaymentMethods { get; init; } = Array.Empty<PrintMethodSummary>();
        public IReadOnlyList<PrintCategorySummary> IncomeCategories { get; init; } = Array.Empty<PrintCategorySummary>();
        public IReadOnlyList<PrintCategorySummary> ExpenseCategories { get; init; } = Array.Empty<PrintCategorySummary>();
        public IReadOnlyList<PrintRecordRow> Records { get; init; } = Array.Empty<PrintRecordRow>();

        public bool IncludesDetailedSections => Template == PrintReportTemplate.AccountingReport;
    }
}
