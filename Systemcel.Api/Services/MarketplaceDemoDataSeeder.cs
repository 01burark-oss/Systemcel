using CashTracker.Core.Entities;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Systemcel.Api.Services;

internal static class MarketplaceDemoDataSeeder
{
    internal const string SeedMarker = "MarketplaceDemoSeedV1";

    private static readonly DemoSupplier[] Suppliers =
    [
        new(
            "demo-marketplace-kuzey-ofis",
            "Kuzey Ofis Tedarik (Demo)",
            "Kırtasiye, Ofis",
            "İstanbul",
            "Ofislerin düzenli sarf malzemesi ihtiyaçları için hızlı sevkiyat.",
            "1111111111",
            "0111111111111111",
            "kuzey-ofis@hs.demo.kep.tr",
            "TR110000000000000000000001",
            "Ataşehir, İstanbul",
            "Deniz Kaya",
            "Şirket",
            "Türkiye geneli",
            "Açılmamış ürünler 14 gün içinde iade edilebilir.",
            8m,
            7,
            [
                new("KOF-A4-80", "A4 Fotokopi Kağıdı 80 gr", "5 paketlik koli", "Kırtasiye", "Koli", 925m, 20m, 120m, 1m, 1),
                new("KOF-TONER-85A", "Uyumlu Toner 85A", "Yüksek kapasiteli siyah toner", "Ofis", "Adet", 489m, 20m, 75m, 1m, 2),
                new("KOF-KALEM-50", "Tükenmez Kalem", "50 adet mavi kalem", "Kırtasiye", "Kutu", 340m, 20m, 90m, 1m, 1)
            ]),
        new(
            "demo-marketplace-ege-ambalaj",
            "Ege Ambalaj (Demo)",
            "Ambalaj, Kargo",
            "İzmir",
            "E-ticaret ve sevkiyat operasyonları için dayanıklı ambalaj ürünleri.",
            "2222222222",
            "0222222222222222",
            "ege-ambalaj@hs.demo.kep.tr",
            "TR220000000000000000000002",
            "Bornova, İzmir",
            "Ece Yılmaz",
            "Şirket",
            "Ege ve Marmara",
            "Standart ürünler 14 gün içinde iade edilebilir.",
            7.5m,
            5,
            [
                new("EGA-KUTU-30", "Kargo Kutusu 30x20x15", "25 adet oluklu mukavva kutu", "Ambalaj", "Paket", 575m, 20m, 80m, 1m, 2),
                new("EGA-BANT-45", "Koli Bandı 45 mm", "6 adet şeffaf koli bandı", "Ambalaj", "Paket", 219m, 20m, 150m, 1m, 1),
                new("EGA-STREC-17", "Streç Film 17 Mikron", "6 adet 300 metre streç film", "Ambalaj", "Koli", 849m, 20m, 65m, 1m, 2)
            ]),
        new(
            "demo-marketplace-marmara-hijyen",
            "Marmara Hijyen (Demo)",
            "Temizlik, Hijyen",
            "Kocaeli",
            "İşletmeler için profesyonel temizlik ve hijyen sarf malzemeleri.",
            "3333333333",
            "0333333333333333",
            "marmara-hijyen@hs.demo.kep.tr",
            "TR330000000000000000000003",
            "Gebze, Kocaeli",
            "Mert Akın",
            "Şirket",
            "Marmara ve İç Anadolu",
            "Ambalajı bozulmamış ürünler 14 gün içinde iade edilebilir.",
            9m,
            10,
            [
                new("MAH-HAVLU-12", "Z Katlı Kağıt Havlu", "12 paketlik koli", "Hijyen", "Koli", 780m, 20m, 110m, 1m, 2),
                new("MAH-SABUN-5", "Sıvı Sabun 5 L", "Profesyonel kullanım için sıvı sabun", "Hijyen", "Bidon", 265m, 20m, 95m, 2m, 1),
                new("MAH-YUZEY-5", "Yüzey Temizleyici 5 L", "Konsantre genel yüzey temizleyici", "Temizlik", "Bidon", 310m, 20m, 85m, 2m, 2)
            ]),
        new(
            "demo-marketplace-anadolu-ikram",
            "Anadolu İkram (Demo)",
            "Gıda, İkram",
            "Ankara",
            "Ofis mutfakları için toplu kahve, çay ve ikram paketleri.",
            "4444444444",
            "0444444444444444",
            "anadolu-ikram@hs.demo.kep.tr",
            "TR440000000000000000000004",
            "Yenimahalle, Ankara",
            "Aylin Demir",
            "Şirket",
            "Türkiye geneli",
            "Gıda güvenliği nedeniyle yalnız açılmamış paketler iade edilebilir.",
            8.5m,
            7,
            [
                new("ANI-KAHVE-1", "Filtre Kahve 1 kg", "Orta kavrulmuş çekirdek kahve", "Gıda", "Paket", 640m, 10m, 70m, 1m, 2),
                new("ANI-CAY-1", "Dökme Çay 1 kg", "Ofis tipi siyah çay", "Gıda", "Paket", 295m, 10m, 100m, 2m, 2),
                new("ANI-SEKER-5", "Tek Sargılı Şeker 5 kg", "Hijyenik tek sargılı küp şeker", "Gıda", "Koli", 510m, 10m, 60m, 1m, 3)
            ])
    ];

