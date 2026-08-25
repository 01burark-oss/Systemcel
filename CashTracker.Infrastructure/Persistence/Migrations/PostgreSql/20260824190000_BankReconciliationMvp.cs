using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260824190000_BankReconciliationMvp")]
public partial class BankReconciliationMvp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BankaHareketi",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IsletmeId = table.Column<int>(type: "integer", nullable: false),
                Tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Tutar = table.Column<decimal>(type: "NUMERIC(18,2)", nullable: false),
                ParaBirimi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Durum = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                KaynakHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                EslesenKaynakTuru = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                EslesenKaynakId = table.Column<int>(type: "integer", nullable: true),
                EslestiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                YokSayildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BankaHareketi", x => x.Id);
                table.ForeignKey("FK_BankaHareketi_Isletme_IsletmeId", x => x.IsletmeId, "Isletme", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_BankaHareketi_IsletmeId_KaynakHash", "BankaHareketi", new[] { "IsletmeId", "KaynakHash" }, unique: true);
        migrationBuilder.CreateIndex("IX_BankaHareketi_IsletmeId_Durum_Tarih", "BankaHareketi", new[] { "IsletmeId", "Durum", "Tarih" });
        migrationBuilder.CreateIndex("IX_BankaHareketi_IsletmeId_EslesenKaynakTuru_EslesenKaynakId", "BankaHareketi", new[] { "IsletmeId", "EslesenKaynakTuru", "EslesenKaynakId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("BankaHareketi");
}
