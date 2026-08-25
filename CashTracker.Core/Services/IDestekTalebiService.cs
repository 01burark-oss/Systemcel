using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IDestekTalebiService
{
    Task<DestekTalebiListeDto> GetMineAsync(CancellationToken ct = default);
    Task<DestekTalebiDto> CreateAsync(DestekTalebiOlusturRequest request, string idempotencyKey, CancellationToken ct = default);
}
