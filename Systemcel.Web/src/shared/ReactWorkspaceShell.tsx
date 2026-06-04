import React from "react";
import {
  AlertTriangle,
  ArrowDownUp,
  BarChart3,
  Bell,
  BriefcaseBusiness,
  Building2,
  CalendarClock,
  CreditCard,
  FileText,
  Globe2,
  Home,
  Landmark,
  Loader2,
  LogOut,
  MessageCircle,
  Send,
  Settings,
  Search,
  ShieldCheck,
  ShoppingCart,
  Wallet,
  WalletCards,
  type LucideIcon
} from "lucide-react";
import { AuthUserButton } from "../auth/AuthUserButton";
import systemcelLogo from "../assets/systemcel-logo.png";
import { AiAssistantPanel } from "./AiAssistantPanel";
import type { UstBarDurumu } from "./chrome";
import { jsonOku } from "./json";

interface ReactWorkspaceShellProps {
  children: React.ReactNode;
  hata?: string;
  islemde?: boolean;
  ustBar: UstBarDurumu | null;
  baslik?: React.ReactNode;
  sagAksiyon?: React.ReactNode;
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

interface Bildirim {
  id: string;
  tur: string;
  onem: string;
  baslik: string;
  mesaj: string;
  aksiyon: string;
  url?: string;
}

const anaMenu: Array<{ href: string; label: string; icon: LucideIcon; adminOnly?: boolean }> = [
  { href: "/", label: "Ana Sayfa", icon: Home },
  { href: "/gelir-gider", label: "Gelir / Gider Kayıtları", icon: ArrowDownUp },
  { href: "/cari-hesaplar", label: "Cari Hesaplar", icon: CreditCard },
  { href: "/urun-stok", label: "Ürün / Stok", icon: ShoppingCart },
  { href: "/faturalar", label: "Faturalar", icon: FileText },
  { href: "/tahsilat-odeme", label: "Tahsilat / Ödeme", icon: WalletCards },
  { href: "/raporlar", label: "Raporlar", icon: BarChart3 },
  { href: "/sohbetler", label: "Sohbetler", icon: MessageCircle },
  { href: "/muhasebeci", label: "Muhasebeci Paneli", icon: BriefcaseBusiness },
  { href: "/muhasebeciler", label: "Muhasebeciler", icon: Search },
  { href: "/yonetim/muhasebeci-basvurulari", label: "Yönetim", icon: ShieldCheck, adminOnly: true },
  { href: "/gib-portal", label: "GİB Portal Ayarları", icon: Globe2 },
  { href: "/ayarlar", label: "Ayarlar", icon: Settings }
];

function menuForWorkspace(ustBar: UstBarDurumu | null, musteriBaglami: boolean) {
  const visibleMenu = anaMenu.filter((item) => !item.adminOnly || ustBar?.yoneticiMi);
  const muhasebeciCalismaAlani = ustBar?.hesapTipi === "Muhasebeci" && !musteriBaglami;
  if (muhasebeciCalismaAlani) {
    return visibleMenu.filter((item) => item.href === "/muhasebeci" || item.href === "/muhasebeciler" || item.href === "/sohbetler" || item.href === "/ayarlar" || item.adminOnly);
  }

  if (musteriBaglami) {
    return visibleMenu.filter((item) => item.href !== "/muhasebeci" && item.href !== "/muhasebeciler");
  }

  return visibleMenu.filter((item) => item.href !== "/muhasebeci");
}

const ayarlarAltMenu = [
  { href: "/ayarlar?sekme=isletme", label: "İşletme", icon: Building2, sekme: "isletme" },
  { href: "/ayarlar?sekme=gib", label: "GİB Portal", icon: Landmark, sekme: "gib" },
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

  return currentPath === href || currentPath.startsWith(`${href}/`);
}

function tarihBic(now: Date) {
  return now.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}

function saatBic(now: Date) {
  return now.toLocaleTimeString("tr-TR", {
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

export function ReactWorkspaceShell({ children, ustBar, baslik, sagAksiyon }: ReactWorkspaceShellProps) {
  const [now, setNow] = React.useState(() => new Date());
  const [bildirimPaneliAcik, setBildirimPaneliAcik] = React.useState(false);
  const [bildirimler, setBildirimler] = React.useState<Bildirim[]>([]);
  const [bildirimYukleniyor, setBildirimYukleniyor] = React.useState(false);
  const [bildirimHata, setBildirimHata] = React.useState("");
  const [sohbetPaneliAcik, setSohbetPaneliAcik] = React.useState(false);
  const [baglamKapatiliyor, setBaglamKapatiliyor] = React.useState(false);
  const rawPath = normalizePath(window.location.pathname);
  const currentPath = rawPath === "/app" ? "/" : rawPath.startsWith("/app/") ? rawPath.slice(4) : rawPath;
  const aktifAyarlarSekmesi = new URLSearchParams(window.location.search).get("sekme")?.toLocaleLowerCase("tr-TR") || "isletme";
  const musteriBaglami = ustBar?.muhasebeciMusteriBaglami ?? false;
  const menuItems = menuForWorkspace(ustBar, musteriBaglami);
  const brandHref = ustBar?.hesapTipi === "Muhasebeci" && !musteriBaglami ? "/app/muhasebeci" : "/app";
  const menuCurrentPath = ustBar?.hesapTipi === "Muhasebeci" && !musteriBaglami && currentPath === "/" ? "/muhasebeci" : currentPath;
  const sohbetler = ustBar?.sohbet?.sohbetler ?? [];
  const sohbetSayisi = ustBar?.sohbet?.okunmamisMesajSayisi ?? 0;

  React.useEffect(() => {
    const handle = window.setInterval(() => setNow(new Date()), 30_000);
    return () => window.clearInterval(handle);
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

  React.useEffect(() => {
    if (bildirimPaneliAcik) {
      bildirimleriYukle();
    }
  }, [bildirimPaneliAcik, bildirimleriYukle]);

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
    <div className={`react-shell ${baslik ? "react-shell--page-title" : ""}`}>
      <aside className="react-sidebar" aria-label="Systemcel menüsü">
        <a className="react-sidebar__brand" href={brandHref} aria-label="Systemcel ana sayfa">
          <span className="react-sidebar__brand-mark" aria-hidden="true">
            <img src={systemcelLogo} alt="" />
          </span>
          <span className="react-sidebar__brand-text">
            <strong>SYSTEMCEL</strong>
            <small>Finance Suite</small>
          </span>
        </a>

        <nav className="react-sidebar__nav" aria-label="Ana menü">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const active = menuAktifMi(menuCurrentPath, item.href);
            return (
              <React.Fragment key={item.href}>
                <a className={`react-sidebar__link ${active ? "active" : ""}`} href={item.href === "/" ? "/app" : `/app${item.href}`}>
                  <Icon size={19} />
                  <span>{item.label}</span>
                </a>
                {item.href === "/ayarlar" && active ? (
                  <div className="react-sidebar__subnav" aria-label="Ayarlar alt menüsü">
                    {ayarlarAltMenu.map((subItem) => {
                      const SubIcon = subItem.icon;
                      const subActive =
                        aktifAyarlarSekmesi === subItem.sekme ||
                        (subItem.sekme === "gib" && aktifAyarlarSekmesi === "gib-portal") ||
                        (subItem.sekme === "telegram" && aktifAyarlarSekmesi === "bot");
                      return (
                        <a key={subItem.href} className={`react-sidebar__sublink ${subActive ? "active" : ""}`} href={`/app${subItem.href}`}>
                          <SubIcon size={16} />
                          <span>{subItem.label}</span>
                        </a>
                      );
                    })}
                  </div>
                ) : null}
              </React.Fragment>
            );
          })}
        </nav>

        <div className="react-sidebar__footer">
          <AuthUserButton />
        </div>
      </aside>

      <main className="react-shell__main">
        <header className="react-topbar">
          <div className="react-topbar__title-slot">
            {baslik ?? null}
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
            <div className="react-topbar__telegram">
              <span className="react-topbar__icon">
                <Send size={22} />
              </span>
              <span className={`react-topbar__badge ${ustBar?.telegramAktif ? "aktif" : "pasif"}`}>
                {ustBar?.telegramAktif ? "Aktif" : "Pasif"}
              </span>
            </div>

            <span className="react-topbar__divider" />

            <div className="react-topbar__clock">
              <strong>{tarihBic(now)}</strong>
              <span>{saatBic(now)}</span>
            </div>

            <span className="react-topbar__divider" />

            <div className="react-topbar__chat-wrap">
              <button
                className="react-topbar__bell react-topbar__chat-button"
                type="button"
                onClick={() => {
                  setSohbetPaneliAcik((current) => !current);
                  setBildirimPaneliAcik(false);
                }}
                aria-label="Sohbetleri göster"
              >
                <MessageCircle size={24} />
                {sohbetSayisi > 0 ? <i>{sohbetSayisi > 9 ? "9+" : sohbetSayisi}</i> : null}
              </button>

              {sohbetPaneliAcik && (
                <div className="react-topbar__chat-panel" role="dialog" aria-label="Sohbetler">
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

            <div className="react-topbar__bell-wrap">
              <button
                className="react-topbar__bell"
                type="button"
                onClick={() => {
                  setBildirimPaneliAcik((current) => !current);
                  setSohbetPaneliAcik(false);
                }}
                aria-label="Bildirimleri göster"
              >
                <Bell size={24} />
                {ustBar?.bildirimVar ? <i>{ustBar.bildirimSayisi > 9 ? "9+" : ustBar.bildirimSayisi}</i> : null}
              </button>

              {bildirimPaneliAcik && (
                <div className="react-topbar__panel" role="dialog" aria-label="Bildirimler">
                  <div className="react-topbar__panel-head">
                    <strong>Bildirimler</strong>
                    {ustBar?.bildirimSayisi ? <span>{ustBar.bildirimSayisi}</span> : null}
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
                              <p>{item.mesaj}</p>
                              {item.aksiyon ? <small>{item.aksiyon}</small> : null}
                            </div>
                          </>
                        );

                        return item.url ? (
                          <a key={item.id} href={item.url} className={`notification-item notification-item--${item.onem}`}>
                            {content}
                          </a>
                        ) : (
                          <article key={item.id} className={`notification-item notification-item--${item.onem}`}>
                            {content}
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
      </main>
    </div>
  );
}
