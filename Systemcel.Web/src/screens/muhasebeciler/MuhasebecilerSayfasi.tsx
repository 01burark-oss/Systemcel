import React from "react";
import {
  ArrowRight,
  BriefcaseBusiness,
  Check,
  ChevronDown,
  Clock3,
  Copy,
  LayoutList,
  Loader2,
  MapPin,
  MessageCircle,
  RotateCcw,
  Search,
  Send,
  ShieldCheck,
  SlidersHorizontal,
  UsersRound,
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
  kisaAciklama: string;
  planAdi: string;
  pro: boolean;
  talepVar: boolean;
  bagli: boolean;
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

interface MuhasebeciSohbetMesaji {
  id: number;
  gonderenAdi: string;
  benimMesajim: boolean;
  mesaj: string;
  createdAt: string;
}

interface MuhasebeciSohbet {
  muhasebeciAdi: string;
  musteriAdi: string;
  durum: string;
  bilgiMesaji: string;
  mesajlar: MuhasebeciSohbetMesaji[];
}

interface MuhasebecilerSayfasiProps {
  publicMode?: boolean;
  ustBar?: UstBarDurumu | null;
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

export function MuhasebecilerSayfasi({ publicMode = false, ustBar, onUstBarYenile }: MuhasebecilerSayfasiProps) {
  const auth = useSystemcelAuth();
  const urlDavetKodu = React.useMemo(() => new URLSearchParams(window.location.search).get("davet") ?? "", []);
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
  const [davetKodu, setDavetKodu] = React.useState(urlDavetKodu);
  const [davetYetki, setDavetYetki] = React.useState<YetkiSeviyesi>("OkumaRapor");
  const [davetIslemde, setDavetIslemde] = React.useState(false);
  const [sohbetProfil, setSohbetProfil] = React.useState<MuhasebeciProfil | null>(null);
  const [sohbet, setSohbet] = React.useState<MuhasebeciSohbet | null>(null);
  const [sohbetMesaji, setSohbetMesaji] = React.useState("");
  const [sohbetIslemde, setSohbetIslemde] = React.useState(false);
  const [konumFiltresi, setKonumFiltresi] = React.useState("");
  const [uzmanlikFiltresi, setUzmanlikFiltresi] = React.useState("");
  const [musteriTipiFiltresi, setMusteriTipiFiltresi] = React.useState("");
  const [siralama, setSiralama] = React.useState("onerilen");
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

  const publicTalepHref = oturumAcik ? appMarketplaceHref(urlDavetKodu) : loginHref(urlDavetKodu);
  const publicHomeHref = oturumAcik ? "/app" : "/";
  const publicLoginHref = oturumAcik ? "/app" : "/giris";
  const publicTalepLabel = oturumAcik ? "Panelde pazaryerini aç" : "Giriş yap ve talep gönder";

  const publicTalepTikla = React.useCallback((event: React.MouseEvent<HTMLAnchorElement>) => {
    if (!publicMode)
      return;

    event.preventDefault();
    const target = oturumAcik ? appMarketplaceHref(urlDavetKodu) : loginHref(urlDavetKodu);
    window.location.assign(target);
  }, [oturumAcik, publicMode, urlDavetKodu]);

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
      setSohbetProfil(profil);
      await sohbetYukle(profil);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Talep gönderilemedi.");
    } finally {
      setTalepGonderiliyor(false);
    }
  }

  async function sohbetYukle(profil: MuhasebeciProfil) {
    setSohbetIslemde(true);
    setHata("");
    try {
      const data = await jsonOku<MuhasebeciSohbet>(`/api/ekran/muhasebeciler/${profil.muhasebeciIsletmeId}/sohbet`);
      setSohbet(data);
      onUstBarYenile?.();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Sohbet yüklenemedi.");
      setSohbet(null);
    } finally {
      setSohbetIslemde(false);
    }
  }

  async function sohbetAc(profil: MuhasebeciProfil) {
    setSohbetProfil(profil);
    setSohbetMesaji("");
    await sohbetYukle(profil);
  }

  async function sohbetMesajiGonder(event: React.FormEvent) {
    event.preventDefault();
    if (!sohbetProfil)
      return;

    setSohbetIslemde(true);
    setHata("");
    try {
      const data = await jsonOku<MuhasebeciSohbet>(`/api/ekran/muhasebeciler/${sohbetProfil.muhasebeciIsletmeId}/sohbet`, {
        method: "POST",
        body: JSON.stringify({ mesaj: sohbetMesaji })
      });
      setSohbet(data);
      setSohbetMesaji("");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Mesaj gönderilemedi.");
    } finally {
      setSohbetIslemde(false);
    }
  }

  async function davetKabulEt(event: React.FormEvent) {
    event.preventDefault();
    setDavetIslemde(true);
    setHata("");
    setMesaj("");
    try {
      const sonuc = await jsonOku<MuhasebeciTalep>("/api/ekran/muhasebeci/davetler/kabul", {
        method: "POST",
        body: JSON.stringify({
          davetKodu,
          yetkiSeviyesi: davetYetki
        })
      });
      setMesaj(`${sonuc.muhasebeciAdi} bağlantısı kabul edildi.`);
      setDavetKodu("");
      await onUstBarYenile?.();
      await yukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Davet kabul edilemedi.");
    } finally {
      setDavetIslemde(false);
    }
  }

  const profiller = veri?.profiller ?? [];
  const konumSecenekleri = React.useMemo(() => uniqueValues(profiller.map((profil) => profil.konum)), [profiller]);
  const uzmanlikSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.uzmanliklar))), [profiller]);
  const musteriTipiSecenekleri = React.useMemo(() => uniqueValues(profiller.flatMap((profil) => splitFilterValues(profil.musteriTipleri))), [profiller]);
  const gorunenProfiller = React.useMemo(() => {
    const filtered = profiller.filter((profil) => {
      return filterMatches(profil.konum, konumFiltresi) &&
        filterMatches(profil.uzmanliklar, uzmanlikFiltresi) &&
        filterMatches(profil.musteriTipleri, musteriTipiFiltresi);
    });

    return [...filtered].sort((a, b) => {
      if (siralama === "deneyim")
        return b.deneyimYili - a.deneyimYili || a.unvan.localeCompare(b.unvan, "tr");

      if (siralama === "ucret")
        return parsePrice(a.ucretBilgisi) - parsePrice(b.ucretBilgisi) || a.unvan.localeCompare(b.unvan, "tr");

      return Number(b.pro) - Number(a.pro) || a.unvan.localeCompare(b.unvan, "tr");
    });
  }, [konumFiltresi, musteriTipiFiltresi, profiller, siralama, uzmanlikFiltresi]);
  const filtreVar = Boolean(konumFiltresi || uzmanlikFiltresi || musteriTipiFiltresi || aktifArama);

  function filtreleriSifirla() {
    setArama("");
    setAktifArama("");
    setKonumFiltresi("");
    setUzmanlikFiltresi("");
    setMusteriTipiFiltresi("");
  }

  const content = (
    <main className={publicMode ? "accountant-marketplace accountant-marketplace--public" : "accountant-marketplace"}>
      <section className="accountant-marketplace__hero">
        <div>
          <span className="accountant-eyebrow">
            <BriefcaseBusiness size={16} />
            Muhasebeci pazaryeri
          </span>
          <h1>{publicMode ? "Systemcel muhasebecileri" : "Muhasebeciler"}</h1>
          <p>
            Yayındaki muhasebeci profillerini konum, uzmanlık ve müşteri tipiyle karşılaştırın.
          </p>
        </div>
        {publicMode ? (
          <a className="accountant-primary-link" href={publicTalepHref} onClick={publicTalepTikla}>
            <span>{publicTalepLabel}</span>
            <ArrowRight size={18} />
          </a>
        ) : null}
      </section>

      {publicMode && urlDavetKodu ? (
        <section className="accountant-invite-strip">
          <Copy size={18} />
          <div>
            <strong>Davet kodu hazır: {urlDavetKodu}</strong>
            <span>Bağlantıyı kabul etmek için giriş yaptıktan sonra kod otomatik doldurulur.</span>
          </div>
          <a href={publicTalepHref} onClick={publicTalepTikla}>{oturumAcik ? "Panelde aç" : "Giriş yap"}</a>
        </section>
      ) : null}

      {!publicMode && !saltOkunur ? (
        <section className="accountant-toolbar accountant-toolbar--invite">
          <form onSubmit={davetKabulEt}>
            <label>
              <span>Davet kodu</span>
              <input value={davetKodu} onChange={(event) => setDavetKodu(event.target.value)} placeholder="MUS-123456" required />
            </label>
            <YetkiSecimi value={davetYetki} onChange={setDavetYetki} />
            <button type="submit" disabled={davetIslemde}>
              {davetIslemde ? <Loader2 size={16} className="spin" /> : <Check size={16} />}
              <span>Davet kabul et</span>
            </button>
          </form>
        </section>
      ) : null}

      {false ? (
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
          <FilterSelect icon={<MapPin size={18} />} label="Konum" value={konumFiltresi} onChange={setKonumFiltresi} options={konumSecenekleri} />
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

      <section className="accountant-marketplace__body">
        <aside className="accountant-filter-panel" aria-label="Filtreler">
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
          <FilterField label="Konum" value={konumFiltresi} onChange={setKonumFiltresi} options={konumSecenekleri} placeholder="Konum seçin" />
          <FilterField label="Uzmanlık alanı" value={uzmanlikFiltresi} onChange={setUzmanlikFiltresi} options={uzmanlikSecenekleri} placeholder="Uzmanlık seçin" />
          <FilterField label="Müşteri tipi" value={musteriTipiFiltresi} onChange={setMusteriTipiFiltresi} options={musteriTipiSecenekleri} placeholder="Müşteri tipi seçin" />
          <button type="button" className="accountant-filter-clear" onClick={filtreleriSifirla} disabled={!filtreVar}>
            <RotateCcw size={16} />
            <span>Filtreleri sıfırla</span>
          </button>
          <button type="button" onClick={() => setAktifArama(arama)}>Filtreleri uygula</button>
        </aside>

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
                  <option value="onerilen">Önerilen</option>
                  <option value="deneyim">Deneyim</option>
                  <option value="ucret">Ücret</option>
                </select>
                <ChevronDown size={16} />
              </label>
              <button type="button" aria-label="Liste görünümü">
                <LayoutList size={18} />
              </button>
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
                    <span className="accountant-card__icon">
                      {profil.profilResmiUrl ? (
                        <img src={profil.profilResmiUrl} alt="" />
                      ) : (
                        <BriefcaseBusiness size={20} />
                      )}
                    </span>
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
              <span className="accountant-card__icon">
                {detayProfil.profilResmiUrl ? (
                  <img src={detayProfil.profilResmiUrl} alt="" />
                ) : (
                  <BriefcaseBusiness size={20} />
                )}
              </span>
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
              <a className="accountant-modal__primary accountant-profile-detail__request-link" href={oturumAcik ? appMarketplaceHref(urlDavetKodu, detayProfil.muhasebeciIsletmeId) : loginHref(urlDavetKodu, detayProfil.muhasebeciIsletmeId)}>
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

      {sohbetProfil && !saltOkunur ? (
        <div className="accountant-modal" role="dialog" aria-modal="true" aria-labelledby="accountant-chat-title">
          <form className="accountant-modal__panel accountant-chat" onSubmit={sohbetMesajiGonder}>
            <button
              type="button"
              className="accountant-modal__close"
              onClick={() => {
                setSohbetProfil(null);
                setSohbet(null);
                setSohbetMesaji("");
              }}
              aria-label="Kapat"
            >
              <X size={18} />
            </button>
            <header>
              <span className="accountant-card__icon">
                <MessageCircle size={20} />
              </span>
              <div>
                <p>Uygulama içi sohbet</p>
                <h2 id="accountant-chat-title">{sohbet?.muhasebeciAdi || sohbetProfil.unvan}</h2>
              </div>
            </header>
            <p className="accountant-chat__notice">
              {sohbet?.bilgiMesaji || "Telefon, e-posta ve web adresi paylaşmadan Systemcel üzerinden konuşun."}
            </p>
            <div className="accountant-chat__messages">
              {sohbetIslemde && !sohbet ? (
                <span className="accountant-chat__empty">Sohbet yükleniyor...</span>
              ) : sohbet?.mesajlar.length ? (
                sohbet.mesajlar.map((item) => (
                  <article key={item.id} className={item.benimMesajim ? "mine" : ""}>
                    <small>{item.gonderenAdi} · {saatBic(item.createdAt)}</small>
                    <p>{item.mesaj}</p>
                  </article>
                ))
              ) : (
                <span className="accountant-chat__empty">Henüz mesaj yok. İlk mesajı Systemcel içinde gönderin.</span>
              )}
            </div>
            <label className="accountant-modal__field">
              <span>Mesaj</span>
              <textarea
                value={sohbetMesaji}
                onChange={(event) => setSohbetMesaji(event.target.value)}
                rows={3}
                placeholder="Bir mesaj yazın..."
              />
            </label>
            <button type="submit" className="accountant-modal__primary" disabled={sohbetIslemde || sohbetMesaji.trim().length === 0}>
              {sohbetIslemde ? <Loader2 size={16} className="spin" /> : <Send size={16} />}
              <span>Mesaj gönder</span>
            </button>
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

function YetkiSecimi({ value, onChange }: { value: YetkiSeviyesi; onChange: (value: YetkiSeviyesi) => void }) {
  return (
    <div className="accountant-permission" role="group" aria-label="Yetki seviyesi">
      <button type="button" className={value === "OkumaRapor" ? "active" : ""} onClick={() => onChange("OkumaRapor")}>
        <ShieldCheck size={16} />
        <span>Okuma + rapor</span>
      </button>
      <button type="button" className={value === "TamIslem" ? "active" : ""} onClick={() => onChange("TamIslem")}>
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

function saatBic(value: string) {
  if (!value)
    return "-";

  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function appMarketplaceHref(davetKodu: string, muhasebeciIsletmeId?: number) {
  const params = new URLSearchParams();
  if (davetKodu)
    params.set("davet", davetKodu);
  if (muhasebeciIsletmeId) {
    params.set("muhasebeciId", String(muhasebeciIsletmeId));
    params.set("talep", "1");
  }

  const query = params.toString();
  return query ? `/app/muhasebeciler?${query}` : "/app/muhasebeciler";
}

function loginHref(davetKodu: string, muhasebeciIsletmeId?: number) {
  const params = new URLSearchParams({
    returnUrl: appMarketplaceHref(davetKodu, muhasebeciIsletmeId),
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
