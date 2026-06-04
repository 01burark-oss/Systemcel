import React from "react";
import {
  Archive,
  ArrowDown,
  ChevronLeft,
  Download,
  FileUp,
  Loader2,
  MessageCircle,
  Paperclip,
  RefreshCw,
  Send,
  Share2,
  Signal,
  SignalZero,
  X
} from "lucide-react";
import * as signalR from "@microsoft/signalr";
import { getAuthToken } from "../../auth/authToken";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";

interface SohbetOzet {
  id: number;
  muhasebeciIsletmeId: number;
  musteriIsletmeId: number;
  talepId?: number | null;
  baglantiId?: number | null;
  baslik: string;
  konu: string;
  karsiTarafAdi: string;
  durum: string;
  sonMesaj: string;
  sonMesajAt?: string | null;
  okunmamisMesajSayisi: number;
  arsivlendi: boolean;
  hedefUrl: string;
}

interface SohbetListe {
  sohbetler: SohbetOzet[];
  okunmamisMesajSayisi: number;
}

interface SohbetEki {
  id: number;
  mesajId?: number | null;
  ekTipi: string;
  dosyaAdi: string;
  icerikTipi: string;
  boyut: number;
  veriTipi: string;
  baslik: string;
  ozetJson: string;
  indirUrl: string;
  createdAt: string;
}

interface SohbetMesaji {
  id: number;
  sohbetId: number;
  gonderenIsletmeId: number;
  gonderenAdi: string;
  benimMesajim: boolean;
  mesajTipi: string;
  clientMessageId: string;
  mesaj: string;
  durum: string;
  okunduAt?: string | null;
  createdAt: string;
  ekler: SohbetEki[];
}

interface MesajSayfasi {
  sohbetId: number;
  sohbet: SohbetOzet;
  mesajlar: SohbetMesaji[];
  hasMore: boolean;
  nextBeforeId?: number | null;
}

interface SohbetlerSayfasiProps {
  ustBar: UstBarDurumu | null;
  onUstBarYenile?: () => unknown | Promise<unknown>;
}

type RealtimeState = "connecting" | "connected" | "fallback";

const PAGE_SIZE = 50;

