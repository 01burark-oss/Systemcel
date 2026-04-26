using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private async Task RunDeferredUpdateCheckAsync()
        {
            if (_hasDeferredUpdateCheckStarted || !_updateSettings.IsConfigured)
                return;

            _hasDeferredUpdateCheckStarted = true;

            try
            {
                var delaySeconds = Math.Max(10, _updateSettings.AutoCheckDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                if (IsDisposed || Disposing)
                    return;

                var result = await FetchUpdateResultAsync(forceRefresh: true);
                if (!result.HasUpdate)
                    return;

                MarkUpdateAvailable(result);
            }
            catch
            {
                // Background checks should never block the user.
            }
        }

        private void OpenBotSettings(Button navButton)
        {
            var form = new InitialSetupForm(_telegramSettings.BotToken, _telegramSettings.ChatId, _telegramSettings.AllowedUserIds, true);
            form.FormClosed += (_, __) =>
            {
                if (form.DialogResult != DialogResult.OK)
                    return;

                UserTelegramSetupStore.Save(_runtimeOptions.AppDataPath, new UserTelegramSetup
                {
                    BotToken = form.BotToken,
                    ChatId = form.ChatId,
                    AllowedUserIds = form.AllowedUserIds
                });

                MessageBox.Show(
                    AppLocalization.T("main.bot.savedBody"),
                    AppLocalization.T("main.bot.savedTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                Application.Restart();
                Close();
            };

            ShowEmbeddedForm(AppLocalization.T("main.nav.bot"), navButton, form);
        }

        private async Task CheckForUpdatesAsync(Button triggerButton)
        {
            if (!_updateSettings.IsConfigured)
            {
                MessageBox.Show(
                    AppLocalization.T("main.update.missingConfigBody"),
                    AppLocalization.T("main.update.missingConfigTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var originalText = triggerButton.Text;
            triggerButton.Enabled = false;
            triggerButton.Text = AppLocalization.T("main.update.checkingButton");

            try
            {
                var result = _cachedUpdateResult is { HasUpdate: true }
                    ? _cachedUpdateResult
                    : await FetchUpdateResultAsync(forceRefresh: true);

                if (!result.HasUpdate)
                {
                    ClearUpdateBadge();
                    MessageBox.Show(
                        AppLocalization.F("main.update.upToDateBody", Application.ProductVersion ?? string.Empty),
                        AppLocalization.T("main.update.upToDateTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                MarkUpdateAvailable(result);
                await PromptAndInstallUpdateAsync(triggerButton, result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("main.update.errorBody", ex.Message),
                    AppLocalization.T("main.update.errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                triggerButton.Enabled = true;
                triggerButton.Text = originalText;
            }
        }

        private async Task<UpdateCheckResult> FetchUpdateResultAsync(bool forceRefresh)
        {
            if (!forceRefresh && _cachedUpdateResult is not null)
                return _cachedUpdateResult;

            var currentVersion = Application.ProductVersion ?? string.Empty;
            _cachedUpdateResult = await _updateService.CheckAsync(_updateSettings, currentVersion);
            return _cachedUpdateResult;
        }

        private void MarkUpdateAvailable(UpdateCheckResult result)
        {
            _cachedUpdateResult = result;
            if (_lblUpdateBadge is null)
                return;

            _lblUpdateBadge.Text = AppLocalization.T("main.badge.updateAvailable");
            _lblUpdateBadge.Visible = result.HasUpdate;
        }

        private void ClearUpdateBadge()
        {
            _cachedUpdateResult = null;
            if (_lblUpdateBadge is not null)
                _lblUpdateBadge.Visible = false;
        }

        private async Task PromptAndInstallUpdateAsync(Button triggerButton, UpdateCheckResult result)
        {
            var installPrompt = result.CanInstallInApp
                ? "Guncelleme paketini indirip kurmak istiyor musunuz?"
                : "Bu surum icin otomatik kurulum paketi yayinlanmamis. Release sayfasini acmak istiyor musunuz?";
            var message =
                $"Yeni surum: {result.LatestTag}\n\n" +
                $"Notlar:\n{(string.IsNullOrWhiteSpace(result.ReleaseNotes) ? "- Not yok -" : result.ReleaseNotes)}\n\n" +
                installPrompt;

            var confirm = MessageBox.Show(
                message,
                AppLocalization.T("main.update.availableTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (confirm != DialogResult.Yes)
                return;

            if (!result.CanInstallInApp)
            {
                var targetUrl = string.IsNullOrWhiteSpace(result.ReleasePageUrl)
                    ? result.AssetDownloadUrl
                    : result.ReleasePageUrl;
                if (string.IsNullOrWhiteSpace(targetUrl))
                    throw new InvalidOperationException("Guncelleme sayfasi bulunamadi.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(result.AssetDownloadUrl))
                throw new InvalidOperationException(AppLocalization.T("main.update.packageMissing"));

            var expectedSha256 = await _updateService.ResolveExpectedSha256Async(result);
            if (string.IsNullOrWhiteSpace(expectedSha256))
                throw new InvalidOperationException(AppLocalization.T("main.update.checksumMissingBody"));

            var packageFileName = string.IsNullOrWhiteSpace(result.AssetName)
                ? "CashTracker-Setup.exe"
                : result.AssetName;

            var packagePath = await _updateService.DownloadAssetAsync(
                result.AssetDownloadUrl,
                packageFileName,
                _runtimeOptions.AppDataPath);

            GitHubUpdateService.VerifyDownloadedAssetHash(packagePath, expectedSha256);

            if (!InstallerLaunchService.TryScheduleInstall(packagePath, Process.GetCurrentProcess().Id))
                throw new InvalidOperationException("Installer baslatilamadi.");

            MessageBox.Show(
                AppLocalization.T("main.update.startedBody"),
                AppLocalization.T("main.update.startedTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            triggerButton.Enabled = false;
            Application.Exit();
        }
    }
}
