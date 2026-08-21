using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IOdemeHatirlatmaSender
{
    bool IsConfigured { get; }
    Task<bool> SendAsync(OdemeHatirlatmaIcerigi reminder, CancellationToken ct = default);
}
