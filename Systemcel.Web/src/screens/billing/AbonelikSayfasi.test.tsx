import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
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
  paraBirimi: "TRY",
  denemeGunSayisi: 14
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
    trialDays: 14,
    extraCustomerCredits: 2,
    includedCustomerCount: 10,
    customerCreditUnitAmount: 0
  },
  onayMetniSurumu: "abonelik-onayi-2026-08-v2",
  onayMetni: "14 günlük deneme sonunda aylık yenilemeyi, dönem sonu iptali ve emredici haklarımı kabul ediyorum."
};

describe("AbonelikSayfasi", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/app/abonelik?plan=muhasebeci_standart&credits=2");
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/abonelik/ozet") return summary;
      if (url === "/api/public/planlar") return plans;
      if (url.startsWith("/api/abonelik/teklif?")) return quote;
      throw new Error(`Unexpected request: ${url}`);
    });
  });

  it("locks initial checkout to monthly and exposes tax, recurring credits and explicit consent", async () => {
    const user = userEvent.setup();
    render(<AbonelikSayfasi />);

    expect(await screen.findByRole("heading", { name: "Planınızı seçin ve koşulları onaylayın" })).toBeVisible();
    expect(screen.getByText("Aylık")).toBeVisible();
    expect(screen.getByRole("spinbutton", { name: /\+1 müşteri kredisi/i })).toHaveValue(2);
    expect(await screen.findByText("12 müşteri")).toBeVisible();
    expect(screen.getByText("KDV (%20)")).toBeVisible();
    expect(screen.getByText(/abonelik-onayi-2026-08-v2/)).toBeVisible();
    expect(screen.getByText(/dönem sonu iptali/i)).toBeVisible();

    const continueButton = screen.getByRole("button", { name: "Kartı güvenle ekle" });
    expect(continueButton).toBeDisabled();
    await user.click(screen.getByRole("checkbox"));
    expect(continueButton).toBeEnabled();
  });

  it("separates trial, free, active and payment-repair actions", () => {
    expect(resolvePrimaryCta(summary)).toBe("Deneme planını yönet");
    expect(resolvePrimaryCta({ ...summary, durum: "Ucretsiz", deneme: null, haklar: { ...summary.haklar, kaynak: "Ucretsiz" } })).toBe("Ücretli plana geç");
    expect(resolvePrimaryCta({ ...summary, durum: "Aktif", deneme: null, abonelik: subscription("Aktif") })).toBe("Planı değiştir");
    expect(resolvePrimaryCta({ ...summary, durum: "OdemeBasarisiz", deneme: null, abonelik: subscription("OdemeBasarisiz") })).toBe("Ödemeyi düzelt");
  });

  it("trusts webhook-backed payment state instead of the callback query alone", () => {
    expect(resolvePaymentResult("basarili", [])?.title).toBe("Ödeme doğrulanıyor");
    expect(resolvePaymentResult("basarili", [payment("Basarili")])?.title).toBe("Ödeme doğrulandı");
    expect(resolvePaymentResult("basarili", [payment("Basarisiz")])?.tone).toBe("danger");
    expect(resolvePaymentResult("iptal", [])?.title).toBe("Checkout iptal edildi");
  });
});

function subscription(durum: string) {
  return { planKodu: "muhasebeci_standart", faturalamaDonemi: "Aylik", ekMusteriKredisi: 0, durum, donemTutari: 799, paraBirimi: "TRY", donemBaslangicAt: "2026-08-01T00:00:00Z", donemBitisAt: "2026-09-01T00:00:00Z", toleransBitisAt: null, donemSonundaIptal: false, iptalAt: null };
}

function payment(durum: string): OdemeKaydi {
  return { id: 1, islemTipi: "Abonelik", durum, planKodu: "muhasebeci_standart", faturalamaDonemi: "Aylik", netTutar: 799, kdvTutar: 159.8, toplamTutar: 958.8, paraBirimi: "TRY", hataKodu: "", createdAt: "2026-08-09T00:00:00Z", tamamlandiAt: null };
}
