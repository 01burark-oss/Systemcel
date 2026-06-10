using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260527090000_AccountantMarketplace")]
    public partial class AccountantMarketplace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "KabulAt",
                table: "MuhasebeciMusteri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kaynak",
                table: "MuhasebeciMusteri",
                type: "text",
                nullable: false,
                defaultValue: "Davet");

            migrationBuilder.AddColumn<int>(
                name: "TalepId",
                table: "MuhasebeciMusteri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YetkiSeviyesi",
                table: "MuhasebeciMusteri",
                type: "text",
                nullable: false,
                defaultValue: "OkumaRapor");

            migrationBuilder.CreateTable(
                name: "MuhasebeciProfil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Yayinda = table.Column<bool>(type: "boolean", nullable: false),
                    Unvan = table.Column<string>(type: "text", nullable: false),
                    Konum = table.Column<string>(type: "text", nullable: false),
                    Uzmanliklar = table.Column<string>(type: "text", nullable: false),
                    MusteriTipleri = table.Column<string>(type: "text", nullable: false),
                    KisaAciklama = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciProfil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MuhasebeciMusteriTalebi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: true),
                    TalepEdenIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Tur = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    YetkiSeviyesi = table.Column<string>(type: "text", nullable: false),
                    DavetKodu = table.Column<string>(type: "text", nullable: false),
                    Mesaj = table.Column<string>(type: "text", nullable: false),
                    SonucAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciMusteriTalebi", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteri_TalepId",
                table: "MuhasebeciMusteri",
                column: "TalepId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteri_YetkiSeviyesi",
                table: "MuhasebeciMusteri",
                column: "YetkiSeviyesi");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciProfil_Konum",
                table: "MuhasebeciProfil",
                column: "Konum");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciProfil_MuhasebeciIsletmeId",
                table: "MuhasebeciProfil",
                column: "MuhasebeciIsletmeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciProfil_Yayinda",
                table: "MuhasebeciProfil",
                column: "Yayinda");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteriTalebi_DavetKodu",
                table: "MuhasebeciMusteriTalebi",
                column: "DavetKodu");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteriTalebi_Durum",
                table: "MuhasebeciMusteriTalebi",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteriTalebi_MuhasebeciIsletmeId",
                table: "MuhasebeciMusteriTalebi",
                column: "MuhasebeciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteriTalebi_MusteriIsletmeId",
                table: "MuhasebeciMusteriTalebi",
                column: "MusteriIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteriTalebi_TalepEdenIsletmeId",
                table: "MuhasebeciMusteriTalebi",
                column: "TalepEdenIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteriTalebi_MuhasebeciIsletmeId_MusteriIsletmeId_Durum",
                table: "MuhasebeciMusteriTalebi",
                columns: new[] { "MuhasebeciIsletmeId", "MusteriIsletmeId", "Durum" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuhasebeciMusteriTalebi");

            migrationBuilder.DropTable(
                name: "MuhasebeciProfil");

            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciMusteri_TalepId",
                table: "MuhasebeciMusteri");

            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciMusteri_YetkiSeviyesi",
                table: "MuhasebeciMusteri");

            migrationBuilder.DropColumn(
                name: "KabulAt",
                table: "MuhasebeciMusteri");

            migrationBuilder.DropColumn(
                name: "Kaynak",
                table: "MuhasebeciMusteri");

            migrationBuilder.DropColumn(
                name: "TalepId",
                table: "MuhasebeciMusteri");

            migrationBuilder.DropColumn(
                name: "YetkiSeviyesi",
                table: "MuhasebeciMusteri");
        }
    }
}
