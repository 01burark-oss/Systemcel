import React from "react";
import { CheckCircle2, Loader2, RefreshCw, ShieldAlert, XCircle } from "lucide-react";
import { jsonOku } from "../../shared/json";

type BasvuruDurumu = "bekleyen" | "aktif" | "reddedildi" | "tumu";

interface MuhasebeciBasvuru {
  kullaniciId: number;
  clerkUserId: string;
  eposta: string;
  adSoyad: string;
  durum: string;
  createdAt: string;
  updatedAt: string;
  sonGirisAt?: string | null;
  isletmeId?: number | null;
  isletmeAdi: string;
  isletmeTuru: string;
  konum: string;
  telefon: string;
  deneyimYili: number;
  profilResmiUrl: string;
  ucretBilgisi: string;
  uzmanliklar: string;
  musteriTipleri: string;
  kisaAciklama: string;
  profilTamam: boolean;
}

interface BasvuruListe {
  yoneticiMi: boolean;
  durumFiltresi: string;
  bekleyenSayisi: number;
  onayliSayisi: number;
  reddedilenSayisi: number;
  basvurular: MuhasebeciBasvuru[];
}

const filtreler: Array<{ value: BasvuruDurumu; label: string }> = [
  { value: "bekleyen", label: "Bekleyen" },
  { value: "aktif", label: "Onaylı" },
  { value: "reddedildi", label: "Reddedilen" },
  { value: "tumu", label: "Tümü" }
];

