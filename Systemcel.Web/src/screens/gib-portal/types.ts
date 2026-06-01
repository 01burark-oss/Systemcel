export interface GibPortalEkranVerisi {
  aktifIsletme: string;
  kullaniciKodu: string;
  hasPassword: boolean;
  testModu: boolean;
  mesaj: string;
}

export interface GibPortalTestSonucu {
  basarili: boolean;
  mesaj: string;
}
