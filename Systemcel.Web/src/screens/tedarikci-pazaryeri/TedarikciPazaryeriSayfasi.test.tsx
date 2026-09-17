import { cleanup, render, screen, waitFor } from "@testing-library/react";
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

  it("iki tedarikçinin ürününü tek sipariş ve tek ödeme akışına gönderir", async () => {
    const initial = {
      aktifIsletmeId: 1,
      profiller: [], talepler: [], acikTalepler: [], gelenTeklifler: [], profil: null,
      benimUrunlerim: [], kaynakUrunler: [], anaSiparisler: [], siparisler: [], siparisKalemleri: [],
      urunler: [
        { id: 10, tedarikciProfilId: 20, tedarikciUnvani: "Marmara Gıda", tedarikciSehri: "İstanbul", sevkiyatBolgeleri: "Türkiye", iadeKosullari: "14 gün", sku: "KAHVE", ad: "Filtre Kahve", aciklama: "1 kg", kategori: "Gıda", birim: "Adet", birimFiyat: 100, kdvOrani: 20, paraBirimi: "TRY", kullanilabilirStok: 10, minimumSiparisMiktari: 1, tahminiTeslimatGun: 2 },
        { id: 11, tedarikciProfilId: 21, tedarikciUnvani: "Ege Ambalaj", tedarikciSehri: "İzmir", sevkiyatBolgeleri: "Türkiye", iadeKosullari: "14 gün", sku: "KUTU", ad: "Kargo Kutusu", aciklama: "20 adet", kategori: "Ambalaj", birim: "Paket", birimFiyat: 50, kdvOrani: 20, paraBirimi: "TRY", kullanilabilirStok: 10, minimumSiparisMiktari: 1, tahminiTeslimatGun: 1 }
      ]
    };
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/tedarikci-pazaryeri/siparisler" && init?.method === "POST")
        return { id: 77, mesaj: "Sipariş oluşturuldu." } as never;
      if (url === "/api/ekran/tedarikci-pazaryeri/siparisler/77/odeme" && init?.method === "POST")
        return { mesaj: "2 tedarikçi siparişi işleme alındı." } as never;
      return initial as never;
    });

    const user = userEvent.setup();
    render(<TedarikciPazaryeriSayfasi />);
    await screen.findByText("Filtre Kahve");
    const addButtons = screen.getAllByRole("button", { name: "Sepete ekle" });
    await user.click(addButtons[0]);
    await user.click(addButtons[1]);
    await user.click(screen.getByRole("button", { name: "Sepet" }));

    expect(screen.getByText("2 tedarikçiden 2 ürün")).toBeVisible();
    await user.type(screen.getByRole("textbox", { name: "Teslimat adresi" }), "Kadıköy, İstanbul");
    await user.click(screen.getByRole("button", { name: /Siparişi oluştur ve öde/ }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/siparisler",
      expect.objectContaining({ method: "POST" })
    ));
    const createCall = vi.mocked(jsonOku).mock.calls.find(([url]) => url === "/api/ekran/tedarikci-pazaryeri/siparisler");
    const body = JSON.parse(String(createCall?.[1]?.body));
    expect(body.kalemler).toEqual([{ urunId: 10, miktar: 1 }, { urunId: 11, miktar: 1 }]);
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/siparisler/77/odeme",
      expect.objectContaining({ method: "POST" })
    ));
  });
});
