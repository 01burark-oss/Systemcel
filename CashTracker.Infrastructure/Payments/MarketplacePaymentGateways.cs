using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Payments;

public sealed class FakeMarketplacePaymentGateway : IMarketplacePaymentGateway
{
    public string Name => "Fake";
    public bool IsConfigured => true;

    public Task<MarketplacePaymentResult> CollectAsync(MarketplacePaymentCommand command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (command.Amount <= 0m)
            return Task.FromResult(new MarketplacePaymentResult(Name, string.Empty, false, "Ödenecek tutar bulunamadı."));

        var transactionId = $"fake_market_{Hash($"{command.OrderReference}:{command.IdempotencyKey}")[..24]}";
        return Task.FromResult(new MarketplacePaymentResult(Name, transactionId, true));
    }

    public Task<MarketplacePaymentResult> RefundAsync(
        string providerTransactionId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(providerTransactionId) || amount <= 0m)
            return Task.FromResult(new MarketplacePaymentResult(Name, string.Empty, false, "İade bilgileri geçersiz."));

        var transactionId = $"fake_refund_{Hash($"{providerTransactionId}:{idempotencyKey}")[..24]}";
        return Task.FromResult(new MarketplacePaymentResult(Name, transactionId, true));
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class UnconfiguredMarketplacePaymentGateway : IMarketplacePaymentGateway
{
    public string Name => "Yapılandırılmadı";
    public bool IsConfigured => false;

    public Task<MarketplacePaymentResult> CollectAsync(MarketplacePaymentCommand command, CancellationToken ct = default) =>
        Task.FromResult(new MarketplacePaymentResult(Name, string.Empty, false, "Kartla ödeme henüz kullanıma açılmadı."));

    public Task<MarketplacePaymentResult> RefundAsync(
        string providerTransactionId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default) =>
        Task.FromResult(new MarketplacePaymentResult(Name, string.Empty, false, "Kartla iade henüz kullanıma açılmadı."));
}
