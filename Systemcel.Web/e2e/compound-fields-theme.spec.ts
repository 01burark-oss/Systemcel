import { test, expect } from "@playwright/test";
import { readFile } from "node:fs/promises";

for (const theme of ["light", "dark"]) {
  test(`brand tiles and compound fields stay unified in ${theme}`, async ({ page }, testInfo) => {
    await page.setContent(`<html data-theme="${theme}"><head><meta name="viewport" content="width=device-width, initial-scale=1"></head><body class="react-shell__body"><div class="react-sidebar__brand-mark"><i></i><i></i><i></i><i></i></div>
      <main class="pos-page"><div class="pos-search"><input aria-label="Ürün ara" placeholder="Ürün adı veya barkod ara"></div></main>
      ${["stock-search", "stock-input-icon", "invoice-search", "payment-search", "cari-input-icon", "chat-center__search"].map(name => `<label class="${name}"><input aria-label="${name}" placeholder="Ara"></label>`).join("")}
      <form class="accountant-filter-search"><label><input aria-label="Muhasebeci ara" placeholder="Muhasebeci ara"></label></form>
      <button class="systemcel-user-button"><span class="systemcel-user-button__text"><strong>Örnek Kullanıcı</strong><small>@kullanici</small></span><svg class="systemcel-user-button__chevron" width="16" height="16"><path d="m4 10 4-4 4 4" fill="none" stroke="currentColor" /></svg></button>
    </body></html>`);
    for (const file of ["styles.css", "app-theme.css", "screens/urun-stok/hizli-satis.css"]) {
      await page.addStyleTag({ content: await readFile(new URL(`../src/${file}`, import.meta.url), "utf8") });
    }
    const tiles = page.locator(".react-sidebar__brand-mark i");
    const muted = await page.locator("body").evaluate(el => getComputedStyle(el).getPropertyValue("--app-muted").trim());
    const color = await page.locator(".systemcel-user-button__text small").evaluate(el => {
      const target = document.createElement("span"); target.style.color = "var(--app-muted)"; el.append(target);
      const expected = getComputedStyle(target).color; target.remove(); return expected;
    });
    expect(muted).not.toBe("");
    await expect(page.locator(".systemcel-user-button__text small")).toHaveCSS("color", color);
    await expect(page.locator(".systemcel-user-button__chevron")).toHaveCSS("color", color);
    await expect(tiles).toHaveCount(4);
    for (const tile of await tiles.all()) {
      const box = await tile.boundingBox();
      expect(box!.width).toBeGreaterThan(10);
      expect(box!.height).toBeGreaterThan(10);
    }
    for (const field of await page.locator("input").all()) {
      await expect(field).toHaveCSS("background-color", "rgba(0, 0, 0, 0)");
      await expect(field).toHaveCSS("border-top-width", "0px");
    }
    await page.screenshot({ path: testInfo.outputPath(`fields-${theme}.png`) });
  });
}
