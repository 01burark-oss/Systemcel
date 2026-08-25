using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface ISubeKurService
{
    Task<SubeKurDurumuDto> GetContextAsync(CancellationToken ct = default);
    Task<SubeFinansOzetiDto> GetFinancialSummaryAsync(int? branchId = null, CancellationToken ct = default);
    Task<SubeOlusturResult> CreateBranchAsync(SubeOlusturRequest request, string idempotencyKey, CancellationToken ct = default);
    Task SetActiveBranchAsync(int branchId, CancellationToken ct = default);
    Task<KurKaydetResult> SaveRateAsync(DovizKuruKaydetRequest request, string idempotencyKey, CancellationToken ct = default);
    Task<IslemKurSnapshot> ResolveSnapshotAsync(string? currency, decimal originalAmount, CancellationToken ct = default);
}
