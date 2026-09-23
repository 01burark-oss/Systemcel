import { expect, test, type Page, type Route } from "@playwright/test";

test("güvenli ödeme QR mal kabulünde tedarikçiye bırakılır", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Supplier flow desktop smoke test");

  const api = await mockWorkspace(page);
  await page.addInitScript(() => window.localStorage.setItem("systemcel.analyticsConsent", "denied"));
  await page.goto("/app/tedarikci-pazaryeri");

  await page.getByRole("button", { name: "Sepete ekle" }).click();
  await page.getByRole("button", { name: "Sepet", exact: true }).click();
  await page.getByRole("textbox", { name: "Teslimat adresi" }).fill("Kadıköy, İstanbul");
  await page.getByRole("button", { name: "Güvenli öde ve sipariş ver" }).click();

  await expect(page.getByText("Sevk edildi", { exact: true })).toBeVisible();
  expect(api.createdBody).toEqual(expect.objectContaining({
    teslimatAdresi: "Kadıköy, İstanbul",
    vadeli: false,
    kalemler: [{ urunId: 10, miktar: 1 }]
  }));

  await page.getByRole("button", { name: "QR okut", exact: true }).click();
  await page.getByRole("textbox", { name: "QR kodu" }).fill("scq1_test");
  await page.getByRole("button", { name: "Etiketi bul" }).click();
  await expect(page.getByRole("heading", { name: "Filtre Kahve" })).toBeVisible();
  await page.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  await expect(page.getByText("Tamamlandı", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Ödemeyi kaydet" })).toHaveCount(0);
  expect(api.lastReceiptBody).toEqual(expect.objectContaining({
    kabulEdilenMiktar: 1,
    reddedilenMiktar: 0,
    idempotencyKey: expect.stringMatching(/^receipt-/)
  }));
});

test("mobil QR bağlantısı kamera izni olmadan elle mal kabule devam eder", async ({ page }, testInfo) => {
  test.skip(!["mobile-small", "mobile-webkit"].includes(testInfo.project.name), "En küçük mobil ve WebKit kabulü");
  const api = await mockWorkspace(page);
  await page.addInitScript((dark) => {
    window.localStorage.setItem("systemcel.analyticsConsent", "denied");
    window.localStorage.setItem("systemcel.theme", dark ? "dark" : "light");
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: { getUserMedia: async () => { throw new DOMException("Permission denied", "NotAllowedError"); } }
    });
  }, testInfo.project.name === "mobile-webkit");
  await page.goto("/app/tedarikci-pazaryeri?qr=scq1_test");

  await expect(page.getByRole("heading", { name: "QR ile mal kabul" })).toBeVisible();
  await expect(page.getByText("Dosya seçilmedi")).toBeVisible();
  await expect(page.getByText("Choose File")).toHaveCount(0);
  const aligned = await page.evaluate(() => {
    const code = document.querySelector<HTMLInputElement>(".supplier-marketplace__modal-body > label input:not([type='file'])")?.getBoundingClientRect();
    const picker = document.querySelector<HTMLElement>(".supplier-marketplace__camera-actions .supplier-marketplace__file-picker-control")?.getBoundingClientRect();
    return Boolean(code && picker && Math.abs(code.left - picker.left) <= 1 && Math.abs(code.right - picker.right) <= 1);
  });
  expect(aligned).toBe(true);
  await page.screenshot({ path: testInfo.outputPath("mobile-qr-picker.png") });
  const chooseFile = page.waitForEvent("filechooser");
  await page.getByText("Dosya seç", { exact: true }).click();
  const fileChooser = await chooseFile;
  expect(await fileChooser.element().getAttribute("aria-label")).toBe("QR görseli yükle");
  await fileChooser.setFiles({ name: "etiket.png", mimeType: "image/png", buffer: Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9YxNJ0kAAAAASUVORK5CYII=", "base64") });
  await expect(page.getByText("etiket.png")).toBeVisible();
  await page.getByRole("button", { name: "Kamerayı aç" }).click();
  await expect(page.getByRole("alert")).toContainText("Kamera izni alınamadı");
  await page.getByRole("button", { name: "Etiketi bul" }).click();
  await expect(page.getByRole("dialog", { name: "QR ile mal kabul" }).getByRole("heading", { name: "Filtre Kahve" })).toBeVisible();
  await expect(page.getByRole("spinbutton", { name: "Kabul edilen miktar" })).toHaveValue("1");
  const receiptDialog = page.getByRole("dialog", { name: "QR ile mal kabul" });
  await expect(receiptDialog.locator("label").filter({ hasText: "QR kodu" }).locator(".supplier-marketplace__required")).toHaveCount(1);
  await expect(receiptDialog.locator("label").filter({ hasText: "Ölçülen ağırlık" }).locator(".supplier-marketplace__required")).toHaveCount(0);
  const rejectedAmount = page.getByRole("spinbutton", { name: "Reddedilen miktar" });
  await rejectedAmount.fill("1");
  await expect(receiptDialog.locator("label").filter({ hasText: "Ret nedeni" }).locator(".supplier-marketplace__required")).toHaveCount(1);
  await expect(page.getByRole("combobox", { name: "2. ret nedeni (varsa)" })).toHaveCount(0);
  await page.getByRole("combobox", { name: "Ret nedeni" }).selectOption("Hasarlı ürün");
  const optionalReason = page.getByRole("combobox", { name: "2. ret nedeni (varsa)" });
  await expect(optionalReason).toBeVisible();
  await expect(receiptDialog.locator("label").filter({ hasText: "2. ret nedeni (varsa)" }).locator(".supplier-marketplace__required")).toHaveCount(0);
  await optionalReason.selectOption("Diğer");
  await expect(page.getByRole("textbox", { name: "2. ret nedeni açıklaması" })).toBeVisible();
  await expect(receiptDialog.locator("label").filter({ hasText: "2. ret nedeni açıklaması" }).locator(".supplier-marketplace__required")).toHaveCount(0);
  await rejectedAmount.fill("0");
  await expect(optionalReason).toHaveCount(0);
  const receiptAligned = await page.evaluate(() => {
    const code = document.querySelector<HTMLInputElement>(".supplier-marketplace__modal-body > label input:not([type='file'])")?.getBoundingClientRect();
    const amount = [...document.querySelectorAll<HTMLInputElement>(".supplier-marketplace__modal input[type='number']")]
      .find((input) => input.parentElement?.textContent?.includes("Kabul edilen miktar"))?.getBoundingClientRect();
    const evidence = [...document.querySelectorAll<HTMLElement>(".supplier-marketplace__file-picker")]
      .find((picker) => picker.textContent?.includes("Fotoğraf veya PDF kanıtı"))
      ?.querySelector<HTMLElement>(".supplier-marketplace__file-picker-control")?.getBoundingClientRect();
    return Boolean(code && amount && evidence && [amount, evidence].every((bounds) =>
      Math.abs(bounds.left - code.left) <= 1 && Math.abs(bounds.right - code.right) <= 1));
  });
  expect(receiptAligned).toBe(true);
  await page.getByText("Fotoğraf veya PDF kanıtı").scrollIntoViewIfNeeded();
  expect(await page.evaluate(() => [...document.querySelectorAll(".supplier-marketplace__modal input, .supplier-marketplace__modal button")]
    .filter((element) => { const bounds = element.getBoundingClientRect(); return bounds.width > 0 && bounds.right > window.innerWidth + 1; })
    .map((element) => element.outerHTML.slice(0, 120)))).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath("mobile-receipt-form.png") });
  await page.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  await expect(page.getByText("Mal kabul kaydedildi; tedarikçi hakedişi serbest bırakıldı.")).toBeVisible();
  expect(api.lastReceiptBody).toEqual(expect.objectContaining({ kabulEdilenMiktar: 1, reddedilenMiktar: 0 }));
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath("mobile-receipt.png"), fullPage: true });
});

