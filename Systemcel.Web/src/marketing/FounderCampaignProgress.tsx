export type FounderCampaignProgressProps = {
  total: number;
  won: number;
  percentage: number;
  language: "tr" | "en";
};

export function FounderCampaignProgress({ total, won, percentage, language }: FounderCampaignProgressProps) {
  const safeTotal = Math.max(0, total);
  const safeWon = Math.min(Math.max(0, won), safeTotal);
  const safePercentage = Math.min(Math.max(0, percentage), 100);
  const tr = language === "tr";

  return (
    <section className="marketing-founder-progress" aria-label={tr ? "Lansman kontenjanı" : "Launch availability"}>
      <div className="marketing-founder-progress__copy">
        <span>{tr ? `İlk ${safeTotal} kontenjanı` : `First ${safeTotal} launch spots`}</span>
        <strong>{safeWon}/{safeTotal} {tr ? "doldu" : "filled"}</strong>
        <b>%{safePercentage}</b>
      </div>
      <div
        className="marketing-founder-progress__track"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={safeTotal}
        aria-valuenow={safeWon}
        aria-valuetext={`${safeWon}/${safeTotal} ${tr ? "kontenjan doldu" : "spots filled"}`}
      >
        <i style={{ transform: `scaleX(${safePercentage / 100})` }} />
      </div>
    </section>
  );
}
