using System.Net;
using System.Text;
using System.Text.Json;
using System.Globalization;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Systemcel.Api.Api;

internal static class BillingApi
{
    private const string ConsentVersion = "abonelik-onayi-2026-10-v6";

    public static void MapBillingApi(this WebApplication app)
    {
        var quoteEndpoint = app.MapGet(
            "/api/abonelik/teklif",
            async (
                string planKodu,
                string faturalamaDonemi,
                int? ekMusteriKredisi,
                IIsletmeService isletmeService,
                IPaymentPricingService pricing,
                IDbContextFactory<CashTrackerDbContext> dbFactory,
                PaymentRuntimeOptions paymentOptions,
                CancellationToken ct) =>
            {
                try
                {
                    var business = await isletmeService.GetActiveAsync();
                    if (paymentOptions.UsesPaytrProvider &&
                        !paymentOptions.AllowsPaytrTestBusiness(business.Id))
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    var period = NormalizeBillingPeriod(faturalamaDonemi);
                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var founderPrice = await CanOfferFounderPriceAsync(db, business.Id, ct);
                    var activeSubscription = await db.Abonelikler.AsNoTracking()
                        .Where(x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi && x.Durum == "Aktif")
                        .OrderByDescending(x => x.DonemBaslangicAt)
                        .FirstOrDefaultAsync(ct);
                    var quote = pricing.CreateChangeQuote(
                        planKodu,
                        business.TenantTipi,
                        period,
                        ekMusteriKredisi ?? 0,
                        ToPricingContext(activeSubscription),
                        DateTime.UtcNow,
                        founderPrice && activeSubscription is null);
                    if (await db.IsletmeDenemeleri.AsNoTracking().AnyAsync(
                            x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi, ct))
                        quote = quote with { TrialDays = 0 };
                    return Results.Ok(new
                    {
                        fiyat = quote,
                        kampanyaKodu = quote.CampaignCode,
                        paytrContactRequired = paymentOptions.AllowsPaytrTestBusiness(business.Id) &&
                            quote.ChangeType != SubscriptionChangeTypes.ScheduledDowngrade,
                        onayMetniSurumu = ConsentVersion,
                        onayMetni = BuildConsentText(quote)
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { mesaj = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { mesaj = ex.Message });
                }
            });

        var summaryEndpoint = app.MapGet(
            "/api/abonelik/ozet",
            async (
                IIsletmeService isletmeService,
                ISubscriptionEntitlementService entitlementService,
                IPaymentPricingService pricing,
                IDbContextFactory<CashTrackerDbContext> dbFactory,
                CancellationToken ct) =>
            {
                var business = await isletmeService.GetActiveAsync();
                var now = DateTime.UtcNow;
                var entitlement = string.Equals(business.TenantTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
                    ? await entitlementService.GetMuhasebeciEntitlementAsync(business.Id, now, ct)
                    : await entitlementService.GetIsletmeEntitlementAsync(business.Id, now, ct);

                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var subscription = await db.Abonelikler.AsNoTracking()
                    .Where(x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi)
                    .OrderByDescending(x => x.DonemBaslangicAt)
                    .FirstOrDefaultAsync(ct);
                var trial = await db.IsletmeDenemeleri.AsNoTracking()
                    .Where(x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi)
                    .OrderByDescending(x => x.BaslangicAt)
                    .FirstOrDefaultAsync(ct);
                var payments = await db.OdemeIslemleri.AsNoTracking()
                    .Where(x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(20)
                    .Select(x => new
                    {
                        x.Id,
                        x.IslemTipi,
                        x.Durum,
                        x.PlanKodu,
                        x.FaturalamaDonemi,
                        x.KampanyaKodu,
                        x.NetTutar,
                        x.ListeNetTutar,
                        x.YenilemeNetTutar,
                        x.KdvTutar,
                        x.ToplamTutar,
                        x.ParaBirimi,
                        x.HataKodu,
                        x.CreatedAt,
                        x.TamamlandiAt
                    })
                    .ToListAsync(ct);

                var cancellationAtPeriodEnd = subscription?.DonemSonundaIptal == true || trial?.DonemSonundaIptal == true;
                SubscriptionCancellationQuote? cancellationQuote = null;
                if (subscription is not null && (subscription.Durum == "Aktif" || subscription.DonemSonundaIptal) && trial?.Durum != "Aktif")
                {
                    var matchedPayments = await db.OdemeIslemleri.AsNoTracking().Where(x =>
                        x.IsletmeId == business.Id && x.HesapTipi == subscription.HesapTipi &&
                        x.SaglayiciIslemId == subscription.SaglayiciAbonelikId).ToListAsync(ct);
                    var matchedPayment = matchedPayments.Count == 1 ? matchedPayments[0] : null;
                    var refunds = matchedPayment is null ? new List<CashTracker.Core.Entities.OdemeIadeTalimati>() :
                        await db.OdemeIadeTalimatlari.AsNoTracking().Where(x => x.OdemeIslemiId == matchedPayment.Id).ToListAsync(ct);
                    cancellationQuote = SubscriptionCancellationPolicy.Calculate(subscription, matchedPayment, refunds, now);
                }
                var canCancel = !cancellationAtPeriodEnd &&
                    (subscription?.Durum == "Aktif" || trial?.Durum == "Aktif");
                var nextRenewalAt = trial?.Durum == "Aktif"
                    ? trial.BitisAt
                    : subscription?.Durum == "Aktif"
                        ? subscription.DonemBitisAt
                        : null;
                var currentRenewalNetAmount = subscription?.YenilemeDonemTutari;
                if (subscription is not null)
                {
                    try
                    {
                        currentRenewalNetAmount = pricing.CreateQuote(
                            subscription.PlanKodu,
                            subscription.HesapTipi,
                            subscription.FaturalamaDonemi,
                            subscription.EkMusteriKredisi,
                            useFounderPrice: false).NetAmount;
                    }
                    catch (InvalidOperationException)
                    {
                        // Eski veya artık satışta olmayan planlarda kayıtlı yenileme tutarını koru.
                    }
                }

                return Results.Ok(new
                {
                    isletmeId = business.Id,
                    isletmeAdi = business.Ad,
                    hesapTipi = business.TenantTipi,
                    haklar = entitlement,
                    durum = trial?.Durum == "Aktif" ? "Deneme" : subscription?.Durum ?? entitlement.Kaynak,
                    sonrakiYenilemeAt = nextRenewalAt,
                    donemSonundaIptal = cancellationAtPeriodEnd,
                    iptalEdilebilir = canCancel,
                    iptalOzeti = cancellationQuote,
                    deneme = trial is null ? null : new
                    {
                        trial.PlanKodu,
                        trial.FaturalamaDonemi,
                        trial.EkMusteriKredisi,
                        trial.Durum,
                        trial.BaslangicAt,
                        trial.BitisAt,
                        trial.OdemeYontemiEklendi,
                        trial.DonemSonundaIptal,
                        trial.IptalAt
                    },
                    abonelik = subscription is null ? null : new
                    {
                        subscription.PlanKodu,
                        subscription.FaturalamaDonemi,
                        subscription.EkMusteriKredisi,
                        subscription.Durum,
                        subscription.DonemTutari,
                        subscription.KampanyaKodu,
                        yenilemeDonemTutari = currentRenewalNetAmount,
                        subscription.IndirimliDonemKalan,
                        subscription.ParaBirimi,
                        subscription.DonemBaslangicAt,
                        subscription.DonemBitisAt,
                        subscription.ToleransBitisAt,
                        subscription.DonemSonundaIptal,
                        subscription.IptalAt,
                        subscription.PlanlananPlanKodu,
                        subscription.PlanlananFaturalamaDonemi,
                        subscription.PlanlananEkMusteriKredisi,
                        subscription.PlanlananDegisiklikAt
                    },
                    odemeler = payments
                });
            });

        var checkoutEndpoint = app.MapPost(
            "/api/abonelik/checkout",
            async (
                CheckoutRequest request,
                HttpContext http,
                IIsletmeService isletmeService,
                ICurrentUserContext currentUserContext,
                ISubscriptionLifecycleService lifecycle,
                IPaymentPricingService pricing,
                IDbContextFactory<CashTrackerDbContext> dbFactory,
                PaymentRuntimeOptions paymentOptions,
                CancellationToken ct) =>
            {
                if (!request.Onaylandi)
                    return Results.BadRequest(new { mesaj = "Abonelik koşullarını onaylamalısınız." });

                var identity = currentUserContext.GetCurrentUser();
                if (identity is null && http.User.Identity?.IsAuthenticated == true)
                    return Results.Unauthorized();

                try
                {
                    var business = await isletmeService.GetActiveAsync();
                    if (paymentOptions.UsesPaytrProvider &&
                        !paymentOptions.AllowsPaytrTestBusiness(business.Id))
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    var email = identity?.Email;
                    if (string.IsNullOrWhiteSpace(email) && identity is not null)
                    {
                        await using var db = await dbFactory.CreateDbContextAsync(ct);
                        email = await db.Kullanicilar.AsNoTracking()
                            .Where(x => x.AuthProviderUserId == identity.ProviderUserId)
                            .Select(x => x.Eposta)
                            .SingleOrDefaultAsync(ct);
                    }

                    email = FirstNonEmpty(email, request.Eposta);
                    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                        return Results.BadRequest(new { mesaj = "Checkout icin gecerli bir e-posta adresi gerekli." });

                    var idempotencyKey = FirstNonEmpty(
                        http.Request.Headers["Idempotency-Key"].FirstOrDefault(),
                        request.IdempotencyKey);
                    if (string.IsNullOrWhiteSpace(idempotencyKey))
                        return Results.BadRequest(new { mesaj = "Idempotency-Key basligi zorunludur." });

                    var baseUri = ResolveBaseUri(http.Request, paymentOptions);
                    var period = NormalizeBillingPeriod(request.FaturalamaDonemi);
                    var useFounderPrice = string.Equals(
                        request.KampanyaKodu,
                        SubscriptionPlanCatalog.KurucuKampanyaKodu,
                        StringComparison.Ordinal);
                    PaymentQuote quote;
                    await using (var trialDb = await dbFactory.CreateDbContextAsync(ct))
                    {
                        var activeSubscription = await trialDb.Abonelikler.AsNoTracking()
                            .Where(x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi && x.Durum == "Aktif")
                            .OrderByDescending(x => x.DonemBaslangicAt)
                            .FirstOrDefaultAsync(ct);
                        quote = pricing.CreateChangeQuote(
                            request.PlanKodu,
                            business.TenantTipi,
                            period,
                            request.EkMusteriKredisi,
                            ToPricingContext(activeSubscription),
                            DateTime.UtcNow,
                            useFounderPrice && activeSubscription is null);
                        if (await trialDb.IsletmeDenemeleri.AsNoTracking().AnyAsync(
                                x => x.IsletmeId == business.Id && x.HesapTipi == business.TenantTipi, ct))
                            quote = quote with { TrialDays = 0 };
                    }
                    if (paymentOptions.UsesPaytrProvider &&
                        quote.ChangeType != SubscriptionChangeTypes.ScheduledDowngrade &&
                        (!ValidPaytrContact(request.OdemeAdSoyad, 60) ||
                         !ValidPaytrContact(request.OdemeAdres, 400) ||
                         !ValidPaytrContact(request.OdemeTelefon, 20)))
                        return Results.BadRequest(new { mesaj = "Ödeme için ad soyad, adres ve telefon gereklidir." });
                    var customerIp = ResolveCustomerIp(http, paymentOptions);
                    if (paymentOptions.UsesPaytrProvider &&
                        quote.ChangeType != SubscriptionChangeTypes.ScheduledDowngrade &&
                        !IPAddress.TryParse(customerIp, out _))
                        return Results.BadRequest(new { mesaj = "Ödeme için geçerli istemci IP adresi bulunamadı." });
                    var consentText = BuildConsentText(quote);
                    var command = new SubscriptionCheckoutCommand(
                        business.Id,
                        business.TenantTipi,
                        request.PlanKodu,
                        period,
                        request.EkMusteriKredisi,
                        request.KampanyaKodu ?? string.Empty,
                        idempotencyKey,
                        identity?.ProviderUserId ?? "local-development-user",
                        email,
                        ConsentVersion,
                        consentText,
                        customerIp,
                        http.Request.Headers.UserAgent.ToString(),
                        new Uri(baseUri, "/app/abonelik?odeme=basarili"),
                        new Uri(baseUri, "/app/abonelik?odeme=basarisiz"),
                        new Uri(baseUri, paymentOptions.UsesPaytrProvider
                            ? "/api/odeme/paytr/bildirim" : "/api/odeme/webhook"),
                        quote.TotalAmount,
                        quote.ProrationCreditNetAmount,
                        quote.ChangeType,
                        request.OdemeAdSoyad?.Trim(),
                        request.OdemeAdres?.Trim(),
                        request.OdemeTelefon?.Trim());

                    if (quote.ChangeType == SubscriptionChangeTypes.ScheduledDowngrade)
                    {
                        var scheduled = await lifecycle.SchedulePlanChangeAsync(command, ct);
                        return Results.Ok(new
                        {
                            odemeIslemiId = (int?)null,
                            checkoutUrl = (string?)null,
                            expiresAt = (DateTime?)null,
                            firstChargeAt = (DateTime?)null,
                            reused = false,
                            scheduled = true,
                            effectiveAt = scheduled.EffectiveAt,
                            onayMetniSurumu = ConsentVersion,
                            onayMetni = consentText,
                            fiyat = scheduled.Quote
                        });
                    }

                    var result = await lifecycle.BeginCheckoutAsync(command, ct);

                    return Results.Ok(new
                    {
                        odemeIslemiId = result.PaymentTransactionId,
                        checkoutUrl = result.Session.CheckoutUrl.AbsoluteUri,
                        expiresAt = result.Session.ExpiresAt,
                        firstChargeAt = result.Session.FirstChargeAt,
                        reused = result.Reused,
                        scheduled = false,
                        effectiveAt = result.Quote.EffectiveAt,
                        onayMetniSurumu = ConsentVersion,
                        onayMetni = consentText,
                        fiyat = result.Quote
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { mesaj = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { mesaj = ex.Message });
                }
            }).RequireRateLimiting("sensitive");

        var cancelChangeEndpoint = app.MapDelete(
            "/api/abonelik/plan-degisikligi",
            async (IIsletmeService isletmeService, ISubscriptionLifecycleService lifecycle, CancellationToken ct) =>
            {
                try
                {
                    var business = await isletmeService.GetActiveAsync();
                    await lifecycle.CancelScheduledPlanChangeAsync(business.Id, ct);
                    return Results.Ok(new { mesaj = "Planlanan değişiklik iptal edildi." });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { mesaj = ex.Message });
                }
            }).RequireRateLimiting("sensitive");

        var cancelEndpoint = app.MapPost(
            "/api/abonelik/iptal",
            async (
                IIsletmeService isletmeService,
                ISubscriptionLifecycleService lifecycle,
                CancellationToken ct) =>
            {
                try
                {
                    var business = await isletmeService.GetActiveAsync();
                    await lifecycle.CancelAtPeriodEndAsync(business.Id, ct);
                    return Results.Ok(new
                    {
                        mesaj = "Iptal talebi alindi. Erisim mevcut donem sonuna kadar devam edecek.",
                        donemSonundaIptal = true
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { mesaj = ex.Message });
                }
            }).RequireRateLimiting("sensitive");

        app.MapPost(
                "/api/odeme/webhook",
                async (
                    PaymentWebhookEnvelope envelope,
                    ISubscriptionLifecycleService lifecycle,
                    CancellationToken ct) =>
                {
                    var result = await lifecycle.ProcessWebhookAsync(envelope, ct);
                    return result.Accepted ? Results.Ok(result) : Results.BadRequest(result);
                })
            .AllowAnonymous()
            .RequireRateLimiting("sensitive");

        app.MapPost(
                "/api/odeme/paytr/bildirim",
                async (HttpContext http, IPaymentProvider provider,
                    ISubscriptionLifecycleService lifecycle, CancellationToken ct) =>
                {
                    if (provider is not PaytrSubscriptionProvider)
                        return Results.NotFound();
                    if (http.Request.ContentType?.StartsWith("application/x-www-form-urlencoded",
                            StringComparison.OrdinalIgnoreCase) != true)
                        return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

                    var body = await ReadBoundedPaytrBodyAsync(http.Request.Body, ct);
                    if (body is null)
                        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
                    if (!TryParsePaytrCallbackForm(body, out var envelope))
                        return Results.BadRequest();
                    var processed = await lifecycle.ProcessWebhookAsync(
                        envelope, ct);
                    return processed.Accepted
                        ? Results.Text("OK", "text/plain; charset=utf-8")
                        : Results.BadRequest();
                })
            .AllowAnonymous()
            .RequireRateLimiting("sensitive");

        app.MapGet("/api/odeme/paytr/form", async (string token, HttpContext http,
                IPaymentProvider provider, PaymentRuntimeOptions options,
                IDbContextFactory<CashTrackerDbContext> dbFactory, CancellationToken ct) =>
            {
                if (provider is not PaytrSubscriptionProvider || token.Length is < 1 or > 512 ||
                    token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
                    return Results.NotFound();
                var checkoutUrl = new Uri(new Uri(options.PublicBaseUrl),
                    "/api/odeme/paytr/form?token=" + Uri.EscapeDataString(token)).AbsoluteUri;
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (!await db.OdemeIslemleri.AsNoTracking().AnyAsync(x =>
                        x.OdemeSaglayici == "PayTR" && x.CheckoutUrl == checkoutUrl &&
                        x.CheckoutExpiresAt > DateTime.UtcNow, ct))
                    return Results.NotFound();
                http.Response.Headers.CacheControl = "no-store";
                http.Response.Headers["Referrer-Policy"] = "no-referrer";
                http.Response.Headers["Content-Security-Policy"] =
                    "default-src 'none'; style-src 'unsafe-inline'; frame-src https://www.paytr.com; base-uri 'none'; frame-ancestors 'none'";
                var iframeUrl = "https://www.paytr.com/odeme/guvenli/" + token;
                var html = "<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\">" +
                    "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                    "<title>Güvenli ödeme · Systemcel</title><style>body{margin:0;background:#f5f7fb;color:#142238;font:16px system-ui}" +
                    "main{max-width:860px;margin:auto;padding:24px 16px}h1{font-size:24px}p{line-height:1.5}" +
                    "iframe{display:block;width:100%;min-height:760px;border:0;background:white;border-radius:12px}" +
                    "a{color:#164c9b}</style></head><body><main><h1>Güvenli ödeme</h1>" +
                    "<p>Ödeme sonucunuz, sağlayıcı bildirimi doğrulandıktan sonra hesabınıza işlenir.</p>" +
                    "<iframe title=\"PayTR güvenli ödeme formu\" src=\"" + iframeUrl + "\" referrerpolicy=\"no-referrer\"></iframe>" +
                    "<p><a href=\"/app/abonelik\">Abonelik sayfasına dön</a></p></main></body></html>";
                return Results.Content(html, "text/html; charset=utf-8");
            })
            .AllowAnonymous();

        app.MapGet("/api/odeme/paytr/donus", (string? sonuc, HttpContext http,
                IPaymentProvider provider) =>
            {
                if (provider is not PaytrSubscriptionProvider)
                    return Results.NotFound();
                var failed = string.Equals(sonuc, "basarisiz", StringComparison.Ordinal);
                http.Response.Headers.CacheControl = "no-store";
                http.Response.Headers["Content-Security-Policy"] =
                    "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; frame-ancestors 'self'";
                var html = "<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\">" +
                    "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                    "<title>Ödeme durumu · Systemcel</title><style>body{font:16px system-ui;background:#f5f7fb;" +
                    "color:#142238;margin:0}main{max-width:560px;margin:10vh auto;padding:28px;background:white;" +
                    "border-radius:16px}a{display:inline-block;padding:12px 18px;background:#164c9b;color:white;" +
                    "border-radius:8px;text-decoration:none}</style></head><body><main><h1>" +
                    (failed ? "Ödeme tamamlanamadı" : "Ödeme sonucu doğrulanıyor") +
                    "</h1><p>Kesin sonuç PayTR'nin sunucu bildirimi doğrulandıktan sonra hesabınıza işlenir.</p>" +
                    "<a target=\"_top\" href=\"/app/abonelik?odeme=" +
                    (failed ? "basarisiz" : "basarili") +
                    "\">Abonelik durumunu görüntüle</a></main></body></html>";
                return Results.Content(html, "text/html; charset=utf-8");
            })
            .AllowAnonymous();

        MapFakeCheckout(app);

        var clerkOptions = app.Services.GetRequiredService<ClerkAuthenticationOptions>();
        if (clerkOptions.Enabled)
        {
            quoteEndpoint.RequireAuthorization();
            summaryEndpoint.RequireAuthorization();
            checkoutEndpoint.RequireAuthorization();
            cancelChangeEndpoint.RequireAuthorization();
            cancelEndpoint.RequireAuthorization();
        }
    }

    private static void MapFakeCheckout(WebApplication app)
    {
        app.MapGet(
                "/api/odeme/test/checkout/{sessionId}",
                async (
                    string sessionId,
                    string merchantReference,
                    IWebHostEnvironment environment,
                    IPaymentProvider provider,
                    IDbContextFactory<CashTrackerDbContext> dbFactory,
                    CancellationToken ct) =>
                {
                    if (environment.IsProduction() || provider is not FakePaymentProvider)
                        return Results.NotFound();

                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var payment = await db.OdemeIslemleri.AsNoTracking().SingleOrDefaultAsync(x =>
                        x.SaglayiciOturumId == sessionId && x.CheckoutAnahtari == merchantReference, ct);
                    if (payment is null)
                        return Results.NotFound();

                    var action = $"/api/odeme/test/checkout/{Uri.EscapeDataString(sessionId)}/complete" +
                                 $"?merchantReference={Uri.EscapeDataString(merchantReference)}";
                    var html = BuildFakeCheckoutHtml(
                        payment.PlanKodu,
                        payment.ToplamTutar,
                        payment.ParaBirimi,
                        action,
                        string.Equals(payment.IslemTipi, "DenemeKartYetkilendirme", StringComparison.Ordinal));
                    return Results.Content(html, "text/html; charset=utf-8");
                })
            .AllowAnonymous();

        app.MapPost(
                "/api/odeme/test/checkout/{sessionId}/complete",
                async (
                    string sessionId,
                    string merchantReference,
                    string? result,
                    IWebHostEnvironment environment,
                    IPaymentProvider provider,
                    ISubscriptionLifecycleService lifecycle,
                    IDbContextFactory<CashTrackerDbContext> dbFactory,
                    CancellationToken ct) =>
                {
                    if (environment.IsProduction() || provider is not FakePaymentProvider fakeProvider)
                        return Results.NotFound();

                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var payment = await db.OdemeIslemleri.AsNoTracking().SingleOrDefaultAsync(x =>
                        x.SaglayiciOturumId == sessionId && x.CheckoutAnahtari == merchantReference, ct);
                    if (payment is null)
                        return Results.NotFound();

                    var succeeded = !string.Equals(result, "fail", StringComparison.OrdinalIgnoreCase);
                    var startsPaidSubscription =
                        string.Equals(payment.IslemTipi, PaymentTransactionTypes.SubscriptionStart, StringComparison.Ordinal) ||
                        string.Equals(payment.IslemTipi, PaymentTransactionTypes.PlanUpgrade, StringComparison.Ordinal);
                    var isAccountantService = string.Equals(
                        payment.IslemTipi,
                        PaymentTransactionTypes.AccountantService,
                        StringComparison.Ordinal);
                    var isPaidCharge = startsPaidSubscription || isAccountantService;
                    var eventType = succeeded
                        ? isPaidCharge ? PaymentEventTypes.PaymentSucceeded : PaymentEventTypes.TrialAuthorized
                        : PaymentEventTypes.PaymentFailed;
                    var payload = JsonSerializer.Serialize(new
                    {
                        eventId = $"fake-{eventType}-{sessionId}",
                        eventType,
                        merchantReference,
                        providerTransactionId = $"fake-tx-{sessionId}",
                        amount = succeeded && isPaidCharge ? payment.ToplamTutar : 0m,
                        currency = payment.ParaBirimi,
                        occurredAt = DateTime.UtcNow
                    });
                    var processed = await lifecycle.ProcessWebhookAsync(
                        new PaymentWebhookEnvelope(payload, fakeProvider.SignPayload(payload)), ct);
                    var redirectBase = isAccountantService ? "/app/muhasebeciler" : "/app/abonelik";
                    var redirect = processed.Accepted
                        ? $"{redirectBase}?odeme=test-basarili"
                        : $"{redirectBase}?odeme=test-basarisiz";
                    return Results.Redirect(redirect);
                })
            .AllowAnonymous();
    }

    private static Uri ResolveBaseUri(HttpRequest request, PaymentRuntimeOptions options)
    {
        if (Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var configured))
            return configured;

        var forwardedProto = request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var scheme = string.IsNullOrWhiteSpace(forwardedProto) ? request.Scheme : forwardedProto.Split(',')[0].Trim();
        return new Uri($"{scheme}://{request.Host}");
    }

    internal static string BuildConsentText(PaymentQuote quote)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var period = string.Equals(quote.BillingPeriod, PaymentBillingPeriods.Annual, StringComparison.OrdinalIgnoreCase)
            ? "yıllık"
            : "aylık";
        var net = quote.NetAmount.ToString("N2", culture);
        var vat = quote.VatAmount.ToString("N2", culture);
        var total = quote.TotalAmount.ToString("N2", culture);
        var cancellation = quote.BillingPeriod == PaymentBillingPeriods.Annual
            ? " Yıllık abonelik iptalinde abonelik başlangıcına göre mevcut aylık dönem tamamlanır; kalan her tam ay için gerçekten ödenen yıllık tutarın 1/12'si üzerinden iade talebi oluşturulur."
            : " Aylık abonelik iptali ödenmiş aylık dönemin sonunda etkili olur ve olağan iptal geçmiş tahsilatı kendiliğinden iade etmez.";
        var credits = quote.ExtraCustomerCredits > 0
            ? $" Buna Standart plana dahil 10 müşteriye ek olarak yinelenen {quote.ExtraCustomerCredits} adet +1 müşteri kredisi dahildir."
            : string.Empty;

        if (quote.ChangeType == SubscriptionChangeTypes.ScheduledDowngrade && quote.EffectiveAt is { } effectiveAt)
        {
            return $"Plan değişikliğinin {effectiveAt:dd.MM.yyyy} tarihinde mevcut dönem sona erdiğinde uygulanmasını onaylıyorum. " +
                   "Bugün tahsilat yapılmayacağını ve mevcut plan haklarımın bu tarihe kadar korunacağını kabul ediyorum.";
        }

        if (quote.ChangeType == SubscriptionChangeTypes.ImmediateUpgrade)
        {
            var fullPeriod = quote.FullPeriodNetAmount.ToString("N2", culture);
            var credit = quote.ProrationCreditNetAmount.ToString("N2", culture);
            return $"Plan yükseltmesinin hemen uygulanmasını; yeni dönem bedeli {fullPeriod} TL'den kullanılmayan dönem kredisi {credit} TL düşüldükten sonra " +
                   $"{net} TL + {vat} TL KDV, toplam {total} TL tahsil edilmesini onaylıyorum.{credits}{cancellation}";
        }

        if (quote.TrialDays <= 0)
        {
            var renewalNet = quote.RenewalNetAmount.ToString("N2", culture);
            var renewalVatAmount = decimal.Round(quote.RenewalNetAmount * quote.VatRate / 100m, 2, MidpointRounding.AwayFromZero);
            var renewalVat = renewalVatAmount.ToString("N2", culture);
            var renewalTotal = (quote.RenewalNetAmount + renewalVatAmount).ToString("N2", culture);
            var campaign = quote.IsFounderPrice
                ? string.Equals(quote.BillingPeriod, PaymentBillingPeriods.Annual, StringComparison.OrdinalIgnoreCase)
                    ? $" İlk 50 kurucu kampanyası fiyatının bugün peşin ödenen 12 aylık dönemin tamamına uygulandığını kabul ediyorum. Sonraki yıllık yenilemede yenileme tarihinde geçerli liste fiyatı uygulanır. Bugünkü yıllık liste fiyatı {renewalNet} TL + {renewalVat} TL KDV, toplam {renewalTotal} TL'dir. Fiyat değişikliği en az 30 gün önce e-posta ve uygulama içinden bildirilir; yenilemeden önce dönem sonu iptal talebi verebilirim."
                    : $" İlk 50 kurucu kampanyası fiyatının ilk {quote.DiscountedPeriodCount} aylık dönem için geçerli olduğunu kabul ediyorum. Kampanya bittikten sonraki aylık yenilemelerde yenileme tarihinde geçerli liste fiyatı uygulanır. Bugünkü aylık liste fiyatı {renewalNet} TL + {renewalVat} TL KDV, toplam {renewalTotal} TL'dir. Fiyat değişikliği en az 30 gün önce e-posta ve uygulama içinden bildirilir; yenilemeden önce dönem sonu iptal talebi verebilirim."
                : $" Aboneliğin sonraki {period} dönemde {renewalNet} TL + {renewalVat} TL KDV, toplam {renewalTotal} TL üzerinden yenileneceğini kabul ediyorum.";
            return $"{period} planın hemen başlamasını; " +
                   $"{net} TL + {vat} TL KDV, toplam {total} TL'nin kayıtlı ödeme yöntemimden bugün tahsil edilmesini onaylıyorum.{credits}{campaign}{cancellation} Emredici yasal haklarım saklıdır.";
        }

        return $"{quote.TrialDays} günlük deneme sonunda iptal etmediğim takdirde {period} plan için " +
               $"{net} TL + {vat} TL KDV, toplam {total} TL'nin kayıtlı ödeme yöntemimden tahsil edilmesini " +
               $"ve aboneliğin {period} olarak yenilenmesini onaylıyorum.{credits} Deneme bitmeden 7 ve 3 gün önce " +
               $"bilgilendirileceğimi kabul ediyorum.{cancellation} Emredici yasal haklarım saklıdır.";
    }

    private static string BuildFakeCheckoutHtml(
        string planCode,
        decimal total,
        string currency,
        string action,
        bool startsTrial)
    {
        var safePlan = WebUtility.HtmlEncode(planCode);
        var safeTotal = WebUtility.HtmlEncode($"{total:N2} {currency}");
        var safeAction = WebUtility.HtmlEncode(action);
        var safeActionLabel = startsTrial ? "Kartı doğrula ve denemeyi başlat" : "Ödemeyi onayla ve aboneliği başlat";
        return $$"""
            <!doctype html>
            <html lang="tr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>Systemcel test checkout</title>
              <style>
                *{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;background:#f4f1e7;color:#090a08;font:16px system-ui,sans-serif}.card{width:min(92vw,520px);padding:32px;border:1px solid #cfcdbf;border-radius:24px;background:#fffefa;box-shadow:0 24px 70px #171a0d20}.tag{font:700 12px ui-monospace,monospace;letter-spacing:.12em;color:#617700}.price{font-size:36px;font-weight:800;margin:12px 0 28px}.actions{display:grid;gap:12px}button{min-height:54px;border:1px solid #161712;border-radius:999px;font-weight:800;font-size:16px;cursor:pointer}.ok{background:#baff00}.fail{background:transparent;color:#8c2f28}
              </style>
            </head>
            <body><main class="card"><div class="tag">SAHTE SAĞLAYICI · YALNIZCA GELİŞTİRME</div><h1>{{safePlan}}</h1><div class="price">{{safeTotal}}</div><div class="actions"><form method="post" action="{{safeAction}}"><button class="ok" type="submit">{{safeActionLabel}}</button></form><form method="post" action="{{safeAction}}&amp;result=fail"><button class="fail" type="submit">Başarısız ödemeyi simüle et</button></form></div></main></body>
            </html>
            """;
    }

    private static bool ValidPaytrContact(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength &&
        !value.Any(char.IsControl);

    private static string ResolveCustomerIp(HttpContext http, PaymentRuntimeOptions options)
    {
        var remote = http.Connection.RemoteIpAddress;
        if (remote is null) return string.Empty;
        var trusted = IPAddress.IsLoopback(remote) || options.PaytrTrustedProxyIps.Any(value =>
            IPAddress.TryParse(value, out var configured) && configured.Equals(remote));
        if (options.UsesPaytrProvider && trusted &&
            http.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var lastHop = forwarded.ToString().Split(',', StringSplitOptions.TrimEntries)[^1];
            if (IPAddress.TryParse(lastHop, out var clientIp))
                return clientIp.ToString();
        }
        return remote.ToString();
    }

    internal static async Task<string?> ReadBoundedPaytrBodyAsync(Stream stream, CancellationToken ct)
    {
        const int maxBytes = 4096;
        var buffer = new byte[maxBytes + 1];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), ct);
            if (read == 0) break;
            length += read;
        }
        return length > maxBytes ? null : Encoding.UTF8.GetString(buffer, 0, length);
    }

    internal static bool TryParsePaytrCallbackForm(string body, out PaymentWebhookEnvelope envelope)
    {
        envelope = new PaymentWebhookEnvelope(string.Empty, string.Empty);
        if (body.Length > 4096) return false;
        var fields = QueryHelpers.ParseQuery(body);
        if (!TryGetSinglePaytrField(fields, "merchant_oid", out var orderId) ||
            !TryGetSinglePaytrField(fields, "status", out var status) ||
            !TryGetSinglePaytrField(fields, "total_amount", out var amount) ||
            !TryGetSinglePaytrField(fields, "hash", out var hash))
            return false;

        envelope = new PaymentWebhookEnvelope(JsonSerializer.Serialize(new
        {
            MerchantOrderId = orderId,
            Status = status,
            TotalAmountKurus = amount
        }), hash);
        return true;
    }

    private static bool TryGetSinglePaytrField(
        Dictionary<string, StringValues> fields, string name, out string value)
    {
        value = string.Empty;
        if (!fields.TryGetValue(name, out var values) || values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0]) || values[0]!.Length > 512)
            return false;
        value = values[0]!;
        return true;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private static CurrentSubscriptionPricingContext? ToPricingContext(CashTracker.Core.Entities.Abonelik? subscription)
    {
        if (subscription?.DonemBitisAt is not { } periodEnd)
            return null;
        return new CurrentSubscriptionPricingContext(
            subscription.PlanKodu,
            subscription.FaturalamaDonemi,
            subscription.EkMusteriKredisi,
            subscription.DonemTutari,
            subscription.DonemBaslangicAt,
            periodEnd);
    }

    private static string NormalizeBillingPeriod(string billingPeriod)
    {
        if (string.Equals(billingPeriod, PaymentBillingPeriods.Monthly, StringComparison.OrdinalIgnoreCase))
            return PaymentBillingPeriods.Monthly;
        if (string.Equals(billingPeriod, PaymentBillingPeriods.Annual, StringComparison.OrdinalIgnoreCase))
            return PaymentBillingPeriods.Annual;
        throw new InvalidOperationException("Faturalama dönemi aylık veya yıllık olmalıdır.");
    }

    private static async Task<bool> CanOfferFounderPriceAsync(
        CashTrackerDbContext db,
        int businessId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var ownRight = await db.KurucuKampanyaHaklari.AsNoTracking().SingleOrDefaultAsync(x =>
            x.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu && x.IsletmeId == businessId, ct);
        if (ownRight is not null)
            return ownRight.Durum == "Rezerve" && ownRight.RezervasyonBitisAt > now;

        if (await db.Abonelikler.AsNoTracking().AnyAsync(x => x.IsletmeId == businessId, ct) ||
            await db.IsletmeDenemeleri.AsNoTracking().AnyAsync(x => x.IsletmeId == businessId, ct) ||
            await db.OdemeIslemleri.AsNoTracking().AnyAsync(x =>
                x.IsletmeId == businessId &&
                (x.Durum == PaymentTransactionStates.Succeeded || x.Durum == PaymentTransactionStates.TrialAuthorized), ct))
            return false;

        var used = await db.KurucuKampanyaHaklari.AsNoTracking().CountAsync(x =>
            x.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu &&
            (x.Durum == "Kazanildi" || (x.Durum == "Rezerve" && x.RezervasyonBitisAt > now)), ct);
        return used < SubscriptionPlanCatalog.KurucuKampanyaKontenjani;
    }

    internal sealed record CheckoutRequest(
        string PlanKodu,
        string FaturalamaDonemi,
        int EkMusteriKredisi,
        string? KampanyaKodu,
        bool Onaylandi,
        string? Eposta,
        string? IdempotencyKey,
        string? OdemeAdSoyad = null,
        string? OdemeAdres = null,
        string? OdemeTelefon = null);
}
