using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api;

internal static class DestekApi
{
    public static void MapDestekApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/destek-talepleri", async (IDestekTalebiService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetMineAsync(ct)); }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden);
            }
        });

        app.MapPost("/api/ekran/destek-talepleri", async (
            DestekTalebiOlusturRequest request,
            HttpContext context,
            IDestekTalebiService service,
            CancellationToken ct) =>
        {
            var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(key))
                return Results.BadRequest(new ApiHata("Idempotency-Key başlığı zorunludur."));
            try { return Results.Ok(await service.CreateAsync(request, key, ct)); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Idempotency-Key", StringComparison.Ordinal))
            {
                return Results.Conflict(new ApiHata(ex.Message));
            }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
        }).RequireRateLimiting("sensitive");
    }

    private sealed record ApiHata(string mesaj);
}
