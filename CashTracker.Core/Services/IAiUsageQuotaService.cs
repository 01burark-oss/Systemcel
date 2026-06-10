using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IAiUsageQuotaService
    {
        Task<AiUsageStatus> GetStatusAsync(CancellationToken ct = default);

        Task<AiUsageStatus> ConsumeAsync(CancellationToken ct = default);
    }
}
