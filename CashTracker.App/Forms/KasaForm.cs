using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using CashTracker.App;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm : Form
    {
        private readonly IKasaService _kasaService;
        private readonly IIsletmeService _isletmeService;
        private readonly IKalemTanimiService _kalemTanimiService;
        private readonly IUrunHizmetService _urunHizmetService;
        private readonly IStokService _stokService;
        private readonly ITelegramApprovalService _telegramApprovalService;
        private readonly AppRuntimeOptions _runtimeOptions;
        private readonly IAppSecurityService _appSecurityService;
        private readonly ILicenseService _licenseService;
        private readonly ReceiptOcrSettings _receiptOcrSettings;

        private DataGridView _grid = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cmbTip = null!;
        private DateTimePicker _dtTarih = null!;
        private TextBox _txtTutar = null!;
        private Button _btnTipGelir = null!;
        private Button _btnTipGider = null!;
        private ComboBox _cmbKalem = null!;
        private Button _btnOdemeNakit = null!;
        private Button _btnOdemeKrediKarti = null!;
        private Button _btnOdemeOnlineOdeme = null!;
        private Button _btnOdemeHavale = null!;
        private Label _lblKalemEmptyHint = null!;
        private Button _btnKalemSettings = null!;
        private TextBox _txtAciklama = null!;
        private CheckBox _chkStokGiris = null!;
        private ComboBox _cmbStokUrun = null!;
        private NumericUpDown _numStokMiktar = null!;
        private Label _lblStokGirisHint = null!;
        private Button _btnSave = null!;
        private Button _btnNew = null!;
        private Button _btnDelete = null!;
        private Button _btnRefresh = null!;
        private TableLayoutPanel _rootLayout = null!;
        private Panel _leftPanel = null!;
        private Panel _rightPanel = null!;
        private Label _lblActiveBusiness = null!;

        private int _selectedId;
        private bool _isLoadingKalemler;
        private bool _isLoadingStockProducts;
        private bool _isBindingGrid;
        private bool _suppressGridToForm;
        private string _selectedOdemeYontemi = "Nakit";
        private List<UrunHizmet> _stockProducts = new();
        private List<Kasa> _allRecords = new();

        public Action? EmbeddedSettingsRequested { get; init; }

        internal KasaForm(
            IKasaService kasaService,
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService,
            IUrunHizmetService urunHizmetService,
            IStokService stokService,
            ITelegramApprovalService telegramApprovalService,
            AppRuntimeOptions runtimeOptions,
            IAppSecurityService appSecurityService,
            ILicenseService licenseService,
            ReceiptOcrSettings receiptOcrSettings)
        {
            _kasaService = kasaService;
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;
            _urunHizmetService = urunHizmetService;
            _stokService = stokService;
            _telegramApprovalService = telegramApprovalService;
            _runtimeOptions = runtimeOptions;
            _appSecurityService = appSecurityService;
            _licenseService = licenseService;
            _receiptOcrSettings = receiptOcrSettings;

            Text = AppLocalization.T("kasa.title");
            Width = 1080;
            Height = 700;
            MinimumSize = new Size(1120, 700);
            UiMetrics.ApplyFormDefaults(this);
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            BuildUi();
            Load += async (_, __) => await LoadAllAsync();
            Resize += (_, __) => ApplyResponsiveLayout();
        }
    }
}
