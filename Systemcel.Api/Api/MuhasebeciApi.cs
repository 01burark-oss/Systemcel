using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Systemcel.Api;

namespace Systemcel.Api.Api;

internal static class MuhasebeciApi
{
    public static void MapMuhasebeciApi(this WebApplication app)
    {
        app.MapGet("/api/public/muhasebeciler", async (
            string? arama,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetPublicMarketplaceAsync(arama, ct));
            }
            catch (Exception ex)
            {
                return Results.Problem($"Muhasebeci pazaryeri yuklenemedi: {ex.Message}");
            }
        });

        app.MapGet("/api/public/muhasebeciler/profil-resimleri/{fileName}", (
            string fileName,
            AppRuntimeOptions runtimeOptions) =>
        {
            var safeName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeName) || safeName != fileName)
                return Results.NotFound();

            var path = Path.Combine(GetAccountantProfileImageDirectory(runtimeOptions), safeName);
            if (!File.Exists(path))
                return Results.NotFound();

            return Results.File(path, ContentTypeForImage(path));
        });

        app.MapGet("/api/ekran/muhasebeciler", async (
            string? arama,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetMarketplaceAsync(arama, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Muhasebeciler yuklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeciler/{muhasebeciIsletmeId:int}/talep", async (
            int muhasebeciIsletmeId,
            MuhasebeciTalepOlusturRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.SubmitMarketplaceRequestAsync(muhasebeciIsletmeId, request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Talep gonderilemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/muhasebeciler/{muhasebeciIsletmeId:int}/sohbet", async (
            int muhasebeciIsletmeId,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetCustomerConversationAsync(muhasebeciIsletmeId, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbet yuklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeciler/{muhasebeciIsletmeId:int}/sohbet", async (
            int muhasebeciIsletmeId,
            MuhasebeciSohbetMesajiGonderRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.SendCustomerConversationMessageAsync(muhasebeciIsletmeId, request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Mesaj gonderilemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/muhasebeci", async (IMuhasebeciPortalService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetPanelAsync(ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Muhasebeci paneli yuklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/profil-resmi", async (
            HttpContext context,
            AppRuntimeOptions runtimeOptions,
            CancellationToken ct) =>
        {
            try
            {
                if (!context.Request.HasFormContentType)
                    return Results.BadRequest(new ApiHata("Profil resmi multipart/form-data olarak gönderilmelidir."));

                var form = await context.Request.ReadFormAsync(ct);
                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Results.BadRequest(new ApiHata("Profil resmi seçilmedi."));

                if (file.Length > 5 * 1024 * 1024)
                    return Results.BadRequest(new ApiHata("Profil resmi en fazla 5 MB olabilir."));

                var extension = ExtensionForImage(file);
                if (string.IsNullOrWhiteSpace(extension))
                    return Results.BadRequest(new ApiHata("Profil resmi JPG, PNG veya WEBP olmalıdır."));

                var directory = GetAccountantProfileImageDirectory(runtimeOptions);
                Directory.CreateDirectory(directory);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(directory, fileName);

                await using var stream = File.Create(path);
                await file.CopyToAsync(stream, ct);

                return Results.Ok(new ProfilResmiYukleSonuc($"/api/public/muhasebeciler/profil-resimleri/{fileName}"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Profil resmi yüklenemedi: {ex.Message}"));
            }
        });

        app.MapPut("/api/ekran/muhasebeci/profil", async (
            MuhasebeciProfilKaydetRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.SaveProfileAsync(request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Profil kaydedilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/davetler", async (
            HttpContext context,
            MuhasebeciTalepOlusturRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                return Results.Ok(await service.CreateInviteAsync(request, baseUrl, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Davet olusturulamadi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/davetler/kabul", async (
            MuhasebeciDavetKabulRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.AcceptInviteAsync(request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Davet kabul edilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/talepler/{talepId:int}/kabul", async (
            int talepId,
            MuhasebeciTalepKararRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.AcceptRequestAsync(talepId, request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Talep kabul edilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/talepler/{talepId:int}/red", async (
            int talepId,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.RejectRequestAsync(talepId, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Talep reddedilemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/muhasebeci/talepler/{talepId:int}/sohbet", async (
            int talepId,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetAccountantRequestConversationAsync(talepId, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbet yuklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/talepler/{talepId:int}/sohbet", async (
            int talepId,
            MuhasebeciSohbetMesajiGonderRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.SendAccountantRequestConversationMessageAsync(talepId, request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Mesaj gonderilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/talepler/{talepId:int}/iptal", async (
            int talepId,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.CancelRequestAsync(talepId, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Talep iptal edilemedi: {ex.Message}"));
            }
        });

        app.MapGet("/api/ekran/muhasebeci/musteriler/{musteriIsletmeId:int}/sohbet", async (
            int musteriIsletmeId,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetAccountantCustomerConversationAsync(musteriIsletmeId, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Sohbet yuklenemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/musteriler/{musteriIsletmeId:int}/sohbet", async (
            int musteriIsletmeId,
            MuhasebeciSohbetMesajiGonderRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.SendAccountantCustomerConversationMessageAsync(musteriIsletmeId, request, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Mesaj gonderilemedi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/musteriler/{musteriIsletmeId:int}/ac", async (
            int musteriIsletmeId,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                await service.OpenCustomerContextAsync(musteriIsletmeId, ct);
                return Results.Ok(new ApiMesaj("Musteri calisma alani acildi."));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Musteri calisma alani acilamadi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/musteri-baglami/kapat", async (
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                await service.CloseCustomerContextAsync(ct);
                return Results.Ok(new ApiMesaj("Musteri baglami kapatildi."));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Musteri baglami kapatilamadi: {ex.Message}"));
            }
        });
    }

    private sealed record ApiHata(string mesaj);
    private sealed record ApiMesaj(string mesaj);
    private sealed record ProfilResmiYukleSonuc(string url);

    private static string GetAccountantProfileImageDirectory(AppRuntimeOptions runtimeOptions)
    {
        return Path.Combine(runtimeOptions.AppDataPath, "uploads", "accountant-profiles");
    }

    private static string ExtensionForImage(IFormFile file)
    {
        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        var extension = Path.GetExtension(file.FileName ?? string.Empty).Trim().ToLowerInvariant();

        return contentType switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ when extension is ".jpg" or ".jpeg" => ".jpg",
            _ when extension is ".png" => ".png",
            _ when extension is ".webp" => ".webp",
            _ => string.Empty
        };
    }

    private static string ContentTypeForImage(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
