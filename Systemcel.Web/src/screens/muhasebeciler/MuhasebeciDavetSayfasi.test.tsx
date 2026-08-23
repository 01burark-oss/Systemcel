import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebeciDavetSayfasi } from "./MuhasebeciDavetSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));
vi.mock("../../auth/SystemcelAuthProvider", () => ({
  useSystemcelAuth: () => ({ clerkEnabled: false, isLoaded: true, isSignedIn: true })
}));

describe("MuhasebeciDavetSayfasi", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (String(url).startsWith("/api/public/muhasebeci-davetleri/")) {
        return {
          musteriAdi: "Bahar Kafe",
          durum: "Beklemede",
          yetkiSeviyesi: "OkumaRapor",
          mesaj: "Aylık kayıtları birlikte yönetelim.",
          sonGecerlilikAt: "2026-09-01T00:00:00Z"
        } as never;
      }
      return { durum: "OdemeBekliyor" } as never;
    });
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("daveti bağlantı kurmadan aylık ücretle ödemeye gönderir", async () => {
    const user = userEvent.setup();
    render(<MuhasebeciDavetSayfasi token="davet-tokeni" />);

    await user.type(await screen.findByRole("spinbutton", { name: "Aylık ücret" }), "2400");
    await user.click(screen.getByRole("button", { name: "Ödemeye gönder" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/muhasebeci/link-davetleri/kabul",
      {
        method: "POST",
        body: JSON.stringify({ token: "davet-tokeni", aylikHizmetBedeli: 2400 })
      }
    ));
    expect(await screen.findByRole("heading", { name: "Müşteri ödemesi bekleniyor" })).toBeVisible();
    expect(screen.queryByText("Bağlantı kuruldu")).not.toBeInTheDocument();
  });
});
