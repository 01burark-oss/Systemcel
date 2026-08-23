import { createElement } from "react";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { CariHesaplarSayfasi, cariHedefId } from "./CariHesaplarSayfasi";
import type { CariDetay, CariEkranVerisi } from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const ekran: CariEkranVerisi = {
  aktifIsletme: "Systemcel Test",
  kartlar: [
    { id: 42, tip: "Musteri", unvan: "Atlas Ltd.", telefon: "5551112233", vergiNo: "1234567890", aktif: true }
  ],
  tipSecenekleri: [{ deger: "Musteri", etiket: "Müşteri" }],
  hareketTipleri: [{ deger: "Borc", etiket: "Borç" }]
};

const detay: CariDetay = {
  kart: {
    id: 42,
    tip: "Musteri",
    unvan: "Atlas Ltd.",
    telefon: "5551112233",
    eposta: "atlas@example.com",
    vergiNoTc: "1234567890",
    vergiDairesi: "Kadıköy",
    adres: "İstanbul",
    aktif: true
  },
  bakiye: 1250,
  hareketler: []
};

beforeEach(() => {
  Object.defineProperty(HTMLElement.prototype, "scrollTo", { configurable: true, value: vi.fn() });
  vi.mocked(jsonOku).mockImplementation(async (url) => {
    if (url === "/api/ekran/cari-hesaplar") return ekran;
    if (url === "/api/ekran/cari-hesaplar/42") return detay;
    throw new Error(`Beklenmeyen istek: ${url}`);
  });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("cariHedefId", () => {
  it("ilk yüklemede mevcut ilk kartı kendiliğinden düzenlemeye açmaz", () => {
    expect(cariHedefId(undefined, null)).toBeNull();
  });

  it("kullanıcının seçili kartını yenileme sırasında korur", () => {
    expect(cariHedefId(undefined, 42)).toBe(42);
  });

  it("açık tercihi ve yeni kart isteğini uygular", () => {
    expect(cariHedefId(7, 42)).toBe(7);
    expect(cariHedefId(null, 42)).toBeNull();
  });
});

describe("CariHesaplarSayfasi satır klavye etkileşimi", () => {
  it.each([
    ["Enter", "{Enter}"],
    ["Space", " "]
  ])("%s ile fare tıklamasıyla aynı cari detayını açar", async (_keyName, key) => {
    const user = userEvent.setup();
    render(createElement(CariHesaplarSayfasi, {
      onIsletmeDegistir: vi.fn(),
      ustBar: null,
      ustBarIslemde: false,
      yenileAnahtari: 0
    }));

    const row = await screen.findByRole("button", { name: "Atlas Ltd. cari hesabını aç" });
    row.focus();
    await user.keyboard(key);

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/cari-hesaplar/42"));
    expect(jsonOku).toHaveBeenCalledTimes(2);
    expect(await screen.findByRole("heading", { name: "Hesabı düzenle" })).toBeVisible();
    expect(screen.getByDisplayValue("atlas@example.com")).toBeVisible();
  });
});
