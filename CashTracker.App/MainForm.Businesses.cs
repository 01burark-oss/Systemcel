using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Entities;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void OpenSettingsDialog()
        {
            ShowSettingsPage();
        }

        private async Task ShowBusinessSelectorMenuAsync()
        {
            if (_btnBusinessSelector is null)
                return;

            _btnBusinessSelector.Enabled = false;
            try
            {
                var businesses = await _isletmeService.GetAllAsync();
                BuildBusinessSelectorMenu(businesses);
                _businessSelectorMenu?.Show(_btnBusinessSelector, new Point(0, _btnBusinessSelector.Height + 6));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.businessLoad", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                _btnBusinessSelector.Enabled = true;
            }
        }

        private void BuildBusinessSelectorMenu(IReadOnlyList<Isletme> businesses)
        {
            _businessSelectorMenu?.Dispose();

            var menu = new ContextMenuStrip
            {
                AutoSize = false,
                Width = Math.Max(_btnBusinessSelector.Width, 260),
                ShowCheckMargin = true,
                ShowImageMargin = false,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(17, 24, 39),
                Font = BrandTheme.CreateFont(10.4f),
                Padding = new Padding(8)
            };

            foreach (var business in businesses)
            {
                var item = new ToolStripMenuItem(business.Ad)
                {
                    Checked = business.IsAktif,
                    Font = business.IsAktif
                        ? BrandTheme.CreateFont(10.4f, FontStyle.Bold)
                        : BrandTheme.CreateFont(10.4f, FontStyle.Regular),
                    Padding = new Padding(10, 8, 10, 8)
                };
                item.Click += async (_, __) => await ActivateBusinessFromMenuAsync(business);
                menu.Items.Add(item);
            }

            if (businesses.Count > 0)
                menu.Items.Add(new ToolStripSeparator());

            var addItem = new ToolStripMenuItem(AppLocalization.T("settings.button.addBusiness"))
            {
                Font = BrandTheme.CreateFont(10.4f, FontStyle.Bold),
                Padding = new Padding(10, 8, 10, 8)
            };
            addItem.Click += (_, __) => OpenSettingsDialog();
            menu.Items.Add(addItem);

            var rowCount = businesses.Count + 1 + (businesses.Count > 0 ? 1 : 0);
            menu.Size = new Size(Math.Max(_btnBusinessSelector.Width, 260), Math.Max(48, rowCount * 34));

            _businessSelectorMenu = menu;
        }

        private async Task ActivateBusinessFromMenuAsync(Isletme business)
        {
            if (business.IsAktif)
                return;

            try
            {
                await _isletmeService.SetActiveAsync(business.Id);
                await RefreshSummariesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.businessActivate", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string FormatBusinessSelectorText(string? businessName)
        {
            var value = string.IsNullOrWhiteSpace(businessName)
                ? "-"
                : businessName.Trim();

            return $"Isletme: {value}";
        }
    }
}
