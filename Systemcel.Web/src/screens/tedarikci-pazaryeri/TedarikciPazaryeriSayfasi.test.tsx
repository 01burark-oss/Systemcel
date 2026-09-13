import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { TedarikciPazaryeriSayfasi } from "./TedarikciPazaryeriSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

afterEach(() => { cleanup(); vi.clearAllMocks(); });

describe("TedarikciPazaryeriSayfasi", () => {
  it("yayındaki tedarikçileri gösterir, arar ve alım talebi formunu açar", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      profiller: [
        { id: 1, unvan: "Marmara Gıda", kategoriler: "Gıda", sehir: "İstanbul", aciklama: "Toptan", dogrulandi: true },
        { id: 3, unvan: "Ege Ambalaj", kategoriler: "Ambalaj", sehir: "İzmir", aciklama: "Kutu", dogrulandi: false }
      ], talepler: [], acikTalepler: [], gelenTeklifler: [], profil: null
    } as never);

    const user = userEvent.setup();
    render(<TedarikciPazaryeriSayfasi />);

    expect(await screen.findByText("Marmara Gıda")).toBeVisible();
    expect(screen.getByText("Ege Ambalaj")).toBeVisible();
    await user.type(screen.getByRole("textbox", { name: "Tedarikçi ara" }), "ege");
    expect(screen.queryByText("Marmara Gıda")).not.toBeInTheDocument();
    expect(screen.getByText("Ege Ambalaj")).toBeVisible();
    await user.click(screen.getByRole("button", { name: /Alım talebi oluştur/ }));
    expect(screen.getByRole("dialog", { name: "Alım talebi oluştur" })).toBeVisible();
  });
});
