using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IAkilliKararService
{
    AkilliKararDurumu GetStatus();
    Task<UrunEslesmeOnerisi> UrunEsleAsync(int isletmeId, UrunEslesmeIstek request, CancellationToken ct = default);
    Task UrunEslesmesiniOnaylaAsync(int isletmeId, UrunEslesmeOnayIstek request, CancellationToken ct = default);
    Task<FaturaKontrolSonucu> FaturaKontrolEtAsync(int isletmeId, FaturaKontrolIstek request, CancellationToken ct = default);
    Task<IReadOnlyList<BugununIsi>> BugununIsleriniGetirAsync(int isletmeId, CancellationToken ct = default);
    Task<ReceiptOcrResult> FisiZenginlestirAsync(int isletmeId, ReceiptOcrResult result, IReadOnlyList<string> giderKalemleri, CancellationToken ct = default);
    Task<IReadOnlyList<SutunEslemeOnerisi>> SutunlariEsleAsync(string veriTuru, IReadOnlyList<string> sutunlar, IReadOnlyList<IReadOnlyDictionary<string, string>> ornekler, CancellationToken ct = default);
    Task<AsistanYonlendirme> AsistaniYonlendirAsync(string mesaj, CancellationToken ct = default);
}
