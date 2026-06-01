using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260525223000_BusinessEasySetup")]
    public partial class BusinessEasySetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IsletmeTuru",
                table: "Isletme",
                type: "text",
                nullable: false,
                defaultValue: "Genel");

            migrationBuilder.AddColumn<string>(
                name: "Konum",
                table: "Isletme",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "KolayKurulumTamamlandi",
                table: "Isletme",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Isletme_IsletmeTuru",
                table: "Isletme",
                column: "IsletmeTuru");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Isletme_IsletmeTuru",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "IsletmeTuru",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "Konum",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "KolayKurulumTamamlandi",
                table: "Isletme");
        }
    }
}
