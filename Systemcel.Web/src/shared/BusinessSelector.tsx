import React from "react";
import { Check, ChevronDown } from "lucide-react";
import type { IsletmeSecenek } from "./chrome";

interface BusinessSelectorProps {
  aktifIsletmeId?: number;
  isletmeler: IsletmeSecenek[];
  disabled?: boolean;
  onChange: (id: number) => void;
}

export function BusinessSelector({
  aktifIsletmeId,
  isletmeler,
  disabled = false,
  onChange
}: BusinessSelectorProps) {
  const [acik, setAcik] = React.useState(false);
  const rootRef = React.useRef<HTMLDivElement | null>(null);
  const menuId = React.useId();
  const etkisiz = disabled || isletmeler.length === 0;
  const aktifIsletme = isletmeler.find((item) => item.id === aktifIsletmeId);

  React.useEffect(() => {
    if (!acik) return;

    function onPointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setAcik(false);
      }
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setAcik(false);
      }
    }

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [acik]);

  return (
    <div
      ref={rootRef}
      className={acik ? "business-selector business-selector--open" : "business-selector"}
      aria-label="İşletme seç"
    >
      <span className="business-selector__prefix">İşletme:</span>
      <button
        type="button"
        className="business-selector__button"
        disabled={etkisiz}
        aria-haspopup="listbox"
        aria-expanded={acik}
        aria-controls={menuId}
        onClick={() => setAcik((current) => !current)}
      >
        {aktifIsletme?.ad ?? "Yükleniyor"}
      </button>
      <ChevronDown className="business-selector__chevron" size={18} />

      {acik && !etkisiz && (
        <div className="business-selector__menu" id={menuId} role="listbox">
          {isletmeler.map((item) => {
            const aktif = item.id === aktifIsletmeId;
            return (
              <button
                key={item.id}
                type="button"
                className={aktif ? "business-selector__option active" : "business-selector__option"}
                role="option"
                aria-selected={aktif}
                onClick={() => {
                  setAcik(false);
                  if (!aktif) {
                    onChange(item.id);
                  }
                }}
              >
                <span>{item.ad}</span>
                {aktif && <Check size={16} />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
