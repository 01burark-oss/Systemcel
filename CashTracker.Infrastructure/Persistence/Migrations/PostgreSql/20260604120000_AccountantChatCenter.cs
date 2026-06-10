using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260604120000_AccountantChatCenter")]
    public partial class AccountantChatCenter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuhasebeciSohbet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    TalepId = table.Column<int>(type: "integer", nullable: true),
                    BaglantiId = table.Column<int>(type: "integer", nullable: true),
                    Konu = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Durum = table.Column<string>(type: "text", nullable: false, defaultValue: "Aktif"),
                    SonMesajAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciSohbet", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "SohbetId",
                table: "MuhasebeciSohbetMesaji",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MesajTipi",
                table: "MuhasebeciSohbetMesaji",
                type: "text",
                nullable: false,
                defaultValue: "Metin");

            migrationBuilder.AddColumn<string>(
                name: "ClientMessageId",
                table: "MuhasebeciSohbetMesaji",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MuhasebeciSohbetEki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SohbetId = table.Column<int>(type: "integer", nullable: false),
                    MesajId = table.Column<int>(type: "integer", nullable: true),
                    YukleyenIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    EkTipi = table.Column<string>(type: "text", nullable: false, defaultValue: "Dosya"),
                    DosyaAdi = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    IcerikTipi = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    DosyaYolu = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Boyut = table.Column<long>(type: "bigint", nullable: false),
                    VeriTipi = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Baslik = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    OzetJson = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciSohbetEki", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MuhasebeciSohbetKatilimciDurumu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SohbetId = table.Column<int>(type: "integer", nullable: false),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Arsivlendi = table.Column<bool>(type: "boolean", nullable: false),
                    ArsivlendiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SonOkumaAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciSohbetKatilimciDurumu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MuhasebeciSohbetVeriIstegi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SohbetId = table.Column<int>(type: "integer", nullable: false),
                    IsteyenIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    HedefIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    VeriTipi = table.Column<string>(type: "text", nullable: false, defaultValue: "GelirGiderOzeti"),
                    AralikKodu = table.Column<string>(type: "text", nullable: false, defaultValue: "last30"),
                    Baslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Bitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false, defaultValue: "Beklemede"),
                    SonucEkId = table.Column<int>(type: "integer", nullable: true),
                    Mesaj = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciSohbetVeriIstegi", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbet_MuhasebeciIsletmeId", table: "MuhasebeciSohbet", column: "MuhasebeciIsletmeId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbet_MusteriIsletmeId", table: "MuhasebeciSohbet", column: "MusteriIsletmeId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbet_TalepId", table: "MuhasebeciSohbet", column: "TalepId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbet_BaglantiId", table: "MuhasebeciSohbet", column: "BaglantiId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbet_SonMesajAt", table: "MuhasebeciSohbet", column: "SonMesajAt");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbet_MuhasebeciIsletmeId_MusteriIsletmeId", table: "MuhasebeciSohbet", columns: new[] { "MuhasebeciIsletmeId", "MusteriIsletmeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetMesaji_SohbetId", table: "MuhasebeciSohbetMesaji", column: "SohbetId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetMesaji_SohbetId_ClientMessageId", table: "MuhasebeciSohbetMesaji", columns: new[] { "SohbetId", "ClientMessageId" });
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetMesaji_SohbetId_Id", table: "MuhasebeciSohbetMesaji", columns: new[] { "SohbetId", "Id" });
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetEki_SohbetId", table: "MuhasebeciSohbetEki", column: "SohbetId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetEki_MesajId", table: "MuhasebeciSohbetEki", column: "MesajId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetEki_YukleyenIsletmeId", table: "MuhasebeciSohbetEki", column: "YukleyenIsletmeId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetEki_EkTipi", table: "MuhasebeciSohbetEki", column: "EkTipi");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetKatilimciDurumu_SohbetId", table: "MuhasebeciSohbetKatilimciDurumu", column: "SohbetId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetKatilimciDurumu_IsletmeId", table: "MuhasebeciSohbetKatilimciDurumu", column: "IsletmeId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetKatilimciDurumu_Arsivlendi", table: "MuhasebeciSohbetKatilimciDurumu", column: "Arsivlendi");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetKatilimciDurumu_SohbetId_IsletmeId", table: "MuhasebeciSohbetKatilimciDurumu", columns: new[] { "SohbetId", "IsletmeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetVeriIstegi_SohbetId", table: "MuhasebeciSohbetVeriIstegi", column: "SohbetId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetVeriIstegi_IsteyenIsletmeId", table: "MuhasebeciSohbetVeriIstegi", column: "IsteyenIsletmeId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetVeriIstegi_HedefIsletmeId", table: "MuhasebeciSohbetVeriIstegi", column: "HedefIsletmeId");
            migrationBuilder.CreateIndex(name: "IX_MuhasebeciSohbetVeriIstegi_Durum", table: "MuhasebeciSohbetVeriIstegi", column: "Durum");

            migrationBuilder.Sql(@"
INSERT INTO ""MuhasebeciSohbet"" (""MuhasebeciIsletmeId"", ""MusteriIsletmeId"", ""TalepId"", ""BaglantiId"", ""Konu"", ""Durum"", ""SonMesajAt"", ""CreatedAt"", ""UpdatedAt"")
SELECT m.""MuhasebeciIsletmeId"",
       m.""MusteriIsletmeId"",
       MAX(m.""TalepId""),
       MAX(m.""BaglantiId""),
       '',
       'Aktif',
       MAX(m.""CreatedAt""),
       MIN(m.""CreatedAt""),
       MAX(m.""CreatedAt"")
FROM ""MuhasebeciSohbetMesaji"" m
WHERE NOT EXISTS (
    SELECT 1 FROM ""MuhasebeciSohbet"" s
    WHERE s.""MuhasebeciIsletmeId"" = m.""MuhasebeciIsletmeId""
      AND s.""MusteriIsletmeId"" = m.""MusteriIsletmeId""
)
GROUP BY m.""MuhasebeciIsletmeId"", m.""MusteriIsletmeId"";");

            migrationBuilder.Sql(@"
UPDATE ""MuhasebeciSohbetMesaji"" m
SET ""SohbetId"" = s.""Id""
FROM ""MuhasebeciSohbet"" s
WHERE m.""SohbetId"" IS NULL
  AND s.""MuhasebeciIsletmeId"" = m.""MuhasebeciIsletmeId""
  AND s.""MusteriIsletmeId"" = m.""MusteriIsletmeId"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MuhasebeciSohbetVeriIstegi");
            migrationBuilder.DropTable(name: "MuhasebeciSohbetKatilimciDurumu");
            migrationBuilder.DropTable(name: "MuhasebeciSohbetEki");
            migrationBuilder.DropTable(name: "MuhasebeciSohbet");

            migrationBuilder.DropIndex(name: "IX_MuhasebeciSohbetMesaji_SohbetId", table: "MuhasebeciSohbetMesaji");
            migrationBuilder.DropIndex(name: "IX_MuhasebeciSohbetMesaji_SohbetId_ClientMessageId", table: "MuhasebeciSohbetMesaji");
            migrationBuilder.DropIndex(name: "IX_MuhasebeciSohbetMesaji_SohbetId_Id", table: "MuhasebeciSohbetMesaji");
            migrationBuilder.DropColumn(name: "SohbetId", table: "MuhasebeciSohbetMesaji");
            migrationBuilder.DropColumn(name: "MesajTipi", table: "MuhasebeciSohbetMesaji");
            migrationBuilder.DropColumn(name: "ClientMessageId", table: "MuhasebeciSohbetMesaji");
        }
    }
}
