using System.Threading;

namespace CashTracker.Infrastructure.Services;

internal static class TelegramAiBusinessScope
{
    private static readonly AsyncLocal<Scope?> Active = new();

    internal static Scope? Current => Active.Value;

    internal static IDisposable Enter(int businessId, string userRef)
    {
        if (businessId <= 0 || string.IsNullOrWhiteSpace(userRef))
            throw new UnauthorizedAccessException("Telegram AI için işletme üyeliği gerekir.");

        var previous = Active.Value;
        Active.Value = new Scope(businessId, userRef);
        return new Restore(previous);
    }

    internal sealed record Scope(int BusinessId, string UserRef);

    private sealed class Restore(Scope? previous) : IDisposable
    {
        public void Dispose() => Active.Value = previous;
    }
}
