import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { openAuthenticatedFile } from "../../shared/authenticatedFile";
import { jsonOku } from "../../shared/json";
import { DashboardSayfasi } from "./DashboardSayfasi";
import type { DashboardEkran } from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));
vi.mock("../../shared/authenticatedFile", () => ({ openAuthenticatedFile: vi.fn() }));

const dashboard: DashboardEkran = {
  aktifIsletme: "Örnek İşletme",
  bugun: {
    etiket: "Bugun",
    aralik: "23.08.2026",
    gelir: 12_000,
    gider: 4_000,
    net: 8_000,
    gelirAdet: 4,
    giderAdet: 2
  },
  paneller: [],
  gelirDegisim: { yuzde: 0, etiket: "", olumlu: true },
  giderDegisim: { yuzde: 0, etiket: "", olumlu: true },
  odemeDagilimi: [],
  netTrend: [],
  brutKarMarji: {
    durum: "VeriYok",
    guvenilir: false,
    satisGeliri: 0,
    satisMaliyeti: 0,
    brutKar: 0,
    brutKarOrani: null,
    satisSatiri: 0,
    eksikMaliyetliSatisSatiri: 0,
    aciklama: "Bu dönemde maliyeti izlenen ürün satışı yok."
  },
  belgeSagligi: {
    skor: 82,
    durum: "Dikkat",
    donemBaslangic: "2026-08-01T00:00:00Z",
    donemBitis: "2026-08-31T23:59:59Z",
    faturaSayisi: 12,
    hazirBelgeSayisi: 9,
    eksikBelgeSayisi: 3,
    taslakFaturaSayisi: 1,
    dosyasiEksikFaturaSayisi: 2,
    satiriEksikFaturaSayisi: 1,
    cariBilgisiEksikFaturaSayisi: 1,
    vadeTarihiEksikFaturaSayisi: 1,
    bekleyenVeriIstegiSayisi: 0,
    sonBelgeAt: "2026-08-22T13:00:00Z",
    muhasebeciBagli: false,
    sorunlar: [
      { kod: "VadeEksik", baslik: "Vade tarihi eksik", adet: 1, puanEtkisi: 10, aksiyonUrl: "/app/faturalar?vade=eksik" },
      { kod: "DosyaEksik", baslik: "Fatura dosyası eksik", adet: 2, puanEtkisi: 30, aksiyonUrl: "/app/faturalar?dosya=eksik" },
      { kod: "SatirEksik", baslik: "Fatura satırı eksik", adet: 1, puanEtkisi: 20, aksiyonUrl: "/app/faturalar?satir=eksik" },
      { kod: "CariEksik", baslik: "Cari bilgisi eksik", adet: 1, puanEtkisi: 4, aksiyonUrl: "/app/faturalar?cari=eksik" }
    ]
  }
};

describe("DashboardSayfasi belge sağlığı", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockResolvedValue(dashboard);
    vi.mocked(openAuthenticatedFile).mockResolvedValue(undefined);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
    Reflect.deleteProperty(navigator, "share");
  });

  it("skoru, sayıları ve en önemli üç sorunu gösterir", async () => {
    render(
      <DashboardSayfasi
        onIsletmeDegistir={vi.fn()}
        ustBar={null}
        ustBarIslemde={false}
        yenileAnahtari={0}
      />
    );

    const kart = await screen.findByRole("region", { name: "Belgeler hazır mı?" });
    expect(within(kart).getByLabelText("Belge skoru 82")).toBeVisible();
    expect(within(kart).getByRole("progressbar", { name: "Belge hazırlık skoru" })).toHaveAttribute("aria-valuenow", "82");
    expect(within(kart).getByText("9", { selector: "dd" })).toBeVisible();
    expect(within(kart).getByText("3", { selector: "dd" })).toBeVisible();
    expect(within(kart).getByText("Fatura dosyası eksik")).toBeVisible();
    expect(within(kart).getByText("Fatura satırı eksik")).toBeVisible();
    expect(within(kart).getByText("Vade tarihi eksik")).toBeVisible();
    expect(within(kart).queryByText("Cari bilgisi eksik")).not.toBeInTheDocument();
    expect(within(kart).getByRole("link", { name: "Muhasebecini bağla" })).toHaveAttribute("href", "/app/muhasebeciler");
    expect(within(kart).queryByRole("link", { name: "Belgeleri otomatik aktar" })).not.toBeInTheDocument();
  });

  it("bağlı muhasebeci için sohbet yolunu gösterir", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      ...dashboard,
      belgeSagligi: { ...dashboard.belgeSagligi!, muhasebeciBagli: true }
    });

    render(
      <DashboardSayfasi
        onIsletmeDegistir={vi.fn()}
        ustBar={null}
        ustBarIslemde={false}
        yenileAnahtari={0}
      />
    );

    const kart = await screen.findByRole("region", { name: "Belgeler hazır mı?" });
    expect(within(kart).getByText("Muhasebecin verileri doğrudan görebilir")).toBeVisible();
    expect(within(kart).getByRole("link", { name: "Sohbete git" })).toHaveAttribute("href", "/app/sohbetler");
    expect(within(kart).queryByRole("link", { name: "Muhasebecini bağla" })).not.toBeInTheDocument();
  });

  it("maliyeti eksik satışlarda brüt kâr tutarı göstermez", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      ...dashboard,
      brutKarMarji: {
        ...dashboard.brutKarMarji,
        durum: "EksikMaliyet",
        eksikMaliyetliSatisSatiri: 2,
        aciklama: "2 satış satırının stok maliyeti eksik olduğu için brüt kâr hesaplanmadı."
      }
    });

    render(<DashboardSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    expect(await screen.findByText("Maliyet eksik")).toBeVisible();
    expect(screen.getByText("2 satış satırının maliyeti girilmeli.")).toBeVisible();
    expect(screen.queryByText("₺0")).not.toBeInTheDocument();
  });

  it("PDF raporunu mesaj yerine indirilebilir dosya olarak ister", async () => {
    const user = userEvent.setup();
    render(
      <DashboardSayfasi
        onIsletmeDegistir={vi.fn()}
        ustBar={null}
        ustBarIslemde={false}
        yenileAnahtari={0}
      />
    );

    await user.click(await screen.findByRole("button", { name: "Raporu Paylaş" }));
    await user.click(screen.getByRole("button", { name: "PDF İndir" }));

    expect(openAuthenticatedFile).toHaveBeenCalledWith(
      "/api/ekran/anasayfa/paylas/pdf",
      expect.objectContaining({ contentType: "application/pdf", download: true })
    );
  });

  it("Email paylaşımında Web Share API kullanır", async () => {
    const user = userEvent.setup();
    const share = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "share", { configurable: true, value: share });
    render(
      <DashboardSayfasi
        onIsletmeDegistir={vi.fn()}
        ustBar={null}
        ustBarIslemde={false}
        yenileAnahtari={0}
      />
    );

    await user.click(await screen.findByRole("button", { name: "Raporu Paylaş" }));
    await user.click(screen.getByRole("button", { name: "Email" }));

    expect(share).toHaveBeenCalledWith(expect.objectContaining({ title: expect.stringContaining("Systemcel") }));
  });
});
