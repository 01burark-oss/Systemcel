using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IBildirimService
{
    Task<IReadOnlyList<BildirimGorunumu>> SyncAndListAsync(int isletmeId, string kullaniciRef, IReadOnlyCollection<BildirimSnapshot> snapshots, CancellationToken ct = default);
    Task<int> MarkReadAsync(int isletmeId, string kullaniciRef, int bildirimId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default);
    Task<BildirimTercihModeli> GetPreferencesAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default);
    Task<BildirimTercihModeli> SavePreferencesAsync(int isletmeId, string kullaniciRef, BildirimTercihModeli model, CancellationToken ct = default);
}

public interface IBildirimOutboxService
{
    Task EnqueueAsync(int isletmeId, string kullaniciRef, int? bildirimId, string idempotencyAnahtari, string kanal, string payloadJson, DateTime nowUtc, CancellationToken ct = default);
    Task<IReadOnlyList<BildirimOutboxClaim>> ClaimAsync(int batchSize, DateTime nowUtc, TimeSpan lease, CancellationToken ct = default);
    Task CompleteAsync(long id, string claimToken, DateTime nowUtc, CancellationToken ct = default);
    Task MarkUnconfiguredAsync(long id, string claimToken, DateTime nowUtc, CancellationToken ct = default);
    Task FailAsync(long id, string claimToken, string errorCode, DateTime nowUtc, int maxAttempts = 5, CancellationToken ct = default);
}

public interface IBildirimKanalAdapter
{
    string Kanal { get; }
    bool IsConfigured { get; }
    Task SendAsync(BildirimOutboxClaim claim, CancellationToken ct = default);
}
