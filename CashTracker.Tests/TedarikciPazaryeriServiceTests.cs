using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class TedarikciPazaryeriServiceTests
{
    [Fact]
    public void PazaryeriOptions_PilotAllowlistFailsClosedForUnlistedBusiness()
    {
        var options = new PazaryeriOptions { Aktif = true, PilotIsletmeIdleri = new HashSet<int> { 7, 9 } };
        Assert.True(options.IsBusinessAllowed(7));
        Assert.False(options.IsBusinessAllowed(8));
        Assert.False(new PazaryeriOptions { Aktif = false }.IsBusinessAllowed(7));
    }

    [Fact]
    public async Task CreateOrder_SplitsCartAcrossSuppliersAndIsIdempotent()
    {
        await using var f = await Fixture.CreateAsync();
        var service = f.Service(1);
        var result = await service.CreateOrderAsync(new PazaryeriSiparisOlusturRequest("Teslimat", "create-1", new[]
        {
            new PazaryeriSepetKalemiRequest(f.ProductA, 2), new PazaryeriSepetKalemiRequest(f.ProductB, 1)
        }));
        var repeat = await service.CreateOrderAsync(new PazaryeriSiparisOlusturRequest("Teslimat", "create-1", new[]
        {
            new PazaryeriSepetKalemiRequest(f.ProductA, 2), new PazaryeriSepetKalemiRequest(f.ProductB, 1)
        }));
        Assert.Equal(result.Id, repeat.Id); Assert.True(repeat.TekrarKullanildi);
        await using var db = f.Db();
        var master = await db.PazaryeriAnaSiparisleri.SingleAsync();
        Assert.Equal(2, await db.TedarikciSiparisleri.CountAsync());
        Assert.Equal(70m, master.AraToplam); Assert.Equal(14m, master.KdvToplam); Assert.Equal(84m, master.GenelToplam);
        Assert.Equal(2, await db.TedarikciSiparisKalemleri.CountAsync());
        Assert.Equal(2m, (await db.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA)).RezerveMiktar);
    }

    [Fact]
    public async Task PayOrder_IsTenantScopedAndIdempotentWithSettlementsAndLedger()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "pay-create", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1), new PazaryeriSepetKalemiRequest(f.ProductB, 1) }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => f.Service(3).PayOrderAsync(order.Id, new PazaryeriOdemeRequest("pay-other")));
        var paid = await f.Service(1).PayOrderAsync(order.Id, new PazaryeriOdemeRequest("payment-1"));
        var repeat = await f.Service(1).PayOrderAsync(order.Id, new PazaryeriOdemeRequest("payment-1"));
        Assert.Equal(order.Id, paid.Id); Assert.True(repeat.TekrarKullanildi);
        await using var db = f.Db();
        Assert.Equal(2, await db.TedarikciHakEdisleri.CountAsync());
        Assert.Equal(2, await db.PazaryeriOdemeDagitimlari.CountAsync());
        Assert.Equal(10, await db.PazaryeriDefterKayitlari.CountAsync());
        Assert.Equal(10m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(10m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductB).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(1m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.RezerveMiktar).SingleAsync());
        Assert.Equal(1m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductB).Select(x => x.RezerveMiktar).SingleAsync());
        var firstOrder = await db.TedarikciSiparisleri.SingleAsync(x => x.TedarikciIsletmeId == 2);
        Assert.Equal(2m, firstOrder.KomisyonTutari);
        Assert.Equal(0.40m, firstOrder.KomisyonKdvTutari);
        Assert.Equal(0.20m, firstOrder.TevkifatTutari);
        Assert.Equal(21.40m, firstOrder.TedarikciHakEdisi);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStockLeavesNoRecords()
    {
        await using var f = await Fixture.CreateAsync(stockA: 0m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "stock-fail", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) })));
        await using var db = f.Db(); Assert.Empty(await db.PazaryeriAnaSiparisleri.ToListAsync()); Assert.Equal(0m, (await db.TedarikciUrunleri.ToListAsync()).Sum(x => x.RezerveMiktar));
    }

    [Fact]
    public async Task Delivered_CreatesAccountingOnce_AndCancelAfterPaymentRefundsStock()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "accounting", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }));
        await f.Service(1).PayOrderAsync(order.Id, new PazaryeriOdemeRequest("payment-accounting"));
        await using (var db = f.Db()) { var id = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync(); await f.Service(2).UpdateSupplierOrderStateAsync(id, new TedarikciSiparisDurumRequest(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null)); await f.Service(2).UpdateSupplierOrderStateAsync(id, new TedarikciSiparisDurumRequest(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null)); await f.Service(2).UpdateSupplierOrderStateAsync(id, new TedarikciSiparisDurumRequest(PazaryeriSiparisDurumlari.SevkEdildi, "Kargo", "TRK", null)); await f.Service(1).UpdateSupplierOrderStateAsync(id, new TedarikciSiparisDurumRequest(PazaryeriSiparisDurumlari.TeslimEdildi, null, null, null)); }
        await using (var db = f.Db()) { Assert.Equal(1, await db.TedarikciFaturaEslesmeleri.CountAsync()); }
        await using (var db = f.Db()) { Assert.Equal(2, await db.Faturalar.CountAsync()); Assert.Equal(1, await db.StokHareketleri.CountAsync(x => x.IsletmeId == 1)); Assert.Equal(PazaryeriSiparisDurumlari.Tamamlandi, (await db.TedarikciSiparisleri.SingleAsync()).Durum); Assert.Equal("Tamamlandi", (await db.TedarikciHakEdisleri.SingleAsync()).Durum); }
    }

    [Fact]
    public async Task CancelAfterPayment_RefundsAndRestoresStock()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "cancel-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }));
        await f.Service(1).PayOrderAsync(order.Id, new PazaryeriOdemeRequest("cancel-payment"));
        await f.Service(1).CancelOrderAsync(order.Id, new PazaryeriIptalRequest("İade"));
        await using var db = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.IadeEdildi, (await db.PazaryeriAnaSiparisleri.SingleAsync()).Durum);
        Assert.Equal(10m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.StokMiktari).SingleAsync());
    }

    [Fact]
    public async Task CancelOneSupplierOrder_RefundsOnlyThatAllocationAndKeepsOtherOrderActive()
    {
        await using var f = await Fixture.CreateAsync();
        var service = f.Service(1);
        var order = await service.CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "partial-create", new[]
        {
            new PazaryeriSepetKalemiRequest(f.ProductA, 1),
            new PazaryeriSepetKalemiRequest(f.ProductB, 1)
        }));
        await service.PayOrderAsync(order.Id, new PazaryeriOdemeRequest("partial-payment"));

        int cancelledOrderId;
        await using (var db = f.Db())
            cancelledOrderId = await db.TedarikciSiparisleri
                .Where(x => x.TedarikciIsletmeId == 2)
                .Select(x => x.Id)
                .SingleAsync();

        await service.CancelSupplierOrderAsync(cancelledOrderId, new PazaryeriIptalRequest("Tedarikçi iptali"));

        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.KismiIade, (await assertDb.PazaryeriAnaSiparisleri.SingleAsync()).Durum);
        Assert.Equal(PazaryeriSiparisDurumlari.IadeEdildi,
            await assertDb.TedarikciSiparisleri.Where(x => x.Id == cancelledOrderId).Select(x => x.Durum).SingleAsync());
        Assert.Equal(PazaryeriSiparisDurumlari.Odendi,
            await assertDb.TedarikciSiparisleri.Where(x => x.Id != cancelledOrderId).Select(x => x.Durum).SingleAsync());
        Assert.Equal(10m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(10m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductB).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(24m, await assertDb.PazaryeriOdemeDagitimlari
            .Where(x => x.TedarikciSiparisId == cancelledOrderId).Select(x => x.IadeTutari).SingleAsync());
        Assert.Equal(0m, await assertDb.PazaryeriOdemeDagitimlari
            .Where(x => x.TedarikciSiparisId != cancelledOrderId).Select(x => x.IadeTutari).SingleAsync());
        Assert.Equal("KismiIade", await assertDb.PazaryeriOdemeleri.Select(x => x.Durum).SingleAsync());
    }

    [Fact]
    public async Task VadeliOrder_ConsumesOnDeliveryAndCreatesUnpaidAccountingWithoutPayment()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "credit-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 2) }, Vadeli: true));
        await using (var db = f.Db())
        {
            Assert.Equal(PazaryeriSiparisDurumlari.SiparisVerildi, (await db.PazaryeriAnaSiparisleri.SingleAsync()).Durum);
            Assert.Equal(2m, (await db.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA)).RezerveMiktar);
        }
        int supplierOrderId;
        await using (var db = f.Db()) supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.SevkEdildi, "Kargo", "TRK", null));
        await f.Service(1).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TeslimEdildi, null, null, null));
        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.CariOdemeBekliyor, (await assertDb.TedarikciSiparisleri.SingleAsync()).Durum);
        Assert.Equal(8m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(0m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.RezerveMiktar).SingleAsync());
        Assert.Equal(2, await assertDb.Faturalar.CountAsync());
        Assert.Equal(2, await assertDb.CariKartlari.CountAsync());
        Assert.Equal(2, await assertDb.CariHareketleri.CountAsync());
        Assert.Equal(1, await assertDb.StokHareketleri.CountAsync(x => x.IsletmeId == 1 && x.Miktar == 2));
        Assert.Empty(await assertDb.PazaryeriOdemeleri.ToListAsync());
        Assert.Empty(await assertDb.PazaryeriOdemeDagitimlari.ToListAsync());
    }

    [Fact]
    public async Task AcceptOffer_CreatesSiparisVerildiOrderIdempotently()
    {
        await using var f = await Fixture.CreateAsync();
        int offerId;
        await using (var db = f.Db())
        {
            db.TedarikAlimTalepleri.Add(new TedarikAlimTalebi { Id = 20, AliciIsletmeId = 1, Baslik = "Talep", Kategori = "Genel", UrunHizmet = "Özel ürün", Miktar = 3, Birim = "Adet", TeslimatSehri = "Istanbul", SonTeklifAt = DateTime.UtcNow.AddDays(1) });
            db.TedarikTeklifleri.Add(new TedarikTeklifi { Id = 30, TalepId = 20, TedarikciIsletmeId = 2, BirimFiyat = 15, KdvOrani = 20, MinimumSiparis = 1, TerminGun = 2 });
            await db.SaveChangesAsync(); offerId = 30;
        }
        var first = await f.Service(1).AcceptOfferAsync(offerId, new("Adres", Vadeli: true));
        var repeat = await f.Service(1).AcceptOfferAsync(offerId, new("Adres", Vadeli: true));
        Assert.Equal(first.Id, repeat.Id); Assert.True(repeat.TekrarKullanildi);
        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.SiparisVerildi, (await assertDb.PazaryeriAnaSiparisleri.SingleAsync()).Durum);
        Assert.Equal(1, await assertDb.TedarikciSiparisleri.CountAsync());
        Assert.Equal("KabulEdildi", (await assertDb.TedarikTeklifleri.SingleAsync()).Durum);
    }

    [Fact]
    public async Task ShipmentQr_IsTenantScopedAndReceiptFinalizesStockOnce()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "qr-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 2) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", "Tedarikçi", "IRS-1", "34 ABC 1", "Sürücü", "Ana depo", null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 2, 1, "LOT-1", DateTime.UtcNow.AddDays(5), null, null) }));
        var label = Assert.Single(shipment.Etiketler);
        Assert.StartsWith("https://systemcel.app/app/tedarikci-pazaryeri?qr=scq1_", label.QrIcerigi);
        await using (var shipmentDb = f.Db())
        {
            var product = await shipmentDb.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
            Assert.Equal(8m, product.StokMiktari);
            Assert.Equal(0m, product.RezerveMiktar);
            var supplierMovement = Assert.Single(await shipmentDb.StokHareketleri.Where(x =>
                x.IsletmeId == 2 && x.Kaynak == "PazaryeriSevkiyat" && x.Miktar == -2m).ToListAsync());
            Assert.Equal(supplierOrderId, supplierMovement.TedarikciSiparisId);
            Assert.Equal(shipment.Id, supplierMovement.TedarikciSevkiyatId);
        }

        await Assert.ThrowsAsync<KeyNotFoundException>(() => f.Service(3).ResolveShipmentQrAsync(label.QrIcerigi));
        var resolved = await f.Service(1).ResolveShipmentQrAsync(label.QrIcerigi);
        Assert.Equal("A", resolved.Sku);
        await using (var expiryDb = f.Db())
        {
            var shippedLine = await expiryDb.TedarikciSevkiyatKalemleri.SingleAsync();
            shippedLine.SonKullanmaTarihi = DateTime.UtcNow.AddDays(-2);
            await expiryDb.SaveChangesAsync();
        }
        var expired = await Assert.ThrowsAsync<ArgumentException>(() => f.Service(1).ReceiveShipmentQrAsync(
            label.QrIcerigi, new("expired-receipt", 2, 0, null, null)));
        Assert.Contains("Son kullanma tarihi", expired.Message);
        await using (var expiryDb = f.Db())
        {
            var shippedLine = await expiryDb.TedarikciSevkiyatKalemleri.SingleAsync();
            shippedLine.SonKullanmaTarihi = DateTime.UtcNow.AddDays(5);
            await expiryDb.SaveChangesAsync();
        }
        var first = await f.Service(1).ReceiveShipmentQrAsync(label.QrIcerigi, new("receipt-1", 2, 0, null, "Eksiksiz"));
        var repeated = await f.Service(1).ReceiveShipmentQrAsync(label.QrIcerigi, new("receipt-1", 2, 0, null, "Eksiksiz"));
        Assert.Equal(first.Id, repeated.Id);
        Assert.True(repeated.TekrarKullanildi);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service(1).ReceiveShipmentQrAsync(label.QrIcerigi, new("receipt-second-key", 2, 0, null, "Tekrar")));

        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.CariOdemeBekliyor, (await assertDb.TedarikciSiparisleri.SingleAsync()).Durum);
        Assert.Equal(8m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(1, await assertDb.StokHareketleri.CountAsync(x => x.IsletmeId == 1 && x.Miktar == 2));
        var buyerMovement = await assertDb.StokHareketleri.SingleAsync(x => x.IsletmeId == 1 && x.Kaynak == "PazaryeriMalKabul");
        Assert.Equal(supplierOrderId, buyerMovement.TedarikciSiparisId);
        Assert.Equal(first.Id, buyerMovement.TedarikciMalKabulId);
        var cariMovements = await assertDb.CariHareketleri.Where(x => x.Kaynak == "PazaryeriMalKabul").ToListAsync();
        Assert.Equal(2, cariMovements.Count);
        Assert.All(cariMovements,
            movement => { Assert.Equal(supplierOrderId, movement.TedarikciSiparisId); Assert.Equal(first.Id, movement.TedarikciMalKabulId); });
        Assert.Single(await assertDb.TedarikciMalKabulleri.ToListAsync());
        var storedLabel = await assertDb.TedarikciSevkiyatEtiketleri.SingleAsync();
        Assert.NotEqual(label.Kod, storedLabel.KodHash);
        Assert.Equal(64, storedLabel.KodHash.Length);
    }

    [Fact]
    public async Task ShipmentQr_WithTemperatureRangeRequiresValidMeasurementBeforeAcceptance()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "temperature-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId,
            new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId,
            new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Soğuk zincir", null, "IRS-TEMP", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, "LOT-TEMP", null, -2m, 5m) }));
        var qr = shipment.Etiketler[0].QrIcerigi;
        var resolved = await f.Service(1).ResolveShipmentQrAsync(qr);
        Assert.Equal(-2m, resolved.SicaklikMin);
        Assert.Equal(5m, resolved.SicaklikMax);

        await Assert.ThrowsAsync<ArgumentException>(() => f.Service(1).ReceiveShipmentQrAsync(qr,
            new("temperature-missing", 1, 0, null, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service(1).ReceiveShipmentQrAsync(qr,
            new("temperature-outside", 1, 0, null, null, OlculenSicaklik: 8m)));
        await using (var db = f.Db())
            Assert.Empty(await db.TedarikciMalKabulleri.ToListAsync());

        await f.Service(1).ReceiveShipmentQrAsync(qr,
            new("temperature-ok", 1, 0, null, null, OlculenSicaklik: 0m));
        await using var verified = f.Db();
        Assert.Equal(0m, await verified.TedarikciMalKabulleri.Select(x => x.OlculenSicaklik).SingleAsync());
    }

    [Fact]
    public async Task CreateShipment_ConcurrentRequestsOnSeparateConnectionsConsumeStockOnce()
    {
        await using var f = await Fixture.CreateAsync(separateConnections: true);
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "parallel-shipment", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        var supplier = f.Service(2);
        await supplier.UpdateSupplierOrderStateAsync(supplierOrderId,
            new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await supplier.UpdateSupplierOrderStateAsync(supplierOrderId,
            new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));

        await using (var first = f.Db())
        await using (var second = f.Db())
        {
            Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstService = f.Service(2);
        var secondService = f.Service(2);
        var request = new TedarikciSevkiyatOlusturRequest(
            "Tedarikçi aracı", null, "IRS-PARALLEL", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) });
        var firstAttempt = RunConcurrentAttemptAsync(start.Task,
            () => firstService.CreateShipmentAsync(supplierOrderId, request));
        var secondAttempt = RunConcurrentAttemptAsync(start.Task,
            () => secondService.CreateShipmentAsync(supplierOrderId, request));
        start.SetResult();
        var attempts = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.Single(attempts, x => x.Result is not null);
        Assert.IsType<InvalidOperationException>(Assert.Single(attempts, x => x.Error is not null).Error);
        await using var assertDb = f.Db();
        Assert.Single(await assertDb.TedarikciSevkiyatlari.ToListAsync());
        Assert.Single(await assertDb.TedarikciSevkiyatEtiketleri.ToListAsync());
        var line = await assertDb.TedarikciSiparisKalemleri.SingleAsync();
        Assert.Equal(1m, line.SevkEdilenMiktar);
        var product = await assertDb.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
        Assert.Equal(9m, product.StokMiktari);
        Assert.Equal(0m, product.RezerveMiktar);
    }

    [Fact]
    public async Task ReceiveShipmentQr_ConcurrentDifferentKeysAcceptsLabelOnceOnSeparateConnections()
    {
        await using var f = await Fixture.CreateAsync(separateConnections: true);
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "parallel-receipt", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        var supplier = f.Service(2);
        await supplier.UpdateSupplierOrderStateAsync(supplierOrderId,
            new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await supplier.UpdateSupplierOrderStateAsync(supplierOrderId,
            new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await supplier.CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-PARALLEL-RECEIPT", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) }));
        var qr = shipment.Etiketler[0].QrIcerigi;

        await using (var first = f.Db())
        await using (var second = f.Db())
        {
            Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstBuyer = f.Service(1);
        var secondBuyer = f.Service(1);
        var firstAttempt = RunConcurrentAttemptAsync(start.Task,
            () => firstBuyer.ReceiveShipmentQrAsync(qr, new("parallel-receipt-a", 1, 0, null, null)));
        var secondAttempt = RunConcurrentAttemptAsync(start.Task,
            () => secondBuyer.ReceiveShipmentQrAsync(qr, new("parallel-receipt-b", 1, 0, null, null)));
        start.SetResult();
        var attempts = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.Single(attempts, x => x.Result is not null);
        Assert.IsType<InvalidOperationException>(Assert.Single(attempts, x => x.Error is not null).Error);
        await using var assertDb = f.Db();
        var receipt = await assertDb.TedarikciMalKabulleri.SingleAsync();
        Assert.Contains(receipt.IdempotencyAnahtari, new[] { "parallel-receipt-a", "parallel-receipt-b" });
        Assert.Equal(1m, receipt.KabulEdilenMiktar);
        Assert.Equal(1m, await assertDb.TedarikciSiparisKalemleri.Select(x => x.KabulEdilenMiktar).SingleAsync());
        Assert.Single(await assertDb.StokHareketleri.Where(x =>
            x.IsletmeId == 1 && x.Kaynak == "PazaryeriMalKabul").ToListAsync());
        Assert.Equal(2, await assertDb.Faturalar.CountAsync());
        Assert.Equal(2, await assertDb.FaturaSatirlari.CountAsync());
        Assert.Equal(9m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductA)
            .Select(x => x.StokMiktari).SingleAsync());
    }

    private static async Task<ConcurrentAttempt<T>> RunConcurrentAttemptAsync<T>(Task start, Func<Task<T>> operation)
    {
        await start;
        try { return new(await operation(), null); }
        catch (Exception error) { return new(default, error); }
    }

    private sealed record ConcurrentAttempt<T>(T? Result, Exception? Error);

    [Fact]
    public async Task CreateShipment_RejectsOverDeliveryWithoutConsumingReservation()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "over-delivery", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-OVER", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 2, 1, null, null, null, null) })));

        await using var assertDb = f.Db();
        var product = await assertDb.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
        Assert.Equal(10m, product.StokMiktari);
        Assert.Equal(1m, product.RezerveMiktar);
        Assert.Empty(await assertDb.TedarikciSevkiyatlari.ToListAsync());
    }

    [Fact]
    public async Task ShipmentQr_RejectedQuantityBlocksSettlementAndOpensDispute()
    {
        await using var f = await Fixture.CreateAsync();
        var master = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "qr-reject", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }));
        await f.Service(1).PayOrderAsync(master.Id, new PazaryeriOdemeRequest("qr-reject-payment"));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Dağıtım ağı", "Bölgesel dağıtıcı", "IRS-2", null, null, "Dağıtım merkezi", null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, "LOT-2", null, 2, 8) }));
        var receiptRequest = new TedarikciMalKabulRequest("receipt-reject", 0, 1, "Hasarlı", "Ambalaj yırtık",
            IkinciRedNedeni: "Sıcaklık uygunsuzluğu");
        await f.Service(1).ReceiveShipmentQrAsync(shipment.Etiketler[0].QrIcerigi, receiptRequest);
        var repeated = await f.Service(1).ReceiveShipmentQrAsync(shipment.Etiketler[0].QrIcerigi, receiptRequest);
        Assert.True(repeated.TekrarKullanildi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[0].QrIcerigi, receiptRequest with { IkinciRedNedeni = "Belge uyuşmazlığı" }));

        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.Itirazli, (await assertDb.TedarikciSiparisleri.SingleAsync()).Durum);
        Assert.Equal("Bloke", (await assertDb.TedarikciHakEdisleri.SingleAsync()).Durum);
        var complaint = await assertDb.TedarikciSiparisSikayetleri.SingleAsync();
        Assert.Equal("Sıcaklık uygunsuzluğu", (await assertDb.TedarikciMalKabulleri.SingleAsync()).IkinciRedNedeni);
        Assert.Contains("Sıcaklık uygunsuzluğu", complaint.Aciklama);
        Assert.Equal(TedarikciSikayetKategorileri.Hasarli, complaint.Kategori);
        Assert.Equal(TedarikciSikayetDurumlari.Acik, complaint.Durum);
        Assert.Empty(await assertDb.Faturalar.ToListAsync());
        Assert.Empty(await assertDb.StokHareketleri.Where(x => x.IsletmeId == 1).ToListAsync());
    }

    [Fact]
    public async Task ResolveDispute_PartialShareRefundsBuyerAndLeavesOnlySupplierSharePayable()
    {
        await using var f = await Fixture.CreateAsync();
        var master = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "dispute-share", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }));
        await f.Service(1).PayOrderAsync(master.Id, new PazaryeriOdemeRequest("dispute-share-payment"));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Dağıtım ağı", null, "IRS-DISPUTE", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) }));
        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[0].QrIcerigi, new("dispute-share-receipt", 0, 1, "Hasarlı", "Ürün hasarlı"));

        await f.Service(1).ResolveDisputeAsync(supplierOrderId, new(
            true,
            "Hasar bedeli alıcıya iade edildi.",
            PazaryeriItirazKararlari.KismiPaylas,
            5m));

        await using var assertDb = f.Db();
        var settlement = await assertDb.TedarikciHakEdisleri.SingleAsync();
        Assert.Equal(5m, settlement.NetTutar);
        Assert.Equal(16.40m, settlement.IadeTutari);
        Assert.Equal("Bekliyor", settlement.Durum);
        Assert.Equal(PazaryeriSiparisDurumlari.HakEdisBekliyor,
            await assertDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
        Assert.Equal("KismiIade", await assertDb.PazaryeriOdemeleri.Select(x => x.Durum).SingleAsync());
        Assert.Equal(16.40m, await assertDb.PazaryeriOdemeDagitimlari.Select(x => x.IadeTutari).SingleAsync());
        Assert.Contains("KismiPaylas", await assertDb.TedarikciSiparisDurumKayitlari
            .OrderByDescending(x => x.Id).Select(x => x.Aciklama).FirstAsync());

        await f.Service(1).CompleteSettlementAsync(supplierOrderId, new("manual-review-transfer"));
        await using var completedDb = f.Db();
        var completedSettlement = await completedDb.TedarikciHakEdisleri.SingleAsync();
        Assert.Equal(5m, completedSettlement.OdenenTutar);
        Assert.Equal("Tamamlandi", completedSettlement.Durum);
        Assert.Equal(PazaryeriSiparisDurumlari.Tamamlandi,
            await completedDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
    }

    [Fact]
    public async Task ShipmentQr_OpenDisputeDoesNotBlockUnrelatedAcceptedLabelPayout()
    {
        await using var f = await Fixture.CreateAsync();
        var master = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "dispute-label-scope", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 2) }));
        await f.Service(1).PayOrderAsync(master.Id, new PazaryeriOdemeRequest("dispute-label-payment"));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-SCOPE", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 2, 2, null, null, null, null) }));

        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[0].QrIcerigi, new("scope-reject", 0, 1, "Hasarlı", "İlk koli hasarlı"));
        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[1].QrIcerigi, new("scope-accept", 1, 0, null, "İkinci koli sağlam"));

        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.Itirazli,
            await assertDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
        var acceptedReceipt = await assertDb.TedarikciMalKabulleri.SingleAsync(x => x.IdempotencyAnahtari == "scope-accept");
        Assert.True(acceptedReceipt.SerbestBirakilanNetTutar > 0m);
        var settlement = await assertDb.TedarikciHakEdisleri.SingleAsync();
        Assert.Equal(acceptedReceipt.SerbestBirakilanNetTutar, settlement.OdenenTutar);
        Assert.Equal("KismenSerbest", settlement.Durum);
    }

    [Fact]
    public async Task ShipmentQr_PartialVadeliAcceptanceBooksOnlyAcceptedQuantity()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "partial-vadeli", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 2) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-PART", null, null, "Ana depo", null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 2, 2, "LOT-P", null, null, null) }));

        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[0].QrIcerigi, new("partial-vadeli-1", 1, 0, null, "İlk koli"));

        await using (var partialDb = f.Db())
        {
            Assert.Equal(PazaryeriSiparisDurumlari.KismenKabul,
                await partialDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
            Assert.All(await partialDb.Faturalar.ToListAsync(), invoice => Assert.Equal(24m, invoice.GenelToplam));
            Assert.Equal(1m, (await partialDb.StokHareketleri
                .Where(x => x.IsletmeId == 1 && x.Kaynak == "PazaryeriMalKabul")
                .Select(x => x.Miktar).ToListAsync()).Sum());
            var product = await partialDb.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
            Assert.Equal(8m, product.StokMiktari);
            Assert.Equal(0m, product.RezerveMiktar);
        }

        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[1].QrIcerigi, new("partial-vadeli-2", 1, 0, null, "İkinci koli"));

        await using var assertDb = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.CariOdemeBekliyor,
            await assertDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
        Assert.All(await assertDb.Faturalar.ToListAsync(), invoice => Assert.Equal(48m, invoice.GenelToplam));
        Assert.Equal(2m, (await assertDb.StokHareketleri
            .Where(x => x.IsletmeId == 1 && x.Kaynak == "PazaryeriMalKabul")
            .Select(x => x.Miktar).ToListAsync()).Sum());
        Assert.Equal(4, await assertDb.FaturaSatirlari.CountAsync());
        var completedProduct = await assertDb.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
        Assert.Equal(8m, completedProduct.StokMiktari);
        Assert.Equal(0m, completedProduct.RezerveMiktar);
    }

    [Fact]
    public async Task ShipmentQr_PartialPrepaidAcceptanceReleasesPayoutProportionally()
    {
        await using var f = await Fixture.CreateAsync();
        var master = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "partial-prepaid", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 2) }));
        await f.Service(1).PayOrderAsync(master.Id, new PazaryeriOdemeRequest("partial-prepaid-payment"));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-PAID", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 2, 2, null, null, null, null) }));

        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[0].QrIcerigi, new("partial-paid-1", 1, 0, null, null));
        await using (var partialDb = f.Db())
        {
            var settlement = await partialDb.TedarikciHakEdisleri.SingleAsync();
            Assert.Equal("KismenSerbest", settlement.Durum);
            Assert.Equal(21.40m, settlement.OdenenTutar);
            var receipt = await partialDb.TedarikciMalKabulleri.SingleAsync();
            Assert.Equal(24m, receipt.KabulBrutTutar);
            Assert.Equal(21.40m, receipt.SerbestBirakilanNetTutar);
            Assert.NotEmpty(receipt.HakEdisAktarimReferansi);
            var paymentMovements = await partialDb.TahsilatOdemeleri.Where(x => x.TedarikciSiparisId == supplierOrderId).ToListAsync();
            Assert.Equal(2, paymentMovements.Count);
            Assert.All(paymentMovements, movement => Assert.Equal(receipt.Id, movement.TedarikciMalKabulId));
        }

        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[1].QrIcerigi, new("partial-paid-2", 1, 0, null, null));
        await using var assertDb = f.Db();
        var completedSettlement = await assertDb.TedarikciHakEdisleri.SingleAsync();
        Assert.Equal("SerbestBirakildi", completedSettlement.Durum);
        Assert.Equal(completedSettlement.NetTutar, completedSettlement.OdenenTutar);
        Assert.Equal(PazaryeriSiparisDurumlari.Tamamlandi,
            await assertDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
        Assert.Equal(completedSettlement.NetTutar,
            (await assertDb.TedarikciMalKabulleri.Select(x => x.SerbestBirakilanNetTutar).ToListAsync()).Sum());
    }

    [Fact]
    public async Task ShipmentQr_RequiresReceiptRoleAndStoresServerSideAuditEvidence()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "receipt-role", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        int branchId;
        int warehouseId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
            var actor = new Kullanici { Id = 50, AuthProvider = "clerk", AuthProviderUserId = "receipt-user", Eposta = "receipt@example.com" };
            var branch = new Sube { IsletmeId = 1, Ad = "Merkez", Kod = "MRK", OlusturmaAnahtari = "receipt-branch", IcerikOzeti = "receipt" };
            db.Kullanicilar.Add(actor);
            db.Subeler.Add(branch);
            await db.SaveChangesAsync();
            var warehouse = new StokDepo { IsletmeId = 1, SubeId = branch.Id, Ad = "Kabul deposu", Kod = "KBL" };
            db.StokDepolari.Add(warehouse);
            db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = 1, KullaniciId = actor.Id, Rol = "personel", Durum = "Aktif", DavetEposta = actor.Eposta });
            await db.SaveChangesAsync();
            branchId = branch.Id;
            warehouseId = warehouse.Id;
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-ROLE", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) },
            VarisDeposu: warehouseId));
        var identity = new FakeCurrentUser("receipt-user");
        var request = new TedarikciMalKabulRequest(
            "receipt-role-1", 1, 0, null, "Sağlam", branchId, warehouseId,
            "forged-user", "device-1", "127.0.0.1", "forged-hash");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            f.Service(2).ValidateReceiptEvidenceAccessAsync(shipment.Etiketler[0].QrIcerigi, branchId, warehouseId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            f.Service(1, identity).ValidateReceiptEvidenceAccessAsync(shipment.Etiketler[0].QrIcerigi, branchId, warehouseId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            f.Service(1, identity).ReceiveShipmentQrAsync(shipment.Etiketler[0].QrIcerigi, request));
        await using (var db = f.Db())
        {
            var membership = await db.IsletmeUyelikleri.SingleAsync(x => x.KullaniciId == 50);
            membership.Rol = "mal_kabul_onaylayicisi";
            await db.SaveChangesAsync();
        }

        await f.Service(1, identity).ValidateReceiptEvidenceAccessAsync(
            shipment.Etiketler[0].QrIcerigi, branchId, warehouseId);
        await f.Service(1, identity).ReceiveShipmentQrAsync(shipment.Etiketler[0].QrIcerigi, request);
        await using var assertDb = f.Db();
        var receipt = await assertDb.TedarikciMalKabulleri.SingleAsync();
        Assert.Equal("receipt-user", receipt.IslemYapanKullaniciRef);
        Assert.Equal(branchId, receipt.SubeId);
        Assert.Equal(warehouseId, receipt.DepoId);
        Assert.Equal("device-1", receipt.CihazRef);
        Assert.Equal(64, receipt.BelgeKarmasi.Length);
        Assert.NotEqual("forged-hash", receipt.BelgeKarmasi);
    }

    [Fact]
    public async Task DeliveryComplaint_AllowsSupplierResponseBuyerOutcomeAndOneVerifiedRating()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "complaint-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: true));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-3", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) }));
        await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler[0].QrIcerigi,
            new("complaint-receipt", 0, 1, "Eksik", "Koli içeriği eksik"));

        int complaintId;
        await using (var db = f.Db())
            complaintId = await db.TedarikciSiparisSikayetleri.Select(x => x.Id).SingleAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            f.Service(3).RespondToComplaintAsync(complaintId, new("Bu sipariş bize ait değil.")));
        var answered = await f.Service(2).RespondToComplaintAsync(
            complaintId, new("Eksik ürün için tamamlayıcı sevkiyat planlandı."));
        Assert.Equal(TedarikciSikayetDurumlari.Yanitlandi, answered.Durum);
        var closed = await f.Service(1).CloseComplaintAsync(
            complaintId, new(true, "Tamamlayıcı sevkiyat teslim edildi."));
        Assert.Equal(TedarikciSikayetDurumlari.Cozuldu, closed.Durum);

        var rating = await f.Service(1).SaveSupplierRatingAsync(
            supplierOrderId, new(4, 2, 3, 5, 1, "Eksik ürün nedeniyle sorun yaşandı."));
        Assert.Equal(3m, rating.OrtalamaPuan);
        var updated = await f.Service(1).SaveSupplierRatingAsync(
            supplierOrderId, new(4, 3, 3, 5, 2, "Tedarikçi dönüş yaptı."));
        Assert.Equal(3.40m, updated.OrtalamaPuan);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            f.Service(2).SaveSupplierRatingAsync(supplierOrderId, new(5, 5, 5, 5, null, null)));

        await using var assertDb = f.Db();
        Assert.Single(await assertDb.TedarikciDegerlendirmeleri.ToListAsync());
        Assert.Equal(PazaryeriSiparisDurumlari.MalKabulBekliyor,
            await assertDb.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
        Assert.Equal(TedarikciSikayetDurumlari.Cozuldu,
            await assertDb.TedarikciSiparisSikayetleri.Select(x => x.Durum).SingleAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectedReceipt_AuthorizedRedeliveryCanShipAndCompleteWithoutErasingAudit(bool vadeli)
    {
        await using var f = await Fixture.CreateAsync();
        var master = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Depo", "redelivery-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }, Vadeli: vadeli));
        if (!vadeli)
            await f.Service(1).PayOrderAsync(master.Id, new PazaryeriOdemeRequest("redelivery-payment"));
        int supplierOrderId;
        int lineId;
        await using (var db = f.Db())
        {
            supplierOrderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }

        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(supplierOrderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var first = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-RED-1", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) }));
        await f.Service(1).ReceiveShipmentQrAsync(first.Etiketler[0].QrIcerigi,
            new("redelivery-reject", 0, 1, "Hasarlı", "Yeniden gönderim gerekli"));

        await f.Service(1).ResolveDisputeAsync(supplierOrderId,
            new PazaryeriItirazCozRequest(true, "Hasarlı ürün yenilenecek.", PazaryeriItirazKararlari.YenidenTeslim));
        await using (var db = f.Db())
        {
            var line = await db.TedarikciSiparisKalemleri.SingleAsync();
            Assert.Equal(0m, line.SevkEdilenMiktar);
            Assert.Equal(0m, line.ReddedilenMiktar);
            Assert.Equal(1, await db.TedarikciMalKabulleri.CountAsync(x => x.ReddedilenMiktar == 1m));
        }

        var replacement = await f.Service(2).CreateShipmentAsync(supplierOrderId, new(
            "Tedarikçi aracı", null, "IRS-RED-2", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, 1, null, null, null, null) }));
        await f.Service(1).ReceiveShipmentQrAsync(replacement.Etiketler[0].QrIcerigi,
            new("redelivery-accept", 1, 0, null, "Sağlam teslim"));

        await using var verified = f.Db();
        var completedLine = await verified.TedarikciSiparisKalemleri.SingleAsync();
        Assert.Equal(1m, completedLine.SevkEdilenMiktar);
        Assert.Equal(1m, completedLine.KabulEdilenMiktar);
        Assert.Equal(0m, completedLine.ReddedilenMiktar);
        Assert.Equal(2, await verified.TedarikciSevkiyatlari.CountAsync());
        Assert.Equal(2, await verified.TedarikciMalKabulleri.CountAsync());
        Assert.Equal(vadeli ? PazaryeriSiparisDurumlari.CariOdemeBekliyor : PazaryeriSiparisDurumlari.Tamamlandi,
            await verified.TedarikciSiparisleri.Select(x => x.Durum).SingleAsync());
    }

    [Fact]
    public async Task ExpirePendingPrepaidOrder_ReleasesReservationAndCancelsOrder()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest("A", "expire-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }));
        var expired = await f.Service(1).ExpirePendingOrdersAsync(DateTime.UtcNow.AddHours(1));
        Assert.Equal(1, expired);
        await using var db = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.IptalEdildi, (await db.PazaryeriAnaSiparisleri.SingleAsync()).Durum);
        Assert.Equal(PazaryeriSiparisDurumlari.IptalEdildi, (await db.TedarikciSiparisleri.SingleAsync()).Durum);
        Assert.Equal(0m, (await db.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA)).RezerveMiktar);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection; private readonly DbContextOptions<CashTrackerDbContext> _options; private readonly string? _dbPath; private readonly FakeIsletme _business = new();
        public int ProductA { get; private set; } public int ProductB { get; private set; }
        private Fixture(SqliteConnection connection, DbContextOptions<CashTrackerDbContext> options, string? dbPath = null) { _connection = connection; _options = options; _dbPath = dbPath; }
        public static async Task<Fixture> CreateAsync(decimal stockA = 10m, bool separateConnections = false)
        { string? dbPath = null; SqliteConnection c; DbContextOptions<CashTrackerDbContext> o; if (separateConnections) { dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_marketplace_race_{Guid.NewGuid():N}.db"); var connectionString = $"Data Source={dbPath};Cache=Shared;Pooling=False"; c = new SqliteConnection(connectionString); await c.OpenAsync(); o = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connectionString).Options; } else { c = new SqliteConnection("DataSource=:memory:"); await c.OpenAsync(); o = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(c).Options; } var f = new Fixture(c, o, dbPath); await using var db = f.Db(); await db.Database.EnsureCreatedAsync(); db.Isletmeler.AddRange(new Isletme { Id = 1, Ad = "Buyer" }, new Isletme { Id = 2, Ad = "Supplier A" }, new Isletme { Id = 3, Ad = "Other" }); db.Kullanicilar.Add(new Kullanici { Id = 1, AuthProvider = "clerk", AuthProviderUserId = "buyer-owner", Eposta = "buyer@example.com", AdSoyad = "Buyer Owner" }); db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = 1, KullaniciId = 1, Rol = "isletme_sahibi", Durum = "Aktif", DavetEposta = "buyer@example.com" }); db.UrunHizmetleri.AddRange(new UrunHizmet { Id = 200, IsletmeId = 2, Tip = "Urun", Ad = "A", Barkod = "A", Birim = "Adet", Aktif = true }, new UrunHizmet { Id = 201, IsletmeId = 3, Tip = "Urun", Ad = "B", Barkod = "B", Birim = "Adet", Aktif = true }); db.TedarikciProfilleri.AddRange(new TedarikciProfil { Id = 10, IsletmeId = 2, Unvan = "A", Dogrulandi = true, Yayinda = true, KomisyonOrani = 10, VergiNo = "1", Iban = "TR1", Adres = "x", YetkiliAdSoyad = "x", PazaryeriSozlesmeVersiyonu = "1" }, new TedarikciProfil { Id = 11, IsletmeId = 3, Unvan = "B", Dogrulandi = true, Yayinda = true, KomisyonOrani = 10, VergiNo = "2", Iban = "TR2", Adres = "x", YetkiliAdSoyad = "x", PazaryeriSozlesmeVersiyonu = "1" }); db.TedarikciUrunleri.AddRange(new TedarikciUrun { Id = 100, TedarikciProfilId = 10, TedarikciIsletmeId = 2, KaynakUrunHizmetId = 200, Sku = "A", Ad = "A", BirimFiyat = 20, KdvOrani = 20, StokMiktari = stockA, MinimumSiparisMiktari = 1 }, new TedarikciUrun { Id = 101, TedarikciProfilId = 11, TedarikciIsletmeId = 3, KaynakUrunHizmetId = 201, Sku = "B", Ad = "B", BirimFiyat = 30, KdvOrani = 20, StokMiktari = 10, MinimumSiparisMiktari = 1 }); await db.SaveChangesAsync(); f.ProductA = 100; f.ProductB = 101; return f; }
        public CashTrackerDbContext Db() => new(_options); public TedarikciPazaryeriService Service(int id, ICurrentUserContext? currentUser = null) { _business.Active = new Isletme { Id = id, Ad = id == 1 ? "Buyer" : id == 2 ? "Supplier A" : "Other" }; return new(new SingleDbContextFactory(_options), _business, new FakeManagement(), new FakeMarketplacePaymentGateway(), new PazaryeriOptions { KomisyonKdvOrani = 20, TevkifatOrani = 1 }, currentUser ?? new FakeCurrentUser(id == 1 ? "buyer-owner" : $"business-{id}-user")); }
        public async ValueTask DisposeAsync() { await _connection.DisposeAsync(); if (_dbPath is null) return; SqliteConnection.ClearAllPools(); if (File.Exists(_dbPath)) File.Delete(_dbPath); }
    }
    private sealed class FakeIsletme : IIsletmeService { public Isletme Active { get; set; } = new() { Id = 1, Ad = "Buyer" }; public Task<List<Isletme>> GetAllAsync()=>Task.FromResult(new List<Isletme>{Active}); public Task<Isletme?> GetByIdAsync(int id)=>Task.FromResult<Isletme?>(id==Active.Id?Active:null); public Task<Isletme> GetActiveAsync()=>Task.FromResult(Active); public Task<int> GetActiveIdAsync()=>Task.FromResult(Active.Id); public Task<int> CreateAsync(string ad,bool makeActive=false)=>Task.FromResult(0); public Task RenameAsync(int id,string ad)=>Task.CompletedTask; public Task UpdateSetupAsync(int id,string ad,string t,string k,bool c,string? h=null,bool? m=null,MuhasebeciProfilKaydetRequest? p=null,string? v=null,string? o=null,string? s=null)=>Task.CompletedTask; public Task SetActiveAsync(int id)=>Task.CompletedTask; public Task SetActiveCustomerContextAsync(int id)=>Task.CompletedTask; public Task ClearActiveCustomerContextAsync()=>Task.CompletedTask; public Task<ActiveBusinessAccess> GetActiveAccessAsync()=>Task.FromResult(default(ActiveBusinessAccess)!); public Task DeleteAsync(int id)=>Task.CompletedTask; }
    private sealed class FakeManagement : ISystemcelYonetimService { public Task<bool> IsCurrentUserAdminAsync(CancellationToken ct=default)=>Task.FromResult(true); public Task<MuhasebeciBasvuruListeDto> GetMuhasebeciBasvurulariAsync(string? d=null,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciBasvuruListeDto)!); public Task<MuhasebeciBasvuruDto> ApproveMuhasebeciBasvurusuAsync(int i,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciBasvuruDto)!); public Task<MuhasebeciBasvuruDto> RejectMuhasebeciBasvurusuAsync(int i,MuhasebeciBasvuruRedRequest r,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciBasvuruDto)!); public Task<YonetimOdemeIncelemeDto> GetOdemeIncelemeAsync(string? d=null,bool s=false,int l=100,CancellationToken ct=default)=>Task.FromResult(default(YonetimOdemeIncelemeDto)!); public Task<MuhasebeciAktarimListeDto> GetMuhasebeciAktarimlariAsync(string d,int? i=null,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciAktarimListeDto)!); public Task<MuhasebeciAktarimOzetDto> CompleteMuhasebeciAktarimiAsync(int i,MuhasebeciAktarimTamamlaRequest r,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciAktarimOzetDto)!); public Task<DestekTalebiListeDto> GetDestekTalepleriAsync(CancellationToken ct=default)=>Task.FromResult(default(DestekTalebiListeDto)!); public Task<DestekTalebiDto> UpdateDestekTalebiAsync(int i,DestekTalebiGuncelleRequest r,CancellationToken ct=default)=>Task.FromResult(default(DestekTalebiDto)!); public Task<EntitlementOverrideResult> ApplyEntitlementOverrideAsync(int i,EntitlementOverrideRequest r,CancellationToken ct=default)=>Task.FromResult(default(EntitlementOverrideResult)!); }
    private sealed class FakeCurrentUser(string providerUserId) : ICurrentUserContext { public CurrentUserIdentity? GetCurrentUser() => new(providerUserId, null, null, true); }
}
