import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { YardimSayfasi } from "./YardimSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const yeniTalep = {
  id: 41, isletmeId: 8, isletmeAdi: "Örnek İşletme", konu: "Fatura taslağı", kategori: "Teknik", aciklama: "Kaydet düğmesi çalışmıyor.", oncelik: "Standart", durum: "Acik", yoneticiYaniti: "", createdAt: "2026-08-24T09:30:00Z", updatedAt: "2026-08-24T09:30:00Z"
};

describe("YardimSayfasi destek talepleri", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockResolvedValue({ talepler: [] } as never);
  });

  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it("talebi yalnız izin verilen alanlarla ve idempotency anahtarıyla oluşturur", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => init?.method === "POST" ? yeniTalep as never : { talepler: [] } as never);
    render(<YardimSayfasi />);

    await user.type(await screen.findByRole("textbox", { name: "Konu" }), "Fatura taslağı");
    await user.selectOptions(screen.getByRole("combobox", { name: "Kategori" }), "Teknik");
    await user.type(screen.getByRole("textbox", { name: "Açıklama" }), "Kaydet düğmesi çalışmıyor.");
    await user.click(screen.getByRole("button", { name: "Talep oluştur" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/destek-talepleri", expect.objectContaining({
      method: "POST",
      headers: expect.objectContaining({ "Idempotency-Key": expect.any(String) })
    })));
    const post = vi.mocked(jsonOku).mock.calls.find(([, init]) => init?.method === "POST");
    expect(JSON.parse(post?.[1]?.body as string)).toEqual({ konu: "Fatura taslağı", kategori: "Teknik", aciklama: "Kaydet düğmesi çalışmıyor." });
    expect(screen.getByText("Talebin kaydedildi. Durumu ve yanıtı burada takip edebilirsin.")).toBeVisible();
    expect(screen.getByText("Açık")).toBeVisible();
  }, 10_000);

  it("başarısız denemeden sonra aynı idempotency anahtarıyla yeniden dener", async () => {
    const user = userEvent.setup();
    let postSayisi = 0;
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => {
      if (init?.method !== "POST") return { talepler: [] } as never;
      postSayisi += 1;
      if (postSayisi === 1) throw new Error("Bağlantı kesildi.");
      return yeniTalep as never;
    });
    render(<YardimSayfasi />);

    await user.type(await screen.findByRole("textbox", { name: "Konu" }), "Fatura taslağı");
    await user.type(screen.getByRole("textbox", { name: "Açıklama" }), "Kaydet düğmesi çalışmıyor.");
    await user.click(screen.getByRole("button", { name: "Talep oluştur" }));
    await screen.findByRole("alert");
    await user.click(screen.getByRole("button", { name: "Talep oluştur" }));

    await waitFor(() => expect(postSayisi).toBe(2));
    const postlar = vi.mocked(jsonOku).mock.calls.filter(([, init]) => init?.method === "POST");
    expect((postlar[0][1]?.headers as Record<string, string>)["Idempotency-Key"]).toBe((postlar[1][1]?.headers as Record<string, string>)["Idempotency-Key"]);
  });
});
