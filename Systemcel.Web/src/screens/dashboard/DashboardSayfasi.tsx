import React from "react";
import {
  ChevronDown,
  FileText,
  Mail,
  MessageCircle,
  RefreshCw,
  Send
} from "lucide-react";
import { BusinessSelector } from "../../shared/BusinessSelector";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import { preventNativeDrag } from "../../shared/noDrag";
import { odemeIkonu, paraBic, paraDegerBic } from "./helpers";
import type { DashboardEkran, NetTrendNokta, OdemeDagilim, OzetKart } from "./types";

const ODEME_RENKLERI = ["#c8ff00", "#11110f", "#a6a493", "#dedbcc"];

function yuzdeBic(orani: number) {
  return `${Math.round(orani)}%`;
}

function ozetBul(ekran: DashboardEkran | null, etiket: string, fallback?: OzetKart) {
  return ekran?.paneller.find((item) => item.etiket === etiket) ?? fallback ?? null;
}

function ozetEtiketi(etiket: string) {
  switch (etiket) {
    case "Bugun":
      return "Bugün";
    case "Son 30 Gun":
      return "Son 30 Gün";
    case "Son 1 Yil":
      return "Son 1 Yıl";
    case "Bu Yil":
      return "Bu Yıl";
    default:
      return etiket;
  }
}

function odemeEtiketi(yontem: string) {
  switch (yontem) {
    case "Kredi Karti":
      return "Kredi Kartı";
    case "Online Odeme":
      return "Online Ödeme";
    default:
      return yontem;
  }
}

function OzetMetrik({
  baslik,
  deger,
  ton,
  trend,
  donem,
  donemSecenekleri,
  onDonemChange
}: {
  baslik: string;
  deger: number;
  ton: "gelir" | "net" | "gider";
  trend?: NetTrendNokta[];
  donem?: string;
  donemSecenekleri?: OzetKart[];
  onDonemChange?: (value: string) => void;
}) {
  const degerDurumu = Math.abs(deger) < 0.005 ? "bos" : deger < 0 ? "negatif" : "pozitif";

  if (ton === "net") {
    return (
      <article className={`snapshot-card snapshot-card--net snapshot-card--benchmark snapshot-card--${degerDurumu}`}>
        <div className="snapshot-card__head">
          <h3>{baslik}</h3>
          <label className="snapshot-period-chip">
            <select
              aria-label="Net kâr dönemi"
              value={donem ?? "Bugun"}
              onChange={(event) => onDonemChange?.(event.target.value)}
            >
              {(donemSecenekleri ?? []).map((secenek) => (
                <option key={secenek.etiket} value={secenek.etiket}>
                  {ozetEtiketi(secenek.etiket)}
                </option>
              ))}
            </select>
            <ChevronDown size={14} />
          </label>
        </div>

        <div className="snapshot-card__value">
          <span className="currency">₺</span>
          <strong>{paraDegerBic(deger)}</strong>
        </div>

        <NetBenchmark bugunNet={deger} trend={trend ?? []} donem={donem ?? "Bugun"} />
      </article>
    );
  }

  return (
    <article className={`snapshot-card snapshot-card--${ton} snapshot-card--${degerDurumu}`}>
      <div className="snapshot-card__head">
        <h3>{baslik}</h3>
      </div>

      <div className="snapshot-card__value">
        <span className="currency">₺</span>
        <strong>{paraDegerBic(deger)}</strong>
      </div>

    </article>
  );
}

