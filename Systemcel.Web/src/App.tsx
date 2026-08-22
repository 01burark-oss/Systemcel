import React from "react";
import { Building2, ChartNoAxesCombined, House, LogOut, MessageCircle, RefreshCw, Search } from "lucide-react";
import { RequireAuth } from "./auth/AuthGate";
import { AuthUserButton } from "./auth/AuthUserButton";
import { useSystemcelAuth } from "./auth/SystemcelAuthProvider";
import systemcelBrand from "./assets/systemcel-brand.svg";
import { BusinessSelector } from "./shared/BusinessSelector";
import type { UstBarDurumu } from "./shared/chrome";
import { jsonOku } from "./shared/json";
import { KolayKurulumModal, type KolayKurulumEkran } from "./shared/KolayKurulumModal";
import { ReactWorkspaceShell } from "./shared/ReactWorkspaceShell";

const AuthSayfasi = React.lazy(() =>
  import("./auth/AuthSayfasi").then((module) => ({ default: module.AuthSayfasi }))
);
const OAuthCallbackSayfasi = React.lazy(() =>
  import("./auth/AuthSayfasi").then((module) => ({ default: module.OAuthCallbackSayfasi }))
);
const PublicContentPage = React.lazy(() =>
  import("./marketing/PublicContentPage").then((module) => ({ default: module.PublicContentPage }))
);
const FaturaMusteriOnaySayfasi = React.lazy(() =>
  import("./screens/faturalar/FaturaMusteriOnaySayfasi").then((module) => ({ default: module.FaturaMusteriOnaySayfasi }))
);
const KaynakIndirmeSayfasi = React.lazy(() =>
  import("./marketing/KaynakIndirmeSayfasi").then((module) => ({ default: module.KaynakIndirmeSayfasi }))
);
const CariHesaplarSayfasi = React.lazy(() =>
  import("./screens/cari/CariHesaplarSayfasi").then((module) => ({ default: module.CariHesaplarSayfasi }))
);
const DashboardSayfasi = React.lazy(() =>
  import("./screens/dashboard/DashboardSayfasi").then((module) => ({ default: module.DashboardSayfasi }))
);
const FinansalGorunumSayfasi = React.lazy(() =>
  import("./screens/finansal-gorunum/FinansalGorunumSayfasi").then((module) => ({ default: module.FinansalGorunumSayfasi }))
);
const FaturalarSayfasi = React.lazy(() =>
  import("./screens/faturalar/FaturalarSayfasi").then((module) => ({ default: module.FaturalarSayfasi }))
);
const GelirGiderSayfasi = React.lazy(() =>
  import("./screens/gelir-gider/GelirGiderSayfasi").then((module) => ({ default: module.GelirGiderSayfasi }))
);
const GibPortalSayfasi = React.lazy(() =>
  import("./screens/gib-portal/GibPortalSayfasi").then((module) => ({ default: module.GibPortalSayfasi }))
);
const YardimSayfasi = React.lazy(() =>
  import("./screens/help/YardimSayfasi").then((module) => ({ default: module.YardimSayfasi }))
);
const MuhasebeciPanelSayfasi = React.lazy(() =>
  import("./screens/muhasebeci/MuhasebeciPanelSayfasi").then((module) => ({ default: module.MuhasebeciPanelSayfasi }))
);
const MuhasebecilerSayfasi = React.lazy(() =>
  import("./screens/muhasebeciler/MuhasebecilerSayfasi").then((module) => ({ default: module.MuhasebecilerSayfasi }))
);
const PinKilitSayfasi = React.lazy(() =>
  import("./screens/pin/PinKilitSayfasi").then((module) => ({ default: module.PinKilitSayfasi }))
);
const RaporlarSayfasi = React.lazy(() =>
  import("./screens/raporlar/RaporlarSayfasi").then((module) => ({ default: module.RaporlarSayfasi }))
);
const SohbetlerSayfasi = React.lazy(() =>
  import("./screens/sohbetler/SohbetlerSayfasi").then((module) => ({ default: module.SohbetlerSayfasi }))
);
const AyarlarSayfasi = React.lazy(() =>
  import("./screens/ayarlar/AyarlarSayfasi").then((module) => ({ default: module.AyarlarSayfasi }))
);
const AbonelikSayfasi = React.lazy(() =>
  import("./screens/billing/AbonelikSayfasi").then((module) => ({ default: module.AbonelikSayfasi }))
);
const TahsilatOdemeSayfasi = React.lazy(() =>
  import("./screens/tahsilat-odeme/TahsilatOdemeSayfasi").then((module) => ({ default: module.TahsilatOdemeSayfasi }))
);
const HizliSatisSayfasi = React.lazy(() =>
  import("./screens/urun-stok/HizliSatisSayfasi").then((module) => ({ default: module.HizliSatisSayfasi }))
);
const UrunStokSayfasi = React.lazy(() =>
  import("./screens/urun-stok/UrunStokSayfasi").then((module) => ({ default: module.UrunStokSayfasi }))
);
const WelcomeSayfasi = React.lazy(() =>
  import("./screens/welcome/WelcomeSayfasi").then((module) => ({ default: module.WelcomeSayfasi }))
);
const MuhasebeciBasvurulariSayfasi = React.lazy(() =>
  import("./screens/yonetim/MuhasebeciBasvurulariSayfasi").then((module) => ({ default: module.MuhasebeciBasvurulariSayfasi }))
);
const OdemeIncelemeSayfasi = React.lazy(() =>
  import("./screens/yonetim/OdemeIncelemeSayfasi").then((module) => ({ default: module.OdemeIncelemeSayfasi }))
);

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
  useNumericInputGuard();

  return (
    <React.Suspense fallback={null}>
      <AppRoutes />
    </React.Suspense>
  );
}

