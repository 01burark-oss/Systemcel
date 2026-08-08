using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Systemcel.Api;
using Systemcel.Api.Api;
using Systemcel.Api.Hubs;
using Systemcel.Api.Import;
using Systemcel.Api.Services;

// Systemcel'in mevcut PostgreSQL semasi timestamp without time zone kullanir.
// Npgsql veri kaynagini kurmadan once bu geriye uyumluluk sozlesmesini etkinlestir.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient.Telegram", LogLevel.None);
builder.Logging.AddFilter("System.Net.Http.HttpClient.DeepSeek", LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var appDataPath = ResolveAppDataPath(builder.Configuration);
Directory.CreateDirectory(appDataPath);

var databaseOptions = ResolveDatabaseOptions(builder.Configuration);
var databasePaths = new DatabasePaths(string.Empty);
var clerkAuthenticationOptions = ClerkAuthenticationSetup.Resolve(builder.Configuration);
if (!builder.Environment.IsDevelopment() && !clerkAuthenticationOptions.Enabled)
    throw new InvalidOperationException("Clerk authentication must be configured outside Development.");
var systemcelEnvironmentName = ResolveEnvironmentName(builder.Configuration, builder.Environment);
var allowedOrigins = ResolveAllowedOrigins(builder.Configuration, builder.Environment);
var yonetimOptions = ResolveYonetimOptions(builder.Configuration);
var telegramSettings = ResolveTelegramSettings(builder.Configuration, appDataPath);
var deepSeekSettings = ResolveDeepSeekSettings(builder.Configuration);
var receiptOcrSettings = builder.Configuration.GetSection("ReceiptOcr").Get<ReceiptOcrSettings>() ?? new ReceiptOcrSettings();
var paymentOptions = ResolvePaymentOptions(builder.Configuration, builder.Environment);
var reminderEmailOptions = ResolveSubscriptionReminderEmailOptions(builder.Configuration);
var secretEncryptionKey = ResolveSecretEncryptionKey(builder.Configuration, builder.Environment, appDataPath);
builder.Services.AddSingleton(databasePaths);
builder.Services.AddSingleton(databaseOptions);
builder.Services.AddSingleton(new AppRuntimeOptions { AppDataPath = appDataPath });
builder.Services.AddSingleton(new MuhasebeciSohbetStorageOptions { AppDataPath = appDataPath });
builder.Services.AddClerkAuthentication(clerkAuthenticationOptions);
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52L * 1024 * 1024;
    options.MemoryBufferThreshold = 64 * 1024;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
            return RateLimitPartition.GetNoLimiter("non-api");

        var subject = context.User.FindFirst("sub")?.Value;
        var partition = !string.IsNullOrWhiteSpace(subject)
            ? $"user:{subject}"
            : $"ip:{context.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = string.IsNullOrWhiteSpace(subject) ? 120 : 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
    options.AddPolicy("sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(BuildRateLimitPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(BuildRateLimitPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://systemcel.app/problems/rate-limit-exceeded",
            title = "Çok fazla istek",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Kısa sürede çok fazla istek gönderildi. Lütfen biraz bekleyip yeniden deneyin.",
            traceId = context.HttpContext.TraceIdentifier
        }, cancellationToken);
    };
});

builder.Services.AddDbContextFactory<CashTrackerDbContext>(options =>
{
    ConfigureDatabase(options, databaseOptions);
});

builder.Services.AddSingleton(telegramSettings);
builder.Services.AddSingleton(yonetimOptions);
builder.Services.AddSingleton(deepSeekSettings);
builder.Services.AddSingleton(receiptOcrSettings);
builder.Services.AddSingleton(paymentOptions);
builder.Services.AddSingleton(reminderEmailOptions);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUserContext, HttpCurrentUserContext>();

builder.Services.AddSingleton<IIsletmeService, IsletmeService>();
builder.Services.AddSingleton<IIsletmeUyelikService, IsletmeUyelikService>();
builder.Services.AddSingleton<IKalemTanimiService, KalemTanimiService>();
builder.Services.AddSingleton<IKasaService, KasaService>();
builder.Services.AddSingleton<ISummaryService, SummaryService>();
builder.Services.AddSingleton<IDailyReportService, DailyReportService>();
builder.Services.AddSingleton<ICariService, CariService>();
builder.Services.AddSingleton<IUrunHizmetService, UrunHizmetService>();
builder.Services.AddSingleton<IStokService, StokService>();
builder.Services.AddSingleton<IHizliSatisService, HizliSatisService>();
builder.Services.AddSingleton<IFaturaService, FaturaService>();
builder.Services.AddSingleton<ITahsilatOdemeService, TahsilatOdemeService>();
builder.Services.AddSingleton<IOnMuhasebeReportService, OnMuhasebeReportService>();
builder.Services.AddSingleton<ISubscriptionEntitlementService, SubscriptionEntitlementService>();
builder.Services.AddSingleton<IEntitlementGuard, EntitlementGuard>();
builder.Services.AddSingleton<IPaymentPricingService>(_ => new PaymentPricingService(paymentOptions.VatRate));
builder.Services.AddSingleton<IPaymentProvider>(_ => paymentOptions.UsesFakeProvider
    ? new FakePaymentProvider(paymentOptions.FakeSecret)
    : new UnconfiguredPaymentProvider());
