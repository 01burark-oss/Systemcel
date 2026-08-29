using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Systemcel.Api.Import;

internal static class ExternalDataMigrationLimits
{
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxRows = 5_000;
    public const int MaxColumns = 32;
    public const int MaxCellLength = 2_000;
}

internal sealed record MigrationError(int Row, string Message);

internal sealed class MigrationPreview
{
    public string DraftId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int DuplicateRows { get; init; }
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<IReadOnlyDictionary<string, string>> SampleRows { get; init; } = [];
    public IReadOnlyList<MigrationError> Errors { get; init; } = [];
    public bool CanApply => ValidRows > 0 && Errors.Count == 0;
    public string? UnsupportedReason { get; init; }
}

internal sealed class MigrationApplyResult
{
    public int Applied { get; init; }
    public int SkippedDuplicates { get; init; }
    public IReadOnlyList<MigrationError> Errors { get; init; } = [];
}

internal sealed class ExternalDataMigrationService
{
    private static readonly TimeSpan DraftLifetime = TimeSpan.FromMinutes(15);
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly ICariService _cariService;
    private readonly IUrunHizmetService _urunService;
    private readonly IStokService _stokService;
    private readonly IKalemTanimiService _kalemService;
    private readonly ConcurrentDictionary<string, Draft> _drafts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _applyGates = new();

