using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class AkilliKararApi
{
    public static void MapAkilliKararApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/ekran/akilli-karar");

        group.MapGet("/durum", (IAkilliKararService service) => Results.Ok(service.GetStatus()));

        group.MapPost("/urun-eslestir", async (
            UrunEslesmeIstek request,
            IIsletmeService businesses,
            IAkilliKararService service,
            CancellationToken ct) =>
        {
            var businessId = await RequireBusinessAsync(businesses, false);
            try { return Results.Ok(await service.UrunEsleAsync(businessId, request, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        group.MapPost("/urun-eslestir/onayla", async (
            UrunEslesmeOnayIstek request,
            IIsletmeService businesses,
            IAkilliKararService service,
            CancellationToken ct) =>
        {
            var businessId = await RequireBusinessAsync(businesses, true);
            try
            {
                await service.UrunEslesmesiniOnaylaAsync(businessId, request, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new ApiHata(ex.Message)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        group.MapPost("/fatura-kontrol", async (
            FaturaKontrolIstek request,
            IIsletmeService businesses,
            IAkilliKararService service,
            CancellationToken ct) =>
        {
            var businessId = await RequireBusinessAsync(businesses, false);
            try { return Results.Ok(await service.FaturaKontrolEtAsync(businessId, request, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        group.MapGet("/bugunun-isleri", async (
            IIsletmeService businesses,
            IAkilliKararService service,
            CancellationToken ct) =>
        {
            var businessId = await RequireBusinessAsync(businesses, false);
            return Results.Ok(await service.BugununIsleriniGetirAsync(businessId, ct));
        });

        group.MapPost("/sutun-eslestir", async (
            SutunEslemeIstek request,
            IAkilliKararService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.SutunlariEsleAsync(
                    request.VeriTuru,
                    request.Sutunlar,
                    request.Ornekler,
                    ct));
            }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        group.MapPost("/asistan-yonlendir", async (
            AsistanYonlendirmeIstek request,
            IAkilliKararService service,
            CancellationToken ct) =>
            Results.Ok(await service.AsistaniYonlendirAsync(request.Mesaj, ct)))
            .RequireRateLimiting("sensitive");
    }

    private static async Task<int> RequireBusinessAsync(IIsletmeService businesses, bool writable)
    {
        var active = await businesses.GetActiveAsync();
        if (!string.Equals(active.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Bu özellik yalnız aktif işletme çalışma alanında kullanılabilir.");
        if (writable)
        {
            var access = await businesses.GetActiveAccessAsync();
            if (access.IsletmeId != active.Id || !access.YazmaYetkisi)
                throw new UnauthorizedAccessException("Bu işletmede kayıt değiştirme yetkiniz yok.");
        }
        return active.Id;
    }

    private sealed class SutunEslemeIstek
    {
        public string VeriTuru { get; set; } = string.Empty;
        public List<string> Sutunlar { get; set; } = [];
        public List<IReadOnlyDictionary<string, string>> Ornekler { get; set; } = [];
    }

    private sealed class AsistanYonlendirmeIstek
    {
        public string Mesaj { get; set; } = string.Empty;
    }

    private sealed record ApiHata(string Mesaj);
}
