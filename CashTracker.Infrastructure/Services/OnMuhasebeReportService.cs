using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class OnMuhasebeReportService : IOnMuhasebeReportService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly IEntitlementGuard? _entitlementGuard;

        public OnMuhasebeReportService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            IEntitlementGuard? entitlementGuard = null)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _entitlementGuard = entitlementGuard;
        }

        public async Task<string> CreateMonthlyExportAsync(DateTime month, string outputDirectory, MonthlyReportExportOptions? options = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Çıktı klasörü seçilmelidir.", nameof(outputDirectory));

            options ??= new MonthlyReportExportOptions();
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            if (_entitlementGuard is not null)
            {
                var entitlement = await _entitlementGuard.GetAsync(activeIsletmeId, HesapTipleri.Isletme, ct);
                _entitlementGuard.EnsureFeature(entitlement, EntitlementFeatures.AdvancedExport);
            }
            var activeBusiness = await _isletmeService.GetActiveAsync();
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var exportRoot = Path.Combine(outputDirectory, $"systemcel-raporlar-{start:yyyy-MM}-{stamp}");
            Directory.CreateDirectory(exportRoot);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var faturalar = await db.Faturalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var faturaIds = faturalar.Select(x => x.Id).ToArray();
            var faturaSatirlari = await db.FaturaSatirlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && faturaIds.Contains(x.FaturaId))
                .OrderBy(x => x.FaturaId)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var cariKartlari = await db.CariKartlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderBy(x => x.Unvan)
                .ToListAsync(ct);
            var cariHareketleri = await db.CariHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var urunler = await db.UrunHizmetleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderBy(x => x.Ad)
                .ToListAsync(ct);
            var stokHareketleri = await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var kasaKayitlari = await db.Kasalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var belgeDosyalari = await db.BelgeDosyalari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && faturaIds.Contains(x.FaturaId))
                .ToListAsync(ct);

            var cariAdlari = cariKartlari.ToDictionary(x => x.Id, x => x.Unvan);
            var urunAdlari = urunler.ToDictionary(x => x.Id, x => x.Ad);
            var faturaNumaralari = faturalar.ToDictionary(x => x.Id, GetInvoiceNo);

            if (options.IncludeExcel && options.IncludeFaturalar)
            {
                await WriteExcelAsync(
                    Path.Combine(exportRoot, "faturalar.xlsx"),
                    [
                        new("Tarih", 16),
                        new("Fatura No", 22),
                        new("Tip", 14),
                        new("Cari", 30),
                        new("Durum", 18),
                        new("Toplam", 18),
                        new("Ödenen", 18),
                        new("Kalan", 18),
                        new("Ödeme", 18),
                        new("Açıklama", 34)
                    ],
                    faturalar.Select(x => new[]
                    {
                        DateTr(x.Tarih),
                        GetInvoiceNo(x),
                        DisplayInvoiceType(x.FaturaTipi),
                        cariAdlari.TryGetValue(x.CariKartId, out var cari) ? cari : "Cari kart",
                        DisplayInvoiceStatus(x.Durum),
                        MoneyTl(x.GenelToplam),
                        MoneyTl(x.OdenenTutar),
                        MoneyTl(Math.Max(0m, x.GenelToplam - x.OdenenTutar)),
                        DisplayPaymentMethod(x.OdemeYontemi),
                        x.Aciklama ?? string.Empty
                    }),
                    ct);

                await WriteExcelAsync(
                    Path.Combine(exportRoot, "fatura_satirlari.xlsx"),
                    [
                        new("Fatura No", 22),
                        new("Ürün / Hizmet", 28),
                        new("Açıklama", 34),
                        new("Miktar", 12),
                        new("Birim", 12),
                        new("Birim Fiyat", 18),
                        new("KDV %", 12),
                        new("KDV Tutarı", 18),
                        new("Satır Toplamı", 18)
                    ],
                    faturaSatirlari.Select(x => new[]
                    {
                        faturaNumaralari.TryGetValue(x.FaturaId, out var faturaNo) ? faturaNo : string.Empty,
                        x.UrunHizmetId.HasValue && urunAdlari.TryGetValue(x.UrunHizmetId.Value, out var urunAd) ? urunAd : string.Empty,
                        x.Aciklama,
                        Quantity(x.Miktar),
                        x.Birim,
                        MoneyTl(x.BirimFiyat),
                        Percent(x.KdvOrani),
                        MoneyTl(x.KdvTutar),
                        MoneyTl(x.SatirToplam)
                    }),
                    ct);
            }

            if (options.IncludeExcel && options.IncludeCari)
            {
                await WriteExcelAsync(
                    Path.Combine(exportRoot, "cari_hareketler.xlsx"),
                    [
                        new("Tarih", 16),
                        new("İşlem", 20),
                        new("Tutar", 18),
                        new("Belge No", 24)
                    ],
                    cariHareketleri.Select(x => new[]
                    {
                        DateTr(x.Tarih),
                        DisplayMovementType(x.HareketTipi),
                        MoneyTl(x.Tutar),
                        ExtractDocumentNo(x.Aciklama)
                    }),
                    ct);

                await WriteExcelAsync(
                    Path.Combine(exportRoot, "cari_kartlar.xlsx"),
                    [
                        new("Tip", 16),
                        new("Unvan", 34),
                        new("Telefon", 18),
                        new("E-posta", 28),
                        new("Vergi / TC No", 18),
                        new("Vergi Dairesi", 22),
                        new("Durum", 14)
                    ],
                    cariKartlari.Select(x => new[]
                    {
                        DisplayCariType(x.Tip),
                        x.Unvan,
                        x.Telefon,
                        x.Eposta,
                        x.VergiNoTc,
                        x.VergiDairesi,
                        x.Aktif ? "Evet" : "Hayır"
                    }),
                    ct);
            }

            if (options.IncludeExcel && options.IncludeStok)
            {
                await WriteExcelAsync(
                    Path.Combine(exportRoot, "stok_hareketleri.xlsx"),
                    [
                        new("Tarih", 16),
                        new("Ürün / Hizmet", 30),
                        new("İşlem", 16),
                        new("Miktar", 14),
                        new("Kaynak", 18),
                        new("Belge No", 24),
                        new("Açıklama", 34)
                    ],
                    stokHareketleri.Select(x => new[]
                    {
                        DateTr(x.Tarih),
                        urunAdlari.TryGetValue(x.UrunHizmetId, out var urunAd) ? urunAd : "Ürün / hizmet",
                        DisplayStockMovementType(x.HareketTipi),
                        Quantity(x.Miktar),
                        DisplaySource(x.Kaynak),
                        ExtractDocumentNo(x.Aciklama),
                        x.Aciklama ?? string.Empty
                    }),
                    ct);

                await WriteExcelAsync(
                    Path.Combine(exportRoot, "urun_hizmetler.xlsx"),
                    [
                        new("Tip", 16),
                        new("Ürün / Hizmet", 30),
                        new("Barkod", 22),
                        new("Birim", 12),
                        new("KDV %", 12),
                        new("Alış Fiyatı", 18),
                        new("Satış Fiyatı", 18),
                        new("Kritik Stok", 16),
                        new("Durum", 14)
                    ],
                    urunler.Select(x => new[]
                    {
                        DisplayProductType(x.Tip),
                        x.Ad,
                        x.Barkod,
                        x.Birim,
                        Percent(x.KdvOrani),
                        MoneyTl(x.AlisFiyati),
                        MoneyTl(x.SatisFiyati),
                        Quantity(x.KritikStok),
                        x.Aktif ? "Evet" : "Hayır"
                    }),
                    ct);
            }

            if (options.IncludeExcel && options.IncludeGelirGider)
            {
                await WriteExcelAsync(
                    Path.Combine(exportRoot, "gelir_gider.xlsx"),
                    [
                        new("Tarih", 16),
                        new("İşlem", 16),
                        new("Tutar", 18),
                        new("Ödeme Yöntemi", 18),
                        new("Kalem", 24),
                        new("Gider Türü", 22),
                        new("Açıklama", 34)
                    ],
                    kasaKayitlari.Select(x => new[]
                    {
                        DateTr(x.Tarih),
                        DisplayCashType(x.Tip),
                        MoneyTl(x.Tutar),
                        DisplayPaymentMethod(x.OdemeYontemi),
                        x.Kalem ?? string.Empty,
                        x.GiderTuru ?? string.Empty,
                        x.Aciklama ?? string.Empty
                    }),
                    ct);
            }

            var kdvRows = faturaSatirlari
                .GroupJoin(faturalar, row => row.FaturaId, invoice => invoice.Id, (row, invoice) => new { Row = row, Invoice = invoice.FirstOrDefault() })
                .Where(x => x.Invoice != null)
                .GroupBy(x => new { x.Invoice!.FaturaTipi, x.Row.KdvOrani })
                .OrderBy(x => x.Key.FaturaTipi)
                .ThenBy(x => x.Key.KdvOrani)
                .Select(x => new[]
                {
                    DisplayInvoiceType(x.Key.FaturaTipi),
                    Percent(x.Key.KdvOrani),
                    MoneyTl(x.Sum(y => y.Row.SatirNetTutar)),
                    MoneyTl(x.Sum(y => y.Row.KdvTutar)),
                    MoneyTl(x.Sum(y => y.Row.SatirToplam))
                });
            if (options.IncludeExcel && options.IncludeKdv)
                await WriteExcelAsync(
                    Path.Combine(exportRoot, "kdv_ozeti.xlsx"),
                    [
                        new("Fatura Tipi", 18),
                        new("KDV Oranı", 14),
                        new("Matrah", 18),
                        new("KDV", 18),
                        new("Toplam", 18)
                    ],
                    kdvRows,
                    ct);

            if (options.IncludeFaturalar)
            {
                var belgeRoot = Path.Combine(exportRoot, "belge_dosyalari");
                Directory.CreateDirectory(belgeRoot);
                foreach (var belge in belgeDosyalari)
                {
                    if (string.IsNullOrWhiteSpace(belge.DosyaYolu) || !File.Exists(belge.DosyaYolu))
                        continue;

                    var safeName = $"{belge.FaturaId}_{belge.BelgeTipi}_{Path.GetFileName(belge.DosyaYolu)}";
                    File.Copy(belge.DosyaYolu, Path.Combine(belgeRoot, safeName), overwrite: true);
                }
            }

            if (options.IncludeHtml)
            {
                var gelirToplami = kasaKayitlari
                    .Where(x => string.Equals(x.Tip, "Gelir", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.Tutar);
                var giderToplami = kasaKayitlari
                    .Where(x => string.Equals(x.Tip, "Gider", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.Tutar);
                await WriteSummaryHtmlAsync(
                    Path.Combine(exportRoot, "ozet.html"),
                    activeBusiness?.Ad ?? "İşletme",
                    start,
                    faturalar.Count,
                    faturalar.Sum(x => x.GenelToplam),
                    faturalar.Sum(x => x.OdenenTutar),
                    kasaKayitlari.Count,
                    gelirToplami,
                    giderToplami,
                    ct);
            }

            var zipPath = exportRoot + ".zip";
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            if (!options.CreateZip)
                return exportRoot;

            ZipFile.CreateFromDirectory(exportRoot, zipPath, CompressionLevel.Fastest, includeBaseDirectory: true);
            return zipPath;
        }

        private sealed record SpreadsheetColumn(string Header, double Width);

        private static async Task WriteExcelAsync(string path, IReadOnlyList<SpreadsheetColumn> columns, IEnumerable<IReadOnlyList<string>> rows, CancellationToken ct)
        {
            var dataRows = rows.ToList();

            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
            await WriteZipEntryAsync(archive, "[Content_Types].xml", ContentTypesXml, ct);
            await WriteZipEntryAsync(archive, "_rels/.rels", RootRelationshipsXml, ct);
            await WriteZipEntryAsync(archive, "xl/workbook.xml", WorkbookXml, ct);
            await WriteZipEntryAsync(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml, ct);
            await WriteZipEntryAsync(archive, "xl/styles.xml", StylesXml, ct);
            await WriteZipEntryAsync(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(columns, dataRows), ct);
        }

        private static async Task WriteZipEntryAsync(ZipArchive archive, string entryName, string contents, CancellationToken ct)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
            await using var entryStream = entry.Open();
            await using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            await writer.WriteAsync(contents.AsMemory(), ct);
        }

        private static string BuildWorksheetXml(IReadOnlyList<SpreadsheetColumn> columns, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var rowCount = Math.Max(1, rows.Count + 1);
            var columnCount = Math.Max(1, columns.Count);
            var lastCell = $"{ColumnName(columnCount)}{rowCount}";
            var builder = new StringBuilder();

            builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
            builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
            builder.Append(CultureInfo.InvariantCulture, $"""<dimension ref="A1:{lastCell}"/>""");
            builder.Append("""<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>""");
            builder.Append("<cols>");
            for (var index = 0; index < columns.Count; index++)
            {
                var width = Math.Clamp(columns[index].Width, 10d, 42d);
                builder.Append(CultureInfo.InvariantCulture, $"""<col min="{index + 1}" max="{index + 1}" width="{width:0.##}" customWidth="1"/>""");
            }
            builder.Append("</cols>");
            builder.Append("<sheetData>");
            builder.Append("""<row r="1" ht="30" customHeight="1">""");
            for (var index = 0; index < columns.Count; index++)
                builder.Append(Cell(index + 1, 1, columns[index].Header, 1));
            builder.Append("</row>");

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var excelRow = rowIndex + 2;
                builder.Append(CultureInfo.InvariantCulture, $"""<row r="{excelRow}" ht="24" customHeight="1">""");
                var row = rows[rowIndex];
                for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                {
                    var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    builder.Append(Cell(columnIndex + 1, excelRow, value, 2));
                }
                builder.Append("</row>");
            }

            builder.Append("</sheetData>");
            builder.Append("""<pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>""");
            builder.Append("</worksheet>");
            return builder.ToString();
        }

        private static string Cell(int columnIndex, int rowNumber, string value, int styleId)
        {
            return $"""<c r="{ColumnName(columnIndex)}{rowNumber}" t="inlineStr" s="{styleId}"><is><t>{EscapeXml(value)}</t></is></c>""";
        }

        private static string ColumnName(int columnNumber)
        {
            var dividend = columnNumber;
            var columnName = string.Empty;
            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private static async Task WriteSummaryHtmlAsync(
            string path,
            string businessName,
            DateTime month,
            int invoiceCount,
            decimal invoiceTotal,
            decimal paidTotal,
            int cashCount,
            decimal incomeTotal,
            decimal expenseTotal,
            CancellationToken ct)
        {
            var html = $$"""
<!doctype html>
<html lang="tr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Systemcel Aylık Rapor Özeti</title>
<style>
:root { color-scheme: light; --ink: #0b0b09; --paper: #f7f5ed; --line: #d9d6ca; --lime: #baff00; --muted: #716f67; }
* { box-sizing: border-box; }
body { font-family: Inter, "Segoe UI", Arial, sans-serif; color: var(--ink); background: var(--paper); margin: 0; padding: 32px; }
.report { max-width: 920px; margin: 0 auto; background: #fffefa; border: 1px solid var(--line); border-radius: 22px; overflow: hidden; }
.header { display: flex; align-items: end; justify-content: space-between; gap: 24px; padding: 30px 32px; border-bottom: 1px solid var(--line); }
.brand { font-size: 25px; font-weight: 850; letter-spacing: -.04em; }
.eyebrow { color: #527000; font-size: 12px; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }
h1 { margin: 8px 0 0; font-size: 34px; letter-spacing: -.035em; }
.period { color: var(--muted); font-weight: 700; white-space: nowrap; }
.metrics { display: grid; grid-template-columns: repeat(3, 1fr); border-bottom: 1px solid var(--line); }
.metric { padding: 24px 26px; border-right: 1px solid var(--line); }
.metric:last-child { border-right: 0; }
.metric small { display: block; color: var(--muted); font-weight: 700; margin-bottom: 8px; }
.metric strong { font-size: 26px; letter-spacing: -.025em; }
.metric.net { background: var(--ink); color: #fff; }
.metric.net small { color: #c9c7bd; }
.metric.net strong { color: var(--lime); }
.details { padding: 26px 32px 32px; }
table { width: 100%; border-collapse: collapse; }
td, th { border-bottom: 1px solid var(--line); padding: 13px 10px; text-align: left; }
th { color: var(--muted); font-size: 12px; letter-spacing: .08em; text-transform: uppercase; }
td:last-child, th:last-child { text-align: right; }
.footer { padding-top: 22px; color: var(--muted); font-size: 12px; }
@media (max-width: 680px) {
  body { padding: 12px; }
  .header { align-items: flex-start; flex-direction: column; padding: 24px; }
  .metrics { grid-template-columns: 1fr; }
  .metric { border-right: 0; border-bottom: 1px solid var(--line); }
  .details { padding: 22px 24px 26px; overflow-x: auto; }
}
@media print {
  body { background: #fff; padding: 0; }
  .report { max-width: none; border: 0; border-radius: 0; }
}
</style>
</head>
<body>
<main class="report">
<header class="header">
  <div>
    <div class="brand">systemcel</div>
    <div class="eyebrow">Aylık muhasebe paketi</div>
    <h1>{{EscapeHtml(businessName)}}</h1>
  </div>
  <div class="period">{{month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"))}}</div>
</header>
<section class="metrics">
  <article class="metric"><small>Toplam gelir</small><strong>{{MoneyTl(incomeTotal)}}</strong></article>
  <article class="metric"><small>Toplam gider</small><strong>{{MoneyTl(expenseTotal)}}</strong></article>
  <article class="metric net"><small>Net sonuç</small><strong>{{MoneyTl(incomeTotal - expenseTotal)}}</strong></article>
</section>
<section class="details">
<table>
<thead><tr><th>Rapor kalemi</th><th>Değer</th></tr></thead>
<tbody>
<tr><td>Fatura sayısı</td><td>{{invoiceCount}}</td></tr>
<tr><td>Fatura toplamı</td><td>{{MoneyTl(invoiceTotal)}}</td></tr>
<tr><td>Tahsil edilen</td><td>{{MoneyTl(paidTotal)}}</td></tr>
<tr><td>Bekleyen fatura bakiyesi</td><td>{{MoneyTl(Math.Max(0m, invoiceTotal - paidTotal))}}</td></tr>
<tr><td>Gelir / gider kayıt sayısı</td><td>{{cashCount}}</td></tr>
</tbody>
</table>
<div class="footer">Systemcel tarafından {{DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))}} tarihinde oluşturuldu.</div>
</section>
</main>
</body>
</html>
""";
            await File.WriteAllTextAsync(path, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);
        }

        private static string EscapeHtml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string EscapeXml(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        private static string DateTr(DateTime value)
        {
            return value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));
        }

        private static string MoneyTl(decimal value)
        {
            var culture = CultureInfo.GetCultureInfo("tr-TR");
            var format = decimal.Round(value, 2) == decimal.Round(value, 0) ? "N0" : "N2";
            return $"{value.ToString(format, culture)} TL";
        }

        private static string Quantity(decimal value)
        {
            return value.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"));
        }

        private static string Percent(decimal value)
        {
            return $"%{value.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))}";
        }

        private static string GetInvoiceNo(Fatura fatura)
        {
            if (!string.IsNullOrWhiteSpace(fatura.PortalBelgeNo))
                return fatura.PortalBelgeNo.Trim();
            if (!string.IsNullOrWhiteSpace(fatura.YerelFaturaNo))
                return fatura.YerelFaturaNo.Trim();
            return $"Fatura-{fatura.Id}";
        }

        private static string ExtractDocumentNo(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var tokens = text.Split([' ', '|', ':', ';', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
            for (var index = tokens.Length - 1; index >= 0; index--)
            {
                var token = tokens[index].Trim();
                if (token.Length >= 4 && token.Any(char.IsDigit))
                    return token;
            }

            return string.Empty;
        }

        private static string DisplayInvoiceType(string value)
        {
            return value switch
            {
                "Satis" => "Satış",
                "Alis" => "Alış",
                _ => value
            };
        }

        private static string DisplayInvoiceStatus(string value)
        {
            return value switch
            {
                "YerelTaslak" => "Taslak",
                "PortalTaslak" => "GİB Taslak",
                "Kesildi" => "Kesildi",
                "KismiOdendi" => "Kısmi Ödendi",
                "Odendi" => "Ödendi",
                "Iptal" => "İptal",
                _ => value
            };
        }

        private static string DisplayMovementType(string value)
        {
            return value switch
            {
                "Borc" => "Borç",
                "Alacak" => "Alacak",
                "Tahsilat" => "Tahsilat",
                "Odeme" => "Ödeme",
                _ => value
            };
        }

        private static string DisplayStockMovementType(string value)
        {
            return value switch
            {
                "Giris" => "Giriş",
                "Cikis" => "Çıkış",
                _ => value
            };
        }

        private static string DisplayProductType(string value)
        {
            return value switch
            {
                "Urun" => "Ürün",
                "Hizmet" => "Hizmet",
                _ => value
            };
        }

        private static string DisplayCariType(string value)
        {
            return value switch
            {
                "Musteri" => "Müşteri",
                "Tedarikci" => "Tedarikçi",
                "HerIkisi" => "Müşteri / Tedarikçi",
                _ => value
            };
        }

        private static string DisplayCashType(string value)
        {
            return value switch
            {
                "Gelir" => "Gelir",
                "Gider" => "Gider",
                _ => value
            };
        }

        private static string DisplayPaymentMethod(string value)
        {
            return value switch
            {
                "Nakit" => "Nakit",
                "KrediKarti" => "Kredi Kartı",
                "OnlineOdeme" => "Online Ödeme",
                "Havale" => "Havale",
                _ => value
            };
        }

        private static string DisplaySource(string value)
        {
            return value switch
            {
                "Manuel" => "Manuel",
                "Fatura" => "Fatura",
                "TahsilatOdeme" => "Tahsilat / Ödeme",
                _ => value
            };
        }

        private const string ContentTypesXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
</Types>
""";

        private const string RootRelationshipsXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""";

        private const string WorkbookXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Rapor" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
""";

        private const string WorkbookRelationshipsXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
""";

        private const string StylesXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="2">
    <font><sz val="11"/><name val="Calibri"/></font>
    <font><b/><sz val="14"/><name val="Calibri"/></font>
  </fonts>
  <fills count="3">
    <fill><patternFill patternType="none"/></fill>
    <fill><patternFill patternType="gray125"/></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FFE5F1DF"/><bgColor indexed="64"/></patternFill></fill>
  </fills>
  <borders count="2">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left style="thin"><color rgb="FFD7DCE3"/></left>
      <right style="thin"><color rgb="FFD7DCE3"/></right>
      <top style="thin"><color rgb="FFD7DCE3"/></top>
      <bottom style="thin"><color rgb="FFD7DCE3"/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count="1">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
  </cellStyleXfs>
  <cellXfs count="3">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
    <xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1">
      <alignment horizontal="center" vertical="center"/>
    </xf>
    <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1">
      <alignment horizontal="center" vertical="center"/>
    </xf>
  </cellXfs>
  <cellStyles count="1">
    <cellStyle name="Normal" xfId="0" builtinId="0"/>
  </cellStyles>
</styleSheet>
""";
    }
}
