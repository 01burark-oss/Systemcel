import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { GelistiriciApiPaneli } from "./AyarlarOperasyonPanelleri";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const kayit = {
  id: 41,
  ad: "Rapor entegrasyonu",
  prefix: "sys_live_a1b2",
  scopes: ["summary:read", "invoices:read"],
  createdAt: "2026-08-24T12:00:00Z",
  lastUsedAt: null,
  expiresAt: "2026-11-22T12:00:00Z",
  revokedAt: null
};

describe("Geliştirici API paneli", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockResolvedValue({ anahtarlar: [] } as never);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("anahtarı yalnız oluşturma yanıtından sonra gösterir ve kopyalar", async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", { configurable: true, value: { writeText } });
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/gelistirici-api/anahtarlar" && !init) return { anahtarlar: [] } as never;
      if (url === "/api/ekran/gelistirici-api/anahtarlar" && init?.method === "POST") {
        return { ...kayit, anahtar: "sys_live_a1b2_cok-gizli-anahtar" } as never;
      }
      throw new Error(`Beklenmeyen istek: ${url}`);
    });

    render(<GelistiriciApiPaneli />);
    await user.type(await screen.findByLabelText("Anahtar adı"), "Rapor entegrasyonu");
    await user.click(screen.getByRole("checkbox", { name: "Faturalar" }));
    await user.click(screen.getByRole("button", { name: "Anahtar oluştur" }));

    expect(await screen.findByDisplayValue("sys_live_a1b2_cok-gizli-anahtar")).toBeVisible();
    expect(screen.getByText(/tekrar gösterilmeyecek/i)).toBeVisible();
    await user.click(screen.getByRole("button", { name: "API anahtarını kopyala" }));
    expect(writeText).toHaveBeenCalledWith("sys_live_a1b2_cok-gizli-anahtar");
    expect(screen.queryByDisplayValue("sys_live_a1b2_cok-gizli-anahtar")).not.toBeInTheDocument();
    expect(jsonOku).toHaveBeenCalledWith("/api/ekran/gelistirici-api/anahtarlar", {
      method: "POST",
      body: JSON.stringify({
        ad: "Rapor entegrasyonu",
        scopes: ["summary:read", "invoices:read"],
        expiresInDays: 90
      })
    });
  });

  it("listede plaintext göstermez ve anahtarı iptal eder", async () => {
    const user = userEvent.setup();
    vi.spyOn(window, "confirm").mockReturnValue(true);
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/gelistirici-api/anahtarlar" && !init) return { anahtarlar: [kayit] } as never;
      if (url === "/api/ekran/gelistirici-api/anahtarlar/41" && init?.method === "DELETE") return undefined as never;
      throw new Error(`Beklenmeyen istek: ${url}`);
    });

    render(<GelistiriciApiPaneli />);
    expect(await screen.findByText("Rapor entegrasyonu")).toBeVisible();
    expect(screen.getByText(/sys_live_a1b2/)).toBeVisible();
    expect(screen.queryByText(/cok-gizli-anahtar/)).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Rapor entegrasyonu anahtarını iptal et" }));

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/gelistirici-api/anahtarlar/41",
      { method: "DELETE" }
    ));
    expect(await screen.findByText("İptal edildi")).toBeVisible();
  });

  it("hak yoksa yükseltme durumunu gösterir", async () => {
    vi.mocked(jsonOku).mockRejectedValue(new Error("Bu özellik mevcut planınızda kullanılamaz."));
    render(<GelistiriciApiPaneli />);

    expect(await screen.findByText("Geliştirici API planınızda açık değil")).toBeVisible();
    expect(screen.getByRole("link", { name: "Planları incele" })).toHaveAttribute("href", "/app/abonelik");
  });
});
