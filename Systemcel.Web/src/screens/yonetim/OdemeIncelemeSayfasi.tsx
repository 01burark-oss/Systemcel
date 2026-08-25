import React from "react";
import { AlertTriangle, ChevronDown, ChevronUp, Loader2, RefreshCw } from "lucide-react";
import { jsonOku } from "../../shared/json";

interface OdemeOlayi { id: number; olayId: string; olayTipi: string; islenmeDurumu: string; saglayiciIslemReferansi: string; payloadHash: string; hataMesaji: string; alindiAt: string; }
interface OdemeIslemi { id: number; isletmeId: number; isletmeAdi: string; planKodu: string; hesapTipi: string; islemTipi: string; durum: string; odemeSaglayici: string; saglayiciOturumReferansi: string; saglayiciIslemReferansi: string; toplamTutar: number; paraBirimi: string; hataKodu: string; hataMesaji: string; updatedAt: string; olaylar: OdemeOlayi[]; }
interface OdemeInceleme { yoneticiMi: boolean; toplamSayisi: number; basariliSayisi: number; hataSayisi: number; islenemeyenOlaySayisi: number; islemler: OdemeIslemi[]; }

export function OdemeIncelemeSayfasi() {
  const [data, setData] = React.useState<OdemeInceleme | null>(null);
  const [sadeceHatalar, setSadeceHatalar] = React.useState(false);
  const [acikId, setAcikId] = React.useState<number | null>(null);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [hata, setHata] = React.useState("");
  const yukle = React.useCallback(async () => {
    setYukleniyor(true); setHata("");
    try { setData(await jsonOku<OdemeInceleme>(`/api/ekran/yonetim/odemeler${sadeceHatalar ? "?sadeceHatalar=true" : ""}`)); }
    catch (error) { setHata(error instanceof Error ? error.message : "Ödeme kayıtları yüklenemedi."); }
    finally { setYukleniyor(false); }
  }, [sadeceHatalar]);
  React.useEffect(() => { document.title = "Ödeme İnceleme"; }, []);
  React.useEffect(() => { yukle().catch(() => undefined); }, [yukle]);

  return <main className="admin-page payment-ops">
    <nav className="admin-subnav" aria-label="Yönetim bölümleri"><a href="/app/yonetim/muhasebeci-basvurulari">Muhasebeci başvuruları</a><a className="active" href="/app/yonetim/odemeler">Ödeme inceleme</a><a href="/app/yonetim/muhasebeci-aktarimlari">Muhasebeci aktarımları</a><a href="/app/yonetim/destek">Destek talepleri</a></nav>
    <section className="admin-page__toolbar"><div className="admin-page__stats" aria-label="Ödeme özeti"><Stat label="Toplam" value={data?.toplamSayisi ?? 0}/><Stat label="Başarılı" value={data?.basariliSayisi ?? 0}/><Stat label="Hatalı" value={data?.hataSayisi ?? 0}/><Stat label="İşlenemeyen olay" value={data?.islenemeyenOlaySayisi ?? 0}/></div><div className="admin-page__actions"><label className="admin-error-filter"><input type="checkbox" checked={sadeceHatalar} onChange={(event) => setSadeceHatalar(event.target.checked)}/>Yalnız hatalar</label><button type="button" onClick={() => yukle()} disabled={yukleniyor} aria-label="Yenile">{yukleniyor ? <Loader2 size={16} className="spin"/> : <RefreshCw size={16}/>}</button></div></section>
    {hata ? <p className="admin-page__error">{hata}</p> : null}
    {yukleniyor ? <div className="admin-state"><Loader2 size={22} className="spin"/><span>Ödeme kayıtları yükleniyor...</span></div> : data?.islemler.length === 0 ? <div className="admin-state"><AlertTriangle size={22}/><span>Bu filtrede ödeme kaydı yok.</span></div> : <div className="admin-table-wrap"><table className="admin-table payment-ops__table"><thead><tr><th>İşletme</th><th>İşlem</th><th>Durum</th><th>Tutar</th><th>Son güncelleme</th><th aria-label="Detay"/></tr></thead><tbody>{data?.islemler.map((islem) => { const acik = acikId === islem.id; return <React.Fragment key={islem.id}><tr><td><strong>{islem.isletmeAdi}</strong><span>#{islem.isletmeId} · {islem.hesapTipi}</span></td><td><strong>{islem.planKodu || islem.islemTipi}</strong><span>{islem.odemeSaglayici || "Sağlayıcı yok"}</span></td><td><span className={`admin-status admin-status--${islem.durum}`}>{islem.durum}</span></td><td><strong>{para(islem.toplamTutar, islem.paraBirimi)}</strong></td><td><span>{tarih(islem.updatedAt)}</span></td><td><button className="admin-row-toggle" type="button" aria-expanded={acik} aria-label={`Ödeme ${islem.id} detayını ${acik ? "kapat" : "aç"}`} onClick={() => setAcikId(acik ? null : islem.id)}>{acik ? <ChevronUp size={17}/> : <ChevronDown size={17}/>}</button></td></tr>{acik ? <tr className="payment-ops__detail"><td colSpan={6}><PaymentDetail payment={islem}/></td></tr> : null}</React.Fragment>; })}</tbody></table></div>}
  </main>;
}

function PaymentDetail({ payment }: { payment: OdemeIslemi }) { return <div className="payment-ops__detail-grid"><section><h3>Sağlayıcı referansları</h3><code>{payment.saglayiciOturumReferansi || "Oturum yok"}</code><code>{payment.saglayiciIslemReferansi || "İşlem yok"}</code></section><section><h3>Hata</h3><strong>{payment.hataKodu || "Hata yok"}</strong><p>{payment.hataMesaji || "Bu işlem için hata kaydı bulunmuyor."}</p></section><section className="payment-ops__events"><h3>Webhook olayları ({payment.olaylar.length})</h3>{payment.olaylar.length === 0 ? <p>İlişkili webhook olayı yok.</p> : payment.olaylar.map((event) => <article key={event.id}><div><strong>{event.olayTipi}</strong><span>{event.islenmeDurumu}</span></div><code>{event.olayId} · {event.payloadHash}</code><span>{tarih(event.alindiAt)}</span>{event.hataMesaji ? <p>{event.hataMesaji}</p> : null}</article>)}</section></div>; }
function Stat({ label, value }: { label: string; value: number }) { return <div><span>{label}</span><strong>{value}</strong></div>; }
function tarih(value: string) { return new Date(value).toLocaleString("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }); }
function para(value: number, currency: string) { return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currency || "TRY" }).format(value); }
