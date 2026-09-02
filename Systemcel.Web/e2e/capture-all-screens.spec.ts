import { expect, test, type Browser, type Page, type Route } from "@playwright/test";
import { fileURLToPath } from "node:url";
import { pathToFileURL } from "node:url";
import path from "node:path";
import { promises as fs } from "node:fs";

type Theme = "light" | "dark";
type CaptureCase = { slug: string; path: string; ready: string };
type ControlIssue = { theme: Theme; screen: string; label: string; kind: "overflow" | "off-center" | "low-contrast"; detail: string };

const webRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const artifactsRoot = path.resolve(webRoot, "..", "artifacts");
const outputRoot = process.env.SYSTEMCEL_SCREENSHOT_DIR
  ? path.resolve(process.env.SYSTEMCEL_SCREENSHOT_DIR)
  : path.join(artifactsRoot, "ui-screenshots");

if (outputRoot === artifactsRoot || !outputRoot.startsWith(`${artifactsRoot}${path.sep}`)) {
  throw new Error("SYSTEMCEL_SCREENSHOT_DIR must be a child of the repository artifacts directory.");
}

const screens: CaptureCase[] = [
  { slug: "01-ana-sayfa", path: "/app", ready: "Belgeler hazır mı?" },
  { slug: "02-finans-durumu", path: "/app/finansal-gorunum", ready: "Alacakların durumu" },
  { slug: "03-gelir-gider", path: "/app/gelir-gider", ready: "Yeni kayıt" },
  { slug: "04-hizli-satis", path: "/app/hizli-satis", ready: "Ürün seç" },
  { slug: "05-urun-ve-stok", path: "/app/urun-stok", ready: "Son hareketler" },
  { slug: "06-stok-defteri", path: "/app/stok-defteri", ready: "Hareket geçmişi" },
  { slug: "07-cari-hesaplar", path: "/app/cari-hesaplar", ready: "Yeni hesap" },
  { slug: "08-faturalar", path: "/app/faturalar", ready: "Faturalar" },
  { slug: "09-tahsilat-ve-odeme", path: "/app/tahsilat-odeme", ready: "Tahsilat ve ödeme" },
  { slug: "10-banka-eslestirme", path: "/app/banka-eslestirme", ready: "Banka hareketi eşleştirme" },
  { slug: "11-raporlar", path: "/app/raporlar", ready: "Rapor oluştur" },
  { slug: "12-sohbetler", path: "/app/sohbetler?sohbetId=1", ready: "Ayşe Mali Müşavirlik" },
  { slug: "13-muhasebeciler", path: "/app/muhasebeciler", ready: "Ada Muhasebe" },
  { slug: "14-muhasebeci-paneli", path: "/app/muhasebeci", ready: "Muhasebeci paneli" },
  { slug: "15-muhasebeci-musterileri", path: "/app/muhasebeci/musteriler", ready: "Müşterilerim" },
  { slug: "16-plan-ve-faturalama", path: "/app/abonelik", ready: "Plan hakları" },
  { slug: "17-ayarlar-isletme", path: "/app/ayarlar?sekme=isletme", ready: "İşletme Ayarları" },
  { slug: "18-ayarlar-gib", path: "/app/ayarlar?sekme=gib", ready: "Portal ayarları" },
  { slug: "19-ayarlar-telegram", path: "/app/ayarlar?sekme=telegram", ready: "Telegram bağlantısı" },
  { slug: "20-yonetim-muhasebeci-basvurulari", path: "/app/yonetim/muhasebeci-basvurulari", ready: "Ada Mali Müşavirlik" },
  { slug: "21-yonetim-odeme-inceleme", path: "/app/yonetim/odemeler", ready: "Örnek İşletme" },
  { slug: "22-yonetim-muhasebeci-aktarimlari", path: "/app/yonetim/muhasebeci-aktarimlari", ready: "Ada Muhasebe" },
  { slug: "23-yonetim-destek", path: "/app/yonetim/destek", ready: "Öncelikli AŞ" },
  { slug: "24-yardim", path: "/yardim", ready: "Bir destek talebi oluştur" },
  { slug: "25-kilit-ekrani", path: "/kilit-ekrani", ready: "Systemcel Giriş" }
];

test.skip(!process.env.SYSTEMCEL_CAPTURE, "Run with npm run capture:screens");
test.describe.configure({ mode: "serial" });
test.setTimeout(240_000);

test("theme geometry is identical across application screens", async ({ browser }, testInfo) => {
  test.skip(!process.env.SYSTEMCEL_GEOMETRY || testInfo.project.name !== "desktop-wide", "Explicit geometry audit");
  const page = await createCapturePage(browser, "light");
  if (process.env.SYSTEMCEL_GEOMETRY_WIDTH) await page.setViewportSize({ width: Number(process.env.SYSTEMCEL_GEOMETRY_WIDTH), height: 1080 });
  await mockApplication(page);
  const issues: unknown[] = [];
  for (const screen of screens.filter(screen => screen.path.startsWith("/app"))) {
    await page.goto(screen.path);
    await expect(page.getByText(screen.ready, { exact: false }).first()).toBeVisible();
    await page.waitForLoadState("networkidle");
    await page.evaluate(() => document.fonts.ready);
    const differences = await page.evaluate(() => {
      const root = document.documentElement;
      const elements = [...document.querySelectorAll<HTMLElement>(".react-shell main, .react-shell section, .react-shell article, .react-shell aside, .react-shell header, .react-shell button, .react-shell input, .react-shell select, .react-shell h1, .react-shell h2, .react-shell p, .react-shell div")];
      const rect = (el: HTMLElement) => {
        const r = el.getBoundingClientRect();
        return [r.x, r.y, r.width, r.height];
      };
      root.dataset.theme = "light";
      const before = elements.map(rect);
      root.dataset.theme = "dark";
      return elements.flatMap((el, i) => {
        const after = rect(el);
        if (!before[i][2] || !after[2] || !before[i].some((v, j) => Math.abs(v - after[j]) > .02)) return [];
        return [{ element: el.tagName + "." + el.className, text: el.textContent?.trim().slice(0, 45), light: before[i], dark: after }];
      });
    });
    if (differences.length) issues.push({ screen: screen.slug, differences });
  }
  await fs.mkdir(outputRoot, { recursive: true });
  await fs.writeFile(path.join(outputRoot, "theme-geometry.json"), JSON.stringify(issues, null, 2));
  await page.context().close();
  expect(issues).toEqual([]);
});

