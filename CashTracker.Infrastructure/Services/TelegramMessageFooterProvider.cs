using System;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramMessageFooterProvider : ITelegramMessageFooterProvider
    {
        private readonly IAiUsageQuotaService _quotaService;

        public TelegramMessageFooterProvider(IAiUsageQuotaService quotaService)
        {
            _quotaService = quotaService;
        }

        public async Task<string> BuildFooterAsync(CancellationToken ct = default)
        {
            var usage = await _quotaService.GetStatusAsync(ct);
            if (!string.Equals(usage.PlanKodu, PlanKodlari.IsletmeBaslangic, StringComparison.OrdinalIgnoreCase) ||
                !usage.Limit.HasValue)
            {
                return string.Empty;
            }

            return $"Kalan Telegram AI hakkı: {usage.Kalan.GetValueOrDefault()}/{usage.Limit.Value}";
        }
    }
}
