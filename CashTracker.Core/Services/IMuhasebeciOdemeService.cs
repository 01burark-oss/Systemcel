using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IMuhasebeciOdemeService
    {
        Task<MuhasebeciOdemeOzetiDto> GetAsync(int talepId, int musteriIsletmeId, CancellationToken ct = default);
        Task<MuhasebeciOdemeCheckoutResult> BeginCheckoutAsync(
            MuhasebeciOdemeCheckoutCommand command,
            CancellationToken ct = default);
        Task<int> EnsureDuePeriodsAsync(DateTime now, CancellationToken ct = default);
    }
}
