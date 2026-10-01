using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SubscriptionRefundInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OdemeIadeTalimati",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OdemeIslemiId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyAnahtari = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferansNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Tutar = table.Column<decimal>(type: "NUMERIC", nullable: false),
                    Durum = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SonHataKodu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeIadeTalimati", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeIadeTalimati_OdemeIslemi_OdemeIslemiId",
                        column: x => x.OdemeIslemiId,
                        principalTable: "OdemeIslemi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OdemeIadeTalimati_Durum",
                table: "OdemeIadeTalimati",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeIadeTalimati_OdemeIslemiId_IdempotencyAnahtari",
                table: "OdemeIadeTalimati",
                columns: new[] { "OdemeIslemiId", "IdempotencyAnahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdemeIadeTalimati_ReferansNo",
                table: "OdemeIadeTalimati",
                column: "ReferansNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OdemeIadeTalimati");
        }
    }
}
