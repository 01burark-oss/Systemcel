import React from "react";
import {
  ArrowRight,
  BriefcaseBusiness,
  Check,
  ChevronDown,
  Clock3,
  Copy,
  Link2,
  Loader2,
  MapPin,
  MessageCircle,
  RotateCcw,
  Search,
  Send,
  ShieldCheck,
  SlidersHorizontal,
  UsersRound,
  UserRound,
  WalletCards,
  X
} from "lucide-react";
import { useSystemcelAuth } from "../../auth/SystemcelAuthProvider";
import systemcelIcon from "../../assets/systemcel-icon.png";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";

type YetkiSeviyesi = "OkumaRapor" | "TamIslem";

interface MuhasebeciProfil {
  muhasebeciIsletmeId: number;
  yayinda: boolean;
  unvan: string;
  konum: string;
  telefon: string;
  deneyimYili: number;
  profilResmiUrl: string;
  ucretBilgisi: string;
  uzmanliklar: string;
  musteriTipleri: string;
  sektorDeneyimleri: string;
  vergiMukellefiTipleri: string;
  uygunIsletmeOlcekleri: string;
  calismaSekilleri: string;
  kisaAciklama: string;
  planAdi: string;
  pro: boolean;
  talepVar: boolean;
  bagli: boolean;
  eslesmeNedenleri: string[];
}

interface MuhasebeciPazaryeri {
  mesaj: string;
  profiller: MuhasebeciProfil[];
}

interface MuhasebeciTalep {
  id: number;
  muhasebeciAdi: string;
  musteriAdi: string;
  durum: string;
  yetkiSeviyesi: YetkiSeviyesi;
}

interface MuhasebecilerSayfasiProps {
  mobileMode?: boolean;
  publicMode?: boolean;
  ustBar?: UstBarDurumu | null;
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

type MobilFiltre = "konum" | "uzmanlik" | "musteriTipi" | "eslesme";

function AccountantAvatar({ src, name }: { src: string; name: string }) {
  const [imageFailed, setImageFailed] = React.useState(false);

  React.useEffect(() => {
    setImageFailed(false);
  }, [src]);

  return (
    <span className="accountant-card__icon">
      {src && !imageFailed ? (
        <img
          src={src}
          alt={`${name} profil fotoğrafı`}
          onError={() => setImageFailed(true)}
        />
      ) : (
        <UserRound aria-hidden="true" size={24} strokeWidth={1.8} />
      )}
    </span>
  );
}

interface MuhasebeciLinkDaveti {
  musteriAdi: string;
  durum: string;
  yetkiSeviyesi: YetkiSeviyesi;
  mesaj: string;
  davetLinki: string;
  sonGecerlilikAt: string;
}

export function MuhasebecilerSayfasi({ mobileMode = false, publicMode = false, ustBar }: MuhasebecilerSayfasiProps) {
  const auth = useSystemcelAuth();
  const [arama, setArama] = React.useState("");
  const [aktifArama, setAktifArama] = React.useState("");
  const [veri, setVeri] = React.useState<MuhasebeciPazaryeri | null>(null);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [hata, setHata] = React.useState("");
  const [mesaj, setMesaj] = React.useState("");
  const [detayProfil, setDetayProfil] = React.useState<MuhasebeciProfil | null>(null);
  const [seciliProfil, setSeciliProfil] = React.useState<MuhasebeciProfil | null>(null);
  const [talepYetki, setTalepYetki] = React.useState<YetkiSeviyesi>("OkumaRapor");
  const [talepMesaji, setTalepMesaji] = React.useState("");
  const [talepGonderiliyor, setTalepGonderiliyor] = React.useState(false);
  const [linkDavetAcik, setLinkDavetAcik] = React.useState(false);
  const [linkDavetYetki, setLinkDavetYetki] = React.useState<YetkiSeviyesi>("OkumaRapor");
  const [linkDavetMesaji, setLinkDavetMesaji] = React.useState("");
  const [linkDavetIslemde, setLinkDavetIslemde] = React.useState(false);
  const [olusanDavet, setOlusanDavet] = React.useState<MuhasebeciLinkDaveti | null>(null);
  const [, setSohbetIslemde] = React.useState(false);
  const [sehirFiltresi, setSehirFiltresi] = React.useState("");
  const [ilceFiltresi, setIlceFiltresi] = React.useState("");
  const [uzmanlikFiltresi, setUzmanlikFiltresi] = React.useState("");
  const [musteriTipiFiltresi, setMusteriTipiFiltresi] = React.useState("");
  const [sektorFiltresi, setSektorFiltresi] = React.useState("");
  const [vergiMukellefiFiltresi, setVergiMukellefiFiltresi] = React.useState("");
  const [isletmeOlcegiFiltresi, setIsletmeOlcegiFiltresi] = React.useState("");
  const [calismaSekliFiltresi, setCalismaSekliFiltresi] = React.useState("");
  const [siralama, setSiralama] = React.useState("uygun");
  const [aktifMobilFiltre, setAktifMobilFiltre] = React.useState<MobilFiltre | null>(null);
  const urlHedefMuhasebeciId = React.useMemo(() => {
    const params = new URLSearchParams(window.location.search);
    const raw = params.get("muhasebeciId") ?? params.get("muhasebeci");
    const parsed = raw ? Number(raw) : 0;
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
  }, []);
  const urlTalepAc = React.useMemo(() => new URLSearchParams(window.location.search).get("talep") === "1", []);
  const urlTalepIslendi = React.useRef(false);
  const saltOkunur = !publicMode && ustBar?.hesapTipi === "Muhasebeci" && !ustBar.muhasebeciMusteriBaglami;
  const oturumAcik = !auth.clerkEnabled || (auth.isLoaded && auth.isSignedIn);

  const yukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      const endpoint = publicMode ? "/api/public/muhasebeciler" : "/api/ekran/muhasebeciler";
      const query = aktifArama.trim();
      const data = await jsonOku<MuhasebeciPazaryeri>(query ? `${endpoint}?arama=${encodeURIComponent(query)}` : endpoint);
      setVeri(data);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Muhasebeciler yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, [aktifArama, publicMode]);

