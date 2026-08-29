using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260829093000_AccountantMarketplaceMatching")]
    public partial class AccountantMarketplaceMatching : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "VergiMukellefiTipi", table: "Isletme", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IsletmeOlcegi", table: "Isletme", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "TercihEdilenCalismaSekli", table: "Isletme", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "SektorDeneyimleri", table: "MuhasebeciProfil", type: "text", nullable: false, defaultValue: "Tüm");
            migrationBuilder.AddColumn<string>(name: "VergiMukellefiTipleri", table: "MuhasebeciProfil", type: "text", nullable: false, defaultValue: "Tüm");
            migrationBuilder.AddColumn<string>(name: "UygunIsletmeOlcekleri", table: "MuhasebeciProfil", type: "text", nullable: false, defaultValue: "Küçük, Orta");
            migrationBuilder.AddColumn<string>(name: "CalismaSekilleri", table: "MuhasebeciProfil", type: "text", nullable: false, defaultValue: "Tüm");
            migrationBuilder.AddColumn<string>(name: "Sektor", table: "MuhasebeciMusteriTalebi", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "VergiMukellefiTipi", table: "MuhasebeciMusteriTalebi", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IsletmeOlcegi", table: "MuhasebeciMusteriTalebi", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CalismaSekli", table: "MuhasebeciMusteriTalebi", type: "text", nullable: false, defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in new[] { "VergiMukellefiTipi", "IsletmeOlcegi", "TercihEdilenCalismaSekli" }) migrationBuilder.DropColumn(name: column, table: "Isletme");
            foreach (var column in new[] { "SektorDeneyimleri", "VergiMukellefiTipleri", "UygunIsletmeOlcekleri", "CalismaSekilleri" }) migrationBuilder.DropColumn(name: column, table: "MuhasebeciProfil");
            foreach (var column in new[] { "Sektor", "VergiMukellefiTipi", "IsletmeOlcegi", "CalismaSekli" }) migrationBuilder.DropColumn(name: column, table: "MuhasebeciMusteriTalebi");
        }
    }
}
