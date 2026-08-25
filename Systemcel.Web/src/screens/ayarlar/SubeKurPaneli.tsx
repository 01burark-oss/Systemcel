import React from "react";
import { ArrowUpRight, Building2, CircleDollarSign, Info, Loader2 } from "lucide-react";
import { jsonOku } from "../../shared/json";
import { yeniIdempotencyAnahtari, type SubeFinansOzeti, type SubeKurDurumu } from "../../shared/subeKur";
import "./sube-kur.css";

function PlanDurumu({ metin }: { metin: string }) {
  return (
    <div className="branch-currency-upgrade">
      <p>{metin}</p>
      <a className="settings-btn settings-btn--navy" href="/app/abonelik">
        Planları gör <ArrowUpRight size={15} />
      </a>
    </div>
  );
}

export function SubeKurPaneli() {
  const [durum, setDurum] = React.useState<SubeKurDurumu | null>(null);
  const [ozet, setOzet] = React.useState<SubeFinansOzeti | null>(null);
  const [ozetSubeId, setOzetSubeId] = React.useState("");
  const [sube, setSube] = React.useState({ ad: "", kod: "" });
  const [kur, setKur] = React.useState({ paraBirimi: "USD", kur: "" });
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");

  const yukle = React.useCallback(async () => {
    const data = await jsonOku<SubeKurDurumu>("/api/ekran/sube-kur/");
    setDurum(data);
  }, []);

  const ozetiYukle = React.useCallback(async (subeId: string) => {
    const query = subeId ? `?subeId=${encodeURIComponent(subeId)}` : "";
    setOzet(await jsonOku<SubeFinansOzeti>(`/api/ekran/sube-kur/finans-ozeti${query}`));
  }, []);

  React.useEffect(() => {
    yukle().catch((error: Error) => setHata(error.message));
    ozetiYukle("").catch((error: Error) => setHata(error.message));
  }, [ozetiYukle, yukle]);

  const calistir = async (islem: () => Promise<unknown>, basari: string) => {
    try {
      setIslemde(true);
      setHata("");
      setMesaj("");
      await islem();
      await yukle();
      setMesaj(basari);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "İşlem tamamlanamadı.");
    } finally {
      setIslemde(false);
    }
  };

  const subeEkle = (event: React.FormEvent) => {
    event.preventDefault();
    const body = { ad: sube.ad.trim(), kod: sube.kod.trim().toLocaleUpperCase("tr-TR") };
    void calistir(async () => {
      await jsonOku("/api/ekran/sube-kur/subeler", {
        method: "POST",
        headers: { "Idempotency-Key": yeniIdempotencyAnahtari() },
        body: JSON.stringify(body)
      });
      setSube({ ad: "", kod: "" });
    }, "Şube eklendi.");
  };

  const kurKaydet = (event: React.FormEvent) => {
    event.preventDefault();
    const body = { paraBirimi: kur.paraBirimi.trim().toUpperCase(), kur: Number(kur.kur) };
    void calistir(async () => {
      await jsonOku("/api/ekran/sube-kur/kurlar", {
        method: "POST",
        headers: { "Idempotency-Key": yeniIdempotencyAnahtari() },
        body: JSON.stringify(body)
      });
      setKur((current) => ({ ...current, kur: "" }));
    }, `${body.paraBirimi} kuru kaydedildi.`);
  };

  if (!durum && !hata) {
    return <section className="settings-card branch-currency-loading" aria-label="Şube ve kur ayarları"><Loader2 className="spin" size={20} /> Ayarlar yükleniyor...</section>;
  }

  return (
    <section className="branch-currency-settings" aria-labelledby="branch-currency-title">
      <header className="branch-currency-settings__head">
        <div>
          <h2 id="branch-currency-title">Şubeler ve para birimleri</h2>
          <p>Yeni kayıtların hangi şubeye ve kur değerine göre işleneceğini yönetin.</p>
        </div>
        {durum?.aktifSube ? <span className="branch-currency-active">Aktif: {durum.aktifSube.ad}</span> : null}
      </header>

      <div className="branch-currency-grid">
        <article className="settings-card branch-currency-card">
          <header className="settings-card__header settings-operation-card__header">
            <span className="settings-operation-card__icon"><Building2 size={20} /></span>
            <div><h2>Şubeler</h2><p>Kayıtlar üst bardaki aktif şubeye işlenir.</p></div>
          </header>
          <div className="branch-list" role="list" aria-label="Şubeler">
            {durum?.subeler.map((row) => (
              <div className="branch-list__row" role="listitem" key={row.id}>
                <span><strong>{row.ad}</strong><small>{row.kod}</small></span>
                <span>{row.id === durum.aktifSube.id ? "Aktif" : row.aktif ? "Kullanımda" : "Pasif"}</span>
              </div>
            ))}
          </div>
          {durum?.cokluSubeAktif ? (
            <form className="settings-operation-form branch-currency-form" onSubmit={subeEkle}>
              <label><span>Şube adı</span><input aria-label="Şube adı" required maxLength={100} value={sube.ad} onChange={(e) => setSube({ ...sube, ad: e.target.value })} /></label>
              <label><span>Şube kodu</span><input aria-label="Şube kodu" required maxLength={20} value={sube.kod} onChange={(e) => setSube({ ...sube, kod: e.target.value })} /></label>
              <button className="settings-btn settings-btn--green" type="submit" disabled={islemde}>Şube ekle</button>
            </form>
          ) : <PlanDurumu metin="Birden fazla şube Kurumsal planda kullanılabilir. Merkez şube kayıtlarınıza devam eder." />}
        </article>

        <article className="settings-card branch-currency-card">
          <header className="settings-card__header settings-operation-card__header">
            <span className="settings-operation-card__icon"><CircleDollarSign size={20} /></span>
            <div><h2>Döviz kurları</h2><p>Kurlar kayıt anında sabitlenir; eski kayıtların kuru değişmez.</p></div>
          </header>
          <div className="settings-inline-notice"><Info size={17} /><span>Kuru siz girersiniz; dış kur servisi kullanılmaz.</span></div>
          <div className="currency-list" aria-label="Güncel kurlar">
            <div className="currency-list__row"><strong>TRY</strong><span>1,0000</span><small>Temel para birimi</small></div>
            {durum?.kurlar.filter((row) => row.paraBirimi !== "TRY").map((row) => (
              <div className="currency-list__row" key={`${row.paraBirimi}-${row.gecerliAt}`}>
                <strong>{row.paraBirimi}</strong>
                <span>{row.kur.toLocaleString("tr-TR", { maximumFractionDigits: 6 })}</span>
                <small>{new Date(row.gecerliAt).toLocaleDateString("tr-TR")}</small>
              </div>
            ))}
          </div>
          {durum?.cokluParaBirimiAktif ? (
            <form className="settings-operation-form branch-currency-form" onSubmit={kurKaydet}>
              <label><span>Para birimi</span><input aria-label="Para birimi" required minLength={3} maxLength={3} value={kur.paraBirimi} onChange={(e) => setKur({ ...kur, paraBirimi: e.target.value })} /></label>
              <label><span>1 birim kaç TRY?</span><input aria-label="TRY kuru" required type="number" inputMode="decimal" min="0.000001" max="1000000" step="any" value={kur.kur} onChange={(e) => setKur({ ...kur, kur: e.target.value })} /></label>
              <button className="settings-btn settings-btn--green" type="submit" disabled={islemde}>Kuru kaydet</button>
            </form>
          ) : <PlanDurumu metin="Dövizli kayıt ve kur yönetimi Kurumsal planda kullanılabilir. TRY kayıtlar etkilenmez." />}
        </article>
      </div>

      <article className="settings-card branch-summary" aria-labelledby="branch-summary-title">
        <header className="branch-summary__head">
          <div><h2 id="branch-summary-title">Şube finans özeti</h2><p>TRY karşılıkları kayıt anındaki kurla hesaplanır.</p></div>
          <label><span>Şube</span><select aria-label="Özet şubesi" value={ozetSubeId} onChange={(event) => { const value = event.target.value; setOzetSubeId(value); void ozetiYukle(value).catch((error: Error) => setHata(error.message)); }}><option value="">Tüm şubeler</option>{durum?.subeler.filter((row) => row.aktif).map((row) => <option key={row.id} value={row.id}>{row.ad}</option>)}</select></label>
        </header>
        {ozet ? <><div className="branch-summary__totals"><div><span>Gelir</span><strong>{ozet.gelirTry.toLocaleString("tr-TR", { style: "currency", currency: "TRY" })}</strong></div><div><span>Gider</span><strong>{ozet.giderTry.toLocaleString("tr-TR", { style: "currency", currency: "TRY" })}</strong></div><div><span>Net</span><strong>{ozet.netTry.toLocaleString("tr-TR", { style: "currency", currency: "TRY" })}</strong></div></div>{ozet.paraBirimleri.length ? <div className="branch-summary__currencies" aria-label="Orijinal tutarlar">{ozet.paraBirimleri.map((row) => <span key={row.paraBirimi}><strong>{row.paraBirimi}</strong> Gelir {row.gelirOrijinal.toLocaleString("tr-TR")} · Gider {row.giderOrijinal.toLocaleString("tr-TR")}</span>)}</div> : <p className="branch-summary__empty">Bu seçimde henüz gelir veya gider yok.</p>}</> : <div className="branch-summary__empty"><Loader2 className="spin" size={16} /> Özet hazırlanıyor...</div>}
      </article>

      {hata || mesaj ? <div className={`settings-operation-feedback ${hata ? "error" : ""}`} role={hata ? "alert" : "status"}>{hata || mesaj}</div> : null}
    </section>
  );
}