function NetBenchmark({ bugunNet, trend, donem }: { bugunNet: number; trend: NetTrendNokta[]; donem: string }) {
  const gecmisTrend = trend.length > 1 ? trend.slice(0, -1) : [];
  const veriOlanGunler = gecmisTrend.filter((item) => item.islemVar ?? Math.abs(item.net) > 0);
  const ortalama = veriOlanGunler.length
    ? veriOlanGunler.reduce((sum, item) => sum + item.net, 0) / veriOlanGunler.length
    : 0;
  const fark = bugunNet - ortalama;
  const yuzde = Math.round((Math.abs(fark) / Math.max(Math.abs(ortalama), 1)) * 100);
  const sinyalOrani = veriOlanGunler.length === 0 ? 0 : Math.min(100, Math.max(10, yuzde));
  const iyi = fark >= 0;
  const durumSinifi = veriOlanGunler.length === 0 ? "notr" : iyi ? "pozitif" : "negatif";
  const durumMetni = NetBenchmarkMetni(veriOlanGunler.length, ortalama, bugunNet, yuzde, iyi);

  return (
    <div className="net-benchmark">
      <div className="net-benchmark__header">
        <span>{donem === "Bugun" ? "Bugünkü performans" : `${ozetEtiketi(donem)} performansı`}</span>
        <strong className={durumSinifi}>Son zamanlara göre</strong>
      </div>

      <strong className={`net-benchmark__insight ${durumSinifi}`}>
        {durumMetni}
      </strong>

      <div className="net-benchmark__bar" aria-hidden="true">
        <span
          className={`net-benchmark__fill ${durumSinifi}`}
          style={{ width: `${sinyalOrani}%` }}
        />
      </div>

    </div>
  );
}

function NetBenchmarkMetni(veriSayisi: number, ortalama: number, bugunNet: number, yuzde: number, iyi: boolean) {
  if (veriSayisi === 0) {
    return "Son zaman kıyası için yeterli veri yok.";
  }

  if (Math.abs(ortalama) < 1) {
    if (bugunNet > 0) return "Son zamanlara göre bugün kâr oluştu.";
    if (bugunNet < 0) return "Son zamanlara göre bugün net sonuç zayıf.";
    return "Son zamanlara göre bugün net sonuç dengede.";
  }

  if (bugunNet > 0) {
    return iyi
      ? `Son zamanlara göre ortalama %${yuzde} daha fazla kâr.`
      : `Son zamanlara göre ortalama %${yuzde} daha düşük kâr.`;
  }

  return iyi
    ? `Son zamanlara göre net sonuç %${yuzde} daha iyi.`
    : `Son zamanlara göre net sonuç %${yuzde} daha zayıf.`;
}

function DonutChart({ odemeler }: { odemeler: OdemeDagilim[] }) {
  const toplam = odemeler.reduce((sum, item) => sum + item.toplam, 0);
  const oranlar = odemeler.map((item) => (toplam > 0 ? item.toplam / toplam : 1 / Math.max(odemeler.length, 1)));
  const cevre = 2 * Math.PI * 68;

  const segments = oranlar.map((oran, index) => {
    const uzunluk = cevre * oran;
    const offset = oranlar
      .slice(0, index)
      .reduce((sum, previousRatio) => sum + cevre * previousRatio, 0);
    return {
      color: ODEME_RENKLERI[index % ODEME_RENKLERI.length],
      dasharray: `${Math.max(uzunluk - 4, 0)} ${cevre}`,
      dashoffset: -offset
    };
  });

  return (
    <div className="payment-chart no-drag" draggable={false} onDragStart={preventNativeDrag}>
      <svg viewBox="0 0 180 180" aria-hidden="true">
        <circle cx="90" cy="90" r="68" className="payment-chart__track" />
        {segments.map((segment) => (
          <circle
            key={`${segment.color}-${segment.dashoffset}`}
            cx="90"
            cy="90"
            r="68"
            className="payment-chart__segment"
            style={{
              stroke: segment.color,
              strokeDasharray: segment.dasharray,
              strokeDashoffset: segment.dashoffset
            }}
          />
        ))}
      </svg>
      <div className="payment-chart__center">
        <strong>{toplam > 0 ? "100%" : "0%"}</strong>
        <span>Toplam</span>
        <small>{paraBic(toplam)}</small>
      </div>
    </div>
  );
}

