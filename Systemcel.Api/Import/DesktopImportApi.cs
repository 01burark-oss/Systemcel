using CashTracker.Core.Import;
using CashTracker.Core.Services;
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

        group.MapPost("/codes", async (
            DesktopImportCodeCreateRequest? request,
            HttpContext httpContext,
            IIsletmeService isletmeService,
            DesktopImportCodeStore codeStore,
            IWebHostEnvironment environment,
            CancellationToken ct) =>
        {
            var target = request?.IsletmeId is { } requestedId
                ? await isletmeService.GetByIdAsync(requestedId)
                : await isletmeService.GetActiveAsync();
            if (target is null)
                return Results.NotFound(new { mesaj = "Hedef isletme bulunamadi veya erisim yetkisi yok." });

            var requestedBy = ResolveRequestIdentity(httpContext, environment);
            var record = await codeStore.CreateAsync(target.Id, requestedBy, ct);

            return Results.Ok(new DesktopImportCodeCreateResponse
            {
                Code = record.Code,
                ExpiresAtUtc = record.ExpiresAtUtc,
                TargetIsletmeId = target.Id,
                RequestedBy = requestedBy,
                ManifestVersion = DesktopImportContract.ManifestVersion,
                PackageEndpoint = "/api/import/desktop/packages"
            });
        }).RequireRateLimiting("sensitive");

        group.MapGet("/codes/{code}", async (
            string code,
            HttpContext httpContext,
            DesktopImportCodeStore codeStore,
            IWebHostEnvironment environment,
            CancellationToken ct) =>
        {
            var record = await codeStore.FindAsync(code, ResolveRequestIdentity(httpContext, environment), ct);
            return record is null
                ? Results.NotFound(new { mesaj = "Aktarim kodu bulunamadi." })
                : Results.Ok(record);
        }).RequireRateLimiting("sensitive");

        group.MapPost("/packages", async (
            HttpRequest request,
            DesktopImportService importService,
            IWebHostEnvironment environment,
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
                var response = await importService.AcceptPackageAsync(
                    code,
                    package,
                    ResolveRequestIdentity(request.HttpContext, environment),
                    ct);
                return Results.Ok(response);
            }
            catch (DesktopImportValidationException ex)
            {
                return Results.BadRequest(new { mesaj = ex.Message });
            }
        }).RequireRateLimiting("upload");
    }

    private static string ResolveRequestIdentity(HttpContext context, IWebHostEnvironment environment)
    {
        var subject = context.User.FindFirst("sub")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(subject))
            return subject;
        if (environment.IsDevelopment())
            return "local-development-user";
        throw new UnauthorizedAccessException("Kimligi dogrulanmis kullanici gerekli.");
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
    public int TargetIsletmeId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = DesktopImportContract.ManifestVersion;
    public string PackageEndpoint { get; set; } = string.Empty;
}
