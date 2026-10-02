using CashTracker.Infrastructure.Payments;
using CashTracker.Core.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task SubscriptionProviderUsesStableOrderIdAndRequiresSignedCallback()
    {
        using var http = new HttpClient(new StubHandler("{\"status\":\"success\",\"token\":\"TOKEN123\"}"));
        var provider = new PaytrSubscriptionProvider(new PaytrIframeClient(http),
            "123456", Key, Salt, testMode: true);
        var request = NewProviderCheckoutRequest();

        var session = await provider.CreateCheckoutAsync(request);
        Assert.Equal(PaytrSubscriptionProvider.CreateOrderId("business-42", request.MerchantReference),
            session.ProviderSessionId);
        Assert.StartsWith("https://systemcel.app/api/odeme/paytr/form?token=TOKEN123", session.CheckoutUrl.AbsoluteUri);

        var payload = JsonSerializer.Serialize(new
        {
            MerchantOrderId = session.ProviderSessionId,
            Status = "success",
            TotalAmountKurus = "3600"
        });
        var signedText = session.ProviderSessionId + Salt + "success" + "3600";
        var signature = Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(signedText)));
        var verified = provider.VerifyWebhook(new PaymentWebhookEnvelope(payload, signature));
        Assert.True(verified.IsValid);
        Assert.Equal(PaymentEventTypes.PaymentSucceeded, verified.Event!.EventType);
        Assert.Equal(36m, verified.Event.Amount);
        Assert.False(provider.VerifyWebhook(new PaymentWebhookEnvelope(
            payload.Replace("3600", "3601", StringComparison.Ordinal), signature)).IsValid);
    }

    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public async Task SubscriptionProviderPostsRequestedModeAndSignsThatMode(bool testMode, string expectedMode)
    {
        using var handler = new CapturingTokenHandler();
        using var http = new HttpClient(handler);
        var provider = new PaytrSubscriptionProvider(new PaytrIframeClient(http),
            "123456", Key, Salt, testMode);

        var session = await provider.CreateCheckoutAsync(NewProviderCheckoutRequest());
        var fields = Assert.IsType<Dictionary<string, string>>(handler.Fields);

        Assert.Equal(expectedMode, fields["test_mode"]);
        Assert.Equal(session.ProviderSessionId, fields["merchant_oid"]);
        var postedInput = new PaytrIframeTokenInput(
            fields["merchant_id"],
            fields["user_ip"],
            fields["merchant_oid"],
            fields["email"],
            long.Parse(fields["payment_amount"], System.Globalization.CultureInfo.InvariantCulture),
            fields["user_basket"],
            fields["no_installment"] == "1",
            int.Parse(fields["max_installment"], System.Globalization.CultureInfo.InvariantCulture),
            fields["currency"],
            testMode);
        Assert.Equal(PaytrIframeProtocol.CreateToken(postedInput, Key, Salt), fields["paytr_token"]);
        Assert.NotEqual(
            PaytrIframeProtocol.CreateToken(postedInput with { TestMode = !testMode }, Key, Salt),
            fields["paytr_token"]);
    }

    private static PaymentCheckoutRequest NewProviderCheckoutRequest()
    {
        var quote = new PaymentQuote(
            "isletme_baslangic", "Isletme", "Aylik", "TRY", 30m, 20m, 6m, 36m,
            0, 0, 1, 0m, string.Empty, false, 30m, 30m, 0, 30m);
        return new PaymentCheckoutRequest(
            "user-provided-idempotency-key", quote, "business-42", "buyer@example.com",
            new Uri("https://systemcel.app/app/abonelik?odeme=basarili"),
            new Uri("https://systemcel.app/app/abonelik?odeme=basarisiz"),
            new Uri("https://systemcel.app/api/odeme/paytr/bildirim"),
            CustomerIp: "203.0.113.7", CustomerName: "Buyer Example",
            CustomerAddress: "Test address", CustomerPhone: "5550000000");
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

    private sealed class CapturingTokenHandler : HttpMessageHandler
    {
        public IReadOnlyDictionary<string, string>? Fields { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://www.paytr.com/odeme/api/get-token", request.RequestUri!.AbsoluteUri);
            var body = await request.Content!.ReadAsStringAsync(ct);
            Fields = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .ToDictionary(
                    pair => Uri.UnescapeDataString(pair[0].Replace('+', ' ')),
                    pair => Uri.UnescapeDataString(pair[1].Replace('+', ' ')),
                    StringComparer.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"success\",\"token\":\"TOKEN123\"}")
            };
        }
    }
}
