import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { BildirimTeslimYonetimSayfasi } from "./BildirimTeslimYonetimSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

afterEach(() => { cleanup(); vi.clearAllMocks(); });

describe("BildirimTeslimYonetimSayfasi", () => {
  it("lists failed deliveries and queues a selected retry", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => init?.method === "POST" ? null as never : [{
      id: 17, isletmeId: 4, kanal: "Eposta", durum: "DeadLetter", denemeSayisi: 5,
      sonHataKodu: "smtp_unavailable", updatedAt: "2026-09-23T08:00:00Z"
    }] as never);
    render(<BildirimTeslimYonetimSayfasi />);

    expect(await screen.findByText("smtp_unavailable")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Yeniden dene" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/yonetim/bildirim-teslimleri/17/yeniden-dene", { method: "POST" }
    ));
    expect(screen.getByRole("status")).toHaveTextContent("Bildirim yeniden gönderim sırasına alındı.");
    expect(screen.queryByText("smtp_unavailable")).not.toBeInTheDocument();
  });

  it("keeps a failed delivery visible when retry is rejected", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => {
      if (init?.method === "POST") throw new Error("Alıcı bu bildirim kanalını kapattı.");
      return [{ id: 17, isletmeId: 4, kanal: "Eposta", durum: "DeadLetter", denemeSayisi: 5,
        sonHataKodu: "smtp_unavailable", updatedAt: "2026-09-23T08:00:00Z" }] as never;
    });
    render(<BildirimTeslimYonetimSayfasi />);
    await user.click(await screen.findByRole("button", { name: "Yeniden dene" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Alıcı bu bildirim kanalını kapattı.");
    expect(screen.getByText("smtp_unavailable")).toBeVisible();
  });
});
