using System.Net;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Infrastructure.Payments;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaytrRefundClientTests
{
    [Fact]
    public async Task Refund_UsesDecimalLiraSignatureAndStableReference()
    {
        Dictionary<string, string>? sent = null;
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal("https://www.paytr.com/odeme/iade", request.RequestUri!.AbsoluteUri);
            var form = await request.Content!.ReadAsStringAsync();
            sent = form.Split('&').Select(x => x.Split('=', 2)).ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1].Replace('+', ' ')));
            return Response("""{"status":"success","merchant_oid":"order123","return_amount":"12.34","is_test":1,"reference_no":"refund123"}""");
        }));
        var result = await new PaytrRefundClient(http).RefundAsync("order123", 12.34m, "refund123", "123456", "key", "salt");
        Assert.True(result.Accepted);
        Assert.True(result.IsDefinitive);
        Assert.Equal(5, sent!.Count);
        Assert.Equal("12.34", sent["return_amount"]);
        Assert.Equal("refund123", sent["reference_no"]);
        Assert.Equal(Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes("key"),
            Encoding.UTF8.GetBytes("123456order12312.34salt"))), sent["paytr_token"]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    public async Task Refund_RejectsInvalidAmountBeforeSending(string amount)
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("Must not send")));
        await Assert.ThrowsAsync<ArgumentException>(() => new PaytrRefundClient(http).RefundAsync("order123",
            decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "refund123", "123456", "key", "salt"));
    }

    [Theory]
    [InlineData("{\"status\":\"success\",\"merchant_oid\":\"other\",\"return_amount\":\"12.34\",\"is_test\":1,\"reference_no\":\"refund123\"}")]
    [InlineData("{\"status\":\"success\",\"merchant_oid\":\"order123\",\"return_amount\":\"12.34\",\"is_test\":0,\"reference_no\":\"refund123\"}")]
    [InlineData("{\"status\":\"success\",\"merchant_oid\":\"order123\",\"return_amount\":\"12.35\",\"is_test\":1,\"reference_no\":\"refund123\"}")]
    [InlineData("{\"status\":\"success\",\"merchant_oid\":\"order123\",\"return_amount\":\"12.34\",\"is_test\":1,\"reference_no\":\"other\"}")]
    [InlineData("not-json")]
    public async Task Refund_MismatchedResponseIsUnknown(string body)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Response(body))));
        var result = await new PaytrRefundClient(http).RefundAsync("order123", 12.34m, "refund123", "123456", "key", "salt");
        Assert.False(result.Accepted);
        Assert.False(result.IsDefinitive);
    }

    [Fact]
    public async Task Refund_DoesNotLeakProviderErrorMessage()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Response("""{"status":"error","err_no":"006","err_msg":"secret or customer details"}"""))));
        var result = await new PaytrRefundClient(http).RefundAsync("order123", 12.34m, "refund123", "123456", "key", "salt");
        Assert.False(result.Accepted);
        Assert.True(result.IsDefinitive);
        Assert.Equal("refund_exceeds_payment", result.ErrorCode);
    }

    [Fact]
    public async Task Refund_TransportFailureIsUnknown()
    {
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("private error")));
        var result = await new PaytrRefundClient(http).RefundAsync("order123", 12.34m, "refund123", "123456", "key", "salt");
        Assert.False(result.IsDefinitive);
        Assert.Equal("transport_error", result.ErrorCode);
    }

    [Fact]
    public async Task Provider_RejectsLivePaymentBeforeRefundRequest()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(request =>
        {
            calls++;
            Assert.EndsWith("/durum-sorgu", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(Response("""{"status":"success","payment_amount":100,"payment_total":100,"currency":"TRY","test_mode":0}"""));
        }));
        var provider = new PaytrSubscriptionProvider(new PaytrIframeClient(http), "123456", "key", "salt", true);
        var result = await provider.RefundAsync("order123", 10m, "refund123");
        Assert.False(result.Accepted);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Refund_StalledResponseBodyIsUnknownWithinDeadline()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StalledStream())
        }))) { Timeout = Timeout.InfiniteTimeSpan };
        var result = await new PaytrRefundClient(http).RefundAsync("order123", 12.34m, "refund123", "123456", "key", "salt")
            .WaitAsync(TimeSpan.FromSeconds(25));
        Assert.False(result.Accepted);
        Assert.False(result.IsDefinitive);
        Assert.Equal("timeout", result.ErrorCode);
    }

    private static HttpResponseMessage Response(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(request);
    }

    private sealed class StalledStream : Stream
    {
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
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
