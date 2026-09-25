import React from "react";
import { Check, PackagePlus, PackageSearch, Plus, Printer, QrCode, ScanLine, Search, ShieldCheck, ShoppingCart, Store, Truck, Upload, X } from "lucide-react";
import { jsonOku } from "../../shared/json";
import "./supplier-marketplace.css";

type Profil = {
  id: number; unvan: string; kategoriler: string; sehir: string; aciklama: string; vergiNo?: string; mersisNo?: string;
  kepAdresi?: string; iban?: string; adres?: string; yetkiliAdSoyad?: string; vergiDurumu?: string; sevkiyatBolgeleri?: string;
  iadeKosullari?: string; pazaryeriSozlesmeVersiyonu?: string; tevkifatMuaf?: boolean; dogrulamaDurumu?: string;
  dogrulamaNotu?: string; dogrulandi: boolean; yayinda?: boolean;
};
type Urun = {
  id: number; tedarikciProfilId: number; tedarikciUnvani: string; tedarikciSehri: string; sevkiyatBolgeleri: string;
  iadeKosullari: string; sku: string; ad: string; aciklama: string; kategori: string; birim: string; birimFiyat: number;
  kdvOrani: number; paraBirimi: string; kullanilabilirStok: number; minimumSiparisMiktari: number; tahminiTeslimatGun: number;
};
type BenimUrunum = { id: number; kaynakUrunHizmetId?: number; sku: string; ad: string; aciklama?: string; kategori: string; birim: string; birimFiyat?: number; kdvOrani?: number; paraBirimi?: string; stokMiktari: number; minimumSiparisMiktari?: number; tahminiTeslimatGun?: number; rezerveMiktar: number; aktif: boolean };
type AnaSiparis = { id: number; siparisNo: string; teslimatAdresi: string; araToplam: number; kdvToplam: number; genelToplam: number; paraBirimi: string; durum: string; createdAt: string };
type TedarikciSiparis = {
  id: number; anaSiparisId: number; anaSiparisNo: string; siparisNo: string; teslimatAdresi: string; aliciIsletmeId: number;
  tedarikciIsletmeId: number; tedarikciUnvani: string; araToplam: number; kdvToplam: number; genelToplam: number;
  paraBirimi: string; komisyonTutari: number; komisyonKdvTutari: number; tevkifatTutari: number; odemeHizmetiBedeli: number;
  tedarikciHakEdisi: number; durum: string; kargoFirmasi: string; kargoTakipNo: string; createdAt: string; malKabulVar: boolean;
};
type SiparisKalemi = { id: number; tedarikciSiparisId: number; tedarikciUrunId: number; sku: string; ad: string; birim: string; miktar: number; sevkEdilenMiktar: number; kabulEdilenMiktar: number; reddedilenMiktar: number; birimFiyat: number; kdvOrani: number; toplamTutar: number };
type Sevkiyat = { id: number; tedarikciSiparisId: number; sevkiyatNo: string; tasimaTipi: string; tasiyici: string; belgeNo: string; belgeUuid: string; belgeDosyaYolu: string; sevkAt?: string; varisDeposu?: number; randevuAt?: string; planlananTeslimAt?: string; durum: string; etiketSayisi: number; okutulanEtiketSayisi: number };
type Hakedis = { id: number; tedarikciSiparisId: number; siparisNo: string; brutTutar: number; netTutar: number; odenenTutar: number; iadeTutari: number; paraBirimi: string; durum: string; planlananAt: string; tamamlandiAt?: string };
type MalKabulHareketi = { id: number; tedarikciSiparisId: number; kabulEdilenMiktar: number; reddedilenMiktar: number; kabulBrutTutar: number; serbestBirakilanNetTutar: number; hakEdisAktarimReferansi: string; hakEdisAktarimHatasi: string; createdAt: string };
type QrEtiketi = { id: number; kod: string; qrIcerigi: string; miktar: number; urunAdi: string; sku: string; birim: string; lotNo: string; sonKullanmaTarihi?: string };
type QrCozumu = { kod: string; siparisId: number; siparisNo: string; urunAdi: string; sku: string; birim: string; miktar: number; lotNo: string; sonKullanmaTarihi?: string; durum: string; sicaklikMin?: number | null; sicaklikMax?: number | null };
type Talep = { id: number; baslik: string; kategori: string; urunHizmet: string; miktar: number; birim: string; teslimatSehri: string; sonTeklifAt: string; aciklama: string; durum?: string; teklifSayisi?: number; teklifVerildi?: boolean };
type Teklif = { id: number; talepId: number; talepBasligi: string; birimFiyat: number; kdvOrani: number; paraBirimi: string; terminGun: number; minimumSiparis: number; not: string; durum: string; tedarikciUnvani: string };
type YonetimProfili = { id: number; unvan: string; vergiNo: string; iban: string; adres: string; yetkiliAdSoyad: string; kategoriler: string; sehir: string; dogrulamaDurumu: string };
type YonetimSiparisi = { id: number; siparisNo: string; tedarikciUnvani: string; durum: string; genelToplam: number; paraBirimi: string; hakedisDurumu: string; planlananAt?: string; yonetimNedeni?: string; itirazYasiSaat?: number | null; tedarikciIlkYanitSuresiSaat?: number | null };
type Sikayet = { id: number; tedarikciSiparisId: number; siparisNo: string; tedarikciUnvani: string; aliciIsletmeId: number; tedarikciIsletmeId: number; kategori: string; aciklama: string; talep: string; durum: string; tedarikciYaniti: string; kapanisNotu: string; createdAt: string; updatedAt: string };
type Degerlendirme = { id: number; tedarikciSiparisId: number; urunUygunluguPuani: number; eksiksizTeslimatPuani: number; hasarsizTeslimatPuani: number; zamanindaTeslimatPuani: number; sorunCozmePuani?: number; ortalamaPuan: number; yorum: string };
type TedarikciPerformansi = { tedarikciProfilId: number; tedarikciIsletmeId: number; puan: number; degerlendirmeSayisi: number; sorunBildirimiSayisi: number };
type SubeSecenegi = { id: number; ad: string; kod: string; varsayilan: boolean };
type DepoSecenegi = { id: number; subeId?: number; ad: string; kod: string; varsayilan: boolean };
type OperasyonMetrikleri = { zamanindaTeslimOrani: number; kabulOrani: number; eksikOrani: number; hasarOrani: number; itirazOrani: number; ortalamaKabulSuresiSaat: number; ortalamaHakedisSuresiSaat: number };
type SiparisReferanslari = {
  paymentAllocations: Array<{ id: number; saglayici: string; saglayiciIslemId: string; durum: string; tutar: number; brutTutar: number; iadeTutari: number }>;
  invoiceMatch: { aliciFaturaId: number; saticiFaturaId: number; tedarikciBelgeNo: string; tedarikciBelgeUuid: string } | null;
  invoices: Array<{ id: number; isletmeId: number; faturaTipi: string; yerelFaturaNo: string; portalBelgeNo: string; genelToplam: number; durum: string }>;
  settlement: { id: number; durum: string; netTutar: number; odenenTutar: number; aktarimReferansi: string } | null;
  stockMovements: Array<{ id: number; isletmeId: number; tedarikciSevkiyatId: number | null; tedarikciMalKabulId: number | null; hareketTipi: string; miktar: number }>;
  cariMovements: Array<{ id: number; isletmeId: number; tedarikciMalKabulId: number | null; hareketTipi: string; tutar: number }>;
  paymentMovements: Array<{ id: number; faturaId: number; tedarikciMalKabulId: number | null; tip: string; tutar: number }>;
};
type StokUzlastirmaKarari = "Kayip" | "TedarikciyeIade" | "YenidenSevk";
type ItirazIncelemesi = { shipments: Array<{ id: number; sevkiyatNo: string; belgeNo: string; belgeUuid: string; belgeDosyaYolu: string; durum: string }>; receipts: Array<{ id: number; kabulEdilenMiktar: number; reddedilenMiktar: number; redNedeni: string; ikinciRedNedeni?: string; not: string; islemYapanKullaniciRef: string; belgeKarmasi: string; fotoKanitiYolu: string; createdAt: string; stokUzlastirmaDurumu?: string | null; stokUzlastirmaNotu?: string | null; stokUzlastirmaAt?: string | null }>; complaints: Array<{ kategori: string; aciklama: string; durum: string; tedarikciYaniti: string }>; history: Array<{ oncekiDurum: string; yeniDurum: string; aciklama: string; createdAt: string }>; references?: SiparisReferanslari };
type Ekran = {
  aktifIsletmeId: number; profiller: Profil[]; talepler: Talep[]; acikTalepler: Talep[]; gelenTeklifler: Teklif[]; profil: Profil | null;
  urunler: Urun[]; benimUrunlerim: BenimUrunum[]; kaynakUrunler: Array<{ id: number; ad: string }>;
  anaSiparisler: AnaSiparis[]; siparisler: TedarikciSiparis[]; siparisKalemleri: SiparisKalemi[];
  sevkiyatlar?: Sevkiyat[]; hakedisler?: Hakedis[]; malKabulHareketleri?: MalKabulHareketi[];
  sikayetler?: Sikayet[]; degerlendirmeler?: Degerlendirme[]; tedarikciPerformanslari?: TedarikciPerformansi[];
  guvenliOdemeHazir?: boolean; malKabulYetkisi?: boolean; subeler?: SubeSecenegi[]; depolar?: DepoSecenegi[];
  operasyonMetrikleri?: OperasyonMetrikleri;
  yonetici?: boolean; yonetimProfilleri?: YonetimProfili[]; yonetimSiparisler?: YonetimSiparisi[];
};
type BarcodeDetectorApi = { detect(source: ImageBitmap): Promise<Array<{ rawValue: string }>> };
type BarcodeDetectorCtor = new (options?: { formats?: string[] }) => BarcodeDetectorApi;
type ModalName = "" | "profil" | "urun" | "talep" | "teklif" | "kabul" | "kargo" | "etiketler" | "qrKabul" | "fatura" | "dogrulama" | "itiraz" | "hakedis" | "sikayet" | "sikayetYanit" | "sikayetSonuc" | "degerlendirme";

const emptyProfile = { unvan: "", kategoriler: "", sehir: "", aciklama: "", vergiNo: "", mersisNo: "", kepAdresi: "", iban: "", adres: "", yetkiliAdSoyad: "", vergiDurumu: "", sevkiyatBolgeleri: "", iadeKosullari: "", pazaryeriSozlesmeVersiyonu: "1.0", tevkifatMuaf: false, yayinda: true };
const emptyProduct = { kaynakUrunHizmetId: "", sku: "", ad: "", aciklama: "", kategori: "", birim: "Adet", birimFiyat: "", kdvOrani: "20", paraBirimi: "TRY", stokMiktari: "", minimumSiparisMiktari: "1", tahminiTeslimatGun: "2", aktif: true };
const emptyRequest = { baslik: "", kategori: "", urunHizmet: "", miktar: "", birim: "Adet", teslimatSehri: "", sonTeklifAt: "", aciklama: "" };
const emptyOffer = { birimFiyat: "", kdvOrani: "20", paraBirimi: "TRY", terminGun: "", minimumSiparis: "", not: "" };
const receiptRejectReasons = [
  { value: "", label: "Ret nedeni seçin" },
  { value: "Eksik ürün", label: "Eksik ürün" },
  { value: "Hasarlı ürün", label: "Hasarlı ürün" },
  { value: "Yanlış ürün", label: "Yanlış ürün" },
  { value: "Kalite uygunsuzluğu", label: "Kalite uygunsuzluğu" },
  { value: "Sıcaklık uygunsuzluğu", label: "Sıcaklık uygunsuzluğu" },
  { value: "Lot veya seri uyuşmazlığı", label: "Lot veya seri uyuşmazlığı" },
  { value: "Son kullanma tarihi uygunsuzluğu", label: "Son kullanma tarihi uygunsuzluğu" },
  { value: "Belge uyuşmazlığı", label: "Belge uyuşmazlığı" },
  { value: "Diğer", label: "Diğer" }
];
const statusLabels: Record<string, string> = { OdemeBekliyor: "Ödeme bekliyor", SiparisVerildi: "Sipariş verildi", Odendi: "Ödendi", TedarikciOnayladi: "Tedarikçi onayladı", Hazirlaniyor: "Hazırlanıyor", SevkeHazir: "Sevke hazır", KismenSevkEdildi: "Kısmen sevk edildi", SevkEdildi: "Sevk edildi", MalKabulBekliyor: "Mal kabul bekliyor", KismenKabul: "Kısmen teslim alındı", TeslimEdildi: "Teslim edildi", HakEdisBekliyor: "Hakediş bekliyor", CariOdemeBekliyor: "Cari ödeme bekliyor", Tamamlandi: "Tamamlandı", IptalEdildi: "İptal edildi", KismiIptal: "Kısmen iptal edildi", IadeBekliyor: "İade bekliyor", IadeEdildi: "İade edildi", KismiIade: "Kısmen iade edildi", Itirazli: "İtirazlı", HakEdisKismenSerbest: "Hakediş kısmen serbest", HakEdisSerbest: "Hakediş serbest", AktarimBasarisiz: "Aktarım başarısız", MutabakatFarki: "Mutabakat farkı" };

