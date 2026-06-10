import React from "react";
import {
  CalendarDays,
  CheckCircle2,
  ChevronUp,
  Clock3,
  FileText,
  Filter,
  MoreVertical,
  Plus,
  Search,
  Send,
  Trash2,
  WalletCards
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type {
  FaturaDetay,
  FaturaEkranVerisi,
  FaturaFormu,
  FaturaListeKaydi,
  TahsilatFormu
} from "./types";

interface FaturalarSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

interface KimlikliMesaj {
  mesaj: string;
  id: number;
}

interface ApiMesaj {
  mesaj: string;
}

interface GibSmsBaslatSonucu {
  mesaj: string;
  operationId: string;
}

function bugun() {
  return new Date().toISOString().slice(0, 10);
}

function ayBasi() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function bosFaturaFormu(tarih = bugun()): FaturaFormu {
  return {
    id: 0,
    cariKartId: "0",
    tarih,
    vadeVar: false,
    vadeTarihi: tarih,
    faturaTipi: "Satis",
    odemeYontemi: "Nakit",
    aciklama: "",
    urunHizmetId: "0",
    satirAciklama: "",
    birim: "Adet",
    miktar: "1",
    birimFiyat: "0",
    kdvOrani: "20",
    iskontoOrani: "0",
    stokEtkilesin: true
  };
}

function bosTahsilatFormu(tarih = bugun()): TahsilatFormu {
  return {
    tutar: "0",
    tarih,
    odemeYontemi: "Nakit",
    aciklama: ""
  };
}

function sayiyaCevir(value: string) {
  const parsed = Number(value.replace(",", ".").trim());
  if (!Number.isFinite(parsed)) {
    throw new Error("Sayısal alanları kontrol edin.");
  }

  return parsed;
}

function paraBic(value: number) {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value);
}

function tarihBic(tarih: string) {
  const parsed = new Date(tarih);
  if (Number.isNaN(parsed.getTime())) {
    return tarih || "-";
  }

  return parsed.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}

function tarihKisaBic(tarih: string) {
  const parsed = new Date(tarih);
  if (Number.isNaN(parsed.getTime())) {
    return tarih || "-";
  }

  return parsed.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}

function durumEtiketi(durum: string) {
  switch (durum) {
    case "Odendi":
      return "Ödendi";
    case "KismiOdendi":
      return "Kısmi Ödendi";
    case "Kesildi":
      return "Bekliyor";
    case "PortalTaslak":
      return "GİB Taslak";
    case "Iptal":
      return "İptal";
    default:
      return "Taslak";
  }
}

function etiketBic(value: string) {
  switch (value) {
    case "Satis":
      return "Satış";
    case "Alis":
      return "Alış";
    case "Odendi":
      return "Ödendi";
    case "KismiOdendi":
    case "Kismi Odendi":
      return "Kısmi Ödendi";
    case "PortalTaslak":
    case "GIB Taslak":
      return "GİB Taslak";
    case "Iptal":
      return "İptal";
    case "Kredi Karti":
      return "Kredi Kartı";
    case "Online Odeme":
      return "Online Ödeme";
    default:
      return value;
  }
}

function formaAktar(detay: FaturaDetay): FaturaFormu {
  const satir = detay.satirlar[0];
  return {
    id: detay.fatura.id,
    cariKartId: String(detay.fatura.cariKartId),
    tarih: detay.fatura.tarih.slice(0, 10),
    vadeVar: Boolean(detay.fatura.vadeTarihi),
    vadeTarihi: detay.fatura.vadeTarihi ? detay.fatura.vadeTarihi.slice(0, 10) : detay.fatura.tarih.slice(0, 10),
    faturaTipi: detay.fatura.faturaTipi || "Satis",
    odemeYontemi: detay.fatura.odemeYontemi || "Nakit",
    aciklama: detay.fatura.aciklama,
    urunHizmetId: String(satir?.urunHizmetId ?? 0),
    satirAciklama: satir?.aciklama ?? "",
    birim: satir?.birim ?? "Adet",
    miktar: String(satir?.miktar ?? 1),
    birimFiyat: String(satir?.birimFiyat ?? 0),
    kdvOrani: String(satir?.kdvOrani ?? 20),
    iskontoOrani: String(satir?.iskontoOrani ?? 0),
    stokEtkilesin: satir?.stokEtkilesin ?? true
  };
}

