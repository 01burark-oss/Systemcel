using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260824160000_ProductSupportTickets")]
    public partial class ProductSupportTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DestekTalebi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    OlusturanKullaniciReferansi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OlusturmaAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Konu = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Kategori = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Oncelik = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Durum = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    YoneticiYaniti = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CozulduAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DestekTalebi", x => x.Id));

            migrationBuilder.CreateIndex("IX_DestekTalebi_Durum", "DestekTalebi", "Durum");
            migrationBuilder.CreateIndex("IX_DestekTalebi_IsletmeId_CreatedAt", "DestekTalebi", new[] { "IsletmeId", "CreatedAt" });
            migrationBuilder.CreateIndex("IX_DestekTalebi_Oncelik_CreatedAt", "DestekTalebi", new[] { "Oncelik", "CreatedAt" });
            migrationBuilder.CreateIndex("IX_DestekTalebi_IsletmeId_OlusturmaAnahtari", "DestekTalebi", new[] { "IsletmeId", "OlusturmaAnahtari" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("DestekTalebi");
        }
    }
}
