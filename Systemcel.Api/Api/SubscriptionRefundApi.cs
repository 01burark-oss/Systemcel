using CashTracker.Core.Services;
using System.Text.Json;

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
                async (int abonelikId, HttpRequest request, ISubscriptionCancellationRefundService service, CancellationToken ct) =>
                {
                    try
                    {
                        if (action == "onayla") await service.ApproveAsync(abonelikId, ct);
                        else if (action == "gonder")
                        {
                            var body = await BillingApi.ReadBoundedPaytrBodyAsync(request.Body, ct);
                            if (!TryParseLiveConfirmation(body, out var confirmed))
                                return Results.BadRequest(new ApiHata("İade gönderim teyidi geçersiz."));
                            await service.DispatchAsync(abonelikId, ct, confirmed);
                        }
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

    internal static bool TryParseLiveConfirmation(string? body, out bool confirmed)
    {
        confirmed = false;
        if (body is null) return false;
        if (body.Length == 0) return true;
        try
        {
            using var json = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 2 });
            if (json.RootElement.ValueKind != JsonValueKind.Object) return false;
            var fields = json.RootElement.EnumerateObject().ToArray();
            if (fields.Length != 1 || fields[0].Name != "canliIadeOnayi" || fields[0].Value.ValueKind != JsonValueKind.True)
                return false;
            confirmed = true;
            return true;
        }
        catch (JsonException) { return false; }
    }
}
