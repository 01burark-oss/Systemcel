import { expect, test, type Route } from "@playwright/test";

test.describe("notification delivery operations", () => {
  test.beforeEach(async ({ page }) => {
    await page.route("**/hubs/muhasebeci-sohbet/**", (route) => route.abort());
    await page.route("**/api/**", (route) => mockApi(route));
  });

  for (const theme of ["light", "dark"]) {
    test(`shows failed deliveries and retries them in ${theme}`, async ({ page }, testInfo) => {
      test.skip(testInfo.project.name !== "desktop-chromium");
      await page.addInitScript((value) => localStorage.setItem("systemcel.theme", value), theme);
      await page.goto("/app/yonetim/bildirim-teslimleri");
      await expect(page.getByRole("main").getByRole("heading", { name: "Bildirim teslimleri" })).toBeVisible();
      await expect(page.getByText("smtp_unavailable")).toBeVisible();
      await expect(page.locator(".admin-table-wrap")).toBeVisible();
      await page.getByRole("button", { name: "Reddet" }).click();
      expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(page.viewportSize()!.width + 1);
      await page.screenshot({ path: testInfo.outputPath(`delivery-${theme}.png`), fullPage: true });

      await page.getByRole("button", { name: "Yeniden dene" }).click();
      await expect(page.getByRole("status")).toHaveText("Bildirim yeniden gönderim sırasına alındı.");
      await expect(page.getByText("Başarısız bildirim teslimi yok.")).toBeVisible();
    });

    test(`keeps the operations table contained at narrow width in ${theme}`, async ({ page }, testInfo) => {
      test.skip(testInfo.project.name !== "desktop-chromium");
      await page.setViewportSize({ width: 780, height: 800 });
      await page.addInitScript((value) => localStorage.setItem("systemcel.theme", value), theme);
      await page.goto("/app/yonetim/bildirim-teslimleri");
      await expect(page.getByText("smtp_unavailable")).toBeVisible();
      await page.getByRole("button", { name: "Reddet" }).click();
      expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(781);
      await page.screenshot({ path: testInfo.outputPath(`delivery-narrow-${theme}.png`), fullPage: true });
    });
  }
});

async function mockApi(route: Route) {
  const path = new URL(route.request().url()).pathname;
  if (path === "/api/public/config") return json(route, { clerk: { enabled: false } });
  if (path === "/api/public/planlar") return json(route, []);
  if (path === "/api/ekran/ust-bar") return json(route, {
    aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
    muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "Tam",
    bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
    telegramAktif: false, yoneticiMi: true, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
  });
  if (path === "/api/ekran/kolay-kurulum") return json(route, {
    tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
    isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
  });
  if (path === "/api/ekran/sohbetler") return json(route, { sohbetler: [], okunmamisMesajSayisi: 0 });
  if (path === "/api/ekran/yonetim/bildirim-teslimleri") return json(route, [{
    id: 17, isletmeId: 42, kanal: "Eposta", durum: "DeadLetter", denemeSayisi: 5,
    sonHataKodu: "smtp_unavailable", updatedAt: "2026-09-23T08:00:00Z"
  }]);
  if (path === "/api/ekran/yonetim/bildirim-teslimleri/17/yeniden-dene") return json(route, null, 200);
  return json(route, { mesaj: `Unexpected route: ${path}` }, 404);
}

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