export function TedarikciPazaryeriSayfasi() {
  const [data, setData] = React.useState<Ekran | null>(null);
  const [tab, setTab] = React.useState("catalog");
  const [modal, setModal] = React.useState<ModalName>("");
  const [selectedRequest, setSelectedRequest] = React.useState<Talep | null>(null);
  const [selectedOffer, setSelectedOffer] = React.useState<Teklif | null>(null);
  const [selectedOrder, setSelectedOrder] = React.useState<TedarikciSiparis | null>(null);
  const [selectedAdminOrder, setSelectedAdminOrder] = React.useState<YonetimSiparisi | null>(null);
  const [disputeReview, setDisputeReview] = React.useState<ItirazIncelemesi | null>(null);
  const [disputeReviewError, setDisputeReviewError] = React.useState("");
  const [stockReconciliationDrafts, setStockReconciliationDrafts] = React.useState<Record<number, { decision: StokUzlastirmaKarari | ""; note: string }>>({});
  const [stockReconciliationBusyId, setStockReconciliationBusyId] = React.useState<number | null>(null);
  const [stockReconciliationError, setStockReconciliationError] = React.useState("");
  const stockReconciliationKeyRef = React.useRef(new Map<number, { fingerprint: string; key: string }>());
  const [selectedProfile, setSelectedProfile] = React.useState<YonetimProfili | null>(null);
  const [selectedComplaint, setSelectedComplaint] = React.useState<Sikayet | null>(null);
  const [query, setQuery] = React.useState("");
  const [cart, setCart] = React.useState<Record<number, number>>({});
  const [deliveryAddress, setDeliveryAddress] = React.useState("");
  const [profileForm, setProfileForm] = React.useState(emptyProfile);
  const [productForm, setProductForm] = React.useState(emptyProduct);
  const [editingProductId, setEditingProductId] = React.useState<number | null>(null);
  const [requestForm, setRequestForm] = React.useState(emptyRequest);
  const [offerForm, setOfferForm] = React.useState(emptyOffer);
  const [shippingForm, setShippingForm] = React.useState({ tasimaTipi: "Tedarikçi aracı", tasiyici: "", belgeNo: "", belgeUuid: "", aracPlaka: "", surucuAdi: "", cikisDeposu: "", varisDeposu: "", sevkAt: "", randevuAt: "", planlananTeslimAt: "", not: "" });
  const [shipmentLines, setShipmentLines] = React.useState<Record<number, { miktar: string; etiketSayisi: string; lotNo: string; sonKullanmaTarihi: string; sicaklikMin: string; sicaklikMax: string; seriNo: string; agirlik: string; paletKoli: string }>>({});
  const [shippingDocumentFile, setShippingDocumentFile] = React.useState<File | null>(null);
  const [qrLabels, setQrLabels] = React.useState<QrEtiketi[]>([]);
  const [qrCode, setQrCode] = React.useState("");
  const [qrResolution, setQrResolution] = React.useState<QrCozumu | null>(null);
  const [receiptForm, setReceiptForm] = React.useState({ kabulEdilenMiktar: "", reddedilenMiktar: "0", redNedeni: "", digerAciklama: "", ikinciRedNedeni: "", ikinciDigerAciklama: "", not: "", subeId: "", depoId: "", olculenAgirlik: "", olculenSicaklik: "" });
  const [receiptEvidenceFile, setReceiptEvidenceFile] = React.useState<File | null>(null);
  const [invoiceForm, setInvoiceForm] = React.useState({ belgeNo: "", belgeUuid: "" });
  const [acceptAddress, setAcceptAddress] = React.useState("");
  const [verificationForm, setVerificationForm] = React.useState({ onaylandi: true, not: "", komisyonOrani: "8", odemeVadesiGun: "7", pspAltUyeIsyeriId: "" });
  const [adminNote, setAdminNote] = React.useState("");
  const [disputeDecision, setDisputeDecision] = React.useState("TedarikciyeAktar");
  const [disputeSupplierAmount, setDisputeSupplierAmount] = React.useState("");
  const [complaintForm, setComplaintForm] = React.useState({ kategori: "Eksik", aciklama: "", talep: "EksigiTamamla" });
  const [complaintResponse, setComplaintResponse] = React.useState("");
  const [complaintOutcome, setComplaintOutcome] = React.useState({ cozuldu: true, not: "" });
  const [ratingForm, setRatingForm] = React.useState({ urunUygunluguPuani: "5", eksiksizTeslimatPuani: "5", hasarsizTeslimatPuani: "5", zamanindaTeslimatPuani: "5", sorunCozmePuani: "", yorum: "" });
  const [message, setMessage] = React.useState("");
  const [error, setError] = React.useState("");
  const [busy, setBusy] = React.useState(false);
  const [cameraOpen, setCameraOpen] = React.useState(false);
  const [cameraError, setCameraError] = React.useState("");
  const cameraVideoRef = React.useRef<HTMLVideoElement>(null);
  const cameraStreamRef = React.useRef<MediaStream | null>(null);

  const load = React.useCallback(async () => {
    try {
      setError("");
      const next = await jsonOku<Ekran>("/api/ekran/tedarikci-pazaryeri");
      setData(next);
      if (next.profil) setProfileForm({ ...emptyProfile, ...next.profil, tevkifatMuaf: Boolean(next.profil.tevkifatMuaf), yayinda: Boolean(next.profil.yayinda) });
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Pazaryeri yüklenemedi.");
    }
  }, []);

  React.useEffect(() => {
    document.title = "Tedarikçi Pazaryeri | Systemcel";
    const qr = new URLSearchParams(window.location.search).get("qr");
    if (qr) { setQrCode(qr); setModal("qrKabul"); }
    void load();
  }, [load]);

  async function send<T>(url: string, method: "POST" | "PUT", body?: unknown) {
    setBusy(true); setError("");
    try { return await jsonOku<T>(url, { method, body: body ? JSON.stringify(body) : undefined }); }
    finally { setBusy(false); }
  }
  async function complete(action: () => Promise<{ mesaj?: string }>, fallback: string) {
    try { const result = await action(); setMessage(result.mesaj || fallback); setModal(""); await load(); return true; }
    catch (actionError) { setError(actionError instanceof Error ? actionError.message : "İşlem tamamlanamadı."); return false; }
  }

  const normalizedQuery = query.toLocaleLowerCase("tr");
  const products = (data?.urunler ?? []).filter((product) => `${product.ad} ${product.kategori} ${product.tedarikciUnvani} ${product.tedarikciSehri}`.toLocaleLowerCase("tr").includes(normalizedQuery));
  const suppliers = (data?.profiller ?? []).filter((profile) => `${profile.unvan} ${profile.kategoriler} ${profile.sehir}`.toLocaleLowerCase("tr").includes(normalizedQuery));
  const cartRows = Object.entries(cart).map(([id, quantity]) => ({ product: data?.urunler.find((item) => item.id === Number(id)), quantity })).filter((row): row is { product: Urun; quantity: number } => Boolean(row.product));
  const cartTotal = cartRows.reduce((sum, row) => sum + row.quantity * row.product.birimFiyat * (1 + row.product.kdvOrani / 100), 0);
  const supplierCount = new Set(cartRows.map((row) => row.product.tedarikciProfilId)).size;
  const performanceByProfile = new Map((data?.tedarikciPerformanslari ?? []).map((item) => [item.tedarikciProfilId, item]));

  function addToCart(product: Urun) {
    setCart((current) => ({ ...current, [product.id]: Math.min(current[product.id] ? current[product.id] + 1 : product.minimumSiparisMiktari, product.kullanilabilirStok) }));
    setMessage(`${product.ad} sepete eklendi.`);
  }
  function setCartQuantity(product: Urun, quantity: number) {
    setCart((current) => {
      if (quantity <= 0) { const next = { ...current }; delete next[product.id]; return next; }
      return { ...current, [product.id]: Math.min(quantity, product.kullanilabilirStok) };
    });
  }

  async function createOrder() {
    if (cartRows.length === 0 || !deliveryAddress.trim()) { setError("Teslimat adresini yazın ve sepete ürün ekleyin."); return; }
    try {
      const securePayment = Boolean(data?.guvenliOdemeHazir);
      const created = await send<{ id: number; mesaj: string }>("/api/ekran/tedarikci-pazaryeri/siparisler", "POST", { teslimatAdresi: deliveryAddress, idempotencyKey: `cart-${crypto.randomUUID()}`, kalemler: cartRows.map(({ product, quantity }) => ({ urunId: product.id, miktar: quantity })), vadeli: !securePayment });
      if (securePayment) await send(`/api/ekran/tedarikci-pazaryeri/siparisler/${created.id}/odeme`, "POST", { idempotencyKey: `payment-${crypto.randomUUID()}` });
      setCart({}); setDeliveryAddress(""); setMessage(securePayment ? "Ödeme güvenli biçimde alındı; teslimat onayına kadar tedarikçi hakedişi blokede." : created.mesaj); setTab("orders"); await load();
    } catch (orderError) { setError(orderError instanceof Error ? orderError.message : "Sipariş tamamlanamadı."); await load(); }
  }
  async function saveProfile() { await complete(() => send("/api/ekran/tedarikci-pazaryeri/profil/basvuru", "PUT", profileForm), "Tedarikçi başvurusu kaydedildi."); }
  async function saveProduct() {
    const body = { ...productForm, kaynakUrunHizmetId: productForm.kaynakUrunHizmetId ? Number(productForm.kaynakUrunHizmetId) : null, birimFiyat: Number(productForm.birimFiyat), kdvOrani: Number(productForm.kdvOrani), stokMiktari: Number(productForm.stokMiktari), minimumSiparisMiktari: Number(productForm.minimumSiparisMiktari), tahminiTeslimatGun: Number(productForm.tahminiTeslimatGun) };
    const url = editingProductId === null ? "/api/ekran/tedarikci-pazaryeri/urunler" : `/api/ekran/tedarikci-pazaryeri/urunler/${editingProductId}`;
    await complete(() => send(url, editingProductId === null ? "POST" : "PUT", body), editingProductId === null ? "Ürün yayınlandı." : "Ürün güncellendi.");
    setProductForm(emptyProduct); setEditingProductId(null);
  }
  function editProduct(product: BenimUrunum) {
    setEditingProductId(product.id);
    setProductForm({ ...emptyProduct, ...product, kaynakUrunHizmetId: product.kaynakUrunHizmetId?.toString() ?? "", birimFiyat: product.birimFiyat?.toString() ?? "", kdvOrani: product.kdvOrani?.toString() ?? "20", stokMiktari: product.stokMiktari.toString(), minimumSiparisMiktari: product.minimumSiparisMiktari?.toString() ?? "1", tahminiTeslimatGun: product.tahminiTeslimatGun?.toString() ?? "2" });
    setModal("urun");
  }
  async function deactivateProduct(product: BenimUrunum) {
    await complete(() => send(`/api/ekran/tedarikci-pazaryeri/urunler/${product.id}`, "PUT", { kaynakUrunHizmetId: product.kaynakUrunHizmetId ?? null, sku: product.sku, ad: product.ad, aciklama: product.aciklama ?? "", kategori: product.kategori, birim: product.birim, birimFiyat: product.birimFiyat ?? 0, kdvOrani: product.kdvOrani ?? 20, paraBirimi: product.paraBirimi ?? "TRY", stokMiktari: product.stokMiktari, minimumSiparisMiktari: product.minimumSiparisMiktari ?? 1, tahminiTeslimatGun: product.tahminiTeslimatGun ?? 0, aktif: false }), "Ürün pasife alındı.");
  }
  async function updateOrderState(order: TedarikciSiparis, durum: string) {
    await complete(() => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${order.id}/durum`, "POST", { durum, kargoFirmasi: "", kargoTakipNo: "", aciklama: "" }), "Sipariş güncellendi.");
  }
  async function cancelSupplierOrder(order: TedarikciSiparis) {
    await complete(() => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${order.id}/iptal`, "POST", { neden: "Sipariş iptali" }), "Tedarikçi siparişi iptal edildi.");
  }
  async function acceptOffer() {
    if (!selectedOffer) return;
    try {
      const securePayment = Boolean(data?.guvenliOdemeHazir);
      const created = await send<{ id: number; mesaj: string }>(`/api/ekran/tedarikci-pazaryeri/teklifler/${selectedOffer.id}/kabul`, "POST", { teslimatAdresi: acceptAddress, vadeli: !securePayment });
      if (securePayment) await send(`/api/ekran/tedarikci-pazaryeri/siparisler/${created.id}/odeme`, "POST", { idempotencyKey: `payment-${crypto.randomUUID()}` });
      setMessage(securePayment ? "Teklif siparişe dönüştürüldü; ödeme teslimat onayına kadar blokede." : created.mesaj);
      setModal(""); setSelectedOffer(null); setAcceptAddress(""); setTab("orders"); await load();
    } catch (offerError) { setError(offerError instanceof Error ? offerError.message : "Teklif siparişe dönüştürülemedi."); }
  }
  async function verifySupplier() {
    if (!selectedProfile) return;
    await complete(() => send(`/api/ekran/yonetim/tedarikci-profilleri/${selectedProfile.id}/dogrula`, "POST", { ...verificationForm, komisyonOrani: Number(verificationForm.komisyonOrani), odemeVadesiGun: Number(verificationForm.odemeVadesiGun) }), verificationForm.onaylandi ? "Tedarikçi onaylandı." : "Başvuru reddedildi.");
    setSelectedProfile(null);
  }
  async function resolveDispute() {
    if (!selectedAdminOrder) return;
    await complete(() => send(`/api/ekran/yonetim/tedarikci-siparisler/${selectedAdminOrder.id}/itiraz-coz`, "POST", { devamEt: true, not: adminNote, karar: disputeDecision, tedarikciyeAktarilacakTutar: disputeDecision === "KismiPaylas" ? Number(disputeSupplierAmount) : null }), "İtiraz kapatıldı.");
    setSelectedAdminOrder(null); setAdminNote(""); setDisputeDecision("TedarikciyeAktar"); setDisputeSupplierAmount("");
  }
  async function openDisputeReview(order: YonetimSiparisi) {
    setSelectedAdminOrder(order); setDisputeReview(null); setDisputeReviewError("");
    setStockReconciliationDrafts({}); setStockReconciliationError(""); stockReconciliationKeyRef.current.clear();
    setAdminNote(""); setModal("itiraz");
    try {
      setBusy(true); setError("");
      setDisputeReview(await jsonOku<ItirazIncelemesi>(`/api/ekran/yonetim/tedarikci-siparisler/${order.id}/inceleme`));
    } catch (reviewError) { setDisputeReviewError(reviewError instanceof Error ? reviewError.message : "İtiraz inceleme paketi yüklenemedi."); }
    finally { setBusy(false); }
  }

  function updateStockReconciliationDraft(receiptId: number, update: Partial<{ decision: StokUzlastirmaKarari | ""; note: string }>) {
    stockReconciliationKeyRef.current.delete(receiptId);
    setStockReconciliationDrafts((current) => ({
      ...current,
      [receiptId]: { decision: current[receiptId]?.decision ?? "", note: current[receiptId]?.note ?? "", ...update }
    }));
    setStockReconciliationError("");
  }

  async function reconcileReceiptStock(receiptId: number) {
    if (!selectedAdminOrder || !disputeReview) return;
    const draft = stockReconciliationDrafts[receiptId];
    if (!draft?.decision || !draft.note.trim()) {
      setStockReconciliationError("Karar seçin ve zorunlu karar notunu yazın.");
      return;
    }
    const fingerprint = JSON.stringify({ orderId: selectedAdminOrder.id, receiptId, decision: draft.decision, note: draft.note.trim() });
    const cachedKey = stockReconciliationKeyRef.current.get(receiptId);
    if (!cachedKey || cachedKey.fingerprint !== fingerprint) {
      stockReconciliationKeyRef.current.set(receiptId, { fingerprint, key: crypto.randomUUID() });
    }
    const idempotencyKey = stockReconciliationKeyRef.current.get(receiptId)!.key;
    setStockReconciliationBusyId(receiptId); setStockReconciliationError("");
    try {
      await send(`/api/ekran/yonetim/tedarikci-mal-kabulleri/${receiptId}/stok-uzlastir`, "POST", {
        idempotencyKey,
        karar: draft.decision,
        not: draft.note.trim()
      });
      stockReconciliationKeyRef.current.delete(receiptId);
      setDisputeReview((current) => current ? {
        ...current,
        receipts: current.receipts.map((item) => item.id === receiptId
          ? { ...item, stokUzlastirmaDurumu: draft.decision, stokUzlastirmaNotu: draft.note.trim(), stokUzlastirmaAt: new Date().toISOString() }
          : item)
      } : current);
      setStockReconciliationDrafts((current) => { const next = { ...current }; delete next[receiptId]; return next; });
      try {
        setDisputeReview(await jsonOku<ItirazIncelemesi>(`/api/ekran/yonetim/tedarikci-siparisler/${selectedAdminOrder.id}/inceleme`));
      } catch {
        setStockReconciliationError("Karar kaydedildi ancak inceleme verileri yenilenemedi. İnceleme penceresini yeniden açın.");
      }
    } catch (reconciliationError) {
      setStockReconciliationError(reconciliationError instanceof Error ? reconciliationError.message : "Stok uzlaştırma kararı kaydedilemedi.");
    } finally { setStockReconciliationBusyId(null); }
  }
  async function completeSettlement() {
    if (!selectedAdminOrder) return;
    await complete(() => send(`/api/ekran/yonetim/tedarikci-siparisler/${selectedAdminOrder.id}/hakedis`, "POST", { aktarimReferansi: adminNote }), "Hakediş tamamlandı.");
    setSelectedAdminOrder(null); setAdminNote("");
  }

  function openComplaint(order: TedarikciSiparis) {
    setSelectedOrder(order);
    setComplaintForm({ kategori: "Eksik", aciklama: "", talep: "EksigiTamamla" });
    setModal("sikayet");
  }

  async function createComplaint() {
    if (!selectedOrder) return;
    await complete(
      () => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${selectedOrder.id}/sikayetler`, "POST", complaintForm),
      "Sorun tedarikçiye iletildi."
    );
    setSelectedOrder(null);
  }

  async function respondToComplaint() {
    if (!selectedComplaint) return;
    await complete(
      () => send(`/api/ekran/tedarikci-pazaryeri/sikayetler/${selectedComplaint.id}/yanit`, "POST", { yanit: complaintResponse }),
      "Yanıt alıcıya iletildi."
    );
    setSelectedComplaint(null); setComplaintResponse("");
  }

  async function closeComplaint() {
    if (!selectedComplaint) return;
    await complete(
      () => send(`/api/ekran/tedarikci-pazaryeri/sikayetler/${selectedComplaint.id}/kapat`, "POST", complaintOutcome),
      "Sorunun sonucu kaydedildi."
    );
    setSelectedComplaint(null); setComplaintOutcome({ cozuldu: true, not: "" });
  }

  function openRating(order: TedarikciSiparis) {
    const existing = data?.degerlendirmeler?.find((item) => item.tedarikciSiparisId === order.id);
    setSelectedOrder(order);
    setRatingForm({
      urunUygunluguPuani: String(existing?.urunUygunluguPuani ?? 5),
      eksiksizTeslimatPuani: String(existing?.eksiksizTeslimatPuani ?? 5),
      hasarsizTeslimatPuani: String(existing?.hasarsizTeslimatPuani ?? 5),
      zamanindaTeslimatPuani: String(existing?.zamanindaTeslimatPuani ?? 5),
      sorunCozmePuani: existing?.sorunCozmePuani ? String(existing.sorunCozmePuani) : "",
      yorum: existing?.yorum ?? ""
    });
    setModal("degerlendirme");
  }

  async function saveRating() {
    if (!selectedOrder) return;
    const body = {
      urunUygunluguPuani: Number(ratingForm.urunUygunluguPuani),
      eksiksizTeslimatPuani: Number(ratingForm.eksiksizTeslimatPuani),
      hasarsizTeslimatPuani: Number(ratingForm.hasarsizTeslimatPuani),
      zamanindaTeslimatPuani: Number(ratingForm.zamanindaTeslimatPuani),
      sorunCozmePuani: ratingForm.sorunCozmePuani ? Number(ratingForm.sorunCozmePuani) : null,
      yorum: ratingForm.yorum
    };
    await complete(
      () => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${selectedOrder.id}/degerlendirme`, "PUT", body),
      "Değerlendirmeniz kaydedildi."
    );
    setSelectedOrder(null);
  }

  function openShipment(order: TedarikciSiparis) {
    const lines = data?.siparisKalemleri.filter((line) => line.tedarikciSiparisId === order.id && line.miktar > (line.sevkEdilenMiktar ?? 0)) ?? [];
    setSelectedOrder(order);
    setShipmentLines(Object.fromEntries(lines.map((line) => [line.id, { miktar: String(line.miktar - (line.sevkEdilenMiktar ?? 0)), etiketSayisi: "1", lotNo: "", sonKullanmaTarihi: "", sicaklikMin: "", sicaklikMax: "", seriNo: "", agirlik: "", paletKoli: "" }])));
    setShippingForm({ tasimaTipi: "Tedarikçi aracı", tasiyici: "", belgeNo: "", belgeUuid: "", aracPlaka: "", surucuAdi: "", cikisDeposu: "", varisDeposu: "", sevkAt: "", randevuAt: "", planlananTeslimAt: "", not: "" });
    setShippingDocumentFile(null);
    setModal("kargo");
  }

  async function createShipment() {
    if (!selectedOrder) return;
    const kalemler = Object.entries(shipmentLines).map(([id, line]) => ({ siparisKalemiId: Number(id), miktar: Number(line.miktar), etiketSayisi: Number(line.etiketSayisi), lotNo: line.lotNo, sonKullanmaTarihi: line.sonKullanmaTarihi || null, sicaklikMin: line.sicaklikMin === "" ? null : Number(line.sicaklikMin), sicaklikMax: line.sicaklikMax === "" ? null : Number(line.sicaklikMax), seriNo: line.seriNo, agirlik: line.agirlik ? Number(line.agirlik) : null, paletKoli: line.paletKoli ? Number(line.paletKoli) : 0 }));
    try {
      const result = await send<{ id: number; etiketler: QrEtiketi[] }>(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${selectedOrder.id}/sevkiyatlar`, "POST", { ...shippingForm, varisDeposu: shippingForm.varisDeposu ? Number(shippingForm.varisDeposu) : null, sevkAt: shippingForm.sevkAt || null, randevuAt: shippingForm.randevuAt || null, planlananTeslimAt: shippingForm.planlananTeslimAt || null, kalemler });
      if (shippingDocumentFile) {
        const formData = new FormData(); formData.append("file", shippingDocumentFile);
        await jsonOku(`/api/ekran/tedarikci-pazaryeri/sevkiyatlar/${result.id}/belge`, { method: "POST", body: formData });
      }
      setQrLabels(result.etiketler); setModal("etiketler"); setMessage("Sevkiyat ve ürün QR etiketleri oluşturuldu."); await load();
    } catch (shipmentError) { setError(shipmentError instanceof Error ? shipmentError.message : "Sevkiyat oluşturulamadı."); }
  }

  async function resolveQr(value = qrCode) {
    const normalized = value.trim();
    if (!normalized) { setError("QR kodunu okutun veya yazın."); return; }
    try {
      const result = await jsonOku<QrCozumu>(`/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/${encodeURIComponent(normalized)}`);
      const defaultWarehouse = data?.depolar?.find((item) => item.varsayilan) ?? data?.depolar?.[0];
      const defaultBranch = data?.subeler?.find((item) => item.id === defaultWarehouse?.subeId) ?? data?.subeler?.find((item) => item.varsayilan) ?? data?.subeler?.[0];
      setQrCode(result.kod); setQrResolution(result); setReceiptEvidenceFile(null); setReceiptForm({ kabulEdilenMiktar: String(result.miktar), reddedilenMiktar: "0", redNedeni: "", digerAciklama: "", ikinciRedNedeni: "", ikinciDigerAciklama: "", not: "", subeId: defaultBranch ? String(defaultBranch.id) : "", depoId: defaultWarehouse ? String(defaultWarehouse.id) : "", olculenAgirlik: "", olculenSicaklik: "" }); setError("");
    } catch (qrError) { setError(qrError instanceof Error ? qrError.message : "QR etiketi okunamadı."); }
  }

  async function scanQrImage(file: File) {
    let bitmap: ImageBitmap;
    try { bitmap = await createImageBitmap(file); } catch { setCameraError("QR görseli okunamadı. Kodu elle girmeyi deneyin."); return; }
    try {
      const Detector = (window as typeof window & { BarcodeDetector?: BarcodeDetectorCtor }).BarcodeDetector;
      let value = "";
      if (Detector) value = (await new Detector({ formats: ["qr_code"] }).detect(bitmap))[0]?.rawValue ?? "";
      if (!value) {
        const imageUrl = URL.createObjectURL(file);
        try { const { BrowserMultiFormatReader } = await import("@zxing/browser"); value = (await new BrowserMultiFormatReader().decodeFromImageUrl(imageUrl)).getText(); }
        finally { URL.revokeObjectURL(imageUrl); }
      }
      if (!value) throw new Error("QR kodu bulunamadı.");
      setQrCode(value); await resolveQr(value);
    } catch {
      setCameraError("QR görselinde kod bulunamadı. Kodu elle girin veya başka bir görsel seçin.");
    } finally { bitmap.close(); }
  }

  async function startCamera() {
    setCameraError("");
    if (!navigator.mediaDevices?.getUserMedia) { setCameraError("Bu cihazda canlı kamera kullanılamıyor. QR görseli yükleyin veya kodu elle girin."); return; }
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: "environment" } }, audio: false });
      cameraStreamRef.current = stream;
      setCameraOpen(true);
      requestAnimationFrame(() => { if (cameraVideoRef.current) { cameraVideoRef.current.srcObject = stream; void cameraVideoRef.current.play(); } });
    } catch { setCameraError("Kamera izni alınamadı. Tarayıcı ayarlarından izin verin veya QR görseli yükleyin."); }
  }

  function stopCamera() {
    cameraStreamRef.current?.getTracks().forEach((track) => track.stop());
    cameraStreamRef.current = null;
    setCameraOpen(false);
  }

  React.useEffect(() => () => { cameraStreamRef.current?.getTracks().forEach((track) => track.stop()); }, []);

  async function receiveQr() {
    if (!qrResolution) return;
    if (!data?.malKabulYetkisi) { setError("Bu işlem için depo veya mal kabul yetkisi gerekir."); return; }
    const deviceStorageKey = "systemcel-receipt-device";
    let deviceRef = window.localStorage.getItem(deviceStorageKey);
    if (!deviceRef) { deviceRef = `device-${crypto.randomUUID()}`; window.localStorage.setItem(deviceStorageKey, deviceRef); }
    const saved = await complete(async () => {
      let evidencePath = "";
      if (receiptEvidenceFile) {
        const formData = new FormData(); formData.append("file", receiptEvidenceFile);
        if (receiptForm.subeId) formData.append("subeId", receiptForm.subeId);
        if (receiptForm.depoId) formData.append("depoId", receiptForm.depoId);
        const uploaded = await jsonOku<{ yol: string }>(`/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/${encodeURIComponent(qrResolution.kod)}/kanit`, { method: "POST", body: formData });
        evidencePath = uploaded.yol;
      }
      return send(`/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/${encodeURIComponent(qrResolution.kod)}/mal-kabul`, "POST", { idempotencyKey: `receipt-${crypto.randomUUID()}`, kabulEdilenMiktar: Number(receiptForm.kabulEdilenMiktar), reddedilenMiktar: Number(receiptForm.reddedilenMiktar), redNedeni: Number(receiptForm.reddedilenMiktar) > 0 ? formatReceiptReason(receiptForm.redNedeni, receiptForm.digerAciklama) : "", ikinciRedNedeni: Number(receiptForm.reddedilenMiktar) > 0 ? formatReceiptReason(receiptForm.ikinciRedNedeni, receiptForm.ikinciDigerAciklama) : "", not: receiptForm.not, subeId: receiptForm.subeId ? Number(receiptForm.subeId) : null, depoId: receiptForm.depoId ? Number(receiptForm.depoId) : null, cihazRef: deviceRef, fotoKanitiYolu: evidencePath, olculenAgirlik: receiptForm.olculenAgirlik ? Number(receiptForm.olculenAgirlik) : null, olculenSicaklik: receiptForm.olculenSicaklik === "" ? null : Number(receiptForm.olculenSicaklik) });
    }, "Mal kabul kaydedildi.");
    if (saved) { setQrCode(""); setQrResolution(null); setReceiptEvidenceFile(null); }
  }

  return <main className="supplier-marketplace" aria-busy={busy}>
    <section className="supplier-marketplace__hero"><div><span className="supplier-marketplace__eyebrow"><Store size={15} /> TEDARİK AĞI</span><h1>Tek sepet. Birden fazla tedarikçi.</h1><p>{data?.guvenliOdemeHazir ? "Ödeme güvenli biçimde alınır; tedarikçi hakedişi teslimat onayında serbest bırakılır." : "Ürünleri karşılaştır, siparişini oluştur; teslimatı, faturayı ve cari ödemeyi tek yerden takip et."}</p></div><div className="supplier-marketplace__hero-actions"><button className="supplier-marketplace__hero-action" onClick={() => setTab("cart")}><ShoppingCart />Sepet ({cartRows.length})</button><button className="supplier-marketplace__hero-action supplier-marketplace__hero-action--secondary" onClick={() => setModal("talep")}><Plus />Alım talebi oluştur</button></div></section>
    <nav className="supplier-marketplace__tabs" aria-label="Pazaryeri bölümleri"><TabButton active={tab === "catalog"} onClick={() => setTab("catalog")}>Ürünler</TabButton><TabButton active={tab === "cart"} onClick={() => setTab("cart")}>Sepet</TabButton><TabButton active={tab === "orders"} onClick={() => setTab("orders")}>Siparişler</TabButton><TabButton active={tab === "sales"} onClick={() => setTab("sales")}>Satışlarım</TabButton><TabButton active={tab === "requests"} onClick={() => setTab("requests")}>Teklif talepleri</TabButton>{data?.yonetici ? <TabButton active={tab === "admin"} onClick={() => setTab("admin")}>Yönetim</TabButton> : null}<button onClick={() => setModal("profil")}><Store size={16} />Tedarikçi hesabım</button></nav>
    {message ? <p className="supplier-marketplace__notice"><Check size={18} />{message}</p> : null}{error ? <div className="supplier-marketplace__error" role="alert"><span>{error}</span><button type="button" onClick={() => void load()}>Yeniden dene</button></div> : null}{busy ? <p className="supplier-marketplace__loading" role="status">İşlem sürüyor…</p> : null}

    {tab === "catalog" ? <><section className="supplier-marketplace__search"><Search aria-hidden="true" /><input aria-label="Tedarikçi ara" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Ürün, kategori veya tedarikçi ara" /></section><section className="supplier-marketplace__catalog">{products.map((product) => { const performance = performanceByProfile.get(product.tedarikciProfilId); return <article key={product.id}><div className="supplier-marketplace__product-head"><span className="supplier-marketplace__avatar">{product.tedarikciUnvani.slice(0, 2).toUpperCase()}</span><div><strong>{product.tedarikciUnvani}</strong><small>{product.tedarikciSehri} · {product.tahminiTeslimatGun} gün</small>{performance?.degerlendirmeSayisi ? <small>★ {performance.puan.toFixed(1)} · {performance.degerlendirmeSayisi} değerlendirme</small> : <small>Henüz değerlendirme yok</small>}</div><ShieldCheck size={18} /></div><div><small>{product.kategori} · {product.sku}</small><h2>{product.ad}</h2><p>{product.aciklama}</p></div><div className="supplier-marketplace__product-foot"><div><strong>{money(product.birimFiyat * (1 + product.kdvOrani / 100), product.paraBirimi)}</strong><small>KDV dahil · {product.birim}</small></div><button onClick={() => addToCart(product)}>Sepete ekle</button></div></article>; })}{data && products.length === 0 ? <Empty text="Bu aramaya uygun ürün yok." /> : null}</section>{suppliers.length > 0 ? <section className="supplier-marketplace__supplier-strip">{suppliers.map((profile) => { const performance = performanceByProfile.get(profile.id); return <span key={profile.id}><ShieldCheck size={14} />{profile.unvan}{performance?.degerlendirmeSayisi ? ` · ★ ${performance.puan.toFixed(1)}` : ""}</span>; })}</section> : null}</> : null}

    {tab === "cart" ? <section className="supplier-marketplace__cart"><header><div><h2>Sepet</h2><p>{supplierCount} tedarikçiden {cartRows.length} ürün</p></div><strong>{money(cartTotal, cartRows[0]?.product.paraBirimi ?? "TRY")}</strong></header>{cartRows.map(({ product, quantity }) => <article key={product.id}><div><strong>{product.ad}</strong><span>{product.tedarikciUnvani} · {money(product.birimFiyat * (1 + product.kdvOrani / 100), product.paraBirimi)}</span></div><label><span>Miktar</span><input type="number" min={product.minimumSiparisMiktari} max={product.kullanilabilirStok} step="1" value={quantity} onChange={(event) => setCartQuantity(product, Number(event.target.value))} /></label><button aria-label={`${product.ad} ürününü sepetten çıkar`} onClick={() => setCartQuantity(product, 0)}><X /></button></article>)}{cartRows.length === 0 ? <Empty text="Sepetiniz boş." /> : <div className="supplier-marketplace__checkout"><Field label="Teslimat adresi" value={deliveryAddress} onChange={setDeliveryAddress} /><p>{data?.guvenliOdemeHazir ? "Ödeme şimdi alınır; tedarikçiye yalnız teslimatı onayladığınızda aktarılır." : "Güvenli ödeme sağlayıcısı henüz bağlı değil; tutar teslimat onayında cariye işlenir."}</p><button disabled={busy} onClick={() => void createOrder()}><ShoppingCart />{data?.guvenliOdemeHazir ? "Güvenli öde ve sipariş ver" : "Siparişi oluştur"}</button></div>}</section> : null}

    {tab === "orders" ? <section className="supplier-marketplace__orders">
      <div className="supplier-marketplace__section-head"><div><h2>Siparişlerim</h2><p>Ürünü QR etiketiyle teslim al; kabul edilen miktar stok ve cariye işlensin.</p></div>{data?.malKabulYetkisi ? <button onClick={() => { setQrCode(""); setQrResolution(null); setModal("qrKabul"); }}><ScanLine />QR ile teslim al</button> : <span>Mal kabul için yetkili ekip üyesi gerekir.</span>}</div>
      <section className="supplier-marketplace__buyer-views" aria-label="Alıcı operasyon görünümleri">
        <article><h3>Beklenen sevkiyatlar</h3><strong>{(data?.sevkiyatlar ?? []).filter((shipment) => ["Hazir", "SevkEdildi"].includes(shipment.durum)).length}</strong><span>yolda veya randevulu sevkiyat</span></article>
        <article><h3>Mal kabul</h3><strong>{(data?.siparisler ?? []).filter((order) => order.aliciIsletmeId === data?.aktifIsletmeId && ["MalKabulBekliyor", "KismenKabul"].includes(order.durum)).length}</strong><span>işlem bekleyen sipariş</span></article>
        <article><h3>Fark / itiraz</h3><strong>{(data?.siparisler ?? []).filter((order) => order.aliciIsletmeId === data?.aktifIsletmeId && order.durum === "Itirazli").length}</strong><span>incelemedeki sipariş</span></article>
        <article><h3>Blokedeki ödemeler</h3><strong>{money((data?.hakedisler ?? []).filter((item) => !["SerbestBirakildi", "Tamamlandi", "IadeEdildi"].includes(item.durum)).reduce((sum, item) => sum + Math.max(0, item.netTutar - item.odenenTutar), 0), (data?.hakedisler ?? [])[0]?.paraBirimi ?? "TRY")}</strong><span>kabul veya aktarım bekliyor</span></article>
      </section>
      {(data?.anaSiparisler ?? []).map((master) => {
        const childOrders = data?.siparisler.filter((order) => order.anaSiparisId === master.id) ?? [];
        return <article key={master.id} className="supplier-marketplace__order-group"><header><div><small>{new Date(master.createdAt).toLocaleDateString("tr-TR")}</small><h2>{master.siparisNo}</h2><span>{statusLabels[master.durum] ?? master.durum}</span></div><strong>{money(master.genelToplam, master.paraBirimi)}</strong></header>{childOrders.map((order) => {
          const canCancel = ["OdemeBekliyor", "SiparisVerildi", "Odendi", "TedarikciOnayladi", "Hazirlaniyor"].includes(order.durum);
          const complaint = data?.sikayetler?.find((item) => item.tedarikciSiparisId === order.id);
          const rating = data?.degerlendirmeler?.find((item) => item.tedarikciSiparisId === order.id);
          const canComplain = ["KismenSevkEdildi", "SevkEdildi", "KismenKabul", "TeslimEdildi", "HakEdisBekliyor", "CariOdemeBekliyor", "Tamamlandi", "Itirazli"].includes(order.durum);
          return <OrderRow key={order.id} order={order} lines={data?.siparisKalemleri.filter((line) => line.tedarikciSiparisId === order.id) ?? []} actions={<>
            {["KismenSevkEdildi", "SevkEdildi", "MalKabulBekliyor", "KismenKabul"].includes(order.durum) && data?.malKabulYetkisi ? <button onClick={() => { setQrCode(""); setQrResolution(null); setModal("qrKabul"); }}><QrCode />QR okut</button> : null}
            {canComplain && !complaint ? <button onClick={() => openComplaint(order)}>Sorun bildir</button> : null}
            {complaint ? <span className="supplier-marketplace__complaint-state">Sorun: {complaintStatusLabel(complaint.durum)}</span> : null}
            {complaint && !["Cozuldu", "Cozulemedi"].includes(complaint.durum) ? <button onClick={() => { setSelectedComplaint(complaint); setComplaintOutcome({ cozuldu: true, not: "" }); setModal("sikayetSonuc"); }}>Sonucu kaydet</button> : null}
            {order.malKabulVar ? <button onClick={() => openRating(order)}>{rating ? "Değerlendirmeyi güncelle" : "Tedarikçiyi değerlendir"}</button> : null}
            {order.durum === "CariOdemeBekliyor" ? <a href="/app/tahsilat-odeme">Ödemeyi kaydet</a> : null}
            {canCancel ? <button onClick={() => void cancelSupplierOrder(order)}>Bu satıcıyı iptal et</button> : null}
          </>} />;
        })}{master.durum === "OdemeBekliyor" ? <div className="supplier-marketplace__order-actions"><button onClick={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/siparisler/${master.id}/odeme`, "POST", { idempotencyKey: `payment-${crypto.randomUUID()}` }), "Ödeme tamamlandı.")}>Öde</button><button onClick={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/siparisler/${master.id}/iptal`, "POST", { neden: "Alıcı iptali" }), "Sipariş iptal edildi.")}>Tümünü iptal et</button></div> : null}</article>;
      })}{data && data.anaSiparisler.length === 0 ? <Empty text="Henüz siparişiniz yok." /> : null}
    </section> : null}

    {tab === "sales" ? <section className="supplier-marketplace__orders"><div className="supplier-marketplace__section-head"><div><h2>Satışlarım</h2><p>Sipariş, sevkiyat, QR etiketi ve hakedişler</p></div><button onClick={() => { setEditingProductId(null); setProductForm(emptyProduct); setModal("urun"); }}><PackagePlus />Ürün ekle</button></div>{!data?.profil?.dogrulandi ? <p className="supplier-marketplace__notice">Ürün yayınlamak için tedarikçi başvurunuzu tamamlayın.</p> : null}{(data?.siparisler ?? []).filter((order) => order.tedarikciIsletmeId === data?.aktifIsletmeId).map((order) => {
      const complaint = data?.sikayetler?.find((item) => item.tedarikciSiparisId === order.id);
      const actions: React.ReactNode[] = [];
      if (["SiparisVerildi", "Odendi"].includes(order.durum)) actions.push(<React.Fragment key="confirm"><button onClick={() => void updateOrderState(order, "TedarikciOnayladi")}>Siparişi onayla</button><button onClick={() => void cancelSupplierOrder(order)}>İptal et</button></React.Fragment>);
      if (order.durum === "TedarikciOnayladi") actions.push(<React.Fragment key="prepare"><button onClick={() => void updateOrderState(order, "Hazirlaniyor")}>Hazırlamaya başla</button><button onClick={() => void cancelSupplierOrder(order)}>İptal et</button></React.Fragment>);
      if (["Hazirlaniyor", "SevkeHazir", "KismenSevkEdildi", "KismenKabul"].includes(order.durum)) actions.push(<button key="shipment" onClick={() => openShipment(order)}><QrCode />Sevkiyat ve QR oluştur</button>);
      if (["TeslimEdildi", "HakEdisBekliyor", "CariOdemeBekliyor"].includes(order.durum)) actions.push(<button key="invoice" onClick={() => { setSelectedOrder(order); setModal("fatura"); }}>Fatura numarası ekle</button>);
      if (complaint) actions.push(<span key="complaint" className="supplier-marketplace__complaint-state">Sorun: {complaintStatusLabel(complaint.durum)}</span>);
      if (complaint && !["Cozuldu", "Cozulemedi"].includes(complaint.durum)) actions.push(<button key="response" onClick={() => { setSelectedComplaint(complaint); setComplaintResponse(complaint.tedarikciYaniti); setModal("sikayetYanit"); }}>{complaint.tedarikciYaniti ? "Yanıtı güncelle" : "Sorunu yanıtla"}</button>);
      return <OrderRow key={order.id} order={order} lines={data?.siparisKalemleri.filter((line) => line.tedarikciSiparisId === order.id) ?? []} actions={actions} showSettlement settlementMovements={data?.malKabulHareketleri?.filter((item) => item.tedarikciSiparisId === order.id) ?? []} />;
    })}<div className="supplier-marketplace__inventory">{(data?.benimUrunlerim ?? []).map((product) => <article key={product.id}><div><strong>{product.ad}</strong><span>{product.sku} · {product.kategori}</span></div><b>{product.stokMiktari - product.rezerveMiktar} {product.birim}</b><button aria-label={`${product.ad} ürününü düzenle`} onClick={() => editProduct(product)}>Düzenle</button>{product.aktif ? <button onClick={() => void deactivateProduct(product)}>Pasife al</button> : <span>Pasif</span>}</article>)}</div></section> : null}

    {tab === "requests" ? <section className="supplier-marketplace__request-space"><div className="supplier-marketplace__request-grid">{(data?.acikTalepler ?? []).map((request) => <article key={request.id}><small>{request.kategori} · {request.teslimatSehri}</small><h3>{request.baslik}</h3><p>{request.urunHizmet}</p><strong>{request.miktar} {request.birim}</strong><span>{new Date(request.sonTeklifAt).toLocaleDateString("tr-TR")} tarihine kadar</span><button disabled={request.teklifVerildi} onClick={() => { setSelectedRequest(request); setModal("teklif"); }}>{request.teklifVerildi ? "Teklif verildi" : "Teklif ver"}</button></article>)}</div><section className="supplier-marketplace__offers"><h2>Alım taleplerim</h2>{(data?.talepler ?? []).map((request) => <article key={request.id}><div><strong>{request.baslik}</strong><span>{request.miktar} {request.birim} · {request.durum}</span></div><b>{request.teklifSayisi} teklif</b></article>)}<h2>Gelen teklifler</h2>{(data?.gelenTeklifler ?? []).map((offer) => <article key={offer.id}><div><strong>{offer.tedarikciUnvani}</strong><span>{offer.talepBasligi} · {offer.terminGun} gün · min. {offer.minimumSiparis} · KDV %{offer.kdvOrani}</span></div><b>{money(offer.birimFiyat, offer.paraBirimi)}</b>{offer.durum === "Gonderildi" ? <button onClick={() => { setSelectedOffer(offer); setAcceptAddress(""); setModal("kabul"); }}>Kabul et ve sipariş oluştur</button> : <em>{offer.durum}</em>}</article>)}</section></section> : null}

    {tab === "admin" && data?.yonetici ? <section className="supplier-marketplace__orders"><div className="supplier-marketplace__section-head"><div><h2>Tedarikçi yönetimi</h2><p>Doğrulama, itiraz, geciken mal kabul ve hakediş işlemleri</p></div></div>{data.operasyonMetrikleri ? <section className="supplier-marketplace__metrics" aria-label="Pazaryeri operasyon metrikleri"><Metric label="Zamanında teslim" value={`%${data.operasyonMetrikleri.zamanindaTeslimOrani}`} /><Metric label="Kabul oranı" value={`%${data.operasyonMetrikleri.kabulOrani}`} /><Metric label="Eksik oranı" value={`%${data.operasyonMetrikleri.eksikOrani}`} /><Metric label="Hasar oranı" value={`%${data.operasyonMetrikleri.hasarOrani}`} /><Metric label="İtiraz oranı" value={`%${data.operasyonMetrikleri.itirazOrani}`} /><Metric label="Ort. kabul" value={`${data.operasyonMetrikleri.ortalamaKabulSuresiSaat} sa.`} /><Metric label="Ort. hakediş" value={`${data.operasyonMetrikleri.ortalamaHakedisSuresiSaat} sa.`} /></section> : null}<section className="supplier-marketplace__offers"><h2>Doğrulama bekleyenler</h2>{(data.yonetimProfilleri ?? []).map((profile) => <article key={profile.id}><div><strong>{profile.unvan}</strong><span>{profile.sehir} · {profile.vergiNo} · {profile.yetkiliAdSoyad}</span></div><button onClick={() => { setSelectedProfile(profile); setVerificationForm({ onaylandi: true, not: "", komisyonOrani: "8", odemeVadesiGun: "7", pspAltUyeIsyeriId: "" }); setModal("dogrulama"); }}>İncele</button></article>)}{(data.yonetimProfilleri ?? []).length === 0 ? <Empty text="Doğrulama bekleyen başvuru yok." /> : null}<h2>Operasyon kuyruğu</h2>{(data.yonetimSiparisler ?? []).map((order) => <article key={order.id}><div><strong>{order.siparisNo} · {order.tedarikciUnvani}</strong><span>{order.yonetimNedeni ? `${order.yonetimNedeni} · ` : ""}{statusLabels[order.durum] ?? order.durum} · {order.hakedisDurumu}{order.planlananAt ? ` · ${new Date(order.planlananAt).toLocaleDateString("tr-TR")}` : ""}</span>{order.durum === "Itirazli" && (order.itirazYasiSaat != null || order.tedarikciIlkYanitSuresiSaat != null) ? <span className="supplier-marketplace__queue-timing">İtiraz yaşı: {order.itirazYasiSaat != null ? `${formatHours(order.itirazYasiSaat)} sa.` : "—"} · Tedarikçi ilk yanıtı: {order.tedarikciIlkYanitSuresiSaat != null ? `${formatHours(order.tedarikciIlkYanitSuresiSaat)} sa.` : "Yanıt yok"}</span> : null}</div><b>{money(order.genelToplam, order.paraBirimi)}</b>{order.durum === "Itirazli" ? <button onClick={() => void openDisputeReview(order)}>İtirazı incele</button> : ["HakEdisBekliyor", "AktarimBasarisiz"].includes(order.durum) || ["Bekliyor", "AktarimBasarisiz", "AktarimBekliyor"].includes(order.hakedisDurumu) ? <button onClick={() => { setSelectedAdminOrder(order); setAdminNote(""); setModal("hakedis"); }}>Hakedişi tamamla</button> : null}</article>)}{(data.yonetimSiparisler ?? []).length === 0 ? <Empty text="Bekleyen yönetim işlemi yok." /> : null}</section></section> : null}

    {modal === "profil" ? <Modal wide title="Tedarikçi hesabı" close={() => setModal("")} submit={() => void saveProfile()} submitLabel="Başvuruyu kaydet"><Field label="Ticaret unvanı" value={profileForm.unvan} onChange={(value) => setProfileForm({ ...profileForm, unvan: value })} /><Field label="Kategoriler" value={profileForm.kategoriler} onChange={(value) => setProfileForm({ ...profileForm, kategoriler: value })} /><Field label="Şehir" value={profileForm.sehir} onChange={(value) => setProfileForm({ ...profileForm, sehir: value })} /><Field label="Tanıtım" value={profileForm.aciklama} onChange={(value) => setProfileForm({ ...profileForm, aciklama: value })} /><Field label="Vergi numarası" value={profileForm.vergiNo} onChange={(value) => setProfileForm({ ...profileForm, vergiNo: value })} /><Field label="MERSİS numarası" value={profileForm.mersisNo} onChange={(value) => setProfileForm({ ...profileForm, mersisNo: value })} required={false} /><Field label="KEP adresi" value={profileForm.kepAdresi} onChange={(value) => setProfileForm({ ...profileForm, kepAdresi: value })} required={false} /><Field label="IBAN" value={profileForm.iban} onChange={(value) => setProfileForm({ ...profileForm, iban: value })} /><Field label="Şirket adresi" value={profileForm.adres} onChange={(value) => setProfileForm({ ...profileForm, adres: value })} /><Field label="Yetkili kişi" value={profileForm.yetkiliAdSoyad} onChange={(value) => setProfileForm({ ...profileForm, yetkiliAdSoyad: value })} /><Field label="Vergi durumu" value={profileForm.vergiDurumu} onChange={(value) => setProfileForm({ ...profileForm, vergiDurumu: value })} required={false} /><Field label="Sevkiyat bölgeleri" value={profileForm.sevkiyatBolgeleri} onChange={(value) => setProfileForm({ ...profileForm, sevkiyatBolgeleri: value })} required={false} /><Field label="İade koşulları" value={profileForm.iadeKosullari} onChange={(value) => setProfileForm({ ...profileForm, iadeKosullari: value })} required={false} /><Field label="Sözleşme sürümü" value={profileForm.pazaryeriSozlesmeVersiyonu} onChange={(value) => setProfileForm({ ...profileForm, pazaryeriSozlesmeVersiyonu: value })} /><label className="supplier-marketplace__check"><input type="checkbox" checked={profileForm.tevkifatMuaf} onChange={(event) => setProfileForm({ ...profileForm, tevkifatMuaf: event.target.checked })} />Tevkifat istisnası var</label>{data?.profil?.dogrulamaDurumu ? <p>Durum: {data.profil.dogrulamaDurumu}</p> : null}</Modal> : null}
    {modal === "urun" ? <Modal title={editingProductId === null ? "Ürün ekle" : "Ürünü düzenle"} close={() => { setModal(""); setEditingProductId(null); setProductForm(emptyProduct); }} submit={() => void saveProduct()} submitLabel={editingProductId === null ? "Ürünü yayınla" : "Ürünü güncelle"}><Field label="Stok kodu" value={productForm.sku} onChange={(value) => setProductForm({ ...productForm, sku: value })} /><Field label="Ürün adı" value={productForm.ad} onChange={(value) => setProductForm({ ...productForm, ad: value })} /><Field label="Kategori" value={productForm.kategori} onChange={(value) => setProductForm({ ...productForm, kategori: value })} /><Field label="Açıklama" value={productForm.aciklama} onChange={(value) => setProductForm({ ...productForm, aciklama: value })} required={false} /><Field label="Birim" value={productForm.birim} onChange={(value) => setProductForm({ ...productForm, birim: value })} /><Field label="KDV hariç fiyat" value={productForm.birimFiyat} onChange={(value) => setProductForm({ ...productForm, birimFiyat: value })} type="number" /><Field label="KDV oranı" value={productForm.kdvOrani} onChange={(value) => setProductForm({ ...productForm, kdvOrani: value })} type="number" /><Field label="Stok" value={productForm.stokMiktari} onChange={(value) => setProductForm({ ...productForm, stokMiktari: value })} type="number" /><Field label="Minimum sipariş" value={productForm.minimumSiparisMiktari} onChange={(value) => setProductForm({ ...productForm, minimumSiparisMiktari: value })} type="number" /><Field label="Tahmini teslimat günü" value={productForm.tahminiTeslimatGun} onChange={(value) => setProductForm({ ...productForm, tahminiTeslimatGun: value })} type="number" /><label className="supplier-marketplace__check"><input type="checkbox" checked={productForm.aktif} onChange={(event) => setProductForm({ ...productForm, aktif: event.target.checked })} />Ürün aktif</label></Modal> : null}
    {modal === "talep" ? <Modal title="Alım talebi oluştur" close={() => setModal("")} submit={() => void complete(() => send("/api/ekran/tedarikci-pazaryeri/talepler", "POST", { ...requestForm, miktar: Number(requestForm.miktar) }), "Alım talebi yayınlandı.")} submitLabel="Talebi yayınla">{(["baslik", "kategori", "urunHizmet", "miktar", "birim", "teslimatSehri", "sonTeklifAt", "aciklama"] as const).map((key) => <Field key={key} label={{ baslik: "Başlık", kategori: "Kategori", urunHizmet: "Ürün veya hizmet", miktar: "Miktar", birim: "Birim", teslimatSehri: "Teslimat şehri", sonTeklifAt: "Son teklif tarihi", aciklama: "Açıklama" }[key]} type={key === "miktar" ? "number" : key === "sonTeklifAt" ? "datetime-local" : "text"} value={requestForm[key]} onChange={(value) => setRequestForm({ ...requestForm, [key]: value })} />)}</Modal> : null}
    {modal === "teklif" ? <Modal title={`${selectedRequest?.baslik ?? "Talep"} için teklif`} close={() => setModal("")} submit={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/talepler/${selectedRequest?.id}/teklifler`, "POST", { ...offerForm, birimFiyat: Number(offerForm.birimFiyat), kdvOrani: Number(offerForm.kdvOrani), terminGun: Number(offerForm.terminGun), minimumSiparis: Number(offerForm.minimumSiparis) }), "Teklif gönderildi.")} submitLabel="Teklifi gönder">{(["birimFiyat", "kdvOrani", "paraBirimi", "terminGun", "minimumSiparis", "not"] as const).map((key) => <Field key={key} label={{ birimFiyat: "Birim fiyat", kdvOrani: "KDV oranı", paraBirimi: "Para birimi", terminGun: "Termin günü", minimumSiparis: "Minimum sipariş", not: "Not" }[key]} type={["birimFiyat", "kdvOrani", "terminGun", "minimumSiparis"].includes(key) ? "number" : "text"} value={offerForm[key]} onChange={(value) => setOfferForm({ ...offerForm, [key]: value })} />)}</Modal> : null}
    {modal === "kabul" && selectedOffer ? <Modal title="Teklifi siparişe dönüştür" close={() => { setModal(""); setSelectedOffer(null); }} submit={() => void acceptOffer()} submitLabel={data?.guvenliOdemeHazir ? "Güvenli öde ve sipariş ver" : "Siparişi oluştur"}><p>{data?.guvenliOdemeHazir ? `${selectedOffer.tedarikciUnvani} için ödeme şimdi alınır ve teslimat onayına kadar blokede tutulur.` : `${selectedOffer.tedarikciUnvani} teklifindeki tutar teslimat onayında cariye işlenecek.`}</p><Field label="Teslimat adresi" value={acceptAddress} onChange={setAcceptAddress} /></Modal> : null}
    {modal === "kargo" && selectedOrder ? <Modal wide title="Sevkiyat ve QR etiketi oluştur" close={() => setModal("")} submit={() => void createShipment()} submitLabel="QR etiketlerini oluştur"><p>Her koli veya ürün etiketi yalnız sipariş kimliğini taşır; fiyat ve ticari bilgiler QR içine yazılmaz.</p><Field label="Taşıma tipi" value={shippingForm.tasimaTipi} onChange={(value) => setShippingForm({ ...shippingForm, tasimaTipi: value })} /><Field label="Taşıyıcı / dağıtım ağı" value={shippingForm.tasiyici} onChange={(value) => setShippingForm({ ...shippingForm, tasiyici: value })} required={false} /><Field label="İrsaliye / belge no" value={shippingForm.belgeNo} onChange={(value) => setShippingForm({ ...shippingForm, belgeNo: value })} required={false} /><Field label="e-İrsaliye UUID" value={shippingForm.belgeUuid} onChange={(value) => setShippingForm({ ...shippingForm, belgeUuid: value })} required={false} /><FilePicker label="İrsaliye fotoğrafı veya PDF" accept="image/jpeg,image/png,image/webp,application/pdf" onChange={setShippingDocumentFile} /><Field label="Araç plakası" value={shippingForm.aracPlaka} onChange={(value) => setShippingForm({ ...shippingForm, aracPlaka: value })} required={false} /><Field label="Sürücü" value={shippingForm.surucuAdi} onChange={(value) => setShippingForm({ ...shippingForm, surucuAdi: value })} required={false} /><Field label="Çıkış deposu" value={shippingForm.cikisDeposu} onChange={(value) => setShippingForm({ ...shippingForm, cikisDeposu: value })} required={false} /><SelectField label="Varış deposu" value={shippingForm.varisDeposu} onChange={(value) => setShippingForm({ ...shippingForm, varisDeposu: value })} options={[{ value: "", label: "Varış deposu seçilmedi" }, ...(data?.depolar ?? []).map((item) => ({ value: String(item.id), label: `${item.ad} · ${item.kod}` }))]} /><Field label="Sevk zamanı" type="datetime-local" value={shippingForm.sevkAt} onChange={(value) => setShippingForm({ ...shippingForm, sevkAt: value })} required={false} /><Field label="Teslimat randevusu" type="datetime-local" value={shippingForm.randevuAt} onChange={(value) => setShippingForm({ ...shippingForm, randevuAt: value })} required={false} /><Field label="Planlanan teslim" type="datetime-local" value={shippingForm.planlananTeslimAt} onChange={(value) => setShippingForm({ ...shippingForm, planlananTeslimAt: value })} required={false} />{(data?.siparisKalemleri ?? []).filter((line) => shipmentLines[line.id]).map((line) => <fieldset key={line.id}><legend>{line.ad} · kalan {line.miktar - (line.sevkEdilenMiktar ?? 0)} {line.birim}</legend><Field label="Sevk miktarı" type="number" value={shipmentLines[line.id].miktar} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], miktar: value } })} /><Field label="QR etiket sayısı" type="number" value={shipmentLines[line.id].etiketSayisi} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], etiketSayisi: value } })} /><Field label="Lot no" value={shipmentLines[line.id].lotNo} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], lotNo: value } })} required={false} /><Field label="Son kullanma tarihi" type="date" value={shipmentLines[line.id].sonKullanmaTarihi} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], sonKullanmaTarihi: value } })} required={false} /><Field label="Sıcaklık alt sınırı (°C)" type="number" value={shipmentLines[line.id].sicaklikMin} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], sicaklikMin: value } })} required={false} /><Field label="Sıcaklık üst sınırı (°C)" type="number" value={shipmentLines[line.id].sicaklikMax} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], sicaklikMax: value } })} required={false} /><Field label="Seri no" value={shipmentLines[line.id].seriNo} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], seriNo: value } })} required={false} /><Field label="Ağırlık" type="number" value={shipmentLines[line.id].agirlik} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], agirlik: value } })} required={false} /><Field label="Palet / koli" type="number" value={shipmentLines[line.id].paletKoli} onChange={(value) => setShipmentLines({ ...shipmentLines, [line.id]: { ...shipmentLines[line.id], paletKoli: value } })} required={false} /></fieldset>)}</Modal> : null}
    {modal === "etiketler" ? <Modal wide title="Sevkiyat QR etiketleri" close={() => setModal("")} submit={() => window.print()} submitLabel="Etiketleri yazdır"><p>Bu kodlar yalnızca şimdi gösterilir. Yazdırın veya güvenli biçimde kaydedin.</p><div className="supplier-marketplace__qr-labels">{qrLabels.map((label) => <article key={label.id}><img src={`/api/ekran/tedarikci-pazaryeri/sevkiyat-qr/${encodeURIComponent(label.kod)}/gorsel`} alt={`${label.urunAdi} sevkiyat QR kodu`} /><div><strong>{label.urunAdi}</strong><span>{label.sku} · {label.miktar} {label.birim}</span>{label.lotNo ? <span>Lot: {label.lotNo}</span> : null}</div></article>)}</div><button type="button" onClick={() => window.print()}><Printer />Yazdır</button></Modal> : null}
    {modal === "qrKabul" ? <Modal wide title="QR ile mal kabul" close={() => { stopCamera(); setModal(""); }} submit={() => void (qrResolution ? receiveQr() : resolveQr())} submitLabel={qrResolution ? "Mal kabulü kaydet" : "Etiketi bul"}><p>Tedarikçi veya sürücünün teslim bildirimi tek başına yeterli değildir. Yetkili alıcı olarak kabul ve ret miktarlarını doğrulayın.</p><Field label="QR kodu" value={qrCode} onChange={setQrCode} /><div className="supplier-marketplace__camera-actions"><button type="button" onClick={() => void startCamera()} disabled={cameraOpen}><ScanLine />{cameraOpen ? "Kamera açık" : "Kamerayı aç"}</button><FilePicker label="QR görseli yükle" accept="image/*" capture="environment" onChange={(file) => { if (file) void scanQrImage(file); }} /></div>{cameraOpen ? <div className="supplier-marketplace__camera"><video ref={cameraVideoRef} muted playsInline aria-label="QR kamera önizlemesi" /><button type="button" onClick={stopCamera}>Kamerayı kapat</button></div> : null}{cameraError ? <p className="supplier-marketplace__error" role="alert">{cameraError}</p> : null}{qrResolution ? <section className="supplier-marketplace__receipt-fields"><h3>{qrResolution.urunAdi}</h3><p>{qrResolution.siparisNo} · {qrResolution.sku} · Etikette {qrResolution.miktar} {qrResolution.birim}{qrResolution.lotNo ? ` · Lot ${qrResolution.lotNo}` : ""}</p>{qrResolution.sonKullanmaTarihi ? <p>Son kullanma: {new Date(qrResolution.sonKullanmaTarihi).toLocaleDateString("tr-TR")}</p> : null}{qrResolution.sicaklikMin != null || qrResolution.sicaklikMax != null ? <p>İzin verilen sıcaklık: {qrResolution.sicaklikMin ?? "—"}–{qrResolution.sicaklikMax ?? "—"} °C. Kabul için ölçüm girin.</p> : null}<SelectField label="Şube" value={receiptForm.subeId} onChange={(value) => setReceiptForm({ ...receiptForm, subeId: value, depoId: data?.depolar?.some((item) => String(item.id) === receiptForm.depoId && (!item.subeId || String(item.subeId) === value)) ? receiptForm.depoId : "" })} options={[{ value: "", label: "Şube seçilmedi" }, ...(data?.subeler ?? []).map((item) => ({ value: String(item.id), label: `${item.ad} · ${item.kod}` }))]} /><SelectField label="Depo" value={receiptForm.depoId} onChange={(value) => setReceiptForm({ ...receiptForm, depoId: value })} options={[{ value: "", label: "Depo seçilmedi" }, ...(data?.depolar ?? []).filter((item) => !receiptForm.subeId || !item.subeId || String(item.subeId) === receiptForm.subeId).map((item) => ({ value: String(item.id), label: `${item.ad} · ${item.kod}` }))]} /><Field label="Kabul edilen miktar" type="number" value={receiptForm.kabulEdilenMiktar} onChange={(value) => setReceiptForm({ ...receiptForm, kabulEdilenMiktar: value })} /><Field label="Reddedilen miktar" type="number" value={receiptForm.reddedilenMiktar} onChange={(value) => setReceiptForm({ ...receiptForm, reddedilenMiktar: value, redNedeni: Number(value) > 0 ? receiptForm.redNedeni : "", digerAciklama: Number(value) > 0 ? receiptForm.digerAciklama : "", ikinciRedNedeni: Number(value) > 0 ? receiptForm.ikinciRedNedeni : "", ikinciDigerAciklama: Number(value) > 0 ? receiptForm.ikinciDigerAciklama : "" })} /><Field label="Ölçülen ağırlık" type="number" value={receiptForm.olculenAgirlik} onChange={(value) => setReceiptForm({ ...receiptForm, olculenAgirlik: value })} required={false} /><Field label="Ölçülen sıcaklık (°C)" type="number" value={receiptForm.olculenSicaklik} onChange={(value) => setReceiptForm({ ...receiptForm, olculenSicaklik: value })} required={Number(receiptForm.kabulEdilenMiktar) > 0 && (qrResolution.sicaklikMin != null || qrResolution.sicaklikMax != null)} /><FilePicker label="Fotoğraf veya PDF kanıtı" accept="image/jpeg,image/png,image/webp,application/pdf" capture="environment" onChange={setReceiptEvidenceFile} />{Number(receiptForm.reddedilenMiktar) > 0 ? <>
      <SelectField label="Ret nedeni" value={receiptForm.redNedeni} onChange={(value) => setReceiptForm({ ...receiptForm, redNedeni: value, digerAciklama: value === "Diğer" ? receiptForm.digerAciklama : "", ikinciRedNedeni: value === receiptForm.ikinciRedNedeni ? "" : receiptForm.ikinciRedNedeni })} options={receiptRejectReasons} required />
      {receiptForm.redNedeni === "Diğer" ? <Field label="Diğer ret nedeni" value={receiptForm.digerAciklama} onChange={(value) => setReceiptForm({ ...receiptForm, digerAciklama: value })} maxLength={73} /> : null}
      {receiptForm.redNedeni ? <SelectField label="2. ret nedeni (varsa)" value={receiptForm.ikinciRedNedeni} onChange={(value) => setReceiptForm({ ...receiptForm, ikinciRedNedeni: value, ikinciDigerAciklama: value === "Diğer" ? receiptForm.ikinciDigerAciklama : "" })} options={receiptRejectReasons.filter((reason) => !reason.value || reason.value !== receiptForm.redNedeni)} /> : null}
      {receiptForm.ikinciRedNedeni === "Diğer" ? <Field label="2. ret nedeni açıklaması" value={receiptForm.ikinciDigerAciklama} onChange={(value) => setReceiptForm({ ...receiptForm, ikinciDigerAciklama: value })} maxLength={153} required={false} /> : null}
    </> : null}<Field label="Teslim notu" value={receiptForm.not} onChange={(value) => setReceiptForm({ ...receiptForm, not: value })} required={false} /></section> : null}</Modal> : null}
    {modal === "sikayet" && selectedOrder ? <Modal title={`${selectedOrder.siparisNo} için sorun bildir`} close={() => { setModal(""); setSelectedOrder(null); }} submit={() => void createComplaint()} submitLabel="Tedarikçiye ilet"><SelectField label="Sorun" value={complaintForm.kategori} onChange={(value) => setComplaintForm({ ...complaintForm, kategori: value })} options={[{ value: "TeslimEdilmedi", label: "Teslim edilmedi" }, { value: "Eksik", label: "Eksik ürün" }, { value: "Hasarli", label: "Hasarlı ürün" }, { value: "YanlisUrun", label: "Yanlış ürün" }, { value: "Kalite", label: "Ürün uygun değil" }, { value: "Sicaklik", label: "Sıcaklık koşulu" }, { value: "BelgeUyusmazligi", label: "Belge uyuşmazlığı" }, { value: "Diger", label: "Diğer" }]} /><SelectField label="Beklediğiniz çözüm" value={complaintForm.talep} onChange={(value) => setComplaintForm({ ...complaintForm, talep: value })} options={[{ value: "EksigiTamamla", label: "Eksik ürün gönderilsin" }, { value: "Degisim", label: "Ürün değiştirilsin" }, { value: "IadeTalebi", label: "İade görüşülsün" }, { value: "Diger", label: "Başka bir çözüm" }]} /><TextAreaField label="Açıklama" value={complaintForm.aciklama} onChange={(value) => setComplaintForm({ ...complaintForm, aciklama: value })} /><p>Systemcel bildirimi ve tarafların yanıtlarını kaydeder; ürünün durumunu veya para iadesini garanti etmez.</p></Modal> : null}
    {modal === "sikayetYanit" && selectedComplaint ? <Modal title={`${selectedComplaint.siparisNo} sorununu yanıtla`} close={() => { setModal(""); setSelectedComplaint(null); }} submit={() => void respondToComplaint()} submitLabel="Yanıtı gönder"><p>{selectedComplaint.aciklama}</p><TextAreaField label="Yanıtınız" value={complaintResponse} onChange={setComplaintResponse} /></Modal> : null}
    {modal === "sikayetSonuc" && selectedComplaint ? <Modal title={`${selectedComplaint.siparisNo} sorun sonucu`} close={() => { setModal(""); setSelectedComplaint(null); }} submit={() => void closeComplaint()} submitLabel="Sonucu kaydet">{selectedComplaint.tedarikciYaniti ? <p>Tedarikçi yanıtı: {selectedComplaint.tedarikciYaniti}</p> : <p>Tedarikçi henüz yanıt vermedi.</p>}<SelectField label="Sonuç" value={complaintOutcome.cozuldu ? "Cozuldu" : "Cozulemedi"} onChange={(value) => setComplaintOutcome({ ...complaintOutcome, cozuldu: value === "Cozuldu" })} options={[{ value: "Cozuldu", label: "Sorun çözüldü" }, { value: "Cozulemedi", label: "Sorun çözülmedi" }]} /><TextAreaField label="Sonuç notu" value={complaintOutcome.not} onChange={(value) => setComplaintOutcome({ ...complaintOutcome, not: value })} required={!complaintOutcome.cozuldu} /></Modal> : null}
    {modal === "degerlendirme" && selectedOrder ? <Modal title={`${selectedOrder.tedarikciUnvani} değerlendirmesi`} close={() => { setModal(""); setSelectedOrder(null); }} submit={() => void saveRating()} submitLabel="Değerlendirmeyi kaydet"><ScoreField label="Ürün uygunluğu" value={ratingForm.urunUygunluguPuani} onChange={(value) => setRatingForm({ ...ratingForm, urunUygunluguPuani: value })} /><ScoreField label="Eksiksiz teslimat" value={ratingForm.eksiksizTeslimatPuani} onChange={(value) => setRatingForm({ ...ratingForm, eksiksizTeslimatPuani: value })} /><ScoreField label="Hasarsız teslimat" value={ratingForm.hasarsizTeslimatPuani} onChange={(value) => setRatingForm({ ...ratingForm, hasarsizTeslimatPuani: value })} /><ScoreField label="Zamanında teslimat" value={ratingForm.zamanindaTeslimatPuani} onChange={(value) => setRatingForm({ ...ratingForm, zamanindaTeslimatPuani: value })} />{data?.sikayetler?.some((item) => item.tedarikciSiparisId === selectedOrder.id) ? <ScoreField label="Sorun çözme" value={ratingForm.sorunCozmePuani || "3"} onChange={(value) => setRatingForm({ ...ratingForm, sorunCozmePuani: value })} /> : null}<TextAreaField label="Yorum" value={ratingForm.yorum} onChange={(value) => setRatingForm({ ...ratingForm, yorum: value })} required={false} /></Modal> : null}
    {modal === "fatura" && selectedOrder ? <Modal title="Faturayı eşleştir" close={() => setModal("")} submit={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${selectedOrder.id}/fatura`, "POST", invoiceForm), "Fatura eşleştirildi.")} submitLabel="Faturayı eşleştir"><Field label="Fatura numarası" value={invoiceForm.belgeNo} onChange={(value) => setInvoiceForm({ ...invoiceForm, belgeNo: value })} /><Field label="Fatura UUID" value={invoiceForm.belgeUuid} onChange={(value) => setInvoiceForm({ ...invoiceForm, belgeUuid: value })} required={false} /></Modal> : null}
    {modal === "dogrulama" && selectedProfile ? <Modal title={`${selectedProfile.unvan} başvurusu`} close={() => { setModal(""); setSelectedProfile(null); }} submit={() => void verifySupplier()} submitLabel={verificationForm.onaylandi ? "Başvuruyu onayla" : "Başvuruyu reddet"}><label className="supplier-marketplace__check"><input type="checkbox" checked={verificationForm.onaylandi} onChange={(event) => setVerificationForm({ ...verificationForm, onaylandi: event.target.checked })} />Başvuruyu onayla</label><Field label="Komisyon oranı" type="number" value={verificationForm.komisyonOrani} onChange={(value) => setVerificationForm({ ...verificationForm, komisyonOrani: value })} /><Field label="Ödeme vadesi (gün)" type="number" value={verificationForm.odemeVadesiGun} onChange={(value) => setVerificationForm({ ...verificationForm, odemeVadesiGun: value })} /><Field label="PSP alt üye işyeri kimliği" value={verificationForm.pspAltUyeIsyeriId} onChange={(value) => setVerificationForm({ ...verificationForm, pspAltUyeIsyeriId: value })} required={false} /><Field label="Yönetici notu" value={verificationForm.not} onChange={(value) => setVerificationForm({ ...verificationForm, not: value })} /></Modal> : null}
    {modal === "itiraz" && selectedAdminOrder ? <Modal wide title={`${selectedAdminOrder.siparisNo} itirazını çöz`} close={() => { setModal(""); setSelectedAdminOrder(null); setDisputeReview(null); setDisputeReviewError(""); setStockReconciliationDrafts({}); setStockReconciliationError(""); stockReconciliationKeyRef.current.clear(); }} submit={() => void resolveDispute()} submitLabel="Kararı uygula" submitDisabled={!disputeReview || stockReconciliationBusyId !== null || (disputeDecision !== "YenidenTeslim" && disputeReview.receipts.some((item) => item.reddedilenMiktar > 0 && item.stokUzlastirmaDurumu === "Bekliyor"))}>
      {disputeReview ? <>
        <section className="supplier-marketplace__review"><h3>İnceleme paketi</h3><p>{disputeReview.shipments.length} sevkiyat · {disputeReview.receipts.length} kabul kaydı · {disputeReview.complaints.length} taraf bildirimi</p>
          {disputeReview.shipments.map((item) => <article key={`s-${item.id}`}><strong>{item.sevkiyatNo}</strong><span>{item.belgeNo || "Belge numarası yok"} · {item.durum}</span>{item.belgeDosyaYolu ? <a href={item.belgeDosyaYolu} target="_blank" rel="noreferrer">İrsaliyeyi aç</a> : null}</article>)}
          {disputeReview.receipts.map((item) => <article key={`r-${item.id}`}><strong>Kabul #{item.id}: {item.kabulEdilenMiktar} kabul / {item.reddedilenMiktar} ret</strong><span>{[item.redNedeni, item.ikinciRedNedeni].filter(Boolean).join("; ") || item.not || "Not yok"} · {new Date(item.createdAt).toLocaleString("tr-TR")}</span>{item.fotoKanitiYolu ? <a href={item.fotoKanitiYolu} target="_blank" rel="noreferrer">Kanıtı aç</a> : null}</article>)}
          {disputeReview.complaints.map((item, index) => <article key={`c-${index}`}><strong>{item.kategori} · {item.durum}</strong><span>{item.aciklama}</span>{item.tedarikciYaniti ? <span>Tedarikçi: {item.tedarikciYaniti}</span> : null}</article>)}
          <OrderReferenceChain references={disputeReview.references} currency={selectedAdminOrder.paraBirimi} />
        </section>
        {disputeReview.receipts.filter((item) => item.reddedilenMiktar > 0 && item.stokUzlastirmaDurumu).map((item) => <StockReconciliationPanel
          key={`stock-${item.id}`}
          receipt={item}
          draft={stockReconciliationDrafts[item.id] ?? { decision: "", note: "" }}
          busy={stockReconciliationBusyId === item.id}
          disabled={stockReconciliationBusyId !== null && stockReconciliationBusyId !== item.id}
          error={stockReconciliationError && stockReconciliationBusyId === null && item.stokUzlastirmaDurumu === "Bekliyor" ? stockReconciliationError : ""}
          onDecision={(decision) => updateStockReconciliationDraft(item.id, { decision })}
          onNote={(note) => updateStockReconciliationDraft(item.id, { note })}
          onSubmit={() => void reconcileReceiptStock(item.id)}
        />)}
        {stockReconciliationError && stockReconciliationBusyId === null && !disputeReview.receipts.some((item) => item.reddedilenMiktar > 0 && item.stokUzlastirmaDurumu === "Bekliyor") ? <p className="supplier-marketplace__stock-reconciliation-error" role="alert">{stockReconciliationError}</p> : null}
        {disputeDecision !== "YenidenTeslim" && disputeReview.receipts.some((item) => item.reddedilenMiktar > 0 && item.stokUzlastirmaDurumu === "Bekliyor") ? <p role="status">İtirazı kapatmadan önce bekleyen ret stokları için karar kaydedin.</p> : null}
      </> : disputeReviewError ? <div className="supplier-marketplace__stock-reconciliation-error" role="alert">{disputeReviewError}<button type="button" onClick={() => void openDisputeReview(selectedAdminOrder)}>Yeniden dene</button></div> : <p role="status">İnceleme paketi yükleniyor…</p>}
      <SelectField label="Karar" value={disputeDecision} onChange={setDisputeDecision} options={[{ value: "TedarikciyeAktar", label: "Tedarikçiye aktar" }, { value: "AliciyaIade", label: "Alıcıya iade" }, { value: "KismiPaylas", label: "Kısmi paylaş" }, { value: "YenidenTeslim", label: "Yeniden teslim" }]} />
      {disputeDecision === "KismiPaylas" ? <Field label="Tedarikçiye aktarılacak net tutar" type="number" value={disputeSupplierAmount} onChange={setDisputeSupplierAmount} /> : null}
      <Field label="Gerekçe ve çözüm notu" value={adminNote} onChange={setAdminNote} />
    </Modal> : null}
    {modal === "hakedis" && selectedAdminOrder ? <Modal title={`${selectedAdminOrder.siparisNo} hakedişi`} close={() => { setModal(""); setSelectedAdminOrder(null); }} submit={() => void completeSettlement()} submitLabel="Hakedişi tamamla"><Field label="Yönetici işlem referansı" value={adminNote} onChange={setAdminNote} /></Modal> : null}
  </main>;
}

function StockReconciliationPanel({
  receipt,
  draft,
  busy,
  disabled,
  error,
  onDecision,
  onNote,
  onSubmit
}: {
  receipt: ItirazIncelemesi["receipts"][number];
  draft: { decision: StokUzlastirmaKarari | ""; note: string };
  busy: boolean;
  disabled: boolean;
  error: string;
  onDecision: (value: StokUzlastirmaKarari | "") => void;
  onNote: (value: string) => void;
  onSubmit: () => void;
}) {
  const decisionLabels: Record<StokUzlastirmaKarari, string> = {
    Kayip: "Kayıp olarak kaydet",
    TedarikciyeIade: "Tedarikçiye iade et",
    YenidenSevk: "Yeniden sevk iste"
  };
  const status = receipt.stokUzlastirmaDurumu ?? "";
  return <section className="supplier-marketplace__stock-reconciliation" aria-label={`Kabul #${receipt.id} stok uzlaştırması`}>
    <h3>Reddedilen miktar: {receipt.reddedilenMiktar}</h3>
    {status === "Bekliyor" ? <>
      <p>Reddedilen ürünlerin stok kaydını seçip gerekçeyi yazın.</p>
      <SelectField label="Stok kararı" value={draft.decision} onChange={(value) => onDecision(value as StokUzlastirmaKarari | "")} options={[
        { value: "", label: "Karar seçin" },
        { value: "Kayip", label: "Kayıp" },
        { value: "TedarikciyeIade", label: "Tedarikçiye iade" },
        { value: "YenidenSevk", label: "Yeniden sevk" }
      ]} />
      <label className="supplier-marketplace__stock-reconciliation-note"><span>Karar notu <b aria-hidden="true">*</b></span><textarea required aria-required="true" value={draft.note} onChange={(event) => onNote(event.target.value)} rows={3} maxLength={1000} /></label>
      {error ? <p className="supplier-marketplace__stock-reconciliation-error" role="alert">{error}</p> : null}
      <button type="button" className="supplier-marketplace__stock-reconciliation-submit" disabled={busy || disabled || !draft.decision || !draft.note.trim()} aria-busy={busy} onClick={onSubmit}>{busy ? "Kaydediliyor…" : "Stok kararını kaydet"}</button>
    </> : status === "ManuelInceleme" ? <div className="supplier-marketplace__stock-reconciliation-result">
      <strong>Eski kayıt: stok geçmişi manuel incelenmeli</strong>
      {receipt.stokUzlastirmaNotu ? <p>{receipt.stokUzlastirmaNotu}</p> : null}
    </div> : <div className="supplier-marketplace__stock-reconciliation-result">
      <strong>{decisionLabels[status as StokUzlastirmaKarari] ?? status}</strong>
      {receipt.stokUzlastirmaNotu ? <p>{receipt.stokUzlastirmaNotu}</p> : null}
      {receipt.stokUzlastirmaAt ? <span>Karar zamanı: {new Date(receipt.stokUzlastirmaAt).toLocaleString("tr-TR")}</span> : null}
    </div>}
  </section>;
}

function formatHours(value: number) {
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 1 });
}

