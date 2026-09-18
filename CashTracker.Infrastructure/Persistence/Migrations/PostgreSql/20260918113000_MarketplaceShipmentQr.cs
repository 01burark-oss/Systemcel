using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260918113000_MarketplaceShipmentQr")]
public sealed class MarketplaceShipmentQr : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "SevkEdilenMiktar", "KabulEdilenMiktar", "ReddedilenMiktar" })
        {
            migrationBuilder.AddColumn<decimal>(
                name: column,
                table: "TedarikciSiparisKalemi",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);
        }

        migrationBuilder.CreateTable(
            name: "TedarikciSevkiyat",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                SevkiyatNo = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                TasimaTipi = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Tasiyici = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                BelgeNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AracPlaka = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                SurucuAdi = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                CikisDeposu = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Not = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                PlanlananTeslimAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TedarikciSevkiyat", x => x.Id);
                table.ForeignKey("FK_TedarikciSevkiyat_Isletme_AliciIsletmeId", x => x.AliciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TedarikciSevkiyat_Isletme_TedarikciIsletmeId", x => x.TedarikciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TedarikciSevkiyat_TedarikciSiparis_TedarikciSiparisId", x => x.TedarikciSiparisId, "TedarikciSiparis", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TedarikciSevkiyatKalemi",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TedarikciSevkiyatId = table.Column<int>(type: "integer", nullable: false),
                TedarikciSiparisKalemiId = table.Column<int>(type: "integer", nullable: false),
                Miktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                EtiketSayisi = table.Column<int>(type: "integer", nullable: false),
                LotNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SonKullanmaTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                SicaklikMin = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                SicaklikMax = table.Column<decimal>(type: "numeric(8,2)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TedarikciSevkiyatKalemi", x => x.Id);
                table.ForeignKey("FK_TedarikciSevkiyatKalemi_TedarikciSevkiyat_TedarikciSevkiyatId", x => x.TedarikciSevkiyatId, "TedarikciSevkiyat", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TedarikciSevkiyatKalemi_TedarikciSiparisKalemi_TedarikciSiparisKalemiId", x => x.TedarikciSiparisKalemiId, "TedarikciSiparisKalemi", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TedarikciSevkiyatEtiketi",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TedarikciSevkiyatKalemiId = table.Column<int>(type: "integer", nullable: false),
                KodHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Miktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                OkutulduAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TedarikciSevkiyatEtiketi", x => x.Id);
                table.ForeignKey("FK_TedarikciSevkiyatEtiketi_TedarikciSevkiyatKalemi_TedarikciSevkiyatKalemiId", x => x.TedarikciSevkiyatKalemiId, "TedarikciSevkiyatKalemi", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TedarikciMalKabul",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TedarikciSevkiyatEtiketiId = table.Column<int>(type: "integer", nullable: false),
                TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                IdempotencyAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                KabulEdilenMiktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                ReddedilenMiktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                RedNedeni = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Not = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TedarikciMalKabul", x => x.Id);
                table.ForeignKey("FK_TedarikciMalKabul_Isletme_AliciIsletmeId", x => x.AliciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TedarikciMalKabul_TedarikciSevkiyatEtiketi_TedarikciSevkiyatEtiketiId", x => x.TedarikciSevkiyatEtiketiId, "TedarikciSevkiyatEtiketi", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TedarikciMalKabul_TedarikciSiparis_TedarikciSiparisId", x => x.TedarikciSiparisId, "TedarikciSiparis", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_TedarikciSevkiyat_SevkiyatNo", "TedarikciSevkiyat", "SevkiyatNo", unique: true);
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyat_TedarikciSiparisId_CreatedAt", "TedarikciSevkiyat", new[] { "TedarikciSiparisId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyat_AliciIsletmeId", "TedarikciSevkiyat", "AliciIsletmeId");
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyat_TedarikciIsletmeId", "TedarikciSevkiyat", "TedarikciIsletmeId");
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyatKalemi_TedarikciSevkiyatId", "TedarikciSevkiyatKalemi", "TedarikciSevkiyatId");
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyatKalemi_TedarikciSiparisKalemiId", "TedarikciSevkiyatKalemi", "TedarikciSiparisKalemiId");
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyatEtiketi_KodHash", "TedarikciSevkiyatEtiketi", "KodHash", unique: true);
        migrationBuilder.CreateIndex("IX_TedarikciSevkiyatEtiketi_TedarikciSevkiyatKalemiId", "TedarikciSevkiyatEtiketi", "TedarikciSevkiyatKalemiId");
        migrationBuilder.CreateIndex("IX_TedarikciMalKabul_TedarikciSevkiyatEtiketiId", "TedarikciMalKabul", "TedarikciSevkiyatEtiketiId", unique: true);
        migrationBuilder.CreateIndex("IX_TedarikciMalKabul_AliciIsletmeId_IdempotencyAnahtari", "TedarikciMalKabul", new[] { "AliciIsletmeId", "IdempotencyAnahtari" }, unique: true);
        migrationBuilder.CreateIndex("IX_TedarikciMalKabul_TedarikciSiparisId_CreatedAt", "TedarikciMalKabul", new[] { "TedarikciSiparisId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TedarikciMalKabul");
        migrationBuilder.DropTable("TedarikciSevkiyatEtiketi");
        migrationBuilder.DropTable("TedarikciSevkiyatKalemi");
        migrationBuilder.DropTable("TedarikciSevkiyat");
        foreach (var column in new[] { "SevkEdilenMiktar", "KabulEdilenMiktar", "ReddedilenMiktar" })
            migrationBuilder.DropColumn(column, "TedarikciSiparisKalemi");
    }
}
