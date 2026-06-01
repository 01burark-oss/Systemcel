import React from "react";
import { ChevronDown } from "lucide-react";

export const helpTopics = [
  { title: "SSS", href: "/yardım#sss" },
  { title: "İlk Kurulum", href: "/yardım#ilk-kurulum" },
  { title: "Gelir / Gider Kayıtları", href: "/yardım#gelir-gider" },
  { title: "Faturalar", href: "/yardım#faturalar" },
  { title: "GİB e-Arşiv", href: "/yardım#gib-e-arsiv" },
  { title: "Telegram Bildirimleri", href: "/yardım#telegram" },
  { title: "Raporlar", href: "/yardım#raporlar" },
  { title: "Abonelik ve Faturalandırma", href: "/yardım#abonelik" },
  { title: "Güvenlik", href: "/yardım#guvenlik" }
];

export function HelpDropdown({ className = "" }: { className?: string }) {
  const [open, setOpen] = React.useState(false);
  const rootRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (!open) {
      return;
    }

    const handlePointerDown = (event: PointerEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  return (
    <div ref={rootRef} className={`marketing-help ${open ? "is-open" : ""} ${className}`.trim()}>
      <button type="button" onClick={() => setOpen((current) => !current)} aria-expanded={open} aria-haspopup="menu">
        Yardım
        <ChevronDown size={16} />
      </button>
      <div className="marketing-help__panel" role="menu" aria-hidden={!open}>
        {helpTopics.map((topic) => (
          <a key={topic.href} href={topic.href} role="menuitem" tabIndex={open ? 0 : -1} onClick={() => setOpen(false)}>
            {topic.title}
          </a>
        ))}
      </div>
    </div>
  );
}
