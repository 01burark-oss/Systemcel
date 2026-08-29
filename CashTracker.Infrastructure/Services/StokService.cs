using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class StokService : IStokService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly ISubeKurService? _subeKurService;

        public StokService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            ISubeKurService? subeKurService = null)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _subeKurService = subeKurService;
        }

        public async Task<decimal> GetCurrentStockAsync(int urunHizmetId, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            var branch = _subeKurService is null ? null : (await _subeKurService.GetContextAsync(ct)).AktifSube;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var amounts = await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.UrunHizmetId == urunHizmetId &&
                    (branch == null || x.SubeId == branch.Id || (branch.Varsayilan && x.SubeId == null)))
                .Select(x => x.Miktar)
                .ToListAsync(ct);

            return amounts.Sum();
        }

        public async Task<List<StokHareket>> GetRecentMovementsAsync(int limit = 20, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            var branch = _subeKurService is null ? null : (await _subeKurService.GetContextAsync(ct)).AktifSube;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId &&
                    (branch == null || x.SubeId == branch.Id || (branch.Varsayilan && x.SubeId == null)))
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.Id)
                .Take(Math.Clamp(limit, 1, 100))
                .ToListAsync(ct);
        }

        public async Task<StokHareketResult> CreateMovementAsync(
            StokHareketCreateRequest request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UrunHizmetId <= 0)
                throw new ArgumentException("Urun secilmelidir.", nameof(request));

            if (request.Miktar == 0)
                throw new ArgumentException("Stok miktari sifir olamaz.", nameof(request));

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var product = await db.UrunHizmetleri.SingleOrDefaultAsync(x =>
                x.Id == request.UrunHizmetId &&
                x.IsletmeId == activeIsletmeId &&
                x.Aktif,
                ct);

            if (product is null)
                throw new InvalidOperationException("Urun aktif isletmede bulunamadi.");

            if (request.Miktar < 0 && request.BirimMaliyet.HasValue)
                throw new ArgumentException("Stok çıkışında birim maliyet girilmez.", nameof(request));

            var activeBranch = _subeKurService is null ? null : (await _subeKurService.GetContextAsync(ct)).AktifSube;
            var warehouseId = await ResolveWarehouseIdAsync(db, activeIsletmeId, activeBranch, ct);
            var movement = new StokHareket
            {
                IsletmeId = activeIsletmeId,
                SubeId = activeBranch?.Id,
                UrunHizmetId = request.UrunHizmetId,
                DepoId = warehouseId,
                Tarih = request.Tarih ?? DateTime.Now,
                Miktar = request.Miktar,
                BirimMaliyet = request.BirimMaliyet ?? 0m,
                MaliyetParaBirimi = string.IsNullOrWhiteSpace(product.ParaBirimi) ? "TRY" : product.ParaBirimi.Trim().ToUpperInvariant(),
                MaliyetKurSnapshot = product.KurSnapshot <= 0m ? 1m : product.KurSnapshot,
                BirimMaliyetTry = decimal.Round((request.BirimMaliyet ?? 0m) * (product.KurSnapshot <= 0m ? 1m : product.KurSnapshot), 2, MidpointRounding.AwayFromZero),
                HareketTipi = request.Miktar > 0 ? "Giris" : "Cikis",
                Kaynak = string.IsNullOrWhiteSpace(request.Kaynak) ? "Manuel" : request.Kaynak.Trim(),
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama) ? null : request.Aciklama.Trim(),
                CreatedAt = DateTime.Now
            };

            db.StokHareketleri.Add(movement);
            await db.SaveChangesAsync(ct);

            var amounts = await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.UrunHizmetId == request.UrunHizmetId &&
                    (activeBranch == null || x.SubeId == activeBranch.Id || (activeBranch.Varsayilan && x.SubeId == null)))
                .Select(x => x.Miktar)
                .ToListAsync(ct);

            return new StokHareketResult
            {
                Hareket = movement,
                MevcutStok = amounts.Sum()
            };
        }

        private static async Task<int?> ResolveWarehouseIdAsync(
            CashTrackerDbContext db,
            int isletmeId,
            SubeDto? activeBranch,
            CancellationToken ct)
        {
            var warehouses = db.StokDepolari
                .AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId && x.Aktif);

            if (activeBranch is null)
            {
                return await warehouses
                    .Where(x => x.Varsayilan)
                    .OrderBy(x => x.Id)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(ct);
            }

            warehouses = activeBranch.Varsayilan
                ? warehouses.Where(x => x.SubeId == activeBranch.Id || x.SubeId == null)
                : warehouses.Where(x => x.SubeId == activeBranch.Id);

            var warehouseId = await warehouses
                .OrderByDescending(x => x.SubeId == activeBranch.Id)
                .ThenByDescending(x => x.Varsayilan)
                .ThenBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (!activeBranch.Varsayilan && warehouseId is null)
                throw new InvalidOperationException("Aktif şube için önce bir stok deposu oluşturulmalıdır.");

            return warehouseId;
        }
    }
}
