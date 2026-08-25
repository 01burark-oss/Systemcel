import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "./json";
import { AktifSubeSecici } from "./AktifSubeSecici";

vi.mock("./json", () => ({ jsonOku: vi.fn() }));

describe("Aktif şube seçici", () => {
  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it("aktif şubeyi ortak üst bardan değiştirir", async () => {
    const user = userEvent.setup();
    const merkez = { id: 1, ad: "Merkez", kod: "MERKEZ", varsayilan: true, aktif: true };
    const kadikoy = { id: 2, ad: "Kadıköy", kod: "KAD", varsayilan: false, aktif: true };
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/sube-kur/" && !init) return { aktifSube: merkez, subeler: [merkez, kadikoy], kurlar: [], cokluSubeAktif: true, cokluParaBirimiAktif: true } as never;
      if (url === "/api/ekran/sube-kur/aktif-sube" && init?.method === "POST") return { aktifSube: kadikoy, subeler: [merkez, kadikoy], kurlar: [], cokluSubeAktif: true, cokluParaBirimiAktif: true } as never;
      throw new Error(`Beklenmeyen istek: ${url}`);
    });

    render(<AktifSubeSecici />);
    const select = await screen.findByRole("combobox", { name: "Aktif şube" });
    await user.selectOptions(select, "2");

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/sube-kur/aktif-sube", {
      method: "POST",
      body: JSON.stringify({ subeId: 2 })
    }));
    expect(select).toHaveValue("2");
  });
});
