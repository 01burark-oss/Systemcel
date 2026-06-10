using CashTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public sealed class CashTrackerDbContext : DbContext
    {
        public CashTrackerDbContext(DbContextOptions<CashTrackerDbContext> options) : base(options) { }

        public DbSet<Kasa> Kasalar => Set<Kasa>();
        public DbSet<Isletme> Isletmeler => Set<Isletme>();
        public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
        public DbSet<IsletmeUyelik> IsletmeUyelikleri => Set<IsletmeUyelik>();
        public DbSet<MuhasebeciMusteri> MuhasebeciMusterileri => Set<MuhasebeciMusteri>();
        public DbSet<MuhasebeciProfil> MuhasebeciProfilleri => Set<MuhasebeciProfil>();
        public DbSet<MuhasebeciMusteriTalebi> MuhasebeciMusteriTalepleri => Set<MuhasebeciMusteriTalebi>();
        public DbSet<MuhasebeciSohbet> MuhasebeciSohbetleri => Set<MuhasebeciSohbet>();
        public DbSet<MuhasebeciSohbetMesaji> MuhasebeciSohbetMesajlari => Set<MuhasebeciSohbetMesaji>();
        public DbSet<MuhasebeciSohbetEki> MuhasebeciSohbetEkleri => Set<MuhasebeciSohbetEki>();
        public DbSet<MuhasebeciSohbetKatilimciDurumu> MuhasebeciSohbetKatilimciDurumlari => Set<MuhasebeciSohbetKatilimciDurumu>();
        public DbSet<MuhasebeciSohbetVeriIstegi> MuhasebeciSohbetVeriIstekleri => Set<MuhasebeciSohbetVeriIstegi>();
        public DbSet<Abonelik> Abonelikler => Set<Abonelik>();
        public DbSet<IsletmeDeneme> IsletmeDenemeleri => Set<IsletmeDeneme>();
        public DbSet<IsletmeEntitlement> IsletmeEntitlementlari => Set<IsletmeEntitlement>();
        public DbSet<AiKullanimDonemi> AiKullanimDonemleri => Set<AiKullanimDonemi>();
        public DbSet<KalemTanimi> KalemTanimlari => Set<KalemTanimi>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<CariKart> CariKartlari => Set<CariKart>();
        public DbSet<CariHareket> CariHareketleri => Set<CariHareket>();
        public DbSet<UrunHizmet> UrunHizmetleri => Set<UrunHizmet>();
        public DbSet<StokHareket> StokHareketleri => Set<StokHareket>();
        public DbSet<Fatura> Faturalar => Set<Fatura>();
        public DbSet<FaturaSatir> FaturaSatirlari => Set<FaturaSatir>();
        public DbSet<TahsilatOdeme> TahsilatOdemeleri => Set<TahsilatOdeme>();
        public DbSet<BelgeDosya> BelgeDosyalari => Set<BelgeDosya>();
        public DbSet<GibPortalAyar> GibPortalAyarlari => Set<GibPortalAyar>();
        public DbSet<GibPortalIslemLog> GibPortalIslemLoglari => Set<GibPortalIslemLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Kasa>(e =>
            {
                e.ToTable("Kasa");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.OdemeYontemi).IsRequired();
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tarih });
            });

            modelBuilder.Entity<Isletme>(e =>
            {
                e.ToTable("Isletme");
                e.HasKey(x => x.Id);
                e.Property(x => x.Ad).IsRequired();
                e.Property(x => x.IsletmeTuru).IsRequired();
                e.Property(x => x.Konum).IsRequired();
                e.Property(x => x.TenantTipi).IsRequired();
                e.HasIndex(x => x.IsAktif);
                e.HasIndex(x => x.MuhasebeciVarMi);
                e.HasIndex(x => x.IsletmeTuru);
                e.HasIndex(x => x.TenantTipi);
                e.HasIndex(x => x.SahipKullaniciId);
                e.HasIndex(x => x.ClerkOrganizationId).IsUnique();
            });

            modelBuilder.Entity<Kullanici>(e =>
            {
                e.ToTable("Kullanici");
                e.HasKey(x => x.Id);
                e.Property(x => x.AuthProvider).IsRequired();
                e.Property(x => x.AuthProviderUserId).IsRequired();
                e.Property(x => x.Eposta).IsRequired();
                e.Property(x => x.AdSoyad).IsRequired();
                e.Property(x => x.HesapTipi).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.HasIndex(x => new { x.AuthProvider, x.AuthProviderUserId }).IsUnique();
                e.HasIndex(x => x.Eposta);
                e.HasIndex(x => x.HesapTipi);
            });

            modelBuilder.Entity<IsletmeUyelik>(e =>
            {
                e.ToTable("IsletmeUyelik");
                e.HasKey(x => x.Id);
                e.Property(x => x.Rol).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.DavetEposta).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => x.KullaniciId);
                e.HasIndex(x => new { x.IsletmeId, x.KullaniciId }).IsUnique();
                e.HasIndex(x => x.DavetKodu).IsUnique();
            });

            modelBuilder.Entity<MuhasebeciMusteri>(e =>
            {
                e.ToTable("MuhasebeciMusteri");
                e.HasKey(x => x.Id);
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.YetkiSeviyesi).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.Property(x => x.Notlar).IsRequired();
                e.HasIndex(x => x.MuhasebeciIsletmeId);
                e.HasIndex(x => x.MusteriIsletmeId);
                e.HasIndex(x => new { x.MuhasebeciIsletmeId, x.MusteriIsletmeId }).IsUnique();
                e.HasIndex(x => x.DavetKodu).IsUnique();
                e.HasIndex(x => x.TalepId);
                e.HasIndex(x => x.YetkiSeviyesi);
            });

            modelBuilder.Entity<MuhasebeciProfil>(e =>
            {
                e.ToTable("MuhasebeciProfil");
                e.HasKey(x => x.Id);
                e.Property(x => x.Unvan).IsRequired();
                e.Property(x => x.Konum).IsRequired();
                e.Property(x => x.Telefon).IsRequired();
                e.Property(x => x.ProfilResmiUrl).IsRequired();
                e.Property(x => x.UcretBilgisi).IsRequired();
                e.Property(x => x.Uzmanliklar).IsRequired();
                e.Property(x => x.MusteriTipleri).IsRequired();
                e.Property(x => x.KisaAciklama).IsRequired();
                e.HasIndex(x => x.MuhasebeciIsletmeId).IsUnique();
                e.HasIndex(x => x.Yayinda);
                e.HasIndex(x => x.Konum);
                e.HasIndex(x => x.DeneyimYili);
            });

            modelBuilder.Entity<MuhasebeciMusteriTalebi>(e =>
            {
                e.ToTable("MuhasebeciMusteriTalebi");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tur).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.YetkiSeviyesi).IsRequired();
                e.Property(x => x.DavetKodu).IsRequired();
                e.Property(x => x.Mesaj).IsRequired();
                e.HasIndex(x => x.MuhasebeciIsletmeId);
                e.HasIndex(x => x.MusteriIsletmeId);
                e.HasIndex(x => x.TalepEdenIsletmeId);
                e.HasIndex(x => x.Durum);
                e.HasIndex(x => x.DavetKodu);
                e.HasIndex(x => new { x.MuhasebeciIsletmeId, x.MusteriIsletmeId, x.Durum });
            });

            modelBuilder.Entity<MuhasebeciSohbet>(e =>
            {
                e.ToTable("MuhasebeciSohbet");
                e.HasKey(x => x.Id);
                e.Property(x => x.Konu).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.HasIndex(x => x.MuhasebeciIsletmeId);
                e.HasIndex(x => x.MusteriIsletmeId);
                e.HasIndex(x => x.TalepId);
                e.HasIndex(x => x.BaglantiId);
                e.HasIndex(x => x.SonMesajAt);
                e.HasIndex(x => new { x.MuhasebeciIsletmeId, x.MusteriIsletmeId }).IsUnique();
            });

            modelBuilder.Entity<MuhasebeciSohbetMesaji>(e =>
            {
                e.ToTable("MuhasebeciSohbetMesaji");
                e.HasKey(x => x.Id);
                e.Property(x => x.MesajTipi).IsRequired();
                e.Property(x => x.ClientMessageId).IsRequired();
                e.Property(x => x.Mesaj).IsRequired();
                e.HasIndex(x => x.SohbetId);
                e.HasIndex(x => x.MuhasebeciIsletmeId);
                e.HasIndex(x => x.MusteriIsletmeId);
                e.HasIndex(x => x.TalepId);
                e.HasIndex(x => x.BaglantiId);
                e.HasIndex(x => x.OkunduAt);
                e.HasIndex(x => new { x.SohbetId, x.ClientMessageId });
                e.HasIndex(x => new { x.SohbetId, x.Id });
                e.HasIndex(x => new { x.MuhasebeciIsletmeId, x.MusteriIsletmeId, x.CreatedAt });
            });

            modelBuilder.Entity<MuhasebeciSohbetEki>(e =>
            {
                e.ToTable("MuhasebeciSohbetEki");
                e.HasKey(x => x.Id);
                e.Property(x => x.EkTipi).IsRequired();
                e.Property(x => x.DosyaAdi).IsRequired();
                e.Property(x => x.IcerikTipi).IsRequired();
                e.Property(x => x.DosyaYolu).IsRequired();
                e.Property(x => x.VeriTipi).IsRequired();
                e.Property(x => x.Baslik).IsRequired();
                e.Property(x => x.OzetJson).IsRequired();
                e.HasIndex(x => x.SohbetId);
                e.HasIndex(x => x.MesajId);
                e.HasIndex(x => x.YukleyenIsletmeId);
                e.HasIndex(x => x.EkTipi);
            });

            modelBuilder.Entity<MuhasebeciSohbetKatilimciDurumu>(e =>
            {
                e.ToTable("MuhasebeciSohbetKatilimciDurumu");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.SohbetId);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.SohbetId, x.IsletmeId }).IsUnique();
                e.HasIndex(x => x.Arsivlendi);
            });

            modelBuilder.Entity<MuhasebeciSohbetVeriIstegi>(e =>
            {
                e.ToTable("MuhasebeciSohbetVeriIstegi");
                e.HasKey(x => x.Id);
                e.Property(x => x.VeriTipi).IsRequired();
                e.Property(x => x.AralikKodu).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.Mesaj).IsRequired();
                e.HasIndex(x => x.SohbetId);
                e.HasIndex(x => x.IsteyenIsletmeId);
                e.HasIndex(x => x.HedefIsletmeId);
                e.HasIndex(x => x.Durum);
            });

            modelBuilder.Entity<Abonelik>(e =>
            {
                e.ToTable("Abonelik");
                e.HasKey(x => x.Id);
                e.Property(x => x.HesapTipi).IsRequired();
                e.Property(x => x.PlanKodu).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.AylikTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired();
                e.Property(x => x.OdemeSaglayici).IsRequired();
                e.Property(x => x.SaglayiciMusteriId).IsRequired();
                e.Property(x => x.SaglayiciAbonelikId).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Durum });
                e.HasIndex(x => x.PlanKodu);
                e.HasIndex(x => x.SaglayiciAbonelikId);
            });

            modelBuilder.Entity<IsletmeDeneme>(e =>
            {
                e.ToTable("IsletmeDeneme");
                e.HasKey(x => x.Id);
                e.Property(x => x.PlanKodu).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.OdemeSaglayici).IsRequired();
                e.Property(x => x.SaglayiciMusteriId).IsRequired();
                e.Property(x => x.SaglayiciOdemeYontemiId).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.PlanKodu }).IsUnique();
                e.HasIndex(x => x.Durum);
            });

            modelBuilder.Entity<IsletmeEntitlement>(e =>
            {
                e.ToTable("IsletmeEntitlement");
                e.HasKey(x => x.Id);
                e.Property(x => x.PlanKodu).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.HasIndex(x => x.IsletmeId).IsUnique();
                e.HasIndex(x => x.PlanKodu);
                e.HasIndex(x => x.Kaynak);
                e.HasIndex(x => x.SponsorMuhasebeciIsletmeId);
            });

            modelBuilder.Entity<AiKullanimDonemi>(e =>
            {
                e.ToTable("AiKullanimDonemi");
                e.HasKey(x => x.Id);
                e.Property(x => x.DonemAnahtari).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.DonemAnahtari }).IsUnique();
            });

            modelBuilder.Entity<KalemTanimi>(e =>
            {
                e.ToTable("KalemTanimi");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Ad).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tip, x.Ad }).IsUnique();
            });

            modelBuilder.Entity<AppSetting>(e =>
            {
                e.ToTable("AppSetting");
                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired();
                e.Property(x => x.Value).IsRequired();
                e.HasIndex(x => x.Key).IsUnique();
            });

            modelBuilder.Entity<CariKart>(e =>
            {
                e.ToTable("CariKart");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Unvan).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Unvan });
                e.HasIndex(x => new { x.IsletmeId, x.VergiNoTc });
            });

            modelBuilder.Entity<CariHareket>(e =>
            {
                e.ToTable("CariHareket");
                e.HasKey(x => x.Id);
                e.Property(x => x.HareketTipi).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId, x.Tarih });
            });

            modelBuilder.Entity<UrunHizmet>(e =>
            {
                e.ToTable("UrunHizmet");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Ad).IsRequired();
                e.Property(x => x.Barkod).IsRequired();
                e.Property(x => x.Birim).IsRequired();
                e.Property(x => x.KdvOrani).HasColumnType("NUMERIC");
                e.Property(x => x.AlisFiyati).HasColumnType("NUMERIC");
                e.Property(x => x.SatisFiyati).HasColumnType("NUMERIC");
                e.Property(x => x.KritikStok).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Barkod });
            });

            modelBuilder.Entity<StokHareket>(e =>
            {
                e.ToTable("StokHareket");
                e.HasKey(x => x.Id);
                e.Property(x => x.Miktar).HasColumnType("NUMERIC");
                e.Property(x => x.HareketTipi).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.UrunHizmetId, x.Tarih });
            });

            modelBuilder.Entity<Fatura>(e =>
            {
                e.ToTable("Fatura");
                e.HasKey(x => x.Id);
                e.Property(x => x.FaturaTipi).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.AraToplam).HasColumnType("NUMERIC");
                e.Property(x => x.IskontoToplam).HasColumnType("NUMERIC");
                e.Property(x => x.KdvToplam).HasColumnType("NUMERIC");
                e.Property(x => x.GenelToplam).HasColumnType("NUMERIC");
                e.Property(x => x.OdenenTutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId });
            });

            modelBuilder.Entity<FaturaSatir>(e =>
            {
                e.ToTable("FaturaSatir");
                e.HasKey(x => x.Id);
                e.Property(x => x.Miktar).HasColumnType("NUMERIC");
                e.Property(x => x.BirimFiyat).HasColumnType("NUMERIC");
                e.Property(x => x.IskontoOrani).HasColumnType("NUMERIC");
                e.Property(x => x.IskontoTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KdvOrani).HasColumnType("NUMERIC");
                e.Property(x => x.KdvTutar).HasColumnType("NUMERIC");
                e.Property(x => x.SatirNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.SatirToplam).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
            });

            modelBuilder.Entity<TahsilatOdeme>(e =>
            {
                e.ToTable("TahsilatOdeme");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId, x.Tarih });
            });

            modelBuilder.Entity<BelgeDosya>(e =>
            {
                e.ToTable("BelgeDosya");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
            });

            modelBuilder.Entity<GibPortalAyar>(e =>
            {
                e.ToTable("GibPortalAyar");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsletmeId).IsUnique();
            });

            modelBuilder.Entity<GibPortalIslemLog>(e =>
            {
                e.ToTable("GibPortalIslemLog");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId, x.Tarih });
            });

            ConfigureDateTimeColumns(modelBuilder);
        }

        private static void ConfigureDateTimeColumns(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (clrType == typeof(DateTime))
                        property.SetColumnType("timestamp without time zone");
                }
            }
        }
    }
}
