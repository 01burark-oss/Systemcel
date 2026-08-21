import React from "react";
import {
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  CalendarDays,
  CircleDollarSign,
  Clock3,
  Gauge,
  Pencil,
  Plus,
  RefreshCw,
  Save,
  Trash2,
  TrendingUp,
  WalletCards,
  X
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import {
  bosPlanFormu,
  durumSinifi,
  paraBic,
  projeksiyonCizgisi,
  projeksiyonOlcegi,
  riskEtiketi,
  ritimEtiketi,
  tarihBic,
  tutarOku,
  yerelTarihDegeri,
  yuzdeBic
} from "./helpers";
import type {
  CariAlacakYaslandirma,
  CariOdemeRitmi,
  FinansalGorunumEkranVerisi,
  NakitProjeksiyonHaftasi,
  PlanlananNakitFormu,
  PlanlananNakitKalemi,
  PlanlananNakitListeYaniti
} from "./types";

interface FinansalGorunumSayfasiProps {
  yenileAnahtari: number;
  ustBar?: UstBarDurumu | null;
}

interface ApiMesaj {
  mesaj?: string;
}

const CHART_WIDTH = 960;
const CHART_HEIGHT = 190;
const HIZLI_KATEGORILER = ["Kira", "Vergi", "SGK", "Maaş", "Abonelik", "Fatura"];

function planlariCoz(value: PlanlananNakitListeYaniti) {
  return Array.isArray(value) ? value : value.planlar;
}

export function FinansalGorunumSayfasi({ ustBar = null, yenileAnahtari }: FinansalGorunumSayfasiProps) {
  const [tarih, setTarih] = React.useState(yerelTarihDegeri);
  const [ekran, setEkran] = React.useState<FinansalGorunumEkranVerisi | null>(null);
  const [planlar, setPlanlar] = React.useState<PlanlananNakitKalemi[]>([]);
  const [planFormu, setPlanFormu] = React.useState<PlanlananNakitFormu>(() => bosPlanFormu());
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [kaydediliyor, setKaydediliyor] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [durum, setDurum] = React.useState("");
  const saltOkunur =
    (ustBar?.muhasebeciMusteriBaglami ?? false) && ustBar?.muhasebeciYetkiSeviyesi !== "TamIslem";

  React.useEffect(() => {
    document.title = "Finans Durumu | Systemcel";
  }, []);

  const verileriYukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      const [gorunum, planYaniti] = await Promise.all([
        jsonOku<FinansalGorunumEkranVerisi>(`/api/ekran/finansal-gorunum?referansTarihi=${encodeURIComponent(tarih)}`),
        jsonOku<PlanlananNakitListeYaniti>("/api/ekran/finansal-gorunum/nakit-planlari")
      ]);
      setEkran(gorunum);
      setPlanlar(planlariCoz(planYaniti).slice().sort((a, b) => a.ilkTarih.localeCompare(b.ilkTarih) || a.id - b.id));
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Finans bilgileri yüklenemedi.");
      setDurum("");
    } finally {
      setYukleniyor(false);
    }
  }, [tarih]);

  React.useEffect(() => {
    void verileriYukle();
  }, [verileriYukle, yenileAnahtari]);

  const paraBirimi = ekran?.paraBirimi || "TRY";
  const projeksiyon = ekran?.nakitProjeksiyonu ?? [];
  const sonHafta = projeksiyon.at(-1);
  const riskliCariler = React.useMemo(
    () => (ekran?.cariRiskleri ?? []).filter((row) => row.riskSeviyesi === "Yuksek"),
    [ekran]
  );
  const yogunCariler = React.useMemo(
    () => (ekran?.cariRiskleri ?? []).slice().sort((a, b) => b.acikAlacak - a.acikAlacak).slice(0, 5),
    [ekran]
  );

  function planFormunuGuncelle<K extends keyof PlanlananNakitFormu>(key: K, value: PlanlananNakitFormu[K]) {
    if (saltOkunur) return;
    setPlanFormu((current) => ({ ...current, [key]: value }));
  }

  function planDuzenle(row: PlanlananNakitKalemi) {
    if (saltOkunur) return;
    setHata("");
    setPlanFormu({
      id: row.id,
      ad: row.ad,
      tip: row.tip === "Gelir" ? "Gelir" : "Gider",
      ilkTarih: row.ilkTarih.slice(0, 10),
      tutar: String(row.tutar).replace(".", ","),
      tekrarTipi: row.tekrarTipi === "Haftalik" ? "Haftalik" : row.tekrarTipi === "Aylik" ? "Aylik" : "TekSefer",
      tekrarAraligi: String(row.tekrarAraligi || 1),
      bitisTarihi: row.bitisTarihi?.slice(0, 10) ?? "",
      kategori: row.kategori ?? "",
      aciklama: row.aciklama ?? "",
      aktif: row.aktif
    });
    document.getElementById("finance-plan-form-title")?.scrollIntoView({ behavior: "smooth", block: "center" });
  }

  async function planKaydet(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (saltOkunur) return;
    try {
      const tutar = tutarOku(planFormu.tutar);
      if (!planFormu.ad.trim()) throw new Error("Plan adı zorunludur.");
      if (!planFormu.ilkTarih) throw new Error("İlk tarih zorunludur.");
      const tekrarAraligi = Number(planFormu.tekrarAraligi);
      if (!Number.isInteger(tekrarAraligi) || tekrarAraligi < 1 || tekrarAraligi > 52) {
        throw new Error("Tekrar aralığı 1 ile 52 arasında olmalıdır.");
      }

      setKaydediliyor(true);
      setHata("");
      const endpoint = planFormu.id > 0
        ? `/api/ekran/finansal-gorunum/nakit-planlari/${planFormu.id}`
        : "/api/ekran/finansal-gorunum/nakit-planlari";
      const result = await jsonOku<ApiMesaj>(endpoint, {
        method: planFormu.id > 0 ? "PUT" : "POST",
        body: JSON.stringify({
          ad: planFormu.ad.trim(),
          tip: planFormu.tip,
          tutar,
          ilkTarih: planFormu.ilkTarih,
          tekrarTipi: planFormu.tekrarTipi,
          tekrarAraligi,
          bitisTarihi: planFormu.tekrarTipi === "TekSefer" || !planFormu.bitisTarihi ? null : planFormu.bitisTarihi,
          kategori: planFormu.kategori.trim(),
          aciklama: planFormu.aciklama.trim() || null,
          aktif: planFormu.aktif
        })
      });
      await verileriYukle();
      setPlanFormu(bosPlanFormu(tarih));
      setDurum(result?.mesaj || "Plan kaydedildi.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Planlanan nakit kalemi kaydedilemedi.");
    } finally {
      setKaydediliyor(false);
    }
  }

  async function planSil(row: PlanlananNakitKalemi) {
    if (saltOkunur || !window.confirm(`“${row.ad}” planı silinsin mi?`)) return;
    try {
      setKaydediliyor(true);
      setHata("");
      const result = await jsonOku<ApiMesaj>(`/api/ekran/finansal-gorunum/nakit-planlari/${row.id}`, { method: "DELETE" });
      if (planFormu.id === row.id) setPlanFormu(bosPlanFormu(tarih));
      await verileriYukle();
      setDurum(result?.mesaj || "Plan silindi.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Planlanan nakit kalemi silinemedi.");
    } finally {
      setKaydediliyor(false);
    }
  }

  const ilkYukleme = yukleniyor && ekran === null;

  return (
    <main className="finance-visibility-page" aria-busy={yukleniyor}>
      <section className="finance-toolbar" aria-label="Tarih seçimi">
        <div className="finance-toolbar__controls">
          <label className="finance-date-field">
            <CalendarDays size={16} aria-hidden="true" />
            <span>Tarih</span>
            <input type="date" value={tarih} onChange={(event) => setTarih(event.target.value)} />
          </label>
          <button type="button" className="finance-icon-button" onClick={() => void verileriYukle()} disabled={yukleniyor} aria-label="Yenile" title="Yenile">
            <RefreshCw className={yukleniyor ? "spin" : ""} size={17} />
          </button>
        </div>
      </section>

      {(durum || hata) && (
        <div className={`finance-feedback${hata ? " finance-feedback--error" : ""}`} role={hata ? "alert" : "status"} aria-live="polite">
          <span>{hata || durum}</span>
          {hata ? <button type="button" onClick={() => void verileriYukle()} disabled={yukleniyor}>Tekrar dene</button> : null}
        </div>
      )}

      {ilkYukleme ? <FinansalGorunumIskeleti /> : ekran ? (
        <>
          <section className="finance-kpis" aria-label="Finansal durum özeti">
            <KpiCard icon={<WalletCards size={18} />} label="Kasa" value={paraBic(ekran.kasaBakiyesi, paraBirimi)} tone={ekran.kasaBakiyesi < 0 ? "danger" : "ink"} />
            <KpiCard icon={<CircleDollarSign size={18} />} label="Açık alacak" value={paraBic(ekran.acikAlacakToplami, paraBirimi)} tone="lime" />
            <KpiCard icon={<Clock3 size={18} />} label="Vadesi geçmiş" value={paraBic(ekran.vadesiGecmisAlacakToplami, paraBirimi)} note={`${yuzdeBic(ekran.acikAlacakToplami > 0 ? (ekran.vadesiGecmisAlacakToplami / ekran.acikAlacakToplami) * 100 : 0)} açık alacak payı`} tone={ekran.vadesiGecmisAlacakToplami > 0 ? "danger" : "neutral"} />
            <KpiCard icon={<TrendingUp size={18} />} label="13 hafta sonra" value={paraBic(sonHafta?.kapanisBakiyesi ?? ekran.kasaBakiyesi, paraBirimi)} tone={ekran.ilkNegatifHafta ? "danger" : "positive"} />
          </section>

          <FinansalUyarilar ekran={ekran} riskliCariSayisi={riskliCariler.length} planSayisi={planlar.length} />

          <section className="finance-grid finance-grid--insights">
            <article className="finance-card finance-card--aging" aria-labelledby="finance-aging-title">
              <CardHeader title="Alacakların durumu" id="finance-aging-title" />
              <div className="finance-aging-bar" aria-hidden="true">
                {ekran.yaslandirma.map((row, index) => <span key={row.kod} className={`finance-aging-bar__segment finance-aging-bar__segment--${index + 1}`} style={{ width: `${Math.max(0, row.oran)}%` }} />)}
              </div>
              <div className="finance-aging-list" role="list" aria-label="Alacakların gecikme süresi">
                {ekran.yaslandirma.map((row, index) => (
                  <div role="listitem" key={row.kod}>
                    <span className={`finance-dot finance-dot--${index + 1}`} aria-hidden="true" />
                    <span><strong>{row.etiket}</strong><small>{row.faturaAdedi} fatura</small></span>
                    <span><strong>{paraBic(row.tutar, paraBirimi)}</strong><small>{yuzdeBic(row.oran)}</small></span>
                  </div>
                ))}
                {ekran.yaslandirma.length === 0 ? <EmptyState text="Açık alacak bulunmuyor." /> : null}
              </div>
              {ekran.cariYaslandirma.length > 0 ? <CariYaslandirmaTablosu rows={ekran.cariYaslandirma} currency={paraBirimi} /> : null}
            </article>

            <article className="finance-card finance-card--concentration" aria-labelledby="finance-concentration-title">
              <CardHeader title="Kimden alacağım var?" id="finance-concentration-title" />
              <div className="finance-concentration-summary">
                <Gauge size={24} aria-hidden="true" />
                <div><span>İlk 3 müşteri</span><strong>{yuzdeBic(ekran.yogunlasma.ilkUcCariOrani)}</strong></div>
                <span className={`finance-status finance-status--${durumSinifi(ekran.yogunlasma.riskSeviyesi)}`}>{riskEtiketi(ekran.yogunlasma.riskSeviyesi)}</span>
              </div>
              <div className="finance-concentration-list">
                {yogunCariler.map((row) => (
                  <div key={row.cariKartId}>
                    <span title={row.unvan}>{row.unvan}</span>
                    <span className="finance-progress" aria-hidden="true"><i style={{ width: `${Math.min(100, Math.max(0, row.acikAlacakOrani))}%` }} /></span>
                    <strong>{yuzdeBic(row.acikAlacakOrani)}</strong>
                  </div>
                ))}
                {yogunCariler.length === 0 ? <EmptyState text="Açık alacak yok." /> : null}
              </div>
            </article>
          </section>

          <article className="finance-card finance-card--projection" aria-labelledby="finance-projection-title">
            <CardHeader title="13 haftalık nakit tahmini" id="finance-projection-title" badge={ekran.ilkNegatifHafta ? `${ekran.ilkNegatifHafta}. haftada açık` : undefined} badgeTone={ekran.ilkNegatifHafta ? "danger" : undefined} />
            <NakitProjeksiyonGrafigi weeks={projeksiyon} currency={paraBirimi} ilkNegatifHafta={ekran.ilkNegatifHafta} />
            <NakitProjeksiyonTablosu weeks={projeksiyon} currency={paraBirimi} />
          </article>

          <article className="finance-card finance-card--rhythm" aria-labelledby="finance-rhythm-title">
            <CardHeader title="Müşterilerin ödeme durumu" id="finance-rhythm-title" />
            <div className="finance-rhythm-summary" aria-label="Risk özeti">
              <span><strong>{riskliCariler.length}</strong> riskli müşteri</span>
              <span><strong>{paraBic(ekran.vadesiGecmisAlacakToplami, paraBirimi)}</strong> vadesi geçmiş</span>
            </div>
            <div className="finance-table-wrap finance-table-wrap--rhythm">
              <table className="finance-table finance-table--rhythm">
                <caption className="sr-only">Müşterilerin ödeme durumu</caption>
                <thead><tr><th scope="col">Müşteri</th><th scope="col">Açık alacak</th><th scope="col">Vadesi geçmiş</th><th scope="col">Gecikme</th><th scope="col">Son dönem</th><th scope="col">Gidişat</th><th scope="col">Durum</th></tr></thead>
                <tbody>
                  {ekran.cariRiskleri.map((row) => <CariRiskSatiri key={row.cariKartId} row={row} currency={paraBirimi} />)}
                  {ekran.cariRiskleri.length === 0 ? <tr><td colSpan={7}><EmptyState text="Açık alacağı bulunan cari hesap yok." /></td></tr> : null}
                </tbody>
              </table>
            </div>
          </article>

          <section className="finance-plan-layout" aria-label="Nakit planı">
            <article className="finance-card finance-plan-form-card">
              <CardHeader title={planFormu.id ? "Planı düzenle" : "Yeni plan"} id="finance-plan-form-title" />
              {saltOkunur ? <div className="finance-readonly-note" role="note">Planları yalnızca görüntüleyebilirsiniz.</div> : null}
              <form className="finance-plan-form" onSubmit={planKaydet}>
                <fieldset disabled={saltOkunur || kaydediliyor}>
                  <legend>Temel bilgiler</legend>
                  <div className="finance-plan-form__grid">
                    <label className="finance-plan-form__wide"><span>Ad</span><input required maxLength={120} value={planFormu.ad} onChange={(event) => planFormunuGuncelle("ad", event.target.value)} placeholder="Aylık kira" /></label>
                    <label><span>Tip</span><select value={planFormu.tip} onChange={(event) => planFormunuGuncelle("tip", event.target.value as "Gelir" | "Gider")}><option value="Gelir">Gelir</option><option value="Gider">Gider</option></select></label>
                    <label><span>Tutar</span><input required inputMode="decimal" value={planFormu.tutar} onChange={(event) => planFormunuGuncelle("tutar", event.target.value)} placeholder="0,00" /></label>
                    <label className="finance-plan-form__wide"><span>Kategori</span><input maxLength={80} value={planFormu.kategori} onChange={(event) => planFormunuGuncelle("kategori", event.target.value)} placeholder="Vergi, kira, maaş..." /></label>
                  </div>
                  <div className="finance-category-chips" aria-label="Hızlı kategori seçimi">
                    {HIZLI_KATEGORILER.map((kategori) => <button key={kategori} type="button" className={planFormu.kategori === kategori ? "active" : ""} onClick={() => planFormunuGuncelle("kategori", kategori)}>{kategori}</button>)}
                  </div>
                </fieldset>

                <fieldset disabled={saltOkunur || kaydediliyor}>
                  <legend>Tarih ve tekrar</legend>
                  <div className="finance-plan-form__grid">
                    <label><span>İlk tarih</span><input required type="date" value={planFormu.ilkTarih} onChange={(event) => planFormunuGuncelle("ilkTarih", event.target.value)} /></label>
                    <label><span>Tekrar</span><select value={planFormu.tekrarTipi} onChange={(event) => planFormunuGuncelle("tekrarTipi", event.target.value as PlanlananNakitFormu["tekrarTipi"])}><option value="TekSefer">Tek sefer</option><option value="Haftalik">Haftalık</option><option value="Aylik">Aylık</option></select></label>
                    {planFormu.tekrarTipi !== "TekSefer" ? <label><span>Tekrar aralığı</span><input type="number" min="1" max="52" step="1" inputMode="numeric" value={planFormu.tekrarAraligi} onChange={(event) => planFormunuGuncelle("tekrarAraligi", event.target.value)} /></label> : null}
                    {planFormu.tekrarTipi !== "TekSefer" ? <label><span>Bitiş tarihi</span><input type="date" min={planFormu.ilkTarih} value={planFormu.bitisTarihi} onChange={(event) => planFormunuGuncelle("bitisTarihi", event.target.value)} /></label> : null}
                    <label className="finance-plan-form__wide"><span>Not</span><textarea maxLength={500} value={planFormu.aciklama} onChange={(event) => planFormunuGuncelle("aciklama", event.target.value)} /></label>
                    <label className="finance-plan-check"><input type="checkbox" checked={planFormu.aktif} onChange={(event) => planFormunuGuncelle("aktif", event.target.checked)} /><span>Tahmine ekle</span></label>
                  </div>
                </fieldset>

                <div className="finance-plan-form__actions">
                  {planFormu.id ? <button type="button" className="finance-button" onClick={() => setPlanFormu(bosPlanFormu(tarih))} disabled={saltOkunur || kaydediliyor}><X size={16} />Vazgeç</button> : null}
                  <button type="submit" className="finance-button finance-button--primary" disabled={saltOkunur || kaydediliyor}><Save size={16} />{planFormu.id ? "Kaydet" : "Ekle"}</button>
                </div>
              </form>
            </article>

            <article className="finance-card finance-plan-list-card" aria-labelledby="finance-plan-list-title">
              <CardHeader title="Eklenen planlar" id="finance-plan-list-title" />
              <div className="finance-plan-list">
                {planlar.map((row) => (
                  <article key={row.id}>
                    <span className={`finance-plan-list__icon finance-plan-list__icon--${row.tip === "Gelir" ? "income" : "expense"}`} aria-hidden="true">{row.tip === "Gelir" ? <ArrowUpRight size={17} /> : <ArrowDownRight size={17} />}</span>
                    <div><strong>{row.ad}</strong><span>{tarihBic(row.ilkTarih)} · {tekrarEtiketi(row.tekrarTipi, row.tekrarAraligi)}{row.kategori ? ` · ${row.kategori}` : ""}{!row.aktif ? " · Pasif" : ""}</span></div>
                    <strong>{row.tip === "Gelir" ? "+" : "−"}{paraBic(row.tutar, paraBirimi)}</strong>
                    <div className="finance-plan-list__actions">
                      <button type="button" onClick={() => planDuzenle(row)} disabled={saltOkunur || kaydediliyor} aria-label={`${row.ad} planını düzenle`}><Pencil size={15} /></button>
                      <button type="button" onClick={() => void planSil(row)} disabled={saltOkunur || kaydediliyor} aria-label={`${row.ad} planını sil`}><Trash2 size={15} /></button>
                    </div>
                  </article>
                ))}
                {planlar.length === 0 ? <div className="finance-plan-empty"><Plus size={21} /><strong>Henüz plan yok</strong></div> : null}
              </div>
            </article>
          </section>

        </>
      ) : null}
    </main>
  );
}

