using System;
using System.Text.Json;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class PaymentInfrastructureTests
    {
        private const string Secret = "systemcel-fake-payment-secret";

        [Fact]
        public void Pricing_UsesServerCatalogAndAddsVat()
        {
            var service = new PaymentPricingService(20m);

            var quote = service.CreateQuote(
                PlanKodlari.IsletmeBuyume,
                HesapTipleri.Isletme,
                PaymentBillingPeriods.Annual);

            Assert.Equal(12384m, quote.NetAmount);
            Assert.Equal(2476.80m, quote.VatAmount);
            Assert.Equal(14860.80m, quote.TotalAmount);
            Assert.Equal(0, quote.TrialDays);
            Assert.False(quote.IsFounderPrice);
        }

        [Fact]
        public void Pricing_RejectsRoleMismatchAndFreePlans()
        {
            var service = new PaymentPricingService();

            Assert.Throws<InvalidOperationException>(() => service.CreateQuote(
                PlanKodlari.MuhasebeciStandart,
                HesapTipleri.Isletme,
                PaymentBillingPeriods.Monthly));
            Assert.Throws<InvalidOperationException>(() => service.CreateQuote(
                PlanKodlari.IsletmeUcretsiz,
                HesapTipleri.Isletme,
                PaymentBillingPeriods.Monthly));
        }

        [Fact]
        public void Pricing_StartsAccountantSubscriptionWithoutTrial()
        {
            var quote = new PaymentPricingService().CreateQuote(
                PlanKodlari.MuhasebeciPro,
                HesapTipleri.Muhasebeci,
                PaymentBillingPeriods.Monthly);

            Assert.Equal(0, quote.TrialDays);
            Assert.Equal(1499m, quote.NetAmount);
        }

        [Fact]
        public void Pricing_AddsRecurringCustomerCreditsOnlyToAccountantStandard()
        {
            var service = new PaymentPricingService();

            var quote = service.CreateQuote(
                PlanKodlari.MuhasebeciStandart,
                HesapTipleri.Muhasebeci,
                PaymentBillingPeriods.Monthly,
                extraCustomerCredits: 2);

            Assert.Equal(999m, quote.NetAmount);
            Assert.Equal(2, quote.ExtraCustomerCredits);
            Assert.Equal(10, quote.IncludedCustomerCount);
            Assert.Equal(50m, quote.CustomerCreditUnitAmount);
            Assert.Throws<InvalidOperationException>(() => service.CreateQuote(
                PlanKodlari.MuhasebeciPro,
                HesapTipleri.Muhasebeci,
                PaymentBillingPeriods.Monthly,
                extraCustomerCredits: 1));
        }

        [Fact]
        public void Pricing_FounderPriceKeepsAddOnCreditsAtListPrice()
        {
            var service = new PaymentPricingService();

            var monthly = service.CreateQuote(
                PlanKodlari.MuhasebeciStandart,
                HesapTipleri.Muhasebeci,
                PaymentBillingPeriods.Monthly,
                extraCustomerCredits: 2,
                useFounderPrice: true);
            var annual = service.CreateQuote(
                PlanKodlari.IsletmeBuyume,
                HesapTipleri.Isletme,
                PaymentBillingPeriods.Annual,
                useFounderPrice: true);

            Assert.Equal(799m, monthly.NetAmount);
            Assert.Equal(999m, monthly.ListNetAmount);
            Assert.Equal(3, monthly.DiscountedPeriodCount);
            Assert.Equal(SubscriptionPlanCatalog.KurucuKampanyaKodu, monthly.CampaignCode);
            Assert.Equal(9504m, annual.NetAmount);
            Assert.Equal(12384m, annual.RenewalNetAmount);
            Assert.Equal(1, annual.DiscountedPeriodCount);
        }

        [Fact]
        public async Task FakeProvider_CreatesDeterministicCheckoutFromServerQuote()
        {
            var provider = new FakePaymentProvider(Secret);
            var quote = new PaymentPricingService().CreateQuote(
                PlanKodlari.IsletmeBaslangic,
                HesapTipleri.Isletme,
                PaymentBillingPeriods.Monthly);
            var request = new PaymentCheckoutRequest(
                "checkout-1",
                quote,
                "business-1",
                "test@systemcel.local",
                new Uri("https://systemcel.local/success"),
                new Uri("https://systemcel.local/failure"),
                new Uri("https://systemcel.local/webhook"));

            var first = await provider.CreateCheckoutAsync(request);
            var second = await provider.CreateCheckoutAsync(request);

            Assert.Equal(first.ProviderSessionId, second.ProviderSessionId);
            Assert.Equal("Fake", first.Provider);
            Assert.Equal(quote.TrialDays, (first.FirstChargeAt!.Value.Date - DateTime.UtcNow.Date).Days);
        }

        [Fact]
        public void FakeProvider_VerifiesSignedWebhookAndRejectsTampering()
        {
            var provider = new FakePaymentProvider(Secret);
            var payload = JsonSerializer.Serialize(new
            {
                eventId = "evt-1",
                eventType = PaymentEventTypes.TrialAuthorized,
                merchantReference = "checkout-1",
                providerTransactionId = "tx-1",
                amount = 588m,
                currency = "TRY",
                occurredAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)
            });

            var valid = provider.VerifyWebhook(new PaymentWebhookEnvelope(payload, provider.SignPayload(payload)));
            var invalid = provider.VerifyWebhook(new PaymentWebhookEnvelope(payload + " ", provider.SignPayload(payload)));

            Assert.True(valid.IsValid);
            Assert.Equal("evt-1", valid.Event!.EventId);
            Assert.Equal(PaymentEventTypes.TrialAuthorized, valid.Event.EventType);
            Assert.False(invalid.IsValid);
        }
    }
}
