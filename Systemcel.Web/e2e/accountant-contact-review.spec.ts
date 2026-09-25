import { expect, test, type Page, type Route } from "@playwright/test";
import fs from "node:fs/promises";

const queueItem = {
  id: 31,
  muhasebeciAdi: "Ada Muhasebe",
  musteriAdi: "Bahar Kafe",
  mesaj: "2026 raporunda 500.000 TL gelir ve 8 çalışan var.",
  createdAt: "2026-09-24T08:00:00Z"
};

test("contact review queue renders and remains usable across themes and narrow widths", async ({ browser }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium");
  const screenshotDir = testInfo.outputPath("contact-review");
  await fs.mkdir(screenshotDir, { recursive: true });

  for (const theme of ["light", "dark"] as const) {
    for (const width of [1366, 360, 320]) {
      const context = await browser.newContext({
        baseURL: "http://127.0.0.1:4173",
        viewport: { width, height: width === 1366 ? 768 : 800 },
        colorScheme: theme,
        locale: "tr-TR",
        reducedMotion: "reduce"
      });
      await context.addInitScript((selectedTheme) => {
        localStorage.setItem("systemcel.theme", selectedTheme);
        localStorage.setItem("systemcel.language", "tr");
      }, theme);
      const page = await context.newPage();
      const fixture = new ContactReviewFixture(page);
      await fixture.install();
      await page.goto("/app/yonetim/muhasebeci-iletisim-incelemeleri");
      await expect(page.getByRole("heading", { name: "İletişim incelemeleri" })).toBeVisible();
      await expect(page.getByText(queueItem.musteriAdi)).toBeVisible();
      await expect(page.locator("html")).toHaveAttribute("data-theme", theme);
      const cookieDismiss = page.getByRole("button", { name: "Reddet", exact: true });
      if (await cookieDismiss.isVisible().catch(() => false)) await cookieDismiss.click();

      const dimensions = await page.evaluate(() => ({
        viewport: innerWidth,
        document: document.documentElement.scrollWidth,
        main: document.querySelector("main")?.getBoundingClientRect().width ?? 0,
        tableWidth: document.querySelector(".contact-review-table")?.getBoundingClientRect().width ?? 0,
        tableScroll: document.querySelector(".contact-review-table")?.scrollWidth ?? 0,
        wideNodes: [...document.querySelectorAll(".contact-review-table, .contact-review-table *")].map((node) => ({
          name: `${node.tagName}.${(node as HTMLElement).className}`,
          width: node.getBoundingClientRect().width,
          scrollWidth: (node as HTMLElement).scrollWidth,
          clientWidth: (node as HTMLElement).clientWidth
        })).filter((node) => node.scrollWidth > node.clientWidth + 1),
        actions: [...document.querySelectorAll(".admin-table__row-actions button")].map((button) => {
          const rect = button.getBoundingClientRect();
          return { left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom, width: rect.width, contentWidth: button.scrollWidth, clientWidth: button.clientWidth };
        })
      }));
      expect(dimensions.document).toBeLessThanOrEqual(dimensions.viewport + 1);
      expect(dimensions.main).toBeLessThanOrEqual(dimensions.viewport + 1);
      expect(dimensions.tableWidth).toBeLessThanOrEqual(dimensions.viewport + 1);
      expect(dimensions.tableScroll, JSON.stringify(dimensions)).toBeLessThanOrEqual(dimensions.tableWidth + 1);
      expect(dimensions.actions).toHaveLength(2);
      for (const action of dimensions.actions) {
        expect(action.left).toBeGreaterThanOrEqual(0);
        expect(action.right).toBeLessThanOrEqual(width + 1);
        expect(action.width).toBeGreaterThan(40);
        expect(action.contentWidth).toBeLessThanOrEqual(action.clientWidth + 1);
      }

      const release = page.getByRole("button", { name: "İletişim bilgisi yok" });
      const reject = page.getByRole("button", { name: "İletişim bilgisi var" });
      await release.focus();
      await expect(release).toBeFocused();
      await page.keyboard.press("Tab");
      await expect(reject).toBeFocused();
      await page.evaluate(() => {
        window.scrollTo(0, 0);
        document.querySelectorAll<HTMLElement>("*").forEach((element) => { element.scrollTop = 0; });
      });
      await page.screenshot({ path: `${screenshotDir}/${theme}-${width}-queue.png`, fullPage: true });

      if (theme === "light" && width === 1366) {
        fixture.holdNextQueueLoad();
        await release.click();
        await expect(page.getByText("İnceleme kuyruğu yükleniyor...")).toBeVisible();
        await page.screenshot({ path: `${screenshotDir}/light-1366-loading.png`, fullPage: true });
        fixture.resolveHeldQueueLoad();
        await expect(page.getByRole("status")).toHaveText("Talep inceleme için serbest bırakıldı.");
        await expect(page.getByText("İncelenecek talep yok.")).toBeVisible();
        await page.screenshot({ path: `${screenshotDir}/light-1366-empty.png`, fullPage: true });

        fixture.setGetFailure(true);
        await page.getByRole("button", { name: "Yenile" }).click();
        await expect(page.getByRole("alert")).toHaveText("İşlem tamamlanamadı. Lütfen tekrar deneyin.");
        await expect(page.getByRole("status")).toHaveCount(0);
        await page.screenshot({ path: `${screenshotDir}/light-1366-error.png`, fullPage: true });
        fixture.setGetFailure(false);
        fixture.setQueue([queueItem]);
        await page.getByRole("button", { name: "Yenile" }).click();
        await expect(page.getByText(queueItem.musteriAdi)).toBeVisible();
        await reject.click();
        await expect(page.getByRole("status")).toHaveText("Talep iletişim bilgisi nedeniyle reddedildi.");
        await expect(page.getByText("İncelenecek talep yok.")).toBeVisible();
        await page.screenshot({ path: `${screenshotDir}/light-1366-success.png`, fullPage: true });
      }
      await context.close();
    }
  }
});

