using System.Data;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed class MuhasebeciOdemeService : IMuhasebeciOdemeService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IPaymentProvider _provider;

    public MuhasebeciOdemeService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IPaymentProvider provider)
    {
        _dbFactory = dbFactory;
        _provider = provider;
    }

    public async Task<MuhasebeciOdemeOzetiDto> GetAsync(
        int talepId,
        int musteriIsletmeId,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var payment = await RequireCustomerPaymentAsync(db, talepId, musteriIsletmeId, ct);
        var payable = await db.MuhasebeciAktarimAlacaklari.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MuhasebeciHizmetOdemesiId == payment.Id, ct);
        return BuildSummary(payment, payable);
    }

    public async Task<MuhasebeciOdemeCheckoutResult> BeginCheckoutAsync(
        MuhasebeciOdemeCheckoutCommand command,
        CancellationToken ct = default)
    {
        Validate(command);
        var checkoutKey = command.IdempotencyKey.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var servicePayment = await RequireCustomerPaymentAsync(db, command.TalepId, command.MusteriIsletmeId, ct);
        if (servicePayment.Durum == MuhasebeciHizmetOdemeDurumlari.TahsilEdildi)
            throw new InvalidOperationException("Bu muhasebeci hizmeti daha önce ödendi.");
        if (servicePayment.Durum == MuhasebeciHizmetOdemeDurumlari.IptalEdildi)
            throw new InvalidOperationException("İptal edilmiş teklif için ödeme başlatılamaz.");

        OdemeIslemi? payment = null;
        if (servicePayment.OdemeIslemiId.HasValue)
        {
            payment = await db.OdemeIslemleri.SingleAsync(x => x.Id == servicePayment.OdemeIslemiId.Value, ct);
            if (!string.Equals(payment.CheckoutAnahtari, checkoutKey, StringComparison.Ordinal))
                throw new InvalidOperationException("Bu talep için farklı bir Idempotency-Key kullanılamaz.");
            if (!string.IsNullOrWhiteSpace(payment.CheckoutUrl) &&
                Uri.TryCreate(payment.CheckoutUrl, UriKind.Absolute, out var existingUrl) &&
                payment.CheckoutExpiresAt is { } existingExpiry && existingExpiry > DateTime.UtcNow)
            {
                await transaction.CommitAsync(ct);
                return new MuhasebeciOdemeCheckoutResult(
                    payment.Id,
                    existingUrl,
                    existingExpiry,
                    true,
                    servicePayment.AylikHizmetBedeli,
                    servicePayment.ParaBirimi);
            }
        }

        var quote = BuildQuote(servicePayment);
        var now = DateTime.UtcNow;
        payment ??= new OdemeIslemi
        {
            IsletmeId = command.MusteriIsletmeId,
            CheckoutAnahtari = checkoutKey,
            HesapTipi = HesapTipleri.Isletme,
            PlanKodu = $"muhasebeci-hizmeti-{command.TalepId}",
            FaturalamaDonemi = PaymentBillingPeriods.Monthly,
            IslemTipi = PaymentTransactionTypes.AccountantService,
            Durum = PaymentTransactionStates.Preparing,
            OdemeSaglayici = _provider.Name,
            NetTutar = quote.NetAmount,
            ListeNetTutar = quote.NetAmount,
            YenilemeNetTutar = quote.NetAmount,
            KdvOrani = quote.VatRate,
            KdvTutar = quote.VatAmount,
            ToplamTutar = quote.TotalAmount,
            ParaBirimi = quote.Currency,
            CreatedAt = now,
            UpdatedAt = now
        };
        if (payment.Id == 0)
        {
            db.OdemeIslemleri.Add(payment);
            await db.SaveChangesAsync(ct);
            servicePayment.OdemeIslemiId = payment.Id;
        }

        try
        {
            var session = await _provider.CreateCheckoutAsync(new PaymentCheckoutRequest(
                checkoutKey,
                quote,
                $"business-{command.MusteriIsletmeId}",
                command.Eposta.Trim(),
                command.BasariliUrl,
                command.BasarisizUrl,
                command.CallbackUrl,
                new Dictionary<string, string>
                {
                    ["islemTipi"] = PaymentTransactionTypes.AccountantService,
                    ["talepId"] = command.TalepId.ToString(),
                    ["muhasebeciIsletmeId"] = servicePayment.MuhasebeciIsletmeId.ToString(),
                    ["musteriIsletmeId"] = servicePayment.MusteriIsletmeId.ToString()
                }), ct);

            payment.OdemeSaglayici = session.Provider;
            payment.SaglayiciOturumId = session.ProviderSessionId;
            payment.CheckoutUrl = session.CheckoutUrl.AbsoluteUri;
            payment.CheckoutExpiresAt = session.ExpiresAt;
            payment.Durum = PaymentTransactionStates.CheckoutOpen;
            payment.HataKodu = string.Empty;
            payment.HataMesaji = string.Empty;
            payment.UpdatedAt = DateTime.UtcNow;
            servicePayment.Durum = MuhasebeciHizmetOdemeDurumlari.CheckoutAcik;
            servicePayment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new MuhasebeciOdemeCheckoutResult(
                payment.Id,
                session.CheckoutUrl,
                session.ExpiresAt,
                false,
                servicePayment.AylikHizmetBedeli,
                servicePayment.ParaBirimi);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            payment.Durum = PaymentTransactionStates.Failed;
            payment.HataKodu = "checkout_create_failed";
            payment.HataMesaji = ex.Message.Length <= 500 ? ex.Message : ex.Message[..500];
            payment.UpdatedAt = DateTime.UtcNow;
            servicePayment.Durum = MuhasebeciHizmetOdemeDurumlari.Basarisiz;
            servicePayment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<MuhasebeciHizmetOdemesi> RequireCustomerPaymentAsync(
        CashTrackerDbContext db,
        int talepId,
        int musteriIsletmeId,
        CancellationToken ct)
    {
        return await db.MuhasebeciHizmetOdemeleri.SingleOrDefaultAsync(x =>
            x.TalepId == talepId && x.MusteriIsletmeId == musteriIsletmeId, ct)
            ?? throw new InvalidOperationException("Ödeme bekleyen muhasebeci teklifi bulunamadı.");
    }

    private static PaymentQuote BuildQuote(MuhasebeciHizmetOdemesi payment) => new(
        $"muhasebeci-hizmeti-{payment.TalepId}",
        HesapTipleri.Isletme,
        PaymentBillingPeriods.Monthly,
        payment.ParaBirimi,
        payment.AylikHizmetBedeli,
        0m,
        0m,
        payment.AylikHizmetBedeli,
        0,
        0,
        0,
        0m,
        string.Empty,
        false,
        payment.AylikHizmetBedeli,
        payment.AylikHizmetBedeli,
        0);

    private static MuhasebeciOdemeOzetiDto BuildSummary(
        MuhasebeciHizmetOdemesi payment,
        MuhasebeciAktarimAlacagi? payable) => new()
    {
        TalepId = payment.TalepId,
        MuhasebeciIsletmeId = payment.MuhasebeciIsletmeId,
        MusteriIsletmeId = payment.MusteriIsletmeId,
        AylikHizmetBedeli = payment.AylikHizmetBedeli,
        ParaBirimi = payment.ParaBirimi,
        OdemeDurumu = payment.Durum,
        OdemeYapilabilir = payment.Durum is MuhasebeciHizmetOdemeDurumlari.OdemeBekliyor
            or MuhasebeciHizmetOdemeDurumlari.CheckoutAcik
            or MuhasebeciHizmetOdemeDurumlari.Basarisiz,
        AktarilacakTutar = payable?.AktarilacakTutar ?? 0m,
        AktarimDonemi = payable?.AktarimDonemi ?? string.Empty,
        AktarimDurumu = payable?.Durum ?? MuhasebeciAktarimDurumlari.Olusmadi
    };

    private static void Validate(MuhasebeciOdemeCheckoutCommand command)
    {
        if (command.TalepId <= 0 || command.MusteriIsletmeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(command.TalepId));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length is < 8 or > 100)
            throw new ArgumentException("Idempotency-Key 8-100 karakter olmalıdır.", nameof(command.IdempotencyKey));
        if (string.IsNullOrWhiteSpace(command.KullaniciReferansi))
            throw new ArgumentException("Kullanıcı referansı zorunludur.", nameof(command.KullaniciReferansi));
        if (string.IsNullOrWhiteSpace(command.Eposta) || !command.Eposta.Contains('@'))
            throw new ArgumentException("Geçerli e-posta zorunludur.", nameof(command.Eposta));
        if (!command.BasariliUrl.IsAbsoluteUri || !command.BasarisizUrl.IsAbsoluteUri || !command.CallbackUrl.IsAbsoluteUri)
            throw new ArgumentException("Checkout dönüş adresleri mutlak URL olmalıdır.");
    }
}
