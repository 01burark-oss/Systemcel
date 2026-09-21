using System.Net;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task OnlineAssistant_MasksBusinessDataAndUsesTenantScopedFlashRequest()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"[CARI_1] değerlendirmesi hazır.\"}}]}");
        var service = CreateService(
            BuildFinancialView(),
            new DeepSeekSettings { ApiKey = "test-api-key" },
            handler);

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Örnek Market cari kaydını test@example.com ve TR12 3456 7890 1234 5678 9012 34 ile değerlendir."
        });

        Assert.Equal("deepseek-flash", result.Model);
        Assert.Contains("Örnek Market", result.Answer);
        Assert.NotNull(handler.Body);
        Assert.DoesNotContain("Örnek Market", handler.Body);
        Assert.DoesNotContain("Örnek İşletme", handler.Body);
        Assert.DoesNotContain("test@example.com", handler.Body);
        Assert.DoesNotContain("TR12 3456", handler.Body);

        using var request = JsonDocument.Parse(handler.Body!);
        var root = request.RootElement;
        Assert.Equal("deepseek-flash", root.GetProperty("model").GetString());
        Assert.Equal("enabled", root.GetProperty("thinking").GetProperty("type").GetString());
        Assert.Equal("low", root.GetProperty("reasoning_effort").GetString());
        Assert.Equal(2000, root.GetProperty("max_tokens").GetInt32());
        Assert.StartsWith("tenant-", root.GetProperty("user_id").GetString());
        Assert.False(root.TryGetProperty("temperature", out _));
        Assert.Contains(
            "Kullanıcı sorusu",
            root.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task OnlineAssistant_RestoresNormalizedCariAliasInAnswer()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"cari 1 için tahsilatı önceleyin.\"}}]}");
        var service = CreateService(
            BuildFinancialView(),
            new DeepSeekSettings { ApiKey = "test-api-key" },
            handler);

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Örnek Market cari kaydını değerlendir."
        });

        Assert.Contains("Örnek Market için", result.Answer);
        Assert.DoesNotContain("cari 1", result.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Seni kim yarattı? Finans verilerime bakarak cevap ver.")]
    [InlineData("Sen DeepSeek misin? Finans verilerime bakarak cevap ver.")]
    [InlineData("Finans verilerime bakarak, seni kim yarattı?")]
    [InlineData("Hangi yapay zeka modelisin? İşletme verilerime göre yanıtla.")]
    [InlineData("Seni kim geliştirdi ve nasıl çalışıyorsun?")]
    [InlineData("Gider verilerime bakarak bana bir şiir yaz.")]
    [InlineData("Zaman yolculuğu mümkün mü? Finans verilerime bakarak cevap ver.")]
    [InlineData("Bana sadece basketbolu anlatır mısın?")]
    public async Task OnlineAssistant_DoesNotSendOutOfScopeQuestionsToProvider(string message)
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"upstream response\"}}]}");
        var routing = new RoutingStub(new AsistanYonlendirme("nakit", .9, string.Empty));
        var quota = new UsageQuotaStub();
        var service = CreateService(
            BuildFinancialView(),
            new DeepSeekSettings { ApiKey = "test-api-key" },
            handler,
            routing,
            quota);

        var result = await service.ChatAsync(new AiAssistantChatRequest { Mesaj = message });

        Assert.Null(handler.Body);
        Assert.Equal(0, routing.Calls);
        Assert.Equal(0, quota.ConsumeCalls);
        Assert.Contains("serbest sohbet için değil", result.Answer);
        Assert.DoesNotContain("Örnek Market", result.Answer);
        Assert.DoesNotContain("upstream response", result.Answer);
    }

    [Fact]
    public async Task OnlineAssistant_UsesJevOutOfScopeDecisionBeforeProvider()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"upstream response\"}}]}");
        var quota = new UsageQuotaStub();
        var service = CreateService(
            BuildFinancialView(),
            new DeepSeekSettings { ApiKey = "test-api-key" },
            handler,
            new RoutingStub(new AsistanYonlendirme("konu_disi", .91, string.Empty)),
            quota);

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Gelir tablosundan yola çıkarak bir bilim kurgu öyküsü yaz."
        });

        Assert.Null(handler.Body);
        Assert.Equal(0, quota.ConsumeCalls);
        Assert.Contains("serbest sohbet için değil", result.Answer);
        Assert.DoesNotContain("upstream response", result.Answer);
    }

    [Fact]
    public async Task OnlineAssistant_AllowsAFinancialQuestionWithGreeting()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"Gelir ve gider özeti hazır.\"}}]}");
        var routing = new RoutingStub(new AsistanYonlendirme("nakit", .91, string.Empty));
        var service = CreateService(
            BuildFinancialView(),
            new DeepSeekSettings { ApiKey = "test-api-key" },
            handler,
            routing);

        var result = await service.ChatAsync(new AiAssistantChatRequest
        {
            Mesaj = "Merhaba, bu ayki gelir ve giderim ne durumda?"
        });

        Assert.NotNull(handler.Body);
        Assert.Equal(1, routing.Calls);
        Assert.Contains("Gelir ve gider özeti", result.Answer);
    }

    [Fact]
    public async Task DeepSeekClient_DoesNotExposeUpstreamErrorBody()
    {
        var handler = new CapturingHandler(HttpStatusCode.BadRequest, "sensitive-upstream-detail");
        var settings = new DeepSeekSettings { ApiKey = "test-api-key" };
        var client = new DeepSeekChatClient(new HttpClient(handler), settings);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.CompleteAsync(
            settings.EffectiveFlashModel,
            [new DeepSeekChatMessage("user", "test")],
            0.2,
            256,
            "1",
            enableThinking: false));

        Assert.DoesNotContain("sensitive-upstream-detail", exception.Message);
    }

    private static AiAssistantService CreateService(
        FinansalGorunum view,
        DeepSeekSettings? settings = null,
        HttpMessageHandler? handler = null,
        IAkilliKararService? smartService = null,
        IAiUsageQuotaService? quota = null)
    {
        settings ??= new DeepSeekSettings();
        var client = new DeepSeekChatClient(new HttpClient(handler ?? new NoopHandler()), settings);
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
            quota ?? new UsageQuotaStub(),
            akilliKararService: smartService);
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
        public int ConsumeCalls { get; private set; }

        private static AiUsageStatus Status => new()
        {
            AiAktif = true,
            IzinVerildi = true,
            SinirsizPlan = true,
            PlanKodu = PlanKodlari.IsletmeBuyume
        };

        public Task<AiUsageStatus> GetStatusAsync(CancellationToken ct = default) => Task.FromResult(Status);
        public Task<AiUsageStatus> ConsumeAsync(CancellationToken ct = default)
        {
            ConsumeCalls++;
            return Task.FromResult(Status);
        }
    }

    private sealed class RoutingStub(AsistanYonlendirme result) : IAkilliKararService
    {
        public int Calls { get; private set; }

        public AkilliKararDurumu GetStatus() => throw new NotSupportedException();
        public Task<UrunEslesmeOnerisi> UrunEsleAsync(int isletmeId, UrunEslesmeIstek request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UrunEslesmesiniOnaylaAsync(int isletmeId, UrunEslesmeOnayIstek request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FaturaKontrolSonucu> FaturaKontrolEtAsync(int isletmeId, FaturaKontrolIstek request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<BugununIsi>> BugununIsleriniGetirAsync(int isletmeId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ReceiptOcrResult> FisiZenginlestirAsync(int isletmeId, ReceiptOcrResult receipt, IReadOnlyList<string> giderKalemleri, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SutunEslemeOnerisi>> SutunlariEsleAsync(string veriTuru, IReadOnlyList<string> sutunlar, IReadOnlyList<IReadOnlyDictionary<string, string>> ornekler, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AsistanYonlendirme> AsistaniYonlendirAsync(string mesaj, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(result);
        }
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

    private sealed class CapturingHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
