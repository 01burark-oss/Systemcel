import React from "react";
import {
  AlertCircle,
  ArrowDownUp,
  ArrowRight,
  CalendarClock,
  Check,
  CheckCircle2,
  ContactRound,
  CreditCard,
  FileCheck2,
  History,
  Loader2,
  MessageCircle,
  Package2,
  ReceiptText,
  ShieldCheck,
  UserRound,
  UsersRound,
  X
} from "lucide-react";
import { legalTexts } from "../../auth/legalTexts";
import { jsonOku } from "../../shared/json";
import type {
  AbonelikOzeti,
  CheckoutYaniti,
  OdemeKaydi,
  PublicPlan,
  TeklifYaniti
} from "./types";
import "./billing.css";

type Modal = "onay" | "iptal" | null;
type ResultTone = "success" | "danger" | "warning";

const durumEtiketleri: Record<string, string> = {
  Aktif: "Aktif",
  Basarili: "Başarılı",
  Basarisiz: "Başarısız",
  CheckoutAcik: "Ödeme bekleniyor",
  Deneme: "Deneme",
  DenemeYetkilendirildi: "Kart doğrulandı",
  Hazirlaniyor: "Hazırlanıyor",
  IadeEdildi: "İade edildi",
  IptalEdildi: "İptal edildi",
  SonaErdi: "Sona erdi",
  OdemeBasarisiz: "Ödeme gerekli",
  Tolerans: "Ödeme bekleniyor"
};

const islemEtiketleri: Record<string, string> = {
  Abonelik: "Plan ödemesi",
  DenemeKartYetkilendirme: "Kart doğrulama",
  Iade: "İade",
  Yenileme: "Plan yenileme"
};

const planEtiketleri: Record<string, string> = {
  isletme_baslangic: "Başlangıç",
  isletme_buyume: "Büyüme",
  isletme_kurumsal: "Kurumsal",
  muhasebeci_standart: "Standart",
  muhasebeci_pro: "Pro"
};

const abonelikSozlesmesi = legalTexts.tr.subscription;

function paraBic(tutar: number, paraBirimi = "TRY") {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: paraBirimi || "TRY",
    minimumFractionDigits: 2
  }).format(tutar);
}

function tarihBic(value: string | null | undefined, saat = false) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "long",
    year: "numeric",
    ...(saat ? { hour: "2-digit", minute: "2-digit" } : {})
  }).format(date);
}

function durumEtiketi(value: string) {
  return durumEtiketleri[value] ?? "İşleniyor";
}

function kullaniciHataMesaji(error: unknown, fallback: string) {
  const message = error instanceof Error ? error.message.trim() : "";
  if (!message) return fallback;
  if (/e-?posta/i.test(message)) return "Geçerli bir e-posta adresi girin.";
  if (/plan.*hesap tipi|hesap tipi.*plan/i.test(message)) return "Bu plan hesabınızla uyumlu değil.";
  if (/checkout|webhook|callback|provider|sağlayıcı|saglayici|idempotency/i.test(message)) return fallback;
  return message;
}

function limitMetni(limit: number | null, tekil: string, cogul = tekil) {
  if (limit === null) return `Sınırsız ${cogul}`;
  return `${limit} ${limit === 1 ? tekil : cogul}`;
}

function durumTonu(value: string) {
  if (["Aktif", "Basarili", "DenemeYetkilendirildi"].includes(value)) return "success";
  if (["Basarisiz", "IptalEdildi"].includes(value)) return "danger";
  if (["CheckoutAcik", "Hazirlaniyor", "Tolerans"].includes(value)) return "warning";
  return "neutral";
}

function urlSecimi() {
  const params = new URLSearchParams(window.location.search);
  const billing = params.get("billing");
  return {
    planKodu: params.get("plan") ?? "",
    faturalamaDonemi: billing === "Yillik" ? "Yillik" as const : "Aylik" as const,
    ekMusteriKredisi: Math.max(0, Number.parseInt(params.get("credits") ?? "0", 10) || 0),
    odemeSonucu: params.get("odeme") ?? ""
  };
}

