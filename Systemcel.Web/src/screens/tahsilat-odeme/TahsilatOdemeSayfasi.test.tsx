import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { TahsilatOdemeSayfasi } from "./TahsilatOdemeSayfasi";
import type { OdemeHatirlatmaOnizleme, TahsilatOdemeEkranVerisi } from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const screenData: TahsilatOdemeEkranVerisi = {
  aktifIsletme: "Systemcel Test İşletmesi",
  hareketler: [
    {
      id: 9,
      no: "HRK-2026-00009",
      tarih: "2026-08-21T00:00:00",
      tip: "Tahsilat",
      cariKartId: 7,
      cariUnvan: "Atlas Yazılım",
      odemeYontemi: "Havale",
      tutar: 2_500,
      durum: "Tamamlandi",
      kaynak: "Manuel",
      aciklama: "Ön ödeme"
    },
    {
      id: -42,
      no: "SAT-2026-0042",
      tarih: "2026-08-21T00:00:00",
      tip: "Tahsilat",
      cariKartId: 7,
      cariUnvan: "Atlas Yazılım",
      odemeYontemi: "Havale",
      tutar: 12_500,
      durum: "Bekliyor",
      kaynak: "Fatura",
      aciklama: "Bekleyen fatura"
    }
  ],
  cariler: [{ id: 7, unvan: "Atlas Yazılım" }],
  faturalar: [{
    id: 42,
    no: "SAT-2026-0042",
    cariKartId: 7,
    cariUnvan: "Atlas Yazılım",
    faturaTipi: "Satis",
    durum: "Kesildi",
    genelToplam: 15_000,
    odenenTutar: 2_500,
    kalan: 12_500,
    odemeYontemi: "Havale",
    aciklama: ""
  }],
  ozet: { toplamTahsilat: 0, tahsilatAdedi: 0, toplamOdeme: 0, odemeAdedi: 0, bekleyen: 12_500, bekleyenAdedi: 1 },
  islemTipleri: [{ deger: "Tahsilat", etiket: "Tahsilat" }, { deger: "Odeme", etiket: "Ödeme" }],
  odemeYontemleri: [{ deger: "Nakit", etiket: "Nakit" }, { deger: "Havale", etiket: "Havale" }],
  paraBirimleri: [{ deger: "TRY", etiket: "TL" }],
  kategoriler: [{ deger: "Genel", etiket: "Genel" }, { deger: "Fatura", etiket: "Fatura" }],
  bugun: "2026-08-22"
};

const preview: OdemeHatirlatmaOnizleme = {
  faturaId: 42,
  isletmeAdi: "Systemcel Test İşletmesi",
  aliciEposta: "muhasebe@atlas.test",
  cariUnvan: "Atlas Yazılım",
  faturaNo: "SAT-2026-0042",
  faturaTarihi: "2026-08-01T00:00:00",
  vadeTarihi: "2026-08-25T00:00:00",
  kalanTutar: 12_500,
  paraBirimi: "TRY",
  konu: "Systemcel Test İşletmesi ödeme hatırlatması | SAT-2026-0042",
  mesaj: "Merhaba Atlas Yazılım,\n\nÖdemenizin vade tarihi 25.08.2026.\n\nSystemcel ile gönderildi · systemcel.app",
  gonderilebilir: true,
  engel: "",
  sonGonderimAt: null
};

