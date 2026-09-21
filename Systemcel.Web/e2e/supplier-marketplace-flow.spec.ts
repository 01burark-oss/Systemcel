import { expect, test, type Page, type Route } from "@playwright/test";

test("güvenli ödeme QR mal kabulünde tedarikçiye bırakılır", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Supplier flow desktop smoke test");

  const api = await mockWorkspace(page);
  await page.addInitScript(() => window.localStorage.setItem("systemcel.analyticsConsent", "denied"));
  await page.goto("/app/tedarikci-pazaryeri");

  await page.getByRole("button", { name: "Sepete ekle" }).click();
  await page.getByRole("button", { name: "Sepet", exact: true }).click();
  await page.getByRole("textbox", { name: "Teslimat adresi" }).fill("Kadıköy, İstanbul");
  await page.getByRole("button", { name: "Güvenli öde ve sipariş ver" }).click();

  await expect(page.getByText("Sevk edildi", { exact: true })).toBeVisible();
  expect(api.createdBody).toEqual(expect.objectContaining({
    teslimatAdresi: "Kadıköy, İstanbul",
    vadeli: false,
    kalemler: [{ urunId: 10, miktar: 1 }]
  }));

  await page.getByRole("button", { name: "QR okut", exact: true }).click();
  await page.getByRole("textbox", { name: "QR kodu" }).fill("scq1_test");
  await page.getByRole("button", { name: "Etiketi bul" }).click();
  await expect(page.getByRole("heading", { name: "Filtre Kahve" })).toBeVisible();
  await page.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  await expect(page.getByText("Tamamlandı", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Ödemeyi kaydet" })).toHaveCount(0);
  expect(api.lastReceiptBody).toEqual(expect.objectContaining({
    kabulEdilenMiktar: 1,
    reddedilenMiktar: 0,
    idempotencyKey: expect.stringMatching(/^receipt-/)
  }));
});

async function mockWorkspace(page: Page) {
  let state = "";
  const api: { createdBody?: unknown; lastReceiptBody?: unknown } = {};

  await page.route("**/api/**", async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === "/api/public/config") return json(route, { clerk: { enabled: false } });
    if (path === "/api/public/planlar") return json(route, []);
    if (path === "/api/ekran/ust-bar") return json(route, {
      aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
      muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "TamIslem",
      bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
      telegramAktif: false, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
    });
    if (path === "/api/ekran/kolay-kurulum") return json(route, {
      tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
      isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
    });
    if (path === "/api/ekran/sohbetler") return json(route, { sohbetler: [], okunmamisMesajSayisi: 0 });
    if (path === "/api/ekran/tedarikci-pazaryeri" && request.method() === "GET") return json(route, marketplace(state));
    if (path === "/api/ekran/tedarikci-pazaryeri/siparisler" && request.method() === "POST") {
      api.createdBody = request.postDataJSON();
      state = "OdemeBekliyor";
      return json(route, { id: 77, mesaj: "Sipariş oluşturuldu." });
    }
    if (path === "/api/ekran/tedarikci-pazaryeri/siparisler/77/odeme" && request.method() === "POST") {
      state = "SevkEdildi";
      return json(route, { mesaj: "Ödeme güvenli biçimde alındı." });
    }
    if (path === "/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/scq1_test" && request.method() === "GET") {
      return json(route, {
        kod: "scq1_test", siparisId: 88, siparisNo: "PAZ-77-01", urunAdi: "Filtre Kahve",
        sku: "KAHVE", birim: "Adet", miktar: 1, lotNo: "", durum: "Aktif"
      });
    }
    if (path === "/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/scq1_test/mal-kabul" && request.method() === "POST") {
      api.lastReceiptBody = request.postDataJSON();
      state = "Tamamlandi";
      return json(route, { mesaj: "Mal kabul kaydedildi; tedarikçi hakedişi serbest bırakıldı." });
    }
    return json(route, { mesaj: `Unexpected route: ${path}` }, 404);
  });
  return api;
}

function marketplace(state: string) {
  const hasOrder = Boolean(state);
  return {
    aktifIsletmeId: 42, guvenliOdemeHazir: true, malKabulYetkisi: true, yonetici: false, profiller: [], talepler: [], acikTalepler: [], gelenTeklifler: [],
    profil: null, benimUrunlerim: [], kaynakUrunler: [],
    urunler: [{
      id: 10, tedarikciProfilId: 20, tedarikciUnvani: "Marmara Gıda", tedarikciSehri: "İstanbul",
      sevkiyatBolgeleri: "Türkiye", iadeKosullari: "14 gün", sku: "KAHVE", ad: "Filtre Kahve",
      aciklama: "1 kg", kategori: "Gıda", birim: "Adet", birimFiyat: 100, kdvOrani: 20,
      paraBirimi: "TRY", kullanilabilirStok: 10, minimumSiparisMiktari: 1, tahminiTeslimatGun: 2
    }],
    anaSiparisler: hasOrder ? [{ id: 77, siparisNo: "PAZ-77", teslimatAdresi: "Kadıköy, İstanbul", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", durum: state, createdAt: "2026-09-17T12:00:00Z" }] : [],
    siparisler: hasOrder ? [{ id: 88, anaSiparisId: 77, anaSiparisNo: "PAZ-77", siparisNo: "PAZ-77-01", teslimatAdresi: "Kadıköy, İstanbul", aliciIsletmeId: 42, tedarikciIsletmeId: 2, tedarikciUnvani: "Marmara Gıda", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", komisyonTutari: 0, komisyonKdvTutari: 0, tevkifatTutari: 0, odemeHizmetiBedeli: 0, tedarikciHakEdisi: 120, durum: state, kargoFirmasi: "Kargo", kargoTakipNo: "TRK-1", createdAt: "2026-09-17T12:00:00Z", malKabulVar: state === "Tamamlandi" }] : [],
    siparisKalemleri: hasOrder ? [{ id: 1, tedarikciSiparisId: 88, tedarikciUrunId: 10, sku: "KAHVE", ad: "Filtre Kahve", birim: "Adet", miktar: 1, sevkEdilenMiktar: 1, kabulEdilenMiktar: state === "Tamamlandi" ? 1 : 0, reddedilenMiktar: 0, birimFiyat: 100, kdvOrani: 20, toplamTutar: 120 }] : []
  };
}

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
