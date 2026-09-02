import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ThemeProvider } from "../../theme/ThemeProvider";
import { GorunumAyari } from "./AyarlarSayfasi";

describe("Görünüm ayarı", () => {
  beforeEach(() => {
    window.localStorage.clear();
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      value: vi.fn().mockReturnValue({ matches: false })
    });
  });

  afterEach(() => {
    cleanup();
    window.localStorage.clear();
    delete document.documentElement.dataset.theme;
    document.documentElement.style.colorScheme = "";
  });

  it("açık ve koyu temayı klavyeyle seçer, durumunu ve tercihi senkron günceller", async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <GorunumAyari />
      </ThemeProvider>
    );

    const control = screen.getByRole("switch", { name: "Tema seçimi: Koyu" });
    expect(control).toBeVisible();
    expect(control).not.toBeChecked();

    await user.tab();
    expect(control).toHaveFocus();
    await user.keyboard("[Space]");

    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    expect(window.localStorage.getItem("systemcel.theme")).toBe("dark");
    expect(control).toBeChecked();

    await user.click(control);
    expect(document.documentElement).toHaveAttribute("data-theme", "light");
    expect(window.localStorage.getItem("systemcel.theme")).toBe("light");
  });
});
