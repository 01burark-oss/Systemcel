namespace CashTracker.Core.Models;

public static class DeveloperApiScopes
{
    public const string ReadAll = "read:all";
    public const string SummaryRead = "summary:read";
    public const string AccountsRead = "accounts:read";
    public const string ProductsRead = "products:read";
    public const string InvoicesRead = "invoices:read";
    public const string BankRead = "bank:read";

    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ReadAll,
        SummaryRead,
        AccountsRead,
        ProductsRead,
        InvoicesRead,
        BankRead
    };
}

public sealed record DeveloperApiKeyCreateRequest(string Name, IReadOnlyList<string> Scopes, int ExpiresInDays);

public sealed record DeveloperApiKeyCreated(
    int Id,
    string Name,
    string Prefix,
    IReadOnlyList<string> Scopes,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string ApiKey);

public sealed record DeveloperApiKeyListItem(
    int Id,
    string Name,
    string Prefix,
    IReadOnlyList<string> Scopes,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt);

public sealed record DeveloperApiIdentity(
    int KeyId,
    int BusinessId,
    string Prefix,
    IReadOnlySet<string> Scopes)
{
    public bool HasScope(string scope) => Scopes.Contains(DeveloperApiScopes.ReadAll) || Scopes.Contains(scope);
}

public sealed record DeveloperApiPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record DeveloperApiBusinessSummary(
    int Id,
    string Name,
    string Currency,
    decimal Income,
    decimal Expense,
    decimal CashBalance,
    int AccountCount,
    int ProductCount,
    int InvoiceCount,
    int OpenBankTransactionCount,
    DateTime GeneratedAt);

public sealed record DeveloperApiAccount(int Id, string Type, string Name, string Phone, string Email, bool Active, DateTime UpdatedAt);
public sealed record DeveloperApiProduct(int Id, string Type, string Name, string Barcode, string Unit, decimal VatRate, decimal PurchasePrice, decimal SalePrice, bool Active, DateTime UpdatedAt);
public sealed record DeveloperApiInvoice(int Id, int AccountId, DateTime Date, DateTime? DueDate, string Type, string Status, string Number, decimal Subtotal, decimal VatTotal, decimal Total, decimal Paid, string PaymentMethod, string? Description, DateTime UpdatedAt);
public sealed record DeveloperApiBankTransaction(int Id, DateTime Date, string Description, decimal Amount, string Currency, string Status, string MatchedResourceType, int? MatchedResourceId, DateTime UpdatedAt);
