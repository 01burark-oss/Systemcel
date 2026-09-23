import React from "react";
import { AlertTriangle, Loader2, RefreshCw } from "lucide-react";
import { jsonOku } from "../../shared/json";

interface BasarisizTeslim {
  id: number;
  isletmeId: number;
  kanal: string;
  durum: string;
  denemeSayisi: number;
  sonHataKodu: string;
  updatedAt: string;
}

export function BildirimTeslimYonetimSayfasi() {
  const [kayitlar, setKayitlar] = React.useState<BasarisizTeslim[]>([]);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState<number | null>(null);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");

  const yukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      setKayitlar(await jsonOku<BasarisizTeslim[]>("/api/ekran/yonetim/bildirim-teslimleri"));
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Bildirim teslimleri yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, []);

  React.useEffect(() => {
    document.title = "Bildirim Teslimleri";
    void yukle();
  }, [yukle]);

  async function yenidenDene(id: number) {
    setIslemde(id);
    setHata("");
    setMesaj("");
    try {
      await jsonOku<null>(`/api/ekran/yonetim/bildirim-teslimleri/${id}/yeniden-dene`, { method: "POST" });
      setKayitlar((current) => current.filter((item) => item.id !== id));
      setMesaj("Bildirim yeniden gönderim sırasına alındı.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Bildirim yeniden denenemedi.");
    } finally {
      setIslemde(null);
    }
  }

  return <main className="admin-page">
    <nav className="admin-subnav" aria-label="Yönetim bölümleri">
      <a href="/app/yonetim/muhasebeci-basvurulari">Muhasebeci başvuruları</a>
      <a href="/app/yonetim/odemeler">Ödeme inceleme</a>
      <a className="active" aria-current="page" href="/app/yonetim/bildirim-teslimleri">Bildirim teslimleri</a>
      <a href="/app/yonetim/muhasebeci-aktarimlari">Muhasebeci aktarımları</a>
      <a href="/app/yonetim/destek">Destek talepleri</a>
    </nav>
    <section className="admin-page__toolbar">
      <div><h1>Bildirim teslimleri</h1><p>Başarısız teslimleri inceleyip yeniden sıraya alın.</p></div>
      <div className="admin-page__actions"><button type="button" onClick={() => void yukle()} disabled={yukleniyor || islemde !== null} aria-label="Bildirim teslimlerini yenile">
        {yukleniyor ? <Loader2 size={16} className="spin" aria-hidden="true" /> : <RefreshCw size={16} aria-hidden="true" />}
      </button></div>
    </section>
    {hata ? <p className="admin-page__error" role="alert">{hata}</p> : null}
    {mesaj ? <p className="admin-page__success" role="status">{mesaj}</p> : null}
    {yukleniyor ? <div className="admin-state" role="status"><Loader2 size={22} className="spin" aria-hidden="true" />Bildirim teslimleri yükleniyor...</div>
      : kayitlar.length === 0 ? <div className="admin-state"><AlertTriangle size={22} aria-hidden="true" />Başarısız bildirim teslimi yok.</div>
      : <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>İşletme</th><th>Kanal</th><th>Durum</th><th>Deneme</th><th>Hata kodu</th><th>Son işlem</th><th>İşlem</th></tr></thead><tbody>
        {kayitlar.map((item) => <tr key={item.id}>
          <td><strong>#{item.isletmeId}</strong></td>
          <td>{item.kanal}</td>
          <td><span className="admin-status">{item.durum === "DeadLetter" ? "Başarısız" : "Kanal hazır değil"}</span></td>
          <td>{item.denemeSayisi}</td>
          <td>{item.sonHataKodu || "—"}</td>
          <td>{new Date(item.updatedAt).toLocaleString("tr-TR")}</td>
          <td><button className="admin-btn" type="button" disabled={islemde !== null} onClick={() => void yenidenDene(item.id)}>
            {islemde === item.id ? <Loader2 size={14} className="spin" aria-hidden="true" /> : null}Yeniden dene
          </button></td>
        </tr>)}
      </tbody></table></div>}
  </main>;
}
