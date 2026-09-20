using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services;

public sealed class UnconfiguredSevkIrsaliyesiAdapter : ISevkIrsaliyesiAdapter
{
    private const string Message = "e-İrsaliye sağlayıcısı yapılandırılmadı; manuel belge numarası ve dosya akışını kullanın.";

    public Task<SevkIrsaliyesiSonucu> GonderAsync(SevkIrsaliyesiGonderRequest request, CancellationToken ct = default) =>
        Task.FromResult(SevkIrsaliyesiSonucu.Yapilandirilmadi(Message));

    public Task<SevkIrsaliyesiSonucu> DurumSorgulaAsync(int isletmeId, string saglayiciBelgeReferansi, CancellationToken ct = default) =>
        Task.FromResult(SevkIrsaliyesiSonucu.Yapilandirilmadi(Message));

    public Task<SevkIrsaliyesiSonucu> YanitGonderAsync(SevkIrsaliyesiYanitRequest request, CancellationToken ct = default) =>
        Task.FromResult(SevkIrsaliyesiSonucu.Yapilandirilmadi(Message));
}
