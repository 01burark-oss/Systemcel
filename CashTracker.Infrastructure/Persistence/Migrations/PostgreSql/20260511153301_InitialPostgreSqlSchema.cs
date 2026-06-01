using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialPostgreSqlSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BelgeDosya",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: false),
                    BelgeTipi = table.Column<string>(type: "text", nullable: false),
                    DosyaYolu = table.Column<string>(type: "text", nullable: false),
                    Kaynak = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BelgeDosya", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CariHareket",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    HareketTipi = table.Column<string>(type: "text", nullable: false),
                    Tutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    Kaynak = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariHareket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CariKart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    Unvan = table.Column<string>(type: "text", nullable: false),
                    Telefon = table.Column<string>(type: "text", nullable: false),
                    Eposta = table.Column<string>(type: "text", nullable: false),
                    Adres = table.Column<string>(type: "text", nullable: false),
                    VergiNoTc = table.Column<string>(type: "text", nullable: false),
                    VergiDairesi = table.Column<string>(type: "text", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariKart", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fatura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VadeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FaturaTipi = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    YerelFaturaNo = table.Column<string>(type: "text", nullable: false),
                    PortalBelgeNo = table.Column<string>(type: "text", nullable: false),
                    PortalUuid = table.Column<string>(type: "text", nullable: false),
                    AraToplam = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    IskontoToplam = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvToplam = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    OdemeYontemi = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    KesildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fatura", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaturaSatir",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: false),
                    UrunHizmetId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: false),
                    Birim = table.Column<string>(type: "text", nullable: false),
                    Miktar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    IskontoOrani = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    IskontoTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    SatirNetTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    SatirToplam = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    StokEtkilesin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaturaSatir", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GibPortalAyar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciKodu = table.Column<string>(type: "text", nullable: false),
                    SifreCipherText = table.Column<string>(type: "text", nullable: false),
                    TestModu = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GibPortalAyar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GibPortalIslemLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Islem = table.Column<string>(type: "text", nullable: false),
                    Basarili = table.Column<bool>(type: "boolean", nullable: false),
                    Mesaj = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GibPortalIslemLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Isletme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    IsAktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Isletme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KalemTanimi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KalemTanimi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kasa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    Tutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    OdemeYontemi = table.Column<string>(type: "text", nullable: false),
                    Kalem = table.Column<string>(type: "text", nullable: true),
                    GiderTuru = table.Column<string>(type: "text", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kasa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StokHareket",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    UrunHizmetId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Miktar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    HareketTipi = table.Column<string>(type: "text", nullable: false),
                    Kaynak = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokHareket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TahsilatOdeme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    Tutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    OdemeYontemi = table.Column<string>(type: "text", nullable: false),
                    KasaId = table.Column<int>(type: "integer", nullable: true),
                    CariHareketId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TahsilatOdeme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UrunHizmet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Tip = table.Column<string>(type: "text", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    Barkod = table.Column<string>(type: "text", nullable: false),
                    Birim = table.Column<string>(type: "text", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    AlisFiyati = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    SatisFiyati = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    KritikStok = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrunHizmet", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSetting_Key",
                table: "AppSetting",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BelgeDosya_IsletmeId",
                table: "BelgeDosya",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_BelgeDosya_IsletmeId_FaturaId",
                table: "BelgeDosya",
                columns: new[] { "IsletmeId", "FaturaId" });

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_IsletmeId",
                table: "CariHareket",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_IsletmeId_CariKartId_Tarih",
                table: "CariHareket",
                columns: new[] { "IsletmeId", "CariKartId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_CariKart_IsletmeId",
                table: "CariKart",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_CariKart_IsletmeId_Unvan",
                table: "CariKart",
                columns: new[] { "IsletmeId", "Unvan" });

            migrationBuilder.CreateIndex(
                name: "IX_CariKart_IsletmeId_VergiNoTc",
                table: "CariKart",
                columns: new[] { "IsletmeId", "VergiNoTc" });

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_IsletmeId",
                table: "Fatura",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_IsletmeId_CariKartId",
                table: "Fatura",
                columns: new[] { "IsletmeId", "CariKartId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_IsletmeId_Tarih",
                table: "Fatura",
                columns: new[] { "IsletmeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_FaturaSatir_IsletmeId",
                table: "FaturaSatir",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaSatir_IsletmeId_FaturaId",
                table: "FaturaSatir",
                columns: new[] { "IsletmeId", "FaturaId" });

            migrationBuilder.CreateIndex(
                name: "IX_GibPortalAyar_IsletmeId",
                table: "GibPortalAyar",
                column: "IsletmeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GibPortalIslemLog_IsletmeId",
                table: "GibPortalIslemLog",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_GibPortalIslemLog_IsletmeId_FaturaId_Tarih",
                table: "GibPortalIslemLog",
                columns: new[] { "IsletmeId", "FaturaId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_Isletme_IsAktif",
                table: "Isletme",
                column: "IsAktif");

            migrationBuilder.CreateIndex(
                name: "IX_KalemTanimi_IsletmeId",
                table: "KalemTanimi",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_KalemTanimi_IsletmeId_Tip_Ad",
                table: "KalemTanimi",
                columns: new[] { "IsletmeId", "Tip", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_IsletmeId",
                table: "Kasa",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_IsletmeId_Tarih",
                table: "Kasa",
                columns: new[] { "IsletmeId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_IsletmeId",
                table: "StokHareket",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_IsletmeId_UrunHizmetId_Tarih",
                table: "StokHareket",
                columns: new[] { "IsletmeId", "UrunHizmetId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_IsletmeId",
                table: "TahsilatOdeme",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_IsletmeId_CariKartId_Tarih",
                table: "TahsilatOdeme",
                columns: new[] { "IsletmeId", "CariKartId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_IsletmeId_FaturaId",
                table: "TahsilatOdeme",
                columns: new[] { "IsletmeId", "FaturaId" });

            migrationBuilder.CreateIndex(
                name: "IX_UrunHizmet_IsletmeId",
                table: "UrunHizmet",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunHizmet_IsletmeId_Barkod",
                table: "UrunHizmet",
                columns: new[] { "IsletmeId", "Barkod" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSetting");

            migrationBuilder.DropTable(
                name: "BelgeDosya");

            migrationBuilder.DropTable(
                name: "CariHareket");

            migrationBuilder.DropTable(
                name: "CariKart");

            migrationBuilder.DropTable(
                name: "Fatura");

            migrationBuilder.DropTable(
                name: "FaturaSatir");

            migrationBuilder.DropTable(
                name: "GibPortalAyar");

            migrationBuilder.DropTable(
                name: "GibPortalIslemLog");

            migrationBuilder.DropTable(
                name: "Isletme");

            migrationBuilder.DropTable(
                name: "KalemTanimi");

            migrationBuilder.DropTable(
                name: "Kasa");

            migrationBuilder.DropTable(
                name: "StokHareket");

            migrationBuilder.DropTable(
                name: "TahsilatOdeme");

            migrationBuilder.DropTable(
                name: "UrunHizmet");
        }
    }
}
