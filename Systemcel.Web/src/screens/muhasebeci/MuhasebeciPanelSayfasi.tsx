import React from "react";
import {
  BriefcaseBusiness,
  Check,
  Loader2,
  MessageCircle,
  Sparkles,
  UsersRound,
  X
} from "lucide-react";
import { jsonOku } from "../../shared/json";
import { MuhasebeciProfilOnizleme } from "../../shared/MuhasebeciProfilOnizleme";
import { planAdiGoster } from "../../shared/planEtiketi";
import { ProfilResmiYukleyici } from "../../shared/ProfilResmiYukleyici";
import { TelefonNumarasiInput } from "../../shared/TelefonNumarasiInput";
import { TURKIYE_KONUMLARI } from "../../shared/turkiyeKonumlari";
import type { MuhasebeciPanel, MuhasebeciProfil, MuhasebeciTalep, YetkiSeviyesi } from "./types";

type SohbetHedefi =
  | { tur: "talep"; id: number; baslik: string }
  | { tur: "musteri"; id: number; baslik: string };

interface ProfilFormu {
  yayinda: boolean;
  unvan: string;
  konum: string;
  telefon: string;
  deneyimYili: number;
  profilResmiUrl: string;
  ucretBilgisi: string;
  uzmanliklar: string;
  musteriTipleri: string;
  kisaAciklama: string;
}

interface MuhasebeciPanelSayfasiProps {
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

const bosProfil: ProfilFormu = {
  yayinda: false,
  unvan: "",
  konum: "",
  telefon: "",
  deneyimYili: 1,
  profilResmiUrl: "",
  ucretBilgisi: "",
  uzmanliklar: "",
  musteriTipleri: "",
  kisaAciklama: ""
};

export function MuhasebeciPanelSayfasi({ onUstBarYenile }: MuhasebeciPanelSayfasiProps) {
  const [panel, setPanel] = React.useState<MuhasebeciPanel | null>(null);
  const [profilFormu, setProfilFormu] = React.useState<ProfilFormu>(bosProfil);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [islemde, setIslemde] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const [talepYetkileri, setTalepYetkileri] = React.useState<Record<number, YetkiSeviyesi>>({});
  const [profilResmiYukleniyor, setProfilResmiYukleniyor] = React.useState(false);
  const [sohbetIslemde, setSohbetIslemde] = React.useState(false);
  const konumDatalistId = React.useId();
  const konumSecenekleri = React.useMemo(() => buildKonumSecenekleri(), []);
  const urlSohbetTalepId = React.useMemo(() => {
    const params = new URLSearchParams(window.location.search);
    const parsed = Number(params.get("talepId") ?? "");
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
  }, []);
  const urlSohbetMusteriId = React.useMemo(() => {
    const params = new URLSearchParams(window.location.search);
    const parsed = Number(params.get("musteriId") ?? "");
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
  }, []);
  const urlSohbetAc = React.useMemo(() => new URLSearchParams(window.location.search).get("sohbet") === "1", []);
  const urlSohbetIslendi = React.useRef(false);

  const yukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      const data = await jsonOku<MuhasebeciPanel>("/api/ekran/muhasebeci");
      setPanel(data);
      setProfilFormu(profilFormuOlustur(data));
      const pending: Record<number, YetkiSeviyesi> = {};
      data.bekleyenTalepler.forEach((talep) => {
        pending[talep.id] = talep.yetkiSeviyesi || "OkumaRapor";
      });
      setTalepYetkileri(pending);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Muhasebeci paneli yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, []);

  React.useEffect(() => {
    document.title = "Muhasebeci Paneli";
  }, []);

  React.useEffect(() => {
    yukle().catch(() => undefined);
  }, [yukle]);

  React.useEffect(() => {
    if (!panel?.hazir || !urlSohbetAc || urlSohbetIslendi.current)
      return;

    const talep = urlSohbetTalepId ? panel.bekleyenTalepler.find((item) => item.id === urlSohbetTalepId) : null;
    const musteri = urlSohbetMusteriId ? panel.musteriler.find((item) => item.isletmeId === urlSohbetMusteriId) : null;
    if (!talep && !musteri)
      return;

    urlSohbetIslendi.current = true;
    temizleSohbetYonlendirmesi();
    sohbetAc(talep
      ? { tur: "talep", id: talep.id, baslik: talep.musteriAdi || "Müşteri" }
      : { tur: "musteri", id: musteri!.isletmeId, baslik: musteri!.ad }).catch(() => undefined);
  }, [panel, urlSohbetAc, urlSohbetMusteriId, urlSohbetTalepId]);

