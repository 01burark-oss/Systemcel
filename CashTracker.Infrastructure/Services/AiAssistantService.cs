using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

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
        private readonly IAiUsageQuotaService _usageQuotaService;

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
            IAiUsageQuotaService usageQuotaService)
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
            _usageQuotaService = usageQuotaService;
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
            var context = await BuildBusinessContextAsync(ct);
            var suggestions = BuildRuleBasedSuggestions(context).Select(x => x.Baslik).Take(3).ToList();

            if (string.IsNullOrWhiteSpace(message))
            {
                return new AiAssistantChatResponse
                {
                    Configured = _settings.IsConfigured,
                    Mode = mode,
                    Model = model,
                    Answer = "Sorunu yaz, işletme verilerine göre kısa ve uygulanabilir bir cevap hazırlayayım.",
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            if (!IsBusinessScopedMessage(message))
            {
                return new AiAssistantChatResponse
                {
                    Configured = _settings.IsConfigured,
                    Mode = mode,
                    Model = model,
                    Answer = "Bu alan serbest sohbet için değil. Gelir, gider, fatura, cari, stok, tahsilat, OCR veya rapor verileriyle ilgili net bir soru yazın.",
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
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            usage = await _usageQuotaService.ConsumeAsync(ct);
            if (!usage.IzinVerildi)
            {
                return new AiAssistantChatResponse
                {
                    Configured = true,
                    Mode = mode,
                    Model = model,
                    Answer = usage.Mesaj,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            try
            {
                var answer = await _deepSeek.CompleteAsync(
                    model,
                    new[]
                    {
                        new DeepSeekChatMessage("system", BuildChatSystemPrompt()),
                        new DeepSeekChatMessage("user", BuildChatUserPrompt(message, context, mode))
                    },
                    mode == "task" ? 0.25 : 0.35,
                    mode == "task" ? 1200 : 2200,
                    ct);

                return new AiAssistantChatResponse
                {
                    Configured = true,
                    Mode = mode,
                    Model = model,
                    Answer = CleanAssistantText(answer),
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
            catch (Exception ex)
            {
                return new AiAssistantChatResponse
                {
                    Configured = true,
                    Mode = mode,
                    Model = model,
                    Answer = "DeepSeek yanıtı alınamadı. Şimdilik yerel analizle devam ediyorum.\n\n" +
                             BuildOfflineAnswer(message, context) +
                             $"\n\nTeknik durum: {ex.Message}",
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
        }

        public async Task<AiBusinessSuggestionsResponse> GetSuggestionsAsync(CancellationToken ct = default)
        {
            var context = await BuildBusinessContextAsync(ct);
            var suggestions = BuildRuleBasedSuggestions(context);
            var model = _settings.EffectiveProModel;
            var usage = await _usageQuotaService.GetStatusAsync(ct);

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
            if (!usage.IzinVerildi)
            {
                return new AiBusinessSuggestionsResponse
                {
                    Configured = true,
                    Model = model,
                    Summary = usage.Mesaj,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }

            try
            {
                var summary = await _deepSeek.CompleteAsync(
                    model,
                    new[]
                    {
                        new DeepSeekChatMessage("system", BuildSuggestionSystemPrompt()),
                        new DeepSeekChatMessage("user", BuildSuggestionUserPrompt(context, suggestions))
                    },
                    0.3,
                    2200,
                    ct);

                return new AiBusinessSuggestionsResponse
                {
                    Configured = true,
                    Model = model,
                    Summary = CleanAssistantText(summary),
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
            catch (Exception ex)
            {
                return new AiBusinessSuggestionsResponse
                {
                    Configured = true,
                    Model = model,
                    Summary = "DeepSeek öneri özeti alınamadı; yerel öneriler gösteriliyor. Teknik durum: " + ex.Message,
                    Suggestions = suggestions,
                    Usage = usage
                };
            }
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
                CariCount = cariCards.Count,
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
                "Türkçe yaz; veri yetersizse bunu belirt.";
        }

        private static string BuildChatUserPrompt(string message, BusinessContext context, string mode)
        {
            return
                $"Mod: {mode}\n" +
                "İşletme bağlamı:\n" +
                BuildContextText(context) +
                "\n\nKullanıcı sorusu:\n" +
                message +
                "\n\nYanıt sınırı: en fazla 180 kelime.";
        }

        private static string BuildSuggestionUserPrompt(
            BusinessContext context,
            IReadOnlyCollection<AiBusinessSuggestion> suggestions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("İşletme bağlamı:");
            sb.AppendLine(BuildContextText(context));
            sb.AppendLine();
            sb.AppendLine("Yerel kural önerileri:");
            foreach (var suggestion in suggestions)
            {
                sb.Append("- ")
                    .Append(suggestion.Baslik)
                    .Append(" | ")
                    .Append(suggestion.Metrik)
                    .Append(" | ")
                    .AppendLine(suggestion.Aciklama);
            }

            return sb.ToString();
        }

        private static string BuildContextText(BusinessContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"İşletme: {context.BusinessName}");
            sb.AppendLine($"Bugün: {context.Today:yyyy-MM-dd}");
            sb.AppendLine($"Analiz aralığı: {context.CurrentFrom:yyyy-MM-dd} - {context.CurrentTo:yyyy-MM-dd}");
            sb.AppendLine($"Gelir: {FormatMoney(context.CurrentSummary.IncomeTotal)} ({context.CurrentSummary.IncomeCount} kayıt)");
            sb.AppendLine($"Gider: {FormatMoney(context.CurrentSummary.ExpenseTotal)} ({context.CurrentSummary.ExpenseCount} kayıt)");
            sb.AppendLine($"Net: {FormatMoney(context.CurrentSummary.Net)}");
            sb.AppendLine($"Önceki 30 gün gider: {FormatMoney(context.PreviousSummary.ExpenseTotal)}, net: {FormatMoney(context.PreviousSummary.Net)}");
            AppendGroups(sb, "En büyük gider kalemleri", context.ExpenseGroups);
            AppendGroups(sb, "En büyük gelir kalemleri", context.IncomeGroups);
            AppendGroups(sb, "Ödeme yöntemleri", context.PaymentGroups);
            sb.AppendLine($"Fatura sayısı: {context.InvoiceCount}, açık fatura bakiyesi: {FormatMoney(context.OutstandingInvoiceTotal)}");
            if (context.OverdueInvoices.Count > 0)
            {
                sb.AppendLine("Vadesi geçen faturalar:");
                foreach (var invoice in context.OverdueInvoices)
                {
                    sb.AppendLine($"- {invoice.YerelFaturaNo}: {FormatMoney(GetRemainingInvoiceAmount(invoice))}, vade {invoice.VadeTarihi:yyyy-MM-dd}, durum {invoice.Durum}");
                }
            }

            sb.AppendLine($"Cari kart: {context.CariCount}, aktif ürün/hizmet: {context.ProductCount}");
            if (context.StockWarnings.Count > 0)
            {
                sb.AppendLine("Kritik stok uyarıları:");
                foreach (var stock in context.StockWarnings)
                    sb.AppendLine($"- {stock.Name}: {stock.CurrentStock.ToString("N2", TrCulture)} {stock.Unit}, kritik {stock.CriticalStock.ToString("N2", TrCulture)}");
            }

            if (context.RecentTransactions.Count > 0)
            {
                sb.AppendLine("Son işlemler:");
                foreach (var row in context.RecentTransactions)
                {
                    sb.AppendLine($"- {row.Tarih:yyyy-MM-dd} {row.Tip} {FormatMoney(row.Tutar)} | {NormalizeLabel(row.Kalem, row.GiderTuru ?? "Belirsiz")} | {row.OdemeYontemi}");
                }
            }

            return sb.ToString();
        }

        private static void AppendGroups(StringBuilder sb, string title, IReadOnlyCollection<CashGroup> groups)
        {
            if (groups.Count == 0)
                return;

            sb.AppendLine(title + ":");
            foreach (var group in groups)
                sb.AppendLine($"- {group.Name}: {FormatMoney(group.Amount)} ({group.Count} kayıt)");
        }

        private static string BuildOfflineAnswer(string message, BusinessContext context)
        {
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
            string[] allowed =
            [
                "gelir", "gider", "masraf", "maliyet", "kar", "kâr", "ciro", "nakit",
                "stok", "ürün", "urun", "fatura", "cari", "tahsilat", "ödeme", "odeme",
                "rapor", "işletme", "isletme", "kalem", "bakiye", "borç", "borc",
                "alacak", "kasa", "vergi", "kdv", "ocr", "fiş", "fis", "dekont",
                "öner", "oner", "analiz", "durum", "risk", "tasarruf", "düşür",
                "dusur", "artır", "artir", "ne yap", "iyi mi", "kötü", "kotu",
                "batıyor", "batiyor"
            ];

            if (allowed.Any(lower.Contains))
                return true;

            string[] blocked =
            [
                "merhaba", "selam", "naber", "nasılsın", "nasilsin", "şaka", "saka",
                "hikaye", "şiir", "siir", "film", "müzik", "muzik", "oyun", "futbol",
                "hava durumu", "kod yaz"
            ];

            return !blocked.Any(lower.Contains) && lower.Length >= 24 && lower.Contains('?');
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
            public int CariCount { get; set; }
            public int ProductCount { get; set; }
            public List<StockWarning> StockWarnings { get; set; } = [];
        }

        private sealed record CashGroup(string Name, decimal Amount, int Count);

        private sealed record StockWarning(string Name, decimal CurrentStock, decimal CriticalStock, string Unit);
    }
}