function FinansalGorunumIskeleti() {
  return <div className="finance-skeleton" aria-hidden="true"><div className="finance-skeleton__kpis">{Array.from({ length: 4 }, (_, index) => <i key={index} />)}</div><div className="finance-skeleton__row"><i /><i /></div><i className="finance-skeleton__chart" /></div>;
}

function KpiCard({ icon, label, note, tone, value }: { icon: React.ReactNode; label: string; note?: string; tone: string; value: string }) {
  return <article className={`finance-kpi finance-kpi--${tone}`}><span className="finance-kpi__icon" aria-hidden="true">{icon}</span><span>{label}</span><strong>{value}</strong>{note ? <small>{note}</small> : null}</article>;
}

function CardHeader({ badge, badgeTone, id, title }: { badge?: string; badgeTone?: string; id: string; title: string }) {
  return <header className="finance-card__header"><h2 id={id}>{title}</h2>{badge ? <strong className={`finance-card__badge finance-card__badge--${badgeTone ?? "neutral"}`}>{badge}</strong> : null}</header>;
}

function EmptyState({ text }: { text: string }) {
  return <p className="finance-empty">{text}</p>;
}

function tekrarEtiketi(tip: string, aralik: number) {
  if (tip === "Haftalik") return aralik > 1 ? `${aralik} haftada bir` : "Her hafta";
  if (tip === "Aylik") return aralik > 1 ? `${aralik} ayda bir` : "Her ay";
  return "Tek sefer";
}

