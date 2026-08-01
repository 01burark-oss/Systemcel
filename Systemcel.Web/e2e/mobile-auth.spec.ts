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
