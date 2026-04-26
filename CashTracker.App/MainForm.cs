using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.Controls;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm : Form
    {
        private readonly IKasaService _kasaService;
        private readonly ISummaryService _summaryService;
        private readonly IIsletmeService _isletmeService;
        private readonly IKalemTanimiService _kalemTanimiService;
        private readonly IDashboardSnapshotService _dashboardSnapshotService;
        private readonly ICariService _cariService;
        private readonly IUrunHizmetService _urunHizmetService;
        private readonly IStokService _stokService;
        private readonly IFaturaService _faturaService;
        private readonly ITahsilatOdemeService _tahsilatOdemeService;
        private readonly IGibPortalService _gibPortalService;
        private readonly IOnMuhasebeReportService _onMuhasebeReportService;
        private readonly ITelegramApprovalService _telegramApprovalService;
        private readonly IAppSecurityService _appSecurityService;
        private readonly BackupReportService _backupReport;
        private readonly TelegramSettings _telegramSettings;
        private readonly UpdateSettings _updateSettings;
        private readonly AppRuntimeOptions _runtimeOptions;
        private readonly ILicenseService _licenseService;
        private readonly ReceiptOcrSettings _receiptOcrSettings;
        private readonly GitHubUpdateService _updateService;
        private readonly StartupMetrics _startupMetrics;
        private readonly System.Windows.Forms.Timer _dateChangeTimer;
        private UpdateCheckResult? _cachedUpdateResult;
        private Label _lblUpdateBadge = null!;
        private Panel _licenseBanner = null!;
        private Label _lblLicenseBannerTitle = null!;
        private Label _lblLicenseBannerText = null!;
        private Button _btnLicenseBannerAction = null!;
        private bool _isAuthenticated = true;
        private bool _isLoadingSummaryRangeSelectors;
        private bool _hasDeferredUpdateCheckStarted;
        private DateTime _lastSummaryDate = DateTime.Today;

        private SummaryCard _cardDaily = null!;
        private SummaryCard _cardPrimaryRange = null!;
        private SummaryCard _cardSecondaryRange = null!;

        private Button _btnUpdateNav = null!;
        private Label _lblActiveBusinessReport = null!;
        private Label _lblDailyOverviewIncome = null!;
        private Label _lblDailyOverviewExpense = null!;
        private Label _lblDailyOverviewNet = null!;
        private Label _lblDailyNakitIncome = null!;
        private Label _lblDailyNakitExpense = null!;
        private Label _lblDailyKrediKartiIncome = null!;
        private Label _lblDailyKrediKartiExpense = null!;
        private Label _lblDailyOnlineOdemeIncome = null!;
        private Label _lblDailyOnlineOdemeExpense = null!;
        private Label _lblDailyHavaleIncome = null!;
        private Label _lblDailyHavaleExpense = null!;
        private Button _btnBusinessSelector = null!;
        private Label _lblTopDate = null!;
        private Label _lblTopTime = null!;
        private Label _lblTopTelegramState = null!;
        private Label _lblSnapshotIncomeValue = null!;
        private Label _lblSnapshotIncomeDelta = null!;
        private Label _lblSnapshotNetValue = null!;
        private Label _lblSnapshotExpenseValue = null!;
        private Label _lblSnapshotExpenseDelta = null!;
        private Label _lblActivePageTitle = null!;
        private Panel _contentHost = null!;
        private Panel _dashboardViewport = null!;
        private Form? _activeEmbeddedForm;
        private Button? _activeNavButton;
        private readonly List<Button> _sidebarButtons = new();
        private DashboardSparkBarsControl _netSparkChart = null!;
        private DashboardDonutChartControl _paymentDistributionChart = null!;
        private ContextMenuStrip? _businessSelectorMenu;

        private sealed class SummaryCard
        {
            public Panel Root { get; set; } = null!;
            public Label? Title { get; set; }
            public Label Income { get; set; } = null!;
            public Label Expense { get; set; } = null!;
            public Label Net { get; set; } = null!;
            public Button SendButton { get; set; } = null!;
            public ComboBox? RangeSelector { get; set; }
            public string DefaultRangeCode { get; set; } = SummaryRangeCatalog.Last30Days;
        }

        public MainForm(
            IKasaService kasaService,
            ISummaryService summaryService,
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService,
            IDashboardSnapshotService dashboardSnapshotService,
            ICariService cariService,
            IUrunHizmetService urunHizmetService,
            IStokService stokService,
            IFaturaService faturaService,
            ITahsilatOdemeService tahsilatOdemeService,
            IGibPortalService gibPortalService,
            IOnMuhasebeReportService onMuhasebeReportService,
            ITelegramApprovalService telegramApprovalService,
            IAppSecurityService appSecurityService,
            BackupReportService backupReport,
            TelegramSettings telegramSettings,
            UpdateSettings updateSettings,
            AppRuntimeOptions runtimeOptions,
            ILicenseService licenseService,
            ReceiptOcrSettings receiptOcrSettings,
            GitHubUpdateService updateService,
            StartupMetrics startupMetrics)
        {
            _kasaService = kasaService;
            _summaryService = summaryService;
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;
            _dashboardSnapshotService = dashboardSnapshotService;
            _cariService = cariService;
            _urunHizmetService = urunHizmetService;
            _stokService = stokService;
            _faturaService = faturaService;
            _tahsilatOdemeService = tahsilatOdemeService;
            _gibPortalService = gibPortalService;
            _onMuhasebeReportService = onMuhasebeReportService;
            _telegramApprovalService = telegramApprovalService;
            _appSecurityService = appSecurityService;
            _backupReport = backupReport;
            _telegramSettings = telegramSettings;
            _updateSettings = updateSettings;
            _runtimeOptions = runtimeOptions;
            _licenseService = licenseService;
            _receiptOcrSettings = receiptOcrSettings;
            _updateService = updateService;
            _startupMetrics = startupMetrics;

            Text = AppLocalization.T("main.title");
            Width = 1320;
            Height = 900;
            MinimumSize = new Size(1320, 820);
            UiMetrics.ApplyFormDefaults(this);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            BuildUi();
            _dateChangeTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
            _dateChangeTimer.Tick += async (_, __) => await RefreshSummariesIfDateChangedAsync();
            _dateChangeTimer.Start();
            Shown += async (_, __) => await InitializeAfterLoginAsync();
            FormClosed += (_, __) =>
            {
                _businessSelectorMenu?.Dispose();
                _activeEmbeddedForm?.Dispose();
                _dateChangeTimer.Dispose();
            };
        }
    }
}
