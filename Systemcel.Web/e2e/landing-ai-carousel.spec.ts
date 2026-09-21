import { expect, test } from "@playwright/test";

test("AI feature cards scroll horizontally on mobile without the live tour", async ({ page, viewport }) => {
  test.skip(!viewport || viewport.width > 700, "Mobile layout only");

  await page.route("**/api/public/config", (route) => route.fulfill({
    status: 200,
    contentType: "application/json",
    body: '{"clerk":{"enabled":true}}'
  }));
  await page.route("**/api/public/planlar", (route) => route.fulfill({ status: 200, contentType: "application/json", body: "[]" }));
  await page.goto("/");

  const carousel = page.getByRole("region", { name: /Systemcel AI/ });
  await carousel.scrollIntoViewIfNeeded();
  await expect(carousel.locator("article")).toHaveCount(6);
  await expect(page.getByRole("button", { name: /Canlı tur|Live tour/ })).toHaveCount(0);

  const initial = await carousel.evaluate((element) => ({
    width: element.clientWidth,
    contentWidth: element.scrollWidth,
    left: element.scrollLeft,
    cardWidth: element.querySelector("article")?.getBoundingClientRect().width ?? 0,
    display: getComputedStyle(element).display
  }));
  expect(initial.display).toBe("flex");
  expect(initial.contentWidth).toBeGreaterThan(initial.width);
  expect(initial.cardWidth).toBeLessThan(initial.width);

  await carousel.evaluate((element) => element.scrollTo({ left: element.clientWidth, behavior: "instant" }));
  await expect.poll(() => carousel.evaluate((element) => element.scrollLeft)).toBeGreaterThan(initial.left);
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(viewport.width);
});
