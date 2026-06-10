using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(CashTrackerDbContext))]
    [Migration("20260529191000_PublishApprovedAccountantProfiles")]
    public partial class PublishApprovedAccountantProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""MuhasebeciProfil"" p
SET ""Yayinda"" = TRUE,
    ""UpdatedAt"" = NOW()
WHERE p.""Yayinda"" = FALSE
  AND NULLIF(TRIM(p.""Telefon""), '') IS NOT NULL
  AND NULLIF(TRIM(p.""ProfilResmiUrl""), '') IS NOT NULL
  AND NULLIF(TRIM(p.""UcretBilgisi""), '') IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM ""Isletme"" i
      WHERE i.""Id"" = p.""MuhasebeciIsletmeId""
        AND i.""TenantTipi"" = 'Muhasebeci'
        AND (
            EXISTS (
                SELECT 1
                FROM ""Kullanici"" k
                WHERE k.""Id"" = i.""SahipKullaniciId""
                  AND k.""HesapTipi"" = 'Muhasebeci'
                  AND k.""Durum"" = 'Aktif'
            )
            OR EXISTS (
                SELECT 1
                FROM ""IsletmeUyelik"" iu
                INNER JOIN ""Kullanici"" k ON k.""Id"" = iu.""KullaniciId""
                WHERE iu.""IsletmeId"" = i.""Id""
                  AND iu.""Durum"" = 'Aktif'
                  AND k.""HesapTipi"" = 'Muhasebeci'
                  AND k.""Durum"" = 'Aktif'
            )
        )
  );");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
