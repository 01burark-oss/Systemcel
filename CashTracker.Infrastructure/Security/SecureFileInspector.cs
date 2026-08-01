using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace CashTracker.Infrastructure.Security;

public enum SecureFilePurpose
{
    ProfileImage,
    ChatAttachment
}

public sealed record SecureFileInspection(
    string DisplayFileName,
    string Extension,
    string ContentType);

public static class SecureFileInspector
{
    private const int SignatureBytes = 512;
    private const int MaxArchiveEntries = 100;
    private const long MaxArchiveExpandedBytes = 50L * 1024 * 1024;
    private const double MaxCompressionRatio = 200d;

    public static async Task<SecureFileInspection> InspectAsync(
        Stream stream,
        string fileName,
        long declaredLength,
        SecureFilePurpose purpose,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
            throw new InvalidOperationException("Dosya akisi guvenli inceleme icin aranabilir olmalidir.");
        if (declaredLength <= 0)
            throw new InvalidOperationException("Dosya bos olamaz.");

        var maxBytes = purpose == SecureFilePurpose.ProfileImage
            ? 5L * 1024 * 1024
            : 10L * 1024 * 1024;
        if (declaredLength > maxBytes)
            throw new InvalidOperationException($"Dosya en fazla {maxBytes / 1024 / 1024} MB olabilir.");

        var displayName = SanitizeDisplayFileName(fileName);
        var suppliedExtension = Path.GetExtension(displayName).ToLowerInvariant();
        var origin = stream.Position;
        var header = new byte[Math.Min(SignatureBytes, (int)Math.Min(declaredLength, SignatureBytes))];
        var read = 0;
        while (read < header.Length)
        {
            var count = await stream.ReadAsync(header.AsMemory(read, header.Length - read), ct);
            if (count == 0)
                break;
            read += count;
        }
        stream.Position = origin;

        var inspection = purpose == SecureFilePurpose.ProfileImage
            ? InspectProfileImage(header.AsSpan(0, read), displayName, suppliedExtension)
            : InspectChatAttachment(stream, header.AsSpan(0, read), displayName, suppliedExtension);
        stream.Position = origin;
        return inspection;
    }

