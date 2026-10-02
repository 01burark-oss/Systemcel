import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Browser, type Page, type Route } from "@playwright/test";
import fs from "node:fs/promises";
import path from "node:path";

const queue = [
  refund(201, "Bahar Kafe", "OnayBekliyor", { onaylanabilir: true }),
  refund(202, "Güneş Market", "OnayBekliyor", { tutar: null, onaylanabilir: true }),
  refund(203, "Mavi Atölye", "Hazir", { onaylanabilir: false, gonderilebilir: true }),
  refund(204, "Ada Ofis", "SonucBekliyor", { onaylanabilir: false, sorgulanabilir: true }),
  refund(205, "Yelken Tasarım", "IncelemeGerekli", { onaylanabilir: false }),
  refund(206, "Kuzey Yazılım", "Tamamlandi", { onaylanabilir: false })
];

test.describe("subscription cancellation refund administration", () => {
  test("queue remains accessible and contained across themes and desktop/mobile widths", async ({ browser }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium");
    const screenshotDir = path.resolve(process.cwd(), "../artifacts/subscription-refund-admin-20261002");
    await fs.mkdir(screenshotDir, { recursive: true });
    for (const theme of ["light", "dark"] as const) {
      for (const width of [1366, 320]) {
        const context = await createAdminContext(browser, theme, width);
        const page = await context.newPage();
        await installFixture(page);
        await page.goto("/app/yonetim/odemeler");
        const panel = page.getByTestId("cancellation-refund-queue");
        await expect(panel.getByRole("heading", { name: "Abonelik iptal iadeleri" })).toBeVisible();
        await expect(panel.getByText("Bahar Kafe")).toBeVisible();
        await expect(panel.getByText("Güneş Market")).toBeVisible();
        await expect(page.locator("html")).toHaveAttribute("data-theme", theme);
        const metrics = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth, listHeight: document.querySelector(".cancellation-refund__list")?.scrollHeight ?? 0 }));
        expect(metrics.document).toBeLessThanOrEqual(metrics.viewport + 1);
        expect(metrics.listHeight).toBeGreaterThan(300);
        const scrollCheck = await panel.evaluate((node) => {
          let ancestor: HTMLElement | null = node as HTMLElement;
          while (ancestor) {
            const style = getComputedStyle(ancestor);
            if ((style.overflowY === "auto" || style.overflowY === "scroll") && ancestor.scrollHeight > ancestor.clientHeight + 1) {
              ancestor.scrollTop = ancestor.scrollHeight;
              const lastItem = node.querySelector(".cancellation-refund__item:last-child")!.getBoundingClientRect();
              const viewport = ancestor.getBoundingClientRect();
              return { scrollTop: ancestor.scrollTop, lastItemBottom: lastItem.bottom, viewportBottom: viewport.bottom, tag: ancestor.tagName, className: ancestor.className };
            }
            ancestor = ancestor.parentElement;
          }
          return null;
        });
        expect(scrollCheck, "No scrollable ancestor contains the long refund list").not.toBeNull();
        expect(scrollCheck!.scrollTop).toBeGreaterThan(0);
        expect(scrollCheck!.lastItemBottom).toBeLessThanOrEqual(scrollCheck!.viewportBottom + 1);
        const actions = panel.getByRole("button", { name: "Bahar Kafe iadesini onayla" });
        await actions.focus();
        await expect(actions).toBeFocused();
        await panel.evaluate((node) => {
          let ancestor: HTMLElement | null = (node as HTMLElement).parentElement;
          while (ancestor) {
            if (ancestor.scrollTop > 0) { ancestor.scrollTop = 0; break; }
            ancestor = ancestor.parentElement;
          }
        });
        await page.screenshot({ path: path.join(screenshotDir, `refund-queue-${theme}-${width}.png`), fullPage: true });
        const accessibility = await new AxeBuilder({ page }).include("[data-testid='cancellation-refund-queue']").analyze();
        expect(accessibility.violations, accessibility.violations.map((violation) => `${violation.id}: ${violation.help}`).join("\n")).toEqual([]);
        await context.close();
      }
    }
  });

  test("approves without body, confirms test dispatch, and queries an ambiguous result", async ({ page }) => {
    test.skip(test.info().project.name !== "desktop-chromium");
    const fixture = await installFixture(page);
    await page.goto("/app/yonetim/odemeler");
    const panel = page.getByTestId("cancellation-refund-queue");
    await expect(panel.locator(".cancellation-refund__details dd", { hasText: "Tutar inceleniyor" })).toBeVisible();
    await expect(panel.getByRole("button", { name: "Güneş Market iadesini onayla" })).toHaveCount(0);
    await expect(panel.getByRole("button", { name: "Güneş Market test iadesini gönder" })).toHaveCount(0);

    await panel.getByRole("button", { name: "Bahar Kafe iadesini onayla" }).click();
    await expect(panel.getByTestId("refund-status-201")).toHaveText("Gönderime hazır");
    expect(fixture.posts.at(-1)).toEqual({ path: "/api/ekran/yonetim/abonelik-iadeleri/201/onayla", body: "" });

    await panel.getByRole("button", { name: "Bahar Kafe test iadesini gönder" }).click();
    await expect(panel.getByRole("group", { name: "Bahar Kafe için test iadesi teyidi" })).toBeVisible();
    expect(fixture.posts).toHaveLength(1);
    await panel.getByRole("button", { name: "Test iadesini gönder", exact: true }).click();
    await expect(panel.getByRole("status")).toContainText("İade kaydı güncellendi. Sonucu kontrol edin.");
    expect(fixture.posts.at(-1)).toEqual({ path: "/api/ekran/yonetim/abonelik-iadeleri/201/gonder", body: "" });

    await panel.getByRole("button", { name: "Ada Ofis iade durumunu sorgula" }).click();
    await expect(panel.getByRole("status")).toContainText("Sağlayıcı iade durumu sorgulandı.");
    expect(fixture.posts.at(-1)).toEqual({ path: "/api/ekran/yonetim/abonelik-iadeleri/204/sorgula", body: "" });
  });

  test("shows action failures and clears stale data after an admin 403", async ({ page }) => {
    test.skip(test.info().project.name !== "desktop-chromium");
    const fixture = await installFixture(page);
    await page.goto("/app/yonetim/odemeler");
    const panel = page.getByTestId("cancellation-refund-queue");
    await expect(panel.getByText("Bahar Kafe")).toBeVisible();
    fixture.failNextAction = true;
    await panel.getByRole("button", { name: "Bahar Kafe iadesini onayla" }).click();
    await expect(panel.getByRole("alert")).toHaveText("Onay kaydedilemedi.");
    fixture.forbidQueue = true;
    await panel.getByRole("button", { name: "İade kuyruğunu yenile" }).click();
    await expect(panel.getByRole("alert")).toHaveText("Yönetici yetkisi gerekli.");
    await expect(panel.getByText("Bahar Kafe")).toHaveCount(0);
  });
});

