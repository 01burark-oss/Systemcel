using System;
using System.Collections.Generic;
using System.Linq;

namespace Systemcel.Api.Printing
{
    internal sealed class PrintRangeOption
    {
        public string Code { get; init; } = string.Empty;
        public string Display { get; init; } = string.Empty;
    }

    internal static class PrintRangeCatalog
    {
        public const string Custom = "custom";

        public static IReadOnlyList<PrintRangeOption> CreateLocalizedOptions(DateTime referenceDate)
        {
            var items = SummaryRangeCatalog
                .CreateLocalizedOptions(referenceDate)
                .Select(x => new PrintRangeOption
                {
                    Code = x.Code,
                    Display = x.Display
                })
                .ToList();

            items.Add(new PrintRangeOption
            {
                Code = Custom,
                Display = AppLocalization.T("print.range.custom")
            });

            return items;
        }

        public static string NormalizeCode(string? code, string fallbackCode)
        {
            if (string.Equals(code, Custom, StringComparison.OrdinalIgnoreCase))
                return Custom;

            return SummaryRangeCatalog.NormalizeCode(code, fallbackCode);
        }

        public static string GetDisplay(string? code, DateTime referenceDate)
        {
            var normalized = NormalizeCode(code, SummaryRangeCatalog.Last30Days);
            return string.Equals(normalized, Custom, StringComparison.OrdinalIgnoreCase)
                ? AppLocalization.T("print.range.custom")
                : SummaryRangeCatalog.GetDisplay(normalized, referenceDate);
        }

        public static bool TryGetRange(string? code, DateTime referenceDate, out DateTime from, out DateTime to)
        {
            var normalized = NormalizeCode(code, SummaryRangeCatalog.Last30Days);
            if (string.Equals(normalized, Custom, StringComparison.OrdinalIgnoreCase))
            {
                from = default;
                to = default;
                return false;
            }

            (from, to) = SummaryRangeCatalog.GetRange(normalized, referenceDate);
            return true;
        }
    }
}
