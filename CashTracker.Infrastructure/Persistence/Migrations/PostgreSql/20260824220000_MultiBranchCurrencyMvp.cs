using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MultiBranchCurrencyMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KurSnapshot",
                table: "UrunHizmet",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "UrunHizmet",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "UrunHizmet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KurSnapshot",
                table: "TahsilatOdeme",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "TahsilatOdeme",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "TahsilatOdeme",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TryKarsiligi",
                table: "TahsilatOdeme",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "StokHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "StokDepo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KurSnapshot",
                table: "Kasa",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "OrijinalTutar",
                table: "Kasa",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "Kasa",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "Kasa",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TryKarsiligi",
                table: "Kasa",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GenelToplamTry",
                table: "Fatura",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KurSnapshot",
                table: "Fatura",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "Fatura",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "Fatura",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KurSnapshot",
                table: "CariHareket",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "CariHareket",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<int>(
                name: "SubeId",
                table: "CariHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TryKarsiligi",
                table: "CariHareket",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DovizKuru",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Kur = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    GecerliAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OlusturmaAnahtari = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IcerikOzeti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DovizKuru", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DovizKuru_Isletme_IsletmeId",
                        column: x => x.IsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("UPDATE \"Kasa\" SET \"OrijinalTutar\" = \"Tutar\", \"TryKarsiligi\" = \"Tutar\"");
            migrationBuilder.Sql("UPDATE \"Fatura\" SET \"GenelToplamTry\" = \"GenelToplam\"");
            migrationBuilder.Sql("UPDATE \"TahsilatOdeme\" SET \"TryKarsiligi\" = \"Tutar\"");
            migrationBuilder.Sql("UPDATE \"CariHareket\" SET \"TryKarsiligi\" = \"Tutar\"");

            migrationBuilder.CreateTable(
                name: "Sube",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Kod = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Varsayilan = table.Column<bool>(type: "boolean", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaAnahtari = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IcerikOzeti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sube", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sube_Isletme_IsletmeId",
                        column: x => x.IsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrunHizmet_IsletmeId_SubeId_Ad",
                table: "UrunHizmet",
                columns: new[] { "IsletmeId", "SubeId", "Ad" });

            migrationBuilder.CreateIndex(
                name: "IX_UrunHizmet_SubeId",
                table: "UrunHizmet",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_IsletmeId_SubeId_Tarih",
                table: "TahsilatOdeme",
                columns: new[] { "IsletmeId", "SubeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_SubeId",
                table: "TahsilatOdeme",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_IsletmeId_SubeId_Tarih",
                table: "StokHareket",
                columns: new[] { "IsletmeId", "SubeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_SubeId",
                table: "StokHareket",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_StokDepo_IsletmeId_SubeId",
                table: "StokDepo",
                columns: new[] { "IsletmeId", "SubeId" });

            migrationBuilder.CreateIndex(
                name: "IX_StokDepo_SubeId",
                table: "StokDepo",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_IsletmeId_SubeId_Tarih",
                table: "Kasa",
                columns: new[] { "IsletmeId", "SubeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_SubeId",
                table: "Kasa",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_IsletmeId_SubeId_Tarih",
                table: "Fatura",
                columns: new[] { "IsletmeId", "SubeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_SubeId",
                table: "Fatura",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_IsletmeId_SubeId_Tarih",
                table: "CariHareket",
                columns: new[] { "IsletmeId", "SubeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_SubeId",
                table: "CariHareket",
                column: "SubeId");

            migrationBuilder.CreateIndex(
                name: "IX_DovizKuru_IsletmeId_OlusturmaAnahtari",
                table: "DovizKuru",
                columns: new[] { "IsletmeId", "OlusturmaAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DovizKuru_IsletmeId_ParaBirimi_GecerliAt",
                table: "DovizKuru",
                columns: new[] { "IsletmeId", "ParaBirimi", "GecerliAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sube_IsletmeId_Kod",
                table: "Sube",
                columns: new[] { "IsletmeId", "Kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sube_IsletmeId_OlusturmaAnahtari",
                table: "Sube",
                columns: new[] { "IsletmeId", "OlusturmaAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sube_IsletmeId_Varsayilan",
                table: "Sube",
                columns: new[] { "IsletmeId", "Varsayilan" });

            migrationBuilder.AddForeignKey(
                name: "FK_CariHareket_Sube_SubeId",
                table: "CariHareket",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fatura_Sube_SubeId",
                table: "Fatura",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kasa_Sube_SubeId",
                table: "Kasa",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StokDepo_Sube_SubeId",
                table: "StokDepo",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareket_Sube_SubeId",
                table: "StokHareket",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TahsilatOdeme_Sube_SubeId",
                table: "TahsilatOdeme",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UrunHizmet_Sube_SubeId",
                table: "UrunHizmet",
                column: "SubeId",
                principalTable: "Sube",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CariHareket_Sube_SubeId",
                table: "CariHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_Fatura_Sube_SubeId",
                table: "Fatura");

            migrationBuilder.DropForeignKey(
                name: "FK_Kasa_Sube_SubeId",
                table: "Kasa");

            migrationBuilder.DropForeignKey(
                name: "FK_StokDepo_Sube_SubeId",
                table: "StokDepo");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareket_Sube_SubeId",
                table: "StokHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_TahsilatOdeme_Sube_SubeId",
                table: "TahsilatOdeme");

            migrationBuilder.DropForeignKey(
                name: "FK_UrunHizmet_Sube_SubeId",
                table: "UrunHizmet");

            migrationBuilder.DropTable(
                name: "DovizKuru");

            migrationBuilder.DropTable(
                name: "Sube");

            migrationBuilder.DropIndex(
                name: "IX_UrunHizmet_IsletmeId_SubeId_Ad",
                table: "UrunHizmet");

            migrationBuilder.DropIndex(
                name: "IX_UrunHizmet_SubeId",
                table: "UrunHizmet");

            migrationBuilder.DropIndex(
                name: "IX_TahsilatOdeme_IsletmeId_SubeId_Tarih",
                table: "TahsilatOdeme");

            migrationBuilder.DropIndex(
                name: "IX_TahsilatOdeme_SubeId",
                table: "TahsilatOdeme");

            migrationBuilder.DropIndex(
                name: "IX_StokHareket_IsletmeId_SubeId_Tarih",
                table: "StokHareket");

            migrationBuilder.DropIndex(
                name: "IX_StokHareket_SubeId",
                table: "StokHareket");

            migrationBuilder.DropIndex(
                name: "IX_StokDepo_IsletmeId_SubeId",
                table: "StokDepo");

            migrationBuilder.DropIndex(
                name: "IX_StokDepo_SubeId",
                table: "StokDepo");

            migrationBuilder.DropIndex(
                name: "IX_Kasa_IsletmeId_SubeId_Tarih",
                table: "Kasa");

            migrationBuilder.DropIndex(
                name: "IX_Kasa_SubeId",
                table: "Kasa");

            migrationBuilder.DropIndex(
                name: "IX_Fatura_IsletmeId_SubeId_Tarih",
                table: "Fatura");

            migrationBuilder.DropIndex(
                name: "IX_Fatura_SubeId",
                table: "Fatura");

            migrationBuilder.DropIndex(
                name: "IX_CariHareket_IsletmeId_SubeId_Tarih",
                table: "CariHareket");

            migrationBuilder.DropIndex(
                name: "IX_CariHareket_SubeId",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "KurSnapshot",
                table: "UrunHizmet");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "UrunHizmet");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "UrunHizmet");

            migrationBuilder.DropColumn(
                name: "KurSnapshot",
                table: "TahsilatOdeme");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "TahsilatOdeme");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "TahsilatOdeme");

            migrationBuilder.DropColumn(
                name: "TryKarsiligi",
                table: "TahsilatOdeme");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "StokHareket");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "StokDepo");

            migrationBuilder.DropColumn(
                name: "KurSnapshot",
                table: "Kasa");

            migrationBuilder.DropColumn(
                name: "OrijinalTutar",
                table: "Kasa");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "Kasa");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "Kasa");

            migrationBuilder.DropColumn(
                name: "TryKarsiligi",
                table: "Kasa");

            migrationBuilder.DropColumn(
                name: "GenelToplamTry",
                table: "Fatura");

            migrationBuilder.DropColumn(
                name: "KurSnapshot",
                table: "Fatura");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "Fatura");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "Fatura");

            migrationBuilder.DropColumn(
                name: "KurSnapshot",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "SubeId",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "TryKarsiligi",
                table: "CariHareket");
        }
    }
}
