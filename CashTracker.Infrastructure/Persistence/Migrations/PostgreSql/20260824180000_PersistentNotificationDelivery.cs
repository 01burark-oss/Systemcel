using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260824180000_PersistentNotificationDelivery")]
public partial class PersistentNotificationDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("BildirimKaydi", table => new
        {
            Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            IsletmeId = table.Column<int>("integer"), KullaniciRef = table.Column<string>("character varying(200)", maxLength: 200),
            KaynakAnahtari = table.Column<string>("character varying(160)", maxLength: 160), Tur = table.Column<string>("character varying(30)", maxLength: 30),
            Onem = table.Column<string>("character varying(20)", maxLength: 20), Baslik = table.Column<string>("character varying(200)", maxLength: 200),
            Mesaj = table.Column<string>("character varying(1000)", maxLength: 1000), Aksiyon = table.Column<string>("character varying(120)", maxLength: 120),
            Url = table.Column<string>("character varying(500)", maxLength: 500), OkunduAt = table.Column<DateTime>("timestamp without time zone", nullable: true),
            CreatedAt = table.Column<DateTime>("timestamp without time zone"), UpdatedAt = table.Column<DateTime>("timestamp without time zone")
        }, constraints: table => table.PrimaryKey("PK_BildirimKaydi", x => x.Id));
        migrationBuilder.CreateIndex("IX_BildirimKaydi_IsletmeId_KullaniciRef_KaynakAnahtari", "BildirimKaydi", new[] { "IsletmeId", "KullaniciRef", "KaynakAnahtari" }, unique: true);
        migrationBuilder.CreateIndex("IX_BildirimKaydi_IsletmeId_KullaniciRef_OkunduAt", "BildirimKaydi", new[] { "IsletmeId", "KullaniciRef", "OkunduAt" });

        migrationBuilder.CreateTable("BildirimTercihi", table => new
        {
            Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            IsletmeId = table.Column<int>("integer"), KullaniciRef = table.Column<string>("character varying(200)", maxLength: 200),
            UygulamaAktif = table.Column<bool>("boolean"), EpostaAktif = table.Column<bool>("boolean"), TelegramAktif = table.Column<bool>("boolean"),
            SessizSaatAktif = table.Column<bool>("boolean"), SessizBaslangicDakika = table.Column<int>("integer"), SessizBitisDakika = table.Column<int>("integer"),
            SaatDilimi = table.Column<string>("character varying(60)", maxLength: 60), CreatedAt = table.Column<DateTime>("timestamp without time zone"), UpdatedAt = table.Column<DateTime>("timestamp without time zone")
        }, constraints: table => table.PrimaryKey("PK_BildirimTercihi", x => x.Id));
        migrationBuilder.CreateIndex("IX_BildirimTercihi_IsletmeId_KullaniciRef", "BildirimTercihi", new[] { "IsletmeId", "KullaniciRef" }, unique: true);

        migrationBuilder.CreateTable("BildirimTeslimOutbox", table => new
        {
            Id = table.Column<long>("bigint").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            IsletmeId = table.Column<int>("integer"), KullaniciRef = table.Column<string>("character varying(200)", maxLength: 200), BildirimId = table.Column<int>("integer", nullable: true),
            IdempotencyAnahtari = table.Column<string>("character varying(160)", maxLength: 160), Kanal = table.Column<string>("character varying(20)", maxLength: 20),
            Durum = table.Column<string>("character varying(30)", maxLength: 30), PayloadJson = table.Column<string>("character varying(4000)", maxLength: 4000), DenemeSayisi = table.Column<int>("integer"),
            SonrakiDenemeAt = table.Column<DateTime>("timestamp without time zone"), ClaimToken = table.Column<string>("character varying(64)", maxLength: 64), ClaimBitisAt = table.Column<DateTime>("timestamp without time zone", nullable: true),
            SonHataKodu = table.Column<string>("character varying(80)", maxLength: 80), TeslimEdildiAt = table.Column<DateTime>("timestamp without time zone", nullable: true), DeadLetterAt = table.Column<DateTime>("timestamp without time zone", nullable: true),
            CreatedAt = table.Column<DateTime>("timestamp without time zone"), UpdatedAt = table.Column<DateTime>("timestamp without time zone")
        }, constraints: table => table.PrimaryKey("PK_BildirimTeslimOutbox", x => x.Id));
        migrationBuilder.CreateIndex("IX_BildirimTeslimOutbox_IsletmeId_KullaniciRef_Kanal_IdempotencyAnahtari", "BildirimTeslimOutbox", new[] { "IsletmeId", "KullaniciRef", "Kanal", "IdempotencyAnahtari" }, unique: true);
        migrationBuilder.CreateIndex("IX_BildirimTeslimOutbox_Durum_SonrakiDenemeAt_ClaimBitisAt", "BildirimTeslimOutbox", new[] { "Durum", "SonrakiDenemeAt", "ClaimBitisAt" });
        migrationBuilder.CreateIndex("IX_BildirimTeslimOutbox_IsletmeId_KullaniciRef", "BildirimTeslimOutbox", new[] { "IsletmeId", "KullaniciRef" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("BildirimTeslimOutbox");
        migrationBuilder.DropTable("BildirimTercihi");
        migrationBuilder.DropTable("BildirimKaydi");
    }
}