  async function profilKaydet(event: React.FormEvent) {
    event.preventDefault();
    await calistir("profil", async () => {
      await jsonOku<MuhasebeciProfil>("/api/ekran/muhasebeci/profil", {
        method: "PUT",
        body: JSON.stringify(profilFormu)
      });
      setMesaj("Pazaryeri profili kaydedildi.");
      await yukle();
    });
  }

  async function talepKabulEt(talep: MuhasebeciTalep) {
    await calistir(`kabul-${talep.id}`, async () => {
      await jsonOku<MuhasebeciTalep>(`/api/ekran/muhasebeci/talepler/${talep.id}/kabul`, {
        method: "POST",
        body: JSON.stringify({
          yetkiSeviyesi: talepYetkileri[talep.id] ?? talep.yetkiSeviyesi ?? "OkumaRapor"
        })
      });
      setMesaj(`${talep.musteriAdi || "Müşteri"} bağlantısı aktif edildi.`);
      await onUstBarYenile?.();
      await yukle();
    });
  }

  async function talepReddet(talep: MuhasebeciTalep) {
    await calistir(`red-${talep.id}`, async () => {
      await jsonOku<MuhasebeciTalep>(`/api/ekran/muhasebeci/talepler/${talep.id}/red`, { method: "POST" });
      setMesaj("Talep reddedildi.");
      await yukle();
    });
  }

  async function sohbetAc(hedef: SohbetHedefi) {
    setSohbetIslemde(true);
    setHata("");
    try {
      const endpoint = hedef.tur === "talep"
        ? `/api/ekran/sohbetler/talepler/${hedef.id}`
        : `/api/ekran/sohbetler/musteriler/${hedef.id}`;
      const result = await jsonOku<{ sohbetId: number }>(endpoint);
      window.location.href = `/app/sohbetler?sohbetId=${result.sohbetId}`;
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Sohbet açılamadı.");
    } finally {
      setSohbetIslemde(false);
    }
  }

