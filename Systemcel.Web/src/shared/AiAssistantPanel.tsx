import React from "react";
import { Bot, Lightbulb, Loader2, Send, Sparkles, X } from "lucide-react";
import { jsonOku } from "./json";

type AiTab = "sohbet" | "oneriler";

interface AiStatus {
  configured: boolean;
  proModel: string;
  flashModel: string;
  baseUrl: string;
  usage?: AiUsageStatus;
}

interface AiUsageStatus {
  aiAktif: boolean;
  planKodu: string;
  planAdi: string;
  sinirsizPlan: boolean;
  donemTipi: string;
  limit?: number | null;
  kullanilan: number;
  kalan?: number | null;
  donemBitisAt: string;
  izinVerildi: boolean;
  limitAsildi: boolean;
  mesaj: string;
}

interface AiSuggestion {
  baslik: string;
  aciklama: string;
  oncelik: string;
  metrik: string;
  kaynak: string;
}

interface AiSuggestionsResponse {
  configured: boolean;
  model: string;
  summary: string;
  suggestions: AiSuggestion[];
  generatedAt: string;
  usage?: AiUsageStatus;
}

interface AiChatResponse {
  configured: boolean;
  mode: string;
  model: string;
  answer: string;
  suggestions: string[];
  generatedAt: string;
  usage?: AiUsageStatus;
}

interface ChatMessage {
  id: string;
  role: "assistant" | "user";
  content: string;
  meta?: string;
}

const introMessage: ChatMessage = {
  id: "intro",
  role: "assistant",
  content: "Hazırım. İşletmenin gelir, gider, fatura ve stok verilerine bakarak cevap verebilirim."
};

