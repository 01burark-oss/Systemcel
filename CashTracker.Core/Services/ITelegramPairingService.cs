using System;

namespace CashTracker.Core.Services
{
    public sealed record TelegramPairingCode(string Code, DateTime CreatedUtc, DateTime ExpiresUtc)
    {
        public int MinutesLeft =>
            Math.Max(1, (int)Math.Ceiling((ExpiresUtc - DateTime.UtcNow).TotalMinutes));
    }

    public interface ITelegramPairingService
    {
        TelegramPairingCode EnsureActiveCode();
        TelegramPairingCode RenewCode();
        bool TryCompletePairing(string code, long chatId, long? userId, out string message);
        void ClearPairing();
    }
}
