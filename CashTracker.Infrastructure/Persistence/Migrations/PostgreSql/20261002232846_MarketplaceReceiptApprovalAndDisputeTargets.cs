using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MarketplaceReceiptApprovalAndDisputeTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ItirazAcildiAt",
                table: "TedarikciSiparis",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ItirazIlkYanitAt",
                table: "TedarikciSiparis",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ItirazKanitToplandiAt",
                table: "TedarikciSiparis",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ItirazYukseltildiAt",
                table: "TedarikciSiparis",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BelgeUyusmazligi",
                table: "TedarikciMalKabul",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IkinciOnayAt",
                table: "TedarikciMalKabul",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IkinciOnayNotu",
                table: "TedarikciMalKabul",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IkinciOnaylayanKullaniciRef",
                table: "TedarikciMalKabul",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MiktarDegisikligi",
                table: "TedarikciMalKabul",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OnayDurumu",
                table: "TedarikciMalKabul",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Onaylandi");

            migrationBuilder.AddColumn<string>(
                name: "OnayNedeni",
                table: "TedarikciMalKabul",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "TedarikciSiparis" AS orders SET "ItirazAcildiAt" = COALESCE(
                    (SELECT history."CreatedAt" FROM "TedarikciSiparisDurumKaydi" AS history
                     WHERE history."TedarikciSiparisId" = orders."Id" AND history."YeniDurum" = 'Itirazli'
                       AND history."OncekiDurum" <> history."YeniDurum"
                     ORDER BY history."Id" DESC LIMIT 1), orders."UpdatedAt")
                WHERE orders."Durum" = 'Itirazli';
                UPDATE "TedarikciSiparis" AS orders SET "ItirazIlkYanitAt" =
                    (SELECT MIN(complaint."YanitlandiAt") FROM "TedarikciSiparisSikayeti" AS complaint
                     WHERE complaint."TedarikciSiparisId" = orders."Id" AND complaint."YanitlandiAt" >= orders."ItirazAcildiAt")
                WHERE orders."Durum" = 'Itirazli';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciMalKabul_TedarikciSiparisId_OnayDurumu",
                table: "TedarikciMalKabul",
                columns: new[] { "TedarikciSiparisId", "OnayDurumu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TedarikciMalKabul_TedarikciSiparisId_OnayDurumu",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "ItirazAcildiAt",
                table: "TedarikciSiparis");

            migrationBuilder.DropColumn(
                name: "ItirazIlkYanitAt",
                table: "TedarikciSiparis");

            migrationBuilder.DropColumn(
                name: "ItirazKanitToplandiAt",
                table: "TedarikciSiparis");

            migrationBuilder.DropColumn(
                name: "ItirazYukseltildiAt",
                table: "TedarikciSiparis");

            migrationBuilder.DropColumn(
                name: "BelgeUyusmazligi",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "IkinciOnayAt",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "IkinciOnayNotu",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "IkinciOnaylayanKullaniciRef",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "MiktarDegisikligi",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "OnayDurumu",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "OnayNedeni",
                table: "TedarikciMalKabul");
        }
    }
}
