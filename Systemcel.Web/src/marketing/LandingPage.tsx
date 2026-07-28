import React from "react";
import {
  ArrowRight,
  Bot,
  Building2,
  Check,
  ChevronDown,
  FileText,
  Landmark,
  Menu,
  MessageCircle,
  Package,
  Play,
  ShieldCheck,
  Sparkles,
  Users,
  WalletCards,
  X,
} from "lucide-react";
import { useSystemcelAuth } from "../auth/SystemcelAuthProvider";
import accountantAyseAvatar from "../assets/accountant-ayse-demirtas.jpg";
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
    kod: "isletme_baslangic", ad: "Başlangıç", hesapTipi: "Isletme", aylikTutar: 490, yillikTutar: 4704,
    yillikEfektifAylikTutar: 392, aiMesajLimiti: 100, kullaniciLimiti: 1, musteriLimiti: null, faturaLimiti: 50,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 30,
  },
  {
    kod: "isletme_buyume", ad: "Büyüme", hesapTipi: "Isletme", aylikTutar: 990, yillikTutar: 9504,
    yillikEfektifAylikTutar: 792, aiMesajLimiti: null, kullaniciLimiti: 3, musteriLimiti: null, faturaLimiti: null,
    bankaMutabakatiAktif: true, stokRaporAktif: true, muhasebeciErisimiAktif: true,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 30,
  },
  {
    kod: "isletme_kurumsal", ad: "Kurumsal", hesapTipi: "Isletme", aylikTutar: 1990, yillikTutar: 19104,
    yillikEfektifAylikTutar: 1592, aiMesajLimiti: null, kullaniciLimiti: null, musteriLimiti: null, faturaLimiti: null,
    bankaMutabakatiAktif: true, stokRaporAktif: true, muhasebeciErisimiAktif: true,
    cokluSubeAktif: true, cokluParaBirimiAktif: true, apiErisimiAktif: true,
    oncelikliDestekAktif: true, denemeGunSayisi: 30,
  },
];

const fallbackAccountantPlans: PublicPlan[] = [
  {
    kod: "muhasebeci_ucretsiz", ad: "Ücretsiz", hesapTipi: "Muhasebeci", aylikTutar: 0, yillikTutar: null,
    yillikEfektifAylikTutar: null, aiMesajLimiti: 0, kullaniciLimiti: 1, musteriLimiti: 3, faturaLimiti: null,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 0,
  },
  {
    kod: "muhasebeci_standart", ad: "Standart", hesapTipi: "Muhasebeci", aylikTutar: 699, yillikTutar: 7045.92,
    yillikEfektifAylikTutar: 587.16, aiMesajLimiti: 100, kullaniciLimiti: 1, musteriLimiti: 10, faturaLimiti: null,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: false, denemeGunSayisi: 0,
  },
  {
    kod: "muhasebeci_pro", ad: "Pro", hesapTipi: "Muhasebeci", aylikTutar: 1199, yillikTutar: 12085.92,
    yillikEfektifAylikTutar: 1007.16, aiMesajLimiti: null, kullaniciLimiti: null, musteriLimiti: null, faturaLimiti: null,
    bankaMutabakatiAktif: false, stokRaporAktif: false, muhasebeciErisimiAktif: false,
    cokluSubeAktif: false, cokluParaBirimiAktif: false, apiErisimiAktif: false,
    oncelikliDestekAktif: true, denemeGunSayisi: 0,
  },
];

