using System;

namespace CashTracker.Core.Models
{
    public static class EntitlementErrorCodes
    {
        public const string SubscriptionRequired = "subscription_required";
        public const string LimitReached = "limit_reached";
        public const string FeatureNotAvailable = "feature_not_available";
    }

    public static class EntitlementLimits
    {
        public const string Business = "business";
        public const string User = "user";
        public const string Invoice = "invoice";
        public const string CashTransaction = "cash_transaction";
        public const string CurrentAccount = "current_account";
        public const string ProductOrService = "product_or_service";
        public const string AccountantCustomer = "accountant_customer";
        public const string AiMessage = "ai_message";
    }

    public static class EntitlementFeatures
    {
        public const string Ai = "ai";
        public const string OfficialEInvoice = "official_e_invoice";
        public const string TelegramAutomation = "telegram_automation";
        public const string AdvancedExport = "advanced_export";
        public const string BankReconciliation = "bank_reconciliation";
        public const string StockReport = "stock_report";
        public const string AdvancedStock = "advanced_stock";
        public const string MultipleBranches = "multiple_branches";
        public const string MultipleCurrencies = "multiple_currencies";
        public const string ApiAccess = "api_access";
    }

    public sealed class EntitlementViolationException : InvalidOperationException
    {
        public EntitlementViolationException(
            string code,
            string message,
            string? limitName = null,
            int? limit = null,
            int? current = null,
            string? suggestedPlanCode = null)
            : base(message)
        {
            Code = code;
            LimitName = limitName;
            Limit = limit;
            Current = current;
            SuggestedPlanCode = suggestedPlanCode;
        }

        public string Code { get; }
        public string? LimitName { get; }
        public int? Limit { get; }
        public int? Current { get; }
        public string? SuggestedPlanCode { get; }
    }
}
