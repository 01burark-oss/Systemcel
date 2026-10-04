using System.Text;
using System.Text.Json;
using Systemcel.Api.Api;
using Systemcel.Api;
using CashTracker.Infrastructure.Payments;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaytrCallbackFormTests
{
    [Fact]
    public void ParsesUrlEncodedCallbackWithoutLosingBase64Plus()
    {
        Assert.True(BillingApi.TryParsePaytrCallbackForm(
            "merchant_oid=ORDER42&status=success&total_amount=3456&hash=ab%2Bcd%3D", out var envelope));
        Assert.Equal("ab+cd=", envelope.Signature);
        using var payload = JsonDocument.Parse(envelope.Payload);
        Assert.Equal("ORDER42", payload.RootElement.GetProperty("MerchantOrderId").GetString());
        Assert.Equal("3456", payload.RootElement.GetProperty("TotalAmountKurus").GetString());
    }

    [Theory]
    [InlineData("merchant_oid=A&merchant_oid=B&status=success&total_amount=1&hash=abc")]
    [InlineData("merchant_oid=A&status=success&total_amount=1")]
    [InlineData("merchant_oid=A&status=&total_amount=1&hash=abc")]
    public void RejectsMissingOrDuplicateSignedFields(string form)
    {
        Assert.False(BillingApi.TryParsePaytrCallbackForm(form, out _));
    }

    [Fact]
    public async Task RejectsBodyAboveFourKilobytes()
    {
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 4097)));
        Assert.Null(await BillingApi.ReadBoundedPaytrBodyAsync(body, CancellationToken.None));
    }

    [Fact]
    public void TestProviderRemainsClosedWithoutHttpsAndExplicitTestBusiness()
    {
        var configured = new PaymentRuntimeOptions
        {
            Provider = "PayTR",
            PaytrMerchantId = "123456",
            PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt",
            PublicBaseUrl = "https://systemcel.example",
            PaytrTestBusinessIds = [42]
        };
        Assert.True(configured.UsesPaytrProvider);
        Assert.True(configured.AllowsPaytrTestBusiness(42));
        Assert.False(configured.AllowsPaytrTestBusiness(43));
        Assert.True(configured.AllowsPaytrBusiness(42));
        Assert.False(configured.AllowsPaytrBusiness(43));
        Assert.False(new PaymentRuntimeOptions
        {
            Provider = "PayTR", PaytrMerchantId = "123456", PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt", PublicBaseUrl = "https://systemcel.example"
        }.UsesPaytrProvider);
        Assert.False(new PaymentRuntimeOptions
        {
            Provider = "PayTR", PaytrMerchantId = "123456", PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt", PublicBaseUrl = "http://systemcel.example",
            PaytrTestBusinessIds = [42]
        }.UsesPaytrProvider);
        Assert.False(new PaymentRuntimeOptions
        {
            Provider = "PayTR", PaytrMerchantId = "123456", PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt", PublicBaseUrl = "https://systemcel.example",
            PaytrTestBusinessIds = [42], PaytrTestMode = false
        }.UsesPaytrProvider);
    }

    [Fact]
    public void LiveProviderRemainsClosedWithoutExplicitEnablementAndLiveAllowlist()
    {
        var configured = new PaymentRuntimeOptions
        {
            Provider = "PayTR",
            PaytrMerchantId = "123456",
            PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt",
            PaytrTestMode = false,
            PublicBaseUrl = "https://systemcel.example",
            PaytrLiveBusinessIds = [84]
        };
        Assert.False(configured.UsesPaytrProvider);
        Assert.False(configured.AllowsPaytrBusiness(84));

        Assert.False(new PaymentRuntimeOptions
        {
            Provider = "PayTR",
            PaytrMerchantId = "123456",
            PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt",
            PaytrLiveEnabled = true,
            PaytrTestMode = false,
            PublicBaseUrl = "https://systemcel.example"
        }.UsesPaytrProvider);
    }

    [Fact]
    public void EnabledLiveProviderAllowsOnlyLiveAllowlistedBusinesses()
    {
        var configured = new PaymentRuntimeOptions
        {
            Provider = "PayTR",
            PaytrMerchantId = "123456",
            PaytrMerchantKey = "live-key",
            PaytrMerchantSalt = "live-salt",
            PaytrLiveEnabled = true,
            PaytrTestMode = false,
            PublicBaseUrl = "https://systemcel.example",
            PaytrTestBusinessIds = [42],
            PaytrLiveBusinessIds = [84],
            PaytrLiveRefundResponseMode = PaytrRefundResponseMode.LiveZero
        };

        Assert.True(configured.UsesPaytrProvider);
        Assert.True(configured.AllowsPaytrBusiness(84));
        Assert.False(configured.AllowsPaytrBusiness(42));
        Assert.False(configured.AllowsPaytrTestBusiness(84));
        Assert.False(configured.AllowsPaytrTestBusiness(42));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("unconfirmed", false)]
    [InlineData("test", false)]
    [InlineData("1", false)]
    [InlineData("zero", true)]
    [InlineData("absent", true)]
    public void UnconfirmedLiveRefundContractBlocksNewCheckoutButPreservesCallbackProvider(string? setting, bool allowed)
    {
        var configured = new PaymentRuntimeOptions
        {
            Provider = "PayTR", PaytrMerchantId = "123456", PaytrMerchantKey = "live-key",
            PaytrMerchantSalt = "live-salt", PaytrLiveEnabled = true, PaytrTestMode = false,
            PublicBaseUrl = "https://systemcel.example", PaytrLiveBusinessIds = [84],
            PaytrLiveRefundResponseMode = PaytrRefundResponseContract.ParseLive(setting)
        };
        Assert.True(configured.UsesPaytrProvider);
        Assert.Equal(allowed, configured.AllowsPaytrBusiness(84));
        Assert.False(configured.AllowsPaytrBusiness(85));
    }

    [Fact]
    public void LiveConfigurationRejectsUnsafeBaseUrlsAndNonpositiveAllowlist()
    {
        foreach (var baseUrl in new[]
                 {
                     "http://systemcel.example",
                     "https://user@systemcel.example",
                     "https://systemcel.example?next=/",
                     "https://systemcel.example/#fragment"
                 })
        {
            Assert.False(CreateLiveOptions(baseUrl, [84]).UsesPaytrProvider);
        }

        Assert.False(CreateLiveOptions("https://systemcel.example", [0, -1]).UsesPaytrProvider);
    }

    [Fact]
    public void PausedLiveCheckoutKeepsProviderAvailableForPendingCallbacksAndQueries()
    {
        var configured = new PaymentRuntimeOptions
        {
            Provider = "PayTR", PaytrMerchantId = "123456", PaytrMerchantKey = "live-key",
            PaytrMerchantSalt = "live-salt", PaytrLiveEnabled = true, PaytrTestMode = false,
            PaytrLiveCheckoutPaused = true, PublicBaseUrl = "https://systemcel.example",
            PaytrLiveBusinessIds = [84]
        };

        Assert.True(configured.UsesPaytrProvider);
        Assert.False(configured.AllowsPaytrBusiness(84));
    }

    [Fact]
    public void LiveEnablementAndAllowlistDoNotChangeTestModeScope()
    {
        var configured = new PaymentRuntimeOptions
        {
            Provider = "PayTR",
            PaytrMerchantId = "123456",
            PaytrMerchantKey = "test-key",
            PaytrMerchantSalt = "test-salt",
            PaytrLiveEnabled = true,
            PaytrTestMode = true,
            PublicBaseUrl = "https://systemcel.example",
            PaytrTestBusinessIds = [42],
            PaytrLiveBusinessIds = [84]
        };

        Assert.True(configured.UsesPaytrProvider);
        Assert.True(configured.AllowsPaytrTestBusiness(42));
        Assert.False(configured.AllowsPaytrTestBusiness(84));
        Assert.True(configured.AllowsPaytrBusiness(42));
        Assert.False(configured.AllowsPaytrBusiness(84));
    }

    private static PaymentRuntimeOptions CreateLiveOptions(string baseUrl, int[] businessIds) => new()
    {
        Provider = "PayTR",
        PaytrMerchantId = "123456",
        PaytrMerchantKey = "live-key",
        PaytrMerchantSalt = "live-salt",
        PaytrLiveEnabled = true,
        PaytrTestMode = false,
        PublicBaseUrl = baseUrl,
        PaytrLiveBusinessIds = businessIds
    };
}
