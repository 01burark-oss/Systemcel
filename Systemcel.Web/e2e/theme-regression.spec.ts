import { expect, test, type Locator, type Page } from "@playwright/test";

test("non-brand application states never fall back to the legacy blue palette", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Deterministic CSS regression project");
  await loadThemeFixture(page);

  const audited = page.locator("[data-theme-audit]");
  await expect(audited).toHaveCount(11);
  await expectNoBlue(audited);

  for (const selector of [
    ".accountant-card__action",
    ".accountant-toolbar button",
    ".settings-btn--green",
    ".settings-btn--navy:not(:disabled)",
    ".gib-btn--primary",
    ".gib-btn:not(.gib-btn--primary)"
  ]) {
    await page.locator(selector).hover();
    await expectNoBlue(page.locator(selector));
  }

  for (const selector of [".accountant-form-grid input", ".settings-field input", ".gib-field input"]) {
    await page.locator(selector).focus();
    await expectNoBlue(page.locator(selector));
  }

  await expectNoBlue(page.locator("button:disabled"));

  const telegramBrand = page.locator(".telegram-btn--primary");
  await expect(telegramBrand).toHaveCSS("background-color", "rgb(34, 158, 217)");
});

async function loadThemeFixture(page: Page) {
  await page.setContent(`
    <!doctype html>
    <html data-theme="light">
      <head></head>
      <body>
        <section class="accountant-panel">
          <button class="accountant-card__action" data-theme-audit>İncele</button>
          <div class="accountant-toolbar"><button data-theme-audit>Filtrele</button></div>
          <div class="accountant-form-grid"><input data-theme-audit aria-label="Muhasebeci alanı" /></div>
        </section>
        <section class="settings-page">
          <button class="settings-btn settings-btn--green" data-theme-audit>Kaydet</button>
          <button class="settings-btn settings-btn--navy" data-theme-audit><span class="spin">Yükleniyor</span></button>
          <button class="settings-btn settings-btn--navy" data-theme-audit disabled>Bekle</button>
          <label class="settings-field"><input data-theme-audit aria-label="Ayar alanı" /></label>
        </section>
        <section class="gib-page">
          <button class="gib-btn gib-btn--primary" data-theme-audit>Bağlantıyı doğrula</button>
          <button class="gib-btn" data-theme-audit>Temizle</button>
          <label class="gib-field"><input data-theme-audit aria-label="GİB alanı" /></label>
          <article class="gib-status-card"><span class="blue" data-theme-audit>Durum</span></article>
        </section>
        <section class="telegram-page">
          <button class="telegram-btn telegram-btn--primary">Telegram'a bağlan</button>
        </section>
      </body>
    </html>
  `);
  await page.addStyleTag({ path: "src/styles.css" });
  await page.addStyleTag({ path: "src/app-theme.css" });
  await expect(page.locator(".settings-btn--green")).toHaveCSS("background-color", "rgb(200, 255, 0)");
}

async function expectNoBlue(locator: Locator) {
  const styles = await locator.evaluateAll((elements) => elements.map((element) => {
    const computed = getComputedStyle(element);
    return {
      element: `${element.tagName.toLowerCase()}.${element.className}`,
      values: [computed.color, computed.backgroundColor, computed.borderTopColor, computed.outlineColor]
    };
  }));

  for (const style of styles) {
    for (const value of style.values) {
      expect(isBlueDominant(value), `${style.element} resolved to legacy blue: ${value}`).toBe(false);
    }
  }
}

function isBlueDominant(value: string) {
  const channels = value.match(/[\d.]+/g)?.slice(0, 3).map(Number);
  if (!channels || channels.length < 3)
    return false;
  const [red, green, blue] = channels;
  return blue - red > 30 && blue - green > 20;
}
