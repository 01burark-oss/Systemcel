using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class DeveloperApiReadService
{
    public const int MaxPage = 10_000;
    public const int MaxPageSize = 100;
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public DeveloperApiReadService(IDbContextFactory<CashTrackerDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<DeveloperApiBusinessSummary?> GetSummaryAsync(int businessId, DateTime? now = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var business = await db.Isletmeler.AsNoTracking().SingleOrDefaultAsync(x => x.Id == businessId && x.TenantTipi == "Isletme", ct);
        if (business is null)
            return null;
        var income = await db.Kasalar.Where(x => x.IsletmeId == businessId && x.Tip == "Gelir").SumAsync(x => (decimal?)x.Tutar, ct) ?? 0;
        var expense = await db.Kasalar.Where(x => x.IsletmeId == businessId && x.Tip == "Gider").SumAsync(x => (decimal?)x.Tutar, ct) ?? 0;
        return new DeveloperApiBusinessSummary(
            business.Id, business.Ad, "TRY", income, expense, income - expense,
            await db.CariKartlari.CountAsync(x => x.IsletmeId == businessId, ct),
            await db.UrunHizmetleri.CountAsync(x => x.IsletmeId == businessId, ct),
            await db.Faturalar.CountAsync(x => x.IsletmeId == businessId, ct),
            await db.BankaHareketleri.CountAsync(x => x.IsletmeId == businessId && x.Durum == "Acik", ct),
            now ?? DateTime.Now);
    }

    public async Task<DeveloperApiPage<DeveloperApiAccount>> GetAccountsAsync(int businessId, int page, int pageSize, CancellationToken ct = default)
    {
        ValidatePage(page, pageSize);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.CariKartlari.AsNoTracking().Where(x => x.IsletmeId == businessId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DeveloperApiAccount(x.Id, x.Tip, x.Unvan, x.Telefon, x.Eposta, x.Aktif, x.UpdatedAt)).ToListAsync(ct);
        return new DeveloperApiPage<DeveloperApiAccount>(rows, page, pageSize, total);
    }

    public async Task<DeveloperApiPage<DeveloperApiProduct>> GetProductsAsync(int businessId, int page, int pageSize, CancellationToken ct = default)
    {
        ValidatePage(page, pageSize);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.UrunHizmetleri.AsNoTracking().Where(x => x.IsletmeId == businessId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DeveloperApiProduct(x.Id, x.Tip, x.Ad, x.Barkod, x.Birim, x.KdvOrani, x.AlisFiyati, x.SatisFiyati, x.Aktif, x.UpdatedAt)).ToListAsync(ct);
        return new DeveloperApiPage<DeveloperApiProduct>(rows, page, pageSize, total);
    }

    public async Task<DeveloperApiPage<DeveloperApiInvoice>> GetInvoicesAsync(int businessId, int page, int pageSize, CancellationToken ct = default)
    {
        ValidatePage(page, pageSize);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.Faturalar.AsNoTracking().Where(x => x.IsletmeId == businessId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DeveloperApiInvoice(
                x.Id, x.CariKartId, x.Tarih, x.VadeTarihi, x.FaturaTipi, x.Durum,
                x.PortalBelgeNo != "" ? x.PortalBelgeNo : x.YerelFaturaNo,
                x.AraToplam, x.KdvToplam, x.GenelToplam, x.OdenenTutar, x.OdemeYontemi, x.Aciklama, x.UpdatedAt)).ToListAsync(ct);
        return new DeveloperApiPage<DeveloperApiInvoice>(rows, page, pageSize, total);
    }

    public async Task<DeveloperApiPage<DeveloperApiBankTransaction>> GetBankTransactionsAsync(int businessId, int page, int pageSize, CancellationToken ct = default)
    {
        ValidatePage(page, pageSize);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.BankaHareketleri.AsNoTracking().Where(x => x.IsletmeId == businessId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DeveloperApiBankTransaction(x.Id, x.Tarih, x.Aciklama, x.Tutar, x.ParaBirimi, x.Durum, x.EslesenKaynakTuru, x.EslesenKaynakId, x.UpdatedAt)).ToListAsync(ct);
        return new DeveloperApiPage<DeveloperApiBankTransaction>(rows, page, pageSize, total);
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page is < 1 or > MaxPage)
            throw new ArgumentOutOfRangeException(nameof(page), $"Sayfa 1 ile {MaxPage} arasında olmalıdır.");
        if (pageSize is < 1 or > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalıdır.");
    }
}
