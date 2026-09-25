using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IEcbKurService
{
    Task<EcbKurBulteniDto> GetLatestAsync(CancellationToken ct = default);
}
