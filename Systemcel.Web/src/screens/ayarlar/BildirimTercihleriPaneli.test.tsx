import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { BildirimTercihleriPaneli } from "./BildirimTercihleriPaneli";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

describe("BildirimTercihleriPaneli", () => {
  afterEach(() => cleanup());

  it("saves channel and quiet-hour preferences", async () => {
    const user = userEvent.setup();
    const initial = { uygulamaAktif: true, epostaAktif: false, telegramAktif: false, sessizSaatAktif: false, sessizBaslangicDakika: 1320, sessizBitisDakika: 480, saatDilimi: "Europe/Istanbul" as const };
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => init?.method === "PUT" ? JSON.parse(String(init.body)) : initial);
    render(<BildirimTercihleriPaneli />);

    expect(await screen.findByRole("switch", { name: "Uygulama içi" })).toBeChecked();
    const email = screen.getByRole("switch", { name: "E-posta" });
    expect(email).not.toBeChecked();
    email.focus();
    await user.keyboard(" ");
    expect(email).toBeChecked();
    await user.click(screen.getByRole("switch", { name: "Sessiz saatleri kullan" }));
    await user.clear(screen.getByLabelText("Başlangıç"));
    await user.type(screen.getByLabelText("Başlangıç"), "23:30");
    await user.click(screen.getByRole("button", { name: "Tercihleri kaydet" }));

    expect(vi.mocked(jsonOku)).toHaveBeenCalledWith("/api/ekran/bildirim-tercihleri", expect.objectContaining({
      method: "PUT",
      body: expect.stringContaining('"epostaAktif":true')
    }));
    expect(await screen.findByText("Bildirim tercihleri kaydedildi.")).toBeVisible();
  });
});
