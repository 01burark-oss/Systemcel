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
    text: "Toplam gelir, gider, net kâr ve ödeme dağılımını dashboard üzerinden izle."
  },
  {
    title: "Operasyonu hızlandır",
    text: "Tekrarlayan finans işlerini daha az manuel işlemle yönet."
  }
];

const modules = [
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
    text: "GİB bağlantı bilgilerini güvenli sakla, taslak ve SMS onay akışlarını yönet."
  },
  {
    icon: Send,
    title: "Telegram Bildirimleri",
    text: "Finansal özetleri ve önemli durumları Telegram üzerinden takip et."
  }
];

const workflow = [
  ["İşletmeni seç", "Her işletmenin kayıtları, ayarları ve raporları ayrı tutulur."],
  ["Gelir ve giderleri işle", "Kasa hareketlerini ödeme yöntemi ve kalem bazında kaydet."],
  ["Fatura oluştur", "Cari, ürün/hizmet ve ödeme bilgilerini faturaya bağla."],
  ["Tahsilat veya ödeme al", "Faturanın finansal karşılığını nakit akışına yansıt."],
  ["Raporla ve karar al", "Dashboard ve raporlarla işletmenin durumunu net şekilde gör."]
];

const automationItems = [
  "Finansal özetleri anlaşılır hale getirir",
  "Dönemsel performansı karşılaştırmayı kolaylaştırır",
  "Rapor ve bildirim akışlarını hızlandırır",
  "AI destekli öneriler için sağlam veri zemini oluşturur"
];

const securityItems = [
  "İşletme bazlı ayrım",
  "Güvenli bağlantı bilgisi saklama",
  "Yetkilendirmeye hazır mimari",
  "Kontrollü veri erişimi"
];

const pricingPlans = [
  ["Başlangıç", "Temel kayıt, dashboard ve raporlama akışları."],
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
          <a className="product-contact" href="mailto:iletisim@systemcel.com">
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
          <h1 id="product-title">Finansal operasyonlarını tek panelden yönet</h1>
          <p>
            Systemcel; gelir, gider, fatura, cari hesap, stok, tahsilat ve raporlama süreçlerini sade
            bir dashboard altında toplar.
          </p>
          <div className="product-hero__actions">
            <a className="product-btn product-btn--primary" href="/">
              Ücretsiz başla
              <ArrowRight size={20} />
            </a>
            <a className="product-btn product-btn--secondary" href="mailto:iletisim@systemcel.com">
              Demo talep et
            </a>
          </div>
          <small>KOBİ’ler, muhasebeciler ve büyüyen ekipler için tasarlandı.</small>
        </div>
      </section>

      <section className="product-band product-problem" id="genel-bakis">
        <div className="product-section-heading">
          <span>Genel Bakış</span>
          <h2>Finans verisi dağınıksa karar almak zorlaşır</h2>
          <p>
            Gelirler ayrı yerde, giderler ayrı tabloda, faturalar başka bir sistemde ve tahsilatlar
            manuel takip ediliyorsa işletmenin gerçek durumunu görmek zaman alır. Systemcel bu parçaları
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
          <span>Dashboard</span>
          <h2>Günün finansal fotoğrafı tek ekranda</h2>
          <p>
            Dashboard, işletmenin anlık finans durumunu sade ve okunabilir kartlarla gösterir. Bugünkü
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
            Kayıt, fatura, cari, stok, tahsilat ve rapor akışları aynı ürün deneyimi içinde birbirine bağlanır.
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
          <span>İş Akışı</span>
          <h2>Kayıttan rapora kadar uçtan uca akış</h2>
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
          <span className="product-section-kicker">AI ve Otomasyon</span>
          <h2>Daha az manuel takip, daha fazla içgörü</h2>
          <p>
            Systemcel’in hedefi yalnızca kayıt tutmak değil; işletmenin finansal ritmini daha okunabilir
            hale getirmektir. Akıllı özetler, otomatik rapor akışları ve bildirimlerle ekibin neye
            odaklanması gerektiğini daha hızlı görür.
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
          <h2>İşletme verileri güvenli ve düzenli tutulur</h2>
          <p>
            Systemcel, finans verilerini işletme bazlı ayrıştırır. Hassas bağlantı bilgileri güvenli
            şekilde saklanır; kullanıcı, işletme ve yetki yapısı modern web mimarisine uygun genişletilebilir
            bir temel üzerine kurulur.
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
          <p>Başlangıçtan muhasebeci portföylerine kadar farklı kullanım seviyeleri için hazırlanır.</p>
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
          <h2>Kurulumdan günlük kullanıma kadar yanında</h2>
          <p>
            İlk kurulum, GİB e-Arşiv, Telegram bildirimleri, abonelik ve günlük kullanım soruları için
            hızlı destek alanları hazırlanır.
          </p>
        </div>
        <div className="product-resource-row">
          {["Yardım Merkezi", "Kurulum Rehberleri", "GİB e-Arşiv Yardımı", "Sık Sorulan Sorular"].map((item) => (
            <a key={item} href="mailto:iletisim@systemcel.com">
              <FileText size={18} />
              {item}
            </a>
          ))}
        </div>
      </section>
    </main>
  );
}
