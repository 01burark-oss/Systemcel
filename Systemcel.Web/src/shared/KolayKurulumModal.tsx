import React from "react";
import { BriefcaseBusiness, Building2, MapPin, Sparkles } from "lucide-react";
import { jsonOku } from "./json";
import { MuhasebeciProfilOnizleme } from "./MuhasebeciProfilOnizleme";
import { ProfilResmiYukleyici } from "./ProfilResmiYukleyici";
import { TelefonNumarasiInput } from "./TelefonNumarasiInput";
import { TURKIYE_KONUMLARI } from "./turkiyeKonumlari";

const ACCOUNT_TYPE_INTENT_KEY = "systemcel.accountTypeIntent";

export interface KolayKurulumTur {
  kod: string;
  ad: string;
  aciklama: string;
  gelirKalemleri: string[];
  giderKalemleri: string[];
}

export interface KolayKurulumEkran {
  tamamlandi: boolean;
  isletmeId: number;
  isletmeAdi: string;
  hesapTipi: "Isletme" | "Muhasebeci";
  isletmeTuru: string;
  konum: string;
  muhasebeciVarMi: boolean;
  mesaj: string;
  turler: KolayKurulumTur[];
}

interface KolayKurulumModalProps {
  ekran: KolayKurulumEkran;
  onComplete: (ekran: KolayKurulumEkran) => void;
  onClose: () => void;
}

