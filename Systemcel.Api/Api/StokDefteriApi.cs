using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Api;

internal static class StokDefteriApi
{
    public static void MapStokDefteriApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/ekran/stok-defteri");

        group.MapGet("/", async (IGelismisStokService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(ct: ct)));

        group.MapPost("/depolar", async (
            StokDepoOlusturRequest request,
            IIsletmeService isletmeService,
            IGelismisStokService service,
            CancellationToken ct) => await HandleWriteAsync(
                isletmeService,
                () => service.CreateWarehouseAsync(request, ct)))
            .RequireRateLimiting("sensitive");

        group.MapPost("/hareketler", async (
            StokHareketIslemRequest request,
            HttpContext context,
            IIsletmeService isletmeService,
            IGelismisStokService service,
            CancellationToken ct) => await HandleIdempotentWriteAsync(
                context,
                isletmeService,
                key => service.CreateMovementAsync(request, key, ct)))
            .RequireRateLimiting("sensitive");

        group.MapPost("/transferler", async (
            StokTransferRequest request,
            HttpContext context,
            IIsletmeService isletmeService,
            IGelismisStokService service,
            CancellationToken ct) => await HandleIdempotentWriteAsync(
                context,
                isletmeService,
                key => service.TransferAsync(request, key, ct)))
            .RequireRateLimiting("sensitive");

        group.MapPost("/sayimlar", async (
            StokSayimRequest request,
            HttpContext context,
            IIsletmeService isletmeService,
            IGelismisStokService service,
            CancellationToken ct) => await HandleIdempotentWriteAsync(
                context,
                isletmeService,
                key => service.CountAsync(request, key, ct)))
            .RequireRateLimiting("sensitive");

        group.MapPost("/islemler/{id:int}/ters-kayit", async (
            int id,
            StokTersKayitRequest request,
            HttpContext context,
            IIsletmeService isletmeService,
            IGelismisStokService service,
            CancellationToken ct) => await HandleIdempotentWriteAsync(
                context,
                isletmeService,
                key => service.ReverseAsync(id, request, key, ct)))
            .RequireRateLimiting("sensitive");
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

    private static async Task EnsureWriteAccessAsync(IIsletmeService isletmeService)
    {
        var active = await isletmeService.GetActiveAsync();
        if (!string.Equals(active.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Stok defteri yalnız işletme çalışma alanında kullanılabilir.");
        var access = await isletmeService.GetActiveAccessAsync();
        if (access.IsletmeId != active.Id || !access.YazmaYetkisi)
            throw new UnauthorizedAccessException("Bu işletmede stok değiştirme yetkiniz yok.");
    }

    private sealed record ApiHata(string mesaj);
}
