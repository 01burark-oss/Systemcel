using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Systemcel.Api;
using Systemcel.Api.Api;
using Systemcel.Api.Hubs;
using Systemcel.Api.Import;
using Systemcel.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient.Telegram", LogLevel.None);
builder.Logging.AddFilter("System.Net.Http.HttpClient.DeepSeek", LogLevel.Warning);

var appDataPath = ResolveAppDataPath(builder.Configuration);
Directory.CreateDirectory(appDataPath);

var databaseOptions = ResolveDatabaseOptions(builder.Configuration);
var databasePaths = new DatabasePaths(string.Empty);
var clerkAuthenticationOptions = ClerkAuthenticationSetup.Resolve(builder.Configuration);
var systemcelEnvironmentName = ResolveEnvironmentName(builder.Configuration, builder.Environment);
var allowedOrigins = ResolveAllowedOrigins(builder.Configuration);
var yonetimOptions = ResolveYonetimOptions(builder.Configuration);
var telegramSettings = ResolveTelegramSettings(builder.Configuration, appDataPath);
var deepSeekSettings = ResolveDeepSeekSettings(builder.Configuration);
var receiptOcrSettings = builder.Configuration.GetSection("ReceiptOcr").Get<ReceiptOcrSettings>() ?? new ReceiptOcrSettings();
builder.Services.AddSingleton(databasePaths);
builder.Services.AddSingleton(databaseOptions);
builder.Services.AddSingleton(new AppRuntimeOptions { AppDataPath = appDataPath });
builder.Services.AddSingleton(new MuhasebeciSohbetStorageOptions { AppDataPath = appDataPath });
builder.Services.AddClerkAuthentication(clerkAuthenticationOptions);
builder.Services.AddSignalR();

builder.Services.AddDbContextFactory<CashTrackerDbContext>(options =>
{
    ConfigureDatabase(options, databaseOptions);
});

builder.Services.AddSingleton(telegramSettings);
builder.Services.AddSingleton(yonetimOptions);
builder.Services.AddSingleton(deepSeekSettings);
builder.Services.AddSingleton(receiptOcrSettings);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUserContext, HttpCurrentUserContext>();

builder.Services.AddSingleton<IIsletmeService, IsletmeService>();
builder.Services.AddSingleton<IKalemTanimiService, KalemTanimiService>();
builder.Services.AddSingleton<IKasaService, KasaService>();
builder.Services.AddSingleton<ISummaryService, SummaryService>();
builder.Services.AddSingleton<IDailyReportService, DailyReportService>();
builder.Services.AddSingleton<ICariService, CariService>();
builder.Services.AddSingleton<IUrunHizmetService, UrunHizmetService>();
builder.Services.AddSingleton<IStokService, StokService>();
builder.Services.AddSingleton<IFaturaService, FaturaService>();
builder.Services.AddSingleton<ITahsilatOdemeService, TahsilatOdemeService>();
builder.Services.AddSingleton<IOnMuhasebeReportService, OnMuhasebeReportService>();
builder.Services.AddSingleton<ISubscriptionEntitlementService, SubscriptionEntitlementService>();
builder.Services.AddSingleton<IMuhasebeciPortalService, MuhasebeciPortalService>();
builder.Services.AddSingleton<IMuhasebeciSohbetMerkeziService, MuhasebeciSohbetMerkeziService>();
builder.Services.AddSingleton<ISystemcelYonetimService, SystemcelYonetimService>();
builder.Services.AddSingleton<IAccountantApplicationNotifier>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var settings = sp.GetRequiredService<TelegramSettings>();
    return new TelegramAccountantApplicationNotifier(factory.CreateClient("Telegram"), settings);
});
builder.Services.AddSingleton<IAiUsageQuotaService, AiUsageQuotaService>();
builder.Services.AddSingleton<ITelegramMessageFooterProvider, TelegramMessageFooterProvider>();
if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
else
    builder.Services.AddSingleton<ISecretProtector, Base64SecretProtector>();
