using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class WebAuthSubscriptionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClerkOrganizationId",
                table: "Isletme",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SahipKullaniciId",
                table: "Isletme",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantTipi",
                table: "Isletme",
                type: "text",
                nullable: false,
                defaultValue: "Isletme");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Isletme",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "Abonelik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    HesapTipi = table.Column<string>(type: "text", nullable: false),
                    PlanKodu = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    AylikTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ParaBirimi = table.Column<string>(type: "text", nullable: false),
                    DonemBaslangicAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DonemBitisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DonemSonundaIptal = table.Column<bool>(type: "boolean", nullable: false),
                    IptalAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OdemeSaglayici = table.Column<string>(type: "text", nullable: false),
                    SaglayiciMusteriId = table.Column<string>(type: "text", nullable: false),
                    SaglayiciAbonelikId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abonelik", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiKullanimDonemi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    DonemAnahtari = table.Column<string>(type: "text", nullable: false),
                    MesajLimiti = table.Column<int>(type: "integer", nullable: true),
                    KullanilanMesaj = table.Column<int>(type: "integer", nullable: false),
                    DonemBaslangicAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DonemBitisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiKullanimDonemi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IsletmeDeneme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    PlanKodu = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    BaslangicAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OdemeYontemiEklendi = table.Column<bool>(type: "boolean", nullable: false),
                    OdemeSaglayici = table.Column<string>(type: "text", nullable: false),
                    SaglayiciMusteriId = table.Column<string>(type: "text", nullable: false),
                    SaglayiciOdemeYontemiId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsletmeDeneme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IsletmeEntitlement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    PlanKodu = table.Column<string>(type: "text", nullable: false),
                    Kaynak = table.Column<string>(type: "text", nullable: false),
                    OcrAktif = table.Column<bool>(type: "boolean", nullable: false),
                    GibAktif = table.Column<bool>(type: "boolean", nullable: false),
                    TelegramAktif = table.Column<bool>(type: "boolean", nullable: false),
                    AiAktif = table.Column<bool>(type: "boolean", nullable: false),
                    AiMesajLimiti = table.Column<int>(type: "integer", nullable: true),
                    KullaniciLimiti = table.Column<int>(type: "integer", nullable: true),
                    MusteriLimiti = table.Column<int>(type: "integer", nullable: true),
                    MuhasebeciPaneliAktif = table.Column<bool>(type: "boolean", nullable: false),
                    OneCikmaAktif = table.Column<bool>(type: "boolean", nullable: false),
                    DonemOtomasyonuAktif = table.Column<bool>(type: "boolean", nullable: false),
                    MusteriSaglikSkoruAktif = table.Column<bool>(type: "boolean", nullable: false),
                    SponsorMuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: true),
                    GecerliBaslangicAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GecerliBitisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsletmeEntitlement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IsletmeUyelik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: true),
                    Rol = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    DavetEposta = table.Column<string>(type: "text", nullable: false),
                    DavetKodu = table.Column<string>(type: "text", nullable: true),
                    DavetEdenKullaniciId = table.Column<int>(type: "integer", nullable: true),
                    DavetAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    KabulAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsletmeUyelik", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthProvider = table.Column<string>(type: "text", nullable: false),
                    AuthProviderUserId = table.Column<string>(type: "text", nullable: false),
                    Eposta = table.Column<string>(type: "text", nullable: false),
                    AdSoyad = table.Column<string>(type: "text", nullable: false),
                    HesapTipi = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    SonGirisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanici", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MuhasebeciMusteri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    DavetKodu = table.Column<string>(type: "text", nullable: true),
                    BaslangicAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notlar = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciMusteri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Isletme_ClerkOrganizationId",
                table: "Isletme",
                column: "ClerkOrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Isletme_SahipKullaniciId",
                table: "Isletme",
                column: "SahipKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Isletme_TenantTipi",
                table: "Isletme",
                column: "TenantTipi");

            migrationBuilder.CreateIndex(
                name: "IX_Abonelik_IsletmeId",
                table: "Abonelik",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_Abonelik_IsletmeId_Durum",
                table: "Abonelik",
                columns: new[] { "IsletmeId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_Abonelik_PlanKodu",
                table: "Abonelik",
                column: "PlanKodu");

            migrationBuilder.CreateIndex(
                name: "IX_Abonelik_SaglayiciAbonelikId",
                table: "Abonelik",
                column: "SaglayiciAbonelikId");

            migrationBuilder.CreateIndex(
                name: "IX_AiKullanimDonemi_IsletmeId",
                table: "AiKullanimDonemi",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_AiKullanimDonemi_IsletmeId_DonemAnahtari",
                table: "AiKullanimDonemi",
                columns: new[] { "IsletmeId", "DonemAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeDeneme_Durum",
                table: "IsletmeDeneme",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeDeneme_IsletmeId",
                table: "IsletmeDeneme",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeDeneme_IsletmeId_PlanKodu",
                table: "IsletmeDeneme",
                columns: new[] { "IsletmeId", "PlanKodu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeEntitlement_IsletmeId",
                table: "IsletmeEntitlement",
                column: "IsletmeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeEntitlement_Kaynak",
                table: "IsletmeEntitlement",
                column: "Kaynak");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeEntitlement_PlanKodu",
                table: "IsletmeEntitlement",
                column: "PlanKodu");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeEntitlement_SponsorMuhasebeciIsletmeId",
                table: "IsletmeEntitlement",
                column: "SponsorMuhasebeciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeUyelik_DavetKodu",
                table: "IsletmeUyelik",
                column: "DavetKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeUyelik_IsletmeId",
                table: "IsletmeUyelik",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeUyelik_IsletmeId_KullaniciId",
                table: "IsletmeUyelik",
                columns: new[] { "IsletmeId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsletmeUyelik_KullaniciId",
                table: "IsletmeUyelik",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanici_AuthProvider_AuthProviderUserId",
                table: "Kullanici",
                columns: new[] { "AuthProvider", "AuthProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanici_Eposta",
                table: "Kullanici",
                column: "Eposta");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanici_HesapTipi",
                table: "Kullanici",
                column: "HesapTipi");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteri_DavetKodu",
                table: "MuhasebeciMusteri",
                column: "DavetKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteri_MuhasebeciIsletmeId",
                table: "MuhasebeciMusteri",
                column: "MuhasebeciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteri_MuhasebeciIsletmeId_MusteriIsletmeId",
                table: "MuhasebeciMusteri",
                columns: new[] { "MuhasebeciIsletmeId", "MusteriIsletmeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciMusteri_MusteriIsletmeId",
                table: "MuhasebeciMusteri",
                column: "MusteriIsletmeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Abonelik");

            migrationBuilder.DropTable(
                name: "AiKullanimDonemi");

            migrationBuilder.DropTable(
                name: "IsletmeDeneme");

            migrationBuilder.DropTable(
                name: "IsletmeEntitlement");

            migrationBuilder.DropTable(
                name: "IsletmeUyelik");

            migrationBuilder.DropTable(
                name: "Kullanici");

            migrationBuilder.DropTable(
                name: "MuhasebeciMusteri");

            migrationBuilder.DropIndex(
                name: "IX_Isletme_ClerkOrganizationId",
                table: "Isletme");

            migrationBuilder.DropIndex(
                name: "IX_Isletme_SahipKullaniciId",
                table: "Isletme");

            migrationBuilder.DropIndex(
                name: "IX_Isletme_TenantTipi",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "ClerkOrganizationId",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "SahipKullaniciId",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "TenantTipi",
                table: "Isletme");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Isletme");
        }
    }
}
