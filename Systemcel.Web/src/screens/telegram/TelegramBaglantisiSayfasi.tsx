import React from "react";
import {
  AlertCircle,
  ChevronRight,
  CheckCircle2,
  Copy,
  ExternalLink,
  Info,
  QrCode,
  RefreshCw,
  Send,
  ShieldCheck,
  Smartphone,
  Trash2
} from "lucide-react";
import { jsonOku } from "../../shared/json";
import type { TelegramEkranVerisi } from "./types";

interface TelegramBaglantisiSayfasiProps {
  onTelegramDurumuDegisti?: () => unknown | Promise<unknown>;
}

export function TelegramBaglantisiSayfasi({ onTelegramDurumuDegisti }: TelegramBaglantisiSayfasiProps) {
  const [ekran, setEkran] = React.useState<TelegramEkranVerisi | null>(null);
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("Telegram bağlantısı yükleniyor...");
  const [qrYuklenemedi, setQrYuklenemedi] = React.useState(false);
  const sonBagliRef = React.useRef<boolean | null>(null);

  const uygula = React.useCallback((data: TelegramEkranVerisi) => {
    const oncekiBagli = sonBagliRef.current;
    sonBagliRef.current = data.bagli;
    setEkran(data);
    setMesaj(data.mesaj);

    const durumDegisti = oncekiBagli === null ? data.bagli : oncekiBagli !== data.bagli;
    if (durumDegisti)
      void onTelegramDurumuDegisti?.();
  }, [onTelegramDurumuDegisti]);

  const yukle = React.useCallback(async () => {
    setHata("");
    const data = await jsonOku<TelegramEkranVerisi>("/api/ekran/telegram");
    uygula(data);
  }, [uygula]);

  React.useEffect(() => {
    yukle().catch((error: Error) => {
      setHata(error.message);
      setMesaj("");
    });
  }, [yukle]);

  React.useEffect(() => {
    if (ekran?.bagli)
      return;

    const handle = window.setInterval(() => {
      yukle().catch(() => {
        // Sessiz yoklama: ekranda kullanicinin son mesajini bozmayalim.
      });
    }, 3_000);

    return () => window.clearInterval(handle);
  }, [ekran?.bagli, yukle]);

  React.useEffect(() => {
    setQrYuklenemedi(false);
  }, [ekran?.qrUrl]);

  const calistir = async (
    islem: () => Promise<TelegramEkranVerisi>,
    fallback: string,
    after?: (data: TelegramEkranVerisi) => void
  ) => {
    try {
      setIslemde(true);
      setHata("");
      const data = await islem();
      uygula(data);
      after?.(data);
    } catch (error) {
      setHata(error instanceof Error ? error.message : fallback);
    } finally {
      setIslemde(false);
    }
  };

  const baslat = () => calistir(
    () => jsonOku<TelegramEkranVerisi>("/api/ekran/telegram/baslat", { method: "POST" }),
    "Telegram bağlantısı başlatılamadı.",
    (data) => {
      window.open(data.baglantiLinki, "_blank", "noopener,noreferrer");
    }
  );

  const kontrolEt = () => calistir(
    () => jsonOku<TelegramEkranVerisi>("/api/ekran/telegram/kontrol", { method: "POST" }),
    "Telegram bağlantısı kontrol edilemedi."
  );

  const testMesajiGonder = () => calistir(
    () => jsonOku<TelegramEkranVerisi>("/api/ekran/telegram/test", { method: "POST" }),
    "Test mesajı gönderilemedi."
  );

  const baglantiyiKaldir = () => {
    if (!window.confirm("Telegram bağlantısı kaldırılsın mı?"))
      return;

    void calistir(
      () => jsonOku<TelegramEkranVerisi>("/api/ekran/telegram", { method: "DELETE" }),
      "Telegram bağlantısı kaldırılamadı."
    );
  };

  const kopyala = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value);
      setMesaj(`${label} kopyalandı.`);
      setHata("");
    } catch {
      setHata(`${label} kopyalanamadı.`);
    }
  };

  const data = ekran ?? {
    bagli: false,
    durum: "Bağlı değil",
    botKullaniciAdi: "SystemcelBot",
    eslestirmeKodu: "SC-------",
    baglantiLinki: "https://t.me/SystemcelBot",
    qrUrl: "",
    gecerlilikDakika: 10,
    mesaj: ""
  };

  const botHandle = `@${data.botKullaniciAdi}`;
  const startKomutu = `/start ${data.eslestirmeKodu}`;

  return (
    <main className={`telegram-page ${data.bagli ? "telegram-page--connected" : ""}`}>
      <header className="telegram-header">
        <div>
          <div className="telegram-breadcrumb">Ayarlar <span>/</span> Telegram</div>
          <div className="telegram-title-row">
            <h2>{data.bagli ? "Telegram Bağlandı" : "Telegram Bağlantısı"}</h2>
            <span className={`telegram-status ${data.bagli ? "connected" : ""}`}>
              {data.bagli ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
              {data.durum}
            </span>
          </div>
          <p>
            {data.bagli
              ? "Telegram bağlantısı aktif. Bildirimler ve raporlar resmi Systemcel botu üzerinden gönderilecek."
              : "Telegram'ı bağlayarak bildirimleri ve raporları doğrudan Telegram üzerinden alabilirsiniz."}
          </p>
        </div>
      </header>

      {data.bagli ? (
        <>
          <section className="telegram-card telegram-connected-card">
            <div className="telegram-connected-hero">
              <span className="telegram-connected-hero__icon">
                <CheckCircle2 size={42} />
              </span>
              <div>
                <strong>Telegram bağlantısı tamamlandı</strong>
                <p>{botHandle} ile Systemcel arasındaki bağlantı aktif.</p>
              </div>
            </div>

            <div className="telegram-connected-meta">
              <div>
                <span>Bot</span>
                <strong>{botHandle}</strong>
              </div>
              <div>
                <span>Durum</span>
                <strong>Aktif</strong>
              </div>
              <div>
                <span>Komutlar</span>
                <strong>/yardim</strong>
              </div>
            </div>
          </section>

          <section className="telegram-card telegram-connected-actions">
            <button className="telegram-after-action" disabled={islemde} type="button" onClick={testMesajiGonder}>
              <Send size={20} />
              <span>
                <strong>Test Mesajı Gönder</strong>
                <small>Bağlantının çalıştığını kontrol edin.</small>
              </span>
              <ChevronRight size={20} />
            </button>
            <button className="telegram-after-action" disabled={islemde} type="button" onClick={kontrolEt}>
              <ShieldCheck size={20} />
              <span>
                <strong>Bağlantıyı Kontrol Et</strong>
                <small>Telegram durumunu yeniden doğrulayın.</small>
              </span>
              <ChevronRight size={20} />
            </button>
            <button className="telegram-after-action telegram-after-action--danger" disabled={islemde} type="button" onClick={baglantiyiKaldir}>
              <Trash2 size={20} />
              <span>
                <strong>Bağlantıyı Kaldır</strong>
                <small>Mevcut Telegram bağlantısını kaldırın.</small>
              </span>
              <ChevronRight size={20} />
            </button>
          </section>
        </>
      ) : (
        <>
          <section className="telegram-main-grid">
            <section className="telegram-card telegram-connect-card">
              <div className="telegram-connect-left">
                <div className="telegram-actions-row">
                  <button className="telegram-btn telegram-btn--primary" disabled={islemde} type="button" onClick={baslat}>
                    <Send size={20} />
                    Telegram'ı Bağla
                  </button>
                  <button className="telegram-btn" disabled={islemde} type="button" onClick={kontrolEt}>
                    <ShieldCheck size={20} />
                    Bağlantıyı Kontrol Et
                  </button>
                </div>

                <div className="telegram-code-box">
                  <span>Eşleştirme Kodu <Info size={17} /></span>
                  <strong>{data.eslestirmeKodu}</strong>
                  <button type="button" onClick={() => kopyala(data.eslestirmeKodu, "Eşleştirme kodu")}>
                    <Copy size={18} />
                  </button>
                </div>

                <div className="telegram-expiry">
                  <RefreshCw size={17} />
                  Kod {data.gecerlilikDakika} dakika geçerlidir
                </div>

                <label className="telegram-link-field">
                  <span>Bağlantı Linki</span>
                  <div>
                    <input readOnly value={data.baglantiLinki} />
                    <button type="button" onClick={() => kopyala(data.baglantiLinki, "Bağlantı linki")}>
                      <Copy size={18} />
                      Linki Kopyala
                    </button>
                  </div>
                </label>

                <div className="telegram-manual">
                  <span className="telegram-manual__icon"><Send size={30} /></span>
                  <div>
                    <strong>Manuel kullanım</strong>
                    <p>Telegram'da {botHandle}'a şu komutu gönder:</p>
                    <button type="button" onClick={() => kopyala(startKomutu, "Manuel komut")}>
                      {startKomutu}
                    </button>
                  </div>
                </div>
              </div>
            </section>

            <section className="telegram-card telegram-qr-card telegram-qr-panel">
              <h3><QrCode size={21} /> QR ile Bağlan</h3>
              <div className="telegram-qr-frame">
                <QrCode className="telegram-qr-placeholder" size={96} />
                {data.qrUrl && !qrYuklenemedi ? (
                  <img alt="Telegram bağlantı QR kodu" src={data.qrUrl} onError={() => setQrYuklenemedi(true)} />
                ) : null}
              </div>
              <p><Smartphone size={20} /> Telefonunuzla okutun veya linki açın.</p>
            </section>

            <aside className="telegram-card telegram-steps-card">
              <div className="telegram-hero-icon"><Send size={54} /></div>
              <h3>Bağlantı Adımları</h3>
              <ol>
                <li><span>1</span> Telegram'ı Bağla</li>
                <li><span>2</span> QR kodu okut veya linki aç</li>
                <li><span>3</span> <code>{startKomutu}</code> komutunu gönder</li>
                <li><span>4</span> Systemcel bağlantıyı doğrular</li>
              </ol>
            </aside>
          </section>

          <section className="telegram-card telegram-after-card">
            <div>
              <h3>Bağlandıktan Sonra</h3>
              <p>Telegram bağlantınız tamamlandıktan sonra aşağıdaki işlemleri yapabilirsiniz.</p>
            </div>
            <button className="telegram-after-action" disabled={islemde || !data.bagli} type="button" onClick={testMesajiGonder}>
              <Send size={20} />
              <span>
                <strong>Test Mesajı Gönder</strong>
                <small>Bağlantının çalıştığını kontrol edin.</small>
              </span>
              <ChevronRight size={20} />
            </button>
            <button className="telegram-after-action" disabled={islemde || !data.bagli} type="button" onClick={baglantiyiKaldir}>
              <Trash2 size={20} />
              <span>
                <strong>Bağlantıyı Kaldır</strong>
                <small>Mevcut Telegram bağlantısını kaldırın.</small>
              </span>
              <ChevronRight size={20} />
            </button>
            <div className="telegram-after-action telegram-after-note">
              <Info size={20} />
              <span>
                <strong>Bağlantı Bilgileri</strong>
                <small>Bağlantı tamamlandıktan sonra aktif olur.</small>
              </span>
              <ChevronRight size={20} />
            </div>
          </section>
        </>
      )}

      {(mesaj || hata || islemde) && (
        <div className={`telegram-feedback ${hata ? "error" : ""}`}>
          {hata || (islemde ? "İşlem yapılıyor..." : mesaj)}
        </div>
      )}

      <a className="telegram-hidden-link" href={data.baglantiLinki} rel="noreferrer" target="_blank">
        <ExternalLink size={14} />
        Telegram linki
      </a>
    </main>
  );
}
