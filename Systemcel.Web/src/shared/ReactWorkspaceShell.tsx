import React from "react";
import {
  AlertTriangle,
  ArrowDownUp,
  BarChart3,
  Bell,
  BriefcaseBusiness,
  Building2,
  CalendarClock,
  ChartNoAxesCombined,
  CreditCard,
  FileText,
  Globe2,
  Home,
  Landmark,
  Loader2,
  LogOut,
  Menu,
  MessageCircle,
  Package,
  ScanBarcode,
  Send,
  Settings,
  Search,
  ShieldCheck,
  ShoppingCart,
  UsersRound,
  Wallet,
  WalletCards,
  X,
  type LucideIcon
} from "lucide-react";
import { AuthUserButton } from "../auth/AuthUserButton";
import { AiAssistantPanel } from "./AiAssistantPanel";
import { AktifSubeSecici } from "./AktifSubeSecici";
import type { UstBarDurumu } from "./chrome";
import { jsonOku, type EntitlementProblemDetail } from "./json";
import { useI18n, type TranslationKey } from "./i18n";

interface ReactWorkspaceShellProps {
  children: React.ReactNode;
  hata?: string;
  islemde?: boolean;
  ustBar: UstBarDurumu | null;
  baslik?: React.ReactNode;
  sagAksiyon?: React.ReactNode;
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

interface WorkspacePageMeta {
  icon: LucideIcon;
  title: string;
}

interface Bildirim {
  id: number;
  kaynakAnahtari: string;
  tur: string;
  onem: string;
  baslik: string;
  mesaj: string;
  aksiyon: string;
  url?: string;
  okundu: boolean;
  createdAt: string;
}

const anaMenu: Array<{ href: string; label: string; icon: LucideIcon; adminOnly?: boolean; bankaMutabakati?: boolean }> = [
  { href: "/", label: "Ana sayfa", icon: Home },
  { href: "/finansal-gorunum", label: "Finans durumu", icon: ChartNoAxesCombined },
  { href: "/gelir-gider", label: "Gelir / gider", icon: ArrowDownUp },
  { href: "/hizli-satis", label: "Hızlı satış", icon: ShoppingCart },
  { href: "/urun-stok", label: "Ürün ve stok", icon: Package },
  { href: "/stok-defteri", label: "Stok defteri", icon: Package },
  { href: "/cari-hesaplar", label: "Cari hesaplar", icon: CreditCard },
  { href: "/faturalar", label: "Faturalar", icon: FileText },
  { href: "/tahsilat-odeme", label: "Tahsilat ve ödeme", icon: WalletCards },
  { href: "/banka-eslestirme", label: "Banka eşleştirme", icon: Landmark, bankaMutabakati: true },
  { href: "/raporlar", label: "Raporlar", icon: BarChart3 },
  { href: "/sohbetler", label: "Sohbetler", icon: MessageCircle },
  { href: "/muhasebeci", label: "Muhasebeci paneli", icon: BriefcaseBusiness },
  { href: "/muhasebeci/musteriler", label: "Müşterilerim", icon: UsersRound },
  { href: "/muhasebeciler", label: "Muhasebeciler", icon: Search },
  { href: "/yonetim/muhasebeci-basvurulari", label: "Yönetim", icon: ShieldCheck, adminOnly: true },
  { href: "/ayarlar", label: "Ayarlar", icon: Settings }
];

function menuForWorkspace(ustBar: UstBarDurumu | null, musteriBaglami: boolean) {
  const visibleMenu = anaMenu.filter((item) =>
    (!item.adminOnly || ustBar?.yoneticiMi) && (!item.bankaMutabakati || ustBar?.bankaMutabakatiAktif));
  const muhasebeciCalismaAlani = ustBar?.hesapTipi === "Muhasebeci" && !musteriBaglami;
  if (muhasebeciCalismaAlani) {
    return visibleMenu.filter((item) => item.href === "/muhasebeci" || item.href === "/muhasebeci/musteriler" || item.href === "/muhasebeciler" || item.href === "/sohbetler" || item.href === "/ayarlar" || item.adminOnly);
  }

  if (musteriBaglami) {
    return visibleMenu.filter((item) => !item.href.startsWith("/muhasebeci"));
  }

  return visibleMenu.filter((item) => item.href !== "/muhasebeci" && item.href !== "/muhasebeci/musteriler");
}

const ayarlarAltMenu = [
  { href: "/ayarlar?sekme=isletme", label: "İşletme", icon: Building2, sekme: "isletme" },
  { href: "/abonelik", label: "Plan ve faturalama", icon: CalendarClock, sekme: "plan" },
  { href: "/ayarlar?sekme=gib", label: "GİB portal", icon: Landmark, sekme: "gib" },
  { href: "/ayarlar?sekme=telegram", label: "Telegram", icon: Send, sekme: "telegram" }
];

function normalizePath(pathname: string) {
  const normalized = pathname.replace(/\/+$/, "");
  return normalized.length === 0 ? "/" : normalized;
}

function menuAktifMi(currentPath: string, href: string) {
  if (href === "/") {
    return currentPath === "/";
  }

  if (href === "/ayarlar" && currentPath === "/abonelik") {
    return true;
  }

  if (href === "/muhasebeci") {
    return currentPath === href;
  }

  return currentPath === href || currentPath.startsWith(`${href}/`);
}

function tarihBic(now: Date, locale: string) {
  return now.toLocaleDateString(locale, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}

function saatBic(now: Date, locale: string) {
  return now.toLocaleTimeString(locale, {
    hour: "2-digit",
    minute: "2-digit"
  });
}

function bildirimIkonu(tur: string) {
  switch (tur) {
    case "odeme":
      return <Wallet size={18} />;
    case "tahsilat":
      return <CalendarClock size={18} />;
    case "risk":
      return <AlertTriangle size={18} />;
    case "sohbet":
      return <MessageCircle size={18} />;
    default:
      return <Bell size={18} />;
  }
}

function yetkiEtiketi(value?: string) {
  return value === "TamIslem" ? "Tam işlem" : "Okuma + rapor";
}

function navKey(href: string): TranslationKey {
  const keys: Record<string, TranslationKey> = {
    "/": "nav.home", "/finansal-gorunum": "nav.finance", "/gelir-gider": "nav.incomeExpense", "/hizli-satis": "nav.quickSale", "/urun-stok": "nav.stock", "/stok-defteri": "nav.stockLedger", "/cari-hesaplar": "nav.accounts", "/faturalar": "nav.invoices", "/tahsilat-odeme": "nav.payments", "/banka-eslestirme": "nav.bank", "/raporlar": "nav.reports", "/sohbetler": "nav.chat", "/muhasebeci": "nav.accountant", "/muhasebeci/musteriler": "nav.clients", "/muhasebeciler": "nav.accountants", "/yonetim/muhasebeci-basvurulari": "nav.admin", "/ayarlar": "nav.settings"
  };
  return keys[href] ?? "nav.home";
}

function workspacePageMeta(path: string, settingsTab: string): WorkspacePageMeta {
  if (path === "/finansal-gorunum") {
    return {
      icon: ChartNoAxesCombined,
      title: "Finans durumu"
    };
  }

  if (path === "/gelir-gider") {
    return {
      icon: ArrowDownUp,
      title: "Gelir / gider"
    };
  }

  if (path === "/cari-hesaplar") {
    return {
      icon: CreditCard,
      title: "Cari hesaplar"
    };
  }

  if (path === "/urun-stok") {
    return {
      icon: ShoppingCart,
      title: "Ürün ve stok"
    };
  }

  if (path === "/stok-defteri") {
    return {
      icon: Package,
      title: "Stok defteri"
    };
  }

  if (path === "/hizli-satis") {
    return {
      icon: ScanBarcode,
      title: "Hızlı satış"
    };
  }

  if (path === "/faturalar") {
    return {
      icon: FileText,
      title: "Faturalar"
    };
  }

  if (path === "/tahsilat-odeme") {
    return {
      icon: WalletCards,
      title: "Tahsilat ve ödeme"
    };
  }

  if (path === "/banka-eslestirme") {
    return {
      icon: Landmark,
      title: "Banka eşleştirme"
    };
  }

  if (path === "/raporlar") {
    return {
      icon: BarChart3,
      title: "Raporlar"
    };
  }

  if (path === "/sohbetler") {
    return {
      icon: MessageCircle,
      title: "Sohbetler"
    };
  }

  if (path === "/muhasebeciler") {
    return {
      icon: Search,
      title: "Muhasebeciler"
    };
  }

  if (path === "/muhasebeci") {
    return {
      icon: BriefcaseBusiness,
      title: "Muhasebeci paneli"
    };
  }

  if (path === "/muhasebeci/musteriler") {
    return {
      icon: UsersRound,
      title: "Müşterilerim"
    };
  }

  if (path === "/abonelik") {
    return {
      icon: CalendarClock,
      title: "Plan ve ödeme"
    };
  }

  if (path.startsWith("/yonetim")) {
    return {
      icon: ShieldCheck,
      title: "Muhasebeci başvuruları"
    };
  }

  if (path === "/gib-portal") {
    return {
      icon: Globe2,
      title: "GİB bağlantısı"
    };
  }

  if (path === "/ayarlar") {
    if (settingsTab === "telegram" || settingsTab === "bot") {
      return {
        icon: Send,
        title: "Telegram"
      };
    }

    if (settingsTab === "gib" || settingsTab === "gib-portal") {
      return {
        icon: Landmark,
        title: "GİB bağlantısı"
      };
    }

    return {
      icon: Settings,
      title: "Ayarlar"
    };
  }

  return {
    icon: Home,
    title: "Ana sayfa"
  };
}

export function ReactWorkspaceShell({ children, ustBar, baslik, sagAksiyon, onUstBarYenile }: ReactWorkspaceShellProps) {
  const { locale, t } = useI18n();
  const [now, setNow] = React.useState(() => new Date());
  const [bildirimPaneliAcik, setBildirimPaneliAcik] = React.useState(false);
  const [bildirimler, setBildirimler] = React.useState<Bildirim[]>([]);
  const [bildirimYukleniyor, setBildirimYukleniyor] = React.useState(false);
  const [bildirimHata, setBildirimHata] = React.useState("");
  const [sohbetPaneliAcik, setSohbetPaneliAcik] = React.useState(false);
  const [baglamKapatiliyor, setBaglamKapatiliyor] = React.useState(false);
  const [mobilMenuAcik, setMobilMenuAcik] = React.useState(false);
  const [planUyarisi, setPlanUyarisi] = React.useState<EntitlementProblemDetail | null>(null);
  const sohbetPanelRef = React.useRef<HTMLDivElement | null>(null);
  const bildirimPanelRef = React.useRef<HTMLDivElement | null>(null);
  const rawPath = normalizePath(window.location.pathname);
  const currentPath = rawPath === "/app" ? "/" : rawPath.startsWith("/app/") ? rawPath.slice(4) : rawPath;
  const aktifAyarlarSekmesi = currentPath === "/abonelik"
    ? "plan"
    : new URLSearchParams(window.location.search).get("sekme")?.toLocaleLowerCase("tr-TR") || "isletme";
  const musteriBaglami = ustBar?.muhasebeciMusteriBaglami ?? false;
  const menuItems = menuForWorkspace(ustBar, musteriBaglami);
  const brandHref = ustBar?.hesapTipi === "Muhasebeci" && !musteriBaglami ? "/app/muhasebeci" : "/app";
  const menuCurrentPath = ustBar?.hesapTipi === "Muhasebeci" && !musteriBaglami && currentPath === "/" ? "/muhasebeci" : currentPath;
  const pageMeta = workspacePageMeta(menuCurrentPath, aktifAyarlarSekmesi);
  const PageIcon = pageMeta.icon;
  const sohbetler = ustBar?.sohbet?.sohbetler ?? [];
  const sohbetSayisi = ustBar?.sohbet?.okunmamisMesajSayisi ?? 0;

  React.useEffect(() => {
    const handle = window.setInterval(() => setNow(new Date()), 30_000);
    return () => window.clearInterval(handle);
  }, []);

  React.useEffect(() => {
    const planUyarisiGoster = (event: Event) => {
      setPlanUyarisi((event as CustomEvent<EntitlementProblemDetail>).detail);
    };
    const escapeIleKapat = (event: KeyboardEvent) => {
      if (event.key === "Escape") setPlanUyarisi(null);
    };
    window.addEventListener("systemcel:entitlement", planUyarisiGoster);
    window.addEventListener("keydown", escapeIleKapat);
    return () => {
      window.removeEventListener("systemcel:entitlement", planUyarisiGoster);
      window.removeEventListener("keydown", escapeIleKapat);
    };
  }, []);

  React.useEffect(() => {
    const menuyuKapat = () => {
      if (window.innerWidth > 980) setMobilMenuAcik(false);
    };
    window.addEventListener("resize", menuyuKapat);
    return () => window.removeEventListener("resize", menuyuKapat);
  }, []);

  const bildirimleriYukle = React.useCallback(async () => {
    setBildirimYukleniyor(true);
    setBildirimHata("");
    try {
      const data = await jsonOku<Bildirim[]>("/api/ekran/bildirimler");
      setBildirimler(data);
    } catch (error) {
      setBildirimHata(error instanceof Error ? error.message : "Bildirimler yüklenemedi.");
    } finally {
      setBildirimYukleniyor(false);
    }
  }, []);

  const bildirimiOkunduYap = React.useCallback(async (id: number) => {
    await jsonOku<{ okunmamisSayisi: number }>(`/api/ekran/bildirimler/${id}/okundu`, { method: "PUT" });
    setBildirimler((current) => current.map((item) => item.id === id ? { ...item, okundu: true } : item));
    await onUstBarYenile?.();
  }, [onUstBarYenile]);

  const tumunuOkunduYap = React.useCallback(async () => {
    await jsonOku<{ okunmamisSayisi: number }>("/api/ekran/bildirimler/tumunu-okundu", { method: "POST" });
    setBildirimler((current) => current.map((item) => ({ ...item, okundu: true })));
    await onUstBarYenile?.();
  }, [onUstBarYenile]);

  React.useEffect(() => {
    if (bildirimPaneliAcik) {
      bildirimleriYukle();
    }
  }, [bildirimPaneliAcik, bildirimleriYukle]);

  React.useEffect(() => {
    if (!sohbetPaneliAcik && !bildirimPaneliAcik) {
      return;
    }

    const disTiklamayiYakala = (event: PointerEvent) => {
      const target = event.target as Node | null;
      if (!target) return;

      if (sohbetPaneliAcik && !sohbetPanelRef.current?.contains(target)) {
        setSohbetPaneliAcik(false);
      }

      if (bildirimPaneliAcik && !bildirimPanelRef.current?.contains(target)) {
        setBildirimPaneliAcik(false);
      }
    };

    const escapeIleKapat = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      setSohbetPaneliAcik(false);
      setBildirimPaneliAcik(false);
    };

    document.addEventListener("pointerdown", disTiklamayiYakala);
    document.addEventListener("keydown", escapeIleKapat);
    return () => {
      document.removeEventListener("pointerdown", disTiklamayiYakala);
      document.removeEventListener("keydown", escapeIleKapat);
    };
  }, [bildirimPaneliAcik, sohbetPaneliAcik]);

  const musteriBaglaminiKapat = React.useCallback(async () => {
    try {
      setBaglamKapatiliyor(true);
      await jsonOku<{ mesaj: string }>("/api/ekran/muhasebeci/musteri-baglami/kapat", { method: "POST" });
      window.location.href = "/app/muhasebeci";
    } finally {
      setBaglamKapatiliyor(false);
    }
  }, []);

  return (
    <div className="react-shell react-shell--page-title">
      <aside className={`react-sidebar ${mobilMenuAcik ? "react-sidebar--open" : ""}`} aria-label="Systemcel menüsü">
        <a className="react-sidebar__brand" href={brandHref} aria-label="Systemcel ana sayfa">
          <span className="react-sidebar__brand-mark" aria-hidden="true">
            <i />
            <i />
            <i />
            <i />
          </span>
          <span className="react-sidebar__brand-text">
            <strong>systemcel</strong>
            <small>Finance Suite</small>
          </span>
        </a>

        <button
          className="react-sidebar__mobile-toggle"
          type="button"
          aria-label={mobilMenuAcik ? "Menüyü kapat" : "Menüyü aç"}
          aria-expanded={mobilMenuAcik}
          onClick={() => setMobilMenuAcik((current) => !current)}
        >
          {mobilMenuAcik ? <X size={22} /> : <Menu size={22} />}
        </button>

        <nav className="react-sidebar__nav" aria-label="Ana menü">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const active = menuAktifMi(menuCurrentPath, item.href);
            return (
              <React.Fragment key={item.href}>
                <a
                  className={`react-sidebar__link ${active ? "active" : ""}`}
                  href={item.href === "/" ? "/app" : `/app${item.href}`}
                  aria-current={active ? "page" : undefined}
                  onClick={() => setMobilMenuAcik(false)}
                >
                  <Icon size={19} />
                  <span>{t(navKey(item.href), item.label)}</span>
                </a>
                {item.href === "/ayarlar" && active ? (
                  <nav className="react-sidebar__subnav" aria-label="Ayarlar alt menüsü">
                    {ayarlarAltMenu.map((subItem) => {
                      const SubIcon = subItem.icon;
                      const subActive =
                        aktifAyarlarSekmesi === subItem.sekme ||
                        (subItem.sekme === "gib" && aktifAyarlarSekmesi === "gib-portal") ||
                        (subItem.sekme === "telegram" && aktifAyarlarSekmesi === "bot");
                      return (
                        <a
                          key={subItem.href}
                          className={`react-sidebar__sublink ${subActive ? "active" : ""}`}
                          href={`/app${subItem.href}`}
                          aria-current={subActive ? "page" : undefined}
                          onClick={() => setMobilMenuAcik(false)}
                        >
                          <SubIcon size={16} />
                          <span>{t(subItem.sekme === "isletme" ? "nav.business" : subItem.sekme === "plan" ? "nav.plan" : subItem.sekme === "gib" ? "nav.gib" : "nav.telegram", subItem.label)}</span>
                        </a>
                      );
                    })}
                  </nav>
                ) : null}
              </React.Fragment>
            );
          })}
        </nav>

        <div className="react-sidebar__footer">
          <AuthUserButton />
        </div>
      </aside>

      <div className="react-shell__main">
        <header className="react-topbar">
          <div className="react-topbar__title-slot">
            {baslik ?? (
              <div className="workspace-page-heading">
                <span className="workspace-page-heading__icon" aria-hidden="true">
                  <PageIcon size={21} />
                </span>
                <div className="workspace-page-heading__copy workspace-page-heading__copy--without-eyebrow">
                  <h1>{pageMeta.title}</h1>
                </div>
              </div>
            )}
            {musteriBaglami ? (
              <div className="react-topbar__context" role="status">
                <span>
                  <BriefcaseBusiness size={16} />
                  <strong>Müşteri çalışma alanı</strong>
                </span>
                <b>{ustBar?.aktifIsletme}</b>
                <small>{ustBar?.muhasebeciAdi ? `${ustBar.muhasebeciAdi} ile` : ""} {yetkiEtiketi(ustBar?.muhasebeciYetkiSeviyesi)}</small>
                <button type="button" onClick={musteriBaglaminiKapat} disabled={baglamKapatiliyor}>
                  {baglamKapatiliyor ? <Loader2 size={15} className="spin" /> : <LogOut size={15} />}
                  <span>Panele dön</span>
                </button>
              </div>
            ) : null}
          </div>

          <div className="react-topbar__actions">
            {ustBar?.hesapTipi === "Isletme" ? <AktifSubeSecici onDegisti={onUstBarYenile} /> : null}

            {ustBar?.hesapTipi === "Isletme" ? <span className="react-topbar__divider react-topbar__divider--branch" /> : null}

            <div
              className="react-topbar__telegram"
              title={ustBar?.telegramAktif ? "Telegram bağlantısı açık" : "Telegram bağlantısı kapalı"}
            >
              <span className="react-topbar__icon">
                <Send size={22} />
              </span>
              <span className={`react-topbar__badge ${ustBar?.telegramAktif ? "aktif" : "pasif"}`}>
                {ustBar?.telegramAktif ? "Bağlı" : "Telegram kapalı"}
              </span>
            </div>

            <span className="react-topbar__divider" />

            <div className="react-topbar__clock">
              <strong>{tarihBic(now, locale)}</strong>
              <span>{saatBic(now, locale)}</span>
            </div>

            <span className="react-topbar__divider" />

            <div ref={sohbetPanelRef} className="react-topbar__chat-wrap">
              <button
                className="react-topbar__bell react-topbar__chat-button"
                type="button"
                onClick={() => {
                  setSohbetPaneliAcik((current) => !current);
                  setBildirimPaneliAcik(false);
                }}
                aria-label="Sohbetleri göster"
                aria-expanded={sohbetPaneliAcik}
                aria-controls="react-topbar-chat-panel"
              >
                <MessageCircle size={24} />
                {sohbetSayisi > 0 ? <i>{sohbetSayisi > 9 ? "9+" : sohbetSayisi}</i> : null}
              </button>

              {sohbetPaneliAcik && (
                <div id="react-topbar-chat-panel" className="react-topbar__chat-panel" role="dialog" aria-label="Sohbetler">
                  <div className="react-topbar__panel-head">
                    <strong>Sohbetler</strong>
                    {sohbetSayisi ? <span>{sohbetSayisi}</span> : null}
                  </div>

                  {sohbetler.length === 0 ? (
                    <p className="notification-state">Henüz sohbet yok.</p>
                  ) : (
                    <div className="topbar-chat-list">
                      {sohbetler.map((item) => (
                        <button
                          key={`${item.muhasebeciIsletmeId}-${item.musteriIsletmeId}-${item.talepId ?? item.baglantiId ?? 0}`}
                          type="button"
                          onClick={() => {
                            window.location.href = item.hedefUrl || "/app/sohbetler";
                          }}
                        >
                          <span>
                            <strong>{item.baslik}</strong>
                            <small>{item.sonMesaj}</small>
                          </span>
                          {item.okunmamisMesajSayisi > 0 ? <i>{item.okunmamisMesajSayisi > 9 ? "9+" : item.okunmamisMesajSayisi}</i> : null}
                        </button>
                      ))}
                    </div>
                  )}
                  <a className="topbar-chat-center-link" href="/app/sohbetler">Sohbet merkezine git</a>
                </div>
              )}
            </div>

            <span className="react-topbar__divider" />

            <div ref={bildirimPanelRef} className="react-topbar__bell-wrap">
              <button
                className="react-topbar__bell"
                type="button"
                onClick={() => {
                  setBildirimPaneliAcik((current) => !current);
                  setSohbetPaneliAcik(false);
                }}
                aria-label="Bildirimleri göster"
                aria-expanded={bildirimPaneliAcik}
                aria-controls="react-topbar-notification-panel"
              >
                <Bell size={24} />
                {ustBar?.bildirimVar ? <i>{ustBar.bildirimSayisi > 9 ? "9+" : ustBar.bildirimSayisi}</i> : null}
              </button>

              {bildirimPaneliAcik && (
                <div id="react-topbar-notification-panel" className="react-topbar__panel" role="dialog" aria-label="Bildirimler">
                  <div className="react-topbar__panel-head">
                    <strong>Bildirimler</strong>
                    {bildirimler.some((item) => !item.okundu) ? <span>{bildirimler.filter((item) => !item.okundu).length}</span> : null}
                    {bildirimler.some((item) => !item.okundu) ? <button type="button" className="billing-link-button" onClick={() => void tumunuOkunduYap()}>Tümünü okundu işaretle</button> : null}
                  </div>

                  {bildirimYukleniyor ? (
                    <p className="notification-state">
                      <Loader2 size={16} />
                      Bildirimler hazırlanıyor...
                    </p>
                  ) : bildirimHata ? (
                    <p className="notification-state notification-state--error">{bildirimHata}</p>
                  ) : bildirimler.length === 0 ? (
                    <p className="notification-state">Henüz bildirim yok.</p>
                  ) : (
                    <div className="notification-list">
                      {bildirimler.map((item) => {
                        const content = (
                          <>
                            <span className={`notification-item__icon notification-item__icon--${item.tur}`}>
                              {bildirimIkonu(item.tur)}
                            </span>
                            <div>
                              <strong>{item.baslik}</strong>
                              {!item.okundu ? <em className="notification-item__unread">Okunmadı</em> : null}
                              <p>{item.mesaj}</p>
                              {item.aksiyon ? <small>{item.aksiyon}</small> : null}
                            </div>
                          </>
                        );

                        return item.url ? (
                          <a key={item.id} href={item.url} onClick={() => { if (!item.okundu) void bildirimiOkunduYap(item.id); }} className={`notification-item notification-item--${item.onem} ${item.okundu ? "notification-item--read" : "notification-item--unread"}`}>
                            {content}
                          </a>
                        ) : (
                          <article key={item.id} className={`notification-item notification-item--${item.onem} ${item.okundu ? "notification-item--read" : "notification-item--unread"}`}>
                            {content}
                            {!item.okundu ? <button type="button" className="billing-link-button" onClick={() => void bildirimiOkunduYap(item.id)}>Okundu işaretle</button> : null}
                          </article>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}
            </div>

            {sagAksiyon ? (
              <>
                <span className="react-topbar__divider" />
                {sagAksiyon}
              </>
            ) : null}
          </div>
        </header>

        <div className="react-shell__body">{children}</div>
        <AiAssistantPanel />
      </div>
      {planUyarisi ? (
        <div className="entitlement-modal-backdrop" role="presentation" onMouseDown={() => setPlanUyarisi(null)}>
          <section
            className="entitlement-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="entitlement-modal-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <span className="entitlement-modal__icon" aria-hidden="true"><AlertTriangle size={23} /></span>
            <div className="entitlement-modal__copy">
              <h2 id="entitlement-modal-title">
                {planUyarisi.code === "limit_reached" ? "Plan limitine ulaştınız" : "Plan yükseltmesi gerekiyor"}
              </h2>
              <p>{planUyarisi.detail}</p>
              {planUyarisi.limit != null ? (
                <div className="entitlement-modal__usage">
                  <span>Mevcut kullanım</span>
                  <strong>{planUyarisi.current ?? planUyarisi.limit} / {planUyarisi.limit}</strong>
                </div>
              ) : null}
            </div>
            <div className="entitlement-modal__actions">
              <button type="button" onClick={() => setPlanUyarisi(null)}>Şimdi değil</button>
              <a href={`/app/abonelik${planUyarisi.suggestedPlanCode ? `?plan=${encodeURIComponent(planUyarisi.suggestedPlanCode)}` : ""}`}>
                Planları incele
              </a>
            </div>
          </section>
        </div>
      ) : null}
    </div>
  );
}
