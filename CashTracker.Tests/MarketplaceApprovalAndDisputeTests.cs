using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed partial class TedarikciPazaryeriServiceTests
{
    private sealed class ApprovalIdentity(string reference) : ICurrentUserContext
    {
        public CurrentUserIdentity? GetCurrentUser() => new(reference, null, null, true);
    }

    private static async Task<(int OrderId, string Qr)> CreateApprovalShipmentAsync(
        Fixture f, decimal gross = 12_000m, string currency = "TRY", int labels = 1)
    {
        await using (var db = f.Db())
        {
            var product = await db.TedarikciUrunleri.SingleAsync(x => x.Id == f.ProductA);
            product.BirimFiyat = gross / 1.2m;
            product.ParaBirimi = currency;
            db.Kullanicilar.Add(new Kullanici { Id = 901, AuthProviderUserId = "second-approver", Durum = "Aktif" });
            db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = 1, KullaniciId = 901, Rol = "mal_kabul_onaylayicisi", Durum = "Aktif" });
            await db.SaveChangesAsync();
        }
        var master = await f.Service(1).CreateOrderAsync(new("Depo", "approval-order", new[] { new PazaryeriSepetKalemiRequest(f.ProductA, 1) }));
        await f.Service(1).PayOrderAsync(master.Id, new("approval-payment"));
        int orderId, lineId;
        await using (var db = f.Db())
        {
            orderId = await db.TedarikciSiparisleri.Select(x => x.Id).SingleAsync();
            lineId = await db.TedarikciSiparisKalemleri.Select(x => x.Id).SingleAsync();
        }
        await f.Service(2).UpdateSupplierOrderStateAsync(orderId, new(PazaryeriSiparisDurumlari.TedarikciOnayladi, null, null, null));
        await f.Service(2).UpdateSupplierOrderStateAsync(orderId, new(PazaryeriSiparisDurumlari.Hazirlaniyor, null, null, null));
        var shipment = await f.Service(2).CreateShipmentAsync(orderId, new("Tedarikçi aracı", null, "IRS-APPROVAL", null, null, null, null, null,
            new[] { new TedarikciSevkiyatKalemiRequest(lineId, 1, labels, null, null, null, null) }));
        Assert.Equal(labels, shipment.Etiketler.Count);
        return (orderId, shipment.Etiketler.First().QrIcerigi);
    }

    [Fact]
    public async Task ReceiptApproval_HighValueHasNoStockAccountingOrPayoutBeforeDifferentPersonApproves()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f);
        var request = new TedarikciMalKabulRequest("pending", 1, 0, null, "Teslim alındı");
        var first = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, request);
        Assert.Equal("OnayBekliyor", first.OnayDurumu);
        Assert.Equal("OnayBekliyor", (await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, request)).OnayDurumu);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).ApproveReceiptAsync(first.Id, new("Kendi onayım")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).UpdateSupplierOrderStateAsync(shipment.OrderId,
            new(PazaryeriSiparisDurumlari.TeslimEdildi, null, null, "Kısayol")));
        await using (var db = f.Db())
        {
            Assert.Empty(await db.Faturalar.ToListAsync());
            Assert.Empty(await db.StokHareketleri.Where(x => x.IsletmeId == 1).ToListAsync());
            Assert.Equal(0m, (await db.TedarikciSiparisKalemleri.SingleAsync()).KabulEdilenMiktar);
            Assert.Equal(0m, (await db.TedarikciHakEdisleri.SingleAsync()).OdenenTutar);
            Assert.Null((await db.TedarikciMalKabulleri.SingleAsync()).MuhasebelestiAt);
        }
        var second = f.Service(1, new ApprovalIdentity("second-approver"), admin: false);
        Assert.Equal("Onaylandi", (await second.ApproveReceiptAsync(first.Id, new("Belge ve miktar kontrol edildi."))).OnayDurumu);
        Assert.True((await second.ApproveReceiptAsync(first.Id, new("Tekrar"))).TekrarKullanildi);
        await using var verify = f.Db();
        var receipt = await verify.TedarikciMalKabulleri.SingleAsync();
        Assert.Equal("buyer-owner", receipt.IslemYapanKullaniciRef);
        Assert.Equal("second-approver", receipt.IkinciOnaylayanKullaniciRef);
        Assert.Equal("Belge ve miktar kontrol edildi.", receipt.IkinciOnayNotu);
        Assert.NotNull(receipt.IkinciOnayAt);
        Assert.Single(await verify.StokHareketleri.Where(x => x.IsletmeId == 1).ToListAsync());
        Assert.Equal(2, await verify.Faturalar.CountAsync());
        Assert.Equal(1m, (await verify.TedarikciSiparisKalemleri.SingleAsync()).KabulEdilenMiktar);
        Assert.True((await verify.TedarikciHakEdisleri.SingleAsync()).OdenenTutar > 0m);
    }

    [Theory]
    [InlineData(10_000, false)]
    [InlineData(10_000.01, true)]
    [InlineData(500, false)]
    public async Task ReceiptApproval_ThresholdIsStrictlyAboveTenThousand(decimal gross, bool pending)
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, gross);
        var result = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("threshold", 1, 0, null, null));
        Assert.Equal(pending ? "OnayBekliyor" : "Onaylandi", result.OnayDurumu);
    }

    [Fact]
    public async Task ReceiptApproval_SplittingHighValueOrderIntoLabelsCannotBypassSecondApproval()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, labels: 2);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("split", .5m, 0, null, null));
        Assert.Equal("OnayBekliyor", receipt.OnayDurumu);
        await using var db = f.Db();
        Assert.Empty(await db.StokHareketleri.Where(x => x.IsletmeId == 1).ToListAsync());
        Assert.Equal(0m, (await db.TedarikciHakEdisleri.SingleAsync()).OdenenTutar);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("complaint")]
    public async Task PayoutClaim_PendingApprovalOrUnresolvedComplaintBlocksExistingReceiptInstruction(string hold)
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f);
        if (hold == "pending") await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("hold", 1, 0, null, null));
        long instructionId;
        await using (var db = f.Db())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            if (hold == "complaint") db.TedarikciSiparisSikayetleri.Add(new TedarikciSiparisSikayeti
            {
                TedarikciSiparisId = shipment.OrderId, AliciIsletmeId = 1, TedarikciIsletmeId = 2,
                Durum = TedarikciSikayetDurumlari.Cozulemedi
            });
            var instruction = await PazaryeriParaTalimatiStore.EnqueueAsync(db,
                new(1, shipment.OrderId, null, PazaryeriParaTalimatiTurleri.Aktarim, "Fake", "prior-payout", "receipt:prior", 100m, "TRY"));
            await db.SaveChangesAsync();
            instructionId = instruction.Id;
            await tx.CommitAsync();
        }
        Assert.False(await new PazaryeriParaTalimatiStore(f.Factory).ClaimByIdAsync(instructionId, DateTime.UtcNow));
        await using (var verify = f.Db())
        {
            var result = await verify.PazaryeriParaTalimatlari.SingleAsync(x => x.Id == instructionId);
            Assert.Equal(PazaryeriParaTalimatiDurumlari.Hazir, result.Durum);
            Assert.Equal(0, result.DenemeSayisi);
            if (hold == "pending") (await verify.TedarikciMalKabulleri.SingleAsync()).OnayDurumu = "Onaylandi";
            else (await verify.TedarikciSiparisSikayetleri.SingleAsync()).Durum = TedarikciSikayetDurumlari.Cozuldu;
            await verify.SaveChangesAsync();
        }
        Assert.True(await new PazaryeriParaTalimatiStore(f.Factory).ClaimByIdAsync(instructionId, DateTime.UtcNow));
    }

    [Fact]
    public async Task ReceiptApproval_ConcurrentSecondApprovalsCreateFinancialAndStockEffectsOnce()
    {
        await using var f = await Fixture.CreateAsync(separateConnections: true);
        await VerifyConcurrentSecondApprovalsAsync(f);
    }

    [PostgreSqlFact]
    public async Task PostgreSql_ConcurrentSecondApprovalsCreateFinancialAndStockEffectsOnce()
    {
        await using var f = await Fixture.CreatePostgresAsync();
        await VerifyConcurrentSecondApprovalsAsync(f);
    }

    private static async Task VerifyConcurrentSecondApprovalsAsync(Fixture f)
    {
        var shipment = await CreateApprovalShipmentAsync(f);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("race", 1, 0, null, null));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = f.Service(1, new ApprovalIdentity("second-approver"), admin: false);
        var second = f.Service(1, new ApprovalIdentity("second-approver"), admin: false);
        var attempts = new[]
        {
            RunConcurrentAttemptAsync(start.Task, () => first.ApproveReceiptAsync(receipt.Id, new("İlk deneme"))),
            RunConcurrentAttemptAsync(start.Task, () => second.ApproveReceiptAsync(receipt.Id, new("İkinci deneme")))
        };
        start.SetResult();
        var results = await Task.WhenAll(attempts);
        Assert.Contains(results, x => x.Result is not null);
        foreach (var error in results.Where(x => x.Error != null).Select(x => x.Error))
            Assert.True(error is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 } or
                Npgsql.PostgresException { SqlState: "40001" } or
                DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "40001" } } || IsPostgresSerializationFailure(error), error?.ToString());
        Assert.True((await first.ApproveReceiptAsync(receipt.Id, new("Yeniden dene"))).TekrarKullanildi);
        await using var db = f.Db();
        Assert.Single(await db.StokHareketleri.Where(x => x.IsletmeId == 1).ToListAsync());
        Assert.Equal(2, await db.Faturalar.CountAsync());
        Assert.Equal(1m, (await db.TedarikciSiparisKalemleri.SingleAsync()).KabulEdilenMiktar);
        var settlement = await db.TedarikciHakEdisleri.SingleAsync();
        Assert.True(settlement.NetTutar > 0m);
        Assert.Equal(settlement.NetTutar, settlement.OdenenTutar);
    }

    [Fact]
    public async Task ReceiptApproval_InactiveUserOrOutsideAssignedBranchCannotApprove()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("scope", 1, 0, null, null));
        var approver = f.Service(1, new ApprovalIdentity("second-approver"), admin: false);
        await using (var db = f.Db())
        {
            (await db.Kullanicilar.SingleAsync(x => x.Id == 901)).Durum = "Pasif";
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => approver.ApproveReceiptAsync(receipt.Id, new("Pasif kullanıcı")));
        await using (var db = f.Db())
        {
            (await db.Kullanicilar.SingleAsync(x => x.Id == 901)).Durum = "Aktif";
            var branch = new Sube { IsletmeId = 1, Ad = "Diğer şube", Aktif = true };
            db.Subeler.Add(branch);
            await db.SaveChangesAsync();
            (await db.IsletmeUyelikleri.SingleAsync(x => x.KullaniciId == 901)).SubeId = branch.Id;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => approver.ApproveReceiptAsync(receipt.Id, new("Şube dışında")));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task ReceiptApproval_ExceptionNeedsSecondPersonEvenBelowThreshold(bool rejected, bool document, bool quantity)
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, 500m);
        var result = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("exception", rejected ? 0 : 1,
            rejected ? 1 : 0, rejected ? "Hasarlı" : null, null, BelgeUyusmazligi: document, MiktarDegisikligi: quantity));
        Assert.Equal("OnayBekliyor", result.OnayDurumu);
        await using var db = f.Db();
        Assert.Equal(0m, (await db.TedarikciHakEdisleri.SingleAsync()).OdenenTutar);
        Assert.Empty(await db.Faturalar.ToListAsync());
    }

    [Fact]
    public async Task ReceiptApproval_DifferentTenantAndWarehouseRoleCannotApprove()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("role", 1, 0, null, null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service(3, new ApprovalIdentity("second-approver"), admin: false)
            .ApproveReceiptAsync(receipt.Id, new("Başka işletme")));
        await using (var db = f.Db())
        {
            var membership = await db.IsletmeUyelikleri.SingleAsync(x => x.KullaniciId == 901);
            membership.Rol = "depo_sorumlusu";
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service(1, new ApprovalIdentity("second-approver"), admin: false)
            .ApproveReceiptAsync(receipt.Id, new("Depo rolü")));
    }

    [Fact]
    public async Task ReceiptApproval_ReplayCannotChangeDocumentOrQuantityFlags()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, 500m);
        var request = new TedarikciMalKabulRequest("flag-key", 1, 0, null, null, BelgeUyusmazligi: true);
        await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, request with { BelgeUyusmazligi = false }));
    }

    [Fact]
    public void DisputeBusinessDays_SkipWeekendAndConfiguredHolidayInIstanbul()
    {
        var friday = new DateTime(2026, 10, 2, 20, 30, 0, DateTimeKind.Utc);
        var holidays = new HashSet<DateOnly> { new(2026, 10, 5) };
        Assert.Equal(new DateTime(2026, 10, 6, 20, 30, 0, DateTimeKind.Utc), PazaryeriIsGunu.SonTarih(friday, 1, holidays));
        Assert.Equal(new DateTime(2026, 10, 7, 20, 30, 0, DateTimeKind.Utc), PazaryeriIsGunu.SonTarih(friday, 2, holidays));
    }

    [Fact]
    public async Task DisputeProgress_OverdueEscalatesOnceAndNeverDecidesWinnerOrReleasesPayout()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, 500m);
        await f.Service(1).DisputeSupplierOrderAsync(shipment.OrderId, "Teslimat belgesi incelensin.");
        var friday = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc);
        await using (var db = f.Db())
        {
            var order = await db.TedarikciSiparisleri.SingleAsync();
            order.ItirazAcildiAt = friday;
            await db.SaveChangesAsync();
        }
        var options = new PazaryeriOptions { ItirazYukseltmeKullaniciRef = "buyer-owner" };
        var service = f.Service(1, options: options);
        Assert.Equal(0, await service.EscalateOverdueDisputesAsync(new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(1, await service.EscalateOverdueDisputesAsync(new DateTime(2026, 10, 5, 10, 0, 1, DateTimeKind.Utc)));
        Assert.Equal(0, await service.EscalateOverdueDisputesAsync(new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc)));
        await service.RecordDisputeProgressAsync(shipment.OrderId, new("IlkYanit", "Taraflara ilk yanıt verildi."));
        await service.RecordDisputeProgressAsync(shipment.OrderId, new("KanitToplandi", "Teslimat belgeleri ve fotoğraflar toplandı."));
        await service.RecordDisputeProgressAsync(shipment.OrderId, new("KanitToplandi", "Tekrar"));
        await using var verify = f.Db();
        var result = await verify.TedarikciSiparisleri.SingleAsync();
        Assert.Equal(PazaryeriSiparisDurumlari.Itirazli, result.Durum);
        Assert.NotNull(result.ItirazIlkYanitAt);
        Assert.NotNull(result.ItirazKanitToplandiAt);
        Assert.Single(await verify.BildirimKayitlari.ToListAsync());
        Assert.Equal(0m, (await verify.TedarikciHakEdisleri.SingleAsync()).OdenenTutar);
        Assert.Equal("Bloke", (await verify.TedarikciHakEdisleri.SingleAsync()).Durum);
        Assert.Single(await verify.TedarikciSiparisDurumKayitlari.Where(x => x.Aciklama.Contains("İtiraz aşaması: KanitToplandi")).ToListAsync());
    }

    [Fact]
    public async Task DisputeProgress_RepeatedOpenCannotRestartDeadlineAndOrdinaryUserCannotMarkEvidence()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, 500m);
        await f.Service(1).DisputeSupplierOrderAsync(shipment.OrderId, "İlk itiraz");
        DateTime? first;
        await using (var db = f.Db()) first = (await db.TedarikciSiparisleri.SingleAsync()).ItirazAcildiAt;
        await f.Service(1).DisputeSupplierOrderAsync(shipment.OrderId, "Ek açıklama");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service(1, admin: false).RecordDisputeProgressAsync(
            shipment.OrderId, new("KanitToplandi", "İzinsiz aşama")));
        await using var verify = f.Db();
        Assert.Equal(first, (await verify.TedarikciSiparisleri.SingleAsync()).ItirazAcildiAt);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task DisputeProgress_LateCompletionStillEscalatesButOnTimeCompletionDoesNot(bool late, bool escalates)
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, 500m);
        await f.Service(1).DisputeSupplierOrderAsync(shipment.OrderId, "Kanıt incelemesi");
        var opened = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);
        await using (var db = f.Db())
        {
            var order = await db.TedarikciSiparisleri.SingleAsync();
            order.ItirazAcildiAt = opened;
            order.ItirazIlkYanitAt = opened.AddDays(1).AddSeconds(late ? 1 : 0);
            order.ItirazKanitToplandiAt = opened.AddDays(2);
            await db.SaveChangesAsync();
        }
        Assert.Equal(escalates ? 1 : 0, await f.Service(1).EscalateOverdueDisputesAsync(opened.AddDays(4)));
        await using var verify = f.Db();
        Assert.Equal(PazaryeriSiparisDurumlari.Itirazli, (await verify.TedarikciSiparisleri.SingleAsync()).Durum);
        Assert.Equal(0m, (await verify.TedarikciHakEdisleri.SingleAsync()).OdenenTutar);
    }

    [Fact]
    public async Task DisputeResolution_PreservesEarlierComplaintNoteAndRecordsManagementDecisionSeparately()
    {
        await using var f = await Fixture.CreateAsync();
        var shipment = await CreateApprovalShipmentAsync(f, 500m);
        var receipt = await f.Service(1).ReceiveShipmentQrAsync(shipment.Qr, new("history-note", 0, 1, "Hasarlı ürün", null));
        await f.Service(1, new ApprovalIdentity("second-approver"), admin: false)
            .ApproveReceiptAsync(receipt.Id, new("Hasar doğrulandı."));
        await using (var db = f.Db())
        {
            var earlierComplaint = await db.TedarikciSiparisSikayetleri.SingleAsync();
            earlierComplaint.Durum = TedarikciSikayetDurumlari.Cozulemedi;
            earlierComplaint.KapanisNotu = "Alıcı: sorun henüz çözülmedi.";
            await db.SaveChangesAsync();
        }
        await f.Service(1).ResolveDisputeAsync(shipment.OrderId,
            new(true, "Yeni teslimat kararlaştırıldı.", PazaryeriItirazKararlari.YenidenTeslim));
        await using var verify = f.Db();
        var complaint = await verify.TedarikciSiparisSikayetleri.SingleAsync();
        Assert.Equal(TedarikciSikayetDurumlari.Cozuldu, complaint.Durum);
        Assert.Equal("Alıcı: sorun henüz çözülmedi.", complaint.KapanisNotu);
        Assert.Contains(await verify.TedarikciSiparisDurumKayitlari.ToListAsync(),
            x => x.Aciklama.Contains("Yeni teslimat kararlaştırıldı."));
    }
}