  const publicTalepHref = oturumAcik ? appMarketplaceHref() : loginHref();
  const publicHomeHref = oturumAcik ? "/app" : "/";
  const publicLoginHref = oturumAcik ? "/app" : "/giris";
  const publicTalepLabel = oturumAcik ? "Panelde pazaryerini aç" : "Giriş yap ve talep gönder";

  const publicTalepTikla = React.useCallback((event: React.MouseEvent<HTMLAnchorElement>) => {
    if (!publicMode)
      return;

    event.preventDefault();
    const target = oturumAcik ? appMarketplaceHref() : loginHref();
    window.location.assign(target);
  }, [oturumAcik, publicMode]);

  React.useEffect(() => {
    document.title = publicMode ? "Systemcel Muhasebeciler" : "Muhasebeciler";
  }, [publicMode]);

  React.useEffect(() => {
    yukle().catch(() => undefined);
  }, [yukle]);

  React.useEffect(() => {
    if (publicMode || saltOkunur || !urlTalepAc || !urlHedefMuhasebeciId || urlTalepIslendi.current || yukleniyor || !veri)
      return;

    urlTalepIslendi.current = true;
    const profil = veri.profiller.find((item) => item.muhasebeciIsletmeId === urlHedefMuhasebeciId);
    temizleTalepYonlendirmesi();

    if (!profil) {
      setHata("Seçilen muhasebeci profili bulunamadı.");
      return;
    }

    if (profil.bagli || profil.talepVar) {
      sohbetAc(profil).catch(() => undefined);
      return;
    }

    setSeciliProfil(profil);
    setTalepYetki("OkumaRapor");
    setTalepMesaji("");
  }, [publicMode, saltOkunur, urlHedefMuhasebeciId, urlTalepAc, veri, yukleniyor]);

  async function talepGonder(event: React.FormEvent) {
    event.preventDefault();
    if (!seciliProfil || saltOkunur)
      return;

    setTalepGonderiliyor(true);
    setHata("");
    setMesaj("");
    try {
      const sonuc = await jsonOku<MuhasebeciTalep>(`/api/ekran/muhasebeciler/${seciliProfil.muhasebeciIsletmeId}/talep`, {
        method: "POST",
        body: JSON.stringify({
          yetkiSeviyesi: talepYetki,
          mesaj: talepMesaji
        })
      });
      setMesaj(`${sonuc.muhasebeciAdi} için talep gönderildi.`);
      const profil = seciliProfil;
      setSeciliProfil(null);
      setTalepMesaji("");
      await yukle();
      await sohbetAc(profil);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Talep gönderilemedi.");
    } finally {
      setTalepGonderiliyor(false);
    }
  }

  async function sohbetAc(profil: MuhasebeciProfil) {
    setSohbetIslemde(true);
    setHata("");
    try {
      const result = await jsonOku<{ sohbetId: number }>(`/api/ekran/sohbetler/muhasebeciler/${profil.muhasebeciIsletmeId}`);
      window.location.href = `/app/sohbetler?sohbetId=${result.sohbetId}`;
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Sohbet açılamadı.");
    } finally {
      setSohbetIslemde(false);
    }
  }

  async function linkDavetOlustur(event: React.FormEvent) {
    event.preventDefault();
    setLinkDavetIslemde(true);
    setHata("");
    setMesaj("");
    try {
      const sonuc = await jsonOku<MuhasebeciLinkDaveti>("/api/ekran/muhasebeci/link-davetleri", {
        method: "POST",
        body: JSON.stringify({
          yetkiSeviyesi: linkDavetYetki,
          mesaj: linkDavetMesaji
        })
      });
      setOlusanDavet(sonuc);
      setMesaj("Davet bağlantısı hazır.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Davet bağlantısı oluşturulamadı.");
    } finally {
      setLinkDavetIslemde(false);
    }
  }

