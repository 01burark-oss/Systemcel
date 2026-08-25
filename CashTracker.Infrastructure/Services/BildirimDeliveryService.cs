using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services;

public sealed class BildirimDeliveryService
{
    private readonly IBildirimOutboxService _outbox;
    private readonly IReadOnlyDictionary<string, IBildirimKanalAdapter> _adapters;

    public BildirimDeliveryService(IBildirimOutboxService outbox, IEnumerable<IBildirimKanalAdapter> adapters)
    {
        _outbox = outbox;
        _adapters = adapters.ToDictionary(x => x.Kanal, StringComparer.Ordinal);
    }

    public async Task<int> DispatchAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var claims = await _outbox.ClaimAsync(25, nowUtc, TimeSpan.FromMinutes(2), ct);
        foreach (var claim in claims)
        {
            if (!_adapters.TryGetValue(claim.Kanal, out var adapter) || !adapter.IsConfigured)
            {
                await _outbox.MarkUnconfiguredAsync(claim.Id, claim.ClaimToken, nowUtc, ct);
                continue;
            }
            try
            {
                await adapter.SendAsync(claim, ct);
                await _outbox.CompleteAsync(claim.Id, claim.ClaimToken, nowUtc, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch
            {
                await _outbox.FailAsync(claim.Id, claim.ClaimToken, "delivery_failed", nowUtc, ct: ct);
            }
        }
        return claims.Count;
    }
}

public sealed class UygulamaBildirimAdapter : IBildirimKanalAdapter
{
    public string Kanal => BildirimKanallari.Uygulama;
    public bool IsConfigured => true;
    public Task SendAsync(BildirimOutboxClaim claim, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class YapilandirilmamisBildirimAdapter : IBildirimKanalAdapter
{
    public YapilandirilmamisBildirimAdapter(string kanal) => Kanal = kanal;
    public string Kanal { get; }
    public bool IsConfigured => false;
    public Task SendAsync(BildirimOutboxClaim claim, CancellationToken ct = default) =>
        throw new InvalidOperationException("Bildirim kanalı yapılandırılmadı.");
}
