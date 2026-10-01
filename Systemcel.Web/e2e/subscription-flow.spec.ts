import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type Route } from "@playwright/test";

const plan = {
  kod: "muhasebeci_standart",
  ad: "Standart",
  hesapTipi: "Muhasebeci",
  aylikTutar: 799,
  yillikTutar: null,
  yillikEfektifAylikTutar: null,
  normalAylikTutar: 899,
  normalYillikTutar: 9061.92,
  kurucuAylikTutar: 699,
  kurucuYillikTutar: 8557.92,
  kampanyaKodu: "kurucu-100-2026",
  kurucuKontenjanKalan: 24,
  paraBirimi: "TRY",
  denemeGunSayisi: 0
};

const baseSummary = {
  isletmeId: 42,
  isletmeAdi: "Örnek Muhasebe",
  hesapTipi: "Muhasebeci",
  haklar: {
    planKodu: "muhasebeci_standart",
    planAdi: "Standart",
    kaynak: "Deneme",
    aylikTutar: 799,
    yillikTutar: 0,
    faturalamaDonemi: "Aylik",
    donemTutari: 799,
    paraBirimi: "TRY",
    aiAktif: true,
    aiMesajLimiti: null,
    kullaniciLimiti: null,
    faturaLimiti: null,
    isletmeLimiti: null,
    gelirGiderIslemLimiti: null,
    cariKartLimiti: null,
    urunHizmetLimiti: null,
    musteriLimiti: 12,
    ekMusteriKredisi: 2,
    saltOkunur: false,
    gecerliBitisAt: "2026-08-15T12:00:00Z"
  },
  durum: "Deneme",
  sonrakiYenilemeAt: "2026-08-15T12:00:00Z",
  donemSonundaIptal: false,
  iptalEdilebilir: true,
  deneme: {
    planKodu: "muhasebeci_standart",
    faturalamaDonemi: "Aylik",
    ekMusteriKredisi: 2,
    durum: "Deneme",
    baslangicAt: "2026-08-01T12:00:00Z",
    bitisAt: "2026-08-15T12:00:00Z",
    odemeYontemiEklendi: false,
    donemSonundaIptal: false,
    iptalAt: null
  },
  abonelik: null,
  odemeler: []
};

async function mockWorkspace(page: Page, summary = baseSummary, expectedBilling: "Aylik" | "Yillik" = "Aylik", expectedCredits = 2, paytrContactRequired = false) {
  await page.route("**/api/**", async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === "/api/public/config") return json(route, { clerk: { enabled: false } });
    if (path === "/api/ekran/ust-bar") {
      return json(route, {
        aktifIsletmeId: 42,
        aktifIsletme: "Örnek Muhasebe",
        hesapTipi: "Muhasebeci",
        muhasebeciMusteriBaglami: false,
        muhasebeciAdi: "",
        muhasebeciYetkiSeviyesi: "Tam",
        bildirimVar: false,
        bildirimSayisi: 0,
        sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
        telegramAktif: false,
        isletmeler: [{ id: 42, ad: "Örnek Muhasebe", aktif: true }]
      });
    }
    if (path === "/api/ekran/kolay-kurulum") {
      return json(route, {
        tamamlandi: true,
        isletmeId: 42,
        isletmeAdi: "Örnek Muhasebe",
        hesapTipi: "Muhasebeci",
        isletmeTuru: "MuhasebeOfisi",
        konum: "İstanbul / Kadıköy",
        muhasebeciVarMi: false,
        mesaj: "",
        turler: []
      });
    }
    if (path === "/api/abonelik/ozet") return json(route, summary);
    if (path === "/api/public/planlar") return json(route, [plan]);
    if (path === "/api/abonelik/teklif") {
      expect(new URL(request.url()).searchParams.get("faturalamaDonemi")).toBe(expectedBilling);
      expect(new URL(request.url()).searchParams.get("ekMusteriKredisi")).toBe(String(expectedCredits));
      const annual = expectedBilling === "Yillik";
      const netAmount = annual ? 9565.92 : 799;
      const vatAmount = annual ? 1913.18 : 159.8;
      return json(route, {
        fiyat: {
          planCode: "muhasebeci_standart",
          accountType: "Muhasebeci",
          billingPeriod: expectedBilling,
          currency: "TRY",
          netAmount,
          vatRate: 20,
          vatAmount,
          totalAmount: netAmount + vatAmount,
          trialDays: 0,
          extraCustomerCredits: expectedCredits,
          includedCustomerCount: 10,
          customerCreditUnitAmount: annual ? 504 : 50,
          campaignCode: "kurucu-100-2026",
          isFounderPrice: true,
          listNetAmount: annual ? 10069.92 : 999,
          renewalNetAmount: annual ? 10069.92 : 999,
          discountedPeriodCount: annual ? 1 : 3,
          fullPeriodNetAmount: netAmount,
          prorationCreditNetAmount: 0,
          changeType: "YeniAbonelik",
          effectiveAt: null,
          targetPeriodEndAt: null
        },
        kampanyaKodu: "kurucu-100-2026",
        onayMetniSurumu: "abonelik-onayi-2026-08-v4",
        onayMetni: "Aylık yenileme, dönem sonu iptal ve emredici yasal haklar saklıdır.",
        ...(paytrContactRequired ? { paytrContactRequired: true } : {})
      });
    }
    if (path === "/api/abonelik/checkout") {
      const payload = request.postDataJSON();
      expect(payload).toMatchObject({
        planKodu: "muhasebeci_standart",
        faturalamaDonemi: expectedBilling,
        ekMusteriKredisi: expectedCredits,
        kampanyaKodu: "kurucu-100-2026",
        onaylandi: true
      });
      if (paytrContactRequired) {
        expect(payload).toMatchObject({
          odemeAdSoyad: "Ayşe Yılmaz",
          odemeAdres: "Test Mahallesi No 1",
          odemeTelefon: "5551234567"
        });
      }
      return json(route, {
        odemeIslemiId: 99,
        checkoutUrl: "http://127.0.0.1:4173/checkout-sent",
        expiresAt: "2026-08-01T19:00:00Z",
        firstChargeAt: "2026-08-15T12:00:00Z",
        reused: false
      });
    }
    return json(route, { mesaj: `Unexpected API route: ${path}` }, 404);
  });
}

