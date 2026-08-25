import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebeciAktarimlariSayfasi } from "./MuhasebeciAktarimlariSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

describe("MuhasebeciAktarimlariSayfasi", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => {
      if (init?.method === "POST") return { durum: "Aktarildi" } as never;
      return {
        yoneticiMi: true,
        aktarimDonemi: "2026-08",
        aktarimlar: [{
          muhasebeciIsletmeId: 12, muhasebeciAdi: "Ada Muhasebe", aktarimDonemi: "2026-08",
          paraBirimi: "TRY", alacakSayisi: 2, tahsilEdilenTutar: 3000,
          platformKomisyonTutari: 300, aktarilacakTutar: 2700, durum: "Bekliyor", aktarimReferansi: ""
        }]
      } as never;
    });
  });

  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it("banka transferi yapmadığını açıklar ve manuel referansı kaydeder", async () => {
    const user = userEvent.setup();
    render(<MuhasebeciAktarimlariSayfasi/>);

    expect(await screen.findByText("Ada Muhasebe")).toBeVisible();
    expect(screen.getByText(/Bu ekran banka transferi yapmaz/)).toBeVisible();
    expect(screen.getAllByText(/₺2.700,00/)).toHaveLength(2);
    await user.type(screen.getByRole("textbox", { name: "Ada Muhasebe transfer referansı" }), "bank-ref-001");
    await user.click(screen.getByRole("button", { name: "Kaydet" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/yonetim/muhasebeci-aktarimlari/12/tamamla",
      expect.objectContaining({ method: "POST", body: expect.stringContaining("bank-ref-001") })
    ));
    expect(await screen.findByRole("status")).toHaveTextContent("Ada Muhasebe için");
  });
});