function OrderReferenceChain({ references, currency }: { references?: SiparisReferanslari; currency: string }) {
  if (!references) return null;
  return <details className="supplier-marketplace__reference-chain">
    <summary>Ödeme, fatura ve hareket bağlantıları</summary>
    <div>
      {references.paymentAllocations.length ? references.paymentAllocations.map((payment) => <p key={`psp-${payment.id}`}>PSP tahsilatı #{payment.id}: {payment.saglayici} · {payment.saglayiciIslemId || "İşlem referansı bekleniyor"} · {money(payment.brutTutar, currency)} · {payment.durum}</p>) : <p>PSP tahsilatı yok; cari ödeme akışı.</p>}
      {references.invoiceMatch ? <p>Fatura eşlemesi: alıcı #{references.invoiceMatch.aliciFaturaId}, satıcı #{references.invoiceMatch.saticiFaturaId}{references.invoiceMatch.tedarikciBelgeNo ? ` · Tedarikçi belgesi ${references.invoiceMatch.tedarikciBelgeNo}` : ""}</p> : <p>Fatura eşlemesi henüz yok.</p>}
      {references.invoices.map((invoice) => <p key={`invoice-${invoice.id}`}>Fatura #{invoice.id}: {invoice.faturaTipi} · {invoice.yerelFaturaNo || invoice.portalBelgeNo} · {money(invoice.genelToplam, currency)}</p>)}
      {references.settlement ? <p>Hakediş #{references.settlement.id}: {references.settlement.durum} · {money(references.settlement.odenenTutar, currency)} / {money(references.settlement.netTutar, currency)}{references.settlement.aktarimReferansi ? ` · ${references.settlement.aktarimReferansi}` : ""}</p> : <p>Hakediş kaydı henüz yok.</p>}
      {references.stockMovements.map((movement) => <p key={`stock-${movement.id}`}>Stok #{movement.id}: {movement.hareketTipi} {movement.miktar} · {movement.tedarikciSevkiyatId ? `Sevkiyat #${movement.tedarikciSevkiyatId}` : movement.tedarikciMalKabulId ? `Kabul #${movement.tedarikciMalKabulId}` : "Sipariş"}</p>)}
      {references.cariMovements.map((movement) => <p key={`cari-${movement.id}`}>Cari #{movement.id}: {movement.hareketTipi} {money(movement.tutar, currency)}{movement.tedarikciMalKabulId ? ` · Kabul #${movement.tedarikciMalKabulId}` : ""}</p>)}
      {references.paymentMovements.map((movement) => <p key={`movement-${movement.id}`}>Ödeme hareketi #{movement.id}: fatura #{movement.faturaId} · {movement.tip} {money(movement.tutar, currency)}{movement.tedarikciMalKabulId ? ` · Kabul #${movement.tedarikciMalKabulId}` : ""}</p>)}
    </div>
  </details>;
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) { return <button className={active ? "active" : ""} aria-pressed={active} onClick={onClick}>{children}</button>; }
function OrderRow({ order, lines, actions, showSettlement = false, settlementMovements = [] }: { order: TedarikciSiparis; lines: SiparisKalemi[]; actions: React.ReactNode; showSettlement?: boolean; settlementMovements?: MalKabulHareketi[] }) {
  return <section className="supplier-marketplace__order-row"><div className="supplier-marketplace__order-title"><Truck /><div><strong>{order.tedarikciUnvani}</strong><span>{order.siparisNo} · {statusLabels[order.durum] ?? order.durum}</span></div></div><ul>{lines.map((line) => <li key={line.id}><span>{line.ad} · {line.miktar} {line.birim}{line.sevkEdilenMiktar || line.kabulEdilenMiktar || line.reddedilenMiktar ? <small>Sevk {line.sevkEdilenMiktar ?? 0} · Kabul {line.kabulEdilenMiktar ?? 0}{line.reddedilenMiktar ? ` · Ret ${line.reddedilenMiktar}` : ""}</small> : null}</span><b>{money(line.toplamTutar, order.paraBirimi)}</b></li>)}</ul>{showSettlement ? <><div className="supplier-marketplace__settlement"><span>Satış {money(order.genelToplam, order.paraBirimi)}</span><span>Kesintiler {money(order.komisyonTutari + order.komisyonKdvTutari + order.tevkifatTutari + order.odemeHizmetiBedeli, order.paraBirimi)}</span><strong>Hakediş {money(order.tedarikciHakEdisi, order.paraBirimi)}</strong></div>{settlementMovements.length ? <div className="supplier-marketplace__settlement-movements"><strong>Kabul ve aktarım hareketleri</strong>{settlementMovements.map((item) => <span key={item.id}>{new Date(item.createdAt).toLocaleString("tr-TR")} · {item.kabulEdilenMiktar} kabul{item.reddedilenMiktar ? ` / ${item.reddedilenMiktar} ret` : ""} · Brüt {money(item.kabulBrutTutar, order.paraBirimi)} · Aktarılan {money(item.serbestBirakilanNetTutar, order.paraBirimi)}{item.hakEdisAktarimHatasi ? ` · Hata: ${item.hakEdisAktarimHatasi}` : item.hakEdisAktarimReferansi ? ` · ${item.hakEdisAktarimReferansi}` : ""}</span>)}</div> : null}</> : null}{order.kargoTakipNo ? <p>{order.kargoFirmasi} · {order.kargoTakipNo}</p> : null}{actions ? <div className="supplier-marketplace__order-actions">{actions}</div> : null}</section>;
}
function FilePicker({ label, accept, onChange, capture }: { label: string; accept: string; onChange: (file: File | null) => void; capture?: "environment" }) {
  const [fileName, setFileName] = React.useState("");
  return <label className="supplier-marketplace__file-picker">
    <span>{label}</span>
    <span className="supplier-marketplace__file-picker-control"><Upload size={18} aria-hidden="true" /><strong>Dosya seç</strong><span>{fileName || "Dosya seçilmedi"}</span></span>
    <input aria-label={label} type="file" accept={accept} capture={capture} onChange={(event) => {
      const file = event.target.files?.[0] ?? null;
      setFileName(file?.name ?? "");
      onChange(file);
    }} />
  </label>;
}
function Field({ label, value, onChange, type = "text", required = true, maxLength }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean; maxLength?: number }) { return <label><span>{label}{required ? <span className="supplier-marketplace__required" aria-hidden="true"> *</span> : null}</span><input required={required} type={type} step={type === "number" ? "any" : undefined} maxLength={maxLength} value={value} onChange={(event) => onChange(event.target.value)} /></label>; }
function SelectField({ label, value, onChange, options, required = false }: { label: string; value: string; onChange: (value: string) => void; options: Array<{ value: string; label: string }>; required?: boolean }) { return <label><span>{label}{required ? <span className="supplier-marketplace__required" aria-hidden="true"> *</span> : null}</span><select required={required} value={value} onChange={(event) => onChange(event.target.value)}>{options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>; }
function TextAreaField({ label, value, onChange, required = true }: { label: string; value: string; onChange: (value: string) => void; required?: boolean }) { return <label><span>{label}{required ? <span className="supplier-marketplace__required" aria-hidden="true"> *</span> : null}</span><textarea required={required} value={value} onChange={(event) => onChange(event.target.value)} rows={4} /></label>; }
function ScoreField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) { return <SelectField label={label} value={value} onChange={onChange} options={[1, 2, 3, 4, 5].map((score) => ({ value: String(score), label: `${score} / 5` }))} />; }
function Metric({ label, value }: { label: string; value: string }) { return <article><span>{label}</span><strong>{value}</strong></article>; }
function formatReceiptReason(reason: string, detail: string) { return reason === "Diğer" && detail.trim() ? `Diğer: ${detail.trim()}` : reason; }
function Modal({ title, close, submit, submitLabel, submitDisabled = false, children, wide = false }: { title: string; close: () => void; submit: () => void; submitLabel: string; submitDisabled?: boolean; children: React.ReactNode; wide?: boolean }) {
  const dialogRef = React.useRef<HTMLDivElement>(null);
  const previousFocus = React.useRef<HTMLElement | null>(null);
  const closeRef = React.useRef(close);
  React.useEffect(() => { closeRef.current = close; }, [close]);
  React.useEffect(() => {
    previousFocus.current = document.activeElement as HTMLElement | null;
    const dialog = dialogRef.current;
    const focusable = () => Array.from(dialog?.querySelectorAll<HTMLElement>('button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])') ?? []);
    focusable()[0]?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") { event.preventDefault(); closeRef.current(); return; }
      if (event.key !== "Tab") return;
      const items = focusable(); if (!items.length) return;
      const first = items[0]; const last = items[items.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => { document.removeEventListener("keydown", onKeyDown); previousFocus.current?.focus(); };
  }, []);
  const titleId = `supplier-modal-title-${title.replace(/\W+/g, "-")}`;
  return <div ref={dialogRef} className="supplier-marketplace__modal" role="dialog" aria-modal="true" aria-labelledby={titleId}><form className={wide ? "supplier-marketplace__modal-form--wide" : undefined} onSubmit={(event) => { event.preventDefault(); submit(); }}><header className="supplier-marketplace__modal-header"><h2 id={titleId}>{title}</h2><button type="button" aria-label="Kapat" onClick={close}><X /></button></header><div className="supplier-marketplace__modal-body">{children}</div><footer className="supplier-marketplace__modal-footer"><button type="submit" className="supplier-marketplace__submit" disabled={submitDisabled}>{submitLabel}</button></footer></form></div>;
}
function Empty({ text }: { text: string }) { return <div className="supplier-marketplace__empty"><PackageSearch /><strong>{text}</strong></div>; }
function money(value: number, currency: string) { return new Intl.NumberFormat("tr-TR", { style: "currency", currency }).format(value); }
function complaintStatusLabel(status: string) { return ({ Acik: "Tedarikçi yanıtı bekleniyor", Yanitlandi: "Yanıtlandı", Cozuldu: "Çözüldü", Cozulemedi: "Çözülmedi" } as Record<string, string>)[status] ?? status; }
