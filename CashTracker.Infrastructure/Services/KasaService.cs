using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class KasaService : IKasaService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly IEntitlementGuard? _entitlementGuard;
        private readonly ISubeKurService? _subeKurService;

        public KasaService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
            : this(dbFactory, isletmeService, null)
        {
        }

        public KasaService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            IEntitlementGuard? entitlementGuard,
            ISubeKurService? subeKurService = null)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _entitlementGuard = entitlementGuard;
            _subeKurService = subeKurService;
        }

        public async Task<List<Kasa>> GetAllAsync(DateTime? from = null, DateTime? to = null)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Kasalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId);

            if (from.HasValue) query = query.Where(x => x.Tarih >= from.Value.Date);
            if (to.HasValue)
            {
                var endExclusive = to.Value.Date.AddDays(1);
                query = query.Where(x => x.Tarih < endExclusive);
            }

            return await query.OrderByDescending(x => x.Tarih).ToListAsync();
        }

        public async Task<Kasa?> GetByIdAsync(int id)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Kasalar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId);
        }

        public async Task<int> CreateAsync(Kasa kasa)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = _entitlementGuard is null
                ? null
                : await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (kasa.Tarih == default)
                kasa.Tarih = DateTime.Now;

            if (_entitlementGuard is not null)
            {
                var entitlement = await _entitlementGuard.GetAsync(activeIsletmeId, HesapTipleri.Isletme);
                _entitlementGuard.EnsureWritable(entitlement);
                var periodStart = new DateTime(kasa.Tarih.Year, kasa.Tarih.Month, 1);
                var periodEnd = periodStart.AddMonths(1);
                var currentCount = await db.Kasalar.CountAsync(x =>
                    x.IsletmeId == activeIsletmeId && x.Tarih >= periodStart && x.Tarih < periodEnd);
                _entitlementGuard.EnsureLimit(entitlement, EntitlementLimits.CashTransaction, currentCount);
            }

            kasa.IsletmeId = activeIsletmeId;
            await ApplySnapshotAsync(kasa);
            kasa.Tip = NormalizeTip(kasa.Tip);
            kasa.OdemeYontemi = NormalizeOdemeYontemi(kasa.OdemeYontemi);
            kasa.Kalem = NormalizeKalem(kasa.Tip, kasa.Kalem, kasa.GiderTuru);
            kasa.GiderTuru = kasa.Tip == "Gider" ? kasa.Kalem : null;
            kasa.CreatedAt = DateTime.Now;
            db.Kasalar.Add(kasa);
            await db.SaveChangesAsync();
            if (transaction is not null)
                await transaction.CommitAsync();

            return kasa.Id;
        }

        public async Task<List<int>> CreateManyAsync(IEnumerable<Kasa> rows)
        {
            var rowList = rows.ToList();
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = _entitlementGuard is null
                ? null
                : await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            foreach (var row in rowList.Where(x => x.Tarih == default))
                row.Tarih = DateTime.Now;

            if (_entitlementGuard is not null && rowList.Count > 0)
            {
                var entitlement = await _entitlementGuard.GetAsync(activeIsletmeId, HesapTipleri.Isletme);
                _entitlementGuard.EnsureWritable(entitlement);
                foreach (var group in rowList.GroupBy(x => new { x.Tarih.Year, x.Tarih.Month }))
                {
                    var periodStart = new DateTime(group.Key.Year, group.Key.Month, 1);
                    var periodEnd = periodStart.AddMonths(1);
                    var currentCount = await db.Kasalar.CountAsync(x =>
                        x.IsletmeId == activeIsletmeId && x.Tarih >= periodStart && x.Tarih < periodEnd);
                    _entitlementGuard.EnsureLimit(
                        entitlement,
                        EntitlementLimits.CashTransaction,
                        currentCount,
                        group.Count());
                }
            }

            var createdIds = new List<int>();
            foreach (var kasa in rowList)
            {
                kasa.IsletmeId = activeIsletmeId;
                await ApplySnapshotAsync(kasa);
                kasa.Tip = NormalizeTip(kasa.Tip);
                kasa.OdemeYontemi = NormalizeOdemeYontemi(kasa.OdemeYontemi);
                kasa.Kalem = NormalizeKalem(kasa.Tip, kasa.Kalem, kasa.GiderTuru);
                kasa.GiderTuru = kasa.Tip == "Gider" ? kasa.Kalem : null;
                kasa.CreatedAt = DateTime.Now;
                db.Kasalar.Add(kasa);
            }

            await db.SaveChangesAsync();
            if (transaction is not null)
                await transaction.CommitAsync();

            foreach (var row in rowList)
                createdIds.Add(row.Id);

            return createdIds;
        }

        public async Task UpdateAsync(Kasa kasa)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();
            var existing = await db.Kasalar.FirstOrDefaultAsync(x => x.Id == kasa.Id && x.IsletmeId == activeIsletmeId);
            if (existing == null)
                return;

            // Preserve CreatedAt while updating editable fields.
            existing.Tarih = kasa.Tarih;
            existing.Tip = NormalizeTip(kasa.Tip);
            existing.Tutar = kasa.Tutar;
            existing.OrijinalTutar = kasa.Tutar;
            existing.TryKarsiligi = decimal.Round(kasa.Tutar * (existing.KurSnapshot <= 0 ? 1m : existing.KurSnapshot), 2, MidpointRounding.AwayFromZero);
            existing.OdemeYontemi = NormalizeOdemeYontemi(kasa.OdemeYontemi);
            existing.Kalem = NormalizeKalem(existing.Tip, kasa.Kalem, kasa.GiderTuru);
            existing.GiderTuru = existing.Tip == "Gider" ? existing.Kalem : null;
            existing.Aciklama = kasa.Aciklama;
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();

            var kasa = await db.Kasalar.FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId);
            if (kasa == null) return;

            db.Kasalar.Remove(kasa);
            await db.SaveChangesAsync();
        }

        private static string NormalizeTip(string value)
        {
            return value switch
            {
                "Giris" => "Gelir",
                "Cikis" => "Gider",
                _ => value
            };
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "nakit" => "Nakit",
                "cash" => "Nakit",
                "kredikarti" => "KrediKarti",
                "kredi karti" => "KrediKarti",
                "kredi kartı" => "KrediKarti",
                "kart" => "KrediKarti",
                "creditcard" => "KrediKarti",
                "credit card" => "KrediKarti",
                "online" => "OnlineOdeme",
                "onlineodeme" => "OnlineOdeme",
                "online odeme" => "OnlineOdeme",
                "online \u00f6deme" => "OnlineOdeme",
                "online payment" => "OnlineOdeme",
                "havale" => "Havale",
                "transfer" => "Havale",
                "bank transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string NormalizeKalem(string tip, string? kalem, string? legacyGiderTuru)
        {
            var normalizedKalem = string.IsNullOrWhiteSpace(kalem) ? null : kalem.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedKalem))
                return normalizedKalem;

            if (tip == "Gider")
            {
                if (!string.IsNullOrWhiteSpace(legacyGiderTuru))
                    return legacyGiderTuru.Trim();
                return "Genel Gider";
            }

            return "Genel Gelir";
        }

        private async Task ApplySnapshotAsync(Kasa row)
        {
            if (_subeKurService is null)
            {
                row.ParaBirimi = string.IsNullOrWhiteSpace(row.ParaBirimi) ? "TRY" : row.ParaBirimi.Trim().ToUpperInvariant();
                row.OrijinalTutar = row.Tutar;
                row.KurSnapshot = row.KurSnapshot <= 0 ? 1m : row.KurSnapshot;
                row.TryKarsiligi = decimal.Round(row.Tutar * row.KurSnapshot, 2, MidpointRounding.AwayFromZero);
                return;
            }

            var snapshot = await _subeKurService.ResolveSnapshotAsync(row.ParaBirimi, row.Tutar);
            row.SubeId = snapshot.SubeId;
            row.ParaBirimi = snapshot.ParaBirimi;
            row.OrijinalTutar = snapshot.OrijinalTutar;
            row.KurSnapshot = snapshot.Kur;
            row.TryKarsiligi = snapshot.TryKarsiligi;
        }
    }
}
