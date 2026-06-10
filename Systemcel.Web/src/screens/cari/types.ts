export interface CariSecenek {
  deger: string;
  etiket: string;
}

export interface CariListeKaydi {
  id: number;
  tip: string;
  unvan: string;
  telefon: string;
  vergiNo: string;
  aktif: boolean;
}

export interface CariKartFormu {
  id: number;
  tip: string;
  unvan: string;
  telefon: string;
  eposta: string;
  vergiNoTc: string;
  vergiDairesi: string;
  adres: string;
  aktif: boolean;
}

export interface CariHareketKaydi {
  id: number;
  tarih: string;
  hareketTipi: string;
  aciklama: string;
  kaynak: string;
  tutar: number;
}

export interface CariDetay {
  kart: CariKartFormu;
  bakiye: number;
  hareketler: CariHareketKaydi[];
}

export interface CariEkranVerisi {
  aktifIsletme: string;
  kartlar: CariListeKaydi[];
  tipSecenekleri: CariSecenek[];
  hareketTipleri: CariSecenek[];
}

export interface CariHareketFormu {
  hareketTipi: string;
  tutar: string;
  tarih: string;
  aciklama: string;
}
