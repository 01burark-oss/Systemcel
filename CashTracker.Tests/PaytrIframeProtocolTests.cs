using CashTracker.Infrastructure.Payments;
using System.Net;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaytrIframeProtocolTests
{
    private const string Key = "test-merchant-key";
    private const string Salt = "test-merchant-salt";

    [Fact]
    public void TokenMatchesPaytrFieldOrderAndHmacSha256Vector()
    {
        var input = new PaytrIframeTokenInput(
            "123456", "203.0.113.7", "ORDER42", "buyer@example.com", 3456,
            "W1siU2VydmljZSIsIjM0LjU2IiwxXV0=", true, 0, "TL", true);

        Assert.Equal("piT0YeiDzBl7uJnCl7Bf86rIbI/B2hPYm0qzeGqJsVw=",
            PaytrIframeProtocol.CreateToken(input, Key, Salt));
    }

    [Fact]
    public void CallbackRequiresTheSignedOrderStatusAndExactMinorUnitString()
    {
        const string hash = "OwAj7S+NxWj9tCgCJBUoXPnKtIsFEGoK05SP8XfPZGM=";
        Assert.True(PaytrIframeProtocol.VerifyCallback(
            new PaytrIframeCallback("ORDER42", "success", "3456", hash), Key, Salt));
        Assert.False(PaytrIframeProtocol.VerifyCallback(
            new PaytrIframeCallback("ORDER43", "success", "3456", hash), Key, Salt));
        Assert.False(PaytrIframeProtocol.VerifyCallback(
            new PaytrIframeCallback("ORDER42", "failed", "3456", hash), Key, Salt));
        Assert.False(PaytrIframeProtocol.VerifyCallback(
            new PaytrIframeCallback("ORDER42", "success", "3457", hash), Key, Salt));
        Assert.False(PaytrIframeProtocol.VerifyCallback(
            new PaytrIframeCallback("ORDER42", "success", "3456", "not-base64"), Key, Salt));
        Assert.False(PaytrIframeProtocol.VerifyCallback(
            new PaytrIframeCallback("ORDER42", "success", new string('9', 100), hash), Key, Salt));
    }

    [Fact]
    public void MoneyConversionRejectsFractionalKurus()
    {
        Assert.Equal(3456, PaytrIframeProtocol.ToKurus(34.56m));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaytrIframeProtocol.ToKurus(34.561m));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaytrIframeProtocol.ToKurus(0m));
    }

    [Fact]
    public void TokenRejectsInvalidOrderIdAndBasket()
    {
        var input = new PaytrIframeTokenInput(
            "123456", "203.0.113.7", "ORDER42", "buyer@example.com", 3456,
            "W1siU2VydmljZSIsIjM0LjU2IiwxXV0=", true, 0, "TL", true);

        Assert.Throws<ArgumentException>(() => PaytrIframeProtocol.CreateToken(input with { MerchantOrderId = "ORDER-42" }, Key, Salt));
        Assert.Throws<ArgumentException>(() => PaytrIframeProtocol.CreateToken(input with { BasketBase64 = "bad" }, Key, Salt));
    }

    [Fact]
    public void TokenRequestContainsRequiredFieldsAndRejectsUnsafeReturnUrls()
    {
        var request = NewCheckoutRequest();
        var fields = PaytrIframeClient.BuildTokenFields(request, Key, Salt);

        Assert.Equal("piT0YeiDzBl7uJnCl7Bf86rIbI/B2hPYm0qzeGqJsVw=", fields["paytr_token"]);
        Assert.Equal("3456", fields["payment_amount"]);
        Assert.Equal("ORDER42", fields["merchant_oid"]);
        Assert.Equal("https://systemcel.app/odeme/sonuc", fields["merchant_ok_url"]);
        Assert.Equal("0", fields["debug_on"]);
        Assert.Throws<ArgumentException>(() => PaytrIframeClient.BuildTokenFields(
            request with { FailureUrl = new Uri("http://systemcel.app/odeme/hata") }, Key, Salt));
    }

    [Theory]
    [InlineData("{\"status\":\"success\",\"token\":\"TOKEN123\"}", true)]
    [InlineData("{\"status\":\"failed\",\"reason\":\"rejected\"}", false)]
    [InlineData("{\"status\":\"success\",\"token\":\"../bad\"}", false)]
    public async Task CheckoutUrlUsesOnlyValidIssuedToken(string body, bool accepted)
    {
        using var http = new HttpClient(new StubHandler(body));
        var client = new PaytrIframeClient(http);
        if (accepted)
        {
            var url = await client.CreateCheckoutUrlAsync(NewCheckoutRequest(), Key, Salt);
            Assert.Equal("https://www.paytr.com/odeme/guvenli/TOKEN123", url.AbsoluteUri);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.CreateCheckoutUrlAsync(NewCheckoutRequest(), Key, Salt));
        }
    }

    private static PaytrIframeCheckoutRequest NewCheckoutRequest() => new(
        new PaytrIframeTokenInput(
            "123456", "203.0.113.7", "ORDER42", "buyer@example.com", 3456,
            "W1siU2VydmljZSIsIjM0LjU2IiwxXV0=", true, 0, "TL", true),
        "Buyer Example", "Test address", "5550000000",
        new Uri("https://systemcel.app/odeme/sonuc"),
        new Uri("https://systemcel.app/odeme/hata"));

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://www.paytr.com/odeme/api/get-token", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        }
    }
}
