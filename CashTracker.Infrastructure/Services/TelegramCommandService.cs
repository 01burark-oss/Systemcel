using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramCommandService
    {
        private readonly TelegramBotService _telegram;
        private readonly TelegramSettings _settings;
        private readonly IKasaService _kasaService;
        private readonly IKalemTanimiService _kalemTanimiService;
        private readonly ISummaryService _summaryService;
        private readonly IIsletmeService _isletmeService;
        private readonly IAppSecurityService _appSecurityService;
        private readonly BackupReportService _backupReport;
        private readonly ITelegramApprovalService _telegramApprovalService;
        private readonly IReceiptOcrService _receiptOcrService;
        private readonly ITelegramReceiptSessionStore _receiptSessionStore;
        private readonly ReceiptOcrSettings _receiptOcrSettings;
        private readonly IUrunHizmetService _urunHizmetService;
        private readonly IStokService _stokService;
        private readonly IBarcodeReaderService _barcodeReaderService;
        private readonly ITelegramStockSessionStore _stockSessionStore;
        private readonly ITelegramPairingService? _telegramPairingService;

        public TelegramCommandService(
            TelegramBotService telegram,
            TelegramSettings settings,
            IKasaService kasaService,
            IKalemTanimiService kalemTanimiService,
            ISummaryService summaryService,
            IIsletmeService isletmeService,
            IAppSecurityService appSecurityService,
            BackupReportService backupReport,
            ITelegramApprovalService telegramApprovalService,
            IReceiptOcrService receiptOcrService,
            ITelegramReceiptSessionStore receiptSessionStore,
            ReceiptOcrSettings receiptOcrSettings,
            IUrunHizmetService urunHizmetService,
            IStokService stokService,
            IBarcodeReaderService barcodeReaderService,
            ITelegramStockSessionStore stockSessionStore,
            ITelegramPairingService? telegramPairingService = null)
        {
            _telegram = telegram;
            _settings = settings;
            _kasaService = kasaService;
            _kalemTanimiService = kalemTanimiService;
            _summaryService = summaryService;
            _isletmeService = isletmeService;
            _appSecurityService = appSecurityService;
            _backupReport = backupReport;
            _telegramApprovalService = telegramApprovalService;
            _receiptOcrService = receiptOcrService;
            _receiptSessionStore = receiptSessionStore;
            _receiptOcrSettings = receiptOcrSettings;
            _urunHizmetService = urunHizmetService;
            _stokService = stokService;
            _barcodeReaderService = barcodeReaderService;
            _stockSessionStore = stockSessionStore;
            _telegramPairingService = telegramPairingService;
        }

        public async Task ProcessUpdateAsync(TelegramUpdate update, CancellationToken ct = default)
        {
            if (!_settings.HasBotToken || !_settings.EnableCommands)
                return;

            var text = (update.Text ?? string.Empty).Trim();
            if (TryHandlePairingStart(update, text, out var pairingMessage))
            {
                await _telegram.SendTextAsync(ToChatId(update.ChatId), pairingMessage, ct);
                return;
            }

            if (!_settings.IsTargetChat(update.ChatId))
            {
                if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    await _telegram.SendTextAsync(
                        ToChatId(update.ChatId),
                        "Systemcel bağlantısı için uygulamadaki Ayarlar > Telegram ekranından yeni bağlantı kodu al.",
                        ct);
                }

                return;
            }

            if (!_settings.IsAllowedUser(update.UserId))
            {
                await _telegram.SendTextAsync(ToChatId(update.ChatId), "Bu kullanıcı yetkili değil.", ct);
                return;
            }

            if (!update.UserId.HasValue)
                return;

            var chatId = update.ChatId;
            var userId = update.UserId.Value;
            var receiptSession = await _receiptSessionStore.GetAsync(chatId, userId, ct);
            var stockSession = await _stockSessionStore.GetAsync(chatId, userId, ct);

            if (update.HasPhoto)
            {
                if (stockSession != null)
                {
                    await _telegram.SendTextAsync(
                        ToChatId(chatId),
                        "Devam eden bir stok oturumu var. Önce onu tamamla veya `iptal` yaz.",
                        ct);
                    return;
                }

                if (TryParseStockCaption(update.Caption, out var stockCaptionArgs))
                {
                    if (receiptSession != null)
                    {
                        await _telegram.SendTextAsync(
                            ToChatId(chatId),
                            "Devam eden bir fiş oturumu var. Önce onu tamamla veya `iptal` yaz.",
                            ct);
                        return;
                    }

                    await StartStockPhotoCommandAsync(update, stockCaptionArgs, ct);
                    return;
                }

                if (receiptSession != null)
                {
                    await _telegram.SendTextAsync(
                        ToChatId(chatId),
                        "Devam eden bir fiş oturumu var. Önce onu tamamla veya `iptal` yaz.",
                        ct);
                    return;
                }

                await StartReceiptSessionAsync(update, ct);
                return;
            }

            if (!string.IsNullOrWhiteSpace(text) && stockSession != null)
            {
                if (text.StartsWith('/') &&
                    TryParseCommand(text, out var activeCommand, out var activeArgs) &&
                    IsSessionCancelCommand(activeCommand, activeArgs))
                {
                    await CancelStockSessionAsync(chatId, userId, ct);
                    return;
                }

                if (text.StartsWith('/'))
                {
                    await _telegram.SendTextAsync(
                        ToChatId(chatId),
                        "Devam eden stok oturumu var. Cevap ver veya `iptal` yaz.",
                        ct);
                    return;
                }

                await HandleStockSessionInputAsync(stockSession, text, ct);
                return;
            }

            if (!string.IsNullOrWhiteSpace(text) &&
                receiptSession != null &&
                !text.StartsWith('/'))
            {
                await HandleReceiptSessionInputAsync(receiptSession, text, ct);
                return;
            }

            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
                return;

            if (!TryParseCommand(text, out var command, out var args))
                return;

            if (receiptSession != null && IsSessionCancelCommand(command, args))
            {
                await CancelReceiptSessionAsync(chatId, userId, ct);
                return;
            }

            try
            {
                switch (command)
                {
                    case "/start":
                    case "/help":
                    case "/yardim":
                    case "/komutlar":
                    case "/yardım":
                        await SendHelpAsync(chatId, ct);
                        break;

                    case "/today":
                    case "/bugun":
                    case "/bugün":
                    {
                        var businessName = await GetActiveBusinessNameAsync();
                        await _backupReport.SendDailyReportAsync(DateTime.Today, $"Telegram komutu | İşletme: {businessName}");
                        break;
                    }
                    case "/summary":
                    case "/ozet":
                    case "/özet":
                        await SendSummaryAsync(args, chatId, ct);
                        break;

                    case "/report":
                    case "/rapor":
                        await SendReadableReportAsync(args, chatId, ct);
                        break;

                    case "/backup":
                    case "/yedek":
                    {
                        var businessName = await GetActiveBusinessNameAsync();
                        await _telegram.SendTextAsync(ToChatId(chatId), $"Yedek alınıyor, lütfen bekleyin.\nİşletme: {businessName}", ct);
                        await _backupReport.SendBackupAsync($"Telegram komutu | İşletme: {businessName}");
                        break;
                    }
                    case "/add":
                    case "/ekle":
                        await AddTransactionWithTypeAsync(args, chatId, ct);
                        break;

                    case "/gelir":
                        await AddTransactionAsync("Gelir", args, chatId, ct);
                        break;

                    case "/gider":
                        await AddTransactionAsync("Gider", args, chatId, ct);
                        break;

                    case "/stok":
                        if (receiptSession != null)
                        {
                            await _telegram.SendTextAsync(
                                ToChatId(chatId),
                                "Devam eden bir fiş oturumu var. Önce onu tamamla veya `iptal` yaz.",
                                ct);
                            break;
                        }

                        await HandleStockTextCommandAsync(update, args, ct);
                        break;

                    case "/sifre":
                    case "/pin":
                        await SendPinChangeDisabledAsync(chatId, ct);
                        break;

                    case "/onay":
                    case "/approve":
                        await ResolveApprovalAsync(args, chatId, true, ct);
                        break;

                    case "/iptal":
                    case "/red":
                    case "/cancel":
                        await ResolveApprovalAsync(args, chatId, false, ct);
                        break;

                    default:
                        await _telegram.SendTextAsync(
                            ToChatId(chatId),
                            "Bilinmeyen komut. Komutları görmek için /yardim yaz.",
                            ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramCommandService command error: {ex}");
                await _telegram.SendTextAsync(
                    ToChatId(chatId),
                    "Komut işlenirken bir hata oluştu. Lütfen tekrar deneyin.",
                    ct);
            }
        }

        private bool TryHandlePairingStart(TelegramUpdate update, string text, out string message)
        {
            message = string.Empty;
            if (_telegramPairingService is null ||
                string.IsNullOrWhiteSpace(text) ||
                !TryParseCommand(text, out var command, out var args) ||
                command != "/start" ||
                args.Length == 0)
            {
                return false;
            }

            var code = args[0]?.Trim() ?? string.Empty;
            if (_telegramPairingService.TryCompletePairing(code, update.ChatId, update.UserId, out message))
                return true;

            message = string.IsNullOrWhiteSpace(message)
                ? "Eşleştirme kodu geçersiz veya süresi doldu."
                : message;
            return true;
        }

        private async Task StartReceiptSessionAsync(TelegramUpdate update, CancellationToken ct)
        {
            if (!_receiptOcrSettings.IsConfigured)
            {
                await _telegram.SendTextAsync(
                    ToChatId(update.ChatId),
                    "Fiş OCR ayarları eksik. ReceiptOcr:ApiKey ayarını gir.",
                    ct);
                return;
            }

            if (string.IsNullOrWhiteSpace(update.PhotoFileId) || !update.UserId.HasValue)
            {
                await _telegram.SendTextAsync(ToChatId(update.ChatId), "Fiş fotoğrafı okunamadı.", ct);
                return;
            }

            string? tempFilePath = null;

            try
            {
                var businessName = await GetActiveBusinessNameAsync();
                var categories = await GetExpenseCategoryNamesAsync();
                var telegramFilePath = await _telegram.GetFilePathAsync(update.PhotoFileId, ct);
                tempFilePath = BuildTempReceiptFilePath(telegramFilePath);
                await _telegram.DownloadFileAsync(telegramFilePath, tempFilePath, ct);

                var request = new ReceiptOcrRequest
                {
                    BusinessName = businessName,
                    Caption = string.IsNullOrWhiteSpace(update.Caption) ? null : update.Caption.Trim(),
                    FileName = Path.GetFileName(tempFilePath),
                    MimeType = ResolveMimeType(tempFilePath),
                    ImageBytes = await File.ReadAllBytesAsync(tempFilePath, ct),
                    AvailableExpenseCategories = categories
                };

                var ocrResult = await _receiptOcrService.AnalyzeReceiptAsync(request, ct);
                var session = BuildReceiptSession(update, businessName, tempFilePath, ocrResult, categories);
                if (session.Items.Count == 0)
                {
                    SafeDeleteFile(tempFilePath);
                    await _telegram.SendTextAsync(
                        ToChatId(update.ChatId),
                        "Fiş okunamadı veya kaydedilebilir ürün bulunamadı.",
                        ct);
                    return;
                }

                await _receiptSessionStore.SaveAsync(session, ct);
                await _telegram.SendTextAsync(
                    ToChatId(update.ChatId),
                    "Fiş okundu. Eksik alanları birlikte tamamlayalım.",
                    ct);
                await AdvanceReceiptSessionAsync(session, ct);
            }
            catch (Exception ex)
            {
                SafeDeleteFile(tempFilePath);
                TryAppendReceiptOcrLog(ex);
                Debug.WriteLine($"TelegramCommandService receipt OCR error: {ex}");
                await _telegram.SendTextAsync(
                    ToChatId(update.ChatId),
                    $"Fiş OCR işlenemedi. {GetReceiptOcrFailureHint(ex)}",
                    ct);
            }
        }

        private TelegramReceiptSessionState BuildReceiptSession(
            TelegramUpdate update,
            string businessName,
            string tempFilePath,
            ReceiptOcrResult result,
            IReadOnlyList<string> categories)
        {
            var session = new TelegramReceiptSessionState
            {
                ChatId = update.ChatId,
                UserId = update.UserId ?? 0,
                SourceMessageId = update.MessageId,
                BusinessName = businessName,
                TempFilePath = tempFilePath,
                Merchant = result.Merchant?.Trim() ?? string.Empty,
                ReceiptDate = result.ReceiptDate,
                PaymentMethod = TryNormalizeOdemeYontemi(result.PaymentMethod, out var paymentMethod)
                    ? paymentMethod
                    : string.Empty,
                ReceiptTotal = result.ReceiptTotal,
                Step = ReceiptSessionStep.ResolveItems
            };

            foreach (var row in result.Items)
            {
                var matchedKalem = TryResolveCategory(row.CandidateKalem, categories);
                var shouldAsk = row.NeedsUserInput || string.IsNullOrWhiteSpace(matchedKalem);

                session.Items.Add(new TelegramReceiptSessionItem
                {
                    RawName = row.RawName.Trim(),
                    Amount = row.Amount,
                    CandidateKalem = row.CandidateKalem?.Trim() ?? string.Empty,
                    Confidence = row.Confidence,
                    NeedsUserInput = row.NeedsUserInput,
                    FinalKalem = shouldAsk ? string.Empty : matchedKalem
                });
            }

            return session;
        }

        private async Task HandleReceiptSessionInputAsync(
            TelegramReceiptSessionState session,
            string input,
            CancellationToken ct)
        {
            var normalized = NormalizeForMatch(input);
            if (normalized is "iptal" or "cancel" or "vazgec" or "vazgeç")
            {
                await CancelReceiptSessionAsync(session.ChatId, session.UserId, ct);
                return;
            }

            switch (session.Step)
            {
                case ReceiptSessionStep.ResolveItems:
                    await ResolveItemInputAsync(session, input, ct);
                    break;

                case ReceiptSessionStep.ConfirmNewCategory:
                    await ResolveNewCategoryConfirmationAsync(session, input, ct);
                    break;

                case ReceiptSessionStep.ResolveDate:
                    await ResolveReceiptDateAsync(session, input, ct);
                    break;

                case ReceiptSessionStep.ResolvePaymentMethod:
                    await ResolvePaymentMethodAsync(session, input, ct);
                    break;

                case ReceiptSessionStep.AwaitFinalConfirmation:
                    await ResolveFinalConfirmationAsync(session, input, ct);
                    break;

                default:
                    await _telegram.SendTextAsync(ToChatId(session.ChatId), "Beklenmeyen fiş oturumu durumu.", ct);
                    break;
            }
        }

        private async Task ResolveItemInputAsync(
            TelegramReceiptSessionState session,
            string input,
            CancellationToken ct)
        {
            var categories = await GetExpenseCategoryNamesAsync();
            var itemIndex = FindNextUnresolvedItemIndex(session);
            if (itemIndex < 0)
            {
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            var item = session.Items[itemIndex];
            var trimmed = input.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                await PromptForCurrentItemAsync(session, ct);
                return;
            }

            if (string.Equals(trimmed, "atla", StringComparison.OrdinalIgnoreCase))
            {
                item.FinalKalem = GetDefaultExpenseCategory(categories);
                session.CurrentItemIndex = itemIndex;
                await _receiptSessionStore.SaveAsync(session, ct);
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            if (trimmed.StartsWith("yeni:", StringComparison.OrdinalIgnoreCase))
            {
                var proposedName = trimmed[5..].Trim();
                if (string.IsNullOrWhiteSpace(proposedName))
                {
                    await _telegram.SendTextAsync(
                        ToChatId(session.ChatId),
                        "Yeni kalem adı boş olamaz. Örnek: yeni: Mutfak Giderleri",
                        ct);
                    await PromptForCurrentItemAsync(session, ct);
                    return;
                }

                session.Step = ReceiptSessionStep.ConfirmNewCategory;
                session.CurrentItemIndex = itemIndex;
                session.PendingCategoryName = proposedName;
                await _receiptSessionStore.SaveAsync(session, ct);
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    $"'{proposedName}' adlı gider kalemi oluşturayım mı? Evet/Hayır",
                    ct);
                return;
            }

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) &&
                index >= 1 && index <= categories.Count)
            {
                item.FinalKalem = categories[index - 1];
                session.CurrentItemIndex = itemIndex;
                await _receiptSessionStore.SaveAsync(session, ct);
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            var matchedByName = TryResolveCategory(trimmed, categories);
            if (!string.IsNullOrWhiteSpace(matchedByName))
            {
                item.FinalKalem = matchedByName;
                session.CurrentItemIndex = itemIndex;
                await _receiptSessionStore.SaveAsync(session, ct);
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "Kalem seçimi anlaşılmadı. Sıra numarası, genel gider kalem adı, `yeni: <ad>` veya `atla` kullan.",
                ct);
            await PromptForCurrentItemAsync(session, ct);
        }

        private async Task ResolveNewCategoryConfirmationAsync(
            TelegramReceiptSessionState session,
            string input,
            CancellationToken ct)
        {
            var normalized = NormalizeForMatch(input);
            if (normalized is "evet" or "e" or "yes" or "y")
            {
                var categoryName = session.PendingCategoryName.Trim();
                await _kalemTanimiService.CreateAsync("Gider", categoryName);
                if (session.CurrentItemIndex >= 0 && session.CurrentItemIndex < session.Items.Count)
                    session.Items[session.CurrentItemIndex].FinalKalem = categoryName;

                session.PendingCategoryName = string.Empty;
                session.Step = ReceiptSessionStep.ResolveItems;
                await _receiptSessionStore.SaveAsync(session, ct);
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            if (normalized is "hayir" or "h" or "no" or "n")
            {
                session.PendingCategoryName = string.Empty;
                session.Step = ReceiptSessionStep.ResolveItems;
                await _receiptSessionStore.SaveAsync(session, ct);
                await PromptForCurrentItemAsync(session, ct);
                return;
            }

            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "Lütfen `Evet` veya `Hayır` cevabı ver.",
                ct);
        }

        private async Task ResolveReceiptDateAsync(
            TelegramReceiptSessionState session,
            string input,
            CancellationToken ct)
        {
            if (string.Equals(input.Trim(), "atla", StringComparison.OrdinalIgnoreCase))
            {
                session.ReceiptDate = DateTime.Now;
                await _receiptSessionStore.SaveAsync(session, ct);
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            if (!TryParseUserDate(input, out var date))
            {
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Tarih anlaşılmadı. Örnek: 2026-03-29 veya 29.03.2026. Geçmek için `atla` yaz.",
                    ct);
                return;
            }

            session.ReceiptDate = date;
            await _receiptSessionStore.SaveAsync(session, ct);
            await AdvanceReceiptSessionAsync(session, ct);
        }

        private async Task ResolvePaymentMethodAsync(
            TelegramReceiptSessionState session,
            string input,
            CancellationToken ct)
        {
            if (string.Equals(input.Trim(), "atla", StringComparison.OrdinalIgnoreCase))
            {
                session.PaymentMethod = "Nakit";
                await _receiptSessionStore.SaveAsync(session, ct);
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            if (!TryNormalizeOdemeYontemi(input, out var normalized))
            {
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Ödeme yöntemi anlaşılmadı. Nakit, KrediKarti, OnlineOdeme veya Havale yaz. Geçmek için `atla` yaz.",
                    ct);
                return;
            }

            session.PaymentMethod = normalized;
            await _receiptSessionStore.SaveAsync(session, ct);
            await AdvanceReceiptSessionAsync(session, ct);
        }

        private async Task ResolveFinalConfirmationAsync(
            TelegramReceiptSessionState session,
            string input,
            CancellationToken ct)
        {
            var normalized = NormalizeForMatch(input);
            if (normalized is not ("onayla" or "evet" or "yes"))
            {
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Kayıtları oluşturmak için `onayla`, vazgeçmek için `iptal` yaz.",
                    ct);
                return;
            }

            var records = BuildReceiptRecords(session);
            var ids = await _kasaService.CreateManyAsync(records);
            await _receiptSessionStore.DeleteAsync(session.ChatId, session.UserId, ct);

            var businessName = await GetActiveBusinessNameAsync();
            var grouped = records
                .GroupBy(x => x.Kalem ?? GetDefaultExpenseCategory(Array.Empty<string>()), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => $"- {x.Key}: {x.Sum(y => y.Tutar):n2}")
                .ToArray();

            var response = new StringBuilder();
            response.AppendLine($"Kaydedildi. {ids.Count} gider kaydı oluşturuldu.");
            response.AppendLine($"İşletme: {businessName}");
            foreach (var line in grouped)
                response.AppendLine(line);

            await _telegram.SendTextAsync(ToChatId(session.ChatId), response.ToString().Trim(), ct);
        }

        private List<Kasa> BuildReceiptRecords(TelegramReceiptSessionState session)
        {
            var receiptDate = session.ReceiptDate ?? DateTime.Now;
            var paymentMethod = string.IsNullOrWhiteSpace(session.PaymentMethod) ? "Nakit" : session.PaymentMethod;
            var description = BuildReceiptDescription(session.Merchant);

            return session.Items
                .GroupBy(x => x.FinalKalem, StringComparer.OrdinalIgnoreCase)
                .Select(group => new Kasa
                {
                    Tarih = receiptDate,
                    Tip = "Gider",
                    Tutar = group.Sum(x => x.Amount),
                    OdemeYontemi = paymentMethod,
                    Kalem = group.First().FinalKalem,
                    GiderTuru = group.First().FinalKalem,
                    Aciklama = description
                })
                .OrderBy(x => x.Kalem, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildReceiptDescription(string? merchant)
        {
            return string.IsNullOrWhiteSpace(merchant)
                ? "OCR Fiş"
                : $"OCR Fiş | {merchant.Trim()}";
        }

        private async Task AdvanceReceiptSessionAsync(
            TelegramReceiptSessionState session,
            CancellationToken ct)
        {
            var unresolvedIndex = FindNextUnresolvedItemIndex(session);
            if (unresolvedIndex >= 0)
            {
                session.Step = ReceiptSessionStep.ResolveItems;
                session.CurrentItemIndex = unresolvedIndex;
                await _receiptSessionStore.SaveAsync(session, ct);
                await PromptForCurrentItemAsync(session, ct);
                return;
            }

            if (!session.ReceiptDate.HasValue)
            {
                session.Step = ReceiptSessionStep.ResolveDate;
                await _receiptSessionStore.SaveAsync(session, ct);
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Fiş tarihi eksik. Tarihi `yyyy-MM-dd` veya `dd.MM.yyyy` olarak gönder. Geçmek için `atla` yaz.",
                    ct);
                return;
            }

            if (string.IsNullOrWhiteSpace(session.PaymentMethod))
            {
                session.Step = ReceiptSessionStep.ResolvePaymentMethod;
                await _receiptSessionStore.SaveAsync(session, ct);
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Ödeme yöntemi eksik. Nakit, KrediKarti, OnlineOdeme veya Havale yaz. Geçmek için `atla` yaz.",
                    ct);
                return;
            }

            session.Step = ReceiptSessionStep.AwaitFinalConfirmation;
            await _receiptSessionStore.SaveAsync(session, ct);
            await SendReceiptSummaryAsync(session, ct);
        }

        private async Task PromptForCurrentItemAsync(TelegramReceiptSessionState session, CancellationToken ct)
        {
            var itemIndex = FindNextUnresolvedItemIndex(session);
            if (itemIndex < 0)
            {
                await AdvanceReceiptSessionAsync(session, ct);
                return;
            }

            session.CurrentItemIndex = itemIndex;
            var item = session.Items[itemIndex];
            var categories = await GetExpenseCategoryNamesAsync();
            var sb = new StringBuilder();
            sb.AppendLine($"Ürün için gider kalemi seç: {item.RawName}");
            sb.AppendLine($"Tutar: {item.Amount:n2}");
            if (!string.IsNullOrWhiteSpace(item.CandidateKalem))
                sb.AppendLine($"AI önerisi: {item.CandidateKalem}");
            sb.AppendLine();
            sb.AppendLine("Kalemler genel gider grupları olmalı. Örnek: Mutfak Giderleri, Personel Giderleri.");
            sb.AppendLine();
            sb.AppendLine("Kalemler:");
            for (var i = 0; i < categories.Count; i++)
                sb.AppendLine($"{i + 1}. {categories[i]}");
            sb.AppendLine();
            sb.AppendLine("Cevap:");
            sb.AppendLine("- sıra numarası");
            sb.AppendLine("- genel gider kalem adı");
            sb.AppendLine("- yeni: <kalem>");
            sb.AppendLine("- atla");
            sb.AppendLine("- iptal");

            await _telegram.SendTextAsync(ToChatId(session.ChatId), sb.ToString().Trim(), ct);
        }

        private async Task SendReceiptSummaryAsync(TelegramReceiptSessionState session, CancellationToken ct)
        {
            var merchant = string.IsNullOrWhiteSpace(session.Merchant) ? "-" : session.Merchant.Trim();
            var grouped = session.Items
                .GroupBy(x => x.FinalKalem, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Kalem = g.First().FinalKalem,
                    Toplam = g.Sum(x => x.Amount),
                    Urunler = string.Join(", ", g.Select(x => x.RawName).Distinct(StringComparer.OrdinalIgnoreCase))
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Fiş özeti");
            sb.AppendLine($"İşletme: {session.BusinessName}");
            sb.AppendLine($"Mağaza: {merchant}");
            sb.AppendLine($"Tarih: {(session.ReceiptDate.HasValue ? session.ReceiptDate.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "-")}");
            sb.AppendLine($"Ödeme: {session.PaymentMethod}");
            if (session.ReceiptTotal.HasValue)
                sb.AppendLine($"Fiş Toplamı: {session.ReceiptTotal.Value:n2}");
            sb.AppendLine();
            sb.AppendLine("Kalemler:");
            foreach (var row in grouped)
                sb.AppendLine($"- {row.Kalem}: {row.Toplam:n2} | {row.Urunler}");
            sb.AppendLine();
            sb.AppendLine("Kaydetmek için `onayla`, vazgeçmek için `iptal` yaz.");

            await _telegram.SendTextAsync(ToChatId(session.ChatId), sb.ToString().Trim(), ct);
        }

        private async Task CancelReceiptSessionAsync(long chatId, long userId, CancellationToken ct)
        {
            await _receiptSessionStore.DeleteAsync(chatId, userId, ct);
            await _telegram.SendTextAsync(ToChatId(chatId), "Fiş oturumu iptal edildi.", ct);
        }

        private static bool TryParseStockCaption(string? caption, out string[] args)
        {
            args = Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(caption))
                return false;

            if (!TryParseCommand(caption.Trim(), out var command, out var parsedArgs))
                return false;

            if (command != "/stok")
                return false;

            args = parsedArgs;
            return true;
        }

        private async Task StartStockPhotoCommandAsync(
            TelegramUpdate update,
            string[] args,
            CancellationToken ct)
        {
            if (!update.UserId.HasValue)
                return;

            if (args.Length != 1 || !TryParseStockQuantity(args[0], out var quantity))
            {
                await _telegram.SendTextAsync(ToChatId(update.ChatId), GetStockUsage(), ct);
                return;
            }

            if (string.IsNullOrWhiteSpace(update.PhotoFileId))
            {
                await _telegram.SendTextAsync(ToChatId(update.ChatId), "Barkod fotoğrafı okunamadı.", ct);
                return;
            }

            string? tempFilePath = null;
            try
            {
                var telegramFilePath = await _telegram.GetFilePathAsync(update.PhotoFileId, ct);
                tempFilePath = BuildTempStockFilePath(telegramFilePath);
                await _telegram.DownloadFileAsync(telegramFilePath, tempFilePath, ct);

                var barcode = await _barcodeReaderService.TryReadAsync(tempFilePath, ct);
                if (!barcode.Success || string.IsNullOrWhiteSpace(barcode.Barcode))
                {
                    await StartStockBarcodeAwaitSessionAsync(
                        update,
                        quantity,
                        "Barkod okunamadı. Lütfen barkod numarasını elle yaz veya `iptal` yaz.",
                        ct);
                    return;
                }

                var session = CreateStockSession(update, quantity);
                await ContinueStockFlowFromBarcodeAsync(session, barcode.Barcode, ct);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramCommandService stock photo error: {ex}");
                await _telegram.SendTextAsync(
                    ToChatId(update.ChatId),
                    "Stok barkod fotoğrafı işlenemedi. Fotoğraf net değilse barkodu elle /stok <barkod> <miktar> şeklinde gönder.",
                    ct);
            }
            finally
            {
                SafeDeleteFile(tempFilePath);
            }
        }

        private async Task HandleStockTextCommandAsync(
            TelegramUpdate update,
            string[] args,
            CancellationToken ct)
        {
            if (!update.UserId.HasValue)
                return;

            if (args.Length == 1 && TryParseStockQuantity(args[0], out var quantityOnly))
            {
                await StartStockBarcodeAwaitSessionAsync(
                    update,
                    quantityOnly,
                    "Stok hareketi için barkodu yaz. Örnek: 8690000000000",
                    ct);
                return;
            }

            if (args.Length == 2 &&
                !string.IsNullOrWhiteSpace(args[0]) &&
                TryParseStockQuantity(args[1], out var quantity))
            {
                var session = CreateStockSession(update, quantity);
                await ContinueStockFlowFromBarcodeAsync(session, args[0], ct);
                return;
            }

            await _telegram.SendTextAsync(ToChatId(update.ChatId), GetStockUsage(), ct);
        }

        private async Task StartStockBarcodeAwaitSessionAsync(
            TelegramUpdate update,
            decimal quantity,
            string prompt,
            CancellationToken ct)
        {
            var session = CreateStockSession(update, quantity);
            session.Step = StockSessionStep.AwaitBarcode;
            await _stockSessionStore.SaveAsync(session, ct);
            await _telegram.SendTextAsync(ToChatId(update.ChatId), prompt, ct);
        }

        private TelegramStockSessionState CreateStockSession(TelegramUpdate update, decimal quantity)
        {
            return new TelegramStockSessionState
            {
                ChatId = update.ChatId,
                UserId = update.UserId ?? 0,
                SourceMessageId = update.MessageId,
                PendingQuantity = quantity,
                Step = StockSessionStep.AwaitBarcode
            };
        }

        private async Task HandleStockSessionInputAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            var normalized = NormalizeForMatch(input);
            if (normalized is "iptal" or "cancel" or "vazgec" or "vazgeÃ§")
            {
                await CancelStockSessionAsync(session.ChatId, session.UserId, ct);
                return;
            }

            switch (session.Step)
            {
                case StockSessionStep.AwaitBarcode:
                    await ContinueStockFlowFromBarcodeAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitProductName:
                    await ResolveStockProductNameAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitUnit:
                    await ResolveStockUnitAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitVatRate:
                    await ResolveStockVatRateAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitPurchasePrice:
                    await ResolveStockPurchasePriceAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitSalePrice:
                    await ResolveStockSalePriceAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitCriticalStock:
                    await ResolveStockCriticalStockAsync(session, input, ct);
                    break;

                case StockSessionStep.AwaitConfirmation:
                    await ResolveStockConfirmationAsync(session, input, ct);
                    break;

                default:
                    await _telegram.SendTextAsync(ToChatId(session.ChatId), "Beklenmeyen stok oturumu durumu.", ct);
                    break;
            }
        }

        private async Task ContinueStockFlowFromBarcodeAsync(
            TelegramStockSessionState session,
            string barcodeInput,
            CancellationToken ct)
        {
            var barcode = NormalizeBarcode(barcodeInput);
            if (string.IsNullOrWhiteSpace(barcode))
            {
                session.Step = StockSessionStep.AwaitBarcode;
                await _stockSessionStore.SaveAsync(session, ct);
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Barkod boş olamaz. Barkod numarasını yaz veya `iptal` yaz.",
                    ct);
                return;
            }

            session.Barcode = barcode;
            var product = await _urunHizmetService.GetByBarcodeAsync(barcode, ct);
            if (product == null)
            {
                session.Step = StockSessionStep.AwaitProductName;
                await _stockSessionStore.SaveAsync(session, ct);
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    $"Bu barkod kayıtlı değil: {barcode}\nÜrün oluşturalım. Ürün adını yaz veya `iptal` yaz.",
                    ct);
                return;
            }

            session.ProductId = product.Id;
            session.ProductName = product.Ad;
            session.Unit = product.Birim;
            session.VatRate = product.KdvOrani;
            session.PurchasePrice = product.AlisFiyati;
            session.SalePrice = product.SatisFiyati;
            session.CriticalStock = product.KritikStok;
            session.Step = StockSessionStep.AwaitConfirmation;
            await _stockSessionStore.SaveAsync(session, ct);
            await SendStockConfirmationAsync(session, product, ct);
        }

        private async Task ResolveStockProductNameAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            var name = input.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await _telegram.SendTextAsync(ToChatId(session.ChatId), "Ürün adı boş olamaz. Ürün adını yaz.", ct);
                return;
            }

            session.ProductName = name;
            session.Step = StockSessionStep.AwaitUnit;
            await _stockSessionStore.SaveAsync(session, ct);
            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "Birim yaz. Örnek: Adet, Kg, Litre. Varsayılan Adet için `atla` yaz.",
                ct);
        }

        private async Task ResolveStockUnitAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            session.Unit = IsSkip(input) ? "Adet" : input.Trim();
            if (string.IsNullOrWhiteSpace(session.Unit))
                session.Unit = "Adet";

            session.Step = StockSessionStep.AwaitVatRate;
            await _stockSessionStore.SaveAsync(session, ct);
            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "KDV oranını yaz. Örnek: 20. Varsayılan 20 için `atla` yaz.",
                ct);
        }

        private async Task ResolveStockVatRateAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            if (IsSkip(input))
            {
                session.VatRate = 20m;
            }
            else if (!TryParseNonNegativeDecimal(input, out var vatRate))
            {
                await _telegram.SendTextAsync(ToChatId(session.ChatId), "KDV oranı sayısal olmalı. Örnek: 20 veya `atla`.", ct);
                return;
            }
            else
            {
                session.VatRate = vatRate;
            }

            session.Step = StockSessionStep.AwaitPurchasePrice;
            await _stockSessionStore.SaveAsync(session, ct);
            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "Alış fiyatını yaz. Bilmiyorsan `atla` yaz.",
                ct);
        }

        private async Task ResolveStockPurchasePriceAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            if (IsSkip(input))
            {
                session.PurchasePrice = 0m;
            }
            else if (!TryParseNonNegativeDecimal(input, out var purchasePrice))
            {
                await _telegram.SendTextAsync(ToChatId(session.ChatId), "Alış fiyatı sayısal olmalı. Örnek: 125,50 veya `atla`.", ct);
                return;
            }
            else
            {
                session.PurchasePrice = purchasePrice;
            }

            session.Step = StockSessionStep.AwaitSalePrice;
            await _stockSessionStore.SaveAsync(session, ct);
            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "Satış fiyatını yaz. Bilmiyorsan `atla` yaz.",
                ct);
        }

        private async Task ResolveStockSalePriceAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            if (IsSkip(input))
            {
                session.SalePrice = 0m;
            }
            else if (!TryParseNonNegativeDecimal(input, out var salePrice))
            {
                await _telegram.SendTextAsync(ToChatId(session.ChatId), "Satış fiyatı sayısal olmalı. Örnek: 150 veya `atla`.", ct);
                return;
            }
            else
            {
                session.SalePrice = salePrice;
            }

            session.Step = StockSessionStep.AwaitCriticalStock;
            await _stockSessionStore.SaveAsync(session, ct);
            await _telegram.SendTextAsync(
                ToChatId(session.ChatId),
                "Kritik stok miktarını yaz. Yoksa `atla` yaz.",
                ct);
        }

        private async Task ResolveStockCriticalStockAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            if (IsSkip(input))
            {
                session.CriticalStock = 0m;
            }
            else if (!TryParseNonNegativeDecimal(input, out var criticalStock))
            {
                await _telegram.SendTextAsync(ToChatId(session.ChatId), "Kritik stok sayısal olmalı. Örnek: 10 veya `atla`.", ct);
                return;
            }
            else
            {
                session.CriticalStock = criticalStock;
            }

            session.Step = StockSessionStep.AwaitConfirmation;
            await _stockSessionStore.SaveAsync(session, ct);
            await SendStockConfirmationAsync(session, product: null, ct);
        }

        private async Task ResolveStockConfirmationAsync(
            TelegramStockSessionState session,
            string input,
            CancellationToken ct)
        {
            var normalized = NormalizeForMatch(input);
            if (normalized is not ("onayla" or "evet" or "yes"))
            {
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Stok hareketini kaydetmek için `onayla`, vazgeçmek için `iptal` yaz.",
                    ct);
                return;
            }

            var product = await ResolveStockSessionProductAsync(session, ct);
            if (product == null)
            {
                await _stockSessionStore.DeleteAsync(session.ChatId, session.UserId, ct);
                await _telegram.SendTextAsync(
                    ToChatId(session.ChatId),
                    "Ürün bulunamadı veya oluşturulamadı. Stok hareketi kaydedilmedi.",
                    ct);
                return;
            }

            var result = await _stokService.CreateMovementAsync(
                new StokHareketCreateRequest
                {
                    UrunHizmetId = product.Id,
                    Miktar = session.PendingQuantity,
                    Kaynak = "Telegram",
                    Aciklama = $"Telegram stok | Barkod: {session.Barcode}"
                },
                ct);

            await _stockSessionStore.DeleteAsync(session.ChatId, session.UserId, ct);
            var businessName = await GetActiveBusinessNameAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Stok hareketi kaydedildi.");
            sb.AppendLine($"İşletme: {businessName}");
            sb.AppendLine($"Ürün: {product.Ad}");
            sb.AppendLine($"Barkod: {session.Barcode}");
            sb.AppendLine($"Miktar: {FormatSignedQuantity(session.PendingQuantity)}");
            sb.AppendLine($"Mevcut stok: {result.MevcutStok:n2} {product.Birim}");
            if (result.IsNegative)
                sb.AppendLine("Uyarı: Mevcut stok eksiye düştü.");

            await _telegram.SendTextAsync(ToChatId(session.ChatId), sb.ToString().Trim(), ct);
        }

        private async Task<UrunHizmet?> ResolveStockSessionProductAsync(
            TelegramStockSessionState session,
            CancellationToken ct)
        {
            if (session.ProductId.HasValue)
                return await _urunHizmetService.GetByIdAsync(session.ProductId.Value, ct);

            try
            {
                var createdId = await _urunHizmetService.CreateAsync(
                    new UrunHizmetCreateRequest
                    {
                        Tip = "Urun",
                        Ad = session.ProductName,
                        Barkod = session.Barcode,
                        Birim = session.Unit,
                        KdvOrani = session.VatRate,
                        AlisFiyati = session.PurchasePrice,
                        SatisFiyati = session.SalePrice,
                        KritikStok = session.CriticalStock
                    },
                    ct);

                return await _urunHizmetService.GetByIdAsync(createdId, ct);
            }
            catch (InvalidOperationException)
            {
                return await _urunHizmetService.GetByBarcodeAsync(session.Barcode, ct);
            }
        }

        private async Task SendStockConfirmationAsync(
            TelegramStockSessionState session,
            UrunHizmet? product,
            CancellationToken ct)
        {
            var isExistingProduct = product != null;
            var productName = isExistingProduct ? product!.Ad : session.ProductName;
            var unit = isExistingProduct ? product!.Birim : session.Unit;
            var currentStock = isExistingProduct
                ? await _stokService.GetCurrentStockAsync(product!.Id, ct)
                : 0m;
            var nextStock = currentStock + session.PendingQuantity;
            var businessName = await GetActiveBusinessNameAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Stok hareketi özeti");
            sb.AppendLine($"İşletme: {businessName}");
            sb.AppendLine(isExistingProduct ? $"Ürün: {productName}" : $"Yeni ürün: {productName}");
            sb.AppendLine($"Barkod: {session.Barcode}");
            sb.AppendLine($"Miktar: {FormatSignedQuantity(session.PendingQuantity)} ({(session.PendingQuantity > 0 ? "Giriş" : "Çıkış")})");
            sb.AppendLine($"Mevcut stok: {currentStock:n2} {unit} -> {nextStock:n2} {unit}");
            if (!isExistingProduct)
            {
                sb.AppendLine($"Birim: {session.Unit}");
                sb.AppendLine($"KDV: %{session.VatRate:n2}");
                sb.AppendLine($"Alış/Satış: {session.PurchasePrice:n2} / {session.SalePrice:n2}");
                sb.AppendLine($"Kritik stok: {session.CriticalStock:n2}");
            }

            if (nextStock < 0)
                sb.AppendLine("Uyarı: Bu işlem stoğu eksiye düşürecek. V1'de engellenmez, sadece uyarılır.");

            sb.AppendLine();
            sb.AppendLine("Kaydetmek için `onayla`, vazgeçmek için `iptal` yaz.");

            await _telegram.SendTextAsync(ToChatId(session.ChatId), sb.ToString().Trim(), ct);
        }

        private async Task CancelStockSessionAsync(long chatId, long userId, CancellationToken ct)
        {
            await _stockSessionStore.DeleteAsync(chatId, userId, ct);
            await _telegram.SendTextAsync(ToChatId(chatId), "Stok oturumu iptal edildi.", ct);
        }

        private static bool IsSkip(string input)
        {
            return string.Equals(input.Trim(), "atla", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseStockQuantity(string raw, out decimal quantity)
        {
            if (!TryParseAmount(raw, out quantity))
                return false;

            return quantity != 0m;
        }

        private static bool TryParseNonNegativeDecimal(string raw, out decimal value)
        {
            if (!TryParseAmount(raw, out value))
                return false;

            return value >= 0m;
        }

        private static string NormalizeBarcode(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string FormatSignedQuantity(decimal quantity)
        {
            return quantity > 0 ? $"+{quantity:n2}" : quantity.ToString("n2", CultureInfo.CurrentCulture);
        }

        private static string GetStockUsage()
        {
            return "Kullanım:\n/stok <barkod> +10\n/stok <barkod> -3\nBarkod fotoğrafı için açıklama: /stok +50";
        }

        private static string BuildTempStockFilePath(string telegramFilePath)
        {
            var extension = Path.GetExtension(telegramFilePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var folder = GetWritableWorkingFolder("Barcodes");
            return Path.Combine(folder, $"barcode_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}{extension}");
        }

        private static bool IsSessionCancelCommand(string command, IReadOnlyList<string> args)
        {
            if (args.Count > 0)
                return false;

            return command is "/iptal" or "/cancel";
        }

        private static bool TryParseCommand(string text, out string command, out string[] args)
        {
            command = string.Empty;
            args = Array.Empty<string>();

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            var commandToken = parts[0];
            var argStart = 1;

            if (commandToken == "/" && parts.Length > 1)
            {
                commandToken = "/" + parts[1];
                argStart = 2;
            }

            command = commandToken.Split('@')[0].Trim().ToLowerInvariant();
            args = parts.Skip(argStart).ToArray();
            return command.StartsWith('/');
        }

        private async Task SendHelpAsync(long chatId, CancellationToken ct)
        {
            var help = TelegramCommandCatalog.BuildHelpText(await GetActiveBusinessNameAsync());
            await _telegram.SendTextAsync(ToChatId(chatId), help, ct);
        }

        private async Task ResolveApprovalAsync(string[] args, long chatId, bool approve, CancellationToken ct)
        {
            if (args.Length == 0)
            {
                await _telegram.SendTextAsync(
                    ToChatId(chatId),
                    approve ? "Kullanım: /onay <kod>" : "Kullanım: /iptal <kod>",
                    ct);
                return;
            }

            var code = args[0]?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                await _telegram.SendTextAsync(
                    ToChatId(chatId),
                    "Onay kodu boş olamaz.",
                    ct);
                return;
            }

            if (!_telegramApprovalService.TryResolve(code, approve, out var title))
            {
                await _telegram.SendTextAsync(
                    ToChatId(chatId),
                    "Kod bulunamadı veya süresi doldu.",
                    ct);
                return;
            }

            var resultText = approve ? "Onaylandı" : "Reddedildi";
            var titleText = string.IsNullOrWhiteSpace(title) ? string.Empty : $" | {title}";
            await _telegram.SendTextAsync(ToChatId(chatId), $"{resultText}{titleText}", ct);
        }

        private async Task SendSummaryAsync(string[] args, long chatId, CancellationToken ct)
        {
            var days = 30;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDays))
                {
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullanım: /ozet [gün]", ct);
                    return;
                }

                days = Math.Clamp(parsedDays, 1, 3650);
            }

            var to = DateTime.Today;
            var from = to.AddDays(-(days - 1));
            var summary = await _summaryService.GetSummaryAsync(from, to);
            var records = await _kasaService.GetAllAsync(from, to);
            var businessName = await GetActiveBusinessNameAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Özet ({from:yyyy-MM-dd} - {to:yyyy-MM-dd})");
            sb.AppendLine($"İşletme: {businessName}");
            sb.AppendLine($"Gelir: {summary.IncomeTotal:n2}");
            sb.AppendLine($"Gider: {summary.ExpenseTotal:n2}");
            sb.AppendLine($"Net: {summary.Net:n2}");
            sb.AppendLine($"İşlem: {summary.IncomeCount + summary.ExpenseCount} (Gelir {summary.IncomeCount}, Gider {summary.ExpenseCount})");
            AppendOdemeYontemiBreakdown(sb, records);
            AppendKalemBreakdown(sb, records, "Gelir");
            AppendKalemBreakdown(sb, records, "Gider");

            await _telegram.SendTextAsync(ToChatId(chatId), sb.ToString().Trim(), ct);
        }

        private async Task SendPinChangeDisabledAsync(long chatId, CancellationToken ct)
        {
            await _telegram.SendTextAsync(
                ToChatId(chatId),
                "PIN değişimi Telegram üzerinden kapatıldı. Lütfen uygulama içindeki Ayarlar ekranını kullanın.",
                ct);
        }

        private async Task SendReadableReportAsync(string[] args, long chatId, CancellationToken ct)
        {
            var days = 30;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDays))
                {
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullanım: /rapor [gün]", ct);
                    return;
                }

                days = Math.Clamp(parsedDays, 1, 3650);
            }

            var to = DateTime.Today;
            var from = to.AddDays(-(days - 1));
            var summary = await _summaryService.GetSummaryAsync(from, to);
            var records = await _kasaService.GetAllAsync(from, to);
            var businessName = await GetActiveBusinessNameAsync();

            var sb = new StringBuilder();
            sb.AppendLine("CashTracker Rapor");
            sb.AppendLine($"Aralık: {from:yyyy-MM-dd} - {to:yyyy-MM-dd}");
            sb.AppendLine($"İşletme: {businessName}");
            sb.AppendLine();
            sb.AppendLine($"Gelir Toplam: {summary.IncomeTotal:n2}");
            sb.AppendLine($"Gider Toplam: {summary.ExpenseTotal:n2}");
            sb.AppendLine($"Net: {summary.Net:n2}");
            sb.AppendLine($"İşlem Sayısı: {summary.IncomeCount + summary.ExpenseCount}");
            AppendOdemeYontemiBreakdown(sb, records);
            sb.AppendLine();
            sb.AppendLine("Hareketler:");

            if (records.Count == 0)
            {
                sb.AppendLine("- Bu aralıkta hareket yok.");
            }
            else
            {
                foreach (var row in records.OrderBy(x => x.Tarih))
                {
                    var kalem = !string.IsNullOrWhiteSpace(row.Kalem)
                        ? row.Kalem
                        : (string.IsNullOrWhiteSpace(row.GiderTuru) ? "-" : row.GiderTuru);
                    var aciklama = string.IsNullOrWhiteSpace(row.Aciklama) ? "-" : row.Aciklama;
                    sb.AppendLine($"{row.Tarih:yyyy-MM-dd HH:mm} | {row.Tip} | {row.Tutar:n2} | {kalem} | {aciklama}");
                }
            }

            var reportDir = Path.Combine(Path.GetTempPath(), "CashTrackerReports");
            Directory.CreateDirectory(reportDir);

            var reportPath = Path.Combine(reportDir, $"cashtracker_rapor_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");

            await File.WriteAllTextAsync(reportPath, sb.ToString(), Encoding.UTF8, ct);

            try
            {
                var caption = $"Rapor ({from:yyyy-MM-dd} - {to:yyyy-MM-dd}) | İşletme: {businessName}";
                await _telegram.SendDocumentAsync(ToChatId(chatId), reportPath, caption, ct);
            }
            finally
            {
                SafeDeleteFile(reportPath);
            }
        }

        private async Task AddTransactionWithTypeAsync(string[] args, long chatId, CancellationToken ct)
        {
            if (args.Length < 2)
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Kullanım: /ekle gelir|gider <tutar> [kalem] [açıklama]", ct);
                return;
            }

            var tip = NormalizeTip(args[0]);
            if (tip is null)
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Tip sadece gelir veya gider olabilir.", ct);
                return;
            }

            await AddTransactionAsync(tip, args.Skip(1).ToArray(), chatId, ct);
        }

        private async Task AddTransactionAsync(string tip, string[] args, long chatId, CancellationToken ct)
        {
            if (args.Length < 1)
            {
                if (tip == "Gider")
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullanım: /gider <tutar> <kalem> [açıklama]", ct);
                else
                    await _telegram.SendTextAsync(ToChatId(chatId), "Kullanım: /gelir <tutar> [kalem] [açıklama]", ct);
                return;
            }

            if (!TryParseAmount(args[0], out var amount) || amount <= 0)
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Tutar sayısal ve sıfırdan büyük olmalı.", ct);
                return;
            }

            var remaining = args.Skip(1).ToArray();
            var kalemler = await _kalemTanimiService.GetByTipAsync(tip);
            var kalemParse = ParseKalemAndAciklama(tip, remaining, kalemler);

            if (tip == "Gider" && string.IsNullOrWhiteSpace(kalemParse.Kalem))
            {
                await _telegram.SendTextAsync(ToChatId(chatId), "Gider için kalem zorunlu. Örnek: /gider 150 market", ct);
                return;
            }

            var kasa = new Kasa
            {
                Tarih = DateTime.Now,
                Tip = tip,
                Tutar = amount,
                Kalem = kalemParse.Kalem,
                GiderTuru = tip == "Gider" ? kalemParse.Kalem : null,
                Aciklama = kalemParse.Aciklama
            };

            var id = await _kasaService.CreateAsync(kasa);
            var kalemText = string.IsNullOrWhiteSpace(kalemParse.Kalem)
                ? GetDefaultKalem(tip, kalemler)
                : kalemParse.Kalem;
            var businessName = await GetActiveBusinessNameAsync();

            await _telegram.SendTextAsync(
                ToChatId(chatId),
                $"Kaydedildi. Id: {id}\nİşletme: {businessName}\nTip: {tip}\nKalem: {kalemText}\nTutar: {amount:n2}",
                ct);
        }

        private static (string Kalem, string? Aciklama) ParseKalemAndAciklama(
            string tip,
            string[] remainingArgs,
            IReadOnlyList<KalemTanimi> kalemler)
        {
            if (remainingArgs.Length == 0)
                return (GetDefaultKalem(tip, kalemler), null);

            if (TryMatchKalemPrefix(kalemler, remainingArgs, out var matchedKalem, out var consumedTokenCount))
            {
                var aciklama = remainingArgs.Length > consumedTokenCount
                    ? string.Join(' ', remainingArgs.Skip(consumedTokenCount))
                    : null;
                return (matchedKalem, aciklama);
            }

            if (tip == "Gider")
            {
                var fallbackKalem = remainingArgs[0].Trim();
                var aciklama = remainingArgs.Length > 1
                    ? string.Join(' ', remainingArgs.Skip(1))
                    : null;
                return (fallbackKalem, aciklama);
            }

            var gelirAciklama = string.Join(' ', remainingArgs);
            return (GetDefaultKalem(tip, kalemler), string.IsNullOrWhiteSpace(gelirAciklama) ? null : gelirAciklama);
        }

        private static bool TryMatchKalemPrefix(
            IReadOnlyList<KalemTanimi> kalemler,
            string[] remainingArgs,
            out string kalem,
            out int consumedTokenCount)
        {
            kalem = string.Empty;
            consumedTokenCount = 0;

            if (remainingArgs.Length == 0 || kalemler.Count == 0)
                return false;

            string? bestMatchName = null;
            var joinedArgs = string.Join(' ', remainingArgs);
            var joinedArgsNormalized = NormalizeForMatch(joinedArgs);

            foreach (var row in kalemler)
            {
                var ad = row.Ad?.Trim();
                if (string.IsNullOrWhiteSpace(ad))
                    continue;

                var normalizedAd = NormalizeForMatch(ad);
                var isExact = string.Equals(joinedArgsNormalized, normalizedAd, StringComparison.Ordinal);
                var isPrefix = joinedArgsNormalized.StartsWith(normalizedAd + " ", StringComparison.Ordinal);
                if (!isExact && !isPrefix)
                    continue;

                if (bestMatchName == null || ad.Length > bestMatchName.Length)
                    bestMatchName = ad;
            }

            if (string.IsNullOrWhiteSpace(bestMatchName))
                return false;

            kalem = bestMatchName;
            consumedTokenCount = bestMatchName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Length;
            return true;
        }

        private static string GetDefaultKalem(string tip, IReadOnlyList<KalemTanimi> kalemler)
        {
            var preferred = tip == "Gider" ? "Genel Gider" : "Genel Gelir";
            if (kalemler.Count == 0)
                return preferred;

            foreach (var row in kalemler)
            {
                if (string.Equals(row.Ad, preferred, StringComparison.OrdinalIgnoreCase))
                    return row.Ad;
            }

            return kalemler[0].Ad;
        }

        private static void AppendKalemBreakdown(StringBuilder sb, IReadOnlyCollection<Kasa> records, string tip)
        {
            var kalemRows = records
                .Where(x => IsTip(x.Tip, tip))
                .GroupBy(GetKalemName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Kalem = g.Key,
                    Toplam = g.Sum(x => x.Tutar),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Toplam)
                .ThenBy(x => x.Kalem, StringComparer.OrdinalIgnoreCase)
                .ToList();

            sb.AppendLine();
            sb.AppendLine($"{tip} Kalemleri:");
            if (kalemRows.Count == 0)
            {
                sb.AppendLine("- Kayıt yok.");
                return;
            }

            foreach (var row in kalemRows)
                sb.AppendLine($"- {row.Kalem}: {row.Toplam:n2} ({row.Count} işlem)");
        }

        private static void AppendOdemeYontemiBreakdown(StringBuilder sb, IReadOnlyCollection<Kasa> records)
        {
            var byMethod = records
                .GroupBy(x => NormalizeOdemeYontemi(x.OdemeYontemi), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Where(x => IsTip(x.Tip, "Gelir")).Sum(x => x.Tutar),
                        Expense = g.Where(x => IsTip(x.Tip, "Gider")).Sum(x => x.Tutar)
                    },
                    StringComparer.OrdinalIgnoreCase);

            sb.AppendLine();
            sb.AppendLine("Ödeme Yöntemleri:");
            foreach (var method in new[] { "Nakit", "KrediKarti", "OnlineOdeme", "Havale" })
            {
                var income = byMethod.TryGetValue(method, out var values) ? values.Income : 0m;
                var expense = byMethod.TryGetValue(method, out values) ? values.Expense : 0m;
                sb.AppendLine($"- {GetOdemeYontemiLabel(method)}: Gelir {income:n2} | Gider {expense:n2} | Net {(income - expense):n2}");
            }
        }

        private static bool IsTip(string? rawTip, string tip)
        {
            var normalized = (rawTip ?? string.Empty).Trim().ToLowerInvariant();
            if (tip == "Gelir")
                return normalized is "gelir" or "giris" or "giriş" or "income";

            if (tip == "Gider")
                return normalized is "gider" or "cikis" or "çıkış" or "expense";

            return false;
        }

        private static string GetKalemName(Kasa row)
        {
            if (!string.IsNullOrWhiteSpace(row.Kalem))
                return row.Kalem.Trim();

            if (!string.IsNullOrWhiteSpace(row.GiderTuru))
                return row.GiderTuru.Trim();

            return IsTip(row.Tip, "Gider") ? "Genel Gider" : "Genel Gelir";
        }

        private static string NormalizeForMatch(string value)
        {
            var normalized = (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('ı', 'i')
                .Replace('ş', 's')
                .Replace('ğ', 'g')
                .Replace('ü', 'u')
                .Replace('ö', 'o')
                .Replace('ç', 'c');

            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized.Normalize(NormalizationForm.FormD))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool TryParseAmount(string raw, out decimal amount)
        {
            var trimmed = raw.Trim();
            if (trimmed.Contains(',') && !trimmed.Contains('.'))
            {
                var commaAsDecimal = trimmed.Replace(',', '.');
                if (decimal.TryParse(commaAsDecimal, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                    return true;
            }

            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return true;

            var normalized = trimmed.Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        private static string? NormalizeTip(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "gelir" => "Gelir",
                "giris" => "Gelir",
                "giriş" => "Gelir",
                "income" => "Gelir",
                "gider" => "Gider",
                "cikis" => "Gider",
                "çıkış" => "Gider",
                "expense" => "Gider",
                _ => null
            };
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            return TryNormalizeOdemeYontemi(value, out var normalized)
                ? normalized
                : "Nakit";
        }

        private static bool TryNormalizeOdemeYontemi(string? value, out string normalized)
        {
            normalized = (value ?? string.Empty).Trim();
            var raw = normalized.ToLowerInvariant();
            switch (raw)
            {
                case "nakit":
                case "cash":
                    normalized = "Nakit";
                    return true;

                case "kredikarti":
                case "kredi karti":
                case "kredi kartı":
                case "kart":
                case "creditcard":
                case "credit card":
                    normalized = "KrediKarti";
                    return true;

                case "online":
                case "onlineodeme":
                case "online odeme":
                case "online ödeme":
                case "online payment":
                    normalized = "OnlineOdeme";
                    return true;

                case "havale":
                case "transfer":
                case "bank transfer":
                    normalized = "Havale";
                    return true;

                default:
                    normalized = string.Empty;
                    return false;
            }
        }

        private static string GetOdemeYontemiLabel(string method)
        {
            return method switch
            {
                "KrediKarti" => "Kredi Kartı",
                "OnlineOdeme" => "Online Ödeme",
                _ => method
            };
        }

        private async Task<string> GetActiveBusinessNameAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                return string.IsNullOrWhiteSpace(active.Ad) ? "Bilinmiyor" : active.Ad.Trim();
            }
            catch
            {
                return "Bilinmiyor";
            }
        }

        private async Task<List<string>> GetExpenseCategoryNamesAsync()
        {
            var categories = await _kalemTanimiService.GetByTipAsync("Gider");
            var names = categories
                .Select(x => x.Ad?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count == 0)
                names.AddRange(DefaultKalemCatalog.DefaultExpenseCategories);

            return names;
        }

        private static string TryResolveCategory(string? value, IReadOnlyList<string> categories)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = NormalizeForMatch(value);
            foreach (var category in categories)
            {
                if (string.Equals(NormalizeForMatch(category), normalized, StringComparison.Ordinal))
                    return category;
            }

            return string.Empty;
        }

        private static string GetDefaultExpenseCategory(IReadOnlyList<string> categories)
        {
            if (categories.Count == 0)
                return "Genel Gider";

            foreach (var category in categories)
            {
                if (string.Equals(category, "Genel Gider", StringComparison.OrdinalIgnoreCase))
                    return category;
            }

            return categories[0];
        }

        private static int FindNextUnresolvedItemIndex(TelegramReceiptSessionState session)
        {
            for (var i = 0; i < session.Items.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(session.Items[i].FinalKalem))
                    return i;
            }

            return -1;
        }

        private static string BuildTempReceiptFilePath(string telegramFilePath)
        {
            var extension = Path.GetExtension(telegramFilePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var folder = GetWritableWorkingFolder("Receipts");
            return Path.Combine(folder, $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}{extension}");
        }

        private static string ResolveMimeType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                _ => "image/jpeg"
            };
        }

        private static bool TryParseUserDate(string value, out DateTime date)
        {
            var formats = new[]
            {
                "yyyy-MM-dd",
                "dd.MM.yyyy",
                "d.M.yyyy",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "yyyy/MM/dd",
                "yyyy-MM-dd HH:mm",
                "dd.MM.yyyy HH:mm"
            };

            if (DateTime.TryParseExact(
                    value.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out date))
            {
                date = date.TimeOfDay == TimeSpan.Zero ? date.Date.Add(DateTime.Now.TimeOfDay) : date;
                return true;
            }

            if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out date))
            {
                date = date.TimeOfDay == TimeSpan.Zero ? date.Date.Add(DateTime.Now.TimeOfDay) : date;
                return true;
            }

            return false;
        }

        private static void SafeDeleteFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }

        private static string GetReceiptOcrFailureHint(Exception ex)
        {
            var message = FlattenExceptionMessages(ex);
            if (string.IsNullOrWhiteSpace(message))
                return "Fotoğraf net değilse tekrar deneyin.";

            if (message.Contains("Telegram getFile", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Telegram file download", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Telegram file_path", StringComparison.OrdinalIgnoreCase))
            {
                return "Telegram fotoğraf indirme aşamasında hata oldu.";
            }

            if (message.Contains("DeepSeek API anahtari eksik", StringComparison.OrdinalIgnoreCase))
                return "OCR/AI ayarlari eksik.";

            if (message.Contains("Receipt OCR ayarlari eksik", StringComparison.OrdinalIgnoreCase))
                return "OCR ayarları eksik.";

            if (message.Contains("Gemini OCR failed", StringComparison.OrdinalIgnoreCase))
            {
                if (message.Contains("429", StringComparison.OrdinalIgnoreCase))
                    return "Gemini kota veya hız limiti aşıldı.";

                if (message.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("403", StringComparison.OrdinalIgnoreCase))
                {
                    return "Gemini API erişimi reddedildi.";
                }

                if (message.Contains("400", StringComparison.OrdinalIgnoreCase))
                    return "Gemini fotoğraf isteğini reddetti.";

                if (message.Contains("500", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("503", StringComparison.OrdinalIgnoreCase))
                {
                    return "Gemini şu anda geçici olarak yanıt vermiyor.";
                }

                return "Gemini OCR isteği başarısız oldu.";
            }

            if (message.Contains("OCR.space", StringComparison.OrdinalIgnoreCase))
                return message.Contains("1 MB", StringComparison.OrdinalIgnoreCase)
                    ? "Fiş fotoğrafı OCR.space ücretsiz limitini aşıyor. Fotoğrafı biraz daha düşük çözünürlükte tekrar gönderin."
                    : "OCR.space fiş metnini okuyamadı.";

            if (message.Contains("DeepSeek yaniti basarisiz", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("DeepSeek bos yanit", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("DeepSeek OCR JSON", StringComparison.OrdinalIgnoreCase))
                return "DeepSeek fis siniflandirmasi yapamadi.";

            if (message.Contains("Gemini OCR yaniti bos", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Gemini OCR JSON", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("JSON object icermiyor", StringComparison.OrdinalIgnoreCase))
            {
                return "Gemini geçerli bir OCR yanıtı döndürmedi.";
            }

            if (message.Contains("sqlite", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("dbupdate", StringComparison.OrdinalIgnoreCase))
            {
                return "OCR oturumu veritabanına kaydedilemedi.";
            }

            if (message.Contains("Receipt image is required.", StringComparison.OrdinalIgnoreCase))
                return "Telegram fotoğraf verisi okunamadı.";

            if (message.Contains("CashTracker gecici klasoru", StringComparison.OrdinalIgnoreCase))
                return "Uygulamanın geçici klasörüne yazılamadı.";

            return "Fotoğraf net değilse tekrar deneyin.";
        }

        private static string FlattenExceptionMessages(Exception ex)
        {
            var sb = new StringBuilder();
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (string.IsNullOrWhiteSpace(current.Message))
                    continue;

                if (sb.Length > 0)
                    sb.Append(" | ");

                sb.Append(current.Message.Trim());
            }

            return sb.ToString();
        }

        private static void TryAppendReceiptOcrLog(Exception ex)
        {
            try
            {
                var folder = GetWritableWorkingFolder("Logs");
                var path = Path.Combine(folder, "receipt-ocr.log");
                var line =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {FlattenExceptionMessages(ex)}{Environment.NewLine}{ex}{Environment.NewLine}---{Environment.NewLine}";
                File.AppendAllText(path, line, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string GetWritableWorkingFolder(string leafFolder)
        {
            foreach (var candidate in GetWorkingFolderCandidates(leafFolder))
            {
                if (TryEnsureWritableDirectory(candidate))
                    return candidate;
            }

            throw new InvalidOperationException($"CashTracker gecici klasoru olusturulamadi: {leafFolder}");
        }

        private static IEnumerable<string> GetWorkingFolderCandidates(string leafFolder)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
                yield return Path.Combine(localAppData, "CashTracker", leafFolder);

            var envLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrWhiteSpace(envLocalAppData))
                yield return Path.Combine(envLocalAppData.Trim(), "CashTracker", leafFolder);

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
                yield return Path.Combine(userProfile, "AppData", "Local", "CashTracker", leafFolder);

            yield return Path.Combine(AppContext.BaseDirectory, "AppData", leafFolder);

            var tempPath = Path.GetTempPath();
            if (!string.IsNullOrWhiteSpace(tempPath))
                yield return Path.Combine(tempPath, "CashTracker", leafFolder);
        }

        private static bool TryEnsureWritableDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                Directory.CreateDirectory(path);
                var probePath = Path.Combine(path, $".write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probePath, "ok");
                File.Delete(probePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ToChatId(long chatId)
        {
            return chatId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
