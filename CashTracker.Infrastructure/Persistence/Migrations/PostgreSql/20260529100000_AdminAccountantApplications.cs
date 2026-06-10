using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260529100000_AdminAccountantApplications")]
    public partial class AdminAccountantApplications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MuhasebeciVarMi",
                table: "Isletme",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Isletme_MuhasebeciVarMi",
                table: "Isletme",
                column: "MuhasebeciVarMi");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Isletme_MuhasebeciVarMi",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "MuhasebeciVarMi",
                table: "Isletme");
        }
    }
}
