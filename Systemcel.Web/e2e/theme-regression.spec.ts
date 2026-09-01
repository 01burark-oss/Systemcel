import { expect, test, type Locator, type Page } from "@playwright/test";

for (const theme of ["light", "dark"] as const) {
  test(`${theme} application states keep the Systemcel palette and readable match score`, async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Deterministic CSS regression project");
    await loadThemeFixture(page, theme);

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
    await expectTextContrast(page.locator(".accountant-match-score"), 4.5);

    if (theme === "dark") {
      await expectNoLightIsland(page.locator("[data-theme-surface]"));
      await expect(page.locator(".billing-period-status")).toHaveCSS("color", "rgb(184, 232, 106)");
      await expect(page.locator(".billing-feedback--warning > span")).toHaveCSS("color", "rgb(242, 201, 109)");
    }

    const telegramBrand = page.locator(".telegram-btn--primary");
    await expect(telegramBrand).toHaveCSS("background-color", "rgb(34, 158, 217)");
  });
}

async function loadThemeFixture(page: Page, theme: "light" | "dark") {
  await page.setContent(`
    <!doctype html>
    <html data-theme="${theme}">
      <head></head>
      <body>
        <div class="react-shell">
          <main class="react-shell__body">
            <section class="accountant-panel">
              <button class="accountant-card__action" data-theme-audit>İncele</button>
              <div class="accountant-toolbar"><button data-theme-audit>Filtrele</button></div>
              <div class="accountant-form-grid"><input data-theme-audit aria-label="Muhasebeci alanı" /></div>
              <article class="accountant-card" data-theme-surface>
                <span class="accountant-match-score" aria-label="Eşleşme skoru yüzde 80">
                  <strong>%80</strong><small>uyum</small>
                </span>
              </article>
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
            <section class="pos-page">
              <div class="pos-summary" data-theme-surface><article><span>1</span><strong>Satış</strong></article></div>
              <section class="pos-catalog" data-theme-surface>
                <div class="pos-search" data-theme-surface><input aria-label="Ürün ara" /></div>
                <div class="pos-capture-actions"><button type="button" data-theme-surface>Fiş oku</button></div>
                <p class="pos-scan-feedback unsupported" data-theme-surface>Tarayıcı desteklemiyor.</p>
                <button class="pos-product-card" type="button" data-theme-surface>
                  <span class="pos-product-card__icon">1</span>
                  <span class="pos-product-card__body"><strong>Ürün</strong><small>Barkod</small></span>
                  <span class="pos-product-card__add">Sepete ekle</span>
                </button>
              </section>
              <section class="pos-cart" data-theme-surface>
                <div class="pos-checkout" data-theme-surface>
                  <div class="pos-payment-options"><button type="button">Nakit</button></div>
                </div>
              </section>
            </section>
            <section class="billing-page">
              <article class="billing-period-card" data-theme-surface><h2>Dönem</h2><span class="billing-period-status">Aktif dönem</span></article>
              <p class="billing-feedback billing-feedback--success"><span>Başarılı</span></p>
              <p class="billing-feedback billing-feedback--warning"><span>Uyarı</span></p>
              <p class="billing-notice billing-notice--danger" data-theme-surface>Plan süresi doldu.</p>
              <span class="billing-status billing-status--success" data-theme-surface>Aktif</span>
              <span class="billing-status billing-status--warning" data-theme-surface>Bekliyor</span>
              <p class="billing-inline-error" data-theme-surface>Plan güncellenemedi.</p>
            </section>
          </main>
        </div>
      </body>
    </html>
  `);
  await page.addStyleTag({ path: "src/styles.css" });
  await page.addStyleTag({ path: "src/app-theme.css" });
  await page.addStyleTag({ path: "src/screens/urun-stok/hizli-satis.css" });
  await page.addStyleTag({ path: "src/screens/billing/billing.css" });
  await expect(page.locator(".settings-btn--green")).toHaveCSS("background-color", "rgb(200, 255, 0)");
}

