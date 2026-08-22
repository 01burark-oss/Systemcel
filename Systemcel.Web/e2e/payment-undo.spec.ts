import { expect, test, type Page, type Route } from "@playwright/test";

test("completed collection can be undone", async ({ page }, testInfo) => {
  test.skip(
    testInfo.project.name !== "desktop-chromium",
    "Collection undo smoke projects"
  );

  await mockWorkspace(page);
  await page.goto("/app/tahsilat-odeme");

  await expect(page.getByText("HRK-2026-00009")).toBeVisible();
  await page.getByRole("button", { name: "HRK-2026-00009 işlemleri" }).click();
  await page.getByRole("menuitem", { name: "Tahsilatı geri al" }).click();

  const dialog = page.getByRole("dialog", { name: "Tahsilat geri alınsın mı?" });
  await expect(dialog.getByText(/Faturanın kalan bakiyesi yeniden açılacak/)).toBeVisible();
  await dialog.getByRole("button", { name: "Tahsilatı geri al" }).click();

  await expect(dialog).toBeHidden();
  await expect(page.locator(".payment-table tbody td").filter({ hasText: "SAT-2026-0042" }).first()).toBeVisible();
  await expect(page.getByRole("button", { name: "Tahsil Et" })).toBeVisible();

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(1);
});

async function mockWorkspace(page: Page) {
  let undone = false;
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
  await page.route("**/api/ekran/tahsilat-odeme/9/geri-al", async (route) => {
    undone = true;
    await json(route, { mesaj: "Tahsilat geri alındı." });
  });
  await page.route("**/api/ekran/tahsilat-odeme", (route) => json(route, paymentResponse(undone)));
}

function paymentResponse(undone: boolean) {
  const completed = {
    id: 9,
    no: "HRK-2026-00009",
    tarih: "2026-08-21",
    tip: "Tahsilat",
    cariKartId: 7,
    cariUnvan: "Atlas Yazılım",
    odemeYontemi: "Havale",
    tutar: 2_500,
    durum: "Tamamlandi",
    kaynak: "TahsilatOdeme",
    aciklama: "Fatura tahsilatı"
  };
  const pending = {
    id: -42,
    no: "SAT-2026-0042",
    tarih: "2026-08-21",
    tip: "Tahsilat",
    cariKartId: 7,
    cariUnvan: "Atlas Yazılım",
    odemeYontemi: "Havale",
    tutar: 2_500,
    durum: "Bekliyor",
    kaynak: "Fatura",
    aciklama: "Bekleyen fatura"
  };

  return {
    aktifIsletme: "Örnek İşletme",
    hareketler: [undone ? pending : completed],
    cariler: [{ id: 7, unvan: "Atlas Yazılım" }],
    faturalar: undone ? [{
      id: 42,
      no: "SAT-2026-0042",
      cariKartId: 7,
      cariUnvan: "Atlas Yazılım",
      faturaTipi: "Satis",
      durum: "Kesildi",
      genelToplam: 2_500,
      odenenTutar: 0,
      kalan: 2_500,
      odemeYontemi: "Havale",
      aciklama: ""
    }] : [],
    ozet: {
      toplamTahsilat: undone ? 0 : 2_500,
      tahsilatAdedi: undone ? 0 : 1,
      toplamOdeme: 0,
      odemeAdedi: 0,
      bekleyen: undone ? 2_500 : 0,
      bekleyenAdedi: undone ? 1 : 0
    },
    islemTipleri: [{ deger: "Tahsilat", etiket: "Tahsilat" }, { deger: "Odeme", etiket: "Ödeme" }],
    odemeYontemleri: [{ deger: "Nakit", etiket: "Nakit" }, { deger: "Havale", etiket: "Havale" }],
    paraBirimleri: [{ deger: "TRY", etiket: "TL" }],
    kategoriler: [{ deger: "Genel", etiket: "Genel" }, { deger: "Fatura", etiket: "Fatura" }],
    bugun: "2026-08-22"
  };
}

async function json(route: Route, body: unknown) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
}
