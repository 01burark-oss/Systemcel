using System.Globalization;
using System.Text;

namespace Systemcel.Api.Printing
{
    internal static class PrintReportPdfExporter
    {
        private const int LinesPerPage = 43;

        public static byte[] Generate(PrintReportData report)
        {
            ArgumentNullException.ThrowIfNull(report);
            var lines = BuildLines(report);
            var pages = lines.Chunk(LinesPerPage).ToArray();
            if (pages.Length == 0) pages = new[] { Array.Empty<string>() };

            var normalFontId = 3 + (pages.Length * 2);
            var boldFontId = normalFontId + 1;
            var objects = new Dictionary<int, byte[]>();
            var pageIds = Enumerable.Range(0, pages.Length).Select(i => 4 + (i * 2)).ToArray();
            objects[1] = Ascii("<< /Type /Catalog /Pages 2 0 R >>");
            objects[2] = Ascii($"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pages.Length} >>");

            for (var index = 0; index < pages.Length; index++)
            {
                var contentId = 3 + (index * 2);
                var pageId = contentId + 1;
                var stream = BuildPageStream(pages[index], index + 1, pages.Length);
                var streamBytes = PdfBytes(stream);
                objects[contentId] = Join(Ascii($"<< /Length {streamBytes.Length} >>\nstream\n"), streamBytes, Ascii("\nendstream"));
                objects[pageId] = Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {normalFontId} 0 R /F2 {boldFontId} 0 R >> >> /Contents {contentId} 0 R >>");
            }

            const string turkishEncoding = "/Encoding << /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [208 /Gbreve 221 /Idotaccent 222 /Scedilla 240 /gbreve 253 /dotlessi 254 /scedilla] >>";
            objects[normalFontId] = Ascii($"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica {turkishEncoding} >>");
            objects[boldFontId] = Ascii($"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold {turkishEncoding} >>");
            return WriteDocument(objects);
        }

        private static List<string> BuildLines(PrintReportData report)
        {
            var lines = new List<string>
            {
                report.ReportTitle, report.BusinessName, report.RangeDisplay,
                $"Belge: {report.DocumentCode} | Olusturma: {report.GeneratedAt:dd.MM.yyyy HH:mm}", string.Empty,
                "FINANSAL OZET", $"Toplam gelir: {Money(report.Summary.IncomeTotal)} TL",
                $"Toplam gider: {Money(report.Summary.ExpenseTotal)} TL", $"Net: {Money(report.Summary.Net)} TL",
                $"Kayit: {report.TotalRecordCount} (gelir {report.Summary.IncomeCount}, gider {report.Summary.ExpenseCount})"
            };
            if (!string.IsNullOrWhiteSpace(report.Note))
            {
                lines.Add(string.Empty); lines.Add("NOT"); lines.Add(report.Note);
            }
            AddSection(lines, "ODEME YONTEMLERI", report.PaymentMethods.Select(x => $"{x.DisplayName}: gelir {Money(x.Income)} TL | gider {Money(x.Expense)} TL | net {Money(x.Net)} TL"));
            if (report.IncludesDetailedSections)
            {
                AddSection(lines, "GELIR KATEGORILERI", report.IncomeCategories.Select(x => $"{x.CategoryName}: {Money(x.Total)} TL ({x.Count} kayit)"));
                AddSection(lines, "GIDER KATEGORILERI", report.ExpenseCategories.Select(x => $"{x.CategoryName}: {Money(x.Total)} TL ({x.Count} kayit)"));
                AddSection(lines, "HAREKETLER", report.Records.Select(x => $"{x.Date:dd.MM.yyyy} | {x.TypeDisplay} | {x.MethodDisplay} | {x.CategoryDisplay} | {Money(x.Amount)} TL | {x.Description}"));
            }
            return lines.SelectMany(Wrap).ToList();
        }

        private static void AddSection(List<string> lines, string title, IEnumerable<string> rows)
        {
            lines.Add(string.Empty); lines.Add(title);
            var values = rows.ToList();
            lines.AddRange(values.Count == 0 ? new[] { "Kayit yok." } : values);
        }

        private static IEnumerable<string> Wrap(string value)
        {
            const int width = 92;
            var remaining = Sanitize(value).Trim();
            if (remaining.Length == 0) return new[] { string.Empty };
            var result = new List<string>();
            while (remaining.Length > width)
            {
                var split = remaining.LastIndexOf(' ', width);
                if (split < 1) split = width;
                result.Add(remaining[..split].TrimEnd());
                remaining = remaining[split..].TrimStart();
            }
            result.Add(remaining);
            return result;
        }

        private static string BuildPageStream(IReadOnlyList<string> lines, int page, int pageCount)
        {
            var body = new StringBuilder("BT\n/F1 10 Tf\n44 792 Td\n");
            foreach (var line in lines)
            {
                body.Append(IsHeading(line) ? "/F2 10 Tf\n" : "/F1 10 Tf\n");
                body.Append('(').Append(Escape(line)).Append(") Tj\n0 -17 Td\n");
            }
            body.Append($"ET\nBT\n/F1 8 Tf\n500 24 Td\n({page} / {pageCount}) Tj\nET");
            return body.ToString();
        }

        private static bool IsHeading(string line) => line.Length > 0 && line == line.ToUpperInvariant() && line.Any(char.IsLetter);
        private static string Money(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        private static string Escape(string value) => Sanitize(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        private static string Sanitize(string value) => new(value
            .Select(ch => ch is >= ' ' and <= '~' || IsSupportedTurkishCharacter(ch) ? ch : ' ')
            .ToArray());

        private static bool IsSupportedTurkishCharacter(char value) => value is
            'Ç' or 'ç' or 'Ğ' or 'ğ' or 'İ' or 'ı' or 'Ö' or 'ö' or 'Ş' or 'ş' or 'Ü' or 'ü';

        private static byte[] PdfBytes(string value)
        {
            var bytes = new byte[value.Length];
            for (var index = 0; index < value.Length; index++)
            {
                bytes[index] = value[index] switch
                {
                    'Ç' => 0xC7, 'ç' => 0xE7,
                    'Ğ' => 0xD0, 'ğ' => 0xF0,
                    'İ' => 0xDD, 'ı' => 0xFD,
                    'Ö' => 0xD6, 'ö' => 0xF6,
                    'Ş' => 0xDE, 'ş' => 0xFE,
                    'Ü' => 0xDC, 'ü' => 0xFC,
                    >= ' ' and <= '~' or '\n' or '\r' or '\t' => (byte)value[index],
                    _ => (byte)' '
                };
            }

            return bytes;
        }

        private static byte[] Join(params byte[][] parts)
        {
            var totalLength = parts.Sum(part => part.Length);
            var result = new byte[totalLength];
            var offset = 0;
            foreach (var part in parts)
            {
                Buffer.BlockCopy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }

            return result;
        }

        private static byte[] WriteDocument(IReadOnlyDictionary<int, byte[]> objects)
        {
            using var output = new MemoryStream();
            Write(output, "%PDF-1.4\n");
            var offsets = new long[objects.Count + 1];
            for (var id = 1; id <= objects.Count; id++)
            {
                offsets[id] = output.Position; Write(output, $"{id} 0 obj\n"); output.Write(objects[id]); Write(output, "\nendobj\n");
            }
            var xref = output.Position;
            Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            for (var id = 1; id <= objects.Count; id++) Write(output, $"{offsets[id]:D10} 00000 n \n");
            Write(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return output.ToArray();
        }

        private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
        private static void Write(Stream stream, string value) => stream.Write(Ascii(value));
    }
}