test("repaired import, collapse and receipt controls", async ({ browser }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-wide");
  for (const theme of ["light", "dark"] as const) {
    const page = await createCapturePage(browser, theme);
    await mockApplication(page);
    await page.route("**/api/ekran/banka-mutabakat/import", route => json(route, { eklenen: 1, tekrar: 0, toplam: 1 }));
    await page.goto("/app/banka-eslestirme");
    const importer = page.getByRole("button", { name: "İçe aktar", exact: true });
    await expect(importer).toBeEnabled();
    const chooser = page.waitForEvent("filechooser");
    await importer.click();
    await (await chooser).setFiles({ name: "hareketler.csv", mimeType: "text/csv", buffer: Buffer.from("Tarih,Aciklama,Tutar\n2026-09-01,Test,100") });
    await expect(page.getByText("1 hareket eklendi.")).toBeVisible();
    await page.locator(".bank-import").screenshot({ path: testInfo.outputPath(`bank-${theme}.png`) });
    await page.goto("/app/tahsilat-odeme");
    await page.getByRole("button", { name: "Tahsilat ve ödeme panelini kapat" }).click();
    const reopen = page.getByRole("button", { name: "Tahsilat ve ödeme panelini aç" });
    await expect(reopen).toBeVisible();
    await page.locator(".payment-form-card").screenshot({ path: testInfo.outputPath(`collapsed-${theme}.png`) });
    await reopen.click();
    await expect(page.locator("#payment-form-body")).toBeVisible();
    await page.locator(".payment-form-card").screenshot({ path: testInfo.outputPath(`payment-${theme}.png`) });
    await page.route("**/api/ekran/mobil-tarama/durum", route => json(route, { fisOcrHazir: true }));
    await page.route("**/api/ekran/mobil-tarama/fis-ocr", route => json(route, { merchant: "Örnek Market", receiptDate: "2026-09-01", receiptTotal: 100, paymentMethod: "Nakit", items: [] }));
    await page.goto("/app/hizli-satis");
    await page.getByLabel("Fiş fotoğrafı").setInputFiles({ name: "fis.jpg", mimeType: "image/jpeg", buffer: Buffer.from("fixture") });
    const date = page.getByLabel("Fiş tarihi");
    await expect(date).toBeVisible();
    await expect(date).toHaveCSS("border-top-width", "1px");
    await expect(date).toHaveCSS("border-bottom-width", "1px");
    await page.getByRole("article", { name: "Okunan fiş" }).screenshot({ path: testInfo.outputPath(`receipt-${theme}.png`) });
    await page.context().close();
  }
});

test("captures every application route in light and dark themes", async ({ browser }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-wide", "The capture command uses one deterministic desktop viewport");
  await fs.rm(outputRoot, { recursive: true, force: true });

  const manifest: Array<CaptureCase & { theme: Theme; file: string }> = [];
  const controlIssues: ControlIssue[] = [];
  for (const theme of ["light", "dark"] as const) {
    const page = await createCapturePage(browser, theme);
    await mockApplication(page);
    const themeDirectory = path.join(outputRoot, theme);
    await fs.mkdir(themeDirectory, { recursive: true });

    for (const screen of screens) {
      await page.goto(screen.path, { waitUntil: "domcontentloaded" });
      await expect(page.getByText(screen.ready, { exact: false }).first(), `${screen.path} did not reach its ready state`).toBeVisible({ timeout: 15_000 });
      await page.waitForLoadState("networkidle");
      controlIssues.push(...(await auditControls(page)).map((issue) => ({ ...issue, theme, screen: screen.slug })));
      await page.addStyleTag({ content: `
        *,*::before,*::after{animation:none!important;transition:none!important;caret-color:transparent!important}
        html,body,#root{height:auto!important;min-height:100%!important;overflow:visible!important}
        .react-shell{height:auto!important;min-height:100vh!important;overflow:visible!important}
        .react-shell__main,.react-shell__body{height:auto!important;max-height:none!important;overflow:visible!important}
        .react-sidebar{position:sticky!important;top:0!important;height:100vh!important;align-self:start!important}
      ` });
      await page.evaluate(async () => {
        await document.fonts.ready;
        window.scrollTo(0, 0);
      });
      const file = path.join(themeDirectory, `${screen.slug}.png`);
      await page.screenshot({ path: file, fullPage: true, animations: "disabled" });
      manifest.push({ ...screen, theme, file: path.relative(outputRoot, file).replaceAll("\\", "/") });
    }

    await page.context().close();
  }

  await fs.writeFile(path.join(outputRoot, "manifest.json"), JSON.stringify({ generatedAt: new Date().toISOString(), viewport: { width: 1920, height: 1080 }, screens: manifest }, null, 2));
  await fs.writeFile(path.join(outputRoot, "ui-audit.json"), JSON.stringify({ generatedAt: new Date().toISOString(), issues: controlIssues }, null, 2));
  await writeGallery();
  await captureOverview(browser, "light");
  await captureOverview(browser, "dark");
  expect(manifest).toHaveLength(screens.length * 2);
  expect(controlIssues, `UI denetiminde ${controlIssues.length} sorun bulundu; artifacts/ui-screenshots/ui-audit.json dosyasını inceleyin`).toEqual([]);
});

