import React from "react";
import { ArrowLeft, ArrowRight, BriefcaseBusiness, Mail, Newspaper } from "lucide-react";
import { legalTexts, publicBusinessIdentity, type LegalTextKey } from "../auth/legalTexts";
import "./marketing.css";

export type PublicPageKind = "about" | "blog" | "careers" | "contact" | "cookies" | LegalTextKey;

export function PublicContentPage({ kind }: { kind: PublicPageKind }) {
  const language = window.localStorage.getItem("systemcel.language") === "en" ? "en" : "tr";
  const legalKey = kind === "terms" || kind === "privacy" || kind === "kvkk" || kind === "subscription" ? kind : null;
  const legal = legalKey ? legalTexts[language][legalKey] : null;

  React.useEffect(() => {
    document.title = legal?.title ?? pageTitle(kind, language);
  }, [kind, language, legal?.title]);

  return (
    <main className="marketing-page marketing-public-page">
      <header className="marketing-public-header">
        <a className="marketing-brand" href="/"><span className="marketing-brand-mark" aria-hidden="true"><i /><i /><i /><i /></span><strong>systemcel</strong></a>
        <a className="marketing-button marketing-button--ghost" href="/"><ArrowLeft size={17} />{language === "tr" ? "Ana sayfa" : "Home"}</a>
      </header>
      <section className="marketing-public-hero marketing-grid-bg">
        <div className="marketing-public-wrap">
          <span className="marketing-eyebrow"><i />SYSTEMCEL</span>
          <h1>{legal?.title ?? pageTitle(kind, language)}</h1>
          <p>{legal?.intro ?? pageLead(kind, language)}</p>
        </div>
      </section>
      <section className="marketing-public-content">
        <div className="marketing-public-wrap">
          {legal ? <LegalContent legal={legal} /> : kind === "about" ? <AboutContent language={language} /> : kind === "blog" ? <BlogContent language={language} /> : kind === "careers" ? <CareersContent language={language} /> : kind === "contact" ? <ContactContent language={language} /> : <CookiesContent language={language} />}
        </div>
      </section>
      <PublicFooter language={language} />
    </main>
  );
}

function LegalContent({ legal }: { legal: (typeof legalTexts)["tr"][LegalTextKey] }) {
  return <article className="marketing-legal-card"><div className="marketing-legal-meta"><strong>{legal.updatedAtLabel}</strong><span>{legal.updatedAt}</span></div>{legal.sections.map((section) => <section key={section.title}><h2>{section.title}</h2><p>{section.text}</p></section>)}<aside>{legal.note}</aside></article>;
}

function AboutContent({ language }: { language: "tr" | "en" }) {
  const tr = language === "tr";
  const points = tr ? [
    ["Muhasebeciyle ortak çalışma", "İşletmeler muhasebecilerini davet eder, yetkisini seçer ve kayıtları aynı çalışma alanında yönetir."],
    ["Sade finans dili", "Gelir, gider, fatura, cari, stok ve rapor süreçlerini anlaşılır iş adımlarına dönüştürüyoruz."],
    ["Tedarikçi pazaryeri", "Farklı tedarikçilerden alım yapmayı, teklif toplamayı ve teslimatları ayrı izlemeyi kolaylaştırır."]
  ] : [
    ["Accountant collaboration", "Businesses invite their accountant, choose access and manage records in one shared workspace."],
    ["A clearer finance language", "We turn income, expenses, invoices, accounts, inventory and reports into understandable workflows."],
    ["Supplier marketplace", "Businesses can buy from multiple suppliers, collect quotes and track deliveries separately."]
  ];

  return <>
    <article className="marketing-contact-card">
      <BriefcaseBusiness />
      <h2>{tr ? "İşletmenin finansını tek yerde topluyoruz." : "We bring business finance together in one place."}</h2>
      <p>{tr ? "Systemcel; işletmelerin finansını yönetmesi, muhasebecileriyle birlikte çalışması ve tedarikçilerden alım yapması için geliştirilen yerli bir platformdur." : "Systemcel is a Turkish platform for managing business finance, working with accountants and buying from suppliers."}</p>
      <a className="marketing-button marketing-button--ink" href="/#on-muhasebe">{tr ? "Ürünü keşfet" : "Explore the product"}<ArrowRight size={17} /></a>
    </article>
    <div className="marketing-content-grid">
      {points.map(([title, text], index) => <article className="marketing-content-card" key={title}><small>0{index + 1}</small><h2>{title}</h2><p>{text}</p></article>)}
    </div>
  </>;
}