test("mal kabul iki ret nedenini ayrı gönderir", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "mobile-small", "Mobil ret formu");
  const api = await mockWorkspace(page);
  await page.addInitScript(() => window.localStorage.setItem("systemcel.analyticsConsent", "denied"));
  await page.goto("/app/tedarikci-pazaryeri?qr=scq1_test");
  await page.getByRole("button", { name: "Etiketi bul" }).click();
  await page.getByRole("spinbutton", { name: "Kabul edilen miktar" }).fill("0");
  await page.getByRole("spinbutton", { name: "Reddedilen miktar" }).fill("1");
  await page.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  expect(api.lastReceiptBody).toBeUndefined();
  await page.getByRole("combobox", { name: "Ret nedeni" }).selectOption("Hasarlı ürün");
  await page.getByRole("combobox", { name: "2. ret nedeni (varsa)" }).selectOption("Diğer");
  await page.getByRole("textbox", { name: "2. ret nedeni açıklaması" }).fill("Paket mührü yok");
  await page.getByRole("combobox", { name: "2. ret nedeni (varsa)" }).scrollIntoViewIfNeeded();
  await page.screenshot({ path: testInfo.outputPath("mobile-rejection-reasons.png") });
  await page.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  await expect(page.getByText("Mal kabul kaydedildi; tedarikçi hakedişi serbest bırakıldı.")).toBeVisible();
  expect(api.lastReceiptBody).toEqual(expect.objectContaining({
    kabulEdilenMiktar: 0,
    reddedilenMiktar: 1,
    redNedeni: "Hasarlı ürün",
    ikinciRedNedeni: "Diğer: Paket mührü yok"
  }));
});

