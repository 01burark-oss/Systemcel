namespace CashTracker.Core.Models;

public sealed class PazaryeriOptions
{
    public bool Aktif { get; init; } = true;
    public IReadOnlySet<int> PilotIsletmeIdleri { get; init; } = new HashSet<int>();
    public decimal VarsayilanKomisyonOrani { get; init; } = 8m;
    public decimal KomisyonKdvOrani { get; init; } = 20m;
    public decimal TevkifatOrani { get; init; } = 1m;
    public decimal OdemeHizmetiOrani { get; init; }
    public int VarsayilanOdemeVadesiGun { get; init; } = 7;
    public int StokRezervasyonSuresiDakika { get; init; } = 30;
    // Unconfigured categories retain the legacy optional-field behavior.
    public IReadOnlyDictionary<string, PazaryeriKategoriSevkKabulKurali> KategoriKurallari { get; init; }
        = new Dictionary<string, PazaryeriKategoriSevkKabulKurali>(StringComparer.OrdinalIgnoreCase);

    public PazaryeriKategoriSevkKabulKurali GetCategoryRule(string? category) =>
        !string.IsNullOrWhiteSpace(category) && KategoriKurallari.TryGetValue(category.Trim(), out var rule)
            ? rule
            : PazaryeriKategoriSevkKabulKurali.Empty;

    public bool IsBusinessAllowed(int businessId) =>
        Aktif && (PilotIsletmeIdleri.Count == 0 || PilotIsletmeIdleri.Contains(businessId));
}

public sealed class PazaryeriKategoriSevkKabulKurali
{
    public static PazaryeriKategoriSevkKabulKurali Empty { get; } = new();

    public bool BelgeNoGerekli { get; init; }
    public bool BelgeUuidGerekli { get; init; }
    public bool TasimaTipiGerekli { get; init; }
    public bool AracPlakaGerekli { get; init; }
    public bool TasiyiciGerekli { get; init; }
    public bool SurucuAdiGerekli { get; init; }
    public bool CikisDeposuGerekli { get; init; }
    public bool SevkZamaniGerekli { get; init; }
    public bool VarisDeposuGerekli { get; init; }
    public bool PlanlananTeslimGerekli { get; init; }
    public bool RandevuGerekli { get; init; }
    public bool LotNoGerekli { get; init; }
    public bool SeriNoGerekli { get; init; }
    public bool AgirlikGerekli { get; init; }
    public decimal? AgirlikToleransiYuzde { get; init; }
    public bool SicaklikAraligiGerekli { get; init; }
    public bool SicaklikOlcumuGerekli { get; init; }
    public bool SonKullanmaTarihiGerekli { get; init; }
    public bool EkBelgeGerekli { get; init; }
}
