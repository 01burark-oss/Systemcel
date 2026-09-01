import React from "react";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ThemeProvider, useTheme } from "./ThemeProvider";

function ThemeProbe() {
  const { theme, toggleTheme } = useTheme();

  return (
    <button type="button" onClick={toggleTheme}>
      {theme}
    </button>
  );
}

function sistemTemasiniAyarla(dark: boolean) {
  vi.stubGlobal("matchMedia", vi.fn((query: string) => ({
    matches: query === "(prefers-color-scheme: dark)" && dark,
    media: query,
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn()
  } satisfies MediaQueryList)));
}

function temaRengiMetasiEkle() {
  const meta = document.createElement("meta");
  meta.name = "theme-color";
  meta.content = "#f2f0e7";
  document.head.append(meta);
  return meta;
}

describe("ThemeProvider", () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
    window.localStorage.clear();
    delete document.documentElement.dataset.theme;
    document.documentElement.style.removeProperty("color-scheme");
    document.querySelectorAll('meta[name="theme-color"]').forEach((meta) => meta.remove());
  });

  it("kayıtlı temayı sistem tercihinin önünde kullanır ve tarayıcı yüzeyine uygular", () => {
    sistemTemasiniAyarla(false);
    window.localStorage.setItem("systemcel.theme", "dark");
    const meta = temaRengiMetasiEkle();

    render(<ThemeProvider><ThemeProbe /></ThemeProvider>);

    expect(screen.getByRole("button", { name: "dark" })).toBeVisible();
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    expect(document.documentElement.style.colorScheme).toBe("dark");
    expect(meta).toHaveAttribute("content", "#11120e");
  });

  it("kayıtlı geçerli tercih yoksa işletim sisteminin koyu temasını kullanır", () => {
    sistemTemasiniAyarla(true);
    window.localStorage.setItem("systemcel.theme", "gecersiz");

    render(<ThemeProvider><ThemeProbe /></ThemeProvider>);

    expect(screen.getByRole("button", { name: "dark" })).toBeVisible();
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    expect(window.localStorage.getItem("systemcel.theme")).toBe("dark");
  });

  it("tema değişikliğini belgeye ve kalıcı tercihe birlikte yansıtır", async () => {
    sistemTemasiniAyarla(false);
    const user = userEvent.setup();
    const meta = temaRengiMetasiEkle();
    render(<ThemeProvider><ThemeProbe /></ThemeProvider>);

    await user.click(screen.getByRole("button", { name: "light" }));

    expect(screen.getByRole("button", { name: "dark" })).toBeVisible();
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    expect(document.documentElement.style.colorScheme).toBe("dark");
    expect(window.localStorage.getItem("systemcel.theme")).toBe("dark");
    expect(meta).toHaveAttribute("content", "#11120e");
  });

  it("başka sekmeden gelen geçerli tema değişikliğini eşitler", async () => {
    sistemTemasiniAyarla(false);
    render(<ThemeProvider><ThemeProbe /></ThemeProvider>);

    act(() => {
      window.dispatchEvent(new StorageEvent("storage", {
        key: "systemcel.theme",
        newValue: "dark"
      }));
    });

    await waitFor(() => expect(screen.getByRole("button", { name: "dark" })).toBeVisible());
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
  });
});
