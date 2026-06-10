using System.Security.Cryptography;
using System.Text.Json;
using CashTracker.Core.Import;

namespace Systemcel.Api.Import;

internal sealed class DesktopImportCodeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storePath;
    private readonly object _sync = new();

    public DesktopImportCodeStore(AppRuntimeOptions runtimeOptions)
    {
        _storePath = Path.Combine(runtimeOptions.AppDataPath, "import", "desktop-import-codes.json");
    }

    public DesktopImportCodeRecord Create(int? targetIsletmeId, string requestedBy)
    {
        lock (_sync)
        {
            var store = Load();
            PruneExpired(store);

            var code = CreateUniqueCode(store);
            var now = DateTime.UtcNow;
            var record = new DesktopImportCodeRecord
            {
                Code = code,
                Status = DesktopImportCodeStatus.Active,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddHours(24),
                TargetIsletmeId = targetIsletmeId,
                RequestedBy = requestedBy
            };

            store.Codes.Add(record);
            Save(store);
            return record;
        }
    }

    public DesktopImportCodeRecord? Find(string code)
    {
        lock (_sync)
        {
            var store = Load();
            var record = store.Codes.FirstOrDefault(x => CodesEqual(x.Code, code));
            if (record is null)
                return null;

            if (record.Status == DesktopImportCodeStatus.Active && record.ExpiresAtUtc <= DateTime.UtcNow)
            {
                record.Status = DesktopImportCodeStatus.Expired;
                Save(store);
            }

            return record;
        }
    }

    public DesktopImportCodeRecord RequireActive(string code)
    {
        var record = Find(code);
        if (record is null)
            throw new DesktopImportValidationException("Aktarim kodu bulunamadi.");

        if (record.Status != DesktopImportCodeStatus.Active)
            throw new DesktopImportValidationException($"Aktarim kodu aktif degil: {record.Status}.");

        if (record.ExpiresAtUtc <= DateTime.UtcNow)
            throw new DesktopImportValidationException("Aktarim kodunun suresi dolmus.");

        return record;
    }

    public void MarkUsed(string code, string packageId, DesktopImportTotals importedTotals)
    {
        lock (_sync)
        {
            var store = Load();
            var record = store.Codes.FirstOrDefault(x => CodesEqual(x.Code, code))
                ?? throw new DesktopImportValidationException("Aktarim kodu bulunamadi.");

            record.Status = DesktopImportCodeStatus.Used;
            record.UsedAtUtc = DateTime.UtcNow;
            record.PackageId = packageId;
            record.ImportedTotals = importedTotals;
            Save(store);
        }
    }

    private static bool CodesEqual(string left, string right)
    {
        return string.Equals(NormalizeCode(left), NormalizeCode(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static string CreateUniqueCode(DesktopImportCodeStoreDocument store)
    {
        for (var i = 0; i < 20; i++)
        {
            var code = $"MIG-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
            if (store.Codes.All(x => !CodesEqual(x.Code, code)))
                return code;
        }

        throw new InvalidOperationException("Aktarim kodu uretilemedi.");
    }

    private static void PruneExpired(DesktopImportCodeStoreDocument store)
    {
        var now = DateTime.UtcNow;
        foreach (var record in store.Codes)
        {
            if (record.Status == DesktopImportCodeStatus.Active && record.ExpiresAtUtc <= now)
                record.Status = DesktopImportCodeStatus.Expired;
        }
    }

    private DesktopImportCodeStoreDocument Load()
    {
        if (!File.Exists(_storePath))
            return new DesktopImportCodeStoreDocument();

        var json = File.ReadAllText(_storePath);
        return JsonSerializer.Deserialize<DesktopImportCodeStoreDocument>(json, JsonOptions)
            ?? new DesktopImportCodeStoreDocument();
    }

    private void Save(DesktopImportCodeStoreDocument store)
    {
        var directory = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(store, JsonOptions);
        File.WriteAllText(_storePath, json);
    }
}

internal static class DesktopImportCodeStatus
{
    public const string Active = "Active";
    public const string Used = "Used";
    public const string Expired = "Expired";
}

internal sealed class DesktopImportCodeStoreDocument
{
    public List<DesktopImportCodeRecord> Codes { get; set; } = new();
}

internal sealed class DesktopImportCodeRecord
{
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = DesktopImportCodeStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(24);
    public DateTime? UsedAtUtc { get; set; }
    public int? TargetIsletmeId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public DesktopImportTotals ImportedTotals { get; set; } = new();
}

internal sealed class DesktopImportValidationException : Exception
{
    public DesktopImportValidationException(string message) : base(message) { }
}
