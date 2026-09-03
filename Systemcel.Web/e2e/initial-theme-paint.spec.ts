import { expect, test } from "@playwright/test";

test("koyu tema ilk karede eski mavi yüzeyleri göstermez", async ({ page }) => {
  await page.setContent(`
    <!doctype html>
    <html data-theme="dark">
      <head><link rel="stylesheet" href="/src/styles.css"></head>
      <body>
        <div class="react-shell">
          <main class="react-shell__main">
            <div class="react-shell__body">
              <div class="mobile-workspace-view">
                <div class="chat-center-page"></div>
                <nav class="mobile-workspace-nav">
                  <a class="active"><span>Merkez</span></a>
                </nav>
              </div>
            </div>
          </main>
        </div>
      </body>
    </html>
  `);
  await page.waitForLoadState("load");

  const surfaces = await page.locator(".react-shell, .mobile-workspace-view, .chat-center-page, .mobile-workspace-nav, .mobile-workspace-nav a.active").evaluateAll((elements) => {
    return elements.map((element) => {
      const style = getComputedStyle(element);
      return `${style.backgroundColor}|${style.backgroundImage}|${style.borderTopColor}|${style.color}`;
    });
  });

  expect(surfaces.join("\n")).not.toMatch(/31, 147, 255|3, 18, 46|6, 26, 63|4, 20, 47/);
});
