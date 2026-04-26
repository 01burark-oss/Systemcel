using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildEditorSection(Panel right)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = System.Drawing.Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.Controls.Add(layout);

            var header = CreateSectionHeader("Islem Kayit Formu", string.Empty);
            header.Margin = new Padding(0, 0, 0, 14);
            layout.Controls.Add(header, 0, 0);

            var form = CreateEditorForm();
            layout.Controls.Add(form, 0, 1);

            AddRow(form, AppLocalization.T("common.date"), out _dtTarih);
            _dtTarih.Enabled = true;

            AddTypeRow(form);
            AddAmountRow(form);
            AddPaymentMethodRow(form);
            AddRow(form, AppLocalization.T("common.category"), out _cmbKalem);
            AddKalemEmptyActionRow(form);
            AddStockLinkRow(form);
            AddRow(form, AppLocalization.T("common.description"), out _txtAciklama);

            _btnKalemSettings.Click += async (_, __) => await OpenSettingsForKalemManagementAsync();

            var buttons = CreateButtonPanel();
            buttons.Controls.AddRange(new Control[] { _btnSave, _btnNew, _btnDelete, _btnRefresh });
            buttons.Margin = new Padding(0, 12, 0, 0);
            layout.Controls.Add(buttons, 0, 2);
        }
    }
}