const copy = {
  tr: {
    announcement: "Yeni — e-Arşiv fatura akışı Systemcel'de yayında",
    accounting: "Ön Muhasebe", ai: "AI Asistan", marketplace: "Pazaryeri", pricing: "Fiyatlandırma",
    signIn: "Giriş Yap", start: "Ücretsiz Başla", eyebrow: "B2B FİNANS PLATFORMU — TR/2026",
    titleA: "Ön muhasebe,", titleB: "yapay zekâ, muhasebecin.", titleC: "Hepsi tek yerde.",
    lead: "Gelir-gider, cari, stok ve fatura akışını tek yerde yönet. Finansal verini anlayan AI asistanıyla çalış, ihtiyaç duyduğunda uzman muhasebecine ulaş.",
    trial: "30 gün ücretsiz dene", tour: "Canlı tur", setup: "5 dk kurulum", cancel: "İstediğin an iptal",
    section1: "Defter seni değil, sen defteri yönet.", section1Text: "Kasa, cari hesap, stok ve faturalar tek akışta birleşir. Tekrarlayan işleri azaltır, karar vermen gereken noktaları görünür kılarız.",
    section2: "Defterine soru sor, yanıtını al.", section2Text: "Systemcel AI, işletme verilerine göre gelir, gider, stok, cari, tahsilat ve rapor sorularına kısa ve uygulanabilir yanıtlar hazırlar.",
    section3: "Muhasebecin bir tık uzağında.", section3Business: "İhtiyacını ve uzmanlık alanlarını belirt; Systemcel, muhasebecileri uzmanlık alanlarının örtüşmesine göre şeffaf bir eşleşme skoru ile sıralar.",
    section3Accountant: "Profilini oluştur, uzmanlığını göster, işletmelerden gelen talepleri yönet ve müşterilerinle güvenli biçimde çalış.",
    forBusiness: "İşletmeler için", forAccountant: "Muhasebeciler için", findAccountant: "Muhasebecini bul", joinMarketplace: "Pazaryerine katıl",
    pricingTitle: "Şeffaf fiyat, sürpriz yok.", monthly: "Aylık", yearly: "Yıllık", discount: "-%20", popular: "Popüler", perMonth: "/ay", billedYearly: "yıllık ödemede", yearlyTotal: "Yıllık toplam", planCta: "30 gün ücretsiz dene",
    finalTitle: "İlk hücreni bugün doldur.", sales: "Satış ekibimizle görüş", footerText: "Ön muhasebe, yapay zekâ ve muhasebeci pazaryeri — işletmenin finansal çalışma alanı.",
    product: "Ürün", company: "Şirket", legal: "Yasal", about: "Hakkımızda", careers: "Kariyer", blog: "Blog", contact: "İletişim", privacy: "Gizlilik", terms: "Kullanım Şartları", cookies: "Çerezler",
    tourTitle: "Systemcel canlı tur", tourText: "Gelir-gider kaydı, cari ve stok takibi, fatura akışı, AI asistanı ve muhasebeci bağlantısı tek çalışma alanında buluşur.", tourAction: "Hesabını oluştur", close: "Kapat",
  },
  en: {
    announcement: "New — e-Archive invoice flow is live in Systemcel",
    accounting: "Accounting", ai: "AI Assistant", marketplace: "Marketplace", pricing: "Pricing",
    signIn: "Sign in", start: "Start free", eyebrow: "B2B FINANCE PLATFORM — TR/2026",
    titleA: "Accounting,", titleB: "AI and your accountant.", titleC: "All in one place.",
    lead: "Manage income, expenses, accounts, inventory and invoices in one place. Work with an AI assistant that understands your financial data and reach an expert accountant when needed.",
    trial: "Try free for 30 days", tour: "Live tour", setup: "5-minute setup", cancel: "Cancel anytime",
    section1: "You run the books — not the other way around.", section1Text: "Cash, accounts, inventory and invoices come together in one flow. Reduce repetitive work and make decisions visible.",
    section2: "Ask your books and get an answer.", section2Text: "Systemcel AI prepares concise, actionable answers about income, expenses, inventory, accounts, collections and reports.",
    section3: "Your accountant is one click away.", section3Business: "Share your needs and areas of expertise; Systemcel ranks accountants with a transparent score based on expertise overlap.",
    section3Accountant: "Create your profile, showcase your expertise, manage business requests and work securely with your clients.",
    forBusiness: "For businesses", forAccountant: "For accountants", findAccountant: "Find an accountant", joinMarketplace: "Join marketplace",
    pricingTitle: "Transparent pricing. No surprises.", monthly: "Monthly", yearly: "Yearly", discount: "-20%", popular: "Popular", perMonth: "/mo", billedYearly: "with annual billing", yearlyTotal: "Annual total", planCta: "Try free for 30 days",
    finalTitle: "Fill your first cell today.", sales: "Talk to sales", footerText: "Accounting, AI and an accountant marketplace — your financial workspace.",
    product: "Product", company: "Company", legal: "Legal", about: "About", careers: "Careers", blog: "Blog", contact: "Contact", privacy: "Privacy", terms: "Terms", cookies: "Cookies",
    tourTitle: "Systemcel live tour", tourText: "Income and expense records, accounts, inventory, invoices, AI assistance and accountant collaboration meet in one workspace.", tourAction: "Create your account", close: "Close",
  },
};

