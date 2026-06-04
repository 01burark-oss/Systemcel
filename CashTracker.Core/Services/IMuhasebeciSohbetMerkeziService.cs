using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public sealed class MuhasebeciSohbetStorageOptions
    {
        public string AppDataPath { get; init; } = string.Empty;
    }

    public sealed class SohbetDosyaYukleme
    {
        public string DosyaAdi { get; init; } = string.Empty;
        public string IcerikTipi { get; init; } = string.Empty;
        public long Boyut { get; init; }
        public Stream Icerik { get; init; } = Stream.Null;
    }

    public sealed class SohbetDosyaIndirme
    {
        public string DosyaAdi { get; init; } = string.Empty;
        public string IcerikTipi { get; init; } = "application/octet-stream";
        public string DosyaYolu { get; init; } = string.Empty;
    }

    public interface IMuhasebeciSohbetMerkeziService
    {
        Task<MuhasebeciSohbetListeDto> GetSohbetlerAsync(bool includeArchived = false, CancellationToken ct = default);
        Task<MuhasebeciSohbetMesajSayfasiDto> GetMesajlarAsync(int sohbetId, int? beforeId = null, int limit = 50, CancellationToken ct = default);
        Task<MuhasebeciSohbetMerkeziMesajiDto> MesajGonderAsync(int sohbetId, MuhasebeciSohbetMesajiOlusturRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetEkiDto> DosyaEkleAsync(int sohbetId, SohbetDosyaYukleme upload, CancellationToken ct = default);
        Task<SohbetDosyaIndirme> DosyaIndirAsync(int ekId, CancellationToken ct = default);
        Task<MuhasebeciSohbetVeriIstegiDto> VeriIstegiOlusturAsync(int sohbetId, MuhasebeciSohbetVeriIstegiRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetMerkeziMesajiDto> VeriPaylasAsync(int sohbetId, MuhasebeciSohbetVeriPaylasimiRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetOzetDto> KonuGuncelleAsync(int sohbetId, MuhasebeciSohbetKonuGuncelleRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetOzetDto> ArsivleAsync(int sohbetId, MuhasebeciSohbetArsivRequest request, CancellationToken ct = default);
        Task<int> GetOrCreateForCustomerAsync(int muhasebeciIsletmeId, CancellationToken ct = default);
        Task<int> GetOrCreateForAccountantRequestAsync(int talepId, CancellationToken ct = default);
        Task<int> GetOrCreateForAccountantCustomerAsync(int musteriIsletmeId, CancellationToken ct = default);
    }
}