builder.Services.AddSingleton<IGibPortalService, GibPortalService>();
builder.Services.AddSingleton<IAppSecurityService, AppSecurityService>();
builder.Services.AddSingleton<IDashboardSnapshotService, DashboardSnapshotService>();
builder.Services.AddSingleton<DatabaseBackupService>();
builder.Services.AddSingleton<BackupReportService>();
builder.Services.AddSingleton<PinReminderService>();
builder.Services.AddHttpClient("Telegram");
builder.Services.AddHttpClient("DeepSeek", client =>
{
    client.Timeout = TimeSpan.FromSeconds(deepSeekSettings.EffectiveTimeoutSeconds);
});
builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var settings = sp.GetRequiredService<TelegramSettings>();
    var footerProvider = sp.GetRequiredService<ITelegramMessageFooterProvider>();
    return new TelegramBotService(factory.CreateClient("Telegram"), settings, footerProvider);
});
builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var settings = sp.GetRequiredService<DeepSeekSettings>();
    return new DeepSeekChatClient(factory.CreateClient("DeepSeek"), settings);
});
builder.Services.AddHttpClient<IGibPortalClient, GibPortalClient>();
if (string.Equals(receiptOcrSettings.EffectiveProvider, "OcrSpace", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IReceiptOcrService, OcrSpaceDeepSeekReceiptOcrService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    });
}
else
{
    builder.Services.AddHttpClient<IReceiptOcrService, GeminiReceiptOcrService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    });
}
builder.Services.AddSingleton<IAiAssistantService, AiAssistantService>();
builder.Services.AddSingleton<ITelegramApprovalService, TelegramApprovalService>();
builder.Services.AddSingleton<ITelegramReceiptSessionStore, TelegramReceiptSessionStore>();
builder.Services.AddSingleton<ITelegramStockSessionStore, TelegramStockSessionStore>();
builder.Services.AddSingleton<ITelegramPairingService, TelegramPairingService>();
builder.Services.AddSingleton<TelegramCommandService>();
builder.Services.AddSingleton<TelegramPollingService>();
if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<IBarcodeReaderService, BarcodeReaderService>();
else
    builder.Services.AddSingleton<IBarcodeReaderService, UnsupportedBarcodeReaderService>();
builder.Services.AddSingleton<DesktopImportCodeStore>();
builder.Services.AddSingleton<DesktopImportService>();
builder.Services.AddSingleton<ScreenApi>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CashTrackerDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await PostgreSqlMigrationGuard.ApplyMigrationsAsync(db);
    await db.Database.CloseConnectionAsync();
}

app.UseCors("ConfiguredOrigins");
if (clerkAuthenticationOptions.Enabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api/ekran") &&
            context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next();
    });
}

app.MapGet("/api/health", () => Results.Ok(new
{
    ad = "Systemcel.Api",
    durum = "hazir",
    veritabani = databaseOptions.Provider,
    tarih = DateTimeOffset.Now
}));

app.MapGet("/api/public/config", () => Results.Ok(new
{
    environmentName = systemcelEnvironmentName,
    clerk = new
    {
        enabled = clerkAuthenticationOptions.Enabled,
        publishableKey = clerkAuthenticationOptions.PublishableKey,
        jsUrl = clerkAuthenticationOptions.JsUrl
    }
}));

app.MapSubscriptionApi();
app.MapDesktopImportApi();
app.MapAiAssistantApi();
app.MapMuhasebeciApi();
app.MapSohbetMerkeziApi();
app.MapYonetimApi();
var sohbetHub = app.MapHub<MuhasebeciSohbetHub>("/hubs/muhasebeci-sohbet");
if (clerkAuthenticationOptions.Enabled)
    sohbetHub.RequireAuthorization();
app.Services.GetRequiredService<ScreenApi>().MapApi(app);
app.Services.GetRequiredService<TelegramPollingService>().Start();
MapReactStaticFiles(app);

await app.RunAsync();

static string ResolveAppDataPath(IConfiguration configuration)
{
    var env = Environment.GetEnvironmentVariable("SYSTEMCEL_APPDATA");
    if (!string.IsNullOrWhiteSpace(env))
        return Path.GetFullPath(env);

    var configured = configuration["Systemcel:AppDataPath"];
    if (!string.IsNullOrWhiteSpace(configured))
        return Path.GetFullPath(configured);

    return Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Systemcel",
        "Web");
}

