using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.UI;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void ShowEmbeddedForm(string title, Button? navButton, Form form, bool refreshAfterClose = false)
        {
            if (_contentHost is null)
            {
                form.Dispose();
                return;
            }

            SuspendLayout();
            _contentHost.SuspendLayout();

            var oldForm = _activeEmbeddedForm;
            _activeEmbeddedForm = null;
            oldForm?.Dispose();
            if (oldForm is not null)
                _ = RefreshSummariesAsync();

            _contentHost.Controls.Clear();
            _dashboardViewport.Visible = false;

            SetActivePageTitle(title);
            SetActiveNavButton(navButton);

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.WindowState = FormWindowState.Normal;
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.Dock = DockStyle.Fill;
            form.BackColor = BrandTheme.AppBackground;
            form.FormClosed += (_, __) =>
            {
                if (!ReferenceEquals(_activeEmbeddedForm, form))
                    return;

                _activeEmbeddedForm = null;
                if (refreshAfterClose)
                    _ = RefreshSummariesAsync();

                ShowDashboardPage();
            };

            _activeEmbeddedForm = form;
            _contentHost.Controls.Add(form);
            form.Show();
            form.BringToFront();

            _contentHost.ResumeLayout(true);
            ResumeLayout(true);
        }

        private void ShowDashboardPage()
        {
            if (_contentHost is null || _dashboardViewport is null)
                return;

            var oldForm = _activeEmbeddedForm;
            _activeEmbeddedForm = null;
            oldForm?.Dispose();

            _contentHost.SuspendLayout();
            _contentHost.Controls.Clear();
            _dashboardViewport.Visible = true;
            _dashboardViewport.Dock = DockStyle.Fill;
            _contentHost.Controls.Add(_dashboardViewport);
            _dashboardViewport.BringToFront();
            _contentHost.ResumeLayout(true);

            SetActivePageTitle("Hizli Finansal Ozet");
            SetActiveNavButton(null);
            _ = RefreshSummariesAsync();
        }

        private void ShowSettingsPage()
        {
            ShowEmbeddedForm(
                AppLocalization.T("settings.title"),
                null,
                new SettingsForm(
                    _isletmeService,
                    _kalemTanimiService,
                    _telegramApprovalService,
                    _runtimeOptions,
                    _appSecurityService,
                    _licenseService,
                    _receiptOcrSettings),
                refreshAfterClose: true);
        }

        private void SetActivePageTitle(string title)
        {
            if (_lblActivePageTitle is null)
                return;

            _lblActivePageTitle.Text = title;
        }

        private void SetActiveNavButton(Button? activeButton)
        {
            _activeNavButton = activeButton;
            foreach (var button in _sidebarButtons)
                ApplySidebarButtonState(button, ReferenceEquals(button, activeButton));
        }

        private static void ApplySidebarButtonState(Button button, bool isActive)
        {
            var normalBack = Color.FromArgb(18, 31, 54);
            var activeBack = Color.FromArgb(43, 61, 98);
            var normalText = Color.FromArgb(233, 239, 247);
            var activeText = Color.White;
            var hoverBack = Color.FromArgb(29, 47, 79);
            var activeHover = Color.FromArgb(50, 70, 110);
            var iconKey = button.Tag as string ?? string.Empty;
            var foreColor = isActive ? activeText : normalText;

            button.BackColor = isActive ? activeBack : normalBack;
            button.ForeColor = foreColor;
            button.Font = BrandTheme.CreateFont(11f, isActive ? FontStyle.Bold : FontStyle.Regular);
            button.Image = DashboardIconFactory.Create(iconKey, foreColor, 20);
            button.FlatAppearance.MouseDownBackColor = isActive ? activeHover : hoverBack;
            button.FlatAppearance.MouseOverBackColor = isActive ? activeHover : hoverBack;
        }
    }
}