export function MuhasebeciBasvurulariSayfasi({ onUstBarYenile }: { onUstBarYenile?: () => unknown | Promise<unknown> }) {
  const [data, setData] = React.useState<BasvuruListe | null>(null);
  const [filtre, setFiltre] = React.useState<BasvuruDurumu>("bekleyen");
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");

  const yukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      const query = filtre === "tumu" ? "" : `?durum=${encodeURIComponent(filtre)}`;
      const sonuc = await jsonOku<BasvuruListe>(`/api/ekran/yonetim/muhasebeci-basvurulari${query}`);
      setData(sonuc);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Başvurular yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, [filtre]);

  React.useEffect(() => {
    document.title = "Muhasebeci Başvuruları";
  }, []);

  React.useEffect(() => {
    yukle().catch(() => undefined);
  }, [yukle]);

  async function calistir(key: string, action: () => Promise<void>) {
    setIslemde(key);
    setHata("");
    setMesaj("");
    try {
      await action();
      await onUstBarYenile?.();
      await yukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "İşlem tamamlanamadı.");
    } finally {
      setIslemde("");
    }
  }

  async function onayla(basvuru: MuhasebeciBasvuru) {
    await calistir(`onay-${basvuru.kullaniciId}`, async () => {
      await jsonOku<MuhasebeciBasvuru>(`/api/ekran/yonetim/muhasebeci-basvurulari/${basvuru.kullaniciId}/onayla`, { method: "POST" });
      setMesaj(`${basvuruBaslik(basvuru)} onaylandı.`);
    });
  }

  async function reddet(basvuru: MuhasebeciBasvuru) {
    await calistir(`red-${basvuru.kullaniciId}`, async () => {
      await jsonOku<MuhasebeciBasvuru>(`/api/ekran/yonetim/muhasebeci-basvurulari/${basvuru.kullaniciId}/reddet`, {
        method: "POST",
        body: JSON.stringify({ sebep: "" })
      });
      setMesaj(`${basvuruBaslik(basvuru)} reddedildi.`);
    });
  }

  return (
    <main className="admin-page">
      <section className="admin-page__toolbar">
        <div className="admin-page__stats" aria-label="Başvuru özetleri">
          <Stat label="Bekleyen" value={data?.bekleyenSayisi ?? 0} />
          <Stat label="Onaylı" value={data?.onayliSayisi ?? 0} />
          <Stat label="Reddedilen" value={data?.reddedilenSayisi ?? 0} />
        </div>

        <div className="admin-page__actions">
          <select value={filtre} onChange={(event) => setFiltre(event.target.value as BasvuruDurumu)} aria-label="Durum filtresi">
            {filtreler.map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
          <button type="button" onClick={() => yukle()} disabled={yukleniyor || Boolean(islemde)} aria-label="Yenile">
            {yukleniyor ? <Loader2 size={16} className="spin" /> : <RefreshCw size={16} />}
          </button>
        </div>
      </section>

      {hata ? <p className="admin-page__error">{hata}</p> : null}
      {mesaj ? <p className="admin-page__success">{mesaj}</p> : null}

      {yukleniyor ? (
        <div className="admin-state">
          <Loader2 size={22} className="spin" />
          <span>Başvurular yükleniyor...</span>
        </div>
      ) : !data?.yoneticiMi ? (
        <div className="admin-state admin-state--danger">
          <ShieldAlert size={24} />
          <span>Bu ekran için yönetici hesabı gerekir.</span>
        </div>
      ) : data.basvurular.length === 0 ? (
        <div className="admin-state">
          <ShieldAlert size={24} />
          <span>Bu filtrede başvuru yok.</span>
        </div>
      ) : (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Başvuru</th>
                <th>Çalışma Alanı</th>
                <th>Durum</th>
                <th>Tarih</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {data.basvurular.map((basvuru) => {
                const pending = basvuru.durum === "MuhasebeciOnayBekliyor";
                const onayEngelli = !pending || !basvuru.profilTamam || Boolean(islemde);
                return (
                  <tr key={basvuru.kullaniciId}>
                    <td className="admin-applicant-cell">
                      {basvuru.profilResmiUrl ? <img src={basvuru.profilResmiUrl} alt="" /> : null}
                      <div>
                        <strong>{basvuruBaslik(basvuru)}</strong>
                        <span>{basvuru.eposta || basvuru.clerkUserId}</span>
                        <span>{basvuru.telefon || "Telefon yok"} · {basvuru.deneyimYili} yıl deneyim</span>
                      </div>
                    </td>
                    <td>
                      <strong>{basvuru.isletmeAdi || "-"}</strong>
                      <span>{[basvuru.isletmeTuru, basvuru.konum].filter(Boolean).join(" · ") || "-"}</span>
                      <span>{basvuru.ucretBilgisi || "Ücret bilgisi yok"}</span>
                      <span>{[basvuru.uzmanliklar, basvuru.musteriTipleri].filter(Boolean).join(" · ") || "Profil detayı yok"}</span>
                    </td>
                    <td>
                      <StatusPill durum={basvuru.durum} />
                    </td>
                    <td>
                      <span>{tarih(basvuru.updatedAt || basvuru.createdAt)}</span>
                    </td>
                    <td className="admin-table__row-actions">
                      <button type="button" className="admin-btn admin-btn--success" onClick={() => onayla(basvuru)} disabled={onayEngelli}>
                        {islemde === `onay-${basvuru.kullaniciId}` ? <Loader2 size={15} className="spin" /> : <CheckCircle2 size={15} />}
                        <span>{pending && !basvuru.profilTamam ? "Profil eksik" : "Onayla"}</span>
                      </button>
                      <button type="button" className="admin-btn admin-btn--danger" onClick={() => reddet(basvuru)} disabled={!pending || Boolean(islemde)}>
                        {islemde === `red-${basvuru.kullaniciId}` ? <Loader2 size={15} className="spin" /> : <XCircle size={15} />}
                        <span>Reddet</span>
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function StatusPill({ durum }: { durum: string }) {
  const label =
    durum === "Aktif"
      ? "Onaylı"
      : durum === "MuhasebeciReddedildi"
        ? "Reddedildi"
        : "Bekliyor";
  return <span className={`admin-status admin-status--${durum}`}>{label}</span>;
}

function basvuruBaslik(basvuru: MuhasebeciBasvuru) {
  return basvuru.adSoyad || basvuru.isletmeAdi || basvuru.eposta || "Muhasebeci";
}

function tarih(value: string) {
  if (!value) return "-";
  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  });
}
