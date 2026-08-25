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
    SubeId INTEGER,
    OrijinalTutar NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    KurSnapshot NUMERIC NOT NULL DEFAULT 1,
    TryKarsiligi NUMERIC NOT NULL DEFAULT 0,
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
    SubeId INTEGER,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    KurSnapshot NUMERIC NOT NULL DEFAULT 1,
    TryKarsiligi NUMERIC NOT NULL DEFAULT 0,
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
    SubeId INTEGER,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    KurSnapshot NUMERIC NOT NULL DEFAULT 1,
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
    DepoId INTEGER,
    StokDefterIslemiId INTEGER,
    SubeId INTEGER,
    Tarih TEXT NOT NULL,
    Miktar NUMERIC NOT NULL DEFAULT 0,
    RezerveMiktar NUMERIC NOT NULL DEFAULT 0,
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
    SubeId INTEGER,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    KurSnapshot NUMERIC NOT NULL DEFAULT 1,
    GenelToplamTry NUMERIC NOT NULL DEFAULT 0,
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
    SubeId INTEGER,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    KurSnapshot NUMERIC NOT NULL DEFAULT 1,
    TryKarsiligi NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    KasaId INTEGER,
    CariHareketId INTEGER,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureOdemeHatirlatmaTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS OdemeHatirlatma (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    AliciEposta TEXT NOT NULL DEFAULT '',
    Konu TEXT NOT NULL DEFAULT '',
    Durum TEXT NOT NULL DEFAULT '',
    Hata TEXT NOT NULL DEFAULT '',
    GonderildiAt TEXT,
    CreatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS StokDepo (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    SubeId INTEGER,
    Ad TEXT NOT NULL,
    Kod TEXT NOT NULL,
    Konum TEXT,
    Varsayilan INTEGER NOT NULL DEFAULT 0,
    Aktif INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_StokDepo_IsletmeId_Kod ON StokDepo(IsletmeId, Kod);
CREATE INDEX IF NOT EXISTS IX_StokDepo_IsletmeId_Varsayilan ON StokDepo(IsletmeId, Varsayilan);

CREATE TABLE IF NOT EXISTS StokDefterIslemi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    IslemAnahtari TEXT NOT NULL,
    IcerikOzeti TEXT NOT NULL,
    IslemTipi TEXT NOT NULL,
    TersKayitKaynakIslemId INTEGER,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_StokDefterIslemi_IsletmeId_IslemAnahtari ON StokDefterIslemi(IsletmeId, IslemAnahtari);
CREATE UNIQUE INDEX IF NOT EXISTS IX_StokDefterIslemi_IsletmeId_TersKayitKaynakIslemId ON StokDefterIslemi(IsletmeId, TersKayitKaynakIslemId);
CREATE INDEX IF NOT EXISTS IX_StokDefterIslemi_TersKayitKaynakIslemId ON StokDefterIslemi(TersKayitKaynakIslemId);
");

            var conn = db.Database.GetDbConnection();
            if (!ColumnExists(conn, "StokHareket", "DepoId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE StokHareket ADD COLUMN DepoId INTEGER;");
            if (!ColumnExists(conn, "StokHareket", "StokDefterIslemiId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE StokHareket ADD COLUMN StokDefterIslemiId INTEGER;");
            if (!ColumnExists(conn, "StokHareket", "RezerveMiktar"))
                db.Database.ExecuteSqlRaw("ALTER TABLE StokHareket ADD COLUMN RezerveMiktar NUMERIC NOT NULL DEFAULT 0;");

            db.Database.ExecuteSqlRaw(@"
CREATE INDEX IF NOT EXISTS IX_StokHareket_IsletmeId_DepoId_UrunHizmetId ON StokHareket(IsletmeId, DepoId, UrunHizmetId);
CREATE INDEX IF NOT EXISTS IX_StokHareket_DepoId ON StokHareket(DepoId);
CREATE INDEX IF NOT EXISTS IX_StokHareket_StokDefterIslemiId ON StokHareket(StokDefterIslemiId);
INSERT INTO StokDepo (IsletmeId, Ad, Kod, Konum, Varsayilan, Aktif, CreatedAt, UpdatedAt)
SELECT Id, 'Merkez Depo', 'MERKEZ', NULL, 1, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM Isletme i
WHERE NOT EXISTS (SELECT 1 FROM StokDepo d WHERE d.IsletmeId = i.Id AND d.Varsayilan = 1);
");
        }

        private static partial void EnsureFaturaMusteriOnayiTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS FaturaMusteriOnayi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    TokenHash TEXT NOT NULL,
    Durum TEXT NOT NULL DEFAULT 'Bekliyor',
    IsletmeAdi TEXT NOT NULL DEFAULT '',
    CariUnvan TEXT NOT NULL DEFAULT '',
    CariVergiNoMaskeli TEXT NOT NULL DEFAULT '',
    CariAdres TEXT NOT NULL DEFAULT '',
    AliciTelefonMaskeli TEXT NOT NULL DEFAULT '',
    FaturaNo TEXT NOT NULL DEFAULT '',
    FaturaTarihi TEXT NOT NULL,
    FaturaToplami NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    Saglayici TEXT NOT NULL DEFAULT '',
    SaglayiciIslemId TEXT NOT NULL DEFAULT '',
    Hata TEXT NOT NULL DEFAULT '',
    GonderildiAt TEXT,
    SonGecerlilikAt TEXT NOT NULL,
    YanitAt TEXT,
    YanitNotu TEXT NOT NULL DEFAULT '',
    IstemciIpHash TEXT NOT NULL DEFAULT '',
    UserAgentHash TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureSubeKurTablesAndColumns(CashTrackerDbContext db, DbConnection conn)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Sube (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Ad TEXT NOT NULL,
    Kod TEXT NOT NULL,
    Varsayilan INTEGER NOT NULL DEFAULT 0,
    Aktif INTEGER NOT NULL DEFAULT 1,
    OlusturmaAnahtari TEXT NOT NULL,
    IcerikOzeti TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Sube_IsletmeId_Kod ON Sube(IsletmeId, Kod);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Sube_IsletmeId_OlusturmaAnahtari ON Sube(IsletmeId, OlusturmaAnahtari);
CREATE INDEX IF NOT EXISTS IX_Sube_IsletmeId_Varsayilan ON Sube(IsletmeId, Varsayilan);

CREATE TABLE IF NOT EXISTS DovizKuru (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    ParaBirimi TEXT NOT NULL,
    Kur NUMERIC NOT NULL,
    GecerliAt TEXT NOT NULL,
    OlusturmaAnahtari TEXT NOT NULL,
    IcerikOzeti TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_DovizKuru_IsletmeId_OlusturmaAnahtari ON DovizKuru(IsletmeId, OlusturmaAnahtari);
CREATE INDEX IF NOT EXISTS IX_DovizKuru_IsletmeId_ParaBirimi_GecerliAt ON DovizKuru(IsletmeId, ParaBirimi, GecerliAt);
");

            AddColumnIfMissing(db, conn, "Kasa", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "Kasa", "OrijinalTutar", "NUMERIC NOT NULL DEFAULT 0");
            AddColumnIfMissing(db, conn, "Kasa", "ParaBirimi", "TEXT NOT NULL DEFAULT 'TRY'");
            AddColumnIfMissing(db, conn, "Kasa", "KurSnapshot", "NUMERIC NOT NULL DEFAULT 1");
            AddColumnIfMissing(db, conn, "Kasa", "TryKarsiligi", "NUMERIC NOT NULL DEFAULT 0");
            AddColumnIfMissing(db, conn, "CariHareket", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "CariHareket", "ParaBirimi", "TEXT NOT NULL DEFAULT 'TRY'");
            AddColumnIfMissing(db, conn, "CariHareket", "KurSnapshot", "NUMERIC NOT NULL DEFAULT 1");
            AddColumnIfMissing(db, conn, "CariHareket", "TryKarsiligi", "NUMERIC NOT NULL DEFAULT 0");
            AddColumnIfMissing(db, conn, "UrunHizmet", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "UrunHizmet", "ParaBirimi", "TEXT NOT NULL DEFAULT 'TRY'");
            AddColumnIfMissing(db, conn, "UrunHizmet", "KurSnapshot", "NUMERIC NOT NULL DEFAULT 1");
            AddColumnIfMissing(db, conn, "StokHareket", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "StokDepo", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "Fatura", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "Fatura", "ParaBirimi", "TEXT NOT NULL DEFAULT 'TRY'");
            AddColumnIfMissing(db, conn, "Fatura", "KurSnapshot", "NUMERIC NOT NULL DEFAULT 1");
            AddColumnIfMissing(db, conn, "Fatura", "GenelToplamTry", "NUMERIC NOT NULL DEFAULT 0");
            AddColumnIfMissing(db, conn, "TahsilatOdeme", "SubeId", "INTEGER");
            AddColumnIfMissing(db, conn, "TahsilatOdeme", "ParaBirimi", "TEXT NOT NULL DEFAULT 'TRY'");
            AddColumnIfMissing(db, conn, "TahsilatOdeme", "KurSnapshot", "NUMERIC NOT NULL DEFAULT 1");
            AddColumnIfMissing(db, conn, "TahsilatOdeme", "TryKarsiligi", "NUMERIC NOT NULL DEFAULT 0");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa SET OrijinalTutar = Tutar WHERE OrijinalTutar = 0;
UPDATE Kasa SET TryKarsiligi = Tutar WHERE TryKarsiligi = 0;
UPDATE CariHareket SET TryKarsiligi = Tutar WHERE TryKarsiligi = 0;
UPDATE Fatura SET GenelToplamTry = GenelToplam WHERE GenelToplamTry = 0;
UPDATE TahsilatOdeme SET TryKarsiligi = Tutar WHERE TryKarsiligi = 0;
CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId_SubeId_Tarih ON Kasa(IsletmeId, SubeId, Tarih);
CREATE INDEX IF NOT EXISTS IX_CariHareket_IsletmeId_SubeId_Tarih ON CariHareket(IsletmeId, SubeId, Tarih);
CREATE INDEX IF NOT EXISTS IX_UrunHizmet_IsletmeId_SubeId_Ad ON UrunHizmet(IsletmeId, SubeId, Ad);
CREATE INDEX IF NOT EXISTS IX_StokHareket_IsletmeId_SubeId_Tarih ON StokHareket(IsletmeId, SubeId, Tarih);
CREATE INDEX IF NOT EXISTS IX_StokDepo_IsletmeId_SubeId ON StokDepo(IsletmeId, SubeId);
CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId_SubeId_Tarih ON Fatura(IsletmeId, SubeId, Tarih);
CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId_SubeId_Tarih ON TahsilatOdeme(IsletmeId, SubeId, Tarih);
");
        }

        private static void AddColumnIfMissing(CashTrackerDbContext db, DbConnection conn, string table, string column, string definition)
        {
            if (!ColumnExists(conn, table, column))
            {
                using var command = conn.CreateCommand();
                command.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
                command.ExecuteNonQuery();
            }
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
    AylikHizmetBedeli NUMERIC NOT NULL DEFAULT 0,
    SonucAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciSohbetMesaji (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SohbetId INTEGER,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER NOT NULL,
    GonderenIsletmeId INTEGER NOT NULL,
    TalepId INTEGER,
    BaglantiId INTEGER,
    MesajTipi TEXT NOT NULL DEFAULT 'Metin',
    ClientMessageId TEXT NOT NULL DEFAULT '',
    Mesaj TEXT NOT NULL DEFAULT '',
    OkunduAt TEXT,
    CreatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciSohbet (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER NOT NULL,
    TalepId INTEGER,
    BaglantiId INTEGER,
    Konu TEXT NOT NULL DEFAULT '',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    SonMesajAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciSohbetEki (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SohbetId INTEGER NOT NULL,
    MesajId INTEGER,
    YukleyenIsletmeId INTEGER NOT NULL,
    EkTipi TEXT NOT NULL DEFAULT 'Dosya',
    DosyaAdi TEXT NOT NULL DEFAULT '',
    IcerikTipi TEXT NOT NULL DEFAULT '',
    DosyaYolu TEXT NOT NULL DEFAULT '',
    Boyut INTEGER NOT NULL DEFAULT 0,
    VeriTipi TEXT NOT NULL DEFAULT '',
    Baslik TEXT NOT NULL DEFAULT '',
    OzetJson TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciSohbetKatilimciDurumu (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SohbetId INTEGER NOT NULL,
    IsletmeId INTEGER NOT NULL,
    Arsivlendi INTEGER NOT NULL DEFAULT 0,
    ArsivlendiAt TEXT,
    SonOkumaAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciSohbetVeriIstegi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SohbetId INTEGER NOT NULL,
    IsteyenIsletmeId INTEGER NOT NULL,
    HedefIsletmeId INTEGER NOT NULL,
    VeriTipi TEXT NOT NULL DEFAULT 'GelirGiderOzeti',
    AralikKodu TEXT NOT NULL DEFAULT 'last30',
    Baslangic TEXT NOT NULL,
    Bitis TEXT NOT NULL,
    Durum TEXT NOT NULL DEFAULT 'Beklemede',
    SonucEkId INTEGER,
    Mesaj TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Abonelik (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    HesapTipi TEXT NOT NULL DEFAULT 'Isletme',
    PlanKodu TEXT NOT NULL DEFAULT 'isletme_ucretsiz',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    AylikTutar NUMERIC NOT NULL DEFAULT 0,
    FaturalamaDonemi TEXT NOT NULL DEFAULT 'Aylik',
    EkMusteriKredisi INTEGER NOT NULL DEFAULT 0,
    DonemTutari NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    DonemBaslangicAt TEXT NOT NULL,
    DonemBitisAt TEXT,
    DonemSonundaIptal INTEGER NOT NULL DEFAULT 0,
    IptalAt TEXT,
    OdemeSorunuAt TEXT,
    ToleransBitisAt TEXT,
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    SaglayiciMusteriId TEXT NOT NULL DEFAULT '',
    SaglayiciAbonelikId TEXT NOT NULL DEFAULT '',
    PlanlananPlanKodu TEXT NOT NULL DEFAULT '',
    PlanlananFaturalamaDonemi TEXT NOT NULL DEFAULT '',
    PlanlananEkMusteriKredisi INTEGER,
    PlanlananDegisiklikAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS IsletmeDeneme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    HesapTipi TEXT NOT NULL DEFAULT 'Isletme',
    PlanKodu TEXT NOT NULL DEFAULT 'isletme_baslangic',
    FaturalamaDonemi TEXT NOT NULL DEFAULT 'Aylik',
    EkMusteriKredisi INTEGER NOT NULL DEFAULT 0,
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    BaslangicAt TEXT NOT NULL,
    BitisAt TEXT NOT NULL,
    OdemeYontemiEklendi INTEGER NOT NULL DEFAULT 0,
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    SaglayiciMusteriId TEXT NOT NULL DEFAULT '',
    SaglayiciOdemeYontemiId TEXT NOT NULL DEFAULT '',
    DonemSonundaIptal INTEGER NOT NULL DEFAULT 0,
    IptalAt TEXT,
    YediGunHatirlatmaAt TEXT,
    UcGunHatirlatmaAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciBaglantiDaveti (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MusteriIsletmeId INTEGER NOT NULL,
    MuhasebeciIsletmeId INTEGER,
    TokenHash TEXT NOT NULL,
    Durum TEXT NOT NULL DEFAULT 'Beklemede',
    YetkiSeviyesi TEXT NOT NULL DEFAULT 'OkumaRapor',
    Mesaj TEXT NOT NULL DEFAULT '',
    SonGecerlilikAt TEXT NOT NULL,
    KabulAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciHizmetOdemesi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TalepId INTEGER NOT NULL,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER NOT NULL,
    OdemeIslemiId INTEGER,
    AylikHizmetBedeli NUMERIC NOT NULL DEFAULT 0,
    HizmetDonemi TEXT NOT NULL DEFAULT '',
    VadeAt TEXT NOT NULL,
    PlatformKomisyonOrani NUMERIC NOT NULL DEFAULT 0,
    PlatformKomisyonTutari NUMERIC NOT NULL DEFAULT 0,
    AktarilacakTutar NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    Durum TEXT NOT NULL DEFAULT 'OdemeBekliyor',
    TahsilEdilenTutar NUMERIC NOT NULL DEFAULT 0,
    TahsilEdildiAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS DestekTalebi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    OlusturanKullaniciReferansi TEXT NOT NULL DEFAULT '',
    OlusturmaAnahtari TEXT NOT NULL,
    Konu TEXT NOT NULL,
    Kategori TEXT NOT NULL,
    Aciklama TEXT NOT NULL,
    Oncelik TEXT NOT NULL DEFAULT 'Standart',
    Durum TEXT NOT NULL DEFAULT 'Acik',
    YoneticiYaniti TEXT NOT NULL DEFAULT '',
    CozulduAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BildirimKaydi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciRef TEXT NOT NULL,
    KaynakAnahtari TEXT NOT NULL,
    Tur TEXT NOT NULL,
    Onem TEXT NOT NULL DEFAULT 'orta',
    Baslik TEXT NOT NULL,
    Mesaj TEXT NOT NULL,
    Aksiyon TEXT NOT NULL DEFAULT '',
    Url TEXT NOT NULL DEFAULT '',
    OkunduAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_BildirimKaydi_IsletmeId_KullaniciRef_KaynakAnahtari ON BildirimKaydi(IsletmeId, KullaniciRef, KaynakAnahtari);
CREATE INDEX IF NOT EXISTS IX_BildirimKaydi_IsletmeId_KullaniciRef_OkunduAt ON BildirimKaydi(IsletmeId, KullaniciRef, OkunduAt);
");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BildirimTercihi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciRef TEXT NOT NULL,
    UygulamaAktif INTEGER NOT NULL DEFAULT 1,
    EpostaAktif INTEGER NOT NULL DEFAULT 0,
    TelegramAktif INTEGER NOT NULL DEFAULT 0,
    SessizSaatAktif INTEGER NOT NULL DEFAULT 0,
    SessizBaslangicDakika INTEGER NOT NULL DEFAULT 1320,
    SessizBitisDakika INTEGER NOT NULL DEFAULT 480,
    SaatDilimi TEXT NOT NULL DEFAULT 'Europe/Istanbul',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_BildirimTercihi_IsletmeId_KullaniciRef ON BildirimTercihi(IsletmeId, KullaniciRef);
");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BildirimTeslimOutbox (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciRef TEXT NOT NULL,
    BildirimId INTEGER,
    IdempotencyAnahtari TEXT NOT NULL,
    Kanal TEXT NOT NULL,
    Durum TEXT NOT NULL DEFAULT 'Bekliyor',
    PayloadJson TEXT NOT NULL DEFAULT '{{}}',
    DenemeSayisi INTEGER NOT NULL DEFAULT 0,
    SonrakiDenemeAt TEXT NOT NULL,
    ClaimToken TEXT NOT NULL DEFAULT '',
    ClaimBitisAt TEXT,
    SonHataKodu TEXT NOT NULL DEFAULT '',
    TeslimEdildiAt TEXT,
    DeadLetterAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_BildirimTeslimOutbox_IsletmeId_KullaniciRef_Kanal_IdempotencyAnahtari ON BildirimTeslimOutbox(IsletmeId, KullaniciRef, Kanal, IdempotencyAnahtari);
CREATE INDEX IF NOT EXISTS IX_BildirimTeslimOutbox_Durum_SonrakiDenemeAt_ClaimBitisAt ON BildirimTeslimOutbox(Durum, SonrakiDenemeAt, ClaimBitisAt);
CREATE INDEX IF NOT EXISTS IX_BildirimTeslimOutbox_IsletmeId_KullaniciRef ON BildirimTeslimOutbox(IsletmeId, KullaniciRef);
");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BankaHareketi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    Aciklama TEXT NOT NULL,
    Tutar NUMERIC NOT NULL,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    Durum TEXT NOT NULL DEFAULT 'Acik',
    KaynakHash TEXT NOT NULL,
    EslesenKaynakTuru TEXT NOT NULL DEFAULT '',
    EslesenKaynakId INTEGER,
    EslestiAt TEXT,
    YokSayildiAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (IsletmeId) REFERENCES Isletme(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_BankaHareketi_IsletmeId_KaynakHash ON BankaHareketi(IsletmeId, KaynakHash);
CREATE INDEX IF NOT EXISTS IX_BankaHareketi_IsletmeId_Durum_Tarih ON BankaHareketi(IsletmeId, Durum, Tarih);
CREATE INDEX IF NOT EXISTS IX_BankaHareketi_IsletmeId_EslesenKaynakTuru_EslesenKaynakId ON BankaHareketi(IsletmeId, EslesenKaynakTuru, EslesenKaynakId);
");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS GelistiriciApiAnahtari (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    OlusturanKullaniciRef TEXT NOT NULL,
    Ad TEXT NOT NULL,
    Prefix TEXT NOT NULL,
    AnahtarHash BLOB NOT NULL,
    ScopeListesi TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    LastUsedAt TEXT,
    ExpiresAt TEXT NOT NULL,
    RevokedAt TEXT,
    RevokedByUserRef TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (IsletmeId) REFERENCES Isletme(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_GelistiriciApiAnahtari_Prefix ON GelistiriciApiAnahtari(Prefix);
CREATE INDEX IF NOT EXISTS IX_GelistiriciApiAnahtari_IsletmeId_CreatedAt ON GelistiriciApiAnahtari(IsletmeId, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_GelistiriciApiAnahtari_IsletmeId_RevokedAt_ExpiresAt ON GelistiriciApiAnahtari(IsletmeId, RevokedAt, ExpiresAt);
");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS MuhasebeciAktarimAlacagi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MuhasebeciHizmetOdemesiId INTEGER NOT NULL,
    MuhasebeciIsletmeId INTEGER NOT NULL,
    MusteriIsletmeId INTEGER NOT NULL,
    TalepId INTEGER NOT NULL,
    TahsilEdilenTutar NUMERIC NOT NULL DEFAULT 0,
    PlatformKomisyonTutari NUMERIC NOT NULL DEFAULT 0,
    AktarilacakTutar NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    AktarimDonemi TEXT NOT NULL DEFAULT '',
    Durum TEXT NOT NULL DEFAULT 'Bekliyor',
    AktarimReferansi TEXT NOT NULL DEFAULT '',
    TahakkukAt TEXT NOT NULL,
    AktarildiAt TEXT,
    TersKayitAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS AbonelikOnayi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciRef TEXT NOT NULL DEFAULT '',
    CheckoutAnahtari TEXT NOT NULL DEFAULT '',
    HesapTipi TEXT NOT NULL DEFAULT 'Isletme',
    PlanKodu TEXT NOT NULL DEFAULT '',
    FaturalamaDonemi TEXT NOT NULL DEFAULT 'Aylik',
    EkMusteriKredisi INTEGER NOT NULL DEFAULT 0,
    MetinSurumu TEXT NOT NULL DEFAULT '',
    MetinHash TEXT NOT NULL DEFAULT '',
    IstemciIpHash TEXT NOT NULL DEFAULT '',
    UserAgentHash TEXT NOT NULL DEFAULT '',
    NetTutar NUMERIC NOT NULL DEFAULT 0,
    TamDonemNetTutar NUMERIC NOT NULL DEFAULT 0,
    KistKrediNetTutar NUMERIC NOT NULL DEFAULT 0,
    DegisiklikTipi TEXT NOT NULL DEFAULT '',
    KdvOrani NUMERIC NOT NULL DEFAULT 0,
    KdvTutar NUMERIC NOT NULL DEFAULT 0,
    ToplamTutar NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    OnayAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS OdemeIslemi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    CheckoutAnahtari TEXT NOT NULL DEFAULT '',
    HesapTipi TEXT NOT NULL DEFAULT 'Isletme',
    PlanKodu TEXT NOT NULL DEFAULT '',
    FaturalamaDonemi TEXT NOT NULL DEFAULT 'Aylik',
    EkMusteriKredisi INTEGER NOT NULL DEFAULT 0,
    IslemTipi TEXT NOT NULL DEFAULT 'DenemeKartYetkilendirme',
    Durum TEXT NOT NULL DEFAULT 'Hazirlaniyor',
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    SaglayiciOturumId TEXT NOT NULL DEFAULT '',
    SaglayiciIslemId TEXT NOT NULL DEFAULT '',
    CheckoutUrl TEXT NOT NULL DEFAULT '',
    CheckoutExpiresAt TEXT,
    NetTutar NUMERIC NOT NULL DEFAULT 0,
    TamDonemNetTutar NUMERIC NOT NULL DEFAULT 0,
    KistKrediNetTutar NUMERIC NOT NULL DEFAULT 0,
    DegisiklikTipi TEXT NOT NULL DEFAULT '',
    HedefDonemBitisAt TEXT,
    KdvOrani NUMERIC NOT NULL DEFAULT 0,
    KdvTutar NUMERIC NOT NULL DEFAULT 0,
    ToplamTutar NUMERIC NOT NULL DEFAULT 0,
    ParaBirimi TEXT NOT NULL DEFAULT 'TRY',
    HataKodu TEXT NOT NULL DEFAULT '',
    HataMesaji TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    TamamlandiAt TEXT,
    SonOlayAt TEXT
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS KurucuKampanyaHakki (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KampanyaKodu TEXT NOT NULL DEFAULT '',
    SiraNo INTEGER NOT NULL,
    CheckoutAnahtari TEXT NOT NULL DEFAULT '',
    Durum TEXT NOT NULL DEFAULT 'Rezerve',
    RezerveAt TEXT NOT NULL,
    RezervasyonBitisAt TEXT NOT NULL,
    KazanildiAt TEXT,
    UpdatedAt TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS OdemeOlayi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    OlayId TEXT NOT NULL DEFAULT '',
    OlayTipi TEXT NOT NULL DEFAULT '',
    CheckoutAnahtari TEXT NOT NULL DEFAULT '',
    SaglayiciIslemId TEXT NOT NULL DEFAULT '',
    IslenmeDurumu TEXT NOT NULL DEFAULT 'Alindi',
    PayloadHash TEXT NOT NULL DEFAULT '',
    HataMesaji TEXT NOT NULL DEFAULT '',
    SaglayiciAt TEXT NOT NULL,
    AlindiAt TEXT NOT NULL,
    IslendiAt TEXT
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
            if (!ColumnExists(conn, "Abonelik", "FaturalamaDonemi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN FaturalamaDonemi TEXT NOT NULL DEFAULT 'Aylik'");

            if (!ColumnExists(conn, "Abonelik", "DonemTutari"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN DonemTutari NUMERIC NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "Abonelik", "EkMusteriKredisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN EkMusteriKredisi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "Abonelik", "OdemeSorunuAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN OdemeSorunuAt TEXT");

            if (!ColumnExists(conn, "Abonelik", "ToleransBitisAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN ToleransBitisAt TEXT");

            if (!ColumnExists(conn, "Abonelik", "KampanyaKodu"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN KampanyaKodu TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "Abonelik", "YenilemeDonemTutari"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN YenilemeDonemTutari NUMERIC NOT NULL DEFAULT 0");
                db.Database.ExecuteSqlRaw("UPDATE Abonelik SET YenilemeDonemTutari = DonemTutari WHERE YenilemeDonemTutari = 0");
            }

            if (!ColumnExists(conn, "Abonelik", "IndirimliDonemKalan"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN IndirimliDonemKalan INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "Abonelik", "PlanlananPlanKodu"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN PlanlananPlanKodu TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(conn, "Abonelik", "PlanlananFaturalamaDonemi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN PlanlananFaturalamaDonemi TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(conn, "Abonelik", "PlanlananEkMusteriKredisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN PlanlananEkMusteriKredisi INTEGER");
            if (!ColumnExists(conn, "Abonelik", "PlanlananDegisiklikAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Abonelik ADD COLUMN PlanlananDegisiklikAt TEXT");

            if (!ColumnExists(conn, "IsletmeDeneme", "HesapTipi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN HesapTipi TEXT NOT NULL DEFAULT 'Isletme'");

            if (!ColumnExists(conn, "IsletmeDeneme", "FaturalamaDonemi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN FaturalamaDonemi TEXT NOT NULL DEFAULT 'Aylik'");

            if (!ColumnExists(conn, "IsletmeDeneme", "EkMusteriKredisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN EkMusteriKredisi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "AbonelikOnayi", "EkMusteriKredisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN EkMusteriKredisi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "AbonelikOnayi", "KampanyaKodu"))
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN KampanyaKodu TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "AbonelikOnayi", "ListeNetTutar"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN ListeNetTutar NUMERIC NOT NULL DEFAULT 0");
                db.Database.ExecuteSqlRaw("UPDATE AbonelikOnayi SET ListeNetTutar = NetTutar WHERE ListeNetTutar = 0");
            }

            if (!ColumnExists(conn, "AbonelikOnayi", "YenilemeNetTutar"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN YenilemeNetTutar NUMERIC NOT NULL DEFAULT 0");
                db.Database.ExecuteSqlRaw("UPDATE AbonelikOnayi SET YenilemeNetTutar = NetTutar WHERE YenilemeNetTutar = 0");
            }

            if (!ColumnExists(conn, "AbonelikOnayi", "TamDonemNetTutar"))
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN TamDonemNetTutar NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "AbonelikOnayi", "KistKrediNetTutar"))
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN KistKrediNetTutar NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "AbonelikOnayi", "DegisiklikTipi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE AbonelikOnayi ADD COLUMN DegisiklikTipi TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "OdemeIslemi", "EkMusteriKredisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN EkMusteriKredisi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "OdemeIslemi", "KampanyaKodu"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN KampanyaKodu TEXT NOT NULL DEFAULT ''");

            if (!ColumnExists(conn, "OdemeIslemi", "ListeNetTutar"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN ListeNetTutar NUMERIC NOT NULL DEFAULT 0");
                db.Database.ExecuteSqlRaw("UPDATE OdemeIslemi SET ListeNetTutar = NetTutar WHERE ListeNetTutar = 0");
            }

            if (!ColumnExists(conn, "OdemeIslemi", "YenilemeNetTutar"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN YenilemeNetTutar NUMERIC NOT NULL DEFAULT 0");
                db.Database.ExecuteSqlRaw("UPDATE OdemeIslemi SET YenilemeNetTutar = NetTutar WHERE YenilemeNetTutar = 0");
            }

            if (!ColumnExists(conn, "OdemeIslemi", "IndirimliDonemSayisi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN IndirimliDonemSayisi INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "OdemeIslemi", "TamDonemNetTutar"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN TamDonemNetTutar NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "OdemeIslemi", "KistKrediNetTutar"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN KistKrediNetTutar NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "OdemeIslemi", "DegisiklikTipi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN DegisiklikTipi TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(conn, "OdemeIslemi", "HedefDonemBitisAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE OdemeIslemi ADD COLUMN HedefDonemBitisAt TEXT");

            if (!ColumnExists(conn, "IsletmeDeneme", "DonemSonundaIptal"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN DonemSonundaIptal INTEGER NOT NULL DEFAULT 0");

            if (!ColumnExists(conn, "IsletmeDeneme", "IptalAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN IptalAt TEXT");

            if (!ColumnExists(conn, "IsletmeDeneme", "YediGunHatirlatmaAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN YediGunHatirlatmaAt TEXT");

            if (!ColumnExists(conn, "IsletmeDeneme", "UcGunHatirlatmaAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE IsletmeDeneme ADD COLUMN UcGunHatirlatmaAt TEXT");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "YetkiSeviyesi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN YetkiSeviyesi TEXT NOT NULL DEFAULT 'OkumaRapor'");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "Kaynak"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN Kaynak TEXT NOT NULL DEFAULT 'Davet'");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "TalepId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN TalepId INTEGER");

            if (!ColumnExists(conn, "MuhasebeciMusteri", "KabulAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteri ADD COLUMN KabulAt TEXT");

            if (!ColumnExists(conn, "MuhasebeciMusteriTalebi", "AylikHizmetBedeli"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciMusteriTalebi ADD COLUMN AylikHizmetBedeli NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "MuhasebeciHizmetOdemesi", "HizmetDonemi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciHizmetOdemesi ADD COLUMN HizmetDonemi TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(conn, "MuhasebeciHizmetOdemesi", "VadeAt"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciHizmetOdemesi ADD COLUMN VadeAt TEXT NOT NULL DEFAULT '1970-01-01 00:00:00'");
            if (!ColumnExists(conn, "MuhasebeciHizmetOdemesi", "PlatformKomisyonOrani"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciHizmetOdemesi ADD COLUMN PlatformKomisyonOrani NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "MuhasebeciHizmetOdemesi", "PlatformKomisyonTutari"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciHizmetOdemesi ADD COLUMN PlatformKomisyonTutari NUMERIC NOT NULL DEFAULT 0");
            if (!ColumnExists(conn, "MuhasebeciHizmetOdemesi", "AktarilacakTutar"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciHizmetOdemesi ADD COLUMN AktarilacakTutar NUMERIC NOT NULL DEFAULT 0");
            db.Database.ExecuteSqlRaw("UPDATE MuhasebeciHizmetOdemesi SET HizmetDonemi = strftime('%Y-%m', COALESCE(TahsilEdildiAt, CreatedAt, CURRENT_TIMESTAMP)) WHERE HizmetDonemi = '';");
            db.Database.ExecuteSqlRaw("UPDATE MuhasebeciHizmetOdemesi SET VadeAt = HizmetDonemi || '-01 00:00:00' WHERE VadeAt = '1970-01-01 00:00:00';");
            db.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS IX_MuhasebeciHizmetOdemesi_TalepId;");

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

            if (!ColumnExists(conn, "MuhasebeciSohbetMesaji", "SohbetId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciSohbetMesaji ADD COLUMN SohbetId INTEGER");

            if (!ColumnExists(conn, "MuhasebeciSohbetMesaji", "MesajTipi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciSohbetMesaji ADD COLUMN MesajTipi TEXT NOT NULL DEFAULT 'Metin'");

            if (!ColumnExists(conn, "MuhasebeciSohbetMesaji", "ClientMessageId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE MuhasebeciSohbetMesaji ADD COLUMN ClientMessageId TEXT NOT NULL DEFAULT ''");
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
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciHizmetOdemesi_TalepId_HizmetDonemi ON MuhasebeciHizmetOdemesi(TalepId, HizmetDonemi);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciHizmetOdemesi_OdemeIslemiId ON MuhasebeciHizmetOdemesi(OdemeIslemiId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciHizmetOdemesi_MusteriIsletmeId_Durum ON MuhasebeciHizmetOdemesi(MusteriIsletmeId, Durum);");
            db.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId;");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId ON MuhasebeciAktarimAlacagi(MuhasebeciHizmetOdemesiId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciAktarimAlacagi_Accrual ON MuhasebeciAktarimAlacagi(MuhasebeciHizmetOdemesiId) WHERE AktarilacakTutar >= 0;");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciAktarimAlacagi_RefundAdjustment ON MuhasebeciAktarimAlacagi(MuhasebeciHizmetOdemesiId) WHERE AktarilacakTutar < 0;");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciAktarimAlacagi_MuhasebeciIsletmeId_AktarimDonemi_Durum ON MuhasebeciAktarimAlacagi(MuhasebeciIsletmeId, AktarimDonemi, Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciAktarimAlacagi_AktarimReferansi ON MuhasebeciAktarimAlacagi(AktarimReferansi);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_DestekTalebi_IsletmeId_OlusturmaAnahtari ON DestekTalebi(IsletmeId, OlusturmaAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DestekTalebi_IsletmeId_CreatedAt ON DestekTalebi(IsletmeId, CreatedAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DestekTalebi_Oncelik_CreatedAt ON DestekTalebi(Oncelik, CreatedAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DestekTalebi_Durum ON DestekTalebi(Durum);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciBaglantiDaveti_TokenHash ON MuhasebeciBaglantiDaveti(TokenHash);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciBaglantiDaveti_MusteriIsletmeId ON MuhasebeciBaglantiDaveti(MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciBaglantiDaveti_MuhasebeciIsletmeId ON MuhasebeciBaglantiDaveti(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciBaglantiDaveti_MusteriIsletmeId_Durum ON MuhasebeciBaglantiDaveti(MusteriIsletmeId, Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbet_MuhasebeciIsletmeId ON MuhasebeciSohbet(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbet_MusteriIsletmeId ON MuhasebeciSohbet(MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciSohbet_MuhasebeciIsletmeId_MusteriIsletmeId ON MuhasebeciSohbet(MuhasebeciIsletmeId, MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbet_TalepId ON MuhasebeciSohbet(TalepId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbet_BaglantiId ON MuhasebeciSohbet(BaglantiId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbet_SonMesajAt ON MuhasebeciSohbet(SonMesajAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_SohbetId ON MuhasebeciSohbetMesaji(SohbetId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_MuhasebeciIsletmeId ON MuhasebeciSohbetMesaji(MuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_MusteriIsletmeId ON MuhasebeciSohbetMesaji(MusteriIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_TalepId ON MuhasebeciSohbetMesaji(TalepId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_BaglantiId ON MuhasebeciSohbetMesaji(BaglantiId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_OkunduAt ON MuhasebeciSohbetMesaji(OkunduAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_SohbetId_ClientMessageId ON MuhasebeciSohbetMesaji(SohbetId, ClientMessageId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_SohbetId_Id ON MuhasebeciSohbetMesaji(SohbetId, Id);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetMesaji_MuhasebeciIsletmeId_MusteriIsletmeId_CreatedAt ON MuhasebeciSohbetMesaji(MuhasebeciIsletmeId, MusteriIsletmeId, CreatedAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetEki_SohbetId ON MuhasebeciSohbetEki(SohbetId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetEki_MesajId ON MuhasebeciSohbetEki(MesajId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetEki_YukleyenIsletmeId ON MuhasebeciSohbetEki(YukleyenIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetEki_EkTipi ON MuhasebeciSohbetEki(EkTipi);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetKatilimciDurumu_SohbetId ON MuhasebeciSohbetKatilimciDurumu(SohbetId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetKatilimciDurumu_IsletmeId ON MuhasebeciSohbetKatilimciDurumu(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_MuhasebeciSohbetKatilimciDurumu_SohbetId_IsletmeId ON MuhasebeciSohbetKatilimciDurumu(SohbetId, IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetKatilimciDurumu_Arsivlendi ON MuhasebeciSohbetKatilimciDurumu(Arsivlendi);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetVeriIstegi_SohbetId ON MuhasebeciSohbetVeriIstegi(SohbetId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetVeriIstegi_IsteyenIsletmeId ON MuhasebeciSohbetVeriIstegi(IsteyenIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetVeriIstegi_HedefIsletmeId ON MuhasebeciSohbetVeriIstegi(HedefIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MuhasebeciSohbetVeriIstegi_Durum ON MuhasebeciSohbetVeriIstegi(Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_IsletmeId ON Abonelik(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_IsletmeId_Durum ON Abonelik(IsletmeId, Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_PlanKodu ON Abonelik(PlanKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Abonelik_SaglayiciAbonelikId ON Abonelik(SaglayiciAbonelikId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeDeneme_IsletmeId ON IsletmeDeneme(IsletmeId);");
            db.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS IX_IsletmeDeneme_IsletmeId_PlanKodu;");
            db.Database.ExecuteSqlRaw(@"
DELETE FROM IsletmeDeneme
WHERE Id NOT IN (
    SELECT MIN(Id)
    FROM IsletmeDeneme
    GROUP BY IsletmeId, HesapTipi
);");

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS YonetimDenetimKaydi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    AktorProviderKullaniciId TEXT NOT NULL DEFAULT '',
    Islem TEXT NOT NULL DEFAULT '',
    KaynakTuru TEXT NOT NULL DEFAULT '',
    OncekiDeger TEXT NOT NULL DEFAULT '',
    YeniDeger TEXT NOT NULL DEFAULT '',
    Gerekce TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL
);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_IsletmeDeneme_IsletmeId_HesapTipi ON IsletmeDeneme(IsletmeId, HesapTipi);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeDeneme_Durum ON IsletmeDeneme(Durum);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AbonelikOnayi_IsletmeId ON AbonelikOnayi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_AbonelikOnayi_IsletmeId_CheckoutAnahtari ON AbonelikOnayi(IsletmeId, CheckoutAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeIslemi_IsletmeId ON OdemeIslemi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_OdemeIslemi_IsletmeId_CheckoutAnahtari ON OdemeIslemi(IsletmeId, CheckoutAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeIslemi_SaglayiciOturumId ON OdemeIslemi(SaglayiciOturumId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeIslemi_SaglayiciIslemId ON OdemeIslemi(SaglayiciIslemId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_KurucuKampanyaHakki_CheckoutAnahtari ON KurucuKampanyaHakki(CheckoutAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_KurucuKampanyaHakki_KampanyaKodu_Durum_RezervasyonBitisAt ON KurucuKampanyaHakki(KampanyaKodu, Durum, RezervasyonBitisAt);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_KurucuKampanyaHakki_KampanyaKodu_IsletmeId ON KurucuKampanyaHakki(KampanyaKodu, IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_KurucuKampanyaHakki_KampanyaKodu_SiraNo ON KurucuKampanyaHakki(KampanyaKodu, SiraNo);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_OdemeOlayi_OdemeSaglayici_OlayId ON OdemeOlayi(OdemeSaglayici, OlayId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeOlayi_CheckoutAnahtari ON OdemeOlayi(CheckoutAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeOlayi_SaglayiciIslemId ON OdemeOlayi(SaglayiciIslemId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_IsletmeEntitlement_IsletmeId ON IsletmeEntitlement(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeEntitlement_PlanKodu ON IsletmeEntitlement(PlanKodu);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeEntitlement_Kaynak ON IsletmeEntitlement(Kaynak);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_IsletmeEntitlement_SponsorMuhasebeciIsletmeId ON IsletmeEntitlement(SponsorMuhasebeciIsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_AiKullanimDonemi_IsletmeId ON AiKullanimDonemi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_AiKullanimDonemi_IsletmeId_DonemAnahtari ON AiKullanimDonemi(IsletmeId, DonemAnahtari);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_YonetimDenetimKaydi_IsletmeId ON YonetimDenetimKaydi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_YonetimDenetimKaydi_CreatedAt ON YonetimDenetimKaydi(CreatedAt);");
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
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeHatirlatma_IsletmeId ON OdemeHatirlatma(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OdemeHatirlatma_IsletmeId_FaturaId_GonderildiAt ON OdemeHatirlatma(IsletmeId, FaturaId, GonderildiAt);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_FaturaMusteriOnayi_TokenHash ON FaturaMusteriOnayi(TokenHash);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FaturaMusteriOnayi_IsletmeId_FaturaId_CreatedAt ON FaturaMusteriOnayi(IsletmeId, FaturaId, CreatedAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FaturaMusteriOnayi_IsletmeId_Durum_SonGecerlilikAt ON FaturaMusteriOnayi(IsletmeId, Durum, SonGecerlilikAt);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BelgeDosya_IsletmeId ON BelgeDosya(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BelgeDosya_IsletmeId_FaturaId ON BelgeDosya(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_GibPortalAyar_IsletmeId ON GibPortalAyar(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GibPortalIslemLog_IsletmeId ON GibPortalIslemLog(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GibPortalIslemLog_IsletmeId_FaturaId_Tarih ON GibPortalIslemLog(IsletmeId, FaturaId, Tarih);");
        }
    }
}
