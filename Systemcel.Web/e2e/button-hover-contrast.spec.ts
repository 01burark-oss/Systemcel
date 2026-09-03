import { expect, test } from "@playwright/test";
import { readFile } from "node:fs/promises";

test("lime buton hover metni koyu kalır", async ({ page }) => {
  await page.setContent(`
    <!doctype html>
    <html data-theme="dark">
      <body class="react-shell__body">
        <main class="accountant-panel settings-page gib-page">
          <a class="accountant-primary-link" href="#">Muhasebeciyi bağla</a>
          <a class="accountant-card__action" href="#">Profili aç</a>
          <section class="accountant-section"><button type="button">İşlem yap</button></section>
          <button class="accountant-modal__primary" type="button">Gönder</button>
          <button class="settings-btn settings-btn--green" type="button">Ayarı kaydet</button>
          <button class="settings-btn settings-btn--navy" type="button">Şablonu değiştir</button>
          <button class="settings-btn settings-btn--primary" type="button">Bildirimleri kaydet</button>
          <button class="gib-btn gib-btn--primary" type="button">Bağlan</button>
        </main>
      </body>
    </html>
  `);
  for (const file of ["styles.css", "app-theme.css"]) {
    await page.addStyleTag({ content: await readFile(`src/${file}`, "utf8") });
  }
  const buttons = page.locator(".accountant-primary-link, .accountant-card__action, .accountant-section > button, .accountant-modal__primary, .settings-btn--green, .gib-btn--primary");
  for (const element of await buttons.all()) {
    await element.hover();
    await page.waitForTimeout(180);
    const style = await element.evaluate((node) => {
      const style = getComputedStyle(node);
      return { color: style.color, background: style.backgroundColor };
    });
    expect(style.color).toBe("rgb(11, 11, 9)");
    expect(style.background).toBe("rgb(220, 255, 102)");
  }

  const settingsButtons = page.locator(".settings-page .settings-btn:not(.settings-btn--danger)");
  for (const element of await settingsButtons.all()) {
    await element.hover();
    await page.waitForTimeout(180);
    const style = await element.evaluate((node) => {
      const style = getComputedStyle(node);
      return { color: style.color, background: style.backgroundColor };
    });
    expect(style.color).toBe("rgb(11, 11, 9)");
    expect(style.background).toBe("rgb(220, 255, 102)");
  }
});
