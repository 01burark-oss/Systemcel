import React from "react";
import { AlertTriangle, Loader2, RefreshCw, Save } from "lucide-react";
import { jsonOku } from "../../shared/json";
import { useI18n } from "../../shared/i18n";

interface DestekTalebi {
  id: number;
  isletmeId: number;
  isletmeAdi: string;
  konu: string;
  kategori: string;
  aciklama: string;
  oncelik: string;
  durum: string;
  yoneticiYaniti: string;
  createdAt: string;
  updatedAt: string;
}

interface DestekTalebiListesi {
  talepler: DestekTalebi[];
}

type Taslak = Pick<DestekTalebi, "durum" | "yoneticiYaniti">;

const durumler = ["Acik", "Islemde", "Cozuldu"];

function tarih(value: string) {
  if (!value) return "-";
  return new Date(value).toLocaleString("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function oncelikSirasi(oncelik: string) {
  return oncelik.trim().toLowerCase() === "oncelikli" ? 0 : 1;
}

function oncelikEtiketi(oncelik: string) {
  return oncelik.trim().toLowerCase() === "oncelikli" ? "Öncelikli" : "Standart";
}

function durumEtiketi(durum: string) {
  const anahtar = durum.trim().toLowerCase();
  if (anahtar.includes("coz") || anahtar.includes("tamam")) return "Çözüldü";
  if (anahtar.includes("incele") || anahtar.includes("islem")) return "İşlemde";
  return "Açık";
}

function talepleriSirala(talepler: DestekTalebi[]) {
  return [...talepler].sort((sol, sag) =>
    oncelikSirasi(sol.oncelik) - oncelikSirasi(sag.oncelik)
    || new Date(sol.createdAt).getTime() - new Date(sag.createdAt).getTime()
  );
}

export function DestekTalepleriYonetimSayfasi() {
  const { t } = useI18n();
  const [talepler, setTalepler] = React.useState<DestekTalebi[]>([]);
  const [taslaklar, setTaslaklar] = React.useState<Record<number, Taslak>>({});
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState<number | null>(null);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");

  const yukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      const sonuc = await jsonOku<DestekTalebiListesi>("/api/ekran/yonetim/destek");
      const sirali = talepleriSirala(sonuc.talepler ?? []);
      setTalepler(sirali);
      setTaslaklar(Object.fromEntries(sirali.map((talep) => [talep.id, { durum: talep.durum, yoneticiYaniti: talep.yoneticiYaniti ?? "" }])));
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Destek talepleri yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, []);

  React.useEffect(() => {
    document.title = "Destek Talepleri";
    yukle().catch(() => undefined);
  }, [yukle]);

  function taslagiGuncelle(id: number, patch: Partial<Taslak>) {
    setTaslaklar((onceki) => ({ ...onceki, [id]: { ...onceki[id], ...patch } }));
  }

  async function kaydet(event: React.FormEvent<HTMLFormElement>, talep: DestekTalebi) {
    event.preventDefault();
    const taslak = taslaklar[talep.id] ?? { durum: talep.durum, yoneticiYaniti: talep.yoneticiYaniti ?? "" };
    setIslemde(talep.id);
    setHata("");
    setMesaj("");
    try {
      const guncellenen = await jsonOku<DestekTalebi>(`/api/ekran/yonetim/destek/${talep.id}/guncelle`, {
        method: "POST",
        body: JSON.stringify({ durum: taslak.durum, yoneticiYaniti: taslak.yoneticiYaniti.trim() })
      });
      setTalepler((onceki) => talepleriSirala(onceki.map((item) => item.id === talep.id ? guncellenen : item)));
      setTaslaklar((onceki) => ({ ...onceki, [talep.id]: { durum: guncellenen.durum, yoneticiYaniti: guncellenen.yoneticiYaniti ?? "" } }));
      setMesaj(`“${talep.konu}” talebi güncellendi.`);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Destek talebi güncellenemedi.");
    } finally {
      setIslemde(null);
    }
  }

  return <main className="admin-page support-ops">
    <nav className="admin-subnav" aria-label="Yönetim bölümleri"><a href="/app/yonetim/muhasebeci-basvurulari">Muhasebeci başvuruları</a><a href="/app/yonetim/odemeler">Ödeme inceleme</a><a href="/app/yonetim/muhasebeci-aktarimlari">Muhasebeci aktarımları</a><a className="active" aria-current="page" href="/app/yonetim/destek">Destek talepleri</a></nav>
    <section className="admin-page__toolbar">
      <div className="admin-page__stats" aria-label="Destek talebi özeti"><Stat label="Toplam talep" value={talepler.length} /><Stat label="Öncelikli" value={talepler.filter((talep) => oncelikSirasi(talep.oncelik) === 0).length} /><Stat label="Açık" value={talepler.filter((talep) => durumEtiketi(talep.durum) === "Açık").length} /></div>
      <div className="admin-page__actions"><button type="button" onClick={() => yukle()} disabled={yukleniyor || islemde !== null} aria-label={t("admin.refresh")}>{yukleniyor ? <Loader2 size={16} className="spin" /> : <RefreshCw size={16} />}</button></div>
    </section>
    {hata ? <p className="admin-page__error" role="alert">{hata}</p> : null}
    {mesaj ? <p className="admin-page__success" role="status">{mesaj}</p> : null}
    {yukleniyor ? <div className="admin-state"><Loader2 size={22} className="spin" /><span>{t("admin.loading")}</span></div> : talepler.length === 0 ? <div className="admin-state"><AlertTriangle size={22} /><span>{t("support.empty")}</span></div> : <div className="admin-table-wrap"><table className="admin-table support-ops__table"><thead><tr><th>Talep</th><th>İşletme</th><th>{t("admin.priority")}</th><th>Tarih</th><th>Durum ve yanıt</th></tr></thead><tbody>{talepler.map((talep) => {
      const taslak = taslaklar[talep.id] ?? { durum: talep.durum, yoneticiYaniti: talep.yoneticiYaniti ?? "" };
      return <tr key={talep.id}><td className="support-ops__request"><strong>{talep.konu}</strong><span>{talep.kategori === "Diger" ? "Diğer" : talep.kategori}</span><p>{talep.aciklama}</p></td><td><strong>{talep.isletmeAdi || `İşletme #${talep.isletmeId}`}</strong><span>#{talep.isletmeId}</span></td><td><span className={`support-ops__priority support-ops__priority--${oncelikSirasi(talep.oncelik) === 0 ? "priority" : "standard"}`}>{oncelikEtiketi(talep.oncelik)}</span></td><td><span>{tarih(talep.createdAt)}</span></td><td className="support-ops__edit"><form onSubmit={(event) => kaydet(event, talep)}><label><span>{t("support.status")}</span><select aria-label={`${talep.konu} ${t("support.statusField")}`} value={taslak.durum} onChange={(event) => taslagiGuncelle(talep.id, { durum: event.target.value })}>{[...new Set([taslak.durum, ...durumler])].map((durum) => <option key={durum} value={durum}>{durumEtiketi(durum)}</option>)}</select></label><label><span>{t("support.reply")}</span><textarea aria-label={`${talep.konu} ${t("support.replyField")}`} value={taslak.yoneticiYaniti} onChange={(event) => taslagiGuncelle(talep.id, { yoneticiYaniti: event.target.value })} maxLength={1000} rows={3} placeholder="Kullanıcıya gösterilecek yanıt" /></label><button className="admin-btn admin-btn--success" type="submit" disabled={islemde !== null}>{islemde === talep.id ? <Loader2 size={15} className="spin" /> : <Save size={15} />}{t("admin.save")}</button></form></td></tr>;
    })}</tbody></table></div>}
  </main>;
}

function Stat({ label, value }: { label: string; value: number }) {
  return <div><span>{label}</span><strong>{value}</strong></div>;
}
