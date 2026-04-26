using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashTracker.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();

        var appData = AppDataPathResolver.Resolve();
        EnvFileLoader.Load();

        var startupMetrics = new StartupMetrics(appData);
        startupMetrics.Mark("program-start");

        var services = new ServiceCollection();
        var dbPath = Path.Combine(appData, "cashtracker.db");
        var hadExistingDb = File.Exists(dbPath);
        var hadExistingAppState = File.Exists(Path.Combine(appData, "app-state.json"));
        var mirrorDbPaths = AppDataPathResolver.GetMirrorRoots(appData)
            .Select(root => Path.Combine(root, "cashtracker.db"))
            .ToArray();
        var appState = AppStateStore.Load(appData);
        AppLocalization.SetLanguage(appState.LanguageCode);
        CultureInfo.DefaultThreadCurrentCulture = AppLocalization.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = AppLocalization.CurrentCulture;

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var userSetup = UserTelegramSetupStore.Load(appData);

        var botToken = FirstNonEmpty(
            config["Telegram:BotToken"],
            userSetup.BotToken);

        var chatId = FirstNonEmpty(
            config["Telegram:ChatId"],
            userSetup.ChatId,
            userSetup.UserId);

        var allowedUserIds = FirstNonEmpty(
            config["Telegram:AllowedUserIds"],
            userSetup.AllowedUserIds,
            ResolveDefaultAllowedUserIds(chatId));

        var telegramSettings = new TelegramSettings
        {
            BotToken = botToken,
            ChatId = chatId,
            EnableCommands = !bool.TryParse(config["Telegram:EnableCommands"], out var ec) || ec,
            AllowedUserIds = allowedUserIds,
            PollTimeoutSeconds = int.TryParse(config["Telegram:PollTimeoutSeconds"], out var pts) ? pts : 20
        };

        var receiptOcrSettings = new ReceiptOcrSettings
        {
            Provider = FirstNonEmpty(config["ReceiptOcr:Provider"], "Gemini"),
            ApiKey = FirstNonEmpty(config["ReceiptOcr:ApiKey"]),
            Model = FirstNonEmpty(config["ReceiptOcr:Model"], "gemini-2.5-flash"),
            SessionTimeoutMinutes = int.TryParse(config["ReceiptOcr:SessionTimeoutMinutes"], out var stm) ? stm : 30
        };

        var updateSettings = new UpdateSettings
        {
            RepoOwner = FirstNonEmpty(config["Update:RepoOwner"], "01burark-oss"),
            RepoName = FirstNonEmpty(config["Update:RepoName"], "CashTracker"),
            AutoCheckDelaySeconds = int.TryParse(config["Update:AutoCheckDelaySeconds"], out var acd) ? acd : 30
        };

        services.AddDbContextFactory<CashTrackerDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IIsletmeService, IsletmeService>();
        services.AddScoped<IKalemTanimiService, KalemTanimiService>();
        services.AddScoped<IKasaService, KasaService>();
        services.AddScoped<ICariService, CariService>();
        services.AddScoped<IUrunHizmetService, UrunHizmetService>();
        services.AddScoped<IStokService, StokService>();
        services.AddScoped<IFaturaService, FaturaService>();
        services.AddScoped<ITahsilatOdemeService, TahsilatOdemeService>();
        services.AddScoped<IGibPortalService, GibPortalService>();
        services.AddScoped<IOnMuhasebeReportService, OnMuhasebeReportService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<IDashboardSnapshotService, DashboardSnapshotService>();
        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        services.AddSingleton<IGibPortalClient, GibPortalClient>();
        services.AddSingleton<IInstallIdentityService, InstallIdentityService>();
        services.AddSingleton<ILicenseRuntimeStateStore, LicenseRuntimeStateStore>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IAppSecurityService, AppSecurityService>();
        services.AddSingleton(telegramSettings);
        services.AddSingleton(receiptOcrSettings);
        services.AddSingleton(updateSettings);
        services.AddSingleton(new AppRuntimeOptions { AppDataPath = appData });
        services.AddSingleton(new DatabasePaths(dbPath, mirrorDbPaths));
        services.AddSingleton(startupMetrics);

        services.AddSingleton<HttpClient>();
        services.AddSingleton<GitHubUpdateService>();
        services.AddSingleton<TelegramBotService>(sp =>
            new TelegramBotService(sp.GetRequiredService<HttpClient>(), telegramSettings.BotToken));
        services.AddSingleton<ITelegramApprovalService, TelegramApprovalService>();
        services.AddSingleton<IReceiptOcrService, GeminiReceiptOcrService>();
        services.AddSingleton<ITelegramReceiptSessionStore, TelegramReceiptSessionStore>();
        services.AddSingleton<ITelegramStockSessionStore, TelegramStockSessionStore>();
        services.AddSingleton<IBarcodeReaderService, BarcodeReaderService>();

        services.AddSingleton<IDailyReportService, DailyReportService>();
        services.AddSingleton<DatabaseBackupService>();
        services.AddSingleton<BackupReportService>();
        services.AddSingleton<PinReminderService>();
        services.AddSingleton<TelegramCommandService>();
        services.AddSingleton<TelegramPollingService>();

        services.AddTransient<MainForm>();

        using var provider = services.BuildServiceProvider();

        using (var db = provider.GetRequiredService<IDbContextFactory<CashTrackerDbContext>>().CreateDbContext())
        {
            db.Database.EnsureCreated();
            SchemaMigrator.EnsureKasaSchema(db);
        }

        startupMetrics.Mark("db-ready");

        if (!EnsureLicenseReady(
                provider,
                new LicenseStartupContext
                {
                    HadExistingAppState = hadExistingAppState,
                    HadExistingDatabase = hadExistingDb
                }))
            return;

        startupMetrics.Mark("license-ready");
        provider.GetRequiredService<ILicenseService>().ApplyReceiptOcrSettingsAsync(receiptOcrSettings).GetAwaiter().GetResult();

        if (!EnsurePinReady(provider))
            return;

        startupMetrics.Mark("pin-ready");

        if (!EnsureOptionalOnboarding(provider, appData, telegramSettings))
            return;

        startupMetrics.Mark("onboarding-ready");

        var mainForm = provider.GetRequiredService<MainForm>();
        mainForm.Shown += async (_, __) =>
        {
            startupMetrics.Mark("mainform-shown");
            if (!telegramSettings.IsEnabled)
                return;

            await Task.Delay(2500);
            provider.GetRequiredService<TelegramPollingService>().Start();
            startupMetrics.Mark("telegram-polling-started");
        };

        Application.Run(mainForm);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string ResolveDefaultAllowedUserIds(string chatId)
    {
        return long.TryParse(chatId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedChatId) &&
               parsedChatId > 0
            ? parsedChatId.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static bool EnsureLicenseReady(ServiceProvider provider, LicenseStartupContext startupContext)
    {
        var licenseService = provider.GetRequiredService<ILicenseService>();
        var startupMetrics = provider.GetRequiredService<StartupMetrics>();
        var access = licenseService.EvaluateAccessAsync(startupContext).GetAwaiter().GetResult();
        startupMetrics.Mark($"license-access:{access.Mode}");
        if (access.Mode != LicenseAccessMode.Blocked)
            return true;

        while (true)
        {
            using var activationForm = new LicenseActivationForm(licenseService, provider.GetRequiredService<ReceiptOcrSettings>());
            var result = activationForm.ShowDialog();
            startupMetrics.Mark($"license-activation-result:{result}");
            if (result != DialogResult.OK)
                return false;

            access = licenseService.EvaluateAccessAsync().GetAwaiter().GetResult();
            startupMetrics.Mark($"license-access-recheck:{access.Mode}");
            if (access.Mode != LicenseAccessMode.Blocked)
                return true;
        }
    }

    private static bool EnsurePinReady(ServiceProvider provider)
    {
        var appSecurity = provider.GetRequiredService<IAppSecurityService>();
        var startupMetrics = provider.GetRequiredService<StartupMetrics>();
        var isDefaultPin = appSecurity.IsDefaultPinAsync().GetAwaiter().GetResult();
        if (isDefaultPin)
        {
            using var setupForm = new PinSetupForm(appSecurity, isFirstRun: true);
            var result = setupForm.ShowDialog();
            startupMetrics.Mark($"pin-setup-result:{result}");
            return result == DialogResult.OK;
        }

        using var loginForm = new PinLoginForm(appSecurity, provider.GetRequiredService<PinReminderService>());
        var loginResult = loginForm.ShowDialog();
        startupMetrics.Mark($"pin-login-result:{loginResult}");
        return loginResult == DialogResult.OK;
    }

    private static bool EnsureOptionalOnboarding(
        ServiceProvider provider,
        string appDataPath,
        TelegramSettings telegramSettings)
    {
        var startupMetrics = provider.GetRequiredService<StartupMetrics>();
        var state = AppStateStore.Load(appDataPath);
        if (state.HasCompletedOnboarding)
        {
            startupMetrics.Mark("onboarding-already-complete");
            return true;
        }

        state.HasCompletedOnboarding = true;
        AppStateStore.Save(appDataPath, state);
        startupMetrics.Mark("onboarding-skipped");
        return true;
    }
}
