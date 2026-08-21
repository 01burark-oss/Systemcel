import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { FinansalGorunumSayfasi } from "./FinansalGorunumSayfasi";
import { yerelTarihDegeri } from "./helpers";
import type {
  FinansalGorunumEkranVerisi,
  NakitProjeksiyonHaftasi,
  PlanlananNakitKalemi
} from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const referenceQuery = `/api/ekran/finansal-gorunum?referansTarihi=${encodeURIComponent(yerelTarihDegeri())}`;
const plansEndpoint = "/api/ekran/finansal-gorunum/nakit-planlari";

const projection: NakitProjeksiyonHaftasi[] = Array.from({ length: 13 }, (_, index) => {
  const start = new Date(Date.UTC(2026, 9, 1 + index * 7));
  const end = new Date(Date.UTC(2026, 9, 7 + index * 7));
  const opening = index === 0 ? 2_500 : index === 1 ? 3_800 : index === 2 ? 2_800 : -100 * (index - 2);
  const closing = index === 0 ? 3_800 : index === 1 ? 2_800 : -100 * (index - 1);
  return {
    hafta: index + 1,
    baslangic: start.toISOString(),
    bitis: end.toISOString(),
    acilisBakiyesi: opening,
    beklenenTahsilat: index === 0 ? 1_200 : 0,
    planlananGelir: index === 0 ? 200 : 0,
    beklenenOdeme: index === 1 ? 1_000 : 0,
    planlananGider: index === 0 ? 100 : index === 2 ? 3_000 : 0,
    netDegisim: closing - opening,
    kapanisBakiyesi: closing
  };
});

const populatedView: FinansalGorunumEkranVerisi = {
  referansTarihi: "2026-09-30T00:00:00",
  paraBirimi: "TRY",
  kasaBakiyesi: 2_500,
  acikAlacakToplami: 4_500,
  vadesiGecmisAlacakToplami: 4_200,
  yaslandirma: [
    { kod: "VadesiGelmedi", etiket: "Vadesi gelmedi", tutar: 300, faturaAdedi: 2, oran: 6.7 },
    { kod: "Gun1_30", etiket: "1-30 gün", tutar: 700, faturaAdedi: 2, oran: 15.6 },
    { kod: "Gun31_60", etiket: "31-60 gün", tutar: 1_100, faturaAdedi: 2, oran: 24.4 },
    { kod: "Gun61_90", etiket: "61-90 gün", tutar: 1_500, faturaAdedi: 2, oran: 33.3 },
    { kod: "Gun91Uzeri", etiket: "91+ gün", tutar: 900, faturaAdedi: 1, oran: 20 }
  ],
  cariYaslandirma: [{
    cariKartId: 7,
    unvan: "Riskli Müşteri",
    toplam: 4_500,
    vadesiGelmemis: 300,
    gun1Ila30: 700,
    gun31Ila60: 1_100,
    gun61Ila90: 1_500,
    gun91VeUzeri: 900,
    acikFaturaAdedi: 9,
    enUzunGecikmeGunu: 91,
    toplamdakiOrani: 100
  }],
  yogunlasma: {
    enBuyukCariOrani: 60,
    ilkUcCariOrani: 100,
    ilkBesCariOrani: 100,
    hhi: 4_600,
    riskSeviyesi: "Yuksek"
  },
  cariRiskleri: [{
    cariKartId: 7,
    unvan: "Riskli Müşteri",
    acikAlacak: 4_500,
    vadesiGecmisAlacak: 4_200,
    enUzunGecikmeGunu: 91,
    acikAlacakOrani: 100,
    ortalamaOdemeSapmasiGunu: 6,
    ortancaOdemeSapmasiGunu: 6,
    ortalamaOdemeSuresiGunu: 36,
    ortancaOdemeSuresiGunu: 36,
    zamanindaOdemeOrani: 33.3,
    odemeAraligiOrtancasiGunu: 10,
    sonDonemDegisimiGunu: 8,
    sonDonemOrnekAdedi: 3,
    oncekiDonemOrnekAdedi: 3,
    tamamlananOdemeAdedi: 6,
    ritimDurumu: "Kotulesiyor",
    riskSeviyesi: "Yuksek"
  }],
  nakitProjeksiyonu: projection,
  ilkNegatifHafta: 3,
  veriUyarilari: [{
    kod: "VadeTarihiEksik",
    mesaj: "Vade tarihi olmayan faturalar için fatura tarihi kullanıldı.",
    kayitAdedi: 2
  }]
};

const plans: PlanlananNakitKalemi[] = [{
  id: 11,
  isletmeId: 1,
  ad: "Aylık kira",
  tip: "Gider",
  tutar: 1_000,
  ilkTarih: "2026-10-01T00:00:00",
  tekrarTipi: "Aylik",
  tekrarAraligi: 1,
  bitisTarihi: null,
  kategori: "Sabit gider",
  aciklama: null,
  aktif: true
}];

