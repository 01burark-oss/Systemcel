export type PricingBilling = "Aylik" | "Yillik";
export type PricingLanguage = "tr" | "en";

export type PricingPlan = {
  aylikTutar: number;
  yillikTutar: number | null;
  yillikEfektifAylikTutar: number | null;
  normalAylikTutar: number;
  kampanyaKodu: string;
  kurucuKontenjanKalan: number;
};

type PricingPresentation = {
  price: number;
  unit: string;
  note: string;
};

function formatTry(value: number) {
  return value.toLocaleString("tr-TR", {
    minimumFractionDigits: Number.isInteger(value) ? 0 : 2,
    maximumFractionDigits: 2,
  });
}

export function buildPricingPresentation(
  plan: PricingPlan,
  billing: PricingBilling,
  language: PricingLanguage,
  annualFallbackFactor = 1,
  monthlyFallbackNote?: string,
): PricingPresentation {
  const tr = language === "tr";
  const campaignActive = Boolean(plan.kampanyaKodu && plan.kurucuKontenjanKalan > 0);

  if (billing === "Aylik") {
    const note = campaignActive
      ? tr
        ? `İlk 3 ay · ardından ₺${formatTry(plan.normalAylikTutar)}/ay`
        : `First 3 months · then ₺${formatTry(plan.normalAylikTutar)}/mo`
      : monthlyFallbackNote ?? (tr ? "Aylık tahsilat" : "Billed monthly");

    return { price: plan.aylikTutar, unit: tr ? "/ay" : "/mo", note };
  }

  const yearlyTotal = plan.yillikTutar ?? plan.aylikTutar * 12 * annualFallbackFactor;
  const equivalentMonthly = plan.yillikEfektifAylikTutar ?? yearlyTotal / 12;
  const monthlyPaymentTotal = campaignActive
    ? plan.aylikTutar * 3 + plan.normalAylikTutar * 9
    : plan.aylikTutar * 12;
  const savings = Math.max(0, monthlyPaymentTotal - yearlyTotal);
  const savingsText = savings > 0
    ? tr
      ? `Aylık ödemeye göre ₺${formatTry(savings)} avantaj · `
      : `Save ₺${formatTry(savings)} vs monthly · `
    : "";
  const equivalentText = tr
    ? `aylık karşılığı ₺${formatTry(equivalentMonthly)}`
    : `monthly equivalent ₺${formatTry(equivalentMonthly)}`;

  return {
    price: yearlyTotal,
    unit: tr ? "/yıl" : "/yr",
    note: `${savingsText}${equivalentText}`,
  };
}
