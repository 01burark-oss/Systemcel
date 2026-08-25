using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class BankaMutabakatService : IBankaMutabakatService
{
    public const long AzamiDosyaBoyutu = 2L * 1024 * 1024;
    private const int AzamiSatirSayisi = 2_000;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.CultureInvariant);
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public BankaMutabakatService(IDbContextFactory<CashTrackerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<BankaHareketDto>> ListeleAsync(int isletmeId, string? durum = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.BankaHareketleri.AsNoTracking().Where(x => x.IsletmeId == isletmeId);
        if (!string.IsNullOrWhiteSpace(durum))
        {
            var normalized = NormalizeDurum(durum);
            query = query.Where(x => x.Durum == normalized);
        }

        return await query
            .OrderByDescending(x => x.Tarih)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .Select(x => new BankaHareketDto(x.Id, x.Tarih, x.Aciklama, x.Tutar, x.ParaBirimi, x.Durum, x.EslesenKaynakTuru, x.EslesenKaynakId))
            .ToListAsync(ct);
    }

    public async Task<BankaCsvImportSonucu> CsvIceAktarAsync(int isletmeId, Stream csv, string dosyaAdi, long uzunluk, CancellationToken ct = default)
    {
        if (isletmeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(isletmeId));
        if (!string.Equals(Path.GetExtension(Path.GetFileName(dosyaAdi)), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Yalnız .csv uzantılı banka hareketi dosyaları kabul edilir.", nameof(dosyaAdi));
        if (uzunluk <= 0 || uzunluk > AzamiDosyaBoyutu)
            throw new ArgumentException("CSV dosyası boş olamaz ve 2 MB sınırını aşamaz.", nameof(uzunluk));

        var bytes = await ReadLimitedAsync(csv, ct);
        ValidateSignature(bytes);
        string content;
        try
        {
            content = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new ArgumentException("CSV dosyası geçerli UTF-8 metni olmalıdır.", nameof(csv));
        }

        var parsed = ParseCsv(content);
        if (parsed.Count < 2)
            throw new ArgumentException("CSV dosyasında başlık ve en az bir hareket satırı bulunmalıdır.", nameof(csv));
        if (parsed.Count - 1 > AzamiSatirSayisi)
            throw new ArgumentException($"Tek dosyada en fazla {AzamiSatirSayisi} hareket içe aktarılabilir.", nameof(csv));

        var header = BuildHeaderMap(parsed[0]);
        var candidates = new Dictionary<string, BankaHareketi>(StringComparer.Ordinal);
        var validRowCount = 0;
        for (var rowIndex = 1; rowIndex < parsed.Count; rowIndex++)
        {
            if (parsed[rowIndex].All(string.IsNullOrWhiteSpace))
                continue;

            var row = ParseRow(parsed[rowIndex], header, rowIndex + 1, isletmeId);
            validRowCount++;
            candidates.TryAdd(row.KaynakHash, row);
        }

        if (candidates.Count == 0)
            throw new ArgumentException("CSV dosyasında içe aktarılabilir hareket bulunamadı.", nameof(csv));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hashes = candidates.Keys.ToList();
        var existing = await db.BankaHareketleri
            .Where(x => x.IsletmeId == isletmeId && hashes.Contains(x.KaynakHash))
            .Select(x => x.KaynakHash)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var additions = candidates.Values.Where(x => !existingSet.Contains(x.KaynakHash)).ToList();
        db.BankaHareketleri.AddRange(additions);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Tenant + hash unique index is the concurrency authority. A racing import may win.
            db.ChangeTracker.Clear();
            existing = await db.BankaHareketleri
                .Where(x => x.IsletmeId == isletmeId && hashes.Contains(x.KaynakHash))
                .Select(x => x.KaynakHash)
                .ToListAsync(ct);
            existingSet = existing.ToHashSet(StringComparer.Ordinal);
            additions = candidates.Values.Where(x => !existingSet.Contains(x.KaynakHash)).ToList();
            if (additions.Count > 0)
            {
                db.BankaHareketleri.AddRange(additions);
                await db.SaveChangesAsync(ct);
            }
        }

        return new BankaCsvImportSonucu(additions.Count, validRowCount - additions.Count, validRowCount);
    }

    public async Task<IReadOnlyList<BankaEslesmeAdayi>> AdaylariGetirAsync(int isletmeId, int hareketId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hareket = await db.BankaHareketleri.AsNoTracking().SingleOrDefaultAsync(x => x.Id == hareketId && x.IsletmeId == isletmeId, ct)
            ?? throw new KeyNotFoundException("Banka hareketi bulunamadı.");
        if (hareket.Durum != BankaHareketDurumlari.Acik)
            return Array.Empty<BankaEslesmeAdayi>();

        return await BuildCandidatesAsync(db, hareket, ct);
    }

    public async Task EslesmeOnaylaAsync(int isletmeId, int hareketId, BankaEslesmeIstek istek, CancellationToken ct = default)
    {
        if (!istek.Onaylandi)
            throw new ArgumentException("Eşleştirme için açık kullanıcı onayı zorunludur.", nameof(istek));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var hareket = await db.BankaHareketleri.SingleOrDefaultAsync(x => x.Id == hareketId && x.IsletmeId == isletmeId, ct)
            ?? throw new KeyNotFoundException("Banka hareketi bulunamadı.");
        if (hareket.Durum != BankaHareketDurumlari.Acik)
            throw new InvalidOperationException("Yalnız açık banka hareketleri eşleştirilebilir.");

        var candidates = await BuildCandidatesAsync(db, hareket, ct);
        if (!candidates.Any(x => x.KaynakTuru == istek.KaynakTuru && x.KaynakId == istek.KaynakId))
            throw new ArgumentException("Seçilen kayıt bu işletme için geçerli bir eşleşme adayı değildir.", nameof(istek));

        hareket.Durum = BankaHareketDurumlari.Eslesti;
        hareket.EslesenKaynakTuru = istek.KaynakTuru;
        hareket.EslesenKaynakId = istek.KaynakId;
        hareket.EslestiAt = DateTime.Now;
        hareket.YokSayildiAt = null;
        hareket.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task YokSayAsync(int isletmeId, int hareketId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hareket = await db.BankaHareketleri.SingleOrDefaultAsync(x => x.Id == hareketId && x.IsletmeId == isletmeId, ct)
            ?? throw new KeyNotFoundException("Banka hareketi bulunamadı.");
        if (hareket.Durum != BankaHareketDurumlari.Acik)
            throw new InvalidOperationException("Yalnız açık banka hareketleri yok sayılabilir.");

        hareket.Durum = BankaHareketDurumlari.YokSayildi;
        hareket.YokSayildiAt = DateTime.Now;
        hareket.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
    }

    private static async Task<IReadOnlyList<BankaEslesmeAdayi>> BuildCandidatesAsync(CashTrackerDbContext db, BankaHareketi hareket, CancellationToken ct)
    {
        // Mevcut finansal kayıtlar para birimi taşımıyor; TRY dışı hareketlerde yanlış aday üretme.
        if (!string.Equals(hareket.ParaBirimi, "TRY", StringComparison.Ordinal))
            return Array.Empty<BankaEslesmeAdayi>();

        var from = hareket.Tarih.Date.AddDays(-30);
        var to = hareket.Tarih.Date.AddDays(31);
        var cariNames = await db.CariKartlari.AsNoTracking()
            .Where(x => x.IsletmeId == hareket.IsletmeId)
            .Select(x => new { x.Id, x.Unvan })
            .ToDictionaryAsync(x => x.Id, x => x.Unvan, ct);
        var raw = new List<(string Type, int Id, string Title, decimal Amount, DateTime Date, string Text)>();

        var invoices = await db.Faturalar.AsNoTracking()
            .Where(x => x.IsletmeId == hareket.IsletmeId && x.Tarih >= from && x.Tarih < to)
            .OrderByDescending(x => x.Tarih).Take(250).ToListAsync(ct);
        raw.AddRange(invoices.Select(x => (
            BankaEslesmeKaynakTurleri.Fatura,
            x.Id,
            $"Fatura {x.YerelFaturaNo.DefaultIfBlank($"#{x.Id}")}",
            string.Equals(x.FaturaTipi, "Alis", StringComparison.OrdinalIgnoreCase) ? -x.GenelToplam : x.GenelToplam,
            x.Tarih,
            $"{x.Aciklama} {cariNames.GetValueOrDefault(x.CariKartId)} {x.YerelFaturaNo}")));

        var payments = await db.TahsilatOdemeleri.AsNoTracking()
            .Where(x => x.IsletmeId == hareket.IsletmeId && x.Tarih >= from && x.Tarih < to)
            .OrderByDescending(x => x.Tarih).Take(250).ToListAsync(ct);
        raw.AddRange(payments.Select(x => (
            BankaEslesmeKaynakTurleri.TahsilatOdeme,
            x.Id,
            $"{(x.Tip == "Odeme" ? "Ödeme" : "Tahsilat")} #{x.Id}",
            x.Tip == "Odeme" ? -x.Tutar : x.Tutar,
            x.Tarih,
            $"{x.Aciklama} {cariNames.GetValueOrDefault(x.CariKartId)}")));

        var ledger = await db.CariHareketleri.AsNoTracking()
            .Where(x => x.IsletmeId == hareket.IsletmeId && x.Tarih >= from && x.Tarih < to)
            .OrderByDescending(x => x.Tarih).Take(250).ToListAsync(ct);
        raw.AddRange(ledger.Select(x => (
            BankaEslesmeKaynakTurleri.CariHareket,
            x.Id,
            $"Cari hareket #{x.Id}",
            x.HareketTipi is "Odeme" or "Alacak" ? -x.Tutar : x.Tutar,
            x.Tarih,
            $"{x.Aciklama} {cariNames.GetValueOrDefault(x.CariKartId)}")));

        return raw
            .Select(x => Score(hareket, x.Type, x.Id, x.Title, x.Amount, x.Date, x.Text))
            .Where(x => x.Skor >= 60)
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.KaynakTuru, StringComparer.Ordinal)
            .ThenBy(x => x.KaynakId)
            .Take(30)
            .ToList();
    }

    private static BankaEslesmeAdayi Score(BankaHareketi bank, string type, int id, string title, decimal amount, DateTime date, string text)
    {
        var score = 0;
        var reasons = new List<string>();
        var difference = Math.Abs(bank.Tutar - amount);
        if (difference <= 0.01m) { score += 60; reasons.Add("Tutar aynı"); }
        else if (difference <= 1m) { score += 50; reasons.Add("Tutar çok yakın"); }

        var days = Math.Abs((bank.Tarih.Date - date.Date).Days);
        if (days == 0) { score += 25; reasons.Add("Tarih aynı"); }
        else if (days <= 3) { score += 20; reasons.Add("Tarih 3 gün içinde"); }
        else if (days <= 7) { score += 12; reasons.Add("Tarih 7 gün içinde"); }
        else { score += 4; }

        var bankTokens = Tokenize(bank.Aciklama);
        var sourceTokens = Tokenize(text);
        if (bankTokens.Count > 0)
        {
            var overlap = bankTokens.Count(sourceTokens.Contains);
            var textScore = (int)Math.Round(15m * overlap / bankTokens.Count, MidpointRounding.AwayFromZero);
            score += textScore;
            if (textScore > 0) reasons.Add("Açıklama benziyor");
        }

        return new BankaEslesmeAdayi(type, id, title, amount, date, Math.Min(score, 100), reasons);
    }

    private static HashSet<string> Tokenize(string? value) => NormalizeText(value)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Where(x => x.Length >= 2)
        .ToHashSet(StringComparer.Ordinal);

    private static async Task<byte[]> ReadLimitedAsync(Stream stream, CancellationToken ct)
    {
        await using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), ct);
            if (read == 0) break;
            if (buffer.Length + read > AzamiDosyaBoyutu)
                throw new ArgumentException("CSV dosyası 2 MB sınırını aşamaz.", nameof(stream));
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        return buffer.ToArray();
    }

    private static void ValidateSignature(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Contains((byte)0))
            throw new ArgumentException("CSV dosyası metin içeriği taşımalıdır.");
        if ((bytes.Length >= 2 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K') ||
            (bytes.Length >= 2 && bytes[0] == (byte)'M' && bytes[1] == (byte)'Z') ||
            (bytes.Length >= 4 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P' && bytes[2] == (byte)'D' && bytes[3] == (byte)'F'))
            throw new ArgumentException("Dosya içeriği CSV metni olarak doğrulanamadı.");
    }

    private static List<List<string>> ParseCsv(string content)
    {
        var text = content.TrimStart('\uFEFF');
        var delimiter = DetectDelimiter(text);
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (ch == delimiter && !quoted) { row.Add(field.ToString()); field.Clear(); }
            else if ((ch == '\r' || ch == '\n') && !quoted)
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear(); rows.Add(row); row = new List<string>();
                if (rows.Count > AzamiSatirSayisi + 2) throw new ArgumentException("CSV satır sınırı aşıldı.");
            }
            else field.Append(ch);
        }
        if (quoted) throw new ArgumentException("CSV dosyasında kapanmamış tırnak bulundu.");
        row.Add(field.ToString());
        if (row.Any(x => x.Length > 0)) rows.Add(row);
        return rows;
    }

    private static char DetectDelimiter(string text)
    {
        var firstLine = text.Split(new[] { '\r', '\n' }, 2)[0];
        return firstLine.Count(x => x == ';') >= firstLine.Count(x => x == ',') ? ';' : ',';
    }

    private sealed record HeaderMap(int Date, int Description, int? Amount, int? Debit, int? Credit, int? Currency, int? Reference);

    private static HeaderMap BuildHeaderMap(IReadOnlyList<string> fields)
    {
        var normalized = fields.Select(NormalizeHeader).ToList();
        if (fields.Any(IsFormulaLike)) throw new ArgumentException("CSV başlıklarında hesap tablosu formülü kullanılamaz.");
        var date = FindHeader(normalized, "tarih", "date", "islemtarihi");
        var description = FindHeader(normalized, "aciklama", "description", "islemaciklamasi");
        var amount = FindOptionalHeader(normalized, "tutar", "amount");
        var debit = FindOptionalHeader(normalized, "borc", "debit");
        var credit = FindOptionalHeader(normalized, "alacak", "credit");
        if (amount is null && debit is null && credit is null)
            throw new ArgumentException("CSV başlıklarında Tutar veya Borç/Alacak sütunları bulunmalıdır.");
        return new HeaderMap(
            date,
            description,
            amount,
            debit,
            credit,
            FindOptionalHeader(normalized, "parabirimi", "doviz", "currency"),
            FindOptionalHeader(normalized, "referans", "referansno", "islemno", "transactionid", "reference"));
    }

    private static BankaHareketi ParseRow(IReadOnlyList<string> fields, HeaderMap header, int line, int businessId)
    {
        string At(int index) => index < fields.Count ? fields[index].Trim() : string.Empty;
        var dateRaw = At(header.Date);
        if (!TryParseDate(dateRaw, out var date)) throw new ArgumentException($"{line}. satırdaki tarih geçersiz.");
        var description = Regex.Replace(At(header.Description), "\\s+", " ").Trim();
        if (description.Length is 0 or > 500) throw new ArgumentException($"{line}. satırdaki açıklama 1-500 karakter olmalıdır.");
        if (IsFormulaLike(description)) throw new ArgumentException($"{line}. satırdaki açıklama hesap tablosu formülü içeremez.");

        decimal amount;
        if (header.Amount is int amountIndex)
        {
            if (!TryParseAmount(At(amountIndex), out amount)) throw new ArgumentException($"{line}. satırdaki tutar geçersiz.");
        }
        else
        {
            var debitRaw = header.Debit is int debitIndex ? At(debitIndex) : string.Empty;
            var creditRaw = header.Credit is int creditIndex ? At(creditIndex) : string.Empty;
            if (!TryParseOptionalAmount(debitRaw, out var debit) || !TryParseOptionalAmount(creditRaw, out var credit))
                throw new ArgumentException($"{line}. satırdaki Borç/Alacak tutarı geçersiz.");
            if (debit != 0 && credit != 0) throw new ArgumentException($"{line}. satırda Borç ve Alacak aynı anda dolu olamaz.");
            amount = credit != 0 ? Math.Abs(credit) : -Math.Abs(debit);
        }
        if (amount == 0 || Math.Abs(amount) > 999_999_999_999m) throw new ArgumentException($"{line}. satırdaki tutar sıfır olamaz veya sınırı aşamaz.");
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

        var currency = header.Currency is int currencyIndex && !string.IsNullOrWhiteSpace(At(currencyIndex)) ? At(currencyIndex).ToUpperInvariant() : "TRY";
        if (!CurrencyPattern.IsMatch(currency)) throw new ArgumentException($"{line}. satırdaki para birimi üç harfli kod olmalıdır.");
        var reference = header.Reference is int referenceIndex ? At(referenceIndex) : string.Empty;
        if (reference.Length > 120 || IsFormulaLike(reference)) throw new ArgumentException($"{line}. satırdaki referans geçersiz.");
        var hashDescription = Regex.Replace(description.Normalize(NormalizationForm.FormKC).ToLowerInvariant(), "\\s+", " ").Trim();
        var hashReference = Regex.Replace(reference.Normalize(NormalizationForm.FormKC).ToLowerInvariant(), "\\s+", " ").Trim();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{date:yyyy-MM-dd}|{hashDescription}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}|{currency}|{hashReference}")));
        return new BankaHareketi { IsletmeId = businessId, Tarih = date.Date, Aciklama = description, Tutar = amount, ParaBirimi = currency, Durum = BankaHareketDurumlari.Acik, KaynakHash = hash };
    }

    private static bool TryParseDate(string value, out DateTime result)
    {
        var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy/MM/dd" };
        return DateTime.TryParseExact(value, formats, TurkishCulture, DateTimeStyles.None, out result);
    }

    private static bool TryParseOptionalAmount(string value, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(value)) { amount = 0; return true; }
        return TryParseAmount(value, out amount);
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        var normalized = value.Trim().Replace("₺", "", StringComparison.Ordinal).Replace("TL", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (normalized.Contains(',')) return decimal.TryParse(normalized, NumberStyles.Number, TurkishCulture, out amount);
        if (Regex.IsMatch(normalized, "^-?\\d{1,3}(\\.\\d{3})+$"))
            return decimal.TryParse(normalized, NumberStyles.Number, TurkishCulture, out amount);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static string NormalizeDurum(string value) => value.Trim().ToLowerInvariant() switch
    {
        "acik" => BankaHareketDurumlari.Acik,
        "eslesti" => BankaHareketDurumlari.Eslesti,
        "yoksayildi" => BankaHareketDurumlari.YokSayildi,
        _ => throw new ArgumentException("Geçersiz banka hareketi durumu.", nameof(value))
    };

    private static int FindHeader(IReadOnlyList<string> fields, params string[] options) =>
        FindOptionalHeader(fields, options) ?? throw new ArgumentException($"CSV başlığında {options[0]} sütunu bulunamadı.");
    private static int? FindOptionalHeader(IReadOnlyList<string> fields, params string[] options)
    {
        for (var i = 0; i < fields.Count; i++) if (options.Contains(fields[i], StringComparer.Ordinal)) return i;
        return null;
    }

    private static bool IsFormulaLike(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@';
    }

    private static string NormalizeHeader(string value) => NormalizeText(value).Replace(" ", "", StringComparison.Ordinal);
    private static string NormalizeText(string? value)
    {
        var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(ch switch { 'ı' => 'i', 'İ' => 'i', _ => char.ToLowerInvariant(ch) });
        }
        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), "[^a-z0-9]+", " ").Trim();
    }
}

internal static class BankaMutabakatStringExtensions
{
    public static string DefaultIfBlank(this string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
