using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SubscriptionCancellationPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PlatformOdemeHizmetiBedeli",
                table: "TedarikciSiparis",
                type: "NUMERIC",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IptalIadeDurumu",
                table: "Abonelik",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IptalIadeOdemeIslemiId",
                table: "Abonelik",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IptalIadeTutari",
                table: "Abonelik",
                type: "NUMERIC",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IptalKalanAySayisi",
                table: "Abonelik",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "IptalOncesiDonemBitisAt",
                table: "Abonelik",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Abonelik_IptalIadeOdemeIslemiId",
                table: "Abonelik",
                column: "IptalIadeOdemeIslemiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Abonelik_OdemeIslemi_IptalIadeOdemeIslemiId",
                table: "Abonelik",
                column: "IptalIadeOdemeIslemiId",
                principalTable: "OdemeIslemi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abonelik_OdemeIslemi_IptalIadeOdemeIslemiId",
                table: "Abonelik");

            migrationBuilder.DropIndex(
                name: "IX_Abonelik_IptalIadeOdemeIslemiId",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "PlatformOdemeHizmetiBedeli",
                table: "TedarikciSiparis");

            migrationBuilder.DropColumn(
                name: "IptalIadeDurumu",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalIadeOdemeIslemiId",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalIadeTutari",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalKalanAySayisi",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalOncesiDonemBitisAt",
                table: "Abonelik");
        }
    }
}
