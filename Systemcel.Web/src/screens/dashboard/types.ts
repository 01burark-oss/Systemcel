export interface OzetKart {
  etiket: string;
  aralik: string;
  gelir: number;
  gider: number;
  net: number;
  gelirAdet: number;
  giderAdet: number;
}

export interface Karsilastirma {
  yuzde: number;
  etiket: string;
  olumlu: boolean;
}

export interface OdemeDagilim {
  yontem: string;
  gelir: number;
  gider: number;
  net: number;
  toplam: number;
}

export interface NetTrendNokta {
  gun: string;
  net: number;
  islemVar?: boolean;
}

export interface SohbetBildirimDurumu {
  okunmamisMesajSayisi: number;
  sohbetler: Array<{
    baslik: string;
    sonMesaj: string;
    sonMesajAt: string;
    okunmamisMesajSayisi: number;
    hedefUrl: string;
  }>;
}

export type BelgeSaglikDurumu = "Hazir" | "Dikkat" | "Eksik" | "VeriYok";

export interface BelgeSaglikSorunu {
  kod: string;
  baslik: string;
  adet: number;
  puanEtkisi: number;
  aksiyonUrl: string;
}

export interface BelgeSaglikOzeti {
  skor: number | null;
  durum: BelgeSaglikDurumu;
  donemBaslangic: string;
  donemBitis: string;
  faturaSayisi: number;
  hazirBelgeSayisi: number;
  eksikBelgeSayisi: number;
  taslakFaturaSayisi: number;
  dosyasiEksikFaturaSayisi: number;
  satiriEksikFaturaSayisi: number;
  cariBilgisiEksikFaturaSayisi: number;
  vadeTarihiEksikFaturaSayisi: number;
  bekleyenVeriIstegiSayisi: number;
  sonBelgeAt: string | null;
  muhasebeciBagli: boolean;
  sorunlar: BelgeSaglikSorunu[];
}

export interface DashboardEkran {
  aktifIsletme: string;
  bugun: OzetKart;
  paneller: OzetKart[];
  gelirDegisim: Karsilastirma;
  giderDegisim: Karsilastirma;
  odemeDagilimi: OdemeDagilim[];
  netTrend: NetTrendNokta[];
  brutKarMarji: BrutKarMarji;
  sohbet?: SohbetBildirimDurumu;
  belgeSagligi?: BelgeSaglikOzeti | null;
}

export interface BrutKarMarji {
  durum: "Hazir" | "EksikMaliyet" | "VeriYok";
  guvenilir: boolean;
  satisGeliri: number;
  satisMaliyeti: number;
  brutKar: number;
  brutKarOrani: number | null;
  satisSatiri: number;
  eksikMaliyetliSatisSatiri: number;
  aciklama: string;
}
