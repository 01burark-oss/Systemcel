import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "./json";
import { ReactWorkspaceShell } from "./ReactWorkspaceShell";
import type { UstBarDurumu } from "./chrome";

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

describe("ReactWorkspaceShell bildirim merkezi", () => {
  afterEach(() => { cleanup(); vi.clearAllMocks(); });

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

    render(<ReactWorkspaceShell ustBar={ustBar}><div>İçerik</div></ReactWorkspaceShell>);
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
    const { rerender } = render(<ReactWorkspaceShell ustBar={{ ...ustBar, bankaMutabakatiAktif: false }}><div>İçerik</div></ReactWorkspaceShell>);
    expect(screen.queryByRole("link", { name: "Banka eşleştirme" })).not.toBeInTheDocument();

    rerender(<ReactWorkspaceShell ustBar={{ ...ustBar, bankaMutabakatiAktif: true }}><div>İçerik</div></ReactWorkspaceShell>);
    expect(screen.getByRole("link", { name: "Banka eşleştirme" })).toHaveAttribute("href", "/app/banka-eslestirme");
  });
});
