using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramPollingService : IDisposable
    {
        private readonly TelegramSettings _settings;
        private readonly TelegramBotService _telegram;
        private readonly TelegramCommandService _commands;

        private readonly CancellationTokenSource _cts = new();
        private Task? _loopTask;
        private long? _offset;
        private string _commandsConfiguredForToken = string.Empty;

        public TelegramPollingService(
            TelegramSettings settings,
            TelegramBotService telegram,
            TelegramCommandService commands)
        {
            _settings = settings;
            _telegram = telegram;
            _commands = commands;
        }

        public void Start()
        {
            if (_loopTask != null) return;

            _loopTask = Task.Run(() => RunAsync(_cts.Token));
        }

        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!_settings.HasBotToken || !_settings.EnableCommands)
                    {
                        _offset = null;
                        _commandsConfiguredForToken = string.Empty;
                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                        continue;
                    }

                    await EnsureCommandMenuAsync(ct);

                    var updates = await _telegram.GetUpdatesAsync(
                        _offset,
                        _settings.GetSafePollTimeoutSeconds(),
                        ct);

                    foreach (var update in updates)
                    {
                        _offset = update.UpdateId + 1;
                        await _commands.ProcessUpdateAsync(update, ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"TelegramPollingService error: {ex}");
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }
        }

        public void NotifySettingsChanged()
        {
            _offset = null;
            _commandsConfiguredForToken = string.Empty;
            Start();
        }

        private async Task EnsureCommandMenuAsync(CancellationToken ct)
        {
            var token = _settings.BotToken.Trim();
            if (string.IsNullOrWhiteSpace(token) || string.Equals(_commandsConfiguredForToken, token, StringComparison.Ordinal))
                return;

            try
            {
                await _telegram.SetCommandsAsync(TelegramCommandCatalog.BotMenu, ct);
                _commandsConfiguredForToken = token;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramPollingService command menu error: {ex}");
            }
        }

        public void Dispose()
        {
            if (_cts.IsCancellationRequested)
                return;

            _cts.Cancel();

            try
            {
                _loopTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramPollingService dispose error: {ex}");
            }

            _cts.Dispose();
        }
    }
}
