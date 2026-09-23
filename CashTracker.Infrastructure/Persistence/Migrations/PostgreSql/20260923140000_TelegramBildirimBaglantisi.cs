using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260923140000_TelegramBildirimBaglantisi")]
public sealed class TelegramBildirimBaglantisiMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TelegramBildirimBaglantisi",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IsletmeId = table.Column<int>(type: "integer", nullable: false),
                KullaniciRef = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                ChatId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                TelegramUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EslestirmeKodu = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                KodGecerliAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                BaglandiAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramBildirimBaglantisi", x => x.Id);
                table.ForeignKey(
                    name: "FK_TelegramBildirimBaglantisi_Isletme_IsletmeId",
                    column: x => x.IsletmeId,
                    principalTable: "Isletme",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TelegramBildirimBaglantisi_EslestirmeKodu",
            table: "TelegramBildirimBaglantisi",
            column: "EslestirmeKodu",
            unique: true,
            filter: "\"EslestirmeKodu\" <> ''");

        migrationBuilder.CreateIndex(
            name: "IX_TelegramBildirimBaglantisi_IsletmeId_KullaniciRef",
            table: "TelegramBildirimBaglantisi",
            columns: new[] { "IsletmeId", "KullaniciRef" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TelegramBildirimBaglantisi");
    }
}
