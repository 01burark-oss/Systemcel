import React from "react";
import { ArrowLeft, ArrowRight, BriefcaseBusiness, Mail, Newspaper } from "lucide-react";
import { legalTexts, type LegalTextKey } from "../auth/legalTexts";
import "./marketing.css";

export type PublicPageKind = "about" | "blog" | "careers" | "contact" | "cookies" | LegalTextKey;

export function PublicContentPage({ kind }: { kind: PublicPageKind }) {
  const language = window.localStorage.getItem("systemcel.language") === "en" ? "en" : "tr";
  const legalKey = kind === "terms" || kind === "privacy" || kind === "kvkk" ? kind : null;
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
    ["İşletme ve muhasebeci odağı", "KOBİ'ler ile muhasebecilerin aynı finansal bağlamda güvenle çalışmasını sağlıyoruz."],
    ["Sade finans dili", "Gelir, gider, fatura, cari, stok ve rapor süreçlerini anlaşılır iş adımlarına dönüştürüyoruz."],
    ["Eyleme dönük yapay zekâ", "Systemcel AI yalnızca veriyi göstermeyi değil, doğru soruyu ve sonraki adımı bulmayı hedefler."]
  ] : [
    ["Built for businesses and accountants", "We help small businesses and accountants work securely in the same financial context."],
    ["A clearer finance language", "We turn income, expenses, invoices, accounts, inventory and reports into understandable workflows."],
    ["Actionable AI", "Systemcel AI aims to help find the right question and next action, not merely display data."]
  ];

  return <>
    <article className="marketing-contact-card">
      <BriefcaseBusiness />
      <h2>{tr ? "Finans operasyonlarını daha anlaşılır hale getiriyoruz." : "We make financial operations easier to understand."}</h2>
      <p>{tr ? "Systemcel; işletmelerin günlük finans akışını yönetmesi ve muhasebecilerin müşterileriyle düzenli biçimde çalışması için geliştirilen yerli bir finans çalışma alanıdır." : "Systemcel is a Turkish financial workspace for businesses running daily finance operations and accountants working neatly with their clients."}</p>
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
    ["e-Arşiv fatura akışını düzenlemek", "Taslak oluşturmadan SMS onayına kadar fatura sürecindeki temel kontrol noktaları."],
    ["Muhasebeciyle dijital çalışma alanı", "Talep, sohbet ve finansal veri paylaşımını e-posta zincirlerinden çıkarmanın pratik faydaları."],
  ] : [
    ["Why one source of truth matters in accounting", "How keeping income, expenses, accounts, inventory and invoices in one business context simplifies daily decisions."],
    ["Organizing the e-Archive invoice flow", "The core checkpoints from drafting an invoice to SMS approval."],
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
  return <article className="marketing-legal-card"><section><h2>{language === "tr" ? "Zorunlu çerezler" : "Essential cookies"}</h2><p>{language === "tr" ? "Systemcel, oturumun ve güvenlik kontrollerinin çalışması için zorunlu teknik verileri kullanabilir. Bunlar hizmetin sunulması için gereklidir." : "Systemcel may use essential technical data for sessions and security controls. These are required to provide the service."}</p></section><section><h2>{language === "tr" ? "Tercihler" : "Preferences"}</h2><p>{language === "tr" ? "Dil ve tema gibi cihaz tercihleri tarayıcınızda saklanabilir. Pazarlama veya reklam çerezi şu anda kullanılmamaktadır." : "Device preferences such as language and theme may be stored in your browser. Marketing or advertising cookies are not currently used."}</p></section></article>;
}

function PublicFooter({ language }: { language: "tr" | "en" }) {
  const tr = language === "tr";
  return <footer className="marketing-footer">
    <div className="marketing-wrap marketing-footer__grid">
      <div><a className="marketing-brand marketing-brand--dark" href="/"><BrandMark /><strong>systemcel</strong></a><p>{tr ? "Ön muhasebe, yapay zekâ ve muhasebeci pazaryeri — işletmenin finansal çalışma alanı." : "Accounting, AI and an accountant marketplace — your financial workspace."}</p></div>
      <FooterGroup title={tr ? "Ürün" : "Product"} links={[[tr ? "Ön Muhasebe" : "Accounting", "/#on-muhasebe"], [tr ? "AI Asistan" : "AI Assistant", "/#ai"], [tr ? "Pazaryeri" : "Marketplace", "/#pazaryeri"], [tr ? "Fiyatlandırma" : "Pricing", "/#fiyat"]]} />
      <FooterGroup title={tr ? "Şirket" : "Company"} links={[[tr ? "Hakkımızda" : "About", "/hakkimizda"], [tr ? "Kariyer" : "Careers", "/kariyer"], ["Blog", "/blog"], [tr ? "İletişim" : "Contact", "/iletisim"]]} />
      <FooterGroup title={tr ? "Yasal" : "Legal"} links={[["KVKK", "/kvkk"], [tr ? "Gizlilik" : "Privacy", "/gizlilik"], [tr ? "Kullanım Şartları" : "Terms", "/kullanim-sartlari"], [tr ? "Çerezler" : "Cookies", "/cerezler"]]} />
    </div>
    <div className="marketing-wrap marketing-footer__bottom"><span>© 2026 SYSTEMCEL — İSTANBUL</span><span>{tr ? "TÜM HAKLARI SAKLIDIR" : "ALL RIGHTS RESERVED"}</span></div>
  </footer>;
}

function BrandMark() { return <span className="marketing-brand-mark" aria-hidden="true"><i /><i /><i /><i /></span>; }
function FooterGroup({ title, links }: { title: string; links: Array<[string, string]> }) { return <div className="marketing-footer__group"><strong>{title}</strong>{links.map(([label, href]) => <a key={href} href={href}>{label}</a>)}</div>; }

function pageTitle(kind: PublicPageKind, language: "tr" | "en") {
  const titles = language === "tr" ? { about: "Hakkımızda", blog: "Systemcel Blog", careers: "Kariyer", contact: "İletişim", cookies: "Çerez Politikası" } : { about: "About", blog: "Systemcel Blog", careers: "Careers", contact: "Contact", cookies: "Cookie Policy" };
  return titles[kind as keyof typeof titles] ?? "Systemcel";
}

function pageLead(kind: PublicPageKind, language: "tr" | "en") {
  const leads = language === "tr" ? { about: "İşletmelerin ve muhasebecilerin finansal süreçlerini daha net, hızlı ve birlikte yönetilebilir hale getiriyoruz.", blog: "Ön muhasebe, finansal operasyon ve dijital iş birliği üzerine ürün notları.", careers: "KOBİ'lerin finansal işlerini sadeleştiren ürünü birlikte inşa edelim.", contact: "Ürün, satış ve destek konularında doğru ekibe doğrudan ulaşın.", cookies: "Systemcel'in tarayıcı verilerini nasıl kullandığına ilişkin açık bilgiler." } : { about: "We make financial workflows for businesses and accountants clearer, faster and easier to manage together.", blog: "Product notes on accounting, financial operations and digital collaboration.", careers: "Build the product that simplifies financial operations for small businesses.", contact: "Reach the right team directly for product, sales and support.", cookies: "Clear information about how Systemcel uses browser data." };
  return leads[kind as keyof typeof leads] ?? "";
}
