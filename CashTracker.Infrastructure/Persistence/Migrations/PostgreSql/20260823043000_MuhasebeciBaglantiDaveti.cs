using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260823043000_MuhasebeciBaglantiDaveti")]
    public partial class MuhasebeciBaglantiDaveti : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuhasebeciBaglantiDaveti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: true),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    YetkiSeviyesi = table.Column<string>(type: "text", nullable: false),
                    Mesaj = table.Column<string>(type: "text", nullable: false),
                    SonGecerlilikAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    KabulAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciBaglantiDaveti", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciBaglantiDaveti_MuhasebeciIsletmeId",
                table: "MuhasebeciBaglantiDaveti",
                column: "MuhasebeciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciBaglantiDaveti_MusteriIsletmeId",
                table: "MuhasebeciBaglantiDaveti",
                column: "MusteriIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciBaglantiDaveti_MusteriIsletmeId_Durum",
                table: "MuhasebeciBaglantiDaveti",
                columns: new[] { "MusteriIsletmeId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciBaglantiDaveti_TokenHash",
                table: "MuhasebeciBaglantiDaveti",
                column: "TokenHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MuhasebeciBaglantiDaveti");
        }
    }
}