test("sıcaklık hatasında QR ve girilen kabul bilgileri korunur", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "mobile-small", "Mobil sıcaklık kontrolü");
  const api = await mockWorkspace(page, { temperatureRange: true });
  await page.addInitScript(() => window.localStorage.setItem("systemcel.analyticsConsent", "denied"));
  await page.goto("/app/tedarikci-pazaryeri?qr=scq1_test");
  await page.getByRole("button", { name: "Etiketi bul" }).click();
  const dialog = page.getByRole("dialog", { name: "QR ile mal kabul" });
  await expect(dialog.getByText("İzin verilen sıcaklık: -2–5 °C. Kabul için ölçüm girin.")).toBeVisible();
  const temperature = dialog.getByRole("spinbutton", { name: "Ölçülen sıcaklık (°C)" });
  await expect(temperature).toHaveAttribute("required", "");
  await temperature.fill("8");
  await dialog.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  await expect(dialog.getByRole("heading", { name: "Filtre Kahve" })).toBeVisible();
  await expect(dialog.getByRole("textbox", { name: "QR kodu" })).toHaveValue("scq1_test");
  await expect(temperature).toHaveValue("8");
  await expect(page.getByRole("alert")).toContainText("sıcaklık sevkiyat aralığı dışında");
  await temperature.fill("0");
  await dialog.getByRole("button", { name: "Mal kabulü kaydet" }).click();
  await expect(dialog).toHaveCount(0);
  expect(api.lastReceiptBody).toEqual(expect.objectContaining({ olculenSicaklik: 0 }));
});

