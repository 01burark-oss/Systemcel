using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    public partial class PaymentLifecycleFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IsletmeDeneme_IsletmeId_PlanKodu",
                table: "IsletmeDeneme");

            migrationBuilder.AddColumn<int>(
                name: "EkMusteriKredisi",
                table: "Abonelik",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OdemeSorunuAt",
                table: "Abonelik",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ToleransBitisAt",
                table: "Abonelik",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DonemSonundaIptal",
                table: "IsletmeDeneme",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EkMusteriKredisi",
                table: "IsletmeDeneme",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HesapTipi",
                table: "IsletmeDeneme",
                type: "text",
                nullable: false,
                defaultValue: "Isletme");

            migrationBuilder.AddColumn<DateTime>(
                name: "IptalAt",
                table: "IsletmeDeneme",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UcGunHatirlatmaAt",
                table: "IsletmeDeneme",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YediGunHatirlatmaAt",
                table: "IsletmeDeneme",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AbonelikOnayi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciRef = table.Column<string>(type: "text", nullable: false),
                    CheckoutAnahtari = table.Column<string>(type: "text", nullable: false),
                    HesapTipi = table.Column<string>(type: "text", nullable: false),
                    PlanKodu = table.Column<string>(type: "text", nullable: false),
                    FaturalamaDonemi = table.Column<string>(type: "text", nullable: false),
                    EkMusteriKredisi = table.Column<int>(type: "integer", nullable: false),
                    MetinSurumu = table.Column<string>(type: "text", nullable: false),
                    MetinHash = table.Column<string>(type: "text", nullable: false),
                    IstemciIpHash = table.Column<string>(type: "text", nullable: false),
                    UserAgentHash = table.Column<string>(type: "text", nullable: false),
                    NetTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ParaBirimi = table.Column<string>(type: "text", nullable: false),
                    OnayAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AbonelikOnayi", x => x.Id));

            migrationBuilder.CreateTable(
                name: "OdemeIslemi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    CheckoutAnahtari = table.Column<string>(type: "text", nullable: false),
                    HesapTipi = table.Column<string>(type: "text", nullable: false),
                    PlanKodu = table.Column<string>(type: "text", nullable: false),
                    FaturalamaDonemi = table.Column<string>(type: "text", nullable: false),
                    EkMusteriKredisi = table.Column<int>(type: "integer", nullable: false),
                    IslemTipi = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    OdemeSaglayici = table.Column<string>(type: "text", nullable: false),
                    SaglayiciOturumId = table.Column<string>(type: "text", nullable: false),
                    SaglayiciIslemId = table.Column<string>(type: "text", nullable: false),
                    CheckoutUrl = table.Column<string>(type: "text", nullable: false),
                    CheckoutExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NetTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ParaBirimi = table.Column<string>(type: "text", nullable: false),
                    HataKodu = table.Column<string>(type: "text", nullable: false),
                    HataMesaji = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TamamlandiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SonOlayAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OdemeIslemi", x => x.Id));

            migrationBuilder.CreateTable(
                name: "OdemeOlayi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OdemeSaglayici = table.Column<string>(type: "text", nullable: false),
                    OlayId = table.Column<string>(type: "text", nullable: false),
                    OlayTipi = table.Column<string>(type: "text", nullable: false),
                    CheckoutAnahtari = table.Column<string>(type: "text", nullable: false),
                    SaglayiciIslemId = table.Column<string>(type: "text", nullable: false),
                    IslenmeDurumu = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "text", nullable: false),
                    HataMesaji = table.Column<string>(type: "text", nullable: false),
                    SaglayiciAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AlindiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IslendiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OdemeOlayi", x => x.Id));

            migrationBuilder.Sql("""
                DELETE FROM "IsletmeDeneme" current_row
                USING "IsletmeDeneme" keeper
                WHERE current_row."IsletmeId" = keeper."IsletmeId"
                  AND current_row."HesapTipi" = keeper."HesapTipi"
                  AND current_row."Id" > keeper."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeDeneme_IsletmeId_HesapTipi",
                table: "IsletmeDeneme",
                columns: new[] { "IsletmeId", "HesapTipi" },
                unique: true);
            migrationBuilder.CreateIndex("IX_AbonelikOnayi_IsletmeId", "AbonelikOnayi", "IsletmeId");
            migrationBuilder.CreateIndex(
                name: "IX_AbonelikOnayi_IsletmeId_CheckoutAnahtari",
                table: "AbonelikOnayi",
                columns: new[] { "IsletmeId", "CheckoutAnahtari" },
                unique: true);
            migrationBuilder.CreateIndex("IX_OdemeIslemi_IsletmeId", "OdemeIslemi", "IsletmeId");
            migrationBuilder.CreateIndex(
                name: "IX_OdemeIslemi_IsletmeId_CheckoutAnahtari",
                table: "OdemeIslemi",
                columns: new[] { "IsletmeId", "CheckoutAnahtari" },
                unique: true);
            migrationBuilder.CreateIndex("IX_OdemeIslemi_SaglayiciIslemId", "OdemeIslemi", "SaglayiciIslemId");
            migrationBuilder.CreateIndex("IX_OdemeIslemi_SaglayiciOturumId", "OdemeIslemi", "SaglayiciOturumId");
            migrationBuilder.CreateIndex("IX_OdemeOlayi_CheckoutAnahtari", "OdemeOlayi", "CheckoutAnahtari");
            migrationBuilder.CreateIndex(
                name: "IX_OdemeOlayi_OdemeSaglayici_OlayId",
                table: "OdemeOlayi",
                columns: new[] { "OdemeSaglayici", "OlayId" },
                unique: true);
            migrationBuilder.CreateIndex("IX_OdemeOlayi_SaglayiciIslemId", "OdemeOlayi", "SaglayiciIslemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("AbonelikOnayi");
            migrationBuilder.DropTable("OdemeIslemi");
            migrationBuilder.DropTable("OdemeOlayi");

            migrationBuilder.DropIndex(
                name: "IX_IsletmeDeneme_IsletmeId_HesapTipi",
                table: "IsletmeDeneme");

            migrationBuilder.DropColumn("EkMusteriKredisi", "Abonelik");
            migrationBuilder.DropColumn("OdemeSorunuAt", "Abonelik");
            migrationBuilder.DropColumn("ToleransBitisAt", "Abonelik");
            migrationBuilder.DropColumn("DonemSonundaIptal", "IsletmeDeneme");
            migrationBuilder.DropColumn("EkMusteriKredisi", "IsletmeDeneme");
            migrationBuilder.DropColumn("HesapTipi", "IsletmeDeneme");
            migrationBuilder.DropColumn("IptalAt", "IsletmeDeneme");
            migrationBuilder.DropColumn("UcGunHatirlatmaAt", "IsletmeDeneme");
            migrationBuilder.DropColumn("YediGunHatirlatmaAt", "IsletmeDeneme");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeDeneme_IsletmeId_PlanKodu",
                table: "IsletmeDeneme",
                columns: new[] { "IsletmeId", "PlanKodu" },
                unique: true);
        }
    }
}
