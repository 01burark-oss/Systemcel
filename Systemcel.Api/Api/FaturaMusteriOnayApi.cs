using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class FaturaMusteriOnayApi
{
    public static void MapFaturaMusteriOnayApi(this WebApplication app)
    {
        app.MapGet(
                "/api/ekran/faturalar/{faturaId:int}/musteri-onayi",
                async (int faturaId, IFaturaMusteriOnayService service, CancellationToken ct) =>
                {
                    try
                    {
                        return Results.Ok(await service.GetLatestAsync(faturaId, ct));
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.NotFound(new { mesaj = ex.Message });
                    }
                })
            .RequireRateLimiting("sensitive");

        app.MapPost(
                "/api/ekran/faturalar/{faturaId:int}/musteri-onayi/gonder",
                async (int faturaId, IFaturaMusteriOnayService service, CancellationToken ct) =>
                {
                    try
                    {
                        return Results.Ok(await service.SendAsync(faturaId, ct));
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { mesaj = ex.Message });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { mesaj = ex.Message });
                    }
                })
            .RequireRateLimiting("sensitive");

        app.MapGet(
                "/api/public/fatura-onaylari/{token}",
                async (string token, IFaturaMusteriOnayService service, CancellationToken ct) =>
                {
                    var result = await service.GetPublicAsync(token, ct);
                    return result is null
                        ? Results.NotFound(new { mesaj = "Teyit bağlantısı bulunamadı." })
                        : Results.Ok(result);
                })
            .AllowAnonymous()
            .RequireRateLimiting("sensitive");

        app.MapPost(
                "/api/public/fatura-onaylari/{token}/yanit",
                async (
                    string token,
                    PublicFaturaMusteriOnayYaniti request,
                    HttpContext context,
                    IFaturaMusteriOnayService service,
                    CancellationToken ct) =>
                {
                    try
                    {
                        var result = await service.RespondAsync(
                            token,
                            request,
                            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                            context.Request.Headers.UserAgent.ToString(),
                            ct);
                        return result is null
                            ? Results.NotFound(new { mesaj = "Teyit bağlantısı bulunamadı." })
                            : Results.Ok(result);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { mesaj = ex.Message });
                    }
                })
            .AllowAnonymous()
            .RequireRateLimiting("sensitive");
    }
}
