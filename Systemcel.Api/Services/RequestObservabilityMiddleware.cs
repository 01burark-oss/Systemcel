using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Routing;

namespace Systemcel.Api.Services;

internal sealed class RequestObservabilityMiddleware
{
    internal const string RequestIdHeaderName = "X-Request-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly RequestTelemetry _telemetry;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger,
        RequestTelemetry telemetry)
    {
        _next = next;
        _logger = logger;
        _telemetry = telemetry;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = ResolveRequestId(context);
        context.TraceIdentifier = requestId;
        context.Response.Headers[RequestIdHeaderName] = requestId;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = requestId,
            ["TraceId"] = requestId
        });
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            await _next(context);
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(startedAt);
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            var statusCode = context.RequestAborted.IsCancellationRequested
                ? 499
                : context.Response.StatusCode;

            _telemetry.Record(context.Request.Method, route, statusCode, duration);
            _logger.LogInformation(
                "HTTP request completed. Method={Method} Route={Route} StatusCode={StatusCode} DurationMs={DurationMs}",
                context.Request.Method,
                route,
                statusCode,
                duration.TotalMilliseconds);
        }
    }

    private static string ResolveRequestId(HttpContext context)
    {
        var suppliedValues = context.Request.Headers[RequestIdHeaderName];
        if (suppliedValues.Count == 1)
        {
            var supplied = suppliedValues[0];
            if (IsSafeRequestId(supplied))
                return supplied!;
        }

        return Activity.Current?.TraceId.ToString() is { Length: > 0 } activityTraceId
            ? activityTraceId
            : Guid.NewGuid().ToString("N");
    }

    private static bool IsSafeRequestId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
            return false;

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.')
                return false;
        }

        return true;
    }
}

internal sealed class RequestTelemetry : IDisposable
{
    internal const string MeterName = "Systemcel.Api";
    internal const string RequestCountName = "systemcel.http.server.request.count";
    internal const string RequestDurationName = "systemcel.http.server.request.duration";
    internal const string RequestErrorCountName = "systemcel.http.server.request.error.count";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _requestCount;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _requestErrorCount;

    public RequestTelemetry()
    {
        _requestCount = _meter.CreateCounter<long>(RequestCountName, "request");
        _requestDuration = _meter.CreateHistogram<double>(RequestDurationName, "ms");
        _requestErrorCount = _meter.CreateCounter<long>(RequestErrorCountName, "request");
    }

    public void Record(string method, string route, int statusCode, TimeSpan duration)
    {
        var tags = new TagList
        {
            { "http.request.method", method },
            { "http.route", route },
            { "http.response.status_code", statusCode }
        };
        _requestCount.Add(1, tags);
        _requestDuration.Record(duration.TotalMilliseconds, tags);

        if (statusCode < 400)
            return;

        tags.Add("error.type", statusCode >= 500 ? "server" : "client");
        _requestErrorCount.Add(1, tags);
    }

    public void Dispose() => _meter.Dispose();
}
