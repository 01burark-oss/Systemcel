import React from "react";
import {
  AlertCircle,
  ArrowRight,
  CalendarClock,
  Check,
  CheckCircle2,
  CreditCard,
  FileCheck2,
  History,
  Loader2,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  X
} from "lucide-react";
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
  Abonelik: "Abonelik ödemesi",
  DenemeKartYetkilendirme: "Deneme kart doğrulaması",
  Iade: "İade",
  Yenileme: "Abonelik yenileme"
};

const planEtiketleri: Record<string, string> = {
  isletme_baslangic: "Başlangıç",
  isletme_buyume: "Büyüme",
  isletme_kurumsal: "Kurumsal",
  muhasebeci_standart: "Standart",
  muhasebeci_pro: "Pro"
};

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
  return durumEtiketleri[value] ?? value;
}

function durumTonu(value: string) {
  if (["Aktif", "Basarili", "DenemeYetkilendirildi"].includes(value)) return "success";
  if (["Basarisiz", "IptalEdildi"].includes(value)) return "danger";
  if (["CheckoutAcik", "Hazirlaniyor", "Tolerans"].includes(value)) return "warning";
  return "neutral";
}

function urlSecimi() {
  const params = new URLSearchParams(window.location.search);
  return {
    planKodu: params.get("plan") ?? "",
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
  const [ekMusteriKredisi, setEkMusteriKredisi] = React.useState(ilkSecim.ekMusteriKredisi);
  const [teklif, setTeklif] = React.useState<TeklifYaniti | null>(null);
  const [modal, setModal] = React.useState<Modal>(null);
  const [onaylandi, setOnaylandi] = React.useState(false);
  const [eposta, setEposta] = React.useState("");
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [teklifYukleniyor, setTeklifYukleniyor] = React.useState(false);
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [bildirim, setBildirim] = React.useState(() => {
    if (ilkSecim.odemeSonucu.includes("basarili")) return "Ödeme doğrulaması tamamlandı. Abonelik durumunuz güncellendi.";
    if (ilkSecim.odemeSonucu.includes("basarisiz")) return "Ödeme doğrulanamadı. Bilgilerinizi kontrol edip yeniden deneyin.";
    return "";
  });
  const idempotencyRef = React.useRef(yeniIdempotencyKey());
  const modalBaslikRef = React.useRef<HTMLHeadingElement | null>(null);
  const modalRef = React.useRef<HTMLElement | null>(null);
  const oncekiOdakRef = React.useRef<HTMLElement | null>(null);
  const islemdeRef = React.useRef(false);

  React.useEffect(() => {
    islemdeRef.current = islemde;
  }, [islemde]);

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
      .catch((error: Error) => setHata(error.message))
      .finally(() => setYukleniyor(false));
  }, [ilkSecim.planKodu, yukle]);

  React.useEffect(() => {
    if (modal !== "onay" || !planKodu) return;
    let gecerli = true;
    setTeklifYukleniyor(true);
    setTeklif(null);
    setHata("");
    const credits = planKodu === "muhasebeci_standart" ? ekMusteriKredisi : 0;
    jsonOku<TeklifYaniti>(`/api/abonelik/teklif?planKodu=${encodeURIComponent(planKodu)}&faturalamaDonemi=Aylik&ekMusteriKredisi=${credits}`)
      .then((yanit) => {
        if (gecerli) setTeklif(yanit);
      })
      .catch((error: Error) => {
        if (gecerli) setHata(error.message);
      })
      .finally(() => {
        if (gecerli) setTeklifYukleniyor(false);
      });
    return () => { gecerli = false; };
  }, [ekMusteriKredisi, modal, planKodu]);

  React.useEffect(() => {
    if (!modal) return;
    const oncekiOverflow = document.body.style.overflow;
    oncekiOdakRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    document.body.style.overflow = "hidden";
    window.requestAnimationFrame(() => modalBaslikRef.current?.focus());
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !islemdeRef.current) {
        setModal(null);
        return;
      }

      if (event.key !== "Tab" || !modalRef.current) return;
      const odaklanabilir = Array.from(modalRef.current.querySelectorAll<HTMLElement>(
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
    setModal(null);
    setOnaylandi(false);
    setTeklif(null);
  };

  const checkoutBaslat = async () => {
    if (!onaylandi || !teklif) return;
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<CheckoutYaniti>("/api/abonelik/checkout", {
        method: "POST",
        headers: { "Idempotency-Key": idempotencyRef.current },
        body: JSON.stringify({
          planKodu,
          faturalamaDonemi: "Aylik",
          ekMusteriKredisi: planKodu === "muhasebeci_standart" ? ekMusteriKredisi : 0,
          onaylandi: true,
          eposta: eposta.trim() || null,
          idempotencyKey: idempotencyRef.current
        })
      });
      window.location.assign(result.checkoutUrl);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Ödeme adımı başlatılamadı.");
      setIslemde(false);
    }
  };

  const iptalEt = async () => {
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<{ mesaj: string }>("/api/abonelik/iptal", { method: "POST" });
      setBildirim(result.mesaj);
      await yukle();
      setModal(null);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "İptal talebi kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  };

  const seciliPlan = planlar.find((plan) => plan.kod === planKodu);
  const bitisAt = ozet?.deneme?.bitisAt ?? ozet?.abonelik?.donemBitisAt ?? ozet?.sonrakiYenilemeAt;
  const planTutari = ozet?.haklar.donemTutari ?? 0;
  const denemeSonaErdi = ozet?.deneme?.durum === "SonaErdi" || ozet?.deneme?.durum === "IptalEdildi";
  const odemeDuzeltmeGerekli = ozet?.abonelik?.durum === "OdemeBasarisiz";

  const planModaliniAc = () => {
    idempotencyRef.current = yeniIdempotencyKey();
    setOnaylandi(false);
    setModal("onay");
  };

  return (
    <main className="billing-page">
      <header className="billing-heading">
        <div>
          <span className="billing-eyebrow"><Sparkles size={14} /> ÜYELİK VE ÖDEMELER</span>
          <h1>Aboneliğiniz, tek bakışta.</h1>
          <p>Planınızı, yenileme tarihinizi ve ödeme hareketlerinizi buradan yönetin.</p>
        </div>
        <button className="billing-button billing-button--secondary" type="button" disabled={yukleniyor} onClick={() => {
          setYukleniyor(true);
          yukle().catch((error: Error) => setHata(error.message)).finally(() => setYukleniyor(false));
        }}>
          <RefreshCw size={16} className={yukleniyor ? "spin" : ""} /> Yenile
        </button>
      </header>

      {bildirim ? (
        <div className={`billing-notice ${ilkSecim.odemeSonucu.includes("basarisiz") ? "billing-notice--danger" : "billing-notice--success"}`} role="status">
          {ilkSecim.odemeSonucu.includes("basarisiz") ? <AlertCircle size={19} /> : <CheckCircle2 size={19} />}
          <span>{bildirim}</span>
          <button type="button" onClick={() => setBildirim("")} aria-label="Bildirimi kapat"><X size={16} /></button>
        </div>
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
                <span className={`billing-status billing-status--${durumTonu(ozet.durum)}`}><i />{durumEtiketi(ozet.durum)}</span>
                <span>{ozet.hesapTipi === "Muhasebeci" ? "Muhasebeci hesabı" : "İşletme hesabı"}</span>
              </div>
              <div className="billing-plan-card__body">
                <div>
                  <small>MEVCUT PLAN</small>
                  <h2>{ozet.haklar.planAdi}</h2>
                  <p>{ozet.isletmeAdi}</p>
                </div>
                <div className="billing-price">
                  <strong>{paraBic(planTutari, ozet.haklar.paraBirimi)}</strong>
                  <span>/{ozet.haklar.faturalamaDonemi === "Yillik" ? "yıl" : "ay"} + KDV</span>
                </div>
              </div>
              <div className="billing-plan-card__actions">
                <button className="billing-button billing-button--primary" type="button" onClick={planModaliniAc}>
                  Planları görüntüle <ArrowRight size={16} />
                </button>
                {ozet.iptalEdilebilir ? (
                  <button className="billing-link-button" type="button" onClick={() => setModal("iptal")}>Aboneliği iptal et</button>
                ) : null}
              </div>
            </article>

            <div className="billing-facts">
              <article>
                <span className="billing-fact-icon"><CalendarClock size={20} /></span>
                <div><small>{ozet.donemSonundaIptal || denemeSonaErdi ? "ERİŞİM BİTİŞİ" : ozet.deneme ? "DENEME BİTİŞİ" : "SONRAKİ YENİLEME"}</small><strong>{tarihBic(bitisAt)}</strong></div>
                <p>{ozet.donemSonundaIptal ? "Bu tarihe kadar tüm haklarınız devam eder." : "Yenileme öncesinde bilgilendirileceksiniz."}</p>
              </article>
              <article>
                <span className="billing-fact-icon"><CreditCard size={20} /></span>
                <div><small>ÖDEME YÖNTEMİ</small><strong>{ozet.deneme?.odemeYontemiEklendi ? "Kart doğrulandı" : "Henüz eklenmedi"}</strong></div>
                <p>{ozet.deneme?.odemeYontemiEklendi ? "Deneme sonundaki tahsilat için hazır." : "Checkout sırasında güvenle ekleyebilirsiniz."}</p>
              </article>
              <article>
                <span className="billing-fact-icon"><ShieldCheck size={20} /></span>
                <div><small>PLAN HAKLARI</small><strong>{ozet.haklar.saltOkunur ? "Salt okunur" : "Kullanıma açık"}</strong></div>
                <p>{ozet.haklar.aiAktif ? (ozet.haklar.aiMesajLimiti ? `${ozet.haklar.aiMesajLimiti} AI mesajı` : "Sınırsız AI erişimi") : "Temel finans özellikleri"}</p>
              </article>
            </div>
          </section>

          {ozet.donemSonundaIptal ? (
            <section className="billing-cancelled" aria-label="Dönem sonu iptal durumu">
              <FileCheck2 size={22} />
              <div><strong>İptal talebiniz kaydedildi</strong><p>Aboneliğiniz {tarihBic(bitisAt)} tarihine kadar aktif; bu tarihten sonra yeni tahsilat yapılmayacak.</p></div>
            </section>
          ) : null}

          {denemeSonaErdi || odemeDuzeltmeGerekli ? (
            <section className="billing-expired" aria-label="Plan veya ödeme işlemi gerekli">
              <AlertCircle size={24} />
              <div>
                <strong>{denemeSonaErdi ? "Deneme süreniz sona erdi" : "Ödeme yönteminizi düzeltmeniz gerekiyor"}</strong>
                <p>{denemeSonaErdi ? "Verileriniz korunuyor; çalışma alanınız uygun plan seçilene kadar kısıtlıdır." : "Tolerans süresi sona erdi. Aboneliğinizi yeniden etkinleştirmek için ödeme adımını tamamlayın."}</p>
              </div>
              <div className="billing-expired__actions">
                <button className="billing-button billing-button--primary" type="button" onClick={planModaliniAc}>Plan seç</button>
                <button className="billing-button billing-button--secondary" type="button" onClick={planModaliniAc}>Ödeme yöntemi ekle</button>
              </div>
            </section>
          ) : null}

          <section className="billing-history-card">
            <header>
              <div><span className="billing-section-icon"><History size={20} /></span><div><h2>Ödeme geçmişi</h2><p>Son 20 abonelik ve kart doğrulama işlemi.</p></div></div>
              <span>{ozet.odemeler.length} işlem</span>
            </header>
            {ozet.odemeler.length === 0 ? (
              <div className="billing-empty"><CreditCard size={28} /><strong>Henüz ödeme hareketi yok</strong><p>Plan checkout'u başladığında işlem burada görünecek.</p></div>
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
                <p className="billing-modal__lead">
                  {teklif?.fiyat.trialDays === 0
                    ? "Deneme hakkınız daha önce kullanıldığı için seçtiğiniz plan bugün başlar. Tahsilat tutarı onay metninde açıkça gösterilir."
                    : "Kartınız bugün ücretlendirilmez. Deneme bitişi ve ilk tahsilat tutarı onay metninde açıkça gösterilir."}
                </p>

                <div className="billing-plan-fields">
                  <label><span>Plan</span><select value={planKodu} onChange={(event) => {
                    setPlanKodu(event.target.value);
                    if (event.target.value !== "muhasebeci_standart") setEkMusteriKredisi(0);
                    setOnaylandi(false);
                    idempotencyRef.current = yeniIdempotencyKey();
                  }}>{planlar.map((plan) => <option key={plan.kod} value={plan.kod}>{plan.ad}</option>)}</select></label>
                  <div className="billing-period-lock"><span>İlk faturalama dönemi</span><strong>Aylık</strong><small>Yıllık plana abonelik başladıktan sonra geçebilirsiniz.</small></div>
                  {planKodu === "muhasebeci_standart" ? <label><span>+1 müşteri kredisi</span><input type="number" min="0" max="10000" step="1" value={ekMusteriKredisi} onChange={(event) => {
                    setEkMusteriKredisi(Math.min(10000, Math.max(0, Number.parseInt(event.target.value || "0", 10) || 0)));
                    setOnaylandi(false);
                    idempotencyRef.current = yeniIdempotencyKey();
                  }} /><small>10 müşteri dahildir. Her kredi aboneliğinizle birlikte aylık yenilenir.</small></label> : null}
                </div>

                {teklifYukleniyor ? <div className="billing-quote-loading"><Loader2 className="spin" size={21} /> Teklif hazırlanıyor…</div> : teklif ? (
                  <>
                    <div className="billing-quote">
                      <div>
                        <small>{teklif.fiyat.trialDays > 0 ? `${teklif.fiyat.trialDays} GÜNLÜK DENEME` : "DOĞRUDAN ABONELİK"}</small>
                        <strong>Bugün {paraBic(teklif.fiyat.trialDays > 0 ? 0 : teklif.fiyat.totalAmount, teklif.fiyat.currency)}</strong>
                      </div>
                      <dl>
                        <div><dt>Plan bedeli</dt><dd>{paraBic(teklif.fiyat.netAmount, teklif.fiyat.currency)}</dd></div>
                        {teklif.fiyat.extraCustomerCredits > 0 ? <div><dt>Müşteri kapasitesi</dt><dd>{teklif.fiyat.includedCustomerCount + teklif.fiyat.extraCustomerCredits} müşteri</dd></div> : null}
                        <div><dt>KDV (%{Math.round(teklif.fiyat.vatRate)})</dt><dd>{paraBic(teklif.fiyat.vatAmount, teklif.fiyat.currency)}</dd></div>
                        <div><dt>{teklif.fiyat.trialDays > 0 ? "Deneme sonrası toplam" : "Bugünkü toplam"}</dt><dd>{paraBic(teklif.fiyat.totalAmount, teklif.fiyat.currency)}</dd></div>
                      </dl>
                    </div>
                    <label className="billing-email"><span>E-posta <small>(hesabınızda yoksa)</small></span><input type="email" autoComplete="email" value={eposta} onChange={(event) => setEposta(event.target.value)} placeholder="ornek@isletme.com" /></label>
                    <label className="billing-consent">
                      <input type="checkbox" checked={onaylandi} onChange={(event) => setOnaylandi(event.target.checked)} />
                      <span><i aria-hidden="true"><Check size={14} /></i><strong>{teklif.onayMetniSurumu}</strong>{teklif.onayMetni}</span>
                    </label>
                  </>
                ) : null}
                {hata ? <div className="billing-inline-error" role="alert"><AlertCircle size={17} />{hata}</div> : null}
                <div className="billing-modal__actions">
                  <button className="billing-button billing-button--secondary" type="button" onClick={modalKapat} disabled={islemde}>Daha sonra</button>
                  <button className="billing-button billing-button--primary" type="button" onClick={checkoutBaslat} disabled={!onaylandi || !teklif || islemde || teklifYukleniyor}>{islemde ? <Loader2 className="spin" size={17} /> : <ShieldCheck size={17} />} {teklif?.fiyat.trialDays === 0 ? "Öde ve aboneliği başlat" : "Kartı güvenle ekle"}</button>
                </div>
                {seciliPlan ? <p className="billing-modal__footnote">{seciliPlan.ad} · {teklif?.fiyat.trialDays === 0 ? "hemen başlayan aylık abonelik" : `${seciliPlan.denemeGunSayisi} gün deneme`} · İstediğiniz zaman dönem sonuna iptal</p> : null}
              </>
            )}
          </section>
        </div>
      ) : null}
    </main>
  );
}

function OdemeSatiri({ odeme }: { odeme: OdemeKaydi }) {
  return (
    <tr>
      <td><div className="billing-table__primary"><span><CreditCard size={17} /></span><div><strong>{islemEtiketleri[odeme.islemTipi] ?? odeme.islemTipi}</strong><small>#{odeme.id}</small></div></div></td>
      <td><strong>{planEtiketleri[odeme.planKodu] ?? odeme.planKodu.replaceAll("_", " ")}</strong><small>{odeme.faturalamaDonemi === "Yillik" ? "Yıllık" : "Aylık"}</small></td>
      <td><strong>{tarihBic(odeme.tamamlandiAt ?? odeme.createdAt)}</strong><small>{tarihBic(odeme.createdAt, true).split(" ").slice(-1)[0]}</small></td>
      <td><span className={`billing-status billing-status--${durumTonu(odeme.durum)}`}><i />{durumEtiketi(odeme.durum)}</span>{odeme.hataKodu ? <small>{odeme.hataKodu}</small> : null}</td>
      <td className="number"><strong>{paraBic(odeme.toplamTutar, odeme.paraBirimi)}</strong><small>{odeme.kdvTutar > 0 ? `${paraBic(odeme.kdvTutar, odeme.paraBirimi)} KDV` : "Kart doğrulama"}</small></td>
    </tr>
  );
}