export function FaturalarSayfasi({
  yenileAnahtari
}: FaturalarSayfasiProps) {
  const pageRef = React.useRef<HTMLElement | null>(null);
  const [ekran, setEkran] = React.useState<FaturaEkranVerisi | null>(null);
  const [seciliId, setSeciliId] = React.useState<number | null>(null);
  const [form, setForm] = React.useState<FaturaFormu>(() => bosFaturaFormu());
  const [tahsilatFormu, setTahsilatFormu] = React.useState<TahsilatFormu>(() => bosTahsilatFormu());
  const [arama, setArama] = React.useState("");
  const [filtreAcik, setFiltreAcik] = React.useState(false);
  const [tarihPanelAcik, setTarihPanelAcik] = React.useState(false);
  const [tipFiltresi, setTipFiltresi] = React.useState("Tumu");
  const [durumFiltresi, setDurumFiltresi] = React.useState("Tumu");
  const [baslangic, setBaslangic] = React.useState(ayBasi());
  const [bitis, setBitis] = React.useState(bugun());
  const [, setDurum] = React.useState("Faturalar yükleniyor...");
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const [smsOnayi, setSmsOnayi] = React.useState<{ operationId: string; mesaj: string } | null>(null);
  const [smsKodu, setSmsKodu] = React.useState("");
  const seciliIdRef = React.useRef<number | null>(null);

  const seciliFatura = React.useMemo(
    () => ekran?.faturalar.find((row) => row.id === seciliId) ?? null,
    [ekran, seciliId]
  );

  const filtreliFaturalar = React.useMemo(() => {
    const query = arama.trim().toLocaleLowerCase("tr-TR");
    return (ekran?.faturalar ?? []).filter((row) => {
      const tarih = row.tarih.slice(0, 10);
      const matchesSearch =
        !query ||
        row.no.toLocaleLowerCase("tr-TR").includes(query) ||
        row.cariUnvan.toLocaleLowerCase("tr-TR").includes(query) ||
        row.aciklama.toLocaleLowerCase("tr-TR").includes(query);
      const matchesType = tipFiltresi === "Tumu" || row.faturaTipi === tipFiltresi;
      const matchesState = durumFiltresi === "Tumu" || row.durum === durumFiltresi;
      const matchesDate = (!baslangic || tarih >= baslangic) && (!bitis || tarih <= bitis);
      return matchesSearch && matchesType && matchesState && matchesDate;
    });
  }, [arama, baslangic, bitis, durumFiltresi, ekran, tipFiltresi]);

  const tahsilOrani = React.useMemo(() => {
    const toplam = ekran?.ozet.toplamFatura ?? 0;
    return toplam <= 0 ? 0 : Math.round(((ekran?.ozet.tahsilEdilen ?? 0) / toplam) * 100);
  }, [ekran]);

  const kalanTutar = React.useMemo(() => {
    if (!seciliFatura) {
      return 0;
    }

    return Math.max(0, seciliFatura.genelToplam - seciliFatura.odenenTutar);
  }, [seciliFatura]);

  const yenile = React.useCallback(async (tercihId?: number | null) => {
    setHata("");
    setDurum("Faturalar yükleniyor...");
    const data = await jsonOku<FaturaEkranVerisi>("/api/ekran/faturalar");
    setEkran(data);

    const hedefId = tercihId === undefined
      ? seciliIdRef.current ?? data.faturalar[0]?.id ?? null
      : tercihId ?? data.faturalar[0]?.id ?? null;
    const hedef = data.faturalar.find((row) => row.id === hedefId) ?? null;
    if (hedef) {
      await faturaSec(hedef.id);
      setDurum(`${data.faturalar.length} fatura hazır.`);
      return;
    }

    seciliIdRef.current = null;
    setSeciliId(null);
    setForm(bosFaturaFormu(data.bugun || bugun()));
    setTahsilatFormu(bosTahsilatFormu(data.bugun || bugun()));
    setDurum("Fatura kaydı yok. Yeni taslak oluşturabilirsiniz.");
  }, []);

  React.useEffect(() => {
    pageRef.current?.scrollTo({ top: 0, left: 0 });
    yenile().catch((error: Error) => {
      setDurum("");
      setHata(error.message);
    });
  }, [yenile, yenileAnahtari]);

  async function faturaSec(id: number) {
    const detay = await jsonOku<FaturaDetay>(`/api/ekran/faturalar/${id}`);
    seciliIdRef.current = id;
    setSeciliId(id);
    setForm(formaAktar(detay));
    const kalan = Math.max(0, detay.fatura.genelToplam - detay.fatura.odenenTutar);
    setTahsilatFormu({
      tutar: String(kalan),
      tarih: ekran?.bugun || bugun(),
      odemeYontemi: detay.fatura.odemeYontemi || "Nakit",
      aciklama: ""
    });
  }

  function formGuncelle<K extends keyof FaturaFormu>(alan: K, deger: FaturaFormu[K]) {
    setForm((current) => ({ ...current, [alan]: deger }));
  }

  function tahsilatGuncelle<K extends keyof TahsilatFormu>(alan: K, deger: TahsilatFormu[K]) {
    setTahsilatFormu((current) => ({ ...current, [alan]: deger }));
  }

  function yeniTaslak() {
    seciliIdRef.current = null;
    setSeciliId(null);
    setForm(bosFaturaFormu(ekran?.bugun || bugun()));
    setTahsilatFormu(bosTahsilatFormu(ekran?.bugun || bugun()));
    setDurum("Yeni fatura taslağı.");
  }

  function urunSec(urunId: string) {
    const urun = ekran?.urunler.find((row) => String(row.id) === urunId);
    setForm((current) => ({
      ...current,
      urunHizmetId: urunId,
      birim: urun?.birim ?? current.birim,
      kdvOrani: String(urun?.kdvOrani ?? current.kdvOrani),
      birimFiyat: String(form.faturaTipi === "Alis" ? urun?.alisFiyati ?? 0 : urun?.satisFiyati ?? 0),
      stokEtkilesin: urun?.tip === "Urun"
    }));
  }

  function payload() {
    if (Number(form.cariKartId) <= 0) {
      throw new Error("Cari seçin.");
    }

    return {
      id: form.id,
      cariKartId: Number(form.cariKartId),
      tarih: form.tarih,
      vadeTarihi: form.vadeVar ? form.vadeTarihi : "",
      faturaTipi: form.faturaTipi,
      odemeYontemi: form.odemeYontemi,
      aciklama: form.aciklama,
      satirlar: [
        {
          urunHizmetId: Number(form.urunHizmetId),
          aciklama: form.satirAciklama,
          birim: form.birim,
          miktar: sayiyaCevir(form.miktar),
          birimFiyat: sayiyaCevir(form.birimFiyat),
          iskontoOrani: sayiyaCevir(form.iskontoOrani),
          kdvOrani: sayiyaCevir(form.kdvOrani),
          stokEtkilesin: form.stokEtkilesin
        }
      ]
    };
  }

  async function taslakKaydet() {
    try {
      setIslemde(true);
      setHata("");
      const body = payload();
      const result = form.id > 0
        ? await jsonOku<KimlikliMesaj>(`/api/ekran/faturalar/${form.id}`, {
            method: "PUT",
            body: JSON.stringify(body)
          })
        : await jsonOku<KimlikliMesaj>("/api/ekran/faturalar", {
            method: "POST",
            body: JSON.stringify(body)
          });

      await yenile(result.id);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Fatura kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  async function seciliIslem(endpoint: string, varsayilanMesaj: string) {
    if (!seciliId) {
      setHata("Önce fatura seçin.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<ApiMesaj>(`/api/ekran/faturalar/${seciliId}/${endpoint}`, { method: "POST" });
      await yenile(seciliId);
      setDurum(result.mesaj || varsayilanMesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : varsayilanMesaj);
    } finally {
      setIslemde(false);
    }
  }

  async function kesOnayla() {
    if (!seciliId) {
      setHata("Önce fatura seçin.");
      return;
    }

    if (seciliFatura?.durum !== "PortalTaslak") {
      await seciliIslem("kes", "Fatura kesildi.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      setSmsKodu("");
      const result = await jsonOku<GibSmsBaslatSonucu>(`/api/ekran/faturalar/${seciliId}/gib-sms-baslat`, { method: "POST" });
      setSmsOnayi({ operationId: result.operationId, mesaj: result.mesaj });
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "GİB SMS onayı başlatılamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function smsOnayla() {
    if (!seciliId || !smsOnayi) {
      setHata("SMS onay bilgisi bulunamadı.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<ApiMesaj>(`/api/ekran/faturalar/${seciliId}/gib-sms-tamamla`, {
        method: "POST",
        body: JSON.stringify({
          operationId: smsOnayi.operationId,
          smsKodu
        })
      });
      setSmsOnayi(null);
      setSmsKodu("");
      await yenile(seciliId);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "GİB SMS onayı tamamlanamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function tahsilatEkle() {
    if (!seciliId) {
      setHata("Önce fatura seçin.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<ApiMesaj>(`/api/ekran/faturalar/${seciliId}/tahsilat-odeme`, {
        method: "POST",
        body: JSON.stringify({
          tutar: sayiyaCevir(tahsilatFormu.tutar),
          tarih: tahsilatFormu.tarih,
          odemeYontemi: tahsilatFormu.odemeYontemi,
          aciklama: tahsilatFormu.aciklama
        })
      });
      await yenile(seciliId);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Tahsilat/ödeme eklenemedi.");
    } finally {
      setIslemde(false);
    }
  }

  return (
    <main className="invoice-page" ref={pageRef}>
      <section className="invoice-layout">
        <div className="invoice-left">
          <section className="invoice-stats">
            <article className="invoice-stat">
              <span className="invoice-stat__icon blue"><FileText size={26} /></span>
              <p>Toplam Fatura</p>
              <strong>{paraBic(ekran?.ozet.toplamFatura ?? 0)}</strong>
              <small>{ekran?.ozet.faturaAdedi ?? 0} adet fatura</small>
            </article>
            <article className="invoice-stat">
              <span className="invoice-stat__icon green"><CheckCircle2 size={27} /></span>
              <p>Tahsil Edilen</p>
              <strong>{paraBic(ekran?.ozet.tahsilEdilen ?? 0)}</strong>
              <small>%{tahsilOrani} tahsilat oranı</small>
            </article>
            <article className="invoice-stat">
              <span className="invoice-stat__icon amber"><Clock3 size={27} /></span>
              <p>Bekleyen</p>
              <strong>{paraBic(ekran?.ozet.bekleyen ?? 0)}</strong>
              <small>{ekran?.ozet.bekleyenAdedi ?? 0} adet bekleyen fatura</small>
            </article>
          </section>

          <section className="invoice-card invoice-card--list">
            <div className="invoice-tools-shell">
              <div className="invoice-list-tools">
                <label className="invoice-search">
                  <Search size={20} />
                  <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Fatura, cari veya açıklama ara..." />
                </label>
                <button
                  className={`invoice-btn invoice-btn--filter ${filtreAcik ? "active" : ""}`}
                  type="button"
                  onClick={() => {
                    setFiltreAcik((current) => !current);
                    setTarihPanelAcik(false);
                  }}
                >
                  <Filter size={18} /> Filtreler
                </button>
                <button
                  className={`invoice-date-range ${tarihPanelAcik ? "active" : ""}`}
                  type="button"
                  onClick={() => {
                    setTarihPanelAcik((current) => !current);
                    setFiltreAcik(false);
                  }}
                >
                  <CalendarDays size={18} />
                  <span>{tarihKisaBic(baslangic)} - {tarihKisaBic(bitis)}</span>
                </button>
                <button className="invoice-btn invoice-btn--primary" onClick={yeniTaslak} disabled={islemde}>
                  <Plus size={19} /> Yeni Fatura
                </button>
              </div>

              {filtreAcik && (
                <div className="invoice-filter-panel">
                  <label>
                    <span>Tip</span>
                    <select value={tipFiltresi} onChange={(event) => setTipFiltresi(event.target.value)}>
                      <option value="Tumu">Tüm Tipler</option>
                      <option value="Satis">Satış</option>
                      <option value="Alis">Alış</option>
                    </select>
                  </label>
                  <label>
                    <span>Durum</span>
                    <select value={durumFiltresi} onChange={(event) => setDurumFiltresi(event.target.value)}>
                      <option value="Tumu">Tüm Durumlar</option>
                      <option value="YerelTaslak">Taslak</option>
                      <option value="PortalTaslak">GİB Taslak</option>
                      <option value="Kesildi">Bekliyor</option>
                      <option value="KismiOdendi">Kısmi Ödendi</option>
                      <option value="Odendi">Ödendi</option>
                      <option value="Iptal">İptal</option>
                    </select>
                  </label>
                  <button type="button" onClick={() => {
                    setTipFiltresi("Tumu");
                    setDurumFiltresi("Tumu");
                  }}>
                    Temizle
                  </button>
                </div>
              )}

              {tarihPanelAcik && (
                <div className="invoice-date-panel">
                  <label>
                    <span>Başlangıç</span>
                    <input type="date" value={baslangic} onChange={(event) => setBaslangic(event.target.value)} />
                  </label>
                  <label>
                    <span>Bitiş</span>
                    <input type="date" value={bitis} onChange={(event) => setBitis(event.target.value)} />
                  </label>
                  <button type="button" onClick={() => {
                    setBaslangic(ayBasi());
                    setBitis(bugun());
                  }}>
                    Bu Ay
                  </button>
                </div>
              )}
            </div>

            <div className="invoice-table-wrap">
              <table className="invoice-table">
                <thead>
                  <tr>
                    <th className="invoice-table__check"><input type="checkbox" aria-label="Tümünü seç" /></th>
                    <th>No</th>
                    <th>Tarih</th>
                    <th>Tip</th>
                    <th>Cari</th>
                    <th>Toplam</th>
                    <th>Ödenen</th>
                    <th>Durum</th>
                    <th className="invoice-table__menu"></th>
                  </tr>
                </thead>
                <tbody>
                  {filtreliFaturalar.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="invoice-empty">Liste boş.</td>
                    </tr>
                  ) : (
                    filtreliFaturalar.map((row) => (
                      <tr
                        key={row.id}
                        className={row.id === seciliId ? "secili" : ""}
                        onClick={() => faturaSec(row.id).catch((error: Error) => setHata(error.message))}
                      >
                        <td className="invoice-table__check"><input type="checkbox" aria-label="Fatura seç" /></td>
                        <td>{row.no || `FAT-${row.id}`}</td>
                        <td>{tarihBic(row.tarih)}</td>
                        <td><span className={`invoice-type ${row.faturaTipi === "Alis" ? "buy" : ""}`}>{etiketBic(row.faturaTipi)}</span></td>
                        <td>{row.cariUnvan}</td>
                        <td>{paraBic(row.genelToplam)}</td>
                        <td>{paraBic(row.odenenTutar)}</td>
                        <td><span className={`invoice-pill ${row.durum}`}>{durumEtiketi(row.durum)}</span></td>
                        <td className="invoice-table__menu"><MoreVertical size={18} /></td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
            <div className="invoice-table-footer">
              <span>Toplam {filtreliFaturalar.length} kayıt</span>
              <span>20 / sayfa</span>
            </div>
          </section>
        </div>

        <aside className="invoice-side">
          <section className="invoice-card invoice-form-card">
            <div className="invoice-card__header">
              <h2>Yeni Fatura Taslağı</h2>
              <ChevronUp size={20} />
            </div>

            <div className="invoice-form-section">
              <h3><i /> Genel Bilgiler</h3>
              <div className="invoice-form-grid">
                <label className="invoice-field">
                  <span>Fatura Tipi</span>
                  <select value={form.faturaTipi} onChange={(event) => formGuncelle("faturaTipi", event.target.value)}>
                    {ekran?.faturaTipleri.map((item) => <option key={item.deger} value={item.deger}>{etiketBic(item.etiket)}</option>)}
                  </select>
                </label>
                <label className="invoice-field">
                  <span>Cari</span>
                  <select value={form.cariKartId} onChange={(event) => formGuncelle("cariKartId", event.target.value)}>
                    <option value="0">Cari seçin...</option>
                    {ekran?.cariler.map((item) => <option key={item.id} value={item.id}>{item.unvan}</option>)}
                  </select>
                </label>
                <label className="invoice-field">
                  <span>Tarih</span>
                  <input type="date" value={form.tarih} onChange={(event) => formGuncelle("tarih", event.target.value)} />
                </label>
                <label className="invoice-field">
                  <span>Ödeme Yöntemi</span>
                  <select value={form.odemeYontemi} onChange={(event) => formGuncelle("odemeYontemi", event.target.value)}>
                    {ekran?.odemeYontemleri.map((item) => <option key={item.deger} value={item.deger}>{etiketBic(item.etiket)}</option>)}
                  </select>
                </label>
                <label className="invoice-check">
                  <input type="checkbox" checked={form.vadeVar} onChange={(event) => formGuncelle("vadeVar", event.target.checked)} />
                  <span>Vade var</span>
                </label>
                <label className="invoice-field">
                  <span>Vade</span>
                  <input type="date" value={form.vadeTarihi} disabled={!form.vadeVar} onChange={(event) => formGuncelle("vadeTarihi", event.target.value)} />
                </label>
                <label className="invoice-field invoice-field--full">
                  <span>Açıklama</span>
                  <input value={form.aciklama} onChange={(event) => formGuncelle("aciklama", event.target.value)} placeholder="Açıklama giriniz..." />
                </label>
              </div>
            </div>

            <div className="invoice-form-section">
              <h3><i /> Ürün Bilgileri</h3>
              <div className="invoice-form-grid invoice-form-grid--three">
                <label className="invoice-field">
                  <span>Ürün</span>
                  <select value={form.urunHizmetId} onChange={(event) => urunSec(event.target.value)}>
                    <option value="0">Ürün seçin...</option>
                    {ekran?.urunler.map((item) => <option key={item.id} value={item.id}>{item.ad}</option>)}
                  </select>
                </label>
                <label className="invoice-field">
                  <span>Birim</span>
                  <input value={form.birim} onChange={(event) => formGuncelle("birim", event.target.value)} />
                </label>
                <label className="invoice-field">
                  <span>Miktar</span>
                  <input value={form.miktar} onChange={(event) => formGuncelle("miktar", event.target.value)} />
                </label>
                <label className="invoice-field">
                  <span>Birim Fiyat (KDV dahil)</span>
                  <input value={form.birimFiyat} onChange={(event) => formGuncelle("birimFiyat", event.target.value)} />
                </label>
                <label className="invoice-field">
                  <span>KDV %</span>
                  <input value={form.kdvOrani} onChange={(event) => formGuncelle("kdvOrani", event.target.value)} />
                </label>
                <label className="invoice-field">
                  <span>İskonto %</span>
                  <input value={form.iskontoOrani} onChange={(event) => formGuncelle("iskontoOrani", event.target.value)} />
                </label>
                <label className="invoice-check">
                  <input type="checkbox" checked={form.stokEtkilesin} onChange={(event) => formGuncelle("stokEtkilesin", event.target.checked)} />
                  <span>Stok etkilensin</span>
                </label>
              </div>
            </div>

            <div className="invoice-form-section">
              <h3><i /> İşlemler</h3>
              <div className="invoice-actions">
                <button className="invoice-btn invoice-btn--primary" onClick={taslakKaydet} disabled={islemde}>
                  <FileText size={18} /> Taslak Oluştur
                </button>
                <button className="invoice-btn" onClick={() => seciliIslem("gib-taslak", "GİB taslak")} disabled={islemde || !seciliId}>
                  <Send size={18} /> GİB Taslak
                </button>
                <button className="invoice-btn invoice-btn--success" onClick={kesOnayla} disabled={islemde || !seciliId}>
                  <CheckCircle2 size={18} /> Kes / Onayla
                </button>
                <button className="invoice-btn invoice-btn--danger" onClick={() => seciliIslem("iptal", "Fatura iptal edildi")} disabled={islemde || !seciliId}>
                  <Trash2 size={18} /> İptal
                </button>
              </div>
            </div>

            <div className="invoice-form-section invoice-payment-section">
              <h3><i /> Tahsilat / Ödeme</h3>
              <div className="invoice-payment-form">
                <label className="invoice-field">
                  <span>Tutar</span>
                  <input value={tahsilatFormu.tutar} onChange={(event) => tahsilatGuncelle("tutar", event.target.value)} />
                </label>
                <label className="invoice-field invoice-field--pay-note">
                  <span>Açıklama</span>
                  <input value={tahsilatFormu.aciklama} onChange={(event) => tahsilatGuncelle("aciklama", event.target.value)} placeholder="Açıklama giriniz..." />
                </label>
                <button className="invoice-btn invoice-btn--primary" onClick={tahsilatEkle} disabled={islemde || !seciliId}>
                  <WalletCards size={18} /> Tahsilat / Ödeme Ekle
                </button>
              </div>
            </div>
          </section>
        </aside>
      </section>

      {smsOnayi && (
        <div className="invoice-sms-backdrop" role="dialog" aria-modal="true" aria-label="GİB SMS onayı">
          <section className="invoice-sms-modal">
            <div className="invoice-sms-modal__header">
              <span><Send size={20} /></span>
              <div>
                <h2>GİB SMS Onayı</h2>
                <p>{smsOnayi.mesaj}</p>
              </div>
            </div>
            <label className="invoice-field">
              <span>SMS Kodu</span>
              <input
                autoFocus
                inputMode="numeric"
                value={smsKodu}
                onChange={(event) => setSmsKodu(event.target.value)}
                placeholder="Gelen kodu girin"
              />
            </label>
            <div className="invoice-sms-modal__actions">
              <button className="invoice-btn" type="button" disabled={islemde} onClick={() => setSmsOnayi(null)}>
                Vazgeç
              </button>
              <button className="invoice-btn invoice-btn--primary" type="button" disabled={islemde || smsKodu.trim().length === 0} onClick={smsOnayla}>
                <CheckCircle2 size={18} /> Onayı Tamamla
              </button>
            </div>
          </section>
        </div>
      )}

      {hata && (
        <div className="invoice-feedback">
          <p className="invoice-feedback__error">{hata}</p>
        </div>
      )}
    </main>
  );
}
