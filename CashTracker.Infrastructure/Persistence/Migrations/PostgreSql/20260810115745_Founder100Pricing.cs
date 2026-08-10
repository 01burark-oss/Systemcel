using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class Founder100Pricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IndirimliDonemSayisi",
                table: "OdemeIslemi",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KampanyaKodu",
                table: "OdemeIslemi",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ListeNetTutar",
                table: "OdemeIslemi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YenilemeNetTutar",
                table: "OdemeIslemi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "KampanyaKodu",
                table: "AbonelikOnayi",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ListeNetTutar",
                table: "AbonelikOnayi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YenilemeNetTutar",
                table: "AbonelikOnayi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IndirimliDonemKalan",
                table: "Abonelik",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KampanyaKodu",
                table: "Abonelik",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "YenilemeDonemTutari",
                table: "Abonelik",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "OdemeIslemi"
                SET "ListeNetTutar" = "NetTutar",
                    "YenilemeNetTutar" = "NetTutar"
                WHERE "ListeNetTutar" = 0 AND "YenilemeNetTutar" = 0;

                UPDATE "AbonelikOnayi"
                SET "ListeNetTutar" = "NetTutar",
                    "YenilemeNetTutar" = "NetTutar"
                WHERE "ListeNetTutar" = 0 AND "YenilemeNetTutar" = 0;

                UPDATE "Abonelik"
                SET "YenilemeDonemTutari" = "DonemTutari"
                WHERE "YenilemeDonemTutari" = 0;
                """);

            migrationBuilder.CreateTable(
                name: "KurucuKampanyaHakki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    KampanyaKodu = table.Column<string>(type: "text", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    CheckoutAnahtari = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    RezerveAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RezervasyonBitisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KazanildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KurucuKampanyaHakki", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KurucuKampanyaHakki_CheckoutAnahtari",
                table: "KurucuKampanyaHakki",
                column: "CheckoutAnahtari",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KurucuKampanyaHakki_KampanyaKodu_Durum_RezervasyonBitisAt",
                table: "KurucuKampanyaHakki",
                columns: new[] { "KampanyaKodu", "Durum", "RezervasyonBitisAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KurucuKampanyaHakki_KampanyaKodu_IsletmeId",
                table: "KurucuKampanyaHakki",
                columns: new[] { "KampanyaKodu", "IsletmeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KurucuKampanyaHakki_KampanyaKodu_SiraNo",
                table: "KurucuKampanyaHakki",
                columns: new[] { "KampanyaKodu", "SiraNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KurucuKampanyaHakki");

            migrationBuilder.DropColumn(
                name: "IndirimliDonemSayisi",
                table: "OdemeIslemi");

            migrationBuilder.DropColumn(
                name: "KampanyaKodu",
                table: "OdemeIslemi");

            migrationBuilder.DropColumn(
                name: "ListeNetTutar",
                table: "OdemeIslemi");

            migrationBuilder.DropColumn(
                name: "YenilemeNetTutar",
                table: "OdemeIslemi");

            migrationBuilder.DropColumn(
                name: "KampanyaKodu",
                table: "AbonelikOnayi");

            migrationBuilder.DropColumn(
                name: "ListeNetTutar",
                table: "AbonelikOnayi");

            migrationBuilder.DropColumn(
                name: "YenilemeNetTutar",
                table: "AbonelikOnayi");

            migrationBuilder.DropColumn(
                name: "IndirimliDonemKalan",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "KampanyaKodu",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "YenilemeDonemTutari",
                table: "Abonelik");
        }
    }
}