function FinansalUyarilar({ ekran, planSayisi, riskliCariSayisi }: { ekran: FinansalGorunumEkranVerisi; planSayisi: number; riskliCariSayisi: number }) {
  const warnings: Array<{ tone: string; title: string }> = [];
  if (ekran.ilkNegatifHafta) warnings.push({ tone: "danger", title: `${ekran.ilkNegatifHafta}. haftada nakit açığı` });
  if (riskliCariSayisi > 0) warnings.push({ tone: "warning", title: `${riskliCariSayisi} riskli müşteri` });
  for (const warning of ekran.veriUyarilari) warnings.push({
    tone: "warning",
    title: warning.kayitAdedi > 0
      ? warning.kod === "VadeTarihiEksik" ? `${warning.kayitAdedi} faturanın vade tarihi eksik` : `${warning.kayitAdedi} eksik kayıt`
      : warning.mesaj
  });
  if (ekran.acikAlacakToplami === 0) warnings.push({ tone: "neutral", title: "Açık alacak yok" });
  if (planSayisi === 0) warnings.push({ tone: "neutral", title: "Nakit planı yok" });
  if (warnings.length === 0) return null;
  return <section className="finance-warnings" aria-label="Uyarılar">{warnings.map((row, index) => <article key={`${row.title}-${index}`} className={`finance-warning finance-warning--${row.tone}`}><AlertTriangle size={16} aria-hidden="true" /><strong>{row.title}</strong></article>)}</section>;
}

