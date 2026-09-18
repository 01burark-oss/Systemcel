import React from "react";
import { ArrowRight, Bot, Building2, CheckCircle2, ChevronDown, Menu, ShieldCheck } from "lucide-react";
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

const aboutLines = [
  {
    icon: Building2,
    title: "KOBİ ve muhasebeci odağı",
    text: "Kendi finansını yöneten işletmeler ve birden fazla müşteriyi takip eden muhasebeciler için sade bir çalışma alanı geliştiriyoruz."
  },
  {
    icon: CheckCircle2,
    title: "Sade finans dili",
    text: "Gelir, gider, fatura, cari, stok ve rapor süreçlerini karmaşık muhasebe ekranlarından çıkarıp anlaşılır iş adımlarına dönüştürüyoruz."
  },
  {
    icon: Bot,
    title: "AI destekli kullanım",
    text: "Systemcel kayıtları özetler ve hangi ödeme, tahsilat veya stok hareketine bakmanız gerektiğini gösterir."
  },
  {
    icon: ShieldCheck,
    title: "Ayrı işletme kayıtları",
    text: "Her işletmenin ve muhasebeciye bağlı her müşterinin kayıtları ayrı çalışma alanlarında tutulur."
  }
];

export function HakkimizdaSayfasi() {
  React.useEffect(() => {
    document.title = "Systemcel Hakkımızda | Finance Suite";
  }, []);

  return (
    <main className="about-page">
      <header className="product-nav" aria-label="Systemcel hakkımızda menüsü">
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
              <a className={item.href === "/hakkimizda" ? "active" : undefined} key={item.href} href={item.href}>
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

      <section className="about-hero" aria-labelledby="about-title">
        <span>Hakkımızda</span>
        <h1 id="about-title">İşletmelerin finansını daha kolay yönetmesi için geliştiriyoruz</h1>
        <p>
          Systemcel; gelir, gider, fatura, cari hesap, stok ve raporları tek yerde toplar. İşletmelerin
          muhasebecileriyle çalışmasını ve tedarikçilerden alım yapmasını kolaylaştırır.
        </p>
      </section>

      <section className="about-story" aria-label="Systemcel yaklaşımı">
        <div>
          <span>Neden</span>
          <h2>Dağınık finans kayıtlarını tek ekranda topluyoruz</h2>
        </div>
        <div>
          <p>
            Birçok işletmede gelirler, giderler, faturalar, tahsilatlar ve raporlar farklı dosya, mesaj ya da sistemlerde
            takip ediliyor. Bu da karar almayı yavaşlatıyor ve önemli ödeme ya da tahsilatların geç fark edilmesine yol açıyor.
          </p>
          <p>
            Systemcel bu parçaları tek yerde birleştirir. Kayıtlar düzenli tutulur, raporlar anlaşılır biçimde
            hazırlanır ve kullanıcı hangi konuya bakması gerektiğini hızlıca görür.
          </p>
        </div>
      </section>

      <section className="about-lines" aria-label="Systemcel değerleri">
        {aboutLines.map((item) => {
          const Icon = item.icon;
          return (
            <article key={item.title}>
              <Icon size={26} />
              <div>
                <h2>{item.title}</h2>
                <p>{item.text}</p>
              </div>
            </article>
          );
        })}
      </section>

      <section className="about-close">
        <p>Systemcel, finans yönetimini daha erişilebilir, hızlı ve anlaşılır hale getirme hedefiyle geliştirilmeye devam ediyor.</p>
        <a className="product-btn product-btn--primary" href="/">
          Ücretsiz başla
          <ArrowRight size={20} />
        </a>
      </section>
    </main>
  );
}
