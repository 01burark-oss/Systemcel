import { describe, expect, it } from "vitest";
import { buildPricingPresentation, type PricingPlan } from "./pricingPresentation";

const starter: PricingPlan = {
  aylikTutar: 490,
  yillikTutar: 6144,
  yillikEfektifAylikTutar: 512,
  normalAylikTutar: 690,
  normalYillikTutar: 6624,
  kampanyaKodu: "kurucu-100-2026",
  kurucuKontenjanKalan: 48,
};

describe("buildPricingPresentation", () => {
  it("aylık lansman fiyatının ne zaman değişeceğini açıklar", () => {
    expect(buildPricingPresentation(starter, "Aylik", "tr")).toEqual({
      price: 490,
      unit: "/ay",
      note: "İlk 3 ay · ardından ₺690/ay",
    });
  });

  it("yıllık toplamı ve normal yıllık fiyata göre gerçek avantajı gösterir", () => {
    expect(buildPricingPresentation(starter, "Yillik", "tr")).toEqual({
      price: 6144,
      unit: "/yıl",
      note: "Normal yıllık fiyata göre ₺480 avantaj · aylık karşılığı ₺512",
    });
  });

  it("Büyüme planında onaylı yıllık fiyat farkını gösterir", () => {
    const growth: PricingPlan = {
      aylikTutar: 990,
      yillikTutar: 11880,
      yillikEfektifAylikTutar: 990,
      normalAylikTutar: 1290,
      normalYillikTutar: 15480,
      kampanyaKodu: "kurucu-100-2026",
      kurucuKontenjanKalan: 50,
    };

    expect(buildPricingPresentation(growth, "Yillik", "tr")).toEqual({
      price: 11880,
      unit: "/yıl",
      note: "Normal yıllık fiyata göre ₺3.600 avantaj · aylık karşılığı ₺990",
    });
  });

  it("kuruşlu muhasebeci fiyatlarını kayıpsız biçimlendirir", () => {
    const standard: PricingPlan = {
      aylikTutar: 699,
      yillikTutar: 8557.92,
      yillikEfektifAylikTutar: 713.16,
      normalAylikTutar: 899,
      normalYillikTutar: 9061.92,
      kampanyaKodu: "kurucu-100-2026",
      kurucuKontenjanKalan: 48,
    };

    expect(buildPricingPresentation(standard, "Yillik", "tr", 0.84)).toEqual({
      price: 8557.92,
      unit: "/yıl",
      note: "Normal yıllık fiyata göre ₺504 avantaj · aylık karşılığı ₺713,16",
    });
  });
});
