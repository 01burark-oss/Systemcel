import React from "react";
import { Check, Copy, Link2, Loader2, Send, UsersRound, X } from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";

type YetkiSeviyesi = "OkumaRapor" | "TamIslem";

interface MuhasebeciLinkDaveti {
  davetLinki: string;
}

export function MuhasebeciBaglantiSayfasi({ ustBar }: { mobileMode?: boolean; ustBar?: UstBarDurumu | null; onUstBarYenile?: () => unknown | Promise<unknown> }) {
  const muhasebeciMi = ustBar?.hesapTipi === "Muhasebeci" && !ustBar.muhasebeciMusteriBaglami;
  const [davetAcik, setDavetAcik] = React.useState(false);
  const [yetki, setYetki] = React.useState<YetkiSeviyesi>("OkumaRapor");
  const [not, setNot] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const [davet, setDavet] = React.useState<MuhasebeciLinkDaveti | null>(null);
  const [geriBildirim, setGeriBildirim] = React.useState("");
  const [hata, setHata] = React.useState("");

  React.useEffect(() => {
    document.title = muhasebeciMi ? "Müşterilerini bağla" : "Muhasebecini bağla";
  }, [muhasebeciMi]);

  function davetiAc() {
    setYetki("OkumaRapor");
    setNot("");
    setDavet(null);
    setGeriBildirim("");
    setHata("");
    setDavetAcik(true);
  }

  async function davetOlustur(event: React.FormEvent) {
    event.preventDefault();
    setIslemde(true);
    setHata("");
    try {
      const sonuc = await jsonOku<MuhasebeciLinkDaveti>("/api/ekran/muhasebeci/link-davetleri", {
        method: "POST",
        body: JSON.stringify({ yetkiSeviyesi: yetki, mesaj: not })
      });
      setDavet(sonuc);
      setGeriBildirim("Davet bağlantısı hazır.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Davet bağlantısı oluşturulamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function kopyala() {
    if (!davet?.davetLinki) return;
    try {
      await navigator.clipboard.writeText(davet.davetLinki);
      setGeriBildirim("Davet bağlantısı kopyalandı.");
      setHata("");
    } catch {
      setHata("Bağlantı kopyalanamadı. Metni seçip kopyalayabilirsiniz.");
    }
  }

  return (
    <main className="accountant-marketplace accountant-connection-page">
      <section className="accountant-marketplace__hero">
        <div><h1>{muhasebeciMi ? "Müşterilerini bağla" : "Muhasebecini bağla"}</h1></div>
      </section>

      <section className="accountant-connection-card">
        <span className="accountant-connection-card__icon">{muhasebeciMi ? <UsersRound size={25} /> : <Link2 size={25} />}</span>
        <div>
          <h2>{muhasebeciMi ? "Mevcut müşterilerinle Systemcel'de çalış" : "Çalıştığın muhasebeciyi davet et"}</h2>
          <p>{muhasebeciMi
            ? "Müşteri ekranından davet oluştur. Müşterin kabul ettiğinde belgeleri, talepleri ve görüşmeleri aynı çalışma alanında yönetebilirsin."
            : "Davet bağlantısını muhasebecine gönder. Kabul ettiğinde hangi bilgilere erişebileceğini sen belirlersin."}</p>
        </div>
        {muhasebeciMi ? (
          <a className="accountant-primary-link" href="/app/muhasebeci/musteriler"><span>Müşterilerime git</span><Send size={18} /></a>
        ) : (
          <button className="accountant-primary-link" type="button" onClick={davetiAc}><span>Davet oluştur</span><Send size={18} /></button>
        )}
      </section>

      <section className="accountant-connection-steps" aria-label="Bağlantı adımları">
        <article><strong>1</strong><div><h3>Davet et</h3><p>Çalıştığın kişiye güvenli davet bağlantısını gönder.</p></div></article>
        <article><strong>2</strong><div><h3>Kabul edilsin</h3><p>Karşı taraf hesabıyla giriş yapıp daveti onaylasın.</p></div></article>
        <article><strong>3</strong><div><h3>Birlikte çalış</h3><p>Belgeleri ve görüşmeleri tek çalışma alanında yönet.</p></div></article>
      </section>

      {geriBildirim ? <p className="accountant-feedback accountant-feedback--success">{geriBildirim}</p> : null}
      {hata ? <p className="accountant-feedback accountant-feedback--error">{hata}</p> : null}

      {davetAcik && !muhasebeciMi ? (
        <div className="accountant-modal" role="dialog" aria-modal="true" aria-labelledby="accountant-link-invite-title">
          <form className="accountant-modal__panel accountant-link-invite-modal" onSubmit={davetOlustur}>
            <button type="button" className="accountant-modal__close" onClick={() => setDavetAcik(false)} aria-label="Kapat"><X size={18} /></button>
            <header><span className="accountant-card__icon"><Link2 size={20} /></span><div><p>Bağlantı daveti</p><h2 id="accountant-link-invite-title">Muhasebecini davet et</h2></div></header>
            {!davet ? <>
              <div className="accountant-link-invite-modal__agreement"><strong>Çalışma yetkisini belirle</strong><p>Muhasebecin daveti kabul ettiğinde seçtiğin yetkiyle işletmene bağlanır.</p></div>
              <div className="accountant-permission-choice" role="group" aria-label="Yetki seviyesi">
                <button type="button" aria-label="Okuma + rapor" className={yetki === "OkumaRapor" ? "active" : ""} onClick={() => setYetki("OkumaRapor")}><Check size={16} /><span><strong>Okuma + rapor</strong><small>Kayıtları görüntüler ve raporlar.</small></span></button>
                <button type="button" aria-label="Tam işlem" className={yetki === "TamIslem" ? "active" : ""} onClick={() => setYetki("TamIslem")}><Check size={16} /><span><strong>Tam işlem</strong><small>Kayıtları görüntüler ve düzenler.</small></span></button>
              </div>
              <label className="accountant-modal__field"><span>Not (isteğe bağlı)</span><textarea value={not} onChange={(event) => setNot(event.target.value)} rows={3} placeholder="Muhasebecine kısa bir not yaz" /></label>
              <button type="submit" className="accountant-modal__primary" disabled={islemde}>{islemde ? <Loader2 size={16} className="spin" /> : <Link2 size={16} />}<span>Davet bağlantısı oluştur</span></button>
            </> : <div className="accountant-link-invite-result">
              <div><Check size={18} /><span><strong>Bağlantı hazır</strong><small>14 gün içinde muhasebecinle paylaş.</small></span></div>
              <label><span>Davet bağlantısı</span><input value={davet.davetLinki} readOnly onFocus={(event) => event.currentTarget.select()} /></label>
              <button type="button" className="accountant-modal__primary" onClick={() => kopyala().catch(() => undefined)}><Copy size={16} /><span>Bağlantıyı kopyala</span></button>
            </div>}
          </form>
        </div>
      ) : null}
    </main>
  );
}
