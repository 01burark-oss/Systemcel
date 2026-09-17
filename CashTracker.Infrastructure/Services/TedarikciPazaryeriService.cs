using System.Data;
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

    public TedarikciPazaryeriService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        ISystemcelYonetimService yonetimService,
        IMarketplacePaymentGateway paymentGateway,
        PazaryeriOptions options)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _yonetimService = yonetimService;
        _paymentGateway = paymentGateway;
        _options = options;
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
        var master = new PazaryeriAnaSiparis
        {
            AliciIsletmeId = buyerId,
            SiparisNo = CreateOrderNumber(now),
            OlusturmaAnahtari = creationKey,
            TeslimatAdresi = request.TeslimatAdresi.Trim(),
            ParaBirimi = products[0].ParaBirimi,
            Durum = PazaryeriSiparisDurumlari.OdemeBekliyor,
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
            var supplierOrder = BuildSupplierOrder(master, profile, supplierGroup.ToList(), requestedQuantities, ++supplierIndex);
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
            product.RezerveMiktar -= line.Miktar;
            product.StokMiktari -= line.Miktar;
            product.UpdatedAt = DateTime.UtcNow;
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
        }

        AddStateHistory(db, order, request.Durum, activeBusinessId, request.Aciklama);
        if (request.Durum == PazaryeriSiparisDurumlari.TeslimEdildi)
        {
            order.TeslimEdildiAt = DateTime.UtcNow;
            order.HakEdisTarihi = await ResolveSettlementDateAsync(db, order.TedarikciProfilId, ct);
            var settlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == order.Id, ct);
            settlement.PlanlananAt = order.HakEdisTarihi.Value;
            settlement.UpdatedAt = DateTime.UtcNow;
            await CreateAccountingRecordsAsync(db, order, ct);
            AddStateHistory(db, order, PazaryeriSiparisDurumlari.HakEdisBekliyor, activeBusinessId, "Teslimat tamamlandı.");
        }

        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
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
        if (orders.Any(x => x.Durum is PazaryeriSiparisDurumlari.SevkEdildi
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
            if (payment is null)
                product.RezerveMiktar = Math.Max(0m, product.RezerveMiktar - line.Miktar);
            else
                product.StokMiktari += line.Miktar;
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
        if (order.Durum is PazaryeriSiparisDurumlari.SevkEdildi
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
            if (payment is null)
                product.RezerveMiktar = Math.Max(0m, product.RezerveMiktar - line.Miktar);
            else
                product.StokMiktari += line.Miktar;
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

        settlement.Durum = "Tamamlandi";
        settlement.OdenenTutar = settlement.NetTutar;
        settlement.AktarimReferansi = request.AktarimReferansi.Trim();
        settlement.TamamlandiAt = DateTime.UtcNow;
        settlement.UpdatedAt = DateTime.UtcNow;
        AddStateHistory(db, order, PazaryeriSiparisDurumlari.Tamamlandi, order.TedarikciIsletmeId, "Hakediş aktarıldı.");
        AddLedgerEntry(db, order.Id, "TedarikciOdeme", "Borc", settlement.NetTutar, settlement.ParaBirimi, "Tedarikçi hakedişi");
        await SyncMasterStateAsync(db, order.AnaSiparisId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private TedarikciSiparis BuildSupplierOrder(
        PazaryeriAnaSiparis master,
        TedarikciProfil profile,
        IReadOnlyList<TedarikciUrun> products,
        IReadOnlyDictionary<int, decimal> quantities,
        int sequence)
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
            Durum = PazaryeriSiparisDurumlari.OdemeBekliyor
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

    private static async Task CreateAccountingRecordsAsync(
        CashTrackerDbContext db,
        TedarikciSiparis order,
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
        var buyerInvoice = CreateInvoice(order, order.AliciIsletmeId, buyerCari.Id, "Alis", $"PZ-ALI-{order.SiparisNo}", now);
        var supplierInvoice = CreateInvoice(order, order.TedarikciIsletmeId, supplierCari.Id, "Satis", $"PZ-SAT-{order.SiparisNo}", now);
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
            if (catalogProduct.KaynakUrunHizmetId is { } sourceId)
            {
                db.StokHareketleri.Add(new StokHareket
                {
                    IsletmeId = order.TedarikciIsletmeId,
                    UrunHizmetId = sourceId,
                    Tarih = now,
                    Miktar = -line.Miktar,
                    HareketTipi = "Cikis",
                    Kaynak = "Pazaryeri",
                    Aciklama = $"Sipariş {order.SiparisNo}"
                });
            }
        }

        AddPaidAccountingEntries(db, buyerInvoice, buyerCari.Id, "Borc", "Odeme", order, now);
        AddPaidAccountingEntries(db, supplierInvoice, supplierCari.Id, "Alacak", "Tahsilat", order, now);
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
        DateTime now) => new()
    {
        IsletmeId = businessId,
        CariKartId = cariId,
        Tarih = now,
        FaturaTipi = type,
        Durum = FaturaDurum.Odendi,
        YerelFaturaNo = invoiceNumber,
        AraToplam = order.AraToplam,
        KdvToplam = order.KdvToplam,
        GenelToplam = order.GenelToplam,
        ParaBirimi = order.ParaBirimi,
        KurSnapshot = 1m,
        GenelToplamTry = order.GenelToplam,
        OdenenTutar = order.GenelToplam,
        OdemeYontemi = "OnlineOdeme",
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
        bool affectsStock) => new()
    {
        IsletmeId = businessId,
        FaturaId = invoiceId,
        UrunHizmetId = productId,
        Aciklama = line.Ad,
        Birim = line.Birim,
        Miktar = line.Miktar,
        BirimFiyat = line.BirimFiyat,
        KdvOrani = line.KdvOrani,
        KdvTutar = line.KdvTutari,
        SatirNetTutar = line.NetTutar,
        SatirToplam = line.ToplamTutar,
        StokEtkilesin = affectsStock
    };

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

    private static void EnsureStateTransitionAllowed(TedarikciSiparis order, int activeBusinessId, string target)
    {
        var supplierTransitions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PazaryeriSiparisDurumlari.Odendi] = PazaryeriSiparisDurumlari.TedarikciOnayladi,
            [PazaryeriSiparisDurumlari.TedarikciOnayladi] = PazaryeriSiparisDurumlari.Hazirlaniyor,
            [PazaryeriSiparisDurumlari.Hazirlaniyor] = PazaryeriSiparisDurumlari.SevkEdildi
        };
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
        else if (states.All(x => x == PazaryeriSiparisDurumlari.TeslimEdildi))
            master.Durum = PazaryeriSiparisDurumlari.TeslimEdildi;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.SevkEdildi))
            master.Durum = PazaryeriSiparisDurumlari.SevkEdildi;
        else if (states.Any(x => x == PazaryeriSiparisDurumlari.Hazirlaniyor))
            master.Durum = PazaryeriSiparisDurumlari.Hazirlaniyor;
        else if (states.All(x => x == PazaryeriSiparisDurumlari.TedarikciOnayladi))
            master.Durum = PazaryeriSiparisDurumlari.TedarikciOnayladi;
        master.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task<DateTime> ResolveSettlementDateAsync(CashTrackerDbContext db, int profileId, CancellationToken ct)
    {
        var days = await db.TedarikciProfilleri.Where(x => x.Id == profileId).Select(x => x.OdemeVadesiGun).SingleAsync(ct);
        return DateTime.UtcNow.AddDays(days);
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
