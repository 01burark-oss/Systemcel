import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { I18nProvider, formatMoney, setAppLanguage, useI18n } from "./i18n";

function Sample() {
  const { language, t } = useI18n();
  return <p>{language}:{t("nav.settings")}</p>;
}

describe("i18n çekirdeği", () => {
  afterEach(() => { cleanup(); window.localStorage.clear(); document.documentElement.lang = "tr"; });

  it("saklanan EN tercihini uygular, html lang'i günceller ve Türkçe fallback'i korur", () => {
    window.localStorage.setItem("systemcel.language", "en");
    render(<I18nProvider><Sample /></I18nProvider>);
    expect(screen.getByText("en:Settings")).toBeVisible();
    expect(document.documentElement).toHaveAttribute("lang", "en");
    expect(formatMoney(12.5, "TRY", "en")).toContain("12.50");
  });

  it("DE tercihini uygulama içinde günceller", async () => {
    render(<I18nProvider><Sample /></I18nProvider>);
    setAppLanguage("de");
    await waitFor(() => expect(screen.getByText("de:Einstellungen")).toBeVisible());
    await waitFor(() => expect(document.documentElement).toHaveAttribute("lang", "de"));
  });
});
