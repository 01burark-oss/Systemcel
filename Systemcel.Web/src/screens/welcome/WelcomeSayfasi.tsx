import React from "react";
import {
  ArrowDown,
  ArrowRight,
  ArrowUp,
  BarChart3,
  Building2,
  Check,
  ChevronDown,
  Clock3,
  Cloud,
  CreditCard,
  Info,
  Landmark,
  Menu,
  Package,
  ReceiptText,
  Send,
  ShieldCheck,
  Sparkles,
  UserRound,
  WalletCards,
} from "lucide-react";
import systemcelIcon from "../../assets/systemcel-icon.png";
import productHeroOverview from "../../assets/product-hero-overview.png";
import welcomeDashboardPreview from "../../assets/welcome-dashboard-preview.png";
import { useSystemcelAuth } from "../../auth/SystemcelAuthProvider";
import { HelpDropdown } from "../../shared/HelpDropdown";
import { NoDragImage } from "../../shared/NoDragImage";
import { preventNativeDrag } from "../../shared/noDrag";

type WelcomeNavItem = {
  label: string;
  href: string;
  hasMenu?: boolean;
  targetId?: string;
};

const navItems: WelcomeNavItem[] = [
  { label: "Ürün", href: "#urun", targetId: "urun" },
  { label: "Özellikler", href: "#ozellikler", targetId: "ozellikler" },
  { label: "Fiyatlandırma", href: "#fiyatlandirma", targetId: "fiyatlandirma" },
  { label: "Yardım", href: "/urun#yardim", hasMenu: true },
  { label: "Hakkımızda", href: "/hakkimizda" }
];

const featureModules = [
  {
    icon: CreditCard,
    title: "Gelir / Gider Takibi",
    text: "Günlük kasa hareketlerini kalem, ödeme yöntemi ve açıklama bazında kaydet."
  },
  {
    icon: Building2,
    title: "Cari Hesaplar",
    text: "Müşteri ve tedarikçi bakiyelerini, hareketlerini ve geçmiş işlemlerini takip et."
  },
  {
    icon: Package,
    title: "Ürün / Stok",
    text: "Ürün, hizmet, barkod, alış-satış fiyatı ve stok hareketlerini yönet."
  },
  {
    icon: ReceiptText,
    title: "Faturalar",
    text: "Satış ve alış faturalarını oluştur, satırları düzenle, ödeme durumunu izle."
  },
  {
    icon: WalletCards,
    title: "Tahsilat / Ödeme",
    text: "Bekleyen faturaları tahsilata veya ödemeye bağla, nakit akışını güncel tut."
  },
  {
    icon: BarChart3,
    title: "Raporlar",
    text: "Finansal özetleri, dönemsel raporları ve çıktı akışlarını tek merkezden hazırla."
  },
  {
    icon: Landmark,
    title: "GİB e-Arşiv Portal",
    text: "GİB portala girmene gerek kalmadan uygulama üzerinden fatura kes."
  },
  {
    icon: Send,
    title: "Telegram Bildirimleri",
    text: "Finansal özetleri ve önemli durumları Telegram üzerinden takip et."
  }
];

type PricingAudience = "isletme" | "muhasebeci";

const accountantSignupHref = "/kayit?hesapTipi=Muhasebeci&returnUrl=%2Fapp%2Fmuhasebeci";

type PricingPlan = {
  title: string;
  price: string;
  icon: React.ElementType<{ size?: number }>;
  features: string[];
  recommended?: boolean;
};

const pricingContent: Record<
  PricingAudience,
  {
    title: string;
    text: string;
    note: string;
    plans: PricingPlan[];
  }
