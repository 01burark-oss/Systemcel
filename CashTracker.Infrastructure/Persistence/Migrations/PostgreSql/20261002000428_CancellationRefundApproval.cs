using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class CancellationRefundApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IptalIadeOnayAt",
                table: "Abonelik",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IptalIadeOnaylayanProviderKullaniciId",
                table: "Abonelik",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IptalIadeTalimatiId",
                table: "Abonelik",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Abonelik_IptalIadeTalimatiId",
                table: "Abonelik",
                column: "IptalIadeTalimatiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Abonelik_OdemeIadeTalimati_IptalIadeTalimatiId",
                table: "Abonelik",
                column: "IptalIadeTalimatiId",
                principalTable: "OdemeIadeTalimati",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abonelik_OdemeIadeTalimati_IptalIadeTalimatiId",
                table: "Abonelik");

            migrationBuilder.DropIndex(
                name: "IX_Abonelik_IptalIadeTalimatiId",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalIadeOnayAt",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalIadeOnaylayanProviderKullaniciId",
                table: "Abonelik");

            migrationBuilder.DropColumn(
                name: "IptalIadeTalimatiId",
                table: "Abonelik");
        }
    }
}
