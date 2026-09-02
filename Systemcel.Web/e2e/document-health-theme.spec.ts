import { test, expect } from "@playwright/test";
import { readFile } from "node:fs/promises";

for (const mode of ["light", "dark"]) {
  test(`lime icons and switches remain legible in ${mode}`, async ({ page }) => {
    await page.setContent(`<html data-theme="${mode}"><body>
      <span class="billing-fact-icon"><svg width="24" height="24"><path stroke="currentColor" d="M2 2L22 22" /></svg></span>
      <div class="billing-right"><span><svg width="24" height="24"></svg></span></div>
      <label>Arşivli <input class="app-switch" type="checkbox" role="switch"></label>
      <section class="accountant-toolbar accountant-link-invite"><button><span>Muhasebecini davet et</span></button></section>
      <div class="gib-page"><section class="gib-card gib-log-card">İşlemler</section><section class="gib-status-grid gib-status-grid--compact"><article>Şifre</article><article>Bağlantı</article></section></div>
    </body></html>`);
    for (const file of ["styles.css", "app-theme.css"]) await page.addStyleTag({ content: await readFile(new URL(`../src/${file}`, import.meta.url), "utf8") });
    await expect(page.locator(".billing-fact-icon svg")).toHaveCSS("color", "rgb(11, 11, 9)");
    await expect(page.locator(".billing-right svg")).toHaveCSS("color", "rgb(11, 11, 9)");
    const toggle = page.getByRole("switch");
    await toggle.focus();
    await page.keyboard.press("Space");
    await expect(toggle).toBeChecked();
    await expect(toggle).toHaveCSS("background-color", "rgb(200, 255, 0)");
    await page.keyboard.press("Space");
    await expect(toggle).not.toBeChecked();
    if ((await page.evaluate(() => matchMedia("(hover: hover) and (pointer: fine)").matches))) {
      const invite = page.getByRole("button", { name: "Muhasebecini davet et" });
      await invite.hover();
      await expect(invite).toHaveCSS("color", "rgb(11, 11, 9)");
      await expect.poll(() => invite.evaluate(el => getComputedStyle(el, "::before").opacity)).toBe("1");
    }
  });
}

test("document health uses neutral dark theme surfaces", async ({ page }, testInfo) => {
  const styles = await readFile(new URL("../src/styles.css", import.meta.url), "utf8");
  const theme = await readFile(new URL("../src/app-theme.css", import.meta.url), "utf8");
  await page.setContent(`<html data-theme="dark"><body><section class="document-health document-health--veriyok">
    <h2>Belgeler hazır mı?</h2><div class="document-health__body">
    <div class="document-health__score">Henüz hesaplanmadı</div>
    <div class="document-health__counts">Hazır 0 · Eksik 0 · Toplam 0</div>
    <div class="document-health__issues">Öncelikli işler</div></div></section></body></html>`);
  await page.addStyleTag({ content: styles });
  await page.addStyleTag({ content: theme });
  const expected = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue("--sc-soft").trim());
  const actual = await page.locator(".document-health").evaluate(el => getComputedStyle(el).getPropertyValue("--document-health-subtle").trim());
  expect(actual).toBe(expected);
  await page.locator(".document-health").screenshot({ path: testInfo.outputPath("document-health-dark.png") });
});
