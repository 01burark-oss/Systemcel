import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { AbonelikSayfasi } from "./AbonelikSayfasi";
import type { AbonelikOzeti, PublicPlan, TeklifYaniti } from "./types";

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
});