> = {
  isletme: {
    title: "İhtiyacına göre ölçeklenen planlar",
    text: "Küçük ekiplerden büyüyen operasyonlara kadar Systemcel'i bütçene göre başlat.",
    note: "Bugün ödeme alınmaz. 30 gün sonra Başlangıç planı 199 TL/ay olarak başlar.",
    plans: [
      {
        title: "Ücretsiz",
        price: "0",
        icon: CreditCard,
        features: ["Temel finans takibi", "1 kullanıcı", "AI yok"]
      },
      {
        title: "Başlangıç",
        price: "199",
        icon: Send,
        recommended: true,
        features: ["OCR", "GİB e-Arşiv", "Telegram bildirimleri", "50 AI mesajı / ay"]
      },
      {
        title: "İşletme",
        price: "399",
        icon: WalletCards,
        features: ["Başlangıç'taki her şey", "Sınırsız AI", "3 kullanıcı"]
      }
    ]
  },
  muhasebeci: {
    title: "Müşteri portföyüne göre büyüyen planlar",
    text: "Muhasebeciler için müşteri, AI ve dönem yönetimi tek abonelik altında.",
    note: "20 müşteride Standart fiyatı Pro ile eşitlenir; aynı fiyata Pro önerilir.",
    plans: [
      {
        title: "Ücretsiz",
        price: "0",
        icon: CreditCard,
        features: ["3 müşteriye kadar", "1 kullanıcı", "AI yok"]
      },
      {
        title: "Standart",
        price: "699",
        icon: Send,
        recommended: true,
        features: ["10 müşteri dahil", "100 AI mesajı / ay", "Ek müşteri +50 TL / ay"]
      },
      {
        title: "Pro",
        price: "1199",
        icon: WalletCards,
        features: ["Sınırsız müşteri", "Sınırsız AI", "Dönem otomasyonu", "Müşteri sağlık skoru"]
      }
    ]
  }
};

