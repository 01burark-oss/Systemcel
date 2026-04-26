using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildUi()
        {
            _rootLayout = CreateRootLayout();
            _leftPanel = CreateSurfacePanel();
            _rightPanel = CreateSurfacePanel();
            _leftPanel.Padding = new Padding(24, 22, 24, 22);
            _rightPanel.Padding = new Padding(24, 22, 24, 22);
            _leftPanel.Margin = new Padding(0, 0, 12, 0);
            _rightPanel.Margin = new Padding(12, 0, 0, 0);
            _rightPanel.AutoScroll = false;

            _rootLayout.Controls.Add(_leftPanel, 0, 0);
            _rootLayout.Controls.Add(_rightPanel, 1, 0);
            Controls.Add(_rootLayout);

            BuildGridSection(_leftPanel);
            BuildEditorSection(_rightPanel);
            ApplyResponsiveLayout();
        }

        private static TableLayoutPanel CreateRootLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(34, 30, 34, 26),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BrandTheme.AppBackground
            };

            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            return root;
        }

        private static Label CreateActiveBusinessLabel()
        {
            var font = BrandTheme.CreateFont(9.2f, FontStyle.Bold);
            var bannerHeight = UiMetrics.GetBannerHeight(font, 20, 44);
            return new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = bannerHeight,
                MinimumSize = new Size(0, bannerHeight),
                Font = font,
                ForeColor = Color.FromArgb(30, 74, 120),
                BackColor = Color.FromArgb(231, 241, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = AppLocalization.F("kasa.activeBusiness", "-")
            };
        }

        private static Panel CreateSurfacePanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            UiMetrics.EnableDoubleBuffer(panel);
            panel.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var path = CreateRoundedPath(rect, 18);
                using var pen = new Pen(Color.FromArgb(211, 220, 232), 1.1f);
                e.Graphics.DrawPath(pen, path);
            };
            return panel;
        }

        private static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            using var path = CreateRoundedPath(new Rectangle(0, 0, control.Width - 1, control.Height - 1), radius);
            var oldRegion = control.Region;
            control.Region = new Region(path);
            oldRegion?.Dispose();
        }
    }
}
