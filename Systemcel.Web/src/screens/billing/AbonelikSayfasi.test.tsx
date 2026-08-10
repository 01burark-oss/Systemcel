import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { AbonelikSayfasi, resolvePaymentResult, resolvePrimaryCta } from "./AbonelikSayfasi";
import type { AbonelikOzeti, OdemeKaydi, PublicPlan, TeklifYaniti } from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const summary: AbonelikOzeti = {
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

const plans: PublicPlan[] = [{
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
  kurucuKontenjanKalan: 74,
  paraBirimi: "TRY",
  denemeGunSayisi: 0
}];

const quote: TeklifYaniti = {
  fiyat: {
    planCode: "muhasebeci_standart",
    accountType: "Muhasebeci",
    billingPeriod: "Aylik",
    currency: "TRY",
    netAmount: 799,
    vatRate: 20,
    vatAmount: 159.8,
    totalAmount: 958.8,
    trialDays: 0,
    extraCustomerCredits: 2,
    includedCustomerCount: 10,
    customerCreditUnitAmount: 50,
    campaignCode: "kurucu-100-2026",
    isFounderPrice: true,
    listNetAmount: 999,
    renewalNetAmount: 999,
    discountedPeriodCount: 3
  },
  kampanyaKodu: "kurucu-100-2026",
  onayMetniSurumu: "abonelik-onayi-2026-08-v4",
  onayMetni: "Aylık planın hemen başlamasını ve lansman bitiminde geçerli liste fiyatıyla yenilenmesini kabul ediyorum."
};

describe("AbonelikSayfasi", () => {
  afterEach(() => cleanup());
  beforeEach(() => {
    window.history.replaceState({}, "", "/app/abonelik?plan=muhasebeci_standart&credits=2");
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/abonelik/ozet") return summary;
      if (url === "/api/public/planlar") return plans;
      if (url.startsWith("/api/abonelik/teklif?")) return quote;
      throw new Error(`Unexpected request: ${url}`);
    });
  });

  it("offers monthly and annual checkout with tax, recurring credits and explicit consent", async () => {
    const user = userEvent.setup();
    render(<AbonelikSayfasi />);

    expect(await screen.findByRole("heading", { name: "Planınızı seçin ve koşulları onaylayın" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Aylık" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: "Yıllık" })).toBeVisible();
    expect(screen.getByRole("spinbutton", { name: /\+1 müşteri kredisi/i })).toHaveValue(2);
    expect(await screen.findByText("12 müşteri")).toBeVisible();
    expect(await screen.findByText("KDV (%20)")).toBeVisible();
    expect(screen.getByText("LANSMANA ÖZEL")).toBeVisible();
    expect(screen.getByText("Bugünkü liste fiyatı")).toBeVisible();
    const consent = screen.getByRole("checkbox", { name: /abonelik sözleşmesini okudum ve aylık aboneliği onaylıyorum/i });
    const contractButton = screen.getByRole("button", { name: "Abonelik sözleşmesini" });
    expect(screen.queryByText(/dönem sonu iptali/i)).not.toBeInTheDocument();

    const continueButton = screen.getByRole("button", { name: "Öde ve aboneliği başlat" });
    expect(continueButton).toBeDisabled();
    await user.click(contractButton);
    expect(screen.getByRole("dialog", { name: "Systemcel Abonelik, Yenileme, İptal ve İade Koşulları" })).toBeVisible();
    expect(consent).not.toBeChecked();
    await user.click(screen.getByRole("button", { name: "Sözleşme penceresini kapat" }));
    expect(screen.queryByRole("dialog", { name: "Systemcel Abonelik, Yenileme, İptal ve İade Koşulları" })).not.toBeInTheDocument();
    await user.click(consent);
    expect(continueButton).toBeEnabled();
  });

  it("shows the period and detailed rights without a duplicate page heading or refresh control", async () => {
    window.history.replaceState({}, "", "/app/abonelik");
    render(<AbonelikSayfasi />);

    expect(await screen.findByRole("heading", { name: "Standart" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Plan dönemi" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Plan hakları" })).toBeVisible();
    expect(screen.getByText("01 Ağustos 2026")).toBeVisible();
    expect(screen.getByText("15 Ağustos 2026")).toBeVisible();
    expect(screen.getByText("Sınırsız AI mesajı")).toBeVisible();
    expect(screen.getByText("12 müşteri")).toBeVisible();
    expect(screen.getByText("Sınırsız fatura")).toBeVisible();
    expect(screen.queryByRole("heading", { name: "Aboneliğiniz, tek bakışta." })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Yenile" })).not.toBeInTheDocument();
    expect(screen.queryByText("ÖDEME YÖNTEMİ")).not.toBeInTheDocument();
  });

  it("separates trial, free, active and payment-repair actions", () => {
    expect(resolvePrimaryCta(summary)).toBe("Deneme planını yönet");
    expect(resolvePrimaryCta({ ...summary, durum: "Ucretsiz", deneme: null, haklar: { ...summary.haklar, kaynak: "Ucretsiz" } })).toBe("Ücretli plana geç");
    expect(resolvePrimaryCta({ ...summary, durum: "Aktif", deneme: null, abonelik: subscription("Aktif") })).toBe("Planı değiştir");
    expect(resolvePrimaryCta({ ...summary, durum: "OdemeBasarisiz", deneme: null, abonelik: subscription("OdemeBasarisiz") })).toBe("Ödemeyi düzelt");
  });

  it("trusts webhook-backed payment state instead of the callback query alone", () => {
    expect(resolvePaymentResult("basarili", [])?.title).toBe("Ödeme kontrol ediliyor");
    expect(resolvePaymentResult("basarili", [payment("Basarili")])?.title).toBe("Ödeme tamamlandı");
    expect(resolvePaymentResult("basarili", [payment("Basarisiz")])?.tone).toBe("danger");
    expect(resolvePaymentResult("iptal", [])?.title).toBe("Ödeme adımı iptal edildi");
    expect(resolvePaymentResult("basarili", [payment("Basarili")])?.message).not.toMatch(/webhook|sağlayıcı|checkout/i);
  });

  it("sends only one checkout request for same-task repeated clicks", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/abonelik/ozet") return summary;
      if (url === "/api/public/planlar") return plans;
      if (url.startsWith("/api/abonelik/teklif?")) return quote;
      if (url === "/api/abonelik/checkout") return new Promise(() => undefined);
      throw new Error(`Unexpected request: ${url}`);
    });
    render(<AbonelikSayfasi />);
    await screen.findByRole("heading", { name: "Planınızı seçin ve koşulları onaylayın" });
    await user.click(await screen.findByRole("checkbox"));
    const button = screen.getByRole("button", { name: "Öde ve aboneliği başlat" });
    button.click();
    button.click();
    await waitFor(() => expect(vi.mocked(jsonOku).mock.calls.filter(([url]) => url === "/api/abonelik/checkout")).toHaveLength(1));
  });
});

function subscription(durum: string) {
  return { planKodu: "muhasebeci_standart", faturalamaDonemi: "Aylik", ekMusteriKredisi: 0, durum, donemTutari: 799, kampanyaKodu: "", yenilemeDonemTutari: 899, indirimliDonemKalan: 0, paraBirimi: "TRY", donemBaslangicAt: "2026-08-01T00:00:00Z", donemBitisAt: "2026-09-01T00:00:00Z", toleransBitisAt: null, donemSonundaIptal: false, iptalAt: null };
}

function payment(durum: string): OdemeKaydi {
  return { id: 1, islemTipi: "Abonelik", durum, planKodu: "muhasebeci_standart", faturalamaDonemi: "Aylik", kampanyaKodu: "", netTutar: 799, listeNetTutar: 899, yenilemeNetTutar: 899, kdvTutar: 159.8, toplamTutar: 958.8, paraBirimi: "TRY", hataKodu: "", createdAt: "2026-08-09T00:00:00Z", tamamlandiAt: null };
}
