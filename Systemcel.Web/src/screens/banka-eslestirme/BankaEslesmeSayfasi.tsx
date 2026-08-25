import React from "react";
import { CheckCircle2, FileSpreadsheet, Link2, RefreshCw, Upload, XCircle } from "lucide-react";
import { jsonOku } from "../../shared/json";

interface BankaHareketi {
  id: number;
  tarih: string;
  aciklama: string;
  tutar: number;
  paraBirimi: string;
  durum: "Acik" | "Eslesti" | "YokSayildi";
  eslesenKaynakTuru?: string;
  eslesenKaynakId?: number | null;
}

interface EslesmeAdayi {
  kaynakTuru: string;
  kaynakId: number;
  baslik: string;
  tutar: number;
  tarih: string;
  skor: number;
  nedenler: string[];
}

interface ImportSonucu {
  eklenen: number;
  tekrar: number;
  toplam: number;
}

export function BankaEslesmeSayfasi({ yenileAnahtari, saltOkunur = false }: { yenileAnahtari: number; saltOkunur?: boolean }) {
  const [hareketler, setHareketler] = React.useState<BankaHareketi[]>([]);
  const [dosya, setDosya] = React.useState<File | null>(null);
  const [adaylar, setAdaylar] = React.useState<EslesmeAdayi[]>([]);
  const [seciliHareket, setSeciliHareket] = React.useState<number | null>(null);
  const [seciliAday, setSeciliAday] = React.useState<EslesmeAdayi | null>(null);
  const [islemde, setIslemde] = React.useState(false);
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [upgrade, setUpgrade] = React.useState(false);

  const yukle = React.useCallback(async () => {
    try {
      setHata("");
      const data = await jsonOku<BankaHareketi[]>("/api/ekran/banka-mutabakat/hareketler");
      setHareketler(data);
      setUpgrade(false);
    } catch (error) {
      const detail = error instanceof Error ? error.message : "Banka hareketleri yüklenemedi.";
      setHata(detail);
      setUpgrade(/plan|abonelik|kullanılamaz|salt okunur/i.test(detail));
    }
  }, []);

  React.useEffect(() => { yukle().catch(() => undefined); }, [yukle, yenileAnahtari]);

  const iceAktar = async () => {
    if (!dosya) { setHata("Önce bir CSV dosyası seçin."); return; }
    try {
      setIslemde(true); setHata(""); setMesaj("");
      const form = new FormData();
      form.append("dosya", dosya);
      const result = await jsonOku<ImportSonucu>("/api/ekran/banka-mutabakat/import", { method: "POST", body: form });
      setMesaj(`${result.eklenen} hareket eklendi${result.tekrar ? `, ${result.tekrar} tekrar atlandı` : ""}.`);
      setDosya(null);
      await yukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "CSV içe aktarılamadı.");
    } finally { setIslemde(false); }
  };

  const adaylariAc = async (id: number) => {
    try {
      setIslemde(true); setHata(""); setSeciliHareket(id);
      const data = await jsonOku<EslesmeAdayi[]>(`/api/ekran/banka-mutabakat/hareketler/${id}/adaylar`);
      setAdaylar(data); setSeciliAday(data[0] ?? null);
    } catch (error) { setHata(error instanceof Error ? error.message : "Adaylar yüklenemedi."); }
    finally { setIslemde(false); }
  };

  const eslestir = async () => {
    if (!seciliHareket || !seciliAday) return;
    try {
      setIslemde(true); setHata("");
      await jsonOku(`/api/ekran/banka-mutabakat/hareketler/${seciliHareket}/eslestir`, {
        method: "POST",
        body: JSON.stringify({ kaynakTuru: seciliAday.kaynakTuru, kaynakId: seciliAday.kaynakId, onaylandi: true })
      });
      setMesaj("Eşleştirme kaydedildi. Finansal kaydın tutarı veya durumu değiştirilmedi.");
      setAdaylar([]); setSeciliAday(null); setSeciliHareket(null);
      await yukle();
    } catch (error) { setHata(error instanceof Error ? error.message : "Eşleştirme kaydedilemedi."); }
    finally { setIslemde(false); }
  };

  const yokSay = async (id: number) => {
    try {
      setIslemde(true); setHata("");
      await jsonOku(`/api/ekran/banka-mutabakat/hareketler/${id}/yok-say`, { method: "POST" });
      setMesaj("Hareket yok sayıldı."); await yukle();
    } catch (error) { setHata(error instanceof Error ? error.message : "Hareket yok sayılamadı."); }
    finally { setIslemde(false); }
  };

  return (
    <main className="bank-page">
      <section className="bank-hero">
        <div><h1>Banka hareketi eşleştirme</h1><p>Bu ilk sürüm yalnız CSV ile çalışır; bankaya doğrudan bağlanmaz.</p></div>
        <button type="button" className="bank-icon-button" onClick={yukle} disabled={islemde} aria-label="Banka hareketlerini yenile"><RefreshCw size={19} /></button>
      </section>

      {upgrade ? (
        <section className="bank-upgrade" role="status">
          <h2>Bu özellik planınızda açık değil</h2>
          <p>{hata}</p><a href="/abonelik">Planları incele</a>
        </section>
      ) : (
        <>
          <section className="bank-import" aria-labelledby="bank-import-title">
            <div><FileSpreadsheet size={25} /><h2 id="bank-import-title">CSV içe aktar</h2></div>
            <p>UTF-8 CSV yükleyin. Tarih, açıklama, tutar veya borç/alacak; isteğe bağlı para birimi ve referans sütunları desteklenir. En fazla 2 MB.</p>
            <div className="bank-import-actions">
              <label className="bank-file-field">CSV dosyası<input aria-label="CSV dosyası" type="file" accept=".csv,text/csv" disabled={saltOkunur} onChange={(event) => setDosya(event.target.files?.[0] ?? null)} /></label>
              <button type="button" onClick={iceAktar} disabled={islemde || !dosya || saltOkunur}><Upload size={18} />İçe aktar</button>
            </div>
            {saltOkunur ? <small>Bu müşteri çalışma alanında okuma yetkiniz var; içe aktarma ve durum değişiklikleri kapalıdır.</small> : null}
            {dosya ? <small>Seçilen: {dosya.name}</small> : null}
          </section>

          <section className="bank-list" aria-labelledby="bank-list-title">
            <div className="bank-section-title"><div><h2 id="bank-list-title">Banka hareketleri</h2><p>Eşleştirme öneridir; seçiminiz onaylanmadan hiçbir kayıt bağlanmaz.</p></div><span>{hareketler.length} hareket</span></div>
            {hareketler.length === 0 ? <div className="bank-empty">Henüz banka hareketi yok. Bankanızdan aldığınız CSV dosyasını yükleyin.</div> : (
              <div className="bank-table-wrap"><table><thead><tr><th>Tarih</th><th>Açıklama</th><th>Tutar</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead><tbody>
                {hareketler.map((item) => <tr key={item.id}>
                  <td>{new Date(item.tarih).toLocaleDateString("tr-TR")}</td><td>{item.aciklama}</td>
                  <td className={item.tutar < 0 ? "negative" : "positive"}>{para(item.tutar, item.paraBirimi)}</td>
                  <td><span className={`bank-status bank-status--${item.durum.toLowerCase()}`}>{durumEtiketi(item.durum)}</span></td>
                  <td>{item.durum === "Acik" ? <div className="bank-row-actions"><button type="button" onClick={() => adaylariAc(item.id)} disabled={islemde}><Link2 size={16} />Adayları gör</button><button type="button" className="subtle" onClick={() => yokSay(item.id)} disabled={islemde || saltOkunur}><XCircle size={16} />Yok say</button></div> : item.durum === "Eslesti" ? <small>{item.eslesenKaynakTuru} #{item.eslesenKaynakId}</small> : null}</td>
                </tr>)}</tbody></table></div>
            )}
          </section>

          {seciliHareket ? <section className="bank-candidates" aria-labelledby="bank-candidates-title">
            <div className="bank-section-title"><div><h2 id="bank-candidates-title">Eşleşme adayları</h2><p>Skor; tutar, tarih ve açıklama benzerliğinden deterministik hesaplanır.</p></div></div>
            {adaylar.length === 0 ? <p>Yeterince güçlü bir aday bulunamadı.</p> : <div className="bank-candidate-grid">{adaylar.map((aday) => <label className={seciliAday?.kaynakTuru === aday.kaynakTuru && seciliAday.kaynakId === aday.kaynakId ? "selected" : ""} key={`${aday.kaynakTuru}-${aday.kaynakId}`}>
              <input type="radio" name="aday" checked={seciliAday?.kaynakTuru === aday.kaynakTuru && seciliAday.kaynakId === aday.kaynakId} onChange={() => setSeciliAday(aday)} />
              <span><strong>{aday.baslik}</strong><small>{new Date(aday.tarih).toLocaleDateString("tr-TR")} · {para(aday.tutar, "TRY")}</small><em>{aday.skor}/100 · {aday.nedenler.join(" · ")}</em></span>
            </label>)}</div>}
            <div className="bank-confirm"><p><CheckCircle2 size={18} />Onay yalnız bu banka hareketini seçilen kayda bağlar; fatura, tahsilat veya cari kayıt değiştirilmez.</p><button type="button" onClick={eslestir} disabled={islemde || !seciliAday || saltOkunur}>Eşleştirmeyi onayla</button></div>
          </section> : null}
        </>
      )}

      {(mesaj || (!upgrade && hata)) ? <div className={`bank-feedback ${hata ? "error" : "success"}`} role="status">{hata || mesaj}</div> : null}
    </main>
  );
}

function para(value: number, currency: string) { return new Intl.NumberFormat("tr-TR", { style: "currency", currency }).format(value); }
function durumEtiketi(value: BankaHareketi["durum"]) { return value === "Acik" ? "Açık" : value === "Eslesti" ? "Eşleşti" : "Yok sayıldı"; }
