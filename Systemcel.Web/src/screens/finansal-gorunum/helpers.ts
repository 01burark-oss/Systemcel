import type { NakitProjeksiyonHaftasi, PlanlananNakitFormu } from "./types";

export function yerelTarihDegeri(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function bosPlanFormu(tarih = yerelTarihDegeri()): PlanlananNakitFormu {
  return {
    id: 0,
    ad: "",
    tip: "Gider",
    tutar: "",
    ilkTarih: tarih,
    tekrarTipi: "TekSefer",
    tekrarAraligi: "1",
    bitisTarihi: "",
    kategori: "",
    aciklama: "",
    aktif: true
  };
}

export function paraBic(value: number, currency = "TRY") {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(Number.isFinite(value) ? value : 0);
}

export function yuzdeBic(value: number) {
  return new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 1
  }).format(Number.isFinite(value) ? value : 0) + "%";
}

export function tarihBic(value: string, short = false) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value || "—";
  return date.toLocaleDateString("tr-TR", short
    ? { day: "2-digit", month: "short" }
    : { day: "2-digit", month: "long", year: "numeric" });
}

export function riskEtiketi(value: string) {
  switch (value) {
    case "Yuksek": return "Yüksek risk";
    case "Orta": return "Orta risk";
    case "Dagitilmis": return "Dengeli dağılım";
    case "Dusuk": return "Düşük risk";
    case "VeriYok": return "Veri yok";
    default: return value || "Veri yok";
  }
}

export function ritimEtiketi(value: string) {
  switch (value) {
    case "Kotulesiyor": return "Yavaşlıyor";
    case "Iyilesiyor": return "İyileşiyor";
    case "Vadesinde": return "Vadesinde";
    case "Dengeli": return "Dengeli";
    case "YetersizVeri": return "Yetersiz veri";
    default: return value || "Yetersiz veri";
  }
}

export function durumSinifi(value: string) {
  if (value === "Yuksek" || value === "Kotulesiyor") return "danger";
  if (value === "Orta") return "warning";
  if (value === "Iyilesiyor" || value === "Vadesinde" || value === "Dagitilmis" || value === "Dusuk") return "positive";
  return "neutral";
}

export function tutarOku(value: string) {
  const normalized = value.trim().replace(/\s/g, "").replace(",", ".");
  const result = Number(normalized);
  if (!Number.isFinite(result) || result <= 0) {
    throw new Error("Tutar sıfırdan büyük olmalıdır.");
  }
  return result;
}

export interface ProjeksiyonOlcegi {
  min: number;
  max: number;
  range: number;
  /** Sıfır çizgisinin grafik alanı içindeki yüzde konumu (üstten). */
  sifirYuzde: number;
}

export function projeksiyonOlcegi(weeks: NakitProjeksiyonHaftasi[]): ProjeksiyonOlcegi {
  if (weeks.length === 0) return { min: 0, max: 0, range: 1, sifirYuzde: 100 };
  const balances = weeks.map((row) => row.kapanisBakiyesi);
  const min = Math.min(0, ...balances);
  const max = Math.max(0, ...balances);
  const range = Math.max(max - min, 1);
  return { min, max, range, sifirYuzde: (max / range) * 100 };
}

/** Kapanış bakiyesi çizgisi; x konumları hafta sütunlarının ortasına hizalıdır. */
export function projeksiyonCizgisi(weeks: NakitProjeksiyonHaftasi[], width = 960, height = 190) {
  if (weeks.length === 0) return "";
  const { min, range } = projeksiyonOlcegi(weeks);
  const xStep = width / weeks.length;
  return weeks.map((row, index) => {
    const x = index * xStep + xStep / 2;
    const y = height - ((row.kapanisBakiyesi - min) / range) * height;
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
}
