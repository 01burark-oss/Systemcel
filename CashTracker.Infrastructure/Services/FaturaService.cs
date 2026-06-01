using System;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class FaturaService : IFaturaService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public FaturaService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<List<Fatura>> GetAllAsync(CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.Faturalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<FaturaDetail?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var fatura = await db.Faturalar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);

            if (fatura == null)
                return null;

            var cari = await db.CariKartlari
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == fatura.CariKartId && x.IsletmeId == activeIsletmeId, ct);
            var satirlar = await db.FaturaSatirlari
                .AsNoTracking()
                .Where(x => x.FaturaId == id && x.IsletmeId == activeIsletmeId)
                .OrderBy(x => x.Id)
                .ToListAsync(ct);

            return new FaturaDetail
            {
                Fatura = fatura,
                Cari = cari,
                Satirlar = satirlar
            };
        }

        public Task<FaturaTotals> CalculateTotalsAsync(IEnumerable<FaturaSatirRequest> satirlar, CancellationToken ct = default)
        {
            var rows = satirlar.Select(CalculateLine).ToList();
            return Task.FromResult(new FaturaTotals
            {
                AraToplam = CalculateAraToplam(rows),
                IskontoToplam = rows.Sum(x => x.IskontoTutar),
                KdvToplam = rows.Sum(x => x.KdvTutar),
                GenelToplam = rows.Sum(x => x.SatirToplam)
            });
        }

        public async Task<int> CreateDraftAsync(FaturaCreateRequest request, CancellationToken ct = default)
        {
            ValidateRequest(request);
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await EnsureCariExistsAsync(db, activeIsletmeId, request.CariKartId, ct);

            var calculated = request.Satirlar.Select(CalculateLine).ToList();
            var fatura = new Fatura
            {
                IsletmeId = activeIsletmeId,
                CariKartId = request.CariKartId,
                Tarih = request.Tarih,
                VadeTarihi = request.VadeTarihi,
                FaturaTipi = NormalizeFaturaTipi(request.FaturaTipi),
                Durum = FaturaDurum.YerelTaslak,
                YerelFaturaNo = await CreateLocalInvoiceNumberAsync(db, activeIsletmeId, request.FaturaTipi, ct),
                OdemeYontemi = NormalizeOdemeYontemi(request.OdemeYontemi),
                Aciklama = NormalizeOptional(request.Aciklama),
                AraToplam = CalculateAraToplam(calculated),
                IskontoToplam = calculated.Sum(x => x.IskontoTutar),
                KdvToplam = calculated.Sum(x => x.KdvTutar),
                GenelToplam = calculated.Sum(x => x.SatirToplam),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Faturalar.Add(fatura);
            await db.SaveChangesAsync(ct);

            foreach (var line in calculated)
            {
                line.IsletmeId = activeIsletmeId;
                line.FaturaId = fatura.Id;
                db.FaturaSatirlari.Add(line);
            }

            await db.SaveChangesAsync(ct);
            return fatura.Id;
        }

        public async Task UpdateDraftAsync(int id, FaturaCreateRequest request, CancellationToken ct = default)
        {
            ValidateRequest(request);
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var fatura = await db.Faturalar
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);

            if (fatura == null)
                return;

            if (fatura.Durum is not (FaturaDurum.YerelTaslak or FaturaDurum.PortalTaslak))
                throw new InvalidOperationException("Sadece taslak faturalar duzenlenebilir.");

            await EnsureCariExistsAsync(db, activeIsletmeId, request.CariKartId, ct);
            var calculated = request.Satirlar.Select(CalculateLine).ToList();

            fatura.CariKartId = request.CariKartId;
            fatura.Tarih = request.Tarih;
            fatura.VadeTarihi = request.VadeTarihi;
            fatura.FaturaTipi = NormalizeFaturaTipi(request.FaturaTipi);
            fatura.OdemeYontemi = NormalizeOdemeYontemi(request.OdemeYontemi);
            fatura.Aciklama = NormalizeOptional(request.Aciklama);
            fatura.AraToplam = CalculateAraToplam(calculated);
            fatura.IskontoToplam = calculated.Sum(x => x.IskontoTutar);
            fatura.KdvToplam = calculated.Sum(x => x.KdvTutar);
            fatura.GenelToplam = calculated.Sum(x => x.SatirToplam);
            fatura.UpdatedAt = DateTime.Now;

            var oldLines = db.FaturaSatirlari.Where(x => x.IsletmeId == activeIsletmeId && x.FaturaId == id);
            db.FaturaSatirlari.RemoveRange(oldLines);
            foreach (var line in calculated)
            {
                line.IsletmeId = activeIsletmeId;
                line.FaturaId = id;
                db.FaturaSatirlari.Add(line);
            }

            await db.SaveChangesAsync(ct);
        }

        public async Task MarkAsPortalDraftAsync(int id, string uuid, string belgeNo, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var fatura = await db.Faturalar.FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);
            if (fatura == null)
                return;

            if (fatura.Durum != FaturaDurum.YerelTaslak && fatura.Durum != FaturaDurum.PortalTaslak)
                throw new InvalidOperationException("Sadece taslak faturalar portal taslagi olabilir.");

            fatura.Durum = FaturaDurum.PortalTaslak;
            fatura.PortalUuid = NormalizeOptional(uuid);
            fatura.PortalBelgeNo = NormalizeOptional(belgeNo);
            fatura.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
        }

        public async Task MarkAsIssuedAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var fatura = await db.Faturalar.FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);
            if (fatura == null)
                return;

            if (fatura.Durum == FaturaDurum.Kesildi ||
                fatura.Durum == FaturaDurum.KismiOdendi ||
                fatura.Durum == FaturaDurum.Odendi)
            {
                return;
            }

            if (fatura.Durum == FaturaDurum.Iptal)
                throw new InvalidOperationException("Iptal fatura kesilemez.");

            var satirlar = await db.FaturaSatirlari
                .Where(x => x.IsletmeId == activeIsletmeId && x.FaturaId == id)
                .ToListAsync(ct);

            fatura.Durum = FaturaDurum.Kesildi;
            fatura.KesildiAt = DateTime.Now;
            fatura.UpdatedAt = DateTime.Now;

            db.CariHareketleri.Add(new CariHareket
            {
                IsletmeId = activeIsletmeId,
                CariKartId = fatura.CariKartId,
                Tarih = fatura.Tarih,
                HareketTipi = fatura.FaturaTipi == "Satis" ? "Alacak" : "Borc",
                Tutar = fatura.GenelToplam,
                Kaynak = "Fatura",
                Aciklama = $"Fatura {fatura.YerelFaturaNo}"
            });

            foreach (var line in satirlar.Where(x => x.StokEtkilesin && x.UrunHizmetId.HasValue))
            {
                db.StokHareketleri.Add(new StokHareket
                {
                    IsletmeId = activeIsletmeId,
                    UrunHizmetId = line.UrunHizmetId!.Value,
                    Tarih = fatura.Tarih,
                    Miktar = fatura.FaturaTipi == "Satis" ? -line.Miktar : line.Miktar,
                    HareketTipi = fatura.FaturaTipi == "Satis" ? "Cikis" : "Giris",
                    Kaynak = "Fatura",
                    Aciklama = $"Fatura stok | {fatura.YerelFaturaNo}"
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        public async Task CancelAsync(int id, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var fatura = await db.Faturalar.FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeIsletmeId, ct);
            if (fatura == null)
                return;

            if (fatura.Durum is FaturaDurum.Kesildi or FaturaDurum.KismiOdendi or FaturaDurum.Odendi)
                throw new InvalidOperationException("Kesilmis faturalar V1'de geri alinmaz; muhasebeci kontrolu gerekir.");

            fatura.Durum = FaturaDurum.Iptal;
            fatura.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
        }

        private static FaturaSatir CalculateLine(FaturaSatirRequest request)
        {
            if (request.Miktar <= 0)
                throw new ArgumentException("Satir miktari sifirdan buyuk olmali.");

            if (request.BirimFiyat < 0)
                throw new ArgumentException("Birim fiyat negatif olamaz.");

            var grossBeforeDiscount = request.Miktar * request.BirimFiyat;
            var discountRate = Math.Clamp(request.IskontoOrani, 0m, 100m);
            var grossDiscount = Math.Round(grossBeforeDiscount * discountRate / 100m, 2, MidpointRounding.AwayFromZero);
            var grossAfterDiscount = Math.Max(0m, grossBeforeDiscount - grossDiscount);
            var vatRate = Math.Max(0m, request.KdvOrani);
            var divisor = 1m + vatRate / 100m;
            var netBeforeDiscount = divisor <= 0m
                ? grossBeforeDiscount
                : Math.Round(grossBeforeDiscount / divisor, 2, MidpointRounding.AwayFromZero);
            var net = divisor <= 0m
                ? grossAfterDiscount
                : Math.Round(grossAfterDiscount / divisor, 2, MidpointRounding.AwayFromZero);
            var discount = Math.Max(0m, netBeforeDiscount - net);
            var vat = Math.Max(0m, grossAfterDiscount - net);

            return new FaturaSatir
            {
                UrunHizmetId = request.UrunHizmetId,
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama) ? "Urun/Hizmet" : request.Aciklama.Trim(),
                Birim = string.IsNullOrWhiteSpace(request.Birim) ? "Adet" : request.Birim.Trim(),
                Miktar = request.Miktar,
                BirimFiyat = request.BirimFiyat,
                IskontoOrani = discountRate,
                IskontoTutar = discount,
                KdvOrani = vatRate,
                KdvTutar = vat,
                SatirNetTutar = net,
                SatirToplam = grossAfterDiscount,
                StokEtkilesin = request.StokEtkilesin
            };
        }

        private static decimal CalculateAraToplam(IEnumerable<FaturaSatir> rows)
        {
            return rows.Sum(x => x.SatirNetTutar + x.IskontoTutar);
        }

        private static void ValidateRequest(FaturaCreateRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.CariKartId <= 0)
                throw new ArgumentException("Cari secilmelidir.", nameof(request));

            if (request.Satirlar.Count == 0)
                throw new ArgumentException("En az bir fatura satiri gerekli.", nameof(request));
        }

        private static async Task EnsureCariExistsAsync(CashTrackerDbContext db, int isletmeId, int cariId, CancellationToken ct)
        {
            var exists = await db.CariKartlari.AnyAsync(x => x.Id == cariId && x.IsletmeId == isletmeId, ct);
            if (!exists)
                throw new InvalidOperationException("Cari aktif isletmede bulunamadi.");
        }

        private static async Task<string> CreateLocalInvoiceNumberAsync(CashTrackerDbContext db, int isletmeId, string rawTip, CancellationToken ct)
        {
            var prefix = NormalizeFaturaTipi(rawTip) == "Satis" ? "SF" : "AF";
            var year = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
            var count = await db.Faturalar.CountAsync(x => x.IsletmeId == isletmeId && x.YerelFaturaNo.StartsWith($"{prefix}{year}"), ct);
            return $"{prefix}{year}{count + 1:000000}";
        }

        internal static string NormalizeFaturaTipi(string? value)
        {
            var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
            return raw switch
            {
                "alis" => "Alis",
                "alış" => "Alis",
                "purchase" => "Alis",
                _ => "Satis"
            };
        }

        internal static string NormalizeOdemeYontemi(string? value)
        {
            var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
            return raw switch
            {
                "kredikarti" or "kredi karti" or "kredi kartı" or "kart" => "KrediKarti",
                "online" or "onlineodeme" or "online odeme" or "online ödeme" => "OnlineOdeme",
                "havale" or "eft" or "transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
