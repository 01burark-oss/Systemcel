import React from "react";
import { buildSubscriptionStartHref } from "../shared/subscriptionIntent";
import {
  ArrowRight,
  Bot,
  ChevronDown,
  FileText,
  Landmark,
  Menu,
  MessageCircle,
  Package,
  PackageSearch,
  ShieldCheck,
  Sparkles,
  Users,
  WalletCards,
  X,
} from "lucide-react";
import { useSystemcelAuth } from "../auth/SystemcelAuthProvider";
import type { FounderCampaignProgressProps } from "./FounderCampaignProgress";
import { PricingPanel } from "./PricingPanel";
import "./marketing.css";

type Language = "tr" | "en";
type Billing = "Aylik" | "Yillik";

type PublicPlan = {
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

const fallbackPlans: PublicPlan[] = [
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

const fallbackAccountantPlans: PublicPlan[] = [
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

const copy = {
  tr: {
    announcement: "Yeni — e-Arşiv fatura akışı Systemcel'de yayında",
    accounting: "Ön muhasebe", ai: "Systemcel AI", marketplace: "Tedarikçi pazaryeri", pricing: "Fiyatlandırma",
    signIn: "Giriş yap", signUp: "Kayıt ol", start: "Lansman fiyatıyla başla", eyebrow: "İşletme finansı",
    titleA: "İşletmenin", titleB: "finansal ihtiyaçları.", titleC: "Hepsi tek yerde.",
    lead: "Gelir, gider, cari hesap, stok ve faturalarını tek yerde yönet. Muhasebecinle birlikte çalış, tedarikçilerden alım yap ve Systemcel AI’dan destek al.",
    trial: "Lansman fiyatıyla başla", setup: "Kolay kurulum", cancel: "Dönem sonunda iptal",
    section1: "Defter seni değil, sen defteri yönet.", section1Text: "Kasa, cari hesap, stok ve faturalarını tek yerde izle. Tekrarlayan işleri azalt, hangi ödeme ve tahsilatlara bakman gerektiğini gör.",
    section2: "Kayıtlarını anlar, sıradaki işi önüne getirir.", section2Text: "Systemcel AI banka hareketi, fiş, tedarikçi ürünü ve faturalar için uygun kaydı önerir; hataları erkenden gösterir. Son karar her zaman sende kalır.",
    section3: "Muhasebecinle aynı çalışma alanında.", section3Business: "Çalıştığın muhasebeciyi davet et, erişim yetkisini seç ve belgelerinle görüşmelerini tek yerde yönet.",
    section3Accountant: "Mevcut müşterilerini davet et, her müşterinin kayıtlarını ayrı izle ve ortak çalışma alanlarını yönet.",
    forBusiness: "İşletmeler için", forAccountant: "Muhasebeciler için", findAccountant: "Muhasebecini davet et", joinMarketplace: "Tedarikçi hesabı aç",
    pricingTitle: "Şeffaf fiyat, sürpriz yok.", monthly: "Aylık", yearly: "Yıllık", discount: "Lansmana özel", popular: "Popüler", perMonth: "/ay", billedYearly: "yıllık ödemede", yearlyTotal: "Yıllık toplam", planCta: "Lansman fiyatıyla başla",
    finalTitle: "İlk kaydını bugün oluştur.", sales: "Satış ekibimizle görüş", footerText: "Ön muhasebe, muhasebeciyle ortak çalışma ve tedarikçi pazaryeri tek yerde.",
    product: "Ürün", soon: "Yakında", multipleBranchesAndCurrencies: "Çoklu şube ve para birimi", integrationApis: "Entegrasyon API'leri", periodAutomation: "Muhasebeci dönem otomasyonu", clientHealthScore: "Müşteri belge sağlık skoru", company: "Şirket", legal: "Yasal", about: "Hakkımızda", careers: "Kariyer", blog: "Blog", contact: "İletişim", privacy: "Gizlilik", terms: "Kullanım Şartları", cookies: "Çerezler",
  },
  en: {
    announcement: "New — e-Archive invoice flow is live in Systemcel",
    accounting: "Accounting", ai: "AI Assistant", marketplace: "Supplier marketplace", pricing: "Pricing",
    signIn: "Sign in", signUp: "Sign up", start: "Start with launch pricing", eyebrow: "B2B FINANCE PLATFORM — TR/2026",
    titleA: "Accounting,", titleB: "your accountant and suppliers.", titleC: "All in one place.",
    lead: "Manage income, expenses, accounts, inventory and invoices in one place. Work with your accountant, buy from suppliers and use an AI assistant for support.",
    trial: "Start with launch pricing", setup: "Easy setup", cancel: "Cancel at period end",
    section1: "You run the books — not the other way around.", section1Text: "Cash, accounts, inventory and invoices come together in one flow. Reduce repetitive work and make decisions visible.",
    section2: "Understands your records and brings the next task forward.", section2Text: "Systemcel AI suggests the right record from bank transactions to receipts and supplier products to invoices, then flags issues early. You stay in control of every decision.",
    section3: "Share one workspace with your accountant.", section3Business: "Invite your current accountant, choose their access and manage records and conversations in one place.",
    section3Accountant: "Invite existing clients, keep each client's records separate and manage shared workspaces.",
    forBusiness: "For businesses", forAccountant: "For accountants", findAccountant: "Invite your accountant", joinMarketplace: "Open a supplier account",
    pricingTitle: "Transparent pricing. No surprises.", monthly: "Monthly", yearly: "Yearly", discount: "Launch offer", popular: "Popular", perMonth: "/mo", billedYearly: "with annual billing", yearlyTotal: "Annual total", planCta: "Start with launch pricing",
    finalTitle: "Create your first record today.", sales: "Talk to sales", footerText: "Accounting, accountant collaboration and a supplier marketplace in one place.",
    product: "Product", soon: "Coming soon", multipleBranchesAndCurrencies: "Multiple branches and currencies", integrationApis: "Integration APIs", periodAutomation: "Accountant period automation", clientHealthScore: "Client document readiness score", company: "Company", legal: "Legal", about: "About", careers: "Careers", blog: "Blog", contact: "Contact", privacy: "Privacy", terms: "Terms", cookies: "Cookies",
  },
};

export function LandingPage() {
  const auth = useSystemcelAuth();
  const pageRef = React.useRef<HTMLDivElement>(null);
  const progressRef = React.useRef<HTMLDivElement>(null);
  const heroLedgerRef = React.useRef<HTMLDivElement>(null);
  const heroTiltFrameRef = React.useRef(0);
  const [language, setLanguage] = React.useState<Language>(() => window.localStorage.getItem("systemcel.language") === "en" ? "en" : "tr");
  const [billing, setBilling] = React.useState<Billing>("Aylik");
  const [plans, setPlans] = React.useState<PublicPlan[]>(fallbackPlans);
  const [accountantPlans, setAccountantPlans] = React.useState<PublicPlan[]>(fallbackAccountantPlans);
  const [founderProgress, setFounderProgress] = React.useState<Omit<FounderCampaignProgressProps, "language"> | null>(null);
  const [pricingAudience, setPricingAudience] = React.useState<"business" | "accountant">("business");
  const [mobileMenuOpen, setMobileMenuOpen] = React.useState(false);
  const [marketSide, setMarketSide] = React.useState<"business" | "accountant">("business");
  const [activeSection, setActiveSection] = React.useState("top");
  const t = copy[language];
  const collaborationLabel = language === "tr" ? "Muhasebeciyle çalışma" : "Accountant collaboration";
  const supplierMarketplaceLabel = t.marketplace;
  const signedIn = !auth.clerkEnabled || auth.isSignedIn;
  React.useEffect(() => {
    document.title = language === "tr" ? "systemcel — Yapay Zekâ Destekli Ön Muhasebe" : "systemcel — AI-powered accounting";
    fetch("/api/public/planlar")
      .then((response) => response.ok ? response.json() : Promise.reject())
      .then((data: PublicPlan[]) => {
        if (!Array.isArray(data)) return;
        const businessPlans = data.filter((plan) => plan.hesapTipi === "Isletme");
        const publicAccountantPlans = data.filter((plan) => plan.hesapTipi === "Muhasebeci");
        if (businessPlans.length === 3) setPlans(businessPlans);
        if (publicAccountantPlans.length === 2) setAccountantPlans(publicAccountantPlans);
        const campaign = data.find((plan) => plan.kurucuKontenjanToplam > 0);
        if (campaign) {
          setFounderProgress({
            total: campaign.kurucuKontenjanToplam,
            won: campaign.kurucuKontenjanKazanilan,
            percentage: campaign.kurucuKontenjanYuzdesi,
          });
        }
      })
      .catch(() => undefined);
  }, [language]);

  React.useEffect(() => () => {
    if (heroTiltFrameRef.current) window.cancelAnimationFrame(heroTiltFrameRef.current);
  }, []);

  React.useEffect(() => {
    if (!mobileMenuOpen) return undefined;
    const page = pageRef.current;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMobileMenuOpen(false);
      }
    };
    document.body.style.overflow = "hidden";
    if (page) page.style.overflowY = "hidden";
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = "";
      if (page) page.style.overflowY = "auto";
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [mobileMenuOpen]);

  React.useEffect(() => {
    const page = pageRef.current;
    if (!page) return undefined;

    const sectionIds = ["top", "on-muhasebe", "ai", "muhasebeci", "pazaryeri", "fiyat"];
    let frame = 0;
    const updateScrollState = () => {
      frame = 0;
      const maxScroll = page.scrollHeight - page.clientHeight;
      const progress = maxScroll > 0 ? page.scrollTop / maxScroll : 0;
      if (progressRef.current) progressRef.current.style.transform = `scaleX(${Math.min(1, Math.max(0, progress))})`;

      const marker = page.scrollTop + 170;
      let current = "top";
      sectionIds.forEach((id) => {
        const section = document.getElementById(id);
        if (section && section.offsetTop <= marker) current = id;
      });
      setActiveSection((previous) => previous === current ? previous : current);
    };
    const onScroll = () => {
      if (!frame) frame = window.requestAnimationFrame(updateScrollState);
    };

    const revealItems = Array.from(page.querySelectorAll<HTMLElement>("[data-reveal]"));
    const revealObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          revealObserver.unobserve(entry.target);
        }
      });
    }, { root: page, threshold: 0.12, rootMargin: "0px 0px -7%" });
    revealItems.forEach((item) => revealObserver.observe(item));

    page.addEventListener("scroll", onScroll, { passive: true });
    updateScrollState();
    return () => {
      page.removeEventListener("scroll", onScroll);
      revealObserver.disconnect();
      if (frame) window.cancelAnimationFrame(frame);
    };
  }, []);

  function changeLanguage() {
    const next = language === "tr" ? "en" : "tr";
    window.localStorage.setItem("systemcel.language", next);
    setLanguage(next);
  }

  function trialHref(planKod = "isletme_buyume") {
    return buildSubscriptionStartHref({ signedIn, accountType: "Isletme", planCode: planKod, billing });
  }

  function accountantHref(planKod: string) {
    return buildSubscriptionStartHref({ signedIn, accountType: "Muhasebeci", planCode: planKod, billing });
  }

  function handleHeroPointerMove(event: React.PointerEvent<HTMLDivElement>) {
    if (event.pointerType === "touch"
      || !window.matchMedia("(hover: hover) and (pointer: fine)").matches
      || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const card = heroLedgerRef.current;
    if (!card) return;
    const bounds = card.getBoundingClientRect();
    const normalizedX = Math.min(1, Math.max(0, (event.clientX - bounds.left) / bounds.width));
    const normalizedY = Math.min(1, Math.max(0, (event.clientY - bounds.top) / bounds.height));
    if (heroTiltFrameRef.current) window.cancelAnimationFrame(heroTiltFrameRef.current);
    heroTiltFrameRef.current = window.requestAnimationFrame(() => {
      card.style.setProperty("--mk-shine-x", `${normalizedX * 100}%`);
      card.style.setProperty("--mk-shine-y", `${normalizedY * 100}%`);
    });
  }

  function handleHeroPointerLeave() {
    const card = heroLedgerRef.current;
    if (!card) return;
    if (heroTiltFrameRef.current) window.cancelAnimationFrame(heroTiltFrameRef.current);
    heroTiltFrameRef.current = window.requestAnimationFrame(() => {
      card.style.setProperty("--mk-shine-x", "50%");
      card.style.setProperty("--mk-shine-y", "34%");
    });
  }

  const hasActiveFounderCampaign = [...plans, ...accountantPlans]
    .some((plan) => Boolean(plan.kampanyaKodu && plan.kurucuKontenjanKalan > 0));
  const pricingCta = hasActiveFounderCampaign
    ? t.trial
    : language === "tr" ? "Planla başla" : "Choose a plan";
  return (
    <div className="marketing-page" ref={pageRef}>
      <a className="marketing-skip" href="#main">{language === "tr" ? "İçeriğe geç" : "Skip to content"}</a>
      <header className="marketing-header">
        <a className="marketing-announcement" href="#on-muhasebe">{t.announcement} <ArrowRight size={14} /></a>
        <nav className="marketing-nav" aria-label={language === "tr" ? "Ana menü" : "Main navigation"}>
          <a className="marketing-brand" href="#top" aria-label="Systemcel ana sayfa"><BrandMark /><strong>systemcel</strong></a>
          <div className="marketing-nav__links">
            <a className={activeSection === "on-muhasebe" ? "active" : ""} aria-current={activeSection === "on-muhasebe" ? "location" : undefined} href="#on-muhasebe">{t.accounting}</a><a className={activeSection === "ai" ? "active" : ""} aria-current={activeSection === "ai" ? "location" : undefined} href="#ai">{t.ai}</a><a className={activeSection === "muhasebeci" ? "active" : ""} aria-current={activeSection === "muhasebeci" ? "location" : undefined} href="#muhasebeci">{collaborationLabel}</a><a className={activeSection === "pazaryeri" ? "active" : ""} aria-current={activeSection === "pazaryeri" ? "location" : undefined} href="#pazaryeri">{supplierMarketplaceLabel}</a><a className={activeSection === "fiyat" ? "active" : ""} aria-current={activeSection === "fiyat" ? "location" : undefined} href="#fiyat">{t.pricing}</a>
          </div>
          <div className="marketing-nav__actions">
            <button className="marketing-language" type="button" onClick={changeLanguage} aria-label="Change language">{language === "tr" ? "EN" : "TR"}</button>
            <a className="marketing-button marketing-button--ghost" href={signedIn ? "/app" : "/giris"}>{signedIn ? (language === "tr" ? "Uygulamaya Git" : "Open app") : t.signIn}</a>
            {!signedIn ? <a className="marketing-button marketing-button--signup" href="/kayit">{t.signUp}</a> : null}
            <a className="marketing-button marketing-button--ink" href={trialHref()}>{pricingCta}</a>
            <button className="marketing-menu-button" type="button" aria-label={language === "tr" ? "Menü" : "Menu"} aria-expanded={mobileMenuOpen} onClick={() => setMobileMenuOpen((value) => !value)}>{mobileMenuOpen ? <X /> : <Menu />}</button>
          </div>
        </nav>
        {mobileMenuOpen ? <div className="marketing-mobile-menu">
          <a href="#on-muhasebe" onClick={() => setMobileMenuOpen(false)}>{t.accounting}</a>
          <a href="#ai" onClick={() => setMobileMenuOpen(false)}>{t.ai}</a>
          <a href="#muhasebeci" onClick={() => setMobileMenuOpen(false)}>{collaborationLabel}</a>
          <a href="#pazaryeri" onClick={() => setMobileMenuOpen(false)}>{supplierMarketplaceLabel}</a>
          <a href="#fiyat" onClick={() => setMobileMenuOpen(false)}>{t.pricing}</a>
          <a href={signedIn ? "/app" : "/giris"}>{signedIn ? (language === "tr" ? "Uygulamaya Git" : "Open app") : t.signIn}</a>
          {!signedIn ? <a href="/kayit">{t.signUp}</a> : null}
          <a className="marketing-mobile-menu__cta" href={trialHref()}>{pricingCta}<ArrowRight size={18} /></a>
        </div> : null}
        <div className="marketing-scroll-progress" aria-hidden="true"><div ref={progressRef} /></div>
      </header>

      <main id="main">
        <section id="top" className="marketing-hero">
          <MarketingFlowField />
          <div className="marketing-wrap marketing-hero__grid">
            <div className="marketing-hero__copy">
              <span className="marketing-eyebrow"><i />{t.eyebrow}</span>
              <h1>{t.titleA}<br />{t.titleB}<br /><em>{t.titleC}</em></h1>
              <p>{t.lead}</p>
              <div className="marketing-hero__actions">
                <a className="marketing-button marketing-button--hero-primary marketing-button--large" href={trialHref()}>{pricingCta}<ArrowRight size={18} /></a>
              </div>
              <div className="marketing-proof"><span>{t.setup}</span><i>·</i><span>{t.cancel}</span></div>
            </div>
            <HeroLedger
              cardRef={heroLedgerRef}
              onPointerMove={handleHeroPointerMove}
              onPointerLeave={handleHeroPointerLeave}
            />
          </div>
          <a className="marketing-scroll-cue" href="#on-muhasebe"><span>{language === "tr" ? "Keşfet" : "Explore"}</span><ChevronDown size={18} /></a>
        </section>

        <section className="marketing-trust-strip" aria-label={language === "tr" ? "Ürün güvenceleri" : "Product assurances"}>
          <div className="marketing-wrap marketing-trust-grid marketing-reveal" data-reveal>
            <Trust icon={<ShieldCheck />} title={language === "tr" ? "İşletme bazlı güvenli alan" : "Secure business workspace"} text={language === "tr" ? "Her kayıt yalnızca yetkili işletme üyelerine görünür." : "Every record is visible only to authorized business members."} />
            <Trust icon={<FileText />} title={language === "tr" ? "GİB e-Arşiv akışı" : "GİB e-Archive flow"} text={language === "tr" ? "Müşteriden bilgi teyidi isteyin; resmi kesimi işletme telefonuna gelen GİB koduyla tamamlayın." : "Request customer detail confirmation, then issue with the GİB code sent to the business phone."} />
            <Trust icon={<MessageCircle />} title={language === "tr" ? "Muhasebeciyle ortak çalışma" : "Accountant collaboration"} text={language === "tr" ? "Talep, sohbet ve veri paylaşımı aynı çalışma alanında." : "Requests, chat and data sharing in one workspace."} />
          </div>
        </section>

        <section id="on-muhasebe" className="marketing-section">
          <div className="marketing-wrap marketing-feature-grid marketing-reveal" data-reveal>
            <SectionCopy number="01" label={t.accounting} title={t.section1} text={t.section1Text} />
            <div className="marketing-ledger-list">
              <FeatureRow icon={<WalletCards />} title={language === "tr" ? "Gelir, gider ve cari" : "Income, expenses and accounts"} text={language === "tr" ? "Nakit akışını ve bakiyeleri birlikte takip et." : "Track cash flow and balances together."} />
              <FeatureRow icon={<Package />} title={language === "tr" ? "Stok ve hizmetler" : "Inventory and services"} text={language === "tr" ? "Ürün, barkod, fiyat ve stok hareketlerini yönet." : "Manage products, barcodes, prices and stock."} />
              <FeatureRow icon={<Landmark />} title="GİB e-Arşiv" text={language === "tr" ? "Fatura akışını uygulamadan tamamla." : "Complete the invoice flow in the app."} />
            </div>
          </div>
        </section>

        <section id="ai" className="marketing-section marketing-section--dark">
          <div className="marketing-wrap marketing-feature-grid marketing-feature-grid--reverse marketing-reveal" data-reveal>
            <div className="marketing-ai-card marketing-ai-demo">
              <span><Sparkles size={16} /> SYSTEMCEL AI</span>
              <div className="marketing-ai-thread">
                <div className="marketing-ai-question">
                  <small>{language === "tr" ? "SİZ" : "YOU"}</small>
                  <p>{language === "tr" ? "Bugün önce hangi işlere bakmalıyım?" : "What should I handle first today?"}</p>
                </div>
                <div className="marketing-ai-typing" aria-hidden="true"><i /><i /><i /></div>
                <div className="marketing-ai-answer">
                  <span><Bot size={20} /></span>
                  <div>
                    <small>{language === "tr" ? "ANALİZ TAMAMLANDI" : "ANALYSIS COMPLETE"}</small>
                    <strong>{language === "tr" ? "Vadesi geçen iki tahsilat, maliyeti önceki alımlardan farklı bir fatura ve eşleşmemiş bir banka hareketi bekliyor." : "Two overdue collections, one invoice with a cost variance and one unmatched bank transaction need attention."}</strong>
                    <div className="marketing-ai-insights">
                      <b>{language === "tr" ? "3 öncelikli iş" : "3 priority tasks"}</b>
                      <b>{language === "tr" ? "Uyarılar birleştirildi" : "Alerts grouped"}</b>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <SectionCopy number="02" label={t.ai} title={t.section2} text={t.section2Text} dark />
          </div>
          <div className="marketing-wrap marketing-ai-capabilities marketing-reveal" data-reveal tabIndex={0} role="region" aria-label={language === "tr" ? "Systemcel AI yetenekleri" : "Systemcel AI capabilities"}>
            <FeatureRow icon={<WalletCards />} title={language === "tr" ? "Doğru kaydı daha hızlı bul" : "Find the right record faster"} text={language === "tr" ? "Banka hareketine uygun işlemi, fişe doğru gider kalemi ve cariyi önerir." : "Suggests the right transaction for a bank movement and the right expense category and account for a receipt."} />
            <FeatureRow icon={<PackageSearch />} title={language === "tr" ? "Tedarikçinin dilini stoklarına çevir" : "Translate supplier products into your inventory"} text={language === "tr" ? "Tedarikçinin ürün adlarını mevcut stoklarınla eşleştirir." : "Matches supplier product names with the items already in your inventory."} />
            <FeatureRow icon={<FileText />} title={language === "tr" ? "Faturayı kaydetmeden kontrol et" : "Check invoices before saving"} text={language === "tr" ? "Stok, birim ve maliyet sapmalarını erkenden gösterir." : "Flags inventory, unit and cost variances before they become records."} />
            <FeatureRow icon={<ShieldCheck />} title={language === "tr" ? "Aynı işi iki kez kaydetme" : "Avoid recording the same transaction twice"} text={language === "tr" ? "Belgenin yeni bir gider mi, mevcut işlemin başka belgesi mi olduğunu kontrol eder." : "Checks whether a document is a new expense or another document for an existing transaction."} />
            <FeatureRow icon={<Bot />} title={language === "tr" ? "Günün işini önüne getir" : "Bring today's work forward"} text={language === "tr" ? "Bugün ilgilenmen gereken üç işi seçer; aynı uyarıları tekrar tekrar göstermez." : "Selects the three tasks that need attention today and groups repeated alerts."} />
            <FeatureRow icon={<Sparkles />} title={language === "tr" ? "Verini kolay taşı, doğru yanıtı bul" : "Move data easily and get the right answer"} text={language === "tr" ? "Başka programdan gelen sütunları tanır; işletme asistanının sorunu ilgili kayıtlarla yanıtlamasını sağlar." : "Recognizes columns imported from other software and selects the right business data for the assistant."} />
          </div>
        </section>

        <section id="muhasebeci" className="marketing-section">
          <div className="marketing-wrap marketing-market-grid marketing-reveal" data-reveal>
            <SectionCopy
              number="03"
              label={collaborationLabel}
              title={t.section3}
              text={marketSide === "business" ? t.section3Business : t.section3Accountant}
            />
            <div className="marketing-market-card">
              <div className="marketing-segmented" role="group" aria-label={collaborationLabel}>
                <button type="button" className={marketSide === "business" ? "active" : ""} aria-pressed={marketSide === "business"} onClick={() => setMarketSide("business")}>{t.forBusiness}</button>
                <button type="button" className={marketSide === "accountant" ? "active" : ""} aria-pressed={marketSide === "accountant"} onClick={() => setMarketSide("accountant")}>{t.forAccountant}</button>
              </div>
              <div className="marketing-market-profile" key={marketSide}><div><Users size={28} /></div><span>{marketSide === "business" ? (language === "tr" ? "Davet · Yetki · Ortak çalışma" : "Invite · Access · Collaboration") : (language === "tr" ? "Müşteriler · Belgeler · Talepler" : "Clients · Records · Requests")}</span><strong>{marketSide === "business" ? (language === "tr" ? "Muhasebecini bağla" : "Connect your accountant") : (language === "tr" ? "Müşterilerini bağla" : "Connect your clients")}</strong></div>
              <a className="marketing-button marketing-button--ink" href={marketSide === "business" ? "/kayit?hesapTipi=Isletme&returnUrl=%2Fapp%2Fmuhasebeciler" : "/kayit?hesapTipi=Muhasebeci&returnUrl=%2Fapp%2Fmuhasebeci%2Fmusteriler"}>{language === "tr" ? "Başla" : "Get started"}<ArrowRight size={17} /></a>
            </div>
          </div>
        </section>

        <section id="pazaryeri" className="marketing-section marketing-section--dark">
          <div className="marketing-wrap marketing-feature-grid marketing-reveal" data-reveal>
            <SectionCopy number="04" label={supplierMarketplaceLabel} title={language === "tr" ? <>Tek sepet.<br />Birden fazla tedarikçi.</> : <>One cart.<br />Multiple suppliers.</>} text={language === "tr" ? "Ürünleri aynı sepette topla; her tedarikçinin siparişini, teslimatını ve faturasını ayrı takip et." : "Combine products in one cart, then track each supplier's order, delivery and invoice separately."} dark />
            <div className="marketing-ledger-list">
              <FeatureRow icon={<PackageSearch />} title={language === "tr" ? "Ürünleri karşılaştır" : "Compare products"} text={language === "tr" ? "Ürün, kategori veya tedarikçi adına göre ara." : "Search by product, category or supplier."} />
              <FeatureRow icon={<WalletCards />} title={language === "tr" ? "Tek sepet, ayrı takip" : "One cart, separate tracking"} text={language === "tr" ? "Farklı tedarikçilerden al; teslimatları ve faturaları ayrı izle." : "Buy from multiple suppliers and track deliveries and invoices separately."} />
              <FeatureRow icon={<FileText />} title={language === "tr" ? "Alım talebi ve teklifler" : "Purchase requests and quotes"} text={language === "tr" ? "İhtiyacını yayınla, gelen teklifleri karşılaştır." : "Publish what you need and compare incoming quotes."} />
              <a className="marketing-button marketing-button--lime" href="/pazaryeri">{language === "tr" ? "Tedarikçi pazaryerine git" : "Open supplier marketplace"}<ArrowRight size={17} /></a>
            </div>
          </div>
        </section>

        <section id="fiyat" className="marketing-pricing">
          <PricingPanel
            language={language}
            billing={billing}
            onBillingChange={setBilling}
            audience={pricingAudience}
            onAudienceChange={setPricingAudience}
            plans={plans}
            accountantPlans={accountantPlans}
            founderProgress={founderProgress}
            businessHref={trialHref}
            accountantHref={accountantHref}
            reveal
          />
        </section>

        <section className="marketing-final-cta"><div className="marketing-wrap"><h2>{t.finalTitle}</h2><div><a className="marketing-button marketing-button--lime marketing-button--large" href={trialHref()}>{pricingCta}<ArrowRight size={18} /></a><a className="marketing-button marketing-button--dark-ghost marketing-button--large" href="mailto:satis@systemcel.app?subject=Systemcel%20Satış%20Görüşmesi">{t.sales}</a></div></div></section>
      </main>

      <footer className="marketing-footer"><div className="marketing-wrap marketing-footer__grid"><div><a className="marketing-brand marketing-brand--dark" href="#top"><BrandMark /><strong>systemcel</strong></a><p>{t.footerText}</p></div><FooterGroup title={t.product} links={[[t.accounting, "#on-muhasebe"], [t.ai, "#ai"], [collaborationLabel, "#muhasebeci"], [supplierMarketplaceLabel, "#pazaryeri"], [t.pricing, "#fiyat"]]} soonTitle={t.soon} soonItems={[t.multipleBranchesAndCurrencies, t.integrationApis, t.periodAutomation]} /><FooterGroup title={t.company} links={[[t.about, "/hakkimizda"], [t.careers, "/kariyer"], [t.blog, "/blog"], [t.contact, "/iletisim"]]} /><FooterGroup title={t.legal} links={[["KVKK", "/kvkk"], [t.privacy, "/gizlilik"], [t.terms, "/kullanim-sartlari"], [language === "tr" ? "Abonelik Koşulları" : "Subscription Terms", "/abonelik-kosullari"], [language === "tr" ? "Pazaryeri satış koşulları" : "Marketplace sale terms", "/pazaryeri-satis-kosullari"], [language === "tr" ? "Teslimat ve kargo" : "Delivery and shipping", "/teslimat-ve-kargo"], [language === "tr" ? "İptal ve iade" : "Cancellation and returns", "/iptal-ve-iade"], [t.cookies, "/cerezler"]]} /></div><div className="marketing-wrap marketing-footer__bottom"><span>© 2026 SYSTEMCEL — İSTANBUL</span><button type="button" onClick={changeLanguage}>{language === "tr" ? "TR / EN" : "EN / TR"}</button></div></footer>

    </div>
  );
}