function AppRoutes() {
  const auth = useSystemcelAuth();

  const rawPath = normalizePath(window.location.pathname);
  const appPath = workspacePathFromPublicPath(rawPath);
  const planDevami = rawPath.startsWith("/app") && new URLSearchParams(window.location.search).has("plan");
  const path = planDevami ? "/abonelik" : appPath === "/telegram" ? "/ayarlar" : appPath;

  if (planDevami && !rawPath.startsWith("/app/abonelik")) {
    window.history.replaceState(null, "", `/app/abonelik${window.location.search}`);
  }

  if (rawPath === "/telegram" || rawPath === "/app/telegram") {
    window.history.replaceState(null, "", "/app/ayarlar?sekme=telegram");
  }

  if (path === "/kilit-ekrani") {
    return <PinKilitSayfasi />;
  }

  if (path === "/oauth-callback") {
    return <OAuthCallbackSayfasi />;
  }

  if (rawPath.startsWith("/fatura-onayi/")) {
    const token = rawPath.slice("/fatura-onayi/".length);
    return <FaturaMusteriOnaySayfasi token={token} />;
  }

  if (rawPath.startsWith("/kaynaklar/")) {
    const kod = rawPath.slice("/kaynaklar/".length).split("/")[0];
    return <KaynakIndirmeSayfasi kod={kod} />;
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
    return <PublicContentPage kind="about" />;
  }

  if (path === "/blog") {
    return <PublicContentPage kind="blog" />;
  }

  if (path === "/kariyer") {
    return <PublicContentPage kind="careers" />;
  }

  if (path === "/iletisim" || decodeURI(path) === "/iletişim") {
    return <PublicContentPage kind="contact" />;
  }

  if (path === "/kvkk") {
    return <PublicContentPage kind="kvkk" />;
  }

  if (path === "/gizlilik") {
    return <PublicContentPage kind="privacy" />;
  }

  if (path === "/kullanim-sartlari" || decodeURI(path) === "/kullanım-şartları") {
    return <PublicContentPage kind="terms" />;
  }

  if (path === "/abonelik-kosullari" || decodeURI(path) === "/abonelik-koşulları") {
    return <PublicContentPage kind="subscription" />;
  }

  if (path === "/cerezler" || decodeURI(path) === "/çerezler") {
    return <PublicContentPage kind="cookies" />;
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
    decoded === "/oauth-callback" ||
    decoded === "/hosgeldin" ||
    decoded === "/muhasebeciler" ||
    decoded === "/yardim" ||
    decoded === "/yardÄ±m" ||
    decoded === "/hakkimizda" ||
    decoded === "/hakkÄ±mÄ±zda" ||
    decoded === "/blog" ||
    decoded === "/kariyer" ||
    decoded === "/iletisim" ||
    decoded === "/iletişim" ||
    decoded === "/kvkk" ||
    decoded === "/gizlilik" ||
    decoded === "/kullanim-sartlari" ||
    decoded === "/kullanım-şartları" ||
    decoded === "/cerezler" ||
    decoded === "/çerezler" ||
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
  const muhasebeciCalismaAlaniRoute = path === "/muhasebeci" || path === "/muhasebeciler" || path === "/sohbetler" || path === "/abonelik" || path === "/ayarlar";
  const routePath = muhasebeciCalismaAlani && !yonetimRoute && !muhasebeciCalismaAlaniRoute ? "/muhasebeci" : path === "/yonetim" ? "/yonetim/muhasebeci-basvurulari" : path;

  const shellUstAksiyon = !ustBar?.muhasebeciMusteriBaglami && !muhasebeciCalismaAlani && routePath !== "/muhasebeci" && !yonetimRoute ? (
    <div className="workspace-page-actions">
      <button
        type="button"
        className="ghost-refresh"
        aria-label="Çalışma alanını yenile"
        title="Çalışma alanını yenile"
        disabled={ustBarIslemde}
        onClick={() => {
          setUstBarIslemde(true);
          Promise.resolve(ustBarYukle())
            .then(() => {
              React.startTransition(() => setYenileAnahtari((current) => current + 1));
            })
            .catch((error: Error) => setUstBarHata(error.message))
            .finally(() => setUstBarIslemde(false));
        }}
      >
        <RefreshCw size={17} />
      </button>
      <BusinessSelector
        aktifIsletmeId={ustBar?.aktifIsletmeId}
        disabled={ustBarIslemde}
        isletmeler={ustBar?.isletmeler ?? []}
        onChange={isletmeDegistir}
      />
    </div>
  ) : null;

  if (mobileWorkspace && routePath === "/sohbetler") {
    return (
      <MobileWorkspaceView active="sohbetler">
        <SohbetlerSayfasi mobileMode ustBar={ustBar} onUstBarYenile={ustBarYukle} />
      </MobileWorkspaceView>
    );
  }

  if (mobileWorkspace && routePath === "/muhasebeciler") {
    return (
      <MobileWorkspaceView active="muhasebeciler">
        <MuhasebecilerSayfasi mobileMode ustBar={ustBar} onUstBarYenile={ustBarYukle} />
      </MobileWorkspaceView>
    );
  }

  if (mobileWorkspace && routePath === "/abonelik") {
    return (
      <MobileWorkspaceView active="merkez">
        <AbonelikSayfasi />
      </MobileWorkspaceView>
    );
  }

  if (mobileWorkspace && routePath === "/finansal-gorunum") {
    return (
      <MobileWorkspaceView active="finans">
        <FinansalGorunumSayfasi yenileAnahtari={yenileAnahtari} ustBar={ustBar} />
      </MobileWorkspaceView>
    );
  }

  if (mobileWorkspace) {
    return (
      <MobileWorkspaceView active="merkez">
        <MobileCompanionScreen
          hesapTipi={ustBar?.hesapTipi ?? ""}
          islemde={ustBarIslemde}
          calismaAlani={ustBar?.aktifIsletme ?? ""}
          sohbetSayisi={ustBar?.sohbet?.okunmamisMesajSayisi ?? 0}
          onSignOut={async () => {
            const redirectUrl = "/giris";
            if (auth.clerk?.signOut) {
              await auth.clerk.signOut({ redirectUrl });
              return;
            }

            window.location.replace(redirectUrl);
          }}
        />
      </MobileWorkspaceView>
    );
  }

  return (
    <ReactWorkspaceShell
      hata={ustBarHata}
      islemde={ustBarIslemde}
      onUstBarYenile={ustBarYukle}
      sagAksiyon={shellUstAksiyon}
      ustBar={ustBar}
    >
      <React.Suspense fallback={null}>
        {routePath === "/gelir-gider" ? (
          <GelirGiderSayfasi
            ustBar={ustBar}
            ustBarIslemde={ustBarIslemde}
            yenileAnahtari={yenileAnahtari}
            onIsletmeDegistir={isletmeDegistir}
          />
        ) : routePath === "/hizli-satis" ? (
          <HizliSatisSayfasi yenileAnahtari={yenileAnahtari} />
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
        ) : routePath === "/finansal-gorunum" ? (
          <FinansalGorunumSayfasi yenileAnahtari={yenileAnahtari} ustBar={ustBar} />
        ) : routePath === "/muhasebeci" ? (
          <MuhasebeciPanelSayfasi onUstBarYenile={ustBarYukle} />
        ) : routePath === "/yonetim/muhasebeci-basvurulari" ? (
          <MuhasebeciBasvurulariSayfasi onUstBarYenile={ustBarYukle} />
        ) : routePath === "/yonetim/odemeler" ? (
          <OdemeIncelemeSayfasi />
        ) : routePath === "/muhasebeciler" ? (
          <MuhasebecilerSayfasi ustBar={ustBar} onUstBarYenile={ustBarYukle} />
        ) : routePath === "/sohbetler" ? (
          <SohbetlerSayfasi ustBar={ustBar} onUstBarYenile={ustBarYukle} />
        ) : routePath === "/gib-portal" ? (
          <GibPortalSayfasi
            ustBar={ustBar}
            ustBarIslemde={ustBarIslemde}
            yenileAnahtari={yenileAnahtari}
            onIsletmeDegistir={isletmeDegistir}
          />
        ) : routePath === "/abonelik" ? (
          <AbonelikSayfasi />
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
      </React.Suspense>
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
  sohbetSayisi,
  onSignOut
}: {
  hesapTipi: string;
  calismaAlani: string;
  islemde: boolean;
  sohbetSayisi: number;
  onSignOut: () => void;
}) {
  const isAccountant = hesapTipi === "Muhasebeci";
  const title = isAccountant ? "Muhasebeci Merkezi" : "Çalışma Alanı";
  const description = isAccountant
    ? "Müşteri sohbetlerini yönetin ve pazaryerindeki işletme taleplerini görüntüleyin."
    : "Muhasebecinizle konuşun veya ihtiyaçlarınıza uygun muhasebecileri karşılaştırın.";

  return (
    <main className="mobile-companion">
      <section className="mobile-companion__panel" aria-labelledby="mobile-companion-title">
        <header className="mobile-companion__brand">
          <img className="mobile-companion__brand-logo" src={systemcelBrand} alt="systemcel Finance Suite" />
        </header>

        <h1 id="mobile-companion-title">{title}</h1>
        <p>{description}</p>

        <div className="mobile-companion__actions" aria-label="Mobil erişim">
          {!isAccountant ? (
            <a href="/app/finansal-gorunum">
              <ChartNoAxesCombined size={18} />
              <span>Finans durumu</span>
            </a>
          ) : null}
          <a href="/app/sohbetler">
            <MessageCircle size={18} />
            <span>Sohbetler</span>
            {sohbetSayisi > 0 ? <i>{sohbetSayisi > 9 ? "9+" : sohbetSayisi}</i> : null}
          </a>
          <a href="/app/muhasebeciler">
            <Search size={18} />
            <span>Muhasebeci pazaryeri</span>
          </a>
        </div>

        {calismaAlani || islemde ? (
          <div className="mobile-companion__workspace">
            <span className="mobile-companion__workspace-icon" aria-hidden="true">
              <Building2 size={20} />
            </span>
            <div>
              <span>Aktif çalışma alanı</span>
              <strong>{islemde ? "Yükleniyor..." : calismaAlani}</strong>
            </div>
          </div>
        ) : null}

        <div className="mobile-companion__summary" aria-label="Çalışma alanı özeti">
          <article>
            <strong>{sohbetSayisi}</strong>
            <span>Okunmamış mesaj</span>
          </article>
          <article>
            <strong>{calismaAlani ? "Aktif" : "—"}</strong>
            <span>Çalışma alanı</span>
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

function MobileWorkspaceView({
  active,
  children
}: {
  active: "merkez" | "finans" | "sohbetler" | "muhasebeciler";
  children: React.ReactNode;
}) {
  return (
    <div className={`mobile-workspace-view mobile-workspace-view--${active}`}>
      <div className="mobile-workspace-view__content">
        <React.Suspense fallback={null}>{children}</React.Suspense>
      </div>
      <nav className="mobile-workspace-nav" aria-label="Mobil çalışma alanı">
        <a className={active === "merkez" ? "active" : ""} href="/app" aria-label="Merkeze dön">
          <House size={18} />
          <span>Merkez</span>
        </a>
        <a className={active === "finans" ? "active" : ""} href="/app/finansal-gorunum" aria-label="Finans durumu">
          <ChartNoAxesCombined size={18} />
          <span>Finans</span>
        </a>
        <a className={active === "sohbetler" ? "active" : ""} href="/app/sohbetler" aria-label="Sohbetler">
          <MessageCircle size={18} />
          <span>Sohbetler</span>
        </a>
        <a className={active === "muhasebeciler" ? "active" : ""} href="/app/muhasebeciler" aria-label="Muhasebeciler">
          <Search size={18} />
          <span>Muhasebeciler</span>
        </a>
        <AuthUserButton compact />
      </nav>
    </div>
  );
}

function useNumericInputGuard() {
  React.useEffect(() => {
    const sayisalAlanMi = (target: EventTarget | null): target is HTMLInputElement => {
      if (!(target instanceof HTMLInputElement)) return false;
      return target.type === "number" || target.inputMode === "decimal" || target.inputMode === "numeric";
    };

    const degerGecerliMi = (input: HTMLInputElement, value: string) => {
      const negatifOlabilir = input.dataset.allowNegative === "true";
      if (input.inputMode === "numeric") {
        return negatifOlabilir ? /^-?\d*$/.test(value) : /^\d*$/.test(value);
      }

      return negatifOlabilir
        ? /^-?\d*(?:[.,]\d*)?$/.test(value)
        : /^\d*(?:[.,]\d*)?$/.test(value);
    };

    const sonrakiDeger = (input: HTMLInputElement, eklenen: string) => {
      const start = input.selectionStart ?? input.value.length;
      const end = input.selectionEnd ?? start;
      return `${input.value.slice(0, start)}${eklenen}${input.value.slice(end)}`;
    };

    const beforeInput = (event: InputEvent) => {
      if (!sayisalAlanMi(event.target) || event.isComposing || event.inputType.startsWith("delete")) return;
      if (event.data === null) return;
      if (!degerGecerliMi(event.target, sonrakiDeger(event.target, event.data))) {
        event.preventDefault();
      }
    };

    const paste = (event: ClipboardEvent) => {
      if (!sayisalAlanMi(event.target)) return;
      const text = event.clipboardData?.getData("text") ?? "";
      if (!degerGecerliMi(event.target, sonrakiDeger(event.target, text))) {
        event.preventDefault();
      }
    };

    document.addEventListener("beforeinput", beforeInput, true);
    document.addEventListener("paste", paste, true);
    return () => {
      document.removeEventListener("beforeinput", beforeInput, true);
      document.removeEventListener("paste", paste, true);
    };
  }, []);
}
