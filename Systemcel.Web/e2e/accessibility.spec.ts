import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type Route } from "@playwright/test";

const publishableKey = "pk_test_ZXhhbXBsZS5jb20k";

test.describe("critical accessibility smoke checks", () => {
  test.beforeEach(async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Desktop Chromium accessibility project only");
    await mockClerk(page);
  });

  test("landing page has no serious or critical axe violations", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: /Hepsi tek yerde/i })).toBeVisible();

    await expectNoSeriousOrCriticalViolations(page);
  });

  test("sign-up page has no serious or critical axe violations", async ({ page }) => {
    await page.goto("/kayit?hesapTipi=Muhasebeci");
    await expect(page.getByRole("heading", { name: "Hesabınızı oluşturun" })).toBeVisible();

    await expectNoSeriousOrCriticalViolations(page);
  });
});

async function expectNoSeriousOrCriticalViolations(page: Page) {
  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
    .analyze();

  const violations = results.violations.filter(({ impact }) => impact === "serious" || impact === "critical");
  const summary = violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact,
    help: violation.help,
    helpUrl: violation.helpUrl,
    targets: violation.nodes.map((node) => node.target.join(" "))
  }));

  expect(summary).toEqual([]);
}

async function mockClerk(page: Page) {
  await page.route("**/api/public/config", (route) => json(route, {
    clerk: {
      enabled: true,
      publishableKey,
      jsUrl: "/fake-clerk-a11y.js"
    }
  }));
  await page.route("**/api/public/planlar", (route) => json(route, []));
  await page.route("**/fake-clerk-a11y.js", (route) => route.fulfill({
    status: 200,
    contentType: "text/javascript",
    body: `
      window.Clerk = {
        isSignedIn: false,
        user: null,
        session: null,
        client: {
          signIn: {
            create: async () => ({}), attemptFirstFactor: async () => ({}),
            prepareSecondFactor: async () => ({}), attemptSecondFactor: async () => ({}),
            authenticateWithRedirect: async () => ({})
          },
          signUp: {
            create: async () => ({}), prepareEmailAddressVerification: async () => ({}),
            attemptEmailAddressVerification: async () => ({}), authenticateWithRedirect: async () => ({})
          }
        },
        load: async () => {},
        setActive: async () => {},
        addListener: () => () => {},
        signOut: async () => {}
      };
    `
  }));
}

async function json(route: Route, body: unknown) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
}