describe("FinansalGorunumSayfasi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === referenceQuery) return populatedView;
      if (url === plansEndpoint) return { planlar: plans };
      throw new Error(`Unexpected request: ${url}`);
    });
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it("loads both GET contracts and renders KPIs, aging, 13 weeks, rhythm and warnings", async () => {
    render(<FinansalGorunumSayfasi yenileAnahtari={0} />);

    expect(screen.getByRole("main")).toHaveAttribute("aria-busy", "true");

    expect(await screen.findByRole("heading", { name: "Alacakların durumu" })).toBeVisible();
    await waitFor(() => expect(screen.getByRole("main")).toHaveAttribute("aria-busy", "false"));
    expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(referenceQuery);
    expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(plansEndpoint);

    const kpis = screen.getByRole("region", { name: "Finansal durum özeti" });
    expect(within(kpis).getByText("Kasa")).toBeVisible();
    expect(within(kpis).getByText(/2\.500,00/)).toBeVisible();
    expect(within(kpis).getByText("Açık alacak")).toBeVisible();
    expect(within(kpis).getByText(/4\.500,00/)).toBeVisible();

    const aging = screen.getByRole("list", { name: "Alacakların gecikme süresi" });
    expect(within(aging).getByText("1-30 gün")).toBeVisible();
    expect(within(aging).getByText("91+ gün")).toBeVisible();
    expect(within(aging).getAllByText("2 fatura")).toHaveLength(4);

    expect(screen.getByRole("heading", { name: "13 haftalık nakit tahmini" })).toBeVisible();
    expect(screen.getByText("13. hf.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Müşterilerin ödeme durumu" })).toBeVisible();
    const rhythm = screen.getByRole("table", { name: "Müşterilerin ödeme durumu" });
    expect(within(rhythm).getByText("Riskli Müşteri")).toBeVisible();
    expect(within(rhythm).getByText("Yavaşlıyor")).toBeVisible();
    expect(within(rhythm).getByText("Yüksek risk")).toBeVisible();

    const warnings = screen.getByRole("region", { name: "Uyarılar" });
    expect(within(warnings).getByText("2 faturanın vade tarihi eksik")).toBeVisible();
    expect(screen.getByText("Aylık kira")).toBeVisible();
    expect(screen.getByText(/Her ay/)).toBeVisible();
  });

  it("posts a recurring plan payload, reloads, and deletes after confirmation", async () => {
    const user = userEvent.setup();
    const confirm = vi.spyOn(window, "confirm").mockReturnValue(true);
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === referenceQuery) return populatedView;
      if (url === plansEndpoint && !init) return { planlar: plans };
      if (url === plansEndpoint && init?.method === "POST") return { mesaj: "Plan kaydedildi." };
      if (url === `${plansEndpoint}/11` && init?.method === "DELETE") return { mesaj: "Plan silindi." };
      throw new Error(`Unexpected request: ${url} ${init?.method ?? "GET"}`);
    });

    render(<FinansalGorunumSayfasi yenileAnahtari={0} />);
    await screen.findByRole("heading", { name: "Yeni plan" });
    await screen.findByText("Aylık kira");

    await user.type(screen.getByRole("textbox", { name: "Ad" }), "  Vergi ve SGK  ");
    await user.selectOptions(screen.getByRole("combobox", { name: "Tip" }), "Gider");
    await user.clear(screen.getByLabelText("İlk tarih"));
    await user.type(screen.getByLabelText("İlk tarih"), "2026-10-05");
    await user.type(screen.getByRole("textbox", { name: "Tutar" }), "1250,50");
    await user.selectOptions(screen.getByRole("combobox", { name: "Tekrar" }), "Aylik");
    await user.clear(screen.getByRole("spinbutton", { name: "Tekrar aralığı" }));
    await user.type(screen.getByRole("spinbutton", { name: "Tekrar aralığı" }), "2");
    await user.type(screen.getByLabelText("Bitiş tarihi"), "2027-02-05");
    await user.type(screen.getByRole("textbox", { name: "Kategori" }), "  Vergi  ");
    await user.type(screen.getByRole("textbox", { name: "Not" }), "  Tahmini ödeme  ");
    expect(screen.getByRole("checkbox", { name: "Tahmine ekle" })).toBeChecked();
    await user.click(screen.getByRole("button", { name: "Ekle" }));

    await waitFor(() => {
      const post = vi.mocked(jsonOku).mock.calls.find(([url, init]) => url === plansEndpoint && init?.method === "POST");
      expect(post).toBeDefined();
      expect(JSON.parse(String(post?.[1]?.body))).toEqual({
        ad: "Vergi ve SGK",
        tip: "Gider",
        tutar: 1_250.5,
        ilkTarih: "2026-10-05",
        tekrarTipi: "Aylik",
        tekrarAraligi: 2,
        bitisTarihi: "2027-02-05",
        kategori: "Vergi",
        aciklama: "Tahmini ödeme",
        aktif: true
      });
    });
    await waitFor(() => expect(vi.mocked(jsonOku).mock.calls.filter(([url]) => url === referenceQuery)).toHaveLength(2));

    await user.click(screen.getByRole("button", { name: "Aylık kira planını sil" }));
    expect(confirm).toHaveBeenCalledWith("“Aylık kira” planı silinsin mi?");
    await waitFor(() => expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(`${plansEndpoint}/11`, { method: "DELETE" }));
    await waitFor(() => expect(vi.mocked(jsonOku).mock.calls.filter(([url]) => url === referenceQuery)).toHaveLength(3));
  }, 10_000);

  it("shows a load error and retries both endpoints", async () => {
    const user = userEvent.setup();
    let fail = true;
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (fail && url === referenceQuery) throw new Error("Sunucu yanıt vermedi.");
      if (url === referenceQuery) return populatedView;
      if (url === plansEndpoint) return { planlar: plans };
      throw new Error(`Unexpected request: ${url}`);
    });

    render(<FinansalGorunumSayfasi yenileAnahtari={0} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Sunucu yanıt vermedi.");
    fail = false;
    await user.click(screen.getByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByRole("heading", { name: "Alacakların durumu" })).toBeVisible();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(screen.getByText("Aylık kira")).toBeVisible();
    expect(vi.mocked(jsonOku).mock.calls.filter(([url]) => url === referenceQuery)).toHaveLength(2);
    expect(vi.mocked(jsonOku).mock.calls.filter(([url]) => url === plansEndpoint)).toHaveLength(2);
  });
});
