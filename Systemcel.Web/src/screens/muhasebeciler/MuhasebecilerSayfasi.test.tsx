import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebecilerSayfasi } from "./MuhasebecilerSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

describe("MuhasebecilerSayfasi", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/app/muhasebeciler");
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/ekran/muhasebeci/link-davetleri") {
        return {
          musteriAdi: "Bahar Kafe",
          durum: "Beklemede",
          yetkiSeviyesi: "TamIslem",
          mesaj: "Aylık belgeleri birlikte yönetelim.",
          davetLinki: "https://systemcel.test/muhasebeci-daveti/1234567890abcdef1234567890abcdef1234567890abcdef",
          sonGecerlilikAt: "2026-09-06T04:00:00"
        } as never;
      }

      return { mesaj: "", profiller: [] } as never;
    });
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("davet kodu kabul çubuğu yerine anlaşma adımında yetkili bağlantı üretir", async () => {
    const user = userEvent.setup();
    render(<MuhasebecilerSayfasi ustBar={{ hesapTipi: "Isletme" } as never} />);

    expect(await screen.findByRole("heading", { name: "Muhasebeciler" })).toBeVisible();
    expect(screen.queryByText("Davet kabul et")).not.toBeInTheDocument();
    expect(screen.queryByRole("group", { name: "Yetki seviyesi" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Muhasebecini davet et" }));
    expect(screen.getByRole("dialog", { name: "Muhasebecini davet et" })).toBeVisible();
    expect(screen.getByRole("group", { name: "Yetki seviyesi" })).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Tam işlem" }));
    await user.type(screen.getByRole("textbox", { name: "Not (isteğe bağlı)" }), "Aylık belgeleri birlikte yönetelim.");
    await user.click(screen.getByRole("button", { name: "Davet bağlantısı oluştur" }));

    expect(await screen.findByDisplayValue(/muhasebeci-daveti\/123456/)).toBeVisible();
    expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/muhasebeci/link-davetleri",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          yetkiSeviyesi: "TamIslem",
          mesaj: "Aylık belgeleri birlikte yönetelim."
        })
      })
    );
  });
});
