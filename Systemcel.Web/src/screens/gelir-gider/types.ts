export type Tur = "gelir" | "gider";

export type OdemeYontemi = "nakit" | "krediKarti" | "onlineOdeme" | "havale";

export interface Kayit {
  id: number;
  tarih: string;
  tur: Tur;
  tutar: number;
  odemeYontemi: OdemeYontemi;
  kalem: string;
  aciklama: string;
}

export interface OdemeSecenek {
  deger: OdemeYontemi;
  etiket: string;
}

export interface StokUrun {
  id: number;
  ad: string;
  birim: string;
}

export interface EkranVerisi {
  aktifIsletme: string;
  kayitlar: Kayit[];
  gelirKalemleri: string[];
  giderKalemleri: string[];
  stokUrunleri: StokUrun[];
  odemeYontemleri: OdemeSecenek[];
}

export interface FormDurumu {
  id: number | null;
  tarih: string;
  tur: Tur;
  tutar: string;
  odemeYontemi: OdemeYontemi;
  kalem: string;
  aciklama: string;
  stokAktif: boolean;
  stokUrunId: number;
  stokMiktar: string;
}
