import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type Route } from "@playwright/test";

test.describe("workspace accessibility", () => {
  test.beforeEach(async ({ page }) => mockApi(page));

  test("settings, support and admin operations have no serious or critical axe violations", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Desktop accessibility matrix");

    const cases = [
      { path: "/app/ayarlar", ready: "Uygulama kilidi" },
      { path: "/yardim", ready: "Bir destek talebi oluştur" },
      { path: "/app/yonetim/destek", ready: "Öncelikli AŞ" },
      { path: "/app/yonetim/muhasebeci-aktarimlari", ready: "Ada Muhasebe" }
    ];

    for (const current of cases) {
      await page.goto(current.path);
      await expect(page.getByText(current.ready, { exact: true }).first()).toBeVisible();
      await expectNoSeriousOrCriticalViolations(page);
    }
  });

  test("quick sale and support reflow remain axe-clean at 320px", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "mobile-small", "320px accessibility matrix");

    await page.goto("/app/hizli-satis");
    await expect(page.getByRole("textbox", { name: "Ürün veya barkod ara" })).toBeVisible();
    await expectNoSeriousOrCriticalViolations(page);

    await page.goto("/yardim");
    await expect(page.getByRole("heading", { name: "Bir destek talebi oluştur" })).toBeVisible();
    await expectNoSeriousOrCriticalViolations(page);
  });
});

async function expectNoSeriousOrCriticalViolations(page: Page) {
  const results = await new AxeBuilder({ page })
    .include("main")
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
    .analyze();
  const violations = results.violations
    .filter(({ impact }) => impact === "serious" || impact === "critical")
    .map(({ id, impact, help, nodes }) => ({
      id,
      impact,
      help,
      targets: nodes.map((node) => node.target.join(" "))
    }));
  expect(violations).toEqual([]);
}

async function mockApi(page: Page) {
  await page.route("**/hubs/muhasebeci-sohbet/**", (route) => route.abort());
  await page.route("**/api/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === "/api/public/config") return json(route, { clerk: { enabled: false } });
    if (path === "/api/public/planlar") return json(route, []);
    if (path === "/api/ekran/ust-bar") return json(route, {
      aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
      muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "Tam",
      bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
      telegramAktif: false, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
    });
    if (path === "/api/ekran/kolay-kurulum") return json(route, {
      tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
      isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
    });
    if (path === "/api/ekran/sohbetler") return json(route, { sohbetler: [], okunmamisMesajSayisi: 0 });
    if (path === "/api/ekran/urun-stok") return json(route, {
      aktifIsletme: "Örnek İşletme",
      urunler: [{ id: 7, ad: "Filtre kahve", barkod: "869000000007", satisFiyati: 120, stokMiktari: 8, kdvOrani: 20 }],
      sonHareketler: [], tipSecenekleri: [], birimSecenekleri: []
    });
    if (path === "/api/ekran/mobil-tarama/durum") return json(route, { fisOcrHazir: false });
    if (path === "/api/ekran/destek-talepleri") return json(route, { talepler: [] });
    if (path === "/api/ekran/ayarlar") return json(route, {
      aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", seciliIsletmeId: 42, seciliKalemId: 1,
      dil: "tr", diller: [{ kod: "tr", ad: "Türkçe" }],
      isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }],
      kalemler: [{ id: 1, tip: "Gelir", ad: "Satış" }], mesaj: ""
    });
    if (path === "/api/ekran/ayarlar/pin") return json(route, { varsayilanPin: true, mesaj: "PIN kilidi hazır." });
    if (path === "/api/ekran/uyelikler") return json(route, {
      sahibiMi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme",
      uyelikler: [{ id: 1, kullaniciId: 1, eposta: "owner@example.test", adSoyad: "İşletme Sahibi", rol: "isletme_sahibi", durum: "Aktif", davetKodu: "" }]
    });
    if (path === "/api/ekran/yonetim/destek") return json(route, { talepler: [{
      id: 2, isletmeId: 12, isletmeAdi: "Öncelikli AŞ", konu: "Teknik destek", kategori: "Teknik",
      aciklama: "Rapor açılmıyor.", oncelik: "Oncelikli", durum: "Islemde", yoneticiYaniti: "İnceliyoruz.",
      createdAt: "2026-08-24T10:00:00Z", updatedAt: "2026-08-24T10:00:00Z"
    }] });
    if (path === "/api/ekran/yonetim/muhasebeci-aktarimlari") return json(route, {
      yoneticiMi: true, aktarimDonemi: "2026-08", aktarimlar: [{
        muhasebeciIsletmeId: 12, muhasebeciAdi: "Ada Muhasebe", aktarimDonemi: "2026-08",
        paraBirimi: "TRY", alacakSayisi: 2, tahsilEdilenTutar: 3000,
        platformKomisyonTutari: 300, aktarilacakTutar: 2700, durum: "Bekliyor", aktarimReferansi: ""
      }]
    });
    return json(route, { mesaj: `Unexpected route: ${path}` }, 404);
  });
}

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