async function auditControls(page: Page): Promise<Array<Omit<ControlIssue, "theme" | "screen">>> {
  return page.locator("button, a[role='button'], select, [class$='__icon']").evaluateAll((controls) => {
    type Rgba = { r: number; g: number; b: number; a: number };
    const issues: Array<{ label: string; kind: "overflow" | "off-center" | "low-contrast"; detail: string }> = [];
    const parseColor = (value: string): Rgba | null => {
      const parts = value.match(/[\d.]+/g)?.map(Number);
      if (!parts || parts.length < 3) return null;
      return { r: parts[0], g: parts[1], b: parts[2], a: parts[3] ?? 1 };
    };
    const blend = (front: Rgba, back: Rgba): Rgba => {
      const alpha = front.a + back.a * (1 - front.a);
      if (alpha === 0) return { r: 0, g: 0, b: 0, a: 0 };
      return {
        r: (front.r * front.a + back.r * back.a * (1 - front.a)) / alpha,
        g: (front.g * front.a + back.g * back.a * (1 - front.a)) / alpha,
        b: (front.b * front.a + back.b * back.a * (1 - front.a)) / alpha,
        a: alpha
      };
    };
    const effectiveBackground = (element: Element): Rgba => {
      let result: Rgba = { r: 255, g: 255, b: 255, a: 0 };
      let current: Element | null = element;
      while (current && result.a < .999) {
        const layer = parseColor(getComputedStyle(current).backgroundColor);
        if (layer && layer.a > 0) result = blend(result, layer);
        current = current.parentElement;
      }
      return result.a < .999 ? blend(result, { r: 255, g: 255, b: 255, a: 1 }) : result;
    };
    const luminance = ({ r, g, b }: Rgba) => {
      const channel = (value: number) => {
        const normalized = value / 255;
        return normalized <= .04045 ? normalized / 12.92 : ((normalized + .055) / 1.055) ** 2.4;
      };
      return .2126 * channel(r) + .7152 * channel(g) + .0722 * channel(b);
    };
    const contrast = (foreground: Rgba, background: Rgba) => {
      const front = foreground.a < .999 ? blend(foreground, background) : foreground;
      const light = Math.max(luminance(front), luminance(background));
      const dark = Math.min(luminance(front), luminance(background));
      return (light + .05) / (dark + .05);
    };
    const colorText = ({ r, g, b }: Rgba) => `rgb(${Math.round(r)}, ${Math.round(g)}, ${Math.round(b)})`;
    for (const control of controls) {
      const element = control as HTMLElement;
      if (element.matches(".finance-chart__week-buttons button")) continue;
      const style = getComputedStyle(element);
      const rect = element.getBoundingClientRect();
      if (style.display === "none" || style.visibility === "hidden" || rect.width < 1 || rect.height < 1 || element.matches(":disabled")) continue;
      const label = (element.getAttribute("aria-label") || element.innerText || element.className || element.tagName).replace(/\s+/g, " ").trim().slice(0, 90);
      const background = effectiveBackground(element);
      const limeSurface = background.r > 120 && background.g > 180 && background.b < 150;
      if (limeSurface) {
        const foreground = parseColor(style.color);
        if (foreground) {
          const ratio = contrast(foreground, background);
          if (ratio < 4.5) issues.push({ label, kind: "low-contrast", detail: `${colorText(foreground)} / ${colorText(background)} = ${ratio.toFixed(2)}:1` });
        }
        element.querySelectorAll("svg").forEach((icon) => {
          const iconColor = parseColor(getComputedStyle(icon).color);
          if (!iconColor) return;
          const ratio = contrast(iconColor, background);
          if (ratio < 3) issues.push({ label: `${label} (ikon)`, kind: "low-contrast", detail: `${colorText(iconColor)} / ${colorText(background)} = ${ratio.toFixed(2)}:1` });
        });
      }
      if (element.scrollWidth > element.clientWidth + 1 || element.scrollHeight > element.clientHeight + 1) {
        issues.push({ label, kind: "overflow", detail: `${element.clientWidth}x${element.clientHeight} içinde ${element.scrollWidth}x${element.scrollHeight}` });
      }
      const centeredLayout = style.justifyContent === "center" || (style.display.includes("grid") && style.placeItems.includes("center"));
      if (!centeredLayout || !label) continue;
      const contentRects: DOMRect[] = [];
      const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
      let node: Node | null;
      while ((node = walker.nextNode())) {
        if (!node.textContent?.trim() || (node.parentElement as HTMLElement | null)?.offsetParent === null) continue;
        const range = document.createRange();
        range.selectNodeContents(node);
        const textRect = range.getBoundingClientRect();
        if (textRect.width && textRect.height) contentRects.push(textRect);
      }
      element.querySelectorAll("svg,img").forEach((icon) => {
        const iconRect = icon.getBoundingClientRect();
        if (iconRect.width && iconRect.height) contentRects.push(iconRect);
      });
      if (!contentRects.length) continue;
      const left = Math.min(...contentRects.map((item) => item.left));
      const right = Math.max(...contentRects.map((item) => item.right));
      const top = Math.min(...contentRects.map((item) => item.top));
      const bottom = Math.max(...contentRects.map((item) => item.bottom));
      const dx = Math.abs((left + right) / 2 - (rect.left + rect.right) / 2);
      const dy = Math.abs((top + bottom) / 2 - (rect.top + rect.bottom) / 2);
      if (dx > 4 || dy > 5) issues.push({ label, kind: "off-center", detail: `merkez farkı x=${dx.toFixed(1)}px y=${dy.toFixed(1)}px` });
    }
    return issues;
  });
}

async function writeGallery() {
  const cards = screens.map((screen) => `
    <article>
      <header><strong>${screenName(screen)}</strong><code>${screen.path}</code></header>
      <div class="pair">
        <figure><figcaption>Light</figcaption><a href="light/${screen.slug}.png"><img src="light/${screen.slug}.png" alt="${screenName(screen)} açık tema"></a></figure>
        <figure><figcaption>Dark</figcaption><a href="dark/${screen.slug}.png"><img src="dark/${screen.slug}.png" alt="${screenName(screen)} koyu tema"></a></figure>
      </div>
    </article>`).join("");
  await fs.writeFile(path.join(outputRoot, "index.html"), galleryDocument("Systemcel ekran galerisi", cards, false));
}

async function captureOverview(browser: Browser, theme: Theme) {
  const cards = screens.map((screen) => `
    <article class="overview-card">
      <header><strong>${screenName(screen)}</strong><code>${screen.path}</code></header>
      <img src="${theme}/${screen.slug}.png" alt="${screenName(screen)} ${theme}">
    </article>`).join("");
  const htmlPath = path.join(outputRoot, `.overview-${theme}.html`);
  await fs.writeFile(htmlPath, galleryDocument(`Systemcel · ${theme === "light" ? "Light" : "Dark"} görünüm`, cards, true, theme));
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, colorScheme: theme });
  const page = await context.newPage();
  await page.goto(pathToFileURL(htmlPath).href, { waitUntil: "load" });
  await page.evaluate(() => document.fonts.ready);
  await page.screenshot({ path: path.join(outputRoot, `overview-${theme}.png`), fullPage: true, animations: "disabled" });
  await context.close();
  await fs.rm(htmlPath, { force: true });
}

