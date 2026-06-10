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

export interface DashboardEkran {
  aktifIsletme: string;
  bugun: OzetKart;
  paneller: OzetKart[];
  gelirDegisim: Karsilastirma;
  giderDegisim: Karsilastirma;
  odemeDagilimi: OdemeDagilim[];
  netTrend: NetTrendNokta[];
  sohbet?: SohbetBildirimDurumu;
}
