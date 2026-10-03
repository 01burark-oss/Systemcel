using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed partial class TedarikciPazaryeriServiceTests
{
    private static async Task<int> ReceiveAndApproveForCorrectionAsync(Fixture f, string suffix = "correction")
    {
        var shipment = await CreateApprovalShipmentAsync(f);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr,
            new($"{suffix}-receive", 1m, 0m, null, "Teslim alındı"));
        Assert.Equal("OnayBekliyor", receipt.OnayDurumu);
        await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .ApproveReceiptAsync(receipt.Id, new("İlk kabul onaylandı."));
        return receipt.Id;
    }

    [Fact]
    public async Task ReceiptCorrection_PartialThenFullReversalsPreserveReceiptAndInvoices()
    {
        await using var f = await Fixture.CreateAsync();
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f);
        int orderId;
        decimal originalSettlementNet, originalPaid, originalGross;
        object[] originalInvoices;
        object[] originalInvoiceLines;
        await using (var db = f.Db())
        {
            var receipt = await db.TedarikciMalKabulleri.SingleAsync(x => x.Id == receiptId);
            orderId = receipt.TedarikciSiparisId;
            originalGross = receipt.KabulBrutTutar;
            originalInvoices = (await db.Faturalar.OrderBy(x => x.Id).Select(x => new
            {
                x.Id, x.IsletmeId, x.SubeId, x.CariKartId, x.Tarih, x.VadeTarihi, x.FaturaTipi, x.Durum,
                x.YerelFaturaNo, x.PortalBelgeNo, x.PortalUuid, x.HizliSatisAnahtari, x.AraToplam,
                x.IskontoToplam, x.KdvToplam, x.GenelToplam, x.ParaBirimi, x.KurSnapshot, x.GenelToplamTry,
                x.OdenenTutar, x.OdemeYontemi, x.Aciklama, x.KesildiAt, x.CreatedAt, x.UpdatedAt
            }).ToListAsync()).Cast<object>().ToArray();
            originalInvoiceLines = (await db.FaturaSatirlari.OrderBy(x => x.Id).Select(x => new
            {
                x.Id, x.IsletmeId, x.FaturaId, x.UrunHizmetId, x.Aciklama, x.Birim, x.Miktar,
                x.BirimFiyat, x.IskontoOrani, x.IskontoTutar, x.KdvOrani, x.KdvTutar,
                x.SatirNetTutar, x.SatirToplam, x.StokEtkilesin
            }).ToListAsync()).Cast<object>().ToArray();
            var initialSettlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
            originalSettlementNet = initialSettlement.NetTutar;
            originalPaid = initialSettlement.OdenenTutar;
            Assert.Equal(initialSettlement.NetTutar, originalPaid);
        }

        var firstRequest = new TedarikciMalKabulDuzeltmeRequest("correction-1", .333m, "Koli eksiği");
        var first = await f.Service(1).RequestReceiptCorrectionAsync(receiptId, firstRequest);
        Assert.Equal("OnayBekliyor", first.Durum);
        Assert.True((await f.Service(1).RequestReceiptCorrectionAsync(receiptId, firstRequest)).TekrarKullanildi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            firstRequest with { Miktar = .334m }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("correction-other", .1m, "Başka talep")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1, new ApprovalIdentity("buyer-owner"), admin: true)
            .DecideReceiptCorrectionAsync(first.Id, new(true, "Kendi talebi")));

        var approver = f.Service(1, new ApprovalIdentity("second-approver"), admin: true);
        var appliedFirst = await approver.DecideReceiptCorrectionAsync(first.Id, new(true, "Eksik doğrulandı"));
        Assert.Equal("Uygulandi", appliedFirst.Durum);
        Assert.True((await approver.DecideReceiptCorrectionAsync(first.Id, new(true, "Eksik doğrulandı"))).TekrarKullanildi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => approver.DecideReceiptCorrectionAsync(first.Id, new(true, "Değişen karar notu")));

        var secondRequest = new TedarikciMalKabulDuzeltmeRequest("correction-2", .667m, "Kalan koli eksiği");
        var second = await f.Service(1).RequestReceiptCorrectionAsync(receiptId, secondRequest);
        await approver.DecideReceiptCorrectionAsync(second.Id, new(true, "Tam iade miktarı doğrulandı"));

        await using var verify = f.Db();
        var unchangedReceipt = await verify.TedarikciMalKabulleri.SingleAsync(x => x.Id == receiptId);
        Assert.Equal(1m, unchangedReceipt.KabulEdilenMiktar);
        Assert.Equal(originalGross, unchangedReceipt.KabulBrutTutar);
        var finalInvoices = (await verify.Faturalar.OrderBy(x => x.Id).Select(x => new
        {
            x.Id, x.IsletmeId, x.SubeId, x.CariKartId, x.Tarih, x.VadeTarihi, x.FaturaTipi, x.Durum,
            x.YerelFaturaNo, x.PortalBelgeNo, x.PortalUuid, x.HizliSatisAnahtari, x.AraToplam,
            x.IskontoToplam, x.KdvToplam, x.GenelToplam, x.ParaBirimi, x.KurSnapshot, x.GenelToplamTry,
            x.OdenenTutar, x.OdemeYontemi, x.Aciklama, x.KesildiAt, x.CreatedAt, x.UpdatedAt
        }).ToListAsync()).Cast<object>().ToArray();
        var finalInvoiceLines = (await verify.FaturaSatirlari.OrderBy(x => x.Id).Select(x => new
        {
            x.Id, x.IsletmeId, x.FaturaId, x.UrunHizmetId, x.Aciklama, x.Birim, x.Miktar,
            x.BirimFiyat, x.IskontoOrani, x.IskontoTutar, x.KdvOrani, x.KdvTutar,
            x.SatirNetTutar, x.SatirToplam, x.StokEtkilesin
        }).ToListAsync()).Cast<object>().ToArray();
        Assert.Equal(originalInvoices, finalInvoices);
        Assert.Equal(originalInvoiceLines, finalInvoiceLines);
        var order = await verify.TedarikciSiparisleri.SingleAsync(x => x.Id == orderId);
        Assert.Equal(PazaryeriSiparisDurumlari.Itirazli, order.Durum);
        var line = await verify.TedarikciSiparisKalemleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.Equal(0m, line.KabulEdilenMiktar);
        Assert.Equal(1m, line.ReddedilenMiktar);
        var settlement = await verify.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.True(settlement.NetTutar < originalPaid);
        Assert.Equal(originalPaid, settlement.OdenenTutar);
        Assert.Equal(0, await verify.PazaryeriParaTalimatlari.CountAsync(x => x.Tur == "Iade"));
        Assert.Equal(0m, (await verify.PazaryeriOdemeDagitimlari.Select(x => x.IadeTutari).ToListAsync()).Sum());

        var stockCorrections = await verify.StokHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId != null).ToListAsync();
        Assert.Equal(2, stockCorrections.Count);
        Assert.Equal(-1m, stockCorrections.Sum(x => x.Miktar));
        var cari = await verify.CariHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId != null).ToListAsync();
        Assert.Equal(4, cari.Count);
        Assert.All(cari, x => Assert.Contains(x.HareketTipi, new[] { "Borc", "Alacak" }));
        Assert.Equal(2, cari.Select(x => x.TedarikciMalKabulDuzeltmeId).Distinct().Count());
        Assert.All(await verify.PazaryeriDefterKayitlari.Where(x => x.TedarikciMalKabulDuzeltmeId != null).ToListAsync(),
            x => Assert.Contains(x.Yon, new[] { "Borc", "Alacak" }));
        var corrections = await verify.TedarikciMalKabulDuzeltmeleri.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(new[] { "IncelemeBekliyor", "IncelemeBekliyor" }, corrections.Select(x => x.ParaDurumu));
        Assert.All(corrections, x => Assert.Equal("BelgeBekliyor", x.BelgeDurumu));
        Assert.All(corrections, x => Assert.Equal("IadeKonumuBekliyor", x.StokDurumu));
        Assert.Equal(originalGross, corrections.Sum(x => x.BrutTutar));
        Assert.Equal(originalGross, corrections.Sum(x => x.NetTutar + x.KdvTutar));
        Assert.Equal(originalPaid, corrections.Sum(x => x.TedarikcidenGeriAlinacakTutar));
        Assert.Equal(originalSettlementNet - corrections.Sum(x => x.HakEdisAzaltimi), settlement.NetTutar);
        Assert.Equal(0m, settlement.NetTutar);
    }

    [Fact]
    public async Task ReceiptCorrection_RequiresActiveAdminAndEnforcesRemainingQuantityAndPendingHold()
    {
        await using var f = await Fixture.CreateAsync();
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "guards");
        var request = new TedarikciMalKabulDuzeltmeRequest("guard-key", .5m, "Sayım farkı");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service(1, admin: false).RequestReceiptCorrectionAsync(receiptId, request));
        await using (var db = f.Db())
        {
            var requester = await db.Kullanicilar.SingleAsync(x => x.AuthProviderUserId == "buyer-owner");
            requester.Durum = "Pasif";
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId, request));
        await using (var db = f.Db())
        {
            var requester = await db.Kullanicilar.SingleAsync(x => x.AuthProviderUserId == "buyer-owner");
            requester.Durum = "Aktif";
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            request with { Miktar = .1234m }));
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            request with { Miktar = 0m }));
        var pending = await f.Service(1).RequestReceiptCorrectionAsync(receiptId, request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("second-pending", .1m, "İkinci talep")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service(1, new ApprovalIdentity("second-approver"), admin: false)
            .DecideReceiptCorrectionAsync(pending.Id, new(false, "Yetkisiz")));
        var rejected = await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(pending.Id, new(false, "Kanıt yetersiz"));
        Assert.Equal("Reddedildi", rejected.Durum);
        var next = await f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("after-reject", 1m, "Tam miktar tekrar değerlendirildi"));
        Assert.Equal("OnayBekliyor", next.Durum);
        await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(next.Id, new(true, "Miktar sınırı doğrulandı"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("over-remaining", 1.001m, "Fazla")));
    }

    [Fact]
    public async Task ReceiptCorrection_InsufficientAvailableStockRollsBackDecisionAndAllEffects()
    {
        await using var f = await Fixture.CreateAsync();
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "stock-shortage");
        int correctionId;
        await using (var db = f.Db())
        {
            var otherDepot = new StokDepo { IsletmeId = 1, Ad = "Diğer depo", Kod = "DIGER", Aktif = true };
            db.StokDepolari.Add(otherDepot);
            await db.SaveChangesAsync();
            var receipt = await db.TedarikciMalKabulleri.SingleAsync(x => x.Id == receiptId);
            var stock = await db.StokHareketleri.SingleAsync(x => x.TedarikciMalKabulId == receiptId && x.IsletmeId == 1);
            db.StokHareketleri.Add(new StokHareket
            {
                IsletmeId = 1, UrunHizmetId = stock.UrunHizmetId, DepoId = stock.DepoId, SubeId = stock.SubeId,
                Miktar = -1m, HareketTipi = "Cikis", Kaynak = "Test", BirimMaliyet = stock.BirimMaliyet
            });
            // This stock is in another depot. A legacy receipt with no depot may not draw from it.
            db.StokHareketleri.Add(new StokHareket
            {
                IsletmeId = 1, UrunHizmetId = stock.UrunHizmetId, DepoId = otherDepot.Id,
                Miktar = 1m, HareketTipi = "Giris", Kaynak = "Test", BirimMaliyet = stock.BirimMaliyet
            });
            await db.SaveChangesAsync();
        }
        var pending = await f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("no-stock", .25m, "Stok yetersizliği testi"));
        correctionId = pending.Id;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(correctionId, new(true, "Onay")));
        await using var verify = f.Db();
        var correction = await verify.TedarikciMalKabulDuzeltmeleri.SingleAsync(x => x.Id == correctionId);
        Assert.Equal("OnayBekliyor", correction.Durum);
        Assert.Empty(await verify.StokHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId != null).ToListAsync());
        Assert.Empty(await verify.CariHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId != null).ToListAsync());
        Assert.Empty(await verify.PazaryeriDefterKayitlari.Where(x => x.TedarikciMalKabulDuzeltmeId != null).ToListAsync());
        Assert.Equal(1m, (await verify.TedarikciSiparisKalemleri.SingleAsync()).KabulEdilenMiktar);
    }

    [Fact]
    public async Task ReceiptCorrection_UsesLinkedDefaultDepotAndDoesNotConsumeReservedStockForLegacyNullReceiptDepot()
    {
        await using var f = await Fixture.CreateAsync();
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "default-depot");
        int defaultDepotId, reservationId;
        await using (var db = f.Db())
        {
            var depot = new StokDepo { IsletmeId = 1, Ad = "Ana depo", Kod = "ANA", Varsayilan = true, Aktif = true };
            db.StokDepolari.Add(depot);
            await db.SaveChangesAsync();
            defaultDepotId = depot.Id;
            var originalStock = await db.StokHareketleri.SingleAsync(x => x.TedarikciMalKabulId == receiptId && x.IsletmeId == 1);
            Assert.Null(originalStock.DepoId);
            var reservation = new StokHareket { IsletmeId = 1, UrunHizmetId = originalStock.UrunHizmetId,
                DepoId = defaultDepotId, RezerveMiktar = .75m, HareketTipi = "Rezervasyon", Kaynak = "Test" };
            db.StokHareketleri.Add(reservation);
            await db.SaveChangesAsync();
            reservationId = reservation.Id;
        }
        var pending = await f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("default-depot-correction", .5m, "Varsayılan depo düzeltme"));
        var approver = f.Service(1, new ApprovalIdentity("second-approver"), admin: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => approver.DecideReceiptCorrectionAsync(pending.Id,
            new(true, "Rezerve stok kullanılamaz")));
        await using (var db = f.Db())
        {
            var reservation = await db.StokHareketleri.SingleAsync(x => x.Id == reservationId);
            reservation.RezerveMiktar = 0m;
            await db.SaveChangesAsync();
        }
        await approver.DecideReceiptCorrectionAsync(pending.Id, new(true, "Serbest stok mevcut"));
        await using var verify = f.Db();
        var correctionStock = await verify.StokHareketleri.SingleAsync(x => x.TedarikciMalKabulDuzeltmeId == pending.Id);
        Assert.Null(correctionStock.DepoId);
        Assert.Equal(-.5m, correctionStock.Miktar);
        var originalStockAgain = await verify.StokHareketleri.SingleAsync(x => x.TedarikciMalKabulId == receiptId && x.IsletmeId == 1 && x.Kaynak == "PazaryeriMalKabul");
        Assert.Null(originalStockAgain.DepoId);
        Assert.Equal(.5m, (await verify.StokHareketleri.Where(x => x.IsletmeId == 1).Select(x => x.Miktar).ToListAsync()).Sum());
    }

    [Fact]
    public async Task ReceiptCorrection_PendingOrAppliedCorrectionBlocksReadyPayoutClaim()
    {
        await using var f = await Fixture.CreateAsync();
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "claim");
        int orderId;
        await using (var db = f.Db()) orderId = (await db.TedarikciMalKabulleri.SingleAsync(x => x.Id == receiptId)).TedarikciSiparisId;
        var instructionId = await EnqueueCorrectionPayoutAsync(f, orderId, "pending-payout");
        await f.Service(1).RequestReceiptCorrectionAsync(receiptId, new("claim-hold", .25m, "Bekleyen düzeltme"));
        var store = new PazaryeriParaTalimatiStore(f.Factory);
        Assert.False(await store.ClaimByIdAsync(instructionId, DateTime.UtcNow));
        var id = (await f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("claim-hold", .25m, "Bekleyen düzeltme"))).Id;
        await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(id, new(true, "Düzeltme uygulandı"));
        Assert.False(await store.ClaimByIdAsync(instructionId, DateTime.UtcNow));
        await using var verify = f.Db();
        Assert.Equal(PazaryeriParaTalimatiDurumlari.Hazir,
            (await verify.PazaryeriParaTalimatlari.SingleAsync(x => x.Id == instructionId)).Durum);
    }

    [Fact]
    public async Task ReceiptCorrection_PreservesUnknownPayoutAndAuditsRecoveryWithoutRefund()
    {
        await using var f = await Fixture.CreateAsync();
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "unknown-payout");
        int orderId;
        decimal paidBefore;
        await using (var db = f.Db())
        {
            orderId = (await db.TedarikciMalKabulleri.SingleAsync(x => x.Id == receiptId)).TedarikciSiparisId;
            paidBefore = (await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId)).OdenenTutar;
        }
        var payoutId = await EnqueueCorrectionPayoutAsync(f, orderId, "unknown-existing-transfer");
        var store = new PazaryeriParaTalimatiStore(f.Factory);
        Assert.True(await store.ClaimByIdAsync(payoutId, DateTime.UtcNow));
        await store.MarkUnknownAsync(payoutId, "provider-outcome-unknown", DateTime.UtcNow);

        var correction = await f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("unknown-correction", .5m, "Aktarım sonucu ayrıca uzlaştırılacak"));
        await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(correction.Id, new(true, "Düzeltme uygulandı"));

        await using var verify = f.Db();
        var payout = await verify.PazaryeriParaTalimatlari.SingleAsync(x => x.Id == payoutId);
        Assert.Equal(PazaryeriParaTalimatiDurumlari.SonucBekliyor, payout.Durum);
        Assert.Equal("provider-outcome-unknown", payout.SonHataKodu);
        Assert.Equal(0, await verify.PazaryeriParaTalimatlari.CountAsync(x => x.Tur == "Iade"));
        Assert.Equal(0m, (await verify.PazaryeriOdemeDagitimlari.Select(x => x.IadeTutari).ToListAsync()).Sum());
        var settlement = await verify.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.Equal(paidBefore, settlement.OdenenTutar);
        var applied = await verify.TedarikciMalKabulDuzeltmeleri.SingleAsync(x => x.Id == correction.Id);
        Assert.Equal(Math.Max(0m, settlement.OdenenTutar - settlement.NetTutar), applied.TedarikcidenGeriAlinacakTutar);
    }

    [Fact]
    public async Task ReceiptCorrection_LateProviderSuccessDuringCorrectionAuditsRecoveryAndKeepsPayoutHeld()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr,
            new("late-success-receipt", 1m, 0m, null, "Teslim alındı"));
        var gateway = new CorrectionDuringPayoutGateway(f, receipt.Id);
        await f.Service(1, new ApprovalIdentity("second-approver"), gateway: gateway, admin: true)
            .ApproveReceiptAsync(receipt.Id, new("Kabul onaylandı"));
        Assert.Equal(1, gateway.ReleaseCalls);

        await using var verify = f.Db();
        var orderId = (await verify.TedarikciMalKabulleri.SingleAsync(x => x.Id == receipt.Id)).TedarikciSiparisId;
        var settlement = await verify.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.True(settlement.OdenenTutar > settlement.NetTutar);
        Assert.Equal("DuzeltmeIncelemesi", settlement.Durum);
        Assert.Null(settlement.TamamlandiAt);
        var correction = await verify.TedarikciMalKabulDuzeltmeleri.SingleAsync();
        Assert.Equal("Uygulandi", correction.Durum);
        Assert.Equal(settlement.OdenenTutar - settlement.NetTutar, correction.TedarikcidenGeriAlinacakTutar);
        Assert.Equal("IncelemeBekliyor", correction.ParaDurumu);
        Assert.Single(await verify.PazaryeriParaTalimatlari.Where(x => x.Tur == PazaryeriParaTalimatiTurleri.Aktarim &&
            x.Durum == PazaryeriParaTalimatiDurumlari.Tamamlandi).ToListAsync());
        Assert.Empty(await verify.PazaryeriParaTalimatlari.Where(x => x.Tur == "Iade").ToListAsync());
        Assert.Equal(0m, (await verify.PazaryeriOdemeDagitimlari.Select(x => x.IadeTutari).ToListAsync()).Sum());
    }

    [Fact]
    public async Task ReceiptCorrection_RejectingPendingCorrectionAfterProviderSuccessRestoresCompletedPayoutState()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr,
            new("late-success-pending-correction", 1m, 0m, null, "Teslim alındı"));
        var gateway = new CorrectionDuringPayoutGateway(f, receipt.Id, approveCorrection: false);
        await f.Service(1, new ApprovalIdentity("second-approver"), gateway: gateway, admin: true)
            .ApproveReceiptAsync(receipt.Id, new("Kabul onaylandı"));
        Assert.Equal(1, gateway.ReleaseCalls);

        int orderId;
        int correctionId;
        await using (var db = f.Db())
        {
            orderId = (await db.TedarikciMalKabulleri.SingleAsync(x => x.Id == receipt.Id)).TedarikciSiparisId;
            correctionId = (await db.TedarikciMalKabulDuzeltmeleri.SingleAsync()).Id;
            var pendingSettlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
            Assert.Equal("DuzeltmeIncelemesi", pendingSettlement.Durum);
            Assert.True(pendingSettlement.OdenenTutar > 0m);
            Assert.Equal(pendingSettlement.NetTutar, pendingSettlement.OdenenTutar);
            Assert.Equal(PazaryeriSiparisDurumlari.HakEdisBekliyor,
                (await db.TedarikciSiparisleri.SingleAsync(x => x.Id == orderId)).Durum);
        }

        await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(correctionId, new(false, "Düzeltme desteklenmedi"));

        await using var verify = f.Db();
        var rejected = await verify.TedarikciMalKabulDuzeltmeleri.SingleAsync(x => x.Id == correctionId);
        Assert.Equal("Reddedildi", rejected.Durum);
        Assert.Equal("Uygulanmaz", rejected.ParaDurumu);
        var settlement = await verify.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.Equal("SerbestBirakildi", settlement.Durum);
        Assert.Equal(settlement.NetTutar, settlement.OdenenTutar);
        Assert.NotNull(settlement.TamamlandiAt);
        Assert.Equal(PazaryeriSiparisDurumlari.Tamamlandi,
            (await verify.TedarikciSiparisleri.SingleAsync(x => x.Id == orderId)).Durum);
        Assert.Single(await verify.PazaryeriParaTalimatlari.Where(x => x.Tur == PazaryeriParaTalimatiTurleri.Aktarim &&
            x.Durum == PazaryeriParaTalimatiDurumlari.Tamamlandi).ToListAsync());
        Assert.Empty(await verify.StokHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId == correctionId).ToListAsync());
        Assert.Empty(await verify.CariHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId == correctionId).ToListAsync());
        Assert.Empty(await verify.PazaryeriDefterKayitlari.Where(x => x.TedarikciMalKabulDuzeltmeId == correctionId).ToListAsync());
        Assert.Empty(await verify.PazaryeriParaTalimatlari.Where(x => x.Tur == "Iade").ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiptCorrection_VadeliCariOrderReversesStockAndCariWithoutInventingMarketplaceSettlement(bool manualInvoicePaid)
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Vadeli depo", $"correction-credit-{manualInvoicePaid}",
            new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 2m) }, Vadeli: true));
        int orderId;
        int lineId;
        await using (var db = f.Db())
        {
            orderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
            db.Kullanicilar.Add(new Kullanici
            {
                Id = 901, AuthProviderUserId = "second-approver", Durum = "Aktif"
            });
            await db.SaveChangesAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(orderId,
            new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(orderId,
            new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(orderId, new(
            "Tedarikçi aracı", null, $"IRS-CORRECTION-CREDIT-{manualInvoicePaid}", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 2m, 1, null, null, null, null) }));
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Etiketler[0].QrIcerigi,
            new($"credit-receipt-{manualInvoicePaid}", 2m, 0m, null, "İki adet teslim alındı"));
        Assert.Equal("Onaylandi", receipt.OnayDurumu);

        int buyerInvoiceId;
        object[] originalInvoices;
        await using (var db = f.Db())
        {
            var match = await db.TedarikciFaturaEslesmeleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
            buyerInvoiceId = match.AliciFaturaId;
            var invoices = await db.Faturalar.Where(x => x.Id == match.AliciFaturaId || x.Id == match.SaticiFaturaId)
                .OrderBy(x => x.Id).ToListAsync();
            if (manualInvoicePaid)
            {
                var buyerInvoice = invoices.Single(x => x.Id == buyerInvoiceId);
                buyerInvoice.OdenenTutar = 12m;
                db.TahsilatOdemeleri.Add(new TahsilatOdeme
                {
                    IsletmeId = buyerInvoice.IsletmeId, SubeId = buyerInvoice.SubeId, FaturaId = buyerInvoice.Id,
                    CariKartId = buyerInvoice.CariKartId, TedarikciSiparisId = orderId, TedarikciMalKabulId = receipt.Id,
                    Tip = buyerInvoice.FaturaTipi == "Alis" ? "Odeme" : "Tahsilat", Tutar = 12m,
                    ParaBirimi = buyerInvoice.ParaBirimi, KurSnapshot = buyerInvoice.KurSnapshot,
                    TryKarsiligi = 12m * buyerInvoice.KurSnapshot, OdemeYontemi = "Nakit", Aciklama = "Elle kaydedilen cari ödeme"
                });
                await db.SaveChangesAsync();
                invoices = await db.Faturalar.Where(x => x.Id == match.AliciFaturaId || x.Id == match.SaticiFaturaId)
                    .OrderBy(x => x.Id).ToListAsync();
            }
            originalInvoices = invoices.Select(x => new
            {
                x.Id, x.IsletmeId, x.CariKartId, x.FaturaTipi, x.Durum, x.YerelFaturaNo,
                x.AraToplam, x.KdvToplam, x.GenelToplam, x.OdenenTutar, x.ParaBirimi
            }).Cast<object>().ToArray();
        }

        var correction = await f.Service(1).RequestReceiptCorrectionAsync(receipt.Id,
            new($"credit-correction-{manualInvoicePaid}", 1m, "Bir adet iade düzeltmesi"));
        var result = await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(correction.Id, new(true, "Cari düzeltme ikinci kişi onayı"));
        Assert.Equal("Uygulandi", result.Durum);

        await using var verify = f.Db();
        var correctionRecord = await verify.TedarikciMalKabulDuzeltmeleri.SingleAsync(x => x.Id == correction.Id);
        Assert.Equal(0m, correctionRecord.HakEdisAzaltimi);
        Assert.Equal(0m, correctionRecord.TedarikcidenGeriAlinacakTutar);
        Assert.Equal(manualInvoicePaid ? "IncelemeBekliyor" : "Uygulanmaz", correctionRecord.ParaDurumu);
        var line = await verify.TedarikciSiparisKalemleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.Equal(1m, line.KabulEdilenMiktar);
        Assert.Equal(1m, line.ReddedilenMiktar);
        var stockReversal = await verify.StokHareketleri.SingleAsync(x => x.TedarikciMalKabulDuzeltmeId == correction.Id);
        Assert.Equal(-1m, stockReversal.Miktar);
        var correctionCari = await verify.CariHareketleri.Where(x => x.TedarikciMalKabulDuzeltmeId == correction.Id).ToListAsync();
        Assert.Equal(2, correctionCari.Count);
        Assert.Equal(new[] { "Alacak", "Borc" }, correctionCari.OrderBy(x => x.IsletmeId).Select(x => x.HareketTipi).ToArray());
        Assert.Equal(0, await verify.TedarikciHakEdisleri.CountAsync(x => x.TedarikciSiparisId == orderId));
        Assert.Empty(await verify.PazaryeriParaTalimatlari.ToListAsync());
        Assert.Empty(await verify.PazaryeriOdemeleri.ToListAsync());
        Assert.Empty(await verify.PazaryeriDefterKayitlari.ToListAsync());
        Assert.Equal(2, await verify.Faturalar.CountAsync());
        var finalInvoices = (await verify.Faturalar.OrderBy(x => x.Id).Select(x => new
        {
            x.Id, x.IsletmeId, x.CariKartId, x.FaturaTipi, x.Durum, x.YerelFaturaNo,
            x.AraToplam, x.KdvToplam, x.GenelToplam, x.OdenenTutar, x.ParaBirimi
        }).ToListAsync()).Cast<object>().ToArray();
        Assert.Equal(originalInvoices, finalInvoices);
        Assert.Equal(manualInvoicePaid ? 1 : 0,
            await verify.TahsilatOdemeleri.CountAsync(x => x.FaturaId == buyerInvoiceId));
    }

    [Fact]
    public async Task ReceiptCorrection_MixedVatLinesReverseFeesOnTheirNetTaxBases()
    {
        await using var f = await Fixture.CreateAsync();
        const int zeroVatProductId = 102;
        await using (var db = f.Db())
        {
            var vatProduct = await db.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
            vatProduct.BirimFiyat = 100m;
            vatProduct.KdvOrani = 20m;
            db.UrunHizmetleri.Add(new UrunHizmet
            {
                Id = 202, IsletmeId = 2, Tip = "Urun", Ad = "Sıfır KDV ürünü", Barkod = "MIXED-0",
                Birim = "Adet", KdvOrani = 0m, Aktif = true
            });
            db.TedarikciUrunleri.Add(new TedarikciUrun
            {
                Id = zeroVatProductId, TedarikciProfilId = 10, TedarikciIsletmeId = 2,
                KaynakUrunHizmetId = 202, Sku = "MIXED-0", Ad = "Sıfır KDV ürünü", Birim = "Adet",
                BirimFiyat = 100m, KdvOrani = 0m, ParaBirimi = "TRY", StokMiktari = 10m,
                MinimumSiparisMiktari = 1m, Aktif = true
            });
            db.Kullanicilar.Add(new Kullanici { Id = 901, AuthProviderUserId = "second-approver", Durum = "Aktif" });
            await db.SaveChangesAsync();
        }
        var master = await f.Service(1).CreateOrderAsync(new PazaryeriSiparisOlusturRequest(
            "Karışık KDV", "mixed-vat-correction-order", new[]
            {
                new PazaryeriSepetKalemiRequest(f.ProductA, 1m),
                new PazaryeriSepetKalemiRequest(zeroVatProductId, 1m)
            }));
        await f.Service(1).PayOrderAsync(master.Id, new("mixed-vat-correction-payment"));

        int orderId;
        int vatLineId;
        int zeroVatLineId;
        await using (var db = f.Db())
        {
            var lines = await db.TedarikciSiparisKalemleri.OrderBy(x => x.Id).ToListAsync();
            orderId = lines[0].TedarikciSiparisId;
            vatLineId = lines.Single(x => x.TedarikciUrunId == f.ProductA).Id;
            zeroVatLineId = lines.Single(x => x.TedarikciUrunId == zeroVatProductId).Id;
            var order = await db.TedarikciSiparisleri.SingleAsync(x => x.Id == orderId);
            Assert.Equal(220m, order.GenelToplam);
            Assert.Equal(200m, order.KomisyonMatrahi);
            Assert.Equal(18m, order.KomisyonTutari);
            Assert.Equal(3.6m, order.KomisyonKdvTutari);
            Assert.Equal(2m, order.TevkifatTutari);
            Assert.Equal(196.4m, order.TedarikciHakEdisi);
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(orderId,
            new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(orderId,
            new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(orderId, new(
            "Tedarikçi aracı", null, "IRS-MIXED-VAT", null, null, null, null, null,
            new[]
            {
                new TedarikciSevkiyatKalemiRequest(vatLineId, 1m, 1, null, null, null, null),
                new TedarikciSevkiyatKalemiRequest(zeroVatLineId, 1m, 1, null, null, null, null)
            }));
        Assert.Equal(2, shipment.Etiketler.Count);
        var vatReceipt = await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler.Single(x => x.Sku != "MIXED-0").QrIcerigi,
            new("mixed-vat-receipt-20", 1m, 0m, null, "Yüzde 20 KDV satırı"));
        var zeroVatReceipt = await f.Service(1).ReceiveShipmentQrAsync(
            shipment.Etiketler.Single(x => x.Sku == "MIXED-0").QrIcerigi,
            new("mixed-vat-receipt-0", 1m, 0m, null, "Sıfır KDV satırı"));
        Assert.Equal("Onaylandi", vatReceipt.OnayDurumu);
        Assert.Equal("Onaylandi", zeroVatReceipt.OnayDurumu);

        int transferCountBefore;
        decimal transferTotalBefore;
        decimal paidBefore;
        await using (var db = f.Db())
        {
            var settlement = await db.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
            paidBefore = settlement.OdenenTutar;
            Assert.Equal(settlement.NetTutar, paidBefore);
            Assert.Equal(196.4m, paidBefore);
            var transfers = await db.PazaryeriParaTalimatlari
                .Where(x => x.TedarikciSiparisId == orderId && x.Tur == PazaryeriParaTalimatiTurleri.Aktarim)
                .ToListAsync();
            transferCountBefore = transfers.Count;
            transferTotalBefore = transfers.Sum(x => x.Tutar);
            Assert.NotEmpty(transfers);
            Assert.All(transfers, x => Assert.Equal(PazaryeriParaTalimatiDurumlari.Tamamlandi, x.Durum));
        }

        var correction = await f.Service(1).RequestReceiptCorrectionAsync(vatReceipt.Id,
            new("mixed-vat-correction", 1m, "Yüzde 20 KDV satırı iade edildi"));
        await f.Service(1, new ApprovalIdentity("second-approver"), admin: true)
            .DecideReceiptCorrectionAsync(correction.Id, new(true, "Net KDV matrahı doğrulandı"));

        await using var verify = f.Db();
        var applied = await verify.TedarikciMalKabulDuzeltmeleri.SingleAsync(x => x.Id == correction.Id);
        Assert.Equal(100m, applied.NetTutar);
        Assert.Equal(120m, applied.BrutTutar);
        Assert.Equal(20m, applied.KdvTutar);
        Assert.Equal(108.2m, applied.HakEdisAzaltimi);
        Assert.Equal(108.2m, applied.TedarikcidenGeriAlinacakTutar);
        Assert.Equal("IncelemeBekliyor", applied.ParaDurumu);

        var ledger = await verify.PazaryeriDefterKayitlari
            .Where(x => x.TedarikciMalKabulDuzeltmeId == correction.Id)
            .ToDictionaryAsync(x => x.Hesap);
        Assert.Equal(5, ledger.Count);
        Assert.Equal(("Borc", 120m), (ledger["BrutSatis"].Yon, ledger["BrutSatis"].Tutar));
        Assert.Equal(("Borc", 108.2m), (ledger["TedarikciHakEdisi"].Yon, ledger["TedarikciHakEdisi"].Tutar));
        Assert.Equal(("Alacak", 9m), (ledger["Komisyon"].Yon, ledger["Komisyon"].Tutar));
        Assert.Equal(("Alacak", 1.8m), (ledger["KomisyonKdv"].Yon, ledger["KomisyonKdv"].Tutar));
        Assert.Equal(("Alacak", 1m), (ledger["Tevkifat"].Yon, ledger["Tevkifat"].Tutar));

        var settlementAfter = await verify.TedarikciHakEdisleri.SingleAsync(x => x.TedarikciSiparisId == orderId);
        Assert.Equal(88.2m, settlementAfter.NetTutar);
        Assert.Equal(paidBefore, settlementAfter.OdenenTutar);
        Assert.Equal(transferCountBefore, await verify.PazaryeriParaTalimatlari.CountAsync(x =>
            x.TedarikciSiparisId == orderId && x.Tur == PazaryeriParaTalimatiTurleri.Aktarim));
        Assert.Equal(transferTotalBefore, (await verify.PazaryeriParaTalimatlari
            .Where(x => x.TedarikciSiparisId == orderId && x.Tur == PazaryeriParaTalimatiTurleri.Aktarim)
            .Select(x => x.Tutar).ToListAsync()).Sum());
        Assert.Empty(await verify.PazaryeriParaTalimatlari.Where(x => x.Tur == "Iade").ToListAsync());
        Assert.Equal(0m, (await verify.PazaryeriOdemeDagitimlari
            .Where(x => x.TedarikciSiparisId == orderId).Select(x => x.IadeTutari).ToListAsync()).Sum());
    }

    private static async Task<long> EnqueueCorrectionPayoutAsync(Fixture f, int orderId, string key)
    {
        await using var db = f.Db();
        await using var tx = await db.Database.BeginTransactionAsync();
        var instruction = await PazaryeriParaTalimatiStore.EnqueueAsync(db,
            new(1, orderId, null, PazaryeriParaTalimatiTurleri.Aktarim, "CorrectionTest", key, key, 100m, "TRY"));
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return instruction.Id;
    }

    [Fact]
    public async Task ReceiptCorrection_ConcurrentRequestsForOneOrderCreateOnePendingCorrection()
    {
        await using var f = await Fixture.CreateAsync(separateConnections: true);
        await VerifyConcurrentCorrectionRequestsAsync(f);
    }

    [PostgreSqlFact]
    public async Task PostgreSql_ConcurrentRequestsForOneOrderCreateOnePendingCorrection()
    {
        await using var f = await Fixture.CreatePostgresAsync();
        await VerifyConcurrentCorrectionRequestsAsync(f);
    }

    [Fact]
    public async Task ReceiptCorrection_ConcurrentIdenticalApprovalsApplyStockAndFinanceOnce()
    {
        await using var f = await Fixture.CreateAsync(separateConnections: true);
        await VerifyConcurrentCorrectionDecisionsAsync(f);
    }

    [PostgreSqlFact]
    public async Task PostgreSql_ConcurrentIdenticalApprovalsApplyStockAndFinanceOnce()
    {
        await using var f = await Fixture.CreatePostgresAsync();
        await VerifyConcurrentCorrectionDecisionsAsync(f);
    }

    private static async Task VerifyConcurrentCorrectionDecisionsAsync(Fixture f)
    {
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "decision-race");
        var correction = await f.Service(1).RequestReceiptCorrectionAsync(receiptId,
            new("decision-race-key", .5m, "Concurrent decision test"));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new TedarikciMalKabulDuzeltmeKararRequest(true, "İki aynı yönetici kararı");
        var firstService = f.Service(1, new ApprovalIdentity("second-approver"), admin: true);
        var secondService = f.Service(1, new ApprovalIdentity("second-approver"), admin: true);
        var attempts = new[]
        {
            RunConcurrentAttemptAsync(start.Task, () => firstService.DecideReceiptCorrectionAsync(correction.Id, request)),
            RunConcurrentAttemptAsync(start.Task, () => secondService.DecideReceiptCorrectionAsync(correction.Id, request))
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);
        Assert.Contains(results, x => x.Result is { TekrarKullanildi: false });
        foreach (var error in results.Where(x => x.Error != null).Select(x => x.Error))
            Assert.True(error is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 } or
                Npgsql.PostgresException { SqlState: "40001" } or
                DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "40001" } } || IsPostgresSerializationFailure(error), error?.ToString());
        Assert.True((await firstService.DecideReceiptCorrectionAsync(correction.Id, request)).TekrarKullanildi);
        await using var db = f.Db();
        var stored = await db.TedarikciMalKabulDuzeltmeleri.SingleAsync(x => x.Id == correction.Id);
        Assert.Equal("Uygulandi", stored.Durum);
        Assert.Equal(1, await db.StokHareketleri.CountAsync(x => x.TedarikciMalKabulDuzeltmeId == correction.Id));
        Assert.Equal(2, await db.CariHareketleri.CountAsync(x => x.TedarikciMalKabulDuzeltmeId == correction.Id));
        var ledger = await db.PazaryeriDefterKayitlari.Where(x => x.TedarikciMalKabulDuzeltmeId == correction.Id).ToListAsync();
        Assert.Equal(5, ledger.Count);
        Assert.Equal(5, ledger.Select(x => x.Hesap).Distinct().Count());
        Assert.All(ledger, x => Assert.Equal(
            x.Hesap is "BrutSatis" or "TedarikciHakEdisi" ? "Borc" : "Alacak", x.Yon));
    }

    private sealed class CorrectionDuringPayoutGateway(Fixture fixture, int receiptId, bool approveCorrection = true) : IMarketplacePaymentGateway
    {
        private readonly FakeMarketplacePaymentGateway _fake = new();
        public string Name => _fake.Name;
        public bool IsConfigured => true;
        public int ReleaseCalls { get; private set; }

        public Task<MarketplacePaymentResult> CollectAsync(MarketplacePaymentCommand command, CancellationToken ct = default) =>
            _fake.CollectAsync(command, ct);

        public Task<MarketplacePaymentResult> RefundAsync(string providerTransactionId, decimal amount,
            string currency, string idempotencyKey, CancellationToken ct = default) =>
            _fake.RefundAsync(providerTransactionId, amount, currency, idempotencyKey, ct);

        public async Task<MarketplacePaymentResult> ReleaseAsync(MarketplacePayoutCommand command, CancellationToken ct = default)
        {
            ReleaseCalls++;
            var correction = await fixture.Service(1).RequestReceiptCorrectionAsync(receiptId,
                new("late-success-correction", .5m, "Tedarikçi aktarımı sırasında girilen düzeltme"), ct);
            if (approveCorrection)
                await fixture.Service(1, new ApprovalIdentity("second-approver"), admin: true)
                    .DecideReceiptCorrectionAsync(correction.Id, new(true, "Düzeltme onaylandı"), ct);
            return new(Name, "provider-late-success", true, IsFinal: true);
        }
    }

    private static async Task VerifyConcurrentCorrectionRequestsAsync(Fixture f)
    {
        var receiptId = await ReceiveAndApproveForCorrectionAsync(f, "request-race");
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service1 = f.Service(1);
        var service2 = f.Service(1);
        var attempts = new[]
        {
            RunConcurrentAttemptAsync(start.Task, () => service1.RequestReceiptCorrectionAsync(receiptId,
                new("race-one", .4m, "Concurrent request one"))),
            RunConcurrentAttemptAsync(start.Task, () => service2.RequestReceiptCorrectionAsync(receiptId,
                new("race-two", .4m, "Concurrent request two")))
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);
        Assert.Single(results, x => x.Result != null);
        foreach (var error in results.Where(x => x.Error != null).Select(x => x.Error))
            Assert.True(error is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 } or
                Npgsql.PostgresException { SqlState: "40001" } or
                InvalidOperationException or
                DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "40001" } }, error?.ToString());
        await using var db = f.Db();
        Assert.Single(await db.TedarikciMalKabulDuzeltmeleri.Where(x => x.TedarikciMalKabulId == receiptId).ToListAsync());
    }
}
