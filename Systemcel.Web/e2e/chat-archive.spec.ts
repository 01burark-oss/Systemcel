import { expect, test, type Page, type Route } from "@playwright/test";

const publishableKey = "pk_test_ZXhhbXBsZS5jb20k";

test("archive state stays unique through rapid archive, restore and re-archive", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Deterministic archive race project");

  let archived = false;
  let archiveCalls = 0;
  let nextListDelay = 0;
  await mockAuthenticatedWorkspace(page);

  await page.route("**/hubs/muhasebeci-sohbet/**", (route) => route.abort());
  await page.route("**/api/ekran/sohbetler?**", async (route) => {
    const snapshot = archived;
    const delay = nextListDelay;
    nextListDelay = 0;
    if (delay)
      await new Promise((resolve) => setTimeout(resolve, delay));
    const includeArchived = new URL(route.request().url()).searchParams.get("includeArchived") === "true";
    await json(route, {
      sohbetler: includeArchived || !snapshot ? [conversation(snapshot)] : [],
      okunmamisMesajSayisi: 0
    });
  });
  await page.route(/\/api\/ekran\/sohbetler\/1\/mesajlar(?:\?.*)?$/, async (route) => {
    const snapshot = archived;
    await new Promise((resolve) => setTimeout(resolve, 80));
    await json(route, {
      sohbetId: 1,
      sohbet: conversation(snapshot),
      mesajlar: [],
      hasMore: false,
      nextBeforeId: null
    });
  });
  await page.route("**/api/ekran/sohbetler/1/arsiv", async (route) => {
    archiveCalls += 1;
    const body = route.request().postDataJSON() as { arsivlendi: boolean };
    await new Promise((resolve) => setTimeout(resolve, 60));
    archived = body.arsivlendi;
    await json(route, conversation(archived));
  });

  await page.goto("/app/sohbetler");
  const listItems = page.locator(".chat-center__list > button");
  await expect(listItems).toHaveCount(1);
  await expect(page.getByRole("button", { name: "Sohbeti arşivle" })).toBeVisible();

  nextListDelay = 250;
  await page.getByRole("button", { name: "Yenile", exact: true }).click();
  await page.getByRole("button", { name: "Sohbeti arşivle" }).evaluate((button) => {
    (button as HTMLButtonElement).click();
    (button as HTMLButtonElement).click();
  });
  await expect(page.getByText("Henüz sohbet yok.")).toBeVisible();
  expect(archiveCalls).toBe(1);

  const archiveToggle = page.locator(".chat-center__archive-toggle input");
  await archiveToggle.check();
  await expect(listItems).toHaveCount(1);
  await expect(page.getByRole("button", { name: "Sohbeti arşivden çıkar" })).toBeVisible();
  await page.getByRole("button", { name: "Sohbeti arşivden çıkar" }).click();
  await expect(page.getByText("Arşivlenmiş sohbet yok.")).toBeVisible();

  await archiveToggle.uncheck();
  await expect(listItems).toHaveCount(1);
  await expect(page.getByRole("button", { name: "Sohbeti arşivle" })).toBeVisible();
  await page.getByRole("button", { name: "Sohbeti arşivle" }).click();
  await expect(page.getByText("Henüz sohbet yok.")).toBeVisible();

  await archiveToggle.check();
  await expect(listItems).toHaveCount(1);
  await expect(listItems.filter({ hasText: "Ayşe Mali Müşavirlik" })).toHaveCount(1);
  expect(archiveCalls).toBe(3);
});

function conversation(arsivlendi: boolean) {
  return {
    id: 1,
    muhasebeciIsletmeId: 7,
    musteriIsletmeId: 42,
    talepId: null,
    baglantiId: 9,
    baslik: "Ayşe Mali Müşavirlik",
    konu: "Aylık belgeler",
    karsiTarafAdi: "Ayşe Mali Müşavirlik",
    durum: "Aktif",
    sonMesaj: "Temmuz belgeleri",
    sonMesajAt: "2026-08-09T08:00:00Z",
    okunmamisMesajSayisi: 0,
    arsivlendi,
    hedefUrl: "/app/sohbetler?sohbetId=1"
  };
}

async function mockAuthenticatedWorkspace(page: Page) {
  await page.route("**/api/public/config", (route) => json(route, {
    clerk: { enabled: true, publishableKey, jsUrl: "/fake-clerk-chat.js" }
  }));
  await page.route("**/fake-clerk-chat.js", (route) => route.fulfill({
    status: 200,
    contentType: "text/javascript",
    body: `window.Clerk = {
      isSignedIn: true,
      user: { id: 'chat-user', fullName: 'Sohbet Kullanıcısı', primaryEmailAddress: { emailAddress: 'chat@example.test' } },
      session: { getToken: async () => 'chat-token' },
      client: { signIn: {}, signUp: {} },
      load: async () => {}, setActive: async () => {}, addListener: () => () => {}, signOut: async () => {}
    };`
  }));
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