function CariRiskSatiri({ currency, row }: { currency: string; row: CariOdemeRitmi }) {
  const change = row.sonDonemDegisimiGunu;
  const highRisk = row.riskSeviyesi === "Yuksek";
  const slowing = row.ritimDurumu === "Kotulesiyor";
  return (
    <tr className={`${highRisk ? "finance-risk-row--high" : ""}${slowing ? " finance-risk-row--slowing" : ""}`}>
      <th scope="row"><strong>{row.unvan}</strong><small>{row.tamamlananOdemeAdedi} tamamlanan ödeme · {row.zamanindaOdemeOrani === null ? "zamanında oranı yok" : `${yuzdeBic(row.zamanindaOdemeOrani)} zamanında`}</small></th>
      <td data-label="Açık alacak">{paraBic(row.acikAlacak, currency)}<small>{yuzdeBic(row.acikAlacakOrani)} pay</small></td>
      <td data-label="Vadesi geçmiş">{paraBic(row.vadesiGecmisAlacak, currency)}<small>En uzun {row.enUzunGecikmeGunu} gün</small></td>
      <td data-label="Gecikme">{row.ortancaOdemeSapmasiGunu === null ? "—" : `${row.ortancaOdemeSapmasiGunu > 0 ? "+" : ""}${row.ortancaOdemeSapmasiGunu} gün`}<small>{row.ortancaOdemeSuresiGunu === null ? "Ödeme süresi yok" : `Genelde ${row.ortancaOdemeSuresiGunu} gün`}</small></td>
      <td data-label="Son değişim" className={change !== null && change > 0 ? "finance-value--danger" : change !== null && change < 0 ? "finance-value--positive" : ""}>{change === null ? "—" : `${change > 0 ? "+" : ""}${change} gün`}<small>{row.sonDonemOrnekAdedi}+{row.oncekiDonemOrnekAdedi} örnek</small></td>
      <td data-label="Gidişat"><span className={`finance-status finance-status--${durumSinifi(row.ritimDurumu)}`}>{ritimEtiketi(row.ritimDurumu)}</span></td>
      <td data-label="Durum"><span className={`finance-status finance-status--${durumSinifi(row.riskSeviyesi)}`}>{riskEtiketi(row.riskSeviyesi)}</span></td>
    </tr>
  );
}

