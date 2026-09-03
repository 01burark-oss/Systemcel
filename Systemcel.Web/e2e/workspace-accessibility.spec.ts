import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type Route } from "@playwright/test";

test.describe("workspace accessibility", () => {
  test.beforeEach(async ({ page }) => mockApi(page));

  test("theme changes preserve settings geometry", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Settings route");
    await page.addInitScript(() => localStorage.setItem("systemcel.theme", "light"));
    await page.goto("/app/ayarlar");
    const control = page.getByRole("switch", { name: "Tema seçimi: Koyu" });
    await expect(control).toBeVisible();
    const geometry = () => page.locator(".settings-page, .settings-appearance, .settings-theme-switch, .settings-grid > .settings-card").evaluateAll(elements => elements.map(el => {
      const { x, y, width, height } = el.getBoundingClientRect();
      return { x, y, width, height };
    }));
    const light = await geometry();
    await control.focus();
    await page.keyboard.press("ArrowRight");
    await expect(control).toBeChecked();
    expect(await geometry()).toEqual(light);
    await expect(page.locator("html")).not.toHaveAttribute("data-theme-transitioning");
    expect(await geometry()).toEqual(light);
    expect((await control.innerText()).trim()).toBe("");
    await control.screenshot({ path: testInfo.outputPath("icon-only-dark.png") });
    const duration = await control.evaluate(button => new Promise<number>(resolve => {
      const panel = document.querySelector(".settings-appearance")!;
      const onTransition = (event: Event) => {
        if ((event as TransitionEvent).propertyName !== "background-color") return;
        panel.removeEventListener("transitionrun", onTransition);
        resolve(Number(getComputedStyle(panel).transitionDuration.split(",")[0].replace("s", "")) * 1000);
      };
      panel.addEventListener("transitionrun", onTransition);
      (button as HTMLButtonElement).click();
    }));
    expect(duration).toBe(150);
    await expect(page.locator("html")).not.toHaveAttribute("data-theme-transitioning");
    expect(await geometry()).toEqual(light);
    await control.screenshot({ path: testInfo.outputPath("icon-only-light.png") });
  });

  test("sliding theme switch persists and respects reduced motion", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Settings desktop route");
    await page.goto("/app/ayarlar");
    const control = page.getByRole("switch", { name: "Tema seçimi: Koyu" });
    await control.focus();
    await page.keyboard.press("ArrowRight");
    await expect(control).toBeChecked();
    await expect(page.locator(".settings-theme-switch__symbol")).toHaveCSS("color", "rgb(11, 11, 9)");
    await control.screenshot({ path: testInfo.outputPath("theme-dark.png") });
    await control.click();
    await expect(control).not.toBeChecked();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
    await control.screenshot({ path: testInfo.outputPath("theme-light.png") });
    await page.reload();
    await expect(control).not.toBeChecked();
    await page.emulateMedia({ reducedMotion: "reduce" });
    const duration = await page.locator(".settings-theme-switch__thumb").evaluate(el => parseFloat(getComputedStyle(el).transitionDuration));
    expect(duration).toBeLessThanOrEqual(0.00001);
    const result = await new AxeBuilder({ page }).include(".settings-theme-switch").analyze();
    expect(result.violations).toEqual([]);
  });

  for (const theme of ["light", "dark"]) {
    test(`notification switches align and remain accessible in ${theme}`, async ({ page }, testInfo) => {
      test.skip(testInfo.project.name !== "desktop-chromium", "Mobile routing uses a separate companion screen without settings.");
      await page.addInitScript((value) => localStorage.setItem("systemcel.theme", value), theme);
      await page.route("**/api/ekran/bildirim-tercihleri", (route) => json(route, {
        uygulamaAktif: true, epostaAktif: false, telegramAktif: false, sessizSaatAktif: false,
        sessizBaslangicDakika: 1320, sessizBitisDakika: 480, saatDilimi: "Europe/Istanbul"
      }));
      await page.goto("/app/ayarlar");
      const panel = page.getByRole("region", { name: "Bildirim tercihleri" });
      await expect(panel.getByRole("switch")).toHaveCount(4);
      await panel.scrollIntoViewIfNeeded();
      const email = panel.getByRole("switch", { name: "E-posta", exact: true });
      await expect(email).not.toBeChecked();
      await email.focus();
      await page.keyboard.press("Space");
      await expect(email).toBeChecked();
      await page.keyboard.press("Space");
      await expect(email).not.toBeChecked();
      const bounds = await panel.boundingBox();
      expect(bounds!.x).toBeGreaterThanOrEqual(0);
      expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(page.viewportSize()!.width + 1);
      const results = await new AxeBuilder({ page }).include(".notification-preferences").analyze();
      expect(results.violations).toEqual([]);
      await panel.screenshot({ path: testInfo.outputPath(`notifications-${theme}.png`) });
    });
  }

  test("settings, support and admin operations have no serious or critical axe violations", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Desktop accessibility matrix");

    const cases = [
      { path: "/app/ayarlar", ready: "Uygulama kilidi" },
      { path: "/yardim", ready: "Bir destek talebi oluştur" },
      { path: "/app/yonetim/destek", ready: "Öncelikli AŞ" },
      { path: "/app/yonetim/muhasebeci-aktarimlari", ready: "Ada Muhasebe" }
    ];

    for (const current of cases) {
      await page.goto(current.path);
      await expect(page.getByText(current.ready, { exact: true }).first()).toBeVisible();
      await expectNoSeriousOrCriticalViolations(page);
    }
  });

  test("quick sale and support reflow remain axe-clean at 320px", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "mobile-small", "320px accessibility matrix");

    await page.goto("/app/hizli-satis");
    await expect(page.getByRole("textbox", { name: "Ürün veya barkod ara" })).toBeVisible();
    await expectNoSeriousOrCriticalViolations(page);

    await page.goto("/yardim");
    await expect(page.getByRole("heading", { name: "Bir destek talebi oluştur" })).toBeVisible();
    await expectNoSeriousOrCriticalViolations(page);
  });

  test("plan penceresi klavye odağını içeride tutar ve kapatılınca geri verir", async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== "desktop-chromium", "Desktop keyboard flow");

    await page.goto("/app/hizli-satis");
    const tetikleyici = page.getByRole("button", { name: "Barkodu ekle" });
    await expect(tetikleyici).toBeVisible();
    await expect(page.locator(".pos-search input")).toBeFocused();
    await tetikleyici.focus();
    await page.evaluate(() => {
      window.dispatchEvent(new CustomEvent("systemcel:entitlement", {
        detail: {
          code: "feature_not_available",
          detail: "Bu özellik mevcut planınızda kullanılamaz.",
          suggestedPlanCode: "isletme_buyume"
        }
      }));
    });

    await page.getByRole("button", { name: "Planları incele" }).click();
    const planPenceresi = page.getByRole("dialog", { name: "Planını seç" });
    const kapat = page.getByRole("button", { name: "Plan penceresini kapat" });
    const sonPlan = planPenceresi.getByRole("link", { name: "Planı incele: Kurumsal planı" });
    await expect(planPenceresi).toBeVisible();
    await expect(kapat).toBeFocused();

    await page.keyboard.press("Shift+Tab");
    await expect(sonPlan).toBeFocused();
    await page.keyboard.press("Tab");
    await expect(kapat).toBeFocused();

    await page.keyboard.press("Escape");
    await expect(planPenceresi).toBeHidden();
    await expect(tetikleyici).toBeFocused();
  });
});

