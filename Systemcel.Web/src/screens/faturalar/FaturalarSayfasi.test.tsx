import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { FaturalarSayfasi } from "./FaturalarSayfasi";
import type { FaturaEkranVerisi } from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const ekran: FaturaEkranVerisi = {
  aktifIsletme: "Systemcel Test",
  faturalar: [
    { id: 1, no: "SF202600001", tarih: "2026-08-05", vadeTarihi: "", faturaTipi: "Satis", durum: "Kesildi", cariKartId: 10, cariUnvan: "Atlas", genelToplam: 1000, odenenTutar: 0, odemeYontemi: "Havale", aciklama: "" },
    { id: 2, no: "SF202600002", tarih: "2026-08-12", vadeTarihi: "", faturaTipi: "Satis", durum: "Odendi", cariKartId: 11, cariUnvan: "Marmara", genelToplam: 2000, odenenTutar: 2000, odemeYontemi: "Nakit", aciklama: "" }
  ],
  cariler: [{ id: 10, unvan: "Atlas" }, { id: 11, unvan: "Marmara" }],
  urunler: [],
  ozet: { toplamFatura: 3000, faturaAdedi: 2, tahsilEdilen: 2000, bekleyen: 1000, bekleyenAdedi: 1 },
  faturaTipleri: [{ deger: "Satis", etiket: "Satış" }],
  odemeYontemleri: [{ deger: "Nakit", etiket: "Nakit" }, { deger: "Havale", etiket: "Havale" }],
  bugun: "2026-08-22"
};

describe("FaturalarSayfasi toplu seçim", () => {
  beforeEach(() => {
    Object.defineProperty(HTMLElement.prototype, "scrollTo", { configurable: true, value: vi.fn() });
    vi.mocked(jsonOku).mockResolvedValue(ekran);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("selects and clears every visible invoice from the header checkbox", async () => {
    const user = userEvent.setup();
    render(<FaturalarSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    const selectAll = await screen.findByRole("checkbox", { name: "Görünen faturaların tümünü seç" });
    const first = screen.getByRole("checkbox", { name: "SF202600001 faturasını seç" });
    const second = screen.getByRole("checkbox", { name: "SF202600002 faturasını seç" });

    await user.click(selectAll);
    expect(first).toBeChecked();
    expect(second).toBeChecked();
    expect(selectAll).toBeChecked();

    await user.click(first);
    expect(first).not.toBeChecked();
    expect(second).toBeChecked();
    expect(selectAll).not.toBeChecked();
    expect((selectAll as HTMLInputElement).indeterminate).toBe(true);

    await user.click(selectAll);
    expect(first).toBeChecked();
    expect(second).toBeChecked();

    await user.click(selectAll);
    expect(first).not.toBeChecked();
    expect(second).not.toBeChecked();
  });
});
