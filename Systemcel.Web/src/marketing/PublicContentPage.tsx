import React from "react";
import { ArrowLeft, ArrowRight, BriefcaseBusiness, Mail, Newspaper } from "lucide-react";
import { legalTexts, type LegalTextKey } from "../auth/legalTexts";
import "./marketing.css";

export type PublicPageKind = "blog" | "careers" | "contact" | "cookies" | LegalTextKey;

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
          {legal ? <LegalContent legal={legal} /> : kind === "blog" ? <BlogContent language={language} /> : kind === "careers" ? <CareersContent language={language} /> : kind === "contact" ? <ContactContent language={language} /> : <CookiesContent language={language} />}
        </div>
      </section>
    </main>
  );
}

function LegalContent({ legal }: { legal: (typeof legalTexts)["tr"][LegalTextKey] }) {
  return <article className="marketing-legal-card"><div className="marketing-legal-meta"><strong>{legal.updatedAtLabel}</strong><span>{legal.updatedAt}</span></div>{legal.sections.map((section) => <section key={section.title}><h2>{section.title}</h2><p>{section.text}</p></section>)}<aside>{legal.note}</aside></article>;
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

function pageTitle(kind: PublicPageKind, language: "tr" | "en") {
  const titles = language === "tr" ? { blog: "Systemcel Blog", careers: "Kariyer", contact: "İletişim", cookies: "Çerez Politikası" } : { blog: "Systemcel Blog", careers: "Careers", contact: "Contact", cookies: "Cookie Policy" };
  return titles[kind as keyof typeof titles] ?? "Systemcel";
}

function pageLead(kind: PublicPageKind, language: "tr" | "en") {
  const leads = language === "tr" ? { blog: "Ön muhasebe, finansal operasyon ve dijital iş birliği üzerine ürün notları.", careers: "KOBİ'lerin finansal işlerini sadeleştiren ürünü birlikte inşa edelim.", contact: "Ürün, satış ve destek konularında doğru ekibe doğrudan ulaşın.", cookies: "Systemcel'in tarayıcı verilerini nasıl kullandığına ilişkin açık bilgiler." } : { blog: "Product notes on accounting, financial operations and digital collaboration.", careers: "Build the product that simplifies financial operations for small businesses.", contact: "Reach the right team directly for product, sales and support.", cookies: "Clear information about how Systemcel uses browser data." };
  return leads[kind as keyof typeof leads] ?? "";
}
