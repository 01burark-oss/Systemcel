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

  it("iki tedarikçinin ürününü vadeli tek sipariş akışına gönderir", async () => {
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
    await user.click(screen.getByRole("button", { name: "Siparişi oluştur" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/siparisler",
      expect.objectContaining({ method: "POST" })
    ));
    const createCall = vi.mocked(jsonOku).mock.calls.find(([url]) => url === "/api/ekran/tedarikci-pazaryeri/siparisler");
    const body = JSON.parse(String(createCall?.[1]?.body));
    expect(body.kalemler).toEqual([{ urunId: 10, miktar: 1 }, { urunId: 11, miktar: 1 }]);
    expect(body.vadeli).toBe(true);
    expect(vi.mocked(jsonOku).mock.calls.some(([url]) => String(url).endsWith("/odeme"))).toBe(false);
  });

  it("kendi ürününü düzenler ve pasife alır", async () => {
    const initial = { aktifIsletmeId: 1, profiller: [], talepler: [], acikTalepler: [], gelenTeklifler: [],
      profil: { id: 1, unvan: "Tedarikçi", kategoriler: "Gıda", sehir: "İstanbul", aciklama: "", dogrulandi: true },
      benimUrunlerim: [{ id: 42, sku: "KAHVE", ad: "Filtre Kahve", aciklama: "", kategori: "Gıda", birim: "Adet", birimFiyat: 100, kdvOrani: 20, paraBirimi: "TRY", stokMiktari: 8, minimumSiparisMiktari: 1, tahminiTeslimatGun: 2, rezerveMiktar: 1, aktif: true }],
      kaynakUrunler: [], urunler: [], anaSiparisler: [], siparisler: [], siparisKalemleri: [] };
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/ekran/tedarikci-pazaryeri/urunler/42") return { mesaj: "Ürün güncellendi." } as never;
      return initial as never;
    });
    const user = userEvent.setup();
    render(<TedarikciPazaryeriSayfasi />);
    await user.click(screen.getByRole("button", { name: "Satışlarım" }));
    await screen.findByText("Filtre Kahve");
    await user.click(screen.getByRole("button", { name: "Filtre Kahve ürününü düzenle" }));
    expect(screen.getByRole("dialog", { name: "Ürünü düzenle" })).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Ürünü güncelle" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/tedarikci-pazaryeri/urunler/42", expect.objectContaining({ method: "PUT" })));
    await user.click(screen.getByRole("button", { name: "Filtre Kahve ürününü düzenle" }));
    await user.click(screen.getByRole("button", { name: "Kapat" }));
    await user.click(screen.getByRole("button", { name: "Pasife al" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/tedarikci-pazaryeri/urunler/42", expect.objectContaining({ method: "PUT" })));
    const calls = vi.mocked(jsonOku).mock.calls.filter(([url]) => url === "/api/ekran/tedarikci-pazaryeri/urunler/42");
    expect(JSON.parse(String(calls.at(-1)?.[1]?.body))).toEqual(expect.objectContaining({ sku: "KAHVE", stokMiktari: 8, aktif: false }));
  });

  it("kabul edilen teklifi teslimat adresiyle siparişe dönüştürür", async () => {
    const initial = { aktifIsletmeId: 1, profiller: [], talepler: [], acikTalepler: [], profil: null,
      gelenTeklifler: [{ id: 9, talepId: 4, talepBasligi: "Kahve alımı", birimFiyat: 80, kdvOrani: 20, paraBirimi: "TRY", terminGun: 2, minimumSiparis: 10, not: "", durum: "Gonderildi", tedarikciUnvani: "Marmara Gıda" }],
      benimUrunlerim: [], kaynakUrunler: [], urunler: [], anaSiparisler: [], siparisler: [], siparisKalemleri: [] };
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/tedarikci-pazaryeri/teklifler/9/kabul" && init?.method === "POST") return { mesaj: "Teklif siparişe dönüştürüldü." } as never;
      return initial as never;
    });
    const user = userEvent.setup();
    render(<TedarikciPazaryeriSayfasi />);
    await user.click(await screen.findByRole("button", { name: "Teklif talepleri" }));
    await user.click(screen.getByRole("button", { name: "Kabul et ve sipariş oluştur" }));
    await user.type(screen.getByRole("textbox", { name: "Teslimat adresi" }), "Kadıköy, İstanbul");
    await user.click(screen.getByRole("button", { name: "Siparişi oluştur" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/teklifler/9/kabul",
      expect.objectContaining({ method: "POST", body: JSON.stringify({ teslimatAdresi: "Kadıköy, İstanbul", vadeli: true }) })
    ));
  });

  it("hazırlanan satış için ürün QR etiketleri oluşturur", async () => {
    const initial = { aktifIsletmeId: 2, profiller: [], talepler: [], acikTalepler: [], gelenTeklifler: [],
      profil: { id: 10, unvan: "Tedarikçi", kategoriler: "Gıda", sehir: "İstanbul", aciklama: "", dogrulandi: true },
      benimUrunlerim: [], kaynakUrunler: [], urunler: [], anaSiparisler: [],
      siparisler: [{ id: 8, anaSiparisId: 7, anaSiparisNo: "PZ-7", siparisNo: "PZ-7-1", teslimatAdresi: "Depo", aliciIsletmeId: 1, tedarikciIsletmeId: 2, tedarikciUnvani: "Tedarikçi", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", komisyonTutari: 0, komisyonKdvTutari: 0, tevkifatTutari: 0, odemeHizmetiBedeli: 0, tedarikciHakEdisi: 120, durum: "Hazirlaniyor", kargoFirmasi: "", kargoTakipNo: "", createdAt: "2026-09-18" }],
      siparisKalemleri: [{ id: 12, tedarikciSiparisId: 8, tedarikciUrunId: 20, sku: "KAHVE", ad: "Filtre Kahve", birim: "Adet", miktar: 2, sevkEdilenMiktar: 0, kabulEdilenMiktar: 0, reddedilenMiktar: 0, birimFiyat: 50, kdvOrani: 20, toplamTutar: 120 }] };
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/8/sevkiyatlar" && init?.method === "POST")
        return { etiketler: [{ id: 1, kod: "scq1_test", qrIcerigi: "systemcel:sevkiyat:scq1_test", miktar: 2, urunAdi: "Filtre Kahve", sku: "KAHVE", birim: "Adet", lotNo: "" }] } as never;
      return initial as never;
    });
    const user = userEvent.setup();
    render(<TedarikciPazaryeriSayfasi />);
    await user.click(await screen.findByRole("button", { name: "Satışlarım" }));
    await user.click(screen.getByRole("button", { name: /Sevkiyat ve QR oluştur/ }));
    expect(screen.getByRole("dialog", { name: "Sevkiyat ve QR etiketi oluştur" })).toBeVisible();
    await user.click(screen.getByRole("button", { name: "QR etiketlerini oluştur" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/8/sevkiyatlar",
      expect.objectContaining({ method: "POST" })
    ));
    expect(await screen.findByAltText("Filtre Kahve sevkiyat QR kodu")).toBeVisible();
  });

  it("teslimat sorunu bildirir ve tedarikçiyi sipariş üzerinden değerlendirir", async () => {
    const initial = {
      aktifIsletmeId: 1, profiller: [], talepler: [], acikTalepler: [], gelenTeklifler: [], profil: null,
      benimUrunlerim: [], kaynakUrunler: [], urunler: [], sikayetler: [], degerlendirmeler: [], tedarikciPerformanslari: [],
      anaSiparisler: [{ id: 7, siparisNo: "PZ-7", teslimatAdresi: "Depo", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", durum: "CariOdemeBekliyor", createdAt: "2026-09-18" }],
      siparisler: [{ id: 8, anaSiparisId: 7, anaSiparisNo: "PZ-7", siparisNo: "PZ-7-1", teslimatAdresi: "Depo", aliciIsletmeId: 1, tedarikciIsletmeId: 2, tedarikciUnvani: "Tedarikçi", araToplam: 100, kdvToplam: 20, genelToplam: 120, paraBirimi: "TRY", komisyonTutari: 0, komisyonKdvTutari: 0, tevkifatTutari: 0, odemeHizmetiBedeli: 0, tedarikciHakEdisi: 120, durum: "CariOdemeBekliyor", kargoFirmasi: "", kargoTakipNo: "", createdAt: "2026-09-18", malKabulVar: true }],
      siparisKalemleri: [{ id: 12, tedarikciSiparisId: 8, tedarikciUrunId: 20, sku: "KAHVE", ad: "Filtre Kahve", birim: "Adet", miktar: 2, sevkEdilenMiktar: 2, kabulEdilenMiktar: 2, reddedilenMiktar: 0, birimFiyat: 50, kdvOrani: 20, toplamTutar: 120 }]
    };
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/8/sikayetler") return { mesaj: "Sorun tedarikçiye iletildi." } as never;
      if (url === "/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/8/degerlendirme") return { mesaj: "Değerlendirmeniz kaydedildi." } as never;
      return initial as never;
    });
    const user = userEvent.setup();
    render(<TedarikciPazaryeriSayfasi />);
    await user.click(await screen.findByRole("button", { name: "Siparişler" }));
    await user.click(screen.getByRole("button", { name: "Sorun bildir" }));
    await user.type(screen.getByRole("textbox", { name: "Açıklama" }), "Kolide bir ürün eksik geldi.");
    await user.click(screen.getByRole("button", { name: "Tedarikçiye ilet" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/8/sikayetler",
      expect.objectContaining({ method: "POST" })
    ));

    await user.click(screen.getByRole("button", { name: "Tedarikçiyi değerlendir" }));
    expect(screen.getByRole("dialog", { name: "Tedarikçi değerlendirmesi" })).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Değerlendirmeyi kaydet" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/8/degerlendirme",
      expect.objectContaining({ method: "PUT" })
    ));
  });
});
