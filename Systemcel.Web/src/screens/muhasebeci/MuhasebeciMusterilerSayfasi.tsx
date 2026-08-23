import React from "react";
import { ExternalLink, Loader2, MessageCircle, RefreshCw, UsersRound } from "lucide-react";
import { jsonOku } from "../../shared/json";
import type { BelgeSaglikOzeti } from "../dashboard/types";
import type { MuhasebeciMusteri, MuhasebeciPanel } from "./types";

interface MuhasebeciMusterilerSayfasiProps {
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

export function MuhasebeciMusterilerSayfasi({ onUstBarYenile }: MuhasebeciMusterilerSayfasiProps) {
  const [panel, setPanel] = React.useState<MuhasebeciPanel | null>(null);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState("");
  const [hata, setHata] = React.useState("");

  const yukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      setPanel(await jsonOku<MuhasebeciPanel>("/api/ekran/muhasebeci"));
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Müşteriler yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, []);

  React.useEffect(() => {
    document.title = "Müşterilerim";
    yukle().catch(() => undefined);
  }, [yukle]);

  async function musteriAc(musteri: MuhasebeciMusteri) {
    await calistir(`musteri-${musteri.isletmeId}`, async () => {
      await jsonOku<{ mesaj: string }>(`/api/ekran/muhasebeci/musteriler/${musteri.isletmeId}/ac`, { method: "POST" });
      await onUstBarYenile?.();
      window.location.href = "/app";
    });
  }

  async function sohbetAc(musteri: MuhasebeciMusteri) {
    await calistir(`sohbet-${musteri.isletmeId}`, async () => {
      const result = await jsonOku<{ sohbetId: number }>(`/api/ekran/sohbetler/musteriler/${musteri.isletmeId}`);
      window.location.href = `/app/sohbetler?sohbetId=${result.sohbetId}`;
    });
  }

  async function calistir(key: string, action: () => Promise<void>) {
    setIslemde(key);
    setHata("");
    try {
      await action();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "İşlem tamamlanamadı.");
    } finally {
      setIslemde("");
    }
  }

  if (yukleniyor) {
    return (
      <main className="accountant-panel">
        <div className="accountant-state">
          <Loader2 className="spin" size={28} />
          <span>Müşteriler yükleniyor...</span>
        </div>
      </main>
    );
  }

  if (!panel?.hazir) {
    return (
      <main className="accountant-panel">
        <section className="accountant-panel__empty">
          <UsersRound size={30} />
          <h1>Müşteriler açılamadı</h1>
          <p>{panel?.mesaj || "Muhasebeci çalışma alanı henüz hazır değil."}</p>
          <a href="/app/muhasebeci">Muhasebeci paneli</a>
        </section>
      </main>
    );
  }

  return (
    <main className="accountant-panel accountant-customers-page">
      <section className="accountant-panel__hero">
        <div>
          <h1>Müşterilerim</h1>
          <p>{panel.musteriler.length} aktif bağlantı</p>
        </div>
        <button
          type="button"
          className="ghost-refresh"
          aria-label="Müşterileri yenile"
          title="Müşterileri yenile"
          onClick={() => yukle().catch(() => undefined)}
        >
          <RefreshCw size={17} />
        </button>
      </section>

      {hata ? <p className="accountant-feedback accountant-feedback--error">{hata}</p> : null}

      {panel.musteriler.length === 0 ? (
        <section className="accountant-panel__empty">
          <UsersRound size={30} />
          <h2>Henüz müşterin yok</h2>
          <p>Panelden davet bağlantısı oluşturup müşterine gönderebilirsin.</p>
          <a href="/app/muhasebeci">Davet oluştur</a>
        </section>
      ) : (
        <section className="accountant-section accountant-section--full" aria-label="Müşteri listesi">
          <div className="accountant-table-wrap">
            <table className="accountant-table">
              <thead>
                <tr>
                  <th>Müşteri</th>
                  <th>Belge durumu</th>
                  <th>Konum</th>
                  <th>Yetki</th>
                  <th>Başlangıç</th>
                  <th><span className="sr-only">İşlemler</span></th>
                </tr>
              </thead>
              <tbody>
                {panel.musteriler.map((musteri) => (
                  <tr key={musteri.isletmeId}>
                    <td><strong>{musteri.ad}</strong></td>
                    <td><BelgeSagligiHucre musteri={musteri} /></td>
                    <td>{musteri.konum || "-"}</td>
                    <td>{yetkiEtiketi(musteri.yetkiSeviyesi)}</td>
                    <td>{tarihBic(musteri.baslangicAt)}</td>
                    <td>
                      <button
                        type="button"
                        onClick={() => sohbetAc(musteri)}
                        disabled={Boolean(islemde)}
                      >
                        {islemde === `sohbet-${musteri.isletmeId}` ? <Loader2 size={15} className="spin" /> : <MessageCircle size={15} />}
                        <span>Sohbet</span>
                      </button>
                      <button
                        type="button"
                        onClick={() => musteriAc(musteri)}
                        disabled={Boolean(islemde)}
                      >
                        {islemde === `musteri-${musteri.isletmeId}` ? <Loader2 size={15} className="spin" /> : <ExternalLink size={15} />}
                        <span>Çalışma alanını aç</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </main>
  );
}

function BelgeSagligiHucre({ musteri }: { musteri: MuhasebeciMusteri }) {
  const ozet = musteri.belgeSagligi;
  if (!ozet) {
    return (
      <span className="accountant-document-health__locked" aria-label={`${musteri.ad} belge durumu: Pro ile açılır`}>
        Pro ile açılır
      </span>
    );
  }

  const skor = ozet.skor === null ? "—" : String(Math.min(100, Math.max(0, Math.round(ozet.skor))));
  const durum = belgeDurumuEtiketi(ozet.durum);
  const sorunOzeti = ozet.sorunlar.slice(0, 3).map((sorun) => `${sorun.baslik}: ${sorun.adet}`).join("\n");

  return (
    <div
      className={`accountant-document-health accountant-document-health--${ozet.durum.toLocaleLowerCase("tr-TR")}`}
      aria-label={`${musteri.ad} belge durumu: ${skor} puan, ${durum}, ${ozet.eksikBelgeSayisi} eksik belge`}
      title={sorunOzeti || undefined}
    >
      <strong>{skor}<small>/100</small></strong>
      <span>{durum}</span>
      <small>{ozet.eksikBelgeSayisi} eksik</small>
    </div>
  );
}

function belgeDurumuEtiketi(durum: BelgeSaglikOzeti["durum"]) {
  switch (durum) {
    case "Hazir": return "Hazır";
    case "Dikkat": return "Dikkat";
    case "Eksik": return "Eksik";
    default: return "Veri yok";
  }
}

function yetkiEtiketi(value: string) {
  return value === "TamIslem" ? "Tam işlem" : "Okuma + rapor";
}

function tarihBic(value: string) {
  return new Date(value).toLocaleDateString("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric" });
}