test("monthly checkout shows recurring credits, VAT and explicit consent", async ({ page }) => {
  await mockWorkspace(page);
  await page.goto("/app/abonelik?plan=muhasebeci_standart&credits=2");

  await expect(page.getByRole("heading", { name: "Abonelik ödemesi" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Aylık", exact: true })).toBeVisible();
  await expect(page.getByRole("spinbutton", { name: /\+1 müşteri kredisi/i })).toHaveValue("2");
  await expect(page.getByRole("dialog").getByText("12 müşteri", { exact: true })).toBeVisible();
  await expect(page.getByText("KDV (%20)")).toBeVisible();
  const contractButton = page.getByRole("button", { name: "Abonelik sözleşmesini" });
  await expect(page.getByText(/dönem sonu iptali/i)).not.toBeVisible();

  const continueButton = page.getByRole("button", { name: "Öde ve aboneliği başlat" });
  await expect(continueButton).toBeDisabled();
  await contractButton.click();
  const contractWindow = page.getByRole("dialog", { name: "Systemcel Abonelik, Yenileme, İptal ve İade Koşulları" });
  await expect(contractWindow).toBeVisible();
  await page.getByRole("button", { name: "Sözleşme penceresini kapat" }).click();
  await expect(contractWindow).not.toBeVisible();
  await page.getByRole("checkbox").press("Space");
  await expect(continueButton).toBeEnabled();
  await continueButton.click();
  await expect(page).toHaveURL(/\/checkout-sent$/);
});

for (const theme of ["light", "dark"]) {
  test(`checkout consent stays contained in a complete card in ${theme}`, async ({ page }, testInfo) => {
    test.skip(!["desktop-chromium", "mobile-small"].includes(testInfo.project.name));
    await page.addInitScript((value) => localStorage.setItem("systemcel.theme", value), theme);
    await mockWorkspace(page);
    await page.goto("/app/abonelik?plan=muhasebeci_standart&credits=2");
    const consent = page.locator(".billing-checkout-footer .billing-consent");
    const checkbox = consent.getByRole("checkbox");
    await checkbox.check();
    await consent.screenshot({ path: testInfo.outputPath(`consent-${theme}-checked.png`) });

    for (const checked of [true, false]) {
      await checkbox.setChecked(checked);
      const layout = await consent.evaluate((element) => {
        const card = element.getBoundingClientRect();
        const style = getComputedStyle(element);
        const controls = Array.from(element.querySelectorAll("label, .billing-consent__copy"))
          .map((child) => child.getBoundingClientRect());
        return {
          borders: [style.borderTopWidth, style.borderRightWidth, style.borderBottomWidth, style.borderLeftWidth].map(parseFloat),
          radius: parseFloat(style.borderTopLeftRadius),
          contained: controls.every((child) => child.left > card.left && child.right < card.right && child.top > card.top && child.bottom < card.bottom)
        };
      });
      expect(layout.borders.every((width) => width > 0)).toBe(true);
      expect(layout.radius).toBeGreaterThan(0);
      expect(layout.contained).toBe(true);
    }
    await checkbox.focus();
    await checkbox.press("Space");
    await expect(checkbox).toBeChecked();
    await expect(page.getByRole("button", { name: "Öde ve aboneliği başlat" })).toBeEnabled();
    const accessibility = await new AxeBuilder({ page }).include(".billing-checkout-footer").analyze();
    expect(accessibility.violations).toEqual([]);
    await page.getByRole("dialog", { name: "Abonelik ödemesi" }).screenshot({ path: testInfo.outputPath(`checkout-${theme}.png`) });
  });
}

test("PayTR checkout requires contact details and sends them to the API", async ({ page }) => {
  await mockWorkspace(page, baseSummary, "Aylik", 2, true);
  await page.goto("/app/abonelik?plan=muhasebeci_standart&credits=2");

  const dialog = page.getByRole("dialog", { name: "Abonelik ödemesi" });
  await expect(dialog.getByRole("textbox", { name: "Ad soyad" })).toHaveAttribute("required", "");
  await expect(dialog.getByRole("textbox", { name: "Adres" })).toHaveAttribute("required", "");
  await expect(dialog.getByRole("textbox", { name: "Telefon" })).toHaveAttribute("required", "");
  await dialog.getByRole("textbox", { name: "Ad soyad" }).fill("Ayşe Yılmaz");
  await dialog.getByRole("textbox", { name: "Adres" }).fill("Test Mahallesi No 1");
  await dialog.getByRole("textbox", { name: "Telefon" }).fill("5551234567");
  await dialog.getByRole("checkbox").check();
  await dialog.getByRole("button", { name: "Öde ve aboneliği başlat" }).click();
  await expect(page).toHaveURL(/\/checkout-sent$/);
});

test("plan modal traps focus, closes with Escape and returns focus to its trigger", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Desktop keyboard accessibility check");
  await mockWorkspace(page, baseSummary, "Aylik", 0);
  await page.goto("/app/abonelik");

  const trigger = page.getByRole("button", { name: "Deneme planını yönet" });
  await trigger.focus();
  await trigger.click();
  const dialog = page.getByRole("dialog", { name: "Abonelik ödemesi" });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByRole("heading", { name: "Abonelik ödemesi", exact: true })).toBeFocused();

  const results = await new AxeBuilder({ page })
    .include(".billing-modal")
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
    .analyze();
  expect(results.violations.filter(({ impact }) => impact === "serious" || impact === "critical")).toEqual([]);

  const close = dialog.getByRole("button", { name: "Pencereyi kapat" });
  await close.focus();
  await page.keyboard.press("Shift+Tab");
  await expect(dialog.getByRole("button", { name: "Daha sonra" })).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(close).toBeFocused();

  await page.keyboard.press("Escape");
  await expect(dialog).not.toBeVisible();
  await expect(trigger).toBeFocused();
});

test("annual checkout carries the selected period through consent and checkout", async ({ page }) => {
  await mockWorkspace(page, baseSummary, "Yillik");
  await page.goto("/app/abonelik?plan=muhasebeci_standart&billing=Yillik&credits=2");

  await expect(page.getByRole("button", { name: "Yıllık" })).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByText("LANSMANA ÖZEL")).toBeVisible();
  await expect(page.getByText("Bugünkü liste fiyatı")).toBeVisible();
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: "Öde ve aboneliği başlat" }).click();
  await expect(page).toHaveURL(/\/checkout-sent$/);
});