class ContactReviewFixture {
  private queue: typeof queueItem[] = [queueItem];
  private failGet = false;
  private holdQueue?: { resolve: () => void; promise: Promise<void> };

  constructor(private readonly page: Page) {}

  async install() {
    await this.page.route("**/api/**", async (route) => this.respond(route));
  }

  setQueue(items: typeof queueItem[]) { this.queue = items; }
  setGetFailure(value: boolean) { this.failGet = value; }
  holdNextQueueLoad() {
    let resolve!: () => void;
    const promise = new Promise<void>((done) => { resolve = done; });
    this.holdQueue = { resolve, promise };
  }
  resolveHeldQueueLoad() { this.holdQueue?.resolve(); this.holdQueue = undefined; }

  private async respond(route: Route) {
    const url = new URL(route.request().url());
    if (route.request().method() === "POST" && url.pathname.endsWith("/karar")) {
      this.queue = [];
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({}) });
    }
    if (url.pathname === "/api/ekran/yonetim/muhasebeci-iletisim-incelemeleri") {
      if (this.holdQueue) await this.holdQueue.promise;
      if (this.failGet) return route.fulfill({ status: 500, contentType: "application/json", body: JSON.stringify({ mesaj: "İnceleme kuyruğu yüklenemedi." }) });
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(this.queue) });
    }
    const fixtures: Record<string, unknown> = {
      "/api/public/config": { clerk: { enabled: false } },
      "/api/public/planlar": [],
      "/api/ekran/ust-bar": {
        aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme", yoneticiMi: true,
        muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "TamIslem",
        isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }], bildirimVar: false, bildirimSayisi: 0,
        sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] }, telegramAktif: false, bankaMutabakatiAktif: true
      },
      "/api/ekran/kolay-kurulum": { tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme" },
      "/api/ai/durum": { kullanilabilir: false, mesaj: "" }
    };
    const body = fixtures[url.pathname];
    return route.fulfill({ status: body === undefined ? 404 : 200, contentType: "application/json", body: JSON.stringify(body ?? { mesaj: "Fixture yanıtı yok." }) });
  }
}
