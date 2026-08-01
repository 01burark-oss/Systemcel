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
    public sealed class CariService : ICariService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly IEntitlementGuard? _entitlementGuard;

        public CariService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
            : this(dbFactory, isletmeService, null)
        {
        }

        public CariService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            IEntitlementGuard? entitlementGuard)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _entitlementGuard = entitlementGuard;
        }

        public async Task<List<CariKart>> GetAllAsync(CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CariKartlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderByDescending(x => x.Aktif)
                .ThenBy(x => x.Unvan)
                .ToListAsync(ct);
        }

        public async Task<CariKart?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CariKartlari
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);
        }

        public async Task<int> CreateAsync(CariKart cariKart, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(cariKart);

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var transaction = _entitlementGuard is null
                ? null
                : await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (_entitlementGuard is not null)
            {
                var entitlement = await _entitlementGuard.GetAsync(activeIsletmeId, HesapTipleri.Isletme, ct);
                _entitlementGuard.EnsureWritable(entitlement);
                var currentCount = await db.CariKartlari.CountAsync(x => x.IsletmeId == activeIsletmeId, ct);
                _entitlementGuard.EnsureLimit(entitlement, EntitlementLimits.CurrentAccount, currentCount);
            }

            cariKart.IsletmeId = activeIsletmeId;
            Normalize(cariKart);
            cariKart.CreatedAt = DateTime.Now;
            cariKart.UpdatedAt = DateTime.Now;

            db.CariKartlari.Add(cariKart);
            await db.SaveChangesAsync(ct);
            if (transaction is not null)
                await transaction.CommitAsync(ct);
            return cariKart.Id;
        }

        public async Task UpdateAsync(CariKart cariKart, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(cariKart);

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.CariKartlari
                .FirstOrDefaultAsync(x => x.Id == cariKart.Id && x.IsletmeId == activeIsletmeId, ct);

            if (existing == null)
                return;

            existing.Tip = cariKart.Tip;
            existing.Unvan = cariKart.Unvan;
            existing.Telefon = cariKart.Telefon;
            existing.Eposta = cariKart.Eposta;
            existing.Adres = cariKart.Adres;
            existing.VergiNoTc = cariKart.VergiNoTc;
            existing.VergiDairesi = cariKart.VergiDairesi;
            existing.Aktif = cariKart.Aktif;
            Normalize(existing);
            existing.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.CariKartlari
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);

            if (row == null)
                return;

            var movements = db.CariHareketleri.Where(x => x.IsletmeId == activeIsletmeId && x.CariKartId == id);
            db.CariHareketleri.RemoveRange(movements);
            db.CariKartlari.Remove(row);
            await db.SaveChangesAsync(ct);
        }

        public async Task<int> CreateHareketAsync(CariHareket hareket, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(hareket);

            if (hareket.CariKartId <= 0)
                throw new ArgumentException("Cari kart secilmelidir.", nameof(hareket));

            if (hareket.Tutar <= 0)
                throw new ArgumentException("Cari hareket tutari sifirdan buyuk olmalidir.", nameof(hareket));

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var exists = await db.CariKartlari
                .AnyAsync(x => x.Id == hareket.CariKartId && x.IsletmeId == activeIsletmeId, ct);

            if (!exists)
                throw new InvalidOperationException("Cari kart aktif isletmede bulunamadi.");

            hareket.IsletmeId = activeIsletmeId;
            hareket.HareketTipi = NormalizeMovementType(hareket.HareketTipi);
            hareket.Kaynak = string.IsNullOrWhiteSpace(hareket.Kaynak) ? "Manuel" : hareket.Kaynak.Trim();
            hareket.Aciklama = string.IsNullOrWhiteSpace(hareket.Aciklama) ? null : hareket.Aciklama.Trim();
            hareket.CreatedAt = DateTime.Now;
            if (hareket.Tarih == default)
                hareket.Tarih = DateTime.Now;

            db.CariHareketleri.Add(hareket);
            await db.SaveChangesAsync(ct);
            return hareket.Id;
        }

        public async Task<List<CariHareket>> GetHareketlerAsync(int cariKartId, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CariHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.CariKartId == cariKartId)
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<decimal> GetBakiyeAsync(int cariKartId, CancellationToken ct = default)
        {
            var movements = await GetHareketlerAsync(cariKartId, ct);
            return movements.Sum(GetSignedAmount);
        }

        private static void Normalize(CariKart cariKart)
        {
            cariKart.Tip = NormalizeCariTip(cariKart.Tip);
            cariKart.Unvan = NormalizeRequired(cariKart.Unvan, "Cari unvan bos olamaz.");
            cariKart.Telefon = cariKart.Telefon?.Trim() ?? string.Empty;
            cariKart.Eposta = cariKart.Eposta?.Trim() ?? string.Empty;
            cariKart.Adres = cariKart.Adres?.Trim() ?? string.Empty;
            cariKart.VergiNoTc = cariKart.VergiNoTc?.Trim() ?? string.Empty;
            cariKart.VergiDairesi = cariKart.VergiDairesi?.Trim() ?? string.Empty;
        }

        private static string NormalizeCariTip(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "tedarikci" => "Tedarikci",
                "supplier" => "Tedarikci",
                "herikisi" => "HerIkisi",
                "her ikisi" => "HerIkisi",
                "musteri" => "Musteri",
                "müşteri" => "Musteri",
                "customer" => "Musteri",
                _ => "Musteri"
            };
        }

        private static string NormalizeMovementType(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "alacak" => "Alacak",
                "tahsilat" => "Tahsilat",
                "odeme" => "Odeme",
                "ödeme" => "Odeme",
                "borc" => "Borc",
                "borç" => "Borc",
                _ => "Borc"
            };
        }

        private static decimal GetSignedAmount(CariHareket hareket)
        {
            return hareket.HareketTipi switch
            {
                "Alacak" or "Odeme" => hareket.Tutar,
                "Borc" or "Tahsilat" => -hareket.Tutar,
                _ => -hareket.Tutar
            };
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
