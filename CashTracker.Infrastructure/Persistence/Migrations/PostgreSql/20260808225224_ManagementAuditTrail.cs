using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class ManagementAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YonetimDenetimKaydi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    AktorProviderKullaniciId = table.Column<string>(type: "text", nullable: false),
                    Islem = table.Column<string>(type: "text", nullable: false),
                    KaynakTuru = table.Column<string>(type: "text", nullable: false),
                    OncekiDeger = table.Column<string>(type: "text", nullable: false),
                    YeniDeger = table.Column<string>(type: "text", nullable: false),
                    Gerekce = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YonetimDenetimKaydi", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YonetimDenetimKaydi_CreatedAt",
                table: "YonetimDenetimKaydi",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_YonetimDenetimKaydi_IsletmeId",
                table: "YonetimDenetimKaydi",
                column: "IsletmeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YonetimDenetimKaydi");
        }
    }
}
