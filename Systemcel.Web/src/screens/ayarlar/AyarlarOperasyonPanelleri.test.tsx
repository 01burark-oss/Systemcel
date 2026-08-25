import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { AyarlarOperasyonPanelleri } from "./AyarlarOperasyonPanelleri";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const memberships = {
  sahibiMi: true,
  isletmeId: 1,
  isletmeAdi: "Pilot İşletme",
  uyelikler: [
    { id: 1, kullaniciId: 1, eposta: "owner@example.com", adSoyad: "İşletme Sahibi", rol: "isletme_sahibi", durum: "Aktif", davetKodu: "" }
  ]
};
const originalClipboard = Object.getOwnPropertyDescriptor(navigator, "clipboard");

describe("Ayarlar operasyon panelleri", () => {
  beforeEach(() => {
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/ayarlar/pin" && !init) return { varsayilanPin: true, mesaj: "PIN kilidi hazır." } as never;
      if (url === "/api/ekran/ayarlar/pin" && init?.method === "PUT") return { mesaj: "PIN güncellendi." } as never;
      if (url === "/api/ekran/uyelikler") return memberships as never;
      if (url === "/api/ekran/gelistirici-api/anahtarlar" && !init) return { anahtarlar: [] } as never;
      if (url === "/api/import/desktop/codes") return { code: "ABC123", expiresAtUtc: "2026-08-24T12:00:00Z", targetIsletmeId: 1, packageEndpoint: "/api/import/desktop/packages" } as never;
      throw new Error(`Beklenmeyen istek: ${url}`);
    });
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
    if (originalClipboard) Object.defineProperty(navigator, "clipboard", originalClipboard);
    else Reflect.deleteProperty(navigator, "clipboard");
  });

  it("varsayılan PIN'i gösterir ve dört haneli yeni PIN'i kaydeder", async () => {
    const user = userEvent.setup();
    render(<AyarlarOperasyonPanelleri />);

    expect(await screen.findByText(/Varsayılan PIN/)).toBeVisible();
    const button = screen.getByRole("button", { name: "PIN'i değiştir" });
    expect(button).toBeDisabled();
    await user.type(screen.getByLabelText("Mevcut PIN"), "0000");
    await user.type(screen.getByLabelText("Yeni PIN"), "4826");
    await user.click(button);

    await waitFor(() => expect(jsonOku).toHaveBeenCalledWith("/api/ekran/ayarlar/pin", {
      method: "PUT",
      body: JSON.stringify({ mevcutPin: "0000", yeniPin: "4826" })
    }));
    expect(await screen.findByRole("status")).toHaveTextContent("PIN güncellendi.");
  });

  it("ekip üyelerini gösterir ve masaüstü aktarım kodu üretir", async () => {
    const user = userEvent.setup();
    render(<AyarlarOperasyonPanelleri />);

    expect(await screen.findByText("İşletme Sahibi")).toBeVisible();
    expect(screen.getByText("Pilot İşletme için üyeleri ve bekleyen davetleri yönetin.")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Aktarım kodu oluştur" }));
    expect(await screen.findByText("ABC123")).toBeVisible();
    expect(screen.getByText("Systemcel ZIP paketini seçin")).toBeVisible();
  });

  it("pano kullanılamadığında davet bağlantısını kaybetmez ve doğru uygulama yolunu gösterir", async () => {
    const user = userEvent.setup();
    Object.defineProperty(navigator, "clipboard", { configurable: true, value: undefined });
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/ayarlar/pin" && !init) return { varsayilanPin: false, mesaj: "" } as never;
      if (url === "/api/ekran/uyelikler" && !init) return memberships as never;
      if (url === "/api/ekran/gelistirici-api/anahtarlar" && !init) return { anahtarlar: [] } as never;
      if (url === "/api/ekran/uyelikler/davet" && init?.method === "POST") return { davetKodu: "invite-code-123456789" } as never;
      throw new Error(`Beklenmeyen istek: ${url}`);
    });
    render(<AyarlarOperasyonPanelleri />);

    await user.type(await screen.findByPlaceholderText("ekip@isletme.com"), "yeni@example.com");
    await user.click(screen.getByRole("button", { name: "Davet oluştur" }));

    expect(await screen.findByText("Davet oluşturuldu. Bağlantıyı aşağıdan kopyalayın.")).toBeVisible();
    expect(screen.getByRole("link", { name: "Davet bağlantısını aç" })).toHaveAttribute(
      "href",
      `${window.location.origin}/app/ayarlar?davet=invite-code-123456789`
    );
  });

  it("ZIP olmayan masaüstü paketini yükleme isteğinden önce reddeder", async () => {
    const user = userEvent.setup();
    render(<AyarlarOperasyonPanelleri />);

    await user.click(await screen.findByRole("button", { name: "Aktarım kodu oluştur" }));
    const picker = screen.getByLabelText(/Systemcel ZIP paketini seçin/);
    fireEvent.change(picker, { target: { files: [new File(["not-a-zip"], "veriler.txt", { type: "text/plain" })] } });

    expect(await screen.findByText("Yalnız Systemcel ZIP paketi seçebilirsiniz.")).toBeVisible();
    expect(screen.getByRole("button", { name: "Paketi içe aktar" })).toBeDisabled();
    expect(jsonOku).not.toHaveBeenCalledWith("/api/import/desktop/packages", expect.anything());
  });
});
