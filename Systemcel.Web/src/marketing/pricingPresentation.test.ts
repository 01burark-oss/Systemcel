import { describe, expect, it } from "vitest";
import { buildPricingPresentation, type PricingPlan } from "./pricingPresentation";

const starter: PricingPlan = {
  aylikTutar: 490,
  yillikTutar: 6144,
  yillikEfektifAylikTutar: 512,
  normalAylikTutar: 690,
  kampanyaKodu: "kurucu-100-2026",
  kurucuKontenjanKalan: 98,
};

describe("buildPricingPresentation", () => {
  it("aylık lansman fiyatının ne zaman değişeceğini açıklar", () => {
    expect(buildPricingPresentation(starter, "Aylik", "tr")).toEqual({
      price: 490,
      unit: "/ay",
      note: "İlk 3 ay · ardından ₺690/ay",
    });
  });

  it("yıllık toplamı ve aylık ödemeye göre gerçek avantajı gösterir", () => {
    expect(buildPricingPresentation(starter, "Yillik", "tr")).toEqual({
      price: 6144,
      unit: "/yıl",
      note: "Aylık ödemeye göre ₺1.536 avantaj · aylık karşılığı ₺512",
    });
  });

  it("kuruşlu muhasebeci fiyatlarını kayıpsız biçimlendirir", () => {
    const standard: PricingPlan = {
      aylikTutar: 699,
      yillikTutar: 8557.92,
      yillikEfektifAylikTutar: 713.16,
      normalAylikTutar: 899,
      kampanyaKodu: "kurucu-100-2026",
      kurucuKontenjanKalan: 98,
    };

    expect(buildPricingPresentation(standard, "Yillik", "tr", 0.84)).toEqual({
      price: 8557.92,
      unit: "/yıl",
      note: "Aylık ödemeye göre ₺1.630,08 avantaj · aylık karşılığı ₺713,16",
    });
  });
});
