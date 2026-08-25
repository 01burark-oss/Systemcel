using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Systemcel.Api.Services;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;

        var (status, title, detail) = exception switch
        {
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Geçersiz istek",
                "İstek biçimi veya boyutu kabul edilmedi."),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Erişim reddedildi",
                "Bu işlem için yetkiniz yok."),
            FileNotFoundException => (
                StatusCodes.Status404NotFound,
                "Dosya bulunamadı",
                "İstenen dosya bulunamadı veya artık erişilebilir değil."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "İşlem tamamlanamadı",
                "Beklenmeyen bir hata oluştu. Lütfen daha sonra yeniden deneyin.")
        };

        var route = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
        if (status >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled API error. TraceId={TraceId} Route={Route}",
                httpContext.TraceIdentifier,
                route);
        }
        else
        {
            _logger.LogWarning(
                "Rejected API request. TraceId={TraceId} Route={Route} ErrorType={ErrorType}",
                httpContext.TraceIdentifier,
                route,
                exception.GetType().Name);
        }

        var problem = new ProblemDetails
        {
            Type = $"https://systemcel.app/problems/http-{status}",
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
