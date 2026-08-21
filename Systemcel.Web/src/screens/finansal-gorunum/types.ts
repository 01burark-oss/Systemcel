export interface AlacakYaslandirmaDilimi {
  kod: string;
  etiket: string;
  tutar: number;
  faturaAdedi: number;
  oran: number;
}

export interface AlacakYogunlasmaOzeti {
  enBuyukCariOrani: number;
  ilkUcCariOrani: number;
  ilkBesCariOrani: number;
  hhi: number;
  riskSeviyesi: string;
}

export interface CariAlacakYaslandirma {
  cariKartId: number;
  unvan: string;
  toplam: number;
  vadesiGelmemis: number;
  gun1Ila30: number;
  gun31Ila60: number;
  gun61Ila90: number;
  gun91VeUzeri: number;
  acikFaturaAdedi: number;
  enUzunGecikmeGunu: number;
  toplamdakiOrani: number;
}

export interface CariOdemeRitmi {
  cariKartId: number;
  unvan: string;
  acikAlacak: number;
  vadesiGecmisAlacak: number;
  enUzunGecikmeGunu: number;
  acikAlacakOrani: number;
  ortalamaOdemeSapmasiGunu: number | null;
  ortancaOdemeSapmasiGunu: number | null;
  ortalamaOdemeSuresiGunu: number | null;
  ortancaOdemeSuresiGunu: number | null;
  zamanindaOdemeOrani: number | null;
  odemeAraligiOrtancasiGunu: number | null;
  sonDonemDegisimiGunu: number | null;
  sonDonemOrnekAdedi: number;
  oncekiDonemOrnekAdedi: number;
  tamamlananOdemeAdedi: number;
  ritimDurumu: string;
  riskSeviyesi: string;
}

export interface NakitProjeksiyonHaftasi {
  hafta: number;
  baslangic: string;
  bitis: string;
  acilisBakiyesi: number;
  beklenenTahsilat: number;
  planlananGelir: number;
  beklenenOdeme: number;
  planlananGider: number;
  netDegisim: number;
  kapanisBakiyesi: number;
}

export interface FinansalVeriUyarisi {
  kod: string;
  mesaj: string;
  kayitAdedi: number;
}

export interface FinansalGorunumEkranVerisi {
  referansTarihi: string;
  paraBirimi: string;
  kasaBakiyesi: number;
  acikAlacakToplami: number;
  vadesiGecmisAlacakToplami: number;
  yaslandirma: AlacakYaslandirmaDilimi[];
  cariYaslandirma: CariAlacakYaslandirma[];
  yogunlasma: AlacakYogunlasmaOzeti;
  cariRiskleri: CariOdemeRitmi[];
  nakitProjeksiyonu: NakitProjeksiyonHaftasi[];
  ilkNegatifHafta: number | null;
  veriUyarilari: FinansalVeriUyarisi[];
}

export interface PlanlananNakitKalemi {
  id: number;
  isletmeId: number;
  ad: string;
  tip: string;
  tutar: number;
  ilkTarih: string;
  tekrarTipi: string;
  tekrarAraligi: number;
  bitisTarihi: string | null;
  kategori: string;
  aciklama: string | null;
  aktif: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface PlanlananNakitFormu {
  id: number;
  ad: string;
  tip: "Gelir" | "Gider";
  tutar: string;
  ilkTarih: string;
  tekrarTipi: "TekSefer" | "Haftalik" | "Aylik";
  tekrarAraligi: string;
  bitisTarihi: string;
  kategori: string;
  aciklama: string;
  aktif: boolean;
}

export type PlanlananNakitListeYaniti =
  | PlanlananNakitKalemi[]
  | { planlar: PlanlananNakitKalemi[] };
