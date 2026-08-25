import React from "react";
import { Bell, Loader2 } from "lucide-react";
import { jsonOku } from "../../shared/json";

interface BildirimTercihleri {
  uygulamaAktif: boolean;
  epostaAktif: boolean;
  telegramAktif: boolean;
  sessizSaatAktif: boolean;
  sessizBaslangicDakika: number;
  sessizBitisDakika: number;
  saatDilimi: "Europe/Istanbul";
}

function saat(value: number) {
  return `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;
}

function dakika(value: string) {
  const [hour, minute] = value.split(":").map(Number);
  return hour * 60 + minute;
}

export function BildirimTercihleriPaneli() {
  const [model, setModel] = React.useState<BildirimTercihleri | null>(null);
  const [islemde, setIslemde] = React.useState(false);
  const [mesaj, setMesaj] = React.useState("");

  React.useEffect(() => {
    jsonOku<BildirimTercihleri>("/api/ekran/bildirim-tercihleri")
      .then(setModel)
      .catch(() => setMesaj("Bildirim tercihleri yüklenemedi."));
  }, []);

  const kaydet = async () => {
    if (!model) return;
    try {
      setIslemde(true);
      setMesaj("");
      setModel(await jsonOku<BildirimTercihleri>("/api/ekran/bildirim-tercihleri", { method: "PUT", body: JSON.stringify(model) }));
      setMesaj("Bildirim tercihleri kaydedildi.");
    } catch {
      setMesaj("Bildirim tercihleri kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  };

  if (!model) return mesaj ? <p role="alert">{mesaj}</p> : null;
  return (
    <section className="settings-card" aria-labelledby="notification-settings-title">
      <header><Bell size={20} /><div><h2 id="notification-settings-title">Bildirim tercihleri</h2><p>Kanalları ve rahatsız edilmek istemediğiniz saatleri seçin.</p></div></header>
      <div className="settings-form-grid">
        <label><input type="checkbox" checked={model.uygulamaAktif} onChange={(e) => setModel({ ...model, uygulamaAktif: e.target.checked })} /> Uygulama içi</label>
        <label><input type="checkbox" checked={model.epostaAktif} onChange={(e) => setModel({ ...model, epostaAktif: e.target.checked })} /> E-posta</label>
        <label><input type="checkbox" checked={model.telegramAktif} onChange={(e) => setModel({ ...model, telegramAktif: e.target.checked })} /> Telegram</label>
        <label><input type="checkbox" checked={model.sessizSaatAktif} onChange={(e) => setModel({ ...model, sessizSaatAktif: e.target.checked })} /> Sessiz saatleri kullan</label>
        {model.sessizSaatAktif ? <>
          <label><span>Başlangıç</span><input type="time" value={saat(model.sessizBaslangicDakika)} onChange={(e) => setModel({ ...model, sessizBaslangicDakika: dakika(e.target.value) })} /></label>
          <label><span>Bitiş</span><input type="time" value={saat(model.sessizBitisDakika)} onChange={(e) => setModel({ ...model, sessizBitisDakika: dakika(e.target.value) })} /></label>
        </> : null}
      </div>
      <button className="settings-btn settings-btn--primary" type="button" disabled={islemde} onClick={kaydet}>{islemde ? <Loader2 className="spin" size={16} /> : null} Tercihleri kaydet</button>
      {mesaj ? <p role="status">{mesaj}</p> : null}
    </section>
  );
}
