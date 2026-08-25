export interface GibPortalEkranVerisi {
  aktifIsletme: string;
  kullaniciKodu: string;
  hasPassword: boolean;
  testModu: boolean;
  sonIslemler: GibPortalIslem[];
  mesaj: string;
}

export interface GibPortalIslem {
  id: number;
  faturaId: number | null;
  tarih: string;
  islem: string;
  basarili: boolean;
  mesaj: string;
}

export interface GibPortalTestSonucu {
  basarili: boolean;
  mesaj: string;
}