function yeniIdempotencyKey() {
  return globalThis.crypto?.randomUUID?.() ?? `checkout-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function AbonelikSayfasi() {
  const ilkSecim = React.useMemo(urlSecimi, []);
  const [ozet, setOzet] = React.useState<AbonelikOzeti | null>(null);
  const [planlar, setPlanlar] = React.useState<PublicPlan[]>([]);
  const [planKodu, setPlanKodu] = React.useState(ilkSecim.planKodu);
  const [faturalamaDonemi, setFaturalamaDonemi] = React.useState<"Aylik" | "Yillik">(ilkSecim.faturalamaDonemi);
  const [ekMusteriKredisi, setEkMusteriKredisi] = React.useState(ilkSecim.ekMusteriKredisi);
  const [teklif, setTeklif] = React.useState<TeklifYaniti | null>(null);
  const [modal, setModal] = React.useState<Modal>(null);
  const [sozlesmeAcik, setSozlesmeAcik] = React.useState(false);
  const [onaylandi, setOnaylandi] = React.useState(false);
  const [eposta, setEposta] = React.useState("");
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [teklifYukleniyor, setTeklifYukleniyor] = React.useState(false);
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [odemeSonucu, setOdemeSonucu] = React.useState(ilkSecim.odemeSonucu);
  const idempotencyRef = React.useRef(yeniIdempotencyKey());
  const modalBaslikRef = React.useRef<HTMLHeadingElement | null>(null);
  const modalRef = React.useRef<HTMLElement | null>(null);
  const sozlesmeBaslikRef = React.useRef<HTMLHeadingElement | null>(null);
  const sozlesmeRef = React.useRef<HTMLElement | null>(null);
  const sozlesmeTetikRef = React.useRef<HTMLButtonElement | null>(null);
  const sozlesmeAcikRef = React.useRef(false);
  const oncekiOdakRef = React.useRef<HTMLElement | null>(null);
  const islemdeRef = React.useRef(false);
  const checkoutIslemdeRef = React.useRef(false);

  React.useEffect(() => {
    islemdeRef.current = islemde;
  }, [islemde]);

  React.useEffect(() => {
    sozlesmeAcikRef.current = sozlesmeAcik;
    if (sozlesmeAcik) window.requestAnimationFrame(() => sozlesmeBaslikRef.current?.focus());
  }, [sozlesmeAcik]);

  const yukle = React.useCallback(async () => {
    setHata("");
    const [ozetYaniti, planYaniti] = await Promise.all([
      jsonOku<AbonelikOzeti>("/api/abonelik/ozet"),
      jsonOku<PublicPlan[]>("/api/public/planlar")
    ]);
    const uygunPlanlar = planYaniti.filter((plan) => plan.hesapTipi === ozetYaniti.hesapTipi);
    setOzet(ozetYaniti);
    setPlanlar(uygunPlanlar);
    setPlanKodu((current) => uygunPlanlar.some((plan) => plan.kod === current)
      ? current
      : uygunPlanlar.find((plan) => plan.kod === ozetYaniti.haklar.planKodu)?.kod ?? uygunPlanlar[0]?.kod ?? "");
  }, []);

  React.useEffect(() => {
    setYukleniyor(true);
    yukle()
      .then(() => {
        if (ilkSecim.planKodu) setModal("onay");
      })
      .catch((error: Error) => setHata(kullaniciHataMesaji(error, "Abonelik bilgileri yüklenemedi.")))
      .finally(() => setYukleniyor(false));
  }, [ilkSecim.planKodu, yukle]);

  React.useEffect(() => {
    if (modal !== "onay" || !planKodu) return;
    let gecerli = true;
    setTeklifYukleniyor(true);
    setTeklif(null);
    setHata("");
    const credits = planKodu === "muhasebeci_standart" ? ekMusteriKredisi : 0;
    jsonOku<TeklifYaniti>(`/api/abonelik/teklif?planKodu=${encodeURIComponent(planKodu)}&faturalamaDonemi=${faturalamaDonemi}&ekMusteriKredisi=${credits}`)
      .then((yanit) => {
        if (gecerli) setTeklif(yanit);
      })
      .catch((error: Error) => {
        if (gecerli) setHata(kullaniciHataMesaji(error, "Plan bilgileri yüklenemedi."));
      })
      .finally(() => {
        if (gecerli) setTeklifYukleniyor(false);
      });
    return () => { gecerli = false; };
  }, [ekMusteriKredisi, faturalamaDonemi, modal, planKodu]);

  React.useEffect(() => {
    if (!modal) return;
    const oncekiOverflow = document.body.style.overflow;
    oncekiOdakRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    document.body.style.overflow = "hidden";
    window.requestAnimationFrame(() => modalBaslikRef.current?.focus());
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !islemdeRef.current) {
        if (sozlesmeAcikRef.current) {
          setSozlesmeAcik(false);
          window.requestAnimationFrame(() => sozlesmeTetikRef.current?.focus());
          return;
        }
        setModal(null);
        return;
      }

      const odakKapsami = sozlesmeAcikRef.current ? sozlesmeRef.current : modalRef.current;
      if (event.key !== "Tab" || !odakKapsami) return;
      const odaklanabilir = Array.from(odakKapsami.querySelectorAll<HTMLElement>(
        'button:not(:disabled), select:not(:disabled), input:not(:disabled), [tabindex]:not([tabindex="-1"])'
      )).filter((element) => element.offsetParent !== null);
      if (odaklanabilir.length === 0) return;
      const ilk = odaklanabilir[0];
      const son = odaklanabilir[odaklanabilir.length - 1];
      if (event.shiftKey && document.activeElement === ilk) {
        event.preventDefault();
        son.focus();
      } else if (!event.shiftKey && document.activeElement === son) {
        event.preventDefault();
        ilk.focus();
      }
    };
    window.addEventListener("keydown", escape);
    return () => {
      document.body.style.overflow = oncekiOverflow;
      window.removeEventListener("keydown", escape);
      oncekiOdakRef.current?.focus();
    };
  }, [modal]);

  const modalKapat = () => {
    if (islemde) return;
    setSozlesmeAcik(false);
    setModal(null);
    setOnaylandi(false);
    setTeklif(null);
  };

  const checkoutBaslat = async () => {
    if (!onaylandi || !teklif || checkoutIslemdeRef.current) return;
    checkoutIslemdeRef.current = true;
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<CheckoutYaniti>("/api/abonelik/checkout", {
        method: "POST",
        headers: { "Idempotency-Key": idempotencyRef.current },
        body: JSON.stringify({
          planKodu,
          faturalamaDonemi,
          ekMusteriKredisi: planKodu === "muhasebeci_standart" ? ekMusteriKredisi : 0,
          kampanyaKodu: teklif.kampanyaKodu || null,
          onaylandi: true,
          eposta: eposta.trim() || null,
          idempotencyKey: idempotencyRef.current
        })
      });
      window.location.assign(result.checkoutUrl);
    } catch (error) {
      setHata(kullaniciHataMesaji(error, "Ödeme işlemi başlatılamadı. Lütfen yeniden deneyin."));
      setIslemde(false);
      checkoutIslemdeRef.current = false;
    }
  };

  const iptalEt = async () => {
    try {
      setIslemde(true);
      setHata("");
      await jsonOku<{ mesaj: string }>("/api/abonelik/iptal", { method: "POST" });
      await yukle();
      setModal(null);
    } catch (error) {
      setHata(kullaniciHataMesaji(error, "İptal talebi kaydedilemedi."));
    } finally {
      setIslemde(false);
    }
  };

  const baslangicAt = ozet?.deneme?.baslangicAt ?? ozet?.abonelik?.donemBaslangicAt;
  const bitisAt = ozet?.deneme?.bitisAt ?? ozet?.abonelik?.donemBitisAt ?? ozet?.sonrakiYenilemeAt;
  const planTutari = ozet?.haklar.donemTutari ?? 0;
  const denemeSonaErdi = ozet?.deneme?.durum === "SonaErdi" || ozet?.deneme?.durum === "IptalEdildi";
  const odemeDuzeltmeGerekli = ozet?.abonelik?.durum === "OdemeBasarisiz";
  const resultView = resolvePaymentResult(odemeSonucu, ozet?.odemeler ?? []);
  const primaryCta = resolvePrimaryCta(ozet);
  const planHaklari = ozet ? [
    {
      icon: <MessageCircle size={20} />,
      text: ozet.haklar.aiAktif
        ? limitMetni(ozet.haklar.aiMesajLimiti, "AI mesajı")
        : "AI mesajı dahil değil"
    },
    { icon: <UserRound size={20} />, text: limitMetni(ozet.haklar.kullaniciLimiti, "kullanıcı") },
    ozet.hesapTipi === "Muhasebeci"
      ? { icon: <UsersRound size={20} />, text: limitMetni(ozet.haklar.musteriLimiti, "müşteri") }
      : { icon: <ReceiptText size={20} />, text: limitMetni(ozet.haklar.faturaLimiti, "fatura") },
    { icon: <ArrowDownUp size={20} />, text: limitMetni(ozet.haklar.gelirGiderIslemLimiti, "gelir-gider kaydı") },
    { icon: <ContactRound size={20} />, text: limitMetni(ozet.haklar.cariKartLimiti, "cari kart") },
    ozet.hesapTipi === "Muhasebeci"
      ? { icon: <ReceiptText size={20} />, text: limitMetni(ozet.haklar.faturaLimiti, "fatura") }
      : { icon: <Package2 size={20} />, text: limitMetni(ozet.haklar.urunHizmetLimiti, "ürün / hizmet") }
  ] : [];

  const planModaliniAc = () => {
    checkoutIslemdeRef.current = false;
    idempotencyRef.current = yeniIdempotencyKey();
    setOnaylandi(false);
    setModal("onay");
  };

  const sozlesmeKapat = () => {
    setSozlesmeAcik(false);
    window.requestAnimationFrame(() => sozlesmeTetikRef.current?.focus());
  };

  return (
    <main className="billing-page">
      {resultView ? (
        <section className={`billing-feedback billing-feedback--${resultView.tone}`} role={resultView.tone === "danger" ? "alert" : "status"} aria-label="Ödeme sonucu">
          <span>{resultView.tone === "success" ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}</span>
          <strong>{resultView.title}</strong>
          {resultView.retry ? <button className="billing-link-button" type="button" onClick={planModaliniAc}>Yeniden dene</button> : null}
          <button className="billing-result__close" type="button" onClick={() => {
            setOdemeSonucu("");
            const url = new URL(window.location.href);
            url.searchParams.delete("odeme");
            window.history.replaceState(null, "", `${url.pathname}${url.search}${url.hash}`);
          }} aria-label="Ödeme sonucunu kapat"><X size={16} /></button>
        </section>
      ) : null}

      {hata && !modal ? (
        <div className="billing-notice billing-notice--danger" role="alert"><AlertCircle size={19} /><span>{hata}</span></div>
      ) : null}

      {yukleniyor && !ozet ? (
        <section className="billing-loading" aria-live="polite"><Loader2 className="spin" size={24} /> Abonelik bilgileri yükleniyor…</section>
      ) : ozet ? (
        <>
          <section className="billing-summary" aria-label="Abonelik özeti">
            <article className="billing-plan-card">
              <div className="billing-plan-card__top">
                <small>MEVCUT PLAN</small>
                <span className={`billing-status billing-status--${durumTonu(ozet.durum)}`}><i />{durumEtiketi(ozet.durum)}</span>
              </div>
              <div className="billing-plan-card__body">
                <div>
                  <h2>{ozet.haklar.planAdi}</h2>
                  <p>{ozet.isletmeAdi}</p>
                </div>
                <div className="billing-price">
                  <strong>{paraBic(planTutari, ozet.haklar.paraBirimi)}</strong>
                  <span>/{ozet.haklar.faturalamaDonemi === "Yillik" ? "yıl" : "ay"} + KDV</span>
                  {ozet.abonelik?.kampanyaKodu ? <small>LANSMANA ÖZEL</small> : null}
                </div>
              </div>
              <div className="billing-plan-card__actions">
                <button className="billing-button billing-button--primary" type="button" onClick={planModaliniAc}>
                  {primaryCta} <ArrowRight size={16} />
                </button>
                {ozet.iptalEdilebilir ? (
                  <button className="billing-link-button" type="button" onClick={() => setModal("iptal")}>Aboneliği iptal et</button>
                ) : null}
              </div>
            </article>

            <article className="billing-period-card">
              <h2>Plan dönemi</h2>
              <div className="billing-period-row">
                <span className="billing-fact-icon"><CalendarClock size={20} /></span>
                <div><small>BAŞLANGIÇ</small><strong>{tarihBic(baslangicAt)}</strong></div>
              </div>
              <div className="billing-period-row">
                <span className="billing-fact-icon"><CalendarClock size={20} /></span>
                <div><small>BİTİŞ</small><strong>{tarihBic(bitisAt)}</strong></div>
              </div>
              <p>{ozet.donemSonundaIptal
                ? "Bu tarihe kadar plan haklarınızı kullanabilirsiniz."
                : ozet.abonelik?.kampanyaKodu && ozet.abonelik.indirimliDonemKalan > 0
                  ? `Lansman fiyatınız ${ozet.abonelik.indirimliDonemKalan} ay daha geçerli. Lansman bitiminde güncel liste fiyatı uygulanır.`
                  : ozet.abonelik?.kampanyaKodu
                    ? `Sonraki yenilemede geçerli liste fiyatı uygulanır. Bugünkü liste fiyatı ${paraBic(ozet.abonelik.yenilemeDonemTutari, ozet.abonelik.paraBirimi)} + KDV.`
                : ozet.deneme
                  ? "Deneme süreniz bu tarihte sona erer."
                  : "Planınız bu tarihte yenilenir."}</p>
              {ozet.donemSonundaIptal ? <span className="billing-period-status"><FileCheck2 size={16} /> İptal talebi alındı</span> : null}
            </article>
          </section>

          <section className="billing-rights-card" aria-label="Plan hakları">
            <header><h2>Plan hakları</h2>{ozet.haklar.saltOkunur ? <span>Salt okunur</span> : null}</header>
            <div className="billing-rights-grid">
              {planHaklari.map((hak, index) => (
                <div className="billing-right" key={`${hak.text}-${index}`}>
                  <span>{hak.icon}</span>
                  <strong>{hak.text}</strong>
                </div>
              ))}
            </div>
          </section>

          {denemeSonaErdi || odemeDuzeltmeGerekli ? (
            <section className="billing-expired" aria-label="Plan veya ödeme işlemi gerekli">
              <AlertCircle size={24} />
              <div>
                <strong>{denemeSonaErdi ? "Deneme süreniz sona erdi" : "Ödeme yönteminizi düzeltmeniz gerekiyor"}</strong>
                <p>{denemeSonaErdi ? "Plan seçene kadar verilerinizi görüntülemeye devam edebilirsiniz." : "Planınızı kullanmaya devam etmek için ödeme bilgilerinizi güncelleyin."}</p>
              </div>
              <div className="billing-expired__actions">
                <button className="billing-button billing-button--primary" type="button" onClick={planModaliniAc}>Plan seç</button>
                <button className="billing-button billing-button--secondary" type="button" onClick={planModaliniAc}>Ödeme yöntemi ekle</button>
              </div>
            </section>
          ) : null}

          <section className="billing-history-card">
            <header>
              <div><span className="billing-section-icon"><History size={20} /></span><div><h2>Ödeme geçmişi</h2></div></div>
              <span>{ozet.odemeler.length} işlem</span>
            </header>
            {ozet.odemeler.length === 0 ? (
              <div className="billing-empty"><CreditCard size={28} /><strong>Henüz ödeme yok</strong><p>Plan ödemeleriniz burada görünür.</p></div>
            ) : (
              <div className="billing-table-wrap">
                <table className="billing-table">
                  <thead><tr><th>İşlem</th><th>Plan</th><th>Tarih</th><th>Durum</th><th className="number">Tutar</th></tr></thead>
                  <tbody>{ozet.odemeler.map((odeme) => <OdemeSatiri key={odeme.id} odeme={odeme} />)}</tbody>
                </table>
              </div>
            )}
          </section>
        </>
      ) : null}

      {modal ? (
        <div className="billing-modal-backdrop" role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget) modalKapat();
        }}>
          <section ref={modalRef} className={`billing-modal ${modal === "iptal" ? "billing-modal--cancel" : ""}`} role="dialog" aria-modal="true" aria-labelledby="billing-modal-title">
            <button className="billing-modal__close" type="button" onClick={modalKapat} disabled={islemde} aria-label="Pencereyi kapat"><X size={19} /></button>
            {modal === "iptal" ? (
              <>
                <span className="billing-modal__icon billing-modal__icon--danger"><CalendarClock size={24} /></span>
                <p className="billing-modal__eyebrow">DÖNEM SONUNDA İPTAL</p>
                <h2 id="billing-modal-title" ref={modalBaslikRef} tabIndex={-1}>Aboneliği dönem sonunda bitir</h2>
                <p className="billing-modal__lead">Erişiminiz <strong>{tarihBic(bitisAt)}</strong> tarihine kadar kesintisiz devam eder. Bu tarihten sonra kartınızdan yeni tahsilat yapılmaz.</p>
                <div className="billing-cancel-list">
                  <span><Check size={16} /> Mevcut dönem haklarınız korunur</span>
                  <span><Check size={16} /> Verileriniz silinmez</span>
                  <span><Check size={16} /> Sonraki otomatik yenileme durdurulur</span>
                </div>
                {hata ? <div className="billing-inline-error" role="alert"><AlertCircle size={17} />{hata}</div> : null}
                <div className="billing-modal__actions">
                  <button className="billing-button billing-button--secondary" type="button" onClick={modalKapat} disabled={islemde}>Vazgeç</button>
                  <button className="billing-button billing-button--danger" type="button" onClick={iptalEt} disabled={islemde}>{islemde ? <Loader2 className="spin" size={17} /> : null} Dönem sonunda iptal et</button>
                </div>
              </>
            ) : (
              <>
                <span className="billing-modal__icon"><FileCheck2 size={24} /></span>
                <p className="billing-modal__eyebrow">AÇIK ONAY</p>
                <h2 id="billing-modal-title" ref={modalBaslikRef} tabIndex={-1}>Planınızı seçin ve koşulları onaylayın</h2>
                <p className="billing-modal__lead">Seçtiğiniz plan bugün başlar. Tutar ve yenileme koşulları ödeme öncesinde gösterilir.</p>

                <div className="billing-plan-fields">
                  <label><span>Plan</span><select value={planKodu} onChange={(event) => {
                    setPlanKodu(event.target.value);
                    if (event.target.value !== "muhasebeci_standart") setEkMusteriKredisi(0);
                    setOnaylandi(false);
                    idempotencyRef.current = yeniIdempotencyKey();
                  }}>{planlar.map((plan) => <option key={plan.kod} value={plan.kod}>{plan.ad}</option>)}</select></label>
                  <fieldset className="billing-period-choice">
                    <legend>Faturalama dönemi</legend>
                    <div>
                      <button type="button" className={faturalamaDonemi === "Aylik" ? "active" : ""} aria-pressed={faturalamaDonemi === "Aylik"} onClick={() => {
                        setFaturalamaDonemi("Aylik");
                        setOnaylandi(false);
                        idempotencyRef.current = yeniIdempotencyKey();
                      }}>Aylık</button>
                      <button type="button" className={faturalamaDonemi === "Yillik" ? "active" : ""} aria-pressed={faturalamaDonemi === "Yillik"} onClick={() => {
                        setFaturalamaDonemi("Yillik");
                        setOnaylandi(false);
                        idempotencyRef.current = yeniIdempotencyKey();
                      }}>Yıllık</button>
                    </div>
                  </fieldset>
                  {planKodu === "muhasebeci_standart" ? <label><span>+1 müşteri kredisi</span><input type="number" min="0" max="10000" step="1" value={ekMusteriKredisi} onChange={(event) => {
                    setEkMusteriKredisi(Math.min(10000, Math.max(0, Number.parseInt(event.target.value || "0", 10) || 0)));
                    setOnaylandi(false);
                    idempotencyRef.current = yeniIdempotencyKey();
                  }} /><small>10 müşteri dahildir. Her kredi aboneliğinizle birlikte aylık yenilenir.</small></label> : null}
                </div>

                {teklifYukleniyor ? <div className="billing-quote-loading"><Loader2 className="spin" size={21} /> Plan bilgileri hazırlanıyor…</div> : teklif ? (
                  <>
                    <div className="billing-quote">
                      <div>
                        <small>{teklif.fiyat.isFounderPrice ? "LANSMANA ÖZEL" : faturalamaDonemi === "Yillik" ? "YILLIK ABONELİK" : "AYLIK ABONELİK"}</small>
                        <strong>Bugün {paraBic(teklif.fiyat.totalAmount, teklif.fiyat.currency)}</strong>
                      </div>
                      <dl>
                        <div><dt>Plan bedeli</dt><dd>{paraBic(teklif.fiyat.netAmount, teklif.fiyat.currency)}</dd></div>
                        {teklif.fiyat.extraCustomerCredits > 0 ? <div><dt>Müşteri kapasitesi</dt><dd>{teklif.fiyat.includedCustomerCount + teklif.fiyat.extraCustomerCredits} müşteri</dd></div> : null}
                        <div><dt>KDV (%{Math.round(teklif.fiyat.vatRate)})</dt><dd>{paraBic(teklif.fiyat.vatAmount, teklif.fiyat.currency)}</dd></div>
                        {teklif.fiyat.isFounderPrice ? <div><dt>Bugünkü liste fiyatı</dt><dd>{paraBic(teklif.fiyat.renewalNetAmount, teklif.fiyat.currency)} + KDV</dd></div> : null}
                        <div><dt>Bugünkü toplam</dt><dd>{paraBic(teklif.fiyat.totalAmount, teklif.fiyat.currency)}</dd></div>
                      </dl>
                    </div>
                    <label className="billing-email"><span>E-posta <small>(hesabınızda yoksa)</small></span><input type="email" autoComplete="email" value={eposta} onChange={(event) => setEposta(event.target.value)} placeholder="ornek@isletme.com" /></label>
                    <div className="billing-consent">
                      <input id="billing-subscription-consent" type="checkbox" checked={onaylandi} onChange={(event) => setOnaylandi(event.target.checked)} aria-labelledby="billing-consent-copy" />
                      <label htmlFor="billing-subscription-consent" aria-label="Abonelik onayını seç"><i aria-hidden="true"><Check size={14} /></i></label>
                      <span id="billing-consent-copy" className="billing-consent__copy">
                        <button ref={sozlesmeTetikRef} className="billing-consent__link" type="button" onClick={() => setSozlesmeAcik(true)}>Abonelik sözleşmesini</button> okudum ve {faturalamaDonemi === "Yillik" ? "yıllık" : "aylık"} aboneliği onaylıyorum.
                      </span>
                    </div>
                  </>
                ) : null}
                {hata ? <div className="billing-inline-error" role="alert"><AlertCircle size={17} />{hata}</div> : null}
                <div className="billing-modal__actions">
                  <button className="billing-button billing-button--secondary" type="button" onClick={modalKapat} disabled={islemde}>Daha sonra</button>
                  <button className="billing-button billing-button--primary" type="button" onClick={checkoutBaslat} disabled={!onaylandi || !teklif || islemde || teklifYukleniyor}>{islemde ? <Loader2 className="spin" size={17} /> : <ShieldCheck size={17} />} Öde ve aboneliği başlat</button>
                </div>
              </>
            )}
          </section>
          {sozlesmeAcik ? (
            <div className="billing-contract-backdrop" role="presentation" onMouseDown={(event) => {
              if (event.target === event.currentTarget) sozlesmeKapat();
            }}>
              <section ref={sozlesmeRef} className="billing-contract-window" role="dialog" aria-modal="true" aria-labelledby="billing-contract-title">
                <button className="billing-contract-window__close" type="button" onClick={sozlesmeKapat} aria-label="Sözleşme penceresini kapat"><X size={18} /></button>
                <p className="billing-modal__eyebrow">ABONELİK SÖZLEŞMESİ</p>
                <h2 id="billing-contract-title" ref={sozlesmeBaslikRef} tabIndex={-1}>{abonelikSozlesmesi.title}</h2>
                <p className="billing-contract-window__updated">{abonelikSozlesmesi.updatedAtLabel}: {abonelikSozlesmesi.updatedAt}</p>
                <p className="billing-contract-window__intro">{abonelikSozlesmesi.intro}</p>
                <div className="billing-contract-window__sections">
                  {abonelikSozlesmesi.sections.map((section) => (
                    <section key={section.title}>
                      <h3>{section.title}</h3>
                      <p>{section.text}</p>
                    </section>
                  ))}
                </div>
                <p className="billing-contract-window__note">{abonelikSozlesmesi.note}</p>
                <div className="billing-contract-window__actions">
                  <button className="billing-button billing-button--primary" type="button" onClick={sozlesmeKapat}>Kapat</button>
                </div>
              </section>
            </div>
          ) : null}
        </div>
      ) : null}
    </main>
  );
}

function OdemeSatiri({ odeme }: { odeme: OdemeKaydi }) {
  return (
    <tr>
      <td><div className="billing-table__primary"><span><CreditCard size={17} /></span><div><strong>{islemEtiketleri[odeme.islemTipi] ?? "Ödeme"}</strong></div></div></td>
      <td><strong>{planEtiketleri[odeme.planKodu] ?? "Plan"}</strong><small>{odeme.faturalamaDonemi === "Yillik" ? "Yıllık" : "Aylık"}</small></td>
      <td><strong>{tarihBic(odeme.tamamlandiAt ?? odeme.createdAt)}</strong><small>{tarihBic(odeme.createdAt, true).split(" ").slice(-1)[0]}</small></td>
      <td><span className={`billing-status billing-status--${durumTonu(odeme.durum)}`}><i />{durumEtiketi(odeme.durum)}</span></td>
      <td className="number"><strong>{paraBic(odeme.toplamTutar, odeme.paraBirimi)}</strong><small>{odeme.kdvTutar > 0 ? `${paraBic(odeme.kdvTutar, odeme.paraBirimi)} KDV` : "Ücret alınmadı"}</small></td>
    </tr>
  );
}

export function resolvePrimaryCta(summary: AbonelikOzeti | null) {
  if (!summary) return "Planları görüntüle";
  if (summary.abonelik?.durum === "OdemeBasarisiz" || summary.abonelik?.durum === "Tolerans") return "Ödemeyi düzelt";
  if (summary.durum === "Deneme") return "Deneme planını yönet";
  if (summary.abonelik?.durum === "Aktif") return "Planı değiştir";
  if (summary.haklar.kaynak === "Ucretsiz") return "Ücretli plana geç";
  return "Planları görüntüle";
}

export function resolvePaymentResult(code: string, payments: OdemeKaydi[]): { tone: ResultTone; title: string; message: string; retry: boolean } | null {
  if (!code) return null;
  const normalized = code.toLocaleLowerCase("tr-TR");
  if (normalized.includes("iptal"))
    return { tone: "warning", title: "Ödeme adımı iptal edildi", message: "Planınız değişmedi.", retry: true };
  if (normalized.includes("basarisiz"))
    return { tone: "danger", title: "Ödeme tamamlanamadı", message: "Bilgilerinizi kontrol edip yeniden deneyin.", retry: true };

  const latest = payments[0];
  if (latest && ["Basarili", "DenemeYetkilendirildi"].includes(latest.durum))
    return { tone: "success", title: "Ödeme tamamlandı", message: "Planınız güncellendi.", retry: false };
  if (latest?.durum === "Basarisiz")
    return { tone: "danger", title: "Ödeme tamamlanamadı", message: "Bilgilerinizi kontrol edip yeniden deneyin.", retry: true };
  return { tone: "warning", title: "Ödeme kontrol ediliyor", message: "Sonuç kısa süre içinde güncellenecek.", retry: false };
}
