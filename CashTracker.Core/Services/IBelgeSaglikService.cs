using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IBelgeSaglikService
    {
        Task<BelgeSaglikOzeti> GetAsync(
            int isletmeId,
            DateTime? referenceDate = null,
            CancellationToken ct = default);
    }
}
