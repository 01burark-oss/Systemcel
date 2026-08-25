using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IBankaMutabakatService
{
    Task<IReadOnlyList<BankaHareketDto>> ListeleAsync(int isletmeId, string? durum = null, CancellationToken ct = default);
    Task<BankaCsvImportSonucu> CsvIceAktarAsync(int isletmeId, Stream csv, string dosyaAdi, long uzunluk, CancellationToken ct = default);
    Task<IReadOnlyList<BankaEslesmeAdayi>> AdaylariGetirAsync(int isletmeId, int hareketId, CancellationToken ct = default);
    Task EslesmeOnaylaAsync(int isletmeId, int hareketId, BankaEslesmeIstek istek, CancellationToken ct = default);
    Task YokSayAsync(int isletmeId, int hareketId, CancellationToken ct = default);
}