    internal static async Task<MarketplaceDemoSeedResult> SeedAsync(
        CashTrackerDbContext db,
        CancellationToken ct = default)
    {
        if (await db.AppSettings.AnyAsync(x => x.Key == SeedMarker, ct))
            return new MarketplaceDemoSeedResult(false, 0, 0);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var createdSuppliers = 0;
        var createdProducts = 0;
        var now = DateTime.UtcNow;

        foreach (var supplier in Suppliers)
        {
            var business = await db.Isletmeler.SingleOrDefaultAsync(
                x => x.ClerkOrganizationId == supplier.OrganizationId,
                ct);
            if (business is null)
            {
                business = new Isletme
                {
                    Ad = supplier.Title,
                    IsletmeTuru = "Tedarikçi",
                    Konum = supplier.City,
                    VergiMukellefiTipi = supplier.TaxStatus,
                    IsletmeOlcegi = "KOBİ",
                    TercihEdilenCalismaSekli = "Pazaryeri",
                    KolayKurulumTamamlandi = true,
                    TenantTipi = "Isletme",
                    ClerkOrganizationId = supplier.OrganizationId,
                    IsAktif = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Isletmeler.Add(business);
                await db.SaveChangesAsync(ct);
            }

            var profile = await db.TedarikciProfilleri.SingleOrDefaultAsync(x => x.IsletmeId == business.Id, ct);
            if (profile is null)
            {
                profile = new TedarikciProfil
                {
                    IsletmeId = business.Id,
                    Unvan = supplier.Title,
                    Kategoriler = supplier.Categories,
                    Sehir = supplier.City,
                    Aciklama = supplier.Description,
                    VergiNo = supplier.TaxNumber,
                    MersisNo = supplier.MersisNumber,
                    KepAdresi = supplier.KepAddress,
                    Iban = supplier.Iban,
                    Adres = supplier.Address,
                    YetkiliAdSoyad = supplier.ContactName,
                    VergiDurumu = supplier.TaxStatus,
                    SevkiyatBolgeleri = supplier.ShippingRegions,
                    IadeKosullari = supplier.ReturnPolicy,
                    PazaryeriSozlesmeVersiyonu = "demo-v1",
                    PspAltUyeIsyeriId = $"demo-{business.Id}",
                    KomisyonOrani = supplier.CommissionRate,
                    OdemeVadesiGun = supplier.SettlementDays,
                    DogrulamaDurumu = "Dogrulandi",
                    DogrulamaNotu = "Geliştirme ortamı demo tedarikçisi.",
                    DogrulandiAt = now,
                    Dogrulandi = true,
                    Yayinda = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.TedarikciProfilleri.Add(profile);
                await db.SaveChangesAsync(ct);
                createdSuppliers++;
            }

            var existingSkuRows = await db.TedarikciUrunleri
                .Where(x => x.TedarikciIsletmeId == business.Id)
                .Select(x => x.Sku)
                .ToListAsync(ct);
            var existingSkus = existingSkuRows.ToHashSet(StringComparer.Ordinal);
            foreach (var product in supplier.Products.Where(x => !existingSkus.Contains(x.Sku)))
            {
                db.TedarikciUrunleri.Add(new TedarikciUrun
                {
                    TedarikciProfilId = profile.Id,
                    TedarikciIsletmeId = business.Id,
                    Sku = product.Sku,
                    Ad = product.Name,
                    Aciklama = product.Description,
                    Kategori = product.Category,
                    Birim = product.Unit,
                    BirimFiyat = product.UnitPrice,
                    KdvOrani = product.VatRate,
                    ParaBirimi = "TRY",
                    StokMiktari = product.Stock,
                    MinimumSiparisMiktari = product.MinimumQuantity,
                    TahminiTeslimatGun = product.DeliveryDays,
                    Aktif = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                createdProducts++;
            }
        }

        db.AppSettings.Add(new AppSetting
        {
            Key = SeedMarker,
            Value = "4 suppliers, 12 products",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new MarketplaceDemoSeedResult(true, createdSuppliers, createdProducts);
    }

    private sealed record DemoSupplier(
        string OrganizationId,
        string Title,
        string Categories,
        string City,
        string Description,
        string TaxNumber,
        string MersisNumber,
        string KepAddress,
        string Iban,
        string Address,
        string ContactName,
        string TaxStatus,
        string ShippingRegions,
        string ReturnPolicy,
        decimal CommissionRate,
        int SettlementDays,
        DemoProduct[] Products);

    private sealed record DemoProduct(
        string Sku,
        string Name,
        string Description,
        string Category,
        string Unit,
        decimal UnitPrice,
        decimal VatRate,
        decimal Stock,
        decimal MinimumQuantity,
        int DeliveryDays);
}

internal sealed record MarketplaceDemoSeedResult(bool Seeded, int SupplierCount, int ProductCount);
