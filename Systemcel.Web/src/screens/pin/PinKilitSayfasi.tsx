import React from "react";
import { Delete, LogIn, LogOut } from "lucide-react";
import { jsonOku } from "../../shared/json";

type HostBridgeWindow = Window &
  typeof globalThis & {
    chrome?: {
      webview?: {
        postMessage: (message: unknown) => void;
      };
    };
  };

function hostMesajGonder(message: string) {
  const bridge = (window as HostBridgeWindow).chrome?.webview;
  if (bridge) {
    bridge.postMessage(message);
  }
}

function kutuDegeri(pin: string, index: number) {
  return index < pin.length ? "*" : "-";
}

export function PinKilitSayfasi() {
  const [pin, setPin] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [bilgi, setBilgi] = React.useState("");

  React.useEffect(() => {
    const oncekiBodyOverflow = document.body.style.overflow;
    const oncekiHtmlOverflow = document.documentElement.style.overflow;

    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";
    window.scrollTo({ top: 0, left: 0 });

    return () => {
      document.body.style.overflow = oncekiBodyOverflow;
      document.documentElement.style.overflow = oncekiHtmlOverflow;
    };
  }, []);

  const girisYap = React.useCallback(
    async (deger?: string) => {
      const aday = (deger ?? pin).trim();
      if (islemde) {
        return;
      }

      if (aday.length !== 4) {
        setHata("4 haneli PIN girin.");
        return;
      }

      try {
        setIslemde(true);
        setHata("");
        setBilgi("");
        await jsonOku("/api/ekran/kilit-ekrani/dogrula", {
          method: "POST",
          body: JSON.stringify({ pin: aday })
        });
        hostMesajGonder("pin-success");
      } catch (error) {
        setPin("");
        setHata(error instanceof Error ? error.message : "PIN doğrulanamadı.");
      } finally {
        setIslemde(false);
      }
    },
    [islemde, pin]
  );

  React.useEffect(() => {
    if (pin.length === 4 && !islemde) {
      void girisYap(pin);
    }
  }, [girisYap, islemde, pin]);

  React.useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (islemde) {
        return;
      }

      if (event.key >= "0" && event.key <= "9") {
        event.preventDefault();
        setPin((current) => (current.length >= 4 ? current : current + event.key));
        setHata("");
        setBilgi("");
        return;
      }

      if (event.key === "Backspace") {
        event.preventDefault();
        setPin((current) => current.slice(0, -1));
        return;
      }

      if (event.key === "Escape") {
        event.preventDefault();
        hostMesajGonder("pin-cancel");
        return;
      }

      if (event.key === "Enter") {
        event.preventDefault();
        void girisYap();
      }
    }

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [girisYap, islemde]);

  function rakamEkle(rakam: string) {
    if (islemde) {
      return;
    }

    setPin((current) => (current.length >= 4 ? current : current + rakam));
    setHata("");
    setBilgi("");
  }

  function temizle() {
    if (islemde) {
      return;
    }

    setPin("");
    setHata("");
    setBilgi("");
  }

  function sil() {
    if (islemde) {
      return;
    }

    setPin((current) => current.slice(0, -1));
  }

  async function sifreyiHatirlat() {
    if (islemde) {
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<{ mesaj: string }>("/api/ekran/kilit-ekrani/hatirlat", {
        method: "POST"
      });
      setBilgi(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "PIN hatırlatma başarısız.");
    } finally {
      setIslemde(false);
    }
  }

  return (
    <main className="pin-lock">
      <section className="pin-lock__card">
        <div className="pin-lock__badge">GÜVENLİ GİRİŞ</div>
        <h1>Systemcel Giriş</h1>

        <div className="pin-lock__display" aria-label="PIN kutuları">
          {Array.from({ length: 4 }, (_, index) => (
            <div key={index} className={`pin-lock__slot ${pin.length === index ? "hazir" : ""}`}>
              {kutuDegeri(pin, index)}
            </div>
          ))}
        </div>

        <div className="pin-lock__status">
          {hata ? <p className="pin-lock__status-error">{hata}</p> : bilgi ? <p>{bilgi}</p> : <p>4 haneli PIN girin.</p>}
        </div>

        <div className="pin-lock__keypad">
          {["1", "2", "3", "4", "5", "6", "7", "8", "9"].map((rakam) => (
            <button key={rakam} type="button" onClick={() => rakamEkle(rakam)} disabled={islemde}>
              {rakam}
            </button>
          ))}
          <button type="button" className="yardimci" onClick={temizle} disabled={islemde}>
            Temizle
          </button>
          <button type="button" onClick={() => rakamEkle("0")} disabled={islemde}>
            0
          </button>
          <button type="button" className="yardimci" onClick={sil} disabled={islemde}>
            <Delete size={18} />
            Sil
          </button>
        </div>

        <button type="button" className="pin-lock__forgot" onClick={() => void sifreyiHatirlat()} disabled={islemde}>
          Şifreyi Unuttum
        </button>

        <div className="pin-lock__actions">
          <button type="button" className="pin-lock__exit" onClick={() => hostMesajGonder("pin-cancel")} disabled={islemde}>
            <LogOut size={18} />
            Çıkış
          </button>
          <button type="button" className="pin-lock__login" onClick={() => void girisYap()} disabled={islemde}>
            <LogIn size={18} />
            Giriş
          </button>
        </div>
      </section>
    </main>
  );
}