function CariYaslandirmaTablosu({ currency, rows }: { currency: string; rows: CariAlacakYaslandirma[] }) {
  return <details className="finance-aging-details"><summary>Müşteri detayları</summary><div className="finance-table-wrap"><table className="finance-table finance-table--aging"><caption className="sr-only">Müşterilerin geciken alacakları</caption><thead><tr><th scope="col">Müşteri</th><th scope="col">Vadesi gelmedi</th><th scope="col">1–30</th><th scope="col">31–60</th><th scope="col">61–90</th><th scope="col">90+</th><th scope="col">Toplam</th></tr></thead><tbody>{rows.map((row) => <tr key={row.cariKartId}><th scope="row"><strong>{row.unvan}</strong><small>{row.acikFaturaAdedi} fatura · en uzun {row.enUzunGecikmeGunu} gün</small></th><td>{paraBic(row.vadesiGelmemis, currency)}</td><td>{paraBic(row.gun1Ila30, currency)}</td><td>{paraBic(row.gun31Ila60, currency)}</td><td>{paraBic(row.gun61Ila90, currency)}</td><td>{paraBic(row.gun91VeUzeri, currency)}</td><td><strong>{paraBic(row.toplam, currency)}</strong><small>{yuzdeBic(row.toplamdakiOrani)} pay</small></td></tr>)}</tbody></table></div></details>;
}