    public static async Task<long> CopyBoundedAsync(
        Stream source,
        Stream destination,
        long declaredLength,
        long maxBytes,
        CancellationToken ct = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            long total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                if (read == 0)
                    break;
                total += read;
                if (total > maxBytes)
                    throw new InvalidOperationException("Dosya izin verilen boyutu asti.");
                await destination.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            if (total != declaredLength)
                throw new InvalidOperationException("Dosya boyutu aktarim bilgisiyle uyusmuyor.");
            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    public static string SanitizeDisplayFileName(string? value)
    {
        var fileName = Path.GetFileName(value ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        fileName = new string(fileName.Where(x => !char.IsControl(x) && !invalid.Contains(x)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
            throw new InvalidOperationException("Gecerli bir dosya adi gerekli.");
        if (fileName.Length > 150)
        {
            var extension = Path.GetExtension(fileName);
            var stemLength = Math.Max(1, 150 - extension.Length);
            fileName = fileName[..stemLength] + extension;
        }
        return fileName;
    }

    public static bool IsPathInside(string candidatePath, string rootPath)
    {
        var candidate = Path.GetFullPath(candidatePath);
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        return candidate.StartsWith(root, OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal);
    }

    private static SecureFileInspection InspectProfileImage(
        ReadOnlySpan<byte> header,
        string displayName,
        string suppliedExtension)
    {
        var detected = DetectBinary(header);
        if (detected is null || detected.Value.Extension is not (".jpg" or ".png" or ".webp"))
            throw new InvalidOperationException("Profil resmi gercek bir JPG, PNG veya WEBP dosyasi olmalidir.");
        EnsureExtensionMatches(suppliedExtension, detected.Value.Extension, allowJpegAlias: true);
        return new SecureFileInspection(displayName, detected.Value.Extension, detected.Value.ContentType);
    }

    private static SecureFileInspection InspectChatAttachment(
        Stream stream,
        ReadOnlySpan<byte> header,
        string displayName,
        string suppliedExtension)
    {
        var detected = DetectBinary(header);
        if (detected is not null)
        {
            if (detected.Value.Extension == ".zip")
            {
                var isXlsx = ValidateArchive(stream);
                detected = isXlsx
                    ? (".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    : (".zip", "application/zip");
            }
            EnsureExtensionMatches(suppliedExtension, detected.Value.Extension, allowJpegAlias: true);
            return new SecureFileInspection(displayName, detected.Value.Extension, detected.Value.ContentType);
        }

        if (!TryDecodeUtf8(header, out var text))
            throw new InvalidOperationException("Dosya turu imzasindan dogrulanamadi.");
        var trimmed = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        var textType = suppliedExtension switch
        {
            ".html" or ".htm" when trimmed.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
                                    trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) =>
                (".html", "text/html"),
            ".xml" when trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                        (trimmed.StartsWith('<') && !trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)) =>
                (".xml", "application/xml"),
            ".csv" when !trimmed.Contains('\0') => (".csv", "text/csv"),
            _ => throw new InvalidOperationException("HTML, XML veya CSV dosyasinin icerigi uzantisiyla uyusmuyor.")
        };
        return new SecureFileInspection(displayName, textType.Item1, textType.Item2);
    }

    private static (string Extension, string ContentType)? DetectBinary(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return (".jpg", "image/jpeg");
        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return (".png", "image/png");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return (".webp", "image/webp");
        if (bytes.StartsWith("%PDF-"u8))
            return (".pdf", "application/pdf");
        if (bytes.StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 }) ||
            bytes.StartsWith(new byte[] { 0x50, 0x4B, 0x05, 0x06 }) ||
            bytes.StartsWith(new byte[] { 0x50, 0x4B, 0x07, 0x08 }))
            return (".zip", "application/zip");
        if (bytes.StartsWith(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }))
            return (".webm", "audio/webm");
        if (bytes.StartsWith("OggS"u8))
            return (".ogg", "audio/ogg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WAVE"u8))
            return (".wav", "audio/wav");
        if (bytes.StartsWith("ID3"u8) ||
            (bytes.Length >= 2 && bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0))
            return (".mp3", "audio/mpeg");
        if (bytes.Length >= 12 && bytes.Slice(4, 4).SequenceEqual("ftyp"u8))
            return (".m4a", "audio/mp4");
        return null;
    }

    private static bool ValidateArchive(Stream stream)
    {
        var origin = stream.Position;
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count == 0 || archive.Entries.Count > MaxArchiveEntries)
                throw new InvalidOperationException("Arsiv dosya sayisi guvenlik sinirini asti.");

            long expanded = 0;
            foreach (var entry in archive.Entries)
            {
                var normalized = entry.FullName.Replace('\\', '/');
                if (normalized.StartsWith('/') || normalized.Split('/').Any(x => x == ".."))
                    throw new InvalidOperationException("Arsiv guvenli olmayan bir dosya yolu iceriyor.");
                expanded = checked(expanded + entry.Length);
                if (expanded > MaxArchiveExpandedBytes)
                    throw new InvalidOperationException("Arsivin acilmis boyutu guvenlik sinirini asti.");
                if (entry.Length > 0 && (entry.CompressedLength == 0 ||
                    entry.Length / (double)entry.CompressedLength > MaxCompressionRatio))
                    throw new InvalidOperationException("Arsiv supheli sikistirma orani nedeniyle reddedildi.");
            }

            return archive.GetEntry("[Content_Types].xml") is not null &&
                   archive.GetEntry("xl/workbook.xml") is not null;
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException("ZIP/XLSX arsivi bozuk veya desteklenmiyor.", ex);
        }
        finally
        {
            stream.Position = origin;
        }
    }

    private static void EnsureExtensionMatches(string supplied, string detected, bool allowJpegAlias)
    {
        if (allowJpegAlias && detected == ".jpg" && supplied is ".jpg" or ".jpeg")
            return;
        if (!string.Equals(supplied, detected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Dosya uzantisi ile gercek icerik imzasi uyusmuyor.");
    }

    private static bool TryDecodeUtf8(ReadOnlySpan<byte> bytes, out string text)
    {
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }
}