describe("TahsilatOdemeSayfasi ödeme hatırlatması", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-08-22T12:00:00+03:00"));
    vi.clearAllMocks();
    vi.mocked(jsonOku).mockImplementation(async (url, init) => {
      if (url === "/api/ekran/tahsilat-odeme" && !init) return screenData;
      if (url === "/api/ekran/tahsilat-odeme/faturalar/42/hatirlatma" && !init) return preview;
      if (url === "/api/ekran/tahsilat-odeme/faturalar/42/hatirlatma" && init?.method === "POST") {
        return { gonderildi: true, mesaj: "Hatırlatma muhasebe@atlas.test adresine gönderildi.", gonderildiAt: "2026-08-22T10:00:00" };
      }
      if (url === "/api/ekran/tahsilat-odeme/9" && init?.method === "PUT") return { mesaj: "Tahsilat/ödeme güncellendi." };
      if (url === "/api/ekran/tahsilat-odeme/9/geri-al" && init?.method === "POST") return { mesaj: "Tahsilat geri alındı." };
      if (url === "/api/ekran/tahsilat-odeme/9" && init?.method === "DELETE") return { mesaj: "Tahsilat/ödeme silindi." };
      throw new Error(`Unexpected request: ${url} ${init?.method ?? "GET"}`);
    });
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("form eylemlerini aynı yerel düğme grubunda tutar", async () => {
    render(<TahsilatOdemeSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    const save = await screen.findByRole("button", { name: /^Kaydet$/ });
    const cancel = screen.getByRole("button", { name: /^İptal$/ });
    const actions = save.closest(".payment-actions");
    expect(actions).not.toBeNull();
    expect(actions).toContainElement(cancel);
    expect(save).toHaveAttribute("type", "button");
    expect(cancel).toHaveAttribute("type", "button");
  });

  it("yeni işlem formunda İşlem bilgileri başlığını yalnızca bir kez gösterir", async () => {
    render(<TahsilatOdemeSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    const formHeading = await screen.findByRole("heading", { name: "Yeni işlem" });
    const form = formHeading.closest(".payment-form-card");
    expect(form).not.toBeNull();
    expect(within(form as HTMLElement).getAllByRole("heading", { name: /^İşlem bilgileri$/i })).toHaveLength(1);
  });

  it("shows the email preview and sends only after confirmation", async () => {
    const user = userEvent.setup();
    render(<TahsilatOdemeSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByText("SAT-2026-0042");
    await user.click(screen.getByRole("button", { name: "Hatırlat" }));

    const dialog = await screen.findByRole("dialog", { name: "Ödeme hatırlatması" });
    expect(within(dialog).getByText("muhasebe@atlas.test")).toBeVisible();
    expect(within(dialog).getByText(preview.konu)).toBeVisible();
    expect(within(dialog).getByText(/Systemcel ile gönderildi/)).toBeVisible();

    await user.click(within(dialog).getByRole("button", { name: "Hatırlatmayı gönder" }));

    await waitFor(() => expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(
      "/api/ekran/tahsilat-odeme/faturalar/42/hatirlatma",
      { method: "POST" }
    ));
    expect(await within(dialog).findByRole("status")).toHaveTextContent("muhasebe@atlas.test adresine gönderildi");
  });

  it("keeps filters open after choosing a filter", async () => {
    const user = userEvent.setup();
    render(<TahsilatOdemeSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByText("SAT-2026-0042");
    await user.click(screen.getByRole("button", { name: "Filtreler" }));
    await user.click(screen.getByRole("button", { name: "Bekleyen" }));

    expect(screen.getByRole("button", { name: "Tümü" })).toBeVisible();
    expect(screen.getByText("SAT-2026-0042")).toBeVisible();
  });

  it("opens undo, edit and delete actions from the three-dot menu", async () => {
    const user = userEvent.setup();
    render(<TahsilatOdemeSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByText("HRK-2026-00009");
    await user.click(screen.getByRole("button", { name: "HRK-2026-00009 işlemleri" }));
    await user.click(screen.getByRole("menuitem", { name: "Tahsilatı geri al" }));
    const undoDialog = screen.getByRole("dialog", { name: "Tahsilat geri alınsın mı?" });
    await user.click(within(undoDialog).getByRole("button", { name: "Tahsilatı geri al" }));
    await waitFor(() => expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(
      "/api/ekran/tahsilat-odeme/9/geri-al",
      { method: "POST" }
    ));

    await user.click(screen.getByRole("button", { name: "HRK-2026-00009 işlemleri" }));
    await user.click(screen.getByRole("menuitem", { name: "Düzenle" }));
    expect(screen.getByRole("heading", { name: "İşlemi düzenle" })).toBeVisible();

    await user.click(screen.getByRole("button", { name: /^Güncelle$/ }));
    await waitFor(() => expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(
      "/api/ekran/tahsilat-odeme/9",
      expect.objectContaining({ method: "PUT" })
    ));

    await user.click(screen.getByRole("button", { name: "HRK-2026-00009 işlemleri" }));
    await user.click(screen.getByRole("menuitem", { name: "Sil" }));
    const dialog = screen.getByRole("dialog", { name: "İşlem silinsin mi?" });
    await user.click(within(dialog).getByRole("button", { name: "Sil" }));
    await waitFor(() => expect(vi.mocked(jsonOku)).toHaveBeenCalledWith(
      "/api/ekran/tahsilat-odeme/9",
      { method: "DELETE" }
    ));
  });
});
