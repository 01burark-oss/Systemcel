using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260917193000_MarketplaceQuoteVat")]
public sealed class MarketplaceQuoteVat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "KdvOrani",
            table: "TedarikTeklifi",
            type: "numeric(5,2)",
            nullable: false,
            defaultValue: 20m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "KdvOrani",
            table: "TedarikTeklifi");
    }
}
