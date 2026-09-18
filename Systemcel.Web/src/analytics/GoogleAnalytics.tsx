import React from "react";

const MEASUREMENT_ID = "G-825YG8XTJK";
const CONSENT_KEY = "systemcel.analyticsConsent";
const SCRIPT_ID = "systemcel-google-analytics";

declare global {
  interface Window {
    dataLayer: unknown[];
    gtag?: (...args: unknown[]) => void;
  }
}

type AnalyticsConsent = "granted" | "denied";

function readConsent(): AnalyticsConsent | null {
  const value = window.localStorage.getItem(CONSENT_KEY);
  return value === "granted" || value === "denied" ? value : null;
}

function loadAnalytics() {
  if (document.getElementById(SCRIPT_ID)) return;

  window.dataLayer = window.dataLayer || [];
  window.gtag = window.gtag || function gtag(...args: unknown[]) {
    window.dataLayer.push(args);
  };
  window.gtag("js", new Date());
  window.gtag("config", MEASUREMENT_ID, { send_page_view: false });

  const script = document.createElement("script");
  script.id = SCRIPT_ID;
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtag/js?id=${MEASUREMENT_ID}`;
  document.head.appendChild(script);
}

function trackPageView() {
  if (readConsent() !== "granted" || !window.gtag) return;
  window.gtag("event", "page_view", {
    page_title: document.title,
    page_location: window.location.href,
    page_path: `${window.location.pathname}${window.location.search}`,
  });
}

export function GoogleAnalytics() {
  React.useEffect(() => {
    if (readConsent() === "granted") {
      loadAnalytics();
      trackPageView();
    }

    const onRouteChange = () => trackPageView();
    window.addEventListener("systemcel:route-change", onRouteChange);
    window.addEventListener("popstate", onRouteChange);
    return () => {
      window.removeEventListener("systemcel:route-change", onRouteChange);
      window.removeEventListener("popstate", onRouteChange);
    };
  }, []);

  return null;
}

export function AnalyticsConsentBanner() {
  const [consent, setConsent] = React.useState<AnalyticsConsent | null>(() => readConsent());

  const chooseConsent = (value: AnalyticsConsent) => {
    window.localStorage.setItem(CONSENT_KEY, value);
    setConsent(value);
    if (value === "granted") {
      loadAnalytics();
      trackPageView();
    }
  };

  if (consent) return null;

  const english = window.localStorage.getItem("systemcel.language") === "en";
  return (
    <aside className="analytics-consent" aria-label={english ? "Cookie and analytics preference" : "Çerez ve analiz tercihi"}>
      <div>
        <strong>{english ? "Cookies and analytics" : "Çerezler ve analiz"}</strong>
        <p>{english ? "We would like to use analytics cookies and Google Analytics to understand how the site is used. They are enabled only with your permission." : "Siteyi nasıl kullandığınızı anlamak için analiz çerezleri ve Google Analytics kullanmak istiyoruz. Yalnızca izin verirseniz etkinleşir."}</p>
      </div>
      <div className="analytics-consent__actions">
        <button type="button" className="analytics-consent__reject" onClick={() => chooseConsent("denied")}>{english ? "Decline" : "Reddet"}</button>
        <button type="button" className="analytics-consent__accept" onClick={() => chooseConsent("granted")}>{english ? "Allow cookies" : "Çerezlere izin ver"}</button>
      </div>
    </aside>
  );
}
