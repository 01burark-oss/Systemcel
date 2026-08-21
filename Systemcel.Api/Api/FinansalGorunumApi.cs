using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api;

internal static class FinansalGorunumApi
{
    public static void MapFinansalGorunumApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/ekran/finansal-gorunum");

        group.MapGet(string.Empty, async (
            DateTime? referansTarihi,
            int? haftaSayisi,
            IFinansalGorunumService service,
            CancellationToken ct) =>
        {
            var weeks = haftaSayisi ?? 13;
            if (weeks is < 1 or > 13)
                return Results.BadRequest(new ApiHata("Hafta sayısı 1 ile 13 arasında olmalıdır."));

            var reference = (referansTarihi ?? DateTime.Today).Date;
            try
            {
                return Results.Ok(await service.GetAsync(reference, weeks, ct));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiHata(ex.Message));
            }
        });

        group.MapGet("/nakit-planlari", async (
            IFinansalGorunumService service,
            CancellationToken ct) =>
            Results.Ok(await service.GetPlanItemsAsync(ct)));

        group.MapPost("/nakit-planlari", async (
            NakitPlanKalemiKaydetRequest? request,
            IFinansalGorunumService service,
            IIsletmeService isletmeService,
            CancellationToken ct) =>
        {
            var readOnly = await RejectReadOnlyAsync(isletmeService);
            if (readOnly is not null)
                return readOnly;
            if (request is null)
                return Results.BadRequest(new ApiHata("Plan kalemi bilgileri gereklidir."));

            try
            {
                var id = await service.CreatePlanItemAsync(request, ct);
                return Results.Created(
                    $"/api/ekran/finansal-gorunum/nakit-planlari/{id}",
                    new KimlikSonucu(id));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiHata(ex.Message));
            }
        });

        group.MapPut("/nakit-planlari/{id:int}", async (
            int id,
            NakitPlanKalemiKaydetRequest? request,
            IFinansalGorunumService service,
            IIsletmeService isletmeService,
            CancellationToken ct) =>
        {
            var readOnly = await RejectReadOnlyAsync(isletmeService);
            if (readOnly is not null)
                return readOnly;
            if (request is null)
                return Results.BadRequest(new ApiHata("Plan kalemi bilgileri gereklidir."));

            try
            {
                return await service.UpdatePlanItemAsync(id, request, ct)
                    ? Results.Ok(new KimlikSonucu(id))
                    : Results.NotFound(new ApiHata("Nakit plan kalemi bulunamadı."));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiHata(ex.Message));
            }
        });

        group.MapDelete("/nakit-planlari/{id:int}", async (
            int id,
            IFinansalGorunumService service,
            IIsletmeService isletmeService,
            CancellationToken ct) =>
        {
            var readOnly = await RejectReadOnlyAsync(isletmeService);
            if (readOnly is not null)
                return readOnly;

            return await service.DeletePlanItemAsync(id, ct)
                ? Results.NoContent()
                : Results.NotFound(new ApiHata("Nakit plan kalemi bulunamadı."));
        });
    }

    private static async Task<IResult?> RejectReadOnlyAsync(IIsletmeService isletmeService)
    {
        var access = await isletmeService.GetActiveAccessAsync();
        return access.MuhasebeciMusteriBaglami && !access.YazmaYetkisi
            ? Results.BadRequest(new ApiHata(
                "Bu müşteri bağlamında yalnızca okuma ve rapor yetkiniz var. Nakit planını değiştirmek için tam işlem yetkisi gerekir."))
            : null;
    }

    private sealed record ApiHata(string mesaj);
    private sealed record KimlikSonucu(int Id);
}
