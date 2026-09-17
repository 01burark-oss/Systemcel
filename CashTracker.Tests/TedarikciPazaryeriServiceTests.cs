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
        Assert.Equal(9m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductA).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(9m, await db.TedarikciUrunleri.Where(x => x.Id == f.ProductB).Select(x => x.StokMiktari).SingleAsync());
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
        await using (var db = f.Db()) { Assert.Equal(2, await db.Faturalar.CountAsync()); Assert.Equal(1, await db.StokHareketleri.CountAsync(x => x.IsletmeId == 1)); }
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
        Assert.Equal(9m, await assertDb.TedarikciUrunleri.Where(x => x.Id == f.ProductB).Select(x => x.StokMiktari).SingleAsync());
        Assert.Equal(24m, await assertDb.PazaryeriOdemeDagitimlari
            .Where(x => x.TedarikciSiparisId == cancelledOrderId).Select(x => x.IadeTutari).SingleAsync());
        Assert.Equal(0m, await assertDb.PazaryeriOdemeDagitimlari
            .Where(x => x.TedarikciSiparisId != cancelledOrderId).Select(x => x.IadeTutari).SingleAsync());
        Assert.Equal("KismiIade", await assertDb.PazaryeriOdemeleri.Select(x => x.Durum).SingleAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection; private readonly DbContextOptions<CashTrackerDbContext> _options; private readonly FakeIsletme _business = new();
        public int ProductA { get; private set; } public int ProductB { get; private set; }
        private Fixture(SqliteConnection connection, DbContextOptions<CashTrackerDbContext> options) { _connection = connection; _options = options; }
        public static async Task<Fixture> CreateAsync(decimal stockA = 10m)
        { var c = new SqliteConnection("DataSource=:memory:"); await c.OpenAsync(); var o = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(c).Options; var f = new Fixture(c, o); await using var db = f.Db(); await db.Database.EnsureCreatedAsync(); db.Isletmeler.AddRange(new Isletme { Id = 1, Ad = "Buyer" }, new Isletme { Id = 2, Ad = "Supplier A" }, new Isletme { Id = 3, Ad = "Other" }); db.TedarikciProfilleri.AddRange(new TedarikciProfil { Id = 10, IsletmeId = 2, Unvan = "A", Dogrulandi = true, Yayinda = true, KomisyonOrani = 10, VergiNo = "1", Iban = "TR1", Adres = "x", YetkiliAdSoyad = "x", PazaryeriSozlesmeVersiyonu = "1" }, new TedarikciProfil { Id = 11, IsletmeId = 3, Unvan = "B", Dogrulandi = true, Yayinda = true, KomisyonOrani = 10, VergiNo = "2", Iban = "TR2", Adres = "x", YetkiliAdSoyad = "x", PazaryeriSozlesmeVersiyonu = "1" }); db.TedarikciUrunleri.AddRange(new TedarikciUrun { Id = 100, TedarikciProfilId = 10, TedarikciIsletmeId = 2, Sku = "A", Ad = "A", BirimFiyat = 20, KdvOrani = 20, StokMiktari = stockA, MinimumSiparisMiktari = 1 }, new TedarikciUrun { Id = 101, TedarikciProfilId = 11, TedarikciIsletmeId = 3, Sku = "B", Ad = "B", BirimFiyat = 30, KdvOrani = 20, StokMiktari = 10, MinimumSiparisMiktari = 1 }); await db.SaveChangesAsync(); f.ProductA = 100; f.ProductB = 101; return f; }
        public CashTrackerDbContext Db() => new(_options); public TedarikciPazaryeriService Service(int id) { _business.Active = new Isletme { Id = id, Ad = id == 1 ? "Buyer" : id == 2 ? "Supplier A" : "Other" }; return new(new SingleDbContextFactory(_options), _business, new FakeManagement(), new FakeMarketplacePaymentGateway(), new PazaryeriOptions { KomisyonKdvOrani = 20, TevkifatOrani = 1 }); } public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
    private sealed class FakeIsletme : IIsletmeService { public Isletme Active { get; set; } = new() { Id = 1, Ad = "Buyer" }; public Task<List<Isletme>> GetAllAsync()=>Task.FromResult(new List<Isletme>{Active}); public Task<Isletme?> GetByIdAsync(int id)=>Task.FromResult<Isletme?>(id==Active.Id?Active:null); public Task<Isletme> GetActiveAsync()=>Task.FromResult(Active); public Task<int> GetActiveIdAsync()=>Task.FromResult(Active.Id); public Task<int> CreateAsync(string ad,bool makeActive=false)=>Task.FromResult(0); public Task RenameAsync(int id,string ad)=>Task.CompletedTask; public Task UpdateSetupAsync(int id,string ad,string t,string k,bool c,string? h=null,bool? m=null,MuhasebeciProfilKaydetRequest? p=null,string? v=null,string? o=null,string? s=null)=>Task.CompletedTask; public Task SetActiveAsync(int id)=>Task.CompletedTask; public Task SetActiveCustomerContextAsync(int id)=>Task.CompletedTask; public Task ClearActiveCustomerContextAsync()=>Task.CompletedTask; public Task<ActiveBusinessAccess> GetActiveAccessAsync()=>Task.FromResult(default(ActiveBusinessAccess)!); public Task DeleteAsync(int id)=>Task.CompletedTask; }
    private sealed class FakeManagement : ISystemcelYonetimService { public Task<bool> IsCurrentUserAdminAsync(CancellationToken ct=default)=>Task.FromResult(true); public Task<MuhasebeciBasvuruListeDto> GetMuhasebeciBasvurulariAsync(string? d=null,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciBasvuruListeDto)!); public Task<MuhasebeciBasvuruDto> ApproveMuhasebeciBasvurusuAsync(int i,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciBasvuruDto)!); public Task<MuhasebeciBasvuruDto> RejectMuhasebeciBasvurusuAsync(int i,MuhasebeciBasvuruRedRequest r,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciBasvuruDto)!); public Task<YonetimOdemeIncelemeDto> GetOdemeIncelemeAsync(string? d=null,bool s=false,int l=100,CancellationToken ct=default)=>Task.FromResult(default(YonetimOdemeIncelemeDto)!); public Task<MuhasebeciAktarimListeDto> GetMuhasebeciAktarimlariAsync(string d,int? i=null,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciAktarimListeDto)!); public Task<MuhasebeciAktarimOzetDto> CompleteMuhasebeciAktarimiAsync(int i,MuhasebeciAktarimTamamlaRequest r,CancellationToken ct=default)=>Task.FromResult(default(MuhasebeciAktarimOzetDto)!); public Task<DestekTalebiListeDto> GetDestekTalepleriAsync(CancellationToken ct=default)=>Task.FromResult(default(DestekTalebiListeDto)!); public Task<DestekTalebiDto> UpdateDestekTalebiAsync(int i,DestekTalebiGuncelleRequest r,CancellationToken ct=default)=>Task.FromResult(default(DestekTalebiDto)!); public Task<EntitlementOverrideResult> ApplyEntitlementOverrideAsync(int i,EntitlementOverrideRequest r,CancellationToken ct=default)=>Task.FromResult(default(EntitlementOverrideResult)!); }
}
