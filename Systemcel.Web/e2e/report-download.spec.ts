import { expect, test, type Page, type Route } from "@playwright/test";

test("report PDF response becomes a browser download", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Deterministic download project");
  await mockWorkspace(page);
  await page.route("**/api/ekran/raporlar", (route) => json(route, {
    aktifIsletme: "Örnek İşletme",
    bugun: "2026-08-24",
    varsayilanDonem: "2026-08",
    formatlar: [{ deger: "zip", etiket: "ZIP", secili: true }],
    icerikler: [],
    yazdirmaSablonlari: [{ deger: "yoneticiOzeti", etiket: "Yönetici Özeti" }],
    tarihAraliklari: [{ deger: "monthly", etiket: "Aylık" }],
    sonPaket: null
  }));
  await page.route("**/api/ekran/raporlar/yazdir/pdf", (route) => route.fulfill({
    status: 200,
    contentType: "application/pdf",
    headers: { "Content-Disposition": "attachment; filename=systemcel-yonetici-ozeti.pdf" },
    body: "%PDF-1.4\n%%EOF\n"
  }));

  await page.goto("/app/raporlar");
  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("button", { name: "PDF Kaydet" }).click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toBe("systemcel-yonetici-ozeti.pdf");
});

async function mockWorkspace(page: Page) {
  await page.route("**/api/public/config", (route) => json(route, {
    clerk: { enabled: true, publishableKey: "pk_test_ZXhhbXBsZS5jb20k", jsUrl: "/fake-clerk-report.js" }
  }));
  await page.route("**/fake-clerk-report.js", (route) => route.fulfill({
    status: 200,
    contentType: "text/javascript",
    body: `window.Clerk = {
      isSignedIn: true,
      user: { id: 'report-user', fullName: 'Rapor Kullanıcısı', primaryEmailAddress: { emailAddress: 'report@example.test' } },
      session: { getToken: async () => 'report-token' }, client: { signIn: {}, signUp: {} },
      load: async () => {}, setActive: async () => {}, addListener: () => () => {}, signOut: async () => {}
    };`
  }));
  await page.route("**/api/ekran/ust-bar", (route) => json(route, {
    aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
    muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "Tam",
    bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
    telegramAktif: false, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
  }));
  await page.route("**/api/ekran/kolay-kurulum", (route) => json(route, {
    tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
    isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
  }));
}

async function json(route: Route, body: unknown) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
}
