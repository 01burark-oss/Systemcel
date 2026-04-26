using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void BuildGridSection(Panel left)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.Controls.Add(layout);

            var header = CreateListHeader();
            header.Margin = new Padding(2, 0, 0, 18);
            layout.Controls.Add(header, 0, 0);

            _grid = CreateGrid();
            _grid.SelectionChanged += async (_, __) => await GridToFormAsync();
            _grid.CellFormatting += GridCellFormatting;
            _grid.Resize += (_, __) => ApplyGridColumnLayout();
            _grid.DataBindingComplete += (_, __) =>
            {
                ApplyGridColumnLayout();
                if (_selectedId != 0)
                    return;

                _grid.ClearSelection();
                _grid.CurrentCell = null;
            };
            layout.Controls.Add(_grid, 0, 1);
        }

        private Control CreateListHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            header.Controls.Add(new Label
            {
                Text = "Tum Kayitlar Listesi",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = BrandTheme.CreateHeadingFont(17.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Margin = new Padding(0, 4, 12, 0)
            }, 0, 0);

            var searchFrame = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 48,
                MinimumSize = new Size(0, 48),
                Margin = new Padding(0),
                Padding = new Padding(40, 8, 12, 8),
                BackColor = Color.White
            };
            ApplyRoundedRegion(searchFrame, 10);
            searchFrame.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = CreateRoundedPath(new Rectangle(0, 0, searchFrame.Width - 1, searchFrame.Height - 1), 10);
                using var pen = new Pen(Color.FromArgb(163, 172, 185), 1.2f);
                e.Graphics.DrawPath(pen, path);
                using var iconPen = new Pen(Color.FromArgb(113, 124, 138), 1.8f);
                e.Graphics.DrawEllipse(iconPen, 16, 15, 13, 13);
                e.Graphics.DrawLine(iconPen, 26, 26, 32, 32);
            };

            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = BrandTheme.CreateFont(11.5f),
                ForeColor = Color.FromArgb(31, 41, 55),
                PlaceholderText = "Ara...",
                Margin = new Padding(0)
            };
            _txtSearch.TextChanged += (_, __) => ApplyRecordFilter();
            searchFrame.Controls.Add(_txtSearch);
            header.Controls.Add(searchFrame, 1, 0);

            return header;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(224, 228, 234),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Vertical,
                ColumnHeadersHeight = 54,
                RowTemplate = { Height = 44 }
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(247, 249, 252),
                ForeColor = Color.FromArgb(17, 24, 39),
                Font = BrandTheme.CreateFont(11f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(22, 29, 41),
                SelectionBackColor = Color.FromArgb(228, 238, 250),
                SelectionForeColor = Color.FromArgb(20, 34, 48),
                Font = BrandTheme.CreateFont(10.7f),
                Padding = new Padding(4, 0, 4, 0)
            };
            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 251, 253)
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tarih", DataPropertyName = "Tarih", HeaderText = AppLocalization.T("common.date"), MinimumWidth = 138 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tip", DataPropertyName = "Tip", HeaderText = AppLocalization.T("common.type"), MinimumWidth = 72 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OdemeYontemi", DataPropertyName = "OdemeYontemi", HeaderText = AppLocalization.T("common.method"), MinimumWidth = 108 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tutar", DataPropertyName = "Tutar", HeaderText = AppLocalization.T("common.amount"), MinimumWidth = 118 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kalem", DataPropertyName = "Kalem", HeaderText = AppLocalization.T("common.category"), MinimumWidth = 132 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Aciklama", DataPropertyName = "Aciklama", HeaderText = AppLocalization.T("common.description"), MinimumWidth = 150 });
            grid.Columns[0].DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" };
            grid.Columns[3].DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight
            };

            return grid;
        }

        private void ApplyGridColumnLayout()
        {
            if (_grid is null || _grid.Columns.Count < 6)
                return;

            var clientWidth = _grid.ClientSize.Width;
            var showDescription = clientWidth >= 900;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                _grid.Columns["Tarih"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _grid.Columns["Tip"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _grid.Columns["OdemeYontemi"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _grid.Columns["Tutar"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _grid.Columns["Kalem"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _grid.Columns["Aciklama"].Visible = showDescription;
                _grid.Columns["Aciklama"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                _grid.Columns["Tarih"].FillWeight = showDescription ? 18 : 24;
                _grid.Columns["Tip"].FillWeight = showDescription ? 9 : 10;
                _grid.Columns["OdemeYontemi"].FillWeight = showDescription ? 13 : 18;
                _grid.Columns["Tutar"].FillWeight = showDescription ? 14 : 18;
                _grid.Columns["Kalem"].FillWeight = showDescription ? 18 : 30;
                _grid.Columns["Aciklama"].FillWeight = 28;
            }
            finally
            {
                _grid.ResumeLayout();
            }
        }

        private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == 2)
            {
                e.Value = MapOdemeYontemiLabel(e.Value?.ToString());
                e.FormattingApplied = true;
                return;
            }

            if (e.ColumnIndex == 1)
            {
                var typeValue = MapTip(e.Value?.ToString());
                e.Value = typeValue == "Gider"
                    ? "↓ " + AppLocalization.GetTipDisplay(typeValue)
                    : "↑ " + AppLocalization.GetTipDisplay(typeValue);
                e.FormattingApplied = true;
                return;
            }

            if (e.ColumnIndex != 3)
                return;

            var row = _grid.Rows[e.RowIndex];
            var tip = MapTip(row.Cells[1].Value?.ToString());
            var style = e.CellStyle;
            if (style is null)
                return;

            if (row.DataBoundItem is CashTracker.Core.Entities.Kasa kasa)
            {
                e.Value = $"{kasa.Tutar:N2} TL";
                e.FormattingApplied = true;
            }

            if (tip == "Gelir")
            {
                style.Font = BrandTheme.CreateFont(10.7f, FontStyle.Bold);
                style.ForeColor = Color.FromArgb(17, 121, 85);
                style.SelectionForeColor = Color.FromArgb(17, 121, 85);
                return;
            }

            if (tip == "Gider")
            {
                style.Font = BrandTheme.CreateFont(10.7f, FontStyle.Bold);
                style.ForeColor = Color.FromArgb(173, 59, 56);
                style.SelectionForeColor = Color.FromArgb(173, 59, 56);
            }
        }
    }
}

