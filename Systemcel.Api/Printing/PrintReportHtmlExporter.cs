using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace Systemcel.Api.Printing
{
    internal static class PrintReportHtmlExporter
    {
        public static string Generate(PrintReportData report)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html><head><meta charset=\"utf-8\"><title>Systemcel Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body{font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;background:#fff;color:#111;margin:0;padding:24px;}");
            html.AppendLine(".page{max-width:980px;margin:0 auto;border:1px solid #6f6f6f;padding:18px 18px 16px;background:#fff;}");
            html.AppendLine(".doc-header{display:block;}");
            html.AppendLine(".hero-title{font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;font-size:22px;font-weight:700;text-align:center;letter-spacing:.01em;margin:0 0 4px;}");
            html.AppendLine(".hero-business{font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;font-size:14px;font-weight:700;text-align:center;margin:0 0 3px;}");
            html.AppendLine(".meta{text-align:center;font-size:12px;color:#303030;margin:0;}");
            html.AppendLine(".divider{border-top:1.4px solid #555;margin:10px 0 12px;}");
            html.AppendLine(".section-title{font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;font-size:15px;font-weight:700;text-align:center;margin:0 0 6px;}");
            html.AppendLine(".note-panel{border:1px solid #6f6f6f;padding:8px 10px;font-size:12px;font-style:italic;min-height:40px;}");
            html.AppendLine(".summary-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px;margin-bottom:6px;}");
            html.AppendLine(".metric-card{border:1px solid #6f6f6f;background:#ececec;padding:6px 8px 8px;}");
            html.AppendLine(".metric-label{font-size:11px;font-weight:700;text-align:center;margin-bottom:8px;}");
            html.AppendLine(".metric-value{font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;font-size:28px;font-weight:700;text-align:center;}");
            html.AppendLine(".counts{text-align:center;font-size:11px;color:#444;margin:4px 0 0;}");
            html.AppendLine(".exec-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;margin-top:10px;align-items:start;}");
            html.AppendLine(".table-panel{display:flex;flex-direction:column;}");
            html.AppendLine(".table-panel table{height:126px;}");
            html.AppendLine("table{width:100%;border-collapse:collapse;font-size:12px;table-layout:fixed;}");
            html.AppendLine("th,td{border:1px solid #7c7c7c;padding:5px 6px;vertical-align:middle;}");
            html.AppendLine("thead th{background:#dcdcdc;font-weight:700;}");
            html.AppendLine(".amount{background:#f5f5f5;text-align:right;}");
            html.AppendLine(".center{text-align:center;}");
            html.AppendLine(".right{text-align:right;}");
            html.AppendLine(".total-row td{background:#ececec;font-weight:700;}");
            html.AppendLine(".summary-title{font-family:'Segoe UI Variable Text','Segoe UI',sans-serif;font-size:14px;font-weight:700;text-align:center;margin:14px 0 6px;}");
            html.AppendLine(".footer{text-align:right;font-size:11px;color:#444;margin-top:8px;}");
            html.AppendLine("</style></head><body><div class=\"page\">");

            AppendHeader(html, report);

            if (report.Template == PrintReportTemplate.ExecutiveSummary)
                AppendExecutiveBody(html, report);
            else
                AppendAccountingBody(html, report);

            html.AppendLine($"<div class=\"footer\">{Encode(AppLocalization.F("print.footer.page", 1))}</div>");
            html.AppendLine("</div></body></html>");
            return html.ToString();
        }

        private static void AppendHeader(StringBuilder html, PrintReportData report)
        {
            html.AppendLine("<div class=\"doc-header\">");
            html.AppendLine($"<div class=\"hero-title\">{Encode(BuildHeadline(report))}</div>");
            html.AppendLine($"<div class=\"hero-business\">{Encode(report.BusinessName)}</div>");
            html.AppendLine($"<p class=\"meta\">{Encode(AppLocalization.T("print.meta.range"))}: {Encode(report.RangeDisplay)} | {Encode(AppLocalization.T("print.meta.generatedAt"))}: {Encode(report.GeneratedAt.ToString("dd.MM.yyyy HH:mm", AppLocalization.CurrentCulture))}</p>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"divider\"></div>");
        }

        private static void AppendExecutiveBody(StringBuilder html, PrintReportData report)
        {
            var net = report.Summary.IncomeTotal - report.Summary.ExpenseTotal;

            html.AppendLine($"<div class=\"section-title\">{Encode(AppLocalization.T("print.section.summary"))}</div>");
            html.AppendLine("<div class=\"summary-grid\">");
            AppendMetricCard(html, AppLocalization.T("print.summary.totalIncome"), report.Summary.IncomeTotal);
            AppendMetricCard(html, AppLocalization.T("print.summary.totalExpense"), report.Summary.ExpenseTotal);
            AppendMetricCard(html, AppLocalization.T("main.daily.net"), net);
            html.AppendLine("</div>");
            html.AppendLine($"<div class=\"counts\">{Encode(AppLocalization.F("print.metric.count", report.VisibleRecordCount))} | {Encode(AppLocalization.F("print.metric.totalCount", report.TotalRecordCount))}</div>");

            html.AppendLine($"<div class=\"summary-title\">{Encode(AppLocalization.T("print.section.note"))}</div>");
            html.AppendLine($"<div class=\"note-panel\">{Encode(string.IsNullOrWhiteSpace(report.Note) ? AppLocalization.T("print.note.placeholder") : report.Note)}</div>");

            html.AppendLine("<div class=\"exec-grid\">");
            AppendPaymentSummary(html, report, fixedBodyRows: 5);
            AppendCompactCategoryTable(html, AppLocalization.T("print.section.incomeCategories"), report.IncomeCategories, fixedBodyRows: 5);
            AppendCompactCategoryTable(html, AppLocalization.T("print.section.expenseCategories"), report.ExpenseCategories, fixedBodyRows: 5);
            html.AppendLine("</div>");

            if (report.IsPreview && report.RecordLimit.HasValue && report.TotalRecordCount > report.VisibleRecordCount)
            {
                html.AppendLine($"<div class=\"counts\">{Encode(AppLocalization.F("print.preview.sampleNote", report.VisibleRecordCount, report.TotalRecordCount))}</div>");
            }
        }

        private static void AppendAccountingBody(StringBuilder html, PrintReportData report)
        {
            AppendRecordTable(html, report);
            AppendPaymentSummary(html, report);

            if (report.IncludesDetailedSections)
            {
                AppendCategoryTable(html, AppLocalization.T("print.section.incomeCategories"), report.IncomeCategories);
                AppendCategoryTable(html, AppLocalization.T("print.section.expenseCategories"), report.ExpenseCategories);
            }
        }

        private static void AppendMetricCard(StringBuilder html, string label, decimal amount)
        {
            html.AppendLine("<div class=\"metric-card\">");
            html.AppendLine($"<div class=\"metric-label\">{Encode(label)}</div>");
            html.AppendLine($"<div class=\"metric-value\">{Encode(amount.ToString("n2", AppLocalization.CurrentCulture))}</div>");
            html.AppendLine("</div>");
        }

        private static void AppendRecordTable(StringBuilder html, PrintReportData report)
        {
            html.AppendLine("<table><thead><tr>");
            html.AppendLine(ColumnHeader(AppLocalization.T("common.date"), "16%"));
            html.AppendLine(ColumnHeader(AppLocalization.T("common.description"), "30%"));
            html.AppendLine(ColumnHeader(AppLocalization.T("common.category"), "20%"));
            html.AppendLine(ColumnHeader(AppLocalization.T("tip.income"), "12% center"));
            html.AppendLine(ColumnHeader(AppLocalization.T("tip.expense"), "12% center"));
            html.AppendLine(ColumnHeader(AppLocalization.T("common.method"), "10% center"));
            html.AppendLine("</tr></thead><tbody>");

            foreach (var row in report.Records)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{Encode(row.Date.ToString("dd.MM.yyyy", AppLocalization.CurrentCulture))}</td>");
                html.AppendLine($"<td>{Encode(row.Description)}</td>");
                html.AppendLine($"<td>{Encode(row.CategoryDisplay)}</td>");
                html.AppendLine($"<td class=\"amount\">{Encode(row.IsIncome ? row.Amount.ToString("n2", AppLocalization.CurrentCulture) : string.Empty)}</td>");
                html.AppendLine($"<td class=\"amount\">{Encode(row.IsIncome ? string.Empty : row.Amount.ToString("n2", AppLocalization.CurrentCulture))}</td>");
                html.AppendLine($"<td class=\"center\">{Encode(row.MethodDisplay)}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("<tr class=\"total-row\">");
            html.AppendLine($"<td colspan=\"3\" class=\"center\">{Encode(AppLocalization.T("print.total.label"))}</td>");
            html.AppendLine($"<td class=\"right\">{Encode(report.Summary.IncomeTotal.ToString("n2", AppLocalization.CurrentCulture))}</td>");
            html.AppendLine($"<td class=\"right\">{Encode(report.Summary.ExpenseTotal.ToString("n2", AppLocalization.CurrentCulture))}</td>");
            html.AppendLine("<td></td>");
            html.AppendLine("</tr>");
            html.AppendLine("</tbody></table>");
        }

        private static void AppendPaymentSummary(StringBuilder html, PrintReportData report, int? fixedBodyRows = null)
        {
            html.AppendLine("<div class=\"table-panel\">");
            html.AppendLine($"<div class=\"summary-title\">{Encode(AppLocalization.T("print.section.paymentMethodsCompact"))}</div>");
            html.AppendLine("<table><thead><tr>");
            html.AppendLine(ColumnHeader(AppLocalization.T("common.method"), "46%"));
            html.AppendLine(ColumnHeader(AppLocalization.T("print.summary.totalIncome"), "27% center"));
            html.AppendLine(ColumnHeader(AppLocalization.T("print.summary.totalExpense"), "27% center"));
            html.AppendLine("</tr></thead><tbody>");

            var allowedRows = fixedBodyRows.HasValue ? fixedBodyRows.Value - 1 : report.PaymentMethods.Count;
            var renderedRows = 0;
            foreach (var row in report.PaymentMethods.Take(allowedRows))
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{Encode(row.DisplayName)}</td>");
                html.AppendLine($"<td class=\"amount\">{Encode(row.Income.ToString("n2", AppLocalization.CurrentCulture))}</td>");
                html.AppendLine($"<td class=\"amount\">{Encode(row.Expense.ToString("n2", AppLocalization.CurrentCulture))}</td>");
                html.AppendLine("</tr>");
                renderedRows++;
            }

            if (fixedBodyRows.HasValue)
            {
                for (var i = renderedRows; i < allowedRows; i++)
                {
                    html.AppendLine("<tr><td>&nbsp;</td><td class=\"amount\">&nbsp;</td><td class=\"amount\">&nbsp;</td></tr>");
                }
            }

            html.AppendLine("<tr class=\"total-row\">");
            html.AppendLine($"<td class=\"center\">{Encode(AppLocalization.T("print.total.general"))}</td>");
            html.AppendLine($"<td class=\"right\">{Encode(report.Summary.IncomeTotal.ToString("n2", AppLocalization.CurrentCulture))}</td>");
            html.AppendLine($"<td class=\"right\">{Encode(report.Summary.ExpenseTotal.ToString("n2", AppLocalization.CurrentCulture))}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</tbody></table>");
            html.AppendLine("</div>");
        }

        private static void AppendCompactCategoryTable(StringBuilder html, string title, System.Collections.Generic.IReadOnlyList<PrintCategorySummary> rows, int fixedBodyRows)
        {
            html.AppendLine("<div class=\"table-panel\">");
            html.AppendLine($"<div class=\"summary-title\">{Encode(title)}</div>");
            html.AppendLine("<table><thead><tr>");
            html.AppendLine(ColumnHeader(AppLocalization.T("common.category"), "54%"));
            html.AppendLine(ColumnHeader(AppLocalization.T("print.column.count"), "16% center"));
            html.AppendLine(ColumnHeader(AppLocalization.T("common.amount"), "30% center"));
            html.AppendLine("</tr></thead><tbody>");

            var items = rows.DefaultIfEmpty(new PrintCategorySummary { CategoryName = "-", Count = 0, Total = 0m }).Take(fixedBodyRows).ToList();
            foreach (var row in items)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{Encode(row.CategoryName)}</td>");
                html.AppendLine($"<td class=\"right\">{Encode(row.Count.ToString(AppLocalization.CurrentCulture))}</td>");
                html.AppendLine($"<td class=\"amount\">{Encode(row.Total.ToString("n2", AppLocalization.CurrentCulture))}</td>");
                html.AppendLine("</tr>");
            }

            for (var i = items.Count; i < fixedBodyRows; i++)
            {
                html.AppendLine("<tr><td>&nbsp;</td><td class=\"right\">&nbsp;</td><td class=\"amount\">&nbsp;</td></tr>");
            }

            html.AppendLine("</tbody></table>");
            html.AppendLine("</div>");
        }

        private static void AppendCategoryTable(StringBuilder html, string title, System.Collections.Generic.IReadOnlyList<PrintCategorySummary> rows)
        {
            html.AppendLine($"<div class=\"summary-title\">{Encode(title)}</div>");
            html.AppendLine("<table><thead><tr>");
            html.AppendLine(ColumnHeader(AppLocalization.T("common.category"), "58%"));
            html.AppendLine(ColumnHeader(AppLocalization.T("print.column.count"), "18% center"));
            html.AppendLine(ColumnHeader(AppLocalization.T("common.amount"), "24% center"));
            html.AppendLine("</tr></thead><tbody>");

            foreach (var row in rows.DefaultIfEmpty(new PrintCategorySummary { CategoryName = "-", Count = 0, Total = 0m }))
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{Encode(row.CategoryName)}</td>");
                html.AppendLine($"<td class=\"right\">{Encode(row.Count.ToString(AppLocalization.CurrentCulture))}</td>");
                html.AppendLine($"<td class=\"amount\">{Encode(row.Total.ToString("n2", AppLocalization.CurrentCulture))}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody></table>");
        }

        private static string ColumnHeader(string text, string style)
        {
            var width = style.Replace(" center", string.Empty);
            var className = style.Contains("center") ? " class=\"center\"" : string.Empty;
            return $"<th style=\"width:{width}\"{className}>{Encode(text)}</th>";
        }

        private static string BuildHeadline(PrintReportData report)
        {
            var from = report.Summary.From == default ? report.GeneratedAt.Date : report.Summary.From.Date;
            var to = report.Summary.To == default ? report.GeneratedAt.Date : report.Summary.To.Date;
            var suffix = AppLocalization.T("print.headline.suffix");

            if (from.Year == to.Year && from.Month == to.Month)
            {
                var monthName = AppLocalization.CurrentCulture.DateTimeFormat.GetMonthName(from.Month).ToUpper(AppLocalization.CurrentCulture);
                return $"{monthName} {from.Year} {suffix}";
            }

            return $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy} {suffix}";
        }

        private static string Encode(string value)
        {
            return WebUtility.HtmlEncode(value);
        }
    }
}
