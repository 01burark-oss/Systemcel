import React from "react";
import { Eye, Lock, LogOut, MessageCircle, Monitor } from "lucide-react";
import { AuthSayfasi } from "./auth/AuthSayfasi";
import { RequireAuth } from "./auth/AuthGate";
import { useSystemcelAuth } from "./auth/SystemcelAuthProvider";
import systemcelIcon from "./assets/systemcel-icon.png";
import { HakkimizdaSayfasi } from "./screens/about/HakkimizdaSayfasi";
import { CariHesaplarSayfasi } from "./screens/cari/CariHesaplarSayfasi";
import { DashboardSayfasi } from "./screens/dashboard/DashboardSayfasi";
import { FaturalarSayfasi } from "./screens/faturalar/FaturalarSayfasi";
import { GelirGiderSayfasi } from "./screens/gelir-gider/GelirGiderSayfasi";
import { GibPortalSayfasi } from "./screens/gib-portal/GibPortalSayfasi";
import { YardimSayfasi } from "./screens/help/YardimSayfasi";
import { MuhasebeciPanelSayfasi } from "./screens/muhasebeci/MuhasebeciPanelSayfasi";
import { MuhasebecilerSayfasi } from "./screens/muhasebeciler/MuhasebecilerSayfasi";
import { PinKilitSayfasi } from "./screens/pin/PinKilitSayfasi";
import { RaporlarSayfasi } from "./screens/raporlar/RaporlarSayfasi";
import { AyarlarSayfasi } from "./screens/ayarlar/AyarlarSayfasi";
import { TahsilatOdemeSayfasi } from "./screens/tahsilat-odeme/TahsilatOdemeSayfasi";
import { UrunStokSayfasi } from "./screens/urun-stok/UrunStokSayfasi";
import { WelcomeSayfasi } from "./screens/welcome/WelcomeSayfasi";
import { MuhasebeciBasvurulariSayfasi } from "./screens/yonetim/MuhasebeciBasvurulariSayfasi";
import { BusinessSelector } from "./shared/BusinessSelector";
import type { UstBarDurumu } from "./shared/chrome";
import { jsonOku } from "./shared/json";
import { KolayKurulumModal, type KolayKurulumEkran } from "./shared/KolayKurulumModal";
import { ReactWorkspaceShell } from "./shared/ReactWorkspaceShell";

function normalizePath(pathname: string) {
  const normalized = pathname.replace(/\/+$/, "");
  return normalized.length === 0 ? "/" : normalized;
}

function pathMatches(path: string, basePath: string) {
  return path === basePath || path.startsWith(`${basePath}/`);
}

function workspacePathFromPublicPath(path: string) {
  return path === "/app" ? "/" : path.startsWith("/app/") ? path.slice(4) : path;
}

export function App() {
  useClientNavigation();
  const auth = useSystemcelAuth();

  const rawPath = normalizePath(window.location.pathname);
  const appPath = workspacePathFromPublicPath(rawPath);
  const path = appPath === "/telegram" ? "/ayarlar" : appPath;

  if (rawPath === "/telegram" || rawPath === "/app/telegram") {
    window.history.replaceState(null, "", "/app/ayarlar?sekme=telegram");
  }

  if (path === "/kilit-ekrani") {
    return <PinKilitSayfasi />;
  }

  if (pathMatches(path, "/giris")) {
    return <AuthSayfasi mode="sign-in" />;
  }

  if (pathMatches(path, "/kayit")) {
    return <AuthSayfasi mode="sign-up" />;
  }

  if (rawPath === "/" || path === "/hosgeldin") {
    if (auth.clerkEnabled && !auth.isLoaded) {
      return null;
    }

    if (!auth.clerkEnabled || auth.isSignedIn) {
      return <SignedInLanding />;
    }

    return <WelcomeSayfasi />;
  }

  if (rawPath === "/urun") {
    return <RemovedProductRoute />;
  }

  if (rawPath === "/muhasebeciler") {
    return <MuhasebecilerSayfasi publicMode />;
  }

  if (path === "/yardim" || decodeURI(path) === "/yardım") {
    return <YardimSayfasi />;
  }

  if (path === "/hakkimizda" || decodeURI(path) === "/hakkımızda") {
    return <HakkimizdaSayfasi />;
  }

  return (
    <RequireAuth>
      <WorkspaceRoutes path={path} />
    </RequireAuth>
  );
}

