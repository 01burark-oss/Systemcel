using System.Data;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class TedarikciPazaryeriService : ITedarikciPazaryeriService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly ISystemcelYonetimService _yonetimService;
    private readonly IMarketplacePaymentGateway _paymentGateway;
    private readonly PazaryeriOptions _options;
    private readonly ICurrentUserContext _currentUserContext;

    public TedarikciPazaryeriService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        ISystemcelYonetimService yonetimService,
        IMarketplacePaymentGateway paymentGateway,
        PazaryeriOptions options,
        ICurrentUserContext currentUserContext)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _yonetimService = yonetimService;
        _paymentGateway = paymentGateway;
        _options = options;
        _currentUserContext = currentUserContext;
    }

    public async Task<TedarikciProfil> SaveSupplierProfileAsync(
        TedarikciOnboardingRequest request,
        CancellationToken ct = default)
    {
        ValidateSupplierProfile(request);
        var businessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var profile = await db.TedarikciProfilleri.SingleOrDefaultAsync(x => x.IsletmeId == businessId, ct);
        var verificationChanged = profile is not null && VerificationFieldsChanged(profile, request);
        if (profile is null)
        {
            profile = new TedarikciProfil
            {
                IsletmeId = businessId,
                KomisyonOrani = _options.VarsayilanKomisyonOrani,
                OdemeVadesiGun = _options.VarsayilanOdemeVadesiGun
            };
            db.TedarikciProfilleri.Add(profile);
        }

        profile.Unvan = request.Unvan.Trim();
        profile.Kategoriler = request.Kategoriler.Trim();
        profile.Sehir = request.Sehir.Trim();
        profile.Aciklama = request.Aciklama.Trim();
        profile.VergiNo = request.VergiNo.Trim();
        profile.MersisNo = request.MersisNo.Trim();
        profile.KepAdresi = request.KepAdresi.Trim();
        profile.Iban = NormalizeIban(request.Iban);
        profile.Adres = request.Adres.Trim();
        profile.YetkiliAdSoyad = request.YetkiliAdSoyad.Trim();
        profile.VergiDurumu = request.VergiDurumu.Trim();
        profile.SevkiyatBolgeleri = request.SevkiyatBolgeleri.Trim();
        profile.IadeKosullari = request.IadeKosullari.Trim();
        profile.PazaryeriSozlesmeVersiyonu = request.PazaryeriSozlesmeVersiyonu.Trim();
        profile.TevkifatMuaf = request.TevkifatMuaf;
        profile.UpdatedAt = DateTime.UtcNow;

        if (verificationChanged)
        {
            profile.Dogrulandi = false;
            profile.DogrulandiAt = null;
            profile.DogrulamaDurumu = "Incelemede";
            profile.DogrulamaNotu = string.Empty;
        }
        else if (!profile.Dogrulandi && HasRequiredVerificationFields(profile))
        {
            profile.DogrulamaDurumu = "Incelemede";
        }

        profile.Yayinda = request.Yayinda && profile.Dogrulandi;
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<TedarikciProfil> VerifySupplierAsync(
        int supplierProfileId,
        TedarikciDogrulamaRequest request,
        CancellationToken ct = default)
    {
        if (!await _yonetimService.IsCurrentUserAdminAsync(ct))
            throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekir.");
        if (request.KomisyonOrani is < 0m or > 100m)
            throw new ArgumentException("Komisyon oranı 0 ile 100 arasında olmalıdır.");
        if (request.OdemeVadesiGun is < 0 or > 365)
            throw new ArgumentException("Ödeme vadesi 0 ile 365 gün arasında olmalıdır.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var profile = await db.TedarikciProfilleri.SingleOrDefaultAsync(x => x.Id == supplierProfileId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi profili bulunamadı.");
        if (request.Onaylandi && !HasRequiredVerificationFields(profile))
            throw new InvalidOperationException("Doğrulama için şirket, vergi, IBAN, adres, yetkili ve sözleşme bilgileri tamamlanmalıdır.");

        profile.Dogrulandi = request.Onaylandi;
        profile.DogrulamaDurumu = request.Onaylandi ? "Onaylandi" : "Reddedildi";
        profile.DogrulamaNotu = (request.Not ?? string.Empty).Trim();
        profile.KomisyonOrani = request.KomisyonOrani;
        profile.OdemeVadesiGun = request.OdemeVadesiGun;
        profile.PspAltUyeIsyeriId = (request.PspAltUyeIsyeriId ?? string.Empty).Trim();
        profile.DogrulandiAt = request.Onaylandi ? DateTime.UtcNow : null;
        profile.Yayinda = request.Onaylandi;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<TedarikciUrun> SaveProductAsync(
        int? productId,
        TedarikciUrunKaydetRequest request,
        CancellationToken ct = default)
    {
        ValidateProduct(request);
        var businessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var profile = await db.TedarikciProfilleri.SingleOrDefaultAsync(
            x => x.IsletmeId == businessId && x.Dogrulandi, ct)
            ?? throw new InvalidOperationException("Ürün yayınlamak için tedarikçi doğrulaması tamamlanmalıdır.");

        if (request.KaynakUrunHizmetId is { } sourceId && !await db.UrunHizmetleri.AnyAsync(
                x => x.Id == sourceId && x.IsletmeId == businessId && x.Aktif, ct))
            throw new ArgumentException("Seçilen ürün bu işletmeye ait değil.");

        var product = productId.HasValue
            ? await db.TedarikciUrunleri.SingleOrDefaultAsync(
                x => x.Id == productId && x.TedarikciIsletmeId == businessId, ct)
            : null;
        if (productId.HasValue && product is null)
            throw new KeyNotFoundException("Ürün bulunamadı.");
        if (product is null)
        {
            product = new TedarikciUrun
            {
                TedarikciProfilId = profile.Id,
                TedarikciIsletmeId = businessId
            };
            db.TedarikciUrunleri.Add(product);
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await db.TedarikciUrunleri.AnyAsync(x =>
                x.TedarikciIsletmeId == businessId && x.Sku == normalizedSku && x.Id != product.Id, ct))
            throw new InvalidOperationException("Bu stok kodu daha önce kullanılmış.");
        if (request.StokMiktari < product.RezerveMiktar)
            throw new InvalidOperationException("Stok, açık siparişler için ayrılan miktarın altına indirilemez.");

        product.KaynakUrunHizmetId = request.KaynakUrunHizmetId;
        product.Sku = normalizedSku;
        product.Ad = request.Ad.Trim();
        product.Aciklama = (request.Aciklama ?? string.Empty).Trim();
        product.Kategori = request.Kategori.Trim();
        product.Birim = request.Birim.Trim();
        product.BirimFiyat = Money(request.BirimFiyat);
        product.KdvOrani = request.KdvOrani;
        product.ParaBirimi = request.ParaBirimi.Trim().ToUpperInvariant();
        product.StokMiktari = request.StokMiktari;
        product.MinimumSiparisMiktari = request.MinimumSiparisMiktari;
        product.TahminiTeslimatGun = request.TahminiTeslimatGun;
        product.Aktif = request.Aktif;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<PazaryeriIslemSonucu> CreateOrderAsync(
        PazaryeriSiparisOlusturRequest request,
        CancellationToken ct = default)
    {
        ValidateCreateOrder(request);
        var buyerId = await _isletmeService.GetActiveIdAsync();
        var creationKey = request.IdempotencyKey.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var existing = await db.PazaryeriAnaSiparisleri.SingleOrDefaultAsync(
            x => x.AliciIsletmeId == buyerId && x.OlusturmaAnahtari == creationKey, ct);
        if (existing is not null)
            return new PazaryeriIslemSonucu(existing.Id, "Sipariş daha önce oluşturuldu.", true);

        var requestedQuantities = request.Kalemler
            .GroupBy(x => x.UrunId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Miktar));
        var productIds = requestedQuantities.Keys.ToList();
        var products = await db.TedarikciUrunleri
            .Where(x => productIds.Contains(x.Id) && x.Aktif)
            .ToListAsync(ct);
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("Sepette artık satışta olmayan bir ürün var.");
        if (products.Any(x => x.TedarikciIsletmeId == buyerId))
            throw new InvalidOperationException("Kendi ürününüzü satın alamazsınız.");

        var profileIds = products.Select(x => x.TedarikciProfilId).Distinct().ToList();
        var profiles = await db.TedarikciProfilleri
            .Where(x => profileIds.Contains(x.Id) && x.Dogrulandi && x.Yayinda)
            .ToDictionaryAsync(x => x.Id, ct);
        if (profiles.Count != profileIds.Count)
            throw new InvalidOperationException("Sepette satışa kapalı bir tedarikçi var.");
        if (products.Select(x => x.ParaBirimi).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
            throw new InvalidOperationException("Aynı ödemede yalnız tek para birimi kullanılabilir.");

        foreach (var product in products)
        {
            var quantity = requestedQuantities[product.Id];
            if (quantity < product.MinimumSiparisMiktari)
                throw new InvalidOperationException($"{product.Ad} için en az {product.MinimumSiparisMiktari} {product.Birim} seçilmelidir.");
            if (quantity > product.StokMiktari - product.RezerveMiktar)
                throw new InvalidOperationException($"{product.Ad} için yeterli stok yok.");
        }

        var now = DateTime.UtcNow;
        var initialState = request.Vadeli
            ? PazaryeriSiparisDurumlari.SiparisVerildi
            : PazaryeriSiparisDurumlari.OdemeBekliyor;
        var master = new PazaryeriAnaSiparis
        {
            AliciIsletmeId = buyerId,
            SiparisNo = CreateOrderNumber(now),
            OlusturmaAnahtari = creationKey,
            TeslimatAdresi = request.TeslimatAdresi.Trim(),
            ParaBirimi = products[0].ParaBirimi,
            Durum = initialState,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.PazaryeriAnaSiparisleri.Add(master);
        await db.SaveChangesAsync(ct);

        var supplierIndex = 0;
        var createdSupplierOrders = new List<TedarikciSiparis>();
        foreach (var supplierGroup in products.GroupBy(x => x.TedarikciProfilId))
        {
            var profile = profiles[supplierGroup.Key];
            var supplierOrder = BuildSupplierOrder(master, profile, supplierGroup.ToList(), requestedQuantities, ++supplierIndex, initialState);
            db.TedarikciSiparisleri.Add(supplierOrder);
            createdSupplierOrders.Add(supplierOrder);
            await db.SaveChangesAsync(ct);

            foreach (var product in supplierGroup)
            {
                var quantity = requestedQuantities[product.Id];
                var net = Money(product.BirimFiyat * quantity);
                var vat = Money(net * product.KdvOrani / 100m);
                db.TedarikciSiparisKalemleri.Add(new TedarikciSiparisKalemi
                {
                    TedarikciSiparisId = supplierOrder.Id,
                    TedarikciUrunId = product.Id,
                    Sku = product.Sku,
                    Ad = product.Ad,
                    Birim = product.Birim,
                    Miktar = quantity,
                    BirimFiyat = product.BirimFiyat,
                    KdvOrani = product.KdvOrani,
                    NetTutar = net,
                    KdvTutari = vat,
                    ToplamTutar = net + vat
                });
                product.RezerveMiktar += quantity;
            }
        }

        master.AraToplam = Money(createdSupplierOrders.Sum(x => x.AraToplam));
        master.KdvToplam = Money(createdSupplierOrders.Sum(x => x.KdvToplam));
        master.GenelToplam = master.AraToplam + master.KdvToplam;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new PazaryeriIslemSonucu(master.Id, $"{supplierIndex} tedarikçiye sipariş oluşturuldu.");
    }

    public async Task<PazaryeriIslemSonucu> AcceptOfferAsync(
        int offerId,
        TedarikTeklifKabulRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.TeslimatAdresi))
            throw new ArgumentException("Teslimat adresi gereklidir.");

        var buyerId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var offer = await db.TedarikTeklifleri.SingleOrDefaultAsync(x => x.Id == offerId, ct)
            ?? throw new KeyNotFoundException("Teklif bulunamadı.");
        var purchaseRequest = await db.TedarikAlimTalepleri.SingleOrDefaultAsync(
            x => x.Id == offer.TalepId && x.AliciIsletmeId == buyerId, ct)
            ?? throw new KeyNotFoundException("Alım talebi bulunamadı.");
        var creationKey = $"offer:{offer.Id}";
        var existing = await db.PazaryeriAnaSiparisleri.SingleOrDefaultAsync(
            x => x.AliciIsletmeId == buyerId && x.OlusturmaAnahtari == creationKey, ct);
        if (existing is not null)
            return new PazaryeriIslemSonucu(existing.Id, "Teklif daha önce siparişe dönüştürüldü.", true);
        if (purchaseRequest.Durum != "Acik" || purchaseRequest.SonTeklifAt <= DateTime.UtcNow || offer.Durum != "Gonderildi")
            throw new InvalidOperationException("Bu teklif artık kabul edilemez.");
        if (purchaseRequest.Miktar < offer.MinimumSiparis)
            throw new InvalidOperationException("Talep miktarı teklifin minimum sipariş miktarının altında.");

        var profile = await db.TedarikciProfilleri.SingleOrDefaultAsync(
            x => x.IsletmeId == offer.TedarikciIsletmeId && x.Dogrulandi && x.Yayinda, ct)
            ?? throw new InvalidOperationException("Tedarikçi satışa açık değil.");
        var now = DateTime.UtcNow;
        var initialState = request.Vadeli
            ? PazaryeriSiparisDurumlari.SiparisVerildi
            : PazaryeriSiparisDurumlari.OdemeBekliyor;
        var subtotal = Money(offer.BirimFiyat * purchaseRequest.Miktar);
        var vat = Money(subtotal * offer.KdvOrani / 100m);
        var total = subtotal + vat;
        var sku = $"RFQ-{purchaseRequest.Id}";
        var quotedProduct = new TedarikciUrun
        {
            TedarikciProfilId = profile.Id,
            TedarikciIsletmeId = profile.IsletmeId,
            Sku = sku,
            Ad = purchaseRequest.UrunHizmet,
            Aciklama = purchaseRequest.Aciklama,
            Kategori = purchaseRequest.Kategori,
            Birim = purchaseRequest.Birim,
            BirimFiyat = offer.BirimFiyat,
            KdvOrani = offer.KdvOrani,
            ParaBirimi = offer.ParaBirimi,
            StokMiktari = purchaseRequest.Miktar,
            RezerveMiktar = purchaseRequest.Miktar,
            MinimumSiparisMiktari = offer.MinimumSiparis,
            TahminiTeslimatGun = offer.TerminGun,
            Aktif = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.TedarikciUrunleri.Add(quotedProduct);

        var master = new PazaryeriAnaSiparis
        {
            AliciIsletmeId = buyerId,
            SiparisNo = CreateOrderNumber(now),
            OlusturmaAnahtari = creationKey,
            TeslimatAdresi = request.TeslimatAdresi.Trim(),
            ParaBirimi = offer.ParaBirimi,
            AraToplam = subtotal,
            KdvToplam = vat,
            GenelToplam = total,
            Durum = initialState,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.PazaryeriAnaSiparisleri.Add(master);
        await db.SaveChangesAsync(ct);

        var supplierOrder = new TedarikciSiparis
        {
            AnaSiparisId = master.Id,
            AliciIsletmeId = buyerId,
            TedarikciIsletmeId = profile.IsletmeId,
            TedarikciProfilId = profile.Id,
            SiparisNo = $"{master.SiparisNo}-A",
            ParaBirimi = offer.ParaBirimi,
            AraToplam = subtotal,
            KdvToplam = vat,
            GenelToplam = total,
            TedarikciHakEdisi = total,
            Durum = initialState,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.TedarikciSiparisleri.Add(supplierOrder);
        await db.SaveChangesAsync(ct);
        db.TedarikciSiparisKalemleri.Add(new TedarikciSiparisKalemi
        {
            TedarikciSiparisId = supplierOrder.Id,
            TedarikciUrunId = quotedProduct.Id,
            Sku = sku,
            Ad = purchaseRequest.UrunHizmet,
            Birim = purchaseRequest.Birim,
            Miktar = purchaseRequest.Miktar,
            BirimFiyat = offer.BirimFiyat,
            KdvOrani = offer.KdvOrani,
            NetTutar = subtotal,
            KdvTutari = vat,
            ToplamTutar = total
        });
        offer.Durum = "KabulEdildi";
        purchaseRequest.Durum = "Sonuclandi";
        await db.TedarikTeklifleri.Where(x => x.TalepId == purchaseRequest.Id && x.Id != offer.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.Durum, "Reddedildi"), ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new PazaryeriIslemSonucu(master.Id, "Teklif siparişe dönüştürüldü.");
    }

    public async Task<PazaryeriIslemSonucu> PayOrderAsync(
        int masterOrderId,
        PazaryeriOdemeRequest request,
        CancellationToken ct = default)
    {
        ValidateIdempotencyKey(request.IdempotencyKey);
        var buyerId = await _isletmeService.GetActiveIdAsync();
        if (!_paymentGateway.IsConfigured)
            throw new InvalidOperationException("Kartla ödeme henüz kullanıma açılmadı.");

        PazaryeriAnaSiparis master;
        List<TedarikciSiparis> supplierOrders;
        PazaryeriOdeme payment;
        await using (var prepareDb = await _dbFactory.CreateDbContextAsync(ct))
        await using (var prepareTx = await prepareDb.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct))
        {
            master = await prepareDb.PazaryeriAnaSiparisleri.SingleOrDefaultAsync(
                x => x.Id == masterOrderId && x.AliciIsletmeId == buyerId, ct)
                ?? throw new KeyNotFoundException("Sipariş bulunamadı.");
            supplierOrders = await prepareDb.TedarikciSiparisleri.Where(x => x.AnaSiparisId == master.Id).ToListAsync(ct);
            if (master.Durum != PazaryeriSiparisDurumlari.OdemeBekliyor)
            {
                if (master.Durum == PazaryeriSiparisDurumlari.Odendi)
                    return new PazaryeriIslemSonucu(master.Id, "Sipariş daha önce ödendi.", true);
                throw new InvalidOperationException("Bu sipariş ödeme için uygun değil.");
            }

            payment = await prepareDb.PazaryeriOdemeleri.SingleOrDefaultAsync(
                x => x.AliciIsletmeId == buyerId && x.IdempotencyAnahtari == request.IdempotencyKey.Trim(), ct)
                ?? new PazaryeriOdeme
                {
                    AnaSiparisId = master.Id,
                    AliciIsletmeId = buyerId,
                    IdempotencyAnahtari = request.IdempotencyKey.Trim(),
                    Saglayici = _paymentGateway.Name,
                    Tutar = master.GenelToplam,
                    ParaBirimi = master.ParaBirimi,
                    Durum = "Hazirlaniyor"
                };
            if (payment.Id == 0)
                prepareDb.PazaryeriOdemeleri.Add(payment);
            else if (payment.AnaSiparisId != master.Id || payment.Tutar != master.GenelToplam)
                throw new InvalidOperationException("Bu ödeme anahtarı farklı bir sipariş için kullanılmış.");
            else if (payment.Durum == "Basarili")
                return new PazaryeriIslemSonucu(master.Id, "Ödeme daha önce tamamlandı.", true);

            await prepareDb.SaveChangesAsync(ct);
            await prepareTx.CommitAsync(ct);
        }

        var providerResult = await _paymentGateway.CollectAsync(new MarketplacePaymentCommand(
            master.SiparisNo,
            request.IdempotencyKey.Trim(),
            master.GenelToplam,
            master.ParaBirimi,
            buyerId,
            supplierOrders.Select(x => new MarketplacePaymentAllocation(
                x.TedarikciIsletmeId, x.GenelToplam, x.TedarikciHakEdisi)).ToList()), ct);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var storedMaster = await db.PazaryeriAnaSiparisleri.SingleAsync(x => x.Id == master.Id && x.AliciIsletmeId == buyerId, ct);
        var storedPayment = await db.PazaryeriOdemeleri.SingleAsync(x => x.Id == payment.Id, ct);
        if (storedPayment.Durum == "Basarili")
            return new PazaryeriIslemSonucu(storedMaster.Id, "Ödeme daha önce tamamlandı.", true);
        if (!providerResult.Succeeded)
        {
            storedPayment.Durum = "Basarisiz";
            storedPayment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new InvalidOperationException(providerResult.Error);
        }

        var storedOrders = await db.TedarikciSiparisleri.Where(x => x.AnaSiparisId == storedMaster.Id).ToListAsync(ct);
        var storedOrderIds = storedOrders.Select(x => x.Id).ToList();
        var lines = await db.TedarikciSiparisKalemleri.Where(x => storedOrderIds.Contains(x.TedarikciSiparisId)).ToListAsync(ct);
        var products = await db.TedarikciUrunleri.Where(x => lines.Select(y => y.TedarikciUrunId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var line in lines)
        {
            var product = products[line.TedarikciUrunId];
            if (product.RezerveMiktar < line.Miktar || product.StokMiktari < line.Miktar)
                throw new InvalidOperationException($"{product.Ad} stok rezervasyonu bozulmuş. Ödeme tamamlanmadan destekle görüşün.");
        }

        var now = DateTime.UtcNow;
        storedPayment.SaglayiciIslemId = providerResult.ProviderTransactionId;
        storedPayment.Saglayici = providerResult.Provider;
        storedPayment.Durum = "Basarili";
        storedPayment.OdendiAt = now;
        storedPayment.UpdatedAt = now;
        storedMaster.Durum = PazaryeriSiparisDurumlari.Odendi;
        storedMaster.UpdatedAt = now;
        foreach (var order in storedOrders)
        {
            AddStateHistory(db, order, PazaryeriSiparisDurumlari.Odendi, buyerId, "Ödeme tamamlandı.");
            order.OdendiAt = now;
            db.PazaryeriOdemeDagitimlari.Add(new PazaryeriOdemeDagitimi
            {
                PazaryeriOdemeId = storedPayment.Id,
                TedarikciSiparisId = order.Id,
                TedarikciIsletmeId = order.TedarikciIsletmeId,
                BrutTutar = order.GenelToplam,
                TedarikciHakEdisi = order.TedarikciHakEdisi,
                ParaBirimi = order.ParaBirimi
            });
            CreateSettlementAndLedger(db, order, now);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new PazaryeriIslemSonucu(storedMaster.Id, $"Ödeme tamamlandı; {storedOrders.Count} tedarikçi siparişi işleme alındı.");
    }

    public async Task UpdateSupplierOrderStateAsync(
        int supplierOrderId,
        TedarikciSiparisDurumRequest request,
        CancellationToken ct = default)
    {
        var activeBusinessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        EnsureStateTransitionAllowed(order, activeBusinessId, request.Durum);

        if (request.Durum == PazaryeriSiparisDurumlari.SevkEdildi)
        {
            order.KargoFirmasi = (request.KargoFirmasi ?? string.Empty).Trim();
            order.KargoTakipNo = (request.KargoTakipNo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(order.KargoFirmasi) || string.IsNullOrWhiteSpace(order.KargoTakipNo))
                throw new ArgumentException("Kargo firması ve takip numarası gereklidir.");
            var shipmentLines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == order.Id).ToListAsync(ct);
            foreach (var line in shipmentLines)
            {
                var remainingToShip = line.Miktar - line.SevkEdilenMiktar;
                if (remainingToShip <= 0m)
                    continue;
                await ConsumeShipmentStockAsync(db, order, line, remainingToShip, "Kargo sevki", null, ct);
                line.SevkEdilenMiktar += remainingToShip;
            }
        }

        AddStateHistory(db, order, request.Durum, activeBusinessId, request.Aciklama);
        if (request.Durum == PazaryeriSiparisDurumlari.TeslimEdildi)
        {
            var lines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == order.Id).ToListAsync(ct);
            foreach (var line in lines)
            {
                line.SevkEdilenMiktar = line.Miktar;
                line.KabulEdilenMiktar = line.Miktar;
                line.ReddedilenMiktar = 0m;
            }
            await FinalizeAcceptedDeliveryAsync(db, order, activeBusinessId, ct);
        }

        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<TedarikciSevkiyatSonucu> CreateShipmentAsync(
        int supplierOrderId,
        TedarikciSevkiyatOlusturRequest request,
        CancellationToken ct = default)
    {
        var supplierBusinessId = await _isletmeService.GetActiveIdAsync();
        if (request.Kalemler.Count == 0)
            throw new ArgumentException("Sevkiyata en az bir ürün ekleyin.");
        if (string.IsNullOrWhiteSpace(request.TasimaTipi))
            throw new ArgumentException("Taşıma tipini seçin.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        if (order.TedarikciIsletmeId != supplierBusinessId)
            throw new UnauthorizedAccessException("Bu sipariş için sevkiyat oluşturamazsınız.");
        if (order.Durum is not (PazaryeriSiparisDurumlari.Hazirlaniyor or PazaryeriSiparisDurumlari.SevkeHazir or PazaryeriSiparisDurumlari.KismenSevkEdildi or PazaryeriSiparisDurumlari.KismenKabul))
            throw new InvalidOperationException("Sevkiyat yalnızca hazırlanan sipariş için oluşturulabilir.");
        if (request.VarisDeposu is not null && !await db.StokDepolari.AnyAsync(
                x => x.Id == request.VarisDeposu && x.IsletmeId == order.AliciIsletmeId && x.Aktif, ct))
            throw new ArgumentException("Varış deposu alıcı işletmeye ait değil.");

        var requestedLineIds = request.Kalemler.Select(x => x.SiparisKalemiId).ToList();
        if (requestedLineIds.Distinct().Count() != requestedLineIds.Count)
            throw new ArgumentException("Aynı sipariş kalemini sevkiyata bir kez ekleyin.");
        var orderLines = await db.TedarikciSiparisKalemleri
            .Where(x => x.TedarikciSiparisId == order.Id && requestedLineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        if (orderLines.Count != request.Kalemler.Count)
            throw new ArgumentException("Sevkiyat kalemlerinden biri bu siparişe ait değil.");

        var shipment = new TedarikciSevkiyat
        {
            TedarikciSiparisId = order.Id,
            AliciIsletmeId = order.AliciIsletmeId,
            TedarikciIsletmeId = order.TedarikciIsletmeId,
            SevkiyatNo = $"SVK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
            TasimaTipi = request.TasimaTipi.Trim(),
            Tasiyici = (request.Tasiyici ?? string.Empty).Trim(),
            BelgeNo = (request.BelgeNo ?? string.Empty).Trim(),
            BelgeUuid = (request.BelgeUuid ?? string.Empty).Trim(),
            AracPlaka = (request.AracPlaka ?? string.Empty).Trim(),
            SurucuAdi = (request.SurucuAdi ?? string.Empty).Trim(),
            CikisDeposu = (request.CikisDeposu ?? string.Empty).Trim(),
            SevkAt = ToUtc(request.SevkAt) ?? DateTime.UtcNow,
            VarisDeposu = request.VarisDeposu,
            RandevuAt = ToUtc(request.RandevuAt),
            PlanlananTeslimAt = ToUtc(request.PlanlananTeslimAt),
            Not = (request.Not ?? string.Empty).Trim()
        };
        db.TedarikciSevkiyatlari.Add(shipment);
        await db.SaveChangesAsync(ct);

        var resultLabels = new List<TedarikciSevkiyatEtiketiDto>();
        foreach (var requested in request.Kalemler)
        {
            if (requested.Miktar <= 0m || requested.EtiketSayisi is < 1 or > 100)
                throw new ArgumentException("Sevk miktarı pozitif, etiket sayısı 1 ile 100 arasında olmalıdır.");
            if (requested.SicaklikMin is not null && requested.SicaklikMax is not null && requested.SicaklikMin > requested.SicaklikMax)
                throw new ArgumentException("Minimum sıcaklık maksimum sıcaklıktan büyük olamaz.");
            if (requested.Agirlik is < 0m || requested.PaletKoli < 0)
                throw new ArgumentException("Ağırlık ve palet/koli bilgisi negatif olamaz.");
            var orderLine = orderLines[requested.SiparisKalemiId];
            if (orderLine.SevkEdilenMiktar + requested.Miktar > orderLine.Miktar)
                throw new InvalidOperationException($"{orderLine.Ad} için sipariş miktarından fazla ürün sevk edilemez.");
            await ConsumeShipmentStockAsync(db, order, orderLine, requested.Miktar, shipment.SevkiyatNo, shipment.Id, ct);

            var shipmentLine = new TedarikciSevkiyatKalemi
            {
                TedarikciSevkiyatId = shipment.Id,
                TedarikciSiparisKalemiId = orderLine.Id,
                Miktar = requested.Miktar,
                EtiketSayisi = requested.EtiketSayisi,
                LotNo = (requested.LotNo ?? string.Empty).Trim(),
                SonKullanmaTarihi = requested.SonKullanmaTarihi,
                SicaklikMin = requested.SicaklikMin,
                SicaklikMax = requested.SicaklikMax,
                SeriNo = (requested.SeriNo ?? string.Empty).Trim(),
                Agirlik = requested.Agirlik,
                PaletKoli = requested.PaletKoli
            };
            db.TedarikciSevkiyatKalemleri.Add(shipmentLine);
            await db.SaveChangesAsync(ct);

            var allocated = 0m;
            for (var index = 0; index < requested.EtiketSayisi; index++)
            {
                var labelQuantity = index == requested.EtiketSayisi - 1
                    ? requested.Miktar - allocated
                    : decimal.Round(requested.Miktar / requested.EtiketSayisi, 3, MidpointRounding.ToZero);
                if (labelQuantity <= 0m)
                    throw new ArgumentException("Etiket sayısı sevk miktarıyla uyumlu değil.");
                allocated += labelQuantity;
                var rawCode = $"scq1_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
                var label = new TedarikciSevkiyatEtiketi
                {
                    TedarikciSevkiyatKalemiId = shipmentLine.Id,
                    KodHash = HashQrCode(rawCode),
                    Miktar = labelQuantity
                };
                db.TedarikciSevkiyatEtiketleri.Add(label);
                await db.SaveChangesAsync(ct);
                resultLabels.Add(new TedarikciSevkiyatEtiketiDto(
                    label.Id, rawCode, $"https://systemcel.app/app/tedarikci-pazaryeri?qr={rawCode}", labelQuantity,
                    orderLine.Ad, orderLine.Sku, orderLine.Birim, shipmentLine.LotNo, shipmentLine.SonKullanmaTarihi));
            }
            orderLine.SevkEdilenMiktar += requested.Miktar;
        }

        var allLines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == order.Id).ToListAsync(ct);
        var nextState = allLines.Any(x => x.KabulEdilenMiktar > 0m || x.ReddedilenMiktar > 0m)
            ? PazaryeriSiparisDurumlari.KismenKabul
            : allLines.All(x => x.SevkEdilenMiktar >= x.Miktar)
                ? PazaryeriSiparisDurumlari.MalKabulBekliyor
                : PazaryeriSiparisDurumlari.KismenSevkEdildi;
        AddStateHistory(db, order, nextState, supplierBusinessId, $"{shipment.SevkiyatNo} oluşturuldu.");
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new TedarikciSevkiyatSonucu(shipment.Id, shipment.SevkiyatNo, shipment.Durum, resultLabels);
    }

    public async Task<TedarikciQrCozumDto> ResolveShipmentQrAsync(string code, CancellationToken ct = default)
    {
        var businessId = await _isletmeService.GetActiveIdAsync();
        var codeHash = HashQrCode(NormalizeQrCode(code));
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await (from label in db.TedarikciSevkiyatEtiketleri.AsNoTracking()
                         join shipmentLine in db.TedarikciSevkiyatKalemleri.AsNoTracking() on label.TedarikciSevkiyatKalemiId equals shipmentLine.Id
                         join shipment in db.TedarikciSevkiyatlari.AsNoTracking() on shipmentLine.TedarikciSevkiyatId equals shipment.Id
                         join orderLine in db.TedarikciSiparisKalemleri.AsNoTracking() on shipmentLine.TedarikciSiparisKalemiId equals orderLine.Id
                         join order in db.TedarikciSiparisleri.AsNoTracking() on shipment.TedarikciSiparisId equals order.Id
                         where label.KodHash == codeHash && (shipment.AliciIsletmeId == businessId || shipment.TedarikciIsletmeId == businessId)
                         select new { label, shipmentLine, orderLine, order }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("QR etiketi bulunamadı veya bu işletmeye ait değil.");
        return new TedarikciQrCozumDto(
            NormalizeQrCode(code), row.order.Id, row.order.SiparisNo, row.orderLine.Ad, row.orderLine.Sku,
            row.orderLine.Birim, row.label.Miktar, row.shipmentLine.LotNo, row.shipmentLine.SonKullanmaTarihi,
            row.label.Durum, row.shipmentLine.SicaklikMin, row.shipmentLine.SicaklikMax);
    }

    public async Task ValidateReceiptEvidenceAccessAsync(
        string code,
        int? branchId,
        int? warehouseId,
        CancellationToken ct = default)
    {
        var buyerBusinessId = await _isletmeService.GetActiveIdAsync();
        var codeHash = HashQrCode(NormalizeQrCode(code));
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var belongsToBuyer = await (from label in db.TedarikciSevkiyatEtiketleri.AsNoTracking()
                                    join shipmentLine in db.TedarikciSevkiyatKalemleri.AsNoTracking() on label.TedarikciSevkiyatKalemiId equals shipmentLine.Id
                                    join shipment in db.TedarikciSevkiyatlari.AsNoTracking() on shipmentLine.TedarikciSevkiyatId equals shipment.Id
                                    where label.KodHash == codeHash && shipment.AliciIsletmeId == buyerBusinessId
                                    select label.Id).AnyAsync(ct);
        if (!belongsToBuyer)
            throw new UnauthorizedAccessException("Bu sevkiyat için mal kabul kanıtı ekleyemezsiniz.");
        _ = await RequireReceiptActorAsync(db, buyerBusinessId, branchId, warehouseId, ct);
    }

    public async Task<TedarikciMalKabulSonucu> ReceiveShipmentQrAsync(
        string code,
        TedarikciMalKabulRequest request,
        CancellationToken ct = default)
    {
        var buyerBusinessId = await _isletmeService.GetActiveIdAsync();
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ArgumentException("İşlem anahtarı gereklidir.");
        if (request.KabulEdilenMiktar < 0m || request.ReddedilenMiktar < 0m)
            throw new ArgumentException("Kabul ve ret miktarları negatif olamaz.");
        if (request.ReddedilenMiktar > 0m && string.IsNullOrWhiteSpace(request.RedNedeni))
            throw new ArgumentException("Reddedilen miktar için neden seçin.");
        var primaryReason = (request.RedNedeni ?? string.Empty).Trim();
        var secondaryReason = (request.IkinciRedNedeni ?? string.Empty).Trim();
        if (primaryReason.Length > 80 || secondaryReason.Length > 160)
            throw new ArgumentException("Ret nedeni çok uzun.");
        if (!string.IsNullOrEmpty(secondaryReason) && (request.ReddedilenMiktar <= 0m ||
            string.IsNullOrEmpty(primaryReason) || string.Equals(primaryReason, secondaryReason, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("İkinci ret nedeni ilk nedenden farklı olmalıdır.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var codeHash = HashQrCode(NormalizeQrCode(code));
        var existing = await db.TedarikciMalKabulleri.SingleOrDefaultAsync(
            x => x.AliciIsletmeId == buyerBusinessId && x.IdempotencyAnahtari == request.IdempotencyKey, ct);
        if (existing is not null)
        {
            var existingCodeHash = await db.TedarikciSevkiyatEtiketleri
                .Where(x => x.Id == existing.TedarikciSevkiyatEtiketiId)
                .Select(x => x.KodHash)
                .SingleAsync(ct);
            if (existingCodeHash != codeHash ||
                existing.KabulEdilenMiktar != request.KabulEdilenMiktar ||
                existing.ReddedilenMiktar != request.ReddedilenMiktar ||
                existing.RedNedeni != primaryReason || existing.IkinciRedNedeni != secondaryReason)
                throw new InvalidOperationException("Bu işlem anahtarı farklı bir mal kabul için daha önce kullanıldı.");
            var existingOrder = await db.TedarikciSiparisleri.SingleAsync(x => x.Id == existing.TedarikciSiparisId, ct);
            return new TedarikciMalKabulSonucu(existing.Id, existingOrder.Durum, true);
        }

        var row = await (from label in db.TedarikciSevkiyatEtiketleri
                         join shipmentLine in db.TedarikciSevkiyatKalemleri on label.TedarikciSevkiyatKalemiId equals shipmentLine.Id
                         join shipment in db.TedarikciSevkiyatlari on shipmentLine.TedarikciSevkiyatId equals shipment.Id
                         join orderLine in db.TedarikciSiparisKalemleri on shipmentLine.TedarikciSiparisKalemiId equals orderLine.Id
                         join order in db.TedarikciSiparisleri on shipment.TedarikciSiparisId equals order.Id
                         where label.KodHash == codeHash && shipment.AliciIsletmeId == buyerBusinessId
                         select new { label, shipmentLine, shipment, orderLine, order }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("QR etiketi bulunamadı veya bu işletmeye ait değil.");
        if (row.label.Durum != "Hazir")
            throw new InvalidOperationException("Bu QR etiketi daha önce işleme alındı.");
        if (request.KabulEdilenMiktar + request.ReddedilenMiktar != row.label.Miktar)
            throw new ArgumentException("Kabul ve ret miktarlarının toplamı etiket miktarına eşit olmalıdır.");
        if (request.OlculenAgirlik is < 0m)
            throw new ArgumentException("Ölçülen ağırlık negatif olamaz.");
        if (request.KabulEdilenMiktar > 0m && row.shipmentLine.SonKullanmaTarihi is { } expiryDate &&
            expiryDate.Date < TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).Date)
            throw new ArgumentException("Son kullanma tarihi geçmiş ürün kabul edilemez; ret veya inceleme kaydı oluşturun.");
        if (request.KabulEdilenMiktar > 0m &&
            (row.shipmentLine.SicaklikMin is not null || row.shipmentLine.SicaklikMax is not null))
        {
            if (request.OlculenSicaklik is null)
                throw new ArgumentException("Sıcaklık aralığı tanımlı ürün için ölçülen sıcaklığı girin.");
            if ((row.shipmentLine.SicaklikMin is { } minimum && request.OlculenSicaklik.Value < minimum) ||
                (row.shipmentLine.SicaklikMax is { } maximum && request.OlculenSicaklik.Value > maximum))
                throw new ArgumentException("Ölçülen sıcaklık sevkiyat aralığı dışında; ürünü kabul etmeden önce ret veya inceleme kaydı oluşturun.");
        }
        if (row.order.Durum is not (PazaryeriSiparisDurumlari.KismenSevkEdildi or PazaryeriSiparisDurumlari.SevkEdildi or PazaryeriSiparisDurumlari.MalKabulBekliyor or PazaryeriSiparisDurumlari.KismenKabul or PazaryeriSiparisDurumlari.Itirazli))
            throw new InvalidOperationException("Sipariş mal kabule uygun durumda değil.");

        var disputeWasOpen = row.order.Durum == PazaryeriSiparisDurumlari.Itirazli;
        var actorReference = await RequireReceiptActorAsync(db, buyerBusinessId, request.SubeId, request.DepoId, ct);
        var suppliedEvidenceHash = (request.BelgeKarmasi ?? string.Empty).Trim().ToLowerInvariant();
        var evidenceHash = suppliedEvidenceHash.Length == 64 && suppliedEvidenceHash.All(Uri.IsHexDigit)
            ? suppliedEvidenceHash
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
                codeHash,
                request.IdempotencyKey.Trim(),
                request.KabulEdilenMiktar,
                request.ReddedilenMiktar,
                primaryReason,
                secondaryReason,
                (request.Not ?? string.Empty).Trim())))).ToLowerInvariant();

        var receipt = new TedarikciMalKabul
        {
            TedarikciSevkiyatEtiketiId = row.label.Id,
            TedarikciSiparisId = row.order.Id,
            AliciIsletmeId = buyerBusinessId,
            IdempotencyAnahtari = request.IdempotencyKey.Trim(),
            KabulEdilenMiktar = request.KabulEdilenMiktar,
            ReddedilenMiktar = request.ReddedilenMiktar,
            RedNedeni = primaryReason,
            IkinciRedNedeni = secondaryReason,
            Not = (request.Not ?? string.Empty).Trim(),
            SubeId = request.SubeId,
            DepoId = request.DepoId,
            IslemYapanKullaniciRef = actorReference,
            CihazRef = Truncate(request.CihazRef, 160),
            IpAdresi = Truncate(request.IpAdresi, 64),
            BelgeKarmasi = evidenceHash,
            FotoKanitiYolu = Truncate(request.FotoKanitiYolu, 500),
            OlculenAgirlik = request.OlculenAgirlik,
            OlculenSicaklik = request.OlculenSicaklik
        };
        db.TedarikciMalKabulleri.Add(receipt);
        row.label.Durum = request.ReddedilenMiktar > 0m ? "Sorunlu" : "KabulEdildi";
        row.label.OkutulduAt = DateTime.UtcNow;
        row.orderLine.KabulEdilenMiktar += request.KabulEdilenMiktar;
        row.orderLine.ReddedilenMiktar += request.ReddedilenMiktar;

        await db.SaveChangesAsync(ct);
        if (request.KabulEdilenMiktar > 0m)
        {
            await ApplyAcceptedReceiptAsync(db, row.order, row.orderLine, receipt, ct);
            await db.SaveChangesAsync(ct);
        }

        if (request.ReddedilenMiktar > 0m)
        {
            await AddRejectedReceiptComplaintAsync(db, row.order, row.orderLine, receipt, ct);
            if (!disputeWasOpen)
                AddStateHistory(db, row.order, PazaryeriSiparisDurumlari.Itirazli, buyerBusinessId,
                    $"{row.orderLine.Ad}: {request.ReddedilenMiktar} {row.orderLine.Birim} reddedildi. {receipt.RedNedeni}{(string.IsNullOrEmpty(receipt.IkinciRedNedeni) ? "" : $"; {receipt.IkinciRedNedeni}")}");
            var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == row.order.Id, ct);
            if (settlement is not null)
            {
                settlement.Durum = settlement.OdenenTutar > 0m ? "KismenSerbest" : "Bloke";
                settlement.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (!disputeWasOpen)
        {
            var lines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == row.order.Id).ToListAsync(ct);
            if (lines.All(x => x.KabulEdilenMiktar >= x.Miktar && x.ReddedilenMiktar == 0m))
            {
                AddStateHistory(db, row.order, PazaryeriSiparisDurumlari.TeslimEdildi, buyerBusinessId, "QR mal kabulü tamamlandı.");
                await FinalizeAcceptedDeliveryAsync(db, row.order, buyerBusinessId, ct);
            }
            else if (row.order.Durum != PazaryeriSiparisDurumlari.KismenKabul)
            {
                AddStateHistory(db, row.order, PazaryeriSiparisDurumlari.KismenKabul, buyerBusinessId, "QR ile kısmi mal kabul yapıldı.");
            }
        }

        var shipmentLabels = await (from shipmentLine in db.TedarikciSevkiyatKalemleri
                                    join label in db.TedarikciSevkiyatEtiketleri on shipmentLine.Id equals label.TedarikciSevkiyatKalemiId
                                    where shipmentLine.TedarikciSevkiyatId == row.shipment.Id
                                    select label).ToListAsync(ct);
        row.shipment.Durum = shipmentLabels.Any(x => x.Durum == "Sorunlu")
            ? "Sorunlu"
            : shipmentLabels.All(x => x.Durum != "Hazir")
                ? "KabulEdildi"
                : "KismenKabul";
        row.shipment.UpdatedAt = DateTime.UtcNow;

        await SyncMasterStateAsync(db, row.order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new TedarikciMalKabulSonucu(receipt.Id, row.order.Durum);
    }

    public async Task<TedarikciSiparisSikayeti> CreateComplaintAsync(
        int supplierOrderId,
        TedarikciSikayetOlusturRequest request,
        CancellationToken ct = default)
    {
        ValidateComplaint(request);
        var buyerBusinessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(
            x => x.Id == supplierOrderId && x.AliciIsletmeId == buyerBusinessId, ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı veya bu işletmeye ait değil.");
        if (order.Durum is PazaryeriSiparisDurumlari.OdemeBekliyor
                or PazaryeriSiparisDurumlari.SiparisVerildi
                or PazaryeriSiparisDurumlari.Odendi
                or PazaryeriSiparisDurumlari.TedarikciOnayladi
                or PazaryeriSiparisDurumlari.Hazirlaniyor
                or PazaryeriSiparisDurumlari.IptalEdildi
                or PazaryeriSiparisDurumlari.IadeEdildi)
            throw new InvalidOperationException("Teslimat başlamadan bu sipariş için sorun bildirilemez.");
        if (await db.TedarikciSiparisSikayetleri.AnyAsync(x => x.TedarikciSiparisId == order.Id, ct))
            throw new InvalidOperationException("Bu sipariş için daha önce sorun bildirildi.");

        var complaint = new TedarikciSiparisSikayeti
        {
            TedarikciSiparisId = order.Id,
            AliciIsletmeId = buyerBusinessId,
            TedarikciIsletmeId = order.TedarikciIsletmeId,
            Kategori = request.Kategori.Trim(),
            Aciklama = request.Aciklama.Trim(),
            Talep = request.Talep.Trim(),
            Durum = TedarikciSikayetDurumlari.Acik
        };
        db.TedarikciSiparisSikayetleri.Add(complaint);

        if (order.Durum != PazaryeriSiparisDurumlari.Tamamlandi && order.Durum != PazaryeriSiparisDurumlari.Itirazli)
            AddStateHistory(db, order, PazaryeriSiparisDurumlari.Itirazli, buyerBusinessId, $"Teslimat sorunu: {complaint.Kategori}");
        var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        if (settlement is not null && settlement.Durum != "Tamamlandi")
        {
            settlement.Durum = "Bloke";
            settlement.UpdatedAt = DateTime.UtcNow;
        }

        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return complaint;
    }

    public async Task<TedarikciSiparisSikayeti> RespondToComplaintAsync(
        int complaintId,
        TedarikciSikayetYanitRequest request,
        CancellationToken ct = default)
    {
        var supplierBusinessId = await _isletmeService.GetActiveIdAsync();
        var response = request.Yanit?.Trim() ?? string.Empty;
        if (response.Length is < 10 or > 2000)
            throw new ArgumentException("Yanıt 10 ile 2000 karakter arasında olmalıdır.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var complaint = await db.TedarikciSiparisSikayetleri.SingleOrDefaultAsync(
            x => x.Id == complaintId && x.TedarikciIsletmeId == supplierBusinessId, ct)
            ?? throw new KeyNotFoundException("Şikâyet bulunamadı veya bu işletmeye ait değil.");
        if (complaint.Durum is TedarikciSikayetDurumlari.Cozuldu or TedarikciSikayetDurumlari.Cozulemedi)
            throw new InvalidOperationException("Kapatılmış şikâyete yanıt verilemez.");

        complaint.TedarikciYaniti = response;
        complaint.Durum = TedarikciSikayetDurumlari.Yanitlandi;
        complaint.YanitlandiAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return complaint;
    }

    public async Task<TedarikciSiparisSikayeti> CloseComplaintAsync(
        int complaintId,
        TedarikciSikayetKapatRequest request,
        CancellationToken ct = default)
    {
        var buyerBusinessId = await _isletmeService.GetActiveIdAsync();
        var note = request.Not?.Trim() ?? string.Empty;
        if (!request.Cozuldu && note.Length < 10)
            throw new ArgumentException("Çözülemeyen sorun için kısa bir açıklama yazın.");
        if (note.Length > 1000)
            throw new ArgumentException("Sonuç notu 1000 karakterden uzun olamaz.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var complaint = await db.TedarikciSiparisSikayetleri.SingleOrDefaultAsync(
            x => x.Id == complaintId && x.AliciIsletmeId == buyerBusinessId, ct)
            ?? throw new KeyNotFoundException("Şikâyet bulunamadı veya bu işletmeye ait değil.");
        if (complaint.Durum is TedarikciSikayetDurumlari.Cozuldu or TedarikciSikayetDurumlari.Cozulemedi)
            throw new InvalidOperationException("Şikâyet daha önce kapatıldı.");

        complaint.Durum = request.Cozuldu ? TedarikciSikayetDurumlari.Cozuldu : TedarikciSikayetDurumlari.Cozulemedi;
        complaint.KapanisNotu = note;
        complaint.KapatildiAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        if (request.Cozuldu)
        {
            var order = await db.TedarikciSiparisleri.SingleAsync(x => x.Id == complaint.TedarikciSiparisId, ct);
            if (order.Durum == PazaryeriSiparisDurumlari.Itirazli)
            {
                var disputeHistory = await db.TedarikciSiparisDurumKayitlari
                    .Where(x => x.TedarikciSiparisId == order.Id && x.YeniDurum == PazaryeriSiparisDurumlari.Itirazli)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(ct);
                var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
                var target = settlement is not null
                    ? PazaryeriSiparisDurumlari.HakEdisBekliyor
                    : disputeHistory?.OncekiDurum == PazaryeriSiparisDurumlari.CariOdemeBekliyor
                        ? PazaryeriSiparisDurumlari.CariOdemeBekliyor
                        : disputeHistory?.OncekiDurum ?? PazaryeriSiparisDurumlari.TeslimEdildi;
                if (settlement is not null)
                {
                    settlement.Durum = "Bekliyor";
                    settlement.UpdatedAt = DateTime.UtcNow;
                }
                AddStateHistory(db, order, target, buyerBusinessId, string.IsNullOrWhiteSpace(note) ? "Alıcı sorunun çözüldüğünü bildirdi." : note);
                await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return complaint;
    }

    public async Task<TedarikciDegerlendirmesi> SaveSupplierRatingAsync(
        int supplierOrderId,
        TedarikciDegerlendirmeKaydetRequest request,
        CancellationToken ct = default)
    {
        ValidateRating(request);
        var buyerBusinessId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(
            x => x.Id == supplierOrderId && x.AliciIsletmeId == buyerBusinessId, ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı veya bu işletmeye ait değil.");
        if (order.Durum is PazaryeriSiparisDurumlari.IptalEdildi or PazaryeriSiparisDurumlari.IadeEdildi)
            throw new InvalidOperationException("İptal veya iade edilmiş sipariş değerlendirilemez.");
        var hasReceipt = await db.TedarikciMalKabulleri.AnyAsync(x => x.TedarikciSiparisId == order.Id, ct);
        if (!hasReceipt && order.TeslimEdildiAt is null)
            throw new InvalidOperationException("Tedarikçi yalnız teslimat kaydından sonra değerlendirilebilir.");

        var rating = await db.TedarikciDegerlendirmeleri.SingleOrDefaultAsync(
            x => x.TedarikciSiparisId == order.Id, ct);
        if (rating is null)
        {
            rating = new TedarikciDegerlendirmesi
            {
                TedarikciSiparisId = order.Id,
                AliciIsletmeId = buyerBusinessId,
                TedarikciIsletmeId = order.TedarikciIsletmeId
            };
            db.TedarikciDegerlendirmeleri.Add(rating);
        }

        rating.UrunUygunluguPuani = request.UrunUygunluguPuani;
        rating.EksiksizTeslimatPuani = request.EksiksizTeslimatPuani;
        rating.HasarsizTeslimatPuani = request.HasarsizTeslimatPuani;
        rating.ZamanindaTeslimatPuani = request.ZamanindaTeslimatPuani;
        rating.SorunCozmePuani = request.SorunCozmePuani;
        rating.Yorum = (request.Yorum ?? string.Empty).Trim();
        var scores = new List<int>
        {
            request.UrunUygunluguPuani,
            request.EksiksizTeslimatPuani,
            request.HasarsizTeslimatPuani,
            request.ZamanindaTeslimatPuani
        };
        if (request.SorunCozmePuani.HasValue)
            scores.Add(request.SorunCozmePuani.Value);
        rating.OrtalamaPuan = decimal.Round(scores.Select(x => (decimal)x).Average(), 2, MidpointRounding.AwayFromZero);
        rating.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return rating;
    }

    public async Task CancelOrderAsync(
        int masterOrderId,
        PazaryeriIptalRequest request,
        CancellationToken ct = default)
    {
        var buyerId = await _isletmeService.GetActiveIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var master = await db.PazaryeriAnaSiparisleri.SingleOrDefaultAsync(
            x => x.Id == masterOrderId && x.AliciIsletmeId == buyerId, ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");
        var orders = await db.TedarikciSiparisleri.Where(x => x.AnaSiparisId == master.Id).ToListAsync(ct);
        if (orders.Any(x => x.Durum is PazaryeriSiparisDurumlari.KismenSevkEdildi
                or PazaryeriSiparisDurumlari.SevkEdildi
                or PazaryeriSiparisDurumlari.KismenKabul
                or PazaryeriSiparisDurumlari.TeslimEdildi
                or PazaryeriSiparisDurumlari.HakEdisBekliyor
                or PazaryeriSiparisDurumlari.Tamamlandi))
            throw new InvalidOperationException("Sevk edilen sipariş uygulamadan iptal edilemez.");

        var orderIds = orders.Select(x => x.Id).ToList();
        var lines = await db.TedarikciSiparisKalemleri.Where(x => orderIds.Contains(x.TedarikciSiparisId)).ToListAsync(ct);
        var products = await db.TedarikciUrunleri.Where(x => lines.Select(y => y.TedarikciUrunId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var payment = await db.PazaryeriOdemeleri.SingleOrDefaultAsync(x => x.AnaSiparisId == master.Id && x.Durum == "Basarili", ct);
        if (payment is not null)
        {
            var refund = await _paymentGateway.RefundAsync(
                payment.SaglayiciIslemId,
                payment.Tutar,
                payment.ParaBirimi,
                $"cancel:{master.Id}:{payment.Id}",
                ct);
            if (!refund.Succeeded)
                throw new InvalidOperationException(refund.Error);
            payment.Durum = "IadeEdildi";
            payment.IadeTutari = payment.Tutar;
            payment.IadeEdildiAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var line in lines)
        {
            var product = products[line.TedarikciUrunId];
            product.RezerveMiktar = Math.Max(0m,
                product.RezerveMiktar - Math.Max(0m, line.Miktar - line.SevkEdilenMiktar));
            product.UpdatedAt = DateTime.UtcNow;
        }

        var targetState = payment is null ? PazaryeriSiparisDurumlari.IptalEdildi : PazaryeriSiparisDurumlari.IadeEdildi;
        master.Durum = targetState;
        master.UpdatedAt = DateTime.UtcNow;
        foreach (var order in orders)
        {
            AddStateHistory(db, order, targetState, buyerId, request.Neden);
            if (payment is not null)
            {
                var allocation = await db.PazaryeriOdemeDagitimlari.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
                allocation.IadeTutari = allocation.BrutTutar;
                allocation.UpdatedAt = DateTime.UtcNow;
            }
            var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
            if (settlement is not null)
            {
                settlement.IadeTutari = settlement.BrutTutar;
                settlement.NetTutar = 0m;
                settlement.Durum = "IptalEdildi";
                settlement.UpdatedAt = DateTime.UtcNow;
                AddLedgerEntry(db, order.Id, "Iade", "Borc", order.GenelToplam, order.ParaBirimi, "Sipariş iadesi");
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task CancelSupplierOrderAsync(
        int supplierOrderId,
        PazaryeriIptalRequest request,
        CancellationToken ct = default)
    {
        var activeBusinessId = await _isletmeService.GetActiveIdAsync();
        if (string.IsNullOrWhiteSpace(request.Neden))
            throw new ArgumentException("İptal nedeni gereklidir.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        if (activeBusinessId != order.AliciIsletmeId && activeBusinessId != order.TedarikciIsletmeId)
            throw new UnauthorizedAccessException("Bu siparişe erişemezsiniz.");
        if (order.Durum is PazaryeriSiparisDurumlari.KismenSevkEdildi
                or PazaryeriSiparisDurumlari.SevkEdildi
                or PazaryeriSiparisDurumlari.KismenKabul
                or PazaryeriSiparisDurumlari.TeslimEdildi
                or PazaryeriSiparisDurumlari.HakEdisBekliyor
                or PazaryeriSiparisDurumlari.Tamamlandi
                or PazaryeriSiparisDurumlari.IptalEdildi
                or PazaryeriSiparisDurumlari.IadeEdildi)
            throw new InvalidOperationException("Bu tedarikçi siparişi artık iptal edilemez.");

        var lines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == order.Id).ToListAsync(ct);
        var products = await db.TedarikciUrunleri.Where(x => lines.Select(y => y.TedarikciUrunId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var payment = await db.PazaryeriOdemeleri.SingleOrDefaultAsync(x =>
            x.AnaSiparisId == order.AnaSiparisId && (x.Durum == "Basarili" || x.Durum == "KismiIade"), ct);
        if (payment is not null)
        {
            var refund = await _paymentGateway.RefundAsync(
                payment.SaglayiciIslemId,
                order.GenelToplam,
                order.ParaBirimi,
                $"supplier-cancel:{order.Id}:{payment.Id}",
                ct);
            if (!refund.Succeeded)
                throw new InvalidOperationException(refund.Error);
            payment.IadeTutari = Money(payment.IadeTutari + order.GenelToplam);
            payment.IadeEdildiAt = DateTime.UtcNow;
            payment.Durum = payment.IadeTutari >= payment.Tutar ? "IadeEdildi" : "KismiIade";
            payment.UpdatedAt = DateTime.UtcNow;
            var allocation = await db.PazaryeriOdemeDagitimlari.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
            allocation.IadeTutari = order.GenelToplam;
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var line in lines)
        {
            var product = products[line.TedarikciUrunId];
            product.RezerveMiktar = Math.Max(0m,
                product.RezerveMiktar - Math.Max(0m, line.Miktar - line.SevkEdilenMiktar));
            product.UpdatedAt = DateTime.UtcNow;
        }

        var targetState = payment is null ? PazaryeriSiparisDurumlari.IptalEdildi : PazaryeriSiparisDurumlari.IadeEdildi;
        AddStateHistory(db, order, targetState, activeBusinessId, request.Neden);
        var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        if (settlement is not null)
        {
            settlement.IadeTutari = settlement.BrutTutar;
            settlement.NetTutar = 0m;
            settlement.Durum = "IptalEdildi";
            settlement.UpdatedAt = DateTime.UtcNow;
            AddLedgerEntry(db, order.Id, "Iade", "Borc", order.GenelToplam, order.ParaBirimi, "Tedarikçi siparişi iadesi");
        }

        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task DisputeSupplierOrderAsync(int supplierOrderId, string reason, CancellationToken ct = default)
    {
        var activeBusinessId = await _isletmeService.GetActiveIdAsync();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("İtiraz nedeni gereklidir.");
        if (reason.Trim().Length > 700)
            throw new ArgumentException("İtiraz nedeni 700 karakterden uzun olamaz.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        if (activeBusinessId != order.AliciIsletmeId && activeBusinessId != order.TedarikciIsletmeId)
            throw new UnauthorizedAccessException("Bu siparişe erişemezsiniz.");
        if (order.Durum is PazaryeriSiparisDurumlari.OdemeBekliyor
                or PazaryeriSiparisDurumlari.IptalEdildi
                or PazaryeriSiparisDurumlari.IadeEdildi
                or PazaryeriSiparisDurumlari.Tamamlandi)
            throw new InvalidOperationException("Bu sipariş için itiraz açılamaz.");

        AddStateHistory(db, order, PazaryeriSiparisDurumlari.Itirazli, activeBusinessId, reason);
        var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        if (settlement is not null)
        {
            settlement.Durum = "Bloke";
            settlement.UpdatedAt = DateTime.UtcNow;
        }
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ResolveDisputeAsync(
        int supplierOrderId,
        PazaryeriItirazCozRequest request,
        CancellationToken ct = default)
    {
        if (!await _yonetimService.IsCurrentUserAdminAsync(ct))
            throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekir.");
        if (string.IsNullOrWhiteSpace(request.Not))
            throw new ArgumentException("İnceleme notu gereklidir.");
        if (request.Not.Trim().Length > 500)
            throw new ArgumentException("İnceleme notu 500 karakterden uzun olamaz.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        if (order.Durum != PazaryeriSiparisDurumlari.Itirazli)
            throw new InvalidOperationException("Siparişte açık bir itiraz bulunmuyor.");
        if (!request.DevamEt)
            return;

        var decision = string.IsNullOrWhiteSpace(request.Karar)
            ? PazaryeriItirazKararlari.TedarikciyeAktar
            : request.Karar.Trim();
        if (decision is not (PazaryeriItirazKararlari.TedarikciyeAktar
                or PazaryeriItirazKararlari.AliciyaIade
                or PazaryeriItirazKararlari.KismiPaylas
                or PazaryeriItirazKararlari.YenidenTeslim))
            throw new ArgumentException("Geçerli bir itiraz kararı seçin.");

        var disputeHistory = await db.TedarikciSiparisDurumKayitlari
            .Where(x => x.TedarikciSiparisId == order.Id && x.YeniDurum == PazaryeriSiparisDurumlari.Itirazli)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        var settlement = await db.TedarikciHakEdisleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        var target = settlement is not null
            ? PazaryeriSiparisDurumlari.HakEdisBekliyor
            : disputeHistory?.OncekiDurum == PazaryeriSiparisDurumlari.CariOdemeBekliyor
                ? PazaryeriSiparisDurumlari.CariOdemeBekliyor
                : disputeHistory?.OncekiDurum ?? PazaryeriSiparisDurumlari.TeslimEdildi;
        var adminReference = Truncate(_currentUserContext.GetCurrentUser()?.ProviderUserId ?? "system-admin", 160);
        var auditNote = $"Yönetici {adminReference}. İtiraz kararı: {decision}. {request.Not.Trim()}";
        if (decision == PazaryeriItirazKararlari.YenidenTeslim)
        {
            var rejectedLines = await db.TedarikciSiparisKalemleri
                .Where(x => x.TedarikciSiparisId == order.Id && x.ReddedilenMiktar > 0m)
                .ToListAsync(ct);
            if (rejectedLines.Count == 0)
                throw new InvalidOperationException("Yeniden teslim için reddedilmiş bir sipariş kalemi bulunmuyor.");
            foreach (var line in rejectedLines)
            {
                var product = await db.TedarikciUrunleri.SingleAsync(x => x.Id == line.TedarikciUrunId, ct);
                if (product.StokMiktari - product.RezerveMiktar < line.ReddedilenMiktar)
                    throw new InvalidOperationException($"{line.Ad} için yeniden sevk edilecek stok bulunmuyor.");
                product.RezerveMiktar += line.ReddedilenMiktar;
                line.SevkEdilenMiktar -= line.ReddedilenMiktar;
                line.ReddedilenMiktar = 0m;
            }
            target = PazaryeriSiparisDurumlari.Hazirlaniyor;
            if (settlement is not null)
            {
                settlement.Durum = "Bloke";
                settlement.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (decision is PazaryeriItirazKararlari.AliciyaIade or PazaryeriItirazKararlari.KismiPaylas)
        {
            if (settlement is null)
                throw new InvalidOperationException("Bu karar için blokeli hakediş kaydı bulunamadı.");

            var distributable = Money(Math.Max(0m, settlement.NetTutar - settlement.OdenenTutar));
            var supplierShare = decision == PazaryeriItirazKararlari.AliciyaIade
                ? 0m
                : Money(request.TedarikciyeAktarilacakTutar ?? -1m);
            if (supplierShare < 0m || supplierShare > distributable)
                throw new ArgumentException("Tedarikçiye aktarılacak tutar blokeli net hakediş aralığında olmalıdır.");

            var buyerRefund = Money(distributable - supplierShare);
            if (buyerRefund > 0m)
            {
                var payment = await db.PazaryeriOdemeleri.SingleOrDefaultAsync(x =>
                    x.AnaSiparisId == order.AnaSiparisId && (x.Durum == "Basarili" || x.Durum == "KismiIade"), ct)
                    ?? throw new InvalidOperationException("Alıcı iadesi için başarılı pazaryeri ödemesi bulunamadı.");
                var refund = await _paymentGateway.RefundAsync(
                    payment.SaglayiciIslemId,
                    buyerRefund,
                    order.ParaBirimi,
                    $"dispute:{order.Id}:{decision}:{buyerRefund:0.00}",
                    ct);
                if (!refund.Succeeded)
                    throw new InvalidOperationException(refund.Error);

                payment.IadeTutari = Money(payment.IadeTutari + buyerRefund);
                payment.IadeEdildiAt = DateTime.UtcNow;
                payment.Durum = payment.IadeTutari >= payment.Tutar ? "IadeEdildi" : "KismiIade";
                payment.UpdatedAt = DateTime.UtcNow;
                var allocation = await db.PazaryeriOdemeDagitimlari.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
                allocation.IadeTutari = Money(allocation.IadeTutari + buyerRefund);
                allocation.UpdatedAt = DateTime.UtcNow;
                settlement.IadeTutari = Money(settlement.IadeTutari + buyerRefund);
                settlement.NetTutar = Money(settlement.OdenenTutar + supplierShare);
                AddLedgerEntry(db, order.Id, "ItirazIadesi", "Borc", buyerRefund, order.ParaBirimi, auditNote);
            }

            settlement.Durum = supplierShare > 0m ? "Bekliyor" : "IadeEdildi";
            settlement.UpdatedAt = DateTime.UtcNow;
            target = supplierShare > 0m
                ? PazaryeriSiparisDurumlari.HakEdisBekliyor
                : PazaryeriSiparisDurumlari.IadeEdildi;
        }
        else if (settlement is not null)
        {
            settlement.Durum = "Bekliyor";
            settlement.UpdatedAt = DateTime.UtcNow;
        }
        AddStateHistory(db, order, target, order.AliciIsletmeId, auditNote);
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task MatchSupplierInvoiceAsync(
        int supplierOrderId,
        TedarikciBelgeEsleRequest request,
        CancellationToken ct = default)
    {
        var activeBusinessId = await _isletmeService.GetActiveIdAsync();
        if (string.IsNullOrWhiteSpace(request.BelgeNo))
            throw new ArgumentException("Fatura numarası gereklidir.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(
            x => x.Id == supplierOrderId && x.TedarikciIsletmeId == activeBusinessId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        var match = await db.TedarikciFaturaEslesmeleri.SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct)
            ?? throw new InvalidOperationException("Fatura eşleştirmesi teslimat tamamlandıktan sonra yapılabilir.");
        var supplierInvoice = await db.Faturalar.SingleAsync(x => x.Id == match.SaticiFaturaId, ct);
        var buyerInvoice = await db.Faturalar.SingleAsync(x => x.Id == match.AliciFaturaId, ct);
        match.TedarikciBelgeNo = request.BelgeNo.Trim();
        match.TedarikciBelgeUuid = (request.BelgeUuid ?? string.Empty).Trim();
        supplierInvoice.PortalBelgeNo = match.TedarikciBelgeNo;
        supplierInvoice.PortalUuid = match.TedarikciBelgeUuid;
        buyerInvoice.PortalBelgeNo = match.TedarikciBelgeNo;
        buyerInvoice.PortalUuid = match.TedarikciBelgeUuid;
        supplierInvoice.UpdatedAt = DateTime.Now;
        buyerInvoice.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteSettlementAsync(
        int supplierOrderId,
        PazaryeriHakEdisTamamlaRequest request,
        CancellationToken ct = default)
    {
        if (!await _yonetimService.IsCurrentUserAdminAsync(ct))
            throw new UnauthorizedAccessException("Bu işlem için yönetici yetkisi gerekir.");
        if (string.IsNullOrWhiteSpace(request.AktarimReferansi))
            throw new ArgumentException("Aktarım referansı gereklidir.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.TedarikciSiparisleri.SingleOrDefaultAsync(x => x.Id == supplierOrderId, ct)
            ?? throw new KeyNotFoundException("Tedarikçi siparişi bulunamadı.");
        if (order.Durum != PazaryeriSiparisDurumlari.HakEdisBekliyor)
            throw new InvalidOperationException("Sipariş hakediş aktarımına hazır değil.");
        var settlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
        if (settlement.Durum == "Tamamlandi")
            return;

        var payment = await db.PazaryeriOdemeleri.SingleOrDefaultAsync(
            x => x.AnaSiparisId == order.AnaSiparisId && (x.Durum == "Basarili" || x.Durum == "KismiIade"), ct)
            ?? throw new InvalidOperationException("Başarılı pazaryeri ödemesi bulunamadı.");
        var remaining = Money(Math.Max(0m, settlement.NetTutar - settlement.OdenenTutar));
        if (remaining <= 0m)
            return;
        var payout = await _paymentGateway.ReleaseAsync(new MarketplacePayoutCommand(
            payment.SaglayiciIslemId,
            $"settlement:{order.Id}:remaining:{settlement.OdenenTutar:0.00}",
            order.TedarikciIsletmeId,
            remaining,
            settlement.ParaBirimi), ct);
        if (!payout.Succeeded)
            throw new InvalidOperationException(payout.Error);

        settlement.Durum = "Tamamlandi";
        settlement.OdenenTutar = Money(settlement.OdenenTutar + remaining);
        settlement.AktarimReferansi = payout.ProviderTransactionId;
        settlement.TamamlandiAt = DateTime.UtcNow;
        settlement.UpdatedAt = DateTime.UtcNow;
        var adminReference = Truncate(_currentUserContext.GetCurrentUser()?.ProviderUserId ?? "system-admin", 160);
        AddStateHistory(db, order, PazaryeriSiparisDurumlari.Tamamlandi, order.AliciIsletmeId,
            $"Yönetici {adminReference}. Hakediş aktarıldı. İşlem referansı: {Truncate(request.AktarimReferansi, 160)}");
        AddLedgerEntry(db, order.Id, "TedarikciOdeme", "Borc", remaining, settlement.ParaBirimi, "Kalan tedarikçi hakedişi");
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<int> ExpirePendingOrdersAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var cutoff = nowUtc.AddMinutes(-Math.Max(1, _options.StokRezervasyonSuresiDakika));
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var masters = await db.PazaryeriAnaSiparisleri
            .Where(x => x.Durum == PazaryeriSiparisDurumlari.OdemeBekliyor && x.CreatedAt <= cutoff)
            .ToListAsync(ct);
        if (masters.Count == 0)
        {
            await transaction.CommitAsync(ct);
            return 0;
        }

        var masterIds = masters.Select(x => x.Id).ToList();
        var orders = await db.TedarikciSiparisleri.Where(x => masterIds.Contains(x.AnaSiparisId)).ToListAsync(ct);
        var orderIds = orders.Select(x => x.Id).ToList();
        var lines = await db.TedarikciSiparisKalemleri.Where(x => orderIds.Contains(x.TedarikciSiparisId)).ToListAsync(ct);
        var productIds = lines.Select(x => x.TedarikciUrunId).Distinct().ToList();
        var products = await db.TedarikciUrunleri.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var line in lines)
        {
            if (products.TryGetValue(line.TedarikciUrunId, out var product))
            {
                product.RezerveMiktar = Math.Max(0m, product.RezerveMiktar - line.Miktar);
                product.UpdatedAt = nowUtc;
            }
        }
        foreach (var order in orders)
            AddStateHistory(db, order, PazaryeriSiparisDurumlari.IptalEdildi, order.AliciIsletmeId, "Ödeme süresi doldu; stok rezervasyonu kaldırıldı.");
        foreach (var master in masters)
        {
            master.Durum = PazaryeriSiparisDurumlari.IptalEdildi;
            master.UpdatedAt = nowUtc;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return masters.Count;
    }

    private TedarikciSiparis BuildSupplierOrder(
        PazaryeriAnaSiparis master,
        TedarikciProfil profile,
        IReadOnlyList<TedarikciUrun> products,
        IReadOnlyDictionary<int, decimal> quantities,
        int sequence,
        string initialState)
    {
        var subtotal = Money(products.Sum(x => x.BirimFiyat * quantities[x.Id]));
        var vat = Money(products.Sum(x => x.BirimFiyat * quantities[x.Id] * x.KdvOrani / 100m));
        var commission = Money(subtotal * profile.KomisyonOrani / 100m);
        var commissionVat = Money(commission * _options.KomisyonKdvOrani / 100m);
        var withholding = profile.TevkifatMuaf ? 0m : Money(subtotal * _options.TevkifatOrani / 100m);
        var paymentFee = Money((subtotal + vat) * _options.OdemeHizmetiOrani / 100m);
        var total = subtotal + vat;
        return new TedarikciSiparis
        {
            AnaSiparisId = master.Id,
            AliciIsletmeId = master.AliciIsletmeId,
            TedarikciIsletmeId = profile.IsletmeId,
            TedarikciProfilId = profile.Id,
            SiparisNo = $"{master.SiparisNo}-{ToAlphaSequence(sequence)}",
            ParaBirimi = master.ParaBirimi,
            AraToplam = subtotal,
            KdvToplam = vat,
            GenelToplam = total,
            KomisyonMatrahi = subtotal,
            KomisyonOrani = profile.KomisyonOrani,
            KomisyonTutari = commission,
            KomisyonKdvTutari = commissionVat,
            TevkifatMatrahi = subtotal,
            TevkifatTutari = withholding,
            OdemeHizmetiBedeli = paymentFee,
            TedarikciHakEdisi = Money(total - commission - commissionVat - withholding - paymentFee),
            Durum = initialState
        };
    }

    private void CreateSettlementAndLedger(CashTrackerDbContext db, TedarikciSiparis order, DateTime now)
    {
        db.TedarikciHakEdisleri.Add(new TedarikciHakEdis
        {
            TedarikciSiparisId = order.Id,
            TedarikciIsletmeId = order.TedarikciIsletmeId,
            BrutTutar = order.GenelToplam,
            KomisyonTutari = order.KomisyonTutari,
            KomisyonKdvTutari = order.KomisyonKdvTutari,
            TevkifatTutari = order.TevkifatTutari,
            OdemeHizmetiBedeli = order.OdemeHizmetiBedeli,
            NetTutar = order.TedarikciHakEdisi,
            ParaBirimi = order.ParaBirimi,
            Durum = "Bekliyor",
            PlanlananAt = now.AddDays(_options.VarsayilanOdemeVadesiGun)
        });
        AddLedgerEntry(db, order.Id, "BrutSatis", "Alacak", order.GenelToplam, order.ParaBirimi, "Müşteri ödemesi");
        AddLedgerEntry(db, order.Id, "Komisyon", "Borc", order.KomisyonTutari, order.ParaBirimi, "Pazaryeri komisyonu");
        AddLedgerEntry(db, order.Id, "KomisyonKdv", "Borc", order.KomisyonKdvTutari, order.ParaBirimi, "Komisyon KDV'si");
        AddLedgerEntry(db, order.Id, "Tevkifat", "Borc", order.TevkifatTutari, order.ParaBirimi, "E-ticaret tevkifatı");
        AddLedgerEntry(db, order.Id, "OdemeHizmeti", "Borc", order.OdemeHizmetiBedeli, order.ParaBirimi, "Ödeme hizmeti bedeli");
        AddLedgerEntry(db, order.Id, "TedarikciHakEdisi", "Alacak", order.TedarikciHakEdisi, order.ParaBirimi, "Tedarikçi net hakedişi");
    }

    private static async Task ConsumeShipmentStockAsync(
        CashTrackerDbContext db,
        TedarikciSiparis order,
        TedarikciSiparisKalemi line,
        decimal quantity,
        string shipmentReference,
        int? shipmentId,
        CancellationToken ct)
    {
        var product = await db.TedarikciUrunleri.SingleAsync(x => x.Id == line.TedarikciUrunId, ct);
        if (product.RezerveMiktar < quantity || product.StokMiktari < quantity)
            throw new InvalidOperationException($"{line.Ad} stok rezervasyonu bozulmuş.");
        product.RezerveMiktar -= quantity;
        product.StokMiktari -= quantity;
        product.UpdatedAt = DateTime.UtcNow;
        if (product.KaynakUrunHizmetId is { } sourceId)
        {
            db.StokHareketleri.Add(new StokHareket
            {
                IsletmeId = order.TedarikciIsletmeId,
                TedarikciSiparisId = order.Id,
                TedarikciSevkiyatId = shipmentId,
                UrunHizmetId = sourceId,
                Tarih = DateTime.Now,
                Miktar = -quantity,
                HareketTipi = "Cikis",
                Kaynak = "PazaryeriSevkiyat",
                Aciklama = $"Sipariş {order.SiparisNo}, sevkiyat {shipmentReference}"
            });
        }
    }

    private async Task FinalizeAcceptedDeliveryAsync(
        CashTrackerDbContext db,
        TedarikciSiparis order,
        int actorBusinessId,
        CancellationToken ct)
    {
        order.TeslimEdildiAt = DateTime.UtcNow;
        order.HakEdisTarihi = await ResolveSettlementDateAsync(db, order.TedarikciProfilId, ct);
        var hasReceiptAccounting = await db.TedarikciMalKabulleri
            .AnyAsync(x => x.TedarikciSiparisId == order.Id && x.MuhasebelestiAt != null, ct);
        var marketplacePayment = await db.PazaryeriOdemeleri.SingleOrDefaultAsync(
            x => x.AnaSiparisId == order.AnaSiparisId && x.Durum == "Basarili", ct);
        if (hasReceiptAccounting)
        {
            if (marketplacePayment is null)
            {
                AddStateHistory(db, order, PazaryeriSiparisDurumlari.CariOdemeBekliyor, actorBusinessId,
                    "Kabul edilen miktarlar stok, fatura ve cariye işlendi.");
                return;
            }

            var receiptSettlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
            receiptSettlement.PlanlananAt = DateTime.UtcNow;
            receiptSettlement.UpdatedAt = DateTime.UtcNow;
            if (receiptSettlement.OdenenTutar >= receiptSettlement.NetTutar)
            {
                receiptSettlement.Durum = "SerbestBirakildi";
                receiptSettlement.TamamlandiAt ??= DateTime.UtcNow;
                AddStateHistory(db, order, PazaryeriSiparisDurumlari.Tamamlandi, actorBusinessId,
                    "Mal kabul ve hakediş tamamlandı.");
            }
            else
            {
                if (receiptSettlement.Durum != "AktarimBasarisiz")
                    receiptSettlement.Durum = "AktarimBekliyor";
                AddStateHistory(db, order, PazaryeriSiparisDurumlari.HakEdisBekliyor, actorBusinessId,
                    "Mal kabul tamamlandı; kalan hakediş güvenli yeniden denemeyi bekliyor.");
            }
            return;
        }

        if (marketplacePayment is not null)
        {
            var settlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
            settlement.PlanlananAt = DateTime.UtcNow;
            settlement.UpdatedAt = DateTime.UtcNow;
            await CreateAccountingRecordsAsync(db, order, true, ct);
            AddStateHistory(db, order, PazaryeriSiparisDurumlari.HakEdisBekliyor, actorBusinessId, "Mal kabul tamamlandı.");
            var payout = await _paymentGateway.ReleaseAsync(new MarketplacePayoutCommand(
                marketplacePayment.SaglayiciIslemId,
                $"settlement:{order.Id}",
                order.TedarikciIsletmeId,
                settlement.NetTutar,
                settlement.ParaBirimi), ct);
            if (payout.Succeeded)
            {
                settlement.Durum = "Tamamlandi";
                settlement.OdenenTutar = settlement.NetTutar;
                settlement.AktarimReferansi = payout.ProviderTransactionId;
                settlement.TamamlandiAt = DateTime.UtcNow;
                settlement.UpdatedAt = DateTime.UtcNow;
                AddStateHistory(db, order, PazaryeriSiparisDurumlari.Tamamlandi, actorBusinessId, "Mal kabul onaylandı; tedarikçi hakedişi serbest bırakıldı.");
                AddLedgerEntry(db, order.Id, "TedarikciOdeme", "Borc", settlement.NetTutar, settlement.ParaBirimi, "Tedarikçi hakedişi");
            }
        }
        else
        {
            await CreateAccountingRecordsAsync(db, order, false, ct);
            AddStateHistory(db, order, PazaryeriSiparisDurumlari.CariOdemeBekliyor, actorBusinessId, "Mal kabul onaylandı; borç ve alacak cariye işlendi.");
        }
    }

    private async Task ApplyAcceptedReceiptAsync(
        CashTrackerDbContext db,
        TedarikciSiparis order,
        TedarikciSiparisKalemi orderLine,
        TedarikciMalKabul receipt,
        CancellationToken ct)
    {
        if (receipt.MuhasebelestiAt is not null || receipt.KabulEdilenMiktar <= 0m)
            return;

        var paid = await db.PazaryeriOdemeleri.AnyAsync(
            x => x.AnaSiparisId == order.AnaSiparisId && x.Durum == "Basarili", ct);
        var acceptedNet = Money(orderLine.BirimFiyat * receipt.KabulEdilenMiktar);
        var acceptedVat = Money(acceptedNet * orderLine.KdvOrani / 100m);
        var acceptedGross = acceptedNet + acceptedVat;
        var now = DateTime.Now;

        var match = await db.TedarikciFaturaEslesmeleri
            .SingleOrDefaultAsync(x => x.TedarikciSiparisId == order.Id, ct);
        Fatura buyerInvoice;
        Fatura supplierInvoice;
        CariKart buyerCari;
        CariKart supplierCari;
        var buyer = await db.Isletmeler.SingleAsync(x => x.Id == order.AliciIsletmeId, ct);
        var supplier = await db.TedarikciProfilleri.SingleAsync(x => x.Id == order.TedarikciProfilId, ct);
        if (match is null)
        {
            buyerCari = await EnsureCariAsync(db, order.AliciIsletmeId, order.TedarikciIsletmeId,
                supplier.Unvan, supplier.VergiNo, supplier.Adres, ct);
            supplierCari = await EnsureCariAsync(db, order.TedarikciIsletmeId, order.AliciIsletmeId,
                buyer.Ad, string.Empty, buyer.Konum, ct);
            buyerInvoice = CreateInvoice(order, order.AliciIsletmeId, buyerCari.Id, "Alis", $"PZ-ALI-{order.SiparisNo}", now, paid);
            supplierInvoice = CreateInvoice(order, order.TedarikciIsletmeId, supplierCari.Id, "Satis", $"PZ-SAT-{order.SiparisNo}", now, paid);
            ResetInvoiceTotals(buyerInvoice);
            ResetInvoiceTotals(supplierInvoice);
            db.Faturalar.AddRange(buyerInvoice, supplierInvoice);
            await db.SaveChangesAsync(ct);
            match = new TedarikciFaturaEslesmesi
            {
                TedarikciSiparisId = order.Id,
                AliciFaturaId = buyerInvoice.Id,
                SaticiFaturaId = supplierInvoice.Id,
                TedarikciBelgeNo = supplierInvoice.YerelFaturaNo
            };
            db.TedarikciFaturaEslesmeleri.Add(match);
        }
        else
        {
            buyerInvoice = await db.Faturalar.SingleAsync(x => x.Id == match.AliciFaturaId, ct);
            supplierInvoice = await db.Faturalar.SingleAsync(x => x.Id == match.SaticiFaturaId, ct);
            buyerCari = await db.CariKartlari.SingleAsync(x => x.Id == buyerInvoice.CariKartId, ct);
            supplierCari = await db.CariKartlari.SingleAsync(x => x.Id == supplierInvoice.CariKartId, ct);
        }

        IncreaseInvoiceTotals(buyerInvoice, acceptedNet, acceptedVat, acceptedGross, paid, now);
        IncreaseInvoiceTotals(supplierInvoice, acceptedNet, acceptedVat, acceptedGross, paid, now);

        var catalogProduct = await db.TedarikciUrunleri.SingleAsync(x => x.Id == orderLine.TedarikciUrunId, ct);
        var buyerProduct = await db.UrunHizmetleri.SingleOrDefaultAsync(x =>
            x.IsletmeId == order.AliciIsletmeId && x.Barkod == orderLine.Sku, ct);
        if (buyerProduct is null)
        {
            buyerProduct = new UrunHizmet
            {
                IsletmeId = order.AliciIsletmeId,
                Tip = "Urun",
                Ad = orderLine.Ad,
                Barkod = orderLine.Sku,
                Birim = orderLine.Birim,
                KdvOrani = orderLine.KdvOrani,
                AlisFiyati = orderLine.BirimFiyat,
                SatisFiyati = orderLine.BirimFiyat,
                ParaBirimi = order.ParaBirimi,
                Aktif = true
            };
            db.UrunHizmetleri.Add(buyerProduct);
            await db.SaveChangesAsync(ct);
        }

        db.FaturaSatirlari.Add(CreateInvoiceLine(
            order.AliciIsletmeId, buyerInvoice.Id, buyerProduct.Id, orderLine, true, receipt.KabulEdilenMiktar));
        db.FaturaSatirlari.Add(CreateInvoiceLine(
            order.TedarikciIsletmeId, supplierInvoice.Id, catalogProduct.KaynakUrunHizmetId, orderLine, false, receipt.KabulEdilenMiktar));
        db.StokHareketleri.Add(new StokHareket
        {
            IsletmeId = order.AliciIsletmeId,
            TedarikciSiparisId = order.Id,
            TedarikciMalKabulId = receipt.Id,
            SubeId = receipt.SubeId,
            DepoId = receipt.DepoId,
            UrunHizmetId = buyerProduct.Id,
            Tarih = now,
            Miktar = receipt.KabulEdilenMiktar,
            BirimMaliyet = orderLine.BirimFiyat,
            MaliyetParaBirimi = order.ParaBirimi,
            MaliyetKurSnapshot = 1m,
            BirimMaliyetTry = orderLine.BirimFiyat,
            HareketTipi = "Giris",
            Kaynak = "PazaryeriMalKabul",
            Aciklama = $"Sipariş {order.SiparisNo}, kabul #{receipt.Id}"
        });
        if (paid)
        {
            AddPaidAccountingEntriesForAmount(db, buyerInvoice, buyerCari.Id, "Borc", "Odeme", order, acceptedGross, now, receipt.Id);
            AddPaidAccountingEntriesForAmount(db, supplierInvoice, supplierCari.Id, "Alacak", "Tahsilat", order, acceptedGross, now, receipt.Id);
        }
        else
        {
            AddInvoiceCariEntryForAmount(db, buyerInvoice, buyerCari.Id, "Borc", order, acceptedGross, now, receipt.Id);
            AddInvoiceCariEntryForAmount(db, supplierInvoice, supplierCari.Id, "Alacak", order, acceptedGross, now, receipt.Id);
        }

        receipt.KabulBrutTutar = acceptedGross;
        receipt.MuhasebelestiAt = DateTime.UtcNow;

        if (!paid)
            return;

        var settlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
        var remaining = Money(Math.Max(0m, settlement.NetTutar - settlement.OdenenTutar));
        if (remaining <= 0m)
            return;
        var lines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == order.Id).ToListAsync(ct);
        var isFinalAcceptedAmount = lines.All(x => x.KabulEdilenMiktar >= x.Miktar && x.ReddedilenMiktar == 0m);
        var proportional = order.GenelToplam <= 0m
            ? 0m
            : Money(settlement.NetTutar * acceptedGross / order.GenelToplam);
        var releaseAmount = isFinalAcceptedAmount ? remaining : Math.Min(remaining, proportional);
        if (releaseAmount <= 0m)
            return;

        var payment = await db.PazaryeriOdemeleri.SingleAsync(
            x => x.AnaSiparisId == order.AnaSiparisId && x.Durum == "Basarili", ct);
        var payout = await _paymentGateway.ReleaseAsync(new MarketplacePayoutCommand(
            payment.SaglayiciIslemId,
            $"settlement:{order.Id}:receipt:{receipt.Id}",
            order.TedarikciIsletmeId,
            releaseAmount,
            settlement.ParaBirimi), ct);
        if (!payout.Succeeded)
        {
            receipt.HakEdisAktarimHatasi = payout.Error;
            settlement.Durum = "AktarimBasarisiz";
            settlement.UpdatedAt = DateTime.UtcNow;
            return;
        }

        receipt.SerbestBirakilanNetTutar = releaseAmount;
        receipt.HakEdisAktarimReferansi = payout.ProviderTransactionId;
        settlement.OdenenTutar = Money(settlement.OdenenTutar + releaseAmount);
        settlement.AktarimReferansi = payout.ProviderTransactionId;
        settlement.Durum = settlement.OdenenTutar >= settlement.NetTutar ? "SerbestBirakildi" : "KismenSerbest";
        settlement.TamamlandiAt = settlement.OdenenTutar >= settlement.NetTutar ? DateTime.UtcNow : null;
        settlement.UpdatedAt = DateTime.UtcNow;
        AddLedgerEntry(db, order.Id, "TedarikciOdeme", "Borc", releaseAmount, settlement.ParaBirimi,
            $"Kabul #{receipt.Id} hakedişi");
    }

    private static void ResetInvoiceTotals(Fatura invoice)
    {
        invoice.AraToplam = 0m;
        invoice.KdvToplam = 0m;
        invoice.GenelToplam = 0m;
        invoice.GenelToplamTry = 0m;
        invoice.OdenenTutar = 0m;
    }

    private static void IncreaseInvoiceTotals(
        Fatura invoice,
        decimal net,
        decimal vat,
        decimal gross,
        bool paid,
        DateTime now)
    {
        invoice.AraToplam = Money(invoice.AraToplam + net);
        invoice.KdvToplam = Money(invoice.KdvToplam + vat);
        invoice.GenelToplam = Money(invoice.GenelToplam + gross);
        invoice.GenelToplamTry = invoice.GenelToplam;
        if (paid)
            invoice.OdenenTutar = invoice.GenelToplam;
        invoice.UpdatedAt = now;
    }

    private static string NormalizeQrCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim();
        const string prefix = "systemcel:sevkiyat:";
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[prefix.Length..];
        else if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            var queryValue = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .FirstOrDefault(part => part.Length == 2 && part[0].Equals("qr", StringComparison.OrdinalIgnoreCase));
            if (queryValue is not null)
                normalized = Uri.UnescapeDataString(queryValue[1]);
        }
        if (!normalized.StartsWith("scq1_", StringComparison.Ordinal) || normalized.Length != 53)
            throw new ArgumentException("Geçersiz sevkiyat QR kodu.");
        return normalized;
    }

    private static string HashQrCode(string rawCode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode))).ToLowerInvariant();

    private async Task<string> RequireReceiptActorAsync(
        CashTrackerDbContext db,
        int businessId,
        int? branchId,
        int? warehouseId,
        CancellationToken ct)
    {
        if (branchId is not null && !await db.Subeler.AnyAsync(x => x.Id == branchId && x.IsletmeId == businessId && x.Aktif, ct))
            throw new ArgumentException("Seçilen şube bu işletmeye ait değil.");
        if (warehouseId is not null)
        {
            var warehouse = await db.StokDepolari.SingleOrDefaultAsync(
                x => x.Id == warehouseId && x.IsletmeId == businessId && x.Aktif, ct)
                ?? throw new ArgumentException("Seçilen depo bu işletmeye ait değil.");
            if (branchId is not null && warehouse.SubeId != branchId)
                throw new ArgumentException("Seçilen depo ve şube birbiriyle eşleşmiyor.");
        }

        var identity = _currentUserContext.GetCurrentUser();
        if (identity is null)
            throw new UnauthorizedAccessException("Mal kabul için oturum açmalısınız.");
        var actor = await db.Kullanicilar.SingleOrDefaultAsync(x => x.AuthProviderUserId == identity.ProviderUserId, ct)
            ?? throw new UnauthorizedAccessException("Mal kabul için kullanıcı kaydı bulunamadı.");
        var membership = await db.IsletmeUyelikleri
            .Where(x => x.IsletmeId == businessId && x.KullaniciId == actor.Id && x.Durum == "Aktif")
            .Select(x => new { x.Rol, x.SubeId, x.DepoId })
            .SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Bu işletmede mal kabul yetkiniz bulunmuyor.");
        if (membership.Rol is not ("isletme_sahibi" or "yonetici" or "depo_sorumlusu" or "mal_kabul_onaylayicisi"))
            throw new UnauthorizedAccessException("Bu işlem için depo veya mal kabul yetkisi gerekir.");
        if (membership.Rol is "depo_sorumlusu" or "mal_kabul_onaylayicisi")
        {
            if (membership.SubeId is not null && branchId != membership.SubeId)
                throw new UnauthorizedAccessException("Bu şube için mal kabul yetkiniz bulunmuyor.");
            if (membership.DepoId is not null && warehouseId != membership.DepoId)
                throw new UnauthorizedAccessException("Bu depo için mal kabul yetkiniz bulunmuyor.");
        }
        return identity.ProviderUserId.Trim();
    }

    private static string Truncate(string? value, int maximumLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } utc => utc,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        { } unspecified => DateTime.SpecifyKind(unspecified, DateTimeKind.Local).ToUniversalTime()
    };

    private static async Task CreateAccountingRecordsAsync(
        CashTrackerDbContext db,
        TedarikciSiparis order,
        bool paid,
        CancellationToken ct)
    {
        if (await db.TedarikciFaturaEslesmeleri.AnyAsync(x => x.TedarikciSiparisId == order.Id, ct))
            return;
        var buyer = await db.Isletmeler.SingleAsync(x => x.Id == order.AliciIsletmeId, ct);
        var supplier = await db.TedarikciProfilleri.SingleAsync(x => x.Id == order.TedarikciProfilId, ct);
        var lines = await db.TedarikciSiparisKalemleri.Where(x => x.TedarikciSiparisId == order.Id).ToListAsync(ct);
        var marketplaceProducts = await db.TedarikciUrunleri
            .Where(x => lines.Select(y => y.TedarikciUrunId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var buyerCari = await EnsureCariAsync(db, order.AliciIsletmeId, order.TedarikciIsletmeId, supplier.Unvan, supplier.VergiNo, supplier.Adres, ct);
        var supplierCari = await EnsureCariAsync(db, order.TedarikciIsletmeId, order.AliciIsletmeId, buyer.Ad, string.Empty, buyer.Konum, ct);
        var now = DateTime.Now;
        var buyerInvoice = CreateInvoice(order, order.AliciIsletmeId, buyerCari.Id, "Alis", $"PZ-ALI-{order.SiparisNo}", now, paid);
        var supplierInvoice = CreateInvoice(order, order.TedarikciIsletmeId, supplierCari.Id, "Satis", $"PZ-SAT-{order.SiparisNo}", now, paid);
        db.Faturalar.AddRange(buyerInvoice, supplierInvoice);
        await db.SaveChangesAsync(ct);

        foreach (var line in lines)
        {
            var catalogProduct = marketplaceProducts[line.TedarikciUrunId];
            var buyerProduct = await db.UrunHizmetleri.SingleOrDefaultAsync(x =>
                x.IsletmeId == order.AliciIsletmeId && x.Barkod == line.Sku, ct);
            if (buyerProduct is null)
            {
                buyerProduct = new UrunHizmet
                {
                    IsletmeId = order.AliciIsletmeId,
                    Tip = "Urun",
                    Ad = line.Ad,
                    Barkod = line.Sku,
                    Birim = line.Birim,
                    KdvOrani = line.KdvOrani,
                    AlisFiyati = line.BirimFiyat,
                    SatisFiyati = line.BirimFiyat,
                    ParaBirimi = order.ParaBirimi,
                    Aktif = true
                };
                db.UrunHizmetleri.Add(buyerProduct);
                await db.SaveChangesAsync(ct);
            }

            db.FaturaSatirlari.Add(CreateInvoiceLine(order.AliciIsletmeId, buyerInvoice.Id, buyerProduct.Id, line, true));
            db.FaturaSatirlari.Add(CreateInvoiceLine(order.TedarikciIsletmeId, supplierInvoice.Id, catalogProduct.KaynakUrunHizmetId, line, false));
            db.StokHareketleri.Add(new StokHareket
            {
                IsletmeId = order.AliciIsletmeId,
                TedarikciSiparisId = order.Id,
                UrunHizmetId = buyerProduct.Id,
                Tarih = now,
                Miktar = line.Miktar,
                BirimMaliyet = line.BirimFiyat,
                MaliyetParaBirimi = order.ParaBirimi,
                MaliyetKurSnapshot = 1m,
                BirimMaliyetTry = line.BirimFiyat,
                HareketTipi = "Giris",
                Kaynak = "Pazaryeri",
                Aciklama = $"Sipariş {order.SiparisNo}"
            });
        }

        if (paid)
        {
            AddPaidAccountingEntries(db, buyerInvoice, buyerCari.Id, "Borc", "Odeme", order, now);
            AddPaidAccountingEntries(db, supplierInvoice, supplierCari.Id, "Alacak", "Tahsilat", order, now);
        }
        else
        {
            AddInvoiceCariEntry(db, buyerInvoice, buyerCari.Id, "Borc", order, now);
            AddInvoiceCariEntry(db, supplierInvoice, supplierCari.Id, "Alacak", order, now);
        }
        db.TedarikciFaturaEslesmeleri.Add(new TedarikciFaturaEslesmesi
        {
            TedarikciSiparisId = order.Id,
            AliciFaturaId = buyerInvoice.Id,
            SaticiFaturaId = supplierInvoice.Id,
            TedarikciBelgeNo = supplierInvoice.YerelFaturaNo
        });
    }

    private static async Task<CariKart> EnsureCariAsync(
        CashTrackerDbContext db,
        int ownerBusinessId,
        int counterpartyBusinessId,
        string title,
        string taxNumber,
        string address,
        CancellationToken ct)
    {
        var marker = $"Pazaryeri işletme #{counterpartyBusinessId}";
        var existing = await db.CariKartlari.SingleOrDefaultAsync(x =>
            x.IsletmeId == ownerBusinessId && x.Adres == marker, ct);
        if (existing is not null)
            return existing;
        var cari = new CariKart
        {
            IsletmeId = ownerBusinessId,
            Tip = "HerIkisi",
            Unvan = title,
            VergiNoTc = taxNumber,
            Adres = marker,
            VergiDairesi = address,
            Aktif = true
        };
        db.CariKartlari.Add(cari);
        await db.SaveChangesAsync(ct);
        return cari;
    }

    private static Fatura CreateInvoice(
        TedarikciSiparis order,
        int businessId,
        int cariId,
        string type,
        string invoiceNumber,
        DateTime now,
        bool paid) => new()
    {
        IsletmeId = businessId,
        CariKartId = cariId,
        Tarih = now,
        FaturaTipi = type,
        Durum = paid ? FaturaDurum.Odendi : FaturaDurum.Kesildi,
        YerelFaturaNo = invoiceNumber,
        AraToplam = order.AraToplam,
        KdvToplam = order.KdvToplam,
        GenelToplam = order.GenelToplam,
        ParaBirimi = order.ParaBirimi,
        KurSnapshot = 1m,
        GenelToplamTry = order.GenelToplam,
        OdenenTutar = paid ? order.GenelToplam : 0m,
        OdemeYontemi = paid ? "OnlineOdeme" : "Vadeli",
        VadeTarihi = paid ? null : order.HakEdisTarihi,
        Aciklama = $"Pazaryeri siparişi {order.SiparisNo}",
        KesildiAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static FaturaSatir CreateInvoiceLine(
        int businessId,
        int invoiceId,
        int? productId,
        TedarikciSiparisKalemi line,
        bool affectsStock,
        decimal? acceptedQuantity = null)
    {
        var quantity = acceptedQuantity ?? line.Miktar;
        var net = Money(line.BirimFiyat * quantity);
        var vat = Money(net * line.KdvOrani / 100m);
        return new FaturaSatir
        {
            IsletmeId = businessId,
            FaturaId = invoiceId,
            UrunHizmetId = productId,
            Aciklama = line.Ad,
            Birim = line.Birim,
            Miktar = quantity,
            BirimFiyat = line.BirimFiyat,
            KdvOrani = line.KdvOrani,
            KdvTutar = vat,
            SatirNetTutar = net,
            SatirToplam = net + vat,
            StokEtkilesin = affectsStock
        };
    }

    private static void AddPaidAccountingEntriesForAmount(
        CashTrackerDbContext db,
        Fatura invoice,
        int cariId,
        string movementType,
        string paymentType,
        TedarikciSiparis order,
        decimal amount,
        DateTime now,
        int receiptId)
    {
        db.CariHareketleri.Add(new CariHareket
        {
            IsletmeId = invoice.IsletmeId,
            TedarikciSiparisId = order.Id,
            TedarikciMalKabulId = receiptId,
            CariKartId = cariId,
            Tarih = now,
            HareketTipi = movementType,
            Tutar = amount,
            ParaBirimi = order.ParaBirimi,
            KurSnapshot = 1m,
            TryKarsiligi = amount,
            Kaynak = "PazaryeriMalKabul",
            Aciklama = $"Sipariş {order.SiparisNo}, kabul #{receiptId}"
        });
        db.TahsilatOdemeleri.Add(new TahsilatOdeme
        {
            IsletmeId = invoice.IsletmeId,
            TedarikciSiparisId = order.Id,
            TedarikciMalKabulId = receiptId,
            FaturaId = invoice.Id,
            CariKartId = cariId,
            Tarih = now,
            Tip = paymentType,
            Tutar = amount,
            ParaBirimi = order.ParaBirimi,
            KurSnapshot = 1m,
            TryKarsiligi = amount,
            OdemeYontemi = "OnlineOdeme",
            Aciklama = $"Pazaryeri siparişi {order.SiparisNo}, kabul #{receiptId}"
        });
    }

    private static void AddInvoiceCariEntryForAmount(
        CashTrackerDbContext db,
        Fatura invoice,
        int cariId,
        string movementType,
        TedarikciSiparis order,
        decimal amount,
        DateTime now,
        int receiptId)
    {
        db.CariHareketleri.Add(new CariHareket
        {
            IsletmeId = invoice.IsletmeId,
            TedarikciSiparisId = order.Id,
            TedarikciMalKabulId = receiptId,
            CariKartId = cariId,
            Tarih = now,
            HareketTipi = movementType,
            Tutar = amount,
            ParaBirimi = order.ParaBirimi,
            KurSnapshot = 1m,
            TryKarsiligi = amount,
            Kaynak = "PazaryeriMalKabul",
            Aciklama = $"Sipariş {order.SiparisNo}, kabul #{receiptId}"
        });
    }

    private static void AddPaidAccountingEntries(
        CashTrackerDbContext db,
        Fatura invoice,
        int cariId,
        string movementType,
        string paymentType,
        TedarikciSiparis order,
        DateTime now)
    {
        var movement = new CariHareket
        {
            IsletmeId = invoice.IsletmeId,
            TedarikciSiparisId = order.Id,
            CariKartId = cariId,
            Tarih = now,
            HareketTipi = movementType,
            Tutar = order.GenelToplam,
            ParaBirimi = order.ParaBirimi,
            KurSnapshot = 1m,
            TryKarsiligi = order.GenelToplam,
            Kaynak = "Pazaryeri",
            Aciklama = $"Sipariş {order.SiparisNo}"
        };
        db.CariHareketleri.Add(movement);
        db.TahsilatOdemeleri.Add(new TahsilatOdeme
        {
            IsletmeId = invoice.IsletmeId,
            TedarikciSiparisId = order.Id,
            FaturaId = invoice.Id,
            CariKartId = cariId,
            Tarih = now,
            Tip = paymentType,
            Tutar = order.GenelToplam,
            ParaBirimi = order.ParaBirimi,
            KurSnapshot = 1m,
            TryKarsiligi = order.GenelToplam,
            OdemeYontemi = "OnlineOdeme",
            Aciklama = $"Pazaryeri siparişi {order.SiparisNo}"
        });
    }

    private static void AddInvoiceCariEntry(
        CashTrackerDbContext db,
        Fatura invoice,
        int cariId,
        string movementType,
        TedarikciSiparis order,
        DateTime now)
    {
        db.CariHareketleri.Add(new CariHareket
        {
            IsletmeId = invoice.IsletmeId,
            TedarikciSiparisId = order.Id,
            CariKartId = cariId,
            Tarih = now,
            HareketTipi = movementType,
            Tutar = order.GenelToplam,
            ParaBirimi = order.ParaBirimi,
            KurSnapshot = 1m,
            TryKarsiligi = order.GenelToplam,
            Kaynak = "Pazaryeri",
            Aciklama = $"Sipariş {order.SiparisNo}"
        });
    }

    private static void EnsureStateTransitionAllowed(TedarikciSiparis order, int activeBusinessId, string target)
    {
        var supplierTransitions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PazaryeriSiparisDurumlari.SiparisVerildi] = PazaryeriSiparisDurumlari.TedarikciOnayladi,
            [PazaryeriSiparisDurumlari.Odendi] = PazaryeriSiparisDurumlari.TedarikciOnayladi,
            [PazaryeriSiparisDurumlari.TedarikciOnayladi] = PazaryeriSiparisDurumlari.Hazirlaniyor,
            [PazaryeriSiparisDurumlari.Hazirlaniyor] = PazaryeriSiparisDurumlari.SevkeHazir,
            [PazaryeriSiparisDurumlari.SevkeHazir] = PazaryeriSiparisDurumlari.SevkEdildi
        };
        if (activeBusinessId == order.TedarikciIsletmeId &&
            order.Durum == PazaryeriSiparisDurumlari.Hazirlaniyor && target == PazaryeriSiparisDurumlari.SevkEdildi)
            return;
        if (activeBusinessId == order.TedarikciIsletmeId &&
            supplierTransitions.TryGetValue(order.Durum, out var allowed) && allowed == target)
            return;
        if (activeBusinessId == order.AliciIsletmeId &&
            order.Durum == PazaryeriSiparisDurumlari.SevkEdildi && target == PazaryeriSiparisDurumlari.TeslimEdildi)
            return;
        throw new InvalidOperationException("Bu sipariş için seçilen durum değişikliği yapılamaz.");
    }

    private static void AddStateHistory(
        CashTrackerDbContext db,
        TedarikciSiparis order,
        string newState,
        int actorBusinessId,
        string? description)
    {
        db.TedarikciSiparisDurumKayitlari.Add(new TedarikciSiparisDurumKaydi
        {
            TedarikciSiparisId = order.Id,
            OncekiDurum = order.Durum,
            YeniDurum = newState,
            IslemYapanIsletmeId = actorBusinessId,
            Aciklama = (description ?? string.Empty).Trim()
        });
        order.Durum = newState;
        order.UpdatedAt = DateTime.UtcNow;
    }

    private static void AddLedgerEntry(
        CashTrackerDbContext db,
        int orderId,
        string account,
        string direction,
        decimal amount,
        string currency,
        string description)
    {
        if (amount == 0m)
            return;
        db.PazaryeriDefterKayitlari.Add(new PazaryeriDefterKaydi
        {
            TedarikciSiparisId = orderId,
            Hesap = account,
            Yon = direction,
            Tutar = amount,
            ParaBirimi = currency,
            Aciklama = description
        });
    }

    private static async Task SyncMasterStateAsync(CashTrackerDbContext db, int masterOrderId, CancellationToken ct)
    {
        var master = await db.PazaryeriAnaSiparisleri.SingleAsync(x => x.Id == masterOrderId, ct);
        // Load entities instead of projecting the database values so state changes that are
        // already tracked in this transaction are reflected before SaveChanges is called.
        var orders = await db.TedarikciSiparisleri.Where(x => x.AnaSiparisId == masterOrderId).ToListAsync(ct);
        var states = orders.Select(x => x.Durum).ToList();
        var cancelled = states.Count(x => x is PazaryeriSiparisDurumlari.IptalEdildi or PazaryeriSiparisDurumlari.IadeEdildi);
        if (cancelled == states.Count)
            master.Durum = states.Any(x => x == PazaryeriSiparisDurumlari.IadeEdildi)
                ? PazaryeriSiparisDurumlari.IadeEdildi
                : PazaryeriSiparisDurumlari.IptalEdildi;
        else if (cancelled > 0)
            master.Durum = states.Any(x => x == PazaryeriSiparisDurumlari.IadeEdildi)
                ? PazaryeriSiparisDurumlari.KismiIade
                : PazaryeriSiparisDurumlari.KismiIptal;
        else if (states.All(x => x == PazaryeriSiparisDurumlari.Tamamlandi))
            master.Durum = PazaryeriSiparisDurumlari.Tamamlandi;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.Itirazli))
            master.Durum = PazaryeriSiparisDurumlari.Itirazli;
        else if (states.All(x => x is PazaryeriSiparisDurumlari.HakEdisBekliyor or PazaryeriSiparisDurumlari.Tamamlandi))
            master.Durum = PazaryeriSiparisDurumlari.HakEdisBekliyor;
        else if (states.All(x => x is PazaryeriSiparisDurumlari.CariOdemeBekliyor or PazaryeriSiparisDurumlari.Tamamlandi))
            master.Durum = PazaryeriSiparisDurumlari.CariOdemeBekliyor;
        else if (states.All(x => x == PazaryeriSiparisDurumlari.TeslimEdildi))
            master.Durum = PazaryeriSiparisDurumlari.TeslimEdildi;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.KismenKabul))
            master.Durum = PazaryeriSiparisDurumlari.KismenKabul;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.MalKabulBekliyor))
            master.Durum = PazaryeriSiparisDurumlari.MalKabulBekliyor;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.SevkEdildi))
            master.Durum = PazaryeriSiparisDurumlari.SevkEdildi;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.KismenSevkEdildi))
            master.Durum = PazaryeriSiparisDurumlari.KismenSevkEdildi;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.Hazirlaniyor))
            master.Durum = PazaryeriSiparisDurumlari.Hazirlaniyor;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.SevkeHazir))
            master.Durum = PazaryeriSiparisDurumlari.SevkeHazir;
        else if (states.All(x => x == PazaryeriSiparisDurumlari.TedarikciOnayladi))
            master.Durum = PazaryeriSiparisDurumlari.TedarikciOnayladi;
        else if (states.All(x => x == PazaryeriSiparisDurumlari.SiparisVerildi))
            master.Durum = PazaryeriSiparisDurumlari.SiparisVerildi;
        master.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task<DateTime> ResolveSettlementDateAsync(CashTrackerDbContext db, int profileId, CancellationToken ct)
    {
        var days = await db.TedarikciProfilleri.Where(x => x.Id == profileId).Select(x => x.OdemeVadesiGun).SingleAsync(ct);
        return DateTime.UtcNow.AddDays(days);
    }

    private static async Task AddRejectedReceiptComplaintAsync(
        CashTrackerDbContext db,
        TedarikciSiparis order,
        TedarikciSiparisKalemi orderLine,
        TedarikciMalKabul receipt,
        CancellationToken ct)
    {
        if (await db.TedarikciSiparisSikayetleri.AnyAsync(x => x.TedarikciSiparisId == order.Id, ct))
            return;

        var category = CategorizeReceiptRejection(receipt.RedNedeni);
        var requestedResolution = category == TedarikciSikayetKategorileri.Eksik ? "EksigiTamamla" : "Degisim";
        var detail = $"{orderLine.Ad}: {receipt.ReddedilenMiktar} {orderLine.Birim} kabul edilmedi. {receipt.RedNedeni}";
        if (!string.IsNullOrWhiteSpace(receipt.IkinciRedNedeni))
            detail = $"{detail}; {receipt.IkinciRedNedeni}";
        if (!string.IsNullOrWhiteSpace(receipt.Not))
            detail = $"{detail} - {receipt.Not}";

        db.TedarikciSiparisSikayetleri.Add(new TedarikciSiparisSikayeti
        {
            TedarikciSiparisId = order.Id,
            AliciIsletmeId = order.AliciIsletmeId,
            TedarikciIsletmeId = order.TedarikciIsletmeId,
            Kategori = category,
            Aciklama = detail,
            Talep = requestedResolution,
            Durum = TedarikciSikayetDurumlari.Acik
        });
    }

    private static string CategorizeReceiptRejection(string reason)
    {
        var normalized = reason.ToLowerInvariant();
        if (normalized.StartsWith("diğer:", StringComparison.Ordinal)) return TedarikciSikayetKategorileri.Diger;
        if (normalized.Contains("eksik", StringComparison.Ordinal)) return TedarikciSikayetKategorileri.Eksik;
        if (normalized.Contains("yanlış", StringComparison.Ordinal) || normalized.Contains("yanlis", StringComparison.Ordinal))
            return TedarikciSikayetKategorileri.YanlisUrun;
        if (normalized.Contains("hasar", StringComparison.Ordinal)) return TedarikciSikayetKategorileri.Hasarli;
        if (normalized.Contains("sıcaklık", StringComparison.Ordinal) || normalized.Contains("sicaklik", StringComparison.Ordinal))
            return TedarikciSikayetKategorileri.Sicaklik;
        if (normalized.Contains("belge", StringComparison.Ordinal)) return TedarikciSikayetKategorileri.BelgeUyusmazligi;
        if (new[] { "kalite", "lot", "seri", "son kullanma" }.Any(x => normalized.Contains(x, StringComparison.Ordinal)))
            return TedarikciSikayetKategorileri.Kalite;
        return TedarikciSikayetKategorileri.Diger;
    }

    private static void ValidateComplaint(TedarikciSikayetOlusturRequest request)
    {
        var categories = new HashSet<string>(StringComparer.Ordinal)
        {
            TedarikciSikayetKategorileri.Eksik,
            TedarikciSikayetKategorileri.Hasarli,
            TedarikciSikayetKategorileri.YanlisUrun,
            TedarikciSikayetKategorileri.Kalite,
            TedarikciSikayetKategorileri.Diger,
            TedarikciSikayetKategorileri.TeslimEdilmedi,
            TedarikciSikayetKategorileri.Sicaklik,
            TedarikciSikayetKategorileri.BelgeUyusmazligi
        };
        var requests = new HashSet<string>(StringComparer.Ordinal) { "Degisim", "EksigiTamamla", "IadeTalebi", "Diger" };
        if (!categories.Contains(request.Kategori?.Trim() ?? string.Empty))
            throw new ArgumentException("Sorun türünü seçin.");
        if (!requests.Contains(request.Talep?.Trim() ?? string.Empty))
            throw new ArgumentException("Beklediğiniz çözümü seçin.");
        var description = request.Aciklama?.Trim() ?? string.Empty;
        if (description.Length is < 10 or > 2000)
            throw new ArgumentException("Açıklama 10 ile 2000 karakter arasında olmalıdır.");
    }

    private static void ValidateRating(TedarikciDegerlendirmeKaydetRequest request)
    {
        var scores = new[]
        {
            request.UrunUygunluguPuani,
            request.EksiksizTeslimatPuani,
            request.HasarsizTeslimatPuani,
            request.ZamanindaTeslimatPuani
        };
        if (scores.Any(x => x is < 1 or > 5) || request.SorunCozmePuani is < 1 or > 5)
            throw new ArgumentException("Puanlar 1 ile 5 arasında olmalıdır.");
        if ((request.Yorum ?? string.Empty).Trim().Length > 1000)
            throw new ArgumentException("Yorum 1000 karakterden uzun olamaz.");
    }

    private static bool HasRequiredVerificationFields(TedarikciProfil profile) =>
        !string.IsNullOrWhiteSpace(profile.Unvan) &&
        !string.IsNullOrWhiteSpace(profile.VergiNo) &&
        !string.IsNullOrWhiteSpace(profile.Iban) &&
        !string.IsNullOrWhiteSpace(profile.Adres) &&
        !string.IsNullOrWhiteSpace(profile.YetkiliAdSoyad) &&
        !string.IsNullOrWhiteSpace(profile.PazaryeriSozlesmeVersiyonu);

    private static bool VerificationFieldsChanged(TedarikciProfil profile, TedarikciOnboardingRequest request) =>
        profile.VergiNo != request.VergiNo.Trim() ||
        profile.MersisNo != request.MersisNo.Trim() ||
        profile.KepAdresi != request.KepAdresi.Trim() ||
        profile.Iban != NormalizeIban(request.Iban) ||
        profile.Adres != request.Adres.Trim() ||
        profile.YetkiliAdSoyad != request.YetkiliAdSoyad.Trim() ||
        profile.VergiDurumu != request.VergiDurumu.Trim() ||
        profile.PazaryeriSozlesmeVersiyonu != request.PazaryeriSozlesmeVersiyonu.Trim() ||
        profile.TevkifatMuaf != request.TevkifatMuaf;

    private static void ValidateSupplierProfile(TedarikciOnboardingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Unvan) || string.IsNullOrWhiteSpace(request.Kategoriler))
            throw new ArgumentException("Unvan ve kategori gereklidir.");
        if (request.Yayinda && (string.IsNullOrWhiteSpace(request.VergiNo) || string.IsNullOrWhiteSpace(request.Iban) ||
                string.IsNullOrWhiteSpace(request.Adres) || string.IsNullOrWhiteSpace(request.YetkiliAdSoyad) ||
                string.IsNullOrWhiteSpace(request.PazaryeriSozlesmeVersiyonu)))
            throw new ArgumentException("Yayın başvurusu için şirket, vergi, IBAN, adres, yetkili ve sözleşme bilgilerini tamamlayın.");
        var iban = NormalizeIban(request.Iban);
        if (!string.IsNullOrWhiteSpace(iban) && (iban.Length != 26 || !iban.StartsWith("TR", StringComparison.Ordinal)))
            throw new ArgumentException("IBAN bilgisini kontrol edin.");
    }

    private static void ValidateProduct(TedarikciUrunKaydetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || string.IsNullOrWhiteSpace(request.Ad) || string.IsNullOrWhiteSpace(request.Kategori))
            throw new ArgumentException("Stok kodu, ürün adı ve kategori gereklidir.");
        if (request.BirimFiyat <= 0m || request.StokMiktari < 0m || request.MinimumSiparisMiktari <= 0m)
            throw new ArgumentException("Fiyat, stok ve minimum sipariş bilgilerini kontrol edin.");
        if (request.KdvOrani is < 0m or > 100m)
            throw new ArgumentException("KDV oranını kontrol edin.");
        if (request.TahminiTeslimatGun is < 0 or > 365)
            throw new ArgumentException("Teslimat süresini kontrol edin.");
        if (request.ParaBirimi.Trim().Length != 3)
            throw new ArgumentException("Para birimini kontrol edin.");
    }

    private static void ValidateCreateOrder(PazaryeriSiparisOlusturRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TeslimatAdresi))
            throw new ArgumentException("Teslimat adresi gereklidir.");
        ValidateIdempotencyKey(request.IdempotencyKey);
        if (request.Kalemler is null || request.Kalemler.Count == 0 || request.Kalemler.Any(x => x.UrunId <= 0 || x.Miktar <= 0m))
            throw new ArgumentException("Sepete en az bir geçerli ürün ekleyin.");
    }

    private static void ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length is < 8 or > 100)
            throw new ArgumentException("İşlem anahtarı 8 ile 100 karakter arasında olmalıdır.");
    }

    private static string NormalizeIban(string value) =>
        new string((value ?? string.Empty).Where(x => !char.IsWhiteSpace(x)).ToArray()).ToUpperInvariant();

    private static decimal Money(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string CreateOrderNumber(DateTime now) =>
        $"PZ-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();

    private static string ToAlphaSequence(int value)
    {
        var result = string.Empty;
        while (value > 0)
        {
            value--;
            result = (char)('A' + value % 26) + result;
            value /= 26;
        }
        return result;
    }
}
