import React from "react";
import { ArrowRight, Building2, Check, Users } from "lucide-react";
import { FounderCampaignProgress, type FounderCampaignProgressProps } from "./FounderCampaignProgress";
import { buildPricingPresentation } from "./pricingPresentation";
import "./marketing.css";

export type PricingLanguage = "tr" | "en";
export type PricingBilling = "Aylik" | "Yillik";
export type PricingAudience = "business" | "accountant";

export type PublicPlan = {
  kod: string;
  ad: string;
  hesapTipi: "Isletme" | "Muhasebeci";
  aylikTutar: number;
  yillikTutar: number | null;
  yillikEfektifAylikTutar: number | null;
  normalAylikTutar: number;
  normalYillikTutar: number | null;
  kurucuAylikTutar: number;
  kurucuYillikTutar: number | null;
  kampanyaKodu: string;
  kurucuKontenjanKalan: number;
  kurucuKontenjanToplam: number;
  kurucuKontenjanKazanilan: number;
  kurucuKontenjanYuzdesi: number;
  aiMesajLimiti: number | null;
  kullaniciLimiti: number | null;
  musteriLimiti: number | null;
  faturaLimiti: number | null;
  bankaMutabakatiAktif: boolean;
  stokRaporAktif: boolean;
  muhasebeciErisimiAktif: boolean;
  cokluSubeAktif: boolean;
  cokluParaBirimiAktif: boolean;
  apiErisimiAktif: boolean;
  oncelikliDestekAktif: boolean;
  denemeGunSayisi: number;
};

export const fallbackPlans: PublicPlan[] = [
  {
    kod: "isletme_baslangic", ad: "Başlangıç", hesapTipi: "Isletme", aylikTutar: 690, yillikTutar: 6624,
    yillikEfektifAylikTutar: 552, normalAylikTutar: 690, normalYillikTutar: 6624, kurucuAylikTutar: 490, kurucuYillikTutar: 6144, kampanyaKodu: "", kurucuKontenjanKalan: 0, kurucuKontenjanToplam: 0, kurucuKontenjanKazanilan: 0, kurucuKontenjanYuzdesi: 0, aiMesajLimiti: 100, kullaniciLimiti: 1, musteriLimiti: null, faturaLimiti: 50,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 0,
  },
  {
    kod: "isletme_buyume", ad: "Büyüme", hesapTipi: "Isletme", aylikTutar: 1290, yillikTutar: 15480,
    yillikEfektifAylikTutar: 1290, normalAylikTutar: 1290, normalYillikTutar: 15480, kurucuAylikTutar: 990, kurucuYillikTutar: 11880, kampanyaKodu: "", kurucuKontenjanKalan: 0, kurucuKontenjanToplam: 0, kurucuKontenjanKazanilan: 0, kurucuKontenjanYuzdesi: 0, aiMesajLimiti: null, kullaniciLimiti: 3, musteriLimiti: null, faturaLimiti: null,
    bankaMutabakatiAktif: true, stokRaporAktif: true, muhasebeciErisimiAktif: true,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 0,
  },
  {
    kod: "isletme_kurumsal", ad: "Kurumsal", hesapTipi: "Isletme", aylikTutar: 2490, yillikTutar: 23904,
    yillikEfektifAylikTutar: 1992, normalAylikTutar: 2490, normalYillikTutar: 23904, kurucuAylikTutar: 1990, kurucuYillikTutar: 22704, kampanyaKodu: "", kurucuKontenjanKalan: 0, kurucuKontenjanToplam: 0, kurucuKontenjanKazanilan: 0, kurucuKontenjanYuzdesi: 0, aiMesajLimiti: null, kullaniciLimiti: null, musteriLimiti: null, faturaLimiti: null,
    bankaMutabakatiAktif: true, stokRaporAktif: true, muhasebeciErisimiAktif: true,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: true, denemeGunSayisi: 0,
  },
];

export const fallbackAccountantPlans: PublicPlan[] = [
  {
    kod: "muhasebeci_standart", ad: "Standart", hesapTipi: "Muhasebeci", aylikTutar: 899, yillikTutar: 9061.92,
    yillikEfektifAylikTutar: 755.16, normalAylikTutar: 899, normalYillikTutar: 9061.92, kurucuAylikTutar: 699, kurucuYillikTutar: 8557.92, kampanyaKodu: "", kurucuKontenjanKalan: 0, kurucuKontenjanToplam: 0, kurucuKontenjanKazanilan: 0, kurucuKontenjanYuzdesi: 0, aiMesajLimiti: 100, kullaniciLimiti: 1, musteriLimiti: 10, faturaLimiti: null,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 0,
  },
  {
    kod: "muhasebeci_pro", ad: "Pro", hesapTipi: "Muhasebeci", aylikTutar: 1499, yillikTutar: 15109.92,
    yillikEfektifAylikTutar: 1259.16, normalAylikTutar: 1499, normalYillikTutar: 15109.92, kurucuAylikTutar: 1199, kurucuYillikTutar: 14353.92, kampanyaKodu: "", kurucuKontenjanKalan: 0, kurucuKontenjanToplam: 0, kurucuKontenjanKazanilan: 0, kurucuKontenjanYuzdesi: 0, aiMesajLimiti: null, kullaniciLimiti: null, musteriLimiti: null, faturaLimiti: null,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: true, denemeGunSayisi: 0,
  },
];