    public ExternalDataMigrationService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        ICariService cariService,
        IUrunHizmetService urunService,
        IStokService stokService,
        IKalemTanimiService kalemService)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _cariService = cariService;
        _urunService = urunService;
        _stokService = stokService;
        _kalemService = kalemService;
    }

    public async Task<MigrationPreview> PreviewAsync(string type, IFormFile file, CancellationToken ct)
    {
        var normalizedType = MigrationCsvParser.NormalizeType(type);
        if (file.Length <= 0)
            throw new MigrationValidationException("Dosya boş.");
        if (file.Length > ExternalDataMigrationLimits.MaxFileBytes)
            throw new MigrationValidationException("Dosya en fazla 10 MB olabilir.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new MigrationValidationException("Şimdilik yalnızca CSV şablonu destekleniyor. Excel, CSV olarak kaydedilebilir.");

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
        ParsedDocument parsed;
        try
        {
            parsed = await MigrationCsvParser.ParseAsync(normalizedType, reader, ct);
        }
        catch (DecoderFallbackException)
        {
            throw new MigrationValidationException("Dosya UTF-8 olarak kaydedilmelidir.");
        }
        var draftId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var businessId = await _isletmeService.GetActiveIdAsync();
        _drafts[draftId] = new Draft(DateTimeOffset.UtcNow.Add(DraftLifetime), businessId, normalizedType, file.FileName, parsed);
        TrimExpiredDrafts();

        return new MigrationPreview
        {
            DraftId = draftId,
            Type = normalizedType,
            FileName = file.FileName,
            TotalRows = parsed.Rows.Count,
            ValidRows = parsed.ValidRows.Count,
            DuplicateRows = parsed.DuplicateRows,
            Headers = parsed.Headers,
            SampleRows = parsed.ValidRows.Take(5).Select(x => (IReadOnlyDictionary<string, string>)x.Values).ToList(),
            Errors = parsed.Errors,
            UnsupportedReason = normalizedType == "fatura"
                ? "Açık faturalar, cari ve satır eşleştirmesi kesin olmadığı için bu sürümde aktarılmıyor."
                : null
        };
    }

    public async Task<MigrationApplyResult> ApplyAsync(string draftId, CancellationToken ct)
    {
        if (!_drafts.TryRemove(draftId, out var draft) || draft.ExpiresAt < DateTimeOffset.UtcNow)
            throw new MigrationValidationException("Önizleme süresi doldu. Dosyayı yeniden seçin.");
        if (draft.Parsed.Errors.Count > 0)
            throw new MigrationValidationException("Düzeltilmesi gereken satırlar var. Önce yeni bir önizleme oluşturun.");
        if (draft.BusinessId != await _isletmeService.GetActiveIdAsync())
            throw new MigrationValidationException("Aktif işletme değişti. Dosyayı yeni işletmede yeniden önizleyin.");
        if (draft.Type == "fatura")
            throw new MigrationValidationException("Açık faturalar bu sürümde aktarılmıyor.");

        var applyGate = _applyGates.GetOrAdd(draft.BusinessId, static _ => new SemaphoreSlim(1, 1));
        await applyGate.WaitAsync(ct);
        try
        {
            return draft.Type switch
            {
                "cari" => await ApplyCariAsync(draft.BusinessId, draft.Parsed.ValidRows, ct),
                "urun" => await ApplyUrunAsync(draft.BusinessId, draft.Parsed.ValidRows, ct),
                "stok" => await ApplyStokAsync(draft.BusinessId, draft.Parsed.ValidRows, ct),
                "kategori" => await ApplyKategoriAsync(draft.Parsed.ValidRows, ct),
                _ => throw new MigrationValidationException("Bu veri türü desteklenmiyor.")
            };
        }
        finally
        {
            applyGate.Release();
        }
    }

    public static string Template(string type)
    {
        return MigrationCsvParser.Template(MigrationCsvParser.NormalizeType(type));
    }

    private async Task<MigrationApplyResult> ApplyCariAsync(int businessId, IReadOnlyList<ParsedRow> rows, CancellationToken ct)
    {
        var existing = await _cariService.GetAllAsync(ct);
        var byName = new Dictionary<string, CariKart>(StringComparer.OrdinalIgnoreCase);
        var byTaxNumber = new Dictionary<string, CariKart>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in existing)
        {
            if (!string.IsNullOrWhiteSpace(card.Unvan)) byName.TryAdd(card.Unvan.Trim(), card);
            if (!string.IsNullOrWhiteSpace(card.VergiNoTc)) byTaxNumber.TryAdd(card.VergiNoTc.Trim(), card);
        }
        var importedCardIds = new HashSet<int>();
        var errors = new List<MigrationError>();
        var applied = 0;
        var duplicates = 0;
        foreach (var row in rows)
        {
            try
            {
                var unvan = row.Required("unvan");
                var vergiNo = row.Optional("vergiNo");
                var duplicate = FindCari(byName, byTaxNumber, unvan, vergiNo);
                if (duplicate is not null && !importedCardIds.Add(duplicate.Id))
                {
                    duplicates++;
                    continue;
                }

                var id = duplicate?.Id ?? await _cariService.CreateAsync(new CariKart
                {
                    Tip = row.Optional("tip", "Musteri"), Unvan = unvan, Telefon = row.Optional("telefon"),
                    Eposta = row.Optional("eposta"), Adres = row.Optional("adres"), VergiNoTc = vergiNo,
                    VergiDairesi = row.Optional("vergiDairesi")
                }, ct);
                if (duplicate is not null) duplicates++;
                else
                {
                    var added = new CariKart { Id = id, Unvan = unvan, VergiNoTc = vergiNo };
                    byName.TryAdd(unvan, added);
                    if (!string.IsNullOrWhiteSpace(vergiNo)) byTaxNumber.TryAdd(vergiNo, added);
                    importedCardIds.Add(id);
                    applied++;
                }
                var balance = row.Decimal("acilisBakiyesi", optional: true);
                if (balance != 0 && !await MovementExistsAsync(businessId, "cari", row.Key("cari"), ct))
                {
                    await _cariService.CreateHareketAsync(new CariHareket
                    {
                        CariKartId = id, Tutar = Math.Abs(balance), HareketTipi = balance > 0 ? "Alacak" : "Borc",
                        Tarih = row.Date("tarih", DateTime.Today), Kaynak = "VeriAktarimi",
                        Aciklama = MovementDescription("cari", row.Key("cari"))
                    }, ct);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            { errors.Add(new MigrationError(row.Number, ex.Message)); }
        }
        return new MigrationApplyResult { Applied = applied, SkippedDuplicates = duplicates, Errors = errors };
    }

    private async Task<MigrationApplyResult> ApplyUrunAsync(int businessId, IReadOnlyList<ParsedRow> rows, CancellationToken ct)
    {
        var existing = await _urunService.GetAllAsync(ct);
        var byName = new Dictionary<string, UrunHizmet>(StringComparer.OrdinalIgnoreCase);
        var byBarcode = new Dictionary<string, UrunHizmet>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in existing)
        {
            if (!string.IsNullOrWhiteSpace(product.Ad)) byName.TryAdd(product.Ad.Trim(), product);
            if (!string.IsNullOrWhiteSpace(product.Barkod)) byBarcode.TryAdd(product.Barkod.Trim(), product);
        }
        var importedProductIds = new HashSet<int>();
        var errors = new List<MigrationError>(); var applied = 0; var duplicates = 0;
        foreach (var row in rows)
        {
            try
            {
                var ad = row.Required("ad"); var barkod = row.Optional("barkod");
                var duplicate = FindProduct(byName, byBarcode, ad, barkod);
                if (duplicate is not null && !importedProductIds.Add(duplicate.Id))
                {
                    duplicates++;
                    continue;
                }
                var id = duplicate?.Id ?? await _urunService.CreateAsync(new UrunHizmetCreateRequest
                {
                    Tip = row.Optional("tip", "Urun"), Ad = ad, Barkod = barkod, Birim = row.Optional("birim", "Adet"),
                    KdvOrani = row.Decimal("kdvOrani", true), AlisFiyati = row.Decimal("alisFiyati", true),
                    SatisFiyati = row.Decimal("satisFiyati", true), ParaBirimi = row.Optional("paraBirimi", "TRY")
                }, ct);
                if (duplicate is not null) duplicates++;
                else
                {
                    var added = new UrunHizmet { Id = id, Ad = ad, Barkod = barkod };
                    byName.TryAdd(ad, added);
                    if (!string.IsNullOrWhiteSpace(barkod)) byBarcode.TryAdd(barkod, added);
                    importedProductIds.Add(id);
                    applied++;
                }
                var stock = row.Decimal("acilisStok", true);
                if (stock != 0 && !await MovementExistsAsync(businessId, "stok", row.Key("stok"), ct))
                    await _stokService.CreateMovementAsync(new StokHareketCreateRequest
                    {
                        UrunHizmetId = id,
                        Miktar = stock,
                        BirimMaliyet = stock > 0m ? row.OptionalDecimal("alisFiyati") : null,
                        Tarih = row.Date("tarih", DateTime.Today),
                        Kaynak = "VeriAktarimi",
                        Aciklama = MovementDescription("stok", row.Key("stok"))
                    }, ct);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            { errors.Add(new MigrationError(row.Number, ex.Message)); }
        }
        return new MigrationApplyResult { Applied = applied, SkippedDuplicates = duplicates, Errors = errors };
    }

    private async Task<MigrationApplyResult> ApplyStokAsync(int businessId, IReadOnlyList<ParsedRow> rows, CancellationToken ct)
    {
        var products = await _urunService.GetAllAsync(ct);
        var errors = new List<MigrationError>(); var applied = 0; var duplicates = 0;
        foreach (var row in rows)
        {
            try
            {
                var product = products.FirstOrDefault(x => (!string.IsNullOrWhiteSpace(row.Optional("barkod")) && x.Barkod.Equals(row.Optional("barkod"), StringComparison.OrdinalIgnoreCase)) || x.Ad.Equals(row.Required("ad"), StringComparison.OrdinalIgnoreCase));
                if (product is null) throw new InvalidOperationException("Ürün bulunamadı. Önce ürün şablonunu aktarın.");
                var key = row.Key("stok");
                if (await MovementExistsAsync(businessId, "stok", key, ct)) { duplicates++; continue; }
                var quantity = row.Decimal("miktar");
                await _stokService.CreateMovementAsync(new StokHareketCreateRequest
                {
                    UrunHizmetId = product.Id,
                    Miktar = quantity,
                    BirimMaliyet = quantity > 0m ? row.OptionalDecimal("birimMaliyet") : null,
                    Tarih = row.Date("tarih", DateTime.Today),
                    Kaynak = "VeriAktarimi",
                    Aciklama = MovementDescription("stok", key)
                }, ct);
                applied++;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            { errors.Add(new MigrationError(row.Number, ex.Message)); }
        }
        return new MigrationApplyResult { Applied = applied, SkippedDuplicates = duplicates, Errors = errors };
    }

    private async Task<MigrationApplyResult> ApplyKategoriAsync(IReadOnlyList<ParsedRow> rows, CancellationToken ct)
    {
        var existing = await _kalemService.GetAllAsync();
        var categories = new HashSet<string>(existing.Select(x => CategoryKey(x.Tip, x.Ad)), StringComparer.OrdinalIgnoreCase);
        var errors = new List<MigrationError>(); var applied = 0; var duplicates = 0;
        foreach (var row in rows)
        {
            try
            {
                var tip = MigrationCsvParser.NormalizeCategoryType(row.Required("tip"));
                var ad = row.Required("ad");
                if (!categories.Add(CategoryKey(tip, ad)))
                {
                    duplicates++;
                    continue;
                }
                await _kalemService.CreateAsync(tip, ad);
                applied++;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { errors.Add(new MigrationError(row.Number, ex.Message)); }
        }
        return new MigrationApplyResult { Applied = applied, SkippedDuplicates = duplicates, Errors = errors };
    }

    private async Task<bool> MovementExistsAsync(int businessId, string type, string key, CancellationToken ct)
    {
        var description = MovementDescription(type, key);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return type == "cari"
            ? await db.CariHareketleri.AnyAsync(x => x.IsletmeId == businessId && x.Kaynak == "VeriAktarimi" && x.Aciklama == description, ct)
            : await db.StokHareketleri.AnyAsync(x => x.IsletmeId == businessId && x.Kaynak == "VeriAktarimi" && x.Aciklama == description, ct);
    }

    private static CariKart? FindCari(IReadOnlyDictionary<string, CariKart> byName, IReadOnlyDictionary<string, CariKart> byTaxNumber, string name, string taxNumber) =>
        byName.GetValueOrDefault(name) ?? (!string.IsNullOrWhiteSpace(taxNumber) ? byTaxNumber.GetValueOrDefault(taxNumber) : null);

    private static UrunHizmet? FindProduct(IReadOnlyDictionary<string, UrunHizmet> byName, IReadOnlyDictionary<string, UrunHizmet> byBarcode, string name, string barcode) =>
        byName.GetValueOrDefault(name) ?? (!string.IsNullOrWhiteSpace(barcode) ? byBarcode.GetValueOrDefault(barcode) : null);

    private static string CategoryKey(string type, string name) => $"{MigrationCsvParser.NormalizeCategoryType(type)}\u001f{name.Trim()}";
    private static string MovementDescription(string type, string key) => $"Veri aktarımı:{type}:{key}";
    private void TrimExpiredDrafts() { foreach (var pair in _drafts) if (pair.Value.ExpiresAt < DateTimeOffset.UtcNow) _drafts.TryRemove(pair.Key, out _); }
    private sealed record Draft(DateTimeOffset ExpiresAt, int BusinessId, string Type, string FileName, ParsedDocument Parsed);
}

internal sealed class MigrationValidationException : Exception
{
    public MigrationValidationException(string message) : base(message) { }
}

internal sealed record ParsedDocument(IReadOnlyList<string> Headers, IReadOnlyList<ParsedRow> Rows, IReadOnlyList<ParsedRow> ValidRows, IReadOnlyList<MigrationError> Errors, int DuplicateRows);
internal sealed record ParsedRow(int Number, IReadOnlyDictionary<string, string> Values)
{
    public string Required(string key) => Optional(key) is { Length: > 0 } value ? value : throw new ArgumentException($"{key} alanı boş.");
    public string Optional(string key, string fallback = "") => Values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
    public decimal Decimal(string key, bool optional = false)
    {
        var raw = Optional(key);
        if (string.IsNullOrWhiteSpace(raw) && optional) return 0m;
        if (MigrationCsvParser.TryDecimal(raw, out var value)) return value;
        throw new ArgumentException($"{key} alanı sayı olmalı.");
    }
    public decimal? OptionalDecimal(string key) => string.IsNullOrWhiteSpace(Optional(key)) ? null : Decimal(key);
    public DateTime Date(string key, DateTime fallback) => string.IsNullOrWhiteSpace(Optional(key)) ? fallback : MigrationCsvParser.TryDate(Optional(key), out var value) ? value : throw new ArgumentException($"{key} alanı tarih olmalı.");
    public string Key(string prefix) => Optional("kayitAnahtari", $"satir-{Number}");
}

internal static class MigrationCsvParser
{
    private static readonly IReadOnlyDictionary<string, string[]> Schemas = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["cari"] = ["kayitAnahtari", "unvan", "tip", "telefon", "eposta", "adres", "vergiNo", "vergiDairesi", "acilisBakiyesi", "tarih"],
        ["urun"] = ["kayitAnahtari", "ad", "tip", "barkod", "birim", "kdvOrani", "alisFiyati", "satisFiyati", "paraBirimi", "acilisStok", "tarih"],
        ["stok"] = ["kayitAnahtari", "ad", "barkod", "miktar", "birimMaliyet", "tarih"],
        ["kategori"] = ["kayitAnahtari", "tip", "ad"],
        ["fatura"] = ["kayitAnahtari", "faturaNo", "cariUnvan", "tarih", "vadeTarihi", "faturaTipi", "genelToplam"]
    };

    public static string NormalizeType(string type) => (type ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "cari" or "cari kart" or "cariler" => "cari", "urun" or "ürün" or "urun-hizmet" => "urun",
        "stok" or "acilis stok" => "stok", "kategori" or "kalem" or "gelir-gider" => "kategori",
        "fatura" or "acik fatura" => "fatura", _ => throw new MigrationValidationException("Veri türü desteklenmiyor.")
    };

    public static async Task<ParsedDocument> ParseAsync(string type, TextReader reader, CancellationToken ct)
    {
        var lines = new List<string>();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (line.Length > ExternalDataMigrationLimits.MaxCellLength * ExternalDataMigrationLimits.MaxColumns) throw new MigrationValidationException("Satır çok uzun.");
            lines.Add(line);
            if (lines.Count > ExternalDataMigrationLimits.MaxRows + 1) throw new MigrationValidationException("Dosyada en fazla 5.000 veri satırı olabilir.");
        }
        if (lines.Count == 0) throw new MigrationValidationException("CSV başlığı bulunamadı.");
        var separator = DetectSeparator(lines[0]);
        var headers = ParseLine(lines[0], separator).Select(NormalizeHeader).ToList();
        if (headers.Count == 0 || headers.Count > ExternalDataMigrationLimits.MaxColumns || headers.Any(string.IsNullOrWhiteSpace) || headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
            throw new MigrationValidationException("CSV başlıkları geçersiz veya tekrar ediyor.");
        var required = Schemas[type];
        if (required.Where(x => x is "unvan" or "ad" or "tip" or "miktar").Any(x => !headers.Contains(x, StringComparer.OrdinalIgnoreCase)))
            throw new MigrationValidationException($"Şablon başlıkları eksik. Beklenen alanlar: {string.Join(", ", required)}");

        var rows = new List<ParsedRow>(); var errors = new List<MigrationError>(); var valid = new List<ParsedRow>(); var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var duplicateRows = 0;
        for (var i = 1; i < lines.Count; i++)
        {
            IReadOnlyList<string> cells;
            try { cells = ParseLine(lines[i], separator); }
            catch (MigrationValidationException ex) { errors.Add(new MigrationError(i + 1, ex.Message)); continue; }
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            if (cells.Count > headers.Count) { errors.Add(new MigrationError(i + 1, "Beklenenden fazla alan var.")); continue; }
            var values = headers.Select((h, index) => new { h, value = index < cells.Count ? cells[index] : string.Empty }).ToDictionary(x => x.h, x => x.value, StringComparer.OrdinalIgnoreCase);
            if (values.Values.Any(x => x.Length > ExternalDataMigrationLimits.MaxCellLength)) { errors.Add(new MigrationError(i + 1, "Bir alan çok uzun.")); continue; }
            var row = new ParsedRow(i + 1, values); rows.Add(row);
            try { ValidateCellSecurity(type, row); ValidateRow(type, row); var key = row.Key(type); if (!keys.Add(key)) { duplicateRows++; continue; } valid.Add(row); }
            catch (ArgumentException ex) { errors.Add(new MigrationError(i + 1, ex.Message)); }
        }
        return new ParsedDocument(headers, rows, valid, errors, duplicateRows);
    }

    public static string Template(string type)
    {
        if (!Schemas.TryGetValue(type, out var columns)) throw new MigrationValidationException("Şablon bulunamadı.");
        var example = type switch { "cari" => "cari-1;Örnek Müşteri;Musteri;;;;;;0;2026-01-01", "urun" => "urun-1;Örnek Ürün;Urun;8690000000000;Adet;20;10;15;TRY;0;2026-01-01", "stok" => "stok-1;Örnek Ürün;8690000000000;10;12,50;2026-01-01", "kategori" => "kategori-1;Gelir;Danışmanlık", _ => "fatura-1;F-001;Örnek Müşteri;2026-01-01;;Satis;1000" };
        return string.Join("\r\n", new[] { string.Join(';', columns), example }) + "\r\n";
    }

    public static bool TryDecimal(string raw, out decimal value)
    {
        var normalized = (raw ?? string.Empty).Trim().Replace("₺", "").Replace(" ", "");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out value) || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
    public static string NormalizeCategoryType(string type) => type.Trim().ToLowerInvariant() switch
    {
        "gelir" or "giris" or "income" => "Gelir",
        "gider" or "cikis" or "expense" => "Gider",
        _ => throw new ArgumentException("tip sadece gelir veya gider olabilir.")
    };
    public static bool TryDate(string raw, out DateTime value) => DateTime.TryParse(raw, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out value) || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    private static char DetectSeparator(string line) => new[] { ';', ',', '\t' }.OrderByDescending(c => line.Count(x => x == c)).First();
    private static string NormalizeHeader(string value) => value.Trim().Trim('\uFEFF').ToLowerInvariant().Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u").Replace("ö", "o").Replace("ç", "c");
    private static IReadOnlyList<string> ParseLine(string line, char separator)
    {
        var values = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++) { var ch = line[i]; if (ch == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; } else if (ch == separator && !quoted) { values.Add(cell.ToString().Trim()); cell.Clear(); } else cell.Append(ch); }
        if (quoted) throw new MigrationValidationException("Kapanmamış tırnak var."); values.Add(cell.ToString().Trim()); return values;
    }
    private static void ValidateRow(string type, ParsedRow row)
    {
        if (type == "cari") { row.Required("unvan"); row.Decimal("acilisBakiyesi", true); }
        else if (type == "urun") { row.Required("ad"); row.Decimal("kdvOrani", true); row.Decimal("alisFiyati", true); row.Decimal("satisFiyati", true); row.Decimal("acilisStok", true); }
        else if (type == "stok") { row.Required("ad"); if (row.Decimal("miktar") == 0) throw new ArgumentException("miktar alanı sıfır olamaz."); row.Decimal("birimMaliyet", true); }
        else if (type == "kategori") { NormalizeCategoryType(row.Required("tip")); row.Required("ad"); }
        else { row.Required("faturaNo"); throw new ArgumentException("Açık faturalar bu sürümde aktarılmıyor."); }
    }

    private static void ValidateCellSecurity(string type, ParsedRow row)
    {
        foreach (var pair in row.Values)
        {
            var value = pair.Value;
            if (ContainsUnsafeControlCharacter(value))
                throw new ArgumentException("Görünmeyen veya yön değiştiren karakterler kabul edilmiyor.");

            var trimmed = value.TrimStart();
            var negativeNumber = trimmed.StartsWith('-') && IsNumericColumn(type, pair.Key) && TryDecimal(trimmed, out _);
            if (trimmed.StartsWith('=') || trimmed.StartsWith('@') || trimmed.StartsWith('+') || (trimmed.StartsWith('-') && !negativeNumber))
                throw new ArgumentException("Formül içeren alanlar kabul edilmiyor.");
        }
    }

    private static bool IsNumericColumn(string type, string column) => type switch
    {
        "cari" => column is "acilisBakiyesi",
        "urun" => column is "kdvOrani" or "alisFiyati" or "satisFiyati" or "acilisStok",
        "stok" => column is "miktar" or "birimMaliyet",
        "fatura" => column is "genelToplam",
        _ => false
    };

    private static bool ContainsUnsafeControlCharacter(string value) => value.Any(character =>
        character == '\0' ||
        (character >= '\u200B' && character <= '\u200F') ||
        (character >= '\u202A' && character <= '\u202E') ||
        (character >= '\u2066' && character <= '\u2069'));
}

internal static class ExternalDataMigrationApi
{
    public static void MapExternalDataMigrationApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/ekran/veri-aktarim");
        var clerkOptions = app.Services.GetRequiredService<ClerkAuthenticationOptions>();
        if (clerkOptions.Enabled)
            group.RequireAuthorization();
        group.MapGet("/sablon/{type}", (string type) =>
        {
            try { return (IResult)Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(ExternalDataMigrationService.Template(type))).ToArray(), "text/csv; charset=utf-8", $"systemcel-{MigrationCsvParser.NormalizeType(type)}-sablon.csv"); }
            catch (MigrationValidationException ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
        });
        group.MapPost("/onizleme", async (HttpRequest request, ExternalDataMigrationService service, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest(new { mesaj = "multipart/form-data bekleniyor." });
            IFormCollection form;
            try
            {
                form = await request.ReadFormAsync(ct);
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { mesaj = "Dosya biçimi okunamadı." });
            }

            var type = form["type"].ToString();
            var file = form.Files.GetFile("file");
            if (file is null) return Results.BadRequest(new { mesaj = "CSV dosyası seçin." });
            try { return Results.Ok(await service.PreviewAsync(type, file, ct)); }
            catch (MigrationValidationException ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
        }).WithMetadata(new RequestSizeLimitAttribute(ExternalDataMigrationLimits.MaxFileBytes + 64 * 1024))
            .RequireRateLimiting("upload");
        group.MapPost("/uygula", async (MigrationApplyRequest request, ExternalDataMigrationService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.DraftId)) return Results.BadRequest(new { mesaj = "Önizleme bulunamadı." });
            try { return Results.Ok(await service.ApplyAsync(request.DraftId, ct)); }
            catch (MigrationValidationException ex) { return Results.BadRequest(new { mesaj = ex.Message }); }
        }).RequireRateLimiting("sensitive");
    }
    private sealed class MigrationApplyRequest { public string DraftId { get; set; } = string.Empty; }
}
