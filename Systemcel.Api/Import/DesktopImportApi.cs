using CashTracker.Core.Import;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Import;

internal static class DesktopImportApi
{
    public static void MapDesktopImportApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/import/desktop");
        var clerkOptions = app.Services.GetRequiredService<ClerkAuthenticationOptions>();
        if (clerkOptions.Enabled)
            group.RequireAuthorization();

        group.MapPost("/codes", (
            DesktopImportCodeCreateRequest? request,
            HttpContext httpContext,
            DesktopImportCodeStore codeStore) =>
        {
            var record = codeStore.Create(
                request?.IsletmeId,
                httpContext.User.FindFirst("sub")?.Value ?? string.Empty);

            return Results.Ok(new DesktopImportCodeCreateResponse
            {
                Code = record.Code,
                ExpiresAtUtc = record.ExpiresAtUtc,
                ManifestVersion = DesktopImportContract.ManifestVersion,
                PackageEndpoint = "/api/import/desktop/packages"
            });
        });

        group.MapGet("/codes/{code}", (string code, DesktopImportCodeStore codeStore) =>
        {
            var record = codeStore.Find(code);
            return record is null
                ? Results.NotFound(new { mesaj = "Aktarim kodu bulunamadi." })
                : Results.Ok(record);
        });

        group.MapPost("/packages", async (
            HttpRequest request,
            DesktopImportService importService,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { mesaj = "multipart/form-data bekleniyor." });

            var form = await request.ReadFormAsync(ct);
            var code = form["code"].ToString();
            var package = form.Files.GetFile("package") ?? form.Files.FirstOrDefault();
            if (package is null)
                return Results.BadRequest(new { mesaj = "package alaninda ZIP dosyasi yukleyin." });

            try
            {
                var response = await importService.AcceptPackageAsync(code, package, ct);
                return Results.Ok(response);
            }
            catch (DesktopImportValidationException ex)
            {
                return Results.BadRequest(new { mesaj = ex.Message });
            }
        });
    }
}

internal sealed class DesktopImportCodeCreateRequest
{
    public int? IsletmeId { get; set; }
}

internal sealed class DesktopImportCodeCreateResponse
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string ManifestVersion { get; set; } = DesktopImportContract.ManifestVersion;
    public string PackageEndpoint { get; set; } = string.Empty;
}
