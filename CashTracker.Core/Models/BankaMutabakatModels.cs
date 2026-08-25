namespace CashTracker.Core.Models;

public static class BankaHareketDurumlari
{
    public const string Acik = "Acik";
    public const string Eslesti = "Eslesti";
    public const string YokSayildi = "YokSayildi";
}

public static class BankaEslesmeKaynakTurleri
{
    public const string Fatura = "Fatura";
    public const string TahsilatOdeme = "TahsilatOdeme";
    public const string CariHareket = "CariHareket";
}

public sealed record BankaHareketDto(
    int Id,
    DateTime Tarih,
    string Aciklama,
    decimal Tutar,
    string ParaBirimi,
    string Durum,
    string EslesenKaynakTuru,
    int? EslesenKaynakId);

public sealed record BankaCsvImportSonucu(int Eklenen, int Tekrar, int Toplam);

public sealed record BankaEslesmeAdayi(
    string KaynakTuru,
    int KaynakId,
    string Baslik,
    decimal Tutar,
    DateTime Tarih,
    int Skor,
    IReadOnlyList<string> Nedenler);

public sealed record BankaEslesmeIstek(string KaynakTuru, int KaynakId, bool Onaylandi);
