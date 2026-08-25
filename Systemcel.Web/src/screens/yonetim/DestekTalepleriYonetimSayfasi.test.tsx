import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { DestekTalepleriYonetimSayfasi } from "./DestekTalepleriYonetimSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const talepler = [
  { id: 3, isletmeId: 11, isletmeAdi: "Standart Ltd.", konu: "Eski talep", kategori: "Hesap", aciklama: "Eski kayıt", oncelik: "Standart", durum: "Acik", yoneticiYaniti: "", createdAt: "2026-08-20T10:00:00Z", updatedAt: "2026-08-20T10:00:00Z" },
  { id: 2, isletmeId: 12, isletmeAdi: "Öncelikli AŞ", konu: "Yeni talep", kategori: "Teknik", aciklama: "Yeni kayıt", oncelik: "Oncelikli", durum: "Islemde", yoneticiYaniti: "İnceliyoruz.", createdAt: "2026-08-24T10:00:00Z", updatedAt: "2026-08-24T10:00:00Z" },
  { id: 4, isletmeId: 13, isletmeAdi: "Yakın Ltd.", konu: "Yakın standart talep", kategori: "Faturalama", aciklama: "Yakın kayıt", oncelik: "Standart", durum: "Acik", yoneticiYaniti: "", createdAt: "2026-08-23T10:00:00Z", updatedAt: "2026-08-23T10:00:00Z" }
];

describe("DestekTalepleriYonetimSayfasi", () => {
  beforeEach(() => { vi.mocked(jsonOku).mockResolvedValue({ talepler } as never); });
  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it("önceliği önce gösterir ve yöneticinin durum ile yanıtı kaydetmesini sağlar", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => init?.method === "POST" ? { ...talepler[1], durum: "Cozuldu", yoneticiYaniti: "Sorun giderildi." } as never : { talepler } as never);
    render(<DestekTalepleriYonetimSayfasi />);

    const cells = await screen.findAllByRole("cell");
    const yeniTalepIndex = cells.findIndex((cell) => cell.textContent?.includes("Yeni talep"));
    const eskiTalepIndex = cells.findIndex((cell) => cell.textContent?.includes("Eski talep"));
    const yakinTalepIndex = cells.findIndex((cell) => cell.textContent?.includes("Yakın standart talep"));
    expect(yeniTalepIndex).toBeLessThan(eskiTalepIndex);
    expect(eskiTalepIndex).toBeLessThan(yakinTalepIndex);
    expect(screen.getAllByText("Öncelikli").at(-1)).toBeVisible();
    await user.selectOptions(screen.getByRole("combobox", { name: /Yeni talep durum/ }), "Cozuldu");
    await user.clear(screen.getByRole("textbox", { name: /Yeni talep yanıt/ }));
    await user.type(screen.getByRole("textbox", { name: /Yeni talep yanıt/ }), "Sorun giderildi.");
    await user.click(screen.getAllByRole("button", { name: "Kaydet" })[0]);

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/yonetim/destek/2/guncelle", expect.objectContaining({
      method: "POST",
      body: JSON.stringify({ durum: "Cozuldu", yoneticiYaniti: "Sorun giderildi." })
    })));
    expect(screen.getByText("“Yeni talep” talebi güncellendi.")).toBeVisible();
  });
});
