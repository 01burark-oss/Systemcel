import React from "react";

export type AppLanguage = "tr" | "en" | "de";

const storageKey = "systemcel.language";
const languageEvent = "systemcel:languagechange";

const localeByLanguage: Record<AppLanguage, string> = { tr: "tr-TR", en: "en-US", de: "de-DE" };

const copy = {
  tr: {
    "nav.stockLedger": "Stok defteri",
    "nav.home": "Ana sayfa", "nav.finance": "Finans durumu", "nav.incomeExpense": "Gelir / gider", "nav.quickSale": "Hızlı satış", "nav.stock": "Ürün ve stok", "nav.accounts": "Cari hesaplar", "nav.invoices": "Faturalar", "nav.payments": "Tahsilat ve ödeme", "nav.bank": "Banka eşleştirme", "nav.reports": "Raporlar", "nav.chat": "Sohbetler", "nav.accountant": "Muhasebeci paneli", "nav.clients": "Müşterilerim", "nav.accountants": "Muhasebeciler", "nav.admin": "Yönetim", "nav.settings": "Ayarlar", "nav.business": "İşletme", "nav.plan": "Plan ve faturalama", "nav.gib": "GİB portal", "nav.telegram": "Telegram", "scope.notice": "Uygulama ekranlarının bir bölümü henüz Türkçe sunulur.",
    "quickSale.title": "Hızlı satış", "quickSale.search": "Ürün veya barkod ara", "quickSale.complete": "Satışı tamamla", "quickSale.emptyCart": "Sepetiniz boş", "quickSale.loading": "Ürünler yükleniyor…", "quickSale.receipt": "Fişi okut", "quickSale.error": "İşlem tamamlanamadı.",
    "support.title": "Bir destek talebi oluştur", "support.subject": "Konu", "support.category": "Kategori", "support.description": "Açıklama", "support.submit": "Talep oluştur", "support.saving": "Kaydediliyor", "support.requests": "Taleplerim", "support.empty": "Henüz destek talebiniz yok.", "support.status": "Durum", "support.statusField": "durum", "support.reply": "Yanıt", "support.replyField": "yanıt", "support.error": "Destek talebi kaydedilemedi.",
    "billing.choose": "Planınızı seçin ve koşulları onaylayın", "billing.change": "Planı değiştir", "billing.select": "Plan seç", "billing.loading": "Abonelik bilgileri yükleniyor…", "billing.period": "Plan dönemi", "billing.rights": "Plan hakları", "billing.error": "Plan bilgileri yüklenemedi.",
    "admin.support": "Destek talepleri", "admin.transfers": "Muhasebeci aktarımları", "admin.save": "Kaydet", "admin.refresh": "Yenile", "admin.loading": "Yükleniyor…", "admin.empty": "Kayıt bulunmuyor.", "admin.status.open": "Açık", "admin.status.processing": "İşlemde", "admin.status.resolved": "Çözüldü", "admin.priority": "Öncelik"
  },
  en: {
    "nav.stockLedger": "Stock ledger",
    "nav.home": "Home", "nav.finance": "Financial overview", "nav.incomeExpense": "Income & expenses", "nav.quickSale": "Quick sale", "nav.stock": "Products & inventory", "nav.accounts": "Accounts", "nav.invoices": "Invoices", "nav.payments": "Collections & payments", "nav.bank": "Bank reconciliation", "nav.reports": "Reports", "nav.chat": "Chats", "nav.accountant": "Accountant workspace", "nav.clients": "My clients", "nav.accountants": "Accountants", "nav.admin": "Administration", "nav.settings": "Settings", "nav.business": "Business", "nav.plan": "Plan & billing", "nav.gib": "GİB portal", "nav.telegram": "Telegram", "scope.notice": "Some application screens are currently available in Turkish.",
    "quickSale.title": "Quick sale", "quickSale.search": "Search products or barcode", "quickSale.complete": "Complete sale", "quickSale.emptyCart": "Your cart is empty", "quickSale.loading": "Loading products…", "quickSale.receipt": "Scan receipt", "quickSale.error": "The transaction could not be completed.",
    "support.title": "Create a support request", "support.subject": "Subject", "support.category": "Category", "support.description": "Description", "support.submit": "Create request", "support.saving": "Saving", "support.requests": "My requests", "support.empty": "You do not have any support requests yet.", "support.status": "Status", "support.statusField": "status", "support.reply": "Reply", "support.replyField": "reply", "support.error": "The support request could not be saved.",
    "billing.choose": "Choose your plan and confirm the terms", "billing.change": "Change plan", "billing.select": "Choose plan", "billing.loading": "Loading subscription details…", "billing.period": "Plan period", "billing.rights": "Plan benefits", "billing.error": "Plan details could not be loaded.",
    "admin.support": "Support requests", "admin.transfers": "Accountant transfers", "admin.save": "Save", "admin.refresh": "Refresh", "admin.loading": "Loading…", "admin.empty": "No records found.", "admin.status.open": "Open", "admin.status.processing": "In progress", "admin.status.resolved": "Resolved", "admin.priority": "Priority"
  },
  de: {
    "nav.stockLedger": "Bestandsbuch",
    "nav.home": "Startseite", "nav.finance": "Finanzübersicht", "nav.incomeExpense": "Einnahmen & Ausgaben", "nav.quickSale": "Schnellverkauf", "nav.stock": "Produkte & Bestand", "nav.accounts": "Konten", "nav.invoices": "Rechnungen", "nav.payments": "Einzüge & Zahlungen", "nav.bank": "Bankabgleich", "nav.reports": "Berichte", "nav.chat": "Chats", "nav.accountant": "Steuerberaterbereich", "nav.clients": "Meine Kunden", "nav.accountants": "Steuerberater", "nav.admin": "Verwaltung", "nav.settings": "Einstellungen", "nav.business": "Unternehmen", "nav.plan": "Plan & Abrechnung", "nav.gib": "GİB-Portal", "nav.telegram": "Telegram", "scope.notice": "Einige Anwendungsseiten sind derzeit auf Türkisch verfügbar.",
    "quickSale.title": "Schnellverkauf", "quickSale.search": "Produkte oder Barcode suchen", "quickSale.complete": "Verkauf abschließen", "quickSale.emptyCart": "Ihr Warenkorb ist leer", "quickSale.loading": "Produkte werden geladen…", "quickSale.receipt": "Beleg scannen", "quickSale.error": "Der Vorgang konnte nicht abgeschlossen werden.",
    "support.title": "Supportanfrage erstellen", "support.subject": "Betreff", "support.category": "Kategorie", "support.description": "Beschreibung", "support.submit": "Anfrage erstellen", "support.saving": "Wird gespeichert", "support.requests": "Meine Anfragen", "support.empty": "Sie haben noch keine Supportanfragen.", "support.status": "Status", "support.statusField": "Status", "support.reply": "Antwort", "support.replyField": "Antwort", "support.error": "Die Supportanfrage konnte nicht gespeichert werden.",
    "billing.choose": "Plan auswählen und Bedingungen bestätigen", "billing.change": "Plan ändern", "billing.select": "Plan auswählen", "billing.loading": "Abonnementdetails werden geladen…", "billing.period": "Planzeitraum", "billing.rights": "Planleistungen", "billing.error": "Plandetails konnten nicht geladen werden.",
    "admin.support": "Supportanfragen", "admin.transfers": "Steuerberater-Überweisungen", "admin.save": "Speichern", "admin.refresh": "Aktualisieren", "admin.loading": "Wird geladen…", "admin.empty": "Keine Einträge gefunden.", "admin.status.open": "Offen", "admin.status.processing": "In Bearbeitung", "admin.status.resolved": "Gelöst", "admin.priority": "Priorität"
  }
} as const;

