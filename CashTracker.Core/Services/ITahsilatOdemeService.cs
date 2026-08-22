using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ITahsilatOdemeService
    {
        Task<int> CreateAsync(TahsilatOdemeRequest request, CancellationToken ct = default);
        Task UpdateMovementAsync(int cariHareketId, TahsilatOdemeHareketGuncelleRequest request, CancellationToken ct = default);
        Task DeleteMovementAsync(int cariHareketId, CancellationToken ct = default);
    }
}
