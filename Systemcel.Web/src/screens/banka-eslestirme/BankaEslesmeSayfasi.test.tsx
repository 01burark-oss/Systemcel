import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { BankaEslesmeSayfasi } from "./BankaEslesmeSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

describe("BankaEslesmeSayfasi CSV MVP akışı", () => {
  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("CSV sınırını açıklar ve dosyayı kullanıcı eylemiyle içe aktarır", async () => {
    vi.mocked(jsonOku)
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce({ eklenen: 1, tekrar: 0, toplam: 1 })
      .mockResolvedValueOnce([]);

    render(<BankaEslesmeSayfasi yenileAnahtari={0} />);

    expect(await screen.findByText(/CSV ile çalışır; bankaya doğrudan bağlanmaz/i)).toBeVisible();
    const file = new File(["Tarih;Açıklama;Tutar\n24.08.2026;Müşteri;1.250,00"], "hareketler.csv", { type: "text/csv" });
    fireEvent.change(screen.getByLabelText(/CSV dosyası/i), { target: { files: [file] } });
    fireEvent.click(screen.getByRole("button", { name: "İçe aktar" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/banka-mutabakat/import",
      expect.objectContaining({ method: "POST", body: expect.any(FormData) })
    ));
    expect(await screen.findByText(/1 hareket eklendi/i)).toBeVisible();
  });

  it("eşleşmeyi açık kullanıcı onayı olmadan göndermez", async () => {
    vi.mocked(jsonOku)
      .mockResolvedValueOnce([{ id: 7, tarih: "2026-08-24", aciklama: "ABC LTD", tutar: 1000, paraBirimi: "TRY", durum: "Acik" }])
      .mockResolvedValueOnce([{ kaynakTuru: "Fatura", kaynakId: 12, baslik: "Fatura #12", tutar: 1000, tarih: "2026-08-24", skor: 100, nedenler: ["Tutar aynı"] }])
      .mockResolvedValueOnce(undefined)
      .mockResolvedValueOnce([{ id: 7, tarih: "2026-08-24", aciklama: "ABC LTD", tutar: 1000, paraBirimi: "TRY", durum: "Eslesti", eslesenKaynakTuru: "Fatura", eslesenKaynakId: 12 }]);

    render(<BankaEslesmeSayfasi yenileAnahtari={0} />);
    fireEvent.click(await screen.findByRole("button", { name: "Adayları gör" }));
    expect(await screen.findByText("Fatura #12")).toBeVisible();
    expect(jsonOku).not.toHaveBeenCalledWith(expect.stringContaining("/eslestir"), expect.anything());

    fireEvent.click(screen.getByRole("button", { name: /eşleştirmeyi onayla/i }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/banka-mutabakat/hareketler/7/eslestir",
      expect.objectContaining({ body: JSON.stringify({ kaynakTuru: "Fatura", kaynakId: 12, onaylandi: true }) })
    ));
  });

  it("hak yoksa teknik hata yerine plan yükseltme durumu gösterir", async () => {
    vi.mocked(jsonOku).mockRejectedValueOnce(new Error("Bu özellik mevcut planınızda kullanılamaz."));

    render(<BankaEslesmeSayfasi yenileAnahtari={0} />);

    expect(await screen.findByRole("heading", { name: "Bu özellik planınızda açık değil" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Planları incele" })).toHaveAttribute("href", "/abonelik");
  });

  it("okuma yetkili müşteri bağlamında değişiklik eylemlerini kapatır", async () => {
    vi.mocked(jsonOku).mockResolvedValueOnce([{ id: 7, tarih: "2026-08-24", aciklama: "ABC LTD", tutar: 1000, paraBirimi: "TRY", durum: "Acik" }]);

    render(<BankaEslesmeSayfasi yenileAnahtari={0} saltOkunur />);

    expect(await screen.findByText(/okuma yetkiniz var/i)).toBeVisible();
    expect(screen.getByLabelText("CSV dosyası")).toBeDisabled();
    expect(screen.getByRole("button", { name: "Yok say" })).toBeDisabled();
  });
});
