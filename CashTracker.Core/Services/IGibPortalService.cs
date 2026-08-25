using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IGibPortalService
    {
        Task<GibPortalSettingsModel?> GetSettingsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<GibPortalLogModel>> GetRecentLogsAsync(int limit = 10, CancellationToken ct = default);
        Task SaveSettingsAsync(GibPortalSaveSettingsRequest request, CancellationToken ct = default);
        Task<GibPortalResult> TestConnectionAsync(CancellationToken ct = default);
        Task<GibPortalResult> CreatePortalDraftAsync(int faturaId, CancellationToken ct = default);
        Task<GibPortalResult> StartSmsApprovalAsync(int faturaId, CancellationToken ct = default);
        Task<GibPortalResult> CompleteSmsApprovalAsync(int faturaId, string operationId, string smsCode, CancellationToken ct = default);
    }
}