async function expectNoSeriousOrCriticalViolations(page: Page) {
  const results = await new AxeBuilder({ page })
    .include("main")
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
    .analyze();
  const violations = results.violations
    .filter(({ impact }) => impact === "serious" || impact === "critical")
    .map(({ id, impact, help, nodes }) => ({
      id,
      impact,
      help,
      nodes: nodes.map((node) => ({
        target: node.target.join(" "),
        html: node.html,
        summary: node.failureSummary
      }))
    }));
  expect(violations).toEqual([]);
}

async function mockApi(page: Page) {
  await page.route("**/hubs/muhasebeci-sohbet/**", (route) => route.abort());
  await page.route("**/api/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === "/api/public/config") return json(route, { clerk: { enabled: false } });
    if (path === "/api/public/planlar") return json(route, []);
    if (path === "/api/ekran/ust-bar") return json(route, {
      aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
      muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "Tam",
      bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
      telegramAktif: false, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
    });
    if (path === "/api/ekran/kolay-kurulum") return json(route, {
      tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
      isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
    });
    if (path === "/api/ekran/sohbetler") return json(route, { sohbetler: [], okunmamisMesajSayisi: 0 });
    if (path === "/api/ekran/urun-stok") return json(route, {
      aktifIsletme: "Örnek İşletme",
      urunler: [{ id: 7, ad: "Filtre kahve", barkod: "869000000007", satisFiyati: 120, stokMiktari: 8, kdvOrani: 20 }],
      sonHareketler: [], tipSecenekleri: [], birimSecenekleri: []
    });
    if (path === "/api/ekran/mobil-tarama/durum") return json(route, { fisOcrHazir: false });
    if (path === "/api/ekran/destek-talepleri") return json(route, { talepler: [] });
    if (path === "/api/ekran/ayarlar") return json(route, {
      aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", seciliIsletmeId: 42, seciliKalemId: 1,
      dil: "tr", diller: [{ kod: "tr", ad: "Türkçe" }],
      isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }],
      kalemler: [{ id: 1, tip: "Gelir", ad: "Satış" }], mesaj: ""
    });
    if (path === "/api/ekran/ayarlar/pin") return json(route, { varsayilanPin: true, mesaj: "PIN kilidi hazır." });
    if (path === "/api/ekran/uyelikler") return json(route, {
      sahibiMi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme",
      uyelikler: [{ id: 1, kullaniciId: 1, eposta: "owner@example.test", adSoyad: "İşletme Sahibi", rol: "isletme_sahibi", durum: "Aktif", davetKodu: "" }]
    });
    if (path === "/api/ekran/yonetim/destek") return json(route, { talepler: [{
      id: 2, isletmeId: 12, isletmeAdi: "Öncelikli AŞ", konu: "Teknik destek", kategori: "Teknik",
      aciklama: "Rapor açılmıyor.", oncelik: "Oncelikli", durum: "Islemde", yoneticiYaniti: "İnceliyoruz.",
      createdAt: "2026-08-24T10:00:00Z", updatedAt: "2026-08-24T10:00:00Z"
    }] });
    if (path === "/api/ekran/yonetim/muhasebeci-aktarimlari") return json(route, {
      yoneticiMi: true, aktarimDonemi: "2026-08", aktarimlar: [{
        muhasebeciIsletmeId: 12, muhasebeciAdi: "Ada Muhasebe", aktarimDonemi: "2026-08",
        paraBirimi: "TRY", alacakSayisi: 2, tahsilEdilenTutar: 3000,
        platformKomisyonTutari: 300, aktarilacakTutar: 2700, durum: "Bekliyor", aktarimReferansi: ""
      }]
    });
    return json(route, { mesaj: `Unexpected route: ${path}` }, 404);
  });
}

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
