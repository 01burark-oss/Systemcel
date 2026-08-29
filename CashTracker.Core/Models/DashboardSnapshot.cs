using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class DashboardSnapshot
    {
        public string ActiveBusinessName { get; set; } = string.Empty;
        public PeriodSummary DailySummary { get; set; } = new();
        public PeriodSummary PrimaryRangeSummary { get; set; } = new();
        public PeriodSummary SecondaryRangeSummary { get; set; } = new();
        public PeriodSummary MonthlySummary { get; set; } = new();
        public PeriodSummary YearlySummary { get; set; } = new();
        public List<DailyPaymentMethodBreakdown> DailyPaymentMethodBreakdowns { get; set; } = new();
        public BrutKarMarjiOzeti GrossMargin { get; set; } = new();
    }
}
