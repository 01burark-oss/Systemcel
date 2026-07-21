using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    public partial class SubscriptionPlan2026 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaturalamaDonemi",
                table: "IsletmeDeneme",
                type: "text",
                nullable: false,
                defaultValue: "Aylik");

            migrationBuilder.AddColumn<decimal>(
                name: "DonemTutari",
                table: "Abonelik",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FaturalamaDonemi",
                table: "Abonelik",
                type: "text",
                nullable: false,
                defaultValue: "Aylik");

            migrationBuilder.Sql(
                "UPDATE \"Abonelik\" SET \"DonemTutari\" = \"AylikTutar\" WHERE \"DonemTutari\" = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaturalamaDonemi",
                table: "IsletmeDeneme");

            migrationBuilder.DropColumn(
                name: "DonemTutari",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "FaturalamaDonemi",
                table: "Abonelik");
        }
    }
}
