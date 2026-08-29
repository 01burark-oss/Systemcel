using CashTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public sealed class CashTrackerDbContext : DbContext
    {
        static CashTrackerDbContext()
        {
            // Mevcut Systemcel semasi bilincli olarak "timestamp without time zone" kullanir.
            // Npgsql 6+ UTC DateTime degerlerini bu tipe varsayilan olarak reddettigi icin,
            // tum istemcilerde ayni legacy sema sozlesmesini context olusmadan once etkinlestir.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public CashTrackerDbContext(DbContextOptions<CashTrackerDbContext> options) : base(options) { }

        public DbSet<Kasa> Kasalar => Set<Kasa>();
        public DbSet<NakitPlanKalemi> NakitPlanKalemleri => Set<NakitPlanKalemi>();
        public DbSet<Isletme> Isletmeler => Set<Isletme>();
        public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
        public DbSet<IsletmeUyelik> IsletmeUyelikleri => Set<IsletmeUyelik>();
        public DbSet<MuhasebeciMusteri> MuhasebeciMusterileri => Set<MuhasebeciMusteri>();
        public DbSet<MuhasebeciProfil> MuhasebeciProfilleri => Set<MuhasebeciProfil>();
        public DbSet<MuhasebeciMusteriTalebi> MuhasebeciMusteriTalepleri => Set<MuhasebeciMusteriTalebi>();
        public DbSet<MuhasebeciHizmetOdemesi> MuhasebeciHizmetOdemeleri => Set<MuhasebeciHizmetOdemesi>();
        public DbSet<MuhasebeciAktarimAlacagi> MuhasebeciAktarimAlacaklari => Set<MuhasebeciAktarimAlacagi>();
        public DbSet<MuhasebeciBaglantiDaveti> MuhasebeciBaglantiDavetleri => Set<MuhasebeciBaglantiDaveti>();
        public DbSet<MuhasebeciSohbet> MuhasebeciSohbetleri => Set<MuhasebeciSohbet>();
        public DbSet<MuhasebeciSohbetMesaji> MuhasebeciSohbetMesajlari => Set<MuhasebeciSohbetMesaji>();
        public DbSet<MuhasebeciSohbetEki> MuhasebeciSohbetEkleri => Set<MuhasebeciSohbetEki>();
        public DbSet<MuhasebeciSohbetKatilimciDurumu> MuhasebeciSohbetKatilimciDurumlari => Set<MuhasebeciSohbetKatilimciDurumu>();
        public DbSet<MuhasebeciSohbetVeriIstegi> MuhasebeciSohbetVeriIstekleri => Set<MuhasebeciSohbetVeriIstegi>();
        public DbSet<Abonelik> Abonelikler => Set<Abonelik>();
        public DbSet<IsletmeDeneme> IsletmeDenemeleri => Set<IsletmeDeneme>();
        public DbSet<AbonelikOnayi> AbonelikOnaylari => Set<AbonelikOnayi>();
        public DbSet<OdemeIslemi> OdemeIslemleri => Set<OdemeIslemi>();
        public DbSet<OdemeOlayi> OdemeOlaylari => Set<OdemeOlayi>();
        public DbSet<KurucuKampanyaHakki> KurucuKampanyaHaklari => Set<KurucuKampanyaHakki>();
        public DbSet<IsletmeEntitlement> IsletmeEntitlementlari => Set<IsletmeEntitlement>();
        public DbSet<AiKullanimDonemi> AiKullanimDonemleri => Set<AiKullanimDonemi>();
        public DbSet<KalemTanimi> KalemTanimlari => Set<KalemTanimi>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<CariKart> CariKartlari => Set<CariKart>();
        public DbSet<CariHareket> CariHareketleri => Set<CariHareket>();
        public DbSet<UrunHizmet> UrunHizmetleri => Set<UrunHizmet>();
        public DbSet<StokHareket> StokHareketleri => Set<StokHareket>();
        public DbSet<StokDepo> StokDepolari => Set<StokDepo>();
        public DbSet<StokDefterIslemi> StokDefterIslemleri => Set<StokDefterIslemi>();
        public DbSet<Sube> Subeler => Set<Sube>();
        public DbSet<DovizKuru> DovizKurlari => Set<DovizKuru>();
        public DbSet<Fatura> Faturalar => Set<Fatura>();
        public DbSet<FaturaSatir> FaturaSatirlari => Set<FaturaSatir>();
        public DbSet<TahsilatOdeme> TahsilatOdemeleri => Set<TahsilatOdeme>();
        public DbSet<OdemeHatirlatma> OdemeHatirlatmalari => Set<OdemeHatirlatma>();
        public DbSet<FaturaMusteriOnayi> FaturaMusteriOnaylari => Set<FaturaMusteriOnayi>();
        public DbSet<BelgeDosya> BelgeDosyalari => Set<BelgeDosya>();
        public DbSet<GibPortalAyar> GibPortalAyarlari => Set<GibPortalAyar>();
        public DbSet<GibPortalIslemLog> GibPortalIslemLoglari => Set<GibPortalIslemLog>();
        public DbSet<DesktopImportCode> DesktopImportKodlari => Set<DesktopImportCode>();
        public DbSet<YonetimDenetimKaydi> YonetimDenetimKayitlari => Set<YonetimDenetimKaydi>();
        public DbSet<DestekTalebi> DestekTalepleri => Set<DestekTalebi>();
        public DbSet<BildirimKaydi> BildirimKayitlari => Set<BildirimKaydi>();
        public DbSet<BildirimTercihi> BildirimTercihleri => Set<BildirimTercihi>();
        public DbSet<BildirimTeslimOutbox> BildirimTeslimOutboxlari => Set<BildirimTeslimOutbox>();
        public DbSet<BankaHareketi> BankaHareketleri => Set<BankaHareketi>();
        public DbSet<GelistiriciApiAnahtari> GelistiriciApiAnahtarlari => Set<GelistiriciApiAnahtari>();

        public override int SaveChanges()
        {
            NormalizeTimestampWithoutTimeZoneValues();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeTimestampWithoutTimeZoneValues();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeTimestampWithoutTimeZoneValues();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            NormalizeTimestampWithoutTimeZoneValues();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Kasa>(e =>
            {
                e.ToTable("Kasa");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.OdemeYontemi).IsRequired();
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.Property(x => x.OrijinalTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KurSnapshot).HasColumnType("NUMERIC");
                e.Property(x => x.TryKarsiligi).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId, x.Tarih });
                e.HasOne<Sube>().WithMany().HasForeignKey(x => x.SubeId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Sube>(e =>
            {
                e.ToTable("Sube");
                e.HasKey(x => x.Id);
                e.Property(x => x.Ad).IsRequired().HasMaxLength(120);
                e.Property(x => x.Kod).IsRequired().HasMaxLength(24);
                e.Property(x => x.OlusturmaAnahtari).IsRequired().HasMaxLength(120);
                e.Property(x => x.IcerikOzeti).IsRequired().HasMaxLength(64);
                e.HasIndex(x => new { x.IsletmeId, x.Kod }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.OlusturmaAnahtari }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.Varsayilan });
                e.HasOne<Isletme>().WithMany().HasForeignKey(x => x.IsletmeId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DovizKuru>(e =>
            {
                e.ToTable("DovizKuru");
                e.HasKey(x => x.Id);
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.Kur).HasColumnType("NUMERIC");
                e.Property(x => x.OlusturmaAnahtari).IsRequired().HasMaxLength(120);
                e.Property(x => x.IcerikOzeti).IsRequired().HasMaxLength(64);
                e.HasIndex(x => new { x.IsletmeId, x.OlusturmaAnahtari }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.ParaBirimi, x.GecerliAt });
                e.HasOne<Isletme>().WithMany().HasForeignKey(x => x.IsletmeId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NakitPlanKalemi>(e =>
            {
                e.ToTable("NakitPlanKalemi");
                e.HasKey(x => x.Id);
                e.Property(x => x.Ad).IsRequired().HasMaxLength(120);
                e.Property(x => x.Tip).IsRequired().HasMaxLength(16);
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.Property(x => x.TekrarTipi).IsRequired().HasMaxLength(16);
                e.Property(x => x.Kategori).IsRequired().HasMaxLength(80);
                e.Property(x => x.Aciklama).HasMaxLength(500);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Aktif, x.IlkTarih });
                e.HasOne<Isletme>()
                    .WithMany()
                    .HasForeignKey(x => x.IsletmeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Isletme>(e =>
            {
                e.ToTable("Isletme");
                e.HasKey(x => x.Id);
                e.Property(x => x.Ad).IsRequired();
                e.Property(x => x.IsletmeTuru).IsRequired();
                e.Property(x => x.VergiMukellefiTipi).IsRequired();
                e.Property(x => x.IsletmeOlcegi).IsRequired();
                e.Property(x => x.TercihEdilenCalismaSekli).IsRequired();
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
                e.Property(x => x.SektorDeneyimleri).IsRequired();
                e.Property(x => x.VergiMukellefiTipleri).IsRequired();
                e.Property(x => x.UygunIsletmeOlcekleri).IsRequired();
                e.Property(x => x.CalismaSekilleri).IsRequired();
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
                e.Property(x => x.AylikHizmetBedeli).HasColumnType("NUMERIC");
                e.Property(x => x.Sektor).IsRequired();
                e.Property(x => x.VergiMukellefiTipi).IsRequired();
                e.Property(x => x.IsletmeOlcegi).IsRequired();
                e.Property(x => x.CalismaSekli).IsRequired();
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
                e.Property(x => x.FaturalamaDonemi).IsRequired();
                e.Property(x => x.KampanyaKodu).IsRequired();
                e.Property(x => x.YenilemeDonemTutari).HasColumnType("NUMERIC");
                e.Property(x => x.DonemTutari).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired();
                e.Property(x => x.OdemeSaglayici).IsRequired();
                e.Property(x => x.SaglayiciMusteriId).IsRequired();
                e.Property(x => x.SaglayiciAbonelikId).IsRequired();
                e.Property(x => x.PlanlananPlanKodu).IsRequired();
                e.Property(x => x.PlanlananFaturalamaDonemi).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Durum });
                e.HasIndex(x => x.PlanKodu);
                e.HasIndex(x => x.SaglayiciAbonelikId);
            });

            modelBuilder.Entity<IsletmeDeneme>(e =>
            {
                e.ToTable("IsletmeDeneme");
                e.HasKey(x => x.Id);
                e.Property(x => x.HesapTipi).IsRequired();
                e.Property(x => x.PlanKodu).IsRequired();
                e.Property(x => x.FaturalamaDonemi).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.OdemeSaglayici).IsRequired();
                e.Property(x => x.SaglayiciMusteriId).IsRequired();
                e.Property(x => x.SaglayiciOdemeYontemiId).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.HesapTipi }).IsUnique();
                e.HasIndex(x => x.Durum);
            });

            modelBuilder.Entity<MuhasebeciBaglantiDaveti>(e =>
            {
                e.ToTable("MuhasebeciBaglantiDaveti");
                e.HasKey(x => x.Id);
                e.Property(x => x.TokenHash).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.YetkiSeviyesi).IsRequired();
                e.Property(x => x.Mesaj).IsRequired();
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => x.MusteriIsletmeId);
                e.HasIndex(x => x.MuhasebeciIsletmeId);
                e.HasIndex(x => new { x.MusteriIsletmeId, x.Durum });
            });

            modelBuilder.Entity<MuhasebeciHizmetOdemesi>(e =>
            {
                e.ToTable("MuhasebeciHizmetOdemesi");
                e.HasKey(x => x.Id);
                e.Property(x => x.AylikHizmetBedeli).HasColumnType("NUMERIC");
                e.Property(x => x.HizmetDonemi).IsRequired().HasMaxLength(7);
                e.Property(x => x.PlatformKomisyonOrani).HasColumnType("NUMERIC");
                e.Property(x => x.PlatformKomisyonTutari).HasColumnType("NUMERIC");
                e.Property(x => x.AktarilacakTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.TahsilEdilenTutar).HasColumnType("NUMERIC");
                e.HasIndex(x => new { x.TalepId, x.HizmetDonemi }).IsUnique();
                e.HasIndex(x => x.OdemeIslemiId).IsUnique();
                e.HasIndex(x => new { x.MusteriIsletmeId, x.Durum });
            });

            modelBuilder.Entity<MuhasebeciAktarimAlacagi>(e =>
            {
                e.ToTable("MuhasebeciAktarimAlacagi");
                e.HasKey(x => x.Id);
                e.Property(x => x.TahsilEdilenTutar).HasColumnType("NUMERIC");
                e.Property(x => x.PlatformKomisyonTutari).HasColumnType("NUMERIC");
                e.Property(x => x.AktarilacakTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.AktarimDonemi).IsRequired().HasMaxLength(7);
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.AktarimReferansi).IsRequired();
                // One service payment can have an original positive accrual and a later
                // negative refund adjustment after that accrual has already been paid out.
                e.HasIndex(x => x.MuhasebeciHizmetOdemesiId);
                e.HasIndex(x => new { x.MuhasebeciIsletmeId, x.AktarimDonemi, x.Durum });
                e.HasIndex(x => x.AktarimReferansi);
            });

            modelBuilder.Entity<YonetimDenetimKaydi>(e =>
            {
                e.ToTable("YonetimDenetimKaydi");
                e.HasKey(x => x.Id);
                e.Property(x => x.AktorProviderKullaniciId).IsRequired();
                e.Property(x => x.Islem).IsRequired();
                e.Property(x => x.KaynakTuru).IsRequired();
                e.Property(x => x.OncekiDeger).IsRequired();
                e.Property(x => x.YeniDeger).IsRequired();
                e.Property(x => x.Gerekce).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => x.CreatedAt);
            });

            modelBuilder.Entity<DestekTalebi>(e =>
            {
                e.ToTable("DestekTalebi");
                e.HasKey(x => x.Id);
                e.Property(x => x.OlusturanKullaniciReferansi).IsRequired().HasMaxLength(200);
                e.Property(x => x.OlusturmaAnahtari).IsRequired().HasMaxLength(100);
                e.Property(x => x.Konu).IsRequired().HasMaxLength(120);
                e.Property(x => x.Kategori).IsRequired().HasMaxLength(30);
                e.Property(x => x.Aciklama).IsRequired().HasMaxLength(4000);
                e.Property(x => x.Oncelik).IsRequired().HasMaxLength(20);
                e.Property(x => x.Durum).IsRequired().HasMaxLength(20);
                e.Property(x => x.YoneticiYaniti).IsRequired().HasMaxLength(1000);
                e.HasIndex(x => new { x.IsletmeId, x.OlusturmaAnahtari }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.CreatedAt });
                e.HasIndex(x => new { x.Oncelik, x.CreatedAt });
                e.HasIndex(x => x.Durum);
            });

            modelBuilder.Entity<BildirimKaydi>(e =>
            {
                e.ToTable("BildirimKaydi");
                e.HasKey(x => x.Id);
                e.Property(x => x.KullaniciRef).IsRequired().HasMaxLength(200);
                e.Property(x => x.KaynakAnahtari).IsRequired().HasMaxLength(160);
                e.Property(x => x.Tur).IsRequired().HasMaxLength(30);
                e.Property(x => x.Onem).IsRequired().HasMaxLength(20);
                e.Property(x => x.Baslik).IsRequired().HasMaxLength(200);
                e.Property(x => x.Mesaj).IsRequired().HasMaxLength(1000);
                e.Property(x => x.Aksiyon).IsRequired().HasMaxLength(120);
                e.Property(x => x.Url).IsRequired().HasMaxLength(500);
                e.HasIndex(x => new { x.IsletmeId, x.KullaniciRef, x.KaynakAnahtari }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.KullaniciRef, x.OkunduAt });
            });

            modelBuilder.Entity<BildirimTercihi>(e =>
            {
                e.ToTable("BildirimTercihi");
                e.HasKey(x => x.Id);
                e.Property(x => x.KullaniciRef).IsRequired().HasMaxLength(200);
                e.Property(x => x.SaatDilimi).IsRequired().HasMaxLength(60);
                e.HasIndex(x => new { x.IsletmeId, x.KullaniciRef }).IsUnique();
            });

            modelBuilder.Entity<BildirimTeslimOutbox>(e =>
            {
                e.ToTable("BildirimTeslimOutbox");
                e.HasKey(x => x.Id);
                e.Property(x => x.KullaniciRef).IsRequired().HasMaxLength(200);
                e.Property(x => x.IdempotencyAnahtari).IsRequired().HasMaxLength(160);
                e.Property(x => x.Kanal).IsRequired().HasMaxLength(20);
                e.Property(x => x.Durum).IsRequired().HasMaxLength(30);
                e.Property(x => x.PayloadJson).IsRequired().HasMaxLength(4000);
                e.Property(x => x.ClaimToken).IsRequired().HasMaxLength(64);
                e.Property(x => x.SonHataKodu).IsRequired().HasMaxLength(80);
                e.HasIndex(x => new { x.IsletmeId, x.KullaniciRef, x.Kanal, x.IdempotencyAnahtari }).IsUnique();
                e.HasIndex(x => new { x.Durum, x.SonrakiDenemeAt, x.ClaimBitisAt });
                e.HasIndex(x => new { x.IsletmeId, x.KullaniciRef });
            });

            modelBuilder.Entity<BankaHareketi>(e =>
            {
                e.ToTable("BankaHareketi");
                e.HasKey(x => x.Id);
                e.Property(x => x.Aciklama).IsRequired().HasMaxLength(500);
                e.Property(x => x.Tutar).HasColumnType("NUMERIC(18,2)");
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.Durum).IsRequired().HasMaxLength(20);
                e.Property(x => x.KaynakHash).IsRequired().HasMaxLength(64);
                e.Property(x => x.EslesenKaynakTuru).IsRequired().HasMaxLength(30);
                e.HasIndex(x => new { x.IsletmeId, x.KaynakHash }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.Durum, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.EslesenKaynakTuru, x.EslesenKaynakId });
                e.HasOne<Isletme>()
                    .WithMany()
                    .HasForeignKey(x => x.IsletmeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GelistiriciApiAnahtari>(e =>
            {
                e.ToTable("GelistiriciApiAnahtari");
                e.HasKey(x => x.Id);
                e.Property(x => x.OlusturanKullaniciRef).IsRequired().HasMaxLength(200);
                e.Property(x => x.Ad).IsRequired().HasMaxLength(100);
                e.Property(x => x.Prefix).IsRequired().HasMaxLength(21);
                e.Property(x => x.AnahtarHash).IsRequired().HasMaxLength(32);
                e.Property(x => x.ScopeListesi).IsRequired().HasMaxLength(500);
                e.Property(x => x.RevokedByUserRef).IsRequired().HasMaxLength(200);
                e.HasIndex(x => x.Prefix).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.CreatedAt });
                e.HasIndex(x => new { x.IsletmeId, x.RevokedAt, x.ExpiresAt });
                e.HasOne<Isletme>()
                    .WithMany()
                    .HasForeignKey(x => x.IsletmeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AbonelikOnayi>(e =>
            {
                e.ToTable("AbonelikOnayi");
                e.HasKey(x => x.Id);
                e.Property(x => x.KullaniciRef).IsRequired();
                e.Property(x => x.CheckoutAnahtari).IsRequired();
                e.Property(x => x.HesapTipi).IsRequired();
                e.Property(x => x.PlanKodu).IsRequired();
                e.Property(x => x.FaturalamaDonemi).IsRequired();
                e.Property(x => x.KampanyaKodu).IsRequired();
                e.Property(x => x.MetinSurumu).IsRequired();
                e.Property(x => x.MetinHash).IsRequired();
                e.Property(x => x.IstemciIpHash).IsRequired();
                e.Property(x => x.UserAgentHash).IsRequired();
                e.Property(x => x.NetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ListeNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.YenilemeNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KdvOrani).HasColumnType("NUMERIC");
                e.Property(x => x.KdvTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ToplamTutar).HasColumnType("NUMERIC");
                e.Property(x => x.TamDonemNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KistKrediNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.DegisiklikTipi).IsRequired();
                e.Property(x => x.ParaBirimi).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.CheckoutAnahtari }).IsUnique();
            });

            modelBuilder.Entity<OdemeIslemi>(e =>
            {
                e.ToTable("OdemeIslemi");
                e.HasKey(x => x.Id);
                e.Property(x => x.CheckoutAnahtari).IsRequired();
                e.Property(x => x.HesapTipi).IsRequired();
                e.Property(x => x.PlanKodu).IsRequired();
                e.Property(x => x.FaturalamaDonemi).IsRequired();
                e.Property(x => x.KampanyaKodu).IsRequired();
                e.Property(x => x.IslemTipi).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.OdemeSaglayici).IsRequired();
                e.Property(x => x.SaglayiciOturumId).IsRequired();
                e.Property(x => x.SaglayiciIslemId).IsRequired();
                e.Property(x => x.CheckoutUrl).IsRequired();
                e.Property(x => x.NetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ListeNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.YenilemeNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KdvOrani).HasColumnType("NUMERIC");
                e.Property(x => x.KdvTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ToplamTutar).HasColumnType("NUMERIC");
                e.Property(x => x.TamDonemNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KistKrediNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired();
                e.Property(x => x.HataKodu).IsRequired();
                e.Property(x => x.HataMesaji).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.CheckoutAnahtari }).IsUnique();
                e.HasIndex(x => x.SaglayiciOturumId);
                e.HasIndex(x => x.SaglayiciIslemId);
            });

            modelBuilder.Entity<KurucuKampanyaHakki>(e =>
            {
                e.ToTable("KurucuKampanyaHakki");
                e.HasKey(x => x.Id);
                e.Property(x => x.KampanyaKodu).IsRequired();
                e.Property(x => x.CheckoutAnahtari).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.HasIndex(x => new { x.KampanyaKodu, x.IsletmeId }).IsUnique();
                e.HasIndex(x => new { x.KampanyaKodu, x.SiraNo }).IsUnique();
                e.HasIndex(x => x.CheckoutAnahtari).IsUnique();
                e.HasIndex(x => new { x.KampanyaKodu, x.Durum, x.RezervasyonBitisAt });
            });

            modelBuilder.Entity<OdemeOlayi>(e =>
            {
                e.ToTable("OdemeOlayi");
                e.HasKey(x => x.Id);
                e.Property(x => x.OdemeSaglayici).IsRequired();
                e.Property(x => x.OlayId).IsRequired();
                e.Property(x => x.OlayTipi).IsRequired();
                e.Property(x => x.CheckoutAnahtari).IsRequired();
                e.Property(x => x.SaglayiciIslemId).IsRequired();
                e.Property(x => x.IslenmeDurumu).IsRequired();
                e.Property(x => x.PayloadHash).IsRequired();
                e.Property(x => x.HataMesaji).IsRequired();
                e.HasIndex(x => new { x.OdemeSaglayici, x.OlayId }).IsUnique();
                e.HasIndex(x => x.CheckoutAnahtari);
                e.HasIndex(x => x.SaglayiciIslemId);
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
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.KurSnapshot).HasColumnType("NUMERIC");
                e.Property(x => x.TryKarsiligi).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId, x.Tarih });
                e.HasOne<Sube>()
                    .WithMany()
                    .HasForeignKey(x => x.SubeId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.KurSnapshot).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Barkod });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId, x.Ad });
                e.HasOne<Sube>()
                    .WithMany()
                    .HasForeignKey(x => x.SubeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StokHareket>(e =>
            {
                e.ToTable("StokHareket");
                e.HasKey(x => x.Id);
                e.Property(x => x.Miktar).HasColumnType("NUMERIC");
                e.Property(x => x.RezerveMiktar).HasColumnType("NUMERIC");
                e.Property(x => x.BirimMaliyet).HasColumnType("NUMERIC");
                e.Property(x => x.MaliyetParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.MaliyetKurSnapshot).HasColumnType("NUMERIC");
                e.Property(x => x.BirimMaliyetTry).HasColumnType("NUMERIC");
                e.Property(x => x.HareketTipi).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.UrunHizmetId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.DepoId, x.UrunHizmetId });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId, x.Tarih });
                e.HasIndex(x => x.StokDefterIslemiId);
                e.HasOne<Sube>()
                    .WithMany()
                    .HasForeignKey(x => x.SubeId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne<StokDepo>()
                    .WithMany()
                    .HasForeignKey(x => x.DepoId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne<StokDefterIslemi>()
                    .WithMany()
                    .HasForeignKey(x => x.StokDefterIslemiId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StokDepo>(e =>
            {
                e.ToTable("StokDepo");
                e.HasKey(x => x.Id);
                e.Property(x => x.Ad).IsRequired().HasMaxLength(120);
                e.Property(x => x.Kod).IsRequired().HasMaxLength(32);
                e.Property(x => x.Konum).HasMaxLength(240);
                e.HasIndex(x => new { x.IsletmeId, x.Kod }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.Varsayilan });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId });
                e.HasOne<Isletme>()
                    .WithMany()
                    .HasForeignKey(x => x.IsletmeId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne<Sube>()
                    .WithMany()
                    .HasForeignKey(x => x.SubeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StokDefterIslemi>(e =>
            {
                e.ToTable("StokDefterIslemi");
                e.HasKey(x => x.Id);
                e.Property(x => x.IslemAnahtari).IsRequired().HasMaxLength(120);
                e.Property(x => x.IcerikOzeti).IsRequired().HasMaxLength(64);
                e.Property(x => x.IslemTipi).IsRequired().HasMaxLength(32);
                e.Property(x => x.Aciklama).HasMaxLength(500);
                e.HasIndex(x => new { x.IsletmeId, x.IslemAnahtari }).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.TersKayitKaynakIslemId }).IsUnique();
                e.HasOne<Isletme>()
                    .WithMany()
                    .HasForeignKey(x => x.IsletmeId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne<StokDefterIslemi>()
                    .WithMany()
                    .HasForeignKey(x => x.TersKayitKaynakIslemId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.KurSnapshot).HasColumnType("NUMERIC");
                e.Property(x => x.GenelToplamTry).HasColumnType("NUMERIC");
                e.Property(x => x.OdenenTutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId });
                e.HasIndex(x => new { x.IsletmeId, x.FaturaTipi, x.Durum, x.VadeTarihi });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.HizliSatisAnahtari }).IsUnique();
                e.HasOne<Sube>()
                    .WithMany()
                    .HasForeignKey(x => x.SubeId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.KurSnapshot).HasColumnType("NUMERIC");
                e.Property(x => x.TryKarsiligi).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.SubeId, x.Tarih });
                e.HasOne<Sube>()
                    .WithMany()
                    .HasForeignKey(x => x.SubeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OdemeHatirlatma>(e =>
            {
                e.ToTable("OdemeHatirlatma");
                e.HasKey(x => x.Id);
                e.Property(x => x.AliciEposta).IsRequired().HasMaxLength(320);
                e.Property(x => x.Konu).IsRequired().HasMaxLength(240);
                e.Property(x => x.Durum).IsRequired().HasMaxLength(24);
                e.Property(x => x.Hata).IsRequired().HasMaxLength(500);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId, x.GonderildiAt });
            });

            modelBuilder.Entity<FaturaMusteriOnayi>(e =>
            {
                e.ToTable("FaturaMusteriOnayi");
                e.HasKey(x => x.Id);
                e.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
                e.Property(x => x.Durum).IsRequired().HasMaxLength(24);
                e.Property(x => x.IsletmeAdi).IsRequired().HasMaxLength(160);
                e.Property(x => x.CariUnvan).IsRequired().HasMaxLength(200);
                e.Property(x => x.CariVergiNoMaskeli).IsRequired().HasMaxLength(32);
                e.Property(x => x.CariAdres).IsRequired().HasMaxLength(500);
                e.Property(x => x.AliciTelefonMaskeli).IsRequired().HasMaxLength(32);
                e.Property(x => x.FaturaNo).IsRequired().HasMaxLength(80);
                e.Property(x => x.FaturaToplami).HasColumnType("NUMERIC");
                e.Property(x => x.ParaBirimi).IsRequired().HasMaxLength(3);
                e.Property(x => x.Saglayici).IsRequired().HasMaxLength(32);
                e.Property(x => x.SaglayiciIslemId).IsRequired().HasMaxLength(120);
                e.Property(x => x.Hata).IsRequired().HasMaxLength(500);
                e.Property(x => x.YanitNotu).IsRequired().HasMaxLength(500);
                e.Property(x => x.IstemciIpHash).IsRequired().HasMaxLength(64);
                e.Property(x => x.UserAgentHash).IsRequired().HasMaxLength(64);
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId, x.CreatedAt });
                e.HasIndex(x => new { x.IsletmeId, x.Durum, x.SonGecerlilikAt });
                e.HasOne<Fatura>()
                    .WithMany()
                    .HasForeignKey(x => x.FaturaId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            modelBuilder.Entity<DesktopImportCode>(e =>
            {
                e.ToTable("DesktopImportCode");
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).IsRequired().HasMaxLength(32);
                e.Property(x => x.Status).IsRequired().HasMaxLength(20);
                e.Property(x => x.RequestedBy).IsRequired().HasMaxLength(200);
                e.Property(x => x.PackageId).IsRequired().HasMaxLength(100);
                e.Property(x => x.ImportedTotalsJson).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
                e.HasIndex(x => new { x.RequestedBy, x.Status, x.ExpiresAtUtc });
                e.HasIndex(x => x.TargetIsletmeId);
                e.HasOne<Isletme>()
                    .WithMany()
                    .HasForeignKey(x => x.TargetIsletmeId)
                    .OnDelete(DeleteBehavior.Restrict);
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

        private void NormalizeTimestampWithoutTimeZoneValues()
        {
            foreach (var entry in ChangeTracker.Entries()
                         .Where(x => x.State is EntityState.Added or EntityState.Modified))
            {
                foreach (var property in entry.Properties)
                {
                    var clrType = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
                    if (clrType != typeof(DateTime) ||
                        property.CurrentValue is not DateTime value ||
                        value.Kind == DateTimeKind.Unspecified)
                    {
                        continue;
                    }

                    property.CurrentValue = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
                }
            }
        }
    }
}
