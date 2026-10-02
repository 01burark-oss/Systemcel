using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class SubscriptionRefundApi
{
    private sealed record ApiHata(string Mesaj);
    public static void MapSubscriptionRefundApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/yonetim/abonelik-iadeleri", async (ISubscriptionCancellationRefundService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ListAsync(ct)); }
            catch (UnauthorizedAccessException) { return Results.Json(new ApiHata("Yönetici yetkisi gerekir."), statusCode: 403); }
        });
        var actions = new[] { "onayla", "gonder", "sorgula" };
        foreach (var action in actions)
        {
            app.MapPost($"/api/ekran/yonetim/abonelik-iadeleri/{{abonelikId:int}}/{action}",
                async (int abonelikId, ISubscriptionCancellationRefundService service, CancellationToken ct) =>
                {
                    try
                    {
                        if (action == "onayla") await service.ApproveAsync(abonelikId, ct);
                        else if (action == "gonder") await service.DispatchAsync(abonelikId, ct);
                        else await service.ReconcileAsync(abonelikId, ct);
                        return Results.NoContent();
                    }
                    catch (UnauthorizedAccessException) { return Results.Json(new ApiHata("Yönetici yetkisi gerekir."), statusCode: 403); }
                    catch (KeyNotFoundException) { return Results.NotFound(new ApiHata("Abonelik bulunamadı.")); }
                    catch (ArgumentException) { return Results.BadRequest(new ApiHata("İade talebi geçersiz.")); }
                    catch (InvalidOperationException) { return Results.Conflict(new ApiHata("İade işlemi tamamlanamadı. Kaydı yenileyip onayı ve ödeme durumunu kontrol edin.")); }
                }).RequireRateLimiting("sensitive");
        }
    }
}
