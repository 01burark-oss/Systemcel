using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramApprovalService : ITelegramApprovalService
    {
        private readonly TelegramBotService _telegram;
        private readonly TelegramSettings _settings;
        private readonly ConcurrentDictionary<string, PendingApproval> _pending =
            new(StringComparer.OrdinalIgnoreCase);

        public TelegramApprovalService(
            TelegramBotService telegram,
            TelegramSettings settings)
        {
            _telegram = telegram;
            _settings = settings;
        }

        public async Task<TelegramApprovalResult> RequestApprovalAsync(
            TelegramApprovalRequest request,
            CancellationToken ct = default)
        {
            if (!_settings.IsEnabled || !_settings.EnableCommands)
                return new TelegramApprovalResult(TelegramApprovalStatus.NotConfigured);

            var timeout = request.Timeout <= TimeSpan.Zero
                ? TimeSpan.FromMinutes(2)
                : request.Timeout;

            var tcs = new TaskCompletionSource<TelegramApprovalStatus>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var code = GenerateUniqueCode();
            if (code is null)
                return new TelegramApprovalResult(TelegramApprovalStatus.Failed, "Onay kodu üretilemedi.");

            var pending = new PendingApproval(request.Title, request.Details, tcs);
            if (!_pending.TryAdd(code, pending))
                return new TelegramApprovalResult(TelegramApprovalStatus.Failed, "Onay kaydı başlatılamadı.");

            try
            {
                var message = BuildApprovalMessage(request, code, timeout);
                await _telegram.SendTextAsync(_settings.ChatId, message, ct);
            }
            catch (Exception ex)
            {
                _pending.TryRemove(code, out _);
                return new TelegramApprovalResult(TelegramApprovalStatus.Failed, ex.Message);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            try
            {
                var status = await tcs.Task.WaitAsync(timeoutCts.Token);
                return new TelegramApprovalResult(status);
            }
            catch (OperationCanceledException)
            {
                _pending.TryRemove(code, out _);
                return new TelegramApprovalResult(TelegramApprovalStatus.TimedOut);
            }
        }

        public bool TryResolve(string code, bool approved, out string? title)
        {
            title = null;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalized = code.Trim();
            if (!_pending.TryRemove(normalized, out var pending))
                return false;

            title = pending.Title;
            var status = approved ? TelegramApprovalStatus.Approved : TelegramApprovalStatus.Rejected;
            pending.Completion.TrySetResult(status);
            return true;
        }

        private string? GenerateUniqueCode()
        {
            for (var i = 0; i < 6; i++)
            {
                var code = RandomNumberGenerator.GetInt32(100000, 1000000)
                    .ToString();
                if (!_pending.ContainsKey(code))
                    return code;
            }

            return null;
        }

        private static string BuildApprovalMessage(TelegramApprovalRequest request, string code, TimeSpan timeout)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Onay gerekiyor.");
            sb.AppendLine($"İşlem: {request.Title}");
            if (!string.IsNullOrWhiteSpace(request.Details))
                sb.AppendLine(request.Details.Trim());
            sb.AppendLine();
            sb.AppendLine($"Onaylamak için: /onay {code}");
            sb.AppendLine($"Reddetmek için: /iptal {code}");
            sb.AppendLine($"Süre: {Math.Max(1, (int)Math.Ceiling(timeout.TotalMinutes))} dk");
            return sb.ToString().Trim();
        }

        private sealed class PendingApproval
        {
            public string Title { get; }
            public string Details { get; }
            public TaskCompletionSource<TelegramApprovalStatus> Completion { get; }

            public PendingApproval(
                string title,
                string details,
                TaskCompletionSource<TelegramApprovalStatus> completion)
            {
                Title = title;
                Details = details;
                Completion = completion;
            }
        }
    }
}
