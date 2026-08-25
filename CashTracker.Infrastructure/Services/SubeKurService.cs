using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class SubeKurService : ISubeKurService
{
    private const decimal MaximumRate = 1_000_000m;
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly IEntitlementGuard? _entitlementGuard;
    private readonly ICurrentUserContext? _currentUserContext;

    public SubeKurService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        IEntitlementGuard? entitlementGuard = null,
        ICurrentUserContext? currentUserContext = null)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _entitlementGuard = entitlementGuard;
        _currentUserContext = currentUserContext;
    }

    public async Task<SubeKurDurumuDto> GetContextAsync(CancellationToken ct = default)
    {
        var businessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var center = await EnsureCenterAsync(db, businessId, ct);
        var active = await GetActiveBranchAsync(db, businessId, center, ct);
        var capabilities = await GetCapabilitiesAsync(businessId, ct);
        var branches = await db.Subeler.AsNoTracking()
            .Where(x => x.IsletmeId == businessId)
            .OrderByDescending(x => x.Varsayilan).ThenBy(x => x.Ad)
            .ToListAsync(ct);
        var latestRates = await db.DovizKurlari.AsNoTracking()
            .Where(x => x.IsletmeId == businessId && x.GecerliAt <= DateTime.Now)
            .OrderByDescending(x => x.GecerliAt).ThenByDescending(x => x.Id)
            .ToListAsync(ct);
        var rates = latestRates.GroupBy(x => x.ParaBirimi).Select(x => x.First()).Select(MapRate).ToList();
        rates.Insert(0, new DovizKuruDto { ParaBirimi = "TRY", Kur = 1m, GecerliAt = DateTime.UnixEpoch });

        return new SubeKurDurumuDto
        {
            AktifSube = MapBranch(active),
            Subeler = branches.Select(MapBranch).ToList(),
            Kurlar = rates,
            CokluSubeAktif = capabilities.MultipleBranches,
            CokluParaBirimiAktif = capabilities.MultipleCurrencies
        };
    }

    public async Task<SubeFinansOzetiDto> GetFinancialSummaryAsync(int? branchId = null, CancellationToken ct = default)
    {
        var businessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var center = await EnsureCenterAsync(db, businessId, ct);
        Sube? selected = null;
        if (branchId is not null)
        {
            selected = await db.Subeler.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == branchId && x.IsletmeId == businessId, ct)
                ?? throw new InvalidOperationException("Şube bu işletmede bulunamadı.");
        }

        var query = db.Kasalar.AsNoTracking().Where(x => x.IsletmeId == businessId);
        if (selected is not null)
        {
            query = selected.Varsayilan
                ? query.Where(x => x.SubeId == selected.Id || x.SubeId == null)
                : query.Where(x => x.SubeId == selected.Id);
        }

        var rows = await query.Select(x => new
        {
            x.Tip,
            ParaBirimi = string.IsNullOrEmpty(x.ParaBirimi) ? "TRY" : x.ParaBirimi,
            Orijinal = x.OrijinalTutar == 0m ? x.Tutar : x.OrijinalTutar,
            Try = x.TryKarsiligi == 0m ? x.Tutar : x.TryKarsiligi
        }).ToListAsync(ct);
        var currencyRows = rows
            .GroupBy(x => x.ParaBirimi)
            .OrderBy(x => x.Key)
            .Select(group => new ParaBirimiOzetiDto
            {
                ParaBirimi = group.Key,
                GelirOrijinal = group.Where(x => x.Tip == "Gelir").Sum(x => x.Orijinal),
                GiderOrijinal = group.Where(x => x.Tip == "Gider").Sum(x => x.Orijinal),
                GelirTry = group.Where(x => x.Tip == "Gelir").Sum(x => x.Try),
                GiderTry = group.Where(x => x.Tip == "Gider").Sum(x => x.Try)
            }).ToList();
        var income = currencyRows.Sum(x => x.GelirTry);
        var expense = currencyRows.Sum(x => x.GiderTry);
        return new SubeFinansOzetiDto
        {
            SubeId = selected?.Id,
            Konsolide = selected is null,
            GelirTry = income,
            GiderTry = expense,
            NetTry = income - expense,
            ParaBirimleri = currencyRows
        };
    }

    public async Task<SubeOlusturResult> CreateBranchAsync(SubeOlusturRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var businessId = await _isletmeService.GetActiveIdAsync();
        await EnsureFeatureAsync(businessId, EntitlementFeatures.MultipleBranches, ct);
        var key = Required(idempotencyKey, 120, "Idempotency-Key");
        var name = Required(request.Ad, 120, "Şube adı");
        var code = NormalizeCode(request.Kod);
        var hash = Hash($"branch|{name}|{code}");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await EnsureCenterAsync(db, businessId, ct);
        var existing = await db.Subeler.AsNoTracking().SingleOrDefaultAsync(x => x.IsletmeId == businessId && x.OlusturmaAnahtari == key, ct);
        if (existing is not null)
        {
            EnsureSame(existing.IcerikOzeti, hash);
            return new SubeOlusturResult { Sube = MapBranch(existing), Tekrarlandi = true };
        }
        if (await db.Subeler.AnyAsync(x => x.IsletmeId == businessId && x.Kod == code, ct))
            throw new InvalidOperationException("Bu şube kodu zaten kullanılıyor.");
        var row = new Sube
        {
            IsletmeId = businessId,
            Ad = name,
            Kod = code,
            OlusturmaAnahtari = key,
            IcerikOzeti = hash,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        db.Subeler.Add(row);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var replay = await db.Subeler.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IsletmeId == businessId && x.OlusturmaAnahtari == key, ct);
            if (replay is null) throw new InvalidOperationException("Bu şube kodu başka bir işlemde oluşturuldu.");
            EnsureSame(replay.IcerikOzeti, hash);
            return new SubeOlusturResult { Sube = MapBranch(replay), Tekrarlandi = true };
        }
        return new SubeOlusturResult { Sube = MapBranch(row) };
    }

    public async Task SetActiveBranchAsync(int branchId, CancellationToken ct = default)
    {
        var businessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var branch = await db.Subeler.SingleOrDefaultAsync(x => x.Id == branchId && x.IsletmeId == businessId && x.Aktif, ct)
            ?? throw new InvalidOperationException("Aktif şube bu işletmede bulunamadı.");
        if (!branch.Varsayilan)
            await EnsureFeatureAsync(businessId, EntitlementFeatures.MultipleBranches, ct);
        var key = BuildActiveBranchKey(businessId);
        var setting = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == key, ct);
        if (setting is null)
        {
            setting = new AppSetting { Key = key, Value = branch.Id.ToString(CultureInfo.InvariantCulture) };
            db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = branch.Id.ToString(CultureInfo.InvariantCulture);
            setting.UpdatedAt = DateTime.Now;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<KurKaydetResult> SaveRateAsync(DovizKuruKaydetRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var currency = NormalizeCurrency(request.ParaBirimi);
        if (currency == "TRY") throw new ArgumentException("TRY kuru her zaman 1'dir ve değiştirilemez.", nameof(request));
        if (request.Kur <= 0 || request.Kur > MaximumRate)
            throw new ArgumentException("Kur sıfırdan büyük ve 1.000.000 değerini aşmayacak şekilde girilmelidir.", nameof(request));
        var businessId = await _isletmeService.GetActiveIdAsync();
        await EnsureFeatureAsync(businessId, EntitlementFeatures.MultipleCurrencies, ct);
        var key = Required(idempotencyKey, 120, "Idempotency-Key");
        var effectiveAt = request.GecerliAt ?? DateTime.Now;
        if (effectiveAt > DateTime.Now.AddDays(1)) throw new ArgumentException("Kur geçerlilik tarihi gelecekte olamaz.", nameof(request));
        var hash = Hash($"rate|{currency}|{request.Kur.ToString("G29", CultureInfo.InvariantCulture)}|{effectiveAt:O}");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.DovizKurlari.AsNoTracking().SingleOrDefaultAsync(x => x.IsletmeId == businessId && x.OlusturmaAnahtari == key, ct);
        if (existing is not null)
        {
            EnsureSame(existing.IcerikOzeti, hash);
            return new KurKaydetResult { Kur = MapRate(existing), Tekrarlandi = true };
        }
        var row = new DovizKuru
        {
            IsletmeId = businessId,
            ParaBirimi = currency,
            Kur = decimal.Round(request.Kur, 6, MidpointRounding.AwayFromZero),
            GecerliAt = effectiveAt,
            OlusturmaAnahtari = key,
            IcerikOzeti = hash,
            CreatedAt = DateTime.Now
        };
        db.DovizKurlari.Add(row);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var replay = await db.DovizKurlari.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IsletmeId == businessId && x.OlusturmaAnahtari == key, ct);
            if (replay is null) throw;
            EnsureSame(replay.IcerikOzeti, hash);
            return new KurKaydetResult { Kur = MapRate(replay), Tekrarlandi = true };
        }
        return new KurKaydetResult { Kur = MapRate(row) };
    }

    public async Task<IslemKurSnapshot> ResolveSnapshotAsync(string? currency, decimal originalAmount, CancellationToken ct = default)
    {
        var businessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var center = await EnsureCenterAsync(db, businessId, ct);
        var branch = await GetActiveBranchAsync(db, businessId, center, ct);
        var normalizedCurrency = NormalizeCurrency(currency);
        var rate = 1m;
        if (normalizedCurrency != "TRY")
        {
            await EnsureFeatureAsync(businessId, EntitlementFeatures.MultipleCurrencies, ct);
            rate = await db.DovizKurlari.AsNoTracking()
                .Where(x => x.IsletmeId == businessId && x.ParaBirimi == normalizedCurrency && x.GecerliAt <= DateTime.Now)
                .OrderByDescending(x => x.GecerliAt).ThenByDescending(x => x.Id)
                .Select(x => x.Kur)
                .FirstOrDefaultAsync(ct);
            if (rate <= 0) throw new InvalidOperationException($"{normalizedCurrency} için güncel manuel kur girilmelidir.");
        }
        if (!branch.Varsayilan)
            await EnsureFeatureAsync(businessId, EntitlementFeatures.MultipleBranches, ct);
        return new IslemKurSnapshot
        {
            SubeId = branch.Id,
            ParaBirimi = normalizedCurrency,
            Kur = rate,
            OrijinalTutar = originalAmount,
            TryKarsiligi = decimal.Round(originalAmount * rate, 2, MidpointRounding.AwayFromZero)
        };
    }

    private async Task<Sube> EnsureCenterAsync(CashTrackerDbContext db, int businessId, CancellationToken ct)
    {
        var center = await db.Subeler.FirstOrDefaultAsync(x => x.IsletmeId == businessId && x.Varsayilan, ct);
        if (center is not null) return center;
        center = new Sube
        {
            IsletmeId = businessId,
            Ad = "Merkez",
            Kod = "MERKEZ",
            Varsayilan = true,
            OlusturmaAnahtari = $"legacy-center-{businessId}",
            IcerikOzeti = Hash($"legacy-center|{businessId}")
        };
        db.Subeler.Add(center);
        try
        {
            await db.SaveChangesAsync(ct);
            return center;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.Subeler.FirstAsync(x => x.IsletmeId == businessId && x.Varsayilan, ct);
        }
    }

    private async Task<Sube> GetActiveBranchAsync(CashTrackerDbContext db, int businessId, Sube center, CancellationToken ct)
    {
        var raw = await db.AppSettings.AsNoTracking().Where(x => x.Key == BuildActiveBranchKey(businessId)).Select(x => x.Value).FirstOrDefaultAsync(ct);
        if (!int.TryParse(raw, out var id)) return center;
        return await db.Subeler.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsletmeId == businessId && x.Aktif, ct) ?? center;
    }

    private async Task<(bool MultipleBranches, bool MultipleCurrencies)> GetCapabilitiesAsync(int businessId, CancellationToken ct)
    {
        if (_entitlementGuard is null) return (true, true);
        var entitlement = await _entitlementGuard.GetAsync(businessId, HesapTipleri.Isletme, ct);
        return (entitlement.CokluSubeAktif && !entitlement.SaltOkunur, entitlement.CokluParaBirimiAktif && !entitlement.SaltOkunur);
    }

    private async Task EnsureFeatureAsync(int businessId, string feature, CancellationToken ct)
    {
        if (_entitlementGuard is null) return;
        var entitlement = await _entitlementGuard.GetAsync(businessId, HesapTipleri.Isletme, ct);
        _entitlementGuard.EnsureFeature(entitlement, feature);
    }

    private string BuildActiveBranchKey(int businessId)
    {
        var providerId = _currentUserContext?.GetCurrentUser()?.ProviderUserId;
        var subject = string.IsNullOrWhiteSpace(providerId) ? "legacy" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(providerId)))[..16];
        return $"AktifSube:{businessId}:{subject}";
    }

    private static string NormalizeCurrency(string? value)
    {
        var currency = string.IsNullOrWhiteSpace(value) ? "TRY" : value.Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(x => x is < 'A' or > 'Z')) throw new ArgumentException("Para birimi üç harfli ISO kodu olmalıdır.");
        return currency;
    }

    private static string NormalizeCode(string? value)
    {
        var code = Required(value, 24, "Şube kodu").ToUpperInvariant();
        if (code.Any(x => !char.IsLetterOrDigit(x) && x != '-')) throw new ArgumentException("Şube kodunda yalnız harf, rakam ve tire kullanılabilir.");
        return code;
    }

    private static string Required(string? value, int max, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException($"{name} zorunludur.");
        if (normalized.Length > max) throw new ArgumentException($"{name} en fazla {max} karakter olabilir.");
        return normalized;
    }

    private static void EnsureSame(string stored, string expected)
    {
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(stored), Convert.FromHexString(expected)))
            throw new InvalidOperationException("Idempotency-Key farklı bir istek için daha önce kullanıldı.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static SubeDto MapBranch(Sube x) => new() { Id = x.Id, Ad = x.Ad, Kod = x.Kod, Varsayilan = x.Varsayilan, Aktif = x.Aktif };
    private static DovizKuruDto MapRate(DovizKuru x) => new() { ParaBirimi = x.ParaBirimi, Kur = x.Kur, GecerliAt = x.GecerliAt };
}
