using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MarketplaceMoneyIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PazaryeriParaTalimati",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: true),
                    PazaryeriOdemeId = table.Column<int>(type: "integer", nullable: true),
                    Tur = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Saglayici = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IdempotencyAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KaynakRef = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SaglayiciIslemId = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    SonHataKodu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DenemeSayisi = table.Column<int>(type: "integer", nullable: false),
                    GonderimBasladiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SonuclandiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PazaryeriParaTalimati", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PazaryeriParaTalimati_Isletme_AliciIsletmeId",
                        column: x => x.AliciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PazaryeriParaTalimati_PazaryeriOdeme_PazaryeriOdemeId",
                        column: x => x.PazaryeriOdemeId,
                        principalTable: "PazaryeriOdeme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PazaryeriParaTalimati_TedarikciSiparis_TedarikciSiparisId",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriParaTalimati_AliciIsletmeId",
                table: "PazaryeriParaTalimati",
                column: "AliciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriParaTalimati_Durum_CreatedAt",
                table: "PazaryeriParaTalimati",
                columns: new[] { "Durum", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriParaTalimati_PazaryeriOdemeId",
                table: "PazaryeriParaTalimati",
                column: "PazaryeriOdemeId");

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriParaTalimati_Saglayici_Tur_IdempotencyAnahtari",
                table: "PazaryeriParaTalimati",
                columns: new[] { "Saglayici", "Tur", "IdempotencyAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriParaTalimati_TedarikciSiparisId_Tur",
                table: "PazaryeriParaTalimati",
                columns: new[] { "TedarikciSiparisId", "Tur" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PazaryeriParaTalimati");
        }
    }
}
