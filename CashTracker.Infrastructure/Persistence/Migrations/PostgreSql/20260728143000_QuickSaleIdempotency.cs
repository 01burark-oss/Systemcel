using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260728143000_QuickSaleIdempotency")]
    public partial class QuickSaleIdempotency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HizliSatisAnahtari",
                table: "Fatura",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_IsletmeId_HizliSatisAnahtari",
                table: "Fatura",
                columns: new[] { "IsletmeId", "HizliSatisAnahtari" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fatura_IsletmeId_HizliSatisAnahtari",
                table: "Fatura");

            migrationBuilder.DropColumn(
                name: "HizliSatisAnahtari",
                table: "Fatura");
        }
    }
}