export function SohbetlerSayfasi({ ustBar, onUstBarYenile }: SohbetlerSayfasiProps) {
  const initialId = React.useMemo(() => {
    const raw = new URLSearchParams(window.location.search).get("sohbetId");
    const parsed = raw ? Number(raw) : 0;
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
  }, []);
  const [includeArchived, setIncludeArchived] = React.useState(false);
  const [liste, setListe] = React.useState<SohbetListe>({ sohbetler: [], okunmamisMesajSayisi: 0 });
  const [aktifSohbetId, setAktifSohbetId] = React.useState(initialId);
  const [aktifSohbet, setAktifSohbet] = React.useState<SohbetOzet | null>(null);
  const [mesajlar, setMesajlar] = React.useState<SohbetMesaji[]>([]);
  const [hasMore, setHasMore] = React.useState(false);
  const [nextBeforeId, setNextBeforeId] = React.useState<number | null>(null);
  const [listeYukleniyor, setListeYukleniyor] = React.useState(true);
  const [mesajYukleniyor, setMesajYukleniyor] = React.useState(false);
  const [eskiYukleniyor, setEskiYukleniyor] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [metin, setMetin] = React.useState("");
  const [konu, setKonu] = React.useState("");
  const [baglanti, setBaglanti] = React.useState<signalR.HubConnection | null>(null);
  const [realtime, setRealtime] = React.useState<RealtimeState>("connecting");
  const [karsiTarafYaziyor, setKarsiTarafYaziyor] = React.useState(false);
  const [asagiButonu, setAsagiButonu] = React.useState(false);
  const [yeniMesajSayisi, setYeniMesajSayisi] = React.useState(0);
  const [veriAraligi, setVeriAraligi] = React.useState("last30");
  const [seciliAy, setSeciliAy] = React.useState(() => new Date().toISOString().slice(0, 7));
  const [ozelBaslangic, setOzelBaslangic] = React.useState(() => new Date(Date.now() - 29 * 86400000).toISOString().slice(0, 10));
  const [ozelBitis, setOzelBitis] = React.useState(() => new Date().toISOString().slice(0, 10));
  const [veriIslemde, setVeriIslemde] = React.useState(false);
  const [dosyaIslemde, setDosyaIslemde] = React.useState(false);
  const messagesRef = React.useRef<HTMLDivElement | null>(null);
  const fileRef = React.useRef<HTMLInputElement | null>(null);
  const typingStopRef = React.useRef<number | null>(null);
  const selectedRef = React.useRef(aktifSohbetId);
  const messagesAtBottomRef = React.useRef(true);

  const viewerAccountantId = ustBar?.muhasebeciMusteriBaglami ? ustBar.muhasebeciIsletmeId : ustBar?.aktifIsletmeId;
  const viewerIsAccountant = Boolean(aktifSohbet && viewerAccountantId && aktifSohbet.muhasebeciIsletmeId === viewerAccountantId);
  const dataActionLabel = viewerIsAccountant ? "Veri iste" : "Veri paylaş";

  React.useEffect(() => {
    document.title = "Sohbetler";
  }, []);

  React.useEffect(() => {
    selectedRef.current = aktifSohbetId;
  }, [aktifSohbetId]);

  const listeYukle = React.useCallback(async () => {
    const data = await jsonOku<SohbetListe>(`/api/ekran/sohbetler?includeArchived=${includeArchived ? "true" : "false"}`);
    setListe(data);
    setListeYukleniyor(false);
    if (!aktifSohbetId && data.sohbetler.length > 0) {
      setAktifSohbetId(data.sohbetler[0].id);
    }
    if (aktifSohbetId) {
      const found = data.sohbetler.find((item) => item.id === aktifSohbetId);
      if (found) {
        setAktifSohbet(found);
        setKonu(found.konu || "");
      }
    }
    await onUstBarYenile?.();
  }, [aktifSohbetId, includeArchived, onUstBarYenile]);

  const mesajlariYukle = React.useCallback(async (sohbetId: number) => {
    if (!sohbetId)
      return;

    setMesajYukleniyor(true);
    setHata("");
    try {
      const data = await jsonOku<MesajSayfasi>(`/api/ekran/sohbetler/${sohbetId}/mesajlar?limit=${PAGE_SIZE}`);
      setAktifSohbet(data.sohbet);
      setKonu(data.sohbet?.konu || "");
      setMesajlar(data.mesajlar);
      setHasMore(data.hasMore);
      setNextBeforeId(data.nextBeforeId ?? null);
      setYeniMesajSayisi(0);
      requestAnimationFrame(() => scrollToBottom("instant"));
      await onUstBarYenile?.();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Sohbet yüklenemedi.");
    } finally {
      setMesajYukleniyor(false);
    }
  }, [onUstBarYenile]);

  React.useEffect(() => {
    setListeYukleniyor(true);
    listeYukle().catch((error: Error) => {
      setHata(error.message);
      setListeYukleniyor(false);
    });
  }, [listeYukle]);

  React.useEffect(() => {
    if (aktifSohbetId) {
      const url = new URL(window.location.href);
      url.searchParams.set("sohbetId", String(aktifSohbetId));
      window.history.replaceState(null, "", `${url.pathname}${url.search}`);
      mesajlariYukle(aktifSohbetId).catch(() => undefined);
    }
  }, [aktifSohbetId, mesajlariYukle]);

  React.useEffect(() => {
    let cancelled = false;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/muhasebeci-sohbet", {
        accessTokenFactory: async () => await getAuthToken() ?? ""
      })
      .withAutomaticReconnect()
      .build();

    connection.on("MessageCreated", (message: SohbetMesaji) => {
      listeYukle().catch(() => undefined);
      if (message.sohbetId !== selectedRef.current)
        return;

      setMesajlar((current) => {
        if (current.some((item) => item.id === message.id || (message.clientMessageId && item.clientMessageId === message.clientMessageId)))
          return current.map((item) => item.clientMessageId === message.clientMessageId ? message : item);
        return [...current, message];
      });

      if (messagesAtBottomRef.current || message.benimMesajim) {
        requestAnimationFrame(() => scrollToBottom("smooth"));
      } else {
        setYeniMesajSayisi((current) => current + 1);
        setAsagiButonu(true);
      }
    });
    connection.on("ConversationUpdated", () => {
      listeYukle().catch(() => undefined);
    });
    connection.on("MessageRead", () => {
      if (selectedRef.current)
        mesajlariYukle(selectedRef.current).catch(() => undefined);
    });
    connection.on("TypingStarted", ({ sohbetId }: { sohbetId: number }) => {
      if (sohbetId === selectedRef.current)
        setKarsiTarafYaziyor(true);
    });
    connection.on("TypingStopped", ({ sohbetId }: { sohbetId: number }) => {
      if (sohbetId === selectedRef.current)
        setKarsiTarafYaziyor(false);
    });
    connection.on("DataRequestUpdated", () => {
      if (selectedRef.current)
        mesajlariYukle(selectedRef.current).catch(() => undefined);
      listeYukle().catch(() => undefined);
    });
    connection.onreconnecting(() => setRealtime("fallback"));
    connection.onreconnected(() => {
      setRealtime("connected");
      if (selectedRef.current)
        connection.invoke("JoinConversation", selectedRef.current).catch(() => undefined);
    });
    connection.onclose(() => setRealtime("fallback"));

    setBaglanti(connection);
    setRealtime("connecting");
    connection.start()
      .then(() => {
        if (!cancelled)
          setRealtime("connected");
      })
      .catch(() => {
        if (!cancelled)
          setRealtime("fallback");
      });

    return () => {
      cancelled = true;
      connection.stop().catch(() => undefined);
    };
  }, [listeYukle, mesajlariYukle]);

  React.useEffect(() => {
    if (!baglanti || realtime !== "connected" || !aktifSohbetId)
      return;

    baglanti.invoke("JoinConversation", aktifSohbetId).catch(() => undefined);
    return () => {
      baglanti.invoke("LeaveConversation", aktifSohbetId).catch(() => undefined);
    };
  }, [aktifSohbetId, baglanti, realtime]);

  React.useEffect(() => {
    if (realtime === "connected")
      return undefined;

    const thread = window.setInterval(() => {
      if (document.visibilityState === "visible" && selectedRef.current)
        mesajlariYukle(selectedRef.current).catch(() => undefined);
    }, 5_000);
    const list = window.setInterval(() => {
      if (document.visibilityState === "visible")
        listeYukle().catch(() => undefined);
    }, 20_000);
    return () => {
      window.clearInterval(thread);
      window.clearInterval(list);
    };
  }, [listeYukle, mesajlariYukle, realtime]);

  async function eskiMesajlariYukle() {
    if (!aktifSohbetId || !hasMore || !nextBeforeId || eskiYukleniyor)
      return;

    const container = messagesRef.current;
    const previousHeight = container?.scrollHeight ?? 0;
    setEskiYukleniyor(true);
    try {
      const data = await jsonOku<MesajSayfasi>(`/api/ekran/sohbetler/${aktifSohbetId}/mesajlar?beforeId=${nextBeforeId}&limit=${PAGE_SIZE}`);
      setMesajlar((current) => [...data.mesajlar, ...current]);
      setHasMore(data.hasMore);
      setNextBeforeId(data.nextBeforeId ?? null);
      requestAnimationFrame(() => {
        if (!container)
          return;
        container.scrollTop = container.scrollHeight - previousHeight + container.scrollTop;
      });
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Eski mesajlar yüklenemedi.");
    } finally {
      setEskiYukleniyor(false);
    }
  }

  async function mesajGonder(event?: React.FormEvent) {
    event?.preventDefault();
    const text = metin.trim();
    if (!aktifSohbetId || !text)
      return;

    const clientMessageId = crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
    const optimistic: SohbetMesaji = {
      id: -Date.now(),
      sohbetId: aktifSohbetId,
      gonderenIsletmeId: ustBar?.aktifIsletmeId ?? 0,
      gonderenAdi: "Ben",
      benimMesajim: true,
      mesajTipi: "Metin",
      clientMessageId,
      mesaj: text,
      durum: "Gönderiliyor",
      createdAt: new Date().toISOString(),
      ekler: []
    };
    setMesajlar((current) => [...current, optimistic]);
    setMetin("");
    requestAnimationFrame(() => scrollToBottom("smooth"));
    try {
      const saved = await jsonOku<SohbetMesaji>(`/api/ekran/sohbetler/${aktifSohbetId}/mesajlar`, {
        method: "POST",
        body: JSON.stringify({ mesaj: text, clientMessageId })
      });
      setMesajlar((current) => current.map((item) => item.clientMessageId === clientMessageId ? saved : item));
      await listeYukle();
    } catch (error) {
      setMesajlar((current) => current.map((item) => item.clientMessageId === clientMessageId ? { ...item, durum: "Hata" } : item));
      setHata(error instanceof Error ? error.message : "Mesaj gönderilemedi.");
    }
  }

  async function konuKaydet() {
    if (!aktifSohbetId || !konu.trim())
      return;

    try {
      const updated = await jsonOku<SohbetOzet>(`/api/ekran/sohbetler/${aktifSohbetId}/konu`, {
        method: "PUT",
        body: JSON.stringify({ konu })
      });
      setAktifSohbet(updated);
      await listeYukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Konu güncellenemedi.");
    }
  }

  async function arsivle() {
    if (!aktifSohbetId || !aktifSohbet)
      return;

    try {
      await jsonOku<SohbetOzet>(`/api/ekran/sohbetler/${aktifSohbetId}/arsiv`, {
        method: "PUT",
        body: JSON.stringify({ arsivlendi: !aktifSohbet.arsivlendi })
      });
      await listeYukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Arşiv durumu değiştirilemedi.");
    }
  }

  async function dosyaSecildi(event: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []);
    if (!aktifSohbetId || files.length === 0)
      return;

    setDosyaIslemde(true);
    setHata("");
    try {
      const form = new FormData();
      files.slice(0, 5).forEach((file) => form.append("files", file));
      await jsonOku<SohbetEki[]>(`/api/ekran/sohbetler/${aktifSohbetId}/ekler`, {
        method: "POST",
        body: form
      });
      await mesajlariYukle(aktifSohbetId);
      await listeYukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Dosya yüklenemedi.");
    } finally {
      setDosyaIslemde(false);
      if (fileRef.current)
        fileRef.current.value = "";
    }
  }

  async function veriAksiyonu() {
    if (!aktifSohbetId)
      return;

    setVeriIslemde(true);
    setHata("");
    try {
      const payload = {
        veriTipi: "GelirGiderOzeti",
        aralikKodu: veriAraligi,
        baslangic: veriAraligi === "selectedMonth" ? seciliAy : veriAraligi === "custom" ? ozelBaslangic : "",
        bitis: veriAraligi === "custom" ? ozelBitis : "",
        mesaj: ""
      };
      const endpoint = viewerIsAccountant ? "veri-istekleri" : "veri-paylasimlari";
      await jsonOku(`/api/ekran/sohbetler/${aktifSohbetId}/${endpoint}`, {
        method: "POST",
        body: JSON.stringify(payload)
      });
      await mesajlariYukle(aktifSohbetId);
      await listeYukle();
    } catch (error) {
      setHata(error instanceof Error ? error.message : `${dataActionLabel} tamamlanamadı.`);
    } finally {
      setVeriIslemde(false);
    }
  }

  function metinDegisti(value: string) {
    setMetin(value);
    if (!baglanti || realtime !== "connected" || !aktifSohbetId)
      return;

    baglanti.invoke("TypingStarted", aktifSohbetId).catch(() => undefined);
    if (typingStopRef.current)
      window.clearTimeout(typingStopRef.current);
    typingStopRef.current = window.setTimeout(() => {
      baglanti.invoke("TypingStopped", aktifSohbetId).catch(() => undefined);
    }, 3_000);
  }

  function scrollKontrol() {
    const el = messagesRef.current;
    if (!el)
      return;

    if (el.scrollTop < 80)
      eskiMesajlariYukle().catch(() => undefined);

    const distance = el.scrollHeight - el.scrollTop - el.clientHeight;
    messagesAtBottomRef.current = distance < 96;
    setAsagiButonu(distance > 180);
    if (distance < 96)
      setYeniMesajSayisi(0);
  }

  function scrollToBottom(mode: ScrollBehavior | "instant") {
    const el = messagesRef.current;
    if (!el)
      return;

    if (mode === "instant") {
      el.scrollTop = el.scrollHeight;
      setYeniMesajSayisi(0);
      setAsagiButonu(false);
      return;
    }

    el.scrollTo({ top: el.scrollHeight, behavior: mode });
    window.setTimeout(() => {
      setYeniMesajSayisi(0);
      setAsagiButonu(false);
    }, 350);
  }

  function adaptiveBottom() {
    const el = messagesRef.current;
    if (!el)
      return;

    const distance = el.scrollHeight - el.scrollTop - el.clientHeight;
    if (distance > 2400) {
      scrollToBottom("instant");
      return;
    }

    if (distance > 900) {
      el.scrollTo({ top: el.scrollHeight, behavior: "auto" });
      setYeniMesajSayisi(0);
      setAsagiButonu(false);
      return;
    }

    scrollToBottom("smooth");
  }

  const aktifVar = Boolean(aktifSohbetId && aktifSohbet);

  return (
    <main className="chat-center-page">
      <section className={`chat-center ${aktifVar ? "chat-center--thread-open" : ""}`}>
        <aside className="chat-center__sidebar">
          <header>
            <div>
              <span className="accountant-eyebrow">
                <MessageCircle size={16} />
                Sohbet Merkezi
              </span>
              <h2>Sohbetler</h2>
            </div>
            <button type="button" onClick={() => listeYukle().catch(() => undefined)} disabled={listeYukleniyor} aria-label="Yenile">
              {listeYukleniyor ? <Loader2 size={17} className="spin" /> : <RefreshCw size={17} />}
            </button>
          </header>
          <label className="chat-center__archive-toggle">
            <input type="checkbox" checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} />
            <span>Arşivlenenleri göster</span>
          </label>
          <div className="chat-center__list">
            {listeYukleniyor ? (
              <p className="chat-center__state"><Loader2 size={17} className="spin" /> Sohbetler yükleniyor...</p>
            ) : liste.sohbetler.length === 0 ? (
              <p className="chat-center__state">Henüz sohbet yok.</p>
            ) : liste.sohbetler.map((item) => (
              <button
                key={item.id}
                type="button"
                className={item.id === aktifSohbetId ? "active" : ""}
                onClick={() => setAktifSohbetId(item.id)}
              >
                <span>
                  <strong>{item.baslik}</strong>
                  <small>{item.konu || "Sohbet"}</small>
                  <em>{item.sonMesaj || "Henüz mesaj yok."}</em>
                </span>
                {item.okunmamisMesajSayisi > 0 ? <i>{item.okunmamisMesajSayisi > 9 ? "9+" : item.okunmamisMesajSayisi}</i> : null}
              </button>
            ))}
          </div>
        </aside>

        <section className="chat-center__thread">
          {!aktifVar ? (
            <div className="chat-center__empty">
              <MessageCircle size={34} />
              <strong>Sohbet seçin</strong>
              <span>Mesaj geçmişi, dosyalar ve veri paylaşımları burada görünür.</span>
            </div>
          ) : (
            <>
              <header className="chat-thread__header">
                <button type="button" className="chat-thread__back" onClick={() => setAktifSohbetId(0)} aria-label="Listeye dön">
                  <ChevronLeft size={19} />
                </button>
                <div>
                  <strong>{aktifSohbet?.baslik}</strong>
                  <label>
                    <span>Konu</span>
                    <input value={konu} onChange={(event) => setKonu(event.target.value)} onBlur={konuKaydet} maxLength={120} />
                  </label>
                  <small>
                    {realtime === "connected" ? <Signal size={14} /> : <SignalZero size={14} />}
                    {realtime === "connected" ? "Canlı" : "Bağlantı geri gelene kadar kısa polling çalışıyor"}
                  </small>
                </div>
                <button type="button" onClick={arsivle}>
                  <Archive size={16} />
                  <span>{aktifSohbet?.arsivlendi ? "Arşivden çıkar" : "Arşivle"}</span>
                </button>
              </header>

              {hata ? (
                <p className="chat-center__error">
                  <span>{hata}</span>
                  <button type="button" onClick={() => setHata("")} aria-label="Kapat"><X size={15} /></button>
                </p>
              ) : null}

              <div className="chat-thread__messages" ref={messagesRef} onScroll={scrollKontrol}>
                {eskiYukleniyor ? <p className="chat-center__state"><Loader2 size={15} className="spin" /> Eski mesajlar yükleniyor...</p> : null}
                {mesajYukleniyor ? (
                  <p className="chat-center__state"><Loader2 size={17} className="spin" /> Mesajlar yükleniyor...</p>
                ) : mesajlar.length === 0 ? (
                  <p className="chat-center__state">Henüz mesaj yok. İlk mesajı Systemcel içinde gönderin.</p>
                ) : mesajlar.map((item) => (
                  <article key={`${item.id}-${item.clientMessageId}`} className={`chat-bubble ${item.benimMesajim ? "mine" : ""} chat-bubble--${item.mesajTipi}`}>
                    <small>{item.gonderenAdi} · {tarihSaat(item.createdAt)}</small>
                    <p>{item.mesaj}</p>
                    {item.ekler.length ? (
                      <div className="chat-bubble__attachments">
                        {item.ekler.map((ek) => ek.ekTipi === "VeriKarti" ? (
                          <DataCard key={ek.id} attachment={ek} />
                        ) : (
                          <a key={ek.id} href={ek.indirUrl} target="_blank" rel="noreferrer">
                            {ek.ekTipi === "RaporPaketi" ? <Share2 size={15} /> : <Download size={15} />}
                            <span>{ek.baslik || ek.dosyaAdi || "Ek"}</span>
                          </a>
                        ))}
                      </div>
                    ) : null}
                    {item.benimMesajim ? <em>{item.durum}</em> : null}
                  </article>
                ))}
              </div>

              {karsiTarafYaziyor ? <span className="chat-thread__typing">{aktifSohbet?.karsiTarafAdi || aktifSohbet?.baslik} yazıyor...</span> : null}
              {asagiButonu ? (
                <button type="button" className="chat-thread__bottom-button" onClick={adaptiveBottom}>
                  <ArrowDown size={17} />
                  <span>{yeniMesajSayisi > 0 ? `${yeniMesajSayisi} yeni mesaj` : "En alta in"}</span>
                </button>
              ) : null}

              <form className="chat-thread__composer" onSubmit={mesajGonder}>
                <div className="chat-thread__tools">
                  <input ref={fileRef} type="file" multiple hidden onChange={dosyaSecildi} accept=".pdf,.xml,.html,.htm,.xlsx,.csv,.zip,.png,.jpg,.jpeg,.webp" />
                  <button type="button" onClick={() => fileRef.current?.click()} disabled={dosyaIslemde}>
                    {dosyaIslemde ? <Loader2 size={16} className="spin" /> : <Paperclip size={16} />}
                    <span>Dosya</span>
                  </button>
                  <label>
                    <span>Dönem</span>
                    <select value={veriAraligi} onChange={(event) => setVeriAraligi(event.target.value)}>
                      <option value="last30">Son 30 gün</option>
                      <option value="thisMonth">Bu ay</option>
                      <option value="previousMonth">Önceki ay</option>
                      <option value="selectedMonth">Seçili ay</option>
                      <option value="custom">Özel aralık</option>
                    </select>
                  </label>
                  {veriAraligi === "selectedMonth" ? <input type="month" value={seciliAy} onChange={(event) => setSeciliAy(event.target.value)} /> : null}
                  {veriAraligi === "custom" ? (
                    <>
                      <input type="date" value={ozelBaslangic} onChange={(event) => setOzelBaslangic(event.target.value)} />
                      <input type="date" value={ozelBitis} onChange={(event) => setOzelBitis(event.target.value)} />
                    </>
                  ) : null}
                  <button type="button" onClick={veriAksiyonu} disabled={veriIslemde}>
                    {veriIslemde ? <Loader2 size={16} className="spin" /> : <FileUp size={16} />}
                    <span>{dataActionLabel}</span>
                  </button>
                </div>
                <label className="chat-thread__input">
                  <textarea
                    value={metin}
                    onChange={(event) => metinDegisti(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" && !event.shiftKey) {
                        event.preventDefault();
                        mesajGonder().catch(() => undefined);
                      }
                    }}
                    placeholder="Bir mesaj yazın..."
                    rows={2}
                  />
                  <button type="submit" disabled={!metin.trim()}>
                    <Send size={17} />
                  </button>
                </label>
              </form>
            </>
          )}
        </section>
      </section>
    </main>
  );
}

function DataCard({ attachment }: { attachment: SohbetEki }) {
  const data = React.useMemo(() => {
    try {
      return JSON.parse(attachment.ozetJson || "{}") as Record<string, unknown>;
    } catch {
      return {};
    }
  }, [attachment.ozetJson]);

  return (
    <div className="chat-data-card">
      <strong>{attachment.baslik || "Veri özeti"}</strong>
      <span>Gelir: {para(data.gelir)}</span>
      <span>Gider: {para(data.gider)}</span>
      <span>Net: {para(data.net)}</span>
    </div>
  );
}

function tarihSaat(value: string) {
  if (!value)
    return "-";
  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function para(value: unknown) {
  const numeric = typeof value === "number" ? value : Number(value ?? 0);
  return numeric.toLocaleString("tr-TR", { style: "currency", currency: "TRY" });
}
