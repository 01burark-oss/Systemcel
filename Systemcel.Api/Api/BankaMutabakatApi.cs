using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Systemcel.Api.Api;

internal static class BankaMutabakatApi
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv", "text/plain", "application/csv", "application/vnd.ms-excel", "application/octet-stream"
    };

    public static void MapBankaMutabakatApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/ekran/banka-mutabakat");

        group.MapGet("/hareketler", async (
            string? durum,
            IIsletmeService isletmeService,
            IEntitlementGuard entitlementGuard,
            IBankaMutabakatService service,
            CancellationToken ct) =>
        {
            var tenant = await RequireTenantAsync(isletmeService, entitlementGuard, false, ct);
            try { return Results.Ok(await service.ListeleAsync(tenant, durum, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        });

        group.MapPost("/import", async (
            HttpRequest request,
            IIsletmeService isletmeService,
            IEntitlementGuard entitlementGuard,
            IBankaMutabakatService service,
            CancellationToken ct) =>
        {
            var tenant = await RequireTenantAsync(isletmeService, entitlementGuard, true, ct);
            if (!request.HasFormContentType)
                return Results.BadRequest(new ApiHata("CSV dosyası multipart/form-data olarak gönderilmelidir."));
            var formFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (formFeature is { IsReadOnly: false }) formFeature.MaxRequestBodySize = BankaMutabakatService.AzamiDosyaBoyutu + 64 * 1024;
            IFormCollection form;
            try { form = await request.ReadFormAsync(ct); }
            catch (InvalidDataException) { return Results.BadRequest(new ApiHata("CSV yükleme isteği geçersiz veya boyut sınırını aşıyor.")); }
            var file = form.Files.GetFile("dosya");
            if (file is null)
                return Results.BadRequest(new ApiHata("CSV dosyası gereklidir."));
            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType.Split(';', 2)[0].Trim()))
                return Results.BadRequest(new ApiHata("Dosya içerik türü CSV metni olarak doğrulanamadı."));
            try
            {
                await using var stream = file.OpenReadStream();
                return Results.Ok(await service.CsvIceAktarAsync(tenant, stream, file.FileName, file.Length, ct));
            }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("upload").WithMetadata(
            new RequestSizeLimitAttribute(BankaMutabakatService.AzamiDosyaBoyutu + 64 * 1024),
            new RequestFormLimitsAttribute { MultipartBodyLengthLimit = BankaMutabakatService.AzamiDosyaBoyutu + 64 * 1024 });

        group.MapGet("/hareketler/{id:int}/adaylar", async (
            int id,
            IIsletmeService isletmeService,
            IEntitlementGuard entitlementGuard,
            IBankaMutabakatService service,
            CancellationToken ct) =>
        {
            var tenant = await RequireTenantAsync(isletmeService, entitlementGuard, false, ct);
            try { return Results.Ok(await service.AdaylariGetirAsync(tenant, id, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(new ApiHata("Banka hareketi bulunamadı.")); }
        });

        group.MapPost("/hareketler/{id:int}/eslestir", async (
            int id,
            BankaEslesmeIstek? request,
            IIsletmeService isletmeService,
            IEntitlementGuard entitlementGuard,
            IBankaMutabakatService service,
            CancellationToken ct) =>
        {
            var tenant = await RequireTenantAsync(isletmeService, entitlementGuard, true, ct);
            if (request is null) return Results.BadRequest(new ApiHata("Eşleşme seçimi gereklidir."));
            try
            {
                await service.EslesmeOnaylaAsync(tenant, id, request, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(new ApiHata("Banka hareketi bulunamadı.")); }
            catch (ArgumentException ex) { return Results.BadRequest(new ApiHata(ex.Message)); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");

        group.MapPost("/hareketler/{id:int}/yok-say", async (
            int id,
            IIsletmeService isletmeService,
            IEntitlementGuard entitlementGuard,
            IBankaMutabakatService service,
            CancellationToken ct) =>
        {
            var tenant = await RequireTenantAsync(isletmeService, entitlementGuard, true, ct);
            try
            {
                await service.YokSayAsync(tenant, id, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(new ApiHata("Banka hareketi bulunamadı.")); }
            catch (InvalidOperationException ex) { return Results.Conflict(new ApiHata(ex.Message)); }
        }).RequireRateLimiting("sensitive");
    }

    private static async Task<int> RequireTenantAsync(
        IIsletmeService isletmeService,
        IEntitlementGuard entitlementGuard,
        bool writable,
        CancellationToken ct)
    {
        var active = await isletmeService.GetActiveAsync();
        if (!string.Equals(active.TenantTipi, HesapTipleri.Isletme, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Banka eşleştirme yalnız aktif işletme çalışma alanında kullanılabilir.");
        if (writable)
        {
            var access = await isletmeService.GetActiveAccessAsync();
            if (access.IsletmeId != active.Id || !access.YazmaYetkisi)
                throw new UnauthorizedAccessException("Bu işletmede banka hareketlerini değiştirme yetkiniz yok.");
        }

        var entitlement = await entitlementGuard.GetAsync(active.Id, HesapTipleri.Isletme, ct);
        entitlementGuard.EnsureFeature(entitlement, EntitlementFeatures.BankReconciliation);
        return active.Id;
    }

    private sealed record ApiHata(string mesaj);
}
