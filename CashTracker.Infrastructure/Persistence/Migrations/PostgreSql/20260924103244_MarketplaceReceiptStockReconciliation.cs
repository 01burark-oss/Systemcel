using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MarketplaceReceiptStockReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StokUzlastiranKullaniciRef",
                table: "TedarikciMalKabul",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StokUzlastirmaAnahtari",
                table: "TedarikciMalKabul",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StokUzlastirmaAt",
                table: "TedarikciMalKabul",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StokUzlastirmaDurumu",
                table: "TedarikciMalKabul",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Uygulanmaz");

            migrationBuilder.Sql("UPDATE \"TedarikciMalKabul\" AS receipt SET \"StokUzlastirmaDurumu\" = CASE WHEN receipt.\"ReddedilenMiktar\" <= 0 THEN 'Uygulanmaz' WHEN EXISTS (SELECT 1 FROM \"TedarikciSiparisDurumKaydi\" AS history WHERE history.\"TedarikciSiparisId\" = receipt.\"TedarikciSiparisId\" AND history.\"CreatedAt\" >= receipt.\"CreatedAt\" AND history.\"Aciklama\" LIKE '%İtiraz kararı: YenidenTeslim.%') THEN 'ManuelInceleme' ELSE 'Bekliyor' END");

            migrationBuilder.AddColumn<string>(
                name: "StokUzlastirmaNotu",
                table: "TedarikciMalKabul",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"TedarikciMalKabul\" SET \"StokUzlastirmaNotu\" = 'Eski yeniden teslim kararı bulundu; ikinci stok işlemini önlemek için manuel incelemeye alındı.' WHERE \"StokUzlastirmaDurumu\" = 'ManuelInceleme'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StokUzlastiranKullaniciRef",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "StokUzlastirmaAnahtari",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "StokUzlastirmaAt",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "StokUzlastirmaDurumu",
                table: "TedarikciMalKabul");

            migrationBuilder.DropColumn(
                name: "StokUzlastirmaNotu",
                table: "TedarikciMalKabul");
        }
    }
}
