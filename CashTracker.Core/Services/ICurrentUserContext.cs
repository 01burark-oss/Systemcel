namespace CashTracker.Core.Services
{
    public interface ICurrentUserContext
    {
        CurrentUserIdentity? GetCurrentUser();
    }

    public sealed record CurrentUserIdentity(
        string ProviderUserId,
        string? Email,
        string? FullName);
}
