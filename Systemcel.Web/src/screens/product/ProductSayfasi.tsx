import React from "react";
import {
  ArrowRight,
  BarChart3,
  Bot,
  Building2,
  CheckCircle2,
  ChevronDown,
  ClipboardList,
  CreditCard,
  FileText,
  Landmark,
  LockKeyhole,
  Menu,
  Package,
  ReceiptText,
  Send,
  Store,
  Users,
  WalletCards
} from "lucide-react";
import productHeroOverview from "../../assets/product-hero-overview.png";
import systemcelIcon from "../../assets/systemcel-icon.png";
import { HelpDropdown } from "../../shared/HelpDropdown";
import { NoDragImage } from "../../shared/NoDragImage";

const navItems = [
  { label: "Ürün", href: "/urun" },
  { label: "Özellikler", href: "/urun#moduller" },
  { label: "Fiyatlandırma", href: "/urun#fiyatlandirma" },
  { label: "Yardım", href: "/urun#yardim", hasMenu: true },
  { label: "Hakkımızda", href: "/hakkimizda" }
];

const problemCards = [
  {
    title: "Dağınık kayıtları toparla",
    text: "Gelir, gider, fatura, tahsilat ve stok hareketlerini aynı işletme altında takip et."
  },
  {
    title: "Bugünü anında gör",
    text: "Toplam gelir, gider, net kâr ve ödeme dağılımını ana ekrandan izle."
  },
  {
    title: "Günlük işleri hızlandır",
    text: "Tekrarlayan finans işlerini daha az elle takip et."
  }
];

const modules = [
  {
    icon: CreditCard,
    title: "Gelir ve gider takibi",
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
    text: "Finansal özetleri, dönem raporlarını ve çıktıları tek yerden hazırla."
  },
  {
    icon: Landmark,
    title: "GİB e-Arşiv Portal",
    text: "Fatura taslağını hazırla ve GİB SMS koduyla gönderimi tamamla."
  },
  {
    icon: Send,
    title: "Telegram bildirimleri",
    text: "Finansal özetleri ve önemli durumları Telegram üzerinden takip et."
  },
  {
    icon: Users,
    title: "Muhasebeciyle çalışma",
    text: "Muhasebecini davet et, yetkisini seç ve belgelerle görüşmeleri tek yerde yönet."
  },
  {
    icon: Store,
    title: "Tedarikçi pazaryeri",
    text: "Birden fazla tedarikçiden alım yap; teslimatları ve faturaları ayrı takip et."
  }
];

const workflow = [
  ["İşletmeni seç", "Her işletmenin kayıtları, ayarları ve raporları ayrı tutulur."],
  ["Gelir ve giderleri işle", "Kasa hareketlerini ödeme yöntemi ve kalem bazında kaydet."],
  ["Fatura oluştur", "Cari, ürün/hizmet ve ödeme bilgilerini faturaya bağla."],
  ["Tahsilat veya ödeme al", "Faturanın finansal karşılığını nakit akışına yansıt."],
  ["Raporla ve karar al", "Ana ekran ve raporlarla işletmenin durumunu net şekilde gör."]
];

const automationItems = [
  "Finansal özetleri anlaşılır hale getirir",
  "Dönemsel performansı karşılaştırmayı kolaylaştırır",
  "Rapor ve bildirimleri daha hızlı hazırlar",
  "AI destekli öneriler için sağlam veri zemini oluşturur"
];

const securityItems = [
  "Her işletme için ayrı kayıtlar",
  "Hesapla sınırlanan erişim",
  "Muhasebeci için seçilebilir yetki",
  "Müşteri kayıtlarının ayrılması"
];

const pricingPlans = [
  ["Başlangıç", "Temel kayıt, ana ekran ve raporlar."],
  ["Pro", "Fatura, stok, tahsilat ve gelişmiş bildirimler."],
  ["Muhasebeci", "Çoklu işletme ve müşteri portföyü yönetimi."],
  ["Kurumsal", "Ekip, yetki ve özel entegrasyon ihtiyaçları."]
];