function SignedInLanding() {
  React.useEffect(() => {
    if (window.location.pathname !== "/app") {
      window.history.replaceState(null, "", "/app");
    }
  }, []);

  return (
    <RequireAuth>
      <WorkspaceRoutes path="/" />
    </RequireAuth>
  );
}

function RemovedProductRoute() {
  const auth = useSystemcelAuth();
  const signedIn = !auth.clerkEnabled || auth.isSignedIn;

  React.useEffect(() => {
    if (auth.clerkEnabled && !auth.isLoaded) {
      return;
    }

    const target = signedIn ? "/app" : "/";
    if (window.location.pathname !== target) {
      window.history.replaceState(null, "", target);
    }
  }, [auth.clerkEnabled, auth.isLoaded, signedIn]);

  if (auth.clerkEnabled && !auth.isLoaded) {
    return null;
  }

  if (signedIn) {
    return (
      <RequireAuth>
        <WorkspaceRoutes path="/" />
      </RequireAuth>
    );
  }

  return <WelcomeSayfasi />;
}

function useClientNavigation() {
  const [, setNavigationVersion] = React.useState(0);

  const refresh = React.useCallback(() => {
    setNavigationVersion((current) => current + 1);
  }, []);

  const navigate = React.useCallback(
    (url: URL, replace = false) => {
      const nextPath = `${url.pathname}${url.search}${url.hash}`;
      const currentPath = `${window.location.pathname}${window.location.search}${window.location.hash}`;

      if (nextPath !== currentPath) {
        if (replace) {
          window.history.replaceState(null, "", nextPath);
        } else {
          window.history.pushState(null, "", nextPath);
        }
      }

      refresh();
      restoreScroll(url);
    },
    [refresh]
  );

  React.useEffect(() => {
    const onPopState = () => refresh();

    const onDocumentClick = (event: MouseEvent) => {
      if (event.defaultPrevented || event.button !== 0 || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
        return;
      }

      const target = event.target instanceof Element ? event.target : null;
      const anchor = target?.closest("a[href]") as HTMLAnchorElement | null;
      if (!anchor || anchor.hasAttribute("download") || (anchor.target && anchor.target !== "_self")) {
        return;
      }

      const href = anchor.getAttribute("href");
      if (!href || href.startsWith("#")) {
        return;
      }

      const url = new URL(anchor.href);
      if (url.origin !== window.location.origin || !isClientRoute(url.pathname)) {
        return;
      }

      event.preventDefault();
      navigate(url);
    };

    window.addEventListener("popstate", onPopState);
    document.addEventListener("click", onDocumentClick);
    return () => {
      window.removeEventListener("popstate", onPopState);
      document.removeEventListener("click", onDocumentClick);
    };
  }, [navigate, refresh]);
}

function isClientRoute(pathname: string) {
  const decoded = safeDecodePath(pathname);
  return (
    pathname === "/" ||
    pathname === "/app" ||
    pathname.startsWith("/app/") ||
    decoded === "/giris" ||
    decoded.startsWith("/giris/") ||
    decoded === "/kayit" ||
    decoded.startsWith("/kayit/") ||
    decoded === "/hosgeldin" ||
    decoded === "/muhasebeciler" ||
    decoded === "/yardim" ||
    decoded === "/yardÄ±m" ||
    decoded === "/hakkimizda" ||
    decoded === "/hakkÄ±mÄ±zda" ||
    decoded === "/telegram" ||
    decoded === "/kilit-ekrani"
  );
}

