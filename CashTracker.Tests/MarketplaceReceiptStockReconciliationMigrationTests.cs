using System.Reflection;
using CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CashTracker.Tests;

public sealed class MarketplaceReceiptStockReconciliationMigrationTests
{
    [Fact]
    public void Backfill_QuarantinesOrdersWithLegacyReshipDecisions()
    {
        var migration = new MarketplaceReceiptStockReconciliation();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var up = typeof(MarketplaceReceiptStockReconciliation).GetMethod(
            "Up", BindingFlags.Instance | BindingFlags.NonPublic)!;
        up.Invoke(migration, [builder]);

        var backfill = Assert.Single(builder.Operations.OfType<SqlOperation>(),
            x => x.Sql.Contains("UPDATE \"TedarikciMalKabul\" AS receipt", StringComparison.Ordinal));

        Assert.Contains("history.\"TedarikciSiparisId\" = receipt.\"TedarikciSiparisId\"", backfill.Sql);
        Assert.Contains("history.\"CreatedAt\" >= receipt.\"CreatedAt\"", backfill.Sql);
        Assert.Contains("İtiraz kararı: YenidenTeslim", backfill.Sql);
        Assert.Contains("THEN 'ManuelInceleme'", backfill.Sql);
        Assert.Contains("ELSE 'Bekliyor'", backfill.Sql);
    }
}