function BlogContent({ language }: { language: "tr" | "en" }) {
  const posts = language === "tr" ? [
    ["Ön muhasebede tek veri kaynağı neden önemli?", "Gelir-gider, cari, stok ve faturaların aynı işletme bağlamında tutulmasının günlük kararları nasıl sadeleştirdiğini anlatıyoruz."],
    ["e-Arşiv fatura akışını düzenlemek", "Taslak, müşteri bilgi teyidi ve işletme telefonuna gelen GİB koduyla resmi kesim adımları."],
    ["Muhasebeciyle dijital çalışma alanı", "Talep, sohbet ve finansal veri paylaşımını e-posta zincirlerinden çıkarmanın pratik faydaları."],
  ] : [
    ["Why one source of truth matters in accounting", "How keeping income, expenses, accounts, inventory and invoices in one business context simplifies daily decisions."],
    ["Organizing the e-Archive invoice flow", "Drafting, customer detail confirmation, and issuing with the GİB code sent to the business phone."],
    ["A digital workspace with your accountant", "Practical benefits of moving requests, chat and financial data sharing beyond email chains."],
  ];
  return <div className="marketing-content-grid">{posts.map(([title, text], index) => <article className="marketing-content-card" key={title}><Newspaper /><small>0{index + 1}</small><h2>{title}</h2><p>{text}</p><a href="mailto:merhaba@systemcel.app?subject=Systemcel%20Blog">{language === "tr" ? "Bu konu hakkında konuş" : "Talk about this topic"}<ArrowRight size={16} /></a></article>)}</div>;
}

function CareersContent({ language }: { language: "tr" | "en" }) {
  return <div className="marketing-contact-card"><BriefcaseBusiness /><h2>{language === "tr" ? "Systemcel'i birlikte büyütelim" : "Let's grow Systemcel together"}</h2><p>{language === "tr" ? "Şu anda yayınlanmış açık pozisyon bulunmuyor. Ürün, mühendislik, tasarım veya müşteri başarısı alanında tanışmak için özgeçmişinizi gönderebilirsiniz." : "There are no published openings right now. Send your resume to meet us about product, engineering, design or customer success."}</p><a className="marketing-button marketing-button--ink" href="mailto:kariyer@systemcel.app?subject=Systemcel%20Kariyer">{language === "tr" ? "Özgeçmiş gönder" : "Send your resume"}<ArrowRight size={17} /></a></div>;
}

function ContactContent({ language }: { language: "tr" | "en" }) {
  return <div className="marketing-content-grid marketing-content-grid--contact"><ContactCard icon={<Mail />} title={language === "tr" ? "Genel iletişim" : "General contact"} text="merhaba@systemcel.app" href="mailto:merhaba@systemcel.app" /><ContactCard icon={<BriefcaseBusiness />} title={language === "tr" ? "Satış ekibi" : "Sales team"} text="satis@systemcel.app" href="mailto:satis@systemcel.app?subject=Systemcel%20Satış%20Görüşmesi" /><ContactCard icon={<Newspaper />} title={language === "tr" ? "Destek" : "Support"} text="destek@systemcel.app" href="mailto:destek@systemcel.app?subject=Systemcel%20Destek" /></div>;
}

function ContactCard({ icon, title, text, href }: { icon: React.ReactNode; title: string; text: string; href: string }) { return <a className="marketing-content-card marketing-contact-link" href={href}>{icon}<h2>{title}</h2><p>{text}</p><span>İletişime geç <ArrowRight size={16} /></span></a>; }

function CookiesContent({ language }: { language: "tr" | "en" }) {
  const tr = language === "tr";
  return <article className="marketing-legal-card">
    <div className="marketing-legal-meta"><strong>{tr ? "Kullanılan çerezler ve tarayıcı verileri" : "Cookies and browser data used"}</strong><span>{tr ? "17 Eylül 2026" : "September 17, 2026"}</span></div>
    <section><h2>{tr ? "Zorunlu oturum ve güvenlik verileri" : "Essential session and security data"}</h2><p>{tr ? "Clerk, oturum açma, kimlik doğrulama ve saldırı önleme için gerekli güvenli çerezleri veya eşdeğer tarayıcı tanımlayıcılarını kullanabilir. Bunlar hizmetin güvenli sunumu için zorunludur." : "Clerk may use secure cookies or equivalent browser identifiers required for sign-in, authentication and abuse prevention. These are essential to provide the service securely."}</p></section>
    <section><h2>{tr ? "Yerel tercihler" : "Local preferences"}</h2><p>{tr ? "systemcel.language dil tercihini; systemcel.accountTypeIntent ise kayıt sırasında seçilen hesap türünü geçici olarak localStorage içinde saklar. Hesap türü niyeti kurulum tamamlandığında silinir; kullanıcı tarayıcı verilerini dilediği zaman temizleyebilir." : "systemcel.language stores the language preference; systemcel.accountTypeIntent temporarily stores the account type selected during registration in localStorage. The account intent is removed after setup, and users can clear browser data at any time."}</p></section>
    <section><h2>{tr ? "Analiz çerezleri" : "Analytics cookies"}</h2><p>{tr ? "Systemcel, yalnızca açık izninizden sonra analiz çerezlerini ve Google Analytics'i etkinleştirir. Reddederseniz analiz araçları yüklenmez. Çerez tercihiniz bu tarayıcıda saklanır." : "Systemcel enables analytics cookies and Google Analytics only after your explicit permission. If you decline, analytics tools are not loaded. Your cookie choice is stored in this browser."}</p></section>
    <aside>{tr
      ? `Veri sorumlusu: ${publicBusinessIdentity.tr.provider}. ${publicBusinessIdentity.tr.tax}. Adres: ${publicBusinessIdentity.tr.address}. İletişim: ${publicBusinessIdentity.tr.contact}.`
      : `Data controller: ${publicBusinessIdentity.en.provider}. ${publicBusinessIdentity.en.tax}. Address: ${publicBusinessIdentity.en.address}. Contact: ${publicBusinessIdentity.en.contact}.`}</aside>
  </article>;
}

