import { expect, test } from "@playwright/test";

test("public catalog shows VAT-inclusive prices and supplier terms without sign-in", async ({ page }) => {
  await page.route("**/api/public/tedarikci-pazaryeri/urunler", (route) => route.fulfill({ json: { urunler: [
    { id: 1, ad: "Galvanizli çelik boru", aciklama: "Yapı tesisatı için standart boru.", kategori: "Tesisat", birim: "Adet", birimFiyat: 125, kdvOrani: 20, paraBirimi: "TRY", tedarikciUnvani: "Marmara Malzeme", tedarikciSehri: "İstanbul", sevkiyatBolgeleri: "Türkiye", iadeKosullari: "Hasarlı ürün için talep açın.", tahminiTeslimatGun: 3 }
  ] } }));
  await page.goto("/pazaryeri");
  await expect(page.getByRole("heading", { name: "Galvanizli çelik boru" })).toBeVisible();
  await expect(page.getByText("₺150,00")).toBeVisible();
  await expect(page.getByText("KDV dahil")).toBeVisible();
  await expect(page.getByText("Sevkiyat bölgesi: Türkiye")).toBeVisible();
  await expect(page.getByText("Satıcının iade koşulları: Hasarlı ürün için talep açın.")).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Pazaryeri koşulları" }).getByRole("link")).toHaveCount(4);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
});

test("sale, delivery, return and contact pages are public", async ({ page }) => {
  for (const [path, title] of [
    ["/pazaryeri-satis-kosullari", "Pazaryeri satış koşulları"],
    ["/teslimat-ve-kargo", "Teslimat ve kargo koşulları"],
    ["/iptal-ve-iade", "İptal ve iade koşulları"]
  ]) {
    await page.goto(path);
    await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
  }
  await page.goto("/iletisim");
  await expect(page.getByRole("link", { name: "0530 065 58 88" })).toHaveAttribute("href", "tel:+905300655888");
  await expect(page.getByText(/Bağlarbaşı Mahallesi/)).toBeVisible();
});