  async function calistir(key: string, action: () => Promise<void>) {
    setIslemde(key);
    setHata("");
    setMesaj("");
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
          <Loader2 className="spin" size={22} />
          <span>Muhasebeci paneli yükleniyor...</span>
        </div>
      </main>
    );
  }

  if (!panel?.hazir) {
    const onayBekliyor = (panel?.mesaj ?? "").toLocaleLowerCase("tr-TR").includes("onay");
    return (
      <main className="accountant-panel">
        <section className="accountant-panel__empty">
          <BriefcaseBusiness size={30} />
          <h1>{onayBekliyor ? "Başvurunuz onay bekliyor" : "Muhasebeci çalışma alanı hazır değil"}</h1>
          <p>{panel?.mesaj || "İlk kurulumda hesap tipini Muhasebeci olarak seçin."}</p>
          {onayBekliyor ? null : <a href="/app/ayarlar">Ayarlar</a>}
        </section>
      </main>
    );
  }

  const entitlement = panel.entitlement;
  const aktifMusteri = entitlement?.aktifMusteriSayisi ?? panel.musteriler.length;
  const musteriLimit = entitlement?.musteriSinirsiz ? "Sınırsız" : String(entitlement?.musteriLimiti ?? 0);

  return (
    <main className="accountant-panel">
      <section className="accountant-panel__hero">
        <div>
          <h1>Muhasebeci paneli</h1>
        </div>
        <div className="accountant-panel__hero-actions">
          {entitlement?.muhasebeciProOnerilir ? (
            <div className="accountant-pro-note">
              <Sparkles size={18} />
              <span>Pro plan bu portföy için daha avantajlı görünüyor.</span>
            </div>
          ) : null}
          <a className="accountant-primary-link" href="/app/muhasebeci/musteriler">
            <UsersRound size={17} />
            Müşterilerim
          </a>
        </div>
      </section>

      {mesaj ? <p className="accountant-feedback accountant-feedback--success">{mesaj}</p> : null}
      {hata ? <p className="accountant-feedback accountant-feedback--error">{hata}</p> : null}

      <section className="accountant-metrics">
        <article>
          <span>Plan</span>
          <strong>{planAdiGoster(entitlement?.planAdi)}</strong>
          <small>{entitlement?.oneCikmaAktif ? "Pazaryerinde öne çıkar" : "Profil yayınlanabilir"}</small>
        </article>
        <article>
          <span>Müşteri limiti</span>
          <strong>{aktifMusteri} / {musteriLimit}</strong>
          <small>Aktif bağlantı sayısı</small>
        </article>
        <article>
          <span>AI</span>
          <strong>{entitlement?.aiSinirsiz ? "Sınırsız" : entitlement?.aiAktif ? `${entitlement.aiMesajLimiti ?? 0} mesaj` : "Kapalı"}</strong>
          <small>{entitlement?.aiAktif ? "Muhasebeci plan hakkı" : "Ücretsiz plan"}</small>
        </article>
      </section>

      <section className="accountant-panel__grid accountant-panel__grid--single">
        <form className="accountant-section accountant-section--profile" onSubmit={profilKaydet}>
          <header>
            <div>
              <span className="accountant-eyebrow">Pazaryeri profili</span>
              <h2>Profil yayınlama</h2>
            </div>
            <label className="accountant-toggle">
              <input
                type="checkbox"
                checked={profilFormu.yayinda}
                onChange={(event) => setProfilFormu((current) => ({ ...current, yayinda: event.target.checked }))}
              />
              <span>Yayında</span>
            </label>
          </header>
          <div className="accountant-form-grid">
            <label>
              <span>Unvan</span>
              <input value={profilFormu.unvan} onChange={(event) => setProfilFormu((current) => ({ ...current, unvan: event.target.value }))} />
            </label>
            <label>
              <span>Konum</span>
              <input
                list={konumDatalistId}
                value={profilFormu.konum}
                onChange={(event) => setProfilFormu((current) => ({ ...current, konum: event.target.value }))}
                placeholder="İl veya il / ilçe seçin"
              />
              <datalist id={konumDatalistId}>
                {konumSecenekleri.map((item) => (
                  <option key={item} value={item} />
                ))}
              </datalist>
            </label>
            <label>
              <span>Telefon numarası</span>
              <TelefonNumarasiInput
                value={profilFormu.telefon}
                onChange={(telefon) => setProfilFormu((current) => ({ ...current, telefon }))}
                required={profilFormu.yayinda}
              />
            </label>
            <label>
              <span>Deneyim yılı</span>
              <input
                type="number"
                min={0}
                value={profilFormu.deneyimYili}
                onChange={(event) => setProfilFormu((current) => ({ ...current, deneyimYili: Number(event.target.value || 0) }))}
              />
            </label>
            <ProfilResmiYukleyici
              className="accountant-form-grid__wide"
              value={profilFormu.profilResmiUrl}
              onChange={(profilResmiUrl) => {
                setProfilFormu((current) => ({ ...current, profilResmiUrl }));
                setMesaj("Profil resmi yüklendi.");
              }}
              required={profilFormu.yayinda}
              disabled={islemde === "profil"}
              onBusyChange={setProfilResmiYukleniyor}
              onError={setHata}
            />
            <label>
              <span>Ücret bilgisi</span>
              <input value={profilFormu.ucretBilgisi} onChange={(event) => setProfilFormu((current) => ({ ...current, ucretBilgisi: event.target.value }))} required={profilFormu.yayinda} />
            </label>
            <label>
              <span>Uzmanlıklar</span>
              <input value={profilFormu.uzmanliklar} onChange={(event) => setProfilFormu((current) => ({ ...current, uzmanliklar: event.target.value }))} />
            </label>
            <label>
              <span>Müşteri tipi</span>
              <input value={profilFormu.musteriTipleri} onChange={(event) => setProfilFormu((current) => ({ ...current, musteriTipleri: event.target.value }))} />
            </label>
            <label className="accountant-form-grid__wide">
              <span>Kısa açıklama</span>
              <textarea value={profilFormu.kisaAciklama} onChange={(event) => setProfilFormu((current) => ({ ...current, kisaAciklama: event.target.value }))} rows={4} />
            </label>
          </div>
          <MuhasebeciProfilOnizleme
            resimUrl={profilFormu.profilResmiUrl}
            unvan={profilFormu.unvan}
            konum={profilFormu.konum}
            deneyimYili={profilFormu.deneyimYili}
            ucretBilgisi={profilFormu.ucretBilgisi}
            uzmanliklar={profilFormu.uzmanliklar}
            musteriTipleri={profilFormu.musteriTipleri}
            kisaAciklama={profilFormu.kisaAciklama}
          />
          <button type="submit" disabled={islemde === "profil" || profilResmiYukleniyor}>
            {islemde === "profil" ? <Loader2 size={16} className="spin" /> : <Check size={16} />}
            <span>Profili kaydet</span>
          </button>
        </form>

      </section>

      <section className="accountant-section accountant-section--full">
        <header>
          <div>
            <span className="accountant-eyebrow">Bekleyen talepler</span>
            <h2>Pazaryeri talepleri</h2>
          </div>
          <strong className="accountant-count">{panel.bekleyenTalepler.length}</strong>
        </header>
        {panel.bekleyenTalepler.length === 0 ? (
          <p className="accountant-empty-row">Bekleyen pazaryeri talebi yok.</p>
        ) : (
          <div className="accountant-request-list">
            {panel.bekleyenTalepler.map((talep) => (
              <article key={talep.id}>
                <div>
                  <strong>{talep.musteriAdi || "Müşteri"}</strong>
                  <span>{talep.mesaj || "Mesaj eklenmemiş."}</span>
                  <small>{tarihBic(talep.createdAt)}</small>
                </div>
                <select
                  value={talepYetkileri[talep.id] ?? talep.yetkiSeviyesi}
                  onChange={(event) => setTalepYetkileri((current) => ({ ...current, [talep.id]: event.target.value as YetkiSeviyesi }))}
                >
                  <option value="OkumaRapor">Okuma + rapor</option>
                  <option value="TamIslem">Tam işlem</option>
                </select>
                <button
                  type="button"
                  onClick={() => sohbetAc({ tur: "talep", id: talep.id, baslik: talep.musteriAdi || "Müşteri" })}
                  disabled={sohbetIslemde}
                >
                  <MessageCircle size={15} />
                  <span>Sohbet</span>
                </button>
                <button type="button" onClick={() => talepKabulEt(talep)} disabled={islemde === `kabul-${talep.id}`}>
                  {islemde === `kabul-${talep.id}` ? <Loader2 size={15} className="spin" /> : <Check size={15} />}
                  <span>Kabul</span>
                </button>
                <button type="button" className="accountant-danger-button" onClick={() => talepReddet(talep)} disabled={islemde === `red-${talep.id}`}>
                  {islemde === `red-${talep.id}` ? <Loader2 size={15} className="spin" /> : <X size={15} />}
                  <span>Red</span>
                </button>
              </article>
            ))}
          </div>
        )}
      </section>

    </main>
  );
}