function safeDecodePath(pathname: string) {
  try {
    return decodeURI(pathname);
  } catch {
    return pathname;
  }
}

function restoreScroll(url: URL) {
  window.requestAnimationFrame(() => {
    if (url.hash) {
      const id = safeDecodePath(url.hash.slice(1));
      const target: Element | null = document.getElementById(id) ?? document.getElementsByName(id)[0] ?? null;
      if (target) {
        target.scrollIntoView();
      }
      return;
    }

    window.scrollTo({ left: 0, top: 0 });
  });
}

function useMobileWorkspaceGate() {
  const [isMobileWorkspace, setIsMobileWorkspace] = React.useState(() => {
    if (typeof window === "undefined" || !window.matchMedia) {
      return false;
    }

    return window.matchMedia("(max-width: 760px), (pointer: coarse)").matches;
  });

  React.useEffect(() => {
    if (!window.matchMedia) {
      return undefined;
    }

    const media = window.matchMedia("(max-width: 760px), (pointer: coarse)");
    const update = () => setIsMobileWorkspace(media.matches);
    update();
    media.addEventListener("change", update);
    return () => media.removeEventListener("change", update);
  }, []);

  return isMobileWorkspace;
}

function WorkspaceRoutes({ path }: { path: string }) {
  const auth = useSystemcelAuth();
  const mobileWorkspace = useMobileWorkspaceGate();
  const [ustBar, setUstBar] = React.useState<UstBarDurumu | null>(null);
  const [ustBarHata, setUstBarHata] = React.useState("");
  const [ustBarIslemde, setUstBarIslemde] = React.useState(false);
  const [yenileAnahtari, setYenileAnahtari] = React.useState(0);
  const [kolayKurulum, setKolayKurulum] = React.useState<KolayKurulumEkran | null>(null);
  const [kurulumGizlendi, setKurulumGizlendi] = React.useState(false);

  const ustBarYukle = React.useCallback(async () => {
    setUstBarHata("");
    const data = await jsonOku<UstBarDurumu>("/api/ekran/ust-bar");
    setUstBar(data);
    return data;
  }, []);

  const kolayKurulumYukle = React.useCallback(async () => {
    const data = await jsonOku<KolayKurulumEkran>("/api/ekran/kolay-kurulum");
    setKolayKurulum(data);
    return data;
  }, []);

  React.useEffect(() => {
    setUstBarIslemde(true);
    ustBarYukle()
      .catch((error: Error) => {
        setUstBarHata(error.message);
      })
      .finally(() => {
        setUstBarIslemde(false);
      });
  }, [ustBarYukle]);

  React.useEffect(() => {
    kolayKurulumYukle().catch(() => undefined);
  }, [kolayKurulumYukle]);

  React.useEffect(() => {
    const yenile = () => {
      if (document.visibilityState === "hidden")
        return;

      ustBarYukle().catch((error: Error) => {
        setUstBarHata(error.message);
      });
    };

    window.addEventListener("focus", yenile);
    document.addEventListener("visibilitychange", yenile);
    return () => {
      window.removeEventListener("focus", yenile);
      document.removeEventListener("visibilitychange", yenile);
    };
  }, [ustBarYukle]);

  React.useEffect(() => {
    const handle = window.setInterval(() => {
      if (document.visibilityState === "visible") {
        ustBarYukle().catch((error: Error) => {
          setUstBarHata(error.message);
        });
      }
    }, 5 * 60_000);

    return () => window.clearInterval(handle);
  }, [ustBarYukle]);

  React.useEffect(() => {
    if (!ustBarHata)
      return;

    const handle = window.setTimeout(() => {
      if (document.visibilityState === "visible") {
        ustBarYukle().catch(() => undefined);
      }
    }, 4_000);

    return () => window.clearTimeout(handle);
  }, [ustBarHata, ustBarYukle]);

  const isletmeDegistir = React.useCallback(async (id: number) => {
    try {
      setUstBarIslemde(true);
      setUstBarHata("");
      const data = await jsonOku<UstBarDurumu>(`/api/ekran/ust-bar/isletme/${id}`, { method: "PUT" });
      setUstBar(data);
      setKurulumGizlendi(false);
      kolayKurulumYukle().catch(() => undefined);
      React.startTransition(() => {
        setYenileAnahtari((current) => current + 1);
      });
    } catch (error) {
      setUstBarHata(error instanceof Error ? error.message : "İşletme değiştirilemedi.");
    } finally {
      setUstBarIslemde(false);
    }
  }, [kolayKurulumYukle]);

  const muhasebeciCalismaAlani = ustBar?.hesapTipi === "Muhasebeci" && !ustBar.muhasebeciMusteriBaglami;
  const yonetimRoute = path === "/yonetim" || path.startsWith("/yonetim/");
  const muhasebeciCalismaAlaniRoute = path === "/muhasebeci" || path === "/muhasebeciler" || path === "/ayarlar";
  const routePath = muhasebeciCalismaAlani && !yonetimRoute && !muhasebeciCalismaAlaniRoute ? "/muhasebeci" : path === "/yonetim" ? "/yonetim/muhasebeci-basvurulari" : path;

  const shellBaslik =
    routePath === "/faturalar"
      ? "Faturalar"
      : routePath === "/tahsilat-odeme"
        ? "Tahsilat / Ödeme"
        : routePath === "/raporlar"
          ? "Raporlar"
          : routePath === "/muhasebeci"
            ? "Muhasebeci Paneli"
            : routePath === "/yonetim/muhasebeci-basvurulari"
              ? "Muhasebeci Başvuruları"
            : routePath === "/muhasebeciler"
              ? "Muhasebeciler"
              : routePath === "/gib-portal"
            ? "GİB Portal Ayarları"
            : routePath === "/ayarlar"
              ? "Ayarlar"
              : "";
  const shellUstBaslik = shellBaslik ? <h1 className="react-topbar__page-title">{shellBaslik}</h1> : null;
  const shellUstAksiyon = shellBaslik && !ustBar?.muhasebeciMusteriBaglami && !muhasebeciCalismaAlani && routePath !== "/muhasebeci" && !yonetimRoute ? (
    <BusinessSelector
      aktifIsletmeId={ustBar?.aktifIsletmeId}
      disabled={ustBarIslemde}
      isletmeler={ustBar?.isletmeler ?? []}
      onChange={isletmeDegistir}
    />
  ) : null;

  if (mobileWorkspace) {
    return (
      <MobileCompanionScreen
        hesapTipi={ustBar?.hesapTipi ?? ""}
        islemde={ustBarIslemde}
        calismaAlani={ustBar?.aktifIsletme ?? ""}
        onSignOut={() => {
          const redirectUrl = "/";
          if (auth.clerk?.signOut) {
            auth.clerk.signOut({ redirectUrl });
            return;
          }

          window.location.href = redirectUrl;
        }}
      />
    );
  }

  return (
    <ReactWorkspaceShell
      baslik={shellUstBaslik}
      hata={ustBarHata}
      islemde={ustBarIslemde}
      onUstBarYenile={ustBarYukle}
      sagAksiyon={shellUstAksiyon}
      ustBar={ustBar}
    >
      {routePath === "/gelir-gider" ? (
        <GelirGiderSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/cari-hesaplar" ? (
        <CariHesaplarSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/urun-stok" ? (
        <UrunStokSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/faturalar" ? (
        <FaturalarSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/tahsilat-odeme" ? (
        <TahsilatOdemeSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/raporlar" ? (
        <RaporlarSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/muhasebeci" ? (
        <MuhasebeciPanelSayfasi onUstBarYenile={ustBarYukle} />
      ) : routePath === "/yonetim/muhasebeci-basvurulari" ? (
        <MuhasebeciBasvurulariSayfasi onUstBarYenile={ustBarYukle} />
      ) : routePath === "/muhasebeciler" ? (
        <MuhasebecilerSayfasi ustBar={ustBar} onUstBarYenile={ustBarYukle} />
      ) : routePath === "/gib-portal" ? (
        <GibPortalSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      ) : routePath === "/ayarlar" ? (
        <AyarlarSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
          onUstBarYenile={ustBarYukle}
        />
      ) : (
        <DashboardSayfasi
          ustBar={ustBar}
          ustBarIslemde={ustBarIslemde}
          yenileAnahtari={yenileAnahtari}
          onIsletmeDegistir={isletmeDegistir}
        />
      )}
      {kolayKurulum && !kolayKurulum.tamamlandi && !kurulumGizlendi ? (
        <KolayKurulumModal
          ekran={kolayKurulum}
          onClose={() => setKurulumGizlendi(true)}
          onComplete={(sonuc) => {
            setKolayKurulum(sonuc);
            setKurulumGizlendi(false);
            ustBarYukle().catch(() => undefined);
            React.startTransition(() => {
              setYenileAnahtari((current) => current + 1);
            });
          }}
        />
      ) : null}
    </ReactWorkspaceShell>
  );
}

