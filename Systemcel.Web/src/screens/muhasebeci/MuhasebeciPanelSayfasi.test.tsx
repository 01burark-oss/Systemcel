import { cleanup, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebeciPanelSayfasi } from "./MuhasebeciPanelSayfasi";
import type { BelgeSaglikOzeti } from "../dashboard/types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const belgeSagligi: BelgeSaglikOzeti = {
  skor: 91,
  durum: "Hazir",
  donemBaslangic: "2026-08-01T00:00:00Z",
  donemBitis: "2026-08-31T23:59:59Z",
  faturaSayisi: 18,
  hazirBelgeSayisi: 18,
  eksikBelgeSayisi: 0,
  taslakFaturaSayisi: 0,
  dosyasiEksikFaturaSayisi: 0,
  satiriEksikFaturaSayisi: 0,
  cariBilgisiEksikFaturaSayisi: 0,
  vadeTarihiEksikFaturaSayisi: 0,
  bekleyenVeriIstegiSayisi: 0,
  sonBelgeAt: "2026-08-22T13:00:00Z",
  muhasebeciBagli: true,
  sorunlar: []
};

const panel = {
  hazir: true,
  muhasebeciIsletmeId: 7,
  muhasebeciAdi: "Örnek Mali Müşavirlik",
  mesaj: "",
  entitlement: {
    planAdi: "Muhasebeci Pro",
    planKodu: "muhasebeci_pro",
    aylikTutar: 1_499,
    paraBirimi: "TRY",
    aiAktif: true,
    aiMesajLimiti: null,
    aiSinirsiz: true,
    musteriLimiti: null,
    musteriSinirsiz: true,
    aktifMusteriSayisi: 2,
    oneCikmaAktif: true,
    muhasebeciProOnerilir: false
  },
  profil: null,
  musteriler: [
    {
      isletmeId: 11,
      ad: "Örnek Market",
      konum: "İstanbul / Kadıköy",
      yetkiSeviyesi: "OkumaRapor",
      durum: "Aktif",
      baslangicAt: "2026-08-01T00:00:00Z",
      belgeSagligi
    },
    {
      isletmeId: 12,
      ad: "Yeni Atölye",
      konum: "Ankara / Çankaya",
      yetkiSeviyesi: "TamIslem",
      durum: "Aktif",
      baslangicAt: "2026-08-04T00:00:00Z",
      belgeSagligi: null
    }
  ],
  bekleyenTalepler: [],
  davetler: []
};

describe("MuhasebeciPanelSayfasi belge durumu", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/app/muhasebeci");
    vi.mocked(jsonOku).mockResolvedValue(panel);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("aktif müşterilerde skoru, durumu ve Pro kilidini gösterir", async () => {
    render(<MuhasebeciPanelSayfasi />);

    const tablo = await screen.findByRole("table");
    expect(within(tablo).getByRole("columnheader", { name: "Belge durumu" })).toBeVisible();
    expect(within(tablo).getByLabelText("Örnek Market belge durumu: 91 puan, Hazır, 0 eksik belge")).toBeVisible();
    expect(within(tablo).getByText("91")).toBeVisible();
    expect(within(tablo).getByText("Hazır")).toBeVisible();
    expect(within(tablo).getByText("0 eksik")).toBeVisible();
    expect(within(tablo).getByLabelText("Yeni Atölye belge durumu: Pro ile açılır")).toHaveTextContent("Pro ile açılır");
  });
});
