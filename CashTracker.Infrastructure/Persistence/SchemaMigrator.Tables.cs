using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static partial class SchemaMigrator
    {
        private static partial void EnsureKasaTable(CashTrackerDbContext db, DbConnection conn)
        {
            if (TableExists(conn, "Kasa"))
                return;

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Kasa (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL DEFAULT 1,
    Tarih TEXT NOT NULL,
    Tip TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    Kalem TEXT,
    GiderTuru TEXT,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureIsletmeTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Isletme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad TEXT NOT NULL,
    IsletmeTuru TEXT NOT NULL DEFAULT 'Genel',
    Konum TEXT NOT NULL DEFAULT '',
    KolayKurulumTamamlandi INTEGER NOT NULL DEFAULT 0,
    MuhasebeciVarMi INTEGER NOT NULL DEFAULT 0,
    IsAktif INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    TenantTipi TEXT NOT NULL DEFAULT 'Isletme',
    SahipKullaniciId INTEGER,
    ClerkOrganizationId TEXT,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);");
        }

        private static partial void EnsureKalemTanimiTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS KalemTanimi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tip TEXT NOT NULL,
    Ad TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureAppSettingTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS AppSetting (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureCariKartTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CariKart (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tip TEXT NOT NULL DEFAULT 'Musteri',
    Unvan TEXT NOT NULL,
    Telefon TEXT NOT NULL DEFAULT '',
    Eposta TEXT NOT NULL DEFAULT '',
    Adres TEXT NOT NULL DEFAULT '',
    VergiNoTc TEXT NOT NULL DEFAULT '',
    VergiDairesi TEXT NOT NULL DEFAULT '',
    Aktif INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureCariHareketTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CariHareket (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    HareketTipi TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    Kaynak TEXT NOT NULL DEFAULT 'Manuel',
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureUrunHizmetTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS UrunHizmet (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tip TEXT NOT NULL DEFAULT 'Urun',
    Ad TEXT NOT NULL,
    Barkod TEXT NOT NULL DEFAULT '',
    Birim TEXT NOT NULL DEFAULT 'Adet',
    KdvOrani NUMERIC NOT NULL DEFAULT 20,
    AlisFiyati NUMERIC NOT NULL DEFAULT 0,
    SatisFiyati NUMERIC NOT NULL DEFAULT 0,
    KritikStok NUMERIC NOT NULL DEFAULT 0,
    Aktif INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureStokHareketTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS StokHareket (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    UrunHizmetId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    Miktar NUMERIC NOT NULL DEFAULT 0,
    HareketTipi TEXT NOT NULL,
    Kaynak TEXT NOT NULL DEFAULT 'Manuel',
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureFaturaTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Fatura (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    VadeTarihi TEXT,
    FaturaTipi TEXT NOT NULL,
    Durum TEXT NOT NULL,
    YerelFaturaNo TEXT NOT NULL DEFAULT '',
    PortalBelgeNo TEXT NOT NULL DEFAULT '',
    PortalUuid TEXT NOT NULL DEFAULT '',
    AraToplam NUMERIC NOT NULL DEFAULT 0,
    IskontoToplam NUMERIC NOT NULL DEFAULT 0,
    KdvToplam NUMERIC NOT NULL DEFAULT 0,
    GenelToplam NUMERIC NOT NULL DEFAULT 0,
    OdenenTutar NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    Aciklama TEXT,
    KesildiAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureFaturaSatirTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS FaturaSatir (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    UrunHizmetId INTEGER,
    Aciklama TEXT NOT NULL,
    Birim TEXT NOT NULL DEFAULT 'Adet',
    Miktar NUMERIC NOT NULL DEFAULT 0,
    BirimFiyat NUMERIC NOT NULL DEFAULT 0,
    IskontoOrani NUMERIC NOT NULL DEFAULT 0,
    IskontoTutar NUMERIC NOT NULL DEFAULT 0,
    KdvOrani NUMERIC NOT NULL DEFAULT 20,
    KdvTutar NUMERIC NOT NULL DEFAULT 0,
    SatirNetTutar NUMERIC NOT NULL DEFAULT 0,
    SatirToplam NUMERIC NOT NULL DEFAULT 0,
    StokEtkilesin INTEGER NOT NULL DEFAULT 1
);");
        }

        private static partial void EnsureTahsilatOdemeTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS TahsilatOdeme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    Tip TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    KasaId INTEGER,
    CariHareketId INTEGER,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureBelgeDosyaTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BelgeDosya (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    BelgeTipi TEXT NOT NULL DEFAULT 'PDF',
    DosyaYolu TEXT NOT NULL,
    Kaynak TEXT NOT NULL DEFAULT 'Yerel',
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureGibPortalAyarTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS GibPortalAyar (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciKodu TEXT NOT NULL DEFAULT '',
    SifreCipherText TEXT NOT NULL DEFAULT '',
    TestModu INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureGibPortalIslemLogTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS GibPortalIslemLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER,
    Tarih TEXT NOT NULL,
    Islem TEXT NOT NULL,
    Basarili INTEGER NOT NULL DEFAULT 0,
    Mesaj TEXT NOT NULL DEFAULT ''
);");
        }

        private static partial void EnsureWebAuthTables(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Kullanici (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AuthProvider TEXT NOT NULL DEFAULT 'clerk',
    AuthProviderUserId TEXT NOT NULL,
    Eposta TEXT NOT NULL DEFAULT '',
    AdSoyad TEXT NOT NULL DEFAULT '',
    HesapTipi TEXT NOT NULL DEFAULT 'Isletme',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    SonGirisAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS IsletmeUyelik (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciId INTEGER,
    Rol TEXT NOT NULL DEFAULT 'isletme_sahibi',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    DavetEposta TEXT NOT NULL DEFAULT '',
    DavetKodu TEXT,
    DavetEdenKullaniciId INTEGER,
    DavetAt TEXT,
    KabulAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciMusteri (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER NOT NULL,
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    YetkiSeviyesi TEXT NOT NULL DEFAULT 'OkumaRapor',
    Kaynak TEXT NOT NULL DEFAULT 'Davet',
    TalepId INTEGER,
    DavetKodu TEXT,
    BaslangicAt TEXT NOT NULL,
    BitisAt TEXT,
    KabulAt TEXT,
    Notlar TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciProfil (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    Yayinda INTEGER NOT NULL DEFAULT 0,
    Unvan TEXT NOT NULL DEFAULT '',
    Konum TEXT NOT NULL DEFAULT '',
    Telefon TEXT NOT NULL DEFAULT '',
    DeneyimYili INTEGER NOT NULL DEFAULT 0,
    ProfilResmiUrl TEXT NOT NULL DEFAULT '',
    UcretBilgisi TEXT NOT NULL DEFAULT '',
    Uzmanliklar TEXT NOT NULL DEFAULT '',
    MusteriTipleri TEXT NOT NULL DEFAULT '',
    KisaAciklama TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciMusteriTalebi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER,
    TalepEdenIsletmeId INTEGER NOT NULL,
    Tur TEXT NOT NULL DEFAULT 'Pazaryeri',
    Durum TEXT NOT NULL DEFAULT 'Beklemede',
    YetkiSeviyesi TEXT NOT NULL DEFAULT 'OkumaRapor',
    DavetKodu TEXT NOT NULL DEFAULT '',
    Mesaj TEXT NOT NULL DEFAULT '',
    SonucAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciSohbetMesaji (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER NOT NULL,
    GonderenIsletmeId INTEGER NOT NULL,
    TalepId INTEGER,
    BaglantiId INTEGER,
    Mesaj TEXT NOT NULL DEFAULT '',
    OkunduAt TEXT,
    CreatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Abonelik (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    HesapTipi TEXT NOT NULL DEFAULT 'Isletme',
    PlanKodu TEXT NOT NULL DEFAULT 'isletme_ucretsiz',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    AylikTutar NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    DonemBaslangicAt TEXT NOT NULL,
    DonemBitisAt TEXT,
    DonemSonundaIptal INTEGER NOT NULL DEFAULT 0,
    IptalAt TEXT,
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    SaglayiciMusteriId TEXT NOT NULL DEFAULT '',
    SaglayiciAbonelikId TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS IsletmeDeneme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    PlanKodu TEXT NOT NULL DEFAULT 'isletme_baslangic',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    BaslangicAt TEXT NOT NULL,
    BitisAt TEXT NOT NULL,
    OdemeYontemiEklendi INTEGER NOT NULL DEFAULT 0,
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    SaglayiciMusteriId TEXT NOT NULL DEFAULT '',
    SaglayiciOdemeYontemiId TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS IsletmeEntitlement (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    PlanKodu TEXT NOT NULL DEFAULT 'isletme_ucretsiz',
    Kaynak TEXT NOT NULL DEFAULT 'Ucretsiz',
    OcrAktif INTEGER NOT NULL DEFAULT 0,
    GibAktif INTEGER NOT NULL DEFAULT 0,
    TelegramAktif INTEGER NOT NULL DEFAULT 0,
    AiAktif INTEGER NOT NULL DEFAULT 0,
    AiMesajLimiti INTEGER,
    KullaniciLimiti INTEGER,
    MusteriLimiti INTEGER,
    MuhasebeciPaneliAktif INTEGER NOT NULL DEFAULT 0,
    OneCikmaAktif INTEGER NOT NULL DEFAULT 0,
    DonemOtomasyonuAktif INTEGER NOT NULL DEFAULT 0,
    MusteriSaglikSkoruAktif INTEGER NOT NULL DEFAULT 0,
    SponsorMuhasebeciIsletmeId INTEGER,
    GecerliBaslangicAt TEXT NOT NULL,
    GecerliBitisAt TEXT,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS AiKullanimDonemi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    DonemAnahtari TEXT NOT NULL,
    MesajLimiti INTEGER,
    KullanilanMesaj INTEGER NOT NULL DEFAULT 0,
    DonemBaslangicAt TEXT NOT NULL,
    DonemBitisAt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureKasaColumns(CashTrackerDbContext db, DbConnection conn)
        {
            if (!ColumnExists(conn, "Kasa", "GiderTuru"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN GiderTuru TEXT");

            if (!ColumnExists(conn, "Kasa", "IsletmeId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN IsletmeId INTEGER NOT NULL DEFAULT 1");

            if (!ColumnExists(conn, "Kasa", "Kalem"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN Kalem TEXT");

            if (!ColumnExists(conn, "Kasa", "OdemeYontemi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit'");
        }

        private static partial void EnsureIsletmeColumns(CashTrackerDbContext db, DbConnection conn)
        {
            if (!ColumnExists(conn, "Isletme", "TenantTipi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN TenantTipi TEXT NOT NULL DEFAULT 'Isletme'");

            if (!ColumnExists(conn, "Isletme", "SahipKullaniciId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN SahipKullaniciId INTEGER");

            if (!ColumnExists(conn, "Isletme", "ClerkOrganizationId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN ClerkOrganizationId TEXT");

            if (!ColumnExists(conn, "Isletme", "IsletmeTuru"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN IsletmeTuru TEXT NOT NULL DEFAULT 'Genel'");

            if (!ColumnExists(conn, "Isletme", "Konum"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN Konum TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "Isletme", "KolayKurulumTamamlandi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN KolayKurulumTamamlandi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "Isletme", "MuhasebeciVarMi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN MuhasebeciVarMi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "Isletme", "UpdatedAt"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Isletme ADD COLUMN UpdatedAt TEXT NOT NULL DEFAULT '1970-01-01 00:00:00'");
                db.Database.ExecuteSqlRaw(@"
UPDATE Isletme
SET UpdatedAt = COALESCE(NULLIF(CreatedAt, ''), CURRENT_TIMESTAMP)
WHERE UpdatedAt = '1970-01-01 00:00:00' OR TRIM(UpdatedAt) = '';");
            }
        }

        private static partial void EnsureWebAuthColumns(CashTrackerDbContext db, DbConnection conn)
        {
            if (!ColumnExists(conn, "MuhasebeciMusteri", "YetkiSeviyesi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN YetkiSeviyesi TEXT NOT NULL DEFAULT 'OkumaRapor'");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "Kaynak"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN Kaynak TEXT NOT NULL DEFAULT 'Davet'");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "TalepId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN TalepId INTEGER");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "KabulAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN KabulAt TEXT");

            if (!ColumnExists(conn, "MuhasebeciProfil", "Telefon"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciProfil ADD COLUMN Telefon TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "MuhasebeciProfil", "DeneyimYili"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciProfil ADD COLUMN DeneyimYili INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "MuhasebeciProfil", "ProfilResmiUrl"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciProfil ADD COLUMN ProfilResmiUrl TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "MuhasebeciProfil", "UcretBilgisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciProfil ADD COLUMN UcretBilgisi TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "MuhasebeciSohbetMesaji", "OkunduAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciSohbetMesaji ADD COLUMN OkunduAt TEXT");
        }

        private static partial void EnsureIndexes(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId ON Kasa(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId_Tarih ON Kasa(IsletmeId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_IsAktif ON Isletme(IsAktif);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_MuhasebeciVarMi ON Isletme(MuhasebeciVarMi);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_IsletmeTuru ON Isletme(IsletmeTuru);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_TenantTipi ON Isletme(TenantTipi);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_SahipKullaniciId ON Isletme(SahipKullaniciId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Isletme_ClerkOrganizationId ON Isletme(ClerkOrganizationId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_KalemTanimi_IsletmeId ON KalemTanimi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_KalemTanimi_IsletmeId_Tip_Ad ON KalemTanimi(IsletmeId, Tip, Ad);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_AppSetting_Key ON AppSetting(Key);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Kullanici_AuthProvider_AuthProviderUserId ON Kullanici(AuthProvider, AuthProviderUserId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kullanici_Eposta ON Kullanici(Eposta);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kullanici_HesapTipi ON Kullanici(HesapTipi);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeUyelik_IsletmeId ON IsletmeUyelik(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeUyelik_KullaniciId ON IsletmeUyelik(KullaniciId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_IsletmeUyelik_IsletmeId_KullaniciId ON IsletmeUyelik(IsletmeId, KullaniciId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_IsletmeUyelik_DavetKodu ON IsletmeUyelik(DavetKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteri_MuhasebeciIsletmeId ON MuhasebeciMusteri(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteri_MusteriIsletmeId ON MuhasebeciMusteri(MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciMusteri_MuhasebeciIsletmeId_MusteriIsletmeId ON MuhasebeciMusteri(MuhasebeciIsletmeId, MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciMusteri_DavetKodu ON MuhasebeciMusteri(DavetKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteri_TalepId ON MuhasebeciMusteri(TalepId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteri_YetkiSeviyesi ON MuhasebeciMusteri(YetkiSeviyesi);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciProfil_MuhasebeciIsletmeId ON MuhasebeciProfil(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciProfil_Yayinda ON MuhasebeciProfil(Yayinda);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciProfil_Konum ON MuhasebeciProfil(Konum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciProfil_DeneyimYili ON MuhasebeciProfil(DeneyimYili);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteriTalebi_MuhasebeciIsletmeId ON MuhasebeciMusteriTalebi(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteriTalebi_MusteriIsletmeId ON MuhasebeciMusteriTalebi(MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteriTalebi_TalepEdenIsletmeId ON MuhasebeciMusteriTalebi(TalepEdenIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteriTalebi_Durum ON MuhasebeciMusteriTalebi(Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteriTalebi_DavetKodu ON MuhasebeciMusteriTalebi(DavetKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciMusteriTalebi_MuhasebeciIsletmeId_MusteriIsletmeId_Durum ON MuhasebeciMusteriTalebi(MuhasebeciIsletmeId, MusteriIsletmeId, Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_MuhasebeciIsletmeId ON MuhasebeciSohbetMesaji(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_MusteriIsletmeId ON MuhasebeciSohbetMesaji(MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_TalepId ON MuhasebeciSohbetMesaji(TalepId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_BaglantiId ON MuhasebeciSohbetMesaji(BaglantiId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_OkunduAt ON MuhasebeciSohbetMesaji(OkunduAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_MuhasebeciIsletmeId_MusteriIsletmeId_CreatedAt ON MuhasebeciSohbetMesaji(MuhasebeciIsletmeId, MusteriIsletmeId, CreatedAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_IsletmeId ON Abonelik(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_IsletmeId_Durum ON Abonelik(IsletmeId, Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_PlanKodu ON Abonelik(PlanKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_SaglayiciAbonelikId ON Abonelik(SaglayiciAbonelikId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeDeneme_IsletmeId ON IsletmeDeneme(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_IsletmeDeneme_IsletmeId_PlanKodu ON IsletmeDeneme(IsletmeId, PlanKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeDeneme_Durum ON IsletmeDeneme(Durum);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_IsletmeEntitlement_IsletmeId ON IsletmeEntitlement(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeEntitlement_PlanKodu ON IsletmeEntitlement(PlanKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeEntitlement_Kaynak ON IsletmeEntitlement(Kaynak);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeEntitlement_SponsorMuhasebeciIsletmeId ON IsletmeEntitlement(SponsorMuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AiKullanimDonemi_IsletmeId ON AiKullanimDonemi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_AiKullanimDonemi_IsletmeId_DonemAnahtari ON AiKullanimDonemi(IsletmeId, DonemAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariKart_IsletmeId ON CariKart(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariKart_IsletmeId_Unvan ON CariKart(IsletmeId, Unvan);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariKart_IsletmeId_VergiNoTc ON CariKart(IsletmeId, VergiNoTc);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariHareket_IsletmeId ON CariHareket(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariHareket_IsletmeId_CariKartId_Tarih ON CariHareket(IsletmeId, CariKartId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UrunHizmet_IsletmeId ON UrunHizmet(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UrunHizmet_IsletmeId_Barkod ON UrunHizmet(IsletmeId, Barkod);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StokHareket_IsletmeId ON StokHareket(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StokHareket_IsletmeId_UrunHizmetId_Tarih ON StokHareket(IsletmeId, UrunHizmetId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId ON Fatura(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId_Tarih ON Fatura(IsletmeId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId_CariKartId ON Fatura(IsletmeId, CariKartId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FaturaSatir_IsletmeId ON FaturaSatir(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FaturaSatir_IsletmeId_FaturaId ON FaturaSatir(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId ON TahsilatOdeme(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId_FaturaId ON TahsilatOdeme(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId_CariKartId_Tarih ON TahsilatOdeme(IsletmeId, CariKartId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BelgeDosya_IsletmeId ON BelgeDosya(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BelgeDosya_IsletmeId_FaturaId ON BelgeDosya(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_GibPortalAyar_IsletmeId ON GibPortalAyar(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GibPortalIslemLog_IsletmeId ON GibPortalIslemLog(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GibPortalIslemLog_IsletmeId_FaturaId_Tarih ON GibPortalIslemLog(IsletmeId, FaturaId, Tarih);");
        }
    }
}
