using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IPaymentPricingService
    {
        PaymentQuote CreateQuote(
            string planCode,
            string accountType,
            string billingPeriod,
            int extraCustomerCredits = 0,
            bool useFounderPrice = false);

        PaymentQuote CreateChangeQuote(
            string planCode,
            string accountType,
            string billingPeriod,
            int extraCustomerCredits,
            CurrentSubscriptionPricingContext? currentSubscription,
            DateTime nowUtc,
            bool useFounderPrice = false);
    }
}