static DatabaseRuntimeOptions ResolveDatabaseOptions(IConfiguration configuration)
{
    var connectionString = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_DATABASE_CONNECTION_STRING"),
        configuration.GetConnectionString("Systemcel"),
        configuration["Systemcel:Database:ConnectionString"]);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Systemcel runtime PostgreSQL connection string bulunamadi. " +
            "SYSTEMCEL_DATABASE_CONNECTION_STRING, ConnectionStrings:Systemcel veya " +
            "Systemcel:Database:ConnectionString ile tanimlayin.");
    }

    return new DatabaseRuntimeOptions
    {
        Provider = "PostgreSql",
        ConnectionString = connectionString
    };
}

static string ResolveEnvironmentName(IConfiguration configuration, IWebHostEnvironment environment)
{
    return FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_ENVIRONMENT_NAME"),
        configuration["Systemcel:EnvironmentName"],
        environment.EnvironmentName,
        "Production")!;
}

static string[] ResolveAllowedOrigins(IConfiguration configuration)
{
    var origins = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_ALLOWED_ORIGINS"),
        configuration["Systemcel:AllowedOrigins"]);

    return SplitCsv(origins);
}

static TelegramSettings ResolveTelegramSettings(IConfiguration configuration, string appDataPath)
{
    var settings = configuration.GetSection("Telegram").Get<TelegramSettings>() ?? new TelegramSettings();
    var userSetup = UserTelegramSetupStore.Load(appDataPath);

    if (!string.IsNullOrWhiteSpace(userSetup.ChatId))
        settings.ChatId = userSetup.ChatId;

    if (!string.IsNullOrWhiteSpace(userSetup.AllowedUserIds))
        settings.AllowedUserIds = userSetup.AllowedUserIds;

    return settings;
}

static SystemcelYonetimOptions ResolveYonetimOptions(IConfiguration configuration)
{
    return new SystemcelYonetimOptions
    {
        AdminClerkUserIds = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_ADMIN_CLERK_USER_IDS"),
            configuration["Systemcel:Admin:ClerkUserIds"]) ?? string.Empty,
        AdminEmails = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_ADMIN_EMAILS"),
            configuration["Systemcel:Admin:Emails"]) ?? string.Empty
    };
}

static DeepSeekSettings ResolveDeepSeekSettings(IConfiguration configuration)
{
    var settings = configuration.GetSection("DeepSeek").Get<DeepSeekSettings>() ?? new DeepSeekSettings();
    var apiKey = FirstNonEmpty(
        Environment.GetEnvironmentVariable("DeepSeek__ApiKey"),
        configuration["DeepSeek:ApiKey"],
        settings.ApiKey);

    if (!string.IsNullOrWhiteSpace(apiKey))
        settings.ApiKey = apiKey;

    return settings;
}

static void ConfigureDatabase(DbContextOptionsBuilder options, DatabaseRuntimeOptions databaseOptions)
{
    options.UseNpgsql(databaseOptions.ConnectionString);
}

static string? FirstNonEmpty(params string?[] values)
{
    foreach (var value in values)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;
    }

    return null;
}

static string[] SplitCsv(string? value)
{
    return (value ?? string.Empty)
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static void MapReactStaticFiles(WebApplication app)
{
    var distPath = ResolveReactDistPath(app.Environment.ContentRootPath);
    var indexPath = Path.Combine(distPath, "index.html");
    if (!Directory.Exists(distPath) || !File.Exists(indexPath))
    {
        app.MapFallback(() => Results.Content(
            "<!doctype html><html><head><meta charset=\"utf-8\"><title>Systemcel</title></head><body style=\"font-family:Segoe UI,sans-serif;padding:32px;background:#ecf1f8;color:#172234\"><h1>Systemcel API hazir</h1><p>React arayuzu icin Systemcel.Web klasorunde build alin.</p></body></html>",
            "text/html; charset=utf-8"));
        return;
    }

    var fileProvider = new PhysicalFileProvider(distPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    app.MapFallback(async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
    });
}

static string ResolveReactDistPath(string contentRoot)
{
    var bundledDistPath = Path.Combine(contentRoot, "wwwroot");
    if (File.Exists(Path.Combine(bundledDistPath, "index.html")))
        return bundledDistPath;

    return Path.GetFullPath(Path.Combine(contentRoot, "..", "Systemcel.Web", "dist"));
}
