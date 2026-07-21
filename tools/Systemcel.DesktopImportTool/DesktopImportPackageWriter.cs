using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CashTracker.Core.Import;

namespace Systemcel.DesktopImportTool;

internal sealed class DesktopImportPackageWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public DesktopImportPackageResult Write(
        string sourceDbPath,
        string? outputPath,
        string transferCode,
        DesktopImportPackageData data)
    {
        var packageId = Guid.NewGuid().ToString("N");
        var workingDirectory = Path.Combine(Path.GetTempPath(), "systemcel-import-" + packageId);
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var files = new List<DesktopImportFileEntry>
            {
                WriteDataFile(workingDirectory, DesktopImportContract.IsletmelerFileName, "Isletme", data.Isletmeler),
                WriteDataFile(workingDirectory, DesktopImportContract.CariKartlarFileName, "CariKart", data.CariKartlar),
                WriteDataFile(workingDirectory, DesktopImportContract.CariHareketlerFileName, "CariHareket", data.CariHareketler),
                WriteDataFile(workingDirectory, DesktopImportContract.UrunlerFileName, "UrunHizmet", data.Urunler),
                WriteDataFile(workingDirectory, DesktopImportContract.StokHareketleriFileName, "StokHareket", data.StokHareketleri),
                WriteDataFile(workingDirectory, DesktopImportContract.FaturalarFileName, "Fatura", data.Faturalar),
                WriteDataFile(workingDirectory, DesktopImportContract.FaturaSatirlariFileName, "FaturaSatir", data.FaturaSatirlari),
                WriteDataFile(workingDirectory, DesktopImportContract.TahsilatOdemelerFileName, "TahsilatOdeme", data.TahsilatOdemeler),
                WriteDataFile(workingDirectory, DesktopImportContract.KasaHareketleriFileName, "KasaHareket", data.KasaHareketleri)
            };

            var sourceFile = new FileInfo(sourceDbPath);
            var manifest = new DesktopImportManifest
            {
                ManifestVersion = DesktopImportContract.ManifestVersion,
                PackageId = packageId,
                CreatedAtUtc = DateTime.UtcNow,
                Source = new DesktopImportSourceInfo
                {
                    AppName = "CashTracker Desktop",
                    DatabaseProvider = "SQLite",
                    DatabaseFileName = sourceFile.Name,
                    DatabaseSizeBytes = sourceFile.Length,
                    DatabaseSha256 = HashFile(sourceDbPath),
                    SchemaVersion = "legacy-sqlite",
                    ExportToolVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty
                },
                Transfer = new DesktopImportTransferInfo
                {
                    Code = transferCode.Trim()
                },
                Totals = data.BuildTotals(),
                Files = files
            };

            WriteJson(Path.Combine(workingDirectory, DesktopImportContract.ManifestFileName), manifest);

            var zipPath = ResolveZipPath(outputPath, packageId);
            var zipDirectory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrWhiteSpace(zipDirectory))
                Directory.CreateDirectory(zipDirectory);
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(workingDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            return new DesktopImportPackageResult(zipPath, manifest);
        }
        finally
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup; the package ZIP has already been produced.
            }
        }
    }

    private static DesktopImportFileEntry WriteDataFile<T>(
        string workingDirectory,
        string fileName,
        string entity,
        IReadOnlyCollection<T> rows)
    {
        var path = Path.Combine(workingDirectory, fileName);
        WriteJson(path, rows);

        return new DesktopImportFileEntry
        {
            Entity = entity,
            Path = fileName,
            Count = rows.Count,
            Sha256 = HashFile(path),
            Required = true
        };
    }

    private static void WriteJson<T>(string path, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static string ResolveZipPath(string? outputPath, string packageId)
    {
        var defaultName = $"systemcel-import-{DateTime.Now:yyyyMMdd-HHmmss}-{packageId[..8]}.zip";
        if (string.IsNullOrWhiteSpace(outputPath))
            return Path.GetFullPath(defaultName);

        var fullPath = Path.GetFullPath(outputPath);
        if (string.Equals(Path.GetExtension(fullPath), ".zip", StringComparison.OrdinalIgnoreCase))
            return fullPath;

        return Path.Combine(fullPath, defaultName);
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}

internal sealed record DesktopImportPackageResult(string ZipPath, DesktopImportManifest Manifest);
