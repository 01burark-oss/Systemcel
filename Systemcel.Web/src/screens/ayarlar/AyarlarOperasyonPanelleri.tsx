import React from "react";
import {
  ArrowRightLeft,
  Braces,
  CheckCircle2,
  Copy,
  DatabaseBackup,
  FileSpreadsheet,
  KeyRound,
  LockKeyhole,
  ShieldCheck,
  Trash2,
  Upload,
  UserRoundPlus,
  Users
} from "lucide-react";
import { jsonOku } from "../../shared/json";

interface Uyelik {
  id: number;
  kullaniciId: number | null;
  eposta: string;
  adSoyad: string;
  rol: string;
  durum: string;
  davetKodu: string;
}

interface UyelikListe {
  sahibiMi: boolean;
  isletmeId: number;
  isletmeAdi: string;
  uyelikler: Uyelik[];
}

interface PinDurumu {
  varsayilanPin: boolean;
  mesaj: string;
}

interface AktarimKodu {
  code: string;
  expiresAtUtc: string;
  targetIsletmeId: number;
  packageEndpoint: string;
}

interface AktarimSonucu {
  packageId: string;
  status: string;
  message: string;
}

interface GelistiriciApiAnahtari {
  id: number;
  ad: string;
  prefix: string;
  scopes: string[];
  createdAt: string;
  lastUsedAt: string | null;
  expiresAt: string;
  revokedAt: string | null;
}

interface GelistiriciApiAnahtarListesi {
  anahtarlar: GelistiriciApiAnahtari[];
}

interface GelistiriciApiAnahtarSonucu extends GelistiriciApiAnahtari {
  anahtar: string;
}

const rolEtiketi: Record<string, string> = {
  isletme_sahibi: "İşletme sahibi",
  yonetici: "Yönetici",
  personel: "Personel"
};
const MAKSIMUM_AKTARIM_PAKETI = 50 * 1024 * 1024;
const GELISTIRICI_API_SCOPES = [
  { value: "summary:read", label: "İşletme özeti" },
  { value: "accounts:read", label: "Cari hesaplar" },
  { value: "products:read", label: "Ürünler" },
  { value: "invoices:read", label: "Faturalar" },
  { value: "bank:read", label: "Banka hareketleri" }
] as const;

export function AyarlarOperasyonPanelleri() {
  return (
    <div className="settings-operations-grid">
      <PinPaneli />
      <EkipPaneli />
      <MasaustuAktarimPaneli />
      <HariciVeriAktarimPaneli />
      <GelistiriciApiPaneli />
    </div>
  );
}

