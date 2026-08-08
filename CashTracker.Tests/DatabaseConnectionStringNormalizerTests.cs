using Systemcel.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class DatabaseConnectionStringNormalizerTests
{
    [Fact]
    public void PostgreSqlUrl_NpgsqlBaglantiDizesineDonusturulur()
    {
        var result = DatabaseConnectionStringNormalizer.NormalizeDatabaseUrl(
            "postgresql://systemcel_app:p%40ss%3Aword@db.example.com:25060/systemcel?sslmode=require");

        Assert.NotNull(result);
        Assert.Contains("Host=db.example.com", result);
        Assert.Contains("Port=25060", result);
        Assert.Contains("Username=systemcel_app", result);
        Assert.Contains("Password=p@ss:word", result);
        Assert.Contains("Database=systemcel", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void AnahtarDegerDizesi_DegistirilmedenKullanilir()
    {
        const string connectionString = "Host=db;Database=systemcel;Username=app;Password=secret;SSL Mode=Require";

        Assert.Equal(connectionString, DatabaseConnectionStringNormalizer.NormalizeDatabaseUrl(connectionString));
    }

    [Fact]
    public void EksikKimlikBilgiliUrl_Reddedilir()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionStringNormalizer.NormalizeDatabaseUrl("postgresql://db.example.com/systemcel"));

        Assert.Contains("username and password", error.Message);
    }
}
