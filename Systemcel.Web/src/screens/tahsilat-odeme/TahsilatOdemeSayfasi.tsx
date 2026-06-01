import React from "react";
import {
  CalendarDays,
  CheckCircle2,
  ChevronUp,
  Clock3,
  CreditCard,
  Filter,
  MoreVertical,
  Plus,
  Save,
  Search,
  Send,
  Trash2,
  WalletCards
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type {
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
  return new Date().toISOString().slice(0, 10);
}

function ayBasi() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
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
  const [durum, setDurum] = React.useState("Tahsilat/ödeme verileri yükleniyor...");
  const [islemde, setIslemde] = React.useState(false);

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
      const result = await jsonOku<ApiMesaj>("/api/ekran/tahsilat-odeme", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      setDurum(result.mesaj);
      setForm(bosForm(ekran?.bugun || bugun()));
      await yenile();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Tahsilat/ödeme eklenemedi.");
    } finally {
      setIslemde(false);
    }
  };

  const taslakOlustur = () => {
    setHata("");
    setDurum("Taslak hazır. Kaydet veya onayla ile işlemi oluşturabilirsiniz.");
  };

  const yeniForm = () => {
    setHata("");
    setDurum("Yeni tahsilat/ödeme hazır.");
    setForm(bosForm(ekran?.bugun || bugun()));
  };

  return (
    <main className="payment-page">
      <section className="payment-layout">
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
                        setFiltreAcik(false);
                      }}
                    >
                      {label}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <PaymentTable rows={filtreliHareketler} onInvoiceSelect={bekleyenFaturaSec} />

            <div className="payment-table-footer">
              <span>Toplam {filtreliHareketler.length} kayıt</span>
              <span>20 / sayfa</span>
            </div>
          </section>
        </div>

        <aside className="payment-side">
          <section className="payment-card payment-form-card">
            <div className="payment-card__header">
            <h2>Yeni Tahsilat / Ödeme</h2>
              <ChevronUp size={21} />
            </div>

            <FormSection title="Genel Bilgiler">
              <div className="payment-form-grid">
                <label className="payment-field">
                  <span>İşlem Tipi</span>
                  <select value={form.islemTipi} onChange={(event) => formGuncelle("islemTipi", event.target.value)}>
                    {(ekran?.islemTipleri ?? []).map((option) => (
                      <option key={option.deger} value={option.deger}>
                        {etiketBic(option.etiket)}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="payment-field">
                  <span>Cari</span>
                  <select value={form.cariKartId} onChange={(event) => formGuncelle("cariKartId", event.target.value)}>
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
                  <input value={form.tutar} onChange={(event) => formGuncelle("tutar", event.target.value)} />
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
                  Kaydet
                </button>
                <button className="payment-btn" disabled={islemde} type="button" onClick={taslakOlustur}>
                  <Send size={17} />
                  Taslak
                </button>
                <button className="payment-btn payment-btn--success" disabled={islemde} type="button" onClick={kaydet}>
                  <CheckCircle2 size={17} />
                  Onayla
                </button>
                <button className="payment-btn payment-btn--danger" disabled={islemde} type="button" onClick={yeniForm}>
                  <Trash2 size={17} />
                  İptal
                </button>
              </div>
            </FormSection>

            <FormSection title="Hizli Not">
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
                  Tahsilat / Ödeme Ekle
                </button>
              </div>
            </FormSection>
          </section>
        </aside>
      </section>

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
  onInvoiceSelect,
  rows
}: {
  onInvoiceSelect: (row: TahsilatOdemeListeKaydi) => void;
  rows: TahsilatOdemeListeKaydi[];
}) {
  return (
    <div className="payment-table-wrap">
      <table className="payment-table">
        <colgroup>
          <col style={{ width: "80px" }} />
          <col style={{ width: "104px" }} />
          <col style={{ width: "92px" }} />
          <col />
          <col style={{ width: "128px" }} />
          <col style={{ width: "124px" }} />
          <col style={{ width: "112px" }} />
          <col className="payment-table__action" />
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
            rows.map((row) => (
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
                    <button
                      className="payment-row-action"
                      type="button"
                      onClick={() => onInvoiceSelect(row)}
                    >
                      {row.tip === "Odeme" ? "Öde" : "Tahsil Et"}
                    </button>
                  ) : (
                    <MoreVertical size={18} />
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
