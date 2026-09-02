import { expect, test } from "@playwright/test";
import { readFile } from "node:fs/promises";

for (const theme of ["light", "dark"]) {
  test(`report controls remain readable in ${theme}`, async ({ page }, testInfo) => {
    await page.setContent(`<html data-theme="${theme}"><head><meta name="viewport" content="width=device-width, initial-scale=1"></head><body class="react-shell__body"><main class="reports-page">
      <fieldset class="reports-period-picker"><legend>Dönem</legend><label><span>Ay</span><select><option>Eylül</option></select></label><label><span>Yıl</span><input value="2026"></label></fieldset>
      <button class="reports-btn reports-btn--success">Yazdır</button><button class="reports-btn reports-btn--success" disabled>Yazdır</button>
    </main></body></html>`);
    for (const file of ["styles.css", "app-theme.css", "screens/raporlar/report-controls.css"]) {
      await page.addStyleTag({ content: await readFile(new URL(`../src/${file}`, import.meta.url), "utf8") });
    }
    const enabled = page.getByRole("button", { name: "Yazdır" }).first();
    const disabled = page.getByRole("button", { name: "Yazdır" }).last();
    await expect(enabled).toHaveCSS("color", "rgb(11, 11, 9)");
    await expect(enabled).toHaveCSS("background-color", "rgb(200, 255, 0)");
    await expect(disabled).toHaveCSS("opacity", "1");
    await expect(disabled).toHaveCSS("color", theme === "dark" ? "rgb(169, 170, 159)" : "rgb(102, 101, 93)");
    await page.locator(".reports-page").screenshot({ path: testInfo.outputPath(`report-controls-${theme}.png`) });
  });
}
