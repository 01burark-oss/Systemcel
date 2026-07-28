using System;
using System.Collections.Generic;
using System.Data;
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
    public sealed class HizliSatisService : IHizliSatisService
    {
        private const string PerakendeMusteriUnvani = "Perakende Müşteri";
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public HizliSatisService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<HizliSatisResult> CreateAsync(HizliSatisCreateRequest request, CancellationToken ct = default)
        {
            ValidateRequest(request);

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            var islemAnahtari = request.IslemAnahtari.Trim();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var previous = await db.Faturalar
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IsletmeId == activeIsletmeId && x.HizliSatisAnahtari == islemAnahtari,
                    ct);
            if (previous is not null)
            {
                await tx.RollbackAsync(ct);
                return ToResult(previous, true);
            }

            var groupedRows = request.Satirlar
                .GroupBy(x => x.UrunHizmetId)
                .Select(x => new HizliSatisSatirRequest
                {
                    UrunHizmetId = x.Key,
                    Miktar = x.Sum(row => row.Miktar)
                })
                .ToList();
            var productIds = groupedRows.Select(x => x.UrunHizmetId).ToList();
            var products = await db.UrunHizmetleri
                .Where(x => x.IsletmeId == activeIsletmeId && x.Aktif && productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("Sepetteki ürünlerden biri bulunamadı veya pasif.");

            var stockMovements = await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && productIds.Contains(x.UrunHizmetId))
                .Select(x => new { x.UrunHizmetId, x.Miktar })
                .ToListAsync(ct);
            var stockByProduct = stockMovements
                .GroupBy(x => x.UrunHizmetId)
                .ToDictionary(x => x.Key, x => x.Sum(row => row.Miktar));

            foreach (var row in groupedRows)
            {
                var product = products[row.UrunHizmetId];
                if (product.SatisFiyati <= 0)
                    throw new InvalidOperationException($"{product.Ad} için satış fiyatı girilmelidir.");

                if (product.Tip == "Urun")
                {
                    var currentStock = stockByProduct.GetValueOrDefault(product.Id);
                    if (row.Miktar > currentStock)
                        throw new InvalidOperationException($"{product.Ad} için stok yetersiz. Mevcut: {currentStock:N2}");
                }
            }

            var cari = await db.CariKartlari.FirstOrDefaultAsync(
                x => x.IsletmeId == activeIsletmeId && x.Unvan == PerakendeMusteriUnvani,
                ct);
            if (cari is null)
            {
                cari = new CariKart
                {
                    IsletmeId = activeIsletmeId,
                    Tip = "Musteri",
                    Unvan = PerakendeMusteriUnvani,
                    Aktif = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.CariKartlari.Add(cari);
                await db.SaveChangesAsync(ct);
            }

            var calculatedRows = groupedRows
                .Select(row => CalculateLine(products[row.UrunHizmetId], row.Miktar))
                .ToList();
            var now = DateTime.Now;
            var saleDate = request.Tarih == default ? now : request.Tarih;
            var paymentMethod = FaturaService.NormalizeOdemeYontemi(request.OdemeYontemi);
            var invoice = new Fatura
            {
                IsletmeId = activeIsletmeId,
                CariKartId = cari.Id,
                Tarih = saleDate,
                FaturaTipi = "Satis",
                Durum = FaturaDurum.Odendi,
                YerelFaturaNo = await CreateLocalInvoiceNumberAsync(db, activeIsletmeId, ct),
                HizliSatisAnahtari = islemAnahtari,
                AraToplam = calculatedRows.Sum(x => x.SatirNetTutar + x.IskontoTutar),
                IskontoToplam = calculatedRows.Sum(x => x.IskontoTutar),
                KdvToplam = calculatedRows.Sum(x => x.KdvTutar),
                GenelToplam = calculatedRows.Sum(x => x.SatirToplam),
                OdemeYontemi = paymentMethod,
                Aciklama = "Hızlı satış",
                KesildiAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            invoice.OdenenTutar = invoice.GenelToplam;
            db.Faturalar.Add(invoice);
            await db.SaveChangesAsync(ct);

            foreach (var line in calculatedRows)
            {
                line.IsletmeId = activeIsletmeId;
                line.FaturaId = invoice.Id;
                db.FaturaSatirlari.Add(line);
                if (line.StokEtkilesin && line.UrunHizmetId.HasValue)
                {
                    db.StokHareketleri.Add(new StokHareket
                    {
                        IsletmeId = activeIsletmeId,
                        UrunHizmetId = line.UrunHizmetId.Value,
                        Tarih = saleDate,
                        Miktar = -line.Miktar,
                        HareketTipi = "Cikis",
                        Kaynak = "HizliSatis",
                        Aciklama = $"Hızlı satış | {invoice.YerelFaturaNo}",
                        CreatedAt = now
                    });
                }
            }

            db.CariHareketleri.Add(new CariHareket
            {
                IsletmeId = activeIsletmeId,
                CariKartId = cari.Id,
                Tarih = saleDate,
                HareketTipi = "Alacak",
                Tutar = invoice.GenelToplam,
                Kaynak = "HizliSatis",
                Aciklama = $"Hızlı satış | {invoice.YerelFaturaNo}",
                CreatedAt = now
            });
            await db.SaveChangesAsync(ct);

            var paymentMovement = new CariHareket
            {
                IsletmeId = activeIsletmeId,
                CariKartId = cari.Id,
                Tarih = saleDate,
                HareketTipi = "Tahsilat",
                Tutar = invoice.GenelToplam,
                Kaynak = "HizliSatis",
                Aciklama = $"Hızlı satış tahsilatı | {invoice.YerelFaturaNo}",
                CreatedAt = now
            };
            var cash = new Kasa
            {
                IsletmeId = activeIsletmeId,
                Tarih = saleDate,
                Tip = "Gelir",
                Tutar = invoice.GenelToplam,
                OdemeYontemi = paymentMethod,
                Kalem = "Hızlı Satış",
                Aciklama = $"Hızlı satış | {invoice.YerelFaturaNo}",
                CreatedAt = now
            };
            db.CariHareketleri.Add(paymentMovement);
            db.Kasalar.Add(cash);
            await db.SaveChangesAsync(ct);

            db.TahsilatOdemeleri.Add(new TahsilatOdeme
            {
                IsletmeId = activeIsletmeId,
                FaturaId = invoice.Id,
                CariKartId = cari.Id,
                Tarih = saleDate,
                Tip = "Tahsilat",
                Tutar = invoice.GenelToplam,
                OdemeYontemi = paymentMethod,
                KasaId = cash.Id,
                CariHareketId = paymentMovement.Id,
                Aciklama = "Hızlı satış tahsilatı",
                CreatedAt = now
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ToResult(invoice, false);
        }

        private static FaturaSatir CalculateLine(UrunHizmet product, decimal quantity)
        {
            var gross = Math.Round(quantity * product.SatisFiyati, 2, MidpointRounding.AwayFromZero);
            var vatRate = Math.Max(0m, product.KdvOrani);
            var divisor = 1m + vatRate / 100m;
            var net = divisor <= 0m ? gross : Math.Round(gross / divisor, 2, MidpointRounding.AwayFromZero);

            return new FaturaSatir
            {
                UrunHizmetId = product.Id,
                Aciklama = product.Ad,
                Birim = product.Birim,
                Miktar = quantity,
                BirimFiyat = product.SatisFiyati,
                IskontoOrani = 0,
                IskontoTutar = 0,
                KdvOrani = vatRate,
                KdvTutar = gross - net,
                SatirNetTutar = net,
                SatirToplam = gross,
                StokEtkilesin = product.Tip == "Urun"
            };
        }

        private static async Task<string> CreateLocalInvoiceNumberAsync(
            CashTrackerDbContext db,
            int isletmeId,
            CancellationToken ct)
        {
            var year = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
            var prefix = $"SF{year}";
            var count = await db.Faturalar.CountAsync(
                x => x.IsletmeId == isletmeId && x.YerelFaturaNo.StartsWith(prefix),
                ct);
            return $"{prefix}{count + 1:000000}";
        }

        private static HizliSatisResult ToResult(Fatura invoice, bool repeated)
        {
            return new HizliSatisResult
            {
                FaturaId = invoice.Id,
                FaturaNo = invoice.YerelFaturaNo,
                Toplam = invoice.GenelToplam,
                Tekrarlandi = repeated
            };
        }

        private static void ValidateRequest(HizliSatisCreateRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.IslemAnahtari))
                throw new ArgumentException("İşlem anahtarı gereklidir.", nameof(request));
            if (request.IslemAnahtari.Trim().Length > 64)
                throw new ArgumentException("İşlem anahtarı en fazla 64 karakter olabilir.", nameof(request));
            if (request.Satirlar.Count == 0)
                throw new ArgumentException("Sepete en az bir ürün ekleyin.", nameof(request));
            if (request.Satirlar.Any(x => x.UrunHizmetId <= 0 || x.Miktar <= 0))
                throw new ArgumentException("Sepet satırlarını kontrol edin.", nameof(request));
        }
    }
}