export function WelcomeSayfasi() {
  const auth = useSystemcelAuth();
  const [pricingAudience, setPricingAudience] = React.useState<PricingAudience>("isletme");
  const [scrollNavState, setScrollNavState] = React.useState({ index: 0, total: 0 });
  const pageRef = React.useRef<HTMLElement | null>(null);
  const scrollKilitli = React.useRef(false);
  const scrollBitisSuresiMs = 760;
  const activePricing = pricingContent[pricingAudience];
  const signedInAppHref = auth.clerkEnabled && auth.isLoaded && auth.isSignedIn ? "/app" : "";
  const businessSignupHref = signedInAppHref || "/kayit?hesapTipi=Isletme";
  const signInHref = signedInAppHref || "/giris";
  const accountantCtaHref = signedInAppHref || accountantSignupHref;

  function pencereyeKaydir(page: HTMLElement, target: HTMLElement, behavior: ScrollBehavior = "smooth") {
    const top = target.offsetTop;

    page.scrollTo({ top, behavior });
    window.setTimeout(() => {
      page.scrollTop = top;
    }, behavior === "smooth" ? scrollBitisSuresiMs : 0);
  }

  function kaydirmaHedefleri(page: HTMLElement) {
    return [
      page.querySelector<HTMLElement>(".welcome-screen--hero"),
      document.getElementById("urun"),
      document.getElementById("ozellikler"),
      document.getElementById("fiyatlandirma")
    ].filter((item): item is HTMLElement => Boolean(item));
  }

  function aktifBolumIndexi(page: HTMLElement, targets = kaydirmaHedefleri(page)) {
    if (targets.length === 0) return 0;

    const currentTop = page.scrollTop;
    return targets.reduce((bestIndex, target, index) => {
      const bestDistance = Math.abs(targets[bestIndex].offsetTop - currentTop);
      const distance = Math.abs(target.offsetTop - currentTop);
      return distance < bestDistance ? index : bestIndex;
    }, 0);
  }

  function kaydirmaDurumunuGuncelle() {
    const page = pageRef.current;
    if (!page) return;

    const targets = kaydirmaHedefleri(page);
    setScrollNavState({ index: aktifBolumIndexi(page, targets), total: targets.length });
  }

  function bolumeGit(direction: -1 | 1) {
    const page = pageRef.current;
    if (!page) return;

    const targets = kaydirmaHedefleri(page);
    if (targets.length === 0) return;

    const currentIndex = aktifBolumIndexi(page, targets);
    const nextIndex = Math.max(0, Math.min(targets.length - 1, currentIndex + direction));
    pencereyeKaydir(page, targets[nextIndex], "smooth");
    setScrollNavState({ index: nextIndex, total: targets.length });
  }

  function hedefeKaydir(targetId: string, behavior: ScrollBehavior = "smooth") {
    const page = pageRef.current;
    const target = document.getElementById(targetId);
    if (!page || !target) return;

    pencereyeKaydir(page, target, behavior);
  }

  React.useEffect(() => {
    const page = pageRef.current;
    if (!page) return;
    const scrollPage = page;
    let snapTimer = 0;
    const mobileOrTouch = window.matchMedia("(max-width: 767px), (pointer: coarse)").matches;

    if (mobileOrTouch) {
      function normalKaydirmayiEngelle(event: WheelEvent | TouchEvent) {
        event.preventDefault();
      }

      window.addEventListener("wheel", normalKaydirmayiEngelle, { passive: false });
      scrollPage.addEventListener("touchmove", normalKaydirmayiEngelle, { passive: false });
      return () => {
        window.removeEventListener("wheel", normalKaydirmayiEngelle);
        scrollPage.removeEventListener("touchmove", normalKaydirmayiEngelle);
      };
    }

    function wheelIleKaydir(event: WheelEvent) {
      if (Math.abs(event.deltaY) < 18) return;

      event.preventDefault();
      if (scrollKilitli.current) return;

      const targets = kaydirmaHedefleri(scrollPage);
      if (targets.length === 0) return;

      const currentTop = scrollPage.scrollTop;
      const currentIndex = aktifBolumIndexi(scrollPage, targets);

      const nextIndex = Math.max(0, Math.min(targets.length - 1, currentIndex + (event.deltaY > 0 ? 1 : -1)));
      if (nextIndex === currentIndex && Math.abs(targets[currentIndex].offsetTop - currentTop) < 4) return;

      scrollKilitli.current = true;
      pencereyeKaydir(scrollPage, targets[nextIndex], "smooth");
      window.setTimeout(() => {
        scrollPage.scrollTop = targets[nextIndex].offsetTop;
        scrollKilitli.current = false;
      }, scrollBitisSuresiMs);
    }

    function kaymaBitinceKilitle() {
      window.clearTimeout(snapTimer);
      snapTimer = window.setTimeout(() => {
        const targets = kaydirmaHedefleri(scrollPage);
        if (targets.length === 0) return;

        const currentTop = scrollPage.scrollTop;
        const nearest = targets.reduce((best, target) => {
          return Math.abs(target.offsetTop - currentTop) < Math.abs(best.offsetTop - currentTop) ? target : best;
        }, targets[0]);

        if (Math.abs(nearest.offsetTop - currentTop) > 3) {
          scrollPage.scrollTo({ top: nearest.offsetTop, behavior: "auto" });
        }
      }, 140);
    }

    window.addEventListener("wheel", wheelIleKaydir, { passive: false });
    scrollPage.addEventListener("scroll", kaymaBitinceKilitle, { passive: true });
    return () => {
      window.clearTimeout(snapTimer);
      window.removeEventListener("wheel", wheelIleKaydir);
      scrollPage.removeEventListener("scroll", kaymaBitinceKilitle);
    };
  }, []);

  React.useEffect(() => {
    const page = pageRef.current;
    if (!page) return;

    kaydirmaDurumunuGuncelle();
    page.addEventListener("scroll", kaydirmaDurumunuGuncelle, { passive: true });
    window.addEventListener("resize", kaydirmaDurumunuGuncelle);
    return () => {
      page.removeEventListener("scroll", kaydirmaDurumunuGuncelle);
      window.removeEventListener("resize", kaydirmaDurumunuGuncelle);
    };
  }, []);

  React.useEffect(() => {
    document.title = "Systemcel - Finance Suite";
  }, []);

  React.useEffect(() => {
    const targetId = window.location.hash.replace("#", "");
    if (!targetId) return;

    window.requestAnimationFrame(() => {
      hedefeKaydir(targetId, "auto");
    });
  }, []);

  function navTiklandi(event: React.MouseEvent<HTMLAnchorElement>, targetId?: string) {
    if (!targetId) return;

    event.preventDefault();
    hedefeKaydir(targetId);
    window.history.replaceState(null, "", `#${targetId}`);
  }

  return (
    <main className="welcome-page" ref={pageRef}>
      <div className="welcome-screen welcome-screen--hero">
        <header className="welcome-nav" aria-label="Systemcel giriş menüsü">
          <a className="welcome-brand" href="/" aria-label="Systemcel ana sayfa">
            <span className="welcome-brand__mark">
              <NoDragImage src={systemcelIcon} alt="" />
            </span>
            <span className="welcome-brand__text">
              <strong>SYSTEMCEL</strong>
              <small>Finance Suite</small>
            </span>
          </a>

          <nav className="welcome-nav__links" aria-label="Ürün bağlantıları">
            {navItems.map((item) =>
              item.label === "Yardım" ? (
                <HelpDropdown key={item.href} />
              ) : (
                <a key={item.href} href={item.href} onClick={(event) => navTiklandi(event, item.targetId)}>
                  {item.label}
                  {item.hasMenu ? <ChevronDown size={16} /> : null}
                </a>
              )
            )}
          </nav>

          <div className="welcome-nav__actions">
            <button type="button" className="welcome-lang">
              TR
              <ChevronDown size={16} />
            </button>
            <a className="welcome-contact" href="mailto:iletisim@systemcel.com">
              İletişim
            </a>
          </div>

          <div className="welcome-mobile-actions">
            <button type="button" className="welcome-mobile-menu" aria-label="Menüyü aç">
              <Menu size={20} />
            </button>
          </div>
        </header>

        <section className="welcome-hero" aria-labelledby="welcome-title">
          <div className="welcome-copy">
            <h1 id="welcome-title" className="welcome-title-desktop">
              İşletmenin gelir, gider
              <br />
              akışını AI destekli
              <br />
              araçlarla tek yerden yönet
            </h1>
            <h1 className="welcome-title-mobile">Nakit akışını AI destekli araçlarla yönet.</h1>

            <p className="welcome-lead welcome-lead-desktop">
              Finansal süreçlerinizi otomatikleştirin, nakit akışınızı görünür kılın, doğru kararları daha hızlı alın.
            </p>
            <p className="welcome-lead welcome-lead-mobile">
              Gelir, gider, fatura ve nakit akışınızı tek yerden takip edin. SYSTEMCEL, finansal kararlarınızı hızlandırır.
            </p>

            <div className="welcome-cta-row welcome-cta-row--desktop">
              <a className="welcome-cta welcome-cta--primary" href={businessSignupHref}>
                Ücretsiz başla
                <ArrowRight size={22} />
              </a>
              <a className="welcome-cta welcome-cta--secondary" href={signInHref}>
                {signedInAppHref ? "Panele git" : "Giriş yap"}
                <ArrowRight size={22} />
              </a>
            </div>

            <div className="welcome-cta-row welcome-cta-row--mobile">
              <a className="welcome-cta welcome-cta--primary" href={businessSignupHref}>
                Ücretsiz başla
                <ArrowRight size={22} />
              </a>
            </div>

            <a className="welcome-accountant-link welcome-accountant-link--desktop" href={accountantCtaHref}>
              <UserRound size={21} />
              <span>Muhasebeci olarak devam et</span>
              <ArrowRight size={18} />
            </a>
            <a className="welcome-mobile-secondary-link" href={accountantCtaHref}>
              Muhasebeci misiniz? Devam edin <span aria-hidden="true">→</span>
            </a>

            <div className="welcome-proof-grid" aria-label="Systemcel güven özellikleri">
              <article>
                <span className="no-drag" draggable={false} onDragStart={preventNativeDrag}>
                  <ShieldCheck size={34} />
                </span>
                <strong>Verileriniz güvende</strong>
                <span>256-bit şifreleme</span>
              </article>
              <article>
                <span className="no-drag" draggable={false} onDragStart={preventNativeDrag}>
                  <Cloud size={34} />
                </span>
                <strong>Bulut tabanlı erişim</strong>
                <span>Her yerden erişin</span>
              </article>
              <article>
                <span className="no-drag" draggable={false} onDragStart={preventNativeDrag}>
                  <Clock3 size={34} />
                </span>
                <strong>Gerçek zamanlı analiz</strong>
                <span>Anlık içgörüler</span>
              </article>
            </div>
          </div>

          <div className="welcome-preview" aria-label="Systemcel uygulama önizlemesi">
            <div className="welcome-preview-window welcome-preview-window--image">
              <NoDragImage src={welcomeDashboardPreview} alt="Systemcel dashboard önizlemesi" loading="eager" />
            </div>
          </div>
        </section>
      </div>

      <section id="urun" className="welcome-product-hero" aria-labelledby="welcome-product-title">
        <NoDragImage src={productHeroOverview} alt="" />
        <div className="welcome-product-hero__shade" />
        <div className="welcome-product-hero__copy">
          <h2 id="welcome-product-title">Finansal operasyonlarını tek panelden yönet</h2>
          <p>
            Systemcel; gelir, gider, fatura, cari hesap, stok, tahsilat ve raporlama süreçlerini sade
            bir dashboard altında toplar.
          </p>
          <div className="welcome-product-hero__actions">
            <a className="welcome-cta welcome-cta--primary" href={businessSignupHref}>
              Ücretsiz başla
              <ArrowRight size={22} />
            </a>
            <a className="welcome-cta welcome-cta--secondary" href="mailto:iletisim@systemcel.com">
              Demo talep et
            </a>
          </div>
          <small>KOBİ’ler, muhasebeciler ve büyüyen ekipler için tasarlandı.</small>
        </div>
      </section>

      <section id="ozellikler" className="welcome-features" aria-labelledby="welcome-features-title">
        <div className="welcome-features__inner">
          <div className="welcome-features__heading">
            <span>Modüller</span>
            <h2 id="welcome-features-title">Finans yönetiminin bütün araçları elinin altında</h2>
            <p>Kayıt, fatura, cari, stok, tahsilat ve rapor akışları aynı ürün deneyimi içinde birbirine bağlanır.</p>
          </div>

          <div className="welcome-features__grid">
            {featureModules.map((module) => {
              const Icon = module.icon;
              return (
                <article key={module.title}>
                  <span className="welcome-features__icon">
                    <Icon size={30} />
                  </span>
                  <strong>{module.title}</strong>
                  <span>{module.text}</span>
                </article>
              );
            })}
          </div>

          <div className="welcome-features__ai">
            <Sparkles size={27} />
            <span>ve tüm bunları <strong>yapay zeka asistanıyla</strong> beraber otomatize et</span>
          </div>
        </div>
      </section>

      <section id="fiyatlandirma" className="welcome-pricing" aria-labelledby="welcome-pricing-title">
        <div className="welcome-pricing__inner">
          <div className="welcome-pricing__tabs" role="tablist" aria-label="Fiyatlandırma türü">
            <button
              type="button"
              role="tab"
              aria-selected={pricingAudience === "isletme"}
              className={pricingAudience === "isletme" ? "active" : ""}
              onClick={() => setPricingAudience("isletme")}
            >
              <Building2 size={23} />
              İşletmeler
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={pricingAudience === "muhasebeci"}
              className={pricingAudience === "muhasebeci" ? "active" : ""}
              onClick={() => setPricingAudience("muhasebeci")}
            >
              <UserRound size={23} />
              Muhasebeciler
            </button>
          </div>

          <div className="welcome-pricing__heading">
            <h2 id="welcome-pricing-title">{activePricing.title}</h2>
            <p>{activePricing.text}</p>
          </div>

          <div className="welcome-pricing__grid">
            {activePricing.plans.map((plan) => {
              const Icon = plan.icon;
              return (
                <article className={plan.recommended ? "welcome-pricing-card is-featured" : "welcome-pricing-card"} key={plan.title}>
                  {plan.recommended ? (
                    <div className="welcome-pricing-card__badge">
                      <Sparkles size={18} />
                      ÖNERİLEN
                    </div>
                  ) : null}

                  <div className="welcome-pricing-card__title">
                    <span>
                      <Icon size={31} />
                    </span>
                    <h3>{plan.title}</h3>
                  </div>

                  <div className="welcome-pricing-card__price">
                    <strong>{plan.price}</strong>
                    <span>TL / ay</span>
                  </div>

                  <ul>
                    {plan.features.map((feature) => (
                      <li key={feature}>
                        <Check size={15} />
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <a
                    className={plan.recommended ? "welcome-pricing-card__button is-primary" : "welcome-pricing-card__button"}
                    href={signedInAppHref || (pricingAudience === "muhasebeci" ? accountantSignupHref : "/kayit?hesapTipi=Isletme")}
                  >
                    Planı seç
                  </a>
                </article>
              );
            })}
          </div>

          <div className="welcome-pricing__note">
            <Info size={22} />
            <span>{activePricing.note}</span>
          </div>
        </div>
      </section>

      <div className="welcome-scroll-nav" aria-label="Sayfa bÃ¶lÃ¼m navigasyonu">
        <button
          type="button"
          aria-label="YukarÄ±daki bÃ¶lÃ¼me git"
          onClick={() => bolumeGit(-1)}
          disabled={scrollNavState.index <= 0}
        >
          <ArrowUp size={20} />
        </button>
        <button
          type="button"
          aria-label="AÅŸaÄŸÄ±daki bÃ¶lÃ¼me git"
          onClick={() => bolumeGit(1)}
          disabled={scrollNavState.total === 0 || scrollNavState.index >= scrollNavState.total - 1}
        >
          <ArrowDown size={20} />
        </button>
      </div>
    </main>
  );
}
