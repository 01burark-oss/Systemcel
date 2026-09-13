using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SupplierMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TedarikAlimTalebi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Baslik = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Kategori = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UrunHizmet = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Birim = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TeslimatSehri = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SonTeklifAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikAlimTalebi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikAlimTalebi_Isletme_AliciIsletmeId",
                        column: x => x.AliciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciProfil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Unvan = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Kategoriler = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sehir = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Dogrulandi = table.Column<bool>(type: "boolean", nullable: false),
                    Yayinda = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciProfil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciProfil_Isletme_IsletmeId",
                        column: x => x.IsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TedarikTeklifi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TalepId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TerminGun = table.Column<int>(type: "integer", nullable: false),
                    MinimumSiparis = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Not = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikTeklifi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikTeklifi_Isletme_TedarikciIsletmeId",
                        column: x => x.TedarikciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikTeklifi_TedarikAlimTalebi_TalepId",
                        column: x => x.TalepId,
                        principalTable: "TedarikAlimTalebi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikAlimTalebi_AliciIsletmeId_Durum",
                table: "TedarikAlimTalebi",
                columns: new[] { "AliciIsletmeId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikAlimTalebi_Durum_SonTeklifAt",
                table: "TedarikAlimTalebi",
                columns: new[] { "Durum", "SonTeklifAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciProfil_IsletmeId",
                table: "TedarikciProfil",
                column: "IsletmeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciProfil_Yayinda_Dogrulandi",
                table: "TedarikciProfil",
                columns: new[] { "Yayinda", "Dogrulandi" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikTeklifi_TalepId_TedarikciIsletmeId",
                table: "TedarikTeklifi",
                columns: new[] { "TalepId", "TedarikciIsletmeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikTeklifi_TedarikciIsletmeId",
                table: "TedarikTeklifi",
                column: "TedarikciIsletmeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TedarikciProfil");

            migrationBuilder.DropTable(
                name: "TedarikTeklifi");

            migrationBuilder.DropTable(
                name: "TedarikAlimTalebi");

        }
    }
}