function BrandMark() { return <span className="marketing-brand-mark" aria-hidden="true"><i /><i /><i /><i /></span>; }

function MarketingFlowField() {
  return (
    <svg className="marketing-flow-field" viewBox="0 0 1440 900" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
      <defs>
        <linearGradient id="marketing-flow-stroke" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#8aad00" stopOpacity="0" />
          <stop offset="48%" stopColor="#8aad00" stopOpacity=".55" />
          <stop offset="100%" stopColor="#c8ff00" stopOpacity="0" />
        </linearGradient>
        <radialGradient id="marketing-flow-node">
          <stop offset="0%" stopColor="#c8ff00" stopOpacity=".95" />
          <stop offset="35%" stopColor="#c8ff00" stopOpacity=".35" />
          <stop offset="100%" stopColor="#c8ff00" stopOpacity="0" />
        </radialGradient>
      </defs>
      <g className="marketing-flow-field__contours">
        <path pathLength="1" d="M-80 662 C170 522 260 735 492 593 S846 400 1112 506 S1394 632 1534 462" />
        <path pathLength="1" d="M-96 715 C160 575 288 781 522 638 S878 456 1132 555 S1390 670 1532 519" />
        <path pathLength="1" d="M-116 768 C148 630 310 824 554 687 S907 516 1150 607 S1394 719 1542 575" />
      </g>
      <g className="marketing-flow-field__route">
        <path className="marketing-flow-field__route-base" pathLength="1" d="M-42 610 C205 452 320 673 532 520 S858 318 1080 433 S1346 568 1498 382" />
        <path className="marketing-flow-field__pulse marketing-flow-field__pulse--one" pathLength="1" d="M-42 610 C205 452 320 673 532 520 S858 318 1080 433 S1346 568 1498 382" />
        <path className="marketing-flow-field__pulse marketing-flow-field__pulse--two" pathLength="1" d="M-42 610 C205 452 320 673 532 520 S858 318 1080 433 S1346 568 1498 382" />
        <circle className="marketing-flow-field__traveler" r="5">
          <animateMotion dur="4.8s" repeatCount="indefinite" path="M-42 610 C205 452 320 673 532 520 S858 318 1080 433 S1346 568 1498 382" />
        </circle>
        <circle className="marketing-flow-field__node marketing-flow-field__node--one" cx="532" cy="520" r="22" />
        <circle className="marketing-flow-field__node marketing-flow-field__node--two" cx="1080" cy="433" r="22" />
      </g>
    </svg>
  );
}

