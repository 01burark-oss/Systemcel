using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ISystemcelYonetimService
    {
        Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);
        Task<MuhasebeciBasvuruListeDto> GetMuhasebeciBasvurulariAsync(string? durum = null, CancellationToken ct = default);
        Task<MuhasebeciBasvuruDto> ApproveMuhasebeciBasvurusuAsync(int kullaniciId, CancellationToken ct = default);
        Task<MuhasebeciBasvuruDto> RejectMuhasebeciBasvurusuAsync(int kullaniciId, MuhasebeciBasvuruRedRequest request, CancellationToken ct = default);
    }
}
