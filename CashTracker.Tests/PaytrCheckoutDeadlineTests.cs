using System.Diagnostics;
using System.Net;
using System.Text;
using CashTracker.Infrastructure.Payments;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaytrCheckoutDeadlineTests
{
    [Fact]
    public async Task CreateCheckoutUrl_StopsWhenResponseBodyExceedsClientTimeout()
    {
        var stream = new BlockingReadStream();
        using var http = new HttpClient(new StubHandler(stream))
        {
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        var client = new PaytrIframeClient(http);
        using var callerCancellation = new CancellationTokenSource();
        var operation = client.CreateCheckoutUrlAsync(
            CreateRequest(), "local-key", "local-salt", callerCancellation.Token);
        var timer = Stopwatch.StartNew();

        try
        {
            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(18)));
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(17),
                $"Expected the internal 15-second deadline, but waited {timer.Elapsed}.");
            Assert.True(stream.ReadCancellationObserved.Task.IsCompleted);
        }
        finally
        {
            callerCancellation.Cancel();
            try { await operation; }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task CreateCheckoutUrl_PropagatesCallerCancellationWhileReadingBody()
    {
        var stream = new BlockingReadStream();
        using var http = new HttpClient(new StubHandler(stream));
        var client = new PaytrIframeClient(http);
        using var callerCancellation = new CancellationTokenSource();

        var operation = client.CreateCheckoutUrlAsync(
            CreateRequest(), "local-key", "local-salt", callerCancellation.Token);
        try
        {
            await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
            callerCancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.True(stream.ReadCancellationObserved.Task.IsCompleted);
        }
        finally
        {
            try { await operation; }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task CreateCheckoutUrl_ParsesSuccessfulTokenResponse()
    {
        var body = Encoding.UTF8.GetBytes("{\"status\":\"success\",\"token\":\"checkout_token-123\"}");
        using var http = new HttpClient(new StubHandler(new MemoryStream(body)));
        var client = new PaytrIframeClient(http);

        var url = await client.CreateCheckoutUrlAsync(CreateRequest(), "local-key", "local-salt");

        Assert.Equal("https://www.paytr.com/odeme/guvenli/checkout_token-123", url.AbsoluteUri);
    }

    private static PaytrIframeCheckoutRequest CreateRequest()
    {
        var basket = Convert.ToBase64String(Encoding.UTF8.GetBytes("[]"));
        return new PaytrIframeCheckoutRequest(
            new PaytrIframeTokenInput("753709", "127.0.0.1", "order123", "buyer@example.com",
                12345, basket, true, 0, "TL", true),
            "Test Buyer", "Test Address", "+905550000000",
            new Uri("https://example.test/success"), new Uri("https://example.test/failure"));
    }

    private sealed class StubHandler(Stream responseStream) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(responseStream)
            });
    }

    private sealed class BlockingReadStream : Stream
    {
        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReadCancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ReadCancellationObserved.TrySetResult();
                throw;
            }

            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
