using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260822143000_PaymentReminder")]
    public partial class PaymentReminder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OdemeHatirlatma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<int>(type: "integer", nullable: false),
                    AliciEposta = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Konu = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Durum = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Hata = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GonderildiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeHatirlatma", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OdemeHatirlatma_IsletmeId",
                table: "OdemeHatirlatma",
                column: "IsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeHatirlatma_IsletmeId_FaturaId_GonderildiAt",
                table: "OdemeHatirlatma",
                columns: new[] { "IsletmeId", "FaturaId", "GonderildiAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OdemeHatirlatma");
        }
    }
}
