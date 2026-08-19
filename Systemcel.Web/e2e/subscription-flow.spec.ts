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

async function mockWorkspace(page: Page, summary = baseSummary, expectedBilling: "Aylik" | "Yillik" = "Aylik") {
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
      expect(new URL(request.url()).searchParams.get("ekMusteriKredisi")).toBe("2");
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
          extraCustomerCredits: 2,
          includedCustomerCount: 10,
          customerCreditUnitAmount: annual ? 504 : 50,
          campaignCode: "kurucu-100-2026",
          isFounderPrice: true,
          listNetAmount: annual ? 10069.92 : 999,
          renewalNetAmount: annual ? 10069.92 : 999,
          discountedPeriodCount: annual ? 1 : 3
        },
        kampanyaKodu: "kurucu-100-2026",
        onayMetniSurumu: "abonelik-onayi-2026-08-v4",
        onayMetni: "Aylık yenileme, dönem sonu iptal ve emredici yasal haklar saklıdır."
      });
    }
    if (path === "/api/abonelik/checkout") {
      const payload = request.postDataJSON();
      expect(payload).toMatchObject({
        planKodu: "muhasebeci_standart",
        faturalamaDonemi: expectedBilling,
        ekMusteriKredisi: 2,
        kampanyaKodu: "kurucu-100-2026",
        onaylandi: true
      });
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

  await expect(page.getByRole("heading", { name: "Planınızı seçin ve koşulları onaylayın" })).toBeVisible();
  await expect(page.getByText("Aylık", { exact: true })).toBeVisible();
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
    await expect(page.getByRole("navigation", { name: "Ayarlar alt menüsü" }).getByRole("link", { name: "Plan ve Faturalama" })).toHaveClass(/active/);
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
