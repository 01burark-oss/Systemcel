using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class MobileReceiptOcrSecurityTests
{
    [Fact]
    public async Task AnalyzeReceiptWithQuotaAsync_WhenAiFeatureIsUnavailable_DoesNotCallPaidProvider()
    {
        var quota = new UsageQuotaStub(new AiUsageStatus
        {
            AiAktif = false,
            IzinVerildi = false,
            PlanKodu = PlanKodlari.IsletmeBaslangic,
            Mesaj = "Yapay zeka özelliği bu planda kullanılamaz."
        });
        var receiptOcr = new ReceiptOcrStub();

        var error = await Assert.ThrowsAsync<EntitlementViolationException>(() =>
            MobilTaramaApi.AnalyzeReceiptWithQuotaAsync(
                quota,
                receiptOcr,
                new ReceiptOcrRequest { ImageBytes = [1] }));

        Assert.Equal(EntitlementErrorCodes.FeatureNotAvailable, error.Code);
        Assert.Equal(1, quota.ConsumeCount);
        Assert.Equal(0, receiptOcr.CallCount);
    }

    [Fact]
    public async Task AnalyzeReceiptWithQuotaAsync_WhenLimitIsReached_DoesNotCallPaidProvider()
    {
        var quota = new UsageQuotaStub(new AiUsageStatus
        {
            AiAktif = true,
            IzinVerildi = false,
            LimitAsildi = true,
            PlanKodu = PlanKodlari.IsletmeBaslangic,
            Limit = 5,
            Kullanilan = 5,
            Mesaj = "Aylık AI kullanım limitiniz doldu."
        });
        var receiptOcr = new ReceiptOcrStub();

        var error = await Assert.ThrowsAsync<EntitlementViolationException>(() =>
            MobilTaramaApi.AnalyzeReceiptWithQuotaAsync(
                quota,
                receiptOcr,
                new ReceiptOcrRequest { ImageBytes = [1] }));

        Assert.Equal(EntitlementErrorCodes.LimitReached, error.Code);
        Assert.Equal(EntitlementLimits.AiMessage, error.LimitName);
        Assert.Equal(1, quota.ConsumeCount);
        Assert.Equal(0, receiptOcr.CallCount);
    }

    [Fact]
    public async Task AnalyzeReceiptWithQuotaAsync_WhenUsageIsAllowed_ConsumesQuotaBeforeCallingProvider()
    {
        var quota = new UsageQuotaStub(new AiUsageStatus
        {
            AiAktif = true,
            IzinVerildi = true,
            PlanKodu = PlanKodlari.IsletmeBuyume,
            Limit = 100,
            Kullanilan = 1
        });
        var receiptOcr = new ReceiptOcrStub();

        var result = await MobilTaramaApi.AnalyzeReceiptWithQuotaAsync(
            quota,
            receiptOcr,
            new ReceiptOcrRequest { ImageBytes = [1] });

        Assert.Same(receiptOcr.Result, result);
        Assert.Equal(1, quota.ConsumeCount);
        Assert.Equal(1, receiptOcr.CallCount);
    }

    private sealed class UsageQuotaStub(AiUsageStatus status) : IAiUsageQuotaService
    {
        public int ConsumeCount { get; private set; }

        public Task<AiUsageStatus> GetStatusAsync(CancellationToken ct = default) => Task.FromResult(status);

        public Task<AiUsageStatus> ConsumeAsync(CancellationToken ct = default)
        {
            ConsumeCount++;
            return Task.FromResult(status);
        }
    }

    private sealed class ReceiptOcrStub : IReceiptOcrService
    {
        public ReceiptOcrResult Result { get; } = new();
        public int CallCount { get; private set; }

        public Task<ReceiptOcrResult> AnalyzeReceiptAsync(ReceiptOcrRequest request, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(Result);
        }
    }
}