export function LandingPage() {
  const auth = useSystemcelAuth();
  const pageRef = React.useRef<HTMLDivElement>(null);
  const progressRef = React.useRef<HTMLDivElement>(null);
  const tourViewportRef = React.useRef<HTMLDivElement>(null);
  const [language, setLanguage] = React.useState<Language>(() => window.localStorage.getItem("systemcel.language") === "en" ? "en" : "tr");
  const [billing, setBilling] = React.useState<Billing>("Aylik");
  const [plans, setPlans] = React.useState<PublicPlan[]>(fallbackPlans);
  const [accountantPlans, setAccountantPlans] = React.useState<PublicPlan[]>(fallbackAccountantPlans);
  const [pricingAudience, setPricingAudience] = React.useState<"business" | "accountant">("business");
  const [mobileMenuOpen, setMobileMenuOpen] = React.useState(false);
  const [tourOpen, setTourOpen] = React.useState(false);
  const [tourStep, setTourStep] = React.useState(0);
  const [tourProgress, setTourProgress] = React.useState(0);
  const [marketSide, setMarketSide] = React.useState<"business" | "accountant">("business");
  const [activeSection, setActiveSection] = React.useState("top");
  const t = copy[language];
  const signedIn = !auth.clerkEnabled || auth.isSignedIn;
  const tourSteps = language === "tr" ? [
    {
      target: "on-muhasebe",
      number: "01",
      eyebrow: "Ön muhasebe",
      title: "Günlük finans akışını tek yerde yönet",
      text: "Gelir-gider, cari hesap, stok ve fatura kayıtlarını dağınık dosyalar yerine aynı çalışma alanında takip et.",
      metricLabel: "Tek çalışma alanı",
      metricValue: "4 temel akış",
      chips: ["Gelir / gider", "Cari", "Stok", "Fatura"],
    },
    {
      target: "ai",
      number: "02",
      eyebrow: "Systemcel AI",
      title: "Verini soruya ve aksiyona dönüştür",
      text: "Finansal verilerini anlayan asistan; nakit akışı, tahsilat riski ve stok hareketleri hakkında uygulanabilir yanıtlar üretir.",
      metricLabel: "Örnek içgörü",
      metricValue: "2 riskli cari",
      chips: ["Nakit akışı", "Tahsilat", "Stok riski"],
    },
    {
      target: "pazaryeri",
      number: "03",
      eyebrow: "Muhasebeci pazaryeri",
      title: "İhtiyacına uygun uzmanla eşleş",
      text: "Uzmanlık, konum ve müşteri tipi uyumuna göre muhasebecileri karşılaştır; güvenli çalışma alanında iletişim kur.",
      metricLabel: "Örnek eşleşme",
      metricValue: "%97 uyum",
      chips: ["Uzmanlık", "Konum", "Güvenli sohbet"],
    },
    {
      target: "fiyat",
      number: "04",
      eyebrow: "Başlangıç",
      title: "Planını seç ve çalışma alanını aç",
      text: "İşletme veya muhasebeci planını seç, hesabını oluştur ve kurulum adımlarını tamamlayarak kullanmaya başla.",
      metricLabel: "Kurulum",
      metricValue: "Yaklaşık 5 dk",
      chips: ["Plan seçimi", "Hesap oluşturma", "Kolay kurulum"],
    },
  ] : [
    {
      target: "on-muhasebe",
      number: "01",
      eyebrow: "Accounting",
      title: "Run your daily finance flow in one place",
      text: "Track income, expenses, accounts, inventory and invoices in one workspace instead of scattered files.",
      metricLabel: "One workspace",
      metricValue: "4 core flows",
      chips: ["Income / expenses", "Accounts", "Inventory", "Invoices"],
    },
    {
      target: "ai",
      number: "02",
      eyebrow: "Systemcel AI",
      title: "Turn your data into answers and actions",
      text: "The assistant understands your financial data and produces actionable answers about cash flow, collections and inventory.",
      metricLabel: "Example insight",
      metricValue: "2 risky accounts",
      chips: ["Cash flow", "Collections", "Inventory risk"],
    },
    {
      target: "pazaryeri",
      number: "03",
      eyebrow: "Accountant marketplace",
      title: "Match with the right expert",
      text: "Compare accountants by expertise, location and customer fit, then collaborate in a secure workspace.",
      metricLabel: "Example match",
      metricValue: "97% fit",
      chips: ["Expertise", "Location", "Secure chat"],
    },
    {
      target: "fiyat",
      number: "04",
      eyebrow: "Get started",
      title: "Choose a plan and open your workspace",
      text: "Choose a business or accountant plan, create your account and complete the setup steps.",
      metricLabel: "Setup",
      metricValue: "About 5 min",
      chips: ["Choose plan", "Create account", "Easy setup"],
    },
  ];
  const activeTourStep = tourSteps[tourStep];
  const tourIcons = [WalletCards, Bot, Users, Check];

  React.useEffect(() => {
    document.title = language === "tr" ? "systemcel — Yapay Zekâ Destekli Ön Muhasebe" : "systemcel — AI-powered accounting";
    fetch("/api/public/planlar")
      .then((response) => response.ok ? response.json() : Promise.reject())
      .then((data: PublicPlan[]) => {
        if (!Array.isArray(data)) return;
        const businessPlans = data.filter((plan) => plan.hesapTipi === "Isletme");
        const publicAccountantPlans = data.filter((plan) => plan.hesapTipi === "Muhasebeci");
        if (businessPlans.length === 3) setPlans(businessPlans);
        if (publicAccountantPlans.length === 3) setAccountantPlans(publicAccountantPlans);
      })
      .catch(() => undefined);
  }, [language]);

  React.useEffect(() => {
    if (!mobileMenuOpen && !tourOpen) return undefined;
    const page = pageRef.current;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMobileMenuOpen(false);
        setTourOpen(false);
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
  }, [mobileMenuOpen, tourOpen]);

  React.useEffect(() => {
    const page = pageRef.current;
    if (!page) return undefined;

    const sectionIds = ["top", "on-muhasebe", "ai", "pazaryeri", "fiyat"];
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
    const returnUrl = `/app?trial=1&plan=${encodeURIComponent(planKod)}&billing=${billing}`;
    return signedIn ? returnUrl : `/kayit?hesapTipi=Isletme&returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  function accountantHref(planKod: string) {
    const returnUrl = `/app/muhasebeci?plan=${encodeURIComponent(planKod)}&billing=${billing}`;
    return signedIn ? returnUrl : `/kayit?hesapTipi=Muhasebeci&returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  function openTour() {
    setTourStep(0);
    setTourProgress(0);
    setTourOpen(true);
    window.requestAnimationFrame(() => tourViewportRef.current?.scrollTo({ top: 0 }));
  }

  function showTourSection(target: string) {
    setTourOpen(false);
    window.requestAnimationFrame(() => {
      document.getElementById(target)?.scrollIntoView({ behavior: "smooth", block: "start" });
    });
  }

  function moveTourToStep(nextStep: number) {
    const clampedStep = Math.min(tourSteps.length - 1, Math.max(0, nextStep));
    const viewport = tourViewportRef.current;
    setTourStep(clampedStep);
    setTourProgress(clampedStep);
    if (!viewport) return;
    const maximumScroll = viewport.scrollHeight - viewport.clientHeight;
    viewport.scrollTo({
      top: tourSteps.length > 1 ? maximumScroll * clampedStep / (tourSteps.length - 1) : 0,
      behavior: "smooth",
    });
  }

  function handleTourScroll(event: React.UIEvent<HTMLDivElement>) {
    const viewport = event.currentTarget;
    const maximumScroll = viewport.scrollHeight - viewport.clientHeight;
    const progress = maximumScroll > 0
      ? viewport.scrollTop / maximumScroll * (tourSteps.length - 1)
      : 0;
    const nextStep = Math.min(tourSteps.length - 1, Math.max(0, Math.round(progress)));
    setTourProgress(progress);
    setTourStep((current) => current === nextStep ? current : nextStep);
  }

  return (
    <div className="marketing-page" ref={pageRef}>
      <a className="marketing-skip" href="#main">{language === "tr" ? "İçeriğe geç" : "Skip to content"}</a>
      <header className="marketing-header">
        <a className="marketing-announcement" href="#on-muhasebe">{t.announcement} <ArrowRight size={14} /></a>
        <nav className="marketing-nav" aria-label={language === "tr" ? "Ana menü" : "Main navigation"}>
          <a className="marketing-brand" href="#top" aria-label="Systemcel ana sayfa"><BrandMark /><strong>systemcel</strong></a>
          <div className="marketing-nav__links">
            <a className={activeSection === "on-muhasebe" ? "active" : ""} aria-current={activeSection === "on-muhasebe" ? "location" : undefined} href="#on-muhasebe">{t.accounting}</a><a className={activeSection === "ai" ? "active" : ""} aria-current={activeSection === "ai" ? "location" : undefined} href="#ai">{t.ai}</a><a className={activeSection === "pazaryeri" ? "active" : ""} aria-current={activeSection === "pazaryeri" ? "location" : undefined} href="#pazaryeri">{t.marketplace}</a><a className={activeSection === "fiyat" ? "active" : ""} aria-current={activeSection === "fiyat" ? "location" : undefined} href="#fiyat">{t.pricing}</a>
          </div>
          <div className="marketing-nav__actions">
            <button className="marketing-language" type="button" onClick={changeLanguage} aria-label="Change language">{language === "tr" ? "EN" : "TR"}</button>
            <a className="marketing-button marketing-button--ghost" href={signedIn ? "/app" : "/giris"}>{signedIn ? (language === "tr" ? "Uygulamaya Git" : "Open app") : t.signIn}</a>
            <a className="marketing-button marketing-button--ink" href={trialHref()}>{t.start}</a>
            <button className="marketing-menu-button" type="button" aria-label={language === "tr" ? "Menü" : "Menu"} aria-expanded={mobileMenuOpen} onClick={() => setMobileMenuOpen((value) => !value)}>{mobileMenuOpen ? <X /> : <Menu />}</button>
          </div>
        </nav>
        {mobileMenuOpen ? <div className="marketing-mobile-menu">
          <a href="#on-muhasebe" onClick={() => setMobileMenuOpen(false)}>{t.accounting}</a>
          <a href="#ai" onClick={() => setMobileMenuOpen(false)}>{t.ai}</a>
          <a href="#pazaryeri" onClick={() => setMobileMenuOpen(false)}>{t.marketplace}</a>
          <a href="#fiyat" onClick={() => setMobileMenuOpen(false)}>{t.pricing}</a>
          <a href={signedIn ? "/app" : "/giris"}>{signedIn ? (language === "tr" ? "Uygulamaya Git" : "Open app") : t.signIn}</a>
          <a className="marketing-mobile-menu__cta" href={trialHref()}>{t.trial}<ArrowRight size={18} /></a>
        </div> : null}
        <div className="marketing-scroll-progress" aria-hidden="true"><div ref={progressRef} /></div>
      </header>

      <main id="main">
        <section id="top" className="marketing-hero marketing-grid-bg">
          <div className="marketing-wrap marketing-hero__grid">
            <div className="marketing-hero__copy">
              <span className="marketing-eyebrow"><i />{t.eyebrow}</span>
              <h1>{t.titleA}<br />{t.titleB}<br /><em>{t.titleC}</em></h1>
              <p>{t.lead}</p>
              <div className="marketing-hero__actions">
                <a className="marketing-button marketing-button--ink marketing-button--large" href={trialHref()}>{t.trial}<ArrowRight size={18} /></a>
                <button className="marketing-button marketing-button--ghost marketing-button--large" type="button" onClick={openTour}><Play size={17} />{t.tour}</button>
              </div>
              <div className="marketing-proof"><span>{t.setup}</span><i>·</i><span>{t.cancel}</span></div>
            </div>
            <HeroLedger />
          </div>
          <a className="marketing-scroll-cue" href="#on-muhasebe"><span>{language === "tr" ? "Keşfet" : "Explore"}</span><ChevronDown size={18} /></a>
        </section>

        <section className="marketing-trust-strip marketing-reveal" data-reveal aria-label={language === "tr" ? "Ürün güvenceleri" : "Product assurances"}>
          <div className="marketing-wrap marketing-trust-grid">
            <Trust icon={<ShieldCheck />} title={language === "tr" ? "İşletme bazlı güvenli alan" : "Secure business workspace"} text={language === "tr" ? "Her kayıt yalnızca yetkili işletme üyelerine görünür." : "Every record is visible only to authorized business members."} />
            <Trust icon={<FileText />} title={language === "tr" ? "GİB e-Arşiv akışı" : "GİB e-Archive flow"} text={language === "tr" ? "Taslak, SMS onayı ve kesim adımlarını tek yerden yönet." : "Manage draft, SMS approval and issuing in one place."} />
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

        <section id="ai" className="marketing-section marketing-section--dark marketing-grid-bg--dark">
          <div className="marketing-wrap marketing-feature-grid marketing-feature-grid--reverse marketing-reveal" data-reveal>
            <div className="marketing-ai-card marketing-ai-demo">
              <span><Sparkles size={16} /> SYSTEMCEL AI</span>
              <div className="marketing-ai-thread">
                <div className="marketing-ai-question">
                  <small>{language === "tr" ? "SİZ" : "YOU"}</small>
                  <p>{language === "tr" ? "Bu ay tahsilat performansım nasıl?" : "How is my collection performance this month?"}</p>
                </div>
                <div className="marketing-ai-typing" aria-hidden="true"><i /><i /><i /></div>
                <div className="marketing-ai-answer">
                  <span><Bot size={20} /></span>
                  <div>
                    <small>{language === "tr" ? "ANALİZ TAMAMLANDI" : "ANALYSIS COMPLETE"}</small>
                    <strong>{language === "tr" ? "Vadesi geçen alacakların toplamı azaldı; en yüksek risk iki cari hesapta yoğunlaşıyor." : "Overdue receivables decreased; the highest risk is concentrated in two accounts."}</strong>
                    <div className="marketing-ai-insights">
                      <b>{language === "tr" ? "Tahsilat ↑ %12" : "Collections ↑ 12%"}</b>
                      <b>{language === "tr" ? "2 riskli cari" : "2 risky accounts"}</b>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <SectionCopy number="02" label={t.ai} title={t.section2} text={t.section2Text} dark />
          </div>
        </section>

        <section id="pazaryeri" className="marketing-section">
          <div className="marketing-wrap marketing-market-grid marketing-reveal" data-reveal>
            <SectionCopy number="03" label={t.marketplace} title={t.section3} text={marketSide === "business" ? t.section3Business : t.section3Accountant} />
            <div className="marketing-market-card">
              <div className="marketing-segmented" role="group" aria-label={t.marketplace}>
                <button type="button" className={marketSide === "business" ? "active" : ""} onClick={() => setMarketSide("business")}>{t.forBusiness}</button>
                <button type="button" className={marketSide === "accountant" ? "active" : ""} onClick={() => setMarketSide("accountant")}>{t.forAccountant}</button>
              </div>
              {marketSide === "business" ? (
                <div className="marketing-market-accountant" key={marketSide}>
                  <div className="marketing-market-accountant__head">
                    <div className="marketing-market-accountant__avatar">
                      <img src={accountantAyseAvatar} alt="Ayşe Demirtaş" />
                      <span aria-label={language === "tr" ? "Doğrulanmış profil" : "Verified profile"}><Check size={15} strokeWidth={3} /></span>
                    </div>
                    <div className="marketing-market-accountant__identity">
                      <strong>Ayşe Demirtaş</strong>
                      <span>{language === "tr" ? "Serbest Muhasebeci Mali Müşavir" : "Certified Public Accountant"}</span>
                    </div>
                    <b className="marketing-market-accountant__score"><Check size={14} strokeWidth={3} />%97 {language === "tr" ? "eşleşme" : "match"}</b>
                  </div>
                  <div className="marketing-market-accountant__facts">
                    <span><ShieldCheck size={17} />{language === "tr" ? "12 yıl deneyim" : "12 years of experience"}</span>
                    <span><WalletCards size={17} />{language === "tr" ? "E-ticaret" : "E-commerce"}</span>
                    <span><FileText size={17} />KDV</span>
                  </div>
                  <p>{language === "tr" ? "E-ticaret ve KOBİ finans süreçlerinde uzman; işletme ihtiyaçlarınla yüksek oranda örtüşüyor." : "Specialized in e-commerce and SME finance, with a strong match for your business needs."}</p>
                  <a className="marketing-button marketing-button--ink" href="/muhasebeciler">{t.findAccountant}<ArrowRight size={17} /></a>
                </div>
              ) : (
                <>
                  <div className="marketing-market-profile" key={marketSide}><div><Users size={28} /></div><span>{language === "tr" ? "Profil · Talepler · Müşteri alanı" : "Profile · Requests · Client workspace"}</span><strong>{t.joinMarketplace}</strong></div>
                  <a className="marketing-button marketing-button--ink" href="/kayit?hesapTipi=Muhasebeci&returnUrl=%2Fapp%2Fmuhasebeci">{t.joinMarketplace}<ArrowRight size={17} /></a>
                </>
              )}
            </div>
          </div>
        </section>

        <section id="fiyat" className="marketing-pricing">
          <div className="marketing-wrap marketing-reveal" data-reveal>
            <div className="marketing-pricing__head"><span className="marketing-eyebrow"><i />{t.pricing}</span><h2>{pricingAudience === "business" ? t.pricingTitle : (language === "tr" ? "Muhasebe ofisin büyüdükçe planın da büyüsün." : "A plan that grows with your accounting practice.")}</h2>
              <div className="marketing-pricing-audience" role="group" aria-label={language === "tr" ? "Plan türü" : "Plan type"}>
                <button type="button" className={pricingAudience === "business" ? "active" : ""} aria-pressed={pricingAudience === "business"} onClick={() => setPricingAudience("business")}><Building2 size={17} />{language === "tr" ? "İşletmeler" : "Businesses"}</button>
                <button type="button" className={pricingAudience === "accountant" ? "active" : ""} aria-pressed={pricingAudience === "accountant"} onClick={() => setPricingAudience("accountant")}><Users size={17} />{language === "tr" ? "Muhasebeciler" : "Accountants"}</button>
              </div>
            </div>
            <div className="marketing-pricing__billing"><div className="marketing-billing"><span>{t.monthly}</span><button type="button" aria-label={language === "tr" ? "Faturalama dönemini değiştir" : "Change billing period"} aria-pressed={billing === "Yillik"} onClick={() => setBilling((value) => value === "Aylik" ? "Yillik" : "Aylik")}><i className={billing === "Yillik" ? "yearly" : ""} /></button><span>{t.yearly} <b>{pricingAudience === "business" ? t.discount : (language === "tr" ? "-%16" : "-16%")}</b></span></div></div>
            <div className="marketing-plan-grid" key={`${pricingAudience}-${billing}`}>{pricingAudience === "business" ? plans.map((plan) => <PlanCard key={plan.kod} plan={plan} billing={billing} language={language} popular={plan.kod === "isletme_buyume"} href={trialHref(plan.kod)} />) : accountantPlans.map((plan) => <AccountantPlanCard key={plan.kod} plan={plan} billing={billing} language={language} popular={plan.kod === "muhasebeci_standart"} href={accountantHref(plan.kod)} />)}</div>
          </div>
        </section>

        <section className="marketing-final-cta"><div className="marketing-wrap"><h2>{t.finalTitle}</h2><div><a className="marketing-button marketing-button--lime marketing-button--large" href={trialHref()}>{t.trial}<ArrowRight size={18} /></a><a className="marketing-button marketing-button--dark-ghost marketing-button--large" href="mailto:satis@systemcel.app?subject=Systemcel%20Satış%20Görüşmesi">{t.sales}</a></div></div></section>
      </main>

      <footer className="marketing-footer"><div className="marketing-wrap marketing-footer__grid"><div><a className="marketing-brand marketing-brand--dark" href="#top"><BrandMark /><strong>systemcel</strong></a><p>{t.footerText}</p></div><FooterGroup title={t.product} links={[[t.accounting, "/#on-muhasebe"], [t.ai, "/#ai"], [t.marketplace, "/#pazaryeri"], [t.pricing, "/#fiyat"]]} /><FooterGroup title={t.company} links={[[t.about, "/hakkimizda"], [t.careers, "/kariyer"], [t.blog, "/blog"], [t.contact, "/iletisim"]]} /><FooterGroup title={t.legal} links={[["KVKK", "/kvkk"], [t.privacy, "/gizlilik"], [t.terms, "/kullanim-sartlari"], [t.cookies, "/cerezler"]]} /></div><div className="marketing-wrap marketing-footer__bottom"><span>© 2026 SYSTEMCEL — İSTANBUL</span><button type="button" onClick={changeLanguage}>{language === "tr" ? "TR / EN" : "EN / TR"}</button></div></footer>

      {tourOpen ? (
        <div className="marketing-modal-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && setTourOpen(false)}>
          <div className="marketing-tour-modal" role="dialog" aria-modal="true" aria-labelledby="tour-title">
            <header className="marketing-tour-header">
              <a className="marketing-brand marketing-brand--dark" href="#top" onClick={() => setTourOpen(false)}>
                <BrandMark /><strong>systemcel</strong>
              </a>
              <div>
                <span><Play size={15} />{language === "tr" ? "Canlı tur" : "Live tour"}</span>
                <strong>{activeTourStep.number} / {String(tourSteps.length).padStart(2, "0")}</strong>
              </div>
              <button className="marketing-tour-modal__close" type="button" onClick={() => setTourOpen(false)} aria-label={t.close}><X /></button>
            </header>

            <nav className="marketing-tour-progress" aria-label={language === "tr" ? "Tur ilerlemesi" : "Tour progress"}>
              {tourSteps.map((step, index) => (
                <button
                  key={step.target}
                  type="button"
                  className={index === tourStep ? "active" : index < tourStep ? "complete" : ""}
                  onClick={() => moveTourToStep(index)}
                  aria-label={`${index + 1}. ${step.eyebrow}`}
                  aria-current={index === tourStep ? "step" : undefined}
                >
                  <span>{step.number}</span>
                  <b>{step.eyebrow}</b>
                </button>
              ))}
            </nav>

            <div className="marketing-tour-viewport" ref={tourViewportRef} onScroll={handleTourScroll}>
              <div className="marketing-tour-scrollworld" style={{ height: `${tourSteps.length * 100}%` }}>
                <div className="marketing-tour-stage" style={{ height: `${100 / tourSteps.length}%` }}>
                  <div className="marketing-tour-camera">
                    {tourSteps.map((step, index) => {
                      const Icon = tourIcons[index] ?? Play;
                      const distance = index - tourProgress;
                      const absoluteDistance = Math.abs(distance);
                      const sceneOpacity = Math.max(0, 1 - absoluteDistance * .72);
                      const sceneScale = Math.max(.68, 1 - absoluteDistance * .16);
                      const sceneTransform = [
                        `translate3d(${distance * 8}%, ${distance * 72}%, ${-absoluteDistance * 620}px)`,
                        `rotateX(${distance * -11}deg)`,
                        `rotateY(${distance * 13}deg)`,
                        `scale(${sceneScale})`,
                      ].join(" ");
                      return (
                        <article
                          className={`marketing-tour-scene${index === tourStep ? " active" : ""}`}
                          key={step.target}
                          aria-hidden={index !== tourStep}
                          style={{
                            opacity: sceneOpacity,
                            transform: sceneTransform,
                            zIndex: tourSteps.length - Math.round(absoluteDistance * 2),
                          }}
                        >
                          <div className="marketing-tour-slide__copy">
                            <span>{step.number} — {step.eyebrow}</span>
                            <h2 id={index === tourStep ? "tour-title" : undefined}>{step.title}</h2>
                            <p>{step.text}</p>
                            <button type="button" onClick={() => showTourSection(step.target)}>
                              {language === "tr" ? "Landing’de bu bölümü gör" : "View this section on the landing page"}
                              <ArrowRight size={17} />
                            </button>
                          </div>

                          <div className={`marketing-tour-visual marketing-tour-visual--${index + 1}`} aria-hidden="true">
                            <div className="marketing-tour-visual__glow" />
                            <div
                              className="marketing-tour-cube"
                              style={{ transform: `rotateX(${28 + tourProgress * 26}deg) rotateY(${38 + tourProgress * 42}deg)` }}
                            >
                              <i /><i /><i /><i /><i /><i />
                            </div>
                            <div className="marketing-tour-orb"><Icon size={20} /></div>
                            <div className="marketing-tour-window">
                              <header>
                                <span><i /><i /><i /></span>
                                <b>SYSTEMCEL / {step.number}</b>
                                <small>{language === "tr" ? "CANLI" : "LIVE"}</small>
                              </header>
                              <div className="marketing-tour-window__body">
                                <aside>
                                  <span className="active"><Icon size={17} /></span>
                                  <span /><span /><span /><span />
                                </aside>
                                <main>
                                  <div className="marketing-tour-window__title">
                                    <span>{step.metricLabel}</span>
                                    <strong>{step.metricValue}</strong>
                                  </div>
                                  <div className="marketing-tour-window__chart">
                                    <i /><i /><i /><i /><i /><i /><i />
                                  </div>
                                  <div className="marketing-tour-window__cards">
                                    {step.chips.map((chip, chipIndex) => (
                                      <span key={chip}><b>{String(chipIndex + 1).padStart(2, "0")}</b>{chip}</span>
                                    ))}
                                  </div>
                                </main>
                              </div>
                            </div>
                            <div className="marketing-tour-float marketing-tour-float--top"><Sparkles size={15} />{step.chips[0]}</div>
                            <div className="marketing-tour-float marketing-tour-float--bottom"><Check size={15} />{step.metricValue}</div>
                          </div>
                        </article>
                      );
                    })}
                  </div>
                  <div className={`marketing-tour-scroll-hint${tourProgress > .2 ? " is-hidden" : ""}`} aria-hidden="true">
                    <span>{language === "tr" ? "Aşağı kaydır" : "Scroll down"}</span>
                    <i />
                  </div>
                </div>
              </div>
            </div>

            <footer className="marketing-tour-footer">
              <div>
                <button type="button" className="marketing-tour-nav marketing-tour-nav--secondary" onClick={() => moveTourToStep(tourStep - 1)} disabled={tourStep === 0}>
                  {language === "tr" ? "Geri" : "Back"}
                </button>
                {tourStep < tourSteps.length - 1 ? (
                  <button type="button" className="marketing-tour-nav marketing-tour-nav--primary" onClick={() => moveTourToStep(tourStep + 1)}>
                    {language === "tr" ? "İleri" : "Next"}<ArrowRight size={17} />
                  </button>
                ) : (
                  <a className="marketing-tour-nav marketing-tour-nav--primary" href={trialHref()}>{t.tourAction}<ArrowRight size={17} /></a>
                )}
              </div>
            </footer>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function BrandMark() { return <span className="marketing-brand-mark" aria-hidden="true"><i /><i /><i /><i /></span>; }

function HeroLedger() {
  return <div className="marketing-hero-board"><div className="marketing-hero-board__head"><span>NAKİT AKIŞI — 2026</span><b>CANLI</b></div><div className="marketing-hero-board__numbers"><div><small>GELİR</small><strong>₺842.300</strong></div><div><small>GİDER</small><strong>₺517.940</strong></div></div><div className="marketing-chart" aria-hidden="true"><svg viewBox="0 0 560 150" preserveAspectRatio="none"><defs><linearGradient id="marketing-cashflow-area" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#c8ff00" stopOpacity=".32" /><stop offset="1" stopColor="#c8ff00" stopOpacity="0" /></linearGradient></defs><path className="marketing-chart__area" d="M0 124 C44 120 73 99 112 103 C157 108 177 76 222 78 C269 81 288 49 332 56 C380 64 401 34 450 40 C493 45 522 20 560 20 L560 150 L0 150 Z" /><path className="marketing-chart__line" pathLength="1" d="M0 124 C44 120 73 99 112 103 C157 108 177 76 222 78 C269 81 288 49 332 56 C380 64 401 34 450 40 C493 45 522 20 560 20" /><circle cx="560" cy="20" r="5" /></svg></div><div className="marketing-float marketing-float--ai">✦ 3 faturanın vadesi bu hafta doluyor</div><div className="marketing-float marketing-float--bank"><Landmark size={17} /><span><strong>Banka hareketi eşleşti</strong><small>İncelemeye hazır</small></span></div></div>;
}

function Trust({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) { return <article>{icon}<div><strong>{title}</strong><span>{text}</span></div></article>; }
function SectionCopy({ number, label, title, text, dark = false }: { number: string; label: string; title: string; text: string; dark?: boolean }) { return <div className={`marketing-section-copy${dark ? " dark" : ""}`}><span className="marketing-section-number">{number}</span><span className="marketing-eyebrow"><i />{label}</span><h2>{title}</h2><p>{text}</p></div>; }
function FeatureRow({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) { return <article><span>{icon}</span><div><strong>{title}</strong><p>{text}</p></div></article>; }

function PlanCard({ plan, billing, language, popular, href }: { plan: PublicPlan; billing: Billing; language: Language; popular: boolean; href: string }) {
  const t = copy[language];
  const price = billing === "Yillik" ? (plan.yillikEfektifAylikTutar ?? plan.aylikTutar) : plan.aylikTutar;
  const yearlyTotal = plan.yillikTutar ?? plan.aylikTutar * 12;
  const features = planFeatures(plan, language);
  const planName = language === "tr" ? plan.ad : plan.kod === "isletme_baslangic" ? "Starter" : plan.kod === "isletme_buyume" ? "Growth" : "Enterprise";
  return <article className={`marketing-plan${popular ? " marketing-plan--popular" : ""}`}><div className="marketing-plan__top"><span>{planName}</span>{popular ? <b>{t.popular}</b> : null}</div><div className="marketing-plan__price"><strong key={`${billing}-${price}`}>₺{price.toLocaleString("tr-TR")}</strong><span>{t.perMonth}</span></div>{billing === "Yillik" ? <small>{t.billedYearly} · {t.yearlyTotal}: ₺{yearlyTotal.toLocaleString("tr-TR")}</small> : <small>{language === "tr" ? "Aylık tahsilat" : "Billed monthly"}</small>}<ul>{features.map((feature) => <li key={feature}><Check size={16} />{feature}</li>)}</ul><a className={`marketing-button ${popular ? "marketing-button--lime" : "marketing-button--ghost"}`} href={href}>{t.planCta}<ArrowRight size={16} /></a></article>;
}

function AccountantPlanCard({ plan, billing, language, popular, href }: { plan: PublicPlan; billing: Billing; language: Language; popular: boolean; href: string }) {
  const tr = language === "tr";
  const features = accountantPlanFeatures(plan, language);
  const planName = tr ? plan.ad : plan.kod === "muhasebeci_ucretsiz" ? "Free" : plan.kod === "muhasebeci_standart" ? "Standard" : "Pro";
  const cta = plan.kod === "muhasebeci_ucretsiz" ? (tr ? "Ücretsiz başla" : "Start free") : (tr ? `${planName} ile başla` : `Start with ${planName}`);
  const annual = billing === "Yillik" && plan.aylikTutar > 0;
  const annualTotal = plan.yillikTutar && plan.yillikTutar > 0 ? plan.yillikTutar : plan.aylikTutar * 12 * 0.84;
  const annualMonthly = plan.yillikEfektifAylikTutar && plan.yillikEfektifAylikTutar > 0 ? plan.yillikEfektifAylikTutar : annualTotal / 12;
  const price = annual ? annualMonthly : plan.aylikTutar;
  const priceNote = plan.aylikTutar === 0
    ? (tr ? "Süresiz ücretsiz" : "Free forever")
    : annual
      ? (tr ? `Yıllık toplam: ₺${annualTotal.toLocaleString("tr-TR")} · %16 avantaj` : `Annual total: ₺${annualTotal.toLocaleString("tr-TR")} · Save 16%`)
      : plan.kod === "muhasebeci_standart"
        ? (tr ? "10 müşteri dahil" : "10 clients included")
        : (tr ? "Sabit aylık ücret" : "Flat monthly fee");
  return <article className={`marketing-plan marketing-plan--accountant${popular ? " marketing-plan--popular" : ""}`}><div className="marketing-plan__top"><span>{planName}</span>{popular ? <b>{tr ? "En çok tercih edilen" : "Most popular"}</b> : null}</div><div className="marketing-plan__price"><strong key={`${billing}-${price}`}>₺{price.toLocaleString("tr-TR")}</strong><span>{tr ? "/ay" : "/mo"}</span></div><small>{priceNote}</small><ul>{features.map((feature) => <li key={feature}><Check size={16} />{feature}</li>)}</ul><a className={`marketing-button ${popular ? "marketing-button--lime" : "marketing-button--ghost"}`} href={href}>{cta}<ArrowRight size={16} /></a></article>;
}

function planFeatures(plan: PublicPlan, language: Language) {
  const tr = language === "tr";
  if (plan.kod === "isletme_baslangic") return [tr ? "Gelir-gider ve cari takibi" : "Income, expenses and accounts", tr ? "Ayda 50 e-Arşiv fatura" : "50 e-Archive invoices/month", tr ? "AI asistan · 100 soru/ay" : "AI assistant · 100 questions/month", tr ? "Tek kullanıcı" : "One user"];
  if (plan.kod === "isletme_buyume") return [tr ? "Sınırsız fatura" : "Unlimited invoices", tr ? "Banka hareketi eşleştirme" : "Bank transaction matching", tr ? "Sınırsız AI" : "Unlimited AI", tr ? "3 kullanıcı + muhasebeci erişimi" : "3 users + accountant access", tr ? "Stok ve raporlar" : "Inventory and reports"];
  return [tr ? "Çoklu şube ve para birimi" : "Multiple branches and currencies", tr ? "Özel entegrasyon API'leri" : "Custom integration APIs", tr ? "Öncelikli destek" : "Priority support", tr ? "Sınırsız kullanıcı" : "Unlimited users", tr ? "Büyüme planındaki her şey" : "Everything in Growth"];
}

function accountantPlanFeatures(plan: PublicPlan, language: Language) {
  const tr = language === "tr";
  if (plan.kod === "muhasebeci_ucretsiz") return [tr ? "3 müşteriye kadar yönetim" : "Manage up to 3 clients", tr ? "Muhasebeci çalışma paneli" : "Accountant workspace", tr ? "Talep ve sohbet akışı" : "Requests and messaging", tr ? "Pazaryeri profili" : "Marketplace profile"];
  if (plan.kod === "muhasebeci_standart") return [tr ? "10 müşteri dahil" : "10 clients included", tr ? "Sonraki müşteri +₺50/ay" : "₺50/mo per extra client", tr ? "AI asistan · 100 soru/ay" : "AI assistant · 100 questions/month", tr ? "Müşteri çalışma alanları" : "Client workspaces", tr ? "Pazaryeri profili" : "Marketplace profile"];
  return [tr ? "Sınırsız müşteri" : "Unlimited clients", tr ? "Sınırsız AI asistan" : "Unlimited AI assistant", tr ? "Pazaryerinde öne çıkma" : "Featured marketplace placement", tr ? "Dönem otomasyonu" : "Period automation", tr ? "Müşteri sağlık skoru" : "Client health score"];
}

function FooterGroup({ title, links }: { title: string; links: string[][] }) { return <div className="marketing-footer__group"><strong>{title}</strong>{links.map(([label, href]) => <a key={href} href={href}>{label}</a>)}</div>; }
