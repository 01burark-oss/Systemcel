using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api;

internal static class YonetimApi
{
    public static void MapYonetimApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/yonetim/muhasebeci-basvurulari", async (
            string? durum,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetMuhasebeciBasvurulariAsync(durum, ct));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Başvurular yüklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/yonetim/muhasebeci-basvurulari/{kullaniciId:int}/onayla", async (
            int kullaniciId,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.ApproveMuhasebeciBasvurusuAsync(kullaniciId, ct));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Başvuru onaylanamadı: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/yonetim/muhasebeci-basvurulari/{kullaniciId:int}/reddet", async (
            int kullaniciId,
            MuhasebeciBasvuruRedRequest request,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.RejectMuhasebeciBasvurusuAsync(kullaniciId, request, ct));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Başvuru reddedilemedi: {ex.Message}"));
            }
        });
    }

    private sealed record ApiHata(string mesaj);
}
