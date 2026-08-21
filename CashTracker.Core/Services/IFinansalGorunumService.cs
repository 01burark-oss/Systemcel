using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IFinansalGorunumService
    {
        Task<FinansalGorunum> GetAsync(
            DateTime referenceDate,
            int projectionWeeks = 13,
            CancellationToken ct = default);

        Task<List<NakitPlanKalemi>> GetPlanItemsAsync(CancellationToken ct = default);
        Task<int> CreatePlanItemAsync(NakitPlanKalemiKaydetRequest request, CancellationToken ct = default);
        Task<bool> UpdatePlanItemAsync(int id, NakitPlanKalemiKaydetRequest request, CancellationToken ct = default);
        Task<bool> DeletePlanItemAsync(int id, CancellationToken ct = default);
    }
}