function NakitProjeksiyonGrafigi({ currency, ilkNegatifHafta, weeks }: { currency: string; ilkNegatifHafta: number | null; weeks: NakitProjeksiyonHaftasi[] }) {
  const [selected, setSelected] = React.useState<number | null>(ilkNegatifHafta ? ilkNegatifHafta - 1 : 0);
  const buttonRefs = React.useRef<Array<HTMLButtonElement | null>>([]);
  const points = projeksiyonCizgisi(weeks, CHART_WIDTH, CHART_HEIGHT);
  const { sifirYuzde } = projeksiyonOlcegi(weeks);
  const maxFlow = Math.max(1, ...weeks.flatMap((row) => [row.beklenenTahsilat + row.planlananGelir, row.beklenenOdeme + row.planlananGider]));

  React.useEffect(() => {
    if (weeks.length === 0) setSelected(null);
    else if (selected !== null && selected >= weeks.length) setSelected(ilkNegatifHafta ? ilkNegatifHafta - 1 : 0);
  }, [ilkNegatifHafta, selected, weeks.length]);

  if (weeks.length === 0) return <EmptyState text="Tahmin yok." />;
  const activeWeek = selected === null ? null : weeks[selected];

  function haftaTuslari(event: React.KeyboardEvent<HTMLButtonElement>, index: number) {
    if (event.key === "Escape") {
      setSelected(null);
      event.currentTarget.blur();
      return;
    }
    if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
    event.preventDefault();
    const next = event.key === "ArrowRight" ? Math.min(weeks.length - 1, index + 1) : Math.max(0, index - 1);
    setSelected(next);
    buttonRefs.current[next]?.focus();
  }

  return (
    <figure className="finance-chart" aria-labelledby="finance-chart-caption">
      <figcaption id="finance-chart-caption"><span><i className="income" />Giriş</span><span><i className="expense" />Çıkış</span><span><i className="balance" />Bakiye</span><span><i className="negative" />Eksi bakiye</span></figcaption>
      <div className="finance-chart__viewport">
        <div className="finance-chart__canvas">
          <div className="finance-chart__plot">
            <div className="finance-chart__negative-zone" style={{ top: `${sifirYuzde}%` }} aria-hidden="true" />
            <div className="finance-chart__zero" style={{ top: `${sifirYuzde}%` }} aria-hidden="true"><span>0</span></div>
            <div className="finance-chart__bars" aria-hidden="true">{weeks.map((row) => <div key={row.hafta}><span className="income" style={{ height: `${((row.beklenenTahsilat + row.planlananGelir) / maxFlow) * 72}%` }} /><span className="expense" style={{ height: `${((row.beklenenOdeme + row.planlananGider) / maxFlow) * 72}%` }} /></div>)}</div>
            <svg viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`} preserveAspectRatio="none" aria-hidden="true"><polyline points={points} /></svg>
            <div className="finance-chart__week-buttons">
              {weeks.map((row, index) => (
                <button key={row.hafta} ref={(node) => { buttonRefs.current[index] = node; }} type="button" className={`${selected === index ? "active" : ""}${row.kapanisBakiyesi < 0 ? " negative" : ""}`} aria-pressed={selected === index} aria-label={`${row.hafta}. hafta, kapanış ${paraBic(row.kapanisBakiyesi, currency)}`} onClick={() => setSelected(index)} onKeyDown={(event) => haftaTuslari(event, index)}>
                  {row.kapanisBakiyesi < 0 ? <AlertTriangle size={12} aria-hidden="true" /> : null}<span>{row.hafta}. hf.</span>
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>
      {activeWeek ? (
        <div className="finance-chart__details" aria-live="polite">
          <strong>{activeWeek.hafta}. hafta <small>{tarihBic(activeWeek.baslangic, true)} – {tarihBic(activeWeek.bitis, true)}</small></strong>
          <span><small>Açılış</small>{paraBic(activeWeek.acilisBakiyesi, currency)}</span>
          <span><small>Giriş</small>{paraBic(activeWeek.beklenenTahsilat + activeWeek.planlananGelir, currency)}</span>
          <span><small>Çıkış</small>{paraBic(activeWeek.beklenenOdeme + activeWeek.planlananGider, currency)}</span>
          <span className={activeWeek.netDegisim < 0 ? "finance-value--danger" : "finance-value--positive"}><small>Net</small>{paraBic(activeWeek.netDegisim, currency)}</span>
          <span className={activeWeek.kapanisBakiyesi < 0 ? "finance-value--danger" : ""}><small>Kapanış</small>{paraBic(activeWeek.kapanisBakiyesi, currency)}</span>
        </div>
      ) : <p className="finance-chart__hint">Ayrıntı için bir hafta seçin.</p>}
    </figure>
  );
}

function NakitProjeksiyonTablosu({ currency, weeks }: { currency: string; weeks: NakitProjeksiyonHaftasi[] }) {
  if (weeks.length === 0) return null;
  return <details className="finance-projection-details"><summary>Tüm haftaları göster</summary><div className="finance-table-wrap"><table className="finance-table finance-table--projection"><caption className="sr-only">13 haftalık nakit tahmini</caption><thead><tr><th scope="col">Hafta</th><th scope="col">Tarih</th><th scope="col">Açılış</th><th scope="col">Beklenen tahsilat</th><th scope="col">Planlanan gelir</th><th scope="col">Beklenen ödeme</th><th scope="col">Planlanan gider</th><th scope="col">Net değişim</th><th scope="col">Kapanış</th></tr></thead><tbody>{weeks.map((row) => <tr key={row.hafta}><th scope="row">{row.hafta}. hafta</th><td>{tarihBic(row.baslangic, true)} – {tarihBic(row.bitis, true)}</td><td>{paraBic(row.acilisBakiyesi, currency)}</td><td className="finance-value--positive">{paraBic(row.beklenenTahsilat, currency)}</td><td className="finance-value--positive">{paraBic(row.planlananGelir, currency)}</td><td>{paraBic(row.beklenenOdeme, currency)}</td><td>{paraBic(row.planlananGider, currency)}</td><td className={row.netDegisim < 0 ? "finance-value--danger" : "finance-value--positive"}>{paraBic(row.netDegisim, currency)}</td><td className={row.kapanisBakiyesi < 0 ? "finance-value--danger" : ""}>{paraBic(row.kapanisBakiyesi, currency)}</td></tr>)}</tbody></table></div></details>;
}
