using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static void AddRow(TableLayoutPanel panel, string label, out TextBox textBox)
        {
            var inputFont = BrandTheme.CreateFont(11.2f);
            var isDescription = label == AppLocalization.T("common.description");
            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = isDescription ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft,
                Font = BrandTheme.CreateHeadingFont(10.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, isDescription ? 10 : 0, 12, 0)
            };
            textBox = new TextBox
            {
                Dock = isDescription ? DockStyle.Fill : DockStyle.Top,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = isDescription ? new Padding(0, 6, 0, 0) : new Padding(0, 2, 0, 2),
                Font = inputFont,
                Height = isDescription ? 72 : 40,
                MinimumSize = new Size(0, isDescription ? 72 : 40),
                Multiline = isDescription,
                ScrollBars = isDescription ? ScrollBars.Vertical : ScrollBars.None
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(textBox);
        }

        private static void AddRow(TableLayoutPanel panel, string label, out ComboBox comboBox)
        {
            var comboFont = BrandTheme.CreateFont(11.2f);
            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandTheme.CreateHeadingFont(10.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, 0, 12, 0)
            };

            comboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 2, 0, 2),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                IntegralHeight = false,
                Font = comboFont,
                Height = 40,
                MinimumSize = new Size(0, 40)
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(comboBox);
        }

        private void AddKalemEmptyActionRow(TableLayoutPanel panel)
        {
            var left = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                Margin = Padding.Empty
            };

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 2)
            };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblKalemEmptyHint = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(0, 0, 0, 6),
                Visible = false
            };

            _btnKalemSettings = CreateButton(AppLocalization.T("kasa.manageCategories"), BrandTheme.Teal, Color.White);
            _btnKalemSettings.Width = 146;
            _btnKalemSettings.MinimumSize = new Size(146, UiMetrics.GetButtonHeight(_btnKalemSettings.Font));
            _btnKalemSettings.Margin = new Padding(0);
            _btnKalemSettings.Anchor = AnchorStyles.Left;
            _btnKalemSettings.Visible = false;

            right.Controls.Add(_lblKalemEmptyHint, 0, 0);
            right.Controls.Add(_btnKalemSettings, 0, 1);

            panel.Controls.Add(left);
            panel.Controls.Add(right);
        }

        private static void AddRow(TableLayoutPanel panel, string label, out DateTimePicker dtp)
        {
            var pickerFont = BrandTheme.CreateFont(11.2f);
            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandTheme.CreateHeadingFont(10.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, 0, 12, 0)
            };
            var wrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                MinimumSize = new Size(0, 40),
                Margin = new Padding(0, 2, 0, 2),
                Padding = new Padding(10, 3, 10, 3),
                BackColor = Color.White
            };
            wrapper.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(157, 166, 179), 1.2f);
                e.Graphics.DrawRectangle(pen, 0, 0, wrapper.Width - 1, wrapper.Height - 1);
            };
            dtp = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Font = pickerFont,
                MinimumSize = new Size(0, 30)
            };
            wrapper.Controls.Add(dtp);
            panel.Controls.Add(lbl);
            panel.Controls.Add(wrapper);
        }
    }
}
