using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IStokService
    {
        Task<decimal> GetCurrentStockAsync(int urunHizmetId, CancellationToken ct = default);
        Task<List<StokHareket>> GetRecentMovementsAsync(int limit = 20, CancellationToken ct = default);
        Task<StokHareketResult> CreateMovementAsync(StokHareketCreateRequest request, CancellationToken ct = default);
    }
}
