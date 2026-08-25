import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { HizliSatisSayfasi } from "./HizliSatisSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));
vi.mock("@zxing/browser", () => ({
  BrowserMultiFormatReader: class {
    decodeFromImageUrl() {
      return Promise.reject(new Error("Test image has no local barcode"));
    }
  }
}));

const stockScreen = {
  aktifIsletme: "Bahar Kafe",
  urunler: [{
    id: 7,
    tip: "Urun",
    ad: "Maden suyu",
    barkod: "8690000000007",
    birim: "Adet",
    kdvOrani: 20,
    alisFiyati: 10,
    satisFiyati: 18,
    kritikStok: 5,
    mevcutStok: 24,
    aktif: true
  }],
  sonHareketler: [],
  tipSecenekleri: [],
  birimSecenekleri: []
};

describe("HizliSatisSayfasi mobil tarama", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/urun-stok") return stockScreen as never;
      if (url === "/api/ekran/mobil-tarama/durum") return { fisOcrHazir: true } as never;
      if (url === "/api/ekran/gelir-gider" && !init) return {
        giderKalemleri: ["Ofis Giderleri", "Ulaşım Giderleri"],
        odemeYontemleri: [
          { deger: "nakit", etiket: "Nakit" },
          { deger: "krediKarti", etiket: "Kredi kartı" }
        ]
      } as never;
      if (url === "/api/ekran/mobil-tarama/barkod" && init?.method === "POST") return { barkod: "8690000000007" } as never;
      if (url === "/api/ekran/mobil-tarama/fis-ocr" && init?.method === "POST") return {
        merchant: "Bahar Market",
        receiptDate: "2026-08-24",
        paymentMethod: "KrediKarti",
        receiptTotal: 128.5,
        items: [{ rawName: "Temizlik", amount: 128.5, candidateKalem: "Ofis Giderleri" }]
      } as never;
      if (url === "/api/ekran/gelir-gider/kayitlar" && init?.method === "POST") return { mesaj: "Kaydedildi" } as never;
      throw new Error(`Unexpected request: ${url}`);
    });
  });

  afterEach(() => cleanup());

  it("kamera barkodunu sunucu fallback'iyle sepete ekler", async () => {
    const user = userEvent.setup();
    render(<HizliSatisSayfasi yenileAnahtari={0} />);
    await screen.findByText("Maden suyu");

    await user.upload(screen.getByLabelText("Barkod fotoğrafı"), new File(["image"], "barcode.jpg", { type: "image/jpeg" }));

    expect(await screen.findByText("8690000000007 barkodu okundu ve sepete eklendi.")).toBeVisible();
    expect(screen.getByText("1 sepette")).toBeVisible();
    expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(
      "/api/ekran/mobil-tarama/barkod",
      expect.objectContaining({ method: "POST", body: expect.any(FormData) })
    );
  });

  it("arama alanını programatik olarak etiketler ve hata durumunu duyurur", async () => {
    const user = userEvent.setup();
    render(<HizliSatisSayfasi yenileAnahtari={0} />);

    expect(await screen.findByRole("textbox", { name: "Ürün veya barkod ara" })).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Barkodu ekle" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Barkod okutun veya ürün arayın.");
  });

  it("fiş sonucunu kayıt oluşturmadan önizler", async () => {
    const user = userEvent.setup();
    render(<HizliSatisSayfasi yenileAnahtari={0} />);
    await waitFor(() => expect(screen.getByRole("button", { name: /Fiş oku/ })).toBeEnabled());

    await user.upload(screen.getByLabelText("Fiş fotoğrafı"), new File(["image"], "receipt.jpg", { type: "image/jpeg" }));

    expect(await screen.findByRole("article", { name: "Okunan fiş" })).toHaveTextContent("Bahar Market");
    expect(screen.getByRole("article", { name: "Okunan fiş" })).toHaveTextContent("1 satır okundu");
    expect(screen.getByLabelText("Fiş tarihi")).toHaveValue("2026-08-24T12:00");
    expect(screen.getByLabelText("Fiş toplamı")).toHaveValue("128,5");
    expect(screen.getByLabelText("Fiş ödeme yöntemi")).toHaveValue("krediKarti");
    expect(screen.getByLabelText("Fiş gider kalemi")).toHaveValue("Ofis Giderleri");
  });

  it("eksik tutar, tarih ve kategoriyle gider kaydı oluşturmaz", async () => {
    const user = userEvent.setup();
    render(<HizliSatisSayfasi yenileAnahtari={0} />);
    await waitFor(() => expect(screen.getByRole("button", { name: /Fiş oku/ })).toBeEnabled());
    await user.upload(screen.getByLabelText("Fiş fotoğrafı"), new File(["image"], "receipt.jpg", { type: "image/jpeg" }));
    const save = await screen.findByRole("button", { name: "Gider olarak kaydet" });

    await user.clear(screen.getByLabelText("Fiş toplamı"));
    await user.type(screen.getByLabelText("Fiş toplamı"), "okunamadı");
    await user.click(save);
    expect(await screen.findByText("Fiş toplamı sıfırdan büyük olmalıdır.")).toBeVisible();

    await user.clear(screen.getByLabelText("Fiş toplamı"));
    await user.type(screen.getByLabelText("Fiş toplamı"), "128,50");
    await user.clear(screen.getByLabelText("Fiş tarihi"));
    await user.click(save);
    expect(await screen.findByText("Geçerli bir fiş tarihi girin.")).toBeVisible();

    await user.type(screen.getByLabelText("Fiş tarihi"), "2026-08-24T12:00");
    await user.selectOptions(screen.getByLabelText("Fiş gider kalemi"), "");
    await user.click(save);
    expect(await screen.findByText("Gider kalemi seçin.")).toBeVisible();
    expect(vi.mocked(jsonOku).mock.calls.filter(([url]) => url === "/api/ekran/gelir-gider/kayitlar")).toHaveLength(0);
  });

  it("çift tıklamada tek gider kaydeder, taslağı temizler ve yenileme bildirir", async () => {
    const onKayitOlusturuldu = vi.fn();
    const user = userEvent.setup();
    render(<HizliSatisSayfasi yenileAnahtari={0} onKayitOlusturuldu={onKayitOlusturuldu} />);
    await waitFor(() => expect(screen.getByRole("button", { name: /Fiş oku/ })).toBeEnabled());
    await user.upload(screen.getByLabelText("Fiş fotoğrafı"), new File(["image"], "receipt.jpg", { type: "image/jpeg" }));
    const save = await screen.findByRole("button", { name: "Gider olarak kaydet" });

    save.click();
    save.click();

    expect(await screen.findByText("Fiş gider olarak kaydedildi. Finansal özet yenilendi.")).toBeVisible();
    expect(screen.queryByRole("article", { name: "Okunan fiş" })).not.toBeInTheDocument();
    expect(onKayitOlusturuldu).toHaveBeenCalledTimes(1);
    const saves = vi.mocked(jsonOku).mock.calls.filter(([url]) => url === "/api/ekran/gelir-gider/kayitlar");
    expect(saves).toHaveLength(1);
    expect(saves[0]?.[1]).toEqual(expect.objectContaining({
      method: "POST",
      body: JSON.stringify({
        tarih: "2026-08-24T12:00",
        tur: "gider",
        tutar: 128.5,
        odemeYontemi: "krediKarti",
        kalem: "Ofis Giderleri",
        aciklama: "Bahar Market | Temizlik",
        stokGiris: { aktif: false, urunId: 0, miktar: 1 }
      })
    }));
  });

  it("OCR anahtarı yoksa yapılandırma mesajı gösterir", async () => {
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/ekran/urun-stok") return stockScreen as never;
      if (url === "/api/ekran/mobil-tarama/durum") return { fisOcrHazir: false } as never;
      throw new Error(`Unexpected request: ${url}`);
    });

    render(<HizliSatisSayfasi yenileAnahtari={0} />);

    expect(await screen.findByRole("alert")).toHaveTextContent(/ReceiptOcr API anahtarını yapılandırmalı/);
    expect(screen.getByRole("button", { name: /Fiş oku/ })).toBeDisabled();
  });
});
