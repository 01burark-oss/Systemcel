export interface RaporSecim {
  deger: string;
  etiket: string;
  secili: boolean;
}

export interface RaporSecenek {
  deger: string;
  etiket: string;
}

export interface RaporPaket {
  varMi: boolean;
  ad: string;
  yol: string;
  klasor: string;
  donem: string;
  olusturmaZamani: string;
}

export interface RaporlarEkranVerisi {
  aktifIsletme: string;
  bugun: string;
  varsayilanDonem: string;
  varsayilanKlasor: string;
  formatlar: RaporSecim[];
  icerikler: RaporSecim[];
  yazdirmaSablonlari: RaporSecenek[];
  tarihAraliklari: RaporSecenek[];
  sonPaket: RaporPaket | null;
}

export interface RaporYazdirFormu {
  sablon: string;
  aralikKodu: string;
  baslangic: string;
  bitis: string;
  notMetni: string;
}
