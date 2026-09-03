import { expect, test } from "@playwright/test";

test("belge durumu kartı dar bir kapsayıcıda kırpılmaz", async ({ page }) => {
  await page.setViewportSize({ width: 1200, height: 800 });
  await page.setContent(`
    <!doctype html>
    <html data-theme="dark">
      <head></head>
      <body>
        <section class="document-health document-health--veriyok" style="width: 280px">
          <header class="document-health__header">
            <div>
              <span class="document-health__eyebrow">Belge durumu</span>
              <h2>Belgeler hazır mı?</h2>
            </div>
            <span class="document-health__status">Veri yok</span>
          </header>
          <div class="document-health__body">
            <div class="document-health__score document-health__score--empty">
              <div class="document-health__score-empty">
                <strong>Henüz hesaplanmadı</strong>
                <small>Belge eklenince skor burada görünür.</small>
              </div>
            </div>
            <dl class="document-health__counts"><div><dt>Hazır</dt><dd>0</dd></div><div><dt>Eksik</dt><dd>0</dd></div><div><dt>Toplam</dt><dd>0</dd></div></dl>
            <div class="document-health__issues"><h3>Öncelikli işler</h3><p>Belge hazırlığı için henüz veri yok.</p></div>
          </div>
        </section>
      </body>
    </html>
  `);
  await page.addStyleTag({ path: "src/styles.css" });
  await page.addStyleTag({ path: "src/app-theme.css" });
  await page.waitForLoadState("load");

  const metrics = await page.evaluate(() => {
    const card = document.querySelector<HTMLElement>(".document-health")!;
    const body = document.querySelector<HTMLElement>(".document-health__body")!;
    const heading = document.querySelector<HTMLElement>(".document-health__header h2")!;
    const score = document.querySelector<HTMLElement>(".document-health__score")!;
    const scoreText = document.querySelector<HTMLElement>(".document-health__score-empty strong")!;
    const rect = (element: HTMLElement) => {
      const value = element.getBoundingClientRect();
      return { left: value.left, right: value.right, width: value.width };
    };
    return {
      card: rect(card),
      body: { clientWidth: body.clientWidth, scrollWidth: body.scrollWidth },
      heading: rect(heading),
      score: rect(score),
      scoreText: rect(scoreText)
    };
  });

  expect(metrics.body.scrollWidth).toBeLessThanOrEqual(metrics.body.clientWidth);
  expect(metrics.heading.right).toBeLessThanOrEqual(metrics.card.right + 0.1);
  expect(metrics.scoreText.right).toBeLessThanOrEqual(metrics.score.right + 0.1);
});

test("belge durumu kartındaki üç panel dengeli genişlikte kalır", async ({ page }) => {
  await page.setViewportSize({ width: 1800, height: 800 });
  await page.setContent(`
    <!doctype html>
    <html data-theme="dark">
      <head></head>
      <body>
        <section class="document-health document-health--veriyok" style="width: 1500px">
          <header class="document-health__header">
            <div><span class="document-health__eyebrow">Belge durumu</span><h2>Belgeler hazır mı?</h2></div>
            <span class="document-health__status">Veri yok</span>
          </header>
          <div class="document-health__body">
            <div class="document-health__score document-health__score--empty"><div class="document-health__score-empty"><strong>Henüz hesaplanmadı</strong><small>Belge eklenince skor burada görünür.</small></div></div>
            <dl class="document-health__counts"><div><dt>Hazır</dt><dd>0</dd></div><div><dt>Eksik</dt><dd>0</dd></div><div><dt>Toplam</dt><dd>0</dd></div></dl>
            <div class="document-health__issues"><h3>Öncelikli işler</h3><p>Belge hazırlığı için henüz veri yok.</p></div>
          </div>
        </section>
      </body>
    </html>
  `);
  await page.addStyleTag({ path: "src/styles.css" });
  await page.addStyleTag({ path: "src/app-theme.css" });
  await page.waitForLoadState("load");

  const widths = await page.locator(".document-health__body > *").evaluateAll((elements) =>
    elements.map((element) => element.getBoundingClientRect().width)
  );
  const layout = await page.locator(".document-health__score-empty").evaluate((element) => ({
    display: getComputedStyle(element).display,
    columns: getComputedStyle(element).gridTemplateColumns,
    score: element.getBoundingClientRect().toJSON(),
    children: Array.from(element.children).map((child) => child.getBoundingClientRect().toJSON()),
    strongCenter: element.querySelector("strong")!.getBoundingClientRect().x + element.querySelector("strong")!.getBoundingClientRect().width / 2,
    smallCenter: element.querySelector("small")!.getBoundingClientRect().x + element.querySelector("small")!.getBoundingClientRect().width / 2,
    countTextAlignments: Array.from(document.querySelectorAll<HTMLElement>(".document-health__counts > div")).map((child) => getComputedStyle(child).textAlign),
    countItemAlignments: Array.from(document.querySelectorAll<HTMLElement>(".document-health__counts > div")).map((child) => getComputedStyle(child).justifyItems)
  }));
  const smallest = Math.min(...widths);
  const largest = Math.max(...widths);

  expect(largest / smallest).toBeLessThanOrEqual(1.15);
  expect(layout.display).toBe("grid");
  expect(layout.columns).not.toBe("none");
  expect(Math.abs(layout.strongCenter - layout.smallCenter)).toBeLessThanOrEqual(1);
  expect(layout.countTextAlignments).toEqual(["center", "center", "center"]);
  expect(layout.countItemAlignments).toEqual(["center", "center", "center"]);
});
