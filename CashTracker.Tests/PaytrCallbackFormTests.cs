using System.Text;
using System.Text.Json;
using Systemcel.Api.Api;
using Systemcel.Api;
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
    public void ProviderRemainsClosedWithoutHttpsAndExplicitTestBusiness()
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
        Assert.True(configured.AllowsPaytrTestBusiness(42));
        Assert.False(configured.AllowsPaytrTestBusiness(43));
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
}
