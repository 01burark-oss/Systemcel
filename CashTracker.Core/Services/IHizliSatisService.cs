using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IHizliSatisService
    {
        Task<HizliSatisResult> CreateAsync(HizliSatisCreateRequest request, CancellationToken ct = default);
    }
}
