namespace CashTracker.Core.Services;

public interface ISubscriptionCancellationRefundService
{
    Task<CancellationRefundQueueDto> ListAsync(CancellationToken ct = default);
    Task ApproveAsync(int subscriptionId, CancellationToken ct = default);
    Task DispatchAsync(int subscriptionId, CancellationToken ct = default);
    Task ReconcileAsync(int subscriptionId, CancellationToken ct = default);
}

public sealed record CancellationRefundQueueDto(bool TestIslemleriAcik, IReadOnlyList<CancellationRefundItemDto> Talepler);

public sealed record CancellationRefundItemDto(
    int AbonelikId, string IsletmeAdi, string HesapTipi, string PlanKodu,
    DateTime? IptalAt, DateTime? DonemBitisAt, int KalanAySayisi, decimal? Tutar,
    string ParaBirimi, string Durum, bool Onaylanabilir, bool Gonderilebilir, bool Sorgulanabilir, DateTime? OnayAt);
