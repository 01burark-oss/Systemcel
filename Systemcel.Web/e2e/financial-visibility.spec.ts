import { expect, test, type Page, type Route } from "@playwright/test";

test("financial visibility is reachable and responsive on desktop and mobile", async ({ page }, testInfo) => {
  test.skip(
    !["desktop-chromium", "mobile"].includes(testInfo.project.name),
    "Financial visibility smoke projects"
  );

  await mockWorkspace(page);
  await page.goto("/app/finansal-gorunum");

  await expect(page.getByRole("heading", { name: "Alacakların durumu" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "13 haftalık nakit tahmini" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Müşterilerin ödeme durumu" })).toBeVisible();
  await expect(page.getByTitle("Örnek Müşteri")).toBeVisible();
  await expect(page.locator(".finance-table--projection tbody tr")).toHaveCount(13);

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(1);

  if (testInfo.project.name === "mobile") {
    await expect(page.getByRole("navigation", { name: "Mobil çalışma alanı" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Finans durumu" })).toHaveClass(/active/);
  } else {
    await expect(page.getByRole("heading", { name: "Finans durumu" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Finans Durumu" })).toBeVisible();
  }

});

async function mockWorkspace(page: Page) {
  await page.route("**/api/public/config", (route) => json(route, { clerk: { enabled: false } }));
  await page.route("**/api/ekran/ust-bar", (route) => json(route, {
    aktifIsletmeId: 42,
    aktifIsletme: "Örnek İşletme",
    hesapTipi: "Isletme",
    muhasebeciMusteriBaglami: false,
    muhasebeciAdi: "",
    muhasebeciYetkiSeviyesi: "TamIslem",
    bildirimVar: false,
    bildirimSayisi: 0,
    sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
    telegramAktif: false,
    isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
  }));
  await page.route("**/api/ekran/kolay-kurulum", (route) => json(route, {
    tamamlandi: true,
    isletmeId: 42,
    isletmeAdi: "Örnek İşletme",
    hesapTipi: "Isletme",
    isletmeTuru: "Genel",
    konum: "İstanbul",
    muhasebeciVarMi: false,
    mesaj: "",
    turler: []
  }));
  await page.route("**/api/ekran/finansal-gorunum/nakit-planlari", (route) => json(route, []));
  await page.route("**/api/ekran/finansal-gorunum?**", (route) => json(route, financeResponse()));
}

function financeResponse() {
  const weeks = Array.from({ length: 13 }, (_, index) => ({
    hafta: index + 1,
    baslangic: `2026-${index < 4 ? "09" : "10"}-${String((index * 7) % 28 + 1).padStart(2, "0")}`,
    bitis: `2026-${index < 4 ? "09" : "10"}-${String((index * 7) % 28 + 7).padStart(2, "0")}`,
    acilisBakiyesi: 10000 + index * 500,
    beklenenTahsilat: 2000,
    planlananGelir: 0,
    beklenenOdeme: 750,
    planlananGider: 750,
    netDegisim: 500,
    kapanisBakiyesi: 10500 + index * 500
  }));

  return {
    referansTarihi: "2026-08-21",
    paraBirimi: "TRY",
    kasaBakiyesi: 10000,
    acikAlacakToplami: 25000,
    vadesiGecmisAlacakToplami: 5000,
    yaslandirma: [
      { kod: "VadesiGelmedi", etiket: "Vadesi gelmedi", tutar: 20000, faturaAdedi: 4, oran: 80 },
      { kod: "Gun0_30", etiket: "1-30 gün", tutar: 5000, faturaAdedi: 1, oran: 20 }
    ],
    cariYaslandirma: [{
      cariKartId: 7,
      unvan: "Örnek Müşteri",
      toplam: 25000,
      vadesiGelmemis: 20000,
      gun1Ila30: 5000,
      gun31Ila60: 0,
      gun61Ila90: 0,
      gun91VeUzeri: 0,
      acikFaturaAdedi: 5,
      enUzunGecikmeGunu: 12,
      toplamdakiOrani: 100
    }],
    yogunlasma: { enBuyukCariOrani: 100, ilkUcCariOrani: 100, ilkBesCariOrani: 100, hhi: 10000, riskSeviyesi: "Yuksek" },
    cariRiskleri: [{
      cariKartId: 7,
      unvan: "Örnek Müşteri",
      acikAlacak: 25000,
      vadesiGecmisAlacak: 5000,
      enUzunGecikmeGunu: 12,
      acikAlacakOrani: 100,
      ortalamaOdemeSapmasiGunu: 4,
      ortancaOdemeSapmasiGunu: 4,
      ortalamaOdemeSuresiGunu: 34,
      ortancaOdemeSuresiGunu: 34,
      zamanindaOdemeOrani: 40,
      odemeAraligiOrtancasiGunu: 30,
      sonDonemDegisimiGunu: 8,
      sonDonemOrnekAdedi: 3,
      oncekiDonemOrnekAdedi: 3,
      tamamlananOdemeAdedi: 6,
      ritimDurumu: "Kotulesiyor",
      riskSeviyesi: "Yuksek"
    }],
    nakitProjeksiyonu: weeks,
    ilkNegatifHafta: null,
    veriUyarilari: []
  };
}

async function json(route: Route, body: unknown) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
}