function HeroLedger({
  cardRef,
  onPointerMove,
  onPointerLeave,
}: {
  cardRef: React.RefObject<HTMLDivElement | null>;
  onPointerMove: (event: React.PointerEvent<HTMLDivElement>) => void;
  onPointerLeave: () => void;
}) {
  return (
    <div className="marketing-hero-board" ref={cardRef} onPointerMove={onPointerMove} onPointerLeave={onPointerLeave}>
      <div className="marketing-hero-board__head">
        <span>NAKİT AKIŞI — 2026</span>
        <b>CANLI</b>
      </div>
      <div className="marketing-hero-board__numbers">
        <div><small>GELİR</small><strong>₺842.300</strong></div>
        <div><small>GİDER</small><strong>₺517.940</strong></div>
      </div>
      <div className="marketing-chart" aria-hidden="true">
        <svg viewBox="0 0 560 150" preserveAspectRatio="none">
          <defs>
            <linearGradient id="marketing-cashflow-area" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#c8ff00" stopOpacity=".24" />
              <stop offset=".56" stopColor="#9fd000" stopOpacity=".1" />
              <stop offset="1" stopColor="#0b0b09" stopOpacity="0" />
            </linearGradient>
          </defs>
          <path className="marketing-chart__area" d="M-8 124 C44 120 73 99 112 103 C157 108 177 76 222 78 C269 81 288 49 332 56 C380 64 401 34 450 40 C493 45 530 20 560 20 L560 150 L-8 150 Z" />
          <path className="marketing-chart__line" pathLength="1" d="M-8 124 C44 120 73 99 112 103 C157 108 177 76 222 78 C269 81 288 49 332 56 C380 64 401 34 450 40 C493 45 530 20 560 20" />
          <path className="marketing-chart__pulse" pathLength="1" d="M-8 124 C44 120 73 99 112 103 C157 108 177 76 222 78 C269 81 288 49 332 56 C380 64 401 34 450 40 C493 45 530 20 560 20" />
        </svg>
        <span className="marketing-chart__node" />
      </div>
    </div>
  );
}

function Trust({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) { return <article>{icon}<div><strong>{title}</strong><span>{text}</span></div></article>; }
function SectionCopy({ number, label, title, text, dark = false }: { number: string; label: string; title: React.ReactNode; text: string; dark?: boolean }) { return <div className={`marketing-section-copy${dark ? " dark" : ""}`}><span className="marketing-section-number">{number}</span><span className="marketing-eyebrow"><i />{label}</span><h2>{title}</h2><p>{text}</p></div>; }
function FeatureRow({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) { return <article><span>{icon}</span><div><strong>{title}</strong><p>{text}</p></div></article>; }

function FooterGroup({ title, links, soonTitle, soonItems = [] }: { title: string; links: Array<[string, string]>; soonTitle?: string; soonItems?: string[] }) {
  return <div className="marketing-footer__group"><strong>{title}</strong>{links.map(([label, href]) => <a key={href} href={href}>{label}</a>)}{soonTitle && soonItems.length > 0 ? <div className="marketing-footer__soon" role="group" aria-label={soonTitle}><strong>{soonTitle}</strong><ul>{soonItems.map((item) => <li key={item}>{item}</li>)}</ul></div> : null}</div>;
}
