import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { openAuthenticatedFile, printAuthenticatedHtml } from "../../shared/authenticatedFile";
import { jsonOku } from "../../shared/json";
import { RaporlarSayfasi } from "./RaporlarSayfasi";
import type { RaporlarEkranVerisi } from "./types";

vi.mock("../../shared/authenticatedFile", () => ({
  openAuthenticatedFile: vi.fn(),
  printAuthenticatedHtml: vi.fn()
}));
vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const ekran: RaporlarEkranVerisi = {
  aktifIsletme: "Örnek İşletme",
  bugun: "2026-08-24",
  varsayilanDonem: "2026-08",
  formatlar: [{ deger: "zip", etiket: "ZIP", secili: true }],
  icerikler: [],
  yazdirmaSablonlari: [{ deger: "yoneticiOzeti", etiket: "Yönetici Özeti" }],
  tarihAraliklari: [{ deger: "monthly", etiket: "Aylık" }],
  sonPaket: null
};

describe("RaporlarSayfasi dışa aktarımı", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockResolvedValue(ekran);
    vi.mocked(openAuthenticatedFile).mockResolvedValue(undefined);
    vi.mocked(printAuthenticatedHtml).mockResolvedValue(undefined);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("PDF düğmesi JSON yol mesajı yerine indirilebilir dosya ister", async () => {
    const user = userEvent.setup();
    render(<RaporlarSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByRole("button", { name: "PDF Kaydet" });
    await user.click(screen.getByRole("button", { name: "PDF Kaydet" }));

    expect(openAuthenticatedFile).toHaveBeenCalledWith(
      "/api/ekran/raporlar/yazdir/pdf",
      expect.objectContaining({
        contentType: "application/pdf",
        download: true,
        request: expect.objectContaining({ method: "POST" })
      })
    );
  });

  it("HTML düğmesi oluşturulan dosyayı indirir", async () => {
    const user = userEvent.setup();
    render(<RaporlarSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByRole("button", { name: "HTML" });
    await user.click(screen.getByRole("button", { name: "HTML" }));

    expect(openAuthenticatedFile).toHaveBeenCalledWith(
      "/api/ekran/raporlar/yazdir/html",
      expect.objectContaining({
        contentType: "text/html;charset=utf-8",
        download: true,
        request: expect.objectContaining({ method: "POST" })
      })
    );
  });

  it("Yazdır düğmesi tarayıcı yazdırma akışını başlatır", async () => {
    const user = userEvent.setup();
    render(<RaporlarSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByRole("button", { name: "Yazdır" });
    await user.click(screen.getByRole("button", { name: "Yazdır" }));

    expect(printAuthenticatedHtml).toHaveBeenCalledWith(
      "/api/ekran/raporlar/yazdir",
      expect.objectContaining({ method: "POST" })
    );
  });
});
