using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Systemcel.Api.Hubs;

namespace Systemcel.Api.Api;

internal static class SohbetMerkeziApi
{
    public static void MapSohbetMerkeziApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/sohbetler", async (
            bool? includeArchived,
            IMuhasebeciSohbetMerkeziService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetSohbetlerAsync(includeArchived ?? false, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbetler yuklenemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/sohbetler/{sohbetId:int}/mesajlar", async (
            int sohbetId,
            int? beforeId,
            int? limit,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.GetMesajlarAsync(sohbetId, beforeId, limit ?? 50, ct);
                await hub.Clients.Group(MuhasebeciSohbetHub.GroupName(sohbetId)).SendAsync("MessageRead", new { sohbetId }, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Mesajlar yuklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/sohbetler/{sohbetId:int}/mesajlar", async (
            int sohbetId,
            MuhasebeciSohbetMesajiOlusturRequest request,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                var message = await service.MesajGonderAsync(sohbetId, request, ct);
                await PublishMessageAsync(hub, message, ct);
                return Results.Ok(message);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Mesaj gonderilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/sohbetler/{sohbetId:int}/ekler", async (
            int sohbetId,
            HttpContext context,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                if (!context.Request.HasFormContentType)
                    return Results.BadRequest(new ApiHata("Dosya multipart/form-data olarak gonderilmelidir."));

                var form = await context.Request.ReadFormAsync(ct);
                if (form.Files.Count > 5)
                    return Results.BadRequest(new ApiHata("Mesaj basina en fazla 5 dosya yuklenebilir."));

                var uploaded = new List<MuhasebeciSohbetEkiDto>();
                foreach (var file in form.Files)
                {
                    await using var stream = file.OpenReadStream();
                    uploaded.Add(await service.DosyaEkleAsync(sohbetId, new SohbetDosyaYukleme
                    {
                        DosyaAdi = file.FileName,
                        IcerikTipi = file.ContentType,
                        Boyut = file.Length,
                        Icerik = stream
                    }, ct));
                }

                await hub.Clients.Group(MuhasebeciSohbetHub.GroupName(sohbetId)).SendAsync("ConversationUpdated", new { sohbetId }, ct);
                return Results.Ok(uploaded);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Dosya yuklenemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/sohbet-ekleri/{ekId:int}/indir", async (
            int ekId,
            IMuhasebeciSohbetMerkeziService service,
            CancellationToken ct) =>
        {
            try
            {
                var file = await service.DosyaIndirAsync(ekId, ct);
                return Results.File(file.DosyaYolu, file.IcerikTipi, file.DosyaAdi);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Dosya indirilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/sohbetler/{sohbetId:int}/veri-istekleri", async (
            int sohbetId,
            MuhasebeciSohbetVeriIstegiRequest request,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.VeriIstegiOlusturAsync(sohbetId, request, ct);
                await hub.Clients.Group(MuhasebeciSohbetHub.GroupName(sohbetId)).SendAsync("DataRequestUpdated", result, ct);
                await hub.Clients.Group(MuhasebeciSohbetHub.GroupName(sohbetId)).SendAsync("ConversationUpdated", new { sohbetId }, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Veri istegi olusturulamadi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/sohbetler/{sohbetId:int}/veri-paylasimlari", async (
            int sohbetId,
            MuhasebeciSohbetVeriPaylasimiRequest request,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                var message = await service.VeriPaylasAsync(sohbetId, request, ct);
                await PublishMessageAsync(hub, message, ct);
                return Results.Ok(message);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Veri paylasilamadi: {ex.Message}"));
            }
        });

        app.MapPut("/api/ekran/sohbetler/{sohbetId:int}/konu", async (
            int sohbetId,
            MuhasebeciSohbetKonuGuncelleRequest request,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.KonuGuncelleAsync(sohbetId, request, ct);
                await hub.Clients.Group(MuhasebeciSohbetHub.GroupName(sohbetId)).SendAsync("ConversationUpdated", result, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Konu guncellenemedi: {ex.Message}"));
            }
        });

        app.MapPut("/api/ekran/sohbetler/{sohbetId:int}/arsiv", async (
            int sohbetId,
            MuhasebeciSohbetArsivRequest request,
            IMuhasebeciSohbetMerkeziService service,
            IHubContext<MuhasebeciSohbetHub> hub,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.ArsivleAsync(sohbetId, request, ct);
                await hub.Clients.Group(MuhasebeciSohbetHub.GroupName(sohbetId)).SendAsync("ConversationUpdated", result, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Arsiv durumu guncellenemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/sohbetler/muhasebeciler/{muhasebeciIsletmeId:int}", async (
            int muhasebeciIsletmeId,
            IMuhasebeciSohbetMerkeziService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(new SohbetIdSonuc(await service.GetOrCreateForCustomerAsync(muhasebeciIsletmeId, ct)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbet acilamadi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/sohbetler/talepler/{talepId:int}", async (
            int talepId,
            IMuhasebeciSohbetMerkeziService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(new SohbetIdSonuc(await service.GetOrCreateForAccountantRequestAsync(talepId, ct)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbet acilamadi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/sohbetler/musteriler/{musteriIsletmeId:int}", async (
            int musteriIsletmeId,
            IMuhasebeciSohbetMerkeziService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(new SohbetIdSonuc(await service.GetOrCreateForAccountantCustomerAsync(musteriIsletmeId, ct)));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbet acilamadi: {ex.Message}"));
            }
        });
    }

    private static Task PublishMessageAsync(IHubContext<MuhasebeciSohbetHub> hub, MuhasebeciSohbetMerkeziMesajiDto message, CancellationToken ct)
    {
        return Task.WhenAll(
            hub.Clients.Group(MuhasebeciSohbetHub.GroupName(message.SohbetId)).SendAsync("MessageCreated", message, ct),
            hub.Clients.Group(MuhasebeciSohbetHub.GroupName(message.SohbetId)).SendAsync("ConversationUpdated", new { sohbetId = message.SohbetId }, ct));
    }

    private sealed record ApiHata(string mesaj);
    private sealed record SohbetIdSonuc(int sohbetId);
}