export function KolayKurulumModal({ ekran, onComplete, onClose }: KolayKurulumModalProps) {
  const baslangicKonum = parseKonum(ekran.konum);
  const baslangicHesapTipi = React.useMemo(() => resolveInitialAccountType(ekran.hesapTipi), [ekran.hesapTipi]);
  const hesapTipiSecimiKilitli = React.useMemo(() => hasAccountTypeIntent(ekran.hesapTipi), [ekran.hesapTipi]);
  const isletmeTurleri = React.useMemo(() => ekran.turler.filter((item) => item.kod !== "MuhasebeOfisi"), [ekran.turler]);
  const [isletmeAdi, setIsletmeAdi] = React.useState(ekran.isletmeAdi || "");
  const [hesapTipi, setHesapTipi] = React.useState<"Isletme" | "Muhasebeci">(baslangicHesapTipi);
  const [isletmeTuru, setIsletmeTuru] = React.useState(
    ekran.isletmeTuru && ekran.isletmeTuru !== "MuhasebeOfisi"
      ? ekran.isletmeTuru
      : isletmeTurleri[0]?.kod || "Genel"
  );
  const [muhasebeciVarMi, setMuhasebeciVarMi] = React.useState(Boolean(ekran.muhasebeciVarMi));
  const [profilTelefon, setProfilTelefon] = React.useState("");
  const [profilDeneyimYili, setProfilDeneyimYili] = React.useState("1");
  const [profilResmiUrl, setProfilResmiUrl] = React.useState("");
  const [profilUcretBilgisi, setProfilUcretBilgisi] = React.useState("");
  const [profilUzmanliklar, setProfilUzmanliklar] = React.useState("");
  const [profilMusteriTipleri, setProfilMusteriTipleri] = React.useState("");
  const [profilKisaAciklama, setProfilKisaAciklama] = React.useState("");
  const [profilResmiYukleniyor, setProfilResmiYukleniyor] = React.useState(false);
  const [il, setIl] = React.useState(() => resolveIl(baslangicKonum.il));
  const [ilce, setIlce] = React.useState(() => resolveIlce(resolveIl(baslangicKonum.il), baslangicKonum.ilce));
  const [kaydediliyor, setKaydediliyor] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const seciliTur = isletmeTurleri.find((item) => item.kod === isletmeTuru) ?? isletmeTurleri[0];
  const seciliIl = TURKIYE_KONUMLARI.find((item) => item.il === il);
  const konum = il && ilce ? `${formatLocationName(il)} / ${formatLocationName(ilce)}` : "";

  React.useEffect(() => {
    if (!seciliIl) {
      setIlce("");
      return;
    }

    if (ilce && !seciliIl.ilceler.includes(ilce)) {
      setIlce("");
    }
  }, [il, ilce, seciliIl]);

  React.useEffect(() => {
    if (hesapTipi !== "Isletme")
      return;

    if (!isletmeTurleri.some((item) => item.kod === isletmeTuru)) {
      setIsletmeTuru(isletmeTurleri[0]?.kod || "Genel");
    }
  }, [hesapTipi, isletmeTuru, isletmeTurleri]);

  async function kaydet(event: React.FormEvent) {
    event.preventDefault();
    setHata("");

    if (hesapTipi === "Muhasebeci" && !profilResmiUrl.trim()) {
      setHata("Profil resmini seçip kırpın.");
      return;
    }

    setKaydediliyor(true);

    try {
      const sonuc = await jsonOku<KolayKurulumEkran>("/api/ekran/kolay-kurulum", {
        method: "POST",
        body: JSON.stringify({
          isletmeAdi,
          hesapTipi,
          isletmeTuru: hesapTipi === "Muhasebeci" ? "MuhasebeOfisi" : isletmeTuru,
          konum,
          muhasebeciVarMi: hesapTipi === "Isletme" ? muhasebeciVarMi : false,
          muhasebeciProfil: hesapTipi === "Muhasebeci"
            ? {
                yayinda: false,
                unvan: isletmeAdi,
                konum,
                telefon: profilTelefon,
                deneyimYili: Number(profilDeneyimYili || 0),
                profilResmiUrl,
                ucretBilgisi: profilUcretBilgisi,
                uzmanliklar: profilUzmanliklar,
                musteriTipleri: profilMusteriTipleri,
                kisaAciklama: profilKisaAciklama
              }
            : null
        })
      });
      window.localStorage.removeItem(ACCOUNT_TYPE_INTENT_KEY);
      onComplete(sonuc);
      if (hesapTipi === "Isletme" && muhasebeciVarMi) {
        window.setTimeout(() => {
          window.location.href = "/app/muhasebeciler";
        }, 0);
      }
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Kurulum kaydedilemedi.");
    } finally {
      setKaydediliyor(false);
    }
  }

  return (
    <div className="setup-overlay" role="dialog" aria-modal="true" aria-labelledby="setup-title">
      <form className="setup-modal" onSubmit={kaydet}>
        <header className="setup-modal__header">
          <span className="setup-modal__icon">
            <Sparkles size={24} />
          </span>
          <div>
            <p>Kolay Kurulum</p>
            <h2 id="setup-title">{hesapTipi === "Muhasebeci" ? "Muhasebeci ofisini hazırlayalım" : "Çalışma alanını birkaç bilgiyle hazırlayalım"}</h2>
          </div>
        </header>

        <div className="setup-modal__grid">
          {hesapTipiSecimiKilitli ? (
            <div className="setup-account-summary">
              {hesapTipi === "Muhasebeci" ? <BriefcaseBusiness size={17} /> : <Building2 size={17} />}
              <span>{hesapTipi === "Muhasebeci" ? "Muhasebeci hesabı" : "İşletme hesabı"}</span>
            </div>
          ) : (
            <div className="setup-account-switch" role="group" aria-label="Hesap tipi">
              <button
                type="button"
                className={hesapTipi === "Isletme" ? "active" : ""}
                onClick={() => setHesapTipi("Isletme")}
              >
                <Building2 size={17} />
                <span>İşletme</span>
              </button>
              <button
                type="button"
                className={hesapTipi === "Muhasebeci" ? "active" : ""}
                onClick={() => setHesapTipi("Muhasebeci")}
              >
                <BriefcaseBusiness size={17} />
                <span>Muhasebeci</span>
              </button>
            </div>
          )}

          <label className="setup-field">
            <span>
              {hesapTipi === "Muhasebeci" ? <BriefcaseBusiness size={17} /> : <Building2 size={17} />}
              {hesapTipi === "Muhasebeci" ? "Ofis adı" : "İşletme adı"}
            </span>
            <input
              value={isletmeAdi}
              onChange={(event) => setIsletmeAdi(event.target.value)}
              placeholder={hesapTipi === "Muhasebeci" ? "Örn. Yılmaz Muhasebe" : "Örn. Systemcel Kafe"}
              required
            />
          </label>

          {hesapTipi === "Isletme" ? (
            <label className="setup-field setup-field--wide">
              <span>
                <Building2 size={17} />
                Sektör
              </span>
              <select value={isletmeTuru} onChange={(event) => setIsletmeTuru(event.target.value)} required>
                {isletmeTurleri.map((tur) => (
                  <option key={tur.kod} value={tur.kod}>
                    {tur.ad}
                  </option>
                ))}
              </select>
            </label>
          ) : null}

          <label className="setup-field">
            <span>
              <MapPin size={17} />
              İl
            </span>
            <select value={il} onChange={(event) => setIl(event.target.value)} required>
              <option value="">İl seçin</option>
              {TURKIYE_KONUMLARI.map((item) => (
                <option key={item.il} value={item.il}>
                  {formatLocationName(item.il)}
                </option>
              ))}
            </select>
          </label>

          <label className="setup-field">
            <span>
              <MapPin size={17} />
              İlçe
            </span>
            <select value={ilce} onChange={(event) => setIlce(event.target.value)} disabled={!seciliIl} required>
              <option value="">İlçe seçin</option>
              {seciliIl?.ilceler.map((item) => (
                <option key={item} value={item}>
                  {formatLocationName(item)}
                </option>
              ))}
            </select>
          </label>

          {hesapTipi === "Muhasebeci" ? (
            <>
              <label className="setup-field">
                <span>Telefon numarası</span>
                <TelefonNumarasiInput
                  value={profilTelefon}
                  onChange={setProfilTelefon}
                  required
                />
              </label>
              <label className="setup-field">
                <span>Deneyim yılı</span>
                <input
                  type="number"
                  min={0}
                  value={profilDeneyimYili}
                  onChange={(event) => setProfilDeneyimYili(event.target.value)}
                  required
                />
              </label>
              <ProfilResmiYukleyici
                className="setup-field setup-field--wide"
                value={profilResmiUrl}
                onChange={setProfilResmiUrl}
                required
                disabled={kaydediliyor}
                onBusyChange={setProfilResmiYukleniyor}
                onError={setHata}
              />
              <label className="setup-field">
                <span>Ücret bilgisi</span>
                <input
                  value={profilUcretBilgisi}
                  onChange={(event) => setProfilUcretBilgisi(event.target.value)}
                  placeholder="Örn. Aylık 2500 TL'den başlar"
                  required
                />
              </label>
              <label className="setup-field">
                <span>
                  <BriefcaseBusiness size={17} />
                  Uzmanlıklar
                </span>
                <input
                  value={profilUzmanliklar}
                  onChange={(event) => setProfilUzmanliklar(event.target.value)}
                  placeholder="E-fatura, KOBİ, bordro"
                  required
                />
              </label>
              <label className="setup-field">
                <span>
                  <Building2 size={17} />
                  Müşteri tipi
                </span>
                <input
                  value={profilMusteriTipleri}
                  onChange={(event) => setProfilMusteriTipleri(event.target.value)}
                  placeholder="Kafe, perakende, hizmet"
                  required
                />
              </label>
              <label className="setup-field">
                <span>
                  <Sparkles size={17} />
                  Kısa açıklama
                </span>
                <input
                  value={profilKisaAciklama}
                  onChange={(event) => setProfilKisaAciklama(event.target.value)}
                  placeholder="Müşterilere sunulan ana hizmet"
                  required
                />
              </label>
            </>
          ) : null}
        </div>

        {hesapTipi === "Muhasebeci" ? (
          <MuhasebeciProfilOnizleme
            resimUrl={profilResmiUrl}
            unvan={isletmeAdi}
            konum={konum}
            deneyimYili={profilDeneyimYili}
            ucretBilgisi={profilUcretBilgisi}
            uzmanliklar={profilUzmanliklar}
            musteriTipleri={profilMusteriTipleri}
            kisaAciklama={profilKisaAciklama}
          />
        ) : null}

        {hesapTipi === "Isletme" ? (
          <section className="setup-accountant-choice" aria-label="Muhasebeci bağlantısı">
            <strong>Muhasebeciniz var mı?</strong>
            <div role="group" aria-label="Muhasebeciniz var mı?">
              <button type="button" className={muhasebeciVarMi ? "active" : ""} onClick={() => setMuhasebeciVarMi(true)}>
                Evet
              </button>
              <button type="button" className={!muhasebeciVarMi ? "active" : ""} onClick={() => setMuhasebeciVarMi(false)}>
                Hayır
              </button>
            </div>
          </section>
        ) : null}

        {hesapTipi === "Isletme" && seciliTur && (
          <section className="setup-preview">
            <div>
              <strong>Gelir kalemleri</strong>
              <div className="setup-chip-list">
                {seciliTur.gelirKalemleri.map((item) => <span key={item}>{item}</span>)}
              </div>
            </div>
            <div>
              <strong>Gider kalemleri</strong>
              <div className="setup-chip-list setup-chip-list--expense">
                {seciliTur.giderKalemleri.map((item) => <span key={item}>{item}</span>)}
              </div>
            </div>
          </section>
        )}

        {hata && <p className="setup-modal__error">{hata}</p>}

        <footer className="setup-modal__actions">
          <button type="button" className="setup-modal__secondary" onClick={onClose} disabled={kaydediliyor || profilResmiYukleniyor}>
            Daha sonra
          </button>
          <button type="submit" className="setup-modal__primary" disabled={kaydediliyor || profilResmiYukleniyor}>
            {profilResmiYukleniyor ? "Profil resmi yükleniyor..." : kaydediliyor ? "Kurulum hazırlanıyor..." : hesapTipi === "Muhasebeci" ? "Başvuruyu gönder" : "Kurulumu tamamla"}
          </button>
        </footer>
      </form>
    </div>
  );
}

