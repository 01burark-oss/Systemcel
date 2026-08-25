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
        Task<YonetimOdemeIncelemeDto> GetOdemeIncelemeAsync(string? durum = null, bool sadeceHatalar = false, int limit = 100, CancellationToken ct = default);
        Task<MuhasebeciAktarimListeDto> GetMuhasebeciAktarimlariAsync(string aktarimDonemi, int? muhasebeciIsletmeId = null, CancellationToken ct = default);
        Task<MuhasebeciAktarimOzetDto> CompleteMuhasebeciAktarimiAsync(int muhasebeciIsletmeId, MuhasebeciAktarimTamamlaRequest request, CancellationToken ct = default);
        Task<DestekTalebiListeDto> GetDestekTalepleriAsync(CancellationToken ct = default);
        Task<DestekTalebiDto> UpdateDestekTalebiAsync(int destekTalebiId, DestekTalebiGuncelleRequest request, CancellationToken ct = default);
        Task<EntitlementOverrideResult> ApplyEntitlementOverrideAsync(int isletmeId, EntitlementOverrideRequest request, CancellationToken ct = default);
    }
}
