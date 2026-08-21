using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class FinansalGorunumService : IFinansalGorunumService
    {
        private static readonly string[] IssuedStatuses =
        [
            FaturaDurum.Kesildi,
            FaturaDurum.KismiOdendi,
            FaturaDurum.Odendi
        ];

        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public FinansalGorunumService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<FinansalGorunum> GetAsync(
            DateTime referenceDate,
            int projectionWeeks = 13,
            CancellationToken ct = default)
        {
            if (referenceDate == default)
                throw new ArgumentException("Referans tarihi gereklidir.", nameof(referenceDate));

            var reference = referenceDate.Date;
            var referenceEnd = reference.AddDays(1);
            var weeks = Math.Clamp(projectionWeeks, 1, 13);
            var projectionStart = referenceEnd;
            var projectionEndExclusive = projectionStart.AddDays(weeks * 7);
            var businessId = await _isletmeService.GetActiveIdAsync();

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var invoices = await db.Faturalar
                .AsNoTracking()
                .Where(x =>
                    x.IsletmeId == businessId &&
                    x.Tarih < referenceEnd &&
                    IssuedStatuses.Contains(x.Durum) &&
                    (!x.KesildiAt.HasValue || x.KesildiAt < referenceEnd))
                .ToListAsync(ct);
            var invoiceIds = invoices.Select(x => x.Id).ToHashSet();
            var allPayments = await db.TahsilatOdemeleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == businessId)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var payments = allPayments
                .Where(x => invoiceIds.Contains(x.FaturaId) && x.Tarih < referenceEnd)
                .ToList();
            var customers = await db.CariKartlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == businessId)
                .ToDictionaryAsync(x => x.Id, ct);
            var cashRows = await db.Kasalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == businessId && x.Tarih < projectionEndExclusive)
                .ToListAsync(ct);
            var planItems = await db.NakitPlanKalemleri
                .AsNoTracking()
                .Where(x =>
                    x.IsletmeId == businessId &&
                    x.Aktif &&
                    x.IlkTarih < projectionEndExclusive &&
                    (!x.BitisTarihi.HasValue || x.BitisTarihi >= projectionStart))
                .ToListAsync(ct);
            var manualUnallocatedCount = await db.CariHareketleri
                .AsNoTracking()
                .CountAsync(x =>
                    x.IsletmeId == businessId &&
                    x.Tarih < referenceEnd &&
                    x.Kaynak == "Manuel" &&
                    (x.HareketTipi == "Tahsilat" || x.HareketTipi == "Odeme"), ct);

            var paymentsByInvoice = payments
                .GroupBy(x => x.FaturaId)
                .ToDictionary(x => x.Key, x => x.OrderBy(row => row.Tarih).ThenBy(row => row.Id).ToList());
            var allPaymentsByInvoice = allPayments
                .Where(x => invoiceIds.Contains(x.FaturaId))
                .GroupBy(x => x.FaturaId)
                .ToDictionary(x => x.Key, x => x.ToList());
            var positions = invoices
                .Select(x => BuildPosition(x, paymentsByInvoice))
                .ToList();
            var openSales = positions.Where(x => IsSale(x.Invoice) && x.Outstanding > 0m).ToList();
            var openPurchases = positions.Where(x => IsPurchase(x.Invoice) && x.Outstanding > 0m).ToList();
            var completedSales = BuildCompletedHistory(
                invoices.Where(IsSale),
                paymentsByInvoice);
            var completedPurchases = BuildCompletedHistory(
                invoices.Where(IsPurchase),
                paymentsByInvoice);
            var collectionDates = payments
                .Where(IsCollection)
                .GroupBy(x => x.CariKartId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(row => row.Tarih.Date).Distinct().OrderBy(date => date).ToList());

            var cashBalance = cashRows
                .Where(x => x.Tarih < referenceEnd)
                .Sum(SignedCashAmount);
            var openTotal = openSales.Sum(x => x.Outstanding);
            var overdueTotal = openSales
                .Where(x => DueDate(x.Invoice) < reference)
                .Sum(x => x.Outstanding);
            var customerAging = BuildCustomerAging(openSales, customers, reference, openTotal);
            var customerRisks = BuildCustomerRisks(
                openSales,
                completedSales,
                collectionDates,
                customers,
                reference,
                openTotal);
            var projection = BuildProjection(
                openSales,
                openPurchases,
                completedSales,
                completedPurchases,
                planItems,
                reference,
                weeks,
                cashBalance);
            var warnings = BuildWarnings(
                positions,
                allPaymentsByInvoice,
                customers,
                cashRows,
                reference,
                manualUnallocatedCount);

            return new FinansalGorunum
            {
                ReferansTarihi = reference,
                KasaBakiyesi = Money(cashBalance),
                AcikAlacakToplami = Money(openTotal),
                VadesiGecmisAlacakToplami = Money(overdueTotal),
                Yaslandirma = BuildAging(openSales, reference, openTotal),
                CariYaslandirma = customerAging,
                Yogunlasma = BuildConcentration(customerAging, openTotal),
                CariRiskleri = customerRisks,
                NakitProjeksiyonu = projection,
                IlkNegatifHafta = projection.FirstOrDefault(x => x.KapanisBakiyesi < 0m)?.Hafta,
                VeriUyarilari = warnings
            };
        }

        public async Task<List<NakitPlanKalemi>> GetPlanItemsAsync(CancellationToken ct = default)
        {
            var businessId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.NakitPlanKalemleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == businessId)
                .OrderByDescending(x => x.Aktif)
                .ThenBy(x => x.IlkTarih)
                .ThenBy(x => x.Ad)
                .ToListAsync(ct);
        }

        public async Task<int> CreatePlanItemAsync(
            NakitPlanKalemiKaydetRequest request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidatePlanRequest(request);

            var businessId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var entity = new NakitPlanKalemi { IsletmeId = businessId };
            ApplyPlanRequest(entity, request);
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;
            db.NakitPlanKalemleri.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdatePlanItemAsync(
            int id,
            NakitPlanKalemiKaydetRequest request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (id <= 0)
                return false;
            ValidatePlanRequest(request);

            var businessId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var entity = await db.NakitPlanKalemleri
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == businessId, ct);
            if (entity is null)
                return false;

            ApplyPlanRequest(entity, request);
            entity.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeletePlanItemAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return false;

            var businessId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var entity = await db.NakitPlanKalemleri
                .FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == businessId, ct);
            if (entity is null)
                return false;

            db.NakitPlanKalemleri.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        private static InvoicePosition BuildPosition(
            Fatura invoice,
            IReadOnlyDictionary<int, List<TahsilatOdeme>> paymentsByInvoice)
        {
            var paymentTotal = paymentsByInvoice.TryGetValue(invoice.Id, out var rows)
                ? rows.Where(x => IsPaymentForInvoice(x, invoice)).Sum(x => x.Tutar)
                : 0m;
            var paid = Math.Min(Math.Max(0m, invoice.GenelToplam), Math.Max(0m, paymentTotal));
            return new InvoicePosition(
                invoice,
                paid,
                Math.Max(0m, invoice.GenelToplam - paid));
        }

        private static Dictionary<int, List<CompletedPayment>> BuildCompletedHistory(
            IEnumerable<Fatura> invoices,
            IReadOnlyDictionary<int, List<TahsilatOdeme>> paymentsByInvoice)
        {
            return invoices
                .Select(invoice => new
                {
                    invoice.CariKartId,
                    Completed = BuildCompletedPayment(invoice, paymentsByInvoice)
                })
                .Where(x => x.Completed is not null)
                .GroupBy(x => x.CariKartId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(row => row.Completed!)
                        .OrderByDescending(row => row.PaidAt)
                        .ToList());
        }

        private static CompletedPayment? BuildCompletedPayment(
            Fatura invoice,
            IReadOnlyDictionary<int, List<TahsilatOdeme>> paymentsByInvoice)
        {
            if (invoice.GenelToplam <= 0m ||
                !paymentsByInvoice.TryGetValue(invoice.Id, out var rows))
            {
                return null;
            }

            decimal paid = 0m;
            foreach (var row in rows.Where(x => IsPaymentForInvoice(x, invoice)))
            {
                paid += row.Tutar;
                if (paid + 0.005m < invoice.GenelToplam)
                    continue;

                var paidAt = row.Tarih.Date;
                return new CompletedPayment(
                    paidAt,
                    (paidAt - DueDate(invoice)).Days,
                    Math.Max(0, (paidAt - invoice.Tarih.Date).Days));
            }

            return null;
        }

        private static List<AlacakYaslandirmaDilimi> BuildAging(
            IReadOnlyCollection<InvoicePosition> invoices,
            DateTime reference,
            decimal openTotal)
        {
            return AgingDefinitions.Select(definition =>
            {
                var selected = invoices
                    .Where(x => definition.Contains((reference - DueDate(x.Invoice)).Days))
                    .ToList();
                var amount = selected.Sum(x => x.Outstanding);
                return new AlacakYaslandirmaDilimi
                {
                    Kod = definition.Code,
                    Etiket = definition.Label,
                    Tutar = Money(amount),
                    FaturaAdedi = selected.Count,
                    Oran = Percent(amount, openTotal)
                };
            }).ToList();
        }

        private static List<CariAlacakYaslandirma> BuildCustomerAging(
            IReadOnlyCollection<InvoicePosition> openSales,
            IReadOnlyDictionary<int, CariKart> customers,
            DateTime reference,
            decimal openTotal)
        {
            return openSales
                .GroupBy(x => x.Invoice.CariKartId)
                .Select(group =>
                {
                    var rows = group.ToList();
                    decimal Bucket(int min, int max) => rows
                        .Where(x =>
                        {
                            var days = (reference - DueDate(x.Invoice)).Days;
                            return days >= min && days <= max;
                        })
                        .Sum(x => x.Outstanding);
                    var total = rows.Sum(x => x.Outstanding);
                    var overdueDays = rows
                        .Select(x => (reference - DueDate(x.Invoice)).Days)
                        .Where(x => x > 0)
                        .ToList();

                    return new CariAlacakYaslandirma
                    {
                        CariKartId = group.Key,
                        Unvan = CustomerName(group.Key, customers),
                        Toplam = Money(total),
                        VadesiGelmemis = Money(Bucket(int.MinValue, 0)),
                        Gun1Ila30 = Money(Bucket(1, 30)),
                        Gun31Ila60 = Money(Bucket(31, 60)),
                        Gun61Ila90 = Money(Bucket(61, 90)),
                        Gun91VeUzeri = Money(Bucket(91, int.MaxValue)),
                        AcikFaturaAdedi = rows.Count,
                        EnUzunGecikmeGunu = overdueDays.Count == 0 ? 0 : overdueDays.Max(),
                        ToplamdakiOrani = Percent(total, openTotal)
                    };
                })
                .OrderByDescending(x => x.Toplam)
                .ThenBy(x => x.Unvan, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<CariOdemeRitmi> BuildCustomerRisks(
            IReadOnlyCollection<InvoicePosition> openSales,
            IReadOnlyDictionary<int, List<CompletedPayment>> completedByCustomer,
            IReadOnlyDictionary<int, List<DateTime>> collectionDates,
            IReadOnlyDictionary<int, CariKart> customers,
            DateTime reference,
            decimal openTotal)
        {
            return openSales
                .GroupBy(x => x.Invoice.CariKartId)
                .Select(group =>
                {
                    var rows = group.ToList();
                    var openAmount = rows.Sum(x => x.Outstanding);
                    var overdueRows = rows.Where(x => DueDate(x.Invoice) < reference).ToList();
                    var overdueAmount = overdueRows.Sum(x => x.Outstanding);
                    var longestDelay = overdueRows.Count == 0
                        ? 0
                        : overdueRows.Max(x => (reference - DueDate(x.Invoice)).Days);
                    var history = completedByCustomer.TryGetValue(group.Key, out var samples)
                        ? samples
                        : [];
                    var recent = history.Take(3).ToList();
                    var previous = history.Skip(3).Take(6).ToList();
                    var change = recent.Count >= 2 && previous.Count >= 2
                        ? RoundOne(Median(recent.Select(x => x.DelayDays)) -
                                   Median(previous.Select(x => x.DelayDays)))
                        : (decimal?)null;
                    var intervals = collectionDates.TryGetValue(group.Key, out var dates)
                        ? dates.Zip(dates.Skip(1), (first, second) => (decimal)(second - first).Days).ToList()
                        : [];
                    decimal? averageDelay = history.Count == 0 ? null : RoundOne(history.Average(x => x.DelayDays));
                    decimal? medianDelay = history.Count == 0 ? null : RoundOne(Median(history.Select(x => x.DelayDays)));
                    decimal? averagePaymentDays = history.Count == 0
                        ? null
                        : RoundOne(history.Average(x => (decimal)x.PaymentDays));
                    decimal? medianPaymentDays = history.Count == 0 ? null : RoundOne(Median(history.Select(x => (decimal)x.PaymentDays)));
                    decimal? onTimeRate = history.Count == 0
                        ? null
                        : RoundOne((decimal)history.Count(x => x.DelayDays <= 0m) / history.Count * 100m);
                    decimal? medianInterval = intervals.Count == 0 ? null : RoundOne(Median(intervals));
                    var share = Percent(openAmount, openTotal);

                    return new CariOdemeRitmi
                    {
                        CariKartId = group.Key,
                        Unvan = CustomerName(group.Key, customers),
                        AcikAlacak = Money(openAmount),
                        VadesiGecmisAlacak = Money(overdueAmount),
                        EnUzunGecikmeGunu = longestDelay,
                        AcikAlacakOrani = share,
                        OrtalamaOdemeSapmasiGunu = averageDelay,
                        OrtancaOdemeSapmasiGunu = medianDelay,
                        OrtalamaOdemeSuresiGunu = averagePaymentDays,
                        OrtancaOdemeSuresiGunu = medianPaymentDays,
                        ZamanindaOdemeOrani = onTimeRate,
                        OdemeAraligiOrtancasiGunu = medianInterval,
                        SonDonemDegisimiGunu = change,
                        SonDonemOrnekAdedi = recent.Count,
                        OncekiDonemOrnekAdedi = previous.Count,
                        TamamlananOdemeAdedi = history.Count,
                        RitimDurumu = RhythmStatus(history.Count, change, medianDelay),
                        RiskSeviyesi = RiskLevel(longestDelay, share, overdueAmount)
                    };
                })
                .OrderByDescending(x => RiskOrder(x.RiskSeviyesi))
                .ThenByDescending(x => x.VadesiGecmisAlacak)
                .ThenByDescending(x => x.AcikAlacak)
                .ThenBy(x => x.Unvan, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static AlacakYogunlasmaOzeti BuildConcentration(
            IReadOnlyCollection<CariAlacakYaslandirma> customers,
            decimal openTotal)
        {
            if (openTotal <= 0m)
                return new AlacakYogunlasmaOzeti();

            var ordered = customers.OrderByDescending(x => x.Toplam).ToList();
            var topOne = Percent(ordered.FirstOrDefault()?.Toplam ?? 0m, openTotal);
            var topThree = Percent(ordered.Take(3).Sum(x => x.Toplam), openTotal);
            var topFive = Percent(ordered.Take(5).Sum(x => x.Toplam), openTotal);
            var hhi = Math.Round(
                ordered.Sum(x =>
                {
                    var share = x.Toplam / openTotal * 100m;
                    return share * share;
                }),
                0,
                MidpointRounding.AwayFromZero);

            return new AlacakYogunlasmaOzeti
            {
                EnBuyukCariOrani = topOne,
                IlkUcCariOrani = topThree,
                IlkBesCariOrani = topFive,
                Hhi = hhi,
                RiskSeviyesi = topOne >= 40m || hhi >= 2500m
                    ? "Yuksek"
                    : topOne >= 25m || hhi >= 1500m
                        ? "Orta"
                        : "Dusuk"
            };
        }

        private static List<NakitProjeksiyonHaftasi> BuildProjection(
            IReadOnlyCollection<InvoicePosition> openSales,
            IReadOnlyCollection<InvoicePosition> openPurchases,
            IReadOnlyDictionary<int, List<CompletedPayment>> completedSales,
            IReadOnlyDictionary<int, List<CompletedPayment>> completedPurchases,
            IReadOnlyCollection<NakitPlanKalemi> planItems,
            DateTime reference,
            int weeks,
            decimal openingBalance)
        {
            var projectionStart = reference.AddDays(1);
            var projectionEnd = projectionStart.AddDays(weeks * 7 - 1);
            var saleLags = BuildExpectedDelayLookup(completedSales);
            var purchaseLags = BuildExpectedDelayLookup(completedPurchases);
            var plannedOccurrences = ExpandPlanItems(planItems, projectionStart, projectionEnd);
            var result = new List<NakitProjeksiyonHaftasi>(weeks);
            var balance = openingBalance;

            for (var index = 0; index < weeks; index++)
            {
                var start = projectionStart.AddDays(index * 7);
                var end = start.AddDays(6);
                var collections = openSales
                    .Where(x => IsWithinWeek(
                        ExpectedDate(x.Invoice, saleLags, projectionStart),
                        start,
                        end))
                    .Sum(x => x.Outstanding);
                var payments = openPurchases
                    .Where(x => IsWithinWeek(
                        ExpectedDate(x.Invoice, purchaseLags, projectionStart),
                        start,
                        end))
                    .Sum(x => x.Outstanding);
                var plannedIncome = plannedOccurrences
                    .Where(x => IsIncome(x.Tip) && IsWithinWeek(x.Date, start, end))
                    .Sum(x => x.Amount);
                var plannedExpense = plannedOccurrences
                    .Where(x => IsExpense(x.Tip) && IsWithinWeek(x.Date, start, end))
                    .Sum(x => x.Amount);
                var net = collections + plannedIncome - payments - plannedExpense;
                var closing = balance + net;

                result.Add(new NakitProjeksiyonHaftasi
                {
                    Hafta = index + 1,
                    Baslangic = start,
                    Bitis = end,
                    AcilisBakiyesi = Money(balance),
                    BeklenenTahsilat = Money(collections),
                    PlanlananGelir = Money(plannedIncome),
                    BeklenenOdeme = Money(payments),
                    PlanlananGider = Money(plannedExpense),
                    NetDegisim = Money(net),
                    KapanisBakiyesi = Money(closing)
                });
                balance = closing;
            }

            return result;
        }

        private static Dictionary<int, int> BuildExpectedDelayLookup(
            IReadOnlyDictionary<int, List<CompletedPayment>> completedByCustomer)
        {
            return completedByCustomer
                .Where(x => x.Value.Count >= 2)
                .ToDictionary(
                x => x.Key,
                x => Math.Max(
                    0,
                    (int)Math.Round(
                        Median(x.Value.Select(row => row.DelayDays)),
                        0,
                        MidpointRounding.AwayFromZero)));
        }

        private static List<PlannedOccurrence> ExpandPlanItems(
            IReadOnlyCollection<NakitPlanKalemi> planItems,
            DateTime start,
            DateTime end)
        {
            var result = new List<PlannedOccurrence>();
            foreach (var item in planItems.Where(x => x.Aktif))
            {
                var repeatType = NormalizeRepeatType(item.TekrarTipi);
                var repeatInterval = Math.Clamp(item.TekrarAraligi, 1, 52);
                var occurrenceIndex = FirstOccurrenceIndex(item.IlkTarih.Date, start, repeatType, repeatInterval);
                for (var step = 0; step < 10000; step++, occurrenceIndex++)
                {
                    var occurrence = repeatType switch
                    {
                        "Haftalik" => item.IlkTarih.Date.AddDays(occurrenceIndex * repeatInterval * 7),
                        "Aylik" => item.IlkTarih.Date.AddMonths(occurrenceIndex * repeatInterval),
                        _ => item.IlkTarih.Date
                    };

                    if (item.BitisTarihi.HasValue && occurrence > item.BitisTarihi.Value.Date)
                        break;
                    if (occurrence > end)
                        break;
                    if (occurrence >= start)
                        result.Add(new PlannedOccurrence(occurrence, item.Tip, item.Tutar));
                    if (repeatType == "TekSefer")
                        break;
                }
            }

            return result;
        }

        private static int FirstOccurrenceIndex(
            DateTime firstDate,
            DateTime rangeStart,
            string repeatType,
            int repeatInterval)
        {
            if (firstDate >= rangeStart || repeatType == "TekSefer")
                return 0;

            if (repeatType == "Haftalik")
            {
                var intervalDays = repeatInterval * 7;
                return Math.Max(0, (int)Math.Ceiling((rangeStart - firstDate).TotalDays / intervalDays));
            }

            var monthDifference = (rangeStart.Year - firstDate.Year) * 12 + rangeStart.Month - firstDate.Month;
            var index = Math.Max(0, monthDifference / repeatInterval);
            while (firstDate.AddMonths(index * repeatInterval) < rangeStart)
                index++;
            return index;
        }

        private static List<FinansalVeriUyarisi> BuildWarnings(
            IReadOnlyCollection<InvoicePosition> positions,
            IReadOnlyDictionary<int, List<TahsilatOdeme>> allPaymentsByInvoice,
            IReadOnlyDictionary<int, CariKart> customers,
            IReadOnlyCollection<Kasa> cashRows,
            DateTime reference,
            int manualUnallocatedCount)
        {
            var warnings = new List<FinansalVeriUyarisi>();
            AddWarning(
                warnings,
                "VadeTarihiEksik",
                "Vade tarihi olmayan faturalar için fatura tarihi kullanıldı.",
                positions.Count(x => x.Invoice.VadeTarihi is null && x.Outstanding > 0m));
            AddWarning(
                warnings,
                "OdemeDetayiUyusmazligi",
                "Faturadaki ödenen tutar ile faturaya bağlı ödeme kayıtları uyuşmuyor.",
                positions.Count(x =>
                {
                    var detailed = allPaymentsByInvoice.TryGetValue(x.Invoice.Id, out var rows)
                        ? rows.Where(row => IsPaymentForInvoice(row, x.Invoice)).Sum(row => row.Tutar)
                        : 0m;
                    return Math.Abs(Math.Max(0m, x.Invoice.OdenenTutar) - Math.Max(0m, detailed)) > 0.01m;
                }));
            AddWarning(
                warnings,
                "FaturayaBaglanmamisOdeme",
                "Faturaya bağlanmamış manuel tahsilat/ödemeler yaşlandırma ve ödeme ritmine dahil edilmedi.",
                manualUnallocatedCount);
            AddWarning(
                warnings,
                "CariKaydiEksik",
                "Bazı açık faturaların cari kartı bulunamadı; geçici cari etiketi kullanıldı.",
                positions
                    .Where(x => x.Outstanding > 0m && !customers.ContainsKey(x.Invoice.CariKartId))
                    .Select(x => x.Invoice.CariKartId)
                    .Distinct()
                    .Count());
            AddWarning(
                warnings,
                "GelecekTarihliKasaKaydi",
                "Gelecek tarihli kasa kayıtları planlı işlem sayılmadı; projeksiyon için nakit planı kullanın.",
                cashRows.Count(x => x.Tarih.Date > reference));
            if (!cashRows.Any(x => x.Tarih.Date <= reference))
            {
                AddWarning(
                    warnings,
                    "KasaAcilisBakiyesiYok",
                    "Kayıtlı kasa hareketi olmadığı için açılış bakiyesi sıfır kabul edildi.",
                    1);
            }

            return warnings;
        }

        private static void AddWarning(
            ICollection<FinansalVeriUyarisi> warnings,
            string code,
            string message,
            int count)
        {
            if (count <= 0)
                return;
            warnings.Add(new FinansalVeriUyarisi
            {
                Kod = code,
                Mesaj = message,
                KayitAdedi = count
            });
        }

        private static void ValidatePlanRequest(NakitPlanKalemiKaydetRequest request)
        {
            var name = request.Ad?.Trim() ?? string.Empty;
            if (name.Length == 0)
                throw new ArgumentException("Plan kalemi adı gereklidir.", nameof(request));
            if (name.Length > 120)
                throw new ArgumentException("Plan kalemi adı 120 karakteri geçemez.", nameof(request));
            if (request.Tutar <= 0m || request.Tutar > 999_999_999_999m)
                throw new ArgumentException("Plan tutarı sıfırdan büyük ve geçerli aralıkta olmalıdır.", nameof(request));
            if (request.IlkTarih == default)
                throw new ArgumentException("İlk tarih gereklidir.", nameof(request));
            if (NormalizePlanType(request.Tip) is null)
                throw new ArgumentException("Plan tipi Gelir veya Gider olmalıdır.", nameof(request));
            if (!IsSupportedRepeatType(request.TekrarTipi))
                throw new ArgumentException("Tekrar tipi TekSefer, Haftalik veya Aylik olmalıdır.", nameof(request));
            var repeatType = NormalizeRepeatType(request.TekrarTipi);
            if (repeatType != "TekSefer" && request.TekrarAraligi is < 1 or > 52)
                throw new ArgumentException("Tekrar aralığı 1 ile 52 arasında olmalıdır.", nameof(request));
            if (request.BitisTarihi.HasValue && request.BitisTarihi.Value.Date < request.IlkTarih.Date)
                throw new ArgumentException("Bitiş tarihi ilk tarihten önce olamaz.", nameof(request));
            if ((request.Kategori?.Trim().Length ?? 0) > 80)
                throw new ArgumentException("Kategori 80 karakteri geçemez.", nameof(request));
            if ((request.Aciklama?.Trim().Length ?? 0) > 500)
                throw new ArgumentException("Açıklama 500 karakteri geçemez.", nameof(request));
        }

        private static void ApplyPlanRequest(
            NakitPlanKalemi entity,
            NakitPlanKalemiKaydetRequest request)
        {
            entity.Ad = request.Ad.Trim();
            entity.Tip = NormalizePlanType(request.Tip)!;
            entity.Tutar = Money(request.Tutar);
            entity.IlkTarih = request.IlkTarih.Date;
            entity.TekrarTipi = NormalizeRepeatType(request.TekrarTipi);
            entity.TekrarAraligi = entity.TekrarTipi == "TekSefer"
                ? 1
                : request.TekrarAraligi;
            entity.BitisTarihi = request.BitisTarihi?.Date;
            entity.Kategori = request.Kategori?.Trim() ?? string.Empty;
            entity.Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                ? null
                : request.Aciklama.Trim();
            entity.Aktif = request.Aktif;
        }

        private static string? NormalizePlanType(string? value)
        {
            return Normalize(value) switch
            {
                "gelir" or "giris" or "income" => "Gelir",
                "gider" or "cikis" or "expense" => "Gider",
                _ => null
            };
        }

        private static string NormalizeRepeatType(string? value)
        {
            return Normalize(value) switch
            {
                "haftalik" or "weekly" => "Haftalik",
                "aylik" or "monthly" => "Aylik",
                _ => "TekSefer"
            };
        }

        private static bool IsSupportedRepeatType(string? value)
        {
            return Normalize(value) is "" or "teksefer" or "oneoff" or "haftalik" or "weekly" or "aylik" or "monthly";
        }

        private static DateTime ExpectedDate(
            Fatura invoice,
            IReadOnlyDictionary<int, int> delayLookup,
            DateTime projectionStart)
        {
            var delay = delayLookup.TryGetValue(invoice.CariKartId, out var value) ? value : 0;
            var date = DueDate(invoice).AddDays(delay);
            return date < projectionStart ? projectionStart : date;
        }

        private static bool IsPaymentForInvoice(TahsilatOdeme payment, Fatura invoice)
        {
            return IsSale(invoice) ? IsCollection(payment) : IsSupplierPayment(payment);
        }

        private static bool IsSale(Fatura invoice) => Normalize(invoice.FaturaTipi) is "satis" or "sale";
        private static bool IsPurchase(Fatura invoice) => Normalize(invoice.FaturaTipi) is "alis" or "purchase";
        private static bool IsCollection(TahsilatOdeme payment) => Normalize(payment.Tip) is "tahsilat" or "collection";
        private static bool IsSupplierPayment(TahsilatOdeme payment) => Normalize(payment.Tip) is "odeme" or "payment";
        private static bool IsIncome(string? value) => Normalize(value) is "gelir" or "giris" or "income";
        private static bool IsExpense(string? value) => Normalize(value) is "gider" or "cikis" or "expense";
        private static DateTime DueDate(Fatura invoice) => (invoice.VadeTarihi ?? invoice.Tarih).Date;
        private static bool IsWithinWeek(DateTime value, DateTime start, DateTime end) => value >= start && value <= end;

        private static decimal SignedCashAmount(Kasa row)
        {
            if (IsIncome(row.Tip)) return row.Tutar;
            if (IsExpense(row.Tip)) return -row.Tutar;
            return 0m;
        }

        private static string CustomerName(int customerId, IReadOnlyDictionary<int, CariKart> customers)
        {
            return customers.TryGetValue(customerId, out var customer) &&
                   !string.IsNullOrWhiteSpace(customer.Unvan)
                ? customer.Unvan.Trim()
                : $"Cari #{customerId}";
        }

        private static string RhythmStatus(int count, decimal? change, decimal? medianDelay)
        {
            if (count < 2) return "YetersizVeri";
            if (change >= 7m) return "Kotulesiyor";
            if (change <= -7m) return "Iyilesiyor";
            if (medianDelay <= 0m) return "Vadesinde";
            return "Dengeli";
        }

        private static string RiskLevel(int longestDelay, decimal share, decimal overdueAmount)
        {
            if (longestDelay > 60 || (share >= 35m && overdueAmount > 0m)) return "Yuksek";
            if (longestDelay > 15 || (share >= 20m && overdueAmount > 0m)) return "Orta";
            return "Dusuk";
        }

        private static int RiskOrder(string risk) => risk switch
        {
            "Yuksek" => 3,
            "Orta" => 2,
            _ => 1
        };

        private static decimal Median(IEnumerable<decimal> values)
        {
            var ordered = values.OrderBy(x => x).ToList();
            if (ordered.Count == 0)
                return 0m;
            var middle = ordered.Count / 2;
            return ordered.Count % 2 == 1
                ? ordered[middle]
                : (ordered[middle - 1] + ordered[middle]) / 2m;
        }

        private static decimal Money(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static decimal RoundOne(decimal value) =>
            Math.Round(value, 1, MidpointRounding.AwayFromZero);

        private static decimal Percent(decimal amount, decimal total)
        {
            return total <= 0m
                ? 0m
                : Math.Round(amount / total * 100m, 1, MidpointRounding.AwayFromZero);
        }

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('\u0131', 'i')
                .Replace('\u015f', 's')
                .Replace('\u011f', 'g')
                .Replace('\u00fc', 'u')
                .Replace('\u00f6', 'o')
                .Replace('\u00e7', 'c');
        }

        private static readonly AgingDefinition[] AgingDefinitions =
        [
            new("VadesiGelmedi", "Vadesi gelmedi", int.MinValue, 0),
            new("Gun1_30", "1-30 gün", 1, 30),
            new("Gun31_60", "31-60 gün", 31, 60),
            new("Gun61_90", "61-90 gün", 61, 90),
            new("Gun91Uzeri", "91+ gün", 91, int.MaxValue)
        ];

        private sealed record InvoicePosition(Fatura Invoice, decimal Paid, decimal Outstanding);
        private sealed record CompletedPayment(DateTime PaidAt, decimal DelayDays, int PaymentDays);
        private sealed record PlannedOccurrence(DateTime Date, string Tip, decimal Amount);
        private sealed record AgingDefinition(string Code, string Label, int MinDays, int MaxDays)
        {
            public bool Contains(int days) => days >= MinDays && days <= MaxDays;
        }
    }
}
