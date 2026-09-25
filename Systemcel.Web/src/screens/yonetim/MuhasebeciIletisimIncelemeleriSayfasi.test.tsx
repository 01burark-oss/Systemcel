import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebeciIletisimIncelemeleriSayfasi } from "./MuhasebeciIletisimIncelemeleriSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

afterEach(() => { cleanup(); vi.clearAllMocks(); });

describe("MuhasebeciIletisimIncelemeleriSayfasi", () => {
  it("lists a queued request and releases it when the match is uncertain", async () => {
    const user = userEvent.setup();
    const item = { id: 31, muhasebeciAdi: "Ada Muhasebe", musteriAdi: "Bahar Kafe", mesaj: "2026 raporu ve 500.000 TL gelir", createdAt: "2026-09-24T08:00:00Z" };
    let queueLoads = 0;
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => {
      if (init?.method === "POST") return null as never;
      queueLoads += 1;
      return queueLoads === 1 ? [item] as never : [] as never;
    });
    render(<MuhasebeciIletisimIncelemeleriSayfasi />);

    expect(await screen.findByText("Bahar Kafe")).toBeVisible();
    expect(screen.getByText(item.mesaj)).toBeVisible();
    await user.click(screen.getByRole("button", { name: "İletişim bilgisi yok" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/yonetim/muhasebeci-iletisim-incelemeleri/31/karar",
      { method: "POST", body: JSON.stringify({ iletisimBilgisiVar: false }) }
    ));
    expect(await screen.findByRole("status")).toHaveTextContent("Talep inceleme için serbest bırakıldı.");
    expect(await screen.findByText("İncelenecek talep yok.")).toBeVisible();
  });
});