async function expectNoBlue(locator: Locator) {
  const styles = await locator.evaluateAll((elements) => elements.map((element) => {
    const computed = getComputedStyle(element);
    return {
      element: `${element.tagName.toLowerCase()}.${element.className}[${element.getAttribute("aria-label") ?? element.textContent?.trim() ?? ""}]`,
      values: {
        color: computed.color,
        backgroundColor: computed.backgroundColor,
        borderTopColor: computed.borderTopColor,
        outlineColor: computed.outlineColor
      }
    };
  }));

  for (const style of styles) {
    for (const [property, value] of Object.entries(style.values)) {
      expect(isBlueDominant(value), `${style.element} ${property} resolved to legacy blue: ${value}`).toBe(false);
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

async function expectTextContrast(locator: Locator, minimumRatio: number) {
  const colors = await locator.evaluate((element) => {
    const parse = (value: string) => {
      const channels = value.match(/[\d.]+/g)?.map(Number) ?? [];
      const scale = value.startsWith("color(srgb") ? 255 : 1;
      return {
        red: (channels[0] ?? 0) * scale,
        green: (channels[1] ?? 0) * scale,
        blue: (channels[2] ?? 0) * scale,
        alpha: channels[3] ?? 1
      };
    };
    const blend = (front: ReturnType<typeof parse>, back: ReturnType<typeof parse>) => {
      const alpha = front.alpha + back.alpha * (1 - front.alpha);
      if (alpha === 0) return { red: 0, green: 0, blue: 0, alpha: 0 };
      return {
        red: (front.red * front.alpha + back.red * back.alpha * (1 - front.alpha)) / alpha,
        green: (front.green * front.alpha + back.green * back.alpha * (1 - front.alpha)) / alpha,
        blue: (front.blue * front.alpha + back.blue * back.alpha * (1 - front.alpha)) / alpha,
        alpha
      };
    };

    const layers = [];
    for (let current: Element | null = element; current; current = current.parentElement) {
      layers.push(parse(getComputedStyle(current).backgroundColor));
    }
    let background = { red: 255, green: 255, blue: 255, alpha: 1 };
    for (let index = layers.length - 1; index >= 0; index -= 1) {
      background = blend(layers[index], background);
    }

    return { foreground: parse(getComputedStyle(element).color), background };
  });

  const ratio = contrastRatio(colors.foreground, colors.background);
  expect(ratio, `Expected text contrast >= ${minimumRatio}:1, received ${ratio.toFixed(2)}:1 (${JSON.stringify(colors)})`).toBeGreaterThanOrEqual(minimumRatio);
}

async function expectNoLightIsland(locator: Locator) {
  const surfaces = await locator.evaluateAll((elements) => elements.map((element) => ({
    element: `${element.tagName.toLowerCase()}.${element.className}`,
    background: getComputedStyle(element).backgroundColor
  })));

  for (const surface of surfaces) {
    const channels = surface.background.match(/[\d.]+/g)?.map(Number) ?? [];
    const [red = 0, green = 0, blue = 0, alpha = 1] = channels;
    const isOpaqueLightSurface = alpha >= .9 && red >= 225 && green >= 225 && blue >= 215;
    expect(isOpaqueLightSurface, `${surface.element} stayed light in dark mode: ${surface.background}`).toBe(false);
  }
}

function contrastRatio(
  foreground: { red: number; green: number; blue: number },
  background: { red: number; green: number; blue: number }
) {
  const luminance = ({ red, green, blue }: { red: number; green: number; blue: number }) => {
    const channel = (value: number) => {
      const normalized = value / 255;
      return normalized <= .04045 ? normalized / 12.92 : ((normalized + .055) / 1.055) ** 2.4;
    };
    return .2126 * channel(red) + .7152 * channel(green) + .0722 * channel(blue);
  };
  const light = Math.max(luminance(foreground), luminance(background));
  const dark = Math.min(luminance(foreground), luminance(background));
  return (light + .05) / (dark + .05);
}
