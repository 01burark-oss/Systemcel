using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class BackupReportService
    {
        private readonly TelegramBotService _telegram;
        private readonly TelegramSettings _settings;
        private readonly IDailyReportService _reportService;
        private readonly DatabaseBackupService _backupService;

        public BackupReportService(
            TelegramBotService telegram,
            TelegramSettings settings,
            IDailyReportService reportService,
            DatabaseBackupService backupService)
        {
            _telegram = telegram;
            _settings = settings;
            _reportService = reportService;
            _backupService = backupService;
        }

        public async Task SendDailyReportAsync(DateTime date, string? note = null)
        {
            if (!_settings.IsEnabled) return;

            var report = await _reportService.GetDailyReportAsync(date);
            var text = BuildReportText(report, note);

            await _telegram.SendTextAsync(_settings.ChatId, text);
        }

        public async Task SendBackupAsync(string? note = null)
        {
            if (!_settings.IsEnabled) return;

            var backupPath = await _backupService.CreateBackupAsync();
            var caption = string.IsNullOrWhiteSpace(note) ? "Veritaban\u0131 yede\u011Fi" : note;

            await _telegram.SendDocumentAsync(_settings.ChatId, backupPath, caption);
        }

        public async Task SendTextAsync(string text)
        {
            if (!_settings.IsEnabled) return;
            if (string.IsNullOrWhiteSpace(text)) return;

            await _telegram.SendTextAsync(_settings.ChatId, text);
        }

        public async Task SendDailyReportAndBackupAsync(DateTime date, string? note = null)
        {
            if (!_settings.IsEnabled) return;

            await SendDailyReportAsync(date, note);
            await SendBackupAsync("Kapan\u0131\u015F yede\u011Fi");
        }

        private static string BuildReportText(DailyReport r, string? note)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"G\u00FCnl\u00FCk Gelir/Gider Raporu ({r.Date:yyyy-MM-dd})");

            if (!string.IsNullOrWhiteSpace(note))
                sb.AppendLine(note);

            sb.AppendLine($"Gelir: {r.IncomeTotal:n2}");
            sb.AppendLine($"Gider: {r.ExpenseTotal:n2}");
            sb.AppendLine($"Net: {(r.IncomeTotal - r.ExpenseTotal):n2}");
            sb.AppendLine($"İşlem: {r.IncomeCount + r.ExpenseCount} (Gelir {r.IncomeCount}, Gider {r.ExpenseCount})");
            sb.AppendLine();
            sb.AppendLine("Ödeme Yöntemleri:");

            foreach (var method in new[] { "Nakit", "KrediKarti", "OnlineOdeme", "Havale" })
            {
                var row = r.PaymentMethodBreakdowns
                    .FirstOrDefault(x => string.Equals(x.Method, method, StringComparison.OrdinalIgnoreCase));
                var income = row?.IncomeTotal ?? 0m;
                var expense = row?.ExpenseTotal ?? 0m;
                sb.AppendLine($"- {GetMethodLabel(method)}: Gelir {income:n2} | Gider {expense:n2} | Net {(income - expense):n2}");
            }

            return sb.ToString().Trim();
        }

        private static string GetMethodLabel(string method)
        {
            return method switch
            {
                "KrediKarti" => "Kredi Kartı",
                "OnlineOdeme" => "Online Ödeme",
                _ => method
            };
        }
    }
}
