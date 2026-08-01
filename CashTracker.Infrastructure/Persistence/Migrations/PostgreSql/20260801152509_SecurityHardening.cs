using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DesktopImportCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TargetIsletmeId = table.Column<int>(type: "integer", nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PackageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImportedTotalsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesktopImportCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesktopImportCode_Isletme_TargetIsletmeId",
                        column: x => x.TargetIsletmeId,
                        principalTable: "Isletme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DesktopImportCode_Code",
                table: "DesktopImportCode",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesktopImportCode_RequestedBy_Status_ExpiresAtUtc",
                table: "DesktopImportCode",
                columns: new[] { "RequestedBy", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DesktopImportCode_TargetIsletmeId",
                table: "DesktopImportCode",
                column: "TargetIsletmeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesktopImportCode");
        }
    }
}
