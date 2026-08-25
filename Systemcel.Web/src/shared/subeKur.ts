export interface SubeSecenek {
  id: number;
  ad: string;
  kod: string;
  varsayilan: boolean;
  aktif: boolean;
}

export interface DovizKuru {
  paraBirimi: string;
  kur: number;
  gecerliAt: string;
}

export interface SubeKurDurumu {
  aktifSube: SubeSecenek;
  subeler: SubeSecenek[];
  kurlar: DovizKuru[];
  cokluSubeAktif: boolean;
  cokluParaBirimiAktif: boolean;
}

export interface ParaBirimiOzeti {
  paraBirimi: string;
  gelirOrijinal: number;
  giderOrijinal: number;
  gelirTry: number;
  giderTry: number;
}

export interface SubeFinansOzeti {
  subeId?: number | null;
  konsolide: boolean;
  gelirTry: number;
  giderTry: number;
  netTry: number;
  paraBirimleri: ParaBirimiOzeti[];
}

export function yeniIdempotencyAnahtari() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}
