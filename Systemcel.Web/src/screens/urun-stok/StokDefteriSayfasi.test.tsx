import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { StokDefteriSayfasi } from "./StokDefteriSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const defter = {
  depolar: [{ id: 1, ad: "Merkez depo", kod: "MRK", konum: "Kat 1", varsayilan: true }, { id: 2, ad: "Mağaza", kod: "MGZ", varsayilan: false }],
  hareketler: [{ id: 8, islemId: 45, urunHizmetId: 3, urunAdi: "Kahve", depoId: 1, depoAdi: "Merkez depo", tarih: "2026-08-24T10:00:00Z", miktar: 12, rezerveMiktar: 2, hareketTipi: "Giris", aciklama: "Açılış", tersKayitVar: false }],
  negatifStokEngelli: true
};
const urunler = { urunler: [{ id: 3, ad: "Kahve", aktif: true, tip: "Urun", barkod: "", birim: "Adet", kdvOrani: 20, alisFiyati: 1, satisFiyati: 2, kritikStok: 0, mevcutStok: 12 }] };

describe("StokDefteriSayfasi", () => {
  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it("depo listesini yükler ve transferi aynı idempotency anahtarıyla gönderir", async () => {
    vi.mocked(jsonOku).mockImplementation(async (url) => url === "/api/ekran/stok-defteri" ? defter as never : url === "/api/ekran/urun-stok" ? urunler as never : {} as never);
    render(<StokDefteriSayfasi />);
    expect((await screen.findAllByText("Merkez depo"))[0]).toBeVisible();

    fireEvent.change(screen.getByLabelText("Transfer ürünü"), { target: { value: "3" } });
    fireEvent.change(screen.getByLabelText("Kaynak depo"), { target: { value: "1" } });
    fireEvent.change(screen.getByLabelText("Hedef depo"), { target: { value: "2" } });
    fireEvent.change(screen.getByLabelText("Transfer miktarı"), { target: { value: "4" } });
    fireEvent.click(screen.getByRole("button", { name: "Transferi kaydet" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/stok-defteri/transferler", expect.objectContaining({
      method: "POST", body: JSON.stringify({ urunHizmetId: 3, kaynakDepoId: 1, hedefDepoId: 2, miktar: 4, aciklama: "" }), headers: expect.objectContaining({ "Idempotency-Key": expect.any(String) })
    })));
  });

  it("sayımı açık onay olmadan göndermez ve ters kaydı ayrı aksiyonla başlatır", async () => {
    vi.mocked(jsonOku).mockImplementation(async (url) => url === "/api/ekran/stok-defteri" ? defter as never : url === "/api/ekran/urun-stok" ? urunler as never : {} as never);
    render(<StokDefteriSayfasi />);
    await screen.findByRole("button", { name: "8 numaralı işlemi ters kaydet" });
    expect(screen.getByRole("button", { name: "Sayımı onayla" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "8 numaralı işlemi ters kaydet" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/stok-defteri/islemler/45/ters-kayit", expect.objectContaining({ method: "POST", headers: expect.objectContaining({ "Idempotency-Key": expect.any(String) }) })));
  });

  it("özellik plan kapsamında değilse dürüst yükseltme durumu gösterir", async () => {
    vi.mocked(jsonOku).mockRejectedValue(new Error("Ücretsiz planında bu özellik kullanılamaz."));

    render(<StokDefteriSayfasi />);

    expect(await screen.findByRole("heading", { name: "Stok defteri planınızda açık değil" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Planları incele" })).toHaveAttribute("href", "/app/abonelik");
  });
});