function createId(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.round(Math.random() * 100000)}`;
}

function formatUsage(usage?: AiUsageStatus) {
  if (!usage)
    return "";

  if (!usage.aiAktif)
    return "";

  if (usage.sinirsizPlan)
    return "";

  if (usage.limit == null)
    return "";

  const kalan = usage.kalan ?? Math.max(0, usage.limit - usage.kullanilan);
  return `Kalan AI hakkı: ${kalan}/${usage.limit}`;
}

export function AiAssistantPanel() {
  const [open, setOpen] = React.useState(false);
  const [activeTab, setActiveTab] = React.useState<AiTab>("oneriler");
  const [status, setStatus] = React.useState<AiStatus | null>(null);
  const [suggestionData, setSuggestionData] = React.useState<AiSuggestionsResponse | null>(null);
  const [messages, setMessages] = React.useState<ChatMessage[]>([introMessage]);
  const [input, setInput] = React.useState("");
  const [loading, setLoading] = React.useState(false);
  const [sending, setSending] = React.useState(false);
  const [error, setError] = React.useState("");

  const loadStatus = React.useCallback(async () => {
    setError("");
    try {
      const statusResponse = await jsonOku<AiStatus>("/api/ai/durum");
      setStatus(statusResponse);
    } catch (err) {
      setError(err instanceof Error ? err.message : "AI asistanı yüklenemedi.");
    }
  }, []);

  const loadSuggestions = React.useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const suggestionsResponse = await jsonOku<AiSuggestionsResponse>("/api/ai/oneriler");
      setSuggestionData(suggestionsResponse);
      setStatus((current) => current ? { ...current, usage: suggestionsResponse.usage ?? current.usage } : current);
    } catch (err) {
      setError(err instanceof Error ? err.message : "AI önerileri yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }, []);

  React.useEffect(() => {
    if (!open)
      return;

    void loadStatus();
  }, [loadStatus, open]);

  const handleSend = React.useCallback(async () => {
    const text = input.trim();
    if (!text || sending)
      return;

    const userMessage: ChatMessage = {
      id: createId("user"),
      role: "user",
      content: text
    };

    setMessages((current) => [...current, userMessage]);
    setInput("");
    setSending(true);
    setError("");

    try {
      const response = await jsonOku<AiChatResponse>("/api/ai/sohbet", {
        method: "POST",
        body: JSON.stringify({
          mesaj: text,
          mode: "chat"
        })
      });

      setStatus((current) => current ? { ...current, usage: response.usage ?? current.usage } : current);
      setMessages((current) => [
        ...current,
        {
          id: createId("assistant"),
          role: "assistant",
          content: response.answer
        }
      ]);
    } catch (err) {
      setMessages((current) => [
        ...current,
        {
          id: createId("assistant-error"),
          role: "assistant",
          content: err instanceof Error ? err.message : "Yanıt alınamadı."
        }
      ]);
    } finally {
      setSending(false);
    }
  }, [input, sending]);

  const topSuggestions = suggestionData?.suggestions ?? [];
  const configured = status?.configured ?? suggestionData?.configured ?? false;
  const usage = status?.usage ?? suggestionData?.usage;
  const usageText = formatUsage(usage);

  return (
    <>
      <button
        className="ai-assistant-trigger"
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-label="AI asistanını aç"
      >
        <Sparkles size={22} />
      </button>

      {open ? (
        <section className="ai-assistant-panel" aria-label="Systemcel AI Asistanı">
          <header className="ai-assistant-panel__header">
            <div>
              <span className="ai-assistant-panel__icon">
                <Bot size={20} />
              </span>
              <div>
                <strong>Systemcel AI</strong>
                <small>{usage && !usage.aiAktif ? "Ücretli plan gerekli" : configured ? "Asistan aktif" : "Anahtar bekleniyor"}</small>
              </div>
            </div>
            <button type="button" onClick={() => setOpen(false)} aria-label="AI asistanını kapat">
              <X size={18} />
            </button>
          </header>

          <div className="ai-assistant-panel__tabs" role="tablist" aria-label="AI asistan sekmeleri">
            <button className={activeTab === "oneriler" ? "active" : ""} type="button" onClick={() => setActiveTab("oneriler")}>
              <Lightbulb size={16} />
              <span>Öneriler</span>
            </button>
            <button className={activeTab === "sohbet" ? "active" : ""} type="button" onClick={() => setActiveTab("sohbet")}>
              <Sparkles size={16} />
              <span>Sohbet</span>
            </button>
          </div>

          {usageText ? (
            <div className={usage?.limitAsildi ? "ai-assistant-panel__usage limit" : "ai-assistant-panel__usage"}>
              {usageText}
            </div>
          ) : null}

          {error ? <div className="ai-assistant-panel__error">{error}</div> : null}

          <div className="ai-assistant-panel__body">
            {activeTab === "sohbet" ? (
              <>
                <div className="ai-assistant-chat">
                  {messages.map((message) => (
                    <article key={message.id} className={`ai-assistant-chat__message ${message.role}`}>
                      <p>{message.content}</p>
                      {message.meta ? <small>{message.meta}</small> : null}
                    </article>
                  ))}
                  {sending ? (
                    <article className="ai-assistant-chat__message assistant">
                      <p className="ai-assistant-loading">
                        <Loader2 className="spin" size={15} />
                        Yanıt hazırlanıyor
                      </p>
                    </article>
                  ) : null}
                </div>

                <form
                  className="ai-assistant-composer"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void handleSend();
                  }}
                >
                  <textarea
                    rows={2}
                    value={input}
                    onChange={(event) => setInput(event.target.value)}
                    placeholder="Giderleri nasıl düşürebiliriz?"
                  />
                  <button type="submit" disabled={!input.trim() || sending} aria-label="Mesaj gönder">
                    {sending ? <Loader2 className="spin" size={18} /> : <Send size={18} />}
                  </button>
                </form>
              </>
            ) : null}

            {activeTab === "oneriler" ? (
              <div className="ai-assistant-suggestions">
                <div className="ai-assistant-suggestions__toolbar">
                  <span>Öneriler</span>
                  <button type="button" onClick={() => void loadSuggestions()} disabled={loading || !configured}>
                    {loading ? <Loader2 className="spin" size={15} /> : <Sparkles size={15} />}
                    Öneri al
                  </button>
                </div>
                {loading ? (
                  <p className="ai-assistant-loading">
                    <Loader2 className="spin" size={15} />
                    Yapay zeka işletmenizi şu anda analiz ediyor, önerilerinizi üretiyoruz...
                  </p>
                ) : null}
                {!loading && !suggestionData ? (
                  <p className="ai-assistant-suggestions__summary ai-assistant-suggestions__empty">
                    Öneri almak için butona basın.
                  </p>
                ) : null}
                {suggestionData?.summary ? <p className="ai-assistant-suggestions__summary">{suggestionData.summary}</p> : null}
                {topSuggestions.map((item) => (
                  <button
                    key={`${item.kaynak}-${item.baslik}`}
                    className="ai-assistant-suggestion"
                    type="button"
                    onClick={() => {
                      setActiveTab("sohbet");
                      setInput(`${item.baslik}: ${item.aciklama}`);
                    }}
                  >
                    <span>
                      <strong>{item.baslik}</strong>
                      <small>{item.aciklama}</small>
                    </span>
                    <em>{item.metrik || item.oncelik}</em>
                  </button>
                ))}
              </div>
            ) : null}
          </div>
        </section>
      ) : null}
    </>
  );
}
