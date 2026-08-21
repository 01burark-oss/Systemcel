using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IOdemeHatirlatmaService
{
    Task<OdemeHatirlatmaOnizleme> GetPreviewAsync(int faturaId, CancellationToken ct = default);
    Task<OdemeHatirlatmaGonderimSonucu> SendAsync(int faturaId, CancellationToken ct = default);
}
