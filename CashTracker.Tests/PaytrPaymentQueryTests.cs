using System.Net;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Infrastructure.Payments;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaytrPaymentQueryTests
{
    private const string MerchantId = "753709";
    private const string MerchantKey = "local-test-key";
    private const string MerchantSalt = "local-test-salt";
    private const string OrderId = "order123";

    [Fact]
    public async Task GetPayment_SendsOnlySignedFieldsToExactStatusEndpoint_AndParsesPaymentAndRefunds()
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            """{"status":"success","payment_amount":"100,50","payment_total":100.5,"currency":"TL","test_mode":"1","returns":[{"return_amount":"20,25","reference_no":"refund123"},{"return_amount":5}]}"""));
        using var http = new HttpClient(handler);
        var client = new PaytrPaymentQueryClient(http);

        var result = await client.GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.True(result.Available);
        var payment = Assert.IsType<CashTracker.Core.Models.ProviderPaymentSnapshot>(result.Payment);
        Assert.Equal(OrderId, payment.ProviderOrderId);
        Assert.Equal(100.50m, payment.Amount);
        Assert.Equal(100.50m, payment.TotalAmount);
        Assert.Equal("TRY", payment.Currency);
        Assert.True(payment.IsTestPayment);
        Assert.Equal(25.25m, payment.RefundedAmount);
        Assert.Equal("refund123", payment.Refunds![0].ReferenceNo);
        Assert.Equal(20.25m, payment.Refunds[0].Amount);
        Assert.Equal("https://www.paytr.com/odeme/durum-sorgu", handler.RequestUri!.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(new[] { "merchant_id", "merchant_oid", "paytr_token" }, handler.Form!.Keys.Order().ToArray());
        Assert.Equal(MerchantId, handler.Form["merchant_id"]);
        Assert.Equal(OrderId, handler.Form["merchant_oid"]);
        Assert.Equal(Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(MerchantKey),
                Encoding.UTF8.GetBytes(MerchantId + OrderId + MerchantSalt))),
            handler.Form["paytr_token"]);
    }

    [Theory]
    [InlineData("TL", "1", true, "TRY")]
    [InlineData("TRY", "0", false, "TRY")]
    [InlineData("TL", 1, true, "TRY")]
    [InlineData("TRY", 0, false, "TRY")]
    public async Task GetPayment_AcceptsDocumentedCurrencyAndTestModeRepresentations(
        string currency, object testMode, bool expectedMode, string expectedCurrency)
    {
        var modeJson = testMode is string mode ? $"\"{mode}\"" : Convert.ToString(testMode, System.Globalization.CultureInfo.InvariantCulture)!;
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            $"{{\"status\":\"success\",\"payment_amount\":\"12.34\",\"payment_total\":\"12,34\",\"currency\":\"{currency}\",\"test_mode\":{modeJson},\"returns\":null}}"));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.True(result.Available);
        Assert.Equal(expectedCurrency, result.Payment!.Currency);
        Assert.Equal(expectedMode, result.Payment.IsTestPayment);
        Assert.Equal(0m, result.Payment.RefundedAmount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPayment_MissingOrNullRefundArrayMeansNoRefunds(bool includeNullReturns)
    {
        var returns = includeNullReturns ? ",\"returns\":null" : string.Empty;
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            $"{{\"status\":\"success\",\"payment_amount\":1,\"payment_total\":1,\"currency\":\"TL\",\"test_mode\":\"1\"{returns}}}"));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.True(result.Available);
        Assert.Equal(0m, result.Payment!.RefundedAmount);
    }

    [Fact]
    public async Task GetPayment_OfficialNotFoundCodeIsAvailableButHasNoPayment()
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            """{"status":"error","err_no":"004","err_msg":"do not expose provider body"}"""));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.True(result.Available);
        Assert.Null(result.Payment);
        Assert.Equal("004", result.ErrorCode);
    }

    [Theory]
    [InlineData("other-error", 200, "{\"status\":\"error\",\"err_no\":\"123\",\"err_msg\":\"secret-body-value\"}")]
    [InlineData("bad-json", 200, "{not-json secret-body-value")]
    [InlineData("unrecognized-status", 200, "{\"status\":\"pending\",\"secret\":\"secret-body-value\"}")]
    [InlineData("http-error", 503, "secret-body-value")]
    public async Task GetPayment_ProviderOrTransportFailuresAreUnavailableAndDoNotLeakResponse(
        string _, int statusCode, string body)
    {
        var handler = new StubHandler(_ => Reply((HttpStatusCode)statusCode, body));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.False(result.Available);
        Assert.Null(result.Payment);
        Assert.DoesNotContain("secret-body-value", result.ErrorCode, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("12.345", "12")]
    [InlineData("-1", "12")]
    [InlineData("NaN", "12")]
    [InlineData("12", "12.345")]
    [InlineData("12", "-1")]
    [InlineData("12", "NaN")]
    public async Task GetPayment_MalformedOrOutOfRangeAmountsAreUnavailable(string amount, string total)
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            $"{{\"status\":\"success\",\"payment_amount\":\"{amount}\",\"payment_total\":\"{total}\",\"currency\":\"TRY\",\"test_mode\":\"1\",\"returns\":[]}}"));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.False(result.Available);
        Assert.Null(result.Payment);
    }

    [Theory]
    [InlineData("JPY", "1")]
    [InlineData("TRY", "yes")]
    [InlineData("TRY", "2")]
    public async Task GetPayment_UnknownCurrencyOrModeIsUnavailable(string currency, string testMode)
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            $"{{\"status\":\"success\",\"payment_amount\":1,\"payment_total\":1,\"currency\":\"{currency}\",\"test_mode\":\"{testMode}\",\"returns\":[]}}"));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.False(result.Available);
        Assert.Null(result.Payment);
    }

    [Fact]
    public async Task GetPayment_RefundTotalAbovePaymentIsUnavailable()
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            """{"status":"success","payment_amount":10,"payment_total":10,"currency":"TRY","test_mode":"1","returns":[{"return_amount":10.01}]}"""));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.False(result.Available);
        Assert.Null(result.Payment);
    }

    [Fact]
    public async Task GetPayment_OversizedResponseIsUnavailable()
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK, new string('x', 200_000)));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.False(result.Available);
        Assert.Null(result.Payment);
    }

    [Fact]
    public async Task GetPayment_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new StubHandler(async ct =>
        {
            cancellation.Cancel();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return Json(HttpStatusCode.OK, "{}");
        });
        using var http = new HttpClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PaytrPaymentQueryClient(http)
                .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt, cancellation.Token));
    }

    [Fact]
    public async Task GetPayment_TransportTimeoutReturnsUnavailable()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("transport timeout"));
        using var http = new HttpClient(handler);

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt);

        Assert.False(result.Available);
        Assert.Null(result.Payment);
        Assert.Equal("timeout", result.ErrorCode);
    }

    [Fact]
    public async Task GetPayment_BodyReadTimeoutAfterHeadersReturnsUnavailable()
    {
        var stream = new BlockingReadStream();
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        }));
        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };

        var result = await new PaytrPaymentQueryClient(http)
            .GetPaymentAsync(OrderId, MerchantId, MerchantKey, MerchantSalt)
            .WaitAsync(TimeSpan.FromSeconds(25));

        Assert.False(result.Available);
        Assert.Null(result.Payment);
        Assert.Equal("timeout", result.ErrorCode);
        Assert.True(stream.CancellationObserved);
    }

    [Fact]
    public async Task SubscriptionProvider_DelegatesStatusQueryUsingConfiguredCredentialsAndMode()
    {
        var handler = new StubHandler(_ => Reply(HttpStatusCode.OK,
            """{"status":"success","payment_amount":12,"payment_total":12,"currency":"TRY","test_mode":"1","returns":[]}"""));
        using var http = new HttpClient(handler);
        var provider = new PaytrSubscriptionProvider(new PaytrIframeClient(http), MerchantId,
            MerchantKey, MerchantSalt, testMode: true);

        var result = await provider.GetPaymentAsync(OrderId);

        Assert.True(provider.ExpectedTestMode);
        Assert.True(result.Available);
        Assert.Equal(OrderId, result.Payment!.ProviderOrderId);
        Assert.Equal(MerchantId, handler.Form!["merchant_id"]);
        Assert.Equal(OrderId, handler.Form["merchant_oid"]);
        Assert.Equal(Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(MerchantKey),
                Encoding.UTF8.GetBytes(MerchantId + OrderId + MerchantSalt))),
            handler.Form["paytr_token"]);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static Task<HttpResponseMessage> Reply(HttpStatusCode statusCode, string body) =>
        Task.FromResult(Json(statusCode, body));

    private sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public Dictionary<string, string>? Form { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            if (request.Content is not null)
            {
                var fields = await request.Content.ReadAsStringAsync(ct);
                Form = fields.Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('=', 2))
                    .ToDictionary(pair => Uri.UnescapeDataString(pair[0]),
                        pair => Uri.UnescapeDataString(pair[1].Replace('+', ' ')), StringComparer.Ordinal);
            }
            return await response(ct);
        }
    }

    private sealed class BlockingReadStream : Stream
    {
        public bool CancellationObserved { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CancellationObserved = true;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }
}
