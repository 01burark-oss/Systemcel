namespace CashTracker.Core.Services;

public sealed record TelegramBildirimBaglantisiDurumu(
    bool Bagli,
    string ChatId,
    DateTime? BaglandiAt);

public interface ITelegramBildirimBaglantiService
{
    Task<TelegramPairingCode> EnsureCodeAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default);
    Task<TelegramPairingCode> RenewCodeAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default);
    Task<TelegramBildirimBaglantisiDurumu> GetStateAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default);
    Task<bool> TryCompleteAsync(string code, long chatId, long? telegramUserId, CancellationToken ct = default);
    Task ClearAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default);
}
