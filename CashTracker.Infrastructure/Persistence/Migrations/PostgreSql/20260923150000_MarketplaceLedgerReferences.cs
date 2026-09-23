using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MarketplaceLedgerReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TedarikciMalKabulId",
                table: "TahsilatOdeme",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciSiparisId",
                table: "TahsilatOdeme",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciMalKabulId",
                table: "StokHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciSevkiyatId",
                table: "StokHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciSiparisId",
                table: "StokHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciMalKabulId",
                table: "CariHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TedarikciSiparisId",
                table: "CariHareket",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_TedarikciMalKabulId",
                table: "TahsilatOdeme",
                column: "TedarikciMalKabulId");

            migrationBuilder.CreateIndex(
                name: "IX_TahsilatOdeme_TedarikciSiparisId",
                table: "TahsilatOdeme",
                column: "TedarikciSiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_TedarikciMalKabulId",
                table: "StokHareket",
                column: "TedarikciMalKabulId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_TedarikciSevkiyatId",
                table: "StokHareket",
                column: "TedarikciSevkiyatId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_TedarikciSiparisId",
                table: "StokHareket",
                column: "TedarikciSiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_TedarikciMalKabulId",
                table: "CariHareket",
                column: "TedarikciMalKabulId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHareket_TedarikciSiparisId",
                table: "CariHareket",
                column: "TedarikciSiparisId");

            migrationBuilder.AddForeignKey(
                name: "FK_CariHareket_TedarikciMalKabul_TedarikciMalKabulId",
                table: "CariHareket",
                column: "TedarikciMalKabulId",
                principalTable: "TedarikciMalKabul",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CariHareket_TedarikciSiparis_TedarikciSiparisId",
                table: "CariHareket",
                column: "TedarikciSiparisId",
                principalTable: "TedarikciSiparis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareket_TedarikciMalKabul_TedarikciMalKabulId",
                table: "StokHareket",
                column: "TedarikciMalKabulId",
                principalTable: "TedarikciMalKabul",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareket_TedarikciSevkiyat_TedarikciSevkiyatId",
                table: "StokHareket",
                column: "TedarikciSevkiyatId",
                principalTable: "TedarikciSevkiyat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StokHareket_TedarikciSiparis_TedarikciSiparisId",
                table: "StokHareket",
                column: "TedarikciSiparisId",
                principalTable: "TedarikciSiparis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TahsilatOdeme_TedarikciMalKabul_TedarikciMalKabulId",
                table: "TahsilatOdeme",
                column: "TedarikciMalKabulId",
                principalTable: "TedarikciMalKabul",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TahsilatOdeme_TedarikciSiparis_TedarikciSiparisId",
                table: "TahsilatOdeme",
                column: "TedarikciSiparisId",
                principalTable: "TedarikciSiparis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CariHareket_TedarikciMalKabul_TedarikciMalKabulId",
                table: "CariHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_CariHareket_TedarikciSiparis_TedarikciSiparisId",
                table: "CariHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareket_TedarikciMalKabul_TedarikciMalKabulId",
                table: "StokHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareket_TedarikciSevkiyat_TedarikciSevkiyatId",
                table: "StokHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_StokHareket_TedarikciSiparis_TedarikciSiparisId",
                table: "StokHareket");

            migrationBuilder.DropForeignKey(
                name: "FK_TahsilatOdeme_TedarikciMalKabul_TedarikciMalKabulId",
                table: "TahsilatOdeme");

            migrationBuilder.DropForeignKey(
                name: "FK_TahsilatOdeme_TedarikciSiparis_TedarikciSiparisId",
                table: "TahsilatOdeme");

            migrationBuilder.DropIndex(
                name: "IX_TahsilatOdeme_TedarikciMalKabulId",
                table: "TahsilatOdeme");

            migrationBuilder.DropIndex(
                name: "IX_TahsilatOdeme_TedarikciSiparisId",
                table: "TahsilatOdeme");

            migrationBuilder.DropIndex(
                name: "IX_StokHareket_TedarikciMalKabulId",
                table: "StokHareket");

            migrationBuilder.DropIndex(
                name: "IX_StokHareket_TedarikciSevkiyatId",
                table: "StokHareket");

            migrationBuilder.DropIndex(
                name: "IX_StokHareket_TedarikciSiparisId",
                table: "StokHareket");

            migrationBuilder.DropIndex(
                name: "IX_CariHareket_TedarikciMalKabulId",
                table: "CariHareket");

            migrationBuilder.DropIndex(
                name: "IX_CariHareket_TedarikciSiparisId",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciMalKabulId",
                table: "TahsilatOdeme");

            migrationBuilder.DropColumn(
                name: "TedarikciSiparisId",
                table: "TahsilatOdeme");

            migrationBuilder.DropColumn(
                name: "TedarikciMalKabulId",
                table: "StokHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciSevkiyatId",
                table: "StokHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciSiparisId",
                table: "StokHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciMalKabulId",
                table: "CariHareket");

            migrationBuilder.DropColumn(
                name: "TedarikciSiparisId",
                table: "CariHareket");

        }
    }
}
