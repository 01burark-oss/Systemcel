using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IMuhasebeciPortalService
    {
        Task<MuhasebeciPazaryeriDto> GetPublicMarketplaceAsync(string? arama = null, CancellationToken ct = default);
        Task<MuhasebeciPazaryeriDto> GetMarketplaceAsync(string? arama = null, CancellationToken ct = default);
        Task<MuhasebeciPanelDto> GetPanelAsync(CancellationToken ct = default);
        Task<MuhasebeciProfilDto> SaveProfileAsync(MuhasebeciProfilKaydetRequest request, CancellationToken ct = default);
        Task<MuhasebeciTalepDto> CreateInviteAsync(MuhasebeciTalepOlusturRequest request, string publicBaseUrl, CancellationToken ct = default);
        Task<MuhasebeciTalepDto> SubmitMarketplaceRequestAsync(int muhasebeciIsletmeId, MuhasebeciTalepOlusturRequest request, CancellationToken ct = default);
        Task<MuhasebeciTalepDto> AcceptInviteAsync(MuhasebeciDavetKabulRequest request, CancellationToken ct = default);
        Task<MuhasebeciTalepDto> AcceptRequestAsync(int talepId, MuhasebeciTalepKararRequest request, CancellationToken ct = default);
        Task<MuhasebeciTalepDto> RejectRequestAsync(int talepId, CancellationToken ct = default);
        Task<MuhasebeciTalepDto> CancelRequestAsync(int talepId, CancellationToken ct = default);
        Task<MuhasebeciSohbetDto> GetCustomerConversationAsync(int muhasebeciIsletmeId, CancellationToken ct = default);
        Task<MuhasebeciSohbetDto> SendCustomerConversationMessageAsync(int muhasebeciIsletmeId, MuhasebeciSohbetMesajiGonderRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetDto> GetAccountantRequestConversationAsync(int talepId, CancellationToken ct = default);
        Task<MuhasebeciSohbetDto> SendAccountantRequestConversationMessageAsync(int talepId, MuhasebeciSohbetMesajiGonderRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetDto> GetAccountantCustomerConversationAsync(int musteriIsletmeId, CancellationToken ct = default);
        Task<MuhasebeciSohbetDto> SendAccountantCustomerConversationMessageAsync(int musteriIsletmeId, MuhasebeciSohbetMesajiGonderRequest request, CancellationToken ct = default);
        Task<MuhasebeciSohbetBildirimDurumuDto> GetConversationNotificationStatusAsync(CancellationToken ct = default);
        Task OpenCustomerContextAsync(int musteriIsletmeId, CancellationToken ct = default);
        Task CloseCustomerContextAsync(CancellationToken ct = default);
    }
}
