import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { MuhasebecilerSayfasi } from "./MuhasebecilerSayfasi";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

describe("MuhasebecilerSayfasi", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/app/muhasebeciler");
    vi.mocked(jsonOku).mockImplementation(async (url) => {
      if (url === "/api/ekran/muhasebeci/link-davetleri") {
        return {
          musteriAdi: "Bahar Kafe",
          durum: "Beklemede",
          yetkiSeviyesi: "TamIslem",
          mesaj: "Aylık belgeleri birlikte yönetelim.",
          davetLinki: "https://systemcel.test/muhasebeci-daveti/1234567890abcdef1234567890abcdef1234567890abcdef",
          sonGecerlilikAt: "2026-09-06T04:00:00"
        } as never;
      }

      return { mesaj: "", profiller: [] } as never;
    });
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("davet kodu kabul çubuğu yerine anlaşma adımında yetkili bağlantı üretir", async () => {
    const user = userEvent.setup();
    render(<MuhasebecilerSayfasi ustBar={{ hesapTipi: "Isletme" } as never} />);

    expect(await screen.findByRole("heading", { name: "Muhasebeciler" })).toBeVisible();
    expect(screen.queryByText("Davet kabul et")).not.toBeInTheDocument();
    expect(screen.queryByRole("group", { name: "Yetki seviyesi" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Muhasebecini davet et" }));
    expect(screen.getByRole("dialog", { name: "Muhasebecini davet et" })).toBeVisible();
    expect(screen.getByRole("group", { name: "Yetki seviyesi" })).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Tam işlem" }));
    await user.type(screen.getByRole("textbox", { name: "Not (isteğe bağlı)" }), "Aylık belgeleri birlikte yönetelim.");
    await user.click(screen.getByRole("button", { name: "Davet bağlantısı oluştur" }));

    expect(await screen.findByDisplayValue(/muhasebeci-daveti\/123456/)).toBeVisible();
    expect(jsonOku).toHaveBeenCalledWith(
      "/api/ekran/muhasebeci/link-davetleri",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          yetkiSeviyesi: "TamIslem",
          mesaj: "Aylık belgeleri birlikte yönetelim."
        })
      })
    );
  }, 10_000);

  it("sayısal eşleşme skorunu gerekçeleriyle gösterir ve uygun sıralamayı varsayılan seçer", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      mesaj: "",
      profiller: [{
        muhasebeciIsletmeId: 9,
        yayinda: true,
        unvan: "Ada Muhasebe",
        konum: "İstanbul / Kadıköy",
        telefon: "",
        deneyimYili: 8,
        profilResmiUrl: "",
        ucretBilgisi: "Aylık 2500 TL",
        uzmanliklar: "Kafe muhasebesi",
        musteriTipleri: "Kafe",
        sektorDeneyimleri: "Kafe",
        vergiMukellefiTipleri: "Şahıs",
        uygunIsletmeOlcekleri: "Küçük",
        calismaSekilleri: "Online",
        kisaAciklama: "Dönem takibi.",
        planAdi: "",
        pro: false,
        talepVar: false,
        bagli: false,
        eslesmeSkoru: 80,
        eslesmeNedenleri: ["Sektörünüzle çalışıyor", "İş yükünüze uygun"]
      }]
    } as never);

    render(<MuhasebecilerSayfasi ustBar={{ hesapTipi: "Isletme" } as never} />);

    expect(await screen.findByText("Sektörünüzle çalışıyor")).toBeVisible();
    expect(screen.getByText("İş yükünüze uygun")).toBeVisible();
    expect(screen.getByLabelText("Eşleşme skoru yüzde 80")).toHaveTextContent("%80");
    expect(screen.getByRole("combobox", { name: /Sıralama/i })).toHaveValue("uygun");
    expect(screen.queryByText("İstanbul / Kadıköy")).not.toBeInTheDocument();
    expect(screen.queryByText("Konum")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Şehir")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("İlçe")).not.toBeInTheDocument();
  });

  it("sana en uygun sıralamasında yüksek skoru önce gösterir", async () => {
    const profile = {
      muhasebeciIsletmeId: 9,
      yayinda: true,
      unvan: "Düşük Uyum",
      konum: "İstanbul",
      telefon: "",
      deneyimYili: 12,
      profilResmiUrl: "",
      ucretBilgisi: "Aylık 2000 TL",
      uzmanliklar: "Genel muhasebe",
      musteriTipleri: "KOBİ",
      sektorDeneyimleri: "Perakende",
      vergiMukellefiTipleri: "Şahıs",
      uygunIsletmeOlcekleri: "Küçük",
      calismaSekilleri: "Online",
      kisaAciklama: "Dönem takibi.",
      planAdi: "",
      pro: true,
      talepVar: false,
      bagli: false,
      eslesmeSkoru: 55,
      eslesmeNedenleri: ["Çalışma biçiminize uygun"]
    };
    vi.mocked(jsonOku).mockResolvedValue({
      mesaj: "",
      profiller: [profile, {
        ...profile,
        muhasebeciIsletmeId: 10,
        unvan: "Yüksek Uyum",
        deneyimYili: 4,
        pro: false,
        eslesmeSkoru: 94,
        eslesmeNedenleri: ["Sektörünüzle çalışıyor", "Mükellef tipinize uygun", "İş yükünüze uygun", "Çalışma biçiminize uygun"]
      }]
    } as never);

    render(<MuhasebecilerSayfasi ustBar={{ hesapTipi: "Isletme" } as never} />);

    const cards = await screen.findAllByRole("article");
    expect(within(cards[0]).getByRole("heading", { name: "Yüksek Uyum" })).toBeVisible();
    expect(within(cards[1]).getByRole("heading", { name: "Düşük Uyum" })).toBeVisible();
  });

  it("tüm mükellef tipleriyle çalışan profili seçili mükellef filtresinde korur", async () => {
    vi.mocked(jsonOku).mockResolvedValue({
      mesaj: "",
      profiller: [
        {
          muhasebeciIsletmeId: 9,
          yayinda: true,
          unvan: "Ada Muhasebe",
          konum: "İstanbul / Kadıköy",
          telefon: "",
          deneyimYili: 8,
          profilResmiUrl: "",
          ucretBilgisi: "Aylık 2500 TL",
          uzmanliklar: "Kafe muhasebesi",
          musteriTipleri: "Kafe",
          sektorDeneyimleri: "Kafe",
          vergiMukellefiTipleri: "Tüm mükellef tipleri",
          uygunIsletmeOlcekleri: "Küçük",
          calismaSekilleri: "Online",
          kisaAciklama: "Dönem takibi.",
          planAdi: "",
          pro: false,
          talepVar: false,
          bagli: false,
          eslesmeNedenleri: []
        },
        {
          muhasebeciIsletmeId: 10,
          yayinda: true,
          unvan: "Bora Mali Müşavirlik",
          konum: "İstanbul / Beşiktaş",
          telefon: "",
          deneyimYili: 6,
          profilResmiUrl: "",
          ucretBilgisi: "Aylık 3000 TL",
          uzmanliklar: "Perakende",
          musteriTipleri: "KOBİ",
          sektorDeneyimleri: "Perakende",
          vergiMukellefiTipleri: "Şahıs",
          uygunIsletmeOlcekleri: "Orta",
          calismaSekilleri: "Hibrit",
          kisaAciklama: "Aylık takip.",
          planAdi: "",
          pro: false,
          talepVar: false,
          bagli: false,
          eslesmeNedenleri: []
        }
      ]
    } as never);

    const user = userEvent.setup();
    render(<MuhasebecilerSayfasi ustBar={{ hesapTipi: "Isletme" } as never} />);

    await screen.findByText("Ada Muhasebe");
    await user.selectOptions(screen.getByLabelText("Mükellef tipi"), "Şahıs");

    expect(screen.getByText("Ada Muhasebe")).toBeVisible();
    expect(screen.getByText("Bora Mali Müşavirlik")).toBeVisible();
  });
});
