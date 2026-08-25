using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260824200000_AdvancedStockLedger")]
public partial class AdvancedStockLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StokDepo",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IsletmeId = table.Column<int>(type: "integer", nullable: false),
                Ad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Kod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Konum = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                Varsayilan = table.Column<bool>(type: "boolean", nullable: false),
                Aktif = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StokDepo", x => x.Id);
                table.ForeignKey("FK_StokDepo_Isletme_IsletmeId", x => x.IsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StokDefterIslemi",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IsletmeId = table.Column<int>(type: "integer", nullable: false),
                IslemAnahtari = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                IcerikOzeti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                IslemTipi = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                TersKayitKaynakIslemId = table.Column<int>(type: "integer", nullable: true),
                Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StokDefterIslemi", x => x.Id);
                table.ForeignKey("FK_StokDefterIslemi_Isletme_IsletmeId", x => x.IsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_StokDefterIslemi_StokDefterIslemi_TersKayitKaynakIslemId", x => x.TersKayitKaynakIslemId, "StokDefterIslemi", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<int>(name: "DepoId", table: "StokHareket", type: "integer", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "RezerveMiktar", table: "StokHareket", type: "NUMERIC", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<int>(name: "StokDefterIslemiId", table: "StokHareket", type: "integer", nullable: true);

        migrationBuilder.CreateIndex("IX_StokDepo_IsletmeId_Kod", "StokDepo", new[] { "IsletmeId", "Kod" }, unique: true);
        migrationBuilder.CreateIndex("IX_StokDepo_IsletmeId_Varsayilan", "StokDepo", new[] { "IsletmeId", "Varsayilan" });
        migrationBuilder.CreateIndex("IX_StokDefterIslemi_IsletmeId_IslemAnahtari", "StokDefterIslemi", new[] { "IsletmeId", "IslemAnahtari" }, unique: true);
        migrationBuilder.CreateIndex("IX_StokDefterIslemi_IsletmeId_TersKayitKaynakIslemId", "StokDefterIslemi", new[] { "IsletmeId", "TersKayitKaynakIslemId" }, unique: true);
        migrationBuilder.CreateIndex("IX_StokDefterIslemi_TersKayitKaynakIslemId", "StokDefterIslemi", "TersKayitKaynakIslemId");
        migrationBuilder.CreateIndex("IX_StokHareket_DepoId", "StokHareket", "DepoId");
        migrationBuilder.CreateIndex("IX_StokHareket_IsletmeId_DepoId_UrunHizmetId", "StokHareket", new[] { "IsletmeId", "DepoId", "UrunHizmetId" });
        migrationBuilder.CreateIndex("IX_StokHareket_StokDefterIslemiId", "StokHareket", "StokDefterIslemiId");

        migrationBuilder.AddForeignKey("FK_StokHareket_StokDepo_DepoId", "StokHareket", "DepoId", "StokDepo", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("FK_StokHareket_StokDefterIslemi_StokDefterIslemiId", "StokHareket", "StokDefterIslemiId", "StokDefterIslemi", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("""
            INSERT INTO "StokDepo" ("IsletmeId", "Ad", "Kod", "Konum", "Varsayilan", "Aktif", "CreatedAt", "UpdatedAt")
            SELECT "Id", 'Merkez Depo', 'MERKEZ', NULL, TRUE, TRUE, NOW(), NOW()
            FROM "Isletme";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_StokHareket_StokDepo_DepoId", "StokHareket");
        migrationBuilder.DropForeignKey("FK_StokHareket_StokDefterIslemi_StokDefterIslemiId", "StokHareket");
        migrationBuilder.DropIndex("IX_StokHareket_IsletmeId_DepoId_UrunHizmetId", "StokHareket");
        migrationBuilder.DropIndex("IX_StokHareket_StokDefterIslemiId", "StokHareket");
        migrationBuilder.DropColumn("DepoId", "StokHareket");
        migrationBuilder.DropColumn("RezerveMiktar", "StokHareket");
        migrationBuilder.DropColumn("StokDefterIslemiId", "StokHareket");
        migrationBuilder.DropTable("StokDepo");
        migrationBuilder.DropTable("StokDefterIslemi");
    }
}
