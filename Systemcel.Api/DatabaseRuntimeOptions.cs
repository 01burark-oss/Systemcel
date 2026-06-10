namespace Systemcel.Api;

public sealed class DatabaseRuntimeOptions
{
    public string Provider { get; init; } = "PostgreSql";
    public string? ConnectionString { get; init; }

    public bool IsPostgreSql => string.Equals(Provider, "PostgreSql", StringComparison.OrdinalIgnoreCase);
}
