using System;
using System.Collections.Generic;
using System.Data;
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
    public sealed class UrunHizmetService : IUrunHizmetService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly IEntitlementGuard? _entitlementGuard;

        public UrunHizmetService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
            : this(dbFactory, isletmeService, null)
        {
        }

        public UrunHizmetService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            IEntitlementGuard? entitlementGuard)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _entitlementGuard = entitlementGuard;
        }

        public async Task<List<UrunHizmet>> GetAllAsync(CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.UrunHizmetleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderByDescending(x => x.Aktif)
                .ThenBy(x => x.Ad)
                .ToListAsync(ct);
        }

        public async Task<UrunHizmet?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.UrunHizmetleri
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);
        }

        public async Task<UrunHizmet?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            var normalizedBarcode = NormalizeOptional(barcode);
            if (string.IsNullOrWhiteSpace(normalizedBarcode))
                return null;

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.UrunHizmetleri
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IsletmeId == activeIsletmeId &&
                    x.Aktif &&
                    x.Barkod == normalizedBarcode,
                    ct);
        }

        public async Task<int> CreateAsync(UrunHizmetCreateRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            var barcode = NormalizeOptional(request.Barkod);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var transaction = _entitlementGuard is null
                ? null
                : await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (_entitlementGuard is not null)
            {
                var entitlement = await _entitlementGuard.GetAsync(activeIsletmeId, HesapTipleri.Isletme, ct);
                _entitlementGuard.EnsureWritable(entitlement);
                var currentCount = await db.UrunHizmetleri.CountAsync(x => x.IsletmeId == activeIsletmeId, ct);
                _entitlementGuard.EnsureLimit(entitlement, EntitlementLimits.ProductOrService, currentCount);
            }

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var duplicate = await db.UrunHizmetleri.AnyAsync(x =>
                    x.IsletmeId == activeIsletmeId &&
                    x.Barkod == barcode,
                    ct);

                if (duplicate)
                    throw new InvalidOperationException("Bu barkod aktif isletmede zaten kayitli.");
            }

            var row = new UrunHizmet
            {
                IsletmeId = activeIsletmeId,
                Tip = NormalizeTip(request.Tip),
                Ad = NormalizeRequired(request.Ad, "Urun adi bos olamaz."),
                Barkod = barcode,
                Birim = string.IsNullOrWhiteSpace(request.Birim) ? "Adet" : request.Birim.Trim(),
                KdvOrani = NormalizeNonNegative(request.KdvOrani),
                AlisFiyati = NormalizeNonNegative(request.AlisFiyati),
                SatisFiyati = NormalizeNonNegative(request.SatisFiyati),
                KritikStok = NormalizeNonNegative(request.KritikStok),
                Aktif = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.UrunHizmetleri.Add(row);
            await db.SaveChangesAsync(ct);
            if (transaction is not null)
                await transaction.CommitAsync(ct);
            return row.Id;
        }

        public async Task UpdateAsync(UrunHizmet urunHizmet, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(urunHizmet);

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            var barcode = NormalizeOptional(urunHizmet.Barkod);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.UrunHizmetleri
                .FirstOrDefaultAsync(x => x.Id == urunHizmet.Id && x.IsletmeId == activeIsletmeId, ct);

            if (existing == null)
                return;

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var duplicate = await db.UrunHizmetleri.AnyAsync(x =>
                    x.IsletmeId == activeIsletmeId &&
                    x.Id != urunHizmet.Id &&
                    x.Barkod == barcode,
                    ct);

                if (duplicate)
                    throw new InvalidOperationException("Bu barkod aktif isletmede zaten kayitli.");
            }

            existing.Tip = NormalizeTip(urunHizmet.Tip);
            existing.Ad = NormalizeRequired(urunHizmet.Ad, "Urun adi bos olamaz.");
            existing.Barkod = barcode;
            existing.Birim = string.IsNullOrWhiteSpace(urunHizmet.Birim) ? "Adet" : urunHizmet.Birim.Trim();
            existing.KdvOrani = NormalizeNonNegative(urunHizmet.KdvOrani);
            existing.AlisFiyati = NormalizeNonNegative(urunHizmet.AlisFiyati);
            existing.SatisFiyati = NormalizeNonNegative(urunHizmet.SatisFiyati);
            existing.KritikStok = NormalizeNonNegative(urunHizmet.KritikStok);
            existing.Aktif = urunHizmet.Aktif;
            existing.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.UrunHizmetleri
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);

            if (row == null)
                return;

            var movements = db.StokHareketleri.Where(x => x.IsletmeId == activeIsletmeId && x.UrunHizmetId == id);
            db.StokHareketleri.RemoveRange(movements);
            db.UrunHizmetleri.Remove(row);
            await db.SaveChangesAsync(ct);
        }

        private static string NormalizeTip(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "hizmet" => "Hizmet",
                "service" => "Hizmet",
                _ => "Urun"
            };
        }

        private static decimal NormalizeNonNegative(decimal value)
        {
            return value < 0 ? 0 : value;
        }

        private static string NormalizeOptional(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string NormalizeRequired(string? value, string message)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException(message);
            return normalized;
        }
    }
}
