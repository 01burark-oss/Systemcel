using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Security;

namespace Systemcel.Api.Api;

internal static class MobilTaramaApi
{
    public static void MapMobilTaramaApi(this WebApplication app)
    {
        app.MapGet("/api/ekran/mobil-tarama/durum", (ReceiptOcrSettings settings) =>
            Results.Ok(new { fisOcrHazir = settings.IsConfigured }));

        app.MapPost("/api/ekran/mobil-tarama/barkod", async (
            HttpContext context,
            IBarcodeReaderService barcodeReader,
            CancellationToken ct) =>
        {
            try
            {
                var file = await ReadImageAsync(context, ct);
                var path = Path.Combine(Path.GetTempPath(), $"systemcel-barcode-{Guid.NewGuid():N}{file.Inspection.Extension}");
                try
                {
                    await File.WriteAllBytesAsync(path, file.Bytes, ct);
                    var result = await barcodeReader.TryReadAsync(path, ct);
                    return result.Success
                        ? Results.Ok(new { barkod = result.Barcode })
                        : Results.BadRequest(new ApiHata(string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Barkod okunamadı." : result.ErrorMessage));
                }
                finally
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Barkod fotoğrafı okunamadı: {ex.Message}"));
            }
        }).RequireRateLimiting("upload");

        app.MapPost("/api/ekran/mobil-tarama/fis-ocr", async (
            HttpContext context,
            ReceiptOcrSettings settings,
            IReceiptOcrService receiptOcr,
            IAiUsageQuotaService usageQuota,
            IIsletmeService businesses,
            IKalemTanimiService categories,
            CancellationToken ct) =>
        {
            if (!settings.IsConfigured)
                return Results.Json(new ApiHata("Fiş okuma şu anda kullanılamıyor."), statusCode: StatusCodes.Status503ServiceUnavailable);

            try
            {
                var file = await ReadImageAsync(context, ct);
                var business = await businesses.GetActiveAsync();
                var expenseCategories = (await categories.GetByTipAsync("Gider"))
                    .Where(x => x.IsletmeId == business.Id)
                    .Select(x => x.Ad)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var result = await AnalyzeReceiptWithQuotaAsync(usageQuota, receiptOcr, new ReceiptOcrRequest
                {
                    BusinessName = business.Ad,
                    FileName = file.Inspection.DisplayFileName,
                    MimeType = file.Inspection.ContentType,
                    ImageBytes = file.Bytes,
                    AvailableExpenseCategories = expenseCategories
                }, ct);
                return Results.Ok(result);
            }
            catch (EntitlementViolationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ApiHata($"Fiş okunamadı: {ex.Message}"));
            }
        }).RequireRateLimiting("upload");
    }

    internal static async Task<ReceiptOcrResult> AnalyzeReceiptWithQuotaAsync(
        IAiUsageQuotaService usageQuota,
        IReceiptOcrService receiptOcr,
        ReceiptOcrRequest request,
        CancellationToken ct = default)
    {
        var usage = await usageQuota.ConsumeAsync(ct);
        EnsureOcrUsageAllowed(usage);
        return await receiptOcr.AnalyzeReceiptAsync(request, ct);
    }

    private static void EnsureOcrUsageAllowed(AiUsageStatus usage)
    {
        if (!usage.AiAktif)
        {
            throw new EntitlementViolationException(
                EntitlementErrorCodes.FeatureNotAvailable,
                usage.Mesaj,
                suggestedPlanCode: PlanKodlari.IsletmeBaslangic);
        }

        if (usage.IzinVerildi)
            return;

        var suggestedPlan = string.Equals(usage.PlanKodu, PlanKodlari.IsletmeBaslangic, StringComparison.Ordinal)
            ? PlanKodlari.IsletmeBuyume
            : PlanKodlari.IsletmeKurumsal;
        throw new EntitlementViolationException(
            EntitlementErrorCodes.LimitReached,
            usage.Mesaj,
            EntitlementLimits.AiMessage,
            usage.Limit,
            usage.Kullanilan,
            suggestedPlan);
    }

    private static async Task<UploadedImage> ReadImageAsync(HttpContext context, CancellationToken ct)
    {
        if (!context.Request.HasFormContentType)
            throw new InvalidOperationException("Fotoğraf multipart/form-data olarak gönderilmelidir.");

        var form = await context.Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("Fotoğraf seçilmedi.");

        await using var input = file.OpenReadStream();
        var inspection = await SecureFileInspector.InspectAsync(
            input,
            file.FileName,
            file.Length,
            SecureFilePurpose.ProfileImage,
            ct);
        await using var output = new MemoryStream((int)file.Length);
        await SecureFileInspector.CopyBoundedAsync(input, output, file.Length, 5L * 1024 * 1024, ct);
        return new UploadedImage(inspection, output.ToArray());
    }

    private sealed record UploadedImage(SecureFileInspection Inspection, byte[] Bytes);
    private sealed record ApiHata(string mesaj);
}
