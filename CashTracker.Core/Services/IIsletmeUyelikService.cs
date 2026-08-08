using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IIsletmeUyelikService
{
    Task<IsletmeUyelikDavetDto> CreateInviteAsync(IsletmeUyelikDavetRequest request, CancellationToken ct = default);
}
