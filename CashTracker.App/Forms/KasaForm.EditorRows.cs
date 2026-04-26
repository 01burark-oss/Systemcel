using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private const int OdemeButtonWidth = 150;
        private const int OdemeButtonGapX = 12;
        private const int OdemeButtonGapY = 8;
        private const int EditorLabelWidth = 120;

        private static TableLayoutPanel CreateEditorForm()
        {
            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                AutoSize = false,
                AutoScroll = false,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, EditorLabelWidth));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return form;
        }

        private void AddTypeRow(TableLayoutPanel form)
        {
            var label = CreateEditorLabel(AppLocalization.T("common.type"));
            _cmbTip = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };

            _cmbTip.Items.AddRange(new object[]
            {
                AppLocalization.T("tip.income"),
                AppLocalization.T("tip.expense")
            });
            _cmbTip.SelectedIndex = 0;
            _cmbTip.SelectedIndexChanged += async (_, __) =>
            {
                ApplyTipButtonStyles();
                await LoadKalemlerForTipAsync();
                UpdateStockLinkUi();
            };

            var typePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 40,
                MinimumSize = new Size(0, 40),
                Margin = new Padding(0, 2, 0, 2),
                BackColor = Color.Transparent
            };
            _btnTipGelir = CreateChoiceButton(AppLocalization.T("tip.income"), 104, 38);
            _btnTipGider = CreateChoiceButton(AppLocalization.T("tip.expense"), 104, 38);
            _btnTipGelir.Click += (_, __) => SetSelectedTip("Gelir");
            _btnTipGider.Click += (_, __) => SetSelectedTip("Gider");
            typePanel.Controls.Add(_btnTipGelir);
            typePanel.Controls.Add(_btnTipGider);
            typePanel.Controls.Add(_cmbTip);

            form.Controls.Add(label);
            form.Controls.Add(typePanel);
            ApplyTipButtonStyles();
        }

        private void AddAmountRow(TableLayoutPanel form)
        {
            var label = CreateEditorLabel(AppLocalization.T("common.amount"));
            var inputFont = BrandTheme.CreateFont(11.5f);
            _txtTutar = new TextBox
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Margin = new Padding(0),
                BorderStyle = BorderStyle.None,
                Font = inputFont,
                Height = UiMetrics.GetTextLineHeight(inputFont) + 4,
                PlaceholderText = AppLocalization.T("kasa.amount.placeholder")
            };
            _txtTutar.KeyPress += AmountTextBoxKeyPress;

            var amountFrame = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                MinimumSize = new Size(0, 40),
                Margin = new Padding(0, 2, 0, 2),
                BackColor = Color.White
            };
            ApplyRoundedRegion(amountFrame, 8);
            amountFrame.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedPath(new Rectangle(0, 0, amountFrame.Width - 1, amountFrame.Height - 1), 8);
                using var pen = new Pen(Color.FromArgb(157, 166, 179), 1.2f);
                e.Graphics.DrawPath(pen, path);
                using var splitPen = new Pen(Color.FromArgb(180, 188, 200), 1f);
                e.Graphics.DrawLine(splitPen, 54, 0, 54, amountFrame.Height);
            };
            var prefix = new Label
            {
                Text = "TL",
                Dock = DockStyle.Left,
                Width = 54,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandTheme.CreateHeadingFont(11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Margin = new Padding(0)
            };
            var inputHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 7, 10, 5),
                BackColor = Color.White
            };
            inputHost.Controls.Add(_txtTutar);
            amountFrame.Controls.Add(inputHost);
            amountFrame.Controls.Add(prefix);

            form.Controls.Add(label);
            form.Controls.Add(amountFrame);
        }

        private void AddPaymentMethodRow(TableLayoutPanel form)
        {
            var label = CreateEditorLabel(AppLocalization.T("common.method"));
            var paymentButtonHeight = UiMetrics.GetCompactButtonHeight(BrandTheme.CreateHeadingFont(10f, FontStyle.Bold));

            var methodsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 3,
                AutoSize = false,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Height = (paymentButtonHeight * 2) + OdemeButtonGapY,
                MinimumSize = new Size(0, (paymentButtonHeight * 2) + OdemeButtonGapY),
                Margin = new Padding(0, 2, 0, 4),
                Padding = new Padding(0)
            };
            methodsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            methodsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OdemeButtonGapX));
            methodsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            methodsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, paymentButtonHeight));
            methodsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, OdemeButtonGapY));
            methodsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, paymentButtonHeight));

            _btnOdemeNakit = CreateOdemeYontemiButton(AppLocalization.T("payment.cash"), "Nakit");
            _btnOdemeKrediKarti = CreateOdemeYontemiButton(AppLocalization.T("payment.card"), "KrediKarti");
            _btnOdemeOnlineOdeme = CreateOdemeYontemiButton(AppLocalization.T("payment.online"), "OnlineOdeme");
            _btnOdemeHavale = CreateOdemeYontemiButton(AppLocalization.T("payment.transfer"), "Havale");

            _btnOdemeNakit.Margin = Padding.Empty;
            _btnOdemeKrediKarti.Margin = Padding.Empty;
            _btnOdemeOnlineOdeme.Margin = Padding.Empty;
            _btnOdemeHavale.Margin = Padding.Empty;

            _btnOdemeNakit.Dock = DockStyle.Fill;
            _btnOdemeKrediKarti.Dock = DockStyle.Fill;
            _btnOdemeOnlineOdeme.Dock = DockStyle.Fill;
            _btnOdemeHavale.Dock = DockStyle.Fill;

            methodsPanel.Controls.Add(_btnOdemeNakit, 0, 0);
            methodsPanel.Controls.Add(_btnOdemeKrediKarti, 2, 0);
            methodsPanel.Controls.Add(_btnOdemeOnlineOdeme, 0, 2);
            methodsPanel.Controls.Add(_btnOdemeHavale, 2, 2);

            form.Controls.Add(label);
            form.Controls.Add(methodsPanel);

            SetSelectedOdemeYontemi("Nakit");
        }

        private static void AmountTextBoxKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            if ((e.KeyChar == ',' || e.KeyChar == '.') &&
                sender is TextBox textBox &&
                !textBox.Text.Contains(',') &&
                !textBox.Text.Contains('.'))
            {
                return;
            }

            e.Handled = true;
        }

        private static Button CreateOdemeYontemiBaseButton(string text)
        {
            var font = BrandTheme.CreateHeadingFont(10f, FontStyle.Bold);
            var buttonHeight = UiMetrics.GetCompactButtonHeight(font);
            var button = new Button
            {
                Text = text,
                Height = buttonHeight,
                MinimumSize = new Size(128, buttonHeight),
                AutoSize = false,
                AutoEllipsis = true,
                Margin = Padding.Empty,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(38, 53, 72),
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(190, 202, 216);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 247, 252);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(229, 239, 247);
            return button;
        }

        private void AddStockLinkRow(TableLayoutPanel form)
        {
            var label = CreateEditorLabel("Stok Girisi");

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = false,
                Height = 92,
                MinimumSize = new Size(0, 92),
                Margin = new Padding(0, 2, 0, 2),
                BackColor = Color.Transparent
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));

            _chkStokGiris = new CheckBox
            {
                Text = "Bu gider stoklu urun alimi",
                AutoSize = true,
                Margin = new Padding(0, 1, 0, 0)
            };
            _chkStokGiris.CheckedChanged += (_, __) => UpdateStockLinkUi();

            _cmbStokUrun = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                IntegralHeight = false,
                Font = BrandTheme.CreateFont(10f),
                Margin = new Padding(0, 0, 0, 3),
                Height = 26,
                MinimumSize = new Size(0, 26)
            };

            _numStokMiktar = new NumericUpDown
            {
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 1_000_000_000,
                Value = 1,
                Dock = DockStyle.Top,
                Font = BrandTheme.CreateFont(10f),
                Height = 26,
                MinimumSize = new Size(0, 26),
                Margin = new Padding(0, 0, 0, 3)
            };

            _lblStokGirisHint = new Label
            {
                Text = "Sadece yeni gider kaydinda stok hareketi olusturulur.",
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(106, 118, 136),
                Font = BrandTheme.CreateFont(8.2f),
                AutoEllipsis = true,
                Margin = new Padding(0)
            };

            panel.Controls.Add(_chkStokGiris, 0, 0);
            panel.Controls.Add(_cmbStokUrun, 0, 1);
            panel.Controls.Add(_numStokMiktar, 0, 2);
            panel.Controls.Add(_lblStokGirisHint, 0, 3);

            form.Controls.Add(label);
            form.Controls.Add(panel);
        }

        private Button CreateOdemeYontemiButton(string text, string value)
        {
            var button = CreateOdemeYontemiBaseButton(text);
            button.Tag = value;
            button.Image = CreateOdemeYontemiIcon(value);
            button.Click += (_, __) => SetSelectedOdemeYontemi(value);
            return button;
        }

        private static Label CreateEditorLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandTheme.CreateHeadingFont(10.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, 0, 12, 0)
            };
        }

        private static Button CreateChoiceButton(string text, int width, int height)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                Margin = new Padding(0, 0, 10, 0),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(31, 41, 55),
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateFont(10.8f),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(196, 205, 216);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 247, 253);
            return button;
        }

        private void SetSelectedTip(string tip)
        {
            var target = tip == "Gider" ? AppLocalization.T("tip.expense") : AppLocalization.T("tip.income");
            _cmbTip.SelectedItem = target;
            ApplyTipButtonStyles();
        }

        private void ApplyTipButtonStyles()
        {
            if (_btnTipGelir is null || _btnTipGider is null || _cmbTip is null)
                return;

            var tip = MapTip(_cmbTip.SelectedItem?.ToString());
            ApplyTypeButtonStyle(_btnTipGelir, tip == "Gelir");
            ApplyTypeButtonStyle(_btnTipGider, tip == "Gider");
        }

        private static void ApplyTypeButtonStyle(Button button, bool selected)
        {
            button.BackColor = selected ? Color.FromArgb(222, 235, 251) : Color.White;
            button.ForeColor = selected ? Color.FromArgb(18, 56, 98) : Color.FromArgb(31, 41, 55);
            button.Font = BrandTheme.CreateFont(10.8f, selected ? FontStyle.Bold : FontStyle.Regular);
            button.FlatAppearance.BorderColor = selected
                ? Color.FromArgb(55, 104, 171)
                : Color.FromArgb(196, 205, 216);
        }

        private static Bitmap CreateOdemeYontemiIcon(string value)
        {
            var icon = new Bitmap(16, 16);
            using var g = Graphics.FromImage(icon);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.FromArgb(49, 75, 106), 1.6f);
            using var fill = new SolidBrush(Color.FromArgb(223, 238, 249));
            using var accent = new SolidBrush(Color.FromArgb(36, 95, 163));

            var normalized = NormalizeOdemeYontemi(value);
            if (normalized == "KrediKarti")
            {
                var frame = new Rectangle(1, 3, 14, 10);
                using var path = CreateRoundedPath(frame, 3);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
                g.DrawLine(pen, 2, 6, 14, 6);
                g.FillRectangle(accent, 3, 9, 4, 2);
                return icon;
            }

            if (normalized == "OnlineOdeme")
            {
                var globe = new Rectangle(2, 2, 12, 12);
                g.FillEllipse(fill, globe);
                g.DrawEllipse(pen, globe);
                g.DrawLine(pen, 4, 8, 12, 8);
                g.FillEllipse(accent, 7, 7, 2, 2);
                return icon;
            }

            if (normalized == "Havale")
            {
                g.DrawLine(pen, 1, 5, 12, 5);
                g.FillPolygon(accent, new[]
                {
                    new Point(12, 2),
                    new Point(15, 5),
                    new Point(12, 8)
                });

                g.DrawLine(pen, 15, 11, 4, 11);
                g.FillPolygon(accent, new[]
                {
                    new Point(4, 8),
                    new Point(1, 11),
                    new Point(4, 14)
                });
                return icon;
            }

            var note = new Rectangle(1, 3, 14, 10);
            using (var path = CreateRoundedPath(note, 2))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            g.FillEllipse(accent, 6, 6, 4, 4);
            return icon;
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
