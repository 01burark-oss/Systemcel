using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MarketplaceReceiptCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TedarikciMalKabulDuzeltmeId",
                table: "StokHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciMalKabulDuzeltmeId",
                table: "PazaryeriDefterKaydi",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciMalKabulDuzeltmeId",
                table: "CariHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TedarikciMalKabulDuzeltme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciMalKabulId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Not = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Durum = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IslemYapanKullaniciRef = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OnaylayanKullaniciRef = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OnayNotu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OnayAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NetTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BrutTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HakEdisAzaltimi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TedarikcidenGeriAlinacakTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaDurumu = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BelgeDurumu = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StokDurumu = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AliciFaturaId = table.Column<int>(type: "integer", nullable: true),
                    SaticiFaturaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciMalKabulDuzeltme", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciMalKabulDuzeltme_Fatura_AliciFaturaId",
                        column: x => x.AliciFaturaId,
                        principalTable: "Fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciMalKabulDuzeltme_Fatura_SaticiFaturaId",
                        column: x => x.SaticiFaturaId,
                        principalTable: "Fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciMalKabulDuzeltme_Isletme_AliciIsletmeId",
                        column: x => x.AliciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciMalKabulDuzeltme_TedarikciMalKabul_TedarikciMalKab~",
                        column: x => x.TedarikciMalKabulId,
                        principalTable: "TedarikciMalKabul",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciMalKabulDuzeltme_TedarikciSiparis_TedarikciSiparis~",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_TedarikciMalKabulDuzeltmeId",
                table: "StokHareket",
                column: "TedarikciMalKabulDuzeltmeId");

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriDefterKaydi_TedarikciMalKabulDuzeltmeId",
                table: "PazaryeriDefterKaydi",
                column: "TedarikciMalKabulDuzeltmeId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_TedarikciMalKabulDuzeltmeId",
                table: "CariHareket",
                column: "TedarikciMalKabulDuzeltmeId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciMalKabulDuzeltme_AliciFaturaId",
                table: "TedarikciMalKabulDuzeltme",
                column: "AliciFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciMalKabulDuzeltme_AliciIsletmeId_IdempotencyAnahtari",
                table: "TedarikciMalKabulDuzeltme",
                columns: new[] { "AliciIsletmeId", "IdempotencyAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciMalKabulDuzeltme_SaticiFaturaId",
                table: "TedarikciMalKabulDuzeltme",
                column: "SaticiFaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciMalKabulDuzeltme_TedarikciMalKabulId",
                table: "TedarikciMalKabulDuzeltme",
                column: "TedarikciMalKabulId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciMalKabulDuzeltme_TedarikciSiparisId_Durum",
                table: "TedarikciMalKabulDuzeltme",
                columns: new[] { "TedarikciSiparisId", "Durum" });

            migrationBuilder.AddForeignKey(
                name: "FK_CariHareket_TedarikciMalKabulDuzeltme_TedarikciMalKabulDuze~",
                table: "CariHareket",
                column: "TedarikciMalKabulDuzeltmeId",
                principalTable: "TedarikciMalKabulDuzeltme",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PazaryeriDefterKaydi_TedarikciMalKabulDuzeltme_TedarikciMal~",
                table: "PazaryeriDefterKaydi",
                column: "TedarikciMalKabulDuzeltmeId",
                principalTable: "TedarikciMalKabulDuzeltme",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareket_TedarikciMalKabulDuzeltme_TedarikciMalKabulDuze~",
                table: "StokHareket",
                column: "TedarikciMalKabulDuzeltmeId",
                principalTable: "TedarikciMalKabulDuzeltme",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CariHareket_TedarikciMalKabulDuzeltme_TedarikciMalKabulDuze~",
                table: "CariHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_PazaryeriDefterKaydi_TedarikciMalKabulDuzeltme_TedarikciMal~",
                table: "PazaryeriDefterKaydi");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareket_TedarikciMalKabulDuzeltme_TedarikciMalKabulDuze~",
                table: "StokHareket");

            migrationBuilder.DropTable(
                name: "TedarikciMalKabulDuzeltme");

            migrationBuilder.DropIndex(
                name: "IX_StokHareket_TedarikciMalKabulDuzeltmeId",
                table: "StokHareket");

            migrationBuilder.DropIndex(
                name: "IX_PazaryeriDefterKaydi_TedarikciMalKabulDuzeltmeId",
                table: "PazaryeriDefterKaydi");

            migrationBuilder.DropIndex(
                name: "IX_CariHareket_TedarikciMalKabulDuzeltmeId",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciMalKabulDuzeltmeId",
                table: "StokHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciMalKabulDuzeltmeId",
                table: "PazaryeriDefterKaydi");

            migrationBuilder.DropColumn(
                name: "TedarikciMalKabulDuzeltmeId",
                table: "CariHareket");
        }
    }
}