builder.Services.AddSingleton<ISubscriptionLifecycleService, SubscriptionLifecycleService>();
builder.Services.AddSingleton<IPaymentReconciliationService, PaymentReconciliationService>();
builder.Services.AddSingleton<ISubscriptionReminderSender>(_ => reminderEmailOptions.IsConfigured
    ? new SmtpSubscriptionReminderSender(reminderEmailOptions)
    : new UnconfiguredSubscriptionReminderSender());
builder.Services.AddHostedService<SubscriptionLifecycleHostedService>();
builder.Services.AddHostedService<PaymentReconciliationHostedService>();
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
if (secretEncryptionKey is not null)
    builder.Services.AddSingleton<ISecretProtector>(new AesGcmSecretProtector(secretEncryptionKey));
else if (OperatingSystem.IsWindows() && builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
else
    throw new InvalidOperationException("Production secret protector configuration is missing.");
builder.Services.AddHostedService<LegacySecretMigrationHostedService>();
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

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "geolocation=(), usb=(), browsing-topics=()");
        headers.TryAdd("Content-Security-Policy", "base-uri 'self'; object-src 'none'; frame-ancestors 'none'");
        return Task.CompletedTask;
    });
    await next();
});
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (EntitlementViolationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://systemcel.app/problems/{ex.Code}",
            title = "Plan sınırı",
            status = StatusCodes.Status409Conflict,
            detail = ex.Message,
            code = ex.Code,
            limitName = ex.LimitName,
            limit = ex.Limit,
            current = ex.Current,
            suggestedPlanCode = ex.SuggestedPlanCode,
            traceId = context.TraceIdentifier
        });
    }
});
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
app.UseRateLimiter();

app.MapGet("/api/health/live", () => Results.Ok(new
{
    ad = "Systemcel.Api",
    durum = "canli",
    tarih = DateTimeOffset.UtcNow
})).AllowAnonymous();
app.MapGet("/api/health/ready", CheckReadinessAsync).AllowAnonymous();
app.MapGet("/api/health", CheckReadinessAsync).AllowAnonymous();

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
app.MapBillingApi();
app.MapDesktopImportApi();
app.MapAiAssistantApi();
app.MapMuhasebeciApi();
app.MapSohbetMerkeziApi();
app.MapYonetimApi();
app.MapUyelikApi();
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

static async Task<IResult> CheckReadinessAsync(
    IDbContextFactory<CashTrackerDbContext> dbFactory,
    DatabaseRuntimeOptions databaseOptions,
    CancellationToken ct)
{
    try
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!await db.Database.CanConnectAsync(ct))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Hizmet hazır değil",
                detail: "Veritabanı bağlantısı kurulamadı.");
        }

        return Results.Ok(new
        {
            ad = "Systemcel.Api",
            durum = "hazir",
            veritabani = databaseOptions.Provider,
            tarih = DateTimeOffset.UtcNow
        });
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        throw;
    }
    catch
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Hizmet hazır değil",
            detail: "Veritabanı sağlık kontrolü tamamlanamadı.");
    }
}

static DatabaseRuntimeOptions ResolveDatabaseOptions(IConfiguration configuration)
{
    var connectionString = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_DATABASE_CONNECTION_STRING"),
        Environment.GetEnvironmentVariable("DATABASE_URL"),
        configuration.GetConnectionString("Systemcel"),
        configuration["Systemcel:Database:ConnectionString"]);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Systemcel runtime PostgreSQL connection string bulunamadi. " +
            "SYSTEMCEL_DATABASE_CONNECTION_STRING, DATABASE_URL, ConnectionStrings:Systemcel veya " +
            "Systemcel:Database:ConnectionString ile tanimlayin.");
    }

    return new DatabaseRuntimeOptions
    {
        Provider = "PostgreSql",
        ConnectionString = connectionString
    };
}

static PaymentRuntimeOptions ResolvePaymentOptions(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configuredProvider = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_PAYMENT_PROVIDER"),
        configuration["Systemcel:Payment:Provider"]);
    var provider = string.IsNullOrWhiteSpace(configuredProvider)
        ? environment.IsDevelopment() ? "Fake" : "Unconfigured"
        : configuredProvider.Trim();

    var fakeSecret = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_FAKE_PAYMENT_SECRET"),
        configuration["Systemcel:Payment:FakeSecret"]);
    if (string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(fakeSecret))
    {
        if (environment.IsDevelopment())
            fakeSecret = "systemcel-local-fake-payment-secret";
        else
            provider = "Unconfigured";
    }

    var vatRate = 20m;
    var configuredVatRate = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_PAYMENT_VAT_RATE"),
        configuration["Systemcel:Payment:VatRate"]);
    if (!string.IsNullOrWhiteSpace(configuredVatRate) &&
        decimal.TryParse(configuredVatRate, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var parsedVatRate))
        vatRate = parsedVatRate;

    return new PaymentRuntimeOptions
    {
        Provider = provider,
        FakeSecret = fakeSecret ?? string.Empty,
        PublicBaseUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_PUBLIC_BASE_URL"),
            configuration["Systemcel:PublicBaseUrl"]) ?? string.Empty,
        VatRate = vatRate
    };
}

