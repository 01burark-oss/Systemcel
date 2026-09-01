import { act, cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "./json";
import { ReactWorkspaceShell } from "./ReactWorkspaceShell";
import type { UstBarDurumu } from "./chrome";
import { ThemeProvider } from "../theme/ThemeProvider";

vi.mock("./json", () => ({ jsonOku: vi.fn() }));
vi.mock("../auth/AuthUserButton", () => ({ AuthUserButton: () => null }));
vi.mock("./AiAssistantPanel", () => ({ AiAssistantPanel: () => null }));

const ustBar: UstBarDurumu = {
  aktifIsletmeId: 7,
  aktifIsletme: "Test işletmesi",
  hesapTipi: "Isletme",
  muhasebeciMusteriBaglami: false,
  muhasebeciAdi: "",
  muhasebeciYetkiSeviyesi: "TamIslem",
  bildirimVar: true,
  bildirimSayisi: 1,
  telegramAktif: false,
  isletmeler: []
};

function shell(ustBarDegeri: UstBarDurumu = ustBar, children: React.ReactNode = <div>İçerik</div>) {
  return (
    <ThemeProvider>
      <ReactWorkspaceShell ustBar={ustBarDegeri}>{children}</ReactWorkspaceShell>
    </ThemeProvider>
  );
}

describe("ReactWorkspaceShell bildirim merkezi", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockResolvedValue({
      aktifSube: { id: 1, ad: "Merkez", kod: "MRK", aktif: true, varsayilan: true },
      subeler: [{ id: 1, ad: "Merkez", kod: "MRK", aktif: true, varsayilan: true }],
      kurlar: [],
      cokluSubeAktif: false,
      cokluParaBirimiAktif: false
    } as never);
  });

  afterEach(() => {
    cleanup();
    vi.resetAllMocks();
    window.localStorage.clear();
    delete document.documentElement.dataset.theme;
    document.documentElement.style.removeProperty("color-scheme");
  });

  it("lists unread state and marks every notification as read", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/bildirimler" && !init) return [{
        id: 41,
        kaynakAnahtari: "geciken-odeme-9",
        tur: "odeme",
        onem: "yuksek",
        baslik: "Ödeme gecikti",
        mesaj: "Vadesi geçen ödeme var.",
        aksiyon: "Ödemeyi kapat",
        url: "",
        okundu: false,
        createdAt: "2026-08-24T00:00:00Z"
      }];
      if (url === "/api/ekran/bildirimler/tumunu-okundu" && init?.method === "POST") return { okunmamisSayisi: 0 };
      throw new Error(`Unexpected request: ${url}`);
    });

    render(shell());
    await user.click(screen.getByRole("button", { name: "Bildirimleri göster" }));

    expect(await screen.findByText("Ödeme gecikti")).toBeVisible();
    expect(screen.getByText("Okunmadı")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Tümünü okundu işaretle" }));

    await waitFor(() => expect(screen.queryByText("Okunmadı")).not.toBeInTheDocument());
    expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(
      "/api/ekran/bildirimler/tumunu-okundu",
      { method: "POST" }
    );
  });

  it("banka eşleştirmeyi yalnız hak aktifken menüde gösterir", () => {
    const { rerender } = render(shell({ ...ustBar, bankaMutabakatiAktif: false }));
    expect(screen.queryByRole("link", { name: "Banka eşleştirme" })).not.toBeInTheDocument();

    rerender(shell({ ...ustBar, bankaMutabakatiAktif: true }));
    expect(screen.getByRole("link", { name: "Banka eşleştirme" })).toHaveAttribute("href", "/app/banka-eslestirme");
  });

  it("tema düğmesi uygulama akışını bozmadan koyu temayı kalıcılaştırır", async () => {
    const user = userEvent.setup();
    window.localStorage.setItem("systemcel.theme", "light");
    render(shell());

    const temaDugmesi = screen.getByRole("button", { name: "Koyu temaya geç" });
    expect(temaDugmesi).toHaveAttribute("aria-pressed", "false");

    await user.click(temaDugmesi);

    expect(screen.getByRole("button", { name: "Açık temaya geç" })).toHaveAttribute("aria-pressed", "true");
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    expect(document.documentElement.style.colorScheme).toBe("dark");
    expect(window.localStorage.getItem("systemcel.theme")).toBe("dark");
    expect(screen.getByText("İçerik")).toBeVisible();
  });

  it("plan uyarısından uygulama akışını bozmayan kapanabilir fiyat penceresini açar", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockRejectedValue(new Error("Plan servisi geçici olarak erişilemiyor"));
    render(shell(ustBar, <button type="button">Kısıtlı özellik</button>));

    const tetikleyici = screen.getByRole("button", { name: "Kısıtlı özellik" });
    tetikleyici.focus();

    act(() => {
      window.dispatchEvent(new CustomEvent("systemcel:entitlement", {
        detail: {
          code: "feature_not_available",
          detail: "Bu özellik mevcut planınızda kullanılamaz.",
          suggestedPlanCode: "isletme_buyume"
        }
      }));
    });

    expect(screen.getByRole("dialog", { name: "Plan yükseltmesi gerekiyor" })).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Planları incele" }));

    expect(screen.queryByRole("dialog", { name: "Plan yükseltmesi gerekiyor" })).not.toBeInTheDocument();
    const planPenceresi = screen.getByRole("dialog", { name: "Planını seç" });
    expect(planPenceresi).toBeVisible();
    const kapat = screen.getByRole("button", { name: "Plan penceresini kapat" });
    await waitFor(() => expect(kapat).toHaveFocus());
    expect(screen.getByRole("button", { name: "İşletmeler" })).toHaveAttribute("aria-pressed", "true");
    const buyumePlani = within(planPenceresi).getByRole("link", { name: "Planı incele: Büyüme planı" });
    expect(buyumePlani).toHaveAttribute("href", "/app/abonelik?plan=isletme_buyume&billing=Aylik");

    fireEvent.keyDown(kapat, { key: "Tab", shiftKey: true });
    const sonPlanBaglantisi = within(planPenceresi).getByRole("link", { name: "Planı incele: Kurumsal planı" });
    expect(sonPlanBaglantisi).toHaveFocus();
    fireEvent.keyDown(sonPlanBaglantisi, { key: "Tab" });
    expect(kapat).toHaveFocus();

    await user.click(kapat);
    expect(planPenceresi).not.toBeInTheDocument();
    await waitFor(() => expect(tetikleyici).toHaveFocus());
  });
});
