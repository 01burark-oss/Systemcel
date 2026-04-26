using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private FlowLayoutPanel CreateButtonPanel()
        {
            _btnSave = CreateButton(AppLocalization.T("common.save"), BrandTheme.Navy, Color.White);
            _btnNew = CreateButton(AppLocalization.T("common.new"), Color.White, BrandTheme.Navy);
            _btnDelete = CreateButton(AppLocalization.T("common.delete"), Color.White, BrandTheme.Navy);
            _btnRefresh = CreateButton(AppLocalization.T("common.refresh"), Color.White, BrandTheme.Navy);

            WireButtons();
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private void WireButtons()
        {
            _btnSave.Click += async (_, __) => await SaveAsync();
            _btnNew.Click += async (_, __) => await ClearFormAsync();
            _btnDelete.Click += async (_, __) => await DeleteAsync();
            _btnRefresh.Click += async (_, __) => await LoadAllAsync();
        }
    }
}
