using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260529133000_AccountantApplicationProfileFields")]
    public partial class AccountantApplicationProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "MuhasebeciProfil",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DeneyimYili",
                table: "MuhasebeciProfil",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProfilResmiUrl",
                table: "MuhasebeciProfil",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UcretBilgisi",
                table: "MuhasebeciProfil",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciProfil_DeneyimYili",
                table: "MuhasebeciProfil",
                column: "DeneyimYili");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciProfil_DeneyimYili",
                table: "MuhasebeciProfil");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "MuhasebeciProfil");

            migrationBuilder.DropColumn(
                name: "DeneyimYili",
                table: "MuhasebeciProfil");

            migrationBuilder.DropColumn(
                name: "ProfilResmiUrl",
                table: "MuhasebeciProfil");

            migrationBuilder.DropColumn(
                name: "UcretBilgisi",
                table: "MuhasebeciProfil");
        }
    }
}
