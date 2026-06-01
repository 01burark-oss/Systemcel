import React from "react";
import {
  CheckCircle2,
  KeyRound,
  LockKeyhole,
  RefreshCw,
  Save,
  ServerCog,
  ShieldCheck,
  TestTube2
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type { GibPortalEkranVerisi, GibPortalTestSonucu } from "./types";

interface GibPortalSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

export function GibPortalSayfasi({ yenileAnahtari }: GibPortalSayfasiProps) {
  const [ekran, setEkran] = React.useState<GibPortalEkranVerisi | null>(null);
  const [kullaniciKodu, setKullaniciKodu] = React.useState("");
  const [sifre, setSifre] = React.useState("");
  const [testModu, setTestModu] = React.useState(false);
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("GİB Portal ayarları yükleniyor...");
  const [testSonucu, setTestSonucu] = React.useState<GibPortalTestSonucu | null>(null);

  const yukle = React.useCallback(async () => {
    setHata("");
    setMesaj("GİB Portal ayarları yükleniyor...");
    const data = await jsonOku<GibPortalEkranVerisi>("/api/ekran/gib-portal");
    setEkran(data);
    setKullaniciKodu(data.kullaniciKodu);
    setSifre("");
    setTestModu(data.testModu);
    setMesaj(data.mesaj);
    setTestSonucu(null);
  }, []);

  React.useEffect(() => {
    yukle().catch((error: Error) => {
      setHata(error.message);
      setMesaj("");
    });
  }, [yukle, yenileAnahtari]);

  const payload = () => ({
    kullaniciKodu,
    sifre,
    testModu
  });

  const kaydet = async () => {
    try {
      setIslemde(true);
      setHata("");
      const data = await jsonOku<GibPortalEkranVerisi>("/api/ekran/gib-portal", {
        method: "POST",
        body: JSON.stringify(payload())
      });
      setEkran(data);
      setSifre("");
      setMesaj(data.mesaj || "GİB Portal ayarları kaydedildi.");
      setTestSonucu(null);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "GİB Portal ayarları kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  };

  const testEt = async () => {
    try {
      setIslemde(true);
      setHata("");
      setMesaj("GİB Portal bağlantısı test ediliyor...");
      const result = await jsonOku<GibPortalTestSonucu>("/api/ekran/gib-portal/test", {
        method: "POST",
        body: JSON.stringify(payload())
      });
      setTestSonucu(result);
      setSifre("");
      setMesaj(result.mesaj);
      await yukle();
      setTestSonucu(result);
    } catch (error) {
      setTestSonucu(null);
      setHata(error instanceof Error ? error.message : "GİB Portal bağlantı testi tamamlanamadı.");
    } finally {
      setIslemde(false);
    }
  };

  const sifreDurumu = ekran?.hasPassword ? "Kayıtlı şifre var" : "Şifre kayıtlı değil";
  const baglantiDurumu = testSonucu
    ? testSonucu.basarili ? "Bağlantı doğrulandı" : "Bağlantı hatası"
    : ekran?.hasPassword ? "Test bekliyor" : "Kurulum bekliyor";

  return (
    <main className="gib-page">
      <section className="gib-hero">
        <div>
          <span className="gib-eyebrow">
            <ServerCog size={18} />
            GİB e-Arşiv Portal
          </span>
          <h1>Portal bağlantısını yönet</h1>
          <p>{ekran?.aktifIsletme || "Aktif işletme"} için kullanıcı kodu ve şifreyi güvenli şekilde sakla, bağlantıyı test et.</p>
        </div>
        <button className="gib-icon-btn" disabled={islemde} type="button" onClick={yukle}>
          <RefreshCw className={islemde ? "spin" : ""} size={20} />
        </button>
      </section>

      <section className="gib-status-grid">
        <article className="gib-status-card">
          <span className="green"><ShieldCheck size={24} /></span>
          <p>Güvenli Saklama</p>
          <strong>Windows DPAPI</strong>
          <small>Şifre sadece bu cihazda çözümlenir.</small>
        </article>
        <article className="gib-status-card">
          <span className={ekran?.hasPassword ? "blue" : "amber"}><LockKeyhole size={24} /></span>
          <p>Şifre Durumu</p>
          <strong>{sifreDurumu}</strong>
          <small>Değiştirmek istemiyorsan şifre alanını boş bırak.</small>
        </article>
        <article className="gib-status-card">
          <span className={testSonucu?.basarili ? "green" : "amber"}><CheckCircle2 size={24} /></span>
          <p>Bağlantı</p>
          <strong>{baglantiDurumu}</strong>
          <small>{testModu ? "Test portalı kullanılıyor." : "Canlı portal kullanılıyor."}</small>
        </article>
      </section>

      <section className="gib-layout">
        <section className="gib-card gib-settings-card">
          <div className="gib-card__header">
            <div>
              <h2>Portal Ayarları</h2>
              <p>Bu bilgiler GİB e-Arşiv Portal'a taslak gönderme ve SMS onayı için kullanılır.</p>
            </div>
            <KeyRound size={24} />
          </div>

          <div className="gib-form">
            <label className="gib-field">
              <span>Kullanıcı Kodu</span>
              <input
                autoComplete="username"
                value={kullaniciKodu}
                onChange={(event) => setKullaniciKodu(event.target.value)}
                placeholder="GİB kullanıcı kodu"
              />
            </label>
            <label className="gib-field">
              <span>Şifre</span>
              <input
                autoComplete="current-password"
                type="password"
                value={sifre}
                onChange={(event) => setSifre(event.target.value)}
                placeholder={ekran?.hasPassword ? "Kayıtlı şifreyi korumak için boş bırak" : "GİB şifresi"}
              />
            </label>
            <label className="gib-toggle">
              <input type="checkbox" checked={testModu} onChange={(event) => setTestModu(event.target.checked)} />
              <span>
                Test portalını kullan
                <small>Gerçek kesim yapmadan bağlantı akışını denemek için.</small>
              </span>
            </label>
          </div>

          <div className="gib-actions">
            <button className="gib-btn" disabled={islemde} type="button" onClick={kaydet}>
              <Save size={18} />
              Kaydet
            </button>
            <button className="gib-btn gib-btn--primary" disabled={islemde} type="button" onClick={testEt}>
              <TestTube2 size={18} />
              Bağlantıyı Test Et
            </button>
          </div>
        </section>

        <aside className="gib-card gib-flow-card">
          <h2>Fatura Akışı</h2>
          <ol>
            <li>
              <strong>GİB Taslak</strong>
              <span>Fatura Portal taslağı olarak gönderilir ve Portal UUID kaydedilir.</span>
            </li>
            <li>
              <strong>SMS Onayı</strong>
              <span>Kes / Onayla butonu kayıtlı telefona SMS kodu gönderir.</span>
            </li>
            <li>
              <strong>Kesildi</strong>
              <span>Kod doğrulanınca fatura kesildi durumuna alınır.</span>
            </li>
          </ol>
        </aside>
      </section>

      {(mesaj || hata) && (
        <div className={`gib-feedback ${hata ? "error" : testSonucu?.basarili ? "success" : ""}`}>
          {hata || mesaj}
        </div>
      )}
    </main>
  );
}
