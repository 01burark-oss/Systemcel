import React from "react";
import { useI18n } from "../../shared/i18n";

export function ReportPeriodPicker({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const { language, locale } = useI18n();
  const [year, month] = value.split("-");
  const [draftYear, setDraftYear] = React.useState(year);
  const labels = language === "en" ? ["Period", "Month", "Year"] : language === "de" ? ["Zeitraum", "Monat", "Jahr"] : ["Dönem", "Ay", "Yıl"];
  React.useEffect(() => setDraftYear(year), [year]);

  return <fieldset className="reports-period-picker">
    <legend>{labels[0]}</legend>
    <label><span>{labels[1]}</span><select aria-label={`${labels[0]} ${labels[1]}`} value={month} onChange={event => onChange(`${year}-${event.target.value}`)}>
      {Array.from({ length: 12 }, (_, index) => <option key={index} value={String(index + 1).padStart(2, "0")}>
        {new Intl.DateTimeFormat(locale, { month: "long", timeZone: "UTC" }).format(new Date(Date.UTC(2026, index, 1)))}
      </option>)}
    </select></label>
    <label><span>{labels[2]}</span><input aria-label={`${labels[0]} ${labels[2]}`} inputMode="numeric" maxLength={4} value={draftYear}
      onChange={event => {
        const next = event.target.value.replace(/\D/g, "").slice(0, 4);
        setDraftYear(next);
        if (/^[1-9]\d{3}$/.test(next)) onChange(`${next}-${month}`);
      }}
      onBlur={() => setDraftYear(year)} /></label>
  </fieldset>;
}