function screenName(screen: CaptureCase) {
  return screen.slug.replace(/^\d+-/, "").split("-").map((word) => word.charAt(0).toLocaleUpperCase("tr-TR") + word.slice(1)).join(" ");
}

function galleryDocument(title: string, cards: string, overview: boolean, theme: Theme = "light") {
  const dark = theme === "dark";
  return `<!doctype html><html lang="tr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${title}</title><style>
    *{box-sizing:border-box}body{margin:0;padding:36px;font-family:Inter,"Segoe UI",sans-serif;color:${dark ? "#f4f3eb" : "#171812"};background:${dark ? "#0b0c09" : "#f2f0e7"}}
    body>header{margin:0 auto 28px;max-width:1800px}h1{margin:0;font-size:34px}body>header p{margin:8px 0 0;opacity:.65}
    main{max-width:1800px;margin:auto;display:grid;gap:${overview ? "18px" : "28px"};grid-template-columns:${overview ? "repeat(4,minmax(0,1fr))" : "1fr"}}
    article{overflow:hidden;border:1px solid ${dark ? "rgba(244,243,235,.14)" : "rgba(23,24,18,.14)"};border-radius:18px;background:${dark ? "#171812" : "#fffef9"};box-shadow:0 18px 46px -34px #000}
    article>header{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:${overview ? "12px 14px" : "16px 20px"};border-bottom:1px solid ${dark ? "rgba(244,243,235,.14)" : "rgba(23,24,18,.12)"}}
    strong{font-size:${overview ? "13px" : "18px"}}code{font-size:${overview ? "9px" : "13px"};opacity:.58}.pair{display:grid;grid-template-columns:1fr 1fr;gap:1px;background:${dark ? "rgba(244,243,235,.14)" : "rgba(23,24,18,.12)"}}
    figure{margin:0;background:${dark ? "#0b0c09" : "#f2f0e7"}}figcaption{padding:9px 15px;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.08em}img{width:100%;display:block;object-fit:cover;object-position:top}.pair img{aspect-ratio:16/9}.overview-card img{height:220px;object-fit:cover;object-position:top}
    a{display:block}@media(max-width:900px){body{padding:16px}.pair,main{grid-template-columns:1fr!important}}
  </style></head><body><header><h1>${title}</h1><p>${screens.length} ekran · 1920 × 1080 · tam sayfa yakalama</p></header><main>${cards}</main></body></html>`;
}

async function createCapturePage(browser: Browser, theme: Theme) {
  const context = await browser.newContext({
    baseURL: "http://127.0.0.1:4173",
    viewport: { width: 1920, height: 1080 },
    colorScheme: theme,
    locale: "tr-TR",
    timezoneId: "Europe/Istanbul",
    reducedMotion: "reduce"
  });
  await context.addInitScript((selectedTheme) => {
    try {
      window.localStorage.setItem("systemcel.theme", selectedTheme);
      window.localStorage.setItem("systemcel.language", "tr");
    } catch {
      // about:blank has an opaque origin; the same script runs again after navigation.
    }
  }, theme);
  const page = await context.newPage();
  return page;
}

async function mockApplication(page: Page) {
  await page.route("**/hubs/muhasebeci-sohbet/**", (route) => route.abort());
  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url());
    const endpoint = url.pathname;
    const body = responseFor(endpoint);
    if (body !== undefined) return json(route, body);
    return json(route, { mesaj: `Capture fixture has no response for ${endpoint}` }, 404);
  });
}

