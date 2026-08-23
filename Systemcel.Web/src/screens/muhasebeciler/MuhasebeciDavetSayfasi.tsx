import React from "react";
import { AlertCircle, CheckCircle2, Link2, Loader2, ShieldCheck } from "lucide-react";
import systemcelBrand from "../../assets/systemcel-brand.svg";
import { useSystemcelAuth } from "../../auth/SystemcelAuthProvider";
import { jsonOku } from "../../shared/json";
import "./muhasebeci-davet.css";

interface MuhasebeciLinkDaveti {
  musteriAdi: string;
  durum: string;
  yetkiSeviyesi: string;
  mesaj: string;
  sonGecerlilikAt: string;
}

export function MuhasebeciDavetSayfasi({ token }: { token: string }) {
  const auth = useSystemcelAuth();
  const [davet, setDavet] = React.useState<MuhasebeciLinkDaveti | null>(null);
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const [aylikHizmetBedeli, setAylikHizmetBedeli] = React.useState("");

  React.useEffect(() => {
    document.title = "Muhasebeci daveti";
    jsonOku<MuhasebeciLinkDaveti>(`/api/public/muhasebeci-davetleri/${encodeURIComponent(token)}`)
      .then(setDavet)
      .catch((error: Error) => setHata(error.message));
  }, [token]);

  const signedIn = !auth.clerkEnabled || (auth.isLoaded && auth.isSignedIn);
  const returnUrl = `/muhasebeci-daveti/${encodeURIComponent(token)}`;
  const authQuery = `hesapTipi=Muhasebeci&returnUrl=${encodeURIComponent(returnUrl)}`;

  async function kabulEt() {
    const monthlyFee = Number(aylikHizmetBedeli);
    if (!Number.isFinite(monthlyFee) || monthlyFee <= 0) {
      setHata("Aylık ücreti girin.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      await jsonOku("/api/ekran/muhasebeci/link-davetleri/kabul", {
        method: "POST",
        body: JSON.stringify({ token, aylikHizmetBedeli: monthlyFee })
      });
      setDavet((current) => current ? { ...current, durum: "OdemeBekliyor" } : current);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Davet kabul edilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  const bekliyor = davet?.durum === "Beklemede";
  const odemeBekliyor = davet?.durum === "OdemeBekliyor";

  return (
    <main className="accountant-invite-page">
      <section className="accountant-invite-shell">
        <header><img src={systemcelBrand} alt="Systemcel" /></header>

        {!davet && !hata ? (
          <div className="accountant-invite-state"><Loader2 className="spin" size={28} /><p>Davet yükleniyor...</p></div>
        ) : !davet ? (
          <div className="accountant-invite-state is-error"><AlertCircle size={30} /><h1>Bağlantı açılamadı</h1><p>{hata}</p></div>
        ) : (
          <>
            <div className={`accountant-invite-intro ${odemeBekliyor ? "is-success" : ""}`}>
              {odemeBekliyor ? <CheckCircle2 size={30} /> : <Link2 size={30} />}
              <div>
                <p>{davet.musteriAdi}</p>
                <h1>{odemeBekliyor ? "Müşteri ödemesi bekleniyor" : "Sizi muhasebecisi olarak davet ediyor"}</h1>
              </div>
            </div>

            <dl className="accountant-invite-details">
              <div><dt>Erişim</dt><dd>{davet.yetkiSeviyesi === "TamIslem" ? "Tam işlem" : "Okuma + rapor"}</dd></div>
              <div><dt>Son gün</dt><dd>{new Date(davet.sonGecerlilikAt).toLocaleDateString("tr-TR")}</dd></div>
              {davet.mesaj ? <div className="is-full"><dt>Not</dt><dd>{davet.mesaj}</dd></div> : null}
            </dl>

            {bekliyor ? (
              <div className="accountant-invite-acceptance">
                {signedIn ? (
                  <>
                    <label>
                      <span>Aylık ücret</span>
                      <span><b>₺</b><input aria-label="Aylık ücret" type="number" min="1" step="0.01" inputMode="decimal" value={aylikHizmetBedeli} onChange={(event) => setAylikHizmetBedeli(event.target.value)} /></span>
                    </label>
                    <div className="accountant-invite-actions">
                      <button type="button" disabled={islemde} onClick={kabulEt}>
                        {islemde ? <Loader2 className="spin" size={17} /> : <ShieldCheck size={17} />}
                        Ödemeye gönder
                      </button>
                    </div>
                  </>
                ) : (
                  <div className="accountant-invite-actions">
                    <a className="is-primary" href={`/kayit?${authQuery}`}>Muhasebeci hesabı oluştur</a>
                    <a href={`/giris?${authQuery}`}>Giriş yap</a>
                  </div>
                )}
              </div>
            ) : null}

            {odemeBekliyor ? <a className="accountant-invite-workspace-link" href="/app/sohbetler">Sohbete git</a> : null}
            {hata ? <p className="accountant-invite-error">{hata}</p> : null}
          </>
        )}
      </section>
    </main>
  );
}
