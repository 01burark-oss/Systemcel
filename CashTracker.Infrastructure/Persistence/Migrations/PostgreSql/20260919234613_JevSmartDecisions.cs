using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class JevSmartDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CariKartId",
                table: "Kasa",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UrunEslesmeTakmaAdi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    UrunHizmetId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: true),
                    KaynakMetin = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Anahtar = table.Column<string>(type: "character varying(560)", maxLength: 560, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrunEslesmeTakmaAdi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrunEslesmeTakmaAdi_CariKart_CariKartId",
                        column: x => x.CariKartId,
                        principalTable: "CariKart",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UrunEslesmeTakmaAdi_Isletme_IsletmeId",
                        column: x => x.IsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UrunEslesmeTakmaAdi_UrunHizmet_UrunHizmetId",
                        column: x => x.UrunHizmetId,
                        principalTable: "UrunHizmet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_CariKartId",
                table: "Kasa",
                column: "CariKartId");

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_IsletmeId_CariKartId_Tarih",
                table: "Kasa",
                columns: new[] { "IsletmeId", "CariKartId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_UrunEslesmeTakmaAdi_CariKartId",
                table: "UrunEslesmeTakmaAdi",
                column: "CariKartId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunEslesmeTakmaAdi_IsletmeId_Anahtar",
                table: "UrunEslesmeTakmaAdi",
                columns: new[] { "IsletmeId", "Anahtar" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrunEslesmeTakmaAdi_IsletmeId_UrunHizmetId",
                table: "UrunEslesmeTakmaAdi",
                columns: new[] { "IsletmeId", "UrunHizmetId" });

            migrationBuilder.CreateIndex(
                name: "IX_UrunEslesmeTakmaAdi_UrunHizmetId",
                table: "UrunEslesmeTakmaAdi",
                column: "UrunHizmetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kasa_CariKart_CariKartId",
                table: "Kasa",
                column: "CariKartId",
                principalTable: "CariKart",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kasa_CariKart_CariKartId",
                table: "Kasa");

            migrationBuilder.DropTable(
                name: "UrunEslesmeTakmaAdi");

            migrationBuilder.DropIndex(
                name: "IX_Kasa_CariKartId",
                table: "Kasa");

            migrationBuilder.DropIndex(
                name: "IX_Kasa_IsletmeId_CariKartId_Tarih",
                table: "Kasa");

            migrationBuilder.DropColumn(
                name: "CariKartId",
                table: "Kasa");
        }
    }
}
