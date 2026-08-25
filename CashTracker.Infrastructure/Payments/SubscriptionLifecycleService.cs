using System.Security.Cryptography;
using System.Text;
using System.Data;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed class SubscriptionLifecycleService : ISubscriptionLifecycleService
{
    private static readonly HashSet<string> SupportedEvents = new(StringComparer.Ordinal)
    {
        PaymentEventTypes.TrialAuthorized,
        PaymentEventTypes.PaymentSucceeded,
        PaymentEventTypes.PaymentFailed,
        PaymentEventTypes.PaymentRefunded,
        PaymentEventTypes.SubscriptionCancelled
    };

    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IPaymentProvider _provider;
    private readonly IPaymentPricingService _pricing;
    private readonly ISubscriptionReminderSender? _reminderSender;
    private readonly MuhasebeciOdemeOptions _accountantPaymentOptions;

    public SubscriptionLifecycleService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IPaymentProvider provider,
        IPaymentPricingService pricing,
        ISubscriptionReminderSender? reminderSender = null,
        MuhasebeciOdemeOptions? accountantPaymentOptions = null)
    {
        _dbFactory = dbFactory;
        _provider = provider;
        _pricing = pricing;
        _reminderSender = reminderSender;
        _accountantPaymentOptions = accountantPaymentOptions ?? new MuhasebeciOdemeOptions();
    }

    public async Task<SubscriptionCheckoutResult> BeginCheckoutAsync(
        SubscriptionCheckoutCommand command,
        CancellationToken ct = default)
    {
        ValidateCheckoutCommand(command);
        var checkoutKey = command.IdempotencyKey.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var business = await db.Isletmeler.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == command.BusinessId, ct)
            ?? throw new InvalidOperationException("Isletme bulunamadi.");
        var existing = await db.OdemeIslemleri
            .SingleOrDefaultAsync(x => x.IsletmeId == command.BusinessId && x.CheckoutAnahtari == checkoutKey, ct);
        var activeSubscription = await db.Abonelikler.AsNoTracking()
            .Where(x => x.IsletmeId == command.BusinessId && x.HesapTipi == command.AccountType && x.Durum == "Aktif")
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefaultAsync(ct);
        var useFounderPrice = existing is null && activeSubscription is null &&
            await ReserveFounderSlotAsync(command, checkoutKey, ct);
        var quote = existing is not null
            ? BuildStoredQuote(existing)
            : _pricing.CreateChangeQuote(
                command.PlanCode,
                command.AccountType,
                command.BillingPeriod,
                command.ExtraCustomerCredits,
                ToPricingContext(activeSubscription),
                DateTime.UtcNow,
                useFounderPrice);
        if (!string.Equals(business.TenantTipi, quote.AccountType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Plan, aktif calisma alaninin hesap tipiyle uyumlu degil.");
        EnsureExpectedQuoteMatches(command, quote);
        if (quote.ChangeType == SubscriptionChangeTypes.ScheduledDowngrade)
            throw new InvalidOperationException("Bu degisiklik odeme gerektirmez; donem sonuna planlanmalidir.");

        if (existing is not null)
        {
            if (string.Equals(existing.IslemTipi, PaymentTransactionTypes.SubscriptionStart, StringComparison.Ordinal))
                quote = quote with { TrialDays = 0 };
            EnsureCheckoutSelectionMatches(existing, quote);
            if (TryBuildExistingResult(existing, quote, out var existingResult))
                return existingResult;
        }

        var trialAlreadyUsed = existing is null && await db.IsletmeDenemeleri.AsNoTracking().AnyAsync(
            x => x.IsletmeId == command.BusinessId && x.HesapTipi == quote.AccountType,
            ct);
        if (trialAlreadyUsed)
            quote = quote with { TrialDays = 0 };

        var now = DateTime.UtcNow;
        var payment = existing ?? new OdemeIslemi
        {
            IsletmeId = command.BusinessId,
            CheckoutAnahtari = checkoutKey,
            HesapTipi = quote.AccountType,
            PlanKodu = quote.PlanCode,
            FaturalamaDonemi = quote.BillingPeriod,
            EkMusteriKredisi = quote.ExtraCustomerCredits,
            KampanyaKodu = quote.CampaignCode,
            ListeNetTutar = quote.ListNetAmount,
            YenilemeNetTutar = quote.RenewalNetAmount,
            IndirimliDonemSayisi = quote.DiscountedPeriodCount,
            IslemTipi = quote.ChangeType == SubscriptionChangeTypes.ImmediateUpgrade
                ? PaymentTransactionTypes.PlanUpgrade
                : quote.TrialDays > 0 ? "DenemeKartYetkilendirme" : PaymentTransactionTypes.SubscriptionStart,
            Durum = PaymentTransactionStates.Preparing,
            OdemeSaglayici = _provider.Name,
            NetTutar = quote.NetAmount,
            KdvOrani = quote.VatRate,
            KdvTutar = quote.VatAmount,
            ToplamTutar = quote.TotalAmount,
            TamDonemNetTutar = quote.FullPeriodNetAmount,
            KistKrediNetTutar = quote.ProrationCreditNetAmount,
            DegisiklikTipi = quote.ChangeType,
            HedefDonemBitisAt = quote.TargetPeriodEndAt,
            ParaBirimi = quote.Currency,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (existing is null)
        {
            db.OdemeIslemleri.Add(payment);
            db.AbonelikOnaylari.Add(new AbonelikOnayi
            {
                IsletmeId = command.BusinessId,
                KullaniciRef = command.UserReference.Trim(),
                CheckoutAnahtari = checkoutKey,
                HesapTipi = quote.AccountType,
                PlanKodu = quote.PlanCode,
                FaturalamaDonemi = quote.BillingPeriod,
                EkMusteriKredisi = quote.ExtraCustomerCredits,
                KampanyaKodu = quote.CampaignCode,
                ListeNetTutar = quote.ListNetAmount,
                YenilemeNetTutar = quote.RenewalNetAmount,
                MetinSurumu = command.ConsentTextVersion.Trim(),
                MetinHash = Sha256(command.ConsentText),
                IstemciIpHash = Sha256(command.ClientIp),
                UserAgentHash = Sha256(command.UserAgent),
                NetTutar = quote.NetAmount,
                TamDonemNetTutar = quote.FullPeriodNetAmount,
                KistKrediNetTutar = quote.ProrationCreditNetAmount,
                DegisiklikTipi = quote.ChangeType,
                KdvOrani = quote.VatRate,
                KdvTutar = quote.VatAmount,
                ToplamTutar = quote.TotalAmount,
                ParaBirimi = quote.Currency,
                OnayAt = now
            });

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                var raced = await db.OdemeIslemleri.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.IsletmeId == command.BusinessId && x.CheckoutAnahtari == checkoutKey, ct);
                if (raced is not null)
                {
                    EnsureCheckoutSelectionMatches(raced, quote);
                    if (TryBuildExistingResult(raced, quote, out var racedResult))
                        return racedResult;
                }
                throw;
            }
        }

        try
        {
            var session = await _provider.CreateCheckoutAsync(new PaymentCheckoutRequest(
                checkoutKey,
                quote,
                $"business-{command.BusinessId}",
                command.CustomerEmail.Trim(),
                command.SuccessUrl,
                command.FailureUrl,
                command.CallbackUrl,
                new Dictionary<string, string>
                {
                    ["businessId"] = command.BusinessId.ToString(),
                    ["accountType"] = quote.AccountType,
                    ["planCode"] = quote.PlanCode,
                    ["billingPeriod"] = quote.BillingPeriod,
                    ["extraCustomerCredits"] = quote.ExtraCustomerCredits.ToString(),
                    ["campaignCode"] = quote.CampaignCode,
                    ["renewalNetAmount"] = quote.RenewalNetAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["changeType"] = quote.ChangeType,
                    ["prorationCreditNetAmount"] = quote.ProrationCreditNetAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }), ct);

            payment.OdemeSaglayici = session.Provider;
            payment.SaglayiciOturumId = session.ProviderSessionId;
            payment.CheckoutUrl = session.CheckoutUrl.AbsoluteUri;
            payment.CheckoutExpiresAt = session.ExpiresAt;
            payment.Durum = PaymentTransactionStates.CheckoutOpen;
            payment.HataKodu = string.Empty;
            payment.HataMesaji = string.Empty;
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return new SubscriptionCheckoutResult(payment.Id, quote, session, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            payment.Durum = PaymentTransactionStates.Failed;
            payment.HataKodu = "checkout_create_failed";
            payment.HataMesaji = Limit(ex.Message, 500);
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PaymentWebhookProcessingResult> ProcessWebhookAsync(
        PaymentWebhookEnvelope envelope,
        CancellationToken ct = default)
    {
        var verification = _provider.VerifyWebhook(envelope);
        if (!verification.IsValid || verification.Event is null)
            return new PaymentWebhookProcessingResult(false, false, "Reddedildi", verification.Error);

        var paymentEvent = verification.Event;
        if (!SupportedEvents.Contains(paymentEvent.EventType))
            return new PaymentWebhookProcessingResult(false, false, "Reddedildi", "Desteklenmeyen odeme olayi.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var duplicate = await db.OdemeOlaylari.AsNoTracking().AnyAsync(x =>
            x.OdemeSaglayici == paymentEvent.Provider && x.OlayId == paymentEvent.EventId, ct);
        if (duplicate)
            return new PaymentWebhookProcessingResult(true, true, "Tekrar", "Olay daha once islendi.");

        var payment = await db.OdemeIslemleri.SingleOrDefaultAsync(
            x => x.CheckoutAnahtari == paymentEvent.MerchantReference, ct);
        if (payment is null)
            return new PaymentWebhookProcessingResult(false, false, "Reddedildi", "Checkout kaydi bulunamadi.");
        if (!string.Equals(payment.OdemeSaglayici, paymentEvent.Provider, StringComparison.OrdinalIgnoreCase))
            return new PaymentWebhookProcessingResult(false, false, "Reddedildi", "Odeme saglayicisi uyusmuyor.");

        var amountError = ValidateEventAmount(payment, paymentEvent);
        if (amountError is not null)
            return new PaymentWebhookProcessingResult(false, false, "Reddedildi", amountError);

        var eventRecord = new OdemeOlayi
        {
            OdemeSaglayici = paymentEvent.Provider,
            OlayId = paymentEvent.EventId,
            OlayTipi = paymentEvent.EventType,
            CheckoutAnahtari = paymentEvent.MerchantReference,
            SaglayiciIslemId = paymentEvent.ProviderTransactionId,
            IslenmeDurumu = "Isleniyor",
            PayloadHash = paymentEvent.PayloadHash,
            SaglayiciAt = EnsureUtc(paymentEvent.OccurredAt),
            AlindiAt = DateTime.UtcNow
        };
        db.OdemeOlaylari.Add(eventRecord);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var applied = ApplyEvent(db, payment, paymentEvent);
            eventRecord.IslenmeDurumu = applied ? "Islendi" : "Yoksayildi";
            eventRecord.IslendiAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new PaymentWebhookProcessingResult(
                true,
                false,
                payment.Durum,
                applied ? "Odeme olayi islendi." : "Sirasi gecmis veya daha once uygulanmis olay yoksayildi.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            if (await db.OdemeOlaylari.AsNoTracking().AnyAsync(x =>
                    x.OdemeSaglayici == paymentEvent.Provider && x.OlayId == paymentEvent.EventId, ct))
                return new PaymentWebhookProcessingResult(true, true, "Tekrar", "Olay eszamanli olarak islendi.");
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task CancelAtPeriodEndAsync(int businessId, CancellationToken ct = default)
    {
        if (businessId <= 0)
            throw new ArgumentOutOfRangeException(nameof(businessId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var subscription = await db.Abonelikler
            .Where(x => x.IsletmeId == businessId && x.Durum == "Aktif")
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefaultAsync(ct);
        var trial = await db.IsletmeDenemeleri
            .Where(x => x.IsletmeId == businessId && x.Durum == "Aktif")
            .OrderByDescending(x => x.BaslangicAt)
            .FirstOrDefaultAsync(ct);

        if (subscription is null && trial is null)
            throw new InvalidOperationException("Iptal edilebilecek aktif deneme veya abonelik bulunamadi.");

        if (subscription is not null)
        {
            subscription.DonemSonundaIptal = true;
            subscription.PlanlananPlanKodu = string.Empty;
            subscription.PlanlananFaturalamaDonemi = string.Empty;
            subscription.PlanlananEkMusteriKredisi = null;
            subscription.PlanlananDegisiklikAt = null;
            subscription.IptalAt = now;
            subscription.UpdatedAt = now;
        }

        if (trial is not null)
        {
            trial.DonemSonundaIptal = true;
            trial.IptalAt = now;
            trial.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<SubscriptionPlanChangeResult> SchedulePlanChangeAsync(
        SubscriptionCheckoutCommand command,
        CancellationToken ct = default)
    {
        ValidateCheckoutCommand(command);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var business = await db.Isletmeler.AsNoTracking().SingleOrDefaultAsync(x => x.Id == command.BusinessId, ct)
            ?? throw new InvalidOperationException("Isletme bulunamadi.");
        if (!string.Equals(business.TenantTipi, command.AccountType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Plan, aktif calisma alaninin hesap tipiyle uyumlu degil.");

        var subscription = await db.Abonelikler
            .Where(x => x.IsletmeId == command.BusinessId && x.HesapTipi == command.AccountType && x.Durum == "Aktif")
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Degisiklik planlanabilecek aktif abonelik bulunamadi.");
        var quote = _pricing.CreateChangeQuote(
            command.PlanCode,
            command.AccountType,
            command.BillingPeriod,
            command.ExtraCustomerCredits,
            ToPricingContext(subscription),
            DateTime.UtcNow);
        if (subscription.DonemSonundaIptal)
            throw new InvalidOperationException("Once donem sonu iptal talebini kaldirin.");
        EnsureExpectedQuoteMatches(command, quote);
        if (quote.ChangeType != SubscriptionChangeTypes.ScheduledDowngrade || quote.EffectiveAt is null)
            throw new InvalidOperationException("Bu plan degisikligi aninda tahsilat gerektirir.");

        var checkoutKey = command.IdempotencyKey.Trim();
        var existing = await db.OdemeIslemleri.SingleOrDefaultAsync(x =>
            x.IsletmeId == command.BusinessId && x.CheckoutAnahtari == checkoutKey, ct);
        if (existing is not null)
        {
            if (existing.IslemTipi != PaymentTransactionTypes.ScheduledPlanChange ||
                !string.Equals(existing.PlanKodu, quote.PlanCode, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.FaturalamaDonemi, quote.BillingPeriod, StringComparison.OrdinalIgnoreCase) ||
                existing.EkMusteriKredisi != quote.ExtraCustomerCredits)
                throw new InvalidOperationException("Ayni checkout anahtari farkli bir plan degisikligiyle kullanilamaz.");
            if (!string.Equals(subscription.PlanlananPlanKodu, quote.PlanCode, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(subscription.PlanlananFaturalamaDonemi, quote.BillingPeriod, StringComparison.OrdinalIgnoreCase) ||
                subscription.PlanlananEkMusteriKredisi != quote.ExtraCustomerCredits ||
                subscription.PlanlananDegisiklikAt != quote.EffectiveAt)
                throw new InvalidOperationException("Bu checkout anahtarina ait planlama artik etkin degil. Yeni bir anahtar kullanin.");
            return new SubscriptionPlanChangeResult(quote, true, quote.EffectiveAt.Value);
        }

        subscription.PlanlananPlanKodu = quote.PlanCode;
        subscription.PlanlananFaturalamaDonemi = quote.BillingPeriod;
        subscription.PlanlananEkMusteriKredisi = quote.ExtraCustomerCredits;
        subscription.PlanlananDegisiklikAt = quote.EffectiveAt;
        subscription.UpdatedAt = DateTime.UtcNow;
        db.OdemeIslemleri.Add(new OdemeIslemi
        {
            IsletmeId = command.BusinessId,
            CheckoutAnahtari = checkoutKey,
            HesapTipi = command.AccountType,
            PlanKodu = quote.PlanCode,
            FaturalamaDonemi = quote.BillingPeriod,
            EkMusteriKredisi = quote.ExtraCustomerCredits,
            IslemTipi = PaymentTransactionTypes.ScheduledPlanChange,
            Durum = PaymentTransactionStates.Succeeded,
            OdemeSaglayici = "Systemcel",
            NetTutar = 0m,
            KdvOrani = quote.VatRate,
            KdvTutar = 0m,
            ToplamTutar = 0m,
            TamDonemNetTutar = quote.FullPeriodNetAmount,
            DegisiklikTipi = quote.ChangeType,
            HedefDonemBitisAt = quote.EffectiveAt,
            ParaBirimi = quote.Currency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TamamlandiAt = DateTime.UtcNow
        });
        db.AbonelikOnaylari.Add(new AbonelikOnayi
        {
            IsletmeId = command.BusinessId,
            KullaniciRef = command.UserReference.Trim(),
            CheckoutAnahtari = checkoutKey,
            HesapTipi = quote.AccountType,
            PlanKodu = quote.PlanCode,
            FaturalamaDonemi = quote.BillingPeriod,
            EkMusteriKredisi = quote.ExtraCustomerCredits,
            KampanyaKodu = string.Empty,
            ListeNetTutar = quote.ListNetAmount,
            YenilemeNetTutar = quote.RenewalNetAmount,
            MetinSurumu = command.ConsentTextVersion.Trim(),
            MetinHash = Sha256(command.ConsentText),
            IstemciIpHash = Sha256(command.ClientIp),
            UserAgentHash = Sha256(command.UserAgent),
            NetTutar = 0m,
            TamDonemNetTutar = quote.FullPeriodNetAmount,
            KistKrediNetTutar = 0m,
            DegisiklikTipi = quote.ChangeType,
            KdvOrani = quote.VatRate,
            KdvTutar = 0m,
            ToplamTutar = 0m,
            ParaBirimi = quote.Currency,
            OnayAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return new SubscriptionPlanChangeResult(quote, true, quote.EffectiveAt.Value);
    }

    public async Task CancelScheduledPlanChangeAsync(int businessId, CancellationToken ct = default)
    {
        if (businessId <= 0) throw new ArgumentOutOfRangeException(nameof(businessId));
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var subscription = await db.Abonelikler
            .Where(x => x.IsletmeId == businessId && x.Durum == "Aktif" && x.PlanlananDegisiklikAt != null)
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Iptal edilecek planlanmis degisiklik bulunamadi.");
        subscription.PlanlananPlanKodu = string.Empty;
        subscription.PlanlananFaturalamaDonemi = string.Empty;
        subscription.PlanlananEkMusteriKredisi = null;
        subscription.PlanlananDegisiklikAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;
        var schedulePayment = await db.OdemeIslemleri
            .Where(x => x.IsletmeId == businessId &&
                        x.IslemTipi == PaymentTransactionTypes.ScheduledPlanChange &&
                        x.Durum == PaymentTransactionStates.Succeeded)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (schedulePayment is not null)
        {
            schedulePayment.Durum = PaymentTransactionStates.Cancelled;
            schedulePayment.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> ReserveFounderSlotAsync(
        SubscriptionCheckoutCommand command,
        string checkoutKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.CampaignCode))
            return false;
        if (!string.Equals(command.CampaignCode, SubscriptionPlanCatalog.KurucuKampanyaKodu, StringComparison.Ordinal))
            throw new InvalidOperationException("Gecersiz kampanya kodu.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var expired = await db.KurucuKampanyaHaklari
            .Where(x => x.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu &&
                        x.Durum == "Rezerve" && x.RezervasyonBitisAt <= now)
            .ToListAsync(ct);
        if (expired.Count > 0)
        {
            db.KurucuKampanyaHaklari.RemoveRange(expired);
            await db.SaveChangesAsync(ct);
        }

        var existingRight = await db.KurucuKampanyaHaklari.SingleOrDefaultAsync(x =>
            x.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu &&
            x.IsletmeId == command.BusinessId, ct);
        if (existingRight is not null)
        {
            if (existingRight.Durum == "Rezerve" &&
                string.Equals(existingRight.CheckoutAnahtari, checkoutKey, StringComparison.Ordinal))
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return true;
            }

            throw new InvalidOperationException("Lansman fiyati hakki bu calisma alani icin daha once kullanildi.");
        }

        var hasPreviousSubscription = await db.Abonelikler.AsNoTracking()
            .AnyAsync(x => x.IsletmeId == command.BusinessId, ct);
        var hasPreviousTrial = await db.IsletmeDenemeleri.AsNoTracking()
            .AnyAsync(x => x.IsletmeId == command.BusinessId, ct);
        var hasCompletedPayment = await db.OdemeIslemleri.AsNoTracking().AnyAsync(x =>
            x.IsletmeId == command.BusinessId &&
            (x.Durum == PaymentTransactionStates.Succeeded || x.Durum == PaymentTransactionStates.TrialAuthorized), ct);
        if (hasPreviousSubscription || hasPreviousTrial || hasCompletedPayment)
            throw new InvalidOperationException("Lansman fiyati yalnizca ilk aboneligini baslatan yeni hesaplar icindir.");

        var usedSlots = await db.KurucuKampanyaHaklari.AsNoTracking()
            .Where(x => x.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu)
            .Select(x => x.SiraNo)
            .ToListAsync(ct);
        var used = usedSlots.ToHashSet();
        var slot = Enumerable.Range(1, SubscriptionPlanCatalog.KurucuKampanyaKontenjani)
            .FirstOrDefault(x => !used.Contains(x));
        if (slot == 0)
            throw new InvalidOperationException("Lansman fiyati kontenjani doldu. Guncel teklifi yenileyin.");

        db.KurucuKampanyaHaklari.Add(new KurucuKampanyaHakki
        {
            IsletmeId = command.BusinessId,
            KampanyaKodu = SubscriptionPlanCatalog.KurucuKampanyaKodu,
            SiraNo = slot,
            CheckoutAnahtari = checkoutKey,
            Durum = "Rezerve",
            RezerveAt = now,
            RezervasyonBitisAt = now.AddMinutes(30),
            UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<SubscriptionReconciliationResult> ReconcileAsync(
        DateTime now,
        CancellationToken ct = default)
    {
        var current = EnsureUtc(now);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var (sevenDayReminders, threeDayReminders) = await SendDueTrialRemindersAsync(db, current, ct);

        var expiredTrials = 0;
        var trials = await db.IsletmeDenemeleri
            .Where(x => x.Durum == "Aktif" && x.BitisAt <= current)
            .ToListAsync(ct);
        foreach (var trial in trials)
        {
            trial.Durum = trial.DonemSonundaIptal ? "IptalEdildi" : "SonaErdi";
            trial.UpdatedAt = current;
            expiredTrials++;
        }

        var expiredSubscriptions = 0;
        var cancelledSubscriptions = 0;
        var gracePeriodsEnded = 0;
        var subscriptions = await db.Abonelikler
            .Where(x => x.Durum == "Aktif")
            .ToListAsync(ct);
        foreach (var subscription in subscriptions)
        {
            if (subscription.ToleransBitisAt is { } graceEnd && EnsureUtc(graceEnd) <= current)
            {
                subscription.Durum = "OdemeBasarisiz";
                subscription.UpdatedAt = current;
                gracePeriodsEnded++;
                continue;
            }

            if (subscription.DonemBitisAt is not { } periodEnd || EnsureUtc(periodEnd) > current)
                continue;

            if (!subscription.DonemSonundaIptal &&
                subscription.PlanlananDegisiklikAt is { } changeAt &&
                EnsureUtc(changeAt) <= current &&
                !string.IsNullOrWhiteSpace(subscription.PlanlananPlanKodu) &&
                !string.IsNullOrWhiteSpace(subscription.PlanlananFaturalamaDonemi))
            {
                // A scheduled downgrade records intent only. It must never grant a new paid
                // period until the provider confirms the next charge through a payment webhook.
                // Keep the target fields so the billing screen can offer the intended checkout.
                subscription.Durum = "SonaErdi";
                subscription.UpdatedAt = current;
                expiredSubscriptions++;
                continue;
            }

            subscription.Durum = subscription.DonemSonundaIptal ? "IptalEdildi" : "SonaErdi";
            subscription.UpdatedAt = current;
            if (subscription.DonemSonundaIptal)
                cancelledSubscriptions++;
            else
                expiredSubscriptions++;
        }

        await db.SaveChangesAsync(ct);

        return new SubscriptionReconciliationResult(
            expiredTrials,
            expiredSubscriptions,
            cancelledSubscriptions,
            gracePeriodsEnded,
            sevenDayReminders,
            threeDayReminders);
    }

    private async Task<(int SevenDay, int ThreeDay)> SendDueTrialRemindersAsync(
        CashTrackerDbContext db,
        DateTime current,
        CancellationToken ct)
    {
        if (_reminderSender is null)
            return (0, 0);

        var trials = await db.IsletmeDenemeleri
            .Where(x => x.Durum == "Aktif" && !x.DonemSonundaIptal && x.BitisAt > current)
            .Where(x => x.YediGunHatirlatmaAt == null || x.UcGunHatirlatmaAt == null)
            .ToListAsync(ct);
        var sevenDay = 0;
        var threeDay = 0;

        foreach (var trial in trials)
        {
            var endsAt = EnsureUtc(trial.BitisAt);
            var daysRemaining = current >= endsAt.AddDays(-3)
                ? 3
                : current >= endsAt.AddDays(-7)
                    ? 7
                    : 0;
            if (daysRemaining == 0 || (daysRemaining == 3 && trial.UcGunHatirlatmaAt is not null) ||
                (daysRemaining == 7 && trial.YediGunHatirlatmaAt is not null))
            {
                continue;
            }

            var email = await db.IsletmeUyelikleri.AsNoTracking()
                .Where(x => x.IsletmeId == trial.IsletmeId && x.Durum == "Aktif" && x.KullaniciId != null)
                .Join(
                    db.Kullanicilar.AsNoTracking(),
                    membership => membership.KullaniciId,
                    user => user.Id,
                    (membership, user) => new { membership.Rol, user.Eposta })
                .Where(x => x.Eposta != string.Empty)
                .OrderByDescending(x => x.Rol == "isletme_sahibi")
                .Select(x => x.Eposta)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(email))
                continue;

            var quote = _pricing.CreateQuote(
                trial.PlanKodu,
                trial.HesapTipi,
                PaymentBillingPeriods.Monthly,
                trial.EkMusteriKredisi);
            var planName = SubscriptionPlanCatalog.Plans
                .Single(x => string.Equals(x.Kod, trial.PlanKodu, StringComparison.OrdinalIgnoreCase))
                .Ad;
            var sent = await _reminderSender.SendTrialEndingAsync(
                new SubscriptionTrialReminder(
                    trial.IsletmeId,
                    trial.HesapTipi,
                    email,
                    planName,
                    daysRemaining,
                    endsAt,
                    quote.NetAmount,
                    quote.VatAmount,
                    quote.TotalAmount,
                    quote.Currency,
                    "/app/abonelik"),
                ct);
            if (!sent)
                continue;

            if (daysRemaining == 3)
            {
                trial.YediGunHatirlatmaAt ??= current;
                trial.UcGunHatirlatmaAt = current;
                threeDay++;
            }
            else
            {
                trial.YediGunHatirlatmaAt = current;
                sevenDay++;
            }
        }

        if (sevenDay + threeDay > 0)
            await db.SaveChangesAsync(ct);

        return (sevenDay, threeDay);
    }

    private bool ApplyEvent(
        CashTrackerDbContext db,
        OdemeIslemi payment,
        PaymentWebhookEvent paymentEvent)
    {
        var occurredAt = EnsureUtc(paymentEvent.OccurredAt);

        if (payment.SonOlayAt is { } lastEventAt && occurredAt < EnsureUtc(lastEventAt))
            return false;

        if (paymentEvent.EventType == PaymentEventTypes.PaymentSucceeded &&
            (payment.Durum is PaymentTransactionStates.Succeeded or PaymentTransactionStates.Refunded) &&
            string.Equals(payment.SaglayiciIslemId, paymentEvent.ProviderTransactionId, StringComparison.Ordinal))
            return false;

        if (payment.Durum == PaymentTransactionStates.Refunded &&
            paymentEvent.EventType != PaymentEventTypes.PaymentRefunded)
            return false;

        if (payment.Durum == PaymentTransactionStates.Succeeded &&
            paymentEvent.EventType == PaymentEventTypes.TrialAuthorized)
            return false;

        payment.SaglayiciIslemId = paymentEvent.ProviderTransactionId;
        payment.SonOlayAt = occurredAt;
        payment.UpdatedAt = DateTime.UtcNow;

        if (string.Equals(payment.IslemTipi, PaymentTransactionTypes.AccountantService, StringComparison.Ordinal))
            return ApplyAccountantServiceEvent(db, payment, paymentEvent, occurredAt, _accountantPaymentOptions);

        switch (paymentEvent.EventType)
        {
            case PaymentEventTypes.TrialAuthorized:
                ApplyTrialAuthorized(db, payment, occurredAt);
                if (!IsTerminal(payment.Durum))
                    payment.Durum = PaymentTransactionStates.TrialAuthorized;
                payment.TamamlandiAt ??= occurredAt;
                break;

            case PaymentEventTypes.PaymentSucceeded:
                ApplyPaymentSucceeded(db, payment, occurredAt);
                payment.Durum = PaymentTransactionStates.Succeeded;
                payment.TamamlandiAt = occurredAt;
                payment.HataKodu = string.Empty;
                payment.HataMesaji = string.Empty;
                break;

            case PaymentEventTypes.PaymentFailed:
                ApplyPaymentFailed(db, payment, occurredAt);
                if (!string.Equals(payment.Durum, PaymentTransactionStates.Succeeded, StringComparison.Ordinal))
                    payment.Durum = PaymentTransactionStates.Failed;
                payment.HataKodu = "provider_payment_failed";
                payment.HataMesaji = "Odeme saglayicisi tahsilatin basarisiz oldugunu bildirdi.";
                break;

            case PaymentEventTypes.PaymentRefunded:
                ApplyPaymentRefunded(db, payment, occurredAt);
                payment.Durum = PaymentTransactionStates.Refunded;
                payment.TamamlandiAt = occurredAt;
                break;

            case PaymentEventTypes.SubscriptionCancelled:
                ApplyCancellation(db, payment, occurredAt);
                if (payment.Durum is not PaymentTransactionStates.Refunded and not PaymentTransactionStates.Succeeded)
                    payment.Durum = PaymentTransactionStates.Cancelled;
                break;
        }

        return true;
    }

    private static bool ApplyAccountantServiceEvent(
        CashTrackerDbContext db,
        OdemeIslemi payment,
        PaymentWebhookEvent paymentEvent,
        DateTime occurredAt,
        MuhasebeciOdemeOptions options)
    {
        var servicePayment = db.MuhasebeciHizmetOdemeleri.SingleOrDefault(x => x.OdemeIslemiId == payment.Id)
            ?? throw new InvalidOperationException("Muhasebeci hizmet ödemesi kaydı bulunamadı.");
        var request = db.MuhasebeciMusteriTalepleri.SingleOrDefault(x => x.Id == servicePayment.TalepId)
            ?? throw new InvalidOperationException("Muhasebeci talebi bulunamadı.");

        switch (paymentEvent.EventType)
        {
            case PaymentEventTypes.PaymentSucceeded:
                if (servicePayment.Durum == MuhasebeciHizmetOdemeDurumlari.IptalEdildi)
                    return false;
                if (servicePayment.Durum == MuhasebeciHizmetOdemeDurumlari.TahsilEdildi)
                    return false;

                var relation = db.MuhasebeciMusterileri.SingleOrDefault(x =>
                    x.MuhasebeciIsletmeId == servicePayment.MuhasebeciIsletmeId &&
                    x.MusteriIsletmeId == servicePayment.MusteriIsletmeId);
                var isFirstPayment = relation is null;
                if ((isFirstPayment && request.Durum != MuhasebeciTalepDurumlari.OdemeBekliyor) ||
                    (!isFirstPayment && (request.Durum != MuhasebeciTalepDurumlari.Kabul || relation!.Durum != "Aktif")))
                    return false;
                if (relation is null)
                {
                    relation = new MuhasebeciMusteri
                    {
                        MuhasebeciIsletmeId = servicePayment.MuhasebeciIsletmeId,
                        MusteriIsletmeId = servicePayment.MusteriIsletmeId,
                        CreatedAt = occurredAt
                    };
                    db.MuhasebeciMusterileri.Add(relation);
                }

                relation.Durum = "Aktif";
                relation.YetkiSeviyesi = request.YetkiSeviyesi;
                relation.Kaynak = request.Tur;
                relation.TalepId = request.Id;
                relation.DavetKodu = string.IsNullOrWhiteSpace(request.DavetKodu) ? relation.DavetKodu : request.DavetKodu;
                if (isFirstPayment)
                    relation.BaslangicAt = occurredAt;
                relation.BitisAt = null;
                relation.KabulAt ??= occurredAt;
                relation.UpdatedAt = occurredAt;

                request.Durum = MuhasebeciTalepDurumlari.Kabul;
                request.SonucAt = occurredAt;
                request.UpdatedAt = occurredAt;
                servicePayment.Durum = MuhasebeciHizmetOdemeDurumlari.TahsilEdildi;
                servicePayment.TahsilEdilenTutar = paymentEvent.Amount;
                servicePayment.PlatformKomisyonOrani = servicePayment.PlatformKomisyonOrani == 0m
                    ? options.PlatformCommissionRate
                    : servicePayment.PlatformKomisyonOrani;
                servicePayment.PlatformKomisyonTutari = decimal.Round(
                    paymentEvent.Amount * servicePayment.PlatformKomisyonOrani / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
                servicePayment.AktarilacakTutar = paymentEvent.Amount - servicePayment.PlatformKomisyonTutari;
                servicePayment.TahsilEdildiAt = occurredAt;
                servicePayment.UpdatedAt = occurredAt;

                if (!db.MuhasebeciAktarimAlacaklari.Any(x =>
                        x.MuhasebeciHizmetOdemesiId == servicePayment.Id &&
                        x.AktarilacakTutar >= 0m))
                {
                    db.MuhasebeciAktarimAlacaklari.Add(new MuhasebeciAktarimAlacagi
                    {
                        MuhasebeciHizmetOdemesiId = servicePayment.Id,
                        MuhasebeciIsletmeId = servicePayment.MuhasebeciIsletmeId,
                        MusteriIsletmeId = servicePayment.MusteriIsletmeId,
                        TalepId = servicePayment.TalepId,
                        TahsilEdilenTutar = paymentEvent.Amount,
                        PlatformKomisyonTutari = servicePayment.PlatformKomisyonTutari,
                        AktarilacakTutar = servicePayment.AktarilacakTutar,
                        ParaBirimi = paymentEvent.Currency,
                        AktarimDonemi = servicePayment.HizmetDonemi,
                        Durum = MuhasebeciAktarimDurumlari.Bekliyor,
                        AktarimReferansi = $"pending-{servicePayment.Id}",
                        TahakkukAt = occurredAt,
                        CreatedAt = occurredAt,
                        UpdatedAt = occurredAt
                    });
                }

                payment.Durum = PaymentTransactionStates.Succeeded;
                payment.TamamlandiAt = occurredAt;
                payment.HataKodu = string.Empty;
                payment.HataMesaji = string.Empty;
                return true;

            case PaymentEventTypes.PaymentFailed:
                servicePayment.Durum = MuhasebeciHizmetOdemeDurumlari.Basarisiz;
                servicePayment.UpdatedAt = occurredAt;
                payment.Durum = PaymentTransactionStates.Failed;
                payment.HataKodu = "provider_payment_failed";
                payment.HataMesaji = "Odeme saglayicisi tahsilatin basarisiz oldugunu bildirdi.";
                return true;

            case PaymentEventTypes.PaymentRefunded:
                servicePayment.Durum = MuhasebeciHizmetOdemeDurumlari.IadeEdildi;
                servicePayment.UpdatedAt = occurredAt;
                request.Durum = MuhasebeciTalepDurumlari.Iptal;
                request.UpdatedAt = occurredAt;
                var activeRelation = db.MuhasebeciMusterileri.SingleOrDefault(x =>
                    x.MuhasebeciIsletmeId == servicePayment.MuhasebeciIsletmeId &&
                    x.MusteriIsletmeId == servicePayment.MusteriIsletmeId &&
                    x.Durum == "Aktif");
                if (activeRelation is not null)
                {
                    activeRelation.Durum = "Pasif";
                    activeRelation.BitisAt = occurredAt;
                    activeRelation.UpdatedAt = occurredAt;
                }
                var payable = db.MuhasebeciAktarimAlacaklari.SingleOrDefault(x =>
                    x.MuhasebeciHizmetOdemesiId == servicePayment.Id &&
                    x.AktarilacakTutar >= 0m);
                if (payable is not null)
                {
                    if (payable.Durum == MuhasebeciAktarimDurumlari.Aktarildi)
                    {
                        var hasClawback = db.MuhasebeciAktarimAlacaklari.Any(x =>
                            x.MuhasebeciHizmetOdemesiId == servicePayment.Id &&
                            x.AktarilacakTutar < 0m);
                        if (!hasClawback)
                        {
                            db.MuhasebeciAktarimAlacaklari.Add(new MuhasebeciAktarimAlacagi
                            {
                                MuhasebeciHizmetOdemesiId = servicePayment.Id,
                                MuhasebeciIsletmeId = servicePayment.MuhasebeciIsletmeId,
                                MusteriIsletmeId = servicePayment.MusteriIsletmeId,
                                TalepId = servicePayment.TalepId,
                                TahsilEdilenTutar = -payable.TahsilEdilenTutar,
                                PlatformKomisyonTutari = -payable.PlatformKomisyonTutari,
                                AktarilacakTutar = -payable.AktarilacakTutar,
                                ParaBirimi = payable.ParaBirimi,
                                AktarimDonemi = occurredAt.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                                Durum = MuhasebeciAktarimDurumlari.Bekliyor,
                                AktarimReferansi = $"clawback-{servicePayment.Id}",
                                TahakkukAt = occurredAt,
                                TersKayitAt = occurredAt,
                                CreatedAt = occurredAt,
                                UpdatedAt = occurredAt
                            });
                        }
                    }
                    else if (payable.Durum == MuhasebeciAktarimDurumlari.Bekliyor)
                    {
                        payable.Durum = MuhasebeciAktarimDurumlari.TersKayit;
                        payable.TersKayitAt = occurredAt;
                        payable.UpdatedAt = occurredAt;
                    }
                }
                payment.Durum = PaymentTransactionStates.Refunded;
                payment.TamamlandiAt = occurredAt;
                return true;

            case PaymentEventTypes.SubscriptionCancelled:
                servicePayment.Durum = MuhasebeciHizmetOdemeDurumlari.IptalEdildi;
                servicePayment.UpdatedAt = occurredAt;
                request.Durum = MuhasebeciTalepDurumlari.Iptal;
                request.UpdatedAt = occurredAt;
                payment.Durum = PaymentTransactionStates.Cancelled;
                payment.TamamlandiAt = occurredAt;
                return true;

            case PaymentEventTypes.TrialAuthorized:
                return false;

            default:
                return false;
        }
    }

    private static void ApplyTrialAuthorized(CashTrackerDbContext db, OdemeIslemi payment, DateTime occurredAt)
    {
        var existing = db.IsletmeDenemeleri.Local.FirstOrDefault(x =>
                x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi)
            ?? db.IsletmeDenemeleri.SingleOrDefault(x =>
                x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi);
        if (existing is not null)
        {
            if (existing.Durum == "Aktif" && existing.OdemeYontemiEklendi)
                return;
            throw new InvalidOperationException("Bu hesap tipi daha once ucretsiz deneme kullandi.");
        }

        var trialDays = string.Equals(payment.HesapTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
            ? 14
            : 30;
        db.IsletmeDenemeleri.Add(new IsletmeDeneme
        {
            IsletmeId = payment.IsletmeId,
            HesapTipi = payment.HesapTipi,
            PlanKodu = payment.PlanKodu,
            FaturalamaDonemi = payment.FaturalamaDonemi,
            EkMusteriKredisi = payment.EkMusteriKredisi,
            Durum = "Aktif",
            BaslangicAt = occurredAt,
            BitisAt = occurredAt.AddDays(trialDays),
            OdemeYontemiEklendi = true,
            OdemeSaglayici = payment.OdemeSaglayici,
            SaglayiciMusteriId = $"business-{payment.IsletmeId}",
            SaglayiciOdemeYontemiId = payment.SaglayiciIslemId,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt
        });
    }

    private static void ApplyPaymentSucceeded(CashTrackerDbContext db, OdemeIslemi payment, DateTime occurredAt)
    {
        var alreadyCreated = db.Abonelikler.Local.Any(x =>
                x.IsletmeId == payment.IsletmeId &&
                x.OdemeSaglayici == payment.OdemeSaglayici &&
                x.SaglayiciAbonelikId == payment.SaglayiciIslemId)
            || db.Abonelikler.Any(x =>
                x.IsletmeId == payment.IsletmeId &&
                x.OdemeSaglayici == payment.OdemeSaglayici &&
                x.SaglayiciAbonelikId == payment.SaglayiciIslemId);
        if (alreadyCreated)
            return;

        var founderRight = db.KurucuKampanyaHaklari.SingleOrDefault(x =>
            x.CheckoutAnahtari == payment.CheckoutAnahtari &&
            x.KampanyaKodu == payment.KampanyaKodu);
        if (founderRight is not null)
        {
            founderRight.Durum = "Kazanildi";
            founderRight.KazanildiAt = occurredAt;
            founderRight.UpdatedAt = occurredAt;
        }

        var activeSubscriptions = db.Abonelikler
            .Where(x => x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi && x.Durum == "Aktif")
            .ToList();
        var replacedSubscription = activeSubscriptions
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefault();
        var preservesCurrentPeriod =
            string.Equals(payment.DegisiklikTipi, SubscriptionChangeTypes.ImmediateUpgrade, StringComparison.Ordinal) &&
            payment.HedefDonemBitisAt is { } targetEnd &&
            replacedSubscription?.DonemBitisAt is { } currentEnd &&
            EnsureUtc(targetEnd) == EnsureUtc(currentEnd);
        var newPeriodStart = preservesCurrentPeriod
            ? EnsureUtc(replacedSubscription!.DonemBaslangicAt)
            : occurredAt;
        foreach (var active in activeSubscriptions)
        {
            active.Durum = "Degistirildi";
            if (!preservesCurrentPeriod)
                active.DonemBitisAt = occurredAt;
            active.UpdatedAt = occurredAt;
        }

        var trial = db.IsletmeDenemeleri.SingleOrDefault(x =>
            x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi && x.Durum == "Aktif");
        if (trial is not null)
        {
            trial.Durum = "Donusturuldu";
            trial.UpdatedAt = occurredAt;
        }

        var periodEnd = payment.HedefDonemBitisAt is { } targetPeriodEnd
            ? EnsureUtc(targetPeriodEnd)
            : string.Equals(payment.FaturalamaDonemi, PaymentBillingPeriods.Annual, StringComparison.Ordinal)
                ? occurredAt.AddYears(1)
                : occurredAt.AddMonths(1);
        var fullPeriodNetAmount = payment.TamDonemNetTutar > 0m ? payment.TamDonemNetTutar : payment.NetTutar;
        db.Abonelikler.Add(new Abonelik
        {
            IsletmeId = payment.IsletmeId,
            HesapTipi = payment.HesapTipi,
            PlanKodu = payment.PlanKodu,
            Durum = "Aktif",
            AylikTutar = string.Equals(payment.FaturalamaDonemi, PaymentBillingPeriods.Annual, StringComparison.Ordinal)
                ? decimal.Round(fullPeriodNetAmount / 12m, 2, MidpointRounding.AwayFromZero)
                : fullPeriodNetAmount,
            FaturalamaDonemi = payment.FaturalamaDonemi,
            EkMusteriKredisi = payment.EkMusteriKredisi,
            KampanyaKodu = payment.KampanyaKodu,
            YenilemeDonemTutari = payment.YenilemeNetTutar,
            IndirimliDonemKalan = string.Equals(payment.FaturalamaDonemi, PaymentBillingPeriods.Monthly, StringComparison.Ordinal)
                ? Math.Max(0, payment.IndirimliDonemSayisi - 1)
                : 0,
            DonemTutari = fullPeriodNetAmount,
            ParaBirimi = payment.ParaBirimi,
            DonemBaslangicAt = newPeriodStart,
            DonemBitisAt = periodEnd,
            OdemeSaglayici = payment.OdemeSaglayici,
            SaglayiciMusteriId = $"business-{payment.IsletmeId}",
            SaglayiciAbonelikId = payment.SaglayiciIslemId,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt
        });
    }

    private static void ApplyPaymentFailed(CashTrackerDbContext db, OdemeIslemi payment, DateTime occurredAt)
    {
        if (string.Equals(payment.IslemTipi, PaymentTransactionTypes.PlanUpgrade, StringComparison.Ordinal))
            return;

        var subscription = db.Abonelikler
            .Where(x => x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi &&
                        (x.Durum == "Aktif" || x.Durum == "SonaErdi"))
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefault();
        if (subscription is null)
            return;

        subscription.Durum = "Aktif";
        subscription.OdemeSorunuAt ??= occurredAt;
        subscription.ToleransBitisAt = occurredAt.AddDays(7);
        subscription.UpdatedAt = occurredAt;
    }

    private static void ApplyPaymentRefunded(CashTrackerDbContext db, OdemeIslemi payment, DateTime occurredAt)
    {
        foreach (var subscription in db.Abonelikler.Where(x =>
                     x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi && x.Durum == "Aktif"))
        {
            subscription.Durum = "IadeEdildi";
            subscription.DonemBitisAt = occurredAt;
            subscription.UpdatedAt = occurredAt;
        }

        if (string.Equals(payment.IslemTipi, PaymentTransactionTypes.PlanUpgrade, StringComparison.Ordinal))
        {
            var previous = db.Abonelikler
                .Where(x => x.IsletmeId == payment.IsletmeId &&
                            x.HesapTipi == payment.HesapTipi &&
                            x.Durum == "Degistirildi" &&
                            x.DonemBitisAt != null &&
                            x.DonemBitisAt > occurredAt)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();
            if (previous is not null)
            {
                previous.Durum = "Aktif";
                previous.UpdatedAt = occurredAt;
            }
        }
    }

    private static void ApplyCancellation(CashTrackerDbContext db, OdemeIslemi payment, DateTime occurredAt)
    {
        var subscription = db.Abonelikler
            .Where(x => x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi && x.Durum == "Aktif")
            .OrderByDescending(x => x.DonemBaslangicAt)
            .FirstOrDefault();
        if (subscription is not null)
        {
            subscription.DonemSonundaIptal = true;
            subscription.IptalAt = occurredAt;
            subscription.UpdatedAt = occurredAt;
        }

        var trial = db.IsletmeDenemeleri.SingleOrDefault(x =>
            x.IsletmeId == payment.IsletmeId && x.HesapTipi == payment.HesapTipi && x.Durum == "Aktif");
        if (trial is not null)
        {
            trial.DonemSonundaIptal = true;
            trial.IptalAt = occurredAt;
            trial.UpdatedAt = occurredAt;
        }
    }

    private static string? ValidateEventAmount(OdemeIslemi payment, PaymentWebhookEvent paymentEvent)
    {
        if (!string.Equals(payment.ParaBirimi, paymentEvent.Currency, StringComparison.OrdinalIgnoreCase))
            return "Webhook para birimi checkout ile uyusmuyor.";

        var permitsZero = paymentEvent.EventType is PaymentEventTypes.TrialAuthorized
            or PaymentEventTypes.PaymentFailed
            or PaymentEventTypes.SubscriptionCancelled;
        if (permitsZero && paymentEvent.Amount == 0)
            return null;
        return paymentEvent.Amount == payment.ToplamTutar
            ? null
            : "Webhook tutari checkout tutariyla uyusmuyor.";
    }

    private static bool TryBuildExistingResult(
        OdemeIslemi payment,
        PaymentQuote quote,
        out SubscriptionCheckoutResult result)
    {
        if (!string.IsNullOrWhiteSpace(payment.CheckoutUrl) &&
            Uri.TryCreate(payment.CheckoutUrl, UriKind.Absolute, out var checkoutUrl) &&
            payment.CheckoutExpiresAt is { } expiresAt &&
            expiresAt > DateTime.UtcNow)
        {
            result = new SubscriptionCheckoutResult(
                payment.Id,
                quote,
                new PaymentCheckoutSession(
                    payment.OdemeSaglayici,
                    payment.SaglayiciOturumId,
                    checkoutUrl,
                    expiresAt,
                    null),
                true);
            return true;
        }

        result = null!;
        return false;
    }

    private static CurrentSubscriptionPricingContext? ToPricingContext(Abonelik? subscription)
    {
        if (subscription?.DonemBitisAt is not { } periodEnd)
            return null;
        return new CurrentSubscriptionPricingContext(
            subscription.PlanKodu,
            subscription.FaturalamaDonemi,
            subscription.EkMusteriKredisi,
            subscription.DonemTutari,
            EnsureUtc(subscription.DonemBaslangicAt),
            EnsureUtc(periodEnd));
    }

    private static PaymentQuote BuildStoredQuote(OdemeIslemi payment) => new(
        payment.PlanKodu,
        payment.HesapTipi,
        payment.FaturalamaDonemi,
        payment.ParaBirimi,
        payment.NetTutar,
        payment.KdvOrani,
        payment.KdvTutar,
        payment.ToplamTutar,
        0,
        payment.EkMusteriKredisi,
        payment.PlanKodu == PlanKodlari.MuhasebeciStandart ? SubscriptionPlanCatalog.MuhasebeciStandartDahilMusteriSayisi : 0,
        payment.FaturalamaDonemi == PaymentBillingPeriods.Annual ? SubscriptionPlanCatalog.EkMusteriKredisiYillikTutar : SubscriptionPlanCatalog.EkMusteriKredisiAylikTutar,
        payment.KampanyaKodu,
        payment.KampanyaKodu == SubscriptionPlanCatalog.KurucuKampanyaKodu,
        payment.ListeNetTutar,
        payment.YenilemeNetTutar,
        payment.IndirimliDonemSayisi,
        payment.TamDonemNetTutar > 0m ? payment.TamDonemNetTutar : payment.NetTutar,
        payment.KistKrediNetTutar,
        string.IsNullOrWhiteSpace(payment.DegisiklikTipi) ? SubscriptionChangeTypes.NewSubscription : payment.DegisiklikTipi,
        payment.CreatedAt,
        payment.HedefDonemBitisAt);

    private static void EnsureCheckoutSelectionMatches(OdemeIslemi payment, PaymentQuote quote)
    {
        if (!string.Equals(payment.PlanKodu, quote.PlanCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payment.HesapTipi, quote.AccountType, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payment.FaturalamaDonemi, quote.BillingPeriod, StringComparison.OrdinalIgnoreCase) ||
            payment.EkMusteriKredisi != quote.ExtraCustomerCredits ||
            payment.ToplamTutar != quote.TotalAmount)
        {
            throw new InvalidOperationException("Ayni checkout anahtari farkli bir plan veya kredi secimiyle kullanilamaz.");
        }
    }

    private static void EnsureExpectedQuoteMatches(SubscriptionCheckoutCommand command, PaymentQuote quote)
    {
        if ((command.ExpectedTotalAmount is { } total && total != quote.TotalAmount) ||
            (command.ExpectedProrationCreditNetAmount is { } credit && credit != quote.ProrationCreditNetAmount) ||
            (!string.IsNullOrWhiteSpace(command.ExpectedChangeType) &&
             !string.Equals(command.ExpectedChangeType, quote.ChangeType, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Abonelik teklifi degisti. Guncel tutari gorup yeniden onaylayin.");
        }
    }

    private static void ValidateCheckoutCommand(SubscriptionCheckoutCommand command)
    {
        if (command.BusinessId <= 0)
            throw new ArgumentOutOfRangeException(nameof(command.BusinessId));
        if (!string.Equals(command.BillingPeriod, PaymentBillingPeriods.Monthly, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(command.BillingPeriod, PaymentBillingPeriods.Annual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Faturalama donemi aylik veya yillik olmalidir.");
        if (command.ExtraCustomerCredits is < 0 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(command.ExtraCustomerCredits), "Ek musteri kredisi 0 ile 10000 arasinda olmalidir.");
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length is < 8 or > 100)
            throw new ArgumentException("Checkout anahtari 8-100 karakter olmalidir.", nameof(command.IdempotencyKey));
        if (string.IsNullOrWhiteSpace(command.UserReference))
            throw new ArgumentException("Kullanici referansi zorunludur.", nameof(command.UserReference));
        if (string.IsNullOrWhiteSpace(command.CustomerEmail) || !command.CustomerEmail.Contains('@'))
            throw new ArgumentException("Gecerli e-posta zorunludur.", nameof(command.CustomerEmail));
        if (string.IsNullOrWhiteSpace(command.ConsentTextVersion) || string.IsNullOrWhiteSpace(command.ConsentText))
            throw new ArgumentException("Abonelik onay metni ve surumu zorunludur.", nameof(command.ConsentText));
        if (!command.SuccessUrl.IsAbsoluteUri || !command.FailureUrl.IsAbsoluteUri || !command.CallbackUrl.IsAbsoluteUri)
            throw new ArgumentException("Checkout donus ve callback adresleri mutlak URL olmalidir.");
    }

    private static bool IsTerminal(string state) => state is
        PaymentTransactionStates.Succeeded or
        PaymentTransactionStates.Refunded or
        PaymentTransactionStates.Cancelled;

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty))).ToLowerInvariant();

    private static string Limit(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
