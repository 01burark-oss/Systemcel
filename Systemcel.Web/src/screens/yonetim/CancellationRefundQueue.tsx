import React from "react";
import { Check, CircleDollarSign, Loader2, RefreshCw, Search, Send } from "lucide-react";
import { jsonOku } from "../../shared/json";
import "./CancellationRefundQueue.css";

interface CancellationRefundRequest {
  abonelikId: number;
  isletmeAdi: string;
  hesapTipi: string;
  planKodu: string;
  iptalAt: string | null;
  donemBitisAt: string | null;
  kalanAySayisi: number;
  tutar: number | null;
  paraBirimi: string;
  durum: string;
  onaylanabilir: boolean;
  gonderilebilir: boolean;
  sorgulanabilir: boolean;
  onayAt: string | null;
}

interface CancellationRefundQueueData {
  testIslemleriAcik: boolean;
  talepler: CancellationRefundRequest[];
}

const endpoint = "/api/ekran/yonetim/abonelik-iadeleri";
const statusLabels: Record<string, string> = {
  OnayBekliyor: "Onay bekliyor",
  IncelemeGerekli: "İnceleme gerekli",
  Hazir: "Gönderime hazır",
  Gonderiliyor: "Gönderiliyor",
  SonucBekliyor: "Sağlayıcı sonucu bekleniyor",
  Tamamlandi: "Tamamlandı",
  KesinBasarisiz: "Kesin başarısız"
};

