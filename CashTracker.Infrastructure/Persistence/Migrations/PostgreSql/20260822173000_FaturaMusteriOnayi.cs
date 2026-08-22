using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260822173000_FaturaMusteriOnayi")]
    public partial class FaturaMusteriOnayi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaturaMusteriOnayi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Durum = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsletmeAdi = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CariUnvan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CariVergiNoMaskeli = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CariAdres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AliciTelefonMaskeli = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FaturaNo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FaturaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FaturaToplami = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Saglayici = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SaglayiciIslemId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Hata = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GonderildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SonGecerlilikAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    YanitAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    YanitNotu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IstemciIpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaturaMusteriOnayi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaturaMusteriOnayi_Fatura_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaturaMusteriOnayi_TokenHash",
                table: "FaturaMusteriOnayi",
                column: "TokenHash",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_FaturaMusteriOnayi_IsletmeId_FaturaId_CreatedAt",
                table: "FaturaMusteriOnayi",
                columns: new[] { "IsletmeId", "FaturaId", "CreatedAt" });
            migrationBuilder.CreateIndex(
                name: "IX_FaturaMusteriOnayi_IsletmeId_Durum_SonGecerlilikAt",
                table: "FaturaMusteriOnayi",
                columns: new[] { "IsletmeId", "Durum", "SonGecerlilikAt" });
            migrationBuilder.CreateIndex(
                name: "IX_FaturaMusteriOnayi_FaturaId",
                table: "FaturaMusteriOnayi",
                column: "FaturaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FaturaMusteriOnayi");
        }
    }
}
