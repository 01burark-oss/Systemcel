using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;

namespace Systemcel.Api.Services
{
    internal enum PinReminderStatus
    {
        Success,
        NotConfigured,
        Failed
    }

    internal sealed record PinReminderResult(PinReminderStatus Status, string Message);

    internal sealed class PinReminderService
    {
        private readonly IAppSecurityService _appSecurityService;
        private readonly BackupReportService _backupReportService;
        private readonly TelegramSettings _telegramSettings;

        public PinReminderService(
            IAppSecurityService appSecurityService,
            BackupReportService backupReportService,
            TelegramSettings telegramSettings)
        {
            _appSecurityService = appSecurityService;
            _backupReportService = backupReportService;
            _telegramSettings = telegramSettings;
        }

        public async Task<PinReminderResult> SendCurrentPinAsync(CancellationToken ct = default)
        {
            if (!_telegramSettings.IsEnabled)
            {
                return new PinReminderResult(
                    PinReminderStatus.NotConfigured,
                    "Telegram ayarları bulunamadı. Önce Telegram entegrasyonunu ayarlayın.");
            }

            try
            {
                var pin = await _appSecurityService.GetPinAsync();
                if (string.IsNullOrWhiteSpace(pin))
                {
                    return new PinReminderResult(
                        PinReminderStatus.Failed,
                        "Kayıtlı bir PIN bulunamadı.");
                }

                var text =
                    "Systemcel PIN Hatırlatma\n" +
                    $"PIN: {pin}\n" +
                    $"Tarih: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}";

                ct.ThrowIfCancellationRequested();
                await _backupReportService.SendTextAsync(text);

                return new PinReminderResult(
                    PinReminderStatus.Success,
                    "Mevcut PIN Telegram üzerinden gönderildi.");
            }
            catch (Exception ex)
            {
                return new PinReminderResult(
                    PinReminderStatus.Failed,
                    $"PIN Telegram'a gönderilemedi: {ex.Message}");
            }
        }
    }
}