  async function davetLinkiniKopyala() {
    if (!olusanDavet?.davetLinki)
      return;

    try {
      await navigator.clipboard.writeText(olusanDavet.davetLinki);
      setMesaj("Davet bağlantısı kopyalandı.");
    } catch {
      setHata("Bağlantı kopyalanamadı. Metni seçip kopyalayabilirsiniz.");
    }
  }

  function linkDavetiniAc() {
    setLinkDavetYetki("OkumaRapor");
    setLinkDavetMesaji("");
    setOlusanDavet(null);
    setHata("");
    setMesaj("");
    setLinkDavetAcik(true);
  }

  const profiller = React.useMemo(() => veri?.profiller ?? [], [veri?.profiller]);
  const gelismisFiltreAktif = import.meta.env.VITE_MARKETPLACE_FILTERS_ENABLED === "true";
  const profilKonumlari = React.useMemo(
    () => profiller.map((profil) => konumuAyir(profil.konum)),
    [profiller]
  );
  const sehirSecenekleri = React.useMemo(() =>
    uniqueValues(profilKonumlari.map((konum) => konum.sehir)),
  [profilKonumlari]);
  const ilceSecenekleri = React.useMemo(() => {
    if (!sehirFiltresi) return [];
    return uniqueValues(
      profilKonumlari
        .filter((konum) => normalizeFilterText(konum.sehir) === normalizeFilterText(sehirFiltresi))
        .map((konum) => konum.ilce)
    );
  }, [profilKonumlari, sehirFiltresi]);
  const uzmanlikSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.uzmanliklar))), [profiller]);
  const musteriTipiSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.musteriTipleri))), [profiller]);
  const sektorSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.sektorDeneyimleri))), [profiller]);
  const vergiMukellefiSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.vergiMukellefiTipleri))), [profiller]);
  const isletmeOlcegiSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.uygunIsletmeOlcekleri))), [profiller]);
  const calismaSekliSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.calismaSekilleri))), [profiller]);
  const gorunenProfiller = React.useMemo(() => {
    const filtered = profiller.filter((profil) => {
      return filterMatches(profil.konum, sehirFiltresi) &&
        filterMatches(profil.konum, ilceFiltresi) &&
        filterMatches(profil.uzmanliklar, uzmanlikFiltresi) &&
        filterMatches(profil.musteriTipleri, musteriTipiFiltresi) &&
        supportedValueMatches(profil.sektorDeneyimleri, sektorFiltresi) &&
        supportedValueMatches(profil.vergiMukellefiTipleri, vergiMukellefiFiltresi) &&
        supportedValueMatches(profil.uygunIsletmeOlcekleri, isletmeOlcegiFiltresi) &&
        supportedValueMatches(profil.calismaSekilleri, calismaSekliFiltresi);
    });

    return [...filtered].sort((a, b) => {
      if (siralama === "deneyim")
        return b.deneyimYili - a.deneyimYili || a.unvan.localeCompare(b.unvan, "tr");

      if (siralama === "ucret")
        return parsePrice(a.ucretBilgisi) - parsePrice(b.ucretBilgisi) || a.unvan.localeCompare(b.unvan, "tr");

      if (siralama === "uygun")
        return b.eslesmeNedenleri.length - a.eslesmeNedenleri.length || Number(b.pro) - Number(a.pro) || a.unvan.localeCompare(b.unvan, "tr");

      return Number(b.pro) - Number(a.pro) || a.unvan.localeCompare(b.unvan, "tr");
    });
  }, [calismaSekliFiltresi, ilceFiltresi, isletmeOlcegiFiltresi, musteriTipiFiltresi, profiller, sehirFiltresi, sektorFiltresi, siralama, uzmanlikFiltresi, vergiMukellefiFiltresi]);
  const filtreVar = Boolean(sehirFiltresi || ilceFiltresi || uzmanlikFiltresi || musteriTipiFiltresi || sektorFiltresi || vergiMukellefiFiltresi || isletmeOlcegiFiltresi || calismaSekliFiltresi || aktifArama);

  function filtreleriSifirla() {
    setArama("");
    setAktifArama("");
    setSehirFiltresi("");
    setIlceFiltresi("");
    setUzmanlikFiltresi("");
    setMusteriTipiFiltresi("");
    setSektorFiltresi("");
    setVergiMukellefiFiltresi("");
    setIsletmeOlcegiFiltresi("");
    setCalismaSekliFiltresi("");
  }

  function sehirDegistir(value: string) {
    setSehirFiltresi(value);
    setIlceFiltresi("");
  }

  const content = (
    <main className={publicMode ? "accountant-marketplace accountant-marketplace--public" : "accountant-marketplace"}>
      <section className="accountant-marketplace__hero">
        <div>
          <h1>{publicMode ? "Systemcel muhasebecileri" : "Muhasebeciler"}</h1>
        </div>
        {publicMode ? (
          <a className="accountant-primary-link" href={publicTalepHref} onClick={publicTalepTikla}>
            <span>{publicTalepLabel}</span>
            <ArrowRight size={18} />
          </a>
        ) : null}
      </section>

      {!publicMode && !saltOkunur ? (
        <section className="accountant-toolbar accountant-link-invite">
          <div>
            <span className="accountant-link-invite__icon"><Link2 size={19} /></span>
            <span>
              <strong>Muhasebeciniz listede yok mu?</strong>
              <small>Bağlantıyı paylaşın; yetki, daveti hazırlarken belirlenir.</small>
            </span>
          </div>
          <button type="button" onClick={linkDavetiniAc}>
            <Send size={16} />
            <span>Muhasebecini davet et</span>
          </button>
        </section>
      ) : null}

      {gelismisFiltreAktif ? (
      <section className="accountant-marketplace__search-panel">
        <form className="accountant-marketplace__search-row" onSubmit={(event) => { event.preventDefault(); setAktifArama(arama); }}>
          <label>
            <Search size={20} />
            <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Unvan, konum veya uzmanlık ara" />
          </label>
          <button type="submit">
            <Search size={18} />
            <span>Ara</span>
          </button>
        </form>
        <div className="accountant-marketplace__quick-filters" aria-label="Hızlı filtreler">
          <FilterSelect icon={<MapPin size={18} />} label="Şehir" value={sehirFiltresi} onChange={sehirDegistir} options={sehirSecenekleri} />
          <FilterSelect icon={<MapPin size={18} />} label="İlçe" value={ilceFiltresi} onChange={setIlceFiltresi} options={ilceSecenekleri} />
          <FilterSelect icon={<BriefcaseBusiness size={18} />} label="Uzmanlık" value={uzmanlikFiltresi} onChange={setUzmanlikFiltresi} options={uzmanlikSecenekleri} />
          <FilterSelect icon={<UsersRound size={18} />} label="Müşteri tipi" value={musteriTipiFiltresi} onChange={setMusteriTipiFiltresi} options={musteriTipiSecenekleri} />
          <button type="button" className="accountant-filter-reset" onClick={filtreleriSifirla} disabled={!filtreVar}>
            <RotateCcw size={17} />
            <span>Filtreleri sıfırla</span>
          </button>
        </div>
      </section>
      ) : null}

      {mesaj ? <p className="accountant-feedback accountant-feedback--success">{mesaj}</p> : null}
      {hata ? <p className="accountant-feedback accountant-feedback--error">{hata}</p> : null}

      {mobileMode || publicMode ? (
        <section className={`accountant-mobile-filters${publicMode && !mobileMode ? " accountant-mobile-filters--responsive" : ""}`} aria-label="Muhasebeci filtreleri">
          <form className="accountant-filter-search" onSubmit={(event) => { event.preventDefault(); setAktifArama(arama); }}>
            <label>
              <Search size={18} />
              <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Muhasebeci ara" />
            </label>
            <button type="submit">
              <Search size={16} />
              <span>Ara</span>
            </button>
          </form>
          <div className="accountant-mobile-filter-tabs">
            <MobileFilterTab active={aktifMobilFiltre === "konum"} icon={<MapPin size={18} />} label="Konum" onClick={() => setAktifMobilFiltre(aktifMobilFiltre === "konum" ? null : "konum")} />
            <MobileFilterTab active={aktifMobilFiltre === "uzmanlik"} icon={<BriefcaseBusiness size={18} />} label="Uzmanlık" onClick={() => setAktifMobilFiltre(aktifMobilFiltre === "uzmanlik" ? null : "uzmanlik")} />
            <MobileFilterTab active={aktifMobilFiltre === "musteriTipi"} icon={<UsersRound size={18} />} label="Müşteri tipi" onClick={() => setAktifMobilFiltre(aktifMobilFiltre === "musteriTipi" ? null : "musteriTipi")} />
            <MobileFilterTab active={aktifMobilFiltre === "eslesme"} icon={<ShieldCheck size={18} />} label="Uyum" onClick={() => setAktifMobilFiltre(aktifMobilFiltre === "eslesme" ? null : "eslesme")} />
          </div>
          {aktifMobilFiltre ? (
            <div className="accountant-mobile-filter-detail">
              {aktifMobilFiltre === "konum" ? (
                <>
                  <FilterField label="Şehir" value={sehirFiltresi} onChange={sehirDegistir} options={sehirSecenekleri} placeholder="Tüm şehirler" />
                  <FilterField label="İlçe" value={ilceFiltresi} onChange={setIlceFiltresi} options={ilceSecenekleri} placeholder={sehirFiltresi ? "Tüm ilçeler" : "Önce şehir seçin"} />
                </>
              ) : null}
              {aktifMobilFiltre === "uzmanlik" ? <FilterField label="Uzmanlık alanı" value={uzmanlikFiltresi} onChange={setUzmanlikFiltresi} options={uzmanlikSecenekleri} placeholder="Tüm uzmanlıklar" /> : null}
              {aktifMobilFiltre === "musteriTipi" ? <FilterField label="Müşteri tipi" value={musteriTipiFiltresi} onChange={setMusteriTipiFiltresi} options={musteriTipiSecenekleri} placeholder="Tüm müşteri tipleri" /> : null}
              {aktifMobilFiltre === "eslesme" ? <>
                <FilterField label="Sektör deneyimi" value={sektorFiltresi} onChange={setSektorFiltresi} options={sektorSecenekleri} placeholder="Tüm sektörler" />
                <FilterField label="Mükellef tipi" value={vergiMukellefiFiltresi} onChange={setVergiMukellefiFiltresi} options={vergiMukellefiSecenekleri} placeholder="Tüm tipler" />
                <FilterField label="İş yükü" value={isletmeOlcegiFiltresi} onChange={setIsletmeOlcegiFiltresi} options={isletmeOlcegiSecenekleri} placeholder="Tüm ölçekler" />
                <FilterField label="Çalışma biçimi" value={calismaSekliFiltresi} onChange={setCalismaSekliFiltresi} options={calismaSekliSecenekleri} placeholder="Tüm biçimler" />
              </> : null}
              <button type="button" className="accountant-filter-clear" onClick={filtreleriSifirla} disabled={!filtreVar}>
                <RotateCcw size={16} />
                <span>Filtreleri sıfırla</span>
              </button>
            </div>
          ) : null}
        </section>
      ) : null}

      <section className="accountant-marketplace__body">
        {!mobileMode ? <aside className="accountant-filter-panel" aria-label="Filtreler">
          <h2>
            <SlidersHorizontal size={18} />
            Filtreler
          </h2>
          <form className="accountant-filter-search" onSubmit={(event) => { event.preventDefault(); setAktifArama(arama); }}>
            <label>
              <Search size={18} />
              <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Muhasebeci ara" />
            </label>
            <button type="submit">
              <Search size={16} />
              <span>Ara</span>
            </button>
          </form>
          <FilterField label="Şehir" value={sehirFiltresi} onChange={sehirDegistir} options={sehirSecenekleri} placeholder="Şehir seçin" />
          <FilterField label="İlçe" value={ilceFiltresi} onChange={setIlceFiltresi} options={ilceSecenekleri} placeholder={sehirFiltresi ? "İlçe seçin" : "Önce şehir seçin"} />
          <FilterField label="Uzmanlık alanı" value={uzmanlikFiltresi} onChange={setUzmanlikFiltresi} options={uzmanlikSecenekleri} placeholder="Uzmanlık seçin" />
          <FilterField label="Müşteri tipi" value={musteriTipiFiltresi} onChange={setMusteriTipiFiltresi} options={musteriTipiSecenekleri} placeholder="Müşteri tipi seçin" />
          <FilterField label="Sektör deneyimi" value={sektorFiltresi} onChange={setSektorFiltresi} options={sektorSecenekleri} placeholder="Sektör seçin" />
          <FilterField label="Mükellef tipi" value={vergiMukellefiFiltresi} onChange={setVergiMukellefiFiltresi} options={vergiMukellefiSecenekleri} placeholder="Mükellef tipi seçin" />
          <FilterField label="İş yükü" value={isletmeOlcegiFiltresi} onChange={setIsletmeOlcegiFiltresi} options={isletmeOlcegiSecenekleri} placeholder="İş yükü seçin" />
          <FilterField label="Çalışma biçimi" value={calismaSekliFiltresi} onChange={setCalismaSekliFiltresi} options={calismaSekliSecenekleri} placeholder="Çalışma biçimi seçin" />
          <button type="button" className="accountant-filter-clear" onClick={filtreleriSifirla} disabled={!filtreVar}>
            <RotateCcw size={16} />
            <span>Filtreleri sıfırla</span>
          </button>
          <button type="button" onClick={() => setAktifArama(arama)}>Filtreleri uygula</button>
        </aside> : null}

        <section className="accountant-results" aria-label="Muhasebeci sonuçları">
          <header className="accountant-results__bar">
            <p>
              <UsersRound size={18} />
              <span><strong>{gorunenProfiller.length}</strong> muhasebeci bulundu</span>
            </p>
            <div>
              <label>
                <span>Sıralama:</span>
                <select value={siralama} onChange={(event) => setSiralama(event.target.value)}>
                  <option value="uygun">Sana en uygun</option>
                  <option value="onerilen">Önerilen</option>
                  <option value="deneyim">Deneyim</option>
                  <option value="ucret">Ücret</option>
                </select>
                <ChevronDown size={16} />
              </label>
            </div>
          </header>

          {yukleniyor ? (
            <div className="accountant-state">
              <Loader2 className="spin" size={22} />
              <span>Muhasebeciler yükleniyor...</span>
            </div>
          ) : gorunenProfiller.length === 0 ? (
            <div className="accountant-state">
              <Search size={22} />
              <span>Bu kritere uygun yayın profili bulunamadı.</span>
            </div>
          ) : (
            <section className="accountant-card-grid" aria-label="Muhasebeciler">
              {gorunenProfiller.map((profil) => (
                <article key={profil.muhasebeciIsletmeId} className="accountant-card">
                  <header>
                    <AccountantAvatar src={profil.profilResmiUrl} name={profil.unvan} />
                    <div>
                      <h2>{profil.unvan}</h2>
                    </div>
                    {profilDurumu(profil) ? <strong className="accountant-status">{profilDurumu(profil)}</strong> : null}
                  </header>
                  <div className="accountant-card__highlights">
                    <span>
                      <MapPin size={16} />
                      <span>
                        <small>Konum</small>
                        <strong>{profil.konum || "Konum belirtilmedi"}</strong>
                      </span>
                    </span>
                    <span>
                      <WalletCards size={16} />
                      <span>
                        <small>Ücret</small>
                        <strong>{profil.ucretBilgisi || "Ücret bilgisi belirtilmedi"}</strong>
                      </span>
                    </span>
                  </div>
                  <p className="accountant-card__summary">{profil.kisaAciklama}</p>
                  {profil.eslesmeNedenleri.length > 0 ? <div className="accountant-match-reasons" aria-label="Eşleşme nedenleri">
                    {profil.eslesmeNedenleri.slice(0, 3).map((neden) => <span key={neden}>{neden}</span>)}
                  </div> : null}
                  <div className="accountant-card__meta">
                    <span>
                      <Clock3 size={15} />
                      {profil.deneyimYili} yıl deneyim
                    </span>
                    <span>
                      <ShieldCheck size={15} />
                      {profil.uzmanliklar}
                    </span>
                    <span>
                      <BriefcaseBusiness size={15} />
                      {profil.musteriTipleri}
                    </span>
                  </div>
                  {publicMode ? (
                    <button type="button" className="accountant-card__action" onClick={() => setDetayProfil(profil)}>
                      <span>Detayları gör</span>
                      <ArrowRight size={18} />
                    </button>
                  ) : saltOkunur ? null : (
                    <button
                      type="button"
                      className="accountant-card__action"
                      onClick={() => {
                        if (profil.bagli || profil.talepVar) {
                          sohbetAc(profil).catch(() => undefined);
                          return;
                        }
                        setSeciliProfil(profil);
                        setTalepYetki("OkumaRapor");
                        setTalepMesaji("");
                      }}
                    >
                      {profil.bagli || profil.talepVar ? <MessageCircle size={16} /> : <Send size={16} />}
                      <span>{profil.bagli ? "Sohbet et" : profil.talepVar ? "Talep sohbeti" : "Talep gönder"}</span>
                    </button>
                  )}
                </article>
              ))}
            </section>
          )}
        </section>
      </section>

      {detayProfil ? (
        <div className="accountant-modal" role="dialog" aria-modal="true" aria-labelledby="accountant-detail-title">
          <article className="accountant-modal__panel accountant-profile-detail">
            <button type="button" className="accountant-modal__close" onClick={() => setDetayProfil(null)} aria-label="Kapat">
              <X size={18} />
            </button>
            <header>
              <AccountantAvatar src={detayProfil.profilResmiUrl} name={detayProfil.unvan} />
              <div>
                <p>Muhasebeci profili</p>
                <h2 id="accountant-detail-title">{detayProfil.unvan}</h2>
              </div>
            </header>
            <div className="accountant-card__highlights">
              <span>
                <MapPin size={16} />
                <span>
                  <small>Konum</small>
                  <strong>{detayProfil.konum || "Konum belirtilmedi"}</strong>
                </span>
              </span>
              <span>
                <WalletCards size={16} />
                <span>
                  <small>Ücret</small>
                  <strong>{detayProfil.ucretBilgisi || "Ücret bilgisi belirtilmedi"}</strong>
                </span>
              </span>
            </div>
            <p className="accountant-profile-detail__summary">{detayProfil.kisaAciklama}</p>
            <div className="accountant-card__meta">
              <span>
                <Clock3 size={15} />
                {detayProfil.deneyimYili} yıl deneyim
              </span>
              <span>
                <ShieldCheck size={15} />
                {detayProfil.uzmanliklar}
              </span>
              <span>
                <BriefcaseBusiness size={15} />
                {detayProfil.musteriTipleri}
              </span>
            </div>
            <p className="accountant-chat__notice accountant-profile-detail__policy">
              İletişim bilgisi paylaşmak yasaktır. Görüşmeler sadece Systemcel üzerinden yapılmalıdır. Aksi halde hesabınız askıya alınır ve ücret iadesi yapılmaz.
            </p>
            <div className="accountant-profile-detail__actions">
              <a className="accountant-modal__primary accountant-profile-detail__request-link" href={oturumAcik ? appMarketplaceHref(detayProfil.muhasebeciIsletmeId) : loginHref(detayProfil.muhasebeciIsletmeId)}>
                <Send size={16} />
                <span>{oturumAcik ? "Talep gönder" : "Giriş yap ve talep gönder"}</span>
              </a>
            </div>
          </article>
        </div>
      ) : null}

      {seciliProfil && !saltOkunur ? (
        <div className="accountant-modal" role="dialog" aria-modal="true" aria-labelledby="accountant-request-title">
          <form className="accountant-modal__panel" onSubmit={talepGonder}>
            <button type="button" className="accountant-modal__close" onClick={() => setSeciliProfil(null)} aria-label="Kapat">
              <X size={18} />
            </button>
            <header>
              <span className="accountant-card__icon">
                <Send size={20} />
              </span>
              <div>
                <p>Bağlantı talebi</p>
                <h2 id="accountant-request-title">{seciliProfil.unvan}</h2>
              </div>
            </header>
            <YetkiSecimi value={talepYetki} onChange={setTalepYetki} />
            <label className="accountant-modal__field">
              <span>Mesaj</span>
              <textarea value={talepMesaji} onChange={(event) => setTalepMesaji(event.target.value)} rows={4} placeholder="Kısa bir not ekleyin" />
            </label>
            <button type="submit" className="accountant-modal__primary" disabled={talepGonderiliyor}>
              {talepGonderiliyor ? <Loader2 size={16} className="spin" /> : <Send size={16} />}
              <span>Talep gönder</span>
            </button>
          </form>
        </div>
      ) : null}

      {linkDavetAcik && !publicMode && !saltOkunur ? (
        <div className="accountant-modal" role="dialog" aria-modal="true" aria-labelledby="accountant-link-invite-title">
          <form className="accountant-modal__panel accountant-link-invite-modal" onSubmit={linkDavetOlustur}>
            <button type="button" className="accountant-modal__close" onClick={() => setLinkDavetAcik(false)} aria-label="Kapat">
              <X size={18} />
            </button>
            <header>
              <span className="accountant-card__icon"><Link2 size={20} /></span>
              <div>
                <p>Bağlantı daveti</p>
                <h2 id="accountant-link-invite-title">Muhasebecini davet et</h2>
              </div>
            </header>
            {!olusanDavet ? (
              <>
                <div className="accountant-link-invite-modal__agreement">
                  <strong>Çalışma yetkisini belirleyin</strong>
                  <p>Muhasebeciniz daveti kabul ettiğinde seçtiğiniz yetkiyle işletmenize bağlanır.</p>
                </div>
                <YetkiSecimi value={linkDavetYetki} onChange={setLinkDavetYetki} />
                <label className="accountant-modal__field">
                  <span>Not (isteğe bağlı)</span>
                  <textarea value={linkDavetMesaji} onChange={(event) => setLinkDavetMesaji(event.target.value)} rows={3} placeholder="Muhasebecinize kısa bir not yazın" />
                </label>
                <button type="submit" className="accountant-modal__primary" disabled={linkDavetIslemde}>
                  {linkDavetIslemde ? <Loader2 size={16} className="spin" /> : <Link2 size={16} />}
                  <span>Davet bağlantısı oluştur</span>
                </button>
              </>
            ) : (
              <div className="accountant-link-invite-result">
                <div>
                  <Check size={18} />
                  <span>
                    <strong>Bağlantı hazır</strong>
                    <small>14 gün içinde muhasebecinizle paylaşın.</small>
                  </span>
                </div>
                <label>
                  <span>Davet bağlantısı</span>
                  <input value={olusanDavet.davetLinki} readOnly onFocus={(event) => event.currentTarget.select()} />
                </label>
                <button type="button" className="accountant-modal__primary" onClick={() => davetLinkiniKopyala().catch(() => undefined)}>
                  <Copy size={16} />
                  <span>Bağlantıyı kopyala</span>
                </button>
              </div>
            )}
          </form>
        </div>
      ) : null}

    </main>
  );

  if (!publicMode)
    return content;

  return (
    <div className="accountant-public-shell">
      <nav className="accountant-public-nav" aria-label="Systemcel">
        <a href={publicHomeHref} className="accountant-public-brand">
          <img src={systemcelIcon} alt="" />
          <span>SYSTEMCEL</span>
        </a>
        <div>
          <a href="/yardim">Yardım</a>
          <a href={publicLoginHref}>{oturumAcik ? "Panel" : "Giriş"}</a>
        </div>
      </nav>
      {content}
    </div>
  );
}