export function ProductSayfasi() {
  const pageRef = React.useRef<HTMLElement | null>(null);

  function bolumeKaydir(targetId: string, behavior: ScrollBehavior = "smooth") {
    const page = pageRef.current;
    const target = document.getElementById(targetId);
    if (!page || !target) return;

    page.scrollTo({ top: Math.max(0, target.offsetTop - 76), behavior });
  }

  React.useEffect(() => {
    document.title = "Systemcel Ürün | Finansal Operasyon Yönetimi";
  }, []);

  React.useEffect(() => {
    const hashIleKaydir = (behavior: ScrollBehavior = "auto") => {
      const targetId = window.location.hash.replace("#", "");
      if (!targetId) return;

      window.requestAnimationFrame(() => bolumeKaydir(targetId, behavior));
    };

    hashIleKaydir("auto");

    const hashDegisti = () => hashIleKaydir("smooth");
    window.addEventListener("hashchange", hashDegisti);

    return () => window.removeEventListener("hashchange", hashDegisti);
  }, []);

  function navTiklandi(event: React.MouseEvent<HTMLAnchorElement>, href: string) {
    const targetId = href.startsWith("/urun#") ? href.split("#")[1] : "";
    if (!targetId) return;

    event.preventDefault();
    window.history.replaceState(null, "", `/urun#${targetId}`);
    bolumeKaydir(targetId);
  }

  return (
    <main className="product-page" ref={pageRef}>
      <header className="product-nav" aria-label="Systemcel ürün menüsü">
        <a className="product-brand" href="/" aria-label="Systemcel ana sayfa">
          <span className="product-brand__mark">
            <NoDragImage src={systemcelIcon} alt="" />
          </span>
          <span className="product-brand__text">
            <strong>SYSTEMCEL</strong>
            <small>Finance Suite</small>
          </span>
        </a>

        <nav className="product-nav__links" aria-label="Ürün bağlantıları">
          {navItems.map((item) =>
            item.label === "Yardım" ? (
              <HelpDropdown key={item.href} />
            ) : (
              <a key={item.href} href={item.href} onClick={(event) => navTiklandi(event, item.href)}>
                {item.label}
                {item.hasMenu ? <ChevronDown size={16} /> : null}
              </a>
            )
          )}
        </nav>

        <div className="product-nav__actions">
          <button type="button" className="product-lang">
            TR
            <ChevronDown size={16} />
          </button>
          <a className="product-contact" href="mailto:merhaba@systemcel.app">
            İletişim
          </a>
        </div>

        <button type="button" className="product-mobile-menu" aria-label="Menüyü aç">
          <Menu size={21} />
        </button>
      </header>

      <section className="product-hero" aria-labelledby="product-title">
        <NoDragImage src={productHeroOverview} alt="" />
        <div className="product-hero__shade" />
        <div className="product-hero__copy">
          <h1 id="product-title">İşletmenin finansını tek yerden yönet</h1>
          <p>
            Systemcel; gelir, gider, fatura, cari hesap, stok, tahsilat ve raporları tek ekranda toplar.
            Muhasebecinle birlikte çalışmanı ve tedarikçilerden alım yapmanı kolaylaştırır.
          </p>
          <div className="product-hero__actions">
            <a className="product-btn product-btn--primary" href="/">
              Ücretsiz başla
              <ArrowRight size={20} />
            </a>
            <a className="product-btn product-btn--secondary" href="mailto:merhaba@systemcel.app">
              Demo talep et
            </a>
          </div>
          <small>KOBİ’ler, muhasebeciler ve büyüyen ekipler için tasarlandı.</small>
        </div>
      </section>

      <section className="product-band product-problem" id="genel-bakis">
        <div className="product-section-heading">
          <span>Genel bakış</span>
          <h2>Finans verisi dağınıksa karar almak zorlaşır</h2>
          <p>
            Gelirler ayrı yerde, giderler ayrı tabloda, faturalar başka bir sistemde ve tahsilatlar
            elle takip ediliyorsa işletmenin gerçek durumunu görmek zaman alır. Systemcel bu parçaları
            birleştirir.
          </p>
        </div>
        <div className="product-problem__grid">
          {problemCards.map((card) => (
            <article key={card.title}>
              <CheckCircle2 size={24} />
              <h3>{card.title}</h3>
              <p>{card.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="product-band product-dashboard" id="dashboard">
        <div className="product-section-heading">
          <span>Ana ekran</span>
          <h2>Günün finansal fotoğrafı tek ekranda</h2>
          <p>
            Ana ekran, işletmenin güncel finans durumunu sade kartlarla gösterir. Bugünkü
            gelir, gider, net kâr, ödeme yöntemleri ve dönemsel özetler aynı alanda görünür.
          </p>
        </div>
        <div className="product-dashboard__list">
          {[
            "Toplam gelir, toplam gider ve net kâr",
            "Nakit, kredi kartı, online ödeme ve havale dağılımı",
            "Bugün, son 30 gün ve son 1 yıl özetleri",
            "Trend grafikleriyle hızlı performans okuması"
          ].map((item) => (
            <span key={item}>
              <BarChart3 size={18} />
              {item}
            </span>
          ))}
        </div>
      </section>

      <section className="product-band product-modules" id="moduller">
        <div className="product-section-heading">
          <span>Modüller</span>
          <h2>Finans yönetiminin temel modülleri hazır</h2>
          <p>
            Kayıt, fatura, cari hesap, stok, tahsilat ve raporlar aynı verilerden beslenir.
          </p>
        </div>
        <div className="product-module-grid">
          {modules.map((item) => {
            const Icon = item.icon;
            return (
              <article key={item.title}>
                <Icon size={24} />
                <h3>{item.title}</h3>
                <p>{item.text}</p>
              </article>
            );
          })}
        </div>
      </section>

      <section className="product-band product-workflow" id="is-akisi">
        <div className="product-section-heading">
          <span>İş akışı</span>
          <h2>Kayıttan rapora tüm süreç</h2>
        </div>
        <div className="product-workflow__steps">
          {workflow.map(([title, text], index) => (
            <article key={title}>
              <strong>{index + 1}</strong>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="product-band product-split" id="otomasyon">
        <div>
          <span className="product-section-kicker">Yapay zekâ ve otomasyon</span>
          <h2>Tekrarlayan işleri azalt, önemli değişiklikleri gör</h2>
          <p>
            Systemcel kayıtları özetler, raporları hazırlar ve önemli değişiklikleri gösterir. Böylece ekip
            hangi ödeme, tahsilat veya stok hareketine bakması gerektiğini daha hızlı görür.
          </p>
        </div>
        <div className="product-check-list">
          {automationItems.map((item) => (
            <span key={item}>
              <Bot size={18} />
              {item}
            </span>
          ))}
        </div>
      </section>

      <section className="product-band product-split" id="guvenlik">
        <div>
          <span className="product-section-kicker">Güvenlik</span>
          <h2>Her işletmenin kayıtları ayrı tutulur</h2>
          <p>
            Systemcel her işletmenin kayıtlarını kendi alanında tutar. Muhasebeci bağlantısında erişim
            düzeyi seçilir; müşteri kayıtları birbirine karışmadan yönetilir.
          </p>
        </div>
        <div className="product-security-grid">
          {securityItems.map((item) => (
            <article key={item}>
              <LockKeyhole size={20} />
              <strong>{item}</strong>
            </article>
          ))}
        </div>
      </section>

      <section className="product-band product-pricing" id="fiyatlandirma">
        <div className="product-section-heading">
          <span>Fiyatlandırma</span>
          <h2>İşletmenin büyüklüğüne göre ölçeklenen planlar</h2>
          <p>İşletmeler ve birden fazla müşteriyi yöneten muhasebeciler için ayrı planlar bulunur.</p>
        </div>
        <div className="product-pricing__grid">
          {pricingPlans.map(([title, text]) => (
            <article key={title}>
              <ClipboardList size={22} />
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="product-band product-resources" id="yardim">
        <div className="product-section-heading">
          <span>Yardım</span>
          <h2>Kurulum ve kullanım yardımı</h2>
          <p>
            İlk kurulum, GİB e-Arşiv, Telegram bildirimleri, abonelik ve günlük kullanım konularında
            yardım alın.
          </p>
        </div>
        <div className="product-resource-row">
          {["Yardım Merkezi", "Kurulum Rehberleri", "GİB e-Arşiv Yardımı", "Sık Sorulan Sorular"].map((item) => (
            <a key={item} href="mailto:merhaba@systemcel.app">
              <FileText size={18} />
              {item}
            </a>
          ))}
        </div>
      </section>
    </main>
  );
}
