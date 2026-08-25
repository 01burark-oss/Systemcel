import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { SubeKurPaneli } from "./SubeKurPaneli";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const durum = {
  aktifSube: { id: 1, ad: "Merkez", kod: "MERKEZ", varsayilan: true, aktif: true },
  subeler: [{ id: 1, ad: "Merkez", kod: "MERKEZ", varsayilan: true, aktif: true }],
  kurlar: [{ paraBirimi: "USD", kur: 34.5, gecerliAt: "2026-08-24T08:00:00Z" }],
  cokluSubeAktif: true,
  cokluParaBirimiAktif: true
};

describe("Şube ve kur ayarları", () => {
  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it("şube ve manuel kur kayıtlarını idempotency anahtarıyla gönderir", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/sube-kur/" && !init) return durum as never;
      if (url === "/api/ekran/sube-kur/finans-ozeti" && !init) return { subeId: null, konsolide: true, gelirTry: 3450, giderTry: 1200, netTry: 2250, paraBirimleri: [{ paraBirimi: "USD", gelirOrijinal: 100, giderOrijinal: 0, gelirTry: 3450, giderTry: 0 }] } as never;
      if (url === "/api/ekran/sube-kur/finans-ozeti?subeId=1" && !init) return { subeId: 1, konsolide: false, gelirTry: 3450, giderTry: 1200, netTry: 2250, paraBirimleri: [] } as never;
      if (url === "/api/ekran/sube-kur/subeler" && init?.method === "POST") return durum as never;
      if (url === "/api/ekran/sube-kur/kurlar" && init?.method === "POST") return durum as never;
      throw new Error(`Beklenmeyen istek: ${url}`);
    });

    render(<SubeKurPaneli />);
    expect((await screen.findAllByText("Merkez"))[0]).toBeVisible();
    expect(screen.getByText(/dış kur servisi kullanılmaz/i)).toBeVisible();
    expect(await screen.findByText("₺2.250,00")).toBeVisible();
    await user.selectOptions(screen.getByRole("combobox", { name: "Özet şubesi" }), "1");
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/sube-kur/finans-ozeti?subeId=1"));

    await user.type(screen.getByLabelText("Şube adı"), "Kadıköy");
    await user.type(screen.getByLabelText("Şube kodu"), "KAD");
    await user.click(screen.getByRole("button", { name: "Şube ekle" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/sube-kur/subeler", expect.objectContaining({
      method: "POST",
      body: JSON.stringify({ ad: "Kadıköy", kod: "KAD" }),
      headers: expect.objectContaining({ "Idempotency-Key": expect.any(String) })
    })));

    await user.clear(screen.getByLabelText("Para birimi"));
    await user.type(screen.getByLabelText("Para birimi"), "EUR");
    await user.type(screen.getByLabelText("TRY kuru"), "37.25");
    await user.click(screen.getByRole("button", { name: "Kuru kaydet" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/sube-kur/kurlar", expect.objectContaining({
      method: "POST",
      body: JSON.stringify({ paraBirimi: "EUR", kur: 37.25 }),
      headers: expect.objectContaining({ "Idempotency-Key": expect.any(String) })
    })));
  });

  it("plan hakkı kapalıysa ekleme alanları yerine yükseltme durumunu gösterir", async () => {
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/ekran/sube-kur/") return { ...durum, cokluSubeAktif: false, cokluParaBirimiAktif: false } as never;
      return { subeId: null, konsolide: true, gelirTry: 0, giderTry: 0, netTry: 0, paraBirimleri: [] } as never;
    });
    render(<SubeKurPaneli />);

    expect((await screen.findAllByText("Merkez"))[0]).toBeVisible();
    expect(screen.getAllByRole("link", { name: "Planları gör" })).toHaveLength(2);
    expect(screen.queryByLabelText("Şube adı")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("TRY kuru")).not.toBeInTheDocument();
  });
});