function parseKonum(konum: string) {
  const parts = (konum || "")
    .split(/[/,-]/)
    .map((item) => item.trim())
    .filter(Boolean);

  return {
    il: parts[0] ?? "",
    ilce: parts[1] ?? ""
  };
}

function resolveIl(value: string) {
  if (!value) return "";
  const normalized = normalizeLocation(value);
  return TURKIYE_KONUMLARI.find((item) => normalizeLocation(item.il) === normalized)?.il ?? "";
}

function resolveInitialAccountType(value: string) {
  const intent = readAccountTypeIntent();
  if (intent) {
    return intent;
  }

  return value === "Muhasebeci" || value?.toLocaleLowerCase("tr-TR") === "muhasebeci"
    ? "Muhasebeci"
    : "Isletme";
}

function hasAccountTypeIntent(value: string) {
  return Boolean(readAccountTypeIntent()) || value === "Muhasebeci" || value?.toLocaleLowerCase("tr-TR") === "muhasebeci";
}

function readAccountTypeIntent(): "Isletme" | "Muhasebeci" | "" {
  const params = new URLSearchParams(window.location.search);
  const queryValue = params.get("hesapTipi") || params.get("accountType");
  const storedValue = window.localStorage.getItem(ACCOUNT_TYPE_INTENT_KEY);
  const candidates = [
    queryValue,
    storedValue
  ];

  return candidates.some((item) => item === "Muhasebeci" || item?.toLocaleLowerCase("tr-TR") === "muhasebeci")
    ? "Muhasebeci"
    : candidates.some((item) => item === "Isletme" || item?.toLocaleLowerCase("tr-TR") === "isletme")
      ? "Isletme"
      : "";
}

function resolveIlce(il: string, value: string) {
  if (!il || !value) return "";
  const normalized = normalizeLocation(value);
  const province = TURKIYE_KONUMLARI.find((item) => item.il === il);
  return province?.ilceler.find((item) => normalizeLocation(item) === normalized) ?? "";
}

function normalizeLocation(value: string) {
  return value
    .trim()
    .toLocaleUpperCase("tr-TR")
    .replace(/\s+/g, " ");
}

function formatLocationName(value: string) {
  return value
    .toLocaleLowerCase("tr-TR")
    .split(" ")
    .map((part) => part ? `${part[0].toLocaleUpperCase("tr-TR")}${part.slice(1)}` : part)
    .join(" ");
}
