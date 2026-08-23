using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Systemcel.Api;
using CashTracker.Infrastructure.Security;

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

        app.MapGet("/api/public/muhasebeci-davetleri/{token}", async (
            string token,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                var invite = await service.GetCustomerLinkInviteAsync(token, ct);
                return invite is null ? Results.NotFound(new ApiHata("Davet bağlantısı bulunamadı.")) : Results.Ok(invite);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Davet bağlantısı açılamadı: {ex.Message}"));
            }
        });

        app.MapGet("/api/public/muhasebeciler/profil-resimleri/{fileName}", (
            string fileName,
            AppRuntimeOptions runtimeOptions) =>
        {
            var safeName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeName) || safeName != fileName)
                return Results.NotFound();

            var directory = GetAccountantProfileImageDirectory(runtimeOptions);
            var path = Path.Combine(directory, safeName);
            if (!SecureFileInspector.IsPathInside(path, directory))
                return Results.NotFound();
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
            IIsletmeService isletmeService,
            CancellationToken ct) =>
        {
            try
            {
                if (!context.Request.HasFormContentType)
                    return Results.BadRequest(new ApiHata("Profil resmi multipart/form-data olarak gönderilmelidir."));

                var activeBusiness = await isletmeService.GetActiveAsync();
                if (!CanUploadProfileImage(activeBusiness))
                    return Results.Json(new ApiHata("Profil resmi yalnız muhasebeci hesabında yüklenebilir."), statusCode: StatusCodes.Status403Forbidden);

                var form = await context.Request.ReadFormAsync(ct);
                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Results.BadRequest(new ApiHata("Profil resmi seçilmedi."));

                await using var input = file.OpenReadStream();
                var inspection = await SecureFileInspector.InspectAsync(
                    input,
                    file.FileName,
                    file.Length,
                    SecureFilePurpose.ProfileImage,
                    ct);

                var directory = GetAccountantProfileImageDirectory(runtimeOptions);
                Directory.CreateDirectory(directory);
                var fileName = $"{Guid.NewGuid():N}{inspection.Extension}";
                var path = Path.Combine(directory, fileName);

                try
                {
                    await using var output = new FileStream(
                        path,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        64 * 1024,
                        FileOptions.Asynchronous | FileOptions.WriteThrough);
                    await SecureFileInspector.CopyBoundedAsync(input, output, file.Length, 5L * 1024 * 1024, ct);
                }
                catch
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    throw;
                }

                return Results.Ok(new ProfilResmiYukleSonuc($"/api/public/muhasebeciler/profil-resimleri/{fileName}"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Profil resmi yüklenemedi: {ex.Message}"));
            }
        }).RequireRateLimiting("upload");

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
            catch (EntitlementViolationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Davet olusturulamadi: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/link-davetleri", async (
            HttpContext context,
            MuhasebeciLinkDavetOlusturRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                return Results.Ok(await service.CreateCustomerLinkInviteAsync(request, baseUrl, ct));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Davet bağlantısı oluşturulamadı: {ex.Message}"));
            }
        });

        app.MapPost("/api/ekran/muhasebeci/link-davetleri/kabul", async (
            MuhasebeciLinkDavetKabulRequest request,
            IMuhasebeciPortalService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.AcceptCustomerLinkInviteAsync(request, ct));
            }
            catch (EntitlementViolationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Davet kabul edilemedi: {ex.Message}"));
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
            catch (EntitlementViolationException)
            {
                throw;
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
            catch (EntitlementViolationException)
            {
                throw;
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

    internal static bool CanUploadProfileImage(Isletme activeBusiness)
    {
        return string.Equals(activeBusiness.TenantTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
            || !activeBusiness.KolayKurulumTamamlandi;
    }

    private sealed record ApiHata(string mesaj);
    private sealed record ApiMesaj(string mesaj);
    private sealed record ProfilResmiYukleSonuc(string url);

    private static string GetAccountantProfileImageDirectory(AppRuntimeOptions runtimeOptions)
    {
        return Path.Combine(runtimeOptions.AppDataPath, "uploads", "accountant-profiles");
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
