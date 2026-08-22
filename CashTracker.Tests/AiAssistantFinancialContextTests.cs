using System.Net;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests;

public sealed class AiAssistantFinancialContextTests
{
    [Fact]
    public async Task OfflineAssistant_ExplainsWhichCustomersPayLate()
    {
        var service = CreateService(BuildFinancialView());

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Kim sürekli geç ödüyor?"
        });

        Assert.Contains("Örnek Market", result.Answer);
        Assert.Contains("25.000", result.Answer);
        Assert.Contains("45 gün", result.Answer);
        Assert.Contains("örnek sayısını", result.Answer);
    }

    [Fact]
    public async Task OfflineAssistant_UsesThirteenWeekProjectionForPayrollQuestion()
    {
        var service = CreateService(BuildFinancialView());

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Maaş gününe kadar kasa yeter mi?"
        });

        Assert.Contains("3. haftada negatife", result.Answer);
        Assert.Contains("10.000", result.Answer);
        Assert.Contains("maaş ödemesini kapsamaz", result.Answer);
    }

    [Fact]
    public async Task OfflineAssistant_ExplainsCustomerRiskWithoutMakingTheSalesDecision()
    {
        var service = CreateService(BuildFinancialView());

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Örnek Market'e mal vereyim mi?"
        });

        Assert.Contains("Örnek Market", result.Answer);
        Assert.Contains("satış kararı değildir", result.Answer);
        Assert.Contains("Tamamlanan ödeme örneği: 8", result.Answer);
    }

    private static AiAssistantService CreateService(FinansalGorunum view)
    {
        var settings = new DeepSeekSettings();
        var client = new DeepSeekChatClient(new HttpClient(new NoopHandler()), settings);
        return new AiAssistantService(
            settings,
            client,
            new FakeIsletmeService
            {
                Active = new Isletme { Id = 1, Ad = "Örnek İşletme", IsAktif = true }
            },
            new FakeKasaService(),
            new FakeSummaryService(),
            new CariStub(),
            new FakeUrunHizmetService(),
            new FakeStokService(),
            new FaturaStub(),
            new FinansalGorunumStub(view),
            new UsageQuotaStub());
    }

    private static FinansalGorunum BuildFinancialView()
    {
        return new FinansalGorunum
        {
            ReferansTarihi = DateTime.Today,
            KasaBakiyesi = 50_000m,
            AcikAlacakToplami = 70_000m,
            VadesiGecmisAlacakToplami = 25_000m,
            IlkNegatifHafta = 3,
            Yogunlasma = new AlacakYogunlasmaOzeti
            {
                RiskSeviyesi = "Yuksek",
                EnBuyukCariOrani = 55m,
                IlkUcCariOrani = 90m
            },
            CariRiskleri =
            [
                new CariOdemeRitmi
                {
                    CariKartId = 10,
                    Unvan = "Örnek Market",
                    AcikAlacak = 40_000m,
                    VadesiGecmisAlacak = 25_000m,
                    EnUzunGecikmeGunu = 45,
                    OrtancaOdemeSapmasiGunu = 18m,
                    ZamanindaOdemeOrani = 25m,
                    TamamlananOdemeAdedi = 8,
                    RitimDurumu = "Yavasliyor",
                    RiskSeviyesi = "Yuksek"
                }
            ],
            NakitProjeksiyonu =
            [
                ProjectionWeek(1, 50_000m, 30_000m),
                ProjectionWeek(2, 30_000m, 5_000m),
                ProjectionWeek(3, 5_000m, -10_000m)
            ]
        };
    }

    private static NakitProjeksiyonHaftasi ProjectionWeek(int week, decimal opening, decimal closing)
    {
        var start = DateTime.Today.AddDays(week * 7 - 6);
        return new NakitProjeksiyonHaftasi
        {
            Hafta = week,
            Baslangic = start,
            Bitis = start.AddDays(6),
            AcilisBakiyesi = opening,
            KapanisBakiyesi = closing,
            NetDegisim = closing - opening
        };
    }

    private sealed class UsageQuotaStub : IAiUsageQuotaService
    {
        private static AiUsageStatus Status => new()
        {
            AiAktif = true,
            IzinVerildi = true,
            SinirsizPlan = true,
            PlanKodu = PlanKodlari.IsletmeBuyume
        };

        public Task<AiUsageStatus> GetStatusAsync(CancellationToken ct = default) => Task.FromResult(Status);
        public Task<AiUsageStatus> ConsumeAsync(CancellationToken ct = default) => Task.FromResult(Status);
    }

    private sealed class FinansalGorunumStub(FinansalGorunum view) : IFinansalGorunumService
    {
        public Task<FinansalGorunum> GetAsync(DateTime referenceDate, int projectionWeeks = 13, CancellationToken ct = default) => Task.FromResult(view);
        public Task<List<NakitPlanKalemi>> GetPlanItemsAsync(CancellationToken ct = default) => Task.FromResult(new List<NakitPlanKalemi>());
        public Task<int> CreatePlanItemAsync(NakitPlanKalemiKaydetRequest request, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> UpdatePlanItemAsync(int id, NakitPlanKalemiKaydetRequest request, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeletePlanItemAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class CariStub : ICariService
    {
        private readonly List<CariKart> _rows = [new() { Id = 10, Unvan = "Örnek Market" }];
        public Task<List<CariKart>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<CariKart?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(_rows.FirstOrDefault(x => x.Id == id));
        public Task<int> CreateAsync(CariKart cariKart, CancellationToken ct = default) => Task.FromResult(1);
        public Task UpdateAsync(CariKart cariKart, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CreateHareketAsync(CariHareket hareket, CancellationToken ct = default) => Task.FromResult(1);
        public Task<List<CariHareket>> GetHareketlerAsync(int cariKartId, CancellationToken ct = default) => Task.FromResult(new List<CariHareket>());
        public Task<decimal> GetBakiyeAsync(int cariKartId, CancellationToken ct = default) => Task.FromResult(0m);
    }

    private sealed class FaturaStub : IFaturaService
    {
        public Task<List<Fatura>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Fatura>());
        public Task<FaturaDetail?> GetDetailAsync(int id, CancellationToken ct = default) => Task.FromResult<FaturaDetail?>(null);
        public Task<FaturaTotals> CalculateTotalsAsync(IEnumerable<FaturaSatirRequest> satirlar, CancellationToken ct = default) => Task.FromResult(new FaturaTotals());
        public Task<int> CreateDraftAsync(FaturaCreateRequest request, CancellationToken ct = default) => Task.FromResult(1);
        public Task UpdateDraftAsync(int id, FaturaCreateRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkAsPortalDraftAsync(int id, string uuid, string belgeNo, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkAsIssuedAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task CancelAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
