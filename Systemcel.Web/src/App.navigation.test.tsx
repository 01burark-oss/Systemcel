import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "./App";

vi.mock("./auth/AuthGate", () => ({ RequireAuth: () => <h1>Workspace</h1> }));
vi.mock("./marketing/PublicContentPage", () => ({ PublicContentPage: () => <main className="marketing-page"><h1>Public content</h1></main> }));
vi.mock("./marketing/KaynakIndirmeSayfasi", () => ({ KaynakIndirmeSayfasi: () => <h1>Resource</h1> }));
vi.mock("./screens/help/YardimSayfasi", () => ({ YardimSayfasi: () => <h1>Help</h1> }));
vi.mock("./screens/welcome/WelcomeSayfasi", () => ({ WelcomeSayfasi: () => <h1>Welcome</h1> }));
vi.mock("./shared/json", () => ({ jsonOku: vi.fn().mockResolvedValue({}) }));

describe("client navigation", () => {
  beforeEach(() => {
    window.history.replaceState(null, "", "/blog");
    vi.spyOn(window, "scrollTo").mockImplementation(() => undefined);
  });
  afterEach(() => { cleanup(); vi.restoreAllMocks(); });

  it.each(["/yardım", "/hakkımızda", "/kaynaklar/ai", "/abonelik-kosullari", "/abonelik-koşulları"])("opens %s without a document reload", async (href) => {
    render(<><App /><a href={href}>Navigate</a></>);
    await screen.findByRole("heading", { name: "Public content" });
    const prevented = !fireEvent.click(screen.getByRole("link", { name: "Navigate" }));
    expect(prevented).toBe(true);
    expect(decodeURI(window.location.pathname)).toBe(href);
  });

  it("does not crash on a malformed URL escape", async () => {
    window.history.replaceState(null, "", "/%E0%A4%A");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Workspace" })).toBeVisible();
  });

  it("resets the marketing page scroller after footer navigation", async () => {
    const containerScrollTo = vi.fn();
    const originalScrollTo = Object.getOwnPropertyDescriptor(HTMLElement.prototype, "scrollTo");
    Object.defineProperty(HTMLElement.prototype, "scrollTo", { configurable: true, value: containerScrollTo });

    try {
      render(<><App /><a href="/hakkimizda">Navigate</a></>);
      await screen.findByRole("heading", { name: "Public content" });
      fireEvent.click(screen.getByRole("link", { name: "Navigate" }));

      await waitFor(() => expect(containerScrollTo).toHaveBeenCalledWith({ left: 0, top: 0 }));
    } finally {
      if (originalScrollTo) {
        Object.defineProperty(HTMLElement.prototype, "scrollTo", originalScrollTo);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, "scrollTo");
      }
    }
  });
});
