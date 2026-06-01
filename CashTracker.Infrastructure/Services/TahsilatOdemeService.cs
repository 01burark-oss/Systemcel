using System;
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
    public sealed class TahsilatOdemeService : ITahsilatOdemeService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public TahsilatOdemeService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<int> CreateAsync(TahsilatOdemeRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.FaturaId <= 0)
                throw new ArgumentException("Fatura secilmelidir.", nameof(request));

            if (request.Tutar <= 0)
                throw new ArgumentException("Tutar sifirdan buyuk olmalidir.", nameof(request));

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var fatura = await db.Faturalar
                .FirstOrDefaultAsync(x => x.Id == request.FaturaId && x.IsletmeId == activeIsletmeId, ct);

            if (fatura == null)
                throw new InvalidOperationException("Fatura bulunamadi.");

            if (fatura.Durum == FaturaDurum.Iptal)
                throw new InvalidOperationException("Iptal faturaya tahsilat/odeme girilemez.");

            if (fatura.Durum is FaturaDurum.YerelTaslak or FaturaDurum.PortalTaslak)
                await IssueDraftInvoiceAsync(db, fatura, activeIsletmeId, ct);
            else if (fatura.Durum is not (FaturaDurum.Kesildi or FaturaDurum.KismiOdendi or FaturaDurum.Odendi))
                throw new InvalidOperationException("Fatura durumu tahsilat/odeme icin uygun degil.");

            var remaining = fatura.GenelToplam - fatura.OdenenTutar;
            var amount = Math.Min(request.Tutar, remaining);
            if (amount <= 0)
                throw new InvalidOperationException("Fatura zaten tamamen odenmis.");

            var isSale = fatura.FaturaTipi == "Satis";
            var hareketTipi = isSale ? "Tahsilat" : "Odeme";
            var odemeYontemi = FaturaService.NormalizeOdemeYontemi(request.OdemeYontemi);

            var kasa = new Kasa
            {
                IsletmeId = activeIsletmeId,
                Tarih = request.Tarih,
                Tip = isSale ? "Gelir" : "Gider",
                Tutar = amount,
                OdemeYontemi = odemeYontemi,
                Kalem = isSale ? "Fatura Tahsilat" : "Fatura Odeme",
                GiderTuru = isSale ? null : "Fatura Odeme",
                Aciklama = $"Fatura {hareketTipi} | {fatura.YerelFaturaNo}",
                CreatedAt = DateTime.Now
            };
            db.Kasalar.Add(kasa);
            await db.SaveChangesAsync(ct);

            var cariHareket = new CariHareket
            {
                IsletmeId = activeIsletmeId,
                CariKartId = fatura.CariKartId,
                Tarih = request.Tarih,
                HareketTipi = hareketTipi,
                Tutar = amount,
                Kaynak = "TahsilatOdeme",
                Aciklama = $"Fatura {hareketTipi} | {fatura.YerelFaturaNo}",
                CreatedAt = DateTime.Now
            };
            db.CariHareketleri.Add(cariHareket);
            await db.SaveChangesAsync(ct);

            var row = new TahsilatOdeme
            {
                IsletmeId = activeIsletmeId,
                FaturaId = fatura.Id,
                CariKartId = fatura.CariKartId,
                Tarih = request.Tarih,
                Tip = hareketTipi,
                Tutar = amount,
                OdemeYontemi = odemeYontemi,
                KasaId = kasa.Id,
                CariHareketId = cariHareket.Id,
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama) ? null : request.Aciklama.Trim(),
                CreatedAt = DateTime.Now
            };
            db.TahsilatOdemeleri.Add(row);

            fatura.OdenenTutar += amount;
            fatura.Durum = fatura.OdenenTutar >= fatura.GenelToplam ? FaturaDurum.Odendi : FaturaDurum.KismiOdendi;
            fatura.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return row.Id;
        }

        private static async Task IssueDraftInvoiceAsync(
            CashTrackerDbContext db,
            Fatura fatura,
            int activeIsletmeId,
            CancellationToken ct)
        {
            var satirlar = await db.FaturaSatirlari
                .Where(x => x.IsletmeId == activeIsletmeId && x.FaturaId == fatura.Id)
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
                Aciklama = $"Fatura {fatura.YerelFaturaNo}",
                CreatedAt = DateTime.Now
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
        }
    }
}