export function GelistiriciApiPaneli() {
  const [anahtarlar, setAnahtarlar] = React.useState<GelistiriciApiAnahtari[]>([]);
  const [ad, setAd] = React.useState("");
  const [scopes, setScopes] = React.useState<string[]>(["summary:read"]);
  const [expiresInDays, setExpiresInDays] = React.useState(90);
  const [yeniAnahtar, setYeniAnahtar] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [upgrade, setUpgrade] = React.useState(false);
  const [islemde, setIslemde] = React.useState(false);

  React.useEffect(() => {
    jsonOku<GelistiriciApiAnahtarListesi>("/api/ekran/gelistirici-api/anahtarlar")
      .then((result) => {
        setAnahtarlar(result.anahtarlar);
        setUpgrade(false);
      })
      .catch((error: Error) => {
        const detail = error.message || "API anahtarları yüklenemedi.";
        setHata(detail);
        setUpgrade(/plan|abonelik|kullanılamaz|açık değil/i.test(detail));
      });
  }, []);

  function scopeDegistir(scope: string, secili: boolean) {
    setScopes((onceki) => secili
      ? [...onceki, scope]
      : onceki.filter((item) => item !== scope));
  }

  async function anahtarOlustur(event: React.FormEvent) {
    event.preventDefault();
    try {
      setIslemde(true);
      setHata("");
      setMesaj("");
      const result = await jsonOku<GelistiriciApiAnahtarSonucu>("/api/ekran/gelistirici-api/anahtarlar", {
        method: "POST",
        body: JSON.stringify({ ad: ad.trim(), scopes, expiresInDays })
      });
      const { anahtar, ...guvenliListeKaydi } = result;
      setAnahtarlar((onceki) => [guvenliListeKaydi, ...onceki]);
      setYeniAnahtar(anahtar);
      setAd("");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "API anahtarı oluşturulamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function anahtarKopyala() {
    try {
      if (!navigator.clipboard?.writeText) throw new Error("Pano kullanılamıyor.");
      await navigator.clipboard.writeText(yeniAnahtar);
      setYeniAnahtar("");
      setMesaj("API anahtarı kopyalandı.");
    } catch {
      setMesaj("Anahtarı alandan elle kopyalayın.");
    }
  }

  async function anahtarIptalEt(anahtar: GelistiriciApiAnahtari) {
    if (!window.confirm(`${anahtar.ad} anahtarı iptal edilsin mi? Bu işlem geri alınamaz.`)) return;
    try {
      setIslemde(true);
      setHata("");
      setMesaj("");
      await jsonOku(`/api/ekran/gelistirici-api/anahtarlar/${anahtar.id}`, { method: "DELETE" });
      const revokedAt = new Date().toISOString();
      setAnahtarlar((onceki) => onceki.map((item) => item.id === anahtar.id ? { ...item, revokedAt } : item));
      setMesaj("API anahtarı iptal edildi.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "API anahtarı iptal edilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  if (upgrade) {
    return (
      <section className="settings-card settings-operation-card settings-operation-card--api settings-api-upgrade" role="status">
        <header className="settings-card__header settings-operation-card__header">
          <span className="settings-operation-card__icon"><Braces size={21} /></span>
          <div><h2>Geliştirici API planınızda açık değil</h2><p>Büyüme veya Kurumsal planla salt okunur API anahtarı oluşturabilirsiniz.</p></div>
        </header>
        <a className="settings-btn settings-btn--navy" href="/app/abonelik">Planları incele</a>
      </section>
    );
  }

  return (
    <section className="settings-card settings-operation-card settings-operation-card--api">
      <header className="settings-card__header settings-operation-card__header">
        <span className="settings-operation-card__icon"><Braces size={21} /></span>
        <div><h2>Geliştirici API</h2><p>Entegrasyonlarınız için süresi ve okuma yetkileri sınırlı anahtar oluşturun.</p></div>
      </header>

      <form className="settings-api-form" onSubmit={anahtarOlustur}>
        <div className="settings-api-form__fields">
          <label><span>Anahtar adı</span><input aria-label="Anahtar adı" maxLength={100} value={ad} onChange={(event) => setAd(event.target.value)} placeholder="Rapor entegrasyonu" required /></label>
          <label><span>Geçerlilik</span><select aria-label="Geçerlilik süresi" value={expiresInDays} onChange={(event) => setExpiresInDays(Number(event.target.value))}>{[30, 90, 180, 365].map((gun) => <option key={gun} value={gun}>{gun} gün</option>)}</select></label>
        </div>
        <fieldset className="settings-api-scopes">
          <legend>Okuma yetkileri</legend>
          {GELISTIRICI_API_SCOPES.map((scope) => (
            <label key={scope.value}>
              <input type="checkbox" checked={scopes.includes(scope.value)} onChange={(event) => scopeDegistir(scope.value, event.target.checked)} />
              <span>{scope.label}</span>
            </label>
          ))}
        </fieldset>
        <button className="settings-btn settings-btn--green" disabled={islemde || !ad.trim() || scopes.length === 0} type="submit">Anahtar oluştur</button>
      </form>

      {yeniAnahtar ? (
        <div className="settings-api-secret" role="status">
          <strong>Bu anahtar tekrar gösterilmeyecek.</strong>
          <div><input aria-label="Yeni API anahtarı" readOnly spellCheck={false} value={yeniAnahtar} /><button className="settings-icon-action" type="button" aria-label="API anahtarını kopyala" onClick={() => void anahtarKopyala()}><Copy size={17} /></button></div>
        </div>
      ) : null}

      <div className="settings-api-list" aria-label="API anahtarları">
        {anahtarlar.length === 0 ? <p className="settings-api-empty">Henüz API anahtarı yok.</p> : anahtarlar.map((anahtar) => (
          <div className="settings-api-row" key={anahtar.id}>
            <div><strong>{anahtar.ad}</strong><code>{anahtar.prefix}…</code><small>{anahtar.scopes.length} okuma yetkisi · {new Date(anahtar.expiresAt).toLocaleDateString("tr-TR")} tarihine kadar</small></div>
            {anahtar.revokedAt ? <span className="settings-status-pill">İptal edildi</span> : <button className="settings-icon-action settings-icon-action--danger" type="button" disabled={islemde} aria-label={`${anahtar.ad} anahtarını iptal et`} onClick={() => void anahtarIptalEt(anahtar)}><Trash2 size={17} /></button>}
          </div>
        ))}
      </div>
      <Geribildirim hata={hata} mesaj={mesaj} />
    </section>
  );
}

function PinPaneli() {
  const [durum, setDurum] = React.useState<PinDurumu | null>(null);
  const [mevcutPin, setMevcutPin] = React.useState("");
  const [yeniPin, setYeniPin] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);

  React.useEffect(() => {
    jsonOku<PinDurumu>("/api/ekran/ayarlar/pin").then(setDurum).catch((error: Error) => setHata(error.message));
  }, []);

  async function pinDegistir(event: React.FormEvent) {
    event.preventDefault();
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<{ mesaj: string }>("/api/ekran/ayarlar/pin", {
        method: "PUT",
        body: JSON.stringify({ mevcutPin, yeniPin })
      });
      setMevcutPin("");
      setYeniPin("");
      setDurum({ varsayilanPin: false, mesaj: result.mesaj });
      setMesaj(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "PIN güncellenemedi.");
    } finally {
      setIslemde(false);
    }
  }

  return (
    <section className="settings-card settings-operation-card">
      <header className="settings-card__header settings-operation-card__header">
        <span className="settings-operation-card__icon"><LockKeyhole size={21} /></span>
        <div><h2>Uygulama kilidi</h2><p>Ortak kullanılan cihazlarda finansal verileri 4 haneli PIN ile koruyun.</p></div>
      </header>
      {durum?.varsayilanPin ? (
        <div className="settings-inline-notice settings-inline-notice--warning">
          <KeyRound size={17} /> Varsayılan PIN <strong>0000</strong>. Şimdi değiştirin.
        </div>
      ) : (
        <div className="settings-inline-notice"><ShieldCheck size={17} /> Özel PIN etkin.</div>
      )}
      <form className="settings-operation-form" onSubmit={pinDegistir}>
        <label><span>Mevcut PIN</span><input aria-label="Mevcut PIN" inputMode="numeric" autoComplete="current-password" maxLength={4} pattern="[0-9]{4}" value={mevcutPin} onChange={(event) => setMevcutPin(event.target.value.replace(/\D/g, ""))} required /></label>
        <label><span>Yeni PIN</span><input aria-label="Yeni PIN" inputMode="numeric" autoComplete="new-password" maxLength={4} pattern="[0-9]{4}" value={yeniPin} onChange={(event) => setYeniPin(event.target.value.replace(/\D/g, ""))} required /></label>
        <button className="settings-btn settings-btn--navy" disabled={islemde || mevcutPin.length !== 4 || yeniPin.length !== 4} type="submit">PIN'i değiştir</button>
      </form>
      <Geribildirim hata={hata} mesaj={mesaj} />
    </section>
  );
}

function EkipPaneli() {
  const queryCode = React.useMemo(() => new URLSearchParams(window.location.search).get("davet") ?? "", []);
  const [liste, setListe] = React.useState<UyelikListe | null>(null);
  const [eposta, setEposta] = React.useState("");
  const [rol, setRol] = React.useState("personel");
  const [davetKodu, setDavetKodu] = React.useState(queryCode);
  const [davetBaglantisi, setDavetBaglantisi] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);

  const yukle = React.useCallback(async () => {
    setListe(await jsonOku<UyelikListe>("/api/ekran/uyelikler"));
  }, []);

  React.useEffect(() => {
    yukle().catch((error: Error) => setHata(error.message));
  }, [yukle]);

  async function calistir(islem: () => Promise<UyelikListe>, basari: string) {
    try {
      setIslemde(true);
      setHata("");
      setListe(await islem());
      setMesaj(basari);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Ekip işlemi tamamlanamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function davetOlustur(event: React.FormEvent) {
    event.preventDefault();
    try {
      setIslemde(true);
      setHata("");
      const invite = await jsonOku<{ davetKodu: string }>("/api/ekran/uyelikler/davet", {
        method: "POST",
        body: JSON.stringify({ eposta, rol })
      });
      const link = `${window.location.origin}/app/ayarlar?davet=${encodeURIComponent(invite.davetKodu)}`;
      setDavetBaglantisi(link);
      setEposta("");
      try {
        if (!navigator.clipboard?.writeText) throw new Error("Clipboard API kullanılamıyor.");
        await navigator.clipboard.writeText(link);
        setMesaj("Davet oluşturuldu. Bağlantı panoya kopyalandı.");
      } catch {
        setMesaj("Davet oluşturuldu. Bağlantıyı aşağıdan kopyalayın.");
      }
      await yukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Davet oluşturulamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function davetKopyala(code: string) {
    const link = `${window.location.origin}/app/ayarlar?davet=${encodeURIComponent(code)}`;
    setDavetBaglantisi(link);
    try {
      if (!navigator.clipboard?.writeText) throw new Error("Clipboard API kullanılamıyor.");
      await navigator.clipboard.writeText(link);
      setMesaj("Davet bağlantısı panoya kopyalandı.");
    } catch {
      setMesaj("Bağlantıyı aşağıdan kopyalayın.");
    }
  }

  return (
    <section className="settings-card settings-operation-card settings-operation-card--team">
      <header className="settings-card__header settings-operation-card__header">
        <span className="settings-operation-card__icon"><Users size={21} /></span>
        <div><h2>Ekip ve yetkiler</h2><p>{liste?.isletmeAdi ?? "Aktif işletme"} için üyeleri ve bekleyen davetleri yönetin.</p></div>
      </header>

      {liste?.sahibiMi ? (
        <form className="settings-operation-form settings-operation-form--invite" onSubmit={davetOlustur}>
          <label><span>E-posta</span><input type="email" value={eposta} onChange={(event) => setEposta(event.target.value)} placeholder="ekip@isletme.com" required /></label>
          <label><span>Rol</span><select value={rol} onChange={(event) => setRol(event.target.value)}><option value="personel">Personel</option><option value="yonetici">Yönetici</option></select></label>
          <button className="settings-btn settings-btn--green" disabled={islemde} type="submit"><UserRoundPlus size={17} /> Davet oluştur</button>
        </form>
      ) : null}

      <div className="settings-team-list" aria-live="polite">
        {(liste?.uyelikler ?? []).map((uye) => (
          <div className="settings-team-row" key={uye.id}>
            <div className="settings-team-row__identity"><strong>{uye.adSoyad || uye.eposta}</strong><small>{uye.adSoyad ? uye.eposta : uye.durum === "DavetBekliyor" ? "Daveti bekliyor" : "Aktif üye"}</small></div>
            {liste?.sahibiMi && uye.rol !== "isletme_sahibi" ? (
              <select aria-label={`${uye.eposta} rolü`} value={uye.rol} disabled={islemde || uye.durum !== "Aktif"} onChange={(event) => void calistir(() => jsonOku<UyelikListe>(`/api/ekran/uyelikler/${uye.id}/rol`, { method: "PUT", body: JSON.stringify({ rol: event.target.value }) }), "Rol güncellendi.")}><option value="personel">Personel</option><option value="yonetici">Yönetici</option></select>
            ) : <span className="settings-status-pill active">{rolEtiketi[uye.rol] ?? uye.rol}</span>}
            <div className="settings-team-row__actions">
              {uye.davetKodu ? <button className="settings-icon-action" type="button" aria-label="Davet bağlantısını kopyala" onClick={() => void davetKopyala(uye.davetKodu)}><Copy size={17} /></button> : null}
              {liste?.sahibiMi && uye.rol !== "isletme_sahibi" && uye.durum === "Aktif" ? <button className="settings-icon-action" type="button" aria-label="Sahipliği devret" onClick={() => window.confirm("İşletme sahipliği bu üyeye devredilsin mi?") && void calistir(() => jsonOku<UyelikListe>(`/api/ekran/uyelikler/${uye.id}/sahiplik-devri`, { method: "POST" }), "İşletme sahipliği devredildi.")}><ArrowRightLeft size={17} /></button> : null}
              {liste?.sahibiMi && uye.rol !== "isletme_sahibi" ? <button className="settings-icon-action settings-icon-action--danger" type="button" aria-label="Üyeyi kaldır" onClick={() => window.confirm("Bu üye veya davet kaldırılsın mı?") && void calistir(() => jsonOku<UyelikListe>(`/api/ekran/uyelikler/${uye.id}`, { method: "DELETE" }), "Üye kaldırıldı.")}><Trash2 size={17} /></button> : null}
            </div>
          </div>
        ))}
      </div>

      {davetBaglantisi ? <a className="settings-invite-link" href={davetBaglantisi}>Davet bağlantısını aç</a> : null}

      <form className="settings-accept-invite" onSubmit={(event) => { event.preventDefault(); void calistir(() => jsonOku<UyelikListe>("/api/ekran/uyelikler/davet/kabul", { method: "POST", body: JSON.stringify({ davetKodu }) }), "Davet kabul edildi."); }}>
        <label><span>Davet kodunuz varsa</span><input value={davetKodu} onChange={(event) => setDavetKodu(event.target.value)} placeholder="Davet kodunu yapıştırın" /></label>
        <button className="settings-btn settings-btn--navy" disabled={islemde || !davetKodu.trim()} type="submit">Daveti kabul et</button>
      </form>
      <Geribildirim hata={hata} mesaj={mesaj} />
    </section>
  );
}

function MasaustuAktarimPaneli() {
  const [kod, setKod] = React.useState<AktarimKodu | null>(null);
  const [dosya, setDosya] = React.useState<File | null>(null);
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);

  function dosyaSec(file: File | null) {
    setMesaj("");
    if (!file) {
      setDosya(null);
      return;
    }
    if (!file.name.toLocaleLowerCase("tr-TR").endsWith(".zip")) {
      setDosya(null);
      setHata("Yalnız Systemcel ZIP paketi seçebilirsiniz.");
      return;
    }
    if (file.size > MAKSIMUM_AKTARIM_PAKETI) {
      setDosya(null);
      setHata("Aktarım paketi en fazla 50 MB olabilir.");
      return;
    }
    setHata("");
    setDosya(file);
  }

  async function kodOlustur() {
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<AktarimKodu>("/api/import/desktop/codes", { method: "POST", body: JSON.stringify({}) });
      setKod(result);
      setMesaj("Tek kullanımlık aktarım kodu hazır.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Aktarım kodu oluşturulamadı.");
    } finally {
      setIslemde(false);
    }
  }

  async function paketiYukle(event: React.FormEvent) {
    event.preventDefault();
    if (!kod || !dosya) return;
    try {
      setIslemde(true);
      setHata("");
      const form = new FormData();
      form.append("code", kod.code);
      form.append("package", dosya);
      const result = await jsonOku<AktarimSonucu>(kod.packageEndpoint, { method: "POST", body: form });
      setMesaj(result.message || "Veriler içe aktarıldı.");
      setDosya(null);
      setKod(null);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Aktarım paketi yüklenemedi.");
    } finally {
      setIslemde(false);
    }
  }

  return (
    <section className="settings-card settings-operation-card">
      <header className="settings-card__header settings-operation-card__header">
        <span className="settings-operation-card__icon"><DatabaseBackup size={21} /></span>
        <div><h2>Eski verileri aktar</h2><p>Masaüstü uygulamasının hazırladığı Systemcel ZIP paketini aktif işletmeye taşıyın.</p></div>
      </header>
      <div className="settings-import-steps"><span><strong>1</strong> Kod oluştur</span><span><strong>2</strong> Paketi seç</span><span><strong>3</strong> Güvenli aktar</span></div>
      {!kod ? <button className="settings-btn settings-btn--navy" type="button" disabled={islemde} onClick={() => void kodOlustur()}>Aktarım kodu oluştur</button> : (
        <form className="settings-import-form" onSubmit={paketiYukle}>
          <div className="settings-transfer-code"><span>Aktarım kodu</span><strong>{kod.code}</strong><small>{new Date(kod.expiresAtUtc).toLocaleString("tr-TR")} tarihine kadar geçerli</small></div>
          <label className="settings-file-picker"><Upload size={19} /><span>{dosya?.name ?? "Systemcel ZIP paketini seçin"}</span><input type="file" accept=".zip,application/zip" onChange={(event) => dosyaSec(event.target.files?.[0] ?? null)} /></label>
          <button className="settings-btn settings-btn--green" type="submit" disabled={islemde || !dosya}>Paketi içe aktar</button>
        </form>
      )}
      <Geribildirim hata={hata} mesaj={mesaj} />
    </section>
  );
}

type HariciVeriTuru = "cari" | "urun" | "stok" | "kategori" | "fatura";
interface HariciOnizleme { draftId: string; type: HariciVeriTuru; fileName: string; totalRows: number; validRows: number; duplicateRows: number; headers: string[]; sampleRows: Record<string, string>[]; errors: { row: number; message: string }[]; unsupportedReason?: string; }
interface HariciAktarimSonucu { applied: number; skippedDuplicates: number; errors: { row: number; message: string }[]; }

function HariciVeriAktarimPaneli() {
  const [tur, setTur] = React.useState<HariciVeriTuru>("cari");
  const [dosya, setDosya] = React.useState<File | null>(null);
  const [onizleme, setOnizleme] = React.useState<HariciOnizleme | null>(null);
  const [sonuc, setSonuc] = React.useState<HariciAktarimSonucu | null>(null);
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);

  const turEtiketi: Record<HariciVeriTuru, string> = { cari: "Cari kartı", urun: "Ürün / hizmet", stok: "Açılış stok", kategori: "Gelir / gider kalemi", fatura: "Açık fatura" };
  function dosyaSec(file: File | null) { setHata(""); setSonuc(null); setOnizleme(null); if (!file) return setDosya(null); if (!file.name.toLowerCase().endsWith(".csv")) return setHata("Şimdilik yalnızca CSV şablonu destekleniyor."); if (file.size > 10 * 1024 * 1024) return setHata("Dosya en fazla 10 MB olabilir."); setDosya(file); }
  async function onizle() {
    if (!dosya) return;
    try { setIslemde(true); setHata(""); setSonuc(null); const form = new FormData(); form.append("type", tur); form.append("file", dosya); setOnizleme(await jsonOku<HariciOnizleme>("/api/ekran/veri-aktarim/onizleme", { method: "POST", body: form })); }
    catch (error) { setHata(error instanceof Error ? error.message : "Dosya önizlenemedi."); }
    finally { setIslemde(false); }
  }
  async function uygula() {
    if (!onizleme || onizleme.errors.length > 0 || onizleme.unsupportedReason) return;
    try { setIslemde(true); setHata(""); setSonuc(await jsonOku<HariciAktarimSonucu>("/api/ekran/veri-aktarim/uygula", { method: "POST", body: JSON.stringify({ draftId: onizleme.draftId }) })); setOnizleme(null); setDosya(null); }
    catch (error) { setHata(error instanceof Error ? error.message : "Aktarım uygulanamadı."); }
    finally { setIslemde(false); }
  }
  return <section className="settings-card settings-operation-card settings-operation-card--migration">
    <header className="settings-card__header settings-operation-card__header"><span className="settings-operation-card__icon"><FileSpreadsheet size={21} /></span><div><h2>CSV ile veri taşı</h2><p>Dosyayı önce kontrol edin; onayınız olmadan kayıt eklenmez.</p></div></header>
    <div className="settings-migration-steps"><span><strong>1</strong> Şablonu indir</span><span><strong>2</strong> Önizle</span><span><strong>3</strong> Onayla</span></div>
    <div className="settings-migration-form"><label><span>Veri türü</span><select value={tur} onChange={(event) => { setTur(event.target.value as HariciVeriTuru); setOnizleme(null); setSonuc(null); }} disabled={islemde}><option value="cari">Cari kartı ve açılış bakiyesi</option><option value="urun">Ürün / hizmet ve açılış stok</option><option value="stok">Açılış stok hareketi</option><option value="kategori">Gelir / gider kalemi</option><option value="fatura">Açık fatura (bu sürümde yok)</option></select></label><a className="settings-btn settings-btn--navy" href={`/api/ekran/veri-aktarim/sablon/${tur}`} download>Şablonu indir</a></div>
    <label className="settings-file-picker"><Upload size={19} /><span>{dosya?.name ?? `${turEtiketi[tur]} CSV dosyasını seçin`}</span><input type="file" accept=".csv,text/csv" onChange={(event) => dosyaSec(event.target.files?.[0] ?? null)} /></label>
    <button className="settings-btn settings-btn--green" type="button" disabled={islemde || !dosya} onClick={() => void onizle()}>{islemde ? "Kontrol ediliyor..." : "Önizlemeyi göster"}</button>
    {onizleme ? <div className="settings-migration-preview" aria-live="polite"><div className="settings-migration-summary"><strong>{onizleme.totalRows} satır</strong><span>{onizleme.validRows} uygun</span><span>{onizleme.duplicateRows} tekrar</span>{onizleme.errors.length ? <span className="error">{onizleme.errors.length} hata</span> : null}</div>{onizleme.unsupportedReason ? <div className="settings-inline-notice settings-inline-notice--warning">{onizleme.unsupportedReason}</div> : null}<div className="settings-migration-table-wrap"><table><thead><tr>{onizleme.headers.slice(0, 5).map((header) => <th key={header}>{header}</th>)}</tr></thead><tbody>{onizleme.sampleRows.map((row, index) => <tr key={index}>{onizleme.headers.slice(0, 5).map((header) => <td key={header}>{row[header] ?? ""}</td>)}</tr>)}</tbody></table></div>{onizleme.errors.length ? <ul className="settings-migration-errors">{onizleme.errors.slice(0, 8).map((error) => <li key={`${error.row}-${error.message}`}>Satır {error.row}: {error.message}</li>)}</ul> : <button className="settings-btn settings-btn--green" type="button" disabled={islemde || Boolean(onizleme.unsupportedReason)} onClick={() => void uygula()}>Bu özeti onayla ve aktar</button>}</div> : null}
    {sonuc ? <div className={`settings-operation-feedback ${sonuc.errors.length ? "error" : ""}`} role={sonuc.errors.length ? "alert" : "status"}>{sonuc.errors.length ? <ul className="settings-migration-errors">{sonuc.errors.slice(0, 8).map((error) => <li key={`${error.row}-${error.message}`}>Satır {error.row}: {error.message}</li>)}</ul> : <><CheckCircle2 size={17} />{sonuc.applied} kayıt aktarıldı. {sonuc.skippedDuplicates ? `${sonuc.skippedDuplicates} tekrar atlandı.` : ""}</>}</div> : null}
    <Geribildirim hata={hata} mesaj="" />
  </section>;
}

function Geribildirim({ hata, mesaj }: { hata: string; mesaj: string }) {
  if (!hata && !mesaj) return null;
  return <div className={`settings-operation-feedback ${hata ? "error" : ""}`} role={hata ? "alert" : "status"}>{hata ? null : <CheckCircle2 size={17} />}{hata || mesaj}</div>;
}
