import React from "react";
import { Bell, Loader2 } from "lucide-react";
import { jsonOku } from "../../shared/json";
import "./bildirim-tercihleri.css";

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
    <section className="settings-card notification-preferences" aria-labelledby="notification-settings-title">
      <header className="settings-card__header notification-preferences__header"><Bell size={20} aria-hidden="true" /><div><h2 id="notification-settings-title">Bildirim tercihleri</h2><p>Kanalları ve rahatsız edilmek istemediğiniz saatleri seçin.</p></div></header>
      <div className="notification-preferences__channels">
        {([
          ["uygulamaAktif", "Uygulama içi"],
          ["epostaAktif", "E-posta"],
          ["telegramAktif", "Telegram"],
          ["sessizSaatAktif", "Sessiz saatleri kullan"]
        ] as const).map(([key, label]) => (
          <label className="notification-preferences__row" key={key}>
            <span>{label}</span>
            <span className="notification-preferences__control">
              <span className="notification-preferences__state" aria-hidden="true">{model[key] ? "Açık" : "Kapalı"}</span>
              <span className="notification-preferences__switch">
                <input type="checkbox" role="switch" aria-label={label} checked={model[key]} disabled={islemde} onChange={(e) => setModel({ ...model, [key]: e.target.checked })} />
                <span className="notification-preferences__track" aria-hidden="true" />
              </span>
            </span>
          </label>
        ))}
      </div>
      <div className="notification-preferences__hours">
        {model.sessizSaatAktif ? <>
          <label><span>Başlangıç</span><input type="time" value={saat(model.sessizBaslangicDakika)} onChange={(e) => setModel({ ...model, sessizBaslangicDakika: dakika(e.target.value) })} /></label>
          <label><span>Bitiş</span><input type="time" value={saat(model.sessizBitisDakika)} onChange={(e) => setModel({ ...model, sessizBitisDakika: dakika(e.target.value) })} /></label>
        </> : null}
      </div>
      <div className="notification-preferences__actions"><button className="settings-btn settings-btn--primary" type="button" disabled={islemde} onClick={kaydet}>{islemde ? <Loader2 className="spin" size={16} aria-hidden="true" /> : null} Tercihleri kaydet</button></div>
      {mesaj ? <p role="status">{mesaj}</p> : null}
    </section>
  );
}
