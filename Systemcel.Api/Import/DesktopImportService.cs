using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using CashTracker.Core.Entities;
using CashTracker.Core.Import;
using CashTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Systemcel.Api.Import;

internal sealed class DesktopImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly DesktopImportCodeStore _codeStore;

    public DesktopImportService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        DesktopImportCodeStore codeStore)
    {
        _dbFactory = dbFactory;
        _codeStore = codeStore;
    }

    public async Task<DesktopImportPackageResponse> AcceptPackageAsync(
        string code,
        IFormFile package,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DesktopImportValidationException("Aktarim kodu zorunlu.");

        if (package.Length <= 0)
            throw new DesktopImportValidationException("Bos paket yuklenemez.");

        var codeRecord = _codeStore.RequireActive(code);
        var data = await ReadPackageAsync(package, ct);

        ValidateManifest(data.Manifest, codeRecord.Code);
        ValidatePackage(data);

        var response = await ImportPackageAsync(codeRecord, data, ct);
        _codeStore.MarkUsed(codeRecord.Code, data.Manifest.PackageId, response.ImportedTotals);
        return response;
    }

    private static async Task<DesktopImportPackageData> ReadPackageAsync(IFormFile package, CancellationToken ct)
    {
        await using var packageStream = package.OpenReadStream();
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);

        var manifestEntry = archive.GetEntry(DesktopImportContract.ManifestFileName)
            ?? throw new DesktopImportValidationException("Paket manifest.json icermiyor.");

        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<DesktopImportManifest>(manifestStream, JsonOptions, ct)
            ?? throw new DesktopImportValidationException("manifest.json okunamadi.");

        return new DesktopImportPackageData
        {
            Manifest = manifest,
            Isletmeler = await ReadRowsAsync<DesktopImportIsletmeRecord>(archive, manifest, DesktopImportContract.IsletmelerFileName, ct),
            CariKartlar = await ReadRowsAsync<DesktopImportCariKartRecord>(archive, manifest, DesktopImportContract.CariKartlarFileName, ct),
            CariHareketler = await ReadRowsAsync<DesktopImportCariHareketRecord>(archive, manifest, DesktopImportContract.CariHareketlerFileName, ct),
            Urunler = await ReadRowsAsync<DesktopImportUrunHizmetRecord>(archive, manifest, DesktopImportContract.UrunlerFileName, ct),
            StokHareketleri = await ReadRowsAsync<DesktopImportStokHareketRecord>(archive, manifest, DesktopImportContract.StokHareketleriFileName, ct),
            Faturalar = await ReadRowsAsync<DesktopImportFaturaRecord>(archive, manifest, DesktopImportContract.FaturalarFileName, ct),
            FaturaSatirlari = await ReadRowsAsync<DesktopImportFaturaSatirRecord>(archive, manifest, DesktopImportContract.FaturaSatirlariFileName, ct),
            TahsilatOdemeler = await ReadRowsAsync<DesktopImportTahsilatOdemeRecord>(archive, manifest, DesktopImportContract.TahsilatOdemelerFileName, ct),
            KasaHareketleri = await ReadRowsAsync<DesktopImportKasaHareketRecord>(archive, manifest, DesktopImportContract.KasaHareketleriFileName, ct)
        };
    }

    private static async Task<List<T>> ReadRowsAsync<T>(
        ZipArchive archive,
        DesktopImportManifest manifest,
        string path,
        CancellationToken ct)
    {
        var fileEntry = FindManifestFile(manifest, path)
            ?? throw new DesktopImportValidationException($"Manifest {path} dosyasini tanimlamiyor.");

        var archiveEntry = archive.GetEntry(path)
            ?? throw new DesktopImportValidationException($"Paket {path} dosyasini icermiyor.");

        var bytes = await ReadEntryBytesAsync(archiveEntry, ct);
        var sha256 = HashSha256(bytes);
        if (!string.Equals(fileEntry.Sha256, sha256, StringComparison.OrdinalIgnoreCase))
            throw new DesktopImportValidationException($"{path} hash dogrulamasi basarisiz.");

        var rows = JsonSerializer.Deserialize<List<T>>(bytes, JsonOptions) ?? new List<T>();
        if (fileEntry.Count != rows.Count)
            throw new DesktopImportValidationException($"{path} kayit sayisi manifest ile uyusmuyor.");

        return rows;
    }

    private static async Task<byte[]> ReadEntryBytesAsync(ZipArchiveEntry entry, CancellationToken ct)
    {
        await using var stream = entry.Open();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        return memory.ToArray();
    }

    private static DesktopImportFileEntry? FindManifestFile(DesktopImportManifest manifest, string path)
    {
        return manifest.Files.FirstOrDefault(x =>
            string.Equals(NormalizePath(x.Path), NormalizePath(path), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string HashSha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void ValidateManifest(DesktopImportManifest manifest, string code)
    {
        if (!string.Equals(manifest.ManifestVersion, DesktopImportContract.ManifestVersion, StringComparison.Ordinal))
            throw new DesktopImportValidationException($"Desteklenmeyen manifestVersion: {manifest.ManifestVersion}");

        if (string.IsNullOrWhiteSpace(manifest.PackageId))
            throw new DesktopImportValidationException("Manifest packageId alanini icermiyor.");

        if (!string.Equals(manifest.Transfer.Code.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DesktopImportValidationException("Manifest aktarim kodu ile yuklenen kod uyusmuyor.");

        foreach (var requiredFile in DesktopImportContract.RequiredDataFiles)
        {
            var file = FindManifestFile(manifest, requiredFile);
            if (file is null || !file.Required)
                throw new DesktopImportValidationException($"Manifest zorunlu {requiredFile} dosyasini icermiyor.");
        }
    }

    private static void ValidatePackage(DesktopImportPackageData data)
    {
        var totals = BuildTotals(data);
        var manifestTotals = data.Manifest.Totals;
        if (manifestTotals.Isletme != totals.Isletme ||
            manifestTotals.CariKart != totals.CariKart ||
            manifestTotals.CariHareket != totals.CariHareket ||
            manifestTotals.UrunHizmet != totals.UrunHizmet ||
            manifestTotals.StokHareket != totals.StokHareket ||
            manifestTotals.Fatura != totals.Fatura ||
            manifestTotals.FaturaSatir != totals.FaturaSatir ||
            manifestTotals.TahsilatOdeme != totals.TahsilatOdeme ||
            manifestTotals.KasaHareket != totals.KasaHareket)
        {
            throw new DesktopImportValidationException("Manifest toplam sayilari veri dosyalariyla uyusmuyor.");
        }

        var errors = new List<string>();
        RequireUnique(data.Isletmeler.Select(x => x.LocalId), "Isletme", errors);
        RequireUnique(data.CariKartlar.Select(x => x.LocalId), "CariKart", errors);
        RequireUnique(data.CariHareketler.Select(x => x.LocalId), "CariHareket", errors);
        RequireUnique(data.Urunler.Select(x => x.LocalId), "UrunHizmet", errors);
        RequireUnique(data.StokHareketleri.Select(x => x.LocalId), "StokHareket", errors);
        RequireUnique(data.Faturalar.Select(x => x.LocalId), "Fatura", errors);
        RequireUnique(data.FaturaSatirlari.Select(x => x.LocalId), "FaturaSatir", errors);
        RequireUnique(data.TahsilatOdemeler.Select(x => x.LocalId), "TahsilatOdeme", errors);
        RequireUnique(data.KasaHareketleri.Select(x => x.LocalId), "KasaHareket", errors);

        var isletmeler = data.Isletmeler.Select(x => x.LocalId).ToHashSet();
        var cariKartlar = data.CariKartlar.Select(x => x.LocalId).ToHashSet();
        var cariHareketler = data.CariHareketler.Select(x => x.LocalId).ToHashSet();
        var urunler = data.Urunler.Select(x => x.LocalId).ToHashSet();
        var faturalar = data.Faturalar.Select(x => x.LocalId).ToHashSet();
        var kasalar = data.KasaHareketleri.Select(x => x.LocalId).ToHashSet();

        foreach (var row in data.CariKartlar)
            RequireReference(isletmeler, row.IsletmeLocalId, "CariKart.IsletmeLocalId", row.LocalId, errors);

        foreach (var row in data.CariHareketler)
        {
            RequireReference(isletmeler, row.IsletmeLocalId, "CariHareket.IsletmeLocalId", row.LocalId, errors);
            RequireReference(cariKartlar, row.CariKartLocalId, "CariHareket.CariKartLocalId", row.LocalId, errors);
        }

        foreach (var row in data.Urunler)
            RequireReference(isletmeler, row.IsletmeLocalId, "UrunHizmet.IsletmeLocalId", row.LocalId, errors);

        foreach (var row in data.StokHareketleri)
        {
            RequireReference(isletmeler, row.IsletmeLocalId, "StokHareket.IsletmeLocalId", row.LocalId, errors);
            RequireReference(urunler, row.UrunHizmetLocalId, "StokHareket.UrunHizmetLocalId", row.LocalId, errors);
        }

        foreach (var row in data.Faturalar)
        {
            RequireReference(isletmeler, row.IsletmeLocalId, "Fatura.IsletmeLocalId", row.LocalId, errors);
            RequireReference(cariKartlar, row.CariKartLocalId, "Fatura.CariKartLocalId", row.LocalId, errors);
        }

        foreach (var row in data.FaturaSatirlari)
        {
            RequireReference(isletmeler, row.IsletmeLocalId, "FaturaSatir.IsletmeLocalId", row.LocalId, errors);
            RequireReference(faturalar, row.FaturaLocalId, "FaturaSatir.FaturaLocalId", row.LocalId, errors);
            if (row.UrunHizmetLocalId.HasValue)
                RequireReference(urunler, row.UrunHizmetLocalId.Value, "FaturaSatir.UrunHizmetLocalId", row.LocalId, errors);
        }

        foreach (var row in data.TahsilatOdemeler)
        {
            RequireReference(isletmeler, row.IsletmeLocalId, "TahsilatOdeme.IsletmeLocalId", row.LocalId, errors);
            RequireReference(faturalar, row.FaturaLocalId, "TahsilatOdeme.FaturaLocalId", row.LocalId, errors);
            RequireReference(cariKartlar, row.CariKartLocalId, "TahsilatOdeme.CariKartLocalId", row.LocalId, errors);
            if (row.KasaLocalId.HasValue)
                RequireReference(kasalar, row.KasaLocalId.Value, "TahsilatOdeme.KasaLocalId", row.LocalId, errors);
            if (row.CariHareketLocalId.HasValue)
                RequireReference(cariHareketler, row.CariHareketLocalId.Value, "TahsilatOdeme.CariHareketLocalId", row.LocalId, errors);
        }

        foreach (var row in data.KasaHareketleri)
            RequireReference(isletmeler, row.IsletmeLocalId, "KasaHareket.IsletmeLocalId", row.LocalId, errors);

        if (errors.Count > 0)
            throw new DesktopImportValidationException(string.Join(" ", errors.Take(8)));
    }

    private static void RequireUnique(IEnumerable<int> ids, string entity, List<string> errors)
    {
        var seen = new HashSet<int>();
        foreach (var id in ids)
        {
            if (id <= 0)
            {
                errors.Add($"{entity} localId gecersiz: {id}.");
                continue;
            }

            if (!seen.Add(id))
                errors.Add($"{entity} localId tekrar ediyor: {id}.");
        }
    }

    private static void RequireReference(HashSet<int> existingIds, int localId, string field, int rowId, List<string> errors)
    {
        if (!existingIds.Contains(localId))
            errors.Add($"{field} icin eksik referans: satir {rowId}, localId {localId}.");
    }

    private async Task<DesktopImportPackageResponse> ImportPackageAsync(
        DesktopImportCodeRecord code,
        DesktopImportPackageData data,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var idMaps = CreateIdMaps();
        var importedTotals = new DesktopImportTotals();

        await ImportIsletmelerAsync(db, data, code, idMaps, importedTotals, ct);
        await ImportKasaHareketleriAsync(db, data, idMaps, importedTotals, ct);
        await ImportCariKartlarAsync(db, data, idMaps, importedTotals, ct);
        await ImportCariHareketlerAsync(db, data, idMaps, importedTotals, ct);
        await ImportUrunlerAsync(db, data, idMaps, importedTotals, ct);
        await ImportStokHareketleriAsync(db, data, idMaps, importedTotals, ct);
        await ImportFaturalarAsync(db, data, idMaps, importedTotals, ct);
        await ImportFaturaSatirlariAsync(db, data, idMaps, importedTotals, ct);
        await ImportTahsilatOdemelerAsync(db, data, idMaps, importedTotals, ct);

        await transaction.CommitAsync(ct);

        return new DesktopImportPackageResponse
        {
            PackageId = data.Manifest.PackageId,
            Status = "Imported",
            ReceivedTotals = BuildTotals(data),
            ImportedTotals = importedTotals,
            IdMaps = idMaps,
            Message = "Masaustu aktarim paketi iceri alindi."
        };
    }

    private static async Task ImportIsletmelerAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        DesktopImportCodeRecord code,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, Isletme Entity)>();
        var startIndex = 0;

        if (code.TargetIsletmeId.HasValue && data.Isletmeler.Count > 0)
        {
            var target = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == code.TargetIsletmeId.Value, ct)
                ?? throw new DesktopImportValidationException($"Hedef isletme bulunamadi: {code.TargetIsletmeId.Value}");

            idMaps["Isletme"][data.Isletmeler[0].LocalId] = target.Id;
            target.UpdatedAt = DateTime.Now;
            totals.Isletme++;
            startIndex = 1;
        }

        foreach (var row in data.Isletmeler.Skip(startIndex))
        {
            var entity = new Isletme
            {
                Ad = string.IsNullOrWhiteSpace(row.Ad) ? "Aktarilan Isletme" : row.Ad.Trim(),
                TenantTipi = string.IsNullOrWhiteSpace(row.TenantTipi) ? "Isletme" : row.TenantTipi.Trim(),
                IsAktif = row.IsAktif,
                CreatedAt = SafeDate(row.CreatedAt),
                UpdatedAt = SafeDate(row.UpdatedAt)
            };

            db.Isletmeler.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["Isletme"][localId] = entity.Id;

        totals.Isletme += pending.Count;
    }

    private static async Task ImportKasaHareketleriAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, Kasa Entity)>();
        foreach (var row in data.KasaHareketleri)
        {
            var entity = new Kasa
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                Tarih = SafeDate(row.Tarih),
                Tip = row.Tip,
                Tutar = row.Tutar,
                OdemeYontemi = row.OdemeYontemi,
                Kalem = row.Kalem,
                GiderTuru = row.GiderTuru,
                Aciklama = row.Aciklama,
                CreatedAt = SafeDate(row.CreatedAt)
            };

            db.Kasalar.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["KasaHareket"][localId] = entity.Id;

        totals.KasaHareket += pending.Count;
    }

    private static async Task ImportCariKartlarAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, CariKart Entity)>();
        foreach (var row in data.CariKartlar)
        {
            var entity = new CariKart
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                Tip = row.Tip,
                Unvan = row.Unvan,
                Telefon = row.Telefon,
                Eposta = row.Eposta,
                Adres = row.Adres,
                VergiNoTc = row.VergiNoTc,
                VergiDairesi = row.VergiDairesi,
                Aktif = row.Aktif,
                CreatedAt = SafeDate(row.CreatedAt),
                UpdatedAt = SafeDate(row.UpdatedAt)
            };

            db.CariKartlari.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["CariKart"][localId] = entity.Id;

        totals.CariKart += pending.Count;
    }

    private static async Task ImportCariHareketlerAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, CariHareket Entity)>();
        foreach (var row in data.CariHareketler)
        {
            var entity = new CariHareket
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                CariKartId = Map(idMaps, "CariKart", row.CariKartLocalId),
                Tarih = SafeDate(row.Tarih),
                HareketTipi = row.HareketTipi,
                Tutar = row.Tutar,
                Kaynak = row.Kaynak,
                Aciklama = row.Aciklama,
                CreatedAt = SafeDate(row.CreatedAt)
            };

            db.CariHareketleri.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["CariHareket"][localId] = entity.Id;

        totals.CariHareket += pending.Count;
    }

    private static async Task ImportUrunlerAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, UrunHizmet Entity)>();
        foreach (var row in data.Urunler)
        {
            var entity = new UrunHizmet
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                Tip = row.Tip,
                Ad = row.Ad,
                Barkod = row.Barkod,
                Birim = row.Birim,
                KdvOrani = row.KdvOrani,
                AlisFiyati = row.AlisFiyati,
                SatisFiyati = row.SatisFiyati,
                KritikStok = row.KritikStok,
                Aktif = row.Aktif,
                CreatedAt = SafeDate(row.CreatedAt),
                UpdatedAt = SafeDate(row.UpdatedAt)
            };

            db.UrunHizmetleri.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["UrunHizmet"][localId] = entity.Id;

        totals.UrunHizmet += pending.Count;
    }

    private static async Task ImportStokHareketleriAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, StokHareket Entity)>();
        foreach (var row in data.StokHareketleri)
        {
            var entity = new StokHareket
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                UrunHizmetId = Map(idMaps, "UrunHizmet", row.UrunHizmetLocalId),
                Tarih = SafeDate(row.Tarih),
                Miktar = row.Miktar,
                HareketTipi = row.HareketTipi,
                Kaynak = row.Kaynak,
                Aciklama = row.Aciklama,
                CreatedAt = SafeDate(row.CreatedAt)
            };

            db.StokHareketleri.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["StokHareket"][localId] = entity.Id;

        totals.StokHareket += pending.Count;
    }

    private static async Task ImportFaturalarAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, Fatura Entity)>();
        foreach (var row in data.Faturalar)
        {
            var entity = new Fatura
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                CariKartId = Map(idMaps, "CariKart", row.CariKartLocalId),
                Tarih = SafeDate(row.Tarih),
                VadeTarihi = SafeDate(row.VadeTarihi),
                FaturaTipi = row.FaturaTipi,
                Durum = row.Durum,
                YerelFaturaNo = row.YerelFaturaNo,
                PortalBelgeNo = row.PortalBelgeNo,
                PortalUuid = row.PortalUuid,
                AraToplam = row.AraToplam,
                IskontoToplam = row.IskontoToplam,
                KdvToplam = row.KdvToplam,
                GenelToplam = row.GenelToplam,
                OdenenTutar = row.OdenenTutar,
                OdemeYontemi = row.OdemeYontemi,
                Aciklama = row.Aciklama,
                KesildiAt = SafeDate(row.KesildiAt),
                CreatedAt = SafeDate(row.CreatedAt),
                UpdatedAt = SafeDate(row.UpdatedAt)
            };

            db.Faturalar.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["Fatura"][localId] = entity.Id;

        totals.Fatura += pending.Count;
    }

    private static async Task ImportFaturaSatirlariAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, FaturaSatir Entity)>();
        foreach (var row in data.FaturaSatirlari)
        {
            var entity = new FaturaSatir
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                FaturaId = Map(idMaps, "Fatura", row.FaturaLocalId),
                UrunHizmetId = row.UrunHizmetLocalId.HasValue
                    ? Map(idMaps, "UrunHizmet", row.UrunHizmetLocalId.Value)
                    : null,
                Aciklama = row.Aciklama,
                Birim = row.Birim,
                Miktar = row.Miktar,
                BirimFiyat = row.BirimFiyat,
                IskontoOrani = row.IskontoOrani,
                IskontoTutar = row.IskontoTutar,
                KdvOrani = row.KdvOrani,
                KdvTutar = row.KdvTutar,
                SatirNetTutar = row.SatirNetTutar,
                SatirToplam = row.SatirToplam,
                StokEtkilesin = row.StokEtkilesin
            };

            db.FaturaSatirlari.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["FaturaSatir"][localId] = entity.Id;

        totals.FaturaSatir += pending.Count;
    }

    private static async Task ImportTahsilatOdemelerAsync(
        CashTrackerDbContext db,
        DesktopImportPackageData data,
        Dictionary<string, Dictionary<int, int>> idMaps,
        DesktopImportTotals totals,
        CancellationToken ct)
    {
        var pending = new List<(int LocalId, TahsilatOdeme Entity)>();
        foreach (var row in data.TahsilatOdemeler)
        {
            var entity = new TahsilatOdeme
            {
                IsletmeId = Map(idMaps, "Isletme", row.IsletmeLocalId),
                FaturaId = Map(idMaps, "Fatura", row.FaturaLocalId),
                CariKartId = Map(idMaps, "CariKart", row.CariKartLocalId),
                Tarih = SafeDate(row.Tarih),
                Tip = row.Tip,
                Tutar = row.Tutar,
                OdemeYontemi = row.OdemeYontemi,
                KasaId = row.KasaLocalId.HasValue
                    ? Map(idMaps, "KasaHareket", row.KasaLocalId.Value)
                    : null,
                CariHareketId = row.CariHareketLocalId.HasValue
                    ? Map(idMaps, "CariHareket", row.CariHareketLocalId.Value)
                    : null,
                Aciklama = row.Aciklama,
                CreatedAt = SafeDate(row.CreatedAt)
            };

            db.TahsilatOdemeleri.Add(entity);
            pending.Add((row.LocalId, entity));
        }

        await db.SaveChangesAsync(ct);
        foreach (var (localId, entity) in pending)
            idMaps["TahsilatOdeme"][localId] = entity.Id;

        totals.TahsilatOdeme += pending.Count;
    }

    private static int Map(Dictionary<string, Dictionary<int, int>> idMaps, string entity, int localId)
    {
        return idMaps.TryGetValue(entity, out var map) && map.TryGetValue(localId, out var id)
            ? id
            : throw new DesktopImportValidationException($"{entity} icin ID eslesmesi bulunamadi: {localId}");
    }

    private static DateTime SafeDate(DateTime value)
    {
        return value == default ? DateTime.Now : value;
    }

    private static DateTime? SafeDate(DateTime? value)
    {
        return value.HasValue && value.Value != default ? value.Value : null;
    }

    private static DesktopImportTotals BuildTotals(DesktopImportPackageData data)
    {
        return new DesktopImportTotals
        {
            Isletme = data.Isletmeler.Count,
            CariKart = data.CariKartlar.Count,
            CariHareket = data.CariHareketler.Count,
            UrunHizmet = data.Urunler.Count,
            StokHareket = data.StokHareketleri.Count,
            Fatura = data.Faturalar.Count,
            FaturaSatir = data.FaturaSatirlari.Count,
            TahsilatOdeme = data.TahsilatOdemeler.Count,
            KasaHareket = data.KasaHareketleri.Count
        };
    }

    private static Dictionary<string, Dictionary<int, int>> CreateIdMaps()
    {
        return new Dictionary<string, Dictionary<int, int>>
        {
            ["Isletme"] = new(),
            ["CariKart"] = new(),
            ["CariHareket"] = new(),
            ["UrunHizmet"] = new(),
            ["StokHareket"] = new(),
            ["Fatura"] = new(),
            ["FaturaSatir"] = new(),
            ["TahsilatOdeme"] = new(),
            ["KasaHareket"] = new()
        };
    }
}

internal sealed class DesktopImportPackageData
{
    public DesktopImportManifest Manifest { get; set; } = new();
    public List<DesktopImportIsletmeRecord> Isletmeler { get; set; } = new();
    public List<DesktopImportCariKartRecord> CariKartlar { get; set; } = new();
    public List<DesktopImportCariHareketRecord> CariHareketler { get; set; } = new();
    public List<DesktopImportUrunHizmetRecord> Urunler { get; set; } = new();
    public List<DesktopImportStokHareketRecord> StokHareketleri { get; set; } = new();
    public List<DesktopImportFaturaRecord> Faturalar { get; set; } = new();
    public List<DesktopImportFaturaSatirRecord> FaturaSatirlari { get; set; } = new();
    public List<DesktopImportTahsilatOdemeRecord> TahsilatOdemeler { get; set; } = new();
    public List<DesktopImportKasaHareketRecord> KasaHareketleri { get; set; } = new();
}

internal sealed class DesktopImportPackageResponse
{
    public string PackageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DesktopImportTotals ReceivedTotals { get; set; } = new();
    public DesktopImportTotals ImportedTotals { get; set; } = new();
    public Dictionary<string, Dictionary<int, int>> IdMaps { get; set; } = new();
}
