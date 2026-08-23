import { expect, test, type Page, type Route } from "@playwright/test";

const publishableKey = "pk_test_ZXhhbXBsZS5jb20k";

test("mobile sign-up uses the full viewport without horizontal overflow", async ({ page, viewport }) => {
  test.skip(!viewport || viewport.width > 430, "Mobile regression matrix only");
  await mockClerk(page, false);
  await page.goto("/kayit?hesapTipi=Muhasebeci");

  await expect(page.getByRole("heading", { name: "Hesabınızı oluşturun" })).toBeVisible();
  await expect(page.getByRole("button", { name: "İşletme" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Muhasebeci" })).toHaveClass(/active/);
  await expect(page.getByLabel("E-posta")).toBeVisible();
  await expect(page.getByLabel("Parola", { exact: true })).toBeVisible();

  if (viewport?.width === 320 && viewport.height === 568) {
    const geometry = await page.evaluate(() => {
      const shell = document.querySelector<HTMLElement>(".auth-shell--sign-up.auth-shell--branded");
      const brand = document.querySelector<HTMLElement>(".auth-shell__brand--top");
      const language = document.querySelector<HTMLElement>(".auth-shell__language");
      const card = document.querySelector<HTMLElement>(".auth-shell__card");
      const heading = document.querySelector<HTMLElement>(".auth-shell__card-head h2");
      if (!shell || !brand || !language || !card || !heading) throw new Error("Sign-up geometry targets are missing");

      const brandBox = brand.getBoundingClientRect();
      const languageBox = language.getBoundingClientRect();
      const cardBox = card.getBoundingClientRect();
      const headingBox = heading.getBoundingClientRect();
      return {
        headerBottom: Math.max(brandBox.bottom, languageBox.bottom),
        cardTop: cardBox.top,
        headingTop: headingBox.top,
        headingBottom: headingBox.bottom,
        viewportHeight: window.innerHeight
      };
    });
    const headerGap = geometry.cardTop - geometry.headerBottom;
    expect(headerGap, "sign-up card must start below the branded header").toBeGreaterThanOrEqual(0);
    expect(headerGap, "sign-up card/header gap should stay compact").toBeLessThanOrEqual(16);
    expect(geometry.headingTop, "sign-up heading must start inside the viewport").toBeGreaterThanOrEqual(0);
    expect(geometry.headingBottom, "sign-up heading must be visible before scrolling").toBeLessThanOrEqual(geometry.viewportHeight);

    const finalAction = page.locator(".auth-shell__switch");
    await finalAction.scrollIntoViewIfNeeded();
    const actionBox = await finalAction.boundingBox();
    const shellScrollTop = await page.locator(".auth-shell--sign-up.auth-shell--branded").evaluate((shell) => shell.scrollTop);
    expect(actionBox, "final sign-up action must have a rendered box").not.toBeNull();
    expect(shellScrollTop, "the compact sign-up shell must allow reaching its final action by scrolling").toBeGreaterThan(0);
    expect(actionBox!.y, "final sign-up action must be reachable by scrolling").toBeGreaterThanOrEqual(0);
    expect(actionBox!.y + actionBox!.height, "final sign-up action must fit in the viewport after scrolling").toBeLessThanOrEqual(viewport.height);
  }

  const overflow = await page.evaluate(() => ({
    viewport: window.innerWidth,
    document: document.documentElement.scrollWidth,
    body: document.body.scrollWidth
  }));
  expect(overflow.document).toBeLessThanOrEqual(overflow.viewport);
  expect(overflow.body).toBeLessThanOrEqual(overflow.viewport);
});

test("mobile sign-out blocks browser-back access to the workspace", async ({ page, viewport }) => {
  test.skip(!viewport || viewport.width > 430, "Mobile regression matrix only");
  await mockClerk(page, true);
  await mockWorkspace(page);
  await page.goto("/app");

  const signOut = page.getByRole("button", { name: "Çıkış yap" });
  await expect(signOut).toBeVisible();
  await signOut.click();
  await expect(page).toHaveURL(/\/giris$/);

  await page.goBack();
  await expect(page).not.toHaveURL(/\/app(?:\/|$)/);
  await page.goto("/app");
  await expect(page.getByRole("heading", { name: "Devam etmek için giriş yap" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Çıkış yap" })).toHaveCount(0);
});

test("every mobile workspace route keeps an accessible sign-out path", async ({ page, viewport }) => {
  test.setTimeout(120_000);
  test.skip(!viewport || viewport.width > 430, "Mobile regression matrix only");
  await mockClerk(page, true);
  await mockWorkspace(page);

  const dedicatedMobileRoutes = new Set(["/app/sohbetler", "/app/muhasebeciler", "/app/abonelik"]);
  const routes = [
    "/app",
    "/app/gelir-gider",
    "/app/hizli-satis",
    "/app/cari-hesaplar",
    "/app/urun-stok",
    "/app/faturalar",
    "/app/tahsilat-odeme",
    "/app/raporlar",
    "/app/muhasebeci",
    "/app/yonetim/muhasebeci-basvurulari",
    "/app/muhasebeciler",
    "/app/sohbetler",
    "/app/gib-portal",
    "/app/abonelik",
    "/app/ayarlar"
  ];

  for (const route of routes) {
    await page.goto(route);
    if (dedicatedMobileRoutes.has(route)) {
      const accountMenu = page.getByRole("button", { name: "Hesap ve çıkış menüsü" });
      await expect(accountMenu).toBeVisible();
      await accountMenu.click();
      await expect(page.getByRole("menuitem", { name: "Çıkış yap" })).toBeVisible();
      await page.keyboard.press("Escape");
    } else {
      await expect(page.getByRole("button", { name: "Çıkış yap" })).toBeVisible();
    }

    const overflow = await page.evaluate(() => ({
      viewport: window.innerWidth,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth
    }));
    expect(overflow.document, `${route} document overflow`).toBeLessThanOrEqual(overflow.viewport);
    expect(overflow.body, `${route} body overflow`).toBeLessThanOrEqual(overflow.viewport);
  }
});

async function mockClerk(page: Page, signedIn: boolean) {
  await page.route("**/api/public/config", (route) => json(route, {
    clerk: {
      enabled: true,
      publishableKey,
      jsUrl: "/fake-clerk.js"
    }
  }));
  await page.route("**/fake-clerk.js", async (route) => {
    const script = `
      (() => {
        const listeners = [];
        const initialSignedIn = ${signedIn ? "true" : "false"} && localStorage.getItem('__e2e_signed_out') !== '1';
        const clerk = {
          isSignedIn: initialSignedIn,
          user: initialSignedIn ? { id: 'user_mobile', fullName: 'Mobil Kullanıcı', primaryEmailAddress: { emailAddress: 'mobile@example.test' } } : null,
          session: initialSignedIn ? { getToken: async () => 'test-token' } : null,
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
          addListener: (callback) => { listeners.push(callback); return () => {}; },
          signOut: async ({ redirectUrl } = {}) => {
            localStorage.setItem('__e2e_signed_out', '1');
            clerk.isSignedIn = false;
            clerk.user = null;
            clerk.session = null;
            listeners.forEach((callback) => callback({ user: null, session: null }));
            window.location.replace(redirectUrl || '/giris');
          }
        };
        window.Clerk = clerk;
      })();
    `;
    await route.fulfill({ status: 200, contentType: "text/javascript", body: script });
  });
}

async function mockWorkspace(page: Page) {
  await page.route("**/hubs/muhasebeci-sohbet/**", (route) => route.abort());
  await page.route("**/api/ekran/sohbetler?**", (route) => json(route, {
    sohbetler: [],
    okunmamisMesajSayisi: 0
  }));
  await page.route("**/api/ekran/muhasebeciler", (route) => json(route, {
    mesaj: "",
    profiller: []
  }));
  await page.route("**/api/abonelik/ozet", (route) => route.fulfill({
    status: 503,
    contentType: "application/json",
    body: JSON.stringify({ mesaj: "E2E mobil rota testi" })
  }));
  await page.route("**/api/public/planlar", (route) => json(route, []));
  await page.route("**/api/ekran/ust-bar", (route) => json(route, {
    aktifIsletmeId: 42,
    aktifIsletme: "Örnek İşletme",
    hesapTipi: "Isletme",
    muhasebeciMusteriBaglami: false,
    muhasebeciAdi: "",
    muhasebeciYetkiSeviyesi: "Tam",
    bildirimVar: false,
    bildirimSayisi: 0,
    sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
    telegramAktif: false,
    isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
  }));
  await page.route("**/api/ekran/kolay-kurulum", (route) => json(route, {
    tamamlandi: true,
    isletmeId: 42,
    isletmeAdi: "Örnek İşletme",
    hesapTipi: "Isletme",
    isletmeTuru: "Genel",
    konum: "İstanbul / Kadıköy",
    muhasebeciVarMi: false,
    mesaj: "",
    turler: []
  }));
}

async function json(route: Route, body: unknown) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
}
