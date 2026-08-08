using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class UyelikApi
{
    public static void MapUyelikApi(this WebApplication app)
    {
        app.MapPost("/api/ekran/uyelikler/davet", async (IsletmeUyelikDavetRequest request, IIsletmeUyelikService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.CreateInviteAsync(request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { mesaj = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
            catch (Exception ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");
    }
}
