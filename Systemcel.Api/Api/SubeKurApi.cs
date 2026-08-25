using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class SubeKurApi
{
    public static void MapSubeKurApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/ekran/sube-kur");

        group.MapGet("/", async (IIsletmeService isletmeService, ISubeKurService service, CancellationToken ct) =>
        {
            await EnsureReadAccessAsync(isletmeService);
            return Results.Ok(await service.GetContextAsync(ct));
        });

        group.MapGet("/finans-ozeti", async (int? subeId, IIsletmeService isletmeService, ISubeKurService service, CancellationToken ct) =>
        {
            await EnsureReadAccessAsync(isletmeService);
            try { return Results.Ok(await service.GetFinancialSummaryAsync(subeId, ct)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        });

        group.MapPost("/subeler", async (
            SubeOlusturRequest request,
            HttpContext context,
            IIsletmeService isletmeService,
            ISubeKurService service,
            CancellationToken ct) => await HandleIdempotentWriteAsync(
                context,
                isletmeService,
                key => service.CreateBranchAsync(request, key, ct)))
            .RequireRateLimiting("sensitive");

        group.MapPost("/aktif-sube", async (
            AktifSubeSecRequest request,
            IIsletmeService isletmeService,
            ISubeKurService service,
            CancellationToken ct) => await HandleWriteAsync(isletmeService, async () =>
            {
                await service.SetActiveBranchAsync(request.SubeId, ct);
                return await service.GetContextAsync(ct);
            }))
            .RequireRateLimiting("sensitive");

        group.MapPost("/kurlar", async (
            DovizKuruKaydetRequest request,
            HttpContext context,
            IIsletmeService isletmeService,
            ISubeKurService service,
            CancellationToken ct) => await HandleIdempotentWriteAsync(
                context,
                isletmeService,
                key => service.SaveRateAsync(request, key, ct)))
            .RequireRateLimiting("sensitive");
    }

    private static async Task<IResult> HandleIdempotentWriteAsync<T>(
        HttpContext context,
        IIsletmeService isletmeService,
        Func<string, Task<T>> action)
    {
        var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
            return Results.BadRequest(new ApiHata("Idempotency-Key başlığı zorunludur."));
        return await HandleWriteAsync(isletmeService, () => action(key));
    }

    private static async Task<IResult> HandleWriteAsync<T>(IIsletmeService isletmeService, Func<Task<T>> action)
    {
        try
        {
            await EnsureWriteAccessAsync(isletmeService);
            return Results.Ok(await action());
        }
        catch (EntitlementViolationException) { throw; }
        catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return Results.Json(new ApiHata(ex.Message), statusCode: StatusCodes.Status403Forbidden); }
    }

    private static async Task EnsureWriteAccessAsync(IIsletmeService isletmeService)
    {
        await EnsureReadAccessAsync(isletmeService);
        var active = await isletmeService.GetActiveAsync();
        var access = await isletmeService.GetActiveAccessAsync();
        if (access.IsletmeId != active.Id || !access.YazmaYetkisi)
            throw new UnauthorizedAccessException("Bu işletmede şube ve kur ayarlarını değiştirme yetkiniz yok.");
    }

    private static async Task EnsureReadAccessAsync(IIsletmeService isletmeService)
    {
        var active = await isletmeService.GetActiveAsync();
        if (!string.Equals(active.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Şube ve kur ayarları yalnız işletme çalışma alanında kullanılabilir.");
    }

    private sealed record AktifSubeSecRequest(int SubeId);
    private sealed record ApiHata(string mesaj);
}
