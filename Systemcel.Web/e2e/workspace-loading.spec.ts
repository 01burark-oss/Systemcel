import { expect, test } from "@playwright/test";

test("workspace shows a themed loading state while auth initializes", async ({ page }) => {
  if (test.info().project.name === "reduced-motion") await page.emulateMedia({ reducedMotion: "reduce" });
  let finishConfig: (() => void) | undefined;
  await page.route("**/api/public/config", async (route) => {
    await new Promise<void>((resolve) => { finishConfig = resolve; });
    await route.fulfill({ status: 200, contentType: "application/json", body: '{"clerk":{"enabled":true}}' });
  });

  try {
    await page.goto("/app", { waitUntil: "domcontentloaded" });
    const loading = page.getByRole("status");
    await expect(loading.getByRole("heading", { name: "Çalışma alanın yükleniyor" })).toBeVisible();
    await expect(page.locator(".auth-shell__panel--compact")).toHaveCount(0);

    const visual = await loading.evaluate((element) => {
      const style = getComputedStyle(element);
      return {
        background: style.backgroundColor,
        reducedMotion: matchMedia("(prefers-reduced-motion: reduce)").matches,
        documentOverflow: document.documentElement.scrollWidth > window.innerWidth
      };
    });

    expect(visual.background).not.toBe("rgba(0, 0, 0, 0)");
    if (test.info().project.name === "reduced-motion") expect(visual.reducedMotion).toBe(true);
    await expect.poll(() => loading.locator(".workspace-loading__orbit-ring").evaluate((ring) => getComputedStyle(ring).animationName))
      .toBe(visual.reducedMotion ? "none" : "workspace-loading-turn");
    expect(visual.documentOverflow).toBe(false);
  } finally {
    finishConfig?.();
  }
});
