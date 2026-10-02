import { expect, test } from "@playwright/test";
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

test("public subscription terms explain prices and refunds across themes and widths", async ({ browser }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Public subscription terms visual contract");

  const screenshotDir = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    "../../artifacts/paytr-live-preparation-20261002"
  );
  await fs.mkdir(screenshotDir, { recursive: true });

  for (const theme of ["light", "dark"] as const) {
    for (const width of [1366, 320]) {
      const context = await browser.newContext({
        baseURL: "http://127.0.0.1:4173",
        viewport: { width, height: width === 1366 ? 768 : 1200 },
        colorScheme: theme,
        locale: "tr-TR",
        reducedMotion: "reduce"
      });
      await context.addInitScript(({ selectedTheme }) => {
        localStorage.setItem("systemcel.theme", selectedTheme);
        localStorage.setItem("systemcel.language", "tr");
        localStorage.setItem("systemcel.analyticsConsent", "denied");
      }, { selectedTheme: theme });

      const page = await context.newPage();
      await page.goto("/abonelik-kosullari");
      await expect(page.locator("html")).toHaveAttribute("data-theme", theme);
      await expect(page.getByRole("heading", { name: "Systemcel Abonelik, Yenileme, İptal ve İade Koşulları", level: 1 })).toBeVisible();

      await expect(page.getByText(/Yıllık toplu ödemede .* satın alınan 12 aylık dönemin tamamını kapsar\./)).toBeVisible();
      await expect(page.getByText(/lansman fiyatı ilk üç aylık dönem için geçerlidir\./)).toBeVisible();
      await expect(page.getByText(/Tek seferlik kart ödemesi otomatik yenileme başlatmaz; sonraki dönem için yeniden ödeme yapılır\./)).toBeVisible();
      await expect(page.getByText(/gerçekten ödenen yıllık toplam tutarın 1\/12’si üzerinden iade talebi oluşturulur\./)).toBeVisible();
      await expect(page.getByText(/Abonelik desteği: destek@systemcel\.app\./)).toBeVisible();

      const footer = page.locator(".marketing-footer");
      const footerTerms = footer.getByRole("link", { name: "Abonelik Koşulları", exact: true });
      await expect(footerTerms).toHaveAttribute("href", "/abonelik-kosullari");
      const contactLink = footer.getByRole("link", { name: "İletişim", exact: true });
      await expect(contactLink).toHaveAttribute("href", "/iletisim");
      await expect(contactLink).toBeVisible();

      await page.keyboard.press("Tab");
      const brandLink = page.locator(".marketing-public-header").getByRole("link", { name: "systemcel", exact: true });
      await expect(brandLink).toBeFocused();
      await expect(brandLink).toBeInViewport();
      expect(await brandLink.evaluate((element) => element.matches(":focus-visible"))).toBe(true);

      const dimensions = await page.evaluate(() => ({
        viewport: document.documentElement.clientWidth,
        document: document.documentElement.scrollWidth,
        body: document.body.scrollWidth
      }));
      expect(dimensions.document, JSON.stringify(dimensions)).toBeLessThanOrEqual(dimensions.viewport + 1);
      expect(dimensions.body, JSON.stringify(dimensions)).toBeLessThanOrEqual(dimensions.viewport + 1);

      const annualCampaignTerms = page.getByText(/Yıllık toplu ödemede/);
      await annualCampaignTerms.scrollIntoViewIfNeeded();
      await expect(annualCampaignTerms).toBeInViewport();
      await page.screenshot({
        path: path.join(screenshotDir, `terms-${theme}-${width}.png`),
        fullPage: false
      });
      await context.close();
    }
  }
});
