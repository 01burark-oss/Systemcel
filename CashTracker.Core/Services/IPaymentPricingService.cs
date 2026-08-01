using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IPaymentPricingService
    {
        PaymentQuote CreateQuote(string planCode, string accountType, string billingPeriod, int extraCustomerCredits = 0);
    }
}
