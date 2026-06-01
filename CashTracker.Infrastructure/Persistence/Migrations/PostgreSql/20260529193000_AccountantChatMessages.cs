using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260529193000_AccountantChatMessages")]
    public partial class AccountantChatMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuhasebeciSohbetMesaji",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuhasebeciIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    MusteriIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    GonderenIsletmeId = table.Column<int>(type: "integer", nullable: false),
                    TalepId = table.Column<int>(type: "integer", nullable: true),
                    BaglantiId = table.Column<int>(type: "integer", nullable: true),
                    Mesaj = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuhasebeciSohbetMesaji", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciSohbetMesaji_BaglantiId",
                table: "MuhasebeciSohbetMesaji",
                column: "BaglantiId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciSohbetMesaji_MuhasebeciIsletmeId",
                table: "MuhasebeciSohbetMesaji",
                column: "MuhasebeciIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciSohbetMesaji_MuhasebeciIsletmeId_MusteriIsletmeId_CreatedAt",
                table: "MuhasebeciSohbetMesaji",
                columns: new[] { "MuhasebeciIsletmeId", "MusteriIsletmeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciSohbetMesaji_MusteriIsletmeId",
                table: "MuhasebeciSohbetMesaji",
                column: "MusteriIsletmeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuhasebeciSohbetMesaji_TalepId",
                table: "MuhasebeciSohbetMesaji",
                column: "TalepId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MuhasebeciSohbetMesaji");
        }
    }
}
