using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MarketplaceOrderSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adres",
                table: "TedarikciProfil",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DogrulamaDurumu",
                table: "TedarikciProfil",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DogrulamaNotu",
                table: "TedarikciProfil",
                type: "character varying(800)",
                maxLength: 800,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DogrulandiAt",
                table: "TedarikciProfil",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IadeKosullari",
                table: "TedarikciProfil",
                type: "character varying(1200)",
                maxLength: 1200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "TedarikciProfil",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KepAdresi",
                table: "TedarikciProfil",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "KomisyonOrani",
                table: "TedarikciProfil",
                type: "numeric(7,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MersisNo",
                table: "TedarikciProfil",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OdemeVadesiGun",
                table: "TedarikciProfil",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PazaryeriSozlesmeVersiyonu",
                table: "TedarikciProfil",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PspAltUyeIsyeriId",
                table: "TedarikciProfil",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SevkiyatBolgeleri",
                table: "TedarikciProfil",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TevkifatMuaf",
                table: "TedarikciProfil",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VergiDurumu",
                table: "TedarikciProfil",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VergiNo",
                table: "TedarikciProfil",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "YetkiliAdSoyad",
                table: "TedarikciProfil",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PazaryeriAnaSiparis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    SiparisNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OlusturmaAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeslimatAdresi = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AraToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PazaryeriAnaSiparis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PazaryeriAnaSiparis_Isletme_AliciIsletmeId",
                        column: x => x.AliciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciUrun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciProfilId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    KaynakUrunHizmetId = table.Column<int>(type: "integer", nullable: true),
                    Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Ad = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Kategori = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Birim = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StokMiktari = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    RezerveMiktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    MinimumSiparisMiktari = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    TahminiTeslimatGun = table.Column<int>(type: "integer", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciUrun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciUrun_Isletme_TedarikciIsletmeId",
                        column: x => x.TedarikciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciUrun_TedarikciProfil_TedarikciProfilId",
                        column: x => x.TedarikciProfilId,
                        principalTable: "TedarikciProfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TedarikciUrun_UrunHizmet_KaynakUrunHizmetId",
                        column: x => x.KaynakUrunHizmetId,
                        principalTable: "UrunHizmet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PazaryeriOdeme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnaSiparisId = table.Column<int>(type: "integer", nullable: false),
                    AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Saglayici = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SaglayiciIslemId = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IadeTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdendiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IadeEdildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PazaryeriOdeme", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PazaryeriOdeme_Isletme_AliciIsletmeId",
                        column: x => x.AliciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PazaryeriOdeme_PazaryeriAnaSiparis_AnaSiparisId",
                        column: x => x.AnaSiparisId,
                        principalTable: "PazaryeriAnaSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciSiparis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnaSiparisId = table.Column<int>(type: "integer", nullable: false),
                    AliciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciProfilId = table.Column<int>(type: "integer", nullable: false),
                    SiparisNo = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AraToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KomisyonMatrahi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KomisyonOrani = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    KomisyonTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KomisyonKdvTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TevkifatMatrahi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TevkifatTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdemeHizmetiBedeli = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TedarikciHakEdisi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    KargoFirmasi = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    KargoTakipNo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OdendiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TeslimEdildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HakEdisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciSiparis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparis_Isletme_AliciIsletmeId",
                        column: x => x.AliciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparis_Isletme_TedarikciIsletmeId",
                        column: x => x.TedarikciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparis_PazaryeriAnaSiparis_AnaSiparisId",
                        column: x => x.AnaSiparisId,
                        principalTable: "PazaryeriAnaSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparis_TedarikciProfil_TedarikciProfilId",
                        column: x => x.TedarikciProfilId,
                        principalTable: "TedarikciProfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PazaryeriDefterKaydi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    Hesap = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Yon = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PazaryeriDefterKaydi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PazaryeriDefterKaydi_TedarikciSiparis_TedarikciSiparisId",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PazaryeriOdemeDagitimi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PazaryeriOdemeId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    BrutTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TedarikciHakEdisi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IadeTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PazaryeriOdemeDagitimi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PazaryeriOdemeDagitimi_Isletme_TedarikciIsletmeId",
                        column: x => x.TedarikciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PazaryeriOdemeDagitimi_PazaryeriOdeme_PazaryeriOdemeId",
                        column: x => x.PazaryeriOdemeId,
                        principalTable: "PazaryeriOdeme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PazaryeriOdemeDagitimi_TedarikciSiparis_TedarikciSiparisId",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciFaturaEslesmesi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    AliciFaturaId = table.Column<int>(type: "integer", nullable: false),
                    SaticiFaturaId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciBelgeNo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TedarikciBelgeUuid = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciFaturaEslesmesi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciFaturaEslesmesi_Fatura_AliciFaturaId",
                        column: x => x.AliciFaturaId,
                        principalTable: "Fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciFaturaEslesmesi_Fatura_SaticiFaturaId",
                        column: x => x.SaticiFaturaId,
                        principalTable: "Fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciFaturaEslesmesi_TedarikciSiparis_TedarikciSiparisId",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciHakEdis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    BrutTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KomisyonTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KomisyonKdvTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TevkifatTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdemeHizmetiBedeli = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IadeTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AktarimReferansi = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    PlanlananAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TamamlandiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciHakEdis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciHakEdis_Isletme_TedarikciIsletmeId",
                        column: x => x.TedarikciIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciHakEdis_TedarikciSiparis_TedarikciSiparisId",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciSiparisDurumKaydi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    OncekiDurum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    YeniDurum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IslemYapanIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciSiparisDurumKaydi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparisDurumKaydi_Isletme_IslemYapanIsletmeId",
                        column: x => x.IslemYapanIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparisDurumKaydi_TedarikciSiparis_TedarikciSipari~",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TedarikciSiparisKalemi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TedarikciSiparisId = table.Column<int>(type: "integer", nullable: false),
                    TedarikciUrunId = table.Column<int>(type: "integer", nullable: false),
                    Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Ad = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Birim = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    NetTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TedarikciSiparisKalemi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparisKalemi_TedarikciSiparis_TedarikciSiparisId",
                        column: x => x.TedarikciSiparisId,
                        principalTable: "TedarikciSiparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TedarikciSiparisKalemi_TedarikciUrun_TedarikciUrunId",
                        column: x => x.TedarikciUrunId,
                        principalTable: "TedarikciUrun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriAnaSiparis_AliciIsletmeId_CreatedAt",
                table: "PazaryeriAnaSiparis",
                columns: new[] { "AliciIsletmeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriAnaSiparis_AliciIsletmeId_OlusturmaAnahtari",
                table: "PazaryeriAnaSiparis",
                columns: new[] { "AliciIsletmeId", "OlusturmaAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriAnaSiparis_SiparisNo",
                table: "PazaryeriAnaSiparis",
                column: "SiparisNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriDefterKaydi_TedarikciSiparisId_CreatedAt",
                table: "PazaryeriDefterKaydi",
                columns: new[] { "TedarikciSiparisId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriOdeme_AliciIsletmeId_IdempotencyAnahtari",
                table: "PazaryeriOdeme",
                columns: new[] { "AliciIsletmeId", "IdempotencyAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriOdeme_AnaSiparisId",
                table: "PazaryeriOdeme",
                column: "AnaSiparisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriOdemeDagitimi_PazaryeriOdemeId_TedarikciIsletmeId",
                table: "PazaryeriOdemeDagitimi",
                columns: new[] { "PazaryeriOdemeId", "TedarikciIsletmeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriOdemeDagitimi_TedarikciIsletmeId",
                table: "PazaryeriOdemeDagitimi",
                column: "TedarikciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_PazaryeriOdemeDagitimi_TedarikciSiparisId",
                table: "PazaryeriOdemeDagitimi",
                column: "TedarikciSiparisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciFaturaEslesmesi_AliciFaturaId",
                table: "TedarikciFaturaEslesmesi",
                column: "AliciFaturaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciFaturaEslesmesi_SaticiFaturaId",
                table: "TedarikciFaturaEslesmesi",
                column: "SaticiFaturaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciFaturaEslesmesi_TedarikciSiparisId",
                table: "TedarikciFaturaEslesmesi",
                column: "TedarikciSiparisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciHakEdis_TedarikciIsletmeId_Durum_PlanlananAt",
                table: "TedarikciHakEdis",
                columns: new[] { "TedarikciIsletmeId", "Durum", "PlanlananAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciHakEdis_TedarikciSiparisId",
                table: "TedarikciHakEdis",
                column: "TedarikciSiparisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparis_AliciIsletmeId_CreatedAt",
                table: "TedarikciSiparis",
                columns: new[] { "AliciIsletmeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparis_AnaSiparisId",
                table: "TedarikciSiparis",
                column: "AnaSiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparis_SiparisNo",
                table: "TedarikciSiparis",
                column: "SiparisNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparis_TedarikciIsletmeId_Durum",
                table: "TedarikciSiparis",
                columns: new[] { "TedarikciIsletmeId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparis_TedarikciProfilId",
                table: "TedarikciSiparis",
                column: "TedarikciProfilId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparisDurumKaydi_IslemYapanIsletmeId",
                table: "TedarikciSiparisDurumKaydi",
                column: "IslemYapanIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparisDurumKaydi_TedarikciSiparisId_CreatedAt",
                table: "TedarikciSiparisDurumKaydi",
                columns: new[] { "TedarikciSiparisId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparisKalemi_TedarikciSiparisId",
                table: "TedarikciSiparisKalemi",
                column: "TedarikciSiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciSiparisKalemi_TedarikciUrunId",
                table: "TedarikciSiparisKalemi",
                column: "TedarikciUrunId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciUrun_Aktif_Kategori",
                table: "TedarikciUrun",
                columns: new[] { "Aktif", "Kategori" });

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciUrun_KaynakUrunHizmetId",
                table: "TedarikciUrun",
                column: "KaynakUrunHizmetId");

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciUrun_TedarikciIsletmeId_Sku",
                table: "TedarikciUrun",
                columns: new[] { "TedarikciIsletmeId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TedarikciUrun_TedarikciProfilId",
                table: "TedarikciUrun",
                column: "TedarikciProfilId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PazaryeriDefterKaydi");

            migrationBuilder.DropTable(
                name: "PazaryeriOdemeDagitimi");

            migrationBuilder.DropTable(
                name: "TedarikciFaturaEslesmesi");

            migrationBuilder.DropTable(
                name: "TedarikciHakEdis");

            migrationBuilder.DropTable(
                name: "TedarikciSiparisDurumKaydi");

            migrationBuilder.DropTable(
                name: "TedarikciSiparisKalemi");

            migrationBuilder.DropTable(
                name: "PazaryeriOdeme");

            migrationBuilder.DropTable(
                name: "TedarikciSiparis");

            migrationBuilder.DropTable(
                name: "TedarikciUrun");

            migrationBuilder.DropTable(
                name: "PazaryeriAnaSiparis");

            migrationBuilder.DropColumn(
                name: "Adres",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "DogrulamaDurumu",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "DogrulamaNotu",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "DogrulandiAt",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "IadeKosullari",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "KepAdresi",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "KomisyonOrani",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "MersisNo",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "OdemeVadesiGun",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "PazaryeriSozlesmeVersiyonu",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "PspAltUyeIsyeriId",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "SevkiyatBolgeleri",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "TevkifatMuaf",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "VergiDurumu",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "VergiNo",
                table: "TedarikciProfil");

            migrationBuilder.DropColumn(
                name: "YetkiliAdSoyad",
                table: "TedarikciProfil");
        }
    }
}
