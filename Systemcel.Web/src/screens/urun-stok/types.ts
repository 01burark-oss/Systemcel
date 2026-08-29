export interface StokSecenek {
  deger: string;
  etiket: string;
}

export interface UrunListeKaydi {
  id: number;
  tip: string;
  ad: string;
  barkod: string;
  birim: string;
  kdvOrani: number;
  alisFiyati: number;
  satisFiyati: number;
  kritikStok: number;
  mevcutStok: number;
  aktif: boolean;
}

export interface StokHareketKaydi {
  id: number;
  urunHizmetId: number;
  urunAdi: string;
  tarih: string;
  hareketTipi: string;
  miktar: number;
  kaynak: string;
  aciklama: string;
}

export interface UrunStokEkranVerisi {
  aktifIsletme: string;
  urunler: UrunListeKaydi[];
  sonHareketler: StokHareketKaydi[];
  tipSecenekleri: StokSecenek[];
  birimSecenekleri: StokSecenek[];
}

export interface UrunFormu {
  id: number;
  tip: string;
  ad: string;
  barkod: string;
  birim: string;
  kdvOrani: string;
  alisFiyati: string;
  satisFiyati: string;
  kritikStok: string;
  aktif: boolean;
}

export interface StokHareketFormu {
  miktar: string;
  birimMaliyet: string;
  tarih: string;
  aciklama: string;
}

export interface HizliSatisSepetSatiri extends UrunListeKaydi {
  miktar: number;
}

export interface HizliSatisSonucu {
  mesaj: string;
  faturaId: number;
  faturaNo: string;
  toplam: number;
  tekrarlandi: boolean;
}
