using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CashTracker.App.Controls;
using CashTracker.App.Forms;
using CashTracker.App.UI;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void BuildUi()
        {
            var sidebarBackground = Color.FromArgb(18, 31, 54);
            var sidebarHover = Color.FromArgb(29, 47, 79);
            var sidebarText = Color.FromArgb(233, 239, 247);
            var sidebarMuted = Color.FromArgb(156, 172, 196);

            var contentBackground = Color.FromArgb(237, 242, 249);
            var surface = Color.White;
            var border = Color.FromArgb(220, 228, 239);
            var heading = Color.FromArgb(17, 24, 39);
            var muted = Color.FromArgb(101, 115, 137);
            var incomeColor = Color.FromArgb(23, 134, 92);
            var expenseColor = Color.FromArgb(177, 40, 40);
            var navyAccent = Color.FromArgb(35, 61, 104);

            SuspendLayout();
            Controls.Clear();

            void ApplyRoundedRegion(Control control, int radius)
            {
                void UpdateRegion()
                {
                    if (control.Width <= 0 || control.Height <= 0)
                        return;

                    using var path = CreateRoundedPath(new Rectangle(0, 0, control.Width - 1, control.Height - 1), radius);
                    var oldRegion = control.Region;
                    control.Region = new Region(path);
                    oldRegion?.Dispose();
                }

                control.HandleCreated += (_, __) => UpdateRegion();
                control.Resize += (_, __) => UpdateRegion();
                UpdateRegion();
            }

            void ApplyCircleRegion(Control control)
            {
                void UpdateRegion()
                {
                    if (control.Width <= 0 || control.Height <= 0)
                        return;

                    using var path = new GraphicsPath();
                    path.AddEllipse(0, 0, control.Width - 1, control.Height - 1);
                    var oldRegion = control.Region;
                    control.Region = new Region(path);
                    oldRegion?.Dispose();
                }

                control.HandleCreated += (_, __) => UpdateRegion();
                control.Resize += (_, __) => UpdateRegion();
                UpdateRegion();
            }

            GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
            {
                var diameter = Math.Max(radius * 2, 1);
                var path = new GraphicsPath();
                path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }

            Panel CreateCardPanel(Padding padding, int minHeight = 0)
            {
                var panel = new Panel
                {
                    BackColor = surface,
                    Padding = padding,
                    Margin = new Padding(0),
                    MinimumSize = new Size(0, minHeight),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                ApplyRoundedRegion(panel, 24);
                panel.Paint += (_, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                    using var path = CreateRoundedPath(rect, 24);
                    using var pen = new Pen(border, 1.1f);
                    e.Graphics.DrawPath(pen, path);
                };
                return panel;
            }

            Button CreateSidebarButton(string text, string iconKey)
            {
                var button = new Button
                {
                    Text = text,
                    Width = 252,
                    Height = 52,
                    Margin = new Padding(0, 0, 0, 10),
                    BackColor = sidebarBackground,
                    ForeColor = sidebarText,
                    FlatStyle = FlatStyle.Flat,
                    Font = BrandTheme.CreateFont(11f, FontStyle.Regular),
                    Image = DashboardIconFactory.Create(iconKey, sidebarText, 20),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(18, 0, 0, 0),
                    Tag = iconKey
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseDownBackColor = sidebarHover;
                button.FlatAppearance.MouseOverBackColor = sidebarHover;
                _sidebarButtons.Add(button);
                return button;
            }

            Button CreateCircleIconButton(string iconKey, Color backColor, Color iconColor, int size = 58)
            {
                var button = new Button
                {
                    Width = size,
                    Height = size,
                    BackColor = backColor,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(0),
                    Image = DashboardIconFactory.Create(iconKey, iconColor, Math.Max(18, size / 2))
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseDownBackColor = backColor;
                button.FlatAppearance.MouseOverBackColor = backColor;
                ApplyCircleRegion(button);
                return button;
            }

            Button CreateGhostIconButton(string iconKey, Color iconColor, int size = 42, int iconSize = 28)
            {
                var button = new Button
                {
                    Width = size,
                    Height = size,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(0),
                    Image = DashboardIconFactory.Create(iconKey, iconColor, iconSize),
                    TabStop = false
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseDownBackColor = Color.Transparent;
                button.FlatAppearance.MouseOverBackColor = Color.Transparent;
                return button;
            }

            Label CreatePillLabel(string text, Color backColor, Color foreColor)
            {
                var label = new Label
                {
                    Text = text,
                    AutoSize = true,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    Font = BrandTheme.CreateFont(10.2f, FontStyle.Bold),
                    Padding = new Padding(14, 8, 14, 8),
                    Margin = new Padding(0)
                };
                ApplyRoundedRegion(label, 14);
                return label;
            }

            SummaryCard CreatePeriodCard(string title, string defaultRangeCode)
            {
                var card = CreateCardPanel(new Padding(18, 18, 18, 18), 250);
                card.Dock = DockStyle.Fill;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 1,
                    RowCount = 2,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BackColor = Color.Transparent
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                card.Controls.Add(layout);

                var titleLabel = new Label
                {
                    Text = title,
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(16.5f, FontStyle.Bold),
                    ForeColor = heading,
                    Margin = new Padding(0, 0, 0, 14)
                };
                layout.Controls.Add(titleLabel, 0, 0);

                var grid = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    RowCount = 3,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BackColor = Color.Transparent
                };
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.Controls.Add(grid, 0, 1);

                var incomeCaption = new Label
                {
                    Text = "Gelir:",
                    AutoSize = true,
                    Font = BrandTheme.CreateFont(13.5f, FontStyle.Regular),
                    ForeColor = heading,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 0, 0, 10)
                };
                var expenseCaption = new Label
                {
                    Text = "Gider:",
                    AutoSize = true,
                    Font = BrandTheme.CreateFont(13.5f, FontStyle.Regular),
                    ForeColor = heading,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 0, 0, 10)
                };
                var netCaption = new Label
                {
                    Text = "Net:",
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(13.5f, FontStyle.Bold),
                    ForeColor = heading,
                    Anchor = AnchorStyles.Left
                };

                var incomeValue = new Label
                {
                    Text = FormatAmount(0m),
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(14.5f, FontStyle.Bold),
                    ForeColor = incomeColor,
                    Anchor = AnchorStyles.Right,
                    Margin = new Padding(0, 0, 0, 10)
                };
                var expenseValue = new Label
                {
                    Text = FormatAmount(0m),
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(14.5f, FontStyle.Bold),
                    ForeColor = expenseColor,
                    Anchor = AnchorStyles.Right,
                    Margin = new Padding(0, 0, 0, 10)
                };
                var netValue = new Label
                {
                    Text = FormatAmount(0m),
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(15f, FontStyle.Bold),
                    ForeColor = incomeColor,
                    Anchor = AnchorStyles.Right
                };

                grid.Controls.Add(incomeCaption, 0, 0);
                grid.Controls.Add(incomeValue, 1, 0);
                grid.Controls.Add(expenseCaption, 0, 1);
                grid.Controls.Add(expenseValue, 1, 1);
                grid.Controls.Add(netCaption, 0, 2);
                grid.Controls.Add(netValue, 1, 2);

                return new SummaryCard
                {
                    Root = card,
                    Title = titleLabel,
                    Income = incomeValue,
                    Expense = expenseValue,
                    Net = netValue,
                    DefaultRangeCode = defaultRangeCode,
                    SendButton = new Button()
                };
            }

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = contentBackground
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 318));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(shell);

            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = sidebarBackground,
                Padding = new Padding(22, 20, 22, 18)
            };
            UiMetrics.EnableDoubleBuffer(sidebar);
            sidebar.Paint += (_, e) =>
            {
                using var brush = new LinearGradientBrush(sidebar.ClientRectangle, Color.FromArgb(17, 30, 52), Color.FromArgb(24, 38, 63), 90f);
                e.Graphics.FillRectangle(brush, sidebar.ClientRectangle);
            };
            shell.Controls.Add(sidebar, 0, 0);

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(sidebarLayout);
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebar.Controls.Add(sidebarLayout);

            var brandRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 26),
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(brandRow);
            sidebarLayout.Controls.Add(brandRow, 0, 0);

            var logo = new BrandLogoControl
            {
                Size = new Size(46, 46),
                Margin = new Padding(0, 7, 12, 0)
            };
            brandRow.Controls.Add(logo);

            brandRow.Controls.Add(new Label
            {
                Text = "CASHTRACKER",
                AutoSize = true,
                ForeColor = Color.White,
                Font = BrandTheme.CreateHeadingFont(15.4f, FontStyle.Bold),
                Margin = new Padding(0, 13, 0, 0)
            });

            var navButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 4, 0),
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(navButtons);
            sidebarLayout.Controls.Add(navButtons, 0, 2);

            var btnGelirGider = CreateSidebarButton(AppLocalization.T("main.nav.records"), "records");
            var btnCari = CreateSidebarButton("Cari Hesaplar", "cari");
            var btnUrunStok = CreateSidebarButton("Urun / Stok", "stock");
            var btnFaturalar = CreateSidebarButton("Faturalar", "invoice");
            var btnTahsilat = CreateSidebarButton("Tahsilat / Odeme", "payment");
            var btnMuhasebeci = CreateSidebarButton("Muhasebeci Raporu", "report");
            var btnGibPortal = CreateSidebarButton("GiB Portal Ayarlari", "globe");
            var btnChangeBot = CreateSidebarButton(AppLocalization.T("main.nav.bot"), "swap");
            var btnPrint = CreateSidebarButton(AppLocalization.T("main.nav.print"), "print");
            _btnUpdateNav = CreateSidebarButton(AppLocalization.T("main.nav.update"), "refresh");

            navButtons.Controls.Add(btnGelirGider);
            navButtons.Controls.Add(btnCari);
            navButtons.Controls.Add(btnUrunStok);
            navButtons.Controls.Add(btnFaturalar);
            navButtons.Controls.Add(btnTahsilat);
            navButtons.Controls.Add(btnMuhasebeci);
            navButtons.Controls.Add(btnGibPortal);
            navButtons.Controls.Add(btnChangeBot);
            navButtons.Controls.Add(btnPrint);
            navButtons.Controls.Add(_btnUpdateNav);

            btnGelirGider.Click += (_, __) =>
            {
                ShowEmbeddedForm(
                    "Gelir & Gider Yönetimi",
                    btnGelirGider,
                    new ReactCashflowForm(
                        _kasaService,
                        _isletmeService,
                        _kalemTanimiService,
                        _urunHizmetService,
                        _stokService),
                    refreshAfterClose: true);
            };
            btnCari.Click += (_, __) =>
            {
                ShowEmbeddedForm("Cari Hesaplar", btnCari, new CariForm(_cariService), refreshAfterClose: true);
            };
            btnUrunStok.Click += (_, __) =>
            {
                ShowEmbeddedForm("Urun / Stok", btnUrunStok, new UrunStokForm(_urunHizmetService, _stokService), refreshAfterClose: true);
            };
            btnFaturalar.Click += (_, __) =>
            {
                ShowEmbeddedForm(
                    "Faturalar",
                    btnFaturalar,
                    new FaturaForm(
                        _faturaService,
                        _cariService,
                        _urunHizmetService,
                        _stokService,
                        _gibPortalService,
                        _tahsilatOdemeService),
                    refreshAfterClose: true);
            };
            btnTahsilat.Click += (_, __) =>
            {
                ShowEmbeddedForm(
                    "Tahsilat / Odeme",
                    btnTahsilat,
                    new FaturaForm(
                        _faturaService,
                        _cariService,
                        _urunHizmetService,
                        _stokService,
                        _gibPortalService,
                        _tahsilatOdemeService),
                    refreshAfterClose: true);
            };
            btnMuhasebeci.Click += (_, __) =>
            {
                ShowEmbeddedForm("Muhasebeci Raporu", btnMuhasebeci, new OnMuhasebeReportForm(_onMuhasebeReportService));
            };
            btnGibPortal.Click += (_, __) =>
            {
                ShowEmbeddedForm("GIB Portal Ayarlari", btnGibPortal, new GibPortalSettingsForm(_gibPortalService));
            };
            btnChangeBot.Click += (_, __) => OpenBotSettings(btnChangeBot);
            btnPrint.Click += (_, __) =>
            {
                ShowEmbeddedForm("Yazdir", btnPrint, new PrintPreviewForm(_kasaService, _summaryService, _isletmeService));
            };
            _btnUpdateNav.Click += async (_, __) => await CheckForUpdatesAsync(_btnUpdateNav);

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(footer);
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebarLayout.Controls.Add(footer, 0, 3);

            footer.Controls.Add(new Label
            {
                Text = AppLocalization.T("main.footer.credit"),
                AutoSize = true,
                ForeColor = sidebarMuted,
                Font = BrandTheme.CreateFont(9.4f),
                Margin = new Padding(2, 0, 0, 16)
            }, 0, 0);

            var footerBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0)
            };
            footerBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.Controls.Add(footerBottom, 0, 1);

            var footerSettings = CreateGhostIconButton("settingsmodern", Color.FromArgb(228, 236, 247), 48, 30);
            footerSettings.Margin = new Padding(0);
            footerSettings.Click += (_, __) => ShowSettingsPage();
            footerBottom.Controls.Add(footerSettings, 0, 0);

            var contentShell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = contentBackground
            };
            UiMetrics.EnableDoubleBuffer(contentShell);
            contentShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contentShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            contentShell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.Controls.Add(contentShell, 1, 0);

            var topPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Padding = new Padding(28, 20, 28, 18)
            };
            UiMetrics.EnableDoubleBuffer(topPanel);
            topPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(border);
                e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
            };
            contentShell.Controls.Add(topPanel, 0, 0);

            var topLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(topLayout);
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.Controls.Add(topLayout);

            var pageTitleHost = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            UiMetrics.EnableDoubleBuffer(pageTitleHost);
            pageTitleHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pageTitleHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            topLayout.Controls.Add(pageTitleHost, 0, 0);

            _lblActivePageTitle = new Label
            {
                Text = "Hizli Finansal Ozet",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = BrandTheme.CreateHeadingFont(25f, FontStyle.Bold),
                ForeColor = heading,
                Margin = new Padding(12, 0, 0, 0),
                UseMnemonic = false
            };
            pageTitleHost.Controls.Add(_lblActivePageTitle, 0, 0);

            var actionsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };
            UiMetrics.EnableDoubleBuffer(actionsRow);
            topLayout.Controls.Add(actionsRow, 1, 0);

            var businessSelectorHost = new Panel
            {
                Width = 250,
                Height = 52,
                BackColor = surface,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(14, 8, 14, 8)
            };
            UiMetrics.EnableDoubleBuffer(businessSelectorHost);
            ApplyRoundedRegion(businessSelectorHost, 16);
            businessSelectorHost.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var hostPath = CreateRoundedPath(new Rectangle(0, 0, businessSelectorHost.Width - 1, businessSelectorHost.Height - 1), 16);
                using var hostPen = new Pen(Color.FromArgb(205, 213, 225), 1.15f);
                e.Graphics.DrawPath(hostPen, hostPath);
            };

            _btnBusinessSelector = new Button
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                ForeColor = heading,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateHeadingFont(11.5f, FontStyle.Bold),
                Padding = new Padding(4, 0, 4, 0),
                Image = DashboardIconFactory.Create("chevrondown", heading, 16),
                ImageAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.TextBeforeImage,
                Margin = new Padding(0),
                Text = FormatBusinessSelectorText(null)
            };
            _btnBusinessSelector.FlatAppearance.BorderSize = 0;
            _btnBusinessSelector.FlatAppearance.MouseOverBackColor = Color.FromArgb(247, 250, 254);
            _btnBusinessSelector.Click += async (_, __) => await ShowBusinessSelectorMenuAsync();
            businessSelectorHost.Controls.Add(_btnBusinessSelector);

            var bellHost = new Panel
            {
                Width = 36,
                Height = 36,
                Margin = new Padding(0, 14, 18, 0),
                BackColor = Color.Transparent
            };
            var bellIcon = new PictureBox
            {
                Image = DashboardIconFactory.Create("bell", heading, 24),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            bellHost.Controls.Add(bellIcon);
            var bellDot = new Panel
            {
                Width = 8,
                Height = 8,
                BackColor = Color.FromArgb(228, 73, 73),
                Location = new Point(24, 6)
            };
            ApplyCircleRegion(bellDot);
            bellHost.Controls.Add(bellDot);

            var datePanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 6, 20, 0),
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(datePanel);
            _lblTopDate = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(12f, FontStyle.Bold),
                ForeColor = heading,
                Margin = new Padding(0, 0, 0, 2)
            };
            _lblTopTime = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateFont(11f),
                ForeColor = Color.FromArgb(66, 74, 89),
                Margin = new Padding(0)
            };
            datePanel.Controls.Add(_lblTopDate, 0, 0);
            datePanel.Controls.Add(_lblTopTime, 0, 1);

            var telegramStack = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 20, 0),
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(telegramStack);
            telegramStack.Controls.Add(CreateCircleIconButton("telegram", Color.FromArgb(58, 165, 232), Color.White, 56));
            _lblTopTelegramState = CreatePillLabel(
                _telegramSettings.IsEnabled ? "Aktif" : "Pasif",
                _telegramSettings.IsEnabled ? Color.FromArgb(233, 244, 237) : Color.FromArgb(252, 237, 237),
                _telegramSettings.IsEnabled ? incomeColor : expenseColor);
            _lblTopTelegramState.Margin = new Padding(12, 12, 0, 0);
            telegramStack.Controls.Add(_lblTopTelegramState);

            _lblUpdateBadge = CreatePillLabel(AppLocalization.T("main.badge.updateAvailable"), Color.FromArgb(255, 244, 214), Color.FromArgb(165, 111, 24));
            _lblUpdateBadge.Visible = false;
            _lblUpdateBadge.Margin = new Padding(0, 14, 16, 0);

            Panel CreateSeparator()
            {
                return new Panel
                {
                    Width = 1,
                    Height = 78,
                    BackColor = Color.FromArgb(226, 231, 239),
                    Margin = new Padding(0, 6, 16, 0)
                };
            }

            actionsRow.Controls.Add(businessSelectorHost);
            actionsRow.Controls.Add(bellHost);
            actionsRow.Controls.Add(CreateSeparator());
            actionsRow.Controls.Add(datePanel);
            actionsRow.Controls.Add(CreateSeparator());
            actionsRow.Controls.Add(telegramStack);
            actionsRow.Controls.Add(_lblUpdateBadge);

            _licenseBanner = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(255, 247, 224),
                Padding = new Padding(24, 14, 24, 14),
                Visible = false
            };
            _licenseBanner.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 192, 104));
                e.Graphics.DrawLine(pen, 0, _licenseBanner.Height - 1, _licenseBanner.Width, _licenseBanner.Height - 1);
            };
            contentShell.Controls.Add(_licenseBanner, 0, 1);

            var bannerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true
            };
            bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _licenseBanner.Controls.Add(bannerLayout);

            var bannerTextStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true
            };
            bannerTextStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bannerLayout.Controls.Add(bannerTextStack, 0, 0);

            _lblLicenseBannerTitle = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 82, 16),
                Margin = new Padding(0, 0, 0, 2),
                Text = AppLocalization.T("license.banner.title")
            };
            bannerTextStack.Controls.Add(_lblLicenseBannerTitle, 0, 0);

            _lblLicenseBannerText = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.3f),
                ForeColor = Color.FromArgb(122, 87, 24),
                MaximumSize = new Size(720, 0),
                Text = AppLocalization.T("license.banner.body")
            };
            bannerTextStack.Controls.Add(_lblLicenseBannerText, 0, 1);

            _btnLicenseBannerAction = new Button
            {
                Text = AppLocalization.T("license.banner.action"),
                Width = 168,
                Height = 40,
                Margin = new Padding(16, 0, 0, 0),
                BackColor = Color.FromArgb(199, 146, 44),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateFont(9.6f, FontStyle.Bold),
                Padding = UiMetrics.ButtonPadding
            };
            _btnLicenseBannerAction.FlatAppearance.BorderColor = Color.FromArgb(183, 131, 27);
            _btnLicenseBannerAction.FlatAppearance.BorderSize = 1;
            _btnLicenseBannerAction.FlatAppearance.MouseOverBackColor = Color.FromArgb(186, 133, 30);
            _btnLicenseBannerAction.Click += (_, __) =>
            {
                ShowSettingsPage();
                _ = RefreshLicenseBannerAsync();
            };
            bannerLayout.Controls.Add(_btnLicenseBannerAction, 1, 0);

            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = contentBackground,
                Margin = new Padding(0)
            };
            UiMetrics.EnableDoubleBuffer(_contentHost);
            contentShell.Controls.Add(_contentHost, 0, 2);

            _dashboardViewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = contentBackground,
                Padding = new Padding(36, 26, 36, 30)
            };
            UiMetrics.EnableDoubleBuffer(_dashboardViewport);
            _dashboardViewport.Paint += (_, e) =>
            {
                using var brush = new LinearGradientBrush(_dashboardViewport.ClientRectangle, Color.FromArgb(243, 246, 251), Color.FromArgb(232, 238, 247), 0f);
                e.Graphics.FillRectangle(brush, _dashboardViewport.ClientRectangle);

                using var accentBrush = new SolidBrush(Color.FromArgb(16, 88, 132, 194));
                e.Graphics.FillEllipse(
                    accentBrush,
                    _dashboardViewport.ClientSize.Width - 360,
                    _dashboardViewport.ClientSize.Height - 240,
                    240,
                    240);
            };
            _contentHost.Controls.Add(_dashboardViewport);

            var dashboard = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(dashboard);
            dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _dashboardViewport.Controls.Add(dashboard);

            var headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 18)
            };
            UiMetrics.EnableDoubleBuffer(headerRow);
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            dashboard.Controls.Add(headerRow, 0, 0);

            var titleStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(titleStack);
            titleStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerRow.Controls.Add(titleStack, 0, 0);

            titleStack.Controls.Add(new Label
            {
                Text = "Hizli Finansal Ozet (Snapshot)",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(21f, FontStyle.Bold),
                ForeColor = heading,
                Margin = new Padding(0, 4, 0, 0)
            }, 0, 0);

            _lblActiveBusinessReport = new Label
            {
                Visible = false
            };
            titleStack.Controls.Add(_lblActiveBusinessReport, 0, 1);

            var kpiRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            UiMetrics.EnableDoubleBuffer(kpiRow);
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            kpiRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dashboard.Controls.Add(kpiRow, 0, 1);

            Panel CreateMetricCard(string title, Color valueColor, out Label valueLabel, out Label deltaLabel, Control footerControl = null!)
            {
                var card = CreateCardPanel(new Padding(20, 18, 20, 18), 172);
                card.Dock = DockStyle.Fill;
                card.Margin = new Padding(0, 0, 18, 0);

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 1,
                    RowCount = 3,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                card.Controls.Add(layout);

                var header = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent
                };
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                layout.Controls.Add(header, 0, 0);

                header.Controls.Add(new Label
                {
                    Text = title,
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(15f, FontStyle.Bold),
                    ForeColor = heading,
                    Margin = new Padding(0, 0, 0, 8)
                }, 0, 0);

                deltaLabel = CreatePillLabel("+0%", Color.FromArgb(233, 244, 237), valueColor);
                deltaLabel.Font = BrandTheme.CreateFont(10f, FontStyle.Bold);
                deltaLabel.Margin = new Padding(10, 0, 0, 0);
                header.Controls.Add(deltaLabel, 1, 0);

                valueLabel = new Label
                {
                    Text = FormatAmount(0m),
                    AutoSize = true,
                    Font = BrandTheme.CreateHeadingFont(32f, FontStyle.Bold),
                    ForeColor = valueColor,
                    Margin = new Padding(0, 12, 0, 4)
                };
                layout.Controls.Add(valueLabel, 0, 1);

                if (footerControl is not null)
                {
                    footerControl.Dock = DockStyle.Top;
                    footerControl.Height = 82;
                    footerControl.Margin = new Padding(0, 6, 0, 0);
                    layout.Controls.Add(footerControl, 0, 2);
                }

                return card;
            }

            var incomeCard = CreateMetricCard("Toplam Gelir", incomeColor, out _lblSnapshotIncomeValue, out _lblSnapshotIncomeDelta);
            _lblSnapshotIncomeDelta.Visible = false;
            var netCardFooter = new Panel { BackColor = surface };
            _netSparkChart = new DashboardSparkBarsControl
            {
                Dock = DockStyle.Fill,
                BackColor = surface
            };
            netCardFooter.Controls.Add(_netSparkChart);
            var netCard = CreateMetricCard("Net Kar", Color.FromArgb(84, 89, 96), out _lblSnapshotNetValue, out var netDelta, netCardFooter);
            netDelta.Visible = false;
            var expenseCard = CreateMetricCard("Toplam Gider", expenseColor, out _lblSnapshotExpenseValue, out _lblSnapshotExpenseDelta);
            _lblSnapshotExpenseDelta.Visible = false;
            expenseCard.Margin = new Padding(0);

            kpiRow.Controls.Add(incomeCard, 0, 0);
            kpiRow.Controls.Add(netCard, 1, 0);
            kpiRow.Controls.Add(expenseCard, 2, 0);

            var paymentCard = CreateCardPanel(new Padding(18, 18, 18, 18), 266);
            paymentCard.Dock = DockStyle.Top;
            paymentCard.Margin = new Padding(0, 0, 0, 18);
            dashboard.Controls.Add(paymentCard, 0, 2);

            var paymentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            UiMetrics.EnableDoubleBuffer(paymentLayout);
            paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            paymentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paymentCard.Controls.Add(paymentLayout);

            var paymentInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            UiMetrics.EnableDoubleBuffer(paymentInfo);
            paymentInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            paymentLayout.Controls.Add(paymentInfo, 0, 0);

            paymentInfo.Controls.Add(new Label
            {
                Text = "Odeme Yontemin Dagilimi",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(16.5f, FontStyle.Bold),
                ForeColor = heading,
                Margin = new Padding(0, 4, 0, 18)
            }, 0, 0);

            Panel CreateLegendRow(string text, Color dotColor)
            {
                var row = new Panel
                {
                    Width = 240,
                    Height = 30,
                    Margin = new Padding(0, 0, 0, 10),
                    BackColor = Color.Transparent
                };

                var dot = new Panel
                {
                    Width = 12,
                    Height = 12,
                    BackColor = dotColor,
                    Location = new Point(0, 8)
                };
                ApplyCircleRegion(dot);
                row.Controls.Add(dot);

                row.Controls.Add(new Label
                {
                    Text = text,
                    AutoSize = true,
                    Font = BrandTheme.CreateFont(13f),
                    ForeColor = heading,
                    Location = new Point(22, 3),
                    BackColor = Color.Transparent
                });

                return row;
            }

            paymentInfo.Controls.Add(CreateLegendRow("Nakit", Color.FromArgb(27, 40, 74)));
            paymentInfo.Controls.Add(CreateLegendRow("Kredi Karti", Color.FromArgb(46, 95, 153)));
            paymentInfo.Controls.Add(CreateLegendRow("Online Odeme", Color.FromArgb(110, 157, 206)));
            paymentInfo.Controls.Add(CreateLegendRow("Havale", Color.FromArgb(189, 204, 224)));

            _paymentDistributionChart = new DashboardDonutChartControl
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Margin = new Padding(0)
            };
            _paymentDistributionChart.ShowOuterLabels = false;
            paymentLayout.Controls.Add(_paymentDistributionChart, 1, 0);

            dashboard.Controls.Add(new Label
            {
                Text = "Zaman Donemi Ozetleri",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(17f, FontStyle.Bold),
                ForeColor = heading,
                Margin = new Padding(0, 0, 0, 14)
            }, 0, 3);

            var periodRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            UiMetrics.EnableDoubleBuffer(periodRow);
            periodRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.5f));
            periodRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.5f));
            periodRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.5f));
            periodRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 216));
            periodRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dashboard.Controls.Add(periodRow, 0, 4);

            _cardDaily = CreatePeriodCard("Bugun", SummaryRangeCatalog.Daily);
            _cardPrimaryRange = CreatePeriodCard(AppLocalization.T("main.summary.range.last30Days"), SummaryRangeCatalog.Last30Days);
            _cardSecondaryRange = CreatePeriodCard(AppLocalization.T("main.summary.range.last1Year"), SummaryRangeCatalog.Last1Year);

            _cardDaily.Root.Margin = new Padding(0, 0, 18, 0);
            _cardPrimaryRange.Root.Margin = new Padding(0, 0, 18, 0);
            _cardSecondaryRange.Root.Margin = new Padding(0, 0, 18, 0);

            periodRow.Controls.Add(_cardDaily.Root, 0, 0);
            periodRow.Controls.Add(_cardPrimaryRange.Root, 1, 0);
            periodRow.Controls.Add(_cardSecondaryRange.Root, 2, 0);

            var sharePanel = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                MinimumSize = new Size(200, 252),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            UiMetrics.EnableDoubleBuffer(sharePanel);
            periodRow.Controls.Add(sharePanel, 3, 0);

            var shareLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(shareLayout);
            shareLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shareLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shareLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sharePanel.Controls.Add(shareLayout);

            var shareButton = new Button
            {
                Text = "Raporu Paylas",
                BackColor = navyAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateHeadingFont(11f, FontStyle.Bold),
                Width = 196,
                Height = 44,
                Image = DashboardIconFactory.Create("chevrondown", Color.White, 14),
                ImageAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.TextBeforeImage,
                Padding = new Padding(16, 0, 16, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            shareButton.FlatAppearance.BorderSize = 0;
            ApplyRoundedRegion(shareButton, 14);
            shareButton.Margin = new Padding(0, 0, 0, 12);
            shareLayout.Controls.Add(shareButton, 0, 0);

            var shareMenu = CreateCardPanel(new Padding(8, 8, 8, 8), 0);
            shareMenu.AutoSize = false;
            shareMenu.Size = new Size(196, 196);
            shareMenu.MinimumSize = new Size(196, 196);
            shareMenu.Dock = DockStyle.Top;
            shareLayout.Controls.Add(shareMenu, 0, 1);

            var shareMenuLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            UiMetrics.EnableDoubleBuffer(shareMenuLayout);
            shareMenuLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shareMenuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            shareMenuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            shareMenuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            shareMenuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            shareMenu.Controls.Add(shareMenuLayout);

            Button CreateShareOption(string text, string iconKey)
            {
                var button = new Button
                {
                    Text = text,
                    Dock = DockStyle.Top,
                    Height = 40,
                    BackColor = Color.White,
                    ForeColor = heading,
                    FlatStyle = FlatStyle.Flat,
                    Font = BrandTheme.CreateFont(10.5f),
                    Image = DashboardIconFactory.Create(iconKey, navyAccent, 18),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(12, 0, 0, 0),
                    Margin = new Padding(0)
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(246, 249, 252);
                return button;
            }

            var btnShareTelegram = CreateShareOption("Telegram", "telegram");
            var btnShareEmail = CreateShareOption("Email", "mail");
            var btnShareWhatsapp = CreateShareOption("WhatsApp", "chat");
            var btnSharePdf = CreateShareOption("PDF Indir", "pdf");

            btnShareTelegram.Click += async (_, __) => await SendSelectedSummaryAsync(_cardPrimaryRange, btnShareTelegram);
            btnShareEmail.Click += (_, __) => MessageBox.Show("Email paylasimi sonraki surumde acilacak.", "Paylasim", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnShareWhatsapp.Click += (_, __) => MessageBox.Show("WhatsApp paylasimi sonraki surumde acilacak.", "Paylasim", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnSharePdf.Click += (_, __) =>
            {
                ShowEmbeddedForm("Yazdir", null, new PrintPreviewForm(_kasaService, _summaryService, _isletmeService));
            };

            shareMenuLayout.Controls.Add(btnShareTelegram, 0, 0);
            shareMenuLayout.Controls.Add(btnShareEmail, 0, 1);
            shareMenuLayout.Controls.Add(btnShareWhatsapp, 0, 2);
            shareMenuLayout.Controls.Add(btnSharePdf, 0, 3);

            UpdateTopClockDisplay();

            ResumeLayout(true);
        }
    }
}
