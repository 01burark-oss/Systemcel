using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260824143000_AccountantMonthlyServicePeriods")]
    public partial class AccountantMonthlyServicePeriods : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciHizmetOdemesi_TalepId",
                table: "MuhasebeciHizmetOdemesi");

            migrationBuilder.AddColumn<decimal>(
                name: "AktarilacakTutar",
                table: "MuhasebeciHizmetOdemesi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HizmetDonemi",
                table: "MuhasebeciHizmetOdemesi",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformKomisyonOrani",
                table: "MuhasebeciHizmetOdemesi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformKomisyonTutari",
                table: "MuhasebeciHizmetOdemesi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "VadeAt",
                table: "MuhasebeciHizmetOdemesi",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                UPDATE "MuhasebeciHizmetOdemesi"
                SET "HizmetDonemi" = TO_CHAR(COALESCE("TahsilEdildiAt", "CreatedAt", NOW()), 'YYYY-MM'),
                    "VadeAt" = DATE_TRUNC('month', COALESCE("TahsilEdildiAt", "CreatedAt", NOW()))
                WHERE "HizmetDonemi" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciHizmetOdemesi_TalepId_HizmetDonemi",
                table: "MuhasebeciHizmetOdemesi",
                columns: new[] { "TalepId", "HizmetDonemi" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciHizmetOdemesi_TalepId_HizmetDonemi",
                table: "MuhasebeciHizmetOdemesi");
            migrationBuilder.DropColumn("AktarilacakTutar", "MuhasebeciHizmetOdemesi");
            migrationBuilder.DropColumn("HizmetDonemi", "MuhasebeciHizmetOdemesi");
            migrationBuilder.DropColumn("PlatformKomisyonOrani", "MuhasebeciHizmetOdemesi");
            migrationBuilder.DropColumn("PlatformKomisyonTutari", "MuhasebeciHizmetOdemesi");
            migrationBuilder.DropColumn("VadeAt", "MuhasebeciHizmetOdemesi");
            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciHizmetOdemesi_TalepId",
                table: "MuhasebeciHizmetOdemesi",
                column: "TalepId",
                unique: true);
        }
    }
}