interface PricingPanelProps {
  language: PricingLanguage;
  billing: PricingBilling;
  onBillingChange: (billing: PricingBilling) => void;
  audience: PricingAudience;
  onAudienceChange: (audience: PricingAudience) => void;
  plans: PublicPlan[];
  accountantPlans: PublicPlan[];
  founderProgress?: Omit<FounderCampaignProgressProps, "language"> | null;
  businessHref: (planCode: string) => string;
  accountantHref: (planCode: string) => string;
  className?: string;
  reveal?: boolean;
}

export function PricingPanel({
  language,
  billing,
  onBillingChange,
  audience,
  onAudienceChange,
  plans,
  accountantPlans,
  founderProgress,
  businessHref,
  accountantHref,
  className = "",
  reveal = false,
}: PricingPanelProps) {
  const tr = language === "tr";
  const hasActiveFounderCampaign = [...plans, ...accountantPlans]
    .some((plan) => Boolean(plan.kampanyaKodu && plan.kurucuKontenjanKalan > 0));
  const annualBillingBadge = hasActiveFounderCampaign
    ? (tr ? "Lansmana özel" : "Launch offer")
    : (tr ? "Yıllık avantaj" : "Annual savings");

  return (
    <div
      className={`marketing-wrap${reveal ? " marketing-reveal" : ""}${className ? ` ${className}` : ""}`}
      data-reveal={reveal ? "" : undefined}
    >
      <div className="marketing-pricing__head">
        <span className="marketing-eyebrow"><i />{tr ? "Fiyatlandırma" : "Pricing"}</span>
        <h2>{audience === "business"
          ? (tr ? "Şeffaf fiyat, sürpriz yok." : "Transparent pricing. No surprises.")
          : (tr ? "Muhasebe ofisin büyüdükçe planın da büyüsün." : "A plan that grows with your accounting practice.")}</h2>
        <div className="marketing-pricing-audience" role="group" aria-label={tr ? "Plan türü" : "Plan type"}>
          <button type="button" className={audience === "business" ? "active" : ""} aria-pressed={audience === "business"} onClick={() => onAudienceChange("business")}><Building2 size={17} />{tr ? "İşletmeler" : "Businesses"}</button>
          <button type="button" className={audience === "accountant" ? "active" : ""} aria-pressed={audience === "accountant"} onClick={() => onAudienceChange("accountant")}><Users size={17} />{tr ? "Muhasebeciler" : "Accountants"}</button>
        </div>
      </div>
      <div className="marketing-pricing__billing">
        <div className="marketing-billing">
          <span>{tr ? "Aylık" : "Monthly"}</span>
          <button type="button" aria-label={tr ? "Faturalama dönemini değiştir" : "Change billing period"} aria-pressed={billing === "Yillik"} onClick={() => onBillingChange(billing === "Aylik" ? "Yillik" : "Aylik")}><i className={billing === "Yillik" ? "yearly" : ""} /></button>
          <span>{tr ? "Yıllık" : "Yearly"} <b>{annualBillingBadge}</b></span>
        </div>
      </div>
      {founderProgress ? <FounderCampaignProgress {...founderProgress} language={language} /> : null}
      <div className={`marketing-plan-grid${audience === "accountant" ? " marketing-plan-grid--accountant" : ""}`} key={`${audience}-${billing}`}>
        {audience === "business"
          ? plans.map((plan) => <PlanCard key={plan.kod} plan={plan} billing={billing} language={language} popular={plan.kod === "isletme_buyume"} href={businessHref(plan.kod)} />)
          : accountantPlans.map((plan) => <AccountantPlanCard key={plan.kod} plan={plan} billing={billing} language={language} popular={plan.kod === "muhasebeci_standart"} href={accountantHref(plan.kod)} />)}
      </div>
    </div>
  );
}

