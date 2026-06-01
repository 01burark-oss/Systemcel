using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CashTracker.Infrastructure.Persistence;

public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CashTrackerDbContext>
{
    public CashTrackerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SYSTEMCEL_DATABASE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                "Host=localhost;Port=5432;Database=systemcel_migrations;Username=postgres;Password=postgres";
        }

        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CashTrackerDbContext(options);
    }
}
