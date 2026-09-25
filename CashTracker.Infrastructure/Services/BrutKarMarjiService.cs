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

namespace CashTracker.Infrastructure.Services;

public sealed class BrutKarMarjiService : IBrutKarMarjiService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly ISubeKurService? _subeKurService;

    public BrutKarMarjiService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        ISubeKurService? subeKurService = null)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _subeKurService = subeKurService;
    }

    public async Task<BrutKarMarjiOzeti> GetAsync(DateTime baslangic, DateTime bitis, CancellationToken ct = default)
    {
        if (bitis.Date < baslangic.Date)
            throw new ArgumentException("Bitiş tarihi başlangıç tarihinden önce olamaz.", nameof(bitis));

        var endExclusive = bitis.Date == DateTime.MaxValue.Date
            ? DateTime.MaxValue
            : bitis.Date.AddDays(1);
        var businessId = await _isletmeService.GetActiveIdAsync();
        var activeBranch = _subeKurService is null ? null : (await _subeKurService.GetContextAsync(ct)).AktifSube;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var invoiceQuery = db.Faturalar.AsNoTracking()
            .Where(x => x.IsletmeId == businessId && x.Tarih < endExclusive &&
                (x.Durum == FaturaDurum.Kesildi || x.Durum == FaturaDurum.KismiOdendi || x.Durum == FaturaDurum.Odendi));
        var invoices = await invoiceQuery
            .Select(x => new InvoiceRow(x.Id, x.SubeId, x.Tarih, x.FaturaTipi, x.KurSnapshot))
            .ToListAsync(ct);
        var invoiceIds = invoices.Select(x => x.Id).ToList();
        List<InvoiceLineRow> invoiceLines = invoiceIds.Count == 0
            ? []
            : await db.FaturaSatirlari.AsNoTracking()
                .Where(x => x.IsletmeId == businessId && invoiceIds.Contains(x.FaturaId) && x.UrunHizmetId.HasValue && x.StokEtkilesin)
                .Select(x => new InvoiceLineRow(x.Id, x.FaturaId, x.UrunHizmetId!.Value, x.Miktar, x.SatirNetTutar))
                .ToListAsync(ct);
        var linesByInvoice = invoiceLines.ToLookup(x => x.InvoiceId);
        var movementQuery = db.StokHareketleri.AsNoTracking()
            .Where(x => x.IsletmeId == businessId && x.Tarih < endExclusive);
        var movements = await movementQuery
            .Select(x => new StockMovementRow(x.Id, x.StokDefterIslemiId, x.UrunHizmetId, x.SubeId, x.Tarih, x.Miktar, x.BirimMaliyetTry, x.Kaynak, x.HareketTipi))
            .ToListAsync(ct);
        var productIds = invoiceLines.Select(x => x.UrunHizmetId)
            .Union(movements.Select(x => x.ProductId)).Distinct().ToList();
        var productTypes = productIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.UrunHizmetleri.AsNoTracking()
                .Where(x => x.IsletmeId == businessId && productIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Tip })
                .ToDictionaryAsync(x => x.Id, x => x.Tip, ct);

        var events = new List<CostEvent>();
        foreach (var invoice in invoices)
        {
            foreach (var line in linesByInvoice[invoice.Id].Where(x => IsProduct(productTypes, x.UrunHizmetId)))
            {
                if (line.Quantity <= 0m)
                {
                    events.Add(CostEvent.Unknown(invoice, line));
                    continue;
                }

                var rate = invoice.ExchangeRate <= 0m ? 1m : invoice.ExchangeRate;
                if (string.Equals(invoice.Type, "Alis", StringComparison.OrdinalIgnoreCase))
                    events.Add(CostEvent.Purchase(invoice, line, decimal.Round(line.NetAmount / line.Quantity * rate, 2, MidpointRounding.AwayFromZero)));
                else
                    events.Add(CostEvent.Sale(invoice, line, decimal.Round(line.NetAmount * rate, 2, MidpointRounding.AwayFromZero)));
            }
        }

        foreach (var movement in movements.Where(x => !IsInvoiceStockSource(x.Source)))
        {
            if (!IsProduct(productTypes, movement.ProductId))
                continue;
            events.Add(IsTransfer(movement.MovementType)
                ? CostEvent.Transfer(movement)
                : CostEvent.Manual(movement));
        }

        var costs = new Dictionary<ProductKey, MovingAverageState>();
        var transferCosts = new Dictionary<int, decimal?>();
        decimal revenue = 0m;
        decimal costOfSales = 0m;
        var saleLines = 0;
        var missingCostLines = 0;

        foreach (var item in events.OrderBy(x => x.Date).ThenBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            var key = new ProductKey(item.ProductId, ResolveCostBranchId(item.BranchId, activeBranch));
            if (!costs.TryGetValue(key, out var state))
            {
                state = new MovingAverageState();
                costs.Add(key, state);
            }

            switch (item.Type)
            {
                case CostEventType.Purchase:
                    state.Receive(item.Quantity, item.UnitCostTry);
                    break;
                case CostEventType.ManualEntry:
                    if (item.UnitCostTry <= 0m)
                        state.MarkUnknown();
                    else
                        state.Receive(item.Quantity, item.UnitCostTry);
                    break;
                case CostEventType.ManualExit:
                    state.Consume(item.Quantity);
                    break;
                case CostEventType.Unknown:
                    state.MarkUnknown();
                    break;
                case CostEventType.TransferExit:
                    var transferredCost = state.Consume(item.Quantity);
                    if (item.TransferId.HasValue)
                        transferCosts[item.TransferId.Value] = transferredCost;
                    break;
                case CostEventType.TransferEntry:
                    if (item.TransferId.HasValue && transferCosts.TryGetValue(item.TransferId.Value, out var transferCost) && transferCost.HasValue)
                        state.ReceiveTotal(item.Quantity, transferCost.Value);
                    else
                        state.ReceiveUnknown(item.Quantity);
                    break;
                case CostEventType.Sale:
                    var inRequestedRange = item.Date.Date >= baslangic.Date && item.Date.Date <= bitis.Date;
                    var inRequestedBranch = IsInActiveBranch(item.BranchId, activeBranch);
                    if (inRequestedRange && inRequestedBranch)
                    {
                        saleLines++;
                        revenue += item.RevenueTry;
                    }

                    var saleCost = state.Consume(item.Quantity);
                    if (inRequestedRange && inRequestedBranch)
                    {
                        if (saleCost.HasValue)
                            costOfSales += saleCost.Value;
                        else
                            missingCostLines++;
                    }
                    break;
            }
        }

        if (saleLines == 0)
        {
            return new BrutKarMarjiOzeti
            {
                Baslangic = baslangic.Date,
                Bitis = bitis.Date,
                Durum = "VeriYok",
                Aciklama = "Bu dönemde maliyeti izlenen ürün satışı yok."
            };
        }

        if (missingCostLines > 0)
        {
            return new BrutKarMarjiOzeti
            {
                Baslangic = baslangic.Date,
                Bitis = bitis.Date,
                SatisGeliriTry = decimal.Round(revenue, 2),
                SatisSatiri = saleLines,
                EksikMaliyetliSatisSatiri = missingCostLines,
                Durum = "EksikMaliyet",
                Aciklama = $"{missingCostLines} satış satırının stok maliyeti eksik olduğu için brüt kâr hesaplanmadı."
            };
        }

        var grossMargin = revenue - costOfSales;
        return new BrutKarMarjiOzeti
        {
            Baslangic = baslangic.Date,
            Bitis = bitis.Date,
            SatisGeliriTry = decimal.Round(revenue, 2),
            SatisMaliyetiTry = decimal.Round(costOfSales, 2),
            BrutKarTry = decimal.Round(grossMargin, 2),
            BrutKarOrani = revenue == 0m ? null : decimal.Round(grossMargin / revenue * 100m, 1),
            SatisSatiri = saleLines,
            Guvenilir = true,
            Durum = "Hazir",
            Aciklama = "KDV hariç satış geliri ve hareketli ortalama stok maliyeti kullanıldı."
        };
    }

    private static bool IsProduct(IReadOnlyDictionary<int, string> productTypes, int productId) => productTypes.TryGetValue(productId, out var type) && string.Equals(type, "Urun", StringComparison.OrdinalIgnoreCase);
    private static bool IsInvoiceStockSource(string source) =>
        string.Equals(source, "Fatura", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(source, "HizliSatis", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(source, "PazaryeriMalKabul", StringComparison.OrdinalIgnoreCase);
    private static bool IsTransfer(string movementType) => movementType.StartsWith("Transfer", StringComparison.OrdinalIgnoreCase);
    private static bool IsInActiveBranch(int? eventBranchId, SubeDto? activeBranch) => activeBranch is null ||
        (activeBranch.Varsayilan
            ? eventBranchId is null || eventBranchId == activeBranch.Id
            : eventBranchId == activeBranch.Id);
    private static int? ResolveCostBranchId(int? eventBranchId, SubeDto? activeBranch) =>
        activeBranch is { Varsayilan: true } && (eventBranchId is null || eventBranchId == activeBranch.Id)
            ? null
            : eventBranchId;

    private sealed record InvoiceRow(int Id, int? BranchId, DateTime Date, string Type, decimal ExchangeRate);
    private sealed record InvoiceLineRow(int Id, int InvoiceId, int UrunHizmetId, decimal Quantity, decimal NetAmount);
    private sealed record StockMovementRow(int Id, int? TransferId, int ProductId, int? BranchId, DateTime Date, decimal Quantity, decimal UnitCostTry, string Source, string MovementType);
    private sealed record ProductKey(int ProductId, int? BranchId);

    private enum CostEventType { Purchase, ManualEntry, ManualExit, Sale, Unknown, TransferExit, TransferEntry }

    private sealed record CostEvent(int Id, DateTime Date, int SortOrder, int ProductId, int? BranchId, int? TransferId, decimal Quantity, decimal UnitCostTry, decimal RevenueTry, CostEventType Type)
    {
        public static CostEvent Purchase(InvoiceRow invoice, InvoiceLineRow line, decimal unitCostTry) => new(line.Id, invoice.Date, 0, line.UrunHizmetId, invoice.BranchId, null, line.Quantity, unitCostTry, 0m, CostEventType.Purchase);
        public static CostEvent Sale(InvoiceRow invoice, InvoiceLineRow line, decimal revenueTry) => new(line.Id, invoice.Date, 2, line.UrunHizmetId, invoice.BranchId, null, line.Quantity, 0m, revenueTry, CostEventType.Sale);
        public static CostEvent Unknown(InvoiceRow invoice, InvoiceLineRow line) => new(line.Id, invoice.Date, 0, line.UrunHizmetId, invoice.BranchId, null, 0m, 0m, 0m, CostEventType.Unknown);
        public static CostEvent Manual(StockMovementRow movement) => new(movement.Id, movement.Date, 1, movement.ProductId, movement.BranchId, null, Math.Abs(movement.Quantity), movement.UnitCostTry, 0m, movement.Quantity > 0m ? CostEventType.ManualEntry : CostEventType.ManualExit);
        public static CostEvent Transfer(StockMovementRow movement) => new(
            movement.Id,
            movement.Date,
            1,
            movement.ProductId,
            movement.BranchId,
            movement.TransferId,
            Math.Abs(movement.Quantity),
            0m,
            0m,
            movement.MovementType.EndsWith("Cikis", StringComparison.OrdinalIgnoreCase) ? CostEventType.TransferExit : CostEventType.TransferEntry);
    }

    private sealed class MovingAverageState
    {
        private decimal _quantity;
        private decimal _totalCost;
        private bool _unknown;

        public void Receive(decimal quantity, decimal unitCostTry)
        {
            if (quantity <= 0m || unitCostTry < 0m) { MarkUnknown(); return; }
            _quantity += quantity;
            _totalCost += quantity * unitCostTry;
        }

        public void ReceiveTotal(decimal quantity, decimal totalCost)
        {
            if (quantity <= 0m || totalCost < 0m) { MarkUnknown(); return; }
            _quantity += quantity;
            _totalCost += totalCost;
        }

        public void ReceiveUnknown(decimal quantity)
        {
            if (quantity > 0m) _quantity += quantity;
            MarkUnknown();
        }

        public void MarkUnknown() => _unknown = true;

        public decimal? Consume(decimal quantity)
        {
            if (quantity <= 0m || _unknown || _quantity < quantity) { _unknown = true; return null; }
            var unitCost = _quantity == 0m ? 0m : _totalCost / _quantity;
            var total = decimal.Round(quantity * unitCost, 2, MidpointRounding.AwayFromZero);
            _quantity -= quantity;
            _totalCost = decimal.Round(Math.Max(0m, _totalCost - total), 2, MidpointRounding.AwayFromZero);
            return total;
        }
    }
}
