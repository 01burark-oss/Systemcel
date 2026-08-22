import React from "react";
import {
  CalendarDays,
  ChevronUp,
  Clock3,
  CreditCard,
  Filter,
  Mail,
  MoreVertical,
  Pencil,
  Plus,
  Save,
  Search,
  Trash2,
  WalletCards,
  X
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type {
  OdemeHatirlatmaGonderimSonucu,
  OdemeHatirlatmaOnizleme,
  TahsilatOdemeEkranVerisi,
  TahsilatOdemeFormu,
  TahsilatOdemeListeKaydi
} from "./types";

interface TahsilatOdemeSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

interface ApiMesaj {
  mesaj: string;
}

function bugun() {
  return yerelTarihDegeri();
}

function ayBasi() {
  const now = new Date();
  return yerelTarihDegeri(new Date(now.getFullYear(), now.getMonth(), 1));
}

function yerelTarihDegeri(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function bosForm(tarih = bugun()): TahsilatOdemeFormu {
  return {
    islemTipi: "Tahsilat",
    cariKartId: "0",
    tarih,
    odemeYontemi: "Nakit",
    vadeVar: false,
    vadeTarihi: tarih,
    aciklama: "",
    tutar: "0",
    paraBirimi: "TRY",
    referansNo: "",
    kategori: "Genel",
    faturaId: "0",
    faturaIleEslestir: false,
    hizliNot: ""
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

function durumEtiketi(durum: string) {
  return durum === "Tamamlandi" ? "Tamamlandı" : etiketBic(durum || "Taslak");
}

function etiketBic(value: string) {
  switch (value) {
    case "Odeme":
      return "Ödeme";
    case "Tamamlandi":
      return "Tamamlandı";
    case "Iptal":
      return "İptal";
    case "Alis":
      return "Alış";
    case "Satis":
      return "Satış";
    case "Kredi Karti":
      return "Kredi Kartı";
    case "Online Odeme":
      return "Online Ödeme";
    default:
      return value;
  }
}

function odemeYontemiDegeri(value: string) {
  const normalized = value.toLocaleLowerCase("tr-TR").replaceAll(" ", "");
  if (normalized === "kredikartı" || normalized === "kredikarti") return "KrediKarti";
  if (normalized === "onlineödeme" || normalized === "onlineodeme") return "OnlineOdeme";
  if (normalized === "havale") return "Havale";
  return "Nakit";
}

function kisaTarihBic(tarih: string) {
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

export function TahsilatOdemeSayfasi({ yenileAnahtari }: TahsilatOdemeSayfasiProps) {
  const [ekran, setEkran] = React.useState<TahsilatOdemeEkranVerisi | null>(null);
  const [form, setForm] = React.useState<TahsilatOdemeFormu>(() => bosForm());
  const [arama, setArama] = React.useState("");
  const [filtreAcik, setFiltreAcik] = React.useState(false);
  const [tipFiltresi, setTipFiltresi] = React.useState("Tum");
  const [baslangic] = React.useState(ayBasi());
  const [bitis] = React.useState(bugun());
  const [hata, setHata] = React.useState("");
  const [, setDurum] = React.useState("Tahsilat/ödeme verileri yükleniyor...");
  const [islemde, setIslemde] = React.useState(false);
  const [formPaneliAcik, setFormPaneliAcik] = React.useState(true);
  const [hatirlatmaFaturaId, setHatirlatmaFaturaId] = React.useState<number | null>(null);
  const [hatirlatma, setHatirlatma] = React.useState<OdemeHatirlatmaOnizleme | null>(null);
  const [hatirlatmaYukleniyor, setHatirlatmaYukleniyor] = React.useState(false);
  const [hatirlatmaGonderiliyor, setHatirlatmaGonderiliyor] = React.useState(false);
  const [hatirlatmaHatasi, setHatirlatmaHatasi] = React.useState("");
  const [hatirlatmaSonucu, setHatirlatmaSonucu] = React.useState("");
  const [duzenlenenHareket, setDuzenlenenHareket] = React.useState<TahsilatOdemeListeKaydi | null>(null);
  const [silinecekHareket, setSilinecekHareket] = React.useState<TahsilatOdemeListeKaydi | null>(null);
  const [siliniyor, setSiliniyor] = React.useState(false);

  const filtreliHareketler = React.useMemo(() => {
    const query = arama.trim().toLocaleLowerCase("tr-TR");
    return (ekran?.hareketler ?? []).filter((row) => {
      const tarih = row.tarih.slice(0, 10);
      const matchesSearch =
        !query ||
        row.no.toLocaleLowerCase("tr-TR").includes(query) ||
        row.cariUnvan.toLocaleLowerCase("tr-TR").includes(query) ||
        row.aciklama.toLocaleLowerCase("tr-TR").includes(query);
      const matchesDate = (!baslangic || tarih >= baslangic) && (!bitis || tarih <= bitis);
      const matchesType =
        tipFiltresi === "Tum" ||
        (tipFiltresi === "Bekleyen" ? row.durum === "Bekliyor" : row.tip === tipFiltresi);
      return matchesSearch && matchesDate && matchesType;
    });
  }, [arama, baslangic, bitis, ekran, tipFiltresi]);

  const yenile = React.useCallback(async () => {
    setHata("");
    setDurum("Tahsilat/ödeme verileri yükleniyor...");
    const data = await jsonOku<TahsilatOdemeEkranVerisi>("/api/ekran/tahsilat-odeme");
    setEkran(data);
    setForm((current) => ({
      ...current,
      tarih: current.tarih || data.bugun,
      vadeTarihi: current.vadeTarihi || data.bugun
    }));
    setDurum(data.hareketler.length === 0 ? "Kayıt yok. Yeni tahsilat/ödeme ekleyebilirsiniz." : `${data.hareketler.length} kayıt hazır.`);
  }, []);

  React.useEffect(() => {
    yenile().catch((error: Error) => {
      setHata(error.message);
      setDurum("");
    });
  }, [yenile, yenileAnahtari]);

  const formGuncelle = <K extends keyof TahsilatOdemeFormu>(key: K, value: TahsilatOdemeFormu[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const seciliFatura = React.useMemo(
    () => ekran?.faturalar.find((row) => row.id === Number(form.faturaId)) ?? null,
    [ekran, form.faturaId]
  );

  const faturaFormunaAktar = React.useCallback((faturaId: number) => {
    const fatura = ekran?.faturalar.find((row) => row.id === faturaId);
    if (!fatura) {
      setForm((current) => ({
        ...current,
        faturaId: "0",
        faturaIleEslestir: false
      }));
      return;
    }

    const islemTipi = fatura.faturaTipi === "Alis" ? "Odeme" : "Tahsilat";
    const aciklama = `${fatura.no} için ${etiketBic(islemTipi).toLocaleLowerCase("tr-TR")}`;

    setHata("");
    setDurum(`${fatura.no} seçildi. Kalan tutar forma aktarıldı.`);
    setDuzenlenenHareket(null);
    setFormPaneliAcik(true);
    setForm((current) => ({
      ...current,
      faturaId: String(fatura.id),
      faturaIleEslestir: true,
      cariKartId: String(fatura.cariKartId),
      islemTipi,
      tutar: fatura.kalan > 0 ? String(fatura.kalan) : current.tutar,
      odemeYontemi: fatura.odemeYontemi || current.odemeYontemi || "Nakit",
      kategori: "Fatura",
      referansNo: current.referansNo && !current.faturaIleEslestir ? current.referansNo : fatura.no,
      aciklama: current.aciklama && !current.faturaIleEslestir ? current.aciklama : aciklama
    }));
  }, [ekran]);

  const faturaSecimiDegisti = (value: string) => {
    const faturaId = Number(value);
    if (!faturaId) {
      setForm((current) => ({
        ...current,
        faturaId: "0",
        faturaIleEslestir: false,
        referansNo: current.faturaIleEslestir ? "" : current.referansNo,
        kategori: current.faturaIleEslestir ? "Genel" : current.kategori
      }));
      return;
    }

    faturaFormunaAktar(faturaId);
  };

  const faturaEslestirmeDegisti = (checked: boolean) => {
    if (checked && seciliFatura) {
      faturaFormunaAktar(seciliFatura.id);
      return;
    }

    formGuncelle("faturaIleEslestir", checked);
  };

  const bekleyenFaturaSec = (row: TahsilatOdemeListeKaydi) => {
    if (row.kaynak !== "Fatura" || row.id >= 0) {
      return;
    }

    faturaFormunaAktar(Math.abs(row.id));
  };

  const hatirlatmaAc = async (row: TahsilatOdemeListeKaydi) => {
    const faturaId = Math.abs(row.id);
    setHatirlatmaFaturaId(faturaId);
    setHatirlatma(null);
    setHatirlatmaHatasi("");
    setHatirlatmaSonucu("");
    setHatirlatmaYukleniyor(true);
    try {
      const preview = await jsonOku<OdemeHatirlatmaOnizleme>(`/api/ekran/tahsilat-odeme/faturalar/${faturaId}/hatirlatma`);
      setHatirlatma(preview);
    } catch (error) {
      setHatirlatmaHatasi(error instanceof Error ? error.message : "Hatırlatma hazırlanamadı.");
    } finally {
      setHatirlatmaYukleniyor(false);
    }
  };

  const hatirlatmaKapat = () => {
    if (hatirlatmaGonderiliyor) return;
    setHatirlatmaFaturaId(null);
    setHatirlatma(null);
    setHatirlatmaHatasi("");
    setHatirlatmaSonucu("");
  };

  const hatirlatmaGonder = async () => {
    if (!hatirlatmaFaturaId || !hatirlatma?.gonderilebilir) return;
    setHatirlatmaGonderiliyor(true);
    setHatirlatmaHatasi("");
    try {
      const result = await jsonOku<OdemeHatirlatmaGonderimSonucu>(`/api/ekran/tahsilat-odeme/faturalar/${hatirlatmaFaturaId}/hatirlatma`, {
        method: "POST"
      });
      setHatirlatmaSonucu(result.mesaj);
      setHatirlatma((current) => current ? { ...current, gonderilebilir: false, engel: "Bu faturanın hatırlatması son 24 saat içinde gönderildi.", sonGonderimAt: result.gonderildiAt } : current);
    } catch (error) {
      setHatirlatmaHatasi(error instanceof Error ? error.message : "Hatırlatma gönderilemedi.");
    } finally {
      setHatirlatmaGonderiliyor(false);
    }
  };

  const hareketDuzenle = (row: TahsilatOdemeListeKaydi) => {
    setHata("");
    setDurum(`${row.no} düzenleniyor.`);
    setDuzenlenenHareket(row);
    setFormPaneliAcik(true);
    setForm({
      ...bosForm(row.tarih.slice(0, 10)),
      islemTipi: row.tip,
      cariKartId: String(row.cariKartId),
      tarih: row.tarih.slice(0, 10),
      odemeYontemi: odemeYontemiDegeri(row.odemeYontemi),
      aciklama: row.aciklama,
      tutar: String(row.tutar)
    });
  };

  const hareketSil = async () => {
    if (!silinecekHareket) return;
    try {
      setSiliniyor(true);
      setHata("");
      const result = await jsonOku<ApiMesaj>(`/api/ekran/tahsilat-odeme/${silinecekHareket.id}`, {
        method: "DELETE"
      });
      if (duzenlenenHareket?.id === silinecekHareket.id) {
        setDuzenlenenHareket(null);
        setForm(bosForm(ekran?.bugun || bugun()));
      }
      setSilinecekHareket(null);
      await yenile();
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Tahsilat/ödeme silinemedi.");
    } finally {
      setSiliniyor(false);
    }
  };

  const kaydet = async () => {
    try {
      setIslemde(true);
      setHata("");
      const payload = {
        islemTipi: form.islemTipi,
        cariKartId: Number(form.cariKartId),
        tarih: form.tarih,
        odemeYontemi: form.odemeYontemi,
        vadeTarihi: form.vadeVar ? form.vadeTarihi : "",
        aciklama: form.aciklama,
        tutar: sayiyaCevir(form.tutar),
        paraBirimi: form.paraBirimi,
        referansNo: form.referansNo,
        kategori: form.kategori,
        faturaId: Number(form.faturaId),
        faturaIleEslestir: form.faturaIleEslestir,
        hizliNot: form.hizliNot
      };
      const result = await jsonOku<ApiMesaj>(duzenlenenHareket
        ? `/api/ekran/tahsilat-odeme/${duzenlenenHareket.id}`
        : "/api/ekran/tahsilat-odeme", {
        method: duzenlenenHareket ? "PUT" : "POST",
        body: JSON.stringify(payload)
      });
      setDurum(result.mesaj);
      setDuzenlenenHareket(null);
      setForm(bosForm(ekran?.bugun || bugun()));
      await yenile();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Tahsilat/ödeme eklenemedi.");
    } finally {
      setIslemde(false);
    }
  };

  const yeniForm = () => {
    setHata("");
    setDurum("Yeni tahsilat/ödeme hazır.");
    setDuzenlenenHareket(null);
    setForm(bosForm(ekran?.bugun || bugun()));
    setFormPaneliAcik(true);
  };

  return (
    <main className="payment-page">
      <section className={`payment-layout ${formPaneliAcik ? "payment-layout--panel-open" : "payment-layout--panel-closed"}`}>
        <div className="payment-left">
          <div className="payment-stats">
            <StatCard
              className="green"
              icon={<WalletCards size={30} />}
              title="Toplam Tahsilat"
              value={paraBic(ekran?.ozet.toplamTahsilat ?? 0)}
              note={`${ekran?.ozet.tahsilatAdedi ?? 0} adet tahsilat`}
            />
            <StatCard
              className="red"
              icon={<CreditCard size={30} />}
              title="Toplam Ödeme"
              value={paraBic(ekran?.ozet.toplamOdeme ?? 0)}
              note={`${ekran?.ozet.odemeAdedi ?? 0} adet ödeme`}
            />
            <StatCard
              className="amber"
              icon={<Clock3 size={30} />}
              title="Bekleyen İşlem"
              value={paraBic(ekran?.ozet.bekleyen ?? 0)}
              note={`${ekran?.ozet.bekleyenAdedi ?? 0} adet bekleyen`}
            />
          </div>

          <section className="payment-card payment-card--list">
            <div className="payment-tools-shell">
              <div className="payment-list-tools">
                <label className="payment-search">
                  <Search size={20} />
                  <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Tahsilat, ödeme veya açıklama ara..." />
                </label>
                <button
                  className={`payment-btn payment-btn--filter ${filtreAcik ? "active" : ""}`}
                  type="button"
                  onClick={() => setFiltreAcik((current) => !current)}
                >
                  <Filter size={18} />
                  Filtreler
                </button>
                <button className="payment-date-range" type="button">
                  <CalendarDays size={18} />
                  <span>{kisaTarihBic(baslangic)} - {kisaTarihBic(bitis)}</span>
                </button>
                <button className="payment-btn payment-btn--primary payment-btn--new" type="button" onClick={yeniForm}>
                  <Plus size={19} />
                  Yeni İşlem
                </button>
              </div>

              {filtreAcik && (
                <div className="payment-filter-panel">
                  {[
                    ["Tum", "Tümü"],
                    ["Tahsilat", "Tahsilat"],
                    ["Odeme", "Ödeme"],
                    ["Bekleyen", "Bekleyen"]
                  ].map(([value, label]) => (
                    <button
                      key={value}
                      className={tipFiltresi === value ? "active" : ""}
                      type="button"
                      onClick={() => {
                        setTipFiltresi(value);
                      }}
                    >
                      {label}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <PaymentTable
              rows={filtreliHareketler}
              onDelete={setSilinecekHareket}
              onEdit={hareketDuzenle}
              onInvoiceSelect={bekleyenFaturaSec}
              onReminder={hatirlatmaAc}
            />

            <div className="payment-table-footer">
              <span>Toplam {filtreliHareketler.length} kayıt</span>
              <span>20 / sayfa</span>
            </div>
          </section>
        </div>

        {formPaneliAcik ? <aside className="payment-side payment-side--drawer">
          <section className="payment-card payment-form-card">
            <div className="payment-card__header">
              <h2>{duzenlenenHareket ? "İşlemi düzenle" : "Yeni işlem"}</h2>
              <button
                type="button"
                className="side-panel-close"
                onClick={() => setFormPaneliAcik(false)}
                aria-label="Tahsilat ve ödeme panelini kapat"
                title="Paneli kapat"
              >
                <ChevronUp size={21} />
              </button>
            </div>

            <FormSection title="İşlem bilgileri">
              <div className="payment-form-grid">
                <label className="payment-field">
                  <span>İşlem Tipi</span>
                  <select disabled={Boolean(duzenlenenHareket && duzenlenenHareket.kaynak !== "Manuel")} value={form.islemTipi} onChange={(event) => formGuncelle("islemTipi", event.target.value)}>
                    {(ekran?.islemTipleri ?? []).map((option) => (
                      <option key={option.deger} value={option.deger}>
                        {etiketBic(option.etiket)}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-field">
                  <span>Cari</span>
                  <select disabled={Boolean(duzenlenenHareket && duzenlenenHareket.kaynak !== "Manuel")} value={form.cariKartId} onChange={(event) => formGuncelle("cariKartId", event.target.value)}>
                    <option value="0">Cari seçin...</option>
                    {(ekran?.cariler ?? []).map((option) => (
                      <option key={option.id} value={option.id}>
                        {option.unvan}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-field">
                  <span>Tarih</span>
                  <input value={form.tarih} onChange={(event) => formGuncelle("tarih", event.target.value)} type="date" />
                </label>
                <label className="payment-field">
                  <span>Ödeme Yöntemi</span>
                  <select value={form.odemeYontemi} onChange={(event) => formGuncelle("odemeYontemi", event.target.value)}>
                    {(ekran?.odemeYontemleri ?? []).map((option) => (
                      <option key={option.deger} value={option.deger}>
                        {etiketBic(option.etiket)}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-check">
                  <input
                    checked={form.vadeVar}
                    onChange={(event) => formGuncelle("vadeVar", event.target.checked)}
                    type="checkbox"
                  />
                  <span>Vade var</span>
                </label>
                <label className="payment-field">
                  <span>Vade</span>
                  <input
                    disabled={!form.vadeVar}
                    value={form.vadeTarihi}
                    onChange={(event) => formGuncelle("vadeTarihi", event.target.value)}
                    type="date"
                  />
                </label>
                <label className="payment-field payment-field--full">
                  <span>Açıklama</span>
                  <textarea
                    value={form.aciklama}
                    onChange={(event) => formGuncelle("aciklama", event.target.value)}
                    placeholder="Açıklama giriniz..."
                  />
                </label>
              </div>
            </FormSection>

            <FormSection title="İşlem Bilgileri">
              <div className="payment-form-grid payment-form-grid--three">
                <label className="payment-field">
                  <span>Tutar</span>
                  <input inputMode="decimal" value={form.tutar} onChange={(event) => formGuncelle("tutar", event.target.value)} />
                </label>
                <label className="payment-field">
                  <span>Para Birimi</span>
                  <select value={form.paraBirimi} onChange={(event) => formGuncelle("paraBirimi", event.target.value)}>
                    {(ekran?.paraBirimleri ?? []).map((option) => (
                      <option key={option.deger} value={option.deger}>
                        {option.etiket}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-field">
                  <span>Referans No</span>
                  <input value={form.referansNo} onChange={(event) => formGuncelle("referansNo", event.target.value)} />
                </label>
                <label className="payment-field">
                  <span>Kategori</span>
                  <select value={form.kategori} onChange={(event) => formGuncelle("kategori", event.target.value)}>
                    {(ekran?.kategoriler ?? []).map((option) => (
                      <option key={option.deger} value={option.deger}>
                        {option.etiket}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-field payment-field--wide">
                  <span>Belge / Fatura</span>
                  <select value={form.faturaId} onChange={(event) => faturaSecimiDegisti(event.target.value)}>
                    <option value="0">Fatura seçin...</option>
                    {(ekran?.faturalar ?? []).map((option) => (
                      <option key={option.id} value={option.id}>
                        {option.no} - {option.cariUnvan} - {paraBic(option.kalan)}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-check">
                  <input
                    checked={form.faturaIleEslestir}
                    onChange={(event) => faturaEslestirmeDegisti(event.target.checked)}
                    type="checkbox"
                  />
                  <span>Fatura ile eşleştir</span>
                </label>
                {seciliFatura && (
                  <div className="payment-invoice-summary">
                    <strong>{seciliFatura.no}</strong>
                    <span>{seciliFatura.cariUnvan}</span>
                    <em>Kalan: {paraBic(seciliFatura.kalan)}</em>
                  </div>
                )}
              </div>
            </FormSection>

            <FormSection title="İşlemler">
              <div className="payment-actions">
                <button className="payment-btn payment-btn--primary" disabled={islemde} type="button" onClick={kaydet}>
                  <Save size={17} />
                  {duzenlenenHareket ? "Güncelle" : "Kaydet"}
                </button>
                <button className="payment-btn payment-btn--danger" disabled={islemde} type="button" onClick={yeniForm}>
                  <Trash2 size={17} />
                  İptal
                </button>
              </div>
            </FormSection>

            <FormSection title="Hızlı Not">
              <div className="payment-note-form">
                <label className="payment-field">
                  <span>Not</span>
                  <textarea
                    value={form.hizliNot}
                    onChange={(event) => formGuncelle("hizliNot", event.target.value)}
                    placeholder="Kısa not giriniz..."
                  />
                </label>
                <button className="payment-btn payment-btn--primary" disabled={islemde} type="button" onClick={kaydet}>
                  <Save size={17} />
                  {duzenlenenHareket ? "İşlemi Güncelle" : "Tahsilat / Ödeme Ekle"}
                </button>
              </div>
            </FormSection>
          </section>
        </aside> : null}
      </section>

      {hatirlatmaFaturaId ? (
        <OdemeHatirlatmaModal
          error={hatirlatmaHatasi}
          loading={hatirlatmaYukleniyor}
          onClose={hatirlatmaKapat}
          onSend={hatirlatmaGonder}
          preview={hatirlatma}
          sending={hatirlatmaGonderiliyor}
          success={hatirlatmaSonucu}
        />
      ) : null}

      {silinecekHareket ? (
        <div className="payment-confirm-modal" role="dialog" aria-modal="true" aria-labelledby="payment-delete-title">
          <section className="payment-confirm-modal__panel">
            <span className="payment-confirm-modal__icon"><Trash2 size={22} /></span>
            <h2 id="payment-delete-title">İşlem silinsin mi?</h2>
            <p><strong>{silinecekHareket.no}</strong> numaralı {silinecekHareket.tip === "Odeme" ? "ödeme" : "tahsilat"} kaydı silinecek.</p>
            {silinecekHareket.kaynak !== "Manuel" ? <small>Bağlı fatura bakiyesi ve kasa kaydı otomatik güncellenecek.</small> : null}
            <div className="payment-confirm-modal__actions">
              <button className="payment-btn" type="button" onClick={() => setSilinecekHareket(null)} disabled={siliniyor}>Vazgeç</button>
              <button className="payment-btn payment-btn--danger" type="button" onClick={hareketSil} disabled={siliniyor}>
                <Trash2 size={16} />
                {siliniyor ? "Siliniyor…" : "Sil"}
              </button>
            </div>
          </section>
        </div>
      ) : null}

      {hata && (
        <p className="payment-feedback">
          <span className="payment-feedback__error">{hata}</span>
        </p>
      )}
    </main>
  );
}

function StatCard({
  className,
  icon,
  note,
  title,
  value
}: {
  className: string;
  icon: React.ReactNode;
  note: string;
  title: string;
  value: string;
}) {
  return (
    <article className="payment-stat">
      <span className={`payment-stat__icon ${className}`}>{icon}</span>
      <p>{title}</p>
      <strong>{value}</strong>
      <small>{note}</small>
    </article>
  );
}

function FormSection({ children, title }: { children: React.ReactNode; title: string }) {
  return (
    <section className="payment-form-section">
      <h3>
        <i />
        {title}
      </h3>
      {children}
    </section>
  );
}

function PaymentTable({
  onDelete,
  onEdit,
  onInvoiceSelect,
  onReminder,
  rows
}: {
  onDelete: (row: TahsilatOdemeListeKaydi) => void;
  onEdit: (row: TahsilatOdemeListeKaydi) => void;
  onInvoiceSelect: (row: TahsilatOdemeListeKaydi) => void;
  onReminder: (row: TahsilatOdemeListeKaydi) => void;
  rows: TahsilatOdemeListeKaydi[];
}) {
  const [acikMenuId, setAcikMenuId] = React.useState<number | null>(null);

  React.useEffect(() => {
    if (acikMenuId === null) return;
    const close = () => setAcikMenuId(null);
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape") close();
    };
    window.addEventListener("click", close);
    window.addEventListener("keydown", escape);
    return () => {
      window.removeEventListener("click", close);
      window.removeEventListener("keydown", escape);
    };
  }, [acikMenuId]);

  return (
    <div className={`payment-table-wrap${rows.length === 0 ? " payment-table-wrap--empty" : ""}`}>
      <table className="payment-table">
        <colgroup>
          <col style={{ width: "80px" }} />
          <col style={{ width: "104px" }} />
          <col style={{ width: "92px" }} />
          <col />
          <col style={{ width: "128px" }} />
          <col style={{ width: "124px" }} />
          <col style={{ width: "112px" }} />
          <col style={{ width: "204px" }} />
        </colgroup>
        <thead>
          <tr>
            <th>No</th>
            <th>
              Tarih
              <ChevronUp className="payment-sort" size={13} />
            </th>
            <th>Tip</th>
            <th>Cari</th>
            <th>Yöntem</th>
            <th>Tutar</th>
            <th>Durum</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td className="payment-empty" colSpan={8} aria-label="Liste boş" />
            </tr>
          ) : (
            rows.map((row, index) => (
              <tr key={row.id}>
                <td>{row.no}</td>
                <td>{tarihBic(row.tarih)}</td>
                <td>
                  <span className={`payment-type ${row.tip === "Odeme" ? "out" : ""}`}>
                    {row.tip === "Odeme" ? "Ödeme" : "Tahsilat"}
                  </span>
                </td>
                <td title={row.cariUnvan}>{row.cariUnvan}</td>
                <td>{etiketBic(row.odemeYontemi)}</td>
                <td>{paraBic(row.tutar)}</td>
                <td>
                  <span className={`payment-pill ${row.durum === "Bekliyor" ? "waiting" : row.durum === "Iptal" ? "danger" : "done"}`}>
                    {durumEtiketi(row.durum)}
                  </span>
                </td>
                <td className="payment-table__action">
                  {row.durum === "Bekliyor" && row.kaynak === "Fatura" ? (
                    <div className="payment-row-actions">
                      {row.tip === "Tahsilat" ? (
                        <button className="payment-row-action payment-row-action--reminder" type="button" onClick={() => onReminder(row)}>
                          <Mail size={14} />
                          Hatırlat
                        </button>
                      ) : null}
                      <button className="payment-row-action" type="button" onClick={() => onInvoiceSelect(row)}>
                        {row.tip === "Odeme" ? "Öde" : "Tahsil Et"}
                      </button>
                    </div>
                  ) : (
                    <div className="payment-row-menu" onClick={(event) => event.stopPropagation()}>
                      <button
                        className="payment-row-menu__trigger"
                        type="button"
                        aria-label={`${row.no} işlemleri`}
                        aria-expanded={acikMenuId === row.id}
                        onClick={() => setAcikMenuId((current) => current === row.id ? null : row.id)}
                      >
                        <MoreVertical size={18} />
                      </button>
                      {acikMenuId === row.id ? (
                        <div className={`payment-row-menu__dropdown${index >= rows.length - 2 ? " payment-row-menu__dropdown--up" : ""}`} role="menu">
                          <button type="button" role="menuitem" onClick={() => { setAcikMenuId(null); onEdit(row); }}>
                            <Pencil size={15} /> Düzenle
                          </button>
                          <button className="danger" type="button" role="menuitem" onClick={() => { setAcikMenuId(null); onDelete(row); }}>
                            <Trash2 size={15} /> Sil
                          </button>
                        </div>
                      ) : null}
                    </div>
                  )}
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

function OdemeHatirlatmaModal({
  error,
  loading,
  onClose,
  onSend,
  preview,
  sending,
  success
}: {
  error: string;
  loading: boolean;
  onClose: () => void;
  onSend: () => void;
  preview: OdemeHatirlatmaOnizleme | null;
  sending: boolean;
  success: string;
}) {
  return (
    <div className="payment-reminder-modal" role="dialog" aria-modal="true" aria-labelledby="payment-reminder-title">
      <section className="payment-reminder-modal__panel">
        <div className="payment-reminder-modal__header">
          <div>
            <span className="payment-reminder-modal__icon"><Mail size={20} /></span>
            <h2 id="payment-reminder-title">Ödeme hatırlatması</h2>
          </div>
          <button type="button" onClick={onClose} aria-label="Kapat" disabled={sending}><X size={20} /></button>
        </div>

        {loading ? <p className="payment-reminder-modal__loading">Hatırlatma hazırlanıyor…</p> : null}
        {!loading && preview ? (
          <>
            <dl className="payment-reminder-summary">
              <div><dt>Alıcı</dt><dd>{preview.cariUnvan}<small>{preview.aliciEposta || "E-posta yok"}</small></dd></div>
              <div><dt>Fatura</dt><dd>{preview.faturaNo}<small>Vade: {preview.vadeTarihi ? tarihBic(preview.vadeTarihi) : "Eklenmemiş"}</small></dd></div>
              <div><dt>Kalan</dt><dd>{paraBic(preview.kalanTutar)}</dd></div>
            </dl>

            <section className="payment-reminder-preview" aria-label="Gönderilecek e-posta">
              <span>Konu</span>
              <strong>{preview.konu}</strong>
              {preview.mesaj ? <p>{preview.mesaj}</p> : null}
            </section>

            {preview.engel && !success ? <p className="payment-reminder-modal__notice">{preview.engel}</p> : null}
          </>
        ) : null}
        {error ? <p className="payment-reminder-modal__error" role="alert">{error}</p> : null}
        {success ? <p className="payment-reminder-modal__success" role="status">{success}</p> : null}

        <div className="payment-reminder-modal__actions">
          <button className="payment-btn" type="button" onClick={onClose} disabled={sending}>{success ? "Kapat" : "Vazgeç"}</button>
          {!success ? (
            <button className="payment-btn payment-btn--primary" type="button" onClick={onSend} disabled={sending || loading || !preview?.gonderilebilir}>
              <Mail size={16} />
              {sending ? "Gönderiliyor…" : "Hatırlatmayı gönder"}
            </button>
          ) : null}
        </div>
      </section>
    </div>
  );
}
