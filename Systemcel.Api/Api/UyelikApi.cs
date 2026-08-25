using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class UyelikApi
{
    public static void MapUyelikApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/uyelikler", async (IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetMembershipsAsync(ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
        });

        app.MapPost("/api/ekran/uyelikler/davet", async (IsletmeUyelikDavetRequest request, IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.CreateInviteAsync(request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/uyelikler/davet/kabul", async (IsletmeUyelikDavetKabulRequest request, IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.AcceptInviteAsync(request.DavetKodu, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { mesaj = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");

        app.MapPut("/api/ekran/uyelikler/{id:int}/rol", async (int id, IsletmeUyelikRolGuncelleRequest request, IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.UpdateRoleAsync(id, request.Rol, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { mesaj = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");

        app.MapDelete("/api/ekran/uyelikler/{id:int}", async (int id, IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.RemoveAsync(id, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { mesaj = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");

        app.MapPost("/api/ekran/uyelikler/{id:int}/sahiplik-devri", async (int id, IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.TransferOwnershipAsync(id, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { mesaj = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");
    }
}
