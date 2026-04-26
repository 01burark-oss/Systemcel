using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.Core.Entities;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private async Task LoadAllAsync()
        {
            _suppressGridToForm = true;
            try
            {
                await RefreshActiveBusinessInfoAsync();
                await LoadStockProductsAsync();
                _allRecords = (await _kasaService.GetAllAsync()).ToList();
                _selectedId = 0;
                ApplyRecordFilter();
                _grid.ClearSelection();
                _grid.CurrentCell = null;
                await ClearFormAsync();
            }
            finally
            {
                _suppressGridToForm = false;
            }
        }

        private async Task GridToFormAsync()
        {
            if (_isBindingGrid || _suppressGridToForm)
                return;

            if (_grid.CurrentRow?.DataBoundItem is not Kasa kasa)
                return;

            _selectedId = kasa.Id;
            _dtTarih.Value = kasa.Tarih;
            _cmbTip.SelectedItem = AppLocalization.GetTipDisplay(MapTip(kasa.Tip));
            _txtTutar.Text = kasa.Tutar.ToString("0.##", AppLocalization.CurrentCulture);
            SetSelectedOdemeYontemi(kasa.OdemeYontemi);
            _txtAciklama.Text = kasa.Aciklama ?? string.Empty;
            await LoadKalemlerForTipAsync(kasa.Kalem ?? kasa.GiderTuru);
            ResetStockLinkForSelectedRecord();
        }

        private async Task ClearFormAsync()
        {
            _selectedId = 0;
            _dtTarih.Value = DateTime.Now;
            _cmbTip.SelectedIndex = 0;
            _txtTutar.Text = string.Empty;
            SetSelectedOdemeYontemi("Nakit");
            _txtAciklama.Text = string.Empty;
            _chkStokGiris.Checked = false;
            _numStokMiktar.Value = 1;
            await LoadKalemlerForTipAsync();
            UpdateStockLinkUi();
        }

        private void ApplyRecordFilter()
        {
            if (_grid is null)
                return;

            var query = (_txtSearch?.Text ?? string.Empty).Trim();
            IEnumerable<Kasa> rows = _allRecords;
            if (!string.IsNullOrWhiteSpace(query))
            {
                rows = rows.Where(row => RecordMatchesSearch(row, query));
            }

            _isBindingGrid = true;
            try
            {
                _grid.DataSource = new BindingList<Kasa>(rows.ToList());
                _grid.ClearSelection();
                _grid.CurrentCell = null;
            }
            finally
            {
                _isBindingGrid = false;
            }
        }

        private static bool RecordMatchesSearch(Kasa row, string query)
        {
            var haystack = string.Join(" ", new[]
            {
                row.Tarih.ToString("dd.MM.yyyy HH:mm", AppLocalization.CurrentCulture),
                AppLocalization.GetTipDisplay(MapTip(row.Tip)),
                MapOdemeYontemiLabel(row.OdemeYontemi),
                row.Tutar.ToString("N2", AppLocalization.CurrentCulture),
                row.Kalem,
                row.GiderTuru,
                row.Aciklama
            });

            return haystack.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        }

        private async Task LoadKalemlerForTipAsync(string? preferredKalem = null)
        {
            if (_isLoadingKalemler)
                return;

            _isLoadingKalemler = true;
            _cmbKalem.Enabled = false;

            try
            {
                var tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? AppLocalization.T("tip.income"));
                var rows = await _kalemTanimiService.GetByTipAsync(tip);
                var kalemler = rows
                    .Select(x => x.Ad)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                var preferred = NormalizeText(preferredKalem ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(preferred) &&
                    !kalemler.Any(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)))
                {
                    kalemler.Insert(0, preferred);
                }

                _cmbKalem.DataSource = null;

                if (kalemler.Count == 0)
                {
                    _cmbKalem.Items.Clear();
                    return;
                }

                _cmbKalem.DataSource = kalemler;
                if (!string.IsNullOrWhiteSpace(preferred))
                {
                    var selectedIndex = kalemler.FindIndex(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase));
                    _cmbKalem.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                }
                else
                {
                    _cmbKalem.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _cmbKalem.DataSource = null;
                _cmbKalem.Items.Clear();
                MessageBox.Show(
                    AppLocalization.F("kasa.error.categoryLoad", ex.Message),
                    AppLocalization.T("kasa.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isLoadingKalemler = false;
                _cmbKalem.Enabled = _cmbKalem.Items.Count > 0;
                UpdateKalemAvailabilityUi();
            }
        }

        private async Task LoadStockProductsAsync()
        {
            if (_isLoadingStockProducts)
                return;

            _isLoadingStockProducts = true;
            try
            {
                _stockProducts = (await _urunHizmetService.GetAllAsync())
                    .Where(x => x.Aktif && string.Equals(x.Tip, "Urun", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Ad, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _cmbStokUrun.DataSource = null;
                _cmbStokUrun.DisplayMember = nameof(UrunHizmet.Ad);
                _cmbStokUrun.ValueMember = nameof(UrunHizmet.Id);
                _cmbStokUrun.DataSource = _stockProducts;
            }
            catch (Exception ex)
            {
                _stockProducts = new List<UrunHizmet>();
                _cmbStokUrun.DataSource = null;
                MessageBox.Show(
                    $"Stok urunleri yuklenemedi: {ex.Message}",
                    AppLocalization.T("kasa.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                _isLoadingStockProducts = false;
                UpdateStockLinkUi();
            }
        }

        private void ResetStockLinkForSelectedRecord()
        {
            _chkStokGiris.Checked = false;
            _numStokMiktar.Value = 1;
            UpdateStockLinkUi();
        }

        private void UpdateStockLinkUi()
        {
            if (_chkStokGiris == null || _cmbStokUrun == null || _numStokMiktar == null || _lblStokGirisHint == null)
                return;

            var tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? AppLocalization.T("tip.income"));
            var isNewExpense = _selectedId == 0 && tip == "Gider";
            var hasProduct = _stockProducts.Count > 0;
            _chkStokGiris.Enabled = isNewExpense && hasProduct;
            _cmbStokUrun.Enabled = isNewExpense && hasProduct && _chkStokGiris.Checked;
            _numStokMiktar.Enabled = isNewExpense && hasProduct && _chkStokGiris.Checked;

            if (tip != "Gider")
            {
                _chkStokGiris.Checked = false;
                _lblStokGirisHint.Text = "Stok girisi sadece gider kaydi icin kullanilir.";
                return;
            }

            if (_selectedId != 0)
            {
                _chkStokGiris.Checked = false;
                _lblStokGirisHint.Text = "Duzenlenen kayitta tekrar stok girisi yapilmaz; yeni gider kaydi acin.";
                return;
            }

            if (!hasProduct)
            {
                _chkStokGiris.Checked = false;
                _lblStokGirisHint.Text = "Once Urun / Stok ekranindan urun karti olusturun.";
                return;
            }

            _lblStokGirisHint.Text = "Kaydedince gider kaydi ve stok girisi birlikte olusur.";
        }

        private void UpdateKalemAvailabilityUi()
        {
            var hasKalem = _cmbKalem.Items.Count > 0;
            var tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? AppLocalization.T("tip.income"));

            _btnSave.Enabled = hasKalem;
            _lblKalemEmptyHint.Visible = !hasKalem;
            _btnKalemSettings.Visible = !hasKalem;

            if (!hasKalem)
            {
                _lblKalemEmptyHint.Text =
                    AppLocalization.F("kasa.hint.noCategory", AppLocalization.GetTipDisplay(tip));
            }
        }

        private void SetSelectedOdemeYontemi(string? value)
        {
            _selectedOdemeYontemi = NormalizeOdemeYontemi(value);
            ApplyOdemeYontemiButtonStyles();
        }

        private void ApplyOdemeYontemiButtonStyles()
        {
            ApplyOdemeYontemiButtonStyle(_btnOdemeNakit, "Nakit");
            ApplyOdemeYontemiButtonStyle(_btnOdemeKrediKarti, "KrediKarti");
            ApplyOdemeYontemiButtonStyle(_btnOdemeOnlineOdeme, "OnlineOdeme");
            ApplyOdemeYontemiButtonStyle(_btnOdemeHavale, "Havale");
        }

        private void ApplyOdemeYontemiButtonStyle(Button button, string methodValue)
        {
            var isSelected = string.Equals(_selectedOdemeYontemi, methodValue, StringComparison.OrdinalIgnoreCase);
            button.BackColor = isSelected
                ? Color.FromArgb(217, 234, 252)
                : Color.White;
            button.ForeColor = isSelected
                ? Color.FromArgb(18, 56, 98)
                : Color.FromArgb(38, 53, 72);
            button.FlatAppearance.BorderColor = isSelected
                ? Color.FromArgb(51, 106, 174)
                : Color.FromArgb(190, 202, 216);
        }

        private async Task RefreshActiveBusinessInfoAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                var businessName = string.IsNullOrWhiteSpace(active.Ad)
                    ? AppLocalization.T("common.unknown")
                    : active.Ad.Trim();

                if (_lblActiveBusiness is not null)
                    _lblActiveBusiness.Text = AppLocalization.F("kasa.activeBusiness", businessName);
                Text = AppLocalization.F("kasa.titleWithBusiness", businessName);
            }
            catch
            {
                if (_lblActiveBusiness is not null)
                    _lblActiveBusiness.Text = AppLocalization.F("kasa.activeBusiness", AppLocalization.T("common.unknown"));
                Text = AppLocalization.T("kasa.title");
            }
        }
    }
}
