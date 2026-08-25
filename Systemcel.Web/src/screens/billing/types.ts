export interface AbonelikHaklari {
  planKodu: string;
  planAdi: string;
  kaynak: string;
  aylikTutar: number;
  yillikTutar: number;
  faturalamaDonemi: string;
  donemTutari: number;
  paraBirimi: string;
  aiAktif: boolean;
  aiMesajLimiti: number | null;
  kullaniciLimiti: number | null;
  faturaLimiti: number | null;
  isletmeLimiti: number | null;
  gelirGiderIslemLimiti: number | null;
  cariKartLimiti: number | null;
  urunHizmetLimiti: number | null;
  musteriLimiti: number | null;
  ekMusteriKredisi: number;
  saltOkunur: boolean;
  gecerliBitisAt: string | null;
}

export interface DenemeOzeti {
  planKodu: string;
  faturalamaDonemi: string;
  ekMusteriKredisi: number;
  durum: string;
  baslangicAt: string;
  bitisAt: string;
  odemeYontemiEklendi: boolean;
  donemSonundaIptal: boolean;
  iptalAt: string | null;
}

export interface AbonelikKaydi {
  planKodu: string;
  faturalamaDonemi: string;
  ekMusteriKredisi: number;
  durum: string;
  donemTutari: number;
  kampanyaKodu: string;
  yenilemeDonemTutari: number;
  indirimliDonemKalan: number;
  paraBirimi: string;
  donemBaslangicAt: string;
  donemBitisAt: string;
  toleransBitisAt: string | null;
  donemSonundaIptal: boolean;
  iptalAt: string | null;
  planlananPlanKodu: string;
  planlananFaturalamaDonemi: string;
  planlananEkMusteriKredisi: number | null;
  planlananDegisiklikAt: string | null;
}

export interface OdemeKaydi {
  id: number;
  islemTipi: string;
  durum: string;
  planKodu: string;
  faturalamaDonemi: string;
  kampanyaKodu: string;
  netTutar: number;
  listeNetTutar: number;
  yenilemeNetTutar: number;
  kdvTutar: number;
  toplamTutar: number;
  paraBirimi: string;
  hataKodu: string;
  createdAt: string;
  tamamlandiAt: string | null;
}

export interface AbonelikOzeti {
  isletmeId: number;
  isletmeAdi: string;
  hesapTipi: string;
  haklar: AbonelikHaklari;
  durum: string;
  sonrakiYenilemeAt: string | null;
  donemSonundaIptal: boolean;
  iptalEdilebilir: boolean;
  deneme: DenemeOzeti | null;
  abonelik: AbonelikKaydi | null;
  odemeler: OdemeKaydi[];
}

export interface PublicPlan {
  kod: string;
  ad: string;
  hesapTipi: string;
  aylikTutar: number;
  yillikTutar: number | null;
  yillikEfektifAylikTutar: number | null;
  normalAylikTutar: number;
  normalYillikTutar: number | null;
  kurucuAylikTutar: number;
  kurucuYillikTutar: number | null;
  kampanyaKodu: string;
  kurucuKontenjanKalan: number;
  paraBirimi: string;
  denemeGunSayisi: number;
}

export interface EntitlementProblem {
  code?: string;
  detail?: string;
  suggestedPlanCode?: string;
}

export interface PaymentQuote {
  planCode: string;
  accountType: string;
  billingPeriod: string;
  currency: string;
  netAmount: number;
  vatRate: number;
  vatAmount: number;
  totalAmount: number;
  trialDays: number;
  extraCustomerCredits: number;
  includedCustomerCount: number;
  customerCreditUnitAmount: number;
  campaignCode: string;
  isFounderPrice: boolean;
  listNetAmount: number;
  renewalNetAmount: number;
  discountedPeriodCount: number;
  fullPeriodNetAmount: number;
  prorationCreditNetAmount: number;
  changeType: "YeniAbonelik" | "AnindaYukseltme" | "DonemSonuDegisiklik";
  effectiveAt: string | null;
  targetPeriodEndAt: string | null;
}

export interface TeklifYaniti {
  fiyat: PaymentQuote;
  kampanyaKodu: string;
  onayMetniSurumu: string;
  onayMetni: string;
}

export interface CheckoutYaniti {
  odemeIslemiId: number | null;
  checkoutUrl: string | null;
  expiresAt: string | null;
  firstChargeAt: string | null;
  reused: boolean;
  scheduled: boolean;
  effectiveAt: string | null;
  fiyat: PaymentQuote;
}