function MobileCompanionScreen({
  hesapTipi,
  calismaAlani,
  islemde,
  onSignOut
}: {
  hesapTipi: string;
  calismaAlani: string;
  islemde: boolean;
  onSignOut: () => void;
}) {
  const isAccountant = hesapTipi === "Muhasebeci";
  const title = isAccountant ? "Mobil muhasebeci akışı hazırlanıyor" : "Mobil erişim sınırlı";
  const description = isAccountant
    ? "Tam müşteri yönetimi masaüstünde kullanılacak. Mobilde müşteri mesajları, bildirimler ve okunabilir özetler için ayrı bir akış sunacağız."
    : "Tam finans paneli masaüstü kullanım için tasarlandı. Mobilde veri okuma, bildirim ve muhasebeciyle mesajlaşma gibi güvenli eşlikçi özellikler sunulacak.";

  return (
    <main className="mobile-companion">
      <section className="mobile-companion__panel" aria-labelledby="mobile-companion-title">
        <header className="mobile-companion__brand">
          <img src={systemcelIcon} alt="" />
          <div>
            <strong>SYSTEMCEL</strong>
            <span>Finance Suite</span>
          </div>
        </header>

        <div className="mobile-companion__badge">
          <Lock size={16} />
          Mobilde tam panel kapalı
        </div>

        <h1 id="mobile-companion-title">{title}</h1>
        <p>{description}</p>

        {calismaAlani || islemde ? (
          <div className="mobile-companion__workspace">
            <span>Çalışma alanı</span>
            <strong>{islemde ? "Yükleniyor..." : calismaAlani}</strong>
          </div>
        ) : null}

        <div className="mobile-companion__features" aria-label="Mobil akış kapsamı">
          <article>
            <Eye size={20} />
            <div>
              <strong>Güvenli özet okuma</strong>
              <span>Gelir, gider, fatura ve bildirimleri işlem yapmadan görüntüleme.</span>
            </div>
          </article>
          <article>
            <MessageCircle size={20} />
            <div>
              <strong>{isAccountant ? "Müşteri mesajları" : "Muhasebeci mesajları"}</strong>
              <span>Onay, soru ve belge talepleri için ayrı mobil mesajlaşma akışı.</span>
            </div>
          </article>
          <article>
            <Monitor size={20} />
            <div>
              <strong>Masaüstünde tam kullanım</strong>
              <span>Kayıt, fatura, stok ve tahsilat işlemleri bilgisayardan yapılır.</span>
            </div>
          </article>
        </div>

        <button className="mobile-companion__signout" type="button" onClick={onSignOut}>
          <LogOut size={18} />
          Çıkış yap
        </button>
      </section>
    </main>
  );
}
