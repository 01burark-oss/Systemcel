import { expect, test, type Page, type Route } from "@playwright/test";

const plan = {
  kod: "muhasebeci_standart",
  ad: "Standart",
  hesapTipi: "Muhasebeci",
  aylikTutar: 799,
  yillikTutar: null,
  yillikEfektifAylikTutar: null,
  paraBirimi: "TRY",
  denemeGunSayisi: 14
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

async function mockWorkspace(page: Page, summary = baseSummary) {
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
      expect(new URL(request.url()).searchParams.get("faturalamaDonemi")).toBe("Aylik");
      expect(new URL(request.url()).searchParams.get("ekMusteriKredisi")).toBe("2");
      return json(route, {
        fiyat: {
          planCode: "muhasebeci_standart",
          accountType: "Muhasebeci",
          billingPeriod: "Aylik",
          currency: "TRY",
          netAmount: 799,
          vatRate: 20,
          vatAmount: 159.8,
          totalAmount: 958.8,
          trialDays: 14,
          extraCustomerCredits: 2,
          includedCustomerCount: 10,
          customerCreditUnitAmount: 0
        },
        onayMetniSurumu: "abonelik-onayi-2026-08-v2",
        onayMetni: "Aylık yenileme, dönem sonu iptal ve emredici yasal haklar saklıdır."
      });
    }
    if (path === "/api/abonelik/checkout") {
      const payload = request.postDataJSON();
      expect(payload).toMatchObject({
        planKodu: "muhasebeci_standart",
        faturalamaDonemi: "Aylik",
        ekMusteriKredisi: 2,
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

  const continueButton = page.getByRole("button", { name: "Kartı güvenle ekle" });
  await expect(continueButton).toBeDisabled();
  await contractButton.click();
  const contractWindow = page.getByRole("dialog", { name: "Systemcel Abonelik, Yenileme, İptal ve İade Koşulları" });
  await expect(contractWindow).toBeVisible();
  await page.getByRole("button", { name: "Sözleşme penceresini kapat" }).click();
  await expect(contractWindow).not.toBeVisible();
  await page.getByRole("checkbox").focus();
  await page.keyboard.press("Space");
  await expect(continueButton).toBeEnabled();
  await continueButton.click();
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
      paraBirimi: "TRY",
      donemBaslangicAt: "2026-08-01T12:00:00Z",
      donemBitisAt: "2026-09-01T12:00:00Z",
      toleransBitisAt: null,
      donemSonundaIptal: true,
      iptalAt: "2026-08-02T12:00:00Z"
    }
  });
  await page.goto("/app/abonelik");

  await expect(page.getByText("İptal talebi alındı")).toBeVisible();
  await expect(page.getByText("Bu tarihe kadar plan haklarınızı kullanabilirsiniz.")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Plan hakları" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Yenile" })).toHaveCount(0);
  await expect(page.getByText(/webhook|checkout|sağlayıcı/i)).toHaveCount(0);
});

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
