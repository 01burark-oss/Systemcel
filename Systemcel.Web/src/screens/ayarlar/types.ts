export interface AyarDil {
  kod: string;
  ad: string;
}

export interface AyarIsletme {
  id: number;
  ad: string;
  aktif: boolean;
}

export interface AyarKalem {
  id: number;
  tip: "Gelir" | "Gider" | string;
  ad: string;
}

export interface AyarlarEkranVerisi {
  aktifIsletmeId: number;
  aktifIsletme: string;
  seciliIsletmeId: number;
  seciliKalemId: number | null;
  dil: string;
  diller: AyarDil[];
  isletmeler: AyarIsletme[];
  kalemler: AyarKalem[];
  mesaj: string;
}
