import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { CancellationRefundQueue } from "./CancellationRefundQueue";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const request = (overrides: Record<string, unknown> = {}) => ({
  abonelikId: 52,
  isletmeAdi: "Bahar Kafe",
  hesapTipi: "Isletme",
  planKodu: "isletme_buyume_yillik",
  iptalAt: "2026-10-01T10:00:00Z",
  donemBitisAt: "2026-10-15T10:00:00Z",
  kalanAySayisi: 11,
  tutar: 11000,
  paraBirimi: "TRY",
  durum: "OnayBekliyor",
  onaylanabilir: true,
  gonderilebilir: false,
  sorgulanabilir: false,
  onayAt: null,
  ...overrides
});

afterEach(() => { cleanup(); vi.clearAllMocks(); });

describe("CancellationRefundQueue", () => {
  it("keeps amount unknown requests out of approval and dispatch", async () => {
    vi.mocked(jsonOku).mockResolvedValue({ testIslemleriAcik: true, talepler: [request({ tutar: null })] } as never);
    render(<CancellationRefundQueue />);

    expect(await screen.findByText("Tutar inceleniyor", { selector: ".cancellation-refund__details dd" })).toBeVisible();
    expect(screen.getByText("Tutar inceleniyor. Tutar kesinleşmeden onay veya gönderim yapılamaz.")).toBeVisible();
    expect(screen.queryByRole("button", { name: "Bahar Kafe iadesini onayla" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Bahar Kafe test iadesini gönder" })).not.toBeInTheDocument();
  });

  it("disables test actions when the server gate is closed", async () => {
    vi.mocked(jsonOku).mockResolvedValue({ testIslemleriAcik: false, talepler: [request({ durum: "Hazir", onaylanabilir: false, gonderilebilir: true })] } as never);
    render(<CancellationRefundQueue />);

    expect(await screen.findByText("Test iade işlemleri kapalı.")).toBeVisible();
    expect(screen.getByRole("button", { name: "Bahar Kafe test iadesini gönder" })).toBeDisabled();
  });

  it("shows a controlled error when the queue response is malformed", async () => {
    vi.mocked(jsonOku).mockResolvedValue({} as never);
    render(<CancellationRefundQueue />);
    expect(await screen.findByRole("alert")).toHaveTextContent("İade kayıtları yüklenemedi.");
  });

  it("approves without a request body and reloads the queue", async () => {
    const user = userEvent.setup();
    let queueLoads = 0;
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (init?.method === "POST") return null as never;
      queueLoads += 1;
      return { testIslemleriAcik: true, talepler: [request(queueLoads === 1 ? {} : { onaylanabilir: false, gonderilebilir: true, durum: "Hazir" })] } as never;
    });
    render(<CancellationRefundQueue />);

    await user.click(await screen.findByRole("button", { name: "Bahar Kafe iadesini onayla" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(`${"/api/ekran/yonetim/abonelik-iadeleri"}/52/onayla`, { method: "POST" }));
    expect(await screen.findByRole("status")).toHaveTextContent("İade talebi onaylandı.");
    expect(screen.getByRole("button", { name: "Bahar Kafe test iadesini gönder" })).toBeVisible();
  });

  it("requires inline test refund confirmation before sending and sends no body", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (init?.method === "POST") return null as never;
      return { testIslemleriAcik: true, talepler: [request({ onaylanabilir: false, gonderilebilir: true, durum: "Hazir" })] } as never;
    });
    render(<CancellationRefundQueue />);

    await user.click(await screen.findByRole("button", { name: "Bahar Kafe test iadesini gönder" }));
    expect(screen.getByText(/Test ödeme iadesi/)).toBeVisible();
    expect(jsonOku).toHaveBeenCalledTimes(1);
    await user.click(screen.getByRole("button", { name: "Test iadesini gönder" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/yonetim/abonelik-iadeleri/52/gonder", { method: "POST" }));
    expect(await screen.findByRole("status")).toHaveTextContent("İade kaydı güncellendi. Sonucu kontrol edin.");
  });

  it("allows querying an ambiguous result and presents translated statuses", async () => {
    const user = userEvent.setup();
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (init?.method === "POST") return null as never;
      return { testIslemleriAcik: true, talepler: [request({ durum: "SonucBekliyor", onaylanabilir: false, gonderilebilir: false, sorgulanabilir: true })] } as never;
    });
    render(<CancellationRefundQueue />);

    expect(await screen.findByText("Sağlayıcı sonucu bekleniyor")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Bahar Kafe iade durumunu sorgula" }));
    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/yonetim/abonelik-iadeleri/52/sorgula", { method: "POST" }));
    expect(await screen.findByRole("status")).toHaveTextContent("Sağlayıcı iade durumu sorgulandı.");
  });

  it("clears previously rendered queue data when the admin endpoint returns 403", async () => {
    const user = userEvent.setup();
    let loads = 0;
    vi.mocked(jsonOku).mockImplementation(async (_url, init) => {
      if (init?.method === "POST") return null as never;
      loads += 1;
      if (loads === 1) return { testIslemleriAcik: true, talepler: [request()] } as never;
      throw new Error("Yönetici yetkisi gerekli.");
    });
    render(<CancellationRefundQueue />);

    expect(await screen.findByText("Bahar Kafe")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "İade kuyruğunu yenile" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Yönetici yetkisi gerekli.");
    await waitFor(() => expect(screen.queryByText("Bahar Kafe")).not.toBeInTheDocument());
  });
});
