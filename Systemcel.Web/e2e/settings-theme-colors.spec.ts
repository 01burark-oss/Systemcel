import { expect, test } from "@playwright/test";
import { readFile } from "node:fs/promises";

test("ayarlar adımları ve alt menü eski mavi/beyaz tema renklerine dönmez", async ({ page }) => {
  await page.setViewportSize({ width: 1200, height: 800 });
  await page.setContent(`
    <!doctype html>
    <html data-theme="dark">
      <body class="react-shell__body">
        <section class="settings-import-steps">
          <span><strong>1</strong> Kod oluştur</span>
          <span><strong>2</strong> Paketi seç</span>
          <span><strong>3</strong> Güvenli aktar</span>
        </section>
        <section class="settings-migration-steps">
          <span><strong>1</strong> Şablonu indir</span>
          <span><strong>2</strong> Önizle</span>
          <span><strong>3</strong> Onayla</span>
        </section>
        <nav class="react-sidebar">
          <a class="react-sidebar__sublink" href="#">GİB portal</a>
        </nav>
      </body>
    </html>
  `);
  for (const file of ["styles.css", "app-theme.css"]) {
    await page.addStyleTag({ content: await readFile(`src/${file}`, "utf8") });
  }

  const stepColors = await page.locator(".settings-import-steps strong, .settings-migration-steps strong").evaluateAll((elements) =>
    elements.map((element) => getComputedStyle(element).color)
  );
  await page.locator(".react-sidebar__sublink").hover();
  await page.waitForTimeout(220);
  const hover = await page.locator(".react-sidebar__sublink").evaluate((element) => {
    const style = getComputedStyle(element);
    return { backgroundImage: style.backgroundImage, color: style.color, borderColor: style.borderColor };
  });

  expect(stepColors).toEqual(Array(6).fill("rgb(11, 11, 9)"));
  expect(hover.backgroundImage).toContain("linear-gradient");
  expect(hover.color).toBe("rgb(244, 243, 235)");
  expect(hover.borderColor).not.toContain("236");
});