test("yönetici itirazında ödeme ve muhasebe referansları izlenir", async ({ page }, testInfo) => {
  test.skip(!["desktop-chromium", "mobile-small"].includes(testInfo.project.name), "Yönetim inceleme görünümü");
  await mockWorkspace(page, { adminReview: true });
  await page.addInitScript((dark) => {
    window.localStorage.setItem("systemcel.analyticsConsent", "denied");
    window.localStorage.setItem("systemcel.theme", dark ? "dark" : "light");
  }, testInfo.project.name === "mobile-small");
  await page.goto("/app/tedarikci-pazaryeri");
  await page.getByRole("button", { name: "Yönetim" }).click();
  await page.getByRole("button", { name: "İtirazı incele" }).click();
  const dialog = page.getByRole("dialog", { name: /itirazını çöz/ });
  await dialog.getByText("Ödeme, fatura ve hareket bağlantıları").click();
  await expect(dialog.getByText(/PSP tahsilatı #31/)).toBeVisible();
  await expect(dialog.getByText(/Fatura eşlemesi: alıcı #41, satıcı #42/)).toBeVisible();
  await expect(dialog.getByText(/Stok #51: Giris 1 · Kabul #61/)).toBeVisible();
  await expect(dialog.getByText(/Cari #71: Borc/)).toBeVisible();
  await expect(dialog.getByText(/Hakediş #81/)).toBeVisible();
  expect(await dialog.evaluate((element) => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath("review-references.png") });
});

async function mockWorkspace(page: Page, options: { temperatureRange?: boolean; adminReview?: boolean } = {}) {
  let state = "";
  const api: { createdBody?: unknown; lastReceiptBody?: unknown } = {};

  await page.route("**/api/**", async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === "/api/public/config") return json(route, { clerk: { enabled: false } });
    if (path === "/api/public/planlar") return json(route, []);
    if (path === "/api/ekran/ust-bar") return json(route, {
      aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
      muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "TamIslem",
      bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
      telegramAktif: false, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
    });
    if (path === "/api/ekran/kolay-kurulum") return json(route, {
      tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
      isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
    });
    if (path === "/api/ekran/sohbetler") return json(route, { sohbetler: [], okunmamisMesajSayisi: 0 });
    if (path === "/api/ekran/tedarikci-pazaryeri" && request.method() === "GET") return json(route, marketplace(state, options.adminReview));
    if (path === "/api/ekran/yonetim/tedarikci-siparisler/88/inceleme" && options.adminReview)
      return json(route, {
        shipments: [{ id: 21, sevkiyatNo: "SEVK-21", belgeNo: "IRS-21", belgeUuid: "", belgeDosyaYolu: "", durum: "Sorunlu" }],
        receipts: [{ id: 61, kabulEdilenMiktar: 1, reddedilenMiktar: 0, redNedeni: "", not: "", islemYapanKullaniciRef: "depo", belgeKarmasi: "", fotoKanitiYolu: "", createdAt: "2026-09-23T12:00:00Z" }],
        complaints: [], history: [], references: {
          paymentAllocations: [{ id: 31, saglayici: "Fake", saglayiciIslemId: "PSP-31", durum: "Basarili", tutar: 120, brutTutar: 120, iadeTutari: 0 }],
          invoiceMatch: { aliciFaturaId: 41, saticiFaturaId: 42, tedarikciBelgeNo: "F-42", tedarikciBelgeUuid: "" },
          invoices: [{ id: 41, isletmeId: 42, faturaTipi: "Alis", yerelFaturaNo: "F-41", portalBelgeNo: "", genelToplam: 120, durum: "Kesildi" }],
          settlement: { id: 81, durum: "Bloke", netTutar: 110, odenenTutar: 0, aktarimReferansi: "" },
          stockMovements: [{ id: 51, isletmeId: 42, tedarikciSevkiyatId: null, tedarikciMalKabulId: 61, hareketTipi: "Giris", miktar: 1 }],
          cariMovements: [{ id: 71, isletmeId: 42, tedarikciMalKabulId: 61, hareketTipi: "Borc", tutar: 120 }],
          paymentMovements: []
        }
      });
    if (path === "/api/ekran/tedarikci-pazaryeri/siparisler" && request.method() === "POST") {
      api.createdBody = request.postDataJSON();
      state = "OdemeBekliyor";
      return json(route, { id: 77, mesaj: "Sipariş oluşturuldu." });
    }
    if (path === "/api/ekran/tedarikci-pazaryeri/siparisler/77/odeme" && request.method() === "POST") {
      state = "SevkEdildi";
      return json(route, { mesaj: "Ödeme güvenli biçimde alındı." });
    }
    if (path === "/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/scq1_test" && request.method() === "GET") {
      return json(route, {
        kod: "scq1_test", siparisId: 88, siparisNo: "PAZ-77-01", urunAdi: "Filtre Kahve",
        sku: "KAHVE", birim: "Adet", miktar: 1, lotNo: "", durum: "Aktif",
        sicaklikMin: options.temperatureRange ? -2 : null, sicaklikMax: options.temperatureRange ? 5 : null
      });
    }
    if (path === "/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/scq1_test/mal-kabul" && request.method() === "POST") {
      api.lastReceiptBody = request.postDataJSON();
      if (options.temperatureRange && (api.lastReceiptBody as { olculenSicaklik: number }).olculenSicaklik > 5)
        return json(route, { mesaj: "Ölçülen sıcaklık sevkiyat aralığı dışında." }, 400);
      state = (api.lastReceiptBody as { reddedilenMiktar: number }).reddedilenMiktar > 0 ? "Itirazli" : "Tamamlandi";
      return json(route, { mesaj: "Mal kabul kaydedildi; tedarikçi hakedişi serbest bırakıldı." });
    }
    return json(route, { mesaj: `Unexpected route: ${path}` }, 404);
  });
  return api;
}

function marketplace(state: string, adminReview = false) {
  const hasOrder = Boolean(state);
  return {
    aktifIsletmeId: 42, guvenliOdemeHazir: true, malKabulYetkisi: true, yonetici: adminReview, profiller: [], talepler: [], acikTalepler: [], gelenTeklifler: [],
    yonetimProfilleri: [], yonetimSiparisler: adminReview ? [{ id: 88, siparisNo: "PAZ-77-01", tedarikciUnvani: "Marmara Gıda", durum: "Itirazli", genelToplam: 120, paraBirimi: "TRY", hakedisDurumu: "Bloke" }] : [],
    profil: null, benimUrunlerim: [], kaynakUrunler: [],
    urunler: [{
      id: 10, tedarikciProfilId: 20, tedarikciUnvani: "Marmara Gıda", tedarikciSehri: "İstanbul",
      sevkiyatBolgeleri: "Türkiye", iadeKosullari: "14 gün", sku: "KAHVE", ad: "Filtre Kahve",
      aciklama: "1 kg", kategori: "Gıda", birim: "Adet", birimFiyat: 100, kdvOrani: 20,
      paraBirimi: "TRY", kullanilabilirStok: 10, minimumSiparisMiktari: 1, tahminiTeslimatGun: 2
    }],
    anaSiparisler: hasOrder ? [{ id: 77, siparisNo: "PAZ-77", teslimatAdresi: "Kadıköy, İstanbul", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", durum: state, createdAt: "2026-09-17T12:00:00Z" }] : [],
    siparisler: hasOrder ? [{ id: 88, anaSiparisId: 77, anaSiparisNo: "PAZ-77", siparisNo: "PAZ-77-01", teslimatAdresi: "Kadıköy, İstanbul", aliciIsletmeId: 42, tedarikciIsletmeId: 2, tedarikciUnvani: "Marmara Gıda", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", komisyonTutari: 0, komisyonKdvTutari: 0, tevkifatTutari: 0, odemeHizmetiBedeli: 0, tedarikciHakEdisi: 120, durum: state, kargoFirmasi: "Kargo", kargoTakipNo: "TRK-1", createdAt: "2026-09-17T12:00:00Z", malKabulVar: state === "Tamamlandi" }] : [],
    siparisKalemleri: hasOrder ? [{ id: 1, tedarikciSiparisId: 88, tedarikciUrunId: 10, sku: "KAHVE", ad: "Filtre Kahve", birim: "Adet", miktar: 1, sevkEdilenMiktar: 1, kabulEdilenMiktar: state === "Tamamlandi" ? 1 : 0, reddedilenMiktar: 0, birimFiyat: 100, kdvOrani: 20, toplamTutar: 120 }] : []
  };
}

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
