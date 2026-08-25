namespace CashTracker.Core.Models;

public static class DestekKategorileri
{
    public const string Teknik = "Teknik";
    public const string Faturalama = "Faturalama";
    public const string Hesap = "Hesap";
    public const string Diger = "Diger";
    public static readonly IReadOnlySet<string> TumKategoriler = new HashSet<string>(StringComparer.Ordinal)
    {
        Teknik, Faturalama, Hesap, Diger
    };
}

public static class DestekOncelikleri
{
    public const string Standart = "Standart";
    public const string Oncelikli = "Oncelikli";
}

public static class DestekTalebiDurumlari
{
    public const string Acik = "Acik";
    public const string Islemde = "Islemde";
    public const string Cozuldu = "Cozuldu";
    public static readonly IReadOnlySet<string> TumDurumlar = new HashSet<string>(StringComparer.Ordinal)
    {
        Acik, Islemde, Cozuldu
    };
}

public sealed class DestekTalebiOlusturRequest
{
    public string Konu { get; init; } = string.Empty;
    public string Kategori { get; init; } = string.Empty;
    public string Aciklama { get; init; } = string.Empty;
}

public sealed class DestekTalebiGuncelleRequest
{
    public string Durum { get; init; } = string.Empty;
    public string YoneticiYaniti { get; init; } = string.Empty;
}

public sealed class DestekTalebiDto
{
    public int Id { get; init; }
    public int IsletmeId { get; init; }
    public string IsletmeAdi { get; init; } = string.Empty;
    public string Konu { get; init; } = string.Empty;
    public string Kategori { get; init; } = string.Empty;
    public string Aciklama { get; init; } = string.Empty;
    public string Oncelik { get; init; } = DestekOncelikleri.Standart;
    public string Durum { get; init; } = DestekTalebiDurumlari.Acik;
    public string YoneticiYaniti { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class DestekTalebiListeDto
{
    public List<DestekTalebiDto> Talepler { get; init; } = new();
}
