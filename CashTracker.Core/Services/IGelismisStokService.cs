using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IGelismisStokService
    {
        Task<StokDefteriDto> GetAsync(int limit = 100, CancellationToken ct = default);
        Task<StokDepoDto> CreateWarehouseAsync(StokDepoOlusturRequest request, CancellationToken ct = default);
        Task<StokDefterIslemResult> CreateMovementAsync(StokHareketIslemRequest request, string idempotencyKey, CancellationToken ct = default);
        Task<StokDefterIslemResult> TransferAsync(StokTransferRequest request, string idempotencyKey, CancellationToken ct = default);
        Task<StokDefterIslemResult> CountAsync(StokSayimRequest request, string idempotencyKey, CancellationToken ct = default);
        Task<StokDefterIslemResult> ReverseAsync(int operationId, StokTersKayitRequest request, string idempotencyKey, CancellationToken ct = default);
    }
}
