import React from "react";
import { Loader2, RefreshCw, ShieldAlert } from "lucide-react";
import { jsonOku } from "../../shared/json";
import "./MuhasebeciIletisimIncelemeleriSayfasi.css";

interface IletisimIncelemesi {
  id: number;
  muhasebeciAdi: string;
  musteriAdi: string;
  mesaj: string;
  createdAt: string;
}

export function MuhasebeciIletisimIncelemeleriSayfasi() {
  const [items, setItems] = React.useState<IletisimIncelemesi[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [busyId, setBusyId] = React.useState<number | null>(null);
  const [error, setError] = React.useState("");
  const [notice, setNotice] = React.useState("");

  const load = React.useCallback(async () => {
    setLoading(true);
    setError("");
    setNotice("");
    try {
      setItems(await jsonOku<IletisimIncelemesi[]>("/api/ekran/yonetim/muhasebeci-iletisim-incelemeleri"));
    } catch (err) {
      setError(err instanceof Error ? err.message : "İnceleme kuyruğu yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }, []);

  React.useEffect(() => {
    document.title = "Muhasebeci iletişim incelemeleri";
    load().catch(() => undefined);
  }, [load]);

  async function decide(item: IletisimIncelemesi, hasContact: boolean) {
    setBusyId(item.id);
    setError("");
    setNotice("");
    try {
      await jsonOku(`/api/ekran/yonetim/muhasebeci-iletisim-incelemeleri/${item.id}/karar`, {
        method: "POST",
        body: JSON.stringify({ iletisimBilgisiVar: hasContact })
      });
      await load();
      setNotice(hasContact ? "Talep iletişim bilgisi nedeniyle reddedildi." : "Talep inceleme için serbest bırakıldı.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Karar kaydedilemedi.");
    } finally {
      setBusyId(null);
    }
  }

  return (
    <main className="admin-page">
      <nav className="admin-subnav" aria-label="Yönetim bölümleri">
        <a href="/app/yonetim/muhasebeci-basvurulari">Muhasebeci başvuruları</a>
        <a className="active" aria-current="page" href="/app/yonetim/muhasebeci-iletisim-incelemeleri">İletişim incelemeleri</a>
        <a href="/app/yonetim/odemeler">Ödeme inceleme</a>
        <a href="/app/yonetim/muhasebeci-aktarimlari">Muhasebeci aktarımları</a>
        <a href="/app/yonetim/destek">Destek talepleri</a>
      </nav>

      <section className="admin-page__toolbar">
        <div><h1>İletişim incelemeleri</h1><p>Belirsiz eşleşmeleri değerlendirip talebi serbest bırakın veya reddedin.</p></div>
        <button type="button" onClick={() => load()} disabled={loading || busyId !== null} aria-label="Yenile">
          {loading ? <Loader2 size={16} className="spin" /> : <RefreshCw size={16} />}
        </button>
      </section>
      {error ? <p className="admin-page__error" role="alert">{error}</p> : null}
      {notice ? <p className="admin-page__success" role="status">{notice}</p> : null}
      {loading ? <div className="admin-state"><Loader2 size={22} className="spin" /><span>İnceleme kuyruğu yükleniyor...</span></div> : null}
      {!loading && items.length === 0 ? <div className="admin-state"><ShieldAlert size={24} /><span>İncelenecek talep yok.</span></div> : null}
      {!loading && items.length > 0 ? (
        <div className="admin-table-wrap contact-review-table-wrap">
          <table className="admin-table contact-review-table">
            <thead><tr><th>İşletmeler</th><th>Talep mesajı</th><th>Tarih</th><th>Karar</th></tr></thead>
            <tbody>{items.map((item) => <tr key={item.id}>
              <td><strong>{item.musteriAdi}</strong><span>Muhasebeci: {item.muhasebeciAdi}</span><span>Talep #{item.id}</span></td>
              <td className="contact-review-message">{item.mesaj || "Mesaj yok"}</td>
              <td>{formatDate(item.createdAt)}</td>
              <td className="admin-table__row-actions">
                <button type="button" className="admin-btn admin-btn--success" disabled={busyId !== null} onClick={() => decide(item, false)}>
                  {busyId === item.id ? <Loader2 size={15} className="spin" /> : null}<span>İletişim bilgisi yok</span>
                </button>
                <button type="button" className="admin-btn admin-btn--danger" disabled={busyId !== null} onClick={() => decide(item, true)}>
                  <span>İletişim bilgisi var</span>
                </button>
              </td>
            </tr>)}</tbody>
          </table>
        </div>
      ) : null}
    </main>
  );
}

function formatDate(value: string) {
  if (!value) return "-";
  return new Date(value).toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });
}