function refund(abonelikId: number, isletmeAdi: string, durum: string, overrides: Record<string, unknown> = {}) {
  return {
    abonelikId, isletmeAdi, hesapTipi: "Isletme", planKodu: "isletme_buyume_yillik",
    iptalAt: "2026-10-01T10:00:00Z", donemBitisAt: "2026-10-15T10:00:00Z", kalanAySayisi: 11,
    tutar: 11000, paraBirimi: "TRY", durum, onaylanabilir: false, gonderilebilir: false,
    sorgulanabilir: false, onayAt: null, ...overrides
  };
}

async function createAdminContext(browser: Browser, theme: "light" | "dark", width: number) {
  const context = await browser.newContext({ baseURL: "http://127.0.0.1:4173", viewport: { width, height: width === 1366 ? 768 : 800 }, colorScheme: theme, locale: "tr-TR", reducedMotion: "reduce" });
  await context.addInitScript(({ selectedTheme }) => {
    localStorage.setItem("systemcel.theme", selectedTheme);
    localStorage.setItem("systemcel.language", "tr");
    localStorage.setItem("systemcel.analyticsConsent", "denied");
  }, { selectedTheme: theme });
  return context;
}

async function installFixture(page: Page) {
  const fixture = { posts: [] as { path: string; body: string }[], failNextAction: false, forbidQueue: false, talepler: queue.map((item) => ({ ...item })) };
  await page.route("**/api/**", async (route) => respond(route, fixture));
  return fixture;
}

async function respond(route: Route, fixture: { posts: { path: string; body: string }[]; failNextAction: boolean; forbidQueue: boolean; talepler: typeof queue }) {
  const url = new URL(route.request().url());
  const path = url.pathname;
  if (path.startsWith("/api/ekran/yonetim/abonelik-iadeleri/") && route.request().method() === "POST") {
    fixture.posts.push({ path, body: route.request().postData() ?? "" });
    if (fixture.failNextAction) {
      fixture.failNextAction = false;
      return json(route, { mesaj: "Onay kaydedilemedi." }, 409);
    }
    if (path.endsWith("/onayla")) fixture.talepler[0] = { ...fixture.talepler[0], durum: "Hazir", onaylanabilir: false, gonderilebilir: true };
    if (path.endsWith("/gonder")) fixture.talepler[0] = { ...fixture.talepler[0], durum: "SonucBekliyor", gonderilebilir: false, sorgulanabilir: true };
    return json(route, {}, 204);
  }
  if (path === "/api/ekran/yonetim/abonelik-iadeleri") {
    if (fixture.forbidQueue) return json(route, { mesaj: "Yönetici yetkisi gerekli." }, 403);
    return json(route, { testIslemleriAcik: true, talepler: fixture.talepler });
  }
  const fixed: Record<string, unknown> = {
    "/api/public/config": { clerk: { enabled: false } },
    "/api/public/planlar": [],
    "/api/ekran/ust-bar": { aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme", yoneticiMi: true, muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "TamIslem", isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }], bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] }, telegramAktif: false },
    "/api/ekran/kolay-kurulum": { tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme" },
    "/api/ekran/yonetim/odemeler": { yoneticiMi: true, toplamSayisi: 0, basariliSayisi: 0, hataSayisi: 0, islenemeyenOlaySayisi: 0, islemler: [] }
  };
  return path in fixed ? json(route, fixed[path]) : json(route, { mesaj: `Fixture yanıtı yok: ${path}` }, 404);
}

async function json(route: Route, body: unknown, status = 200) {
  if (status === 204) return route.fulfill({ status });
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