function responseFor(endpoint: string): unknown {
  if (endpoint === "/api/public/config") return { clerk: { enabled: false } };
  if (endpoint === "/api/public/planlar") return publicPlans;
  if (endpoint === "/api/ekran/ust-bar") return topbar;
  if (endpoint === "/api/ekran/kolay-kurulum") return { tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme", isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: true, mesaj: "", turler: [] };
  if (endpoint === "/api/ekran/sube-kur/") return branchSetup;
  if (endpoint === "/api/ekran/sube-kur/finans-ozeti") return { subeId: null, konsolide: true, gelirTry: 48_500, giderTry: 19_250, netTry: 29_250, paraBirimleri: [] };
  if (endpoint === "/api/ekran/anasayfa") return dashboard;
  if (endpoint === "/api/ekran/finansal-gorunum") return finance;
  if (endpoint === "/api/ekran/finansal-gorunum/nakit-planlari") return cashPlans;
  if (endpoint === "/api/ekran/gelir-gider") return incomeExpense;
  if (endpoint === "/api/ekran/urun-stok") return inventory;
  if (endpoint === "/api/ekran/mobil-tarama/durum") return { fisOcrHazir: false };
  if (endpoint === "/api/ekran/stok-defteri") return stockLedger;
  if (endpoint === "/api/ekran/cari-hesaplar") return accounts;
  if (endpoint === "/api/ekran/cari-hesaplar/7") return accountDetail;
  if (endpoint === "/api/ekran/faturalar") return invoices;
  if (endpoint === "/api/ekran/tahsilat-odeme") return payments;
  if (endpoint === "/api/ekran/banka-mutabakat/hareketler") return bankMovements;
  if (endpoint === "/api/ekran/raporlar") return reports;
  if (endpoint === "/api/ekran/sohbetler") return { sohbetler: [conversation], okunmamisMesajSayisi: 0 };
  if (endpoint === "/api/ekran/sohbetler/1/mesajlar") return { sohbetId: 1, sohbet: conversation, mesajlar: chatMessages, hasMore: false, nextBeforeId: null };
  if (endpoint === "/api/ekran/muhasebeciler") return accountantMarketplace;
  if (endpoint === "/api/ekran/muhasebeci") return accountantPanel;
  if (endpoint === "/api/abonelik/ozet") return subscriptionSummary;
  if (endpoint === "/api/ekran/ayarlar") return settings;
  if (endpoint === "/api/ekran/ayarlar/pin") return { varsayilanPin: false, mesaj: "PIN kilidi etkin." };
  if (endpoint === "/api/ekran/uyelikler") return memberships;
  if (endpoint === "/api/ekran/gelistirici-api/anahtarlar") return { anahtarlar: [] };
  if (endpoint === "/api/ekran/bildirim-tercihleri") return notificationPreferences;
  if (endpoint === "/api/ekran/gib-portal") return gib;
  if (endpoint === "/api/ekran/telegram") return telegram;
  if (endpoint === "/api/ekran/yonetim/muhasebeci-basvurulari") return adminApplications;
  if (endpoint === "/api/ekran/yonetim/odemeler") return adminPayments;
  if (endpoint === "/api/ekran/yonetim/muhasebeci-aktarimlari") return adminTransfers;
  if (endpoint === "/api/ekran/yonetim/destek") return adminSupport;
  if (endpoint === "/api/ekran/destek-talepleri") return { talepler: supportRequests };
  if (endpoint === "/api/ai/durum") return { kullanilabilir: false, mesaj: "" };
  return undefined;
}

const topbar = {
  aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme", muhasebeciMusteriBaglami: false,
  muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "TamIslem", yoneticiMi: true, bankaMutabakatiAktif: true,
  bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] }, telegramAktif: false,
  isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
};
const branchSetup = { aktifSube: { id: 1, ad: "Merkez", kod: "MRK", varsayilan: true, aktif: true }, subeler: [{ id: 1, ad: "Merkez", kod: "MRK", varsayilan: true, aktif: true }], kurlar: [{ paraBirimi: "USD", kur: 41.2, gecerliAt: "2026-09-01T08:00:00Z" }], cokluSubeAktif: true, cokluParaBirimiAktif: true };
const dashboard = {
  aktifIsletme: "Örnek İşletme", bugun: { etiket: "Bugün", aralik: "01.09.2026", gelir: 48_500, gider: 19_250, net: 29_250, gelirAdet: 8, giderAdet: 5 },
  paneller: [], gelirDegisim: { yuzde: 12, etiket: "Geçen aya göre", olumlu: true }, giderDegisim: { yuzde: 4, etiket: "Geçen aya göre", olumlu: false },
  odemeDagilimi: [{ yontem: "Havale", gelir: 31_000, gider: 12_000, net: 19_000, toplam: 43_000 }],
  netTrend: [{ gun: "Pzt", net: 4_000, islemVar: true }, { gun: "Sal", net: 7_500, islemVar: true }, { gun: "Çar", net: 5_250, islemVar: true }, { gun: "Per", net: 8_500, islemVar: true }, { gun: "Cum", net: 4_000, islemVar: true }],
  brutKarMarji: { durum: "Hazir", guvenilir: true, satisGeliri: 48_500, satisMaliyeti: 18_200, brutKar: 30_300, brutKarOrani: 62.5, satisSatiri: 22, eksikMaliyetliSatisSatiri: 0, aciklama: "Satış maliyetleri güncel." },
  belgeSagligi: { skor: 86, durum: "Dikkat", donemBaslangic: "2026-09-01T00:00:00Z", donemBitis: "2026-09-30T23:59:59Z", faturaSayisi: 14, hazirBelgeSayisi: 12, eksikBelgeSayisi: 2, taslakFaturaSayisi: 1, dosyasiEksikFaturaSayisi: 1, satiriEksikFaturaSayisi: 0, cariBilgisiEksikFaturaSayisi: 0, vadeTarihiEksikFaturaSayisi: 1, bekleyenVeriIstegiSayisi: 0, sonBelgeAt: "2026-09-01T08:00:00Z", muhasebeciBagli: true, sorunlar: [{ kod: "DosyaEksik", baslik: "Fatura dosyası eksik", adet: 1, puanEtkisi: 8, aksiyonUrl: "/app/faturalar" }, { kod: "VadeEksik", baslik: "Vade tarihi eksik", adet: 1, puanEtkisi: 6, aksiyonUrl: "/app/faturalar" }] }
};
const weeks = Array.from({ length: 13 }, (_, index) => ({ hafta: index + 1, baslangic: `2026-09-${String(index + 1).padStart(2, "0")}`, bitis: `2026-09-${String(index + 7).padStart(2, "0")}`, acilisBakiyesi: 85_000 + index * 3_000, beklenenTahsilat: 18_000, planlananGelir: 2_000, beklenenOdeme: 11_000, planlananGider: 4_000, netDegisim: 5_000, kapanisBakiyesi: 90_000 + index * 3_000 }));
const finance = { referansTarihi: "2026-09-01", paraBirimi: "TRY", kasaBakiyesi: 85_000, acikAlacakToplami: 64_000, vadesiGecmisAlacakToplami: 14_000, yaslandirma: [{ kod: "VadesiGelmedi", etiket: "Vadesi gelmedi", tutar: 50_000, faturaAdedi: 5, oran: 78 }, { kod: "Gun0_30", etiket: "1-30 gün", tutar: 14_000, faturaAdedi: 2, oran: 22 }], cariYaslandirma: [{ cariKartId: 7, unvan: "Atlas Yazılım", toplam: 64_000, vadesiGelmemis: 50_000, gun1Ila30: 14_000, gun31Ila60: 0, gun61Ila90: 0, gun91VeUzeri: 0, acikFaturaAdedi: 7, enUzunGecikmeGunu: 12, toplamdakiOrani: 100 }], yogunlasma: { enBuyukCariOrani: 58, ilkUcCariOrani: 82, ilkBesCariOrani: 100, hhi: 4200, riskSeviyesi: "Orta" }, cariRiskleri: [{ cariKartId: 7, unvan: "Atlas Yazılım", acikAlacak: 64_000, vadesiGecmisAlacak: 14_000, enUzunGecikmeGunu: 12, acikAlacakOrani: 100, ortalamaOdemeSapmasiGunu: 3, ortancaOdemeSapmasiGunu: 2, ortalamaOdemeSuresiGunu: 31, ortancaOdemeSuresiGunu: 30, zamanindaOdemeOrani: 74, odemeAraligiOrtancasiGunu: 30, sonDonemDegisimiGunu: 1, sonDonemOrnekAdedi: 4, oncekiDonemOrnekAdedi: 4, tamamlananOdemeAdedi: 12, ritimDurumu: "Dengeli", riskSeviyesi: "Orta" }], nakitProjeksiyonu: weeks, ilkNegatifHafta: null, veriUyarilari: [] };
const cashPlans = [{ id: 1, isletmeId: 42, ad: "Ofis kirası", tip: "Gider", tutar: 18_000, ilkTarih: "2026-09-05", tekrarTipi: "Aylik", tekrarAraligi: 1, bitisTarihi: null, kategori: "Kira", aciklama: "Merkez ofis", aktif: true }];
const incomeExpense = { aktifIsletme: "Örnek İşletme", kayitlar: [{ id: 1, tarih: "2026-09-01T10:00", tur: "gelir", tutar: 12_500, odemeYontemi: "havale", kalem: "Satış", aciklama: "Kurumsal sipariş" }, { id: 2, tarih: "2026-09-01T13:00", tur: "gider", tutar: 3_200, odemeYontemi: "krediKarti", kalem: "Tedarik", aciklama: "Stok alımı" }], gelirKalemleri: ["Satış", "Hizmet"], giderKalemleri: ["Tedarik", "Kira"], stokUrunleri: [{ id: 7, ad: "Filtre kahve", birim: "Adet" }], odemeYontemleri: [{ deger: "nakit", etiket: "Nakit" }, { deger: "krediKarti", etiket: "Kredi kartı" }, { deger: "onlineOdeme", etiket: "Online ödeme" }, { deger: "havale", etiket: "Havale" }] };
const inventory = { aktifIsletme: "Örnek İşletme", urunler: [{ id: 7, tip: "Urun", ad: "Filtre kahve", barkod: "869000000007", birim: "Adet", kdvOrani: 20, alisFiyati: 72, satisFiyati: 120, kritikStok: 5, mevcutStok: 18, stokMiktari: 18, aktif: true }, { id: 8, tip: "Urun", ad: "Seramik kupa", barkod: "869000000008", birim: "Adet", kdvOrani: 20, alisFiyati: 95, satisFiyati: 180, kritikStok: 4, mevcutStok: 9, stokMiktari: 9, aktif: true }], sonHareketler: [{ id: 3, urunHizmetId: 7, urunAdi: "Filtre kahve", tarih: "2026-09-01T09:00:00Z", hareketTipi: "Giris", miktar: 12, kaynak: "Stok", aciklama: "Tedarik girişi" }], tipSecenekleri: [{ deger: "Urun", etiket: "Ürün" }, { deger: "Hizmet", etiket: "Hizmet" }], birimSecenekleri: [{ deger: "Adet", etiket: "Adet" }] };
const stockLedger = { negatifStokEngelli: true, depolar: [{ id: 1, ad: "Merkez Depo", kod: "MRK", konum: "Kadıköy", varsayilan: true }, { id: 2, ad: "Mağaza", kod: "MGZ", konum: "Beşiktaş", varsayilan: false }], hareketler: [{ id: 45, islemId: 45, urunHizmetId: 7, urunAdi: "Filtre kahve", depoId: 1, depoAdi: "Merkez Depo", tarih: "2026-09-01T09:30:00Z", miktar: 12, rezerveMiktar: 0, hareketTipi: "Giriş", aciklama: "Tedarik girişi", tersKayitVar: false }] };
const accounts = { aktifIsletme: "Örnek İşletme", kartlar: [{ id: 7, tip: "Musteri", unvan: "Atlas Yazılım", telefon: "0212 555 00 00", vergiNo: "1234567890", aktif: true }], tipSecenekleri: [{ deger: "Musteri", etiket: "Müşteri" }, { deger: "Tedarikci", etiket: "Tedarikçi" }], hareketTipleri: [{ deger: "Borc", etiket: "Borç" }, { deger: "Alacak", etiket: "Alacak" }] };
const accountDetail = { kart: { id: 7, tip: "Musteri", unvan: "Atlas Yazılım", telefon: "0212 555 00 00", eposta: "finans@atlas.test", vergiNoTc: "1234567890", vergiDairesi: "Kadıköy", adres: "İstanbul", aktif: true }, bakiye: 18_500, hareketler: [{ id: 1, tarih: "2026-09-01", hareketTipi: "Borc", aciklama: "SAT-2026-0042", kaynak: "Fatura", tutar: 18_500 }] };
const invoices = { aktifIsletme: "Örnek İşletme", faturalar: [{ id: 42, no: "SAT-2026-0042", tarih: "2026-09-01", vadeTarihi: "2026-09-15", faturaTipi: "Satis", durum: "Kesildi", cariKartId: 7, cariUnvan: "Atlas Yazılım", genelToplam: 18_500, odenenTutar: 5_000, odemeYontemi: "Havale", aciklama: "Eylül hizmeti" }], cariler: [{ id: 7, unvan: "Atlas Yazılım" }], urunler: inventory.urunler, ozet: { toplamFatura: 18_500, faturaAdedi: 1, tahsilEdilen: 5_000, bekleyen: 13_500, bekleyenAdedi: 1 }, faturaTipleri: [{ deger: "Satis", etiket: "Satış" }, { deger: "Alis", etiket: "Alış" }], odemeYontemleri: [{ deger: "Havale", etiket: "Havale" }], bugun: "2026-09-01" };
const payments = { aktifIsletme: "Örnek İşletme", hareketler: [{ id: 9, no: "HRK-2026-00009", tarih: "2026-09-01", tip: "Tahsilat", cariKartId: 7, cariUnvan: "Atlas Yazılım", odemeYontemi: "Havale", tutar: 5_000, durum: "Tamamlandi", kaynak: "TahsilatOdeme", aciklama: "Kısmi tahsilat" }], cariler: [{ id: 7, unvan: "Atlas Yazılım" }], faturalar: [{ id: 42, no: "SAT-2026-0042", cariKartId: 7, cariUnvan: "Atlas Yazılım", faturaTipi: "Satis", durum: "Kesildi", genelToplam: 18_500, odenenTutar: 5_000, kalan: 13_500, odemeYontemi: "Havale", aciklama: "" }], ozet: { toplamTahsilat: 5_000, tahsilatAdedi: 1, toplamOdeme: 0, odemeAdedi: 0, bekleyen: 13_500, bekleyenAdedi: 1 }, islemTipleri: [{ deger: "Tahsilat", etiket: "Tahsilat" }, { deger: "Odeme", etiket: "Ödeme" }], odemeYontemleri: [{ deger: "Nakit", etiket: "Nakit" }, { deger: "Havale", etiket: "Havale" }], paraBirimleri: [{ deger: "TRY", etiket: "TL" }], kategoriler: [{ deger: "Fatura", etiket: "Fatura" }], bugun: "2026-09-01" };
const bankMovements = [{ id: 1, tarih: "2026-09-01", aciklama: "ATLAS YAZILIM HAVALE", tutar: 5_000, paraBirimi: "TRY", durum: "Acik" }, { id: 2, tarih: "2026-08-31", aciklama: "OFİS KİRASI", tutar: -18_000, paraBirimi: "TRY", durum: "Eslesti", eslesenKaynakTuru: "Gider", eslesenKaynakId: 2 }];
const reports = { aktifIsletme: "Örnek İşletme", bugun: "2026-09-01", varsayilanDonem: "2026-09", formatlar: [{ deger: "zip", etiket: "ZIP", secili: true }, { deger: "pdf", etiket: "PDF", secili: true }], icerikler: [{ deger: "gelirGider", etiket: "Gelir / gider", secili: true }, { deger: "faturalar", etiket: "Faturalar", secili: true }], yazdirmaSablonlari: [{ deger: "yoneticiOzeti", etiket: "Yönetici Özeti" }], tarihAraliklari: [{ deger: "monthly", etiket: "Aylık" }], sonPaket: { varMi: true, ad: "Eylül 2026 rapor paketi", donem: "2026-09", olusturmaZamani: "2026-09-01T08:30:00Z" } };
const conversation = { id: 1, muhasebeciIsletmeId: 7, musteriIsletmeId: 42, talepId: null, baglantiId: 9, baslik: "Ayşe Mali Müşavirlik", konu: "Eylül belgeleri", karsiTarafAdi: "Ayşe Mali Müşavirlik", durum: "Aktif", sonMesaj: "Belgeleriniz hazır görünüyor.", sonMesajAt: "2026-09-01T08:00:00Z", okunmamisMesajSayisi: 0, arsivlendi: false, hedefUrl: "/app/sohbetler?sohbetId=1" };
const chatMessages = [{ id: 1, sohbetId: 1, gonderenKullaniciId: 7, gonderenAdi: "Ayşe", metin: "Belgeleriniz hazır görünüyor.", createdAt: "2026-09-01T08:00:00Z", benim: false, ekler: [], paylasilanVeri: null }];
const accountantMarketplace = { mesaj: "", profiller: [{ muhasebeciIsletmeId: 9, yayinda: true, unvan: "Ada Muhasebe", konum: "İstanbul / Kadıköy", telefon: "", deneyimYili: 8, profilResmiUrl: "", ucretBilgisi: "Aylık 2.500 TL’den başlar", uzmanliklar: "E-fatura, KOBİ, bordro", musteriTipleri: "KOBİ", sektorDeneyimleri: "E-ticaret, perakende, hizmet", vergiMukellefiTipleri: "Tüm mükellef tipleri", uygunIsletmeOlcekleri: "Küçük, Orta", calismaSekilleri: "Online, Hibrit", kisaAciklama: "Ön muhasebe ve dijital dönüşüm desteği.", planAdi: "Pro", pro: true, talepVar: false, bagli: false, eslesmeSkoru: 80, eslesmeNedenleri: ["Sektörünüzle çalışıyor", "Mükellef tipinize uygun", "İş yükünüze uygun", "Çalışma biçiminize uygun"] }] };
const documentHealth = dashboard.belgeSagligi;
const accountantPanel = { hazir: true, muhasebeciIsletmeId: 7, muhasebeciAdi: "Örnek Mali Müşavirlik", mesaj: "", entitlement: { planAdi: "Muhasebeci Pro", planKodu: "muhasebeci_pro", aylikTutar: 1_499, paraBirimi: "TRY", aiAktif: true, aiMesajLimiti: null, aiSinirsiz: true, musteriLimiti: null, musteriSinirsiz: true, aktifMusteriSayisi: 2, oneCikmaAktif: true, muhasebeciProOnerilir: false }, profil: null, musteriler: [{ isletmeId: 11, ad: "Örnek Market", konum: "İstanbul / Kadıköy", yetkiSeviyesi: "OkumaRapor", durum: "Aktif", baslangicAt: "2026-08-01T00:00:00Z", belgeSagligi: documentHealth }, { isletmeId: 12, ad: "Yeni Atölye", konum: "Ankara / Çankaya", yetkiSeviyesi: "TamIslem", durum: "Aktif", baslangicAt: "2026-08-04T00:00:00Z", belgeSagligi: null }], bekleyenTalepler: [], davetler: [] };
const publicPlans = [{ kod: "isletme_buyume", ad: "Büyüme", hesapTipi: "Isletme", aylikTutar: 1_290, yillikTutar: 11_880, yillikEfektifAylikTutar: 990, normalAylikTutar: 1_290, normalYillikTutar: 15_480, kurucuAylikTutar: 990, kurucuYillikTutar: 11_880, kampanyaKodu: "ilk-50", kurucuKontenjanKalan: 31, paraBirimi: "TRY", denemeGunSayisi: 14 }];
const subscriptionSummary = { isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme", haklar: { planKodu: "isletme_buyume", planAdi: "Büyüme", kaynak: "Abonelik", aylikTutar: 1_290, yillikTutar: 11_880, faturalamaDonemi: "Yillik", donemTutari: 11_880, paraBirimi: "TRY", aiAktif: true, aiMesajLimiti: 250, kullaniciLimiti: 5, faturaLimiti: null, isletmeLimiti: 3, gelirGiderIslemLimiti: null, cariKartLimiti: null, urunHizmetLimiti: null, musteriLimiti: null, ekMusteriKredisi: 0, saltOkunur: false, gecerliBitisAt: "2027-09-01T00:00:00Z" }, durum: "Aktif", sonrakiYenilemeAt: "2027-09-01T00:00:00Z", donemSonundaIptal: false, iptalEdilebilir: true, deneme: null, abonelik: { planKodu: "isletme_buyume", faturalamaDonemi: "Yillik", ekMusteriKredisi: 0, durum: "Aktif", donemTutari: 11_880, kampanyaKodu: "ilk-50", yenilemeDonemTutari: 15_480, indirimliDonemKalan: 0, paraBirimi: "TRY", donemBaslangicAt: "2026-09-01T00:00:00Z", donemBitisAt: "2027-09-01T00:00:00Z", toleransBitisAt: null, donemSonundaIptal: false, iptalAt: null, planlananPlanKodu: "", planlananFaturalamaDonemi: "", planlananEkMusteriKredisi: null, planlananDegisiklikAt: null }, odemeler: [{ id: 1, islemTipi: "Abonelik", durum: "Basarili", planKodu: "isletme_buyume", faturalamaDonemi: "Yillik", kampanyaKodu: "ilk-50", netTutar: 11_880, listeNetTutar: 15_480, yenilemeNetTutar: 15_480, kdvTutar: 2_376, toplamTutar: 14_256, paraBirimi: "TRY", hataKodu: "", createdAt: "2026-09-01T00:00:00Z", tamamlandiAt: "2026-09-01T00:01:00Z" }] };
const settings = { aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", seciliIsletmeId: 42, seciliKalemId: 1, dil: "tr", diller: [{ kod: "tr", ad: "Türkçe" }, { kod: "en", ad: "English" }], isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }, { id: 43, ad: "İkinci Şube", aktif: false }], kalemler: [{ id: 1, tip: "Gelir", ad: "Satış" }, { id: 2, tip: "Gelir", ad: "Hizmet" }, { id: 3, tip: "Gider", ad: "Kira" }], mesaj: "" };
const memberships = { sahibiMi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", uyelikler: [{ id: 1, kullaniciId: 1, eposta: "owner@example.test", adSoyad: "İşletme Sahibi", rol: "isletme_sahibi", durum: "Aktif", davetKodu: "" }, { id: 2, kullaniciId: 2, eposta: "ekip@example.test", adSoyad: "Ekip Üyesi", rol: "personel", durum: "Aktif", davetKodu: "" }] };
const notificationPreferences = { epostaAktif: true, telegramAktif: false, vadeHatirlatmalari: true, stokUyarilari: true, gunlukOzet: true, haftalikOzet: false };
const gib = { aktifIsletme: "Örnek İşletme", kullaniciKodu: "1234567890", hasPassword: true, testModu: true, sonIslemler: [{ id: 1, faturaId: 42, tarih: "2026-09-01T09:00:00Z", islem: "Bağlantı testi", basarili: true, mesaj: "Bağlantı hazır." }], mesaj: "GİB bağlantısı hazır." };
const telegram = { bagli: false, durum: "Bağlı değil", botKullaniciAdi: "SystemcelBot", eslestirmeKodu: "SYS-4821", baglantiLinki: "https://t.me/SystemcelBot?start=SYS-4821", qrUrl: "", gecerlilikDakika: 10, mesaj: "Telegram bağlantısını tamamlayabilirsiniz." };
const adminApplications = { yoneticiMi: true, durumFiltresi: "bekleyen", bekleyenSayisi: 1, onayliSayisi: 4, reddedilenSayisi: 1, basvurular: [{ kullaniciId: 7, clerkUserId: "user_7", eposta: "ada@example.test", adSoyad: "Ada Mali Müşavirlik", durum: "MuhasebeciOnayBekliyor", createdAt: "2026-08-31T10:00:00Z", updatedAt: "2026-09-01T08:00:00Z", isletmeId: 77, isletmeAdi: "Ada Mali Müşavirlik", isletmeTuru: "MuhasebeOfisi", konum: "İstanbul", telefon: "0532 555 00 00", deneyimYili: 8, profilResmiUrl: "", ucretBilgisi: "Aylık 2.500 TL", uzmanliklar: "KOBİ, e-fatura", musteriTipleri: "KOBİ", kisaAciklama: "Dijital muhasebe desteği", profilTamam: true }] };
const adminPayments = { yoneticiMi: true, toplamSayisi: 2, basariliSayisi: 1, hataSayisi: 1, islenemeyenOlaySayisi: 0, islemler: [{ id: 99, isletmeId: 42, isletmeAdi: "Örnek İşletme", planKodu: "isletme_buyume", hesapTipi: "Isletme", islemTipi: "Abonelik", durum: "Basarili", odemeSaglayici: "PayTR", saglayiciOturumReferansi: "sess_demo", saglayiciIslemReferansi: "txn_demo", toplamTutar: 14_256, paraBirimi: "TRY", hataKodu: "", hataMesaji: "", updatedAt: "2026-09-01T09:00:00Z", olaylar: [] }] };
const adminTransfers = { yoneticiMi: true, aktarimDonemi: "2026-09", aktarimlar: [{ muhasebeciIsletmeId: 12, muhasebeciAdi: "Ada Muhasebe", aktarimDonemi: "2026-09", paraBirimi: "TRY", alacakSayisi: 2, tahsilEdilenTutar: 3_000, platformKomisyonTutari: 300, aktarilacakTutar: 2_700, durum: "Bekliyor", aktarimReferansi: "" }] };
const adminSupport = { talepler: [{ id: 2, isletmeId: 12, isletmeAdi: "Öncelikli AŞ", konu: "Teknik destek", kategori: "Teknik", aciklama: "Rapor açılmıyor.", oncelik: "Oncelikli", durum: "Islemde", yoneticiYaniti: "İnceliyoruz.", createdAt: "2026-08-24T10:00:00Z", updatedAt: "2026-09-01T08:00:00Z" }] };
const supportRequests = [{ id: 3, konu: "Eylül raporu", kategori: "Raporlama", aciklama: "Rapor hakkında bilgi", oncelik: "Normal", durum: "Acik", yoneticiYaniti: "", createdAt: "2026-09-01T08:00:00Z", updatedAt: "2026-09-01T08:00:00Z" }];

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}
