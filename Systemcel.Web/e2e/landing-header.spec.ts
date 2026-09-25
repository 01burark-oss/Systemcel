import { expect, test } from "@playwright/test";

test.describe("landing header", () => {
  test("keeps desktop navigation labels on one line", async ({ page }) => {
    await page.setViewportSize({ width: 1707, height: 900 });
    await page.goto("/");

    const navigation = page.getByRole("navigation", { name: "Ana menü" });
    const labels = navigation.locator(".marketing-nav__links a");

    for (const width of [1707, 1366, 1261]) {
      await page.setViewportSize({ width, height: 900 });
      await expect(labels).toHaveCount(5);
      await expect(labels.first()).toBeVisible();
      expect(await labels.evaluateAll((items) => items.map((item) => {
        const range = document.createRange();
        range.selectNodeContents(item);
        return range.getClientRects().length;
      }))).toEqual([1, 1, 1, 1, 1]);
      expect(await navigation.evaluate((element) => element.scrollWidth <= element.clientWidth)).toBe(true);
    }
  });

  test("uses the compact menu before desktop navigation becomes cramped", async ({ page }) => {
    await page.setViewportSize({ width: 1200, height: 800 });
    await page.goto("/");

    const navigation = page.getByRole("navigation", { name: "Ana menü" });
    await expect(navigation.locator(".marketing-nav__links")).toBeHidden();
    await expect(navigation.getByRole("button", { name: "Menü" })).toBeVisible();
  });

  test("offers separate sign-in and sign-up actions", async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 800 });
    await page.goto("/");

    const actions = page.locator(".marketing-nav__actions");
    await expect(actions.getByRole("link", { name: "Giriş yap", exact: true })).toHaveAttribute("href", "/giris");
    await expect(actions.getByRole("link", { name: "Kayıt ol", exact: true })).toHaveAttribute("href", "/kayit");
    expect(await actions.evaluate((element) => element.scrollWidth <= element.clientWidth)).toBe(true);
  });

  test("fits the smallest supported mobile viewport", async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 568 });
    await page.goto("/");

    const navigation = page.getByRole("navigation", { name: "Ana menü" });
    await expect(navigation.locator(".marketing-nav__links")).toBeHidden();
    await expect(navigation.getByRole("button", { name: "Menü" })).toBeVisible();
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
  });

  test("scrolls footer product links to the requested section", async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto("/");

    const pageScroller = page.locator(".marketing-page");
    await pageScroller.evaluate((element) => element.scrollTo(0, element.scrollHeight));
    await page.locator(".marketing-footer").getByRole("link", { name: "Ön muhasebe" }).click();

    await expect(page).toHaveURL(/#on-muhasebe$/);
    await expect.poll(async () => page.locator("#on-muhasebe").evaluate((element) => Math.round(element.getBoundingClientRect().top))).toBeLessThan(130);
  });

  test("starts footer destination pages at the top", async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto("/");

    const pageScroller = page.locator(".marketing-page");
    await pageScroller.evaluate((element) => element.scrollTo(0, element.scrollHeight));
    await page.locator(".marketing-footer").getByRole("link", { name: "Hakkımızda" }).click();

    await expect(page).toHaveURL(/\/hakkimizda$/);
    await expect(page.getByRole("heading", { name: "Hakkımızda" })).toBeVisible();
    await expect.poll(async () => page.locator(".marketing-page").evaluate((element) => Math.round(element.scrollTop))).toBe(0);
  });

  test("uses the approved hero and supplier marketplace headings", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { level: 1 })).toHaveText("İşletmeninfinansal ihtiyaçları.Hepsi tek yerde.");
    const marketplaceHeading = page.locator("#pazaryeri h2");
    await expect(marketplaceHeading).toHaveText("Tek sepet.Birden fazla tedarikçi.");
    await expect(marketplaceHeading.locator("br")).toHaveCount(1);
  });

  test("opens the supplier catalog without asking visitors to sign in", async ({ page }) => {
    await page.route("**/api/public/tedarikci-pazaryeri/urunler", async (route) => {
      await route.fulfill({ json: { urunler: [{ id: 1, ad: "Çelik boru", aciklama: "Galvanizli", kategori: "Tesisat", birim: "Adet", birimFiyat: 125, kdvOrani: 20, paraBirimi: "TRY", tedarikciUnvani: "Örnek Tedarikçi", tedarikciSehri: "İstanbul", tahminiTeslimatGun: 2 }] } });
    });
    await page.goto("/");
    await page.getByRole("link", { name: "Tedarikçi pazaryerine git" }).click();

    await expect(page).toHaveURL(/\/pazaryeri$/);
    await expect(page.getByRole("heading", { name: "Tedarikçi pazaryeri" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Çelik boru" })).toBeVisible();
    await page.getByRole("searchbox", { name: "Ürün ara" }).fill("bulunmayan");
    await expect(page.getByText("Aramanıza uygun ürün bulunamadı.")).toBeVisible();
    await page.getByRole("searchbox", { name: "Ürün ara" }).fill("");
    await expect(page.getByRole("link", { name: "Sipariş ver" })).toHaveAttribute("href", "/giris?returnUrl=%2Fapp%2Ftedarikci-pazaryeri");
  });
});
