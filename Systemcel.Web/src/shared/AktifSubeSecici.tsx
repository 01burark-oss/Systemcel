import React from "react";
import { Building2, ChevronDown, Loader2 } from "lucide-react";
import { jsonOku } from "./json";
import type { SubeKurDurumu } from "./subeKur";
import "./aktif-sube.css";

interface AktifSubeSeciciProps {
  onDegisti?: () => unknown | Promise<unknown>;
}

export function AktifSubeSecici({ onDegisti }: AktifSubeSeciciProps) {
  const [durum, setDurum] = React.useState<SubeKurDurumu | null>(null);
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");

  React.useEffect(() => {
    let aktif = true;
    jsonOku<SubeKurDurumu>("/api/ekran/sube-kur/")
      .then((data) => { if (aktif) setDurum(data); })
      .catch(() => { /* Üst bar, şube bağlamı olmayan çalışma alanlarını bozmaz. */ });
    return () => { aktif = false; };
  }, []);

  const subeSec = async (subeId: number) => {
    if (!durum || subeId === durum.aktifSube.id) return;
    const onceki = durum;
    try {
      setIslemde(true);
      setHata("");
      setDurum({ ...durum, aktifSube: durum.subeler.find((row) => row.id === subeId) ?? durum.aktifSube });
      const response = await jsonOku<SubeKurDurumu | { durum?: SubeKurDurumu }>("/api/ekran/sube-kur/aktif-sube", {
        method: "POST",
        body: JSON.stringify({ subeId })
      });
      const yeniDurum = "aktifSube" in response ? response : response.durum;
      if (yeniDurum) setDurum(yeniDurum);
      window.dispatchEvent(new CustomEvent("systemcel:sube-degisti", { detail: { subeId } }));
      await onDegisti?.();
    } catch (error) {
      setDurum(onceki);
      setHata(error instanceof Error ? error.message : "Şube değiştirilemedi.");
    } finally {
      setIslemde(false);
    }
  };

  if (!durum) return null;

  const aktifSubeler = durum.subeler.filter((row) => row.aktif);
  return (
    <div className="active-branch" title={hata || "Yeni kayıtlar seçili şubeye işlenir."}>
      <Building2 size={17} aria-hidden="true" />
      <label>
        <span>Aktif şube</span>
        <select
          aria-label="Aktif şube"
          value={durum.aktifSube.id}
          disabled={islemde || !durum.cokluSubeAktif || aktifSubeler.length < 2}
          onChange={(event) => void subeSec(Number(event.target.value))}
        >
          {aktifSubeler.map((row) => <option key={row.id} value={row.id}>{row.ad}</option>)}
        </select>
        <ChevronDown className="active-branch__chevron" size={14} aria-hidden="true" />
      </label>
      {islemde ? <Loader2 className="spin" size={15} aria-label="Şube değiştiriliyor" /> : null}
      {hata ? <span className="sr-only" role="alert">{hata}</span> : null}
    </div>
  );
}
