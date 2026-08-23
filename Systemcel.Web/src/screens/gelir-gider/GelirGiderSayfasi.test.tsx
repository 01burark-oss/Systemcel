import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "./api";
import { GelirGiderSayfasi } from "./GelirGiderSayfasi";
import type { EkranVerisi } from "./types";

vi.mock("./api", () => ({ jsonOku: vi.fn() }));

const ekran: EkranVerisi = {
  aktifIsletme: "Systemcel Test",
  kayitlar: [
    {
      id: 17,
      tarih: "2026-08-18T10:30",
      tur: "gider",
      tutar: 245.5,
      odemeYontemi: "nakit",
      kalem: "Bakim Giderleri",
      aciklama: "Kahve makinesi bakımı"
    }
  ],
  gelirKalemleri: ["Satış"],
  giderKalemleri: ["Bakim Giderleri"],
  stokUrunleri: [],
  odemeYontemleri: [
    { deger: "nakit", etiket: "Nakit" },
    { deger: "krediKarti", etiket: "Kredi Kartı" },
    { deger: "onlineOdeme", etiket: "Online Ödeme" },
    { deger: "havale", etiket: "Havale" }
  ]
};

describe("GelirGiderSayfasi satır klavye etkileşimi", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockResolvedValue(ekran);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it.each([
    ["Enter", "{Enter}"],
    ["Space", " "]
  ])("%s ile fare tıklamasıyla aynı kaydı düzenlemeye açar", async (_keyName, key) => {
    const user = userEvent.setup();
    render(<GelirGiderSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    const row = await screen.findByRole("button", { name: /gider kaydını aç$/i });
    expect(row.closest("td")).toHaveClass("ledger-table__action");
    const bubbledClick = vi.fn();
    document.addEventListener("click", bubbledClick);
    row.focus();
    try {
      await user.keyboard(key);
    } finally {
      document.removeEventListener("click", bubbledClick);
    }

    expect(screen.getByRole("heading", { name: "Kaydı düzenle" })).toBeVisible();
    expect(bubbledClick).not.toHaveBeenCalled();
    expect(screen.getByPlaceholderText("Tutar girin")).toHaveValue("245,5");
    expect(screen.getByDisplayValue("Kahve makinesi bakımı")).toBeVisible();
  });

  it("ödeme seçeneklerini ortak düzende, TL ekini tutarın sağında gösterir", async () => {
    render(<GelirGiderSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    const paymentButtons = await screen.findAllByRole("button", { name: /^(Nakit|Kredi Kartı|Online Ödeme|Havale)$/ });
    expect(paymentButtons).toHaveLength(4);
    expect(paymentButtons[0].closest(".satir")).toHaveClass("satir--payment-methods");
    expect(paymentButtons.every((button) => button.closest(".odeme-grid") === paymentButtons[0].closest(".odeme-grid"))).toBe(true);

    const amountInput = screen.getByPlaceholderText("Tutar girin");
    const amountControl = amountInput.closest(".tutar-alani");
    expect(amountControl).not.toBeNull();
    expect(Array.from(amountControl?.children ?? []).map((element) => element.tagName)).toEqual(["INPUT", "STRONG"]);
    expect(amountControl?.querySelector("strong")).toHaveTextContent("TL");
  });
});
