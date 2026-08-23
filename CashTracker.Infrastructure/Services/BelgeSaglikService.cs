using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class BelgeSaglikService : IBelgeSaglikService
    {
        private const int TaslakAzamiPuan = 25;
        private const int DosyaAzamiPuan = 20;
        private const int SatirAzamiPuan = 20;
        private const int CariAzamiPuan = 15;
        private const int VadeAzamiPuan = 10;
        private const int VeriIstegiAzamiPuan = 10;

        private static readonly string[] KesilmisDurumlar =
        [
            FaturaDurum.Kesildi,
            FaturaDurum.KismiOdendi,
            FaturaDurum.Odendi
        ];

        private static readonly string[] DesteklenenBelgeTipleri = ["PDF", "XML", "HTML"];

        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public BelgeSaglikService(IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<BelgeSaglikOzeti> GetAsync(
            int isletmeId,
            DateTime? referenceDate = null,
            CancellationToken ct = default)
        {
            if (isletmeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(isletmeId), "İşletme kimliği geçerli olmalıdır.");

            var reference = (referenceDate ?? DateTime.Today).Date;
            var periodStart = new DateTime(reference.Year, reference.Month, 1);
            var periodEndExclusive = periodStart.AddMonths(1);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var invoices = await db.Faturalar
                .AsNoTracking()
                .Where(x =>
                    x.IsletmeId == isletmeId &&
                    x.Tarih >= periodStart &&
                    x.Tarih < periodEndExclusive &&
                    x.Durum != FaturaDurum.Iptal)
                .Select(x => new InvoiceRow(
                    x.Id,
                    x.CariKartId,
                    x.Tarih,
                    x.VadeTarihi,
                    x.FaturaTipi,
                    x.Durum,
                    x.PortalBelgeNo,
                    x.PortalUuid))
                .ToListAsync(ct);

            var invoiceIds = invoices.Select(x => x.Id).ToList();
            var customerIds = invoices.Select(x => x.CariKartId).Distinct().ToList();

            var invoiceIdsWithLines = invoiceIds.Count == 0
                ? new HashSet<int>()
                : (await db.FaturaSatirlari
                    .AsNoTracking()
                    .Where(x => x.IsletmeId == isletmeId && invoiceIds.Contains(x.FaturaId))
                    .Select(x => x.FaturaId)
                    .Distinct()
                    .ToListAsync(ct))
                    .ToHashSet();

            var invoiceIdsWithFiles = invoiceIds.Count == 0
                ? new HashSet<int>()
                : (await db.BelgeDosyalari
                    .AsNoTracking()
                    .Where(x => x.IsletmeId == isletmeId && invoiceIds.Contains(x.FaturaId))
                    .Select(x => new FileRow(x.FaturaId, x.BelgeTipi, x.DosyaYolu))
                    .ToListAsync(ct))
                    .Where(x =>
                        DesteklenenBelgeTipleri.Contains(x.BelgeTipi.Trim().ToUpperInvariant()) &&
                        !string.IsNullOrWhiteSpace(x.DosyaYolu))
                    .Select(x => x.FaturaId)
                    .ToHashSet();

            var customers = customerIds.Count == 0
                ? new Dictionary<int, CustomerRow>()
                : await db.CariKartlari
                    .AsNoTracking()
                    .Where(x => x.IsletmeId == isletmeId && customerIds.Contains(x.Id))
                    .Select(x => new CustomerRow(x.Id, x.Unvan, x.VergiNoTc))
                    .ToDictionaryAsync(x => x.Id, ct);

            var pendingDataRequestCount = await db.MuhasebeciSohbetVeriIstekleri
                .AsNoTracking()
                .CountAsync(x =>
                    x.HedefIsletmeId == isletmeId &&
                    x.Durum == MuhasebeciSohbetVeriIstegiDurumlari.Beklemede, ct);

            var accountantLinked = await db.MuhasebeciMusterileri
                .AsNoTracking()
                .AnyAsync(x =>
                    x.MusteriIsletmeId == isletmeId &&
                    x.Durum == "Aktif" &&
                    x.BaslangicAt < reference.AddDays(1) &&
                    (!x.BitisAt.HasValue || x.BitisAt.Value >= reference), ct);

            var result = new BelgeSaglikOzeti
            {
                DonemBaslangic = periodStart,
                DonemBitis = periodEndExclusive.AddDays(-1),
                FaturaSayisi = invoices.Count,
                BekleyenVeriIstegiSayisi = pendingDataRequestCount,
                SonBelgeAt = invoices.Count == 0 ? null : invoices.Max(x => x.Tarih),
                MuhasebeciBagli = accountantLinked
            };

            if (invoices.Count == 0)
            {
                if (pendingDataRequestCount > 0)
                {
                    result.Sorunlar.Add(new BelgeSaglikSorunu
                    {
                        Kod = "BekleyenVeriIstegi",
                        Baslik = "Muhasebecinin bekleyen veri isteği var",
                        Adet = pendingDataRequestCount,
                        PuanEtkisi = 0,
                        AksiyonUrl = "/app/sohbetler"
                    });
                }

                return result;
            }

            var assessments = invoices.Select(invoice =>
            {
                var isDraft = invoice.Durum is FaturaDurum.YerelTaslak or FaturaDurum.PortalTaslak;
                var hasPortalDocument =
                    string.Equals(invoice.FaturaTipi, "Satis", StringComparison.OrdinalIgnoreCase) &&
                    KesilmisDurumlar.Contains(invoice.Durum) &&
                    (!string.IsNullOrWhiteSpace(invoice.PortalUuid) ||
                     !string.IsNullOrWhiteSpace(invoice.PortalBelgeNo));
                var fileMissing = !invoiceIdsWithFiles.Contains(invoice.Id) && !hasPortalDocument;
                var lineMissing = !invoiceIdsWithLines.Contains(invoice.Id);
                var customerMissing =
                    !customers.TryGetValue(invoice.CariKartId, out var customer) ||
                    string.IsNullOrWhiteSpace(customer.Unvan) ||
                    string.IsNullOrWhiteSpace(customer.VergiNoTc);
                var dueDateMissing = !invoice.VadeTarihi.HasValue;

                return new InvoiceAssessment(
                    isDraft,
                    fileMissing,
                    lineMissing,
                    customerMissing,
                    dueDateMissing);
            }).ToList();

            result.TaslakFaturaSayisi = assessments.Count(x => x.IsDraft);
            result.DosyasiEksikFaturaSayisi = assessments.Count(x => x.FileMissing);
            result.SatiriEksikFaturaSayisi = assessments.Count(x => x.LineMissing);
            result.CariBilgisiEksikFaturaSayisi = assessments.Count(x => x.CustomerMissing);
            result.VadeTarihiEksikFaturaSayisi = assessments.Count(x => x.DueDateMissing);
            result.EksikBelgeSayisi = assessments.Count(x => x.HasAnyProblem);
            result.HazirBelgeSayisi = result.FaturaSayisi - result.EksikBelgeSayisi;

            AddIssue(result, "TaslakFatura", "Tamamlanmayı bekleyen taslak faturalar var", result.TaslakFaturaSayisi, TaslakAzamiPuan, "/app/faturalar");
            AddIssue(result, "BelgeDosyasiEksik", "Fatura belgesi eksik", result.DosyasiEksikFaturaSayisi, DosyaAzamiPuan, "/app/faturalar");
            AddIssue(result, "FaturaSatiriEksik", "Satırı bulunmayan faturalar var", result.SatiriEksikFaturaSayisi, SatirAzamiPuan, "/app/faturalar");
            AddIssue(result, "CariBilgisiEksik", "Cari unvanı veya vergi kimlik bilgisi eksik", result.CariBilgisiEksikFaturaSayisi, CariAzamiPuan, "/app/cari-hesaplar");
            AddIssue(result, "VadeTarihiEksik", "Vade tarihi eksik faturalar var", result.VadeTarihiEksikFaturaSayisi, VadeAzamiPuan, "/app/faturalar");
            AddIssue(result, "BekleyenVeriIstegi", "Muhasebecinin bekleyen veri isteği var", pendingDataRequestCount, VeriIstegiAzamiPuan, "/app/sohbetler");

            result.Skor = Math.Clamp(100 - result.Sorunlar.Sum(x => x.PuanEtkisi), 0, 100);
            result.Durum = result.Skor >= 85
                ? BelgeSaglikDurumlari.Hazir
                : result.Skor >= 60
                    ? BelgeSaglikDurumlari.Dikkat
                    : BelgeSaglikDurumlari.Eksik;

            return result;

            void AddIssue(
                BelgeSaglikOzeti summary,
                string code,
                string title,
                int count,
                int maxPoints,
                string actionUrl)
            {
                if (count <= 0)
                    return;

                var ratio = Math.Min(1m, count / (decimal)summary.FaturaSayisi);
                var points = (int)Math.Round(maxPoints * ratio, MidpointRounding.AwayFromZero);
                summary.Sorunlar.Add(new BelgeSaglikSorunu
                {
                    Kod = code,
                    Baslik = title,
                    Adet = count,
                    PuanEtkisi = Math.Max(1, points),
                    AksiyonUrl = actionUrl
                });
            }
        }

        private sealed record InvoiceRow(
            int Id,
            int CariKartId,
            DateTime Tarih,
            DateTime? VadeTarihi,
            string FaturaTipi,
            string Durum,
            string PortalBelgeNo,
            string PortalUuid);

        private sealed record CustomerRow(int Id, string Unvan, string VergiNoTc);

        private sealed record FileRow(int FaturaId, string BelgeTipi, string DosyaYolu);

        private sealed record InvoiceAssessment(
            bool IsDraft,
            bool FileMissing,
            bool LineMissing,
            bool CustomerMissing,
            bool DueDateMissing)
        {
            public bool HasAnyProblem =>
                IsDraft || FileMissing || LineMissing || CustomerMissing || DueDateMissing;
        }
    }
}
