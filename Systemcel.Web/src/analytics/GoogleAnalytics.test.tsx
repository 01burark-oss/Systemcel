import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { AnalyticsConsentBanner, GoogleAnalytics } from "./GoogleAnalytics";

const CONSENT_KEY = "systemcel.analyticsConsent";
const SCRIPT_ID = "systemcel-google-analytics";

describe("Google Analytics consent", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.dataLayer = [];
    delete window.gtag;
    document.getElementById(SCRIPT_ID)?.remove();
  });

  afterEach(() => {
    cleanup();
    document.getElementById(SCRIPT_ID)?.remove();
    delete window.gtag;
  });

  it("does not load analytics before consent and keeps it disabled after rejection", async () => {
    const user = userEvent.setup();
    render(<><GoogleAnalytics /><AnalyticsConsentBanner /></>);

    expect(document.getElementById(SCRIPT_ID)).toBeNull();
    await user.click(screen.getByRole("button", { name: "Reddet" }));

    expect(window.localStorage.getItem(CONSENT_KEY)).toBe("denied");
    expect(document.getElementById(SCRIPT_ID)).toBeNull();
    expect(screen.queryByRole("complementary", { name: "Çerez ve analiz tercihi" })).not.toBeInTheDocument();
  });

  it("loads analytics and records a page view only after approval", async () => {
    const user = userEvent.setup();
    render(<><GoogleAnalytics /><AnalyticsConsentBanner /></>);

    await user.click(screen.getByRole("button", { name: "Çerezlere izin ver" }));

    const script = document.getElementById(SCRIPT_ID) as HTMLScriptElement | null;
    expect(window.localStorage.getItem(CONSENT_KEY)).toBe("granted");
    expect(script?.src).toContain("googletagmanager.com/gtag/js?id=G-825YG8XTJK");
    expect(window.dataLayer.some((entry) => Array.isArray(entry) && entry[0] === "event" && entry[1] === "page_view")).toBe(true);
  });
});
