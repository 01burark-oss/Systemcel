using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260824120000_AccountantServicePaymentGate")]
    public partial class AccountantServicePaymentGate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AylikHizmetBedeli",
                table: "MuhasebeciMusteriTalebi",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "MuhasebeciHizmetOdemesi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TalepId = table.Column<int>(type: "integer", nullable: false),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    OdemeIslemiId = table.Column<int>(type: "integer", nullable: true),
                    AylikHizmetBedeli = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    TahsilEdilenTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    TahsilEdildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_MuhasebeciHizmetOdemesi", x => x.Id));

            migrationBuilder.CreateTable(
                name: "MuhasebeciAktarimAlacagi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeciHizmetOdemesiId = table.Column<int>(type: "integer", nullable: false),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    TalepId = table.Column<int>(type: "integer", nullable: false),
                    TahsilEdilenTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    PlatformKomisyonTutari = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    AktarilacakTutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AktarimDonemi = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Durum = table.Column<string>(type: "text", nullable: false),
                    AktarimReferansi = table.Column<string>(type: "text", nullable: false),
                    TahakkukAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AktarildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TersKayitAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_MuhasebeciAktarimAlacagi", x => x.Id));

            migrationBuilder.CreateIndex("IX_MuhasebeciHizmetOdemesi_TalepId", "MuhasebeciHizmetOdemesi", "TalepId", unique: true);
            migrationBuilder.CreateIndex("IX_MuhasebeciHizmetOdemesi_OdemeIslemiId", "MuhasebeciHizmetOdemesi", "OdemeIslemiId", unique: true);
            migrationBuilder.CreateIndex("IX_MuhasebeciHizmetOdemesi_MusteriIsletmeId_Durum", "MuhasebeciHizmetOdemesi", new[] { "MusteriIsletmeId", "Durum" });
            migrationBuilder.CreateIndex("IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId", "MuhasebeciAktarimAlacagi", "MuhasebeciHizmetOdemesiId", unique: true);
            migrationBuilder.CreateIndex("IX_MuhasebeciAktarimAlacagi_MuhasebeciIsletmeId_AktarimDonemi_Durum", "MuhasebeciAktarimAlacagi", new[] { "MuhasebeciIsletmeId", "AktarimDonemi", "Durum" });
            migrationBuilder.CreateIndex("IX_MuhasebeciAktarimAlacagi_AktarimReferansi", "MuhasebeciAktarimAlacagi", "AktarimReferansi");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("MuhasebeciAktarimAlacagi");
            migrationBuilder.DropTable("MuhasebeciHizmetOdemesi");
            migrationBuilder.DropColumn("AylikHizmetBedeli", "MuhasebeciMusteriTalebi");
        }
    }
}
