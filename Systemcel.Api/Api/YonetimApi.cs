using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api;

internal static class YonetimApi
{
    public static void MapYonetimApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/yonetim/bildirim-teslimleri", async (
            ISystemcelYonetimService management,
            IBildirimOutboxService outbox,
            CancellationToken ct) =>
        {
            if (!await management.IsCurrentUserAdminAsync(ct))
                return Results.Json(new ApiHata("Yönetici yetkisi gerekir."), statusCode: StatusCodes.Status403Forbidden);
            return Results.Ok(await outbox.ListFailedAsync(ct: ct));
        });

        app.MapGet("/api/ekran/yonetim/muhasebeci-iletisim-incelemeleri", async (
            ISystemcelYonetimService management,
            IMuhasebeciPortalService marketplace,
            CancellationToken ct) =>
        {
            if (!await management.IsCurrentUserAdminAsync(ct))
                return Results.Json(new ApiHata("Yönetici yetkisi gerekir."), statusCode: StatusCodes.Status403Forbidden);
            return Results.Ok(await marketplace.GetContactReviewQueueAsync(ct));
        });

        app.MapPost("/api/ekran/yonetim/muhasebeci-iletisim-incelemeleri/{talepId:int}/karar", async (
            int talepId,
            IletisimIncelemesiKararRequest request,
            ISystemcelYonetimService management,
            IMuhasebeciPortalService marketplace,
            CancellationToken ct) =>
        {
            if (!await management.IsCurrentUserAdminAsync(ct))
                return Results.Json(new ApiHata("Yönetici yetkisi gerekir."), statusCode: StatusCodes.Status403Forbidden);
            try { return Results.Ok(await marketplace.DecideContactReviewAsync(talepId, request, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/yonetim/bildirim-teslimleri/{id:long}/yeniden-dene", async (
            long id,
            ISystemcelYonetimService management,
            IBildirimOutboxService outbox,
            CancellationToken ct) =>
        {
            if (!await management.IsCurrentUserAdminAsync(ct))
                return Results.Json(new ApiHata("Yönetici yetkisi gerekir."), statusCode: StatusCodes.Status403Forbidden);
            try
            {
                await outbox.RetryFailedAsync(id, DateTime.UtcNow, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapPut("/api/ekran/yonetim/isletmeler/{isletmeId:int}/haklar", async (int isletmeId, EntitlementOverrideRequest request, ISystemcelYonetimService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ApplyEntitlementOverrideAsync(isletmeId, request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (Exception ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapGet("/api/ekran/yonetim/odemeler", async (
            string? durum,
            bool? sadeceHatalar,
            int? limit,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetOdemeIncelemeAsync(durum, sadeceHatalar ?? false, limit ?? 100, ct));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Odeme kayitlari yuklenemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/yonetim/destek", async (
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetDestekTalepleriAsync(ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
        });

        app.MapPost("/api/ekran/yonetim/destek/{destekTalebiId:int}/guncelle", async (
            int destekTalebiId,
            DestekTalebiGuncelleRequest request,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try { return Results.Ok(await service.UpdateDestekTalebiAsync(destekTalebiId, request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        app.MapGet("/api/ekran/yonetim/muhasebeci-aktarimlari", async (
            string aktarimDonemi,
            int? muhasebeciIsletmeId,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetMuhasebeciAktarimlariAsync(aktarimDonemi, muhasebeciIsletmeId, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (Exception ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        });

        app.MapPost("/api/ekran/yonetim/muhasebeci-aktarimlari/{muhasebeciIsletmeId:int}/tamamla", async (
            int muhasebeciIsletmeId,
            MuhasebeciAktarimTamamlaRequest request,
            ISystemcelYonetimService service,
            CancellationToken ct) =>
        {
            try { return Results.Ok(await service.CompleteMuhasebeciAktarimiAsync(muhasebeciIsletmeId, request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
            catch (Exception ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

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
