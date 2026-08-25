using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IIsletmeUyelikService
{
    Task<IsletmeUyelikListeDto> GetMembershipsAsync(CancellationToken ct = default);
    Task<IsletmeUyelikDavetDto> CreateInviteAsync(IsletmeUyelikDavetRequest request, CancellationToken ct = default);
    Task<IsletmeUyelikListeDto> AcceptInviteAsync(string inviteCode, CancellationToken ct = default);
    Task<IsletmeUyelikListeDto> UpdateRoleAsync(int membershipId, string role, CancellationToken ct = default);
    Task<IsletmeUyelikListeDto> RemoveAsync(int membershipId, CancellationToken ct = default);
    Task<IsletmeUyelikListeDto> TransferOwnershipAsync(int membershipId, CancellationToken ct = default);
}
