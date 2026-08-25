import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { GibPortalSayfasi } from "./GibPortalSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

describe("GibPortalSayfasi işlem geçmişi", () => {
  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("aktif işletmenin son bağlantı ve fatura işlemlerini gösterir", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      aktifIsletme: "Pilot İşletme",
      kullaniciKodu: "pilot",
      hasPassword: true,
      testModu: true,
      mesaj: "GİB Portal ayarları hazır.",
      sonIslemler: [
        { id: 2, faturaId: 31, tarih: "2026-08-24T10:30:00", islem: "CreatePortalDraft", basarili: true, mesaj: "Taslak oluşturuldu." },
        { id: 1, faturaId: null, tarih: "2026-08-24T10:00:00", islem: "TestConnection", basarili: false, mesaj: "Parola reddedildi." }
      ]
    });

    render(<GibPortalSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    expect(await screen.findByText("Portal taslağı")).toBeVisible();
    expect(screen.getByText("Fatura #31", { exact: false })).toBeVisible();
    expect(screen.getByText("Taslak oluşturuldu.")).toBeVisible();
    expect(screen.getByText("Bağlantı testi")).toBeVisible();
    expect(screen.getByText("Parola reddedildi.")).toBeVisible();
  });
});
