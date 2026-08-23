import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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

describe("MuhasebeciPanelSayfasi", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/app/muhasebeci");
    vi.mocked(jsonOku).mockResolvedValue(panel);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("müşteri listesini ayrı Müşterilerim ekranına taşır", async () => {
    render(<MuhasebeciPanelSayfasi />);

    expect(await screen.findByRole("heading", { name: "Muhasebeci paneli" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Müşterilerim" })).toHaveAttribute("href", "/app/muhasebeci/musteriler");
    expect(screen.queryByRole("columnheader", { name: "Belge durumu" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Davet linki oluştur" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Davet kodları" })).not.toBeInTheDocument();
  });

  it("talebi bağlantı kurmadan aylık ücretle ödeme adımına gönderir", async () => {
    const user = userEvent.setup();
    const pendingPanel = {
      ...panel,
      musteriler: [],
      bekleyenTalepler: [{
        id: 41,
        muhasebeciAdi: panel.muhasebeciAdi,
        musteriAdi: "Bahar Kafe",
        tur: "Pazaryeri",
        durum: "Beklemede",
        yetkiSeviyesi: "OkumaRapor",
        davetKodu: "",
        davetLinki: "",
        mesaj: "Aylık kayıtlar için destek istiyorum.",
        createdAt: "2026-08-24T10:00:00Z"
      }]
    };
    vi.mocked(jsonOku).mockImplementation(async (url, options) => {
      if (url === "/api/ekran/muhasebeci" && !options)
        return pendingPanel as never;
      return pendingPanel.bekleyenTalepler[0] as never;
    });

    render(<MuhasebeciPanelSayfasi />);

    await user.type(await screen.findByRole("spinbutton", { name: "Bahar Kafe aylık ücreti" }), "1250");
    await user.click(screen.getByRole("button", { name: "Ödemeye gönder" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/muhasebeci/talepler/41/kabul",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ yetkiSeviyesi: "OkumaRapor", aylikHizmetBedeli: 1250 })
      })
    ));
    expect(await screen.findByText("Bahar Kafe için ödeme adımı açıldı.")).toBeVisible();
    expect(screen.queryByText("bağlantısı aktif edildi")).not.toBeInTheDocument();
  });

  it("ödeme bekleyen talebi yeniden kabul ettirmez", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      ...panel,
      musteriler: [],
      bekleyenTalepler: [{
        id: 42,
        muhasebeciAdi: panel.muhasebeciAdi,
        musteriAdi: "Bahar Kafe",
        tur: "Pazaryeri",
        durum: "OdemeBekliyor",
        yetkiSeviyesi: "OkumaRapor",
        davetKodu: "",
        davetLinki: "",
        mesaj: "",
        createdAt: "2026-08-24T10:00:00Z",
        aylikHizmetBedeli: 1250,
        odemeDurumu: "Bekliyor",
        odemeYapilabilir: true
      }]
    } as never);

    render(<MuhasebeciPanelSayfasi />);

    expect(await screen.findByText("Müşteri ödemesi bekleniyor")).toBeVisible();
    expect(screen.getByText("₺1.250,00")).toBeVisible();
    expect(screen.queryByRole("button", { name: "Ödemeye gönder" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Red" })).not.toBeInTheDocument();
  });
});
