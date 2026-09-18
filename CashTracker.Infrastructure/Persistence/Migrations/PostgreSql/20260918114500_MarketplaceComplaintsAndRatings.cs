using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260918114500_MarketplaceComplaintsAndRatings")]
public sealed class MarketplaceComplaintsAndRatings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TedarikciSiparisSikayeti",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                Kategori = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Aciklama = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Talep = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                TedarikciYaniti = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                KapanisNotu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                YanitlandiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                KapatildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TedarikciSiparisSikayeti", x => x.Id);
                table.ForeignKey("FK_TedarikciSiparisSikayeti_TedarikciSiparis_TedarikciSiparisId", x => x.TedarikciSiparisId, "TedarikciSiparis", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TedarikciSiparisSikayeti_Isletme_AliciIsletmeId", x => x.AliciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TedarikciSiparisSikayeti_Isletme_TedarikciIsletmeId", x => x.TedarikciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TedarikciDegerlendirmesi",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                UrunUygunluguPuani = table.Column<int>(type: "integer", nullable: false),
                EksiksizTeslimatPuani = table.Column<int>(type: "integer", nullable: false),
                HasarsizTeslimatPuani = table.Column<int>(type: "integer", nullable: false),
                ZamanindaTeslimatPuani = table.Column<int>(type: "integer", nullable: false),
                SorunCozmePuani = table.Column<int>(type: "integer", nullable: true),
                OrtalamaPuan = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                Yorum = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TedarikciDegerlendirmesi", x => x.Id);
                table.ForeignKey("FK_TedarikciDegerlendirmesi_TedarikciSiparis_TedarikciSiparisId", x => x.TedarikciSiparisId, "TedarikciSiparis", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TedarikciDegerlendirmesi_Isletme_AliciIsletmeId", x => x.AliciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TedarikciDegerlendirmesi_Isletme_TedarikciIsletmeId", x => x.TedarikciIsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_TedarikciSiparisSikayeti_TedarikciSiparisId", "TedarikciSiparisSikayeti", "TedarikciSiparisId", unique: true);
        migrationBuilder.CreateIndex("IX_TedarikciSiparisSikayeti_TedarikciIsletmeId_Durum", "TedarikciSiparisSikayeti", new[] { "TedarikciIsletmeId", "Durum" });
        migrationBuilder.CreateIndex("IX_TedarikciSiparisSikayeti_AliciIsletmeId_CreatedAt", "TedarikciSiparisSikayeti", new[] { "AliciIsletmeId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_TedarikciDegerlendirmesi_TedarikciSiparisId", "TedarikciDegerlendirmesi", "TedarikciSiparisId", unique: true);
        migrationBuilder.CreateIndex("IX_TedarikciDegerlendirmesi_TedarikciIsletmeId_CreatedAt", "TedarikciDegerlendirmesi", new[] { "TedarikciIsletmeId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_TedarikciDegerlendirmesi_AliciIsletmeId_CreatedAt", "TedarikciDegerlendirmesi", new[] { "AliciIsletmeId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TedarikciDegerlendirmesi");
        migrationBuilder.DropTable("TedarikciSiparisSikayeti");
    }
}
