using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AdvancedFinancialVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NakitPlanKalemi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tip = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Tutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    IlkTarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TekrarTipi = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TekrarAraligi = table.Column<int>(type: "integer", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Kategori = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NakitPlanKalemi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NakitPlanKalemi_Isletme_IsletmeId",
                        column: x => x.IsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_IsletmeId_FaturaId_Tarih",
                table: "TahsilatOdeme",
                columns: new[] { "IsletmeId", "FaturaId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_IsletmeId_FaturaTipi_Durum_VadeTarihi",
                table: "Fatura",
                columns: new[] { "IsletmeId", "FaturaTipi", "Durum", "VadeTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_NakitPlanKalemi_IsletmeId",
                table: "NakitPlanKalemi",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_NakitPlanKalemi_IsletmeId_Aktif_IlkTarih",
                table: "NakitPlanKalemi",
                columns: new[] { "IsletmeId", "Aktif", "IlkTarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NakitPlanKalemi");

            migrationBuilder.DropIndex(
                name: "IX_TahsilatOdeme_IsletmeId_FaturaId_Tarih",
                table: "TahsilatOdeme");

            migrationBuilder.DropIndex(
                name: "IX_Fatura_IsletmeId_FaturaTipi_Durum_VadeTarihi",
                table: "Fatura");
        }
    }
}
