using System;
using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public sealed class MonthlyReportExportOptions
    {
        public bool IncludeExcel { get; set; } = true;
        public bool IncludeHtml { get; set; } = true;
        public bool CreateZip { get; set; } = true;
        public bool IncludeFaturalar { get; set; } = true;
        public bool IncludeCari { get; set; } = true;
        public bool IncludeStok { get; set; } = true;
        public bool IncludeGelirGider { get; set; } = true;
        public bool IncludeKdv { get; set; } = true;
    }

    public interface IOnMuhasebeReportService
    {
        Task<string> CreateMonthlyExportAsync(DateTime month, string outputDirectory, MonthlyReportExportOptions? options = null, CancellationToken ct = default);
    }
}
