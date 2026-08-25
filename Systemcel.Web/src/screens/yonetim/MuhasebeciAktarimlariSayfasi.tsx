import React from "react";
import { AlertTriangle, Loader2, RefreshCw } from "lucide-react";
import { jsonOku } from "../../shared/json";
import { formatDate, formatMoney, useI18n } from "../../shared/i18n";

interface AktarimOzet {
  muhasebeciIsletmeId: number;
  muhasebeciAdi: string;
  aktarimDonemi: string;
  paraBirimi: string;
  alacakSayisi: number;
  tahsilEdilenTutar: number;
  platformKomisyonTutari: number;
  aktarilacakTutar: number;
  durum: string;
  aktarimReferansi: string;
  aktarildiAt?: string | null;
}

interface AktarimListe { yoneticiMi: boolean; aktarimDonemi: string; aktarimlar: AktarimOzet[]; }

export function MuhasebeciAktarimlariSayfasi() {
  const { t } = useI18n();
  const [donem, setDonem] = React.useState(() => new Date().toISOString().slice(0, 7));
  const [data, setData] = React.useState<AktarimListe | null>(null);
  const [referanslar, setReferanslar] = React.useState<Record<number, string>>({});
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState<number | null>(null);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");

  const yukle = React.useCallback(async () => {
    setYukleniyor(true); setHata("");
    try { setData(await jsonOku<AktarimListe>(`/api/ekran/yonetim/muhasebeci-aktarimlari?aktarimDonemi=${encodeURIComponent(donem)}`)); }
    catch (error) { setHata(error instanceof Error ? error.message : "Aktarım kayıtları yüklenemedi."); }
    finally { setYukleniyor(false); }
  }, [donem]);

  React.useEffect(() => { document.title = "Muhasebeci Aktarımları"; }, []);
  React.useEffect(() => { yukle().catch(() => undefined); }, [yukle]);

  async function tamamla(item: AktarimOzet) {
    const referans = (referanslar[item.muhasebeciIsletmeId] ?? "").trim();
    if (referans.length < 6) { setHata("Transfer referansı en az 6 karakter olmalıdır."); return; }
    setIslemde(item.muhasebeciIsletmeId); setHata(""); setMesaj("");
    try {
      await jsonOku<AktarimOzet>(`/api/ekran/yonetim/muhasebeci-aktarimlari/${item.muhasebeciIsletmeId}/tamamla`, {
        method: "POST",
        body: JSON.stringify({ aktarimDonemi: donem, aktarimReferansi: referans })
      });
      setMesaj(`${item.muhasebeciAdi} için ${donem} dönemi aktarılmış olarak kaydedildi.`);
      await yukle();
    } catch (error) { setHata(error instanceof Error ? error.message : "Aktarım kaydedilemedi."); }
    finally { setIslemde(null); }
  }

  const bekleyen = data?.aktarimlar.filter((item) => item.durum === "Bekliyor") ?? [];
  const bekleyenToplam = Object.entries(bekleyen.reduce<Record<string, number>>((toplamlar, item) => {
    toplamlar[item.paraBirimi] = (toplamlar[item.paraBirimi] ?? 0) + item.aktarilacakTutar;
    return toplamlar;
  }, {})).map(([paraBirimi, tutar]) => para(tutar, paraBirimi)).join(" · ") || para(0, "TRY");
  return <main className="admin-page accountant-transfers">
    <nav className="admin-subnav" aria-label={t("nav.admin")}><a href="/app/yonetim/muhasebeci-basvurulari">Muhasebeci başvuruları</a><a href="/app/yonetim/odemeler">Ödeme inceleme</a><a className="active" aria-current="page" href="/app/yonetim/muhasebeci-aktarimlari">{t("admin.transfers")}</a><a href="/app/yonetim/destek">{t("admin.support")}</a></nav>
    <p className="admin-page__success">Bu ekran banka transferi yapmaz. Bankada tamamladığınız toplu aktarımın referansını dönem kaydına işler.</p>
    <section className="admin-page__toolbar"><div className="admin-page__stats" aria-label="Aktarım özeti"><Stat label="Bekleyen muhasebeci" value={bekleyen.length}/><Stat label="Bekleyen net tutar" value={bekleyenToplam}/></div><div className="admin-page__actions"><label>Dönem <input aria-label="Aktarım dönemi" type="month" value={donem} onChange={(event) => setDonem(event.target.value)}/></label><button type="button" onClick={() => yukle()} disabled={yukleniyor} aria-label="Yenile">{yukleniyor ? <Loader2 size={16} className="spin"/> : <RefreshCw size={16}/>}</button></div></section>
    {hata ? <p className="admin-page__error" role="alert">{hata}</p> : null}{mesaj ? <p className="admin-page__success" role="status">{mesaj}</p> : null}
    {yukleniyor ? <div className="admin-state"><Loader2 size={22} className="spin"/><span>{t("admin.loading")}</span></div> : data?.aktarimlar.length === 0 ? <div className="admin-state"><AlertTriangle size={22}/><span>{t("admin.empty")}</span></div> : <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Muhasebeci</th><th>Dönem</th><th>Tahsilat</th><th>Komisyon</th><th>Net aktarım</th><th>Durum / işlem</th></tr></thead><tbody>{data?.aktarimlar.map((item) => <tr key={`${item.muhasebeciIsletmeId}-${item.paraBirimi}-${item.durum}-${item.aktarimReferansi}`}><td><strong>{item.muhasebeciAdi}</strong><span>{item.alacakSayisi} hakediş</span></td><td><strong>{item.aktarimDonemi}</strong><span>{item.paraBirimi}</span></td><td><strong>{para(item.tahsilEdilenTutar, item.paraBirimi)}</strong></td><td><strong>{para(item.platformKomisyonTutari, item.paraBirimi)}</strong></td><td><strong>{para(item.aktarilacakTutar, item.paraBirimi)}</strong></td><td>{item.durum === "Bekliyor" ? <div className="admin-table__row-actions"><input aria-label={`${item.muhasebeciAdi} transfer referansı`} value={referanslar[item.muhasebeciIsletmeId] ?? ""} onChange={(event) => setReferanslar((onceki) => ({ ...onceki, [item.muhasebeciIsletmeId]: event.target.value }))} placeholder="Banka transfer referansı"/><button className="admin-btn admin-btn--success" type="button" disabled={islemde !== null} onClick={() => tamamla(item)}>{islemde === item.muhasebeciIsletmeId ? <Loader2 size={14} className="spin"/> : null}{t("admin.save")}</button></div> : <><span className={`admin-status admin-status--${item.durum}`}>{item.durum}</span><span>{item.aktarimReferansi || "Referans yok"}{item.aktarildiAt ? ` · ${tarih(item.aktarildiAt)}` : ""}</span></>}</td></tr>)}</tbody></table></div>}
  </main>;
}

function Stat({ label, value }: { label: string; value: number | string }) { return <div><span>{label}</span><strong>{value}</strong></div>; }
function tarih(value: string) { return formatDate(value, undefined, { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }); }
function para(value: number, currency: string) { return formatMoney(value, currency); }
