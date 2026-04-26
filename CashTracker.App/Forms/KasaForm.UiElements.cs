using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static Panel CreateSectionHeader(string title, string subtitle)
        {
            var titleFont = BrandTheme.CreateHeadingFont(17.5f, FontStyle.Bold);
            var subtitleFont = BrandTheme.CreateFont(9.4f, FontStyle.Regular);
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, string.IsNullOrWhiteSpace(subtitle)
                    ? UiMetrics.GetTextLineHeight(titleFont) + 8
                    : UiMetrics.GetHeaderHeight(titleFont, subtitleFont, 18, 2))
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var titleLabel = new Label
            {
                Text = title,
                Font = titleFont,
                ForeColor = Color.FromArgb(42, 50, 61),
                AutoSize = true,
                Margin = new Padding(0)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            if (string.IsNullOrWhiteSpace(subtitle))
                return panel;

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(106, 118, 136),
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 0)
            };
            layout.Controls.Add(subtitleLabel, 0, 1);

            return panel;
        }

        private static Button CreateButton(string text, Color back, Color fore)
        {
            var font = BrandTheme.CreateHeadingFont(10.4f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                Width = 104,
                Height = UiMetrics.GetButtonHeight(font, 44),
                MinimumSize = new Size(104, UiMetrics.GetButtonHeight(font, 44)),
                BackColor = back,
                ForeColor = fore,
                Font = font,
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0),
                Padding = UiMetrics.ButtonPadding,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(back.R - 12, 0),
                Math.Max(back.G - 12, 0),
                Math.Max(back.B - 12, 0));

            return button;
        }
    }
}
