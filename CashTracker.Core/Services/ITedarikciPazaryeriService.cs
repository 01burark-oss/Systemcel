using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface ITedarikciPazaryeriService
{
    Task<TedarikciProfil> SaveSupplierProfileAsync(TedarikciOnboardingRequest request, CancellationToken ct = default);
    Task<TedarikciProfil> VerifySupplierAsync(int supplierProfileId, TedarikciDogrulamaRequest request, CancellationToken ct = default);
    Task<TedarikciUrun> SaveProductAsync(int? productId, TedarikciUrunKaydetRequest request, CancellationToken ct = default);
    Task<PazaryeriIslemSonucu> CreateOrderAsync(PazaryeriSiparisOlusturRequest request, CancellationToken ct = default);
    Task<PazaryeriIslemSonucu> AcceptOfferAsync(int offerId, TedarikTeklifKabulRequest request, CancellationToken ct = default);
    Task<PazaryeriIslemSonucu> PayOrderAsync(int masterOrderId, PazaryeriOdemeRequest request, CancellationToken ct = default);
    Task UpdateSupplierOrderStateAsync(int supplierOrderId, TedarikciSiparisDurumRequest request, CancellationToken ct = default);
    Task<TedarikciSevkiyatSonucu> CreateShipmentAsync(int supplierOrderId, TedarikciSevkiyatOlusturRequest request, CancellationToken ct = default);
    Task<TedarikciQrCozumDto> ResolveShipmentQrAsync(string code, CancellationToken ct = default);
    Task<TedarikciMalKabulSonucu> ReceiveShipmentQrAsync(string code, TedarikciMalKabulRequest request, CancellationToken ct = default);
    Task<TedarikciSiparisSikayeti> CreateComplaintAsync(int supplierOrderId, TedarikciSikayetOlusturRequest request, CancellationToken ct = default);
    Task<TedarikciSiparisSikayeti> RespondToComplaintAsync(int complaintId, TedarikciSikayetYanitRequest request, CancellationToken ct = default);
    Task<TedarikciSiparisSikayeti> CloseComplaintAsync(int complaintId, TedarikciSikayetKapatRequest request, CancellationToken ct = default);
    Task<TedarikciDegerlendirmesi> SaveSupplierRatingAsync(int supplierOrderId, TedarikciDegerlendirmeKaydetRequest request, CancellationToken ct = default);
    Task CancelOrderAsync(int masterOrderId, PazaryeriIptalRequest request, CancellationToken ct = default);
    Task CancelSupplierOrderAsync(int supplierOrderId, PazaryeriIptalRequest request, CancellationToken ct = default);
    Task DisputeSupplierOrderAsync(int supplierOrderId, string reason, CancellationToken ct = default);
    Task ResolveDisputeAsync(int supplierOrderId, PazaryeriItirazCozRequest request, CancellationToken ct = default);
    Task MatchSupplierInvoiceAsync(int supplierOrderId, TedarikciBelgeEsleRequest request, CancellationToken ct = default);
    Task CompleteSettlementAsync(int supplierOrderId, PazaryeriHakEdisTamamlaRequest request, CancellationToken ct = default);
    Task<int> ExpirePendingOrdersAsync(DateTime nowUtc, CancellationToken ct = default);
}

public interface IMarketplacePaymentGateway
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<MarketplacePaymentResult> CollectAsync(MarketplacePaymentCommand command, CancellationToken ct = default);
    Task<MarketplacePaymentResult> RefundAsync(string providerTransactionId, decimal amount, string currency, string idempotencyKey, CancellationToken ct = default);
    Task<MarketplacePaymentResult> ReleaseAsync(MarketplacePayoutCommand command, CancellationToken ct = default);
}