function PublicFooter({ language }: { language: "tr" | "en" }) {
  const tr = language === "tr";
  return <footer className="marketing-footer">
    <div className="marketing-wrap marketing-footer__grid">
      <div><a className="marketing-brand marketing-brand--dark" href="/"><BrandMark /><strong>systemcel</strong></a><p>{tr ? "Ön muhasebe, muhasebeciyle ortak çalışma ve tedarikçi pazaryeri." : "Accounting, accountant collaboration and a supplier marketplace."}</p></div>
      <FooterGroup title={tr ? "Ürün" : "Product"} links={[[tr ? "Ön muhasebe" : "Accounting", "/#on-muhasebe"], [tr ? "Yapay zekâ asistanı" : "AI Assistant", "/#ai"], [tr ? "Muhasebeciyle çalışma" : "Accountant collaboration", "/#muhasebeci"], [tr ? "Tedarikçi pazaryeri" : "Supplier marketplace", "/#pazaryeri"], [tr ? "Fiyatlandırma" : "Pricing", "/#fiyat"]]} soonTitle={tr ? "Yakında" : "Coming soon"} soonItems={tr ? ["Banka hareketi eşleştirme", "Çoklu şube ve para birimi", "Entegrasyon API'leri", "Muhasebeci dönem otomasyonu"] : ["Bank transaction matching", "Multiple branches and currencies", "Integration APIs", "Accountant period automation"]} />
      <FooterGroup title={tr ? "Şirket" : "Company"} links={[[tr ? "Hakkımızda" : "About", "/hakkimizda"], [tr ? "Kariyer" : "Careers", "/kariyer"], ["Blog", "/blog"], [tr ? "İletişim" : "Contact", "/iletisim"]]} />
      <FooterGroup title={tr ? "Yasal" : "Legal"} links={[["KVKK", "/kvkk"], [tr ? "Gizlilik" : "Privacy", "/gizlilik"], [tr ? "Kullanım Şartları" : "Terms", "/kullanim-sartlari"], [tr ? "Abonelik Koşulları" : "Subscription Terms", "/abonelik-kosullari"], [tr ? "Çerezler" : "Cookies", "/cerezler"]]} />
    </div>
    <div className="marketing-wrap marketing-footer__bottom"><span>© 2026 SYSTEMCEL — İSTANBUL</span><span>{tr ? "TÜM HAKLARI SAKLIDIR" : "ALL RIGHTS RESERVED"}</span></div>
  </footer>;
}

function BrandMark() { return <span className="marketing-brand-mark" aria-hidden="true"><i /><i /><i /><i /></span>; }
function FooterGroup({ title, links, soonTitle, soonItems = [] }: { title: string; links: Array<[string, string]>; soonTitle?: string; soonItems?: string[] }) {
  return <div className="marketing-footer__group"><strong>{title}</strong>{links.map(([label, href]) => <a key={href} href={href}>{label}</a>)}{soonTitle && soonItems.length > 0 ? <div className="marketing-footer__soon" role="group" aria-label={soonTitle}><strong>{soonTitle}</strong><ul>{soonItems.map((item) => <li key={item}>{item}</li>)}</ul></div> : null}</div>;
}

function pageTitle(kind: PublicPageKind, language: "tr" | "en") {
  const titles = language === "tr" ? { about: "Hakkımızda", blog: "Systemcel Blog", careers: "Kariyer", contact: "İletişim", cookies: "Çerez Politikası" } : { about: "About", blog: "Systemcel Blog", careers: "Careers", contact: "Contact", cookies: "Cookie Policy" };
  return titles[kind as keyof typeof titles] ?? "Systemcel";
}

function pageLead(kind: PublicPageKind, language: "tr" | "en") {
  const leads = language === "tr" ? { about: "İşletmelerin finansını yönetmesini, muhasebecileriyle çalışmasını ve tedarikçilerden alım yapmasını kolaylaştırıyoruz.", blog: "Ön muhasebe, işletme finansı ve dijital iş birliği üzerine ürün notları.", careers: "KOBİ'lerin finansal işlerini sadeleştiren ürünü birlikte geliştirelim.", contact: "Ürün, satış ve destek için doğru ekibe ulaşın.", cookies: "Systemcel'in kullandığı çerezleri ve tarayıcı verilerini öğrenin." } : { about: "We make it easier for businesses to manage finance, work with accountants and buy from suppliers.", blog: "Product notes on accounting, business finance and digital collaboration.", careers: "Help build a product that simplifies finance for small businesses.", contact: "Reach the right team for product, sales or support.", cookies: "Learn which cookies and browser data Systemcel uses." };
  return leads[kind as keyof typeof leads] ?? "";
}