static SubscriptionReminderEmailOptions ResolveSubscriptionReminderEmailOptions(IConfiguration configuration)
{
    var configuredPort = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_PORT"),
        configuration["Systemcel:Email:Smtp:Port"]);
    var configuredSsl = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_ENABLE_SSL"),
        configuration["Systemcel:Email:Smtp:EnableSsl"]);

    return new SubscriptionReminderEmailOptions
    {
        Host = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_HOST"),
            configuration["Systemcel:Email:Smtp:Host"]) ?? string.Empty,
        Port = int.TryParse(configuredPort, out var port) && port is > 0 and <= 65535 ? port : 587,
        EnableSsl = !bool.TryParse(configuredSsl, out var enableSsl) || enableSsl,
        UserName = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_USERNAME"),
            configuration["Systemcel:Email:Smtp:UserName"]) ?? string.Empty,
        Password = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_PASSWORD"),
            configuration["Systemcel:Email:Smtp:Password"]) ?? string.Empty,
        FromAddress = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_FROM_ADDRESS"),
            configuration["Systemcel:Email:Smtp:FromAddress"]) ?? string.Empty,
        FromName = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_SMTP_FROM_NAME"),
            configuration["Systemcel:Email:Smtp:FromName"]) ?? "Systemcel",
        PublicBaseUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SYSTEMCEL_PUBLIC_BASE_URL"),
            configuration["Systemcel:PublicBaseUrl"]) ?? "https://systemcel.app"
    };
}

static byte[]? ResolveSecretEncryptionKey(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    string appDataPath)
{
    var configured = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_SECRET_ENCRYPTION_KEY"),
        configuration["Systemcel:Security:SecretEncryptionKey"]);
    if (!string.IsNullOrWhiteSpace(configured))
    {
        try
        {
            var key = Convert.FromBase64String(configured.Trim());
            if (key.Length != 32)
                throw new InvalidOperationException("SYSTEMCEL_SECRET_ENCRYPTION_KEY must decode to exactly 32 bytes.");
            return key;
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "SYSTEMCEL_SECRET_ENCRYPTION_KEY must be a valid Base64 encoded 32-byte key.", ex);
        }
    }

    if (!environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "SYSTEMCEL_SECRET_ENCRYPTION_KEY is required outside Development. " +
            "Store a stable Base64 encoded 32-byte key as an encrypted platform secret.");
    }

    if (OperatingSystem.IsWindows())
        return null;

    var keyDirectory = Path.Combine(appDataPath, "security");
    var keyPath = Path.Combine(keyDirectory, "local-development-aes.key");
    Directory.CreateDirectory(keyDirectory);
    if (File.Exists(keyPath))
        return Convert.FromBase64String(File.ReadAllText(keyPath).Trim());

    var generated = RandomNumberGenerator.GetBytes(32);
    File.WriteAllText(keyPath, Convert.ToBase64String(generated));
    try
    {
        File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    catch (PlatformNotSupportedException)
    {
        // Development-only fallback; production never reaches this branch.
    }
    return generated;
}

static string ResolveEnvironmentName(IConfiguration configuration, IWebHostEnvironment environment)
{
    return FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_ENVIRONMENT_NAME"),
        configuration["Systemcel:EnvironmentName"],
        environment.EnvironmentName,
        "Production")!;
}

static string[] ResolveAllowedOrigins(IConfiguration configuration, IWebHostEnvironment environment)
{
    var origins = FirstNonEmpty(
        Environment.GetEnvironmentVariable("SYSTEMCEL_ALLOWED_ORIGINS"),
        configuration["Systemcel:AllowedOrigins"]);

    var resolved = SplitCsv(origins)
        .Select(x => x.TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (!environment.IsDevelopment() && resolved.Any(x =>
            x.Contains('*', StringComparison.Ordinal) ||
            !Uri.TryCreate(x, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException(
            "SYSTEMCEL_ALLOWED_ORIGINS may contain only explicit HTTPS origins outside Development.");
    }

    return resolved;
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

static string BuildRateLimitPartition(HttpContext context)
{
    var subject = context.User.FindFirst("sub")?.Value;
    return !string.IsNullOrWhiteSpace(subject)
        ? $"user:{subject}"
        : $"ip:{context.Connection.RemoteIpAddress}";
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
