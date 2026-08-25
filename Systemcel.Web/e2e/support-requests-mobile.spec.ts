import { expect, test } from "@playwright/test";

test("help support request form has no horizontal overflow at 320px", async ({ page, viewport }) => {
  test.skip(viewport?.width !== 320, "320px support form regression");
  await page.route("**/api/public/config", (route) => route.fulfill({ contentType: "application/json", body: JSON.stringify({ clerk: { enabled: false } }) }));
  await page.route("**/api/ekran/destek-talepleri", (route) => route.fulfill({ contentType: "application/json", body: JSON.stringify({ talepler: [] }) }));

  await page.goto("/yardim");
  await expect(page.getByRole("heading", { name: "Bir destek talebi oluştur" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Talep oluştur" })).toBeVisible();

  const overflow = await page.evaluate(() => ({ viewport: window.innerWidth, document: document.documentElement.scrollWidth, body: document.body.scrollWidth }));
  expect(overflow.document).toBeLessThanOrEqual(overflow.viewport);
  expect(overflow.body).toBeLessThanOrEqual(overflow.viewport);
});
