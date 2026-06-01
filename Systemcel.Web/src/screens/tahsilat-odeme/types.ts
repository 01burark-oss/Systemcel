export interface TahsilatOdemeSecenek {
  deger: string;
  etiket: string;
}

export interface TahsilatOdemeCariSecenek {
  id: number;
  unvan: string;
}

export interface TahsilatOdemeFaturaSecenek {
  id: number;
  no: string;
  cariKartId: number;
  cariUnvan: string;
  faturaTipi: string;
  durum: string;
  genelToplam: number;
  odenenTutar: number;
  kalan: number;
  odemeYontemi: string;
  aciklama: string;
}

export interface TahsilatOdemeListeKaydi {
  id: number;
  no: string;
  tarih: string;
  tip: string;
  cariKartId: number;
  cariUnvan: string;
  odemeYontemi: string;
  tutar: number;
  durum: string;
  kaynak: string;
  aciklama: string;
}

export interface TahsilatOdemeOzet {
  toplamTahsilat: number;
  tahsilatAdedi: number;
  toplamOdeme: number;
  odemeAdedi: number;
  bekleyen: number;
  bekleyenAdedi: number;
}

export interface TahsilatOdemeEkranVerisi {
  aktifIsletme: string;
  hareketler: TahsilatOdemeListeKaydi[];
  cariler: TahsilatOdemeCariSecenek[];
  faturalar: TahsilatOdemeFaturaSecenek[];
  ozet: TahsilatOdemeOzet;
  islemTipleri: TahsilatOdemeSecenek[];
  odemeYontemleri: TahsilatOdemeSecenek[];
  paraBirimleri: TahsilatOdemeSecenek[];
  kategoriler: TahsilatOdemeSecenek[];
  bugun: string;
}

export interface TahsilatOdemeFormu {
  islemTipi: string;
  cariKartId: string;
  tarih: string;
  odemeYontemi: string;
  vadeVar: boolean;
  vadeTarihi: string;
  aciklama: string;
  tutar: string;
  paraBirimi: string;
  referansNo: string;
  kategori: string;
  faturaId: string;
  faturaIleEslestir: boolean;
  hizliNot: string;
}
