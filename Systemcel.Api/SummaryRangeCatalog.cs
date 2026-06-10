using System;
using System.Collections.Generic;
using System.Linq;

namespace Systemcel.Api
{
    internal sealed class SummaryRangeOption
    {
        public string Code { get; init; } = string.Empty;
        public string Display { get; init; } = string.Empty;
    }

    internal static class SummaryRangeCatalog
    {
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Last30Days = "last_30_days";
        public const string Monthly = "monthly";
        public const string Last3Months = "last_3_months";
        public const string Last6Months = "last_6_months";
        public const string Last1Year = "last_1_year";

        private static readonly (string Code, string LocalizationKey)[] Definitions =
        {
            (Daily, "main.summary.range.daily"),
            (Weekly, "main.summary.range.weekly"),
            (Last30Days, "main.summary.range.last30Days"),
            (Monthly, "main.summary.range.monthly"),
            (Last3Months, "main.summary.range.last3Months"),
            (Last6Months, "main.summary.range.last6Months"),
            (Last1Year, "main.summary.range.last1Year")
        };

        public static IReadOnlyList<SummaryRangeOption> CreateLocalizedOptions(DateTime referenceDate)
        {
            return Definitions
                .Select(x => new SummaryRangeOption
                {
                    Code = x.Code,
                    Display = GetDisplay(x.Code, referenceDate)
                })
                .ToArray();
        }

        public static string NormalizeCode(string? code, string fallbackCode)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var matched = Definitions.FirstOrDefault(x =>
                    string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(matched.Code))
                    return matched.Code;
            }

            return fallbackCode;
        }

        public static string GetDisplay(string? code, DateTime referenceDate)
        {
            var normalized = NormalizeCode(code, Monthly);
            if (string.Equals(normalized, Monthly, StringComparison.OrdinalIgnoreCase))
                return ToMonthDisplay(referenceDate);

            var definition = Definitions.First(x => string.Equals(x.Code, normalized, StringComparison.OrdinalIgnoreCase));
            return AppLocalization.T(definition.LocalizationKey);
        }

        public static (DateTime From, DateTime To) GetRange(string? code, DateTime referenceDate)
        {
            var today = referenceDate.Date;
            var normalized = NormalizeCode(code, Monthly);

            return normalized switch
            {
                Daily => (today, today),
                Weekly => (today.AddDays(-6), today),
                Last30Days => (today.AddDays(-29), today),
                Monthly => (new DateTime(today.Year, today.Month, 1), today),
                Last3Months => (today.AddMonths(-3).AddDays(1), today),
                Last6Months => (today.AddMonths(-6).AddDays(1), today),
                Last1Year => (today.AddYears(-1).AddDays(1), today),
                _ => (today.AddMonths(-1).AddDays(1), today)
            };
        }

        private static string ToMonthDisplay(DateTime referenceDate)
        {
            var month = referenceDate.ToString("MMMM", AppLocalization.CurrentCulture);
            var monthName = AppLocalization.CurrentCulture.TextInfo.ToTitleCase(month);
            return AppLocalization.F("main.summary.range.currentMonth", monthName);
        }
    }
}