function temizleSohbetYonlendirmesi() {
  const params = new URLSearchParams(window.location.search);
  params.delete("talepId");
  params.delete("musteriId");
  params.delete("sohbet");
  const query = params.toString();
  window.history.replaceState(null, "", `${window.location.pathname}${query ? `?${query}` : ""}`);
}

function profilFormuOlustur(panel: MuhasebeciPanel): ProfilFormu {
  return {
    yayinda: panel.profil?.yayinda ?? false,
    unvan: panel.profil?.unvan || panel.muhasebeciAdi || "",
    konum: panel.profil?.konum || "",
    telefon: panel.profil?.telefon || "",
    deneyimYili: panel.profil?.deneyimYili ?? 1,
    profilResmiUrl: panel.profil?.profilResmiUrl || "",
    ucretBilgisi: panel.profil?.ucretBilgisi || "",
    uzmanliklar: panel.profil?.uzmanliklar || "",
    musteriTipleri: panel.profil?.musteriTipleri || "",
    kisaAciklama: panel.profil?.kisaAciklama || ""
  };
}

function buildKonumSecenekleri() {
  return TURKIYE_KONUMLARI.flatMap((konum) => [
    formatLocationName(konum.il),
    ...konum.ilceler.map((ilce) => `${formatLocationName(konum.il)} / ${formatLocationName(ilce)}`)
  ]);
}

function formatLocationName(value: string) {
  return value
    .toLocaleLowerCase("tr-TR")
    .split(" ")
    .map((part) => part ? `${part[0].toLocaleUpperCase("tr-TR")}${part.slice(1)}` : part)
    .join(" ");
}

function tarihBic(value: string) {
  if (!value)
    return "-";

  return new Date(value).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}
