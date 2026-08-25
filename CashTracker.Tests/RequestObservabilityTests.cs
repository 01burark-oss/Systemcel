using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Systemcel.Api.Services;
using Xunit;

namespace CashTracker.Tests;

public class RequestObservabilityTests
{
    [Fact]
    public async Task InvokeAsync_UsesSafeIncomingRequestIdAcrossHeaderTraceAndLogScope()
    {
        var logger = new CapturingLogger<RequestObservabilityMiddleware>();
        using var telemetry = new RequestTelemetry();
        var middleware = new RequestObservabilityMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            logger,
            telemetry);
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestObservabilityMiddleware.RequestIdHeaderName] = "client-request_42";

        await middleware.InvokeAsync(context);

        Assert.Equal("client-request_42", context.TraceIdentifier);
        Assert.Equal("client-request_42", context.Response.Headers[RequestObservabilityMiddleware.RequestIdHeaderName]);
        Assert.Contains(logger.Scopes, scope =>
            scope.TryGetValue("RequestId", out var value) && Equals(value, "client-request_42"));
    }

    [Theory]
    [InlineData("contains space")]
    [InlineData("contains/secret")]
    [InlineData("satir\r\nheader")]
    public async Task InvokeAsync_ReplacesUnsafeIncomingRequestId(string suppliedId)
    {
        using var telemetry = new RequestTelemetry();
        var middleware = new RequestObservabilityMiddleware(
            _ => Task.CompletedTask,
            new CapturingLogger<RequestObservabilityMiddleware>(),
            telemetry);
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestObservabilityMiddleware.RequestIdHeaderName] = suppliedId;

        await middleware.InvokeAsync(context);

        var resolved = context.Response.Headers[RequestObservabilityMiddleware.RequestIdHeaderName].ToString();
        Assert.NotEmpty(resolved);
        Assert.NotEqual(suppliedId, resolved);
        Assert.Equal(resolved, context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_RecordsRequestDurationAndServerErrorWithBoundedTags()
    {
        var values = new List<(string Name, double Value, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, activeListener) =>
        {
            if (instrument.Meter.Name == RequestTelemetry.MeterName)
                activeListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            values.Add((instrument.Name, measurement, tags.ToArray().ToDictionary(x => x.Key, x => x.Value))));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            values.Add((instrument.Name, measurement, tags.ToArray().ToDictionary(x => x.Key, x => x.Value))));
        listener.Start();

        using var telemetry = new RequestTelemetry();
        var middleware = new RequestObservabilityMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Task.CompletedTask;
            },
            new CapturingLogger<RequestObservabilityMiddleware>(),
            telemetry);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;

        await middleware.InvokeAsync(context);

        Assert.Contains(values, x => x.Name == RequestTelemetry.RequestCountName && x.Value == 1);
        Assert.Contains(values, x => x.Name == RequestTelemetry.RequestDurationName && x.Value >= 0);
        Assert.Contains(values, x =>
            x.Name == RequestTelemetry.RequestErrorCountName &&
            x.Value == 1 &&
            Equals(x.Tags["error.type"], "server"));
        Assert.All(values, x =>
        {
            Assert.DoesNotContain("request.id", x.Tags.Keys);
            Assert.DoesNotContain("url.path", x.Tags.Keys);
        });
    }

    [Fact]
    public async Task GlobalExceptionHandler_UsesTheRequestTraceIdentifierInProblemDetails()
    {
        var handler = new GlobalExceptionHandler(new CapturingLogger<GlobalExceptionHandler>());
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "request-problem-7"
        };
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("sensitive"), default);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("request-problem-7", json.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("sensitive", json.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<Dictionary<string, object?>> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
                Scopes.Add(values.ToDictionary(x => x.Key, x => x.Value));
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
