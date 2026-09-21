using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.Extensions.Logging;

namespace CashTracker.Infrastructure.Services
{
    public sealed class AiAssistantService : IAiAssistantService
    {
        private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

        private readonly DeepSeekSettings _settings;
        private readonly DeepSeekChatClient _deepSeek;
        private readonly IIsletmeService _isletmeService;
        private readonly IKasaService _kasaService;
        private readonly ISummaryService _summaryService;
        private readonly ICariService _cariService;
        private readonly IUrunHizmetService _urunHizmetService;
        private readonly IStokService _stokService;
        private readonly IFaturaService _faturaService;
        private readonly IFinansalGorunumService _finansalGorunumService;
        private readonly IBrutKarMarjiService? _brutKarMarjiService;
        private readonly IAiUsageQuotaService _usageQuotaService;
        private readonly ILogger<AiAssistantService>? _logger;
        private readonly IAkilliKararService? _akilliKararService;

        public AiAssistantService(
            DeepSeekSettings settings,
            DeepSeekChatClient deepSeek,
            IIsletmeService isletmeService,
            IKasaService kasaService,
            ISummaryService summaryService,
            ICariService cariService,
            IUrunHizmetService urunHizmetService,
            IStokService stokService,
            IFaturaService faturaService,
            IFinansalGorunumService finansalGorunumService,
            IAiUsageQuotaService usageQuotaService,
            IBrutKarMarjiService? brutKarMarjiService = null,
            ILogger<AiAssistantService>? logger = null,
            IAkilliKararService? akilliKararService = null)
        {
            _settings = settings;
            _deepSeek = deepSeek;
            _isletmeService = isletmeService;
            _kasaService = kasaService;
            _summaryService = summaryService;
            _cariService = cariService;
            _urunHizmetService = urunHizmetService;
            _stokService = stokService;
            _faturaService = faturaService;
            _finansalGorunumService = finansalGorunumService;
            _brutKarMarjiService = brutKarMarjiService;
            _usageQuotaService = usageQuotaService;
            _logger = logger;
            _akilliKararService = akilliKararService;
        }

        public async Task<AiAssistantStatus> GetStatusAsync(CancellationToken ct = default)
        {
            return new AiAssistantStatus
            {
                Configured = _settings.IsConfigured,
                ProModel = _settings.EffectiveProModel,
                FlashModel = _settings.EffectiveFlashModel,
                BaseUrl = _settings.EffectiveBaseUrl,
                Usage = await _usageQuotaService.GetStatusAsync(ct)
            };
        }

