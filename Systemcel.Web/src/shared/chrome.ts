export interface IsletmeSecenek {
  id: number;
  ad: string;
  aktif: boolean;
}

export interface SohbetBildirim {
  muhasebeciIsletmeId: number;
  musteriIsletmeId: number;
  talepId?: number | null;
  baglantiId?: number | null;
  baslik: string;
  sonMesaj: string;
  sonMesajAt: string;
  okunmamisMesajSayisi: number;
  hedefUrl: string;
}

export interface SohbetBildirimDurumu {
  okunmamisMesajSayisi: number;
  sohbetler: SohbetBildirim[];
}

export interface UstBarDurumu {
  aktifIsletmeId: number;
  aktifIsletme: string;
  hesapTipi: string;
  muhasebeciMusteriBaglami: boolean;
  muhasebeciIsletmeId?: number | null;
  muhasebeciAdi: string;
  muhasebeciYetkiSeviyesi: string;
  bildirimVar: boolean;
  bildirimSayisi: number;
  sohbet?: SohbetBildirimDurumu;
  yoneticiMi?: boolean;
  telegramAktif: boolean;
  isletmeler: IsletmeSecenek[];
}