function DonemKarti({ kart }: { kart: OzetKart }) {
  return (
    <article className="legacy-summary-card">
      <h3>{ozetEtiketi(kart.etiket)}</h3>
      <div className="legacy-summary-card__rows">
        <div>
          <span>Gelir:</span>
          <strong className="gelir">{paraDegerBic(kart.gelir)}</strong>
        </div>
        <div>
          <span>Gider:</span>
          <strong className="gider">{paraDegerBic(kart.gider)}</strong>
        </div>
        <div>
          <span className="net-title">Net:</span>
          <strong className={kart.net >= 0 ? "gelir" : "gider"}>{paraDegerBic(kart.net)}</strong>
        </div>
      </div>
    </article>
  );
}

interface DashboardSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

export function DashboardSayfasi({
  onIsletmeDegistir,
  ustBar,
  ustBarIslemde,
  yenileAnahtari
}: DashboardSayfasiProps) {
  const [ekran, setEkran] = React.useState<DashboardEkran | null>(null);
  const [durum, setDurum] = React.useState("Anasayfa yükleniyor...");
  const [hata, setHata] = React.useState("");
  const [yenileniyor, setYenileniyor] = React.useState(false);
  const [paylasimAcik, setPaylasimAcik] = React.useState(false);
  const [paylasimIslemde, setPaylasimIslemde] = React.useState(false);
  const [paylasimMesaj, setPaylasimMesaj] = React.useState("");
  const [netDonem, setNetDonem] = React.useState("Bugun");

  React.useEffect(() => {
    document.title = "CashTracker Gösterge Paneli";
  }, []);

  const yenile = React.useCallback(async () => {
    setYenileniyor(true);
    setHata("");
    setDurum("Anasayfa yükleniyor...");

    try {
      const data = await jsonOku<DashboardEkran>("/api/ekran/anasayfa");
      setEkran(data);
      setDurum("Anasayfa güncel.");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Anasayfa yüklenemedi.";
      setHata(message);
      setDurum("");
    } finally {
      setYenileniyor(false);
    }
  }, []);

  React.useEffect(() => {
    yenile().catch((error: Error) => {
      setDurum("");
      setHata(error.message);
      setYenileniyor(false);
    });
  }, [yenile, yenileAnahtari]);

  const son30Gun = React.useMemo(() => ozetBul(ekran, "Son 30 Gun"), [ekran]);
  const son1Yil = React.useMemo(() => ozetBul(ekran, "Son 1 Yil"), [ekran]);
  const netDonemSecenekleri = React.useMemo(
    () => ekran ? [{ ...ekran.bugun, etiket: "Bugun" }, ...ekran.paneller] : [],
    [ekran]
  );
  const seciliNetDonem = React.useMemo(
    () => netDonemSecenekleri.find((item) => item.etiket === netDonem) ?? netDonemSecenekleri[0] ?? null,
    [netDonem, netDonemSecenekleri]
  );

  const odemeSatirlari = React.useMemo(() => {
    const satirlar = ekran?.odemeDagilimi ?? [];
    const toplam = satirlar.reduce((sum, item) => sum + item.toplam, 0);
    return satirlar.map((item, index) => ({
      ...item,
      renk: ODEME_RENKLERI[index % ODEME_RENKLERI.length],
      oran: toplam > 0 ? (item.toplam / toplam) * 100 : 0
    }));
  }, [ekran]);

  async function paylas(kanal: "telegram" | "pdf") {
    try {
      setPaylasimIslemde(true);
      const sonuc = await jsonOku<{ mesaj: string }>(`/api/ekran/anasayfa/paylas/${kanal}`, { method: "POST" });
      setPaylasimMesaj(sonuc.mesaj);
      window.alert(sonuc.mesaj);
      setPaylasimAcik(false);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Paylaşım işlemi tamamlanamadı.";
      setPaylasimMesaj(message);
      window.alert(message);
    } finally {
      setPaylasimIslemde(false);
    }
  }

  function placeholderMesajiGoster() {
    const message = "Bu özellik daha sonra eklenecektir.";
    setPaylasimMesaj(message);
    window.alert(message);
  }

  return (
    <main className="dashboard-page">
      <section className="legacy-dashboard">
        <div className="legacy-dashboard__header">
          <div>
            <h1>Hızlı Finansal Özet (Snapshot)</h1>
            <p className={hata ? "legacy-dashboard__status legacy-dashboard__status--error" : "legacy-dashboard__status"}>
              {hata || durum}
            </p>
          </div>

          <div className="legacy-dashboard__header-actions">
            <button className="ghost-refresh" onClick={() => yenile().catch(() => undefined)} disabled={yenileniyor} title="Yenile">
              <RefreshCw className={yenileniyor ? "spin" : ""} size={16} />
            </button>

            <BusinessSelector
              aktifIsletmeId={ustBar?.aktifIsletmeId}
              isletmeler={ustBar?.isletmeler ?? []}
              disabled={ustBarIslemde}
              onChange={onIsletmeDegistir}
            />
          </div>
        </div>

        <section className="snapshot-grid">
          <OzetMetrik baslik="Toplam Gelir" deger={ekran?.bugun.gelir ?? 0} ton="gelir" />
          <OzetMetrik
            baslik="Net Kâr"
            deger={seciliNetDonem?.net ?? 0}
            ton="net"
            trend={netDonem === "Bugun" ? ekran?.netTrend : []}
            donem={netDonem}
            donemSecenekleri={netDonemSecenekleri}
            onDonemChange={setNetDonem}
          />
          <OzetMetrik baslik="Toplam Gider" deger={ekran?.bugun.gider ?? 0} ton="gider" />
        </section>

        <section className="legacy-panel payment-panel-legacy">
          <div className="legacy-panel__header">
            <div>
              <h2>Ödeme Yöntemi Dağılımı</h2>
              <p>Bugün kullanılan kanalların genel dağılımı.</p>
            </div>
          </div>

          <div className="payment-panel-legacy__content">
            <div className="payment-legend">
              {odemeSatirlari.map((item) => (
                <div className="payment-legend__row" key={item.yontem}>
                  <div className="payment-legend__name">
                    <span className="payment-legend__dot" style={{ backgroundColor: item.renk }} />
                    {odemeIkonu(item.yontem)}
                    <span>{odemeEtiketi(item.yontem)}</span>
                  </div>
                  <div className="payment-legend__values">
                    <strong>{paraBic(item.toplam)}</strong>
                    <small>{yuzdeBic(item.oran)}</small>
                  </div>
                </div>
              ))}
            </div>

            <DonutChart odemeler={ekran?.odemeDagilimi ?? []} />
          </div>
        </section>

        <section className="legacy-summary-block">
          <h2>Zaman Dönemi Özetleri</h2>

          <div className="legacy-summary-grid">
            {ekran?.bugun && <DonemKarti kart={{ ...ekran.bugun, etiket: "Bugun" }} />}
            {son30Gun && <DonemKarti kart={son30Gun} />}
            {son1Yil && <DonemKarti kart={son1Yil} />}

            <aside className="share-rail">
              <button
                className="share-rail__primary"
                type="button"
                onClick={() => setPaylasimAcik((current) => !current)}
                disabled={paylasimIslemde}
              >
                Raporu Paylaş
                <ChevronDown className={paylasimAcik ? "share-rail__arrow acik" : "share-rail__arrow"} size={16} />
              </button>

              {paylasimAcik && (
                <div className="share-rail__menu">
                  <button type="button" onClick={() => paylas("telegram")} disabled={paylasimIslemde}>
                    <Send size={18} />
                    Telegram
                  </button>
                  <button type="button" onClick={placeholderMesajiGoster} disabled={paylasimIslemde}>
                    <Mail size={18} />
                    Email
                  </button>
                  <button type="button" onClick={placeholderMesajiGoster} disabled={paylasimIslemde}>
                    <MessageCircle size={18} />
                    WhatsApp
                  </button>
                  <button type="button" onClick={() => paylas("pdf")} disabled={paylasimIslemde}>
                    <FileText size={18} />
                    PDF İndir
                  </button>
                </div>
              )}

              {paylasimMesaj && <p className="share-rail__hint">{paylasimMesaj}</p>}
            </aside>
          </div>
        </section>
      </section>
    </main>
  );
}
