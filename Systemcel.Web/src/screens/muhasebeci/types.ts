import type { BelgeSaglikOzeti } from "../dashboard/types";

export type YetkiSeviyesi = "OkumaRapor" | "TamIslem";

export interface Entitlement {
  planAdi: string;
  planKodu: string;
  aylikTutar: number;
  paraBirimi: string;
  aiAktif: boolean;
  aiMesajLimiti?: number | null;
  aiSinirsiz?: boolean;
  musteriLimiti?: number | null;
  musteriSinirsiz?: boolean;
  aktifMusteriSayisi?: number | null;
  oneCikmaAktif: boolean;
  muhasebeciProOnerilir: boolean;
}

export interface MuhasebeciProfil {
  muhasebeciIsletmeId: number;
  yayinda: boolean;
  unvan: string;
  konum: string;
  telefon: string;
  deneyimYili: number;
  profilResmiUrl: string;
  ucretBilgisi: string;
  uzmanliklar: string;
  musteriTipleri: string;
  kisaAciklama: string;
  planAdi: string;
  pro: boolean;
}

export interface MuhasebeciMusteri {
  isletmeId: number;
  ad: string;
  konum: string;
  yetkiSeviyesi: YetkiSeviyesi;
  durum: string;
  baslangicAt: string;
  belgeSagligi?: BelgeSaglikOzeti | null;
}

export interface MuhasebeciTalep {
  id: number;
  muhasebeciAdi: string;
  musteriAdi: string;
  tur: string;
  durum: string;
  yetkiSeviyesi: YetkiSeviyesi;
  davetKodu: string;
  davetLinki: string;
  mesaj: string;
  createdAt: string;
  aylikHizmetBedeli?: number;
  odemeDurumu?: string;
  odemeYapilabilir?: boolean;
}

export interface MuhasebeciPanel {
  hazir: boolean;
  muhasebeciIsletmeId: number;
  muhasebeciAdi: string;
  mesaj: string;
  entitlement?: Entitlement | null;
  profil?: MuhasebeciProfil | null;
  musteriler: MuhasebeciMusteri[];
  bekleyenTalepler: MuhasebeciTalep[];
  davetler: MuhasebeciTalep[];
}
