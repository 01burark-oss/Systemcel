using System;
using System.Collections.Generic;
using System.Data;
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

namespace CashTracker.Infrastructure.Services
{
    public sealed class GelismisStokService : IGelismisStokService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;
        private readonly IEntitlementGuard? _entitlementGuard;
        private readonly ISubeKurService? _subeKurService;

        public GelismisStokService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService,
            IEntitlementGuard? entitlementGuard = null,
            ISubeKurService? subeKurService = null)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
            _entitlementGuard = entitlementGuard;
            _subeKurService = subeKurService;
        }

        public async Task<StokDefteriDto> GetAsync(int limit = 100, CancellationToken ct = default)
        {
            var isletmeId = await GetAuthorizedBusinessIdAsync(ct);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var defaultWarehouse = await EnsureDefaultWarehouseAsync(db, isletmeId, ct);
            var warehouses = await db.StokDepolari.AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId && x.Aktif)
                .OrderByDescending(x => x.Varsayilan).ThenBy(x => x.Ad)
                .ToListAsync(ct);
            var movements = await db.StokHareketleri.AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId)
                .OrderByDescending(x => x.Tarih).ThenByDescending(x => x.Id)
                .Take(Math.Clamp(limit, 1, 250))
                .ToListAsync(ct);
            var productIds = movements.Select(x => x.UrunHizmetId).Distinct().ToList();
            var productNames = await db.UrunHizmetleri.AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId && productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad, ct);
            var warehouseNames = warehouses.ToDictionary(x => x.Id, x => x.Ad);
            var visibleOperationIds = movements
                .Where(x => x.StokDefterIslemiId.HasValue)
                .Select(x => x.StokDefterIslemiId!.Value)
                .Distinct()
                .ToList();
            var reversed = await db.StokDefterIslemleri.AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId &&
                    x.TersKayitKaynakIslemId != null &&
                    visibleOperationIds.Contains(x.TersKayitKaynakIslemId.Value))
                .Select(x => x.TersKayitKaynakIslemId!.Value)
                .ToListAsync(ct);
            var reversedSet = reversed.ToHashSet();

            return new StokDefteriDto
            {
                Depolar = warehouses.Select(MapWarehouse).ToList(),
                Hareketler = movements.Select(x => new StokDefterHareketDto
                {
                    Id = x.Id,
                    IslemId = x.StokDefterIslemiId,
                    UrunHizmetId = x.UrunHizmetId,
                    UrunAdi = productNames.GetValueOrDefault(x.UrunHizmetId, "Silinmiş ürün"),
                    DepoId = x.DepoId ?? defaultWarehouse.Id,
                    DepoAdi = x.DepoId is null ? defaultWarehouse.Ad : warehouseNames.GetValueOrDefault(x.DepoId.Value, "Silinmiş depo"),
                    Tarih = x.Tarih,
                    Miktar = x.Miktar,
                    RezerveMiktar = x.RezerveMiktar,
                    HareketTipi = x.HareketTipi,
                    Aciklama = x.Aciklama,
                    TersKayitVar = x.StokDefterIslemiId is int operationId && reversedSet.Contains(operationId)
                }).ToList()
            };
        }

        public async Task<StokDepoDto> CreateWarehouseAsync(StokDepoOlusturRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var name = Required(request.Ad, 120, "Depo adı");
            var code = Required(request.Kod, 32, "Depo kodu").ToUpperInvariant();
            var isletmeId = await GetAuthorizedBusinessIdAsync(ct);
            var branchSnapshot = _subeKurService is null ? null : await _subeKurService.ResolveSnapshotAsync("TRY", 0, ct);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            if (await db.StokDepolari.AnyAsync(x => x.IsletmeId == isletmeId && x.Kod == code, ct))
                throw new InvalidOperationException("Bu depo kodu zaten kullanılıyor.");
            var warehouse = new StokDepo
            {
                IsletmeId = isletmeId,
                SubeId = branchSnapshot?.SubeId,
                Ad = name,
                Kod = code,
                Konum = Optional(request.Konum, 240),
                Varsayilan = !await db.StokDepolari.AnyAsync(x => x.IsletmeId == isletmeId && x.Aktif, ct)
            };
            db.StokDepolari.Add(warehouse);
            await db.SaveChangesAsync(ct);
            return MapWarehouse(warehouse);
        }

        public Task<StokDefterIslemResult> CreateMovementAsync(
            StokHareketIslemRequest request,
            string idempotencyKey,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Miktar <= 0) throw new ArgumentException("Miktar sıfırdan büyük olmalıdır.", nameof(request));
            var allowed = new[] { StokDefterIslemTipleri.Giris, StokDefterIslemTipleri.Cikis, StokDefterIslemTipleri.Rezervasyon, StokDefterIslemTipleri.RezervasyonBirakma };
            if (!allowed.Contains(request.IslemTipi, StringComparer.Ordinal))
                throw new ArgumentException("Geçersiz stok hareketi türü.", nameof(request));
            var hash = Hash($"movement|{request.UrunHizmetId}|{request.DepoId}|{request.IslemTipi}|{Invariant(request.Miktar)}|{Normalize(request.Aciklama)}");
            return ExecuteIdempotentAsync(idempotencyKey, hash, request.IslemTipi, request.Aciklama, async (db, isletmeId, operation, token) =>
            {
                var warehouses = await RequireProductAndWarehousesAsync(db, isletmeId, request.UrunHizmetId, [request.DepoId], token);
                var (physical, reserved) = await GetBalanceAsync(db, isletmeId, request.UrunHizmetId, request.DepoId, token);
                var physicalDelta = request.IslemTipi switch
                {
                    StokDefterIslemTipleri.Giris => request.Miktar,
                    StokDefterIslemTipleri.Cikis => -request.Miktar,
                    _ => 0m
                };
                var reservedDelta = request.IslemTipi switch
                {
                    StokDefterIslemTipleri.Rezervasyon => request.Miktar,
                    StokDefterIslemTipleri.RezervasyonBirakma => -request.Miktar,
                    _ => 0m
                };
                EnsureValidBalance(physical + physicalDelta, reserved + reservedDelta);
                AddMovement(db, operation, request.UrunHizmetId, request.DepoId, warehouses[request.DepoId], physicalDelta, reservedDelta, request.IslemTipi, request.Aciklama);
            }, ct);
        }

        public Task<StokDefterIslemResult> TransferAsync(StokTransferRequest request, string idempotencyKey, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Miktar <= 0) throw new ArgumentException("Transfer miktarı sıfırdan büyük olmalıdır.", nameof(request));
            if (request.KaynakDepoId == request.HedefDepoId) throw new ArgumentException("Kaynak ve hedef depo farklı olmalıdır.", nameof(request));
            var hash = Hash($"transfer|{request.UrunHizmetId}|{request.KaynakDepoId}|{request.HedefDepoId}|{Invariant(request.Miktar)}|{Normalize(request.Aciklama)}");
            return ExecuteIdempotentAsync(idempotencyKey, hash, StokDefterIslemTipleri.Transfer, request.Aciklama, async (db, isletmeId, operation, token) =>
            {
                var warehouses = await RequireProductAndWarehousesAsync(db, isletmeId, request.UrunHizmetId, [request.KaynakDepoId, request.HedefDepoId], token);
                var (physical, reserved) = await GetBalanceAsync(db, isletmeId, request.UrunHizmetId, request.KaynakDepoId, token);
                EnsureValidBalance(physical - request.Miktar, reserved);
                AddMovement(db, operation, request.UrunHizmetId, request.KaynakDepoId, warehouses[request.KaynakDepoId], -request.Miktar, 0, "TransferCikis", request.Aciklama);
                AddMovement(db, operation, request.UrunHizmetId, request.HedefDepoId, warehouses[request.HedefDepoId], request.Miktar, 0, "TransferGiris", request.Aciklama);
            }, ct);
        }

        public Task<StokDefterIslemResult> CountAsync(StokSayimRequest request, string idempotencyKey, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.Onaylandi) throw new ArgumentException("Sayım sonucu kullanıcı tarafından onaylanmalıdır.", nameof(request));
            if (request.SayilanMiktar < 0) throw new ArgumentException("Sayılan miktar negatif olamaz.", nameof(request));
            var hash = Hash($"count|{request.UrunHizmetId}|{request.DepoId}|{Invariant(request.SayilanMiktar)}|{Normalize(request.Aciklama)}");
            return ExecuteIdempotentAsync(idempotencyKey, hash, StokDefterIslemTipleri.SayimDuzeltme, request.Aciklama, async (db, isletmeId, operation, token) =>
            {
                var warehouses = await RequireProductAndWarehousesAsync(db, isletmeId, request.UrunHizmetId, [request.DepoId], token);
                var (physical, reserved) = await GetBalanceAsync(db, isletmeId, request.UrunHizmetId, request.DepoId, token);
                EnsureValidBalance(request.SayilanMiktar, reserved);
                var delta = request.SayilanMiktar - physical;
                if (delta != 0)
                    AddMovement(db, operation, request.UrunHizmetId, request.DepoId, warehouses[request.DepoId], delta, 0, StokDefterIslemTipleri.SayimDuzeltme, request.Aciklama);
            }, ct);
        }

        public Task<StokDefterIslemResult> ReverseAsync(int operationId, StokTersKayitRequest request, string idempotencyKey, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (operationId <= 0) throw new ArgumentOutOfRangeException(nameof(operationId));
            var hash = Hash($"reverse|{operationId}|{Normalize(request.Aciklama)}");
            return ExecuteIdempotentAsync(idempotencyKey, hash, StokDefterIslemTipleri.TersKayit, request.Aciklama, async (db, isletmeId, reversal, token) =>
            {
                var source = await db.StokDefterIslemleri
                    .SingleOrDefaultAsync(x => x.Id == operationId && x.IsletmeId == isletmeId, token)
                    ?? throw new InvalidOperationException("Ters kaydı alınacak işlem bulunamadı.");
                if (source.TersKayitKaynakIslemId is not null)
                    throw new InvalidOperationException("Bir ters kayıt yeniden ters çevrilemez.");
                if (await db.StokDefterIslemleri.AnyAsync(x => x.IsletmeId == isletmeId && x.TersKayitKaynakIslemId == source.Id, token))
                    throw new InvalidOperationException("Bu işlemin ters kaydı daha önce oluşturuldu.");
                var rows = await db.StokHareketleri.Where(x => x.IsletmeId == isletmeId && x.StokDefterIslemiId == source.Id).ToListAsync(token);
                foreach (var row in rows)
                {
                    if (row.DepoId is null) throw new InvalidOperationException("Legacy stok hareketleri işlem bazında ters çevrilemez.");
                    var (physical, reserved) = await GetBalanceAsync(db, isletmeId, row.UrunHizmetId, row.DepoId.Value, token);
                    EnsureValidBalance(physical - row.Miktar, reserved - row.RezerveMiktar);
                }
                reversal.TersKayitKaynakIslemId = source.Id;
                foreach (var row in rows)
                    AddMovement(db, reversal, row.UrunHizmetId, row.DepoId!.Value, row.SubeId, -row.Miktar, -row.RezerveMiktar, StokDefterIslemTipleri.TersKayit, request.Aciklama);
            }, ct);
        }

        private async Task<StokDefterIslemResult> ExecuteIdempotentAsync(
            string idempotencyKey,
            string hash,
            string operationType,
            string? description,
            Func<CashTrackerDbContext, int, StokDefterIslemi, CancellationToken, Task> action,
            CancellationToken ct)
        {
            var key = Required(idempotencyKey, 120, "Idempotency-Key");
            var isletmeId = await GetAuthorizedBusinessIdAsync(ct);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var existing = await db.StokDefterIslemleri.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.IsletmeId == isletmeId && x.IslemAnahtari == key, ct);
                if (existing is not null)
                {
                    EnsureSamePayload(existing, hash);
                    return MapOperation(existing, true);
                }

                var operation = new StokDefterIslemi
                {
                    IsletmeId = isletmeId,
                    IslemAnahtari = key,
                    IcerikOzeti = hash,
                    IslemTipi = operationType,
                    Aciklama = Optional(description, 500),
                    CreatedAt = DateTime.Now
                };
                db.StokDefterIslemleri.Add(operation);
                await db.SaveChangesAsync(ct);
                await action(db, isletmeId, operation, ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return MapOperation(operation, false);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                await using var lookup = await _dbFactory.CreateDbContextAsync(ct);
                var winner = await lookup.StokDefterIslemleri.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.IsletmeId == isletmeId && x.IslemAnahtari == key, ct);
                if (winner is null) throw;
                EnsureSamePayload(winner, hash);
                return MapOperation(winner, true);
            }
        }

        private async Task<int> GetAuthorizedBusinessIdAsync(CancellationToken ct)
        {
            var isletmeId = await _isletmeService.GetActiveIdAsync();
            if (_entitlementGuard is not null)
            {
                var entitlement = await _entitlementGuard.GetAsync(isletmeId, HesapTipleri.Isletme, ct);
                _entitlementGuard.EnsureFeature(entitlement, EntitlementFeatures.AdvancedStock);
            }
            return isletmeId;
        }

        private static async Task<Dictionary<int, int?>> RequireProductAndWarehousesAsync(CashTrackerDbContext db, int isletmeId, int productId, int[] warehouseIds, CancellationToken ct)
        {
            if (!await db.UrunHizmetleri.AnyAsync(x => x.Id == productId && x.IsletmeId == isletmeId && x.Aktif && x.Tip == "Urun", ct))
                throw new InvalidOperationException("Ürün aktif işletmede bulunamadı.");
            var distinct = warehouseIds.Distinct().ToArray();
            var warehouses = await db.StokDepolari.Where(x => distinct.Contains(x.Id) && x.IsletmeId == isletmeId && x.Aktif).ToDictionaryAsync(x => x.Id, x => x.SubeId, ct);
            if (warehouses.Count != distinct.Length) throw new InvalidOperationException("Depo aktif işletmede bulunamadı.");
            return warehouses;
        }

        private static async Task<(decimal Physical, decimal Reserved)> GetBalanceAsync(CashTrackerDbContext db, int isletmeId, int productId, int warehouseId, CancellationToken ct)
        {
            var isDefault = await db.StokDepolari.AnyAsync(x => x.Id == warehouseId && x.IsletmeId == isletmeId && x.Varsayilan, ct);
            var rows = await db.StokHareketleri
                .Where(x => x.IsletmeId == isletmeId && x.UrunHizmetId == productId && (x.DepoId == warehouseId || (isDefault && x.DepoId == null)))
                .Select(x => new { x.Miktar, x.RezerveMiktar })
                .ToListAsync(ct);
            return (rows.Sum(x => x.Miktar), rows.Sum(x => x.RezerveMiktar));
        }

        private static void EnsureValidBalance(decimal physical, decimal reserved)
        {
            if (physical < 0) throw new InvalidOperationException("Negatif stok oluşturulamaz.");
            if (reserved < 0) throw new InvalidOperationException("Bırakılacak rezervasyon mevcut rezervasyondan fazla olamaz.");
            if (reserved > physical) throw new InvalidOperationException("Kullanılabilir stok yetersiz.");
        }

        private static void AddMovement(CashTrackerDbContext db, StokDefterIslemi operation, int productId, int warehouseId, int? branchId, decimal physicalDelta, decimal reservedDelta, string type, string? description)
        {
            db.StokHareketleri.Add(new StokHareket
            {
                IsletmeId = operation.IsletmeId,
                SubeId = branchId,
                UrunHizmetId = productId,
                DepoId = warehouseId,
                StokDefterIslemiId = operation.Id,
                Tarih = operation.CreatedAt,
                Miktar = physicalDelta,
                RezerveMiktar = reservedDelta,
                HareketTipi = type,
                Kaynak = "GelismisStok",
                Aciklama = Optional(description, 500),
                CreatedAt = operation.CreatedAt
            });
        }

        private static async Task<StokDepo> EnsureDefaultWarehouseAsync(CashTrackerDbContext db, int isletmeId, CancellationToken ct)
        {
            var existing = await db.StokDepolari.FirstOrDefaultAsync(x => x.IsletmeId == isletmeId && x.Varsayilan && x.Aktif, ct);
            if (existing is not null) return existing;
            var warehouse = new StokDepo { IsletmeId = isletmeId, Ad = "Merkez Depo", Kod = "MERKEZ", Varsayilan = true };
            db.StokDepolari.Add(warehouse);
            await db.SaveChangesAsync(ct);
            return warehouse;
        }

        private static StokDepoDto MapWarehouse(StokDepo value) => new()
        {
            Id = value.Id,
            Ad = value.Ad,
            Kod = value.Kod,
            Konum = value.Konum,
            Varsayilan = value.Varsayilan
        };

        private static StokDefterIslemResult MapOperation(StokDefterIslemi value, bool replay) => new()
        {
            IslemId = value.Id,
            IslemTipi = value.IslemTipi,
            Tekrarlandi = replay,
            TersKayitKaynakIslemId = value.TersKayitKaynakIslemId
        };

        private static void EnsureSamePayload(StokDefterIslemi operation, string hash)
        {
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(operation.IcerikOzeti), Convert.FromHexString(hash)))
                throw new InvalidOperationException("Idempotency-Key farklı bir istek için daha önce kullanıldı.");
        }

        private static string Required(string? value, int maxLength, string label)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException($"{label} zorunludur.");
            if (normalized.Length > maxLength) throw new ArgumentException($"{label} en fazla {maxLength} karakter olabilir.");
            return normalized;
        }

        private static string? Optional(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            if (normalized.Length > maxLength) throw new ArgumentException($"Metin en fazla {maxLength} karakter olabilir.");
            return normalized;
        }

        private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
        private static string Invariant(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
        private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
