using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260829093000_StockMovementCostSnapshot")]
public partial class StockMovementCostSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "BirimMaliyet", table: "StokHareket", type: "NUMERIC", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "MaliyetParaBirimi", table: "StokHareket", type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY");
        migrationBuilder.AddColumn<decimal>(name: "MaliyetKurSnapshot", table: "StokHareket", type: "NUMERIC", nullable: false, defaultValue: 1m);
        migrationBuilder.AddColumn<decimal>(name: "BirimMaliyetTry", table: "StokHareket", type: "NUMERIC", nullable: false, defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BirimMaliyet", table: "StokHareket");
        migrationBuilder.DropColumn(name: "MaliyetParaBirimi", table: "StokHareket");
        migrationBuilder.DropColumn(name: "MaliyetKurSnapshot", table: "StokHareket");
        migrationBuilder.DropColumn(name: "BirimMaliyetTry", table: "StokHareket");
    }
}