test("period-end cancellation remains visible until access ends", async ({ page }) => {
  await mockWorkspace(page, {
    ...baseSummary,
    durum: "Aktif",
    donemSonundaIptal: true,
    iptalEdilebilir: false,
    deneme: null,
    abonelik: {
      planKodu: "muhasebeci_standart",
      faturalamaDonemi: "Aylik",
      ekMusteriKredisi: 2,
      durum: "Aktif",
      donemTutari: 799,
      kampanyaKodu: "kurucu-100-2026",
      yenilemeDonemTutari: 999,
      indirimliDonemKalan: 2,
      paraBirimi: "TRY",
      donemBaslangicAt: "2026-08-01T12:00:00Z",
      donemBitisAt: "2026-09-01T12:00:00Z",
      toleransBitisAt: null,
      donemSonundaIptal: true,
      iptalAt: "2026-08-02T12:00:00Z"
    }
  });
  await page.goto("/app/abonelik");

  if ((page.viewportSize()?.width ?? 0) > 980) {
    const mainNavigation = page.getByRole("navigation", { name: "Ana menü" });
    await expect(mainNavigation.getByRole("link", { name: "Abonelik", exact: true })).toHaveCount(0);
    await expect(mainNavigation.getByRole("link", { name: "Ayarlar", exact: true })).toHaveClass(/active/);
    await expect(page.getByRole("navigation", { name: "Ayarlar alt menüsü" }).getByRole("link", { name: "Plan ve faturalama" })).toHaveClass(/active/);
  }

  await expect(page.getByText("İptal talebi alındı")).toBeVisible();
  await expect(page.getByText("Bu tarihe kadar plan haklarınızı kullanabilirsiniz.")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Plan hakları" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Yenile" })).toHaveCount(0);
  await expect(page.getByText(/webhook|checkout|sağlayıcı/i)).toHaveCount(0);
});

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
