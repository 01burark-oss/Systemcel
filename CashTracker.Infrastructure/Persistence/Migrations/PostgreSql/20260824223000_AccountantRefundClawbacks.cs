using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260824223000_AccountantRefundClawbacks")]
    public partial class AccountantRefundClawbacks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId",
                table: "MuhasebeciAktarimAlacagi");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId",
                table: "MuhasebeciAktarimAlacagi",
                column: "MuhasebeciHizmetOdemesiId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciAktarimAlacagi_Accrual",
                table: "MuhasebeciAktarimAlacagi",
                column: "MuhasebeciHizmetOdemesiId",
                unique: true,
                filter: "\"AktarilacakTutar\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciAktarimAlacagi_RefundAdjustment",
                table: "MuhasebeciAktarimAlacagi",
                column: "MuhasebeciHizmetOdemesiId",
                unique: true,
                filter: "\"AktarilacakTutar\" < 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciAktarimAlacagi_Accrual",
                table: "MuhasebeciAktarimAlacagi");

            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciAktarimAlacagi_RefundAdjustment",
                table: "MuhasebeciAktarimAlacagi");

            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId",
                table: "MuhasebeciAktarimAlacagi");

            migrationBuilder.Sql("""
                DELETE FROM "MuhasebeciAktarimAlacagi"
                WHERE "AktarilacakTutar" < 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId",
                table: "MuhasebeciAktarimAlacagi",
                column: "MuhasebeciHizmetOdemesiId",
                unique: true);
        }
    }
}
