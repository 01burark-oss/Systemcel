using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class DeveloperApiAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GelistiriciApiAnahtari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsletmeId = table.Column<int>(type: "integer", nullable: false),
                    OlusturanKullaniciRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    AnahtarHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    ScopeListesi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RevokedByUserRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GelistiriciApiAnahtari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GelistiriciApiAnahtari_Isletme_IsletmeId",
                        column: x => x.IsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GelistiriciApiAnahtari_IsletmeId_CreatedAt",
                table: "GelistiriciApiAnahtari",
                columns: new[] { "IsletmeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GelistiriciApiAnahtari_IsletmeId_RevokedAt_ExpiresAt",
                table: "GelistiriciApiAnahtari",
                columns: new[] { "IsletmeId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GelistiriciApiAnahtari_Prefix",
                table: "GelistiriciApiAnahtari",
                column: "Prefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GelistiriciApiAnahtari");
        }
    }
}