function PlanCard({ plan, billing, language, popular, href }: { plan: PublicPlan; billing: PricingBilling; language: PricingLanguage; popular: boolean; href: string }) {
  const tr = language === "tr";
  const pricing = buildPricingPresentation(plan, billing, language);
  const features = planFeatures(plan, language);
  const planName = tr ? plan.ad : plan.kod === "isletme_baslangic" ? "Starter" : plan.kod === "isletme_buyume" ? "Growth" : "Enterprise";
  const cta = plan.kampanyaKodu ? (tr ? "Lansman fiyatıyla başla" : "Start with launch pricing") : tr ? "Planı incele" : "View plan";
  return <article className={`marketing-plan${popular ? " marketing-plan--popular" : ""}`}><div className="marketing-plan__top"><span>{planName}</span>{popular ? <b>{tr ? "Popüler" : "Popular"}</b> : null}</div><div className="marketing-plan__price"><strong key={`${billing}-${pricing.price}`}>₺{pricing.price.toLocaleString("tr-TR")}</strong><span>{pricing.unit}</span></div><small>{pricing.note}</small><ul>{features.map((feature) => <li key={feature}><Check size={16} />{feature}</li>)}</ul><a className={`marketing-button ${popular ? "marketing-button--lime" : "marketing-button--ghost"}`} href={href} aria-label={tr ? `${cta}: ${planName} planı` : `${cta}: ${planName} plan`}>{cta}<ArrowRight size={16} /></a></article>;
}

function AccountantPlanCard({ plan, billing, language, popular, href }: { plan: PublicPlan; billing: PricingBilling; language: PricingLanguage; popular: boolean; href: string }) {
  const tr = language === "tr";
  const features = accountantPlanFeatures(plan, language);
  const planName = tr ? plan.ad : plan.kod === "muhasebeci_standart" ? "Standard" : "Pro";
  const cta = tr ? `${planName} ile başla` : `Start with ${planName}`;
  const monthlyFallbackNote = plan.kod === "muhasebeci_standart"
    ? (tr ? "10 müşteri dahil" : "10 clients included")
    : (tr ? "Sabit aylık ücret" : "Flat monthly fee");
  const pricing = buildPricingPresentation(plan, billing, language, 0.84, monthlyFallbackNote);
  return <article className={`marketing-plan marketing-plan--accountant${popular ? " marketing-plan--popular" : ""}`}><div className="marketing-plan__top"><span>{planName}</span>{popular ? <b>{tr ? "En çok tercih edilen" : "Most popular"}</b> : null}</div><div className="marketing-plan__price"><strong key={`${billing}-${pricing.price}`}>₺{pricing.price.toLocaleString("tr-TR")}</strong><span>{pricing.unit}</span></div><small>{pricing.note}</small><ul>{features.map((feature) => <li key={feature}><Check size={16} />{feature}</li>)}</ul><a className={`marketing-button ${popular ? "marketing-button--lime" : "marketing-button--ghost"}`} href={href} aria-label={tr ? `${cta}: ${planName} planı` : `${cta}: ${planName} plan`}>{cta}<ArrowRight size={16} /></a></article>;
}

function planFeatures(plan: PublicPlan, language: PricingLanguage) {
  const tr = language === "tr";
  if (plan.kod === "isletme_baslangic") return [tr ? "Gelir-gider ve cari takibi" : "Income, expenses and accounts", tr ? "Ayda 50 e-Arşiv fatura" : "50 e-Archive invoices/month", tr ? "AI asistan · 100 soru/ay" : "AI assistant · 100 questions/month", tr ? "Tek kullanıcı" : "One user"];
  if (plan.kod === "isletme_buyume") return [tr ? "Sınırsız fatura" : "Unlimited invoices", tr ? "Sınırsız AI" : "Unlimited AI", tr ? "3 kullanıcı + muhasebeci erişimi" : "3 users + accountant access", tr ? "Stok ve raporlar" : "Inventory and reports"];
  return [tr ? "Öncelikli destek" : "Priority support", tr ? "Sınırsız kullanıcı" : "Unlimited users", tr ? "Büyüme planındaki her şey" : "Everything in Growth"];
}

function accountantPlanFeatures(plan: PublicPlan, language: PricingLanguage) {
  const tr = language === "tr";
  if (plan.kod === "muhasebeci_standart") return [tr ? "10 müşteri dahil" : "10 clients included", tr ? "Sonraki müşteri +₺50/ay" : "₺50/mo per extra client", tr ? "AI asistan · 100 soru/ay" : "AI assistant · 100 questions/month", tr ? "Müşteri çalışma alanları" : "Client workspaces", tr ? "Pazaryeri profili" : "Marketplace profile"];
  return [tr ? "Sınırsız müşteri" : "Unlimited clients", tr ? "Müşteri belge sağlık skoru" : "Client document readiness score", tr ? "Sınırsız AI asistan" : "Unlimited AI assistant", tr ? "Pazaryerinde öne çıkma" : "Featured marketplace placement"];
}
