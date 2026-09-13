import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebeciBaglantiSayfasi } from "./MuhasebeciBaglantiSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("MuhasebeciBaglantiSayfasi", () => {
  it("işletmenin seçtiği yetkiyle muhasebeci daveti oluşturur", async () => {
    vi.mocked(jsonOku).mockResolvedValue({ davetLinki: "https://systemcel.test/muhasebeci-daveti/abc" } as never);
    const user = userEvent.setup();

    render(<MuhasebeciBaglantiSayfasi ustBar={{ hesapTipi: "Isletme" } as never} />);
    await user.click(screen.getByRole("button", { name: "Davet oluştur" }));
    await user.click(screen.getByRole("button", { name: "Tam işlem" }));
    await user.type(screen.getByRole("textbox", { name: "Not (isteğe bağlı)" }), "Eylül kayıtlarını paylaşalım.");
    await user.click(screen.getByRole("button", { name: "Davet bağlantısı oluştur" }));

    expect(await screen.findByDisplayValue(/muhasebeci-daveti\/abc/)).toBeVisible();
    expect(jsonOku).toHaveBeenCalledWith("/api/ekran/muhasebeci/link-davetleri", {
      method: "POST",
      body: JSON.stringify({ yetkiSeviyesi: "TamIslem", mesaj: "Eylül kayıtlarını paylaşalım." })
    });
  });

  it("muhasebeciyi mevcut müşteri davetleri ekranına yönlendirir", () => {
    render(<MuhasebeciBaglantiSayfasi ustBar={{ hesapTipi: "Muhasebeci", muhasebeciMusteriBaglami: false } as never} />);

    expect(screen.getByRole("heading", { name: "Müşterilerini bağla" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Müşterilerime git" })).toHaveAttribute("href", "/app/muhasebeci/musteriler");
  });
});
