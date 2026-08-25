using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260824173000_SubscriptionPlanChanges")]
    public partial class SubscriptionPlanChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("PlanlananPlanKodu", "Abonelik", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>("PlanlananFaturalamaDonemi", "Abonelik", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>("PlanlananEkMusteriKredisi", "Abonelik", type: "integer", nullable: true);
            migrationBuilder.AddColumn<DateTime>("PlanlananDegisiklikAt", "Abonelik", type: "timestamp without time zone", nullable: true);
            migrationBuilder.AddColumn<decimal>("TamDonemNetTutar", "AbonelikOnayi", type: "NUMERIC", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>("KistKrediNetTutar", "AbonelikOnayi", type: "NUMERIC", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>("DegisiklikTipi", "AbonelikOnayi", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<decimal>("TamDonemNetTutar", "OdemeIslemi", type: "NUMERIC", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>("KistKrediNetTutar", "OdemeIslemi", type: "NUMERIC", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>("DegisiklikTipi", "OdemeIslemi", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>("HedefDonemBitisAt", "OdemeIslemi", type: "timestamp without time zone", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("PlanlananPlanKodu", "Abonelik");
            migrationBuilder.DropColumn("PlanlananFaturalamaDonemi", "Abonelik");
            migrationBuilder.DropColumn("PlanlananEkMusteriKredisi", "Abonelik");
            migrationBuilder.DropColumn("PlanlananDegisiklikAt", "Abonelik");
            migrationBuilder.DropColumn("TamDonemNetTutar", "AbonelikOnayi");
            migrationBuilder.DropColumn("KistKrediNetTutar", "AbonelikOnayi");
            migrationBuilder.DropColumn("DegisiklikTipi", "AbonelikOnayi");
            migrationBuilder.DropColumn("TamDonemNetTutar", "OdemeIslemi");
            migrationBuilder.DropColumn("KistKrediNetTutar", "OdemeIslemi");
            migrationBuilder.DropColumn("DegisiklikTipi", "OdemeIslemi");
            migrationBuilder.DropColumn("HedefDonemBitisAt", "OdemeIslemi");
        }
    }
}