export function CancellationRefundQueue() {
  const [data, setData] = React.useState<CancellationRefundQueueData | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [busyId, setBusyId] = React.useState<number | null>(null);
  const [confirmId, setConfirmId] = React.useState<number | null>(null);
  const [error, setError] = React.useState("");
  const [notice, setNotice] = React.useState("");

  const load = React.useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await jsonOku<CancellationRefundQueueData>(endpoint);
      if (!result || !Array.isArray(result.talepler) || typeof result.testIslemleriAcik !== "boolean") {
        throw new Error("İade kayıtları yüklenemedi.");
      }
      setData(result);
    } catch (err) {
      setData(null);
      setError(err instanceof Error ? err.message : "Abonelik iade kuyruğu yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }, []);

  React.useEffect(() => { void load(); }, [load]);

  async function act(request: CancellationRefundRequest, action: "onayla" | "gonder" | "sorgula") {
    setBusyId(request.abonelikId);
    setError("");
    setNotice("");
    try {
      await jsonOku(`${endpoint}/${request.abonelikId}/${action}`, { method: "POST" });
      setConfirmId(null);
      await load();
      setNotice(action === "onayla" ? "İade talebi onaylandı." : action === "gonder" ? "İade kaydı güncellendi. Sonucu kontrol edin." : "Sağlayıcı iade durumu sorgulandı.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "İade işlemi tamamlanamadı.");
    } finally {
      setBusyId(null);
    }
  }

  return <section className="cancellation-refund" aria-labelledby="cancellation-refund-title" data-testid="cancellation-refund-queue">
    <header className="cancellation-refund__header">
      <div className="cancellation-refund__title">
        <span className="cancellation-refund__icon" aria-hidden="true"><CircleDollarSign size={19} /></span>
        <div><h2 id="cancellation-refund-title">Abonelik iptal iadeleri</h2><p>İadeleri onaylayın, test ortamında gönderin ve sonucu sağlayıcıdan doğrulayın.</p></div>
      </div>
      <button type="button" className="cancellation-refund__refresh" onClick={() => void load()} disabled={loading || busyId !== null} aria-label="İade kuyruğunu yenile">
        {loading ? <Loader2 size={16} className="spin" /> : <RefreshCw size={16} />}
      </button>
    </header>
    {error ? <p className="cancellation-refund__message cancellation-refund__message--error" role="alert">{error}</p> : null}
    {notice ? <p className="cancellation-refund__message cancellation-refund__message--success" role="status">{notice}</p> : null}
    {data && !data.testIslemleriAcik ? <p className="cancellation-refund__closed" role="note">Test iade işlemleri kapalı.</p> : null}
    {loading ? <div className="cancellation-refund__state" role="status"><Loader2 size={20} className="spin" />İade kuyruğu yükleniyor…</div> : null}
    {!loading && data?.talepler.length === 0 ? <div className="cancellation-refund__state">İncelenecek abonelik iadesi yok.</div> : null}
    {!loading && data && data.talepler.length > 0 ? <div className="cancellation-refund__list">
      {data.talepler.map((request) => {
        const busy = busyId === request.abonelikId;
        const unknownAmount = request.tutar === null;
        const statusLabel = statusLabels[request.durum] ?? "İnceleme gerekli";
        return <article className="cancellation-refund__item" key={request.abonelikId}>
          <div className="cancellation-refund__item-main">
            <div className="cancellation-refund__business"><strong>{request.isletmeAdi}</strong><span>{accountTypeLabel(request.hesapTipi)} · {subscriptionPlanLabel(request.planKodu)}</span></div>
            <span className={`cancellation-refund__status cancellation-refund__status--${request.durum}`} data-testid={`refund-status-${request.abonelikId}`}>{statusLabel}</span>
          </div>
          <dl className="cancellation-refund__details">
            <div><dt>İade tutarı</dt><dd>{unknownAmount ? "Tutar inceleniyor" : money(request.tutar, request.paraBirimi)}</dd></div>
            <div><dt>Kalan süre</dt><dd>{request.kalanAySayisi} ay</dd></div>
            <div><dt>İptal talebi</dt><dd>{formatDate(request.iptalAt)}</dd></div>
            <div><dt>Dönem bitişi</dt><dd>{formatDate(request.donemBitisAt)}</dd></div>
            <div><dt>İade onayı</dt><dd>{formatDate(request.onayAt)}</dd></div>
          </dl>
          {unknownAmount ? <p className="cancellation-refund__review">Tutar inceleniyor. Tutar kesinleşmeden onay veya gönderim yapılamaz.</p> : null}
          {confirmId === request.abonelikId && request.gonderilebilir && data.testIslemleriAcik ? <div className="cancellation-refund__confirm" role="group" aria-label={`${request.isletmeAdi} için test iadesi teyidi`}>
            <p><strong>Test ödeme iadesi</strong> — sağlayıcıya {money(request.tutar, request.paraBirimi)} tutarında test iade isteği gönderilecek.</p>
            <div><button type="button" className="cancellation-refund__button cancellation-refund__button--primary" disabled={busyId !== null || loading} onClick={() => void act(request, "gonder")}>{busy ? <Loader2 size={15} className="spin" /> : <Send size={15} />}Test iadesini gönder</button><button type="button" className="cancellation-refund__button" disabled={busyId !== null || loading} onClick={() => setConfirmId(null)}>Vazgeç</button></div>
          </div> : null}
          <div className="cancellation-refund__actions">
            {request.onaylanabilir && !unknownAmount ? <button type="button" className="cancellation-refund__button cancellation-refund__button--primary" disabled={busyId !== null || loading || !data.testIslemleriAcik} onClick={() => void act(request, "onayla")} aria-label={`${request.isletmeAdi} iadesini onayla`}>{busy ? <Loader2 size={15} className="spin" /> : <Check size={15} />}Onayla</button> : null}
            {request.gonderilebilir && !unknownAmount ? <button type="button" className="cancellation-refund__button cancellation-refund__button--primary" disabled={busyId !== null || loading || !data.testIslemleriAcik || confirmId === request.abonelikId} onClick={() => setConfirmId(request.abonelikId)} aria-label={`${request.isletmeAdi} test iadesini gönder`}>{busy ? <Loader2 size={15} className="spin" /> : <Send size={15} />}Gönder</button> : null}
            {request.sorgulanabilir ? <button type="button" className="cancellation-refund__button" disabled={busyId !== null || loading || !data.testIslemleriAcik} onClick={() => void act(request, "sorgula")} aria-label={`${request.isletmeAdi} iade durumunu sorgula`}>{busy ? <Loader2 size={15} className="spin" /> : <Search size={15} />}Durumu sorgula</button> : null}
          </div>
        </article>;
      })}
    </div> : null}
  </section>;
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" }) : "—";
}

function money(value: number | null, currency: string) {
  return value === null ? "Tutar inceleniyor" : new Intl.NumberFormat("tr-TR", { style: "currency", currency: currency || "TRY" }).format(value);
}

function accountTypeLabel(value: string) {
  return value === "Muhasebeci" || value === "MuhasebeOfisi" ? "Muhasebeci" : "İşletme";
}

function subscriptionPlanLabel(value: string) {
  return value.toLocaleLowerCase("tr-TR").includes("yillik") || value.toLocaleLowerCase("tr-TR").includes("annual") ? "Yıllık abonelik" : "Abonelik";
}