function FilterSelect({
  icon,
  label,
  value,
  onChange,
  options
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: string[];
}) {
  return (
    <label className="accountant-filter-chip">
      {icon}
      <select value={value} onChange={(event) => onChange(event.target.value)} aria-label={label}>
        <option value="">{label}</option>
        {options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
      <ChevronDown size={15} />
    </label>
  );
}

function FilterField({
  label,
  value,
  onChange,
  options,
  placeholder
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: string[];
  placeholder: string;
}) {
  return (
    <label className="accountant-filter-field">
      <span>{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
      <ChevronDown size={16} />
    </label>
  );
}

function MobileFilterTab({
  active,
  icon,
  label,
  onClick
}: {
  active: boolean;
  icon: React.ReactNode;
  label: string;
  onClick: () => void;
}) {
  return (
    <button type="button" className={active ? "active" : ""} onClick={onClick} aria-expanded={active}>
      {icon}
      <span>{label}</span>
    </button>
  );
}

function YetkiSecimi({ value, onChange }: { value: YetkiSeviyesi; onChange: (value: YetkiSeviyesi) => void }) {
  return (
    <div className="accountant-permission" role="group" aria-label="Yetki seviyesi">
      <button type="button" className={value === "OkumaRapor" ? "active" : ""} aria-pressed={value === "OkumaRapor"} onClick={() => onChange("OkumaRapor")}>
        <ShieldCheck size={16} />
        <span>Okuma + rapor</span>
      </button>
      <button type="button" className={value === "TamIslem" ? "active" : ""} aria-pressed={value === "TamIslem"} onClick={() => onChange("TamIslem")}>
        <Check size={16} />
        <span>Tam işlem</span>
      </button>
    </div>
  );
}

function profilDurumu(profil: MuhasebeciProfil) {
  if (profil.bagli)
    return "Bağlı";
  if (profil.talepVar)
    return "Talep var";
  return "";
}

function appMarketplaceHref(muhasebeciIsletmeId?: number) {
  const params = new URLSearchParams();
  if (muhasebeciIsletmeId) {
    params.set("muhasebeciId", String(muhasebeciIsletmeId));
    params.set("talep", "1");
  }

  const query = params.toString();
  return query ? `/app/muhasebeciler?${query}` : "/app/muhasebeciler";
}

function loginHref(muhasebeciIsletmeId?: number) {
  const params = new URLSearchParams({
    returnUrl: appMarketplaceHref(muhasebeciIsletmeId),
    hesapTipi: "Isletme"
  });
  return `/giris?${params.toString()}`;
}

function temizleTalepYonlendirmesi() {
  const params = new URLSearchParams(window.location.search);
  params.delete("muhasebeciId");
  params.delete("muhasebeci");
  params.delete("talep");
  const query = params.toString();
  window.history.replaceState(null, "", `${window.location.pathname}${query ? `?${query}` : ""}`);
}

function uniqueValues(values: string[]) {
  return [...new Set(values.map((value) => value.trim()).filter(Boolean))]
    .sort((a, b) => a.localeCompare(b, "tr"));
}

function konumEtiketi(value: string) {
  return value
    .toLocaleLowerCase("tr-TR")
    .replace(/(^|[\s/-])([\p{L}])/gu, (_, separator: string, letter: string) =>
      `${separator}${letter.toLocaleUpperCase("tr-TR")}`);
}

function konumuAyir(value: string) {
  const [sehir = "", ilce = ""] = value
    .split(/[/,-]/)
    .map((item) => item.trim())
    .filter(Boolean);

  return {
    sehir: konumEtiketi(sehir),
    ilce: konumEtiketi(ilce)
  };
}

function splitFilterValues(value: string) {
  return value
    .split(/[,;/]/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function filterMatches(value: string, filter: string) {
  if (!filter)
    return true;

  return normalizeFilterText(value).includes(normalizeFilterText(filter));
}

function supportedValueMatches(supportedValues: string, filter: string) {
  if (!filter)
    return true;

  const normalizedFilter = normalizeFilterText(filter);
  return splitFilterValues(supportedValues).some((value) => {
    const normalizedValue = normalizeFilterText(value);
    return normalizedValue === normalizedFilter ||
      normalizedValue === "tum" ||
      normalizedValue === "hepsi" ||
      normalizedValue === "all" ||
      normalizedValue.startsWith("tum ") ||
      normalizedValue.startsWith("hepsi ");
  });
}

function normalizeFilterText(value: string) {
  return value
    .trim()
    .toLocaleLowerCase("tr-TR")
    .replaceAll("ı", "i")
    .replaceAll("ş", "s")
    .replaceAll("ğ", "g")
    .replaceAll("ü", "u")
    .replaceAll("ö", "o")
    .replaceAll("ç", "c");
}

function parsePrice(value: string) {
  const match = value.replace(/\./g, "").replace(",", ".").match(/\d+(?:\.\d+)?/);
  return match ? Number(match[0]) : Number.MAX_SAFE_INTEGER;
}
