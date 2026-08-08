using Npgsql;

namespace Systemcel.Api;

public static class DatabaseConnectionStringNormalizer
{
    public static string? NormalizeDatabaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("DATABASE_URL is not a valid PostgreSQL URL.");

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(userInfo[0]))
            throw new InvalidOperationException("DATABASE_URL must contain a username and password.");

        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("DATABASE_URL must contain a database name.");

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            Database = database,
            SslMode = SslMode.Require
        }.ConnectionString;
    }
}
