using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Tests.Support
{
    internal sealed class FakeKasaService : IKasaService
    {
        private readonly List<Kasa> _rows;

        public FakeKasaService(IEnumerable<Kasa>? seed = null)
        {
            _rows = seed?.ToList() ?? new List<Kasa>();
            NextId = _rows.Count == 0 ? 1 : _rows.Max(x => x.Id) + 1;
        }

        public int NextId { get; set; }
        public Kasa? LastCreated { get; private set; }
        public IReadOnlyList<Kasa> Rows => _rows;

        public Task<List<Kasa>> GetAllAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _rows.AsEnumerable();

            if (from.HasValue)
                query = query.Where(x => x.Tarih >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.Tarih <= to.Value);

            return Task.FromResult(query.ToList());
        }

        public Task<Kasa?> GetByIdAsync(int id)
        {
            return Task.FromResult(_rows.FirstOrDefault(x => x.Id == id));
        }

        public Task<int> CreateAsync(Kasa kasa)
        {
            kasa.Id = NextId++;
            _rows.Add(kasa);
            LastCreated = kasa;
            return Task.FromResult(kasa.Id);
        }

        public Task<List<int>> CreateManyAsync(IEnumerable<Kasa> rows)
        {
            var ids = new List<int>();
            foreach (var row in rows)
            {
                row.Id = NextId++;
                _rows.Add(row);
                LastCreated = row;
                ids.Add(row.Id);
            }

            return Task.FromResult(ids);
        }

        public Task UpdateAsync(Kasa kasa)
        {
            var existing = _rows.FindIndex(x => x.Id == kasa.Id);
            if (existing >= 0)
                _rows[existing] = kasa;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _rows.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeKalemTanimiService : IKalemTanimiService
    {
        private readonly List<KalemTanimi> _rows;

        public FakeKalemTanimiService(IEnumerable<KalemTanimi>? seed = null)
        {
            _rows = seed?.ToList() ?? new List<KalemTanimi>();
            NextId = _rows.Count == 0 ? 1 : _rows.Max(x => x.Id) + 1;
        }

        public int NextId { get; set; }

        public Task<List<KalemTanimi>> GetAllAsync()
        {
            return Task.FromResult(_rows.ToList());
        }

        public Task<List<KalemTanimi>> GetByTipAsync(string tip)
        {
            var rows = _rows
                .Where(x => string.Equals(x.Tip, tip, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(rows);
        }

        public Task<int> CreateAsync(string tip, string ad)
        {
            var row = new KalemTanimi
            {
                Id = NextId++,
                Tip = tip,
                Ad = ad
            };
            _rows.Add(row);
            return Task.FromResult(row.Id);
        }

        public Task UpdateAsync(int id, string ad)
        {
            var row = _rows.FirstOrDefault(x => x.Id == id);
            if (row != null)
                row.Ad = ad;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _rows.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeSummaryService : ISummaryService
    {
        public PeriodSummary SummaryToReturn { get; set; } = new PeriodSummary();

        public Task<PeriodSummary> GetSummaryAsync(DateTime from, DateTime to)
        {
            return Task.FromResult(new PeriodSummary
            {
                From = from,
                To = to,
                IncomeTotal = SummaryToReturn.IncomeTotal,
                ExpenseTotal = SummaryToReturn.ExpenseTotal,
                IncomeCount = SummaryToReturn.IncomeCount,
                ExpenseCount = SummaryToReturn.ExpenseCount
            });
        }

        public Task<PeriodSummary> GetMonthlySummaryAsync(int year, int month)
        {
            return Task.FromResult(SummaryToReturn);
        }
    }

    internal sealed class FakeIsletmeService : IIsletmeService
    {
        public Isletme Active { get; set; } = new Isletme { Id = 1, Ad = "Varsayilan", IsAktif = true };

        public Task<List<Isletme>> GetAllAsync()
        {
            return Task.FromResult(new List<Isletme> { Active });
        }

        public Task<Isletme?> GetByIdAsync(int id)
        {
            return Task.FromResult(id == Active.Id ? Active : null);
        }

        public Task<Isletme> GetActiveAsync()
        {
            return Task.FromResult(Active);
        }

        public Task<int> GetActiveIdAsync()
        {
            return Task.FromResult(Active.Id);
        }

        public Task<int> CreateAsync(string ad, bool makeActive = false)
        {
            var created = new Isletme
            {
                Id = Active.Id + 1,
                Ad = ad,
                IsAktif = makeActive
            };

            if (makeActive)
                Active = created;

            return Task.FromResult(created.Id);
        }

        public Task RenameAsync(int id, string ad)
        {
            if (id == Active.Id)
                Active.Ad = ad;

            return Task.CompletedTask;
        }

        public Task UpdateSetupAsync(int id, string ad, string isletmeTuru, string konum, bool tamamlandi, string? hesapTipi = null, bool? muhasebeciVarMi = null, MuhasebeciProfilKaydetRequest? muhasebeciProfil = null)
        {
            if (id == Active.Id)
            {
                Active.Ad = ad;
                Active.IsletmeTuru = isletmeTuru;
                Active.Konum = konum;
                Active.KolayKurulumTamamlandi = tamamlandi;
                if (muhasebeciVarMi.HasValue)
                    Active.MuhasebeciVarMi = muhasebeciVarMi.Value;
                if (!string.IsNullOrWhiteSpace(hesapTipi))
                    Active.TenantTipi = hesapTipi;
            }

            return Task.CompletedTask;
        }

        public Task SetActiveAsync(int id)
        {
            Active.Id = id;
            Active.IsAktif = true;
            return Task.CompletedTask;
        }

        public Task SetActiveCustomerContextAsync(int musteriIsletmeId)
        {
            Active.Id = musteriIsletmeId;
            Active.IsAktif = true;
            return Task.CompletedTask;
        }

        public Task ClearActiveCustomerContextAsync()
        {
            return Task.CompletedTask;
        }

        public Task<ActiveBusinessAccess> GetActiveAccessAsync()
        {
            return Task.FromResult(new ActiveBusinessAccess
            {
                IsletmeId = Active.Id,
                MuhasebeciMusteriBaglami = false,
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.TamIslem
            });
        }

        public Task DeleteAsync(int id)
        {
            if (id == Active.Id)
                Active.IsAktif = false;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeDailyReportService : IDailyReportService
    {
        public Task<DailyReport> GetDailyReportAsync(DateTime date)
        {
            return Task.FromResult(new DailyReport
            {
                Date = date,
                IncomeTotal = 0,
                ExpenseTotal = 0,
                IncomeCount = 0,
                ExpenseCount = 0
            });
        }
    }

    internal sealed class FakeAppSecurityService : IAppSecurityService
    {
        public string Pin { get; private set; } = "0000";

        public Task<string> GetPinAsync()
        {
            return Task.FromResult(Pin);
        }

        public Task SetPinAsync(string pin)
        {
            Pin = pin;
            return Task.CompletedTask;
        }

        public Task<bool> VerifyPinAsync(string pin)
        {
            return Task.FromResult(string.Equals(Pin, pin, StringComparison.Ordinal));
        }

        public Task<bool> IsDefaultPinAsync()
        {
            return Task.FromResult(string.Equals(Pin, "0000", StringComparison.Ordinal));
        }
    }

    internal sealed class FakeTelegramApprovalService : ITelegramApprovalService
    {
        public TelegramApprovalStatus NextStatus { get; set; } = TelegramApprovalStatus.NotConfigured;
        public string? NextMessage { get; set; }

        public Task<TelegramApprovalResult> RequestApprovalAsync(
            TelegramApprovalRequest request,
            System.Threading.CancellationToken ct = default)
        {
            return Task.FromResult(new TelegramApprovalResult(NextStatus, NextMessage));
        }

        public bool TryResolve(string code, bool approved, out string? title)
        {
            title = null;
            return false;
        }
    }

    internal sealed class FakeReceiptOcrService : IReceiptOcrService
    {
        public ReceiptOcrResult NextResult { get; set; } = new ReceiptOcrResult();
        public Exception? NextException { get; set; }
        public ReceiptOcrRequest? LastRequest { get; private set; }

        public Task<ReceiptOcrResult> AnalyzeReceiptAsync(ReceiptOcrRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            if (NextException != null)
                throw NextException;

            return Task.FromResult(NextResult);
        }
    }

    internal sealed class FakeTelegramReceiptSessionStore : ITelegramReceiptSessionStore
    {
        private readonly Dictionary<string, TelegramReceiptSessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);

        public TelegramReceiptSessionState? LastSaved { get; private set; }

        public Task<TelegramReceiptSessionState?> GetAsync(long chatId, long userId, CancellationToken ct = default)
        {
            _sessions.TryGetValue(BuildKey(chatId, userId), out var value);
            return Task.FromResult(value);
        }

        public Task SaveAsync(TelegramReceiptSessionState state, CancellationToken ct = default)
        {
            state.UpdatedAtUtc = DateTime.UtcNow;
            _sessions[BuildKey(state.ChatId, state.UserId)] = state;
            LastSaved = state;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(long chatId, long userId, CancellationToken ct = default)
        {
            _sessions.Remove(BuildKey(chatId, userId));
            return Task.CompletedTask;
        }

        private static string BuildKey(long chatId, long userId)
        {
            return $"{chatId}:{userId}";
        }
    }

    internal sealed class FakeBarcodeReaderService : IBarcodeReaderService
    {
        public BarcodeReadResult NextResult { get; set; } = BarcodeReadResult.Found("8690000000000");
        public string? LastImagePath { get; private set; }

        public Task<BarcodeReadResult> TryReadAsync(string imagePath, CancellationToken ct = default)
        {
            LastImagePath = imagePath;
            return Task.FromResult(NextResult);
        }
    }

    internal sealed class FakeUrunHizmetService : IUrunHizmetService
    {
        private readonly List<UrunHizmet> _rows;

        public FakeUrunHizmetService(IEnumerable<UrunHizmet>? seed = null)
        {
            _rows = seed?.ToList() ?? new List<UrunHizmet>();
            NextId = _rows.Count == 0 ? 1 : _rows.Max(x => x.Id) + 1;
        }

        public int NextId { get; set; }
        public IReadOnlyList<UrunHizmet> Rows => _rows;

        public Task<List<UrunHizmet>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_rows.ToList());
        }

        public Task<UrunHizmet?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(_rows.FirstOrDefault(x => x.Id == id));
        }

        public Task<UrunHizmet?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            return Task.FromResult(_rows.FirstOrDefault(x =>
                string.Equals(x.Barkod, barcode, StringComparison.OrdinalIgnoreCase) && x.Aktif));
        }

        public Task<int> CreateAsync(UrunHizmetCreateRequest request, CancellationToken ct = default)
        {
            var duplicate = _rows.Any(x =>
                !string.IsNullOrWhiteSpace(request.Barkod) &&
                string.Equals(x.Barkod, request.Barkod, StringComparison.OrdinalIgnoreCase));

            if (duplicate)
                throw new InvalidOperationException("Bu barkod aktif isletmede zaten kayitli.");

            var row = new UrunHizmet
            {
                Id = NextId++,
                IsletmeId = 1,
                Tip = request.Tip,
                Ad = request.Ad,
                Barkod = request.Barkod,
                Birim = string.IsNullOrWhiteSpace(request.Birim) ? "Adet" : request.Birim,
                KdvOrani = request.KdvOrani,
                AlisFiyati = request.AlisFiyati,
                SatisFiyati = request.SatisFiyati,
                KritikStok = request.KritikStok,
                Aktif = true
            };

            _rows.Add(row);
            return Task.FromResult(row.Id);
        }

        public Task UpdateAsync(UrunHizmet urunHizmet, CancellationToken ct = default)
        {
            var existing = _rows.FindIndex(x => x.Id == urunHizmet.Id);
            if (existing >= 0)
                _rows[existing] = urunHizmet;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            _rows.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeStokService : IStokService
    {
        private readonly Dictionary<int, decimal> _currentStock = new();

        public List<StokHareketCreateRequest> Requests { get; } = new();
        public StokHareketCreateRequest? LastRequest => Requests.LastOrDefault();

        public void SetCurrentStock(int urunHizmetId, decimal stock)
        {
            _currentStock[urunHizmetId] = stock;
        }

        public Task<decimal> GetCurrentStockAsync(int urunHizmetId, CancellationToken ct = default)
        {
            _currentStock.TryGetValue(urunHizmetId, out var stock);
            return Task.FromResult(stock);
        }

        public Task<List<StokHareket>> GetRecentMovementsAsync(int limit = 20, CancellationToken ct = default)
        {
            var rows = Requests
                .TakeLast(Math.Clamp(limit, 1, 100))
                .Select((request, index) => new StokHareket
                {
                    Id = index + 1,
                    UrunHizmetId = request.UrunHizmetId,
                    Tarih = request.Tarih ?? DateTime.Today,
                    Miktar = request.Miktar,
                    Kaynak = request.Kaynak,
                    Aciklama = request.Aciklama
                })
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.Id)
                .ToList();

            return Task.FromResult(rows);
        }

        public Task<StokHareketResult> CreateMovementAsync(StokHareketCreateRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            _currentStock.TryGetValue(request.UrunHizmetId, out var stock);
            stock += request.Miktar;
            _currentStock[request.UrunHizmetId] = stock;

            return Task.FromResult(new StokHareketResult
            {
                Hareket = new StokHareket
                {
                    Id = Requests.Count,
                    UrunHizmetId = request.UrunHizmetId,
                    Miktar = request.Miktar,
                    Kaynak = request.Kaynak,
                    Aciklama = request.Aciklama
                },
                MevcutStok = stock
            });
        }
    }

    internal sealed class FakeTelegramStockSessionStore : ITelegramStockSessionStore
    {
        private readonly Dictionary<string, TelegramStockSessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);

        public TelegramStockSessionState? LastSaved { get; private set; }

        public Task<TelegramStockSessionState?> GetAsync(long chatId, long userId, CancellationToken ct = default)
        {
            _sessions.TryGetValue(BuildKey(chatId, userId), out var value);
            return Task.FromResult(value);
        }

        public Task SaveAsync(TelegramStockSessionState state, CancellationToken ct = default)
        {
            state.UpdatedAtUtc = DateTime.UtcNow;
            _sessions[BuildKey(state.ChatId, state.UserId)] = state;
            LastSaved = state;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(long chatId, long userId, CancellationToken ct = default)
        {
            _sessions.Remove(BuildKey(chatId, userId));
            return Task.CompletedTask;
        }

        private static string BuildKey(long chatId, long userId)
        {
            return $"{chatId}:{userId}";
        }
    }

}
