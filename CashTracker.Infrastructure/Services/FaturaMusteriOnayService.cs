using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class FaturaMusteriOnayService : IFaturaMusteriOnayService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly IMusteriSmsSender _smsSender;
    private readonly MusteriSmsSettings _settings;

    public FaturaMusteriOnayService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        IMusteriSmsSender smsSender,
        MusteriSmsSettings settings)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _smsSender = smsSender;
        _settings = settings;
    }

    public async Task<FaturaMusteriOnayGonderimSonucu> SendAsync(
        int faturaId,
        CancellationToken ct = default)
    {
        if (faturaId <= 0)
            throw new ArgumentException("Geçerli bir fatura seçin.", nameof(faturaId));
        if (!_smsSender.IsConfigured)
            throw new InvalidOperationException("Müşteri SMS gönderimi yapılandırılmamış. Netgsm kullanıcı adı, parola ve gönderici başlığını ekleyin.");

        var active = await _isletmeService.GetActiveAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fatura = await db.Faturalar.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == faturaId && x.IsletmeId == active.Id, ct)
            ?? throw new InvalidOperationException("Fatura bulunamadı.");
        var cari = await db.CariKartlari.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == fatura.CariKartId && x.IsletmeId == active.Id, ct)
            ?? throw new InvalidOperationException("Faturanın cari kaydı bulunamadı.");

        ValidateInvoice(fatura);
        var phone = NormalizeTurkishMobile(cari.Telefon);
        if (phone is null)
            throw new InvalidOperationException("Müşteri teyidi için cari karta 05XXXXXXXXX biçiminde geçerli bir cep telefonu ekleyin.");

        var now = DateTime.Now;
        var cooldownFrom = now.AddMinutes(-_settings.EffectiveResendCooldownMinutes);
        var latest = await db.FaturaMusteriOnaylari
            .Where(x => x.IsletmeId == active.Id && x.FaturaId == fatura.Id)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (latest is not null &&
            latest.GonderildiAt >= cooldownFrom &&
            latest.Durum == FaturaMusteriOnayDurumlari.Bekliyor)
        {
            throw new InvalidOperationException($"Bu müşteriye son {_settings.EffectiveResendCooldownMinutes} dakika içinde teyit bağlantısı gönderildi.");
        }

        var pending = await db.FaturaMusteriOnaylari
            .Where(x => x.IsletmeId == active.Id &&
                        x.FaturaId == fatura.Id &&
                        x.Durum == FaturaMusteriOnayDurumlari.Bekliyor)
            .ToListAsync(ct);
        foreach (var row in pending)
        {
            row.Durum = FaturaMusteriOnayDurumlari.Iptal;
            row.UpdatedAt = now;
        }

        var token = CreateToken();
        var expiresAt = now.AddHours(_settings.EffectiveLinkExpiryHours);
        var approval = new FaturaMusteriOnayi
        {
            IsletmeId = active.Id,
            FaturaId = fatura.Id,
            CariKartId = cari.Id,
            TokenHash = Sha256(token),
            Durum = FaturaMusteriOnayDurumlari.Bekliyor,
            IsletmeAdi = Display(active.Ad, "İşletme"),
            CariUnvan = Display(cari.Unvan, $"Cari #{cari.Id}"),
            CariVergiNoMaskeli = MaskTaxId(cari.VergiNoTc),
            CariAdres = Display(cari.Adres, "Adres kaydı yok"),
            AliciTelefonMaskeli = MaskPhone(phone),
            FaturaNo = InvoiceNo(fatura),
            FaturaTarihi = fatura.Tarih,
            FaturaToplami = fatura.GenelToplam,
            ParaBirimi = "TRY",
            Saglayici = "Netgsm",
            SonGecerlilikAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.FaturaMusteriOnaylari.Add(approval);
        await db.SaveChangesAsync(ct);

        var url = $"{_settings.EffectivePublicBaseUrl}/fatura-onayi/{token}";
        var smsText = $"{approval.IsletmeAdi}: {approval.FaturaNo} numaralı fatura taslağındaki bilgilerinizi kontrol edin: {url} Bu teyit resmi e-belge onayı değildir.";
        var sent = await _smsSender.SendAsync(phone, smsText, ct);

        approval.Saglayici = sent.Saglayici;
        approval.SaglayiciIslemId = sent.IslemId;
        approval.Hata = sent.Basarili ? string.Empty : Limit(sent.Hata, 500);
        approval.GonderildiAt = sent.Basarili ? DateTime.Now : null;
        approval.Durum = sent.Basarili
            ? FaturaMusteriOnayDurumlari.Bekliyor
            : FaturaMusteriOnayDurumlari.Gonderilemedi;
        approval.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);

        return new FaturaMusteriOnayGonderimSonucu(
            approval.Id,
            approval.FaturaId,
            approval.Durum,
            approval.AliciTelefonMaskeli,
            url,
            approval.SonGecerlilikAt,
            approval.GonderildiAt,
            sent.Basarili
                ? $"Teyit bağlantısı {approval.AliciTelefonMaskeli} numarasına gönderildi."
                : $"Teyit bağlantısı oluşturuldu ancak SMS gönderilemedi: {approval.Hata}");
    }

    public async Task<FaturaMusteriOnayDurumu> GetLatestAsync(
        int faturaId,
        CancellationToken ct = default)
    {
        var active = await _isletmeService.GetActiveAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var invoiceExists = await db.Faturalar.AsNoTracking()
            .AnyAsync(x => x.Id == faturaId && x.IsletmeId == active.Id, ct);
        if (!invoiceExists)
            throw new InvalidOperationException("Fatura bulunamadı.");

        var row = await db.FaturaMusteriOnaylari.AsNoTracking()
            .Where(x => x.IsletmeId == active.Id && x.FaturaId == faturaId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return row is null
            ? new FaturaMusteriOnayDurumu(null, faturaId, "Yok", string.Empty, null, null, null, string.Empty)
            : new FaturaMusteriOnayDurumu(
                row.Id,
                row.FaturaId,
                EffectiveStatus(row, DateTime.Now),
                row.AliciTelefonMaskeli,
                row.GonderildiAt,
                row.SonGecerlilikAt,
                row.YanitAt,
                row.YanitNotu);
    }

    public async Task<PublicFaturaMusteriOnayDetayi?> GetPublicAsync(
        string token,
        CancellationToken ct = default)
    {
        var tokenHash = TokenHashOrNull(token);
        if (tokenHash is null)
            return null;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.FaturaMusteriOnaylari
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        if (row is null)
            return null;

        await RefreshStatusAsync(db, row, ct);
        return ToPublic(row);
    }

    public async Task<PublicFaturaMusteriOnayDetayi?> RespondAsync(
        string token,
        PublicFaturaMusteriOnayYaniti response,
        string clientIp,
        string userAgent,
        CancellationToken ct = default)
    {
        var tokenHash = TokenHashOrNull(token);
        if (tokenHash is null)
            return null;
        var note = (response.Aciklama ?? string.Empty).Trim();
        if (note.Length > 500)
            throw new ArgumentException("Açıklama 500 karakteri geçemez.", nameof(response));
        if (!response.BilgilerDogru && note.Length < 3)
            throw new ArgumentException("Düzeltilecek bilgiyi kısaca yazın.", nameof(response));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.FaturaMusteriOnaylari
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        if (row is null)
            return null;

        await RefreshStatusAsync(db, row, ct);
        if (row.Durum != FaturaMusteriOnayDurumlari.Bekliyor)
            return ToPublic(row);

        var now = DateTime.Now;
        row.Durum = response.BilgilerDogru
            ? FaturaMusteriOnayDurumlari.Onaylandi
            : FaturaMusteriOnayDurumlari.DuzeltmeIstendi;
        row.YanitNotu = note;
        row.YanitAt = now;
        row.IstemciIpHash = Sha256(clientIp ?? string.Empty);
        row.UserAgentHash = Sha256(userAgent ?? string.Empty);
        row.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return ToPublic(row);
    }

    private static void ValidateInvoice(Fatura invoice)
    {
        if (!string.Equals(invoice.FaturaTipi, "Satis", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Müşteri teyidi yalnızca satış faturası taslakları için gönderilebilir.");
        if (invoice.Durum is not (FaturaDurum.YerelTaslak or FaturaDurum.PortalTaslak))
            throw new InvalidOperationException("Müşteri teyidi yalnızca kesilmemiş fatura taslağı için gönderilebilir.");
        if (invoice.GenelToplam <= 0)
            throw new InvalidOperationException("Müşteri teyidi göndermek için fatura toplamı sıfırdan büyük olmalıdır.");
    }

    private static async Task RefreshStatusAsync(
        CashTrackerDbContext db,
        FaturaMusteriOnayi row,
        CancellationToken ct)
    {
        if (row.Durum != FaturaMusteriOnayDurumlari.Bekliyor)
            return;

        var now = DateTime.Now;
        var invoice = await db.Faturalar.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == row.FaturaId && x.IsletmeId == row.IsletmeId, ct);
        if (invoice is null || invoice.Durum is FaturaDurum.Kesildi or FaturaDurum.Iptal)
            row.Durum = FaturaMusteriOnayDurumlari.Iptal;
        else if (invoice.UpdatedAt > row.CreatedAt.AddSeconds(1))
            row.Durum = FaturaMusteriOnayDurumlari.Iptal;
        else if (row.SonGecerlilikAt <= now)
            row.Durum = FaturaMusteriOnayDurumlari.SuresiDoldu;
        else
            return;

        row.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
    }

    private static PublicFaturaMusteriOnayDetayi ToPublic(FaturaMusteriOnayi row)
    {
        var description = row.Durum switch
        {
            FaturaMusteriOnayDurumlari.Bekliyor => "Bilgilerinizi kontrol edip yanıtlayın. Bu işlem resmi e-belge onayı değildir.",
            FaturaMusteriOnayDurumlari.Onaylandi => "Bilgilerinizin doğru olduğunu bildirdiniz. Resmi fatura, işletmenin GİB onayından sonra kesilir.",
            FaturaMusteriOnayDurumlari.DuzeltmeIstendi => "Düzeltme talebiniz işletmeye iletildi.",
            FaturaMusteriOnayDurumlari.SuresiDoldu => "Bu bağlantının süresi doldu. İşletmeden yeni bağlantı isteyin.",
            FaturaMusteriOnayDurumlari.Iptal => "Fatura taslağı değişti veya bağlantı iptal edildi. İşletmeden yeni bağlantı isteyin.",
            _ => "Bu teyit bağlantısı kullanılamıyor."
        };
        return new PublicFaturaMusteriOnayDetayi(
            row.Durum,
            row.IsletmeAdi,
            row.CariUnvan,
            row.CariVergiNoMaskeli,
            row.CariAdres,
            row.FaturaNo,
            row.FaturaTarihi,
            row.FaturaToplami,
            row.ParaBirimi,
            row.SonGecerlilikAt,
            row.YanitAt,
            description);
    }

    private static string EffectiveStatus(FaturaMusteriOnayi row, DateTime now) =>
        row.Durum == FaturaMusteriOnayDurumlari.Bekliyor && row.SonGecerlilikAt <= now
            ? FaturaMusteriOnayDurumlari.SuresiDoldu
            : row.Durum;

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string? TokenHashOrNull(string? token)
    {
        var value = token?.Trim() ?? string.Empty;
        return value.Length is < 40 or > 100 ? null : Sha256(value);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? NormalizeTurkishMobile(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0090", StringComparison.Ordinal))
            digits = digits[4..];
        else if (digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12)
            digits = digits[2..];
        else if (digits.StartsWith('0') && digits.Length == 11)
            digits = digits[1..];
        return digits.Length == 10 && digits[0] == '5' ? digits : null;
    }

    private static string MaskPhone(string phone) => $"0{phone[..3]} *** ** {phone[^2..]}";

    private static string MaskTaxId(string? value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        if (normalized.Length < 5)
            return "Kayıt yok";
        return normalized[..2] + new string('*', normalized.Length - 4) + normalized[^2..];
    }

    private static string InvoiceNo(Fatura fatura)
    {
        var number = string.IsNullOrWhiteSpace(fatura.PortalBelgeNo)
            ? fatura.YerelFaturaNo
            : fatura.PortalBelgeNo;
        return Display(number, $"Taslak #{fatura.Id}");
    }

    private static string Display(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Limit(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