        public async Task<AiAssistantChatResponse> ChatAsync(
            AiAssistantChatRequest request,
            CancellationToken ct = default)
        {
            var message = Normalize(request.Mesaj);
            var mode = NormalizeMode(request.Mode);
            var model = mode == "task" ? _settings.EffectiveFlashModel : _settings.EffectiveProModel;
            var usage = await _usageQuotaService.GetStatusAsync(ct);

            if (!string.IsNullOrWhiteSpace(message) && !IsBusinessScopedMessage(message))
                return BuildOutOfScopeResponse(mode, model, usage);

            if (!string.IsNullOrWhiteSpace(message))
                EnsureAiUsageAllowed(usage);

            var routing = _akilliKararService is null
                ? new AsistanYonlendirme("genel", 0, string.Empty)
                : await _akilliKararService.AsistaniYonlendirAsync(message, ct);
            if (string.Equals(routing.Niyet, "konu_disi", StringComparison.Ordinal))
                return BuildOutOfScopeResponse(mode, model, usage);

            var context = await BuildBusinessContextAsync(ct);
            var privacy = PromptPrivacyMap.Create(context);
            var suggestions = BuildRuleBasedSuggestions(context).Select(x => x.Baslik).Take(3).ToList();

            if (string.IsNullOrWhiteSpace(message))
            {
                return new AiAssistantChatResponse
                {
                    Configured = _settings.IsConfigured,
                    Mode = mode,
                    Model = model,
                    Answer = "Sorunu yaz, işletme verilerine göre kısa ve uygulanabilir bir cevap hazırlayayım.",
                    Intent = routing.Niyet,
                    ActionPath = routing.AksiyonUrl,
                    RoutingConfidence = routing.Guven,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            if (!_settings.IsConfigured)
            {
                return new AiAssistantChatResponse
                {
                    Configured = false,
                    Mode = mode,
                    Model = model,
                    Answer = BuildOfflineAnswer(message, context),
                    Intent = routing.Niyet,
                    ActionPath = routing.AksiyonUrl,
                    RoutingConfidence = routing.Guven,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            usage = await _usageQuotaService.ConsumeAsync(ct);
            EnsureAiUsageAllowed(usage);

            try
            {
                var answer = await _deepSeek.CompleteAsync(
                    model,
                    new[]
                    {
                        new DeepSeekChatMessage("system", BuildChatSystemPrompt()),
                        new DeepSeekChatMessage("user", BuildChatUserPrompt(privacy.Redact(message), context, mode, privacy, routing.Niyet))
                    },
                    mode == "task" ? 0.25 : 0.35,
                    mode == "task" ? 500 : 2000,
                    context.BusinessId.ToString(CultureInfo.InvariantCulture),
                    enableThinking: mode != "task",
                    reasoningEffort: "low",
                    ct: ct);

                return new AiAssistantChatResponse
                {
                    Configured = true,
                    Mode = mode,
                    Model = model,
                    Answer = privacy.Restore(CleanAssistantText(answer)),
                    Intent = routing.Niyet,
                    ActionPath = routing.AksiyonUrl,
                    RoutingConfidence = routing.Guven,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "AI chat request failed. BusinessId={BusinessId} Model={Model}", context.BusinessId, model);
                return new AiAssistantChatResponse
                {
                    Configured = true,
                    Mode = mode,
                    Model = model,
                    Answer = "AI yanıtı şu anda alınamadı. Yerel analizle devam ediyorum.\n\n" +
                             BuildOfflineAnswer(message, context),
                    Intent = routing.Niyet,
                    ActionPath = routing.AksiyonUrl,
                    RoutingConfidence = routing.Guven,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
        }

        public async Task<AiBusinessSuggestionsResponse> GetSuggestionsAsync(CancellationToken ct = default)
        {
            var context = await BuildBusinessContextAsync(ct);
            var suggestions = BuildRuleBasedSuggestions(context);
            if (_akilliKararService is not null)
            {
                var dailyTasks = await _akilliKararService.BugununIsleriniGetirAsync(context.BusinessId, ct);
                if (dailyTasks.Count > 0)
                {
                    suggestions = dailyTasks.Select(x => new AiBusinessSuggestion
                    {
                        Baslik = x.Baslik,
                        Aciklama = x.Aciklama,
                        Oncelik = x.Oncelik,
                        Metrik = x.Metrik,
                        Kaynak = "Bugünün işleri"
                    }).Take(3).ToList();
                }
            }
            var privacy = PromptPrivacyMap.Create(context);
            var model = _settings.EffectiveProModel;
            var usage = await _usageQuotaService.GetStatusAsync(ct);
            EnsureAiUsageAllowed(usage);

            if (!_settings.IsConfigured)
            {
                return new AiBusinessSuggestionsResponse
                {
                    Configured = false,
                    Model = model,
                    Summary = "DeepSeek API anahtarı bekleniyor. Bu arada Systemcel yerel finans kurallarıyla öneri üretiyor.",
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            usage = await _usageQuotaService.ConsumeAsync(ct);
            EnsureAiUsageAllowed(usage);

            try
            {
                var summary = await _deepSeek.CompleteAsync(
                    model,
                    new[]
                    {
                        new DeepSeekChatMessage("system", BuildSuggestionSystemPrompt()),
                        new DeepSeekChatMessage("user", BuildSuggestionUserPrompt(context, suggestions, privacy))
                    },
                    0.3,
                    700,
                    context.BusinessId.ToString(CultureInfo.InvariantCulture),
                    enableThinking: false,
                    ct: ct);

                return new AiBusinessSuggestionsResponse
                {
                    Configured = true,
                    Model = model,
                    Summary = privacy.Restore(CleanAssistantText(summary)),
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "AI suggestion request failed. BusinessId={BusinessId} Model={Model}", context.BusinessId, model);
                return new AiBusinessSuggestionsResponse
                {
                    Configured = true,
                    Model = model,
                    Summary = "AI özeti şu anda alınamadı. Yerel öneriler gösteriliyor.",
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
        }

        private static void EnsureAiUsageAllowed(AiUsageStatus usage)
        {
            if (!usage.AiAktif)
            {
                throw new EntitlementViolationException(
                    EntitlementErrorCodes.FeatureNotAvailable,
                    usage.Mesaj,
                    suggestedPlanCode: PlanKodlari.IsletmeBaslangic);
            }

            if (!usage.IzinVerildi)
            {
                var suggestedPlan = string.Equals(usage.PlanKodu, PlanKodlari.IsletmeBaslangic, StringComparison.Ordinal)
                    ? PlanKodlari.IsletmeBuyume
                    : PlanKodlari.IsletmeKurumsal;
                throw new EntitlementViolationException(
                    EntitlementErrorCodes.LimitReached,
                    usage.Mesaj,
                    EntitlementLimits.AiMessage,
                    usage.Limit,
                    usage.Kullanilan,
                    suggestedPlan);
            }
        }

        private AiAssistantChatResponse BuildOutOfScopeResponse(string mode, string model, AiUsageStatus usage)
        {
            return new AiAssistantChatResponse
            {
                Configured = _settings.IsConfigured,
                Mode = mode,
                Model = model,
                Answer = "Bu alan serbest sohbet için değil. Gelir, gider, fatura, cari, stok, tahsilat, OCR veya rapor verileriyle ilgili net bir soru yazın.",
                Usage = usage
            };
        }

        private async Task<BusinessContext> BuildBusinessContextAsync(CancellationToken ct)
        {
            var today = DateTime.Today;
            var currentFrom = today.AddDays(-29);
            var previousFrom = today.AddDays(-59);
            var previousTo = today.AddDays(-30);

            var isletme = await _isletmeService.GetActiveAsync();
            var currentSummary = await _summaryService.GetSummaryAsync(currentFrom, today);
            var previousSummary = await _summaryService.GetSummaryAsync(previousFrom, previousTo);
            var cashRows = await _kasaService.GetAllAsync(currentFrom, today);
            var invoices = await _faturaService.GetAllAsync(ct);
            var cariCards = await _cariService.GetAllAsync(ct);
            var products = await _urunHizmetService.GetAllAsync(ct);
            var financialView = await _finansalGorunumService.GetAsync(today, 13, ct);
            var grossMargin = _brutKarMarjiService is null
                ? null
                : await _brutKarMarjiService.GetAsync(currentFrom, today, ct);

            var activeProducts = products
                .Where(x => x.Aktif && string.Equals(x.Tip, "Urun", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.KritikStok)
                .Take(40)
                .ToList();

            var stockWarnings = new List<StockWarning>();
            foreach (var product in activeProducts.Where(x => x.KritikStok > 0))
            {
                var stock = await _stokService.GetCurrentStockAsync(product.Id, ct);
                if (stock <= product.KritikStok)
                {
                    stockWarnings.Add(new StockWarning(
                        product.Ad,
                        stock,
                        product.KritikStok,
                        product.Birim));
                }
            }

            return new BusinessContext
            {
                BusinessId = isletme.Id,
                BusinessName = isletme.Ad,
                Today = today,
                CurrentFrom = currentFrom,
                CurrentTo = today,
                PreviousFrom = previousFrom,
                PreviousTo = previousTo,
                CurrentSummary = currentSummary,
                PreviousSummary = previousSummary,
                ExpenseGroups = BuildCashGroups(cashRows, "Gider"),
                IncomeGroups = BuildCashGroups(cashRows, "Gelir"),
                PaymentGroups = cashRows
                    .GroupBy(x => NormalizeLabel(x.OdemeYontemi, "Belirsiz"))
                    .Select(x => new CashGroup(x.Key, x.Sum(r => r.Tutar), x.Count()))
                    .OrderByDescending(x => x.Amount)
                    .Take(6)
                    .ToList(),
                RecentTransactions = cashRows
                    .OrderByDescending(x => x.Tarih)
                    .ThenByDescending(x => x.Id)
                    .Take(8)
                    .ToList(),
                InvoiceCount = invoices.Count,
                OutstandingInvoiceTotal = invoices.Sum(GetRemainingInvoiceAmount),
                OverdueInvoices = invoices
                    .Where(x => x.VadeTarihi.HasValue &&
                                x.VadeTarihi.Value.Date < today &&
                                GetRemainingInvoiceAmount(x) > 0 &&
                                !string.Equals(x.Durum, "Iptal", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.VadeTarihi)
                    .Take(5)
                    .ToList(),
                FinancialView = financialView,
                GrossMargin = grossMargin,
                CariCount = cariCards.Count,
                CariNames = cariCards.Select(x => x.Unvan).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                ProductCount = products.Count(x => x.Aktif),
                StockWarnings = stockWarnings
                    .OrderBy(x => x.CurrentStock - x.CriticalStock)
                    .Take(8)
                    .ToList()
            };
        }

        private static List<CashGroup> BuildCashGroups(IEnumerable<Kasa> rows, string tip)
        {
            return rows
                .Where(x => string.Equals(x.Tip, tip, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => NormalizeLabel(x.Kalem, NormalizeLabel(x.GiderTuru, "Belirsiz")))
                .Select(x => new CashGroup(x.Key, x.Sum(r => r.Tutar), x.Count()))
                .OrderByDescending(x => x.Amount)
                .Take(8)
                .ToList();
        }

        private static List<AiBusinessSuggestion> BuildRuleBasedSuggestions(BusinessContext context)
        {
            var suggestions = new List<AiBusinessSuggestion>();

            if (context.CurrentSummary.ExpenseTotal > context.CurrentSummary.IncomeTotal &&
                context.CurrentSummary.ExpenseTotal > 0)
            {
                suggestions.Add(new AiBusinessSuggestion
                {
                    Baslik = "Giderler gelirleri aşıyor",
                    Aciklama = "Son 30 günde net sonuç negatif. Önce en büyük gider kaleminde satın alma, abonelik ve tekrarlı ödeme kontrolü yapın.",
                    Oncelik = "Yüksek",
                    Metrik = $"Net {FormatMoney(context.CurrentSummary.Net)}",
                    Kaynak = "Nakit akışı"
                });
            }

            var previousExpense = context.PreviousSummary.ExpenseTotal;
            if (previousExpense > 0)
            {
                var expenseChange = (context.CurrentSummary.ExpenseTotal - previousExpense) / previousExpense;
                if (expenseChange >= 0.2m)
                {
                    suggestions.Add(new AiBusinessSuggestion
                    {
                        Baslik = "Gider artışı hızlandı",
                        Aciklama = "Son 30 gün giderleri önceki 30 güne göre belirgin yükselmiş. Artışın hangi kalemden geldiğini haftalık kırılımla izleyin.",
                        Oncelik = "Yüksek",
                        Metrik = $"+{expenseChange.ToString("P0", TrCulture)}",
                        Kaynak = "Trend"
                    });
                }
            }

            var topExpense = context.ExpenseGroups.FirstOrDefault();
            if (topExpense is not null && context.CurrentSummary.ExpenseTotal > 0)
            {
                var share = topExpense.Amount / context.CurrentSummary.ExpenseTotal;
                if (share >= 0.25m)
                {
                    suggestions.Add(new AiBusinessSuggestion
                    {
                        Baslik = $"{topExpense.Name} kalemi öne çıkıyor",
                        Aciklama = "Bu kalem toplam giderin önemli bir bölümünü oluşturuyor. Tedarikçi alternatifi, limit veya onay akışı eklemek tasarruf sağlayabilir.",
                        Oncelik = share >= 0.45m ? "Yüksek" : "Normal",
                        Metrik = $"{FormatMoney(topExpense.Amount)} / {share.ToString("P0", TrCulture)}",
                        Kaynak = "Gider dağılımı"
                    });
                }
            }

            var overdueTotal = context.OverdueInvoices.Sum(GetRemainingInvoiceAmount);
            if (overdueTotal > 0)
            {
                suggestions.Add(new AiBusinessSuggestion
                {
                    Baslik = "Vadesi geçen tahsilatlar var",
                    Aciklama = "Tahsilat aksaması nakit akışını sıkıştırabilir. En eski vade tarihli faturaları önceliklendirip kısa hatırlatma akışı kurun.",
                    Oncelik = "Yüksek",
                    Metrik = FormatMoney(overdueTotal),
                    Kaynak = "Fatura"
                });
            }

            if (context.StockWarnings.Count > 0)
            {
                var first = context.StockWarnings[0];
                suggestions.Add(new AiBusinessSuggestion
                {
                    Baslik = "Kritik stok seviyeleri var",
                    Aciklama = $"{first.Name} başta olmak üzere stok seviyesi kritik eşiğe yaklaşan ürünleri kontrol edin.",
                    Oncelik = "Normal",
                    Metrik = $"{context.StockWarnings.Count} ürün",
                    Kaynak = "Stok"
                });
            }

            if (context.CurrentSummary.IncomeCount + context.CurrentSummary.ExpenseCount < 5)
            {
                suggestions.Add(new AiBusinessSuggestion
                {
                    Baslik = "Veri seti henüz zayıf",
                    Aciklama = "Daha tutarlı öneriler için gelir ve gider kayıtlarını günlük işleyin; açıklama ve kalem alanlarını boş bırakmayın.",
                    Oncelik = "Normal",
                    Metrik = $"{context.CurrentSummary.IncomeCount + context.CurrentSummary.ExpenseCount} kayıt",
                    Kaynak = "Veri kalitesi"
                });
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add(new AiBusinessSuggestion
                {
                    Baslik = "Nakit akışı dengede görünüyor",
                    Aciklama = "Mevcut veride acil risk görünmüyor. Sonraki adım olarak en karlı gelir kalemlerini ve tekrarlı giderleri birlikte izleyin.",
                    Oncelik = "Normal",
                    Metrik = $"Net {FormatMoney(context.CurrentSummary.Net)}",
                    Kaynak = "Genel analiz"
                });
            }

            return suggestions.Take(5).ToList();
        }

        private static string BuildChatSystemPrompt()
        {
            return
                "Sen Systemcel Finance Suite içinde çalışan profesyonel bir işletme finans asistanısın.\n" +
                "Yanıtlarını Türkçe ver. Cevapların kısa, net, uygulanabilir ve sayılara dayalı olsun.\n" +
                "Panel içinde okunacağı için yanıtı en fazla 5 madde ve 180 kelimeyle sınırla; tablo verme.\n" +
                "Markdown sembolleri, kalın yazı işaretleri veya kod bloğu kullanma; düz metin yaz.\n" +
                "Yalnızca verilen işletme bağlamından çıkarım yap; veri yoksa bunu açıkça söyle.\n" +
                "Köşeli parantezli anonim işletme ve cari kodlarını aynen koru; bunlar kullanıcıya gösterilmeden gerçek adlara dönüştürülecek.\n" +
                "Cari risk sorularında karar verme; açık alacak, gecikme, ödeme örneği ve veri kalitesini birlikte açıkla.\n" +
                "Nakit yeterliliği sorularında yalnız 13 haftalık projeksiyonu ve kayıtlı planları kullan; maaş veya başka plan kalemi kayıtlı değilse kesin sonuç verme.\n" +
                "Kayıt ekleme, silme veya değiştirme yetkin yok; böyle taleplerde danışmanlık ve kontrol listesi sun.\n" +
                "Vergi, hukuk veya resmi muhasebe konularında kesin hüküm verme; gerektiğinde mali müşavir kontrolü öner.\n" +
                "Gereksiz pazarlama dili kullanma.";
        }

        private static string BuildSuggestionSystemPrompt()
        {
            return
                "Sen Systemcel içinde çalışan proaktif finans analisti modusun.\n" +
                "Aşağıdaki işletme verilerine ve yerel kural önerilerine bakarak yalnızca 3 kısa madde üret.\n" +
                "Her madde tek cümle olsun, toplam yanıt 90 kelimeyi geçmesin, giriş veya sonuç paragrafı yazma.\n" +
                "Biçim: '- Başlık: uygulanabilir öneri ve metrik.'\n" +
                "Markdown kalın yazı veya kod bloğu kullanma.\n" +
                "Köşeli parantezli anonim işletme ve cari kodlarını aynen koru; bunlar kullanıcıya gösterilmeden gerçek adlara dönüştürülecek.\n" +
                "Türkçe yaz; veri yetersizse bunu belirt.";
        }

        private static string BuildChatUserPrompt(
            string message,
            BusinessContext context,
            string mode,
            PromptPrivacyMap privacy,
            string intent)
        {
            return
                $"Mod: {mode}\n" +
                "İşletme bağlamı:\n" +
                $"Odak alanı: {intent}\n" +
                BuildContextText(context, privacy, intent) +
                "\n\nKullanıcı sorusu:\n" +
                message +
                "\n\nYanıt sınırı: en fazla 180 kelime.";
        }

        private static string BuildSuggestionUserPrompt(
            BusinessContext context,
            IReadOnlyCollection<AiBusinessSuggestion> suggestions,
            PromptPrivacyMap privacy)
        {
            var sb = new StringBuilder();
            sb.AppendLine("İşletme bağlamı:");
            sb.AppendLine(BuildContextText(context, privacy));
            sb.AppendLine();
            sb.AppendLine("Yerel kural önerileri:");
            foreach (var suggestion in suggestions)
            {
                sb.Append("- ")
                    .Append(privacy.Redact(suggestion.Baslik))
                    .Append(" | ")
                    .Append(suggestion.Metrik)
                    .Append(" | ")
                    .AppendLine(privacy.Redact(suggestion.Aciklama));
            }

            return sb.ToString();
        }

        private static string BuildContextText(BusinessContext context, PromptPrivacyMap privacy, string intent = "genel")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"İşletme: {privacy.Redact(context.BusinessName)}");
            sb.AppendLine($"Bugün: {context.Today:yyyy-MM-dd}");
            sb.AppendLine($"Analiz aralığı: {context.CurrentFrom:yyyy-MM-dd} - {context.CurrentTo:yyyy-MM-dd}");
            if (intent is "genel" or "nakit" or "rapor" or "kayit")
            {
                sb.AppendLine($"Gelir: {FormatMoney(context.CurrentSummary.IncomeTotal)} ({context.CurrentSummary.IncomeCount} kayıt)");
                sb.AppendLine($"Gider: {FormatMoney(context.CurrentSummary.ExpenseTotal)} ({context.CurrentSummary.ExpenseCount} kayıt)");
                sb.AppendLine($"Net: {FormatMoney(context.CurrentSummary.Net)}");
                sb.AppendLine($"Önceki 30 gün gider: {FormatMoney(context.PreviousSummary.ExpenseTotal)}, net: {FormatMoney(context.PreviousSummary.Net)}");
                AppendGroups(sb, "En büyük gider kalemleri", context.ExpenseGroups, privacy);
                AppendGroups(sb, "En büyük gelir kalemleri", context.IncomeGroups, privacy);
                AppendGroups(sb, "Ödeme yöntemleri", context.PaymentGroups, privacy);
            }
            if (intent is "genel" or "tahsilat" or "rapor")
            {
                sb.AppendLine($"Fatura sayısı: {context.InvoiceCount}, açık fatura bakiyesi: {FormatMoney(context.OutstandingInvoiceTotal)}");
                if (context.OverdueInvoices.Count > 0)
                {
                    sb.AppendLine("Vadesi geçen faturalar:");
                    foreach (var invoice in context.OverdueInvoices)
                        sb.AppendLine($"- {privacy.Redact(invoice.YerelFaturaNo)}: {FormatMoney(GetRemainingInvoiceAmount(invoice))}, vade {invoice.VadeTarihi:yyyy-MM-dd}, durum {invoice.Durum}");
                }
            }

            if (intent is "genel" or "nakit" or "tahsilat" or "rapor")
                AppendFinancialView(sb, context.FinancialView, privacy);
            if (intent is "genel" or "rapor" && context.GrossMargin?.Guvenilir == true)
            {
                sb.AppendLine($"Son 30 gün brüt kâr: {FormatMoney(context.GrossMargin.BrutKarTry)}, marj %{context.GrossMargin.BrutKarOrani?.ToString("N1", TrCulture) ?? "—"}; KDV hariç hareketli ortalama stok maliyetiyle hesaplandı.");
            }

            if (intent is "genel" or "stok")
            {
                sb.AppendLine($"Aktif ürün/hizmet: {context.ProductCount}");
                if (context.StockWarnings.Count > 0)
                {
                    sb.AppendLine("Kritik stok uyarıları:");
                    foreach (var stock in context.StockWarnings)
                        sb.AppendLine($"- {privacy.Redact(stock.Name)}: {stock.CurrentStock.ToString("N2", TrCulture)} {stock.Unit}, kritik {stock.CriticalStock.ToString("N2", TrCulture)}");
                }
            }

            if (intent is "genel" or "nakit" or "kayit" && context.RecentTransactions.Count > 0)
            {
                sb.AppendLine("Son işlemler:");
                foreach (var row in context.RecentTransactions)
                {
                    var label = NormalizeLabel(row.Kalem, row.GiderTuru ?? "Belirsiz");
                    sb.AppendLine($"- {row.Tarih:yyyy-MM-dd} {row.Tip} {FormatMoney(row.Tutar)} | {privacy.Redact(label)} | {privacy.Redact(row.OdemeYontemi)}");
                }
            }

            return sb.ToString();
        }

        private static void AppendGroups(
            StringBuilder sb,
            string title,
            IReadOnlyCollection<CashGroup> groups,
            PromptPrivacyMap privacy)
        {
            if (groups.Count == 0)
                return;

            sb.AppendLine(title + ":");
            foreach (var group in groups)
                sb.AppendLine($"- {privacy.Redact(group.Name)}: {FormatMoney(group.Amount)} ({group.Count} kayıt)");
        }

        private static void AppendFinancialView(StringBuilder sb, FinansalGorunum view, PromptPrivacyMap privacy)
        {
            sb.AppendLine("Finansal görünüm:");
            sb.AppendLine($"- Kasa bakiyesi: {FormatMoney(view.KasaBakiyesi)}");
            sb.AppendLine($"- Açık alacak: {FormatMoney(view.AcikAlacakToplami)}, vadesi geçmiş: {FormatMoney(view.VadesiGecmisAlacakToplami)}");
            sb.AppendLine($"- Alacak yoğunlaşması: {view.Yogunlasma.RiskSeviyesi}; en büyük cari %{view.Yogunlasma.EnBuyukCariOrani.ToString("N1", TrCulture)}, ilk üç cari %{view.Yogunlasma.IlkUcCariOrani.ToString("N1", TrCulture)}");

            if (view.CariRiskleri.Count > 0)
            {
                sb.AppendLine("Cari ödeme ritmi ve riskleri:");
                foreach (var customer in view.CariRiskleri.Take(10))
                {
                    var medianDelay = customer.OrtancaOdemeSapmasiGunu.HasValue
                        ? $", ortanca vade sapması {customer.OrtancaOdemeSapmasiGunu.Value.ToString("N1", TrCulture)} gün"
                        : ", ödeme ritmi için örnek yetersiz";
                    var onTimeRate = customer.ZamanindaOdemeOrani.HasValue
                        ? $", zamanında ödeme %{customer.ZamanindaOdemeOrani.Value.ToString("N0", TrCulture)}"
                        : string.Empty;
                    sb.AppendLine($"- {privacy.Redact(customer.Unvan)}: risk {customer.RiskSeviyesi}, ritim {customer.RitimDurumu}, açık {FormatMoney(customer.AcikAlacak)}, gecikmiş {FormatMoney(customer.VadesiGecmisAlacak)}, en uzun gecikme {customer.EnUzunGecikmeGunu} gün{medianDelay}{onTimeRate}, tamamlanan ödeme {customer.TamamlananOdemeAdedi}");
                }
            }

            if (view.NakitProjeksiyonu.Count > 0)
            {
                sb.AppendLine($"13 haftalık nakit projeksiyonu; ilk negatif hafta: {(view.IlkNegatifHafta.HasValue ? view.IlkNegatifHafta.Value.ToString(TrCulture) : "yok")}");
                foreach (var week in view.NakitProjeksiyonu.Take(13))
                {
                    sb.AppendLine($"- Hafta {week.Hafta} ({week.Baslangic:yyyy-MM-dd}/{week.Bitis:yyyy-MM-dd}): açılış {FormatMoney(week.AcilisBakiyesi)}, tahsilat {FormatMoney(week.BeklenenTahsilat + week.PlanlananGelir)}, ödeme {FormatMoney(week.BeklenenOdeme + week.PlanlananGider)}, kapanış {FormatMoney(week.KapanisBakiyesi)}");
                }
            }

            if (view.VeriUyarilari.Count > 0)
            {
                sb.AppendLine("Finansal veri uyarıları:");
                foreach (var warning in view.VeriUyarilari.Take(8))
                    sb.AppendLine($"- {warning.Mesaj} ({warning.KayitAdedi} kayıt)");
            }
        }

        private static string BuildOfflineAnswer(string message, BusinessContext context)
        {
            var normalizedMessage = message.ToLower(TrCulture);
            if ((normalizedMessage.Contains("kim") && (normalizedMessage.Contains("geç öd") || normalizedMessage.Contains("gec od"))) ||
                normalizedMessage.Contains("sürekli geç") || normalizedMessage.Contains("surekli gec"))
            {
                var delayed = context.FinancialView.CariRiskleri
                    .Where(x => x.VadesiGecmisAlacak > 0 || (x.OrtancaOdemeSapmasiGunu ?? 0) > 0)
                    .Take(3)
                    .ToList();
                if (delayed.Count == 0)
                    return "Sürekli geç ödeyen bir cari göstermek için yeterli tamamlanmış ödeme ve gecikmiş alacak verisi yok.";

                var lines = delayed.Select(x =>
                    $"{x.Unvan}: {FormatMoney(x.VadesiGecmisAlacak)} gecikmiş alacak, en uzun gecikme {x.EnUzunGecikmeGunu} gün, risk {x.RiskSeviyesi}.");
                return string.Join(Environment.NewLine, lines) + Environment.NewLine +
                       "Karar vermeden önce örnek sayısını ve son ödeme tarihlerini cari detayından kontrol edin.";
            }

            if ((normalizedMessage.Contains("maaş") || normalizedMessage.Contains("maas")) &&
                (normalizedMessage.Contains("kasa") || normalizedMessage.Contains("yeter")))
            {
                var projection = context.FinancialView.NakitProjeksiyonu;
                if (projection.Count == 0)
                    return "Maaş gününe kadar kasa yeterliliğini hesaplamak için nakit projeksiyonu verisi yok.";

                var minimum = projection.MinBy(x => x.KapanisBakiyesi)!;
                var status = context.FinancialView.IlkNegatifHafta.HasValue
                    ? $"Projeksiyon {context.FinancialView.IlkNegatifHafta}. haftada negatife dönüyor."
                    : "13 haftalık projeksiyonda negatif kapanış görünmüyor.";
                return $"{status} En düşük haftalık kapanış {minimum.Hafta}. haftada {FormatMoney(minimum.KapanisBakiyesi)}. " +
                       "Maaş tutarı ve tarihi nakit planına ekli değilse bu sonuç maaş ödemesini kapsamaz.";
            }

            if ((normalizedMessage.Contains("mal vere") || normalizedMessage.Contains("satış yap") || normalizedMessage.Contains("satis yap")) &&
                context.FinancialView.CariRiskleri.Count > 0)
            {
                var customer = context.FinancialView.CariRiskleri.FirstOrDefault(x =>
                    normalizedMessage.Contains(x.Unvan.ToLower(TrCulture)));
                if (customer is null)
                    return "Cari riskini değerlendirebilmem için müşteri unvanını soruya ekleyin.";

                return $"{customer.Unvan} için açık alacak {FormatMoney(customer.AcikAlacak)}, gecikmiş alacak {FormatMoney(customer.VadesiGecmisAlacak)}, en uzun gecikme {customer.EnUzunGecikmeGunu} gün ve risk seviyesi {customer.RiskSeviyesi}. " +
                       $"Bu veri tek başına satış kararı değildir; limit, teminat ve sipariş tutarıyla birlikte değerlendirin. Tamamlanan ödeme örneği: {customer.TamamlananOdemeAdedi}.";
            }

            var topExpense = context.ExpenseGroups.FirstOrDefault();
            var sb = new StringBuilder();
            sb.AppendLine($"Son 30 günde gelir {FormatMoney(context.CurrentSummary.IncomeTotal)}, gider {FormatMoney(context.CurrentSummary.ExpenseTotal)}, net {FormatMoney(context.CurrentSummary.Net)}.");
            if (topExpense is not null)
                sb.AppendLine($"En büyük gider kalemi {topExpense.Name}: {FormatMoney(topExpense.Amount)}.");
            sb.AppendLine("Hızlı aksiyon: en büyük gider kalemini haftalık limite bağlayın, vadesi geçen alacakları önceleyin ve açıklamasız kayıtları tamamlayın.");
            if (message.Contains("gider", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine("Gider azaltma için ilk bakılacak yerler: tekrarlı abonelikler, düşük hacimli tedarikçi alımları ve plansız stok yenilemeleri.");
            return sb.ToString().Trim();
        }

        private static string NormalizeMode(string? mode)
        {
            var raw = Normalize(mode).ToLowerInvariant();
            return raw is "task" or "gorev" or "görev" ? "task" : "chat";
        }

        private static bool IsBusinessScopedMessage(string message)
        {
            var raw = Normalize(message);
            if (string.IsNullOrWhiteSpace(raw))
                return true;

            var lower = raw.ToLower(TrCulture);
            string[] outsideScope =
            [
                "sen kimsin", "seni kim", "kim yaratt", "kim gelişt", "kim gelist",
                "deepseek misin", "chatgpt misin", "hangi yapay zeka", "yapay zeka model",
                "hangi dil modeli", "hangi sağlayıcı", "hangi saglayici", "hangi provider",
                "modelin ne", "modelinin adı", "modelinin adi", "sistem prompt",
                "system prompt", "talimatlarını", "talimatlarini", "ignore previous instructions",
                "naber", "nasılsın", "nasilsin", "şaka yap", "saka yap",
                "hikaye yaz", "öykü yaz", "oyku yaz", "şiir yaz", "siir yaz",
                "film öner", "film oner", "müzik öner", "muzik oner", "futbol maçı",
                "futbol maci", "hava durumu", "kod yaz"
            ];

            if (outsideScope.Any(lower.Contains))
                return false;

            string[] businessTerms =
            [
                "gelir", "gider", "masraf", "maliyet", "kâr", "karlılı", "kar marj",
                "kar zarar", "ciro", "nakit", "stok", "ürün", "urun", "fatura",
                "cari", "tahsilat", "ödeme", "odeme", "rapor", "bakiye",
                "borç", "borc", "alacak", "kasa", "vergi", "kdv", "ocr",
                "fiş", "fis", "dekont", "geç öd", "gec od", "maaş", "maas",
                "tedarik", "mal ver", "satış", "satis", "finans"
            ];

            var firstQuestion = lower.Split('?', 2)[0];
            return businessTerms.Any(term =>
                Regex.IsMatch(firstQuestion, $@"(?<!\p{{L}}){Regex.Escape(term)}", RegexOptions.CultureInvariant));
        }

        private static string CleanAssistantText(string value)
        {
            return Normalize(value)
                .Replace("**", string.Empty, StringComparison.Ordinal)
                .Replace("__", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        private static string Normalize(string? value, string fallback = "")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string NormalizeLabel(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static decimal GetRemainingInvoiceAmount(Fatura invoice)
        {
            return Math.Max(0m, invoice.GenelToplam - invoice.OdenenTutar);
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("C0", TrCulture);
        }

        private sealed class BusinessContext
        {
            public int BusinessId { get; set; }
            public string BusinessName { get; set; } = string.Empty;
            public DateTime Today { get; set; }
            public DateTime CurrentFrom { get; set; }
            public DateTime CurrentTo { get; set; }
            public DateTime PreviousFrom { get; set; }
            public DateTime PreviousTo { get; set; }
            public PeriodSummary CurrentSummary { get; set; } = new();
            public PeriodSummary PreviousSummary { get; set; } = new();
            public List<CashGroup> ExpenseGroups { get; set; } = [];
            public List<CashGroup> IncomeGroups { get; set; } = [];
            public List<CashGroup> PaymentGroups { get; set; } = [];
            public List<Kasa> RecentTransactions { get; set; } = [];
            public int InvoiceCount { get; set; }
            public decimal OutstandingInvoiceTotal { get; set; }
            public List<Fatura> OverdueInvoices { get; set; } = [];
            public FinansalGorunum FinancialView { get; set; } = new();
            public BrutKarMarjiOzeti? GrossMargin { get; set; }
            public int CariCount { get; set; }
            public List<string> CariNames { get; set; } = [];
            public int ProductCount { get; set; }
            public List<StockWarning> StockWarnings { get; set; } = [];
        }

        private sealed class PromptPrivacyMap
        {
            private static readonly Regex EmailPattern = new(
                @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            private static readonly Regex IbanPattern = new(
                @"\bTR(?:\s?\d){24}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            private readonly List<KeyValuePair<string, string>> _replacements = [];

            public static PromptPrivacyMap Create(BusinessContext context)
            {
                var map = new PromptPrivacyMap();
                map.Add(context.BusinessName, "[ISLETME]");

                var index = 1;
                foreach (var name in context.CariNames)
                    map.Add(name, $"[CARI_{index++}]");

                index = 1;
                foreach (var invoice in context.OverdueInvoices)
                    map.Add(invoice.YerelFaturaNo, $"[FATURA_{index++}]");

                index = 1;
                foreach (var stock in context.StockWarnings)
                    map.Add(stock.Name, $"[URUN_{index++}]");

                index = 1;
                foreach (var label in context.ExpenseGroups.Select(x => x.Name)
                             .Concat(context.IncomeGroups.Select(x => x.Name))
                             .Concat(context.RecentTransactions.Select(x => NormalizeLabel(x.Kalem, x.GiderTuru ?? "Belirsiz")))
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    map.Add(label, $"[KALEM_{index++}]");
                }

                return map;
            }

            public string Redact(string? value)
            {
                var result = value ?? string.Empty;
                foreach (var replacement in _replacements.OrderByDescending(x => x.Key.Length))
                    result = result.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
                result = EmailPattern.Replace(result, "[EPOSTA]");
                return IbanPattern.Replace(result, "[IBAN]");
            }

            public string Restore(string value)
            {
                var result = value;
                foreach (var replacement in _replacements)
                {
                    result = result.Replace(replacement.Value, replacement.Key, StringComparison.OrdinalIgnoreCase);

                    if (!replacement.Value.StartsWith("[CARI_", StringComparison.Ordinal) ||
                        !replacement.Value.EndsWith(']'))
                    {
                        continue;
                    }

                    // Models sometimes normalize the privacy token while composing prose
                    // (for example "cari 1" or "CARI-1"). Restore those variants too so
                    // internal aliases never become user-facing counterparty names.
                    var aliasNumber = replacement.Value.AsSpan(6, replacement.Value.Length - 7).ToString();
                    result = Regex.Replace(
                        result,
                        $@"(?<![\p{{L}}\p{{N}}_])\[?cari[\s_-]*{Regex.Escape(aliasNumber)}\]?(?![\p{{L}}\p{{N}}_])",
                        _ => replacement.Key,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                return result;
            }

            private void Add(string? original, string alias)
            {
                var normalized = original?.Trim();
                if (string.IsNullOrWhiteSpace(normalized) ||
                    _replacements.Any(x => string.Equals(x.Key, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
                _replacements.Add(new KeyValuePair<string, string>(normalized, alias));
            }
        }

        private sealed record CashGroup(string Name, decimal Amount, int Count);

        private sealed record StockWarning(string Name, decimal CurrentStock, decimal CriticalStock, string Unit);
    }
}
