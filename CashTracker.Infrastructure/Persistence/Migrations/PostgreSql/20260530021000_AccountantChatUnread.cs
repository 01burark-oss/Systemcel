using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260530021000_AccountantChatUnread")]
    public partial class AccountantChatUnread : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OkunduAt",
                table: "MuhasebeciSohbetMesaji",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciSohbetMesaji_OkunduAt",
                table: "MuhasebeciSohbetMesaji",
                column: "OkunduAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuhasebeciSohbetMesaji_OkunduAt",
                table: "MuhasebeciSohbetMesaji");

            migrationBuilder.DropColumn(
                name: "OkunduAt",
                table: "MuhasebeciSohbetMesaji");
        }
    }
}
