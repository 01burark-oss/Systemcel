namespace CashTracker.Core.Models;

public sealed record TedarikciProfilKaydetRequest(string Unvan, string Kategoriler, string Sehir, string Aciklama, bool Yayinda);
public sealed record TedarikAlimTalebiOlusturRequest(string Baslik, string Kategori, string UrunHizmet, decimal Miktar, string Birim, string TeslimatSehri, DateTime SonTeklifAt, string Aciklama);
public sealed record TedarikTeklifiOlusturRequest(decimal BirimFiyat, string ParaBirimi, int TerminGun, decimal MinimumSiparis, string Not);
