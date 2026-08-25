import React from "react";
import { ArrowRightLeft, ClipboardCheck, Loader2, PackagePlus, RotateCcw } from "lucide-react";
import { jsonOku } from "../../shared/json";
import type { UrunStokEkranVerisi } from "./types";

type Depo = { id: number; ad: string; kod: string; konum?: string; varsayilan: boolean };
type Hareket = { id: number; islemId?: number; urunHizmetId: number; urunAdi: string; depoId?: number; depoAdi?: string; tarih: string; miktar: number; rezerveMiktar: number; hareketTipi: string; aciklama?: string; tersKayitVar: boolean };
type Defter = { depolar: Depo[]; hareketler: Hareket[]; negatifStokEngelli: boolean };

const key = () => globalThis.crypto?.randomUUID?.() ?? `stok-${Date.now()}-${Math.random().toString(16).slice(2)}`;
const sayi = (value: string) => Number(value.replace(",", "."));

export function StokDefteriSayfasi() {
  const [defter, setDefter] = React.useState<Defter | null>(null);
  const [urunler, setUrunler] = React.useState<UrunStokEkranVerisi["urunler"]>([]);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState(false);
  const [upgrade, setUpgrade] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const [depo, setDepo] = React.useState({ ad: "", kod: "", konum: "" });
  const [transfer, setTransfer] = React.useState({ urunHizmetId: "", kaynakDepoId: "", hedefDepoId: "", miktar: "", aciklama: "" });
  const [sayim, setSayim] = React.useState({ urunHizmetId: "", depoId: "", sayilanMiktar: "", aciklama: "", onaylandi: false });

  const yukle = React.useCallback(async () => {
    setYukleniyor(true); setHata("");
    try {
      const [nextDefter, urunEkrani] = await Promise.all([jsonOku<Defter>("/api/ekran/stok-defteri"), jsonOku<UrunStokEkranVerisi>("/api/ekran/urun-stok")]);
      setDefter(nextDefter); setUrunler(urunEkrani.urunler.filter((urun) => urun.aktif && urun.tip === "Urun")); setUpgrade("");
    } catch (error) {
      const detail = error instanceof Error ? error.message : "Stok defteri yüklenemedi.";
      if (/feature_not_available|planınızda açık değil|kullanılamaz|kullanılamıyor/i.test(detail)) setUpgrade(detail); else setHata(detail);
    } finally { setYukleniyor(false); }
  }, []);

  React.useEffect(() => { void yukle(); }, [yukle]);

  async function gonder(path: string, body: object, success: string) {
    setIslemde(true); setHata(""); setMesaj("");
    try { await jsonOku(path, { method: "POST", headers: { "Idempotency-Key": key() }, body: JSON.stringify(body) }); setMesaj(success); await yukle(); }
    catch (error) { const detail = error instanceof Error ? error.message : "İşlem kaydedilemedi."; if (/feature_not_available|planınızda açık değil|kullanılamaz|kullanılamıyor/i.test(detail)) setUpgrade(detail); else setHata(detail); }
    finally { setIslemde(false); }
  }

  function depoEkle(event: React.FormEvent) { event.preventDefault(); if (!depo.ad.trim() || !depo.kod.trim()) return setHata("Depo adı ve kodu zorunludur."); void gonder("/api/ekran/stok-defteri/depolar", { ad: depo.ad.trim(), kod: depo.kod.trim(), konum: depo.konum.trim() || undefined }, "Depo eklendi."); setDepo({ ad: "", kod: "", konum: "" }); }
  function transferEt(event: React.FormEvent) { event.preventDefault(); const miktar = sayi(transfer.miktar); if (!transfer.urunHizmetId || !transfer.kaynakDepoId || !transfer.hedefDepoId || !Number.isFinite(miktar) || miktar <= 0) return setHata("Transfer için ürün, iki farklı depo ve geçerli miktar seçin."); if (transfer.kaynakDepoId === transfer.hedefDepoId) return setHata("Kaynak ve hedef depo farklı olmalıdır."); void gonder("/api/ekran/stok-defteri/transferler", { urunHizmetId: Number(transfer.urunHizmetId), kaynakDepoId: Number(transfer.kaynakDepoId), hedefDepoId: Number(transfer.hedefDepoId), miktar, aciklama: transfer.aciklama.trim() }, "Transfer kaydedildi."); }
  function sayimiKaydet(event: React.FormEvent) { event.preventDefault(); const sayilanMiktar = sayi(sayim.sayilanMiktar); if (!sayim.onaylandi || !sayim.urunHizmetId || !sayim.depoId || !Number.isFinite(sayilanMiktar) || sayilanMiktar < 0) return; void gonder("/api/ekran/stok-defteri/sayimlar", { urunHizmetId: Number(sayim.urunHizmetId), depoId: Number(sayim.depoId), sayilanMiktar, onaylandi: true, aciklama: sayim.aciklama.trim() }, "Sayım farkı onaylanarak kaydedildi."); }

  if (upgrade) return <main className="stock-ledger" ><section className="stock-ledger__upgrade" role="status"><h1>Stok defteri planınızda açık değil</h1><p>{upgrade}</p><a href="/app/abonelik">Planları incele</a></section></main>;
  return <main className="stock-ledger">
    <header className="stock-ledger__hero"><div><span>Stok operasyonları</span><h1>Stok defteri</h1><p>Depo, transfer ve sayım kayıtlarını hareket geçmişi üzerinden izleyin.</p></div>{defter?.negatifStokEngelli ? <strong>Negatif stok engeli açık</strong> : null}</header>
    {hata ? <p className="stock-ledger__feedback error" role="alert">{hata}</p> : null}{mesaj ? <p className="stock-ledger__feedback" role="status">{mesaj}</p> : null}
    {yukleniyor ? <p className="stock-ledger__state" role="status"><Loader2 className="spin" size={18} /> Stok defteri yükleniyor…</p> : <>
      <section className="stock-ledger__grid">
        <section className="stock-ledger__card" aria-labelledby="depolar-title"><header><PackagePlus size={20}/><h2 id="depolar-title">Depolar</h2></header><ul className="stock-ledger__warehouses">{defter?.depolar.map((item) => <li key={item.id}><strong>{item.ad}</strong><span>{item.kod}{item.konum ? ` · ${item.konum}` : ""}{item.varsayilan ? " · Varsayılan" : ""}</span></li>)}</ul><form onSubmit={depoEkle} className="stock-ledger__form"><label>Depo adı<input value={depo.ad} onChange={(e) => setDepo({ ...depo, ad: e.target.value })} required /></label><label>Depo kodu<input value={depo.kod} onChange={(e) => setDepo({ ...depo, kod: e.target.value })} required /></label><label>Konum <small>(isteğe bağlı)</small><input value={depo.konum} onChange={(e) => setDepo({ ...depo, konum: e.target.value })} /></label><button disabled={islemde} type="submit">Depo ekle</button></form></section>
        <section className="stock-ledger__card" aria-labelledby="transfer-title"><header><ArrowRightLeft size={20}/><h2 id="transfer-title">Depolar arası transfer</h2></header><form onSubmit={transferEt} className="stock-ledger__form"><UrunSec value={transfer.urunHizmetId} onChange={(value) => setTransfer({ ...transfer, urunHizmetId: value })} label="Transfer ürünü" urunler={urunler}/><DepoSec label="Kaynak depo" value={transfer.kaynakDepoId} onChange={(value) => setTransfer({ ...transfer, kaynakDepoId: value })} depolar={defter?.depolar ?? []}/><DepoSec label="Hedef depo" value={transfer.hedefDepoId} onChange={(value) => setTransfer({ ...transfer, hedefDepoId: value })} depolar={defter?.depolar ?? []}/><label>Transfer miktarı<input aria-label="Transfer miktarı" inputMode="decimal" value={transfer.miktar} onChange={(e) => setTransfer({ ...transfer, miktar: e.target.value })} required /></label><label>Açıklama <small>(isteğe bağlı)</small><input value={transfer.aciklama} onChange={(e) => setTransfer({ ...transfer, aciklama: e.target.value })} /></label><button disabled={islemde} type="submit">Transferi kaydet</button></form></section>
        <section className="stock-ledger__card" aria-labelledby="sayim-title"><header><ClipboardCheck size={20}/><h2 id="sayim-title">Sayım onayı</h2></header><form onSubmit={sayimiKaydet} className="stock-ledger__form"><UrunSec value={sayim.urunHizmetId} onChange={(value) => setSayim({ ...sayim, urunHizmetId: value })} label="Sayım ürünü" urunler={urunler}/><DepoSec label="Sayım deposu" value={sayim.depoId} onChange={(value) => setSayim({ ...sayim, depoId: value })} depolar={defter?.depolar ?? []}/><label>Sayılan miktar<input inputMode="decimal" value={sayim.sayilanMiktar} onChange={(e) => setSayim({ ...sayim, sayilanMiktar: e.target.value })} required /></label><label>Açıklama <small>(isteğe bağlı)</small><input value={sayim.aciklama} onChange={(e) => setSayim({ ...sayim, aciklama: e.target.value })} /></label><label className="stock-ledger__confirm"><input type="checkbox" checked={sayim.onaylandi} onChange={(e) => setSayim({ ...sayim, onaylandi: e.target.checked })} /> Sayım farkını ve oluşacak kaydı onaylıyorum.</label><button disabled={islemde || !sayim.onaylandi} type="submit">Sayımı onayla</button></form></section>
      </section>
      <section className="stock-ledger__history" aria-labelledby="hareketler-title"><header><div><h2 id="hareketler-title">Hareket geçmişi</h2><p>Ters kayıt, önceki hareketi silmez; ayrı bir denge kaydı oluşturur.</p></div></header>{defter?.hareketler.length ? <div className="stock-ledger__table-wrap"><table><thead><tr><th>Tarih</th><th>Ürün</th><th>Depo</th><th>Hareket</th><th>Miktar</th><th>Rezerve</th><th><span className="sr-only">İşlem</span></th></tr></thead><tbody>{defter.hareketler.map((item) => <tr key={item.id}><td>{new Date(item.tarih).toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" })}</td><td><strong>{item.urunAdi}</strong><small>{item.aciklama}</small></td><td>{item.depoAdi ?? "—"}</td><td>{item.hareketTipi}</td><td>{item.miktar}</td><td>{item.rezerveMiktar}</td><td>{item.islemId && !item.tersKayitVar ? <button type="button" className="stock-ledger__reverse" aria-label={`${item.id} numaralı işlemi ters kaydet`} disabled={islemde} onClick={() => void gonder(`/api/ekran/stok-defteri/islemler/${item.islemId}/ters-kayit`, { aciklama: "" }, "Ters kayıt oluşturuldu.")}><RotateCcw size={15}/>Ters kayıt</button> : <small>{item.tersKayitVar ? "Ters kayıt var" : "—"}</small>}</td></tr>)}</tbody></table></div> : <p className="stock-ledger__state">Henüz stok hareketi yok.</p>}</section>
    </>}
  </main>;
}

function UrunSec({ label, value, onChange, urunler }: { label: string; value: string; onChange: (value: string) => void; urunler: UrunStokEkranVerisi["urunler"] }) { return <label>{label}<select aria-label={label} value={value} onChange={(e) => onChange(e.target.value)} required><option value="">Ürün seçin</option>{urunler.map((urun) => <option key={urun.id} value={urun.id}>{urun.ad}</option>)}</select></label>; }
function DepoSec({ label, value, onChange, depolar }: { label: string; value: string; onChange: (value: string) => void; depolar: Depo[] }) { return <label>{label}<select aria-label={label} value={value} onChange={(e) => onChange(e.target.value)} required><option value="">Depo seçin</option>{depolar.map((depo) => <option key={depo.id} value={depo.id}>{depo.ad}</option>)}</select></label>; }
