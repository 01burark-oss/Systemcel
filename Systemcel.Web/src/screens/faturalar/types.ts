export interface FaturaSecenek {
  deger: string;
  etiket: string;
}

export interface FaturaCariSecenek {
  id: number;
  unvan: string;
}

export interface FaturaUrunSecenek {
  id: number;
  ad: string;
  tip: string;
  birim: string;
  kdvOrani: number;
  alisFiyati: number;
  satisFiyati: number;
}

export interface FaturaListeKaydi {
  id: number;
  no: string;
  tarih: string;
  vadeTarihi: string;
  faturaTipi: string;
  durum: string;
  cariKartId: number;
  cariUnvan: string;
  genelToplam: number;
  odenenTutar: number;
  odemeYontemi: string;
  aciklama: string;
}

export interface FaturaOzet {
  toplamFatura: number;
  faturaAdedi: number;
  tahsilEdilen: number;
  bekleyen: number;
  bekleyenAdedi: number;
}

export interface FaturaEkranVerisi {
  aktifIsletme: string;
  faturalar: FaturaListeKaydi[];
  cariler: FaturaCariSecenek[];
  urunler: FaturaUrunSecenek[];
  ozet: FaturaOzet;
  faturaTipleri: FaturaSecenek[];
  odemeYontemleri: FaturaSecenek[];
  bugun: string;
}

export interface FaturaSatirDetay {
  id: number;
  urunHizmetId: number;
  aciklama: string;
  birim: string;
  miktar: number;
  birimFiyat: number;
  iskontoOrani: number;
  kdvOrani: number;
  stokEtkilesin: boolean;
  satirToplam: number;
}

export interface FaturaDetay {
  fatura: {
    id: number;
    cariKartId: number;
    tarih: string;
    vadeTarihi: string;
    faturaTipi: string;
    durum: string;
    yerelFaturaNo: string;
    portalBelgeNo: string;
    portalUuid: string;
    araToplam: number;
    iskontoToplam: number;
    kdvToplam: number;
    genelToplam: number;
    odenenTutar: number;
    odemeYontemi: string;
    aciklama: string;
    cariUnvan: string;
  };
  satirlar: FaturaSatirDetay[];
}

export interface FaturaFormu {
  id: number;
  cariKartId: string;
  tarih: string;
  vadeVar: boolean;
  vadeTarihi: string;
  faturaTipi: string;
  odemeYontemi: string;
  aciklama: string;
  urunHizmetId: string;
  satirAciklama: string;
  birim: string;
  miktar: string;
  birimFiyat: string;
  kdvOrani: string;
  iskontoOrani: string;
  stokEtkilesin: boolean;
}

export interface TahsilatFormu {
  tutar: string;
  tarih: string;
  odemeYontemi: string;
  aciklama: string;
}
