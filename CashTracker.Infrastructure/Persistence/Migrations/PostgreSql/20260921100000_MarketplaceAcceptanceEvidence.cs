using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260921100000_MarketplaceAcceptanceEvidence")]
public sealed class MarketplaceAcceptanceEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("BelgeUuid", "TedarikciSevkiyat", type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("BelgeDosyaYolu", "TedarikciSevkiyat", type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>("SevkAt", "TedarikciSevkiyat", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>("VarisDeposu", "TedarikciSevkiyat", type: "integer", nullable: true);
        migrationBuilder.AddColumn<DateTime>("RandevuAt", "TedarikciSevkiyat", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>("SeriNo", "TedarikciSevkiyatKalemi", type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<decimal>("Agirlik", "TedarikciSevkiyatKalemi", type: "numeric(18,3)", nullable: true);
        migrationBuilder.AddColumn<int>("PaletKoli", "TedarikciSevkiyatKalemi", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("SubeId", "TedarikciMalKabul", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>("DepoId", "TedarikciMalKabul", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>("IslemYapanKullaniciRef", "TedarikciMalKabul", type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("CihazRef", "TedarikciMalKabul", type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("IpAdresi", "TedarikciMalKabul", type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("BelgeKarmasi", "TedarikciMalKabul", type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("FotoKanitiYolu", "TedarikciMalKabul", type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<decimal>("OlculenAgirlik", "TedarikciMalKabul", type: "numeric(18,3)", nullable: true);
        migrationBuilder.AddColumn<decimal>("OlculenSicaklik", "TedarikciMalKabul", type: "numeric(8,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>("KabulBrutTutar", "TedarikciMalKabul", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("SerbestBirakilanNetTutar", "TedarikciMalKabul", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<DateTime>("MuhasebelestiAt", "TedarikciMalKabul", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>("HakEdisAktarimReferansi", "TedarikciMalKabul", type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("HakEdisAktarimHatasi", "TedarikciMalKabul", type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>("SubeId", "IsletmeUyelik", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>("DepoId", "IsletmeUyelik", type: "integer", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "BelgeUuid", "BelgeDosyaYolu", "SevkAt", "VarisDeposu", "RandevuAt" }) migrationBuilder.DropColumn(column, "TedarikciSevkiyat");
        foreach (var column in new[] { "SeriNo", "Agirlik", "PaletKoli" }) migrationBuilder.DropColumn(column, "TedarikciSevkiyatKalemi");
        foreach (var column in new[] { "SubeId", "DepoId", "IslemYapanKullaniciRef", "CihazRef", "IpAdresi", "BelgeKarmasi", "FotoKanitiYolu", "OlculenAgirlik", "OlculenSicaklik", "KabulBrutTutar", "SerbestBirakilanNetTutar", "MuhasebelestiAt", "HakEdisAktarimReferansi", "HakEdisAktarimHatasi" }) migrationBuilder.DropColumn(column, "TedarikciMalKabul");
        foreach (var column in new[] { "SubeId", "DepoId" }) migrationBuilder.DropColumn(column, "IsletmeUyelik");
    }
}
