import React from "react";
import {
  CheckCircle2,
  FileText,
  Globe2,
  Info,
  Moon,
  MoreVertical,
  Search,
  Store,
  Sun
} from "lucide-react";
import { GibPortalSayfasi } from "../gib-portal/GibPortalSayfasi";
import { TelegramBaglantisiSayfasi } from "../telegram/TelegramBaglantisiSayfasi";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import { setAppLanguage } from "../../shared/i18n";
import { useTheme } from "../../theme/ThemeProvider";
import type { AyarKalem, AyarlarEkranVerisi } from "./types";
import { AyarlarOperasyonPanelleri } from "./AyarlarOperasyonPanelleri";
import { BildirimTercihleriPaneli } from "./BildirimTercihleriPaneli";
import { SubeKurPaneli } from "./SubeKurPaneli";
import "./gorunum.css";

interface AyarlarSayfasiProps {
  onIsletmeDegistir: (id: number) => void | Promise<void>;
  onUstBarYenile?: () => unknown | Promise<unknown>;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

const settingsText = {
  tr: {
    loading: "Ayarlar yükleniyor...",
    languageSaveError: "Dil ayarı kaydedilemedi.",
    activeBusinessError: "Aktif işletme değiştirilemedi.",
    businessUpdateError: "İşletme güncellenemedi.",
    businessAddError: "İşletme eklenemedi.",
    businessDeleteConfirm: (name: string) => `${name} silinsin mi? Bu işletmeye ait kayıtlar da silinir.`,
    businessDeleteError: "İşletme silinemedi.",
    categoryAddError: "Kalem eklenemedi.",
    categoryUpdatePrompt: "Kalem adını güncelle",
    categoryUpdateError: "Kalem güncellenemedi.",
    categoryDeleteConfirm: (name: string) => `${name} kalemi silinsin mi?`,
    categoryDeleteError: "Kalem silinemedi.",
    working: "İşlem yapılıyor...",
    businessTitle: "İşletme Ayarları",
    businessSubtitle: "İşletmenize ait temel bilgileri güncelleyin ve yeni işletme ekleyin.",
    appearanceTitle: "Görünüm",
    appearanceSubtitle: "Uygulamanın renk temasını seçin. Tercihiniz bu cihazda kaydedilir.",
    themeChoice: "Tema seçimi",
    lightTheme: "Açık",
    darkTheme: "Koyu",
    selected: "Seçili",
    language: "Uygulama Dili",
    applyLanguage: "Dili Uygula",
    activeBusiness: "Aktif İşletme",
    active: "Aktif",
    passive: "Pasif",
    businessName: "İşletme Adı",
    businessNamePlaceholder: "İşletme adı",
    renameBusiness: "Yeniden Adlandır",
    deleteBusiness: "İşletmeyi Sil",
    newBusiness: "Yeni İşletme",
    newBusinessSubtitle: "Yeni bir işletme ekleyerek kaydetmeye başlayın.",
    newBusinessPlaceholder: "Yeni işletme adı girin",
    addBusiness: "İşletme Ekle",
    activeBusinessInfo: "Bu işletme aktif. Ayarlar ve kayıtlar bu işletmeye göre listelenir.",
    categoriesTitle: "Gelir / Gider Kalemleri",
    categoriesSubtitle: "Gelir ve gider için kullanacağınız kalemleri yönetin.",
    categoryType: "Kalem Türü",
    income: "Gelir",
    expense: "Gider",
    categorySearch: "Kalem ara...",
    updateSelected: "Seçiliyi Güncelle",
    deleteSelected: "Seçiliyi Sil",
    noCategory: "Kalem bulunamadı.",
    newCategoryPlaceholder: "Yeni kalem adı girin",
    addCategory: "Kalem Ekle",
    categoryCount: (tip: string, count: number) => `${tip} için ${count} kalem bulunuyor.`,
    footer: "© 2026 Systemcel Finance Suite. Tüm hakları saklıdır."
  },
  en: {
    loading: "Loading settings...",
    languageSaveError: "Language setting could not be saved.",
    activeBusinessError: "Active business could not be changed.",
    businessUpdateError: "Business could not be updated.",
    businessAddError: "Business could not be added.",
    businessDeleteConfirm: (name: string) => `Delete ${name}? Records for this business will also be deleted.`,
    businessDeleteError: "Business could not be deleted.",
    categoryAddError: "Category could not be added.",
    categoryUpdatePrompt: "Update category name",
    categoryUpdateError: "Category could not be updated.",
    categoryDeleteConfirm: (name: string) => `Delete ${name} category?`,
    categoryDeleteError: "Category could not be deleted.",
    working: "Working...",
    businessTitle: "Business Settings",
    businessSubtitle: "Update core business information and add a new business.",
    appearanceTitle: "Appearance",
    appearanceSubtitle: "Choose the application's color theme. Your preference is saved on this device.",
    themeChoice: "Theme selection",
    lightTheme: "Light",
    darkTheme: "Dark",
    selected: "Selected",
    language: "Application Language",
    applyLanguage: "Apply Language",
    activeBusiness: "Active Business",
    active: "Active",
    passive: "Passive",
    businessName: "Business Name",
    businessNamePlaceholder: "Business name",
    renameBusiness: "Rename",
    deleteBusiness: "Delete Business",
    newBusiness: "New Business",
    newBusinessSubtitle: "Add a new business and start recording.",
    newBusinessPlaceholder: "Enter new business name",
    addBusiness: "Add Business",
    activeBusinessInfo: "This business is active. Settings and records are listed for this business.",
    categoriesTitle: "Income / Expense Categories",
    categoriesSubtitle: "Manage categories used for income and expenses.",
    categoryType: "Category Type",
    income: "Income",
    expense: "Expense",
    categorySearch: "Search category...",
    updateSelected: "Update Selected",
    deleteSelected: "Delete Selected",
    noCategory: "No category found.",
    newCategoryPlaceholder: "Enter new category name",
    addCategory: "Add Category",
    categoryCount: (tip: string, count: number) => `${count} category found for ${tip}.`,
    footer: "© 2026 Systemcel Finance Suite. All rights reserved."
  },
  de: {
    loading: "Einstellungen werden geladen...",
    languageSaveError: "Spracheinstellung konnte nicht gespeichert werden.",
    activeBusinessError: "Aktives Unternehmen konnte nicht geändert werden.",
    businessUpdateError: "Unternehmen konnte nicht aktualisiert werden.",
    businessAddError: "Unternehmen konnte nicht hinzugefügt werden.",
    businessDeleteConfirm: (name: string) => `${name} löschen? Die Datensätze dieses Unternehmens werden ebenfalls gelöscht.`,
    businessDeleteError: "Unternehmen konnte nicht gelöscht werden.",
    categoryAddError: "Kategorie konnte nicht hinzugefügt werden.",
    categoryUpdatePrompt: "Kategorienamen aktualisieren",
    categoryUpdateError: "Kategorie konnte nicht aktualisiert werden.",
    categoryDeleteConfirm: (name: string) => `Kategorie ${name} löschen?`,
    categoryDeleteError: "Kategorie konnte nicht gelöscht werden.",
    working: "Vorgang läuft...",
    businessTitle: "Unternehmenseinstellungen",
    businessSubtitle: "Aktualisieren Sie Basisdaten und fügen Sie ein neues Unternehmen hinzu.",
    appearanceTitle: "Darstellung",
    appearanceSubtitle: "Wählen Sie das Farbschema der Anwendung. Ihre Auswahl wird auf diesem Gerät gespeichert.",
    themeChoice: "Farbschema auswählen",
    lightTheme: "Hell",
    darkTheme: "Dunkel",
    selected: "Ausgewählt",
    language: "Anwendungssprache",
    applyLanguage: "Sprache Anwenden",
    activeBusiness: "Aktives Unternehmen",
    active: "Aktiv",
    passive: "Passiv",
    businessName: "Unternehmensname",
    businessNamePlaceholder: "Unternehmensname",
    renameBusiness: "Umbenennen",
    deleteBusiness: "Unternehmen Löschen",
    newBusiness: "Neues Unternehmen",
    newBusinessSubtitle: "Fügen Sie ein neues Unternehmen hinzu und starten Sie die Erfassung.",
    newBusinessPlaceholder: "Neuen Unternehmensnamen eingeben",
    addBusiness: "Unternehmen Hinzufügen",
    activeBusinessInfo: "Dieses Unternehmen ist aktiv. Einstellungen und Datensätze werden danach gefiltert.",
    categoriesTitle: "Einnahmen- / Ausgabenkategorien",
    categoriesSubtitle: "Verwalten Sie Kategorien für Einnahmen und Ausgaben.",
    categoryType: "Kategorieart",
    income: "Einnahme",
    expense: "Ausgabe",
    categorySearch: "Kategorie suchen...",
    updateSelected: "Auswahl Aktualisieren",
    deleteSelected: "Auswahl Löschen",
    noCategory: "Keine Kategorie gefunden.",
    newCategoryPlaceholder: "Neuen Kategorienamen eingeben",
    addCategory: "Kategorie Hinzufügen",
    categoryCount: (tip: string, count: number) => `${count} Kategorie(n) für ${tip} gefunden.`,
    footer: "© 2026 Systemcel Finance Suite. Alle Rechte vorbehalten."
  }
};

function textForLanguage(language: string) {
  return settingsText[language as keyof typeof settingsText] ?? settingsText.tr;
}

export function GorunumAyari({ language = "tr" }: { language?: string }) {
  const { theme, setTheme } = useTheme();
  const L = textForLanguage(language);
  const titleId = React.useId();

  return (
    <section className="settings-appearance settings-card" aria-labelledby={titleId}>
      <div className="settings-appearance__intro">
        <span className="settings-appearance__icon" aria-hidden="true">
          {theme === "light" ? <Sun size={22} /> : <Moon size={22} />}
        </span>
        <div>
          <h2 id={titleId}>{L.appearanceTitle}</h2>
          <p>{L.appearanceSubtitle}</p>
        </div>
      </div>

      <div className="settings-appearance__choices" role="group" aria-label={L.themeChoice}>
        <button type="button" aria-pressed={theme === "light"} onClick={() => setTheme("light")}>
          <Sun size={19} aria-hidden="true" />
          <span>{L.lightTheme}</span>
          <small aria-hidden="true">{theme === "light" ? L.selected : ""}</small>
        </button>
        <button type="button" aria-pressed={theme === "dark"} onClick={() => setTheme("dark")}>
          <Moon size={19} aria-hidden="true" />
          <span>{L.darkTheme}</span>
          <small aria-hidden="true">{theme === "dark" ? L.selected : ""}</small>
        </button>
      </div>
    </section>
  );
}

type AyarlarSekmesi = "isletme" | "gib" | "telegram";

function ilkSekme(): AyarlarSekmesi {
  const raw = new URLSearchParams(window.location.search).get("sekme")?.toLocaleLowerCase("tr-TR") ?? "";
  if (raw === "gib" || raw === "gib-portal")
    return "gib";
  if (raw === "telegram" || raw === "bot")
    return "telegram";
  return "isletme";
}

export function AyarlarSayfasi({ onIsletmeDegistir, onUstBarYenile, ustBar, yenileAnahtari }: AyarlarSayfasiProps) {
  const urlSekmesi = ilkSekme();
  const [ekran, setEkran] = React.useState<AyarlarEkranVerisi | null>(null);
  const [aktifSekme, setAktifSekme] = React.useState<AyarlarSekmesi>(() => ilkSekme());
  const [seciliIsletmeId, setSeciliIsletmeId] = React.useState(0);
  const [isletmeAdi, setIsletmeAdi] = React.useState("");
  const [yeniIsletmeAdi, setYeniIsletmeAdi] = React.useState("");
  const [dil, setDil] = React.useState("tr");
  const [kalemTipi, setKalemTipi] = React.useState<"Gelir" | "Gider">("Gelir");
  const [kalemArama, setKalemArama] = React.useState("");
  const [seciliKalemId, setSeciliKalemId] = React.useState<number | null>(null);
  const [yeniKalemAdi, setYeniKalemAdi] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const L = textForLanguage(dil);

  React.useEffect(() => {
    setAktifSekme(urlSekmesi);
  }, [urlSekmesi]);

  const uygula = React.useCallback((data: AyarlarEkranVerisi) => {
    setEkran(data);
    setDil(data.dil || "tr");
    setAppLanguage(data.dil || "tr");
    setSeciliIsletmeId(data.seciliIsletmeId || data.aktifIsletmeId);
    const selectedBusiness = data.isletmeler.find((row) => row.id === (data.seciliIsletmeId || data.aktifIsletmeId));
    setIsletmeAdi(selectedBusiness?.ad ?? data.aktifIsletme ?? "");
    setSeciliKalemId(data.seciliKalemId ?? data.kalemler.find((row) => row.tip === kalemTipi)?.id ?? null);
    setMesaj(data.mesaj || "");
  }, [kalemTipi]);

  const yukle = React.useCallback(async () => {
    setHata("");
    setMesaj(L.loading);
    const data = await jsonOku<AyarlarEkranVerisi>("/api/ekran/ayarlar");
    uygula(data);
  }, [L.loading, uygula]);

  React.useEffect(() => {
    yukle().catch((error: Error) => {
      setHata(error.message);
      setMesaj("");
    });
  }, [yukle, yenileAnahtari]);

  React.useEffect(() => {
    if (!ekran)
      return;

    const selectedBusiness = ekran.isletmeler.find((row) => row.id === seciliIsletmeId);
    setIsletmeAdi(selectedBusiness?.ad ?? "");
  }, [ekran, seciliIsletmeId]);

  const isletmeler = ekran?.isletmeler ?? [];
  const seciliIsletme = isletmeler.find((row) => row.id === seciliIsletmeId);
  const kalemler = (ekran?.kalemler ?? []).filter((row) => row.tip === kalemTipi);
  const gorunenKalemler = kalemler.filter((row) =>
    row.ad.toLocaleLowerCase("tr-TR").includes(kalemArama.toLocaleLowerCase("tr-TR").trim())
  );
  const seciliKalem = kalemler.find((row) => row.id === seciliKalemId) ?? gorunenKalemler[0] ?? null;

  React.useEffect(() => {
    if (!gorunenKalemler.length) {
      setSeciliKalemId(null);
      return;
    }

    if (!gorunenKalemler.some((row) => row.id === seciliKalemId))
      setSeciliKalemId(gorunenKalemler[0].id);
  }, [gorunenKalemler, seciliKalemId]);

  const calistir = async (islem: () => Promise<AyarlarEkranVerisi>, fallback: string) => {
    try {
      setIslemde(true);
      setHata("");
      const data = await islem();
      uygula(data);
      if (data.aktifIsletmeId && data.aktifIsletmeId !== ustBar?.aktifIsletmeId)
        await onIsletmeDegistir(data.aktifIsletmeId);
    } catch (error) {
      setHata(error instanceof Error ? error.message : fallback);
    } finally {
      setIslemde(false);
    }
  };

  const diliUygula = () => calistir(
    () => jsonOku<AyarlarEkranVerisi>("/api/ekran/ayarlar/dil", {
      method: "PUT",
      body: JSON.stringify({ dil })
    }),
    L.languageSaveError
  );

  const aktifIsletmeSec = (id: number) => calistir(
    async () => {
      await onIsletmeDegistir(id);
      return jsonOku<AyarlarEkranVerisi>(`/api/ekran/ayarlar/isletmeler/${id}/aktif`, { method: "PUT" });
    },
    L.activeBusinessError
  );

  const isletmeYenidenAdlandir = () => calistir(
    () => jsonOku<AyarlarEkranVerisi>(`/api/ekran/ayarlar/isletmeler/${seciliIsletmeId}`, {
      method: "PUT",
      body: JSON.stringify({ ad: isletmeAdi })
    }),
    L.businessUpdateError
  );

  const isletmeEkle = () => calistir(
    () => jsonOku<AyarlarEkranVerisi>("/api/ekran/ayarlar/isletmeler", {
      method: "POST",
      body: JSON.stringify({ ad: yeniIsletmeAdi })
    }).then((data) => {
      setYeniIsletmeAdi("");
      return data;
    }),
    L.businessAddError
  );

  const isletmeSil = () => {
    if (!seciliIsletme)
      return;

    if (!window.confirm(L.businessDeleteConfirm(seciliIsletme.ad)))
      return;

    void calistir(
      () => jsonOku<AyarlarEkranVerisi>(`/api/ekran/ayarlar/isletmeler/${seciliIsletme.id}`, { method: "DELETE" }),
      L.businessDeleteError
    );
  };

  const kalemEkle = () => calistir(
    () => jsonOku<AyarlarEkranVerisi>("/api/ekran/ayarlar/kalemler", {
      method: "POST",
      body: JSON.stringify({ tip: kalemTipi, ad: yeniKalemAdi })
    }).then((data) => {
      setYeniKalemAdi("");
      setKalemArama("");
      return data;
    }),
    L.categoryAddError
  );

  const kalemGuncelle = () => {
    if (!seciliKalem)
      return;

    const yeniAd = window.prompt(L.categoryUpdatePrompt, seciliKalem.ad)?.trim();
    if (!yeniAd)
      return;

    void calistir(
      () => jsonOku<AyarlarEkranVerisi>(`/api/ekran/ayarlar/kalemler/${seciliKalem.id}`, {
        method: "PUT",
        body: JSON.stringify({ tip: kalemTipi, ad: yeniAd })
      }),
      L.categoryUpdateError
    );
  };

  const kalemSil = () => {
    if (!seciliKalem)
      return;

    if (!window.confirm(L.categoryDeleteConfirm(seciliKalem.ad)))
      return;

    void calistir(
      () => jsonOku<AyarlarEkranVerisi>(`/api/ekran/ayarlar/kalemler/${seciliKalem.id}`, { method: "DELETE" }),
      L.categoryDeleteError
    );
  };

  return (
    <main className={`settings-page settings-page--tabbed ${aktifSekme !== "isletme" ? "settings-page--wide" : ""}`}>
      {aktifSekme === "isletme" ? (
      <>
      <GorunumAyari language={dil} />
      <div className="settings-grid">
        <section className="settings-card settings-card--business">
          <header className="settings-card__header">
            <h2>{L.businessTitle}</h2>
          </header>

          <div className="settings-section-row">
            <span className="settings-row-icon"><Globe2 size={22} /></span>
            <strong>{L.language}</strong>
            <select aria-label={L.language} value={dil} onChange={(event) => setDil(event.target.value)} disabled={islemde}>
              {(ekran?.diller ?? []).map((row) => (
                <option key={row.kod} value={row.kod}>{row.ad}</option>
              ))}
            </select>
            <button className="settings-btn settings-btn--navy" disabled={islemde} type="button" onClick={diliUygula}>
              {L.applyLanguage}
            </button>
          </div>

          <div className="settings-section-row">
            <span className="settings-row-icon"><Store size={22} /></span>
            <strong>{L.activeBusiness}</strong>
            <select
              aria-label={L.activeBusiness}
              value={seciliIsletmeId}
              onChange={(event) => aktifIsletmeSec(Number(event.target.value))}
              disabled={islemde || isletmeler.length === 0}
            >
              {isletmeler.map((row) => (
                <option key={row.id} value={row.id}>{row.ad}</option>
              ))}
            </select>
            <span className={`settings-status-pill ${seciliIsletme?.aktif ? "active" : ""}`}>
              {seciliIsletme?.aktif ? L.active : L.passive}
            </span>
          </div>

          <div className="settings-block">
            <div className="settings-block__title">
              <FileText size={20} />
              <strong>{L.businessName}</strong>
            </div>
            <div className="settings-inline-actions">
              <input
                aria-label={L.businessName}
                value={isletmeAdi}
                onChange={(event) => setIsletmeAdi(event.target.value)}
                placeholder={L.businessNamePlaceholder}
                disabled={islemde || !seciliIsletme}
              />
              <button className="settings-btn settings-btn--navy" disabled={islemde || !seciliIsletme} type="button" onClick={isletmeYenidenAdlandir}>
                {L.renameBusiness}
              </button>
              <button className="settings-btn settings-btn--danger" disabled={islemde || isletmeler.length <= 1} type="button" onClick={isletmeSil}>
                {L.deleteBusiness}
              </button>
            </div>
          </div>

          <div className="settings-block settings-block--new">
            <h3>{L.newBusiness}</h3>
            <div className="settings-inline-actions">
              <input
                aria-label={L.newBusiness}
                value={yeniIsletmeAdi}
                onChange={(event) => setYeniIsletmeAdi(event.target.value)}
                placeholder={L.newBusinessPlaceholder}
                disabled={islemde}
              />
              <button className="settings-btn settings-btn--green" disabled={islemde} type="button" onClick={isletmeEkle}>
                {L.addBusiness}
              </button>
            </div>
          </div>

          <div className="settings-info">
            <Info size={18} />
            <span>{L.activeBusinessInfo}</span>
          </div>
        </section>

        <section className="settings-card settings-card--categories">
          <header className="settings-card__header">
            <h2>{L.categoriesTitle}</h2>
          </header>

          <label className="settings-field settings-field--tip">
            <span>{L.categoryType}</span>
            <select
              value={kalemTipi}
              onChange={(event) => {
                setKalemTipi(event.target.value === "Gider" ? "Gider" : "Gelir");
                setKalemArama("");
              }}
              disabled={islemde}
            >
              <option value="Gelir">{L.income}</option>
              <option value="Gider">{L.expense}</option>
            </select>
          </label>

          <div className="settings-toolbar">
            <label className="settings-search">
              <Search size={20} />
              <input
                aria-label={L.categorySearch}
                value={kalemArama}
                onChange={(event) => setKalemArama(event.target.value)}
                placeholder={L.categorySearch}
                disabled={islemde}
              />
            </label>
            <button className="settings-btn settings-btn--navy" disabled={islemde || !seciliKalem} type="button" onClick={kalemGuncelle}>
              {L.updateSelected}
            </button>
            <button className="settings-btn settings-btn--danger" disabled={islemde || !seciliKalem} type="button" onClick={kalemSil}>
              {L.deleteSelected}
            </button>
          </div>

          <div className="settings-list" role="group" aria-label="Kalemler">
            {gorunenKalemler.length === 0 ? (
              <div className="settings-list__empty">{L.noCategory}</div>
            ) : (
              gorunenKalemler.map((row: AyarKalem) => (
                <button
                  key={row.id}
                  className={`settings-list__row ${row.id === seciliKalemId ? "selected" : ""}`}
                  type="button"
                  aria-pressed={row.id === seciliKalemId}
                  onClick={() => setSeciliKalemId(row.id)}
                >
                  <span>{row.ad}</span>
                  <MoreVertical size={20} />
                </button>
              ))
            )}
          </div>

          <div className="settings-add-category">
            <input
              aria-label={L.newCategoryPlaceholder}
              value={yeniKalemAdi}
              onChange={(event) => setYeniKalemAdi(event.target.value)}
              placeholder={L.newCategoryPlaceholder}
              disabled={islemde}
            />
            <button className="settings-btn settings-btn--green" disabled={islemde} type="button" onClick={kalemEkle}>
              {L.addCategory}
            </button>
          </div>

          <div className="settings-success">
            <CheckCircle2 size={18} />
            <span>{L.categoryCount(kalemTipi === "Gider" ? L.expense : L.income, kalemler.length)}</span>
          </div>
        </section>
      </div>
      {ustBar?.hesapTipi === "Isletme" ? <SubeKurPaneli /> : null}
      <AyarlarOperasyonPanelleri />
      <BildirimTercihleriPaneli />
      </>
      ) : aktifSekme === "gib" ? (
        <section className="settings-tab-panel settings-tab-panel--embedded">
          <GibPortalSayfasi
            ustBar={ustBar}
            ustBarIslemde={false}
            yenileAnahtari={yenileAnahtari}
            onIsletmeDegistir={(id) => { void onIsletmeDegistir(id); }}
          />
        </section>
      ) : (
        <section className="settings-tab-panel">
          <TelegramBaglantisiSayfasi onTelegramDurumuDegisti={onUstBarYenile} />
        </section>
      )}

      {aktifSekme === "isletme" && (hata || mesaj || islemde) && (
        <div className={`settings-feedback ${hata ? "error" : ""}`} role={hata ? "alert" : "status"}>
          {hata || (islemde ? L.working : mesaj)}
        </div>
      )}

      <footer className="settings-footer">{L.footer}</footer>
    </main>
  );
}
