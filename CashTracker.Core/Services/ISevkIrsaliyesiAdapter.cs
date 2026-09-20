using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface ISevkIrsaliyesiAdapter
{
    Task<SevkIrsaliyesiSonucu> GonderAsync(SevkIrsaliyesiGonderRequest request, CancellationToken ct = default);
    Task<SevkIrsaliyesiSonucu> DurumSorgulaAsync(int isletmeId, string saglayiciBelgeReferansi, CancellationToken ct = default);
    Task<SevkIrsaliyesiSonucu> YanitGonderAsync(SevkIrsaliyesiYanitRequest request, CancellationToken ct = default);
}
