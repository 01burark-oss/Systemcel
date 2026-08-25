using System.Text;
using CashTracker.Core.Models;
using Systemcel.Api.Printing;
using Xunit;

namespace CashTracker.Tests;

public sealed class PrintReportPdfExporterTests
{
    [Fact]
    public void Generate_WritesValidMultiPagePdfWithReportContent()
    {
        var report = new PrintReportData
        {
            Template = PrintReportTemplate.AccountingReport,
            ReportTitle = "Muhasebe Raporu",
            BusinessName = "Örnek İşletme",
            RangeDisplay = "01.08.2026 - 24.08.2026",
            DocumentCode = "SYS-20260824",
            GeneratedAt = new DateTime(2026, 8, 24, 10, 30, 0),
            TotalRecordCount = 60,
            Summary = new PeriodSummary
            {
                From = new DateTime(2026, 8, 1),
                To = new DateTime(2026, 8, 24),
                IncomeTotal = 12500,
                ExpenseTotal = 3250,
                IncomeCount = 40,
                ExpenseCount = 20
            },
            Records = Enumerable.Range(1, 60).Select(index => new PrintRecordRow
            {
                Date = new DateTime(2026, 8, Math.Min(index, 24)),
                TypeDisplay = "Gelir",
                MethodDisplay = "Banka",
                CategoryDisplay = "Satış",
                Description = $"Kayıt {index}",
                Amount = index * 100
            }).ToArray()
        };

        var pdf = PrintReportPdfExporter.Generate(report);
        var text = Encoding.ASCII.GetString(pdf);

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/Type /Catalog", text);
        Assert.Contains("/Count 2", text);
        Assert.Contains("Muhasebe Raporu", text);
        Assert.Contains("/Gbreve", text);
        Assert.True(pdf.AsSpan().IndexOf(new byte[] { 0xD6, (byte)'r', (byte)'n', (byte)'e', (byte)'k' }) >= 0);
        Assert.True(pdf.AsSpan().IndexOf(new byte[] { (byte)'S', (byte)'a', (byte)'t', 0xFD, 0xFE }) >= 0);
        Assert.Contains("startxref", text);
        Assert.EndsWith("%%EOF\n", text);
    }
}