export type TranslationKey = keyof typeof copy.tr;

export function readAppLanguage(value: string | null | undefined): AppLanguage {
  return value === "en" || value === "de" ? value : "tr";
}

export function getAppLanguage(): AppLanguage {
  return readAppLanguage(window.localStorage.getItem(storageKey));
}

export function setAppLanguage(language: string) {
  const normalized = readAppLanguage(language);
  window.localStorage.setItem(storageKey, normalized);
  window.dispatchEvent(new CustomEvent<AppLanguage>(languageEvent, { detail: normalized }));
}

export function formatDate(value: Date | string | number, language = getAppLanguage(), options?: Intl.DateTimeFormatOptions) {
  return new Intl.DateTimeFormat(localeByLanguage[language], options).format(new Date(value));
}

export function formatMoney(value: number, currency = "TRY", language = getAppLanguage()) {
  return new Intl.NumberFormat(localeByLanguage[language], { style: "currency", currency: currency || "TRY" }).format(value);
}

interface I18nValue {
  language: AppLanguage;
  locale: string;
  t: (key: TranslationKey, fallback?: string) => string;
  setLanguage: (language: string) => void;
}

const defaultValue: I18nValue = { language: "tr", locale: localeByLanguage.tr, t: (key, fallback) => copy.tr[key] ?? fallback ?? key, setLanguage: setAppLanguage };
const I18nContext = React.createContext<I18nValue>(defaultValue);

export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = React.useState<AppLanguage>(getAppLanguage);

  React.useEffect(() => {
    const update = (event: Event) => setLanguageState(readAppLanguage((event as CustomEvent<string>).detail));
    window.addEventListener(languageEvent, update);
    return () => window.removeEventListener(languageEvent, update);
  }, []);

  React.useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  const value = React.useMemo<I18nValue>(() => ({
    language,
    locale: localeByLanguage[language],
    t: (key, fallback) => copy[language][key] ?? copy.tr[key] ?? fallback ?? key,
    setLanguage: setAppLanguage
  }), [language]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  return React.useContext(I18nContext);
}
