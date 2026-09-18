import React from "react";
import { Check, PackagePlus, PackageSearch, Plus, Search, ShieldCheck, ShoppingCart, Store, Truck, X } from "lucide-react";
import { jsonOku } from "../../shared/json";

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
  tedarikciHakEdisi: number; durum: string; kargoFirmasi: string; kargoTakipNo: string; createdAt: string;
};
type SiparisKalemi = { id: number; tedarikciSiparisId: number; tedarikciUrunId: number; sku: string; ad: string; birim: string; miktar: number; birimFiyat: number; kdvOrani: number; toplamTutar: number };
type Talep = { id: number; baslik: string; kategori: string; urunHizmet: string; miktar: number; birim: string; teslimatSehri: string; sonTeklifAt: string; aciklama: string; durum?: string; teklifSayisi?: number; teklifVerildi?: boolean };
type Teklif = { id: number; talepId: number; talepBasligi: string; birimFiyat: number; kdvOrani: number; paraBirimi: string; terminGun: number; minimumSiparis: number; not: string; durum: string; tedarikciUnvani: string };
type YonetimProfili = { id: number; unvan: string; vergiNo: string; iban: string; adres: string; yetkiliAdSoyad: string; kategoriler: string; sehir: string; dogrulamaDurumu: string };
type YonetimSiparisi = { id: number; siparisNo: string; tedarikciUnvani: string; durum: string; genelToplam: number; paraBirimi: string; hakedisDurumu: string; planlananAt?: string };
type Ekran = {
  aktifIsletmeId: number; profiller: Profil[]; talepler: Talep[]; acikTalepler: Talep[]; gelenTeklifler: Teklif[]; profil: Profil | null;
  urunler: Urun[]; benimUrunlerim: BenimUrunum[]; kaynakUrunler: Array<{ id: number; ad: string }>;
  anaSiparisler: AnaSiparis[]; siparisler: TedarikciSiparis[]; siparisKalemleri: SiparisKalemi[];
  guvenliOdemeHazir?: boolean; yonetici?: boolean; yonetimProfilleri?: YonetimProfili[]; yonetimSiparisler?: YonetimSiparisi[];
};
type ModalName = "" | "profil" | "urun" | "talep" | "teklif" | "kabul" | "kargo" | "fatura" | "dogrulama" | "itiraz" | "hakedis";

const emptyProfile = { unvan: "", kategoriler: "", sehir: "", aciklama: "", vergiNo: "", mersisNo: "", kepAdresi: "", iban: "", adres: "", yetkiliAdSoyad: "", vergiDurumu: "", sevkiyatBolgeleri: "", iadeKosullari: "", pazaryeriSozlesmeVersiyonu: "1.0", tevkifatMuaf: false, yayinda: true };
const emptyProduct = { kaynakUrunHizmetId: "", sku: "", ad: "", aciklama: "", kategori: "", birim: "Adet", birimFiyat: "", kdvOrani: "20", paraBirimi: "TRY", stokMiktari: "", minimumSiparisMiktari: "1", tahminiTeslimatGun: "2", aktif: true };
const emptyRequest = { baslik: "", kategori: "", urunHizmet: "", miktar: "", birim: "Adet", teslimatSehri: "", sonTeklifAt: "", aciklama: "" };
const emptyOffer = { birimFiyat: "", kdvOrani: "20", paraBirimi: "TRY", terminGun: "", minimumSiparis: "", not: "" };
const statusLabels: Record<string, string> = { OdemeBekliyor: "Ödeme bekliyor", SiparisVerildi: "Sipariş verildi", Odendi: "Ödendi", TedarikciOnayladi: "Tedarikçi onayladı", Hazirlaniyor: "Hazırlanıyor", SevkEdildi: "Sevk edildi", TeslimEdildi: "Teslim edildi", HakEdisBekliyor: "Hakediş bekliyor", CariOdemeBekliyor: "Cari ödeme bekliyor", Tamamlandi: "Tamamlandı", IptalEdildi: "İptal edildi", KismiIptal: "Kısmen iptal edildi", IadeBekliyor: "İade bekliyor", IadeEdildi: "İade edildi", KismiIade: "Kısmen iade edildi", Itirazli: "İtirazlı" };

export function TedarikciPazaryeriSayfasi() {
  const [data, setData] = React.useState<Ekran | null>(null);
  const [tab, setTab] = React.useState("catalog");
  const [modal, setModal] = React.useState<ModalName>("");
  const [selectedRequest, setSelectedRequest] = React.useState<Talep | null>(null);
  const [selectedOffer, setSelectedOffer] = React.useState<Teklif | null>(null);
  const [selectedOrder, setSelectedOrder] = React.useState<TedarikciSiparis | null>(null);
  const [selectedAdminOrder, setSelectedAdminOrder] = React.useState<YonetimSiparisi | null>(null);
  const [selectedProfile, setSelectedProfile] = React.useState<YonetimProfili | null>(null);
  const [query, setQuery] = React.useState("");
  const [cart, setCart] = React.useState<Record<number, number>>({});
  const [deliveryAddress, setDeliveryAddress] = React.useState("");
  const [profileForm, setProfileForm] = React.useState(emptyProfile);
  const [productForm, setProductForm] = React.useState(emptyProduct);
  const [editingProductId, setEditingProductId] = React.useState<number | null>(null);
  const [requestForm, setRequestForm] = React.useState(emptyRequest);
  const [offerForm, setOfferForm] = React.useState(emptyOffer);
  const [shippingForm, setShippingForm] = React.useState({ kargoFirmasi: "", kargoTakipNo: "" });
  const [invoiceForm, setInvoiceForm] = React.useState({ belgeNo: "", belgeUuid: "" });
  const [acceptAddress, setAcceptAddress] = React.useState("");
  const [verificationForm, setVerificationForm] = React.useState({ onaylandi: true, not: "", komisyonOrani: "8", odemeVadesiGun: "7", pspAltUyeIsyeriId: "" });
  const [adminNote, setAdminNote] = React.useState("");
  const [message, setMessage] = React.useState("");
  const [error, setError] = React.useState("");
  const [busy, setBusy] = React.useState(false);

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

  React.useEffect(() => { document.title = "Tedarikçi Pazaryeri | Systemcel"; void load(); }, [load]);

  async function send<T>(url: string, method: "POST" | "PUT", body?: unknown) {
    setBusy(true); setError("");
    try { return await jsonOku<T>(url, { method, body: body ? JSON.stringify(body) : undefined }); }
    finally { setBusy(false); }
  }
  async function complete(action: () => Promise<{ mesaj?: string }>, fallback: string) {
    try { const result = await action(); setMessage(result.mesaj || fallback); setModal(""); await load(); }
    catch (actionError) { setError(actionError instanceof Error ? actionError.message : "İşlem tamamlanamadı."); }
  }

  const normalizedQuery = query.toLocaleLowerCase("tr");
  const products = (data?.urunler ?? []).filter((product) => `${product.ad} ${product.kategori} ${product.tedarikciUnvani} ${product.tedarikciSehri}`.toLocaleLowerCase("tr").includes(normalizedQuery));
  const suppliers = (data?.profiller ?? []).filter((profile) => `${profile.unvan} ${profile.kategoriler} ${profile.sehir}`.toLocaleLowerCase("tr").includes(normalizedQuery));
  const cartRows = Object.entries(cart).map(([id, quantity]) => ({ product: data?.urunler.find((item) => item.id === Number(id)), quantity })).filter((row): row is { product: Urun; quantity: number } => Boolean(row.product));
  const cartTotal = cartRows.reduce((sum, row) => sum + row.quantity * row.product.birimFiyat * (1 + row.product.kdvOrani / 100), 0);
  const supplierCount = new Set(cartRows.map((row) => row.product.tedarikciProfilId)).size;

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
    await complete(() => send(`/api/ekran/yonetim/tedarikci-siparisler/${selectedAdminOrder.id}/itiraz-coz`, "POST", { devamEt: true, not: adminNote }), "İtiraz kapatıldı.");
    setSelectedAdminOrder(null); setAdminNote("");
  }
  async function completeSettlement() {
    if (!selectedAdminOrder) return;
    await complete(() => send(`/api/ekran/yonetim/tedarikci-siparisler/${selectedAdminOrder.id}/hakedis`, "POST", { aktarimReferansi: adminNote }), "Hakediş tamamlandı.");
    setSelectedAdminOrder(null); setAdminNote("");
  }

  return <main className="supplier-marketplace" aria-busy={busy}>
    <section className="supplier-marketplace__hero"><div><span className="supplier-marketplace__eyebrow"><Store size={15} /> TEDARİK AĞI</span><h1>Tek sepet. Birden fazla tedarikçi.</h1><p>{data?.guvenliOdemeHazir ? "Ödeme güvenli biçimde alınır; tedarikçi hakedişi teslimat onayında serbest bırakılır." : "Ürünleri karşılaştır, siparişini oluştur; teslimatı, faturayı ve cari ödemeyi tek yerden takip et."}</p></div><div className="supplier-marketplace__hero-actions"><button className="supplier-marketplace__hero-action" onClick={() => setTab("cart")}><ShoppingCart />Sepet ({cartRows.length})</button><button className="supplier-marketplace__hero-action supplier-marketplace__hero-action--secondary" onClick={() => setModal("talep")}><Plus />Alım talebi oluştur</button></div></section>
    <nav className="supplier-marketplace__tabs" aria-label="Pazaryeri bölümleri"><TabButton active={tab === "catalog"} onClick={() => setTab("catalog")}>Ürünler</TabButton><TabButton active={tab === "cart"} onClick={() => setTab("cart")}>Sepet</TabButton><TabButton active={tab === "orders"} onClick={() => setTab("orders")}>Siparişler</TabButton><TabButton active={tab === "sales"} onClick={() => setTab("sales")}>Satışlarım</TabButton><TabButton active={tab === "requests"} onClick={() => setTab("requests")}>Teklif talepleri</TabButton>{data?.yonetici ? <TabButton active={tab === "admin"} onClick={() => setTab("admin")}>Yönetim</TabButton> : null}<button onClick={() => setModal("profil")}><Store size={16} />Tedarikçi hesabım</button></nav>
    {message ? <p className="supplier-marketplace__notice"><Check size={18} />{message}</p> : null}{error ? <p className="supplier-marketplace__error" role="alert">{error}</p> : null}

    {tab === "catalog" ? <><section className="supplier-marketplace__search"><Search aria-hidden="true" /><input aria-label="Tedarikçi ara" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Ürün, kategori veya tedarikçi ara" /></section><section className="supplier-marketplace__catalog">{products.map((product) => <article key={product.id}><div className="supplier-marketplace__product-head"><span className="supplier-marketplace__avatar">{product.tedarikciUnvani.slice(0, 2).toUpperCase()}</span><div><strong>{product.tedarikciUnvani}</strong><small>{product.tedarikciSehri} · {product.tahminiTeslimatGun} gün</small></div><ShieldCheck size={18} /></div><div><small>{product.kategori} · {product.sku}</small><h2>{product.ad}</h2><p>{product.aciklama}</p></div><div className="supplier-marketplace__product-foot"><div><strong>{money(product.birimFiyat * (1 + product.kdvOrani / 100), product.paraBirimi)}</strong><small>KDV dahil · {product.birim}</small></div><button onClick={() => addToCart(product)}>Sepete ekle</button></div></article>)}{data && products.length === 0 ? <Empty text="Bu aramaya uygun ürün yok." /> : null}</section>{suppliers.length > 0 ? <section className="supplier-marketplace__supplier-strip">{suppliers.map((profile) => <span key={profile.id}><ShieldCheck size={14} />{profile.unvan}</span>)}</section> : null}</> : null}

    {tab === "cart" ? <section className="supplier-marketplace__cart"><header><div><h2>Sepet</h2><p>{supplierCount} tedarikçiden {cartRows.length} ürün</p></div><strong>{money(cartTotal, cartRows[0]?.product.paraBirimi ?? "TRY")}</strong></header>{cartRows.map(({ product, quantity }) => <article key={product.id}><div><strong>{product.ad}</strong><span>{product.tedarikciUnvani} · {money(product.birimFiyat * (1 + product.kdvOrani / 100), product.paraBirimi)}</span></div><label><span>Miktar</span><input type="number" min={product.minimumSiparisMiktari} max={product.kullanilabilirStok} step="1" value={quantity} onChange={(event) => setCartQuantity(product, Number(event.target.value))} /></label><button aria-label={`${product.ad} ürününü sepetten çıkar`} onClick={() => setCartQuantity(product, 0)}><X /></button></article>)}{cartRows.length === 0 ? <Empty text="Sepetiniz boş." /> : <div className="supplier-marketplace__checkout"><Field label="Teslimat adresi" value={deliveryAddress} onChange={setDeliveryAddress} /><p>{data?.guvenliOdemeHazir ? "Ödeme şimdi alınır; tedarikçiye yalnız teslimatı onayladığınızda aktarılır." : "Güvenli ödeme sağlayıcısı henüz bağlı değil; tutar teslimat onayında cariye işlenir."}</p><button disabled={busy} onClick={() => void createOrder()}><ShoppingCart />{data?.guvenliOdemeHazir ? "Güvenli öde ve sipariş ver" : "Siparişi oluştur"}</button></div>}</section> : null}

    {tab === "orders" ? <section className="supplier-marketplace__orders">{(data?.anaSiparisler ?? []).map((master) => { const childOrders = data?.siparisler.filter((order) => order.anaSiparisId === master.id) ?? []; return <article key={master.id} className="supplier-marketplace__order-group"><header><div><small>{new Date(master.createdAt).toLocaleDateString("tr-TR")}</small><h2>{master.siparisNo}</h2><span>{statusLabels[master.durum] ?? master.durum}</span></div><strong>{money(master.genelToplam, master.paraBirimi)}</strong></header>{childOrders.map((order) => { const canCancel = ["OdemeBekliyor", "SiparisVerildi", "Odendi", "TedarikciOnayladi", "Hazirlaniyor"].includes(order.durum); return <OrderRow key={order.id} order={order} lines={data?.siparisKalemleri.filter((line) => line.tedarikciSiparisId === order.id) ?? []} actions={<>{order.durum === "SevkEdildi" ? <button onClick={() => void updateOrderState(order, "TeslimEdildi")}>Teslim aldım</button> : null}{order.durum === "CariOdemeBekliyor" ? <a href="/app/tahsilat-odeme">Ödemeyi kaydet</a> : null}{canCancel ? <button onClick={() => void cancelSupplierOrder(order)}>Bu satıcıyı iptal et</button> : null}</>} />; })}{master.durum === "OdemeBekliyor" ? <div className="supplier-marketplace__order-actions"><button onClick={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/siparisler/${master.id}/odeme`, "POST", { idempotencyKey: `payment-${crypto.randomUUID()}` }), "Ödeme tamamlandı.")}>Öde</button><button onClick={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/siparisler/${master.id}/iptal`, "POST", { neden: "Alıcı iptali" }), "Sipariş iptal edildi.")}>Tümünü iptal et</button></div> : null}</article>; })}{data && data.anaSiparisler.length === 0 ? <Empty text="Henüz siparişiniz yok." /> : null}</section> : null}

    {tab === "sales" ? <section className="supplier-marketplace__orders"><div className="supplier-marketplace__section-head"><div><h2>Satışlarım</h2><p>Sipariş, kargo ve hakedişler</p></div><button onClick={() => { setEditingProductId(null); setProductForm(emptyProduct); setModal("urun"); }}><PackagePlus />Ürün ekle</button></div>{!data?.profil?.dogrulandi ? <p className="supplier-marketplace__notice">Ürün yayınlamak için tedarikçi başvurunuzu tamamlayın.</p> : null}{(data?.siparisler ?? []).filter((order) => order.tedarikciIsletmeId === data?.aktifIsletmeId).map((order) => { let action: React.ReactNode = null; if (["SiparisVerildi", "Odendi"].includes(order.durum)) action = <><button onClick={() => void updateOrderState(order, "TedarikciOnayladi")}>Siparişi onayla</button><button onClick={() => void cancelSupplierOrder(order)}>İptal et</button></>; if (order.durum === "TedarikciOnayladi") action = <><button onClick={() => void updateOrderState(order, "Hazirlaniyor")}>Hazırlamaya başla</button><button onClick={() => void cancelSupplierOrder(order)}>İptal et</button></>; if (order.durum === "Hazirlaniyor") action = <><button onClick={() => { setSelectedOrder(order); setModal("kargo"); }}>Kargoya ver</button><button onClick={() => void cancelSupplierOrder(order)}>İptal et</button></>; if (["TeslimEdildi", "HakEdisBekliyor", "CariOdemeBekliyor"].includes(order.durum)) action = <button onClick={() => { setSelectedOrder(order); setModal("fatura"); }}>Fatura numarası ekle</button>; return <OrderRow key={order.id} order={order} lines={data?.siparisKalemleri.filter((line) => line.tedarikciSiparisId === order.id) ?? []} actions={action} showSettlement />; })}<div className="supplier-marketplace__inventory">{(data?.benimUrunlerim ?? []).map((product) => <article key={product.id}><div><strong>{product.ad}</strong><span>{product.sku} · {product.kategori}</span></div><b>{product.stokMiktari - product.rezerveMiktar} {product.birim}</b><button aria-label={`${product.ad} ürününü düzenle`} onClick={() => editProduct(product)}>Düzenle</button>{product.aktif ? <button onClick={() => void deactivateProduct(product)}>Pasife al</button> : <span>Pasif</span>}</article>)}</div></section> : null}

    {tab === "requests" ? <section className="supplier-marketplace__request-space"><div className="supplier-marketplace__request-grid">{(data?.acikTalepler ?? []).map((request) => <article key={request.id}><small>{request.kategori} · {request.teslimatSehri}</small><h3>{request.baslik}</h3><p>{request.urunHizmet}</p><strong>{request.miktar} {request.birim}</strong><span>{new Date(request.sonTeklifAt).toLocaleDateString("tr-TR")} tarihine kadar</span><button disabled={request.teklifVerildi} onClick={() => { setSelectedRequest(request); setModal("teklif"); }}>{request.teklifVerildi ? "Teklif verildi" : "Teklif ver"}</button></article>)}</div><section className="supplier-marketplace__offers"><h2>Alım taleplerim</h2>{(data?.talepler ?? []).map((request) => <article key={request.id}><div><strong>{request.baslik}</strong><span>{request.miktar} {request.birim} · {request.durum}</span></div><b>{request.teklifSayisi} teklif</b></article>)}<h2>Gelen teklifler</h2>{(data?.gelenTeklifler ?? []).map((offer) => <article key={offer.id}><div><strong>{offer.tedarikciUnvani}</strong><span>{offer.talepBasligi} · {offer.terminGun} gün · min. {offer.minimumSiparis} · KDV %{offer.kdvOrani}</span></div><b>{money(offer.birimFiyat, offer.paraBirimi)}</b>{offer.durum === "Gonderildi" ? <button onClick={() => { setSelectedOffer(offer); setAcceptAddress(""); setModal("kabul"); }}>Kabul et ve sipariş oluştur</button> : <em>{offer.durum}</em>}</article>)}</section></section> : null}

    {tab === "admin" && data?.yonetici ? <section className="supplier-marketplace__orders"><div className="supplier-marketplace__section-head"><div><h2>Tedarikçi yönetimi</h2><p>Doğrulama, itiraz ve hakediş işlemleri</p></div></div><section className="supplier-marketplace__offers"><h2>Doğrulama bekleyenler</h2>{(data.yonetimProfilleri ?? []).map((profile) => <article key={profile.id}><div><strong>{profile.unvan}</strong><span>{profile.sehir} · {profile.vergiNo} · {profile.yetkiliAdSoyad}</span></div><button onClick={() => { setSelectedProfile(profile); setVerificationForm({ onaylandi: true, not: "", komisyonOrani: "8", odemeVadesiGun: "7", pspAltUyeIsyeriId: "" }); setModal("dogrulama"); }}>İncele</button></article>)}{(data.yonetimProfilleri ?? []).length === 0 ? <Empty text="Doğrulama bekleyen başvuru yok." /> : null}<h2>İtiraz ve hakedişler</h2>{(data.yonetimSiparisler ?? []).map((order) => <article key={order.id}><div><strong>{order.siparisNo} · {order.tedarikciUnvani}</strong><span>{statusLabels[order.durum] ?? order.durum} · {order.hakedisDurumu}{order.planlananAt ? ` · ${new Date(order.planlananAt).toLocaleDateString("tr-TR")}` : ""}</span></div><b>{money(order.genelToplam, order.paraBirimi)}</b>{order.durum === "Itirazli" ? <button onClick={() => { setSelectedAdminOrder(order); setAdminNote(""); setModal("itiraz"); }}>İtirazı çöz</button> : <button onClick={() => { setSelectedAdminOrder(order); setAdminNote(""); setModal("hakedis"); }}>Hakedişi tamamla</button>}</article>)}{(data.yonetimSiparisler ?? []).length === 0 ? <Empty text="Bekleyen yönetim işlemi yok." /> : null}</section></section> : null}

    {modal === "profil" ? <Modal wide title="Tedarikçi hesabı" close={() => setModal("")} submit={() => void saveProfile()} submitLabel="Başvuruyu kaydet"><Field label="Ticaret unvanı" value={profileForm.unvan} onChange={(value) => setProfileForm({ ...profileForm, unvan: value })} /><Field label="Kategoriler" value={profileForm.kategoriler} onChange={(value) => setProfileForm({ ...profileForm, kategoriler: value })} /><Field label="Şehir" value={profileForm.sehir} onChange={(value) => setProfileForm({ ...profileForm, sehir: value })} /><Field label="Tanıtım" value={profileForm.aciklama} onChange={(value) => setProfileForm({ ...profileForm, aciklama: value })} /><Field label="Vergi numarası" value={profileForm.vergiNo} onChange={(value) => setProfileForm({ ...profileForm, vergiNo: value })} /><Field label="MERSİS numarası" value={profileForm.mersisNo} onChange={(value) => setProfileForm({ ...profileForm, mersisNo: value })} required={false} /><Field label="KEP adresi" value={profileForm.kepAdresi} onChange={(value) => setProfileForm({ ...profileForm, kepAdresi: value })} required={false} /><Field label="IBAN" value={profileForm.iban} onChange={(value) => setProfileForm({ ...profileForm, iban: value })} /><Field label="Şirket adresi" value={profileForm.adres} onChange={(value) => setProfileForm({ ...profileForm, adres: value })} /><Field label="Yetkili kişi" value={profileForm.yetkiliAdSoyad} onChange={(value) => setProfileForm({ ...profileForm, yetkiliAdSoyad: value })} /><Field label="Vergi durumu" value={profileForm.vergiDurumu} onChange={(value) => setProfileForm({ ...profileForm, vergiDurumu: value })} required={false} /><Field label="Sevkiyat bölgeleri" value={profileForm.sevkiyatBolgeleri} onChange={(value) => setProfileForm({ ...profileForm, sevkiyatBolgeleri: value })} required={false} /><Field label="İade koşulları" value={profileForm.iadeKosullari} onChange={(value) => setProfileForm({ ...profileForm, iadeKosullari: value })} required={false} /><Field label="Sözleşme sürümü" value={profileForm.pazaryeriSozlesmeVersiyonu} onChange={(value) => setProfileForm({ ...profileForm, pazaryeriSozlesmeVersiyonu: value })} /><label className="supplier-marketplace__check"><input type="checkbox" checked={profileForm.tevkifatMuaf} onChange={(event) => setProfileForm({ ...profileForm, tevkifatMuaf: event.target.checked })} />Tevkifat istisnası var</label>{data?.profil?.dogrulamaDurumu ? <p>Durum: {data.profil.dogrulamaDurumu}</p> : null}</Modal> : null}
    {modal === "urun" ? <Modal title={editingProductId === null ? "Ürün ekle" : "Ürünü düzenle"} close={() => { setModal(""); setEditingProductId(null); setProductForm(emptyProduct); }} submit={() => void saveProduct()} submitLabel={editingProductId === null ? "Ürünü yayınla" : "Ürünü güncelle"}><Field label="Stok kodu" value={productForm.sku} onChange={(value) => setProductForm({ ...productForm, sku: value })} /><Field label="Ürün adı" value={productForm.ad} onChange={(value) => setProductForm({ ...productForm, ad: value })} /><Field label="Kategori" value={productForm.kategori} onChange={(value) => setProductForm({ ...productForm, kategori: value })} /><Field label="Açıklama" value={productForm.aciklama} onChange={(value) => setProductForm({ ...productForm, aciklama: value })} required={false} /><Field label="Birim" value={productForm.birim} onChange={(value) => setProductForm({ ...productForm, birim: value })} /><Field label="KDV hariç fiyat" value={productForm.birimFiyat} onChange={(value) => setProductForm({ ...productForm, birimFiyat: value })} type="number" /><Field label="KDV oranı" value={productForm.kdvOrani} onChange={(value) => setProductForm({ ...productForm, kdvOrani: value })} type="number" /><Field label="Stok" value={productForm.stokMiktari} onChange={(value) => setProductForm({ ...productForm, stokMiktari: value })} type="number" /><Field label="Minimum sipariş" value={productForm.minimumSiparisMiktari} onChange={(value) => setProductForm({ ...productForm, minimumSiparisMiktari: value })} type="number" /><Field label="Tahmini teslimat günü" value={productForm.tahminiTeslimatGun} onChange={(value) => setProductForm({ ...productForm, tahminiTeslimatGun: value })} type="number" /><label className="supplier-marketplace__check"><input type="checkbox" checked={productForm.aktif} onChange={(event) => setProductForm({ ...productForm, aktif: event.target.checked })} />Ürün aktif</label></Modal> : null}
    {modal === "talep" ? <Modal title="Alım talebi oluştur" close={() => setModal("")} submit={() => void complete(() => send("/api/ekran/tedarikci-pazaryeri/talepler", "POST", { ...requestForm, miktar: Number(requestForm.miktar) }), "Alım talebi yayınlandı.")} submitLabel="Talebi yayınla">{(["baslik", "kategori", "urunHizmet", "miktar", "birim", "teslimatSehri", "sonTeklifAt", "aciklama"] as const).map((key) => <Field key={key} label={{ baslik: "Başlık", kategori: "Kategori", urunHizmet: "Ürün veya hizmet", miktar: "Miktar", birim: "Birim", teslimatSehri: "Teslimat şehri", sonTeklifAt: "Son teklif tarihi", aciklama: "Açıklama" }[key]} type={key === "miktar" ? "number" : key === "sonTeklifAt" ? "datetime-local" : "text"} value={requestForm[key]} onChange={(value) => setRequestForm({ ...requestForm, [key]: value })} />)}</Modal> : null}
    {modal === "teklif" ? <Modal title={`${selectedRequest?.baslik ?? "Talep"} için teklif`} close={() => setModal("")} submit={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/talepler/${selectedRequest?.id}/teklifler`, "POST", { ...offerForm, birimFiyat: Number(offerForm.birimFiyat), kdvOrani: Number(offerForm.kdvOrani), terminGun: Number(offerForm.terminGun), minimumSiparis: Number(offerForm.minimumSiparis) }), "Teklif gönderildi.")} submitLabel="Teklifi gönder">{(["birimFiyat", "kdvOrani", "paraBirimi", "terminGun", "minimumSiparis", "not"] as const).map((key) => <Field key={key} label={{ birimFiyat: "Birim fiyat", kdvOrani: "KDV oranı", paraBirimi: "Para birimi", terminGun: "Termin günü", minimumSiparis: "Minimum sipariş", not: "Not" }[key]} type={["birimFiyat", "kdvOrani", "terminGun", "minimumSiparis"].includes(key) ? "number" : "text"} value={offerForm[key]} onChange={(value) => setOfferForm({ ...offerForm, [key]: value })} />)}</Modal> : null}
    {modal === "kabul" && selectedOffer ? <Modal title="Teklifi siparişe dönüştür" close={() => { setModal(""); setSelectedOffer(null); }} submit={() => void acceptOffer()} submitLabel={data?.guvenliOdemeHazir ? "Güvenli öde ve sipariş ver" : "Siparişi oluştur"}><p>{data?.guvenliOdemeHazir ? `${selectedOffer.tedarikciUnvani} için ödeme şimdi alınır ve teslimat onayına kadar blokede tutulur.` : `${selectedOffer.tedarikciUnvani} teklifindeki tutar teslimat onayında cariye işlenecek.`}</p><Field label="Teslimat adresi" value={acceptAddress} onChange={setAcceptAddress} /></Modal> : null}
    {modal === "kargo" && selectedOrder ? <Modal title="Kargoya ver" close={() => setModal("")} submit={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${selectedOrder.id}/durum`, "POST", { durum: "SevkEdildi", ...shippingForm, aciklama: "" }), "Kargo bilgileri kaydedildi.")} submitLabel="Kargoya ver"><Field label="Kargo firması" value={shippingForm.kargoFirmasi} onChange={(value) => setShippingForm({ ...shippingForm, kargoFirmasi: value })} /><Field label="Takip numarası" value={shippingForm.kargoTakipNo} onChange={(value) => setShippingForm({ ...shippingForm, kargoTakipNo: value })} /></Modal> : null}
    {modal === "fatura" && selectedOrder ? <Modal title="Faturayı eşleştir" close={() => setModal("")} submit={() => void complete(() => send(`/api/ekran/tedarikci-pazaryeri/tedarikci-siparisler/${selectedOrder.id}/fatura`, "POST", invoiceForm), "Fatura eşleştirildi.")} submitLabel="Faturayı eşleştir"><Field label="Fatura numarası" value={invoiceForm.belgeNo} onChange={(value) => setInvoiceForm({ ...invoiceForm, belgeNo: value })} /><Field label="Fatura UUID" value={invoiceForm.belgeUuid} onChange={(value) => setInvoiceForm({ ...invoiceForm, belgeUuid: value })} required={false} /></Modal> : null}
    {modal === "dogrulama" && selectedProfile ? <Modal title={`${selectedProfile.unvan} başvurusu`} close={() => { setModal(""); setSelectedProfile(null); }} submit={() => void verifySupplier()} submitLabel={verificationForm.onaylandi ? "Başvuruyu onayla" : "Başvuruyu reddet"}><label className="supplier-marketplace__check"><input type="checkbox" checked={verificationForm.onaylandi} onChange={(event) => setVerificationForm({ ...verificationForm, onaylandi: event.target.checked })} />Başvuruyu onayla</label><Field label="Komisyon oranı" type="number" value={verificationForm.komisyonOrani} onChange={(value) => setVerificationForm({ ...verificationForm, komisyonOrani: value })} /><Field label="Ödeme vadesi (gün)" type="number" value={verificationForm.odemeVadesiGun} onChange={(value) => setVerificationForm({ ...verificationForm, odemeVadesiGun: value })} /><Field label="PSP alt üye işyeri kimliği" value={verificationForm.pspAltUyeIsyeriId} onChange={(value) => setVerificationForm({ ...verificationForm, pspAltUyeIsyeriId: value })} required={false} /><Field label="Yönetici notu" value={verificationForm.not} onChange={(value) => setVerificationForm({ ...verificationForm, not: value })} /></Modal> : null}
    {modal === "itiraz" && selectedAdminOrder ? <Modal title={`${selectedAdminOrder.siparisNo} itirazını çöz`} close={() => { setModal(""); setSelectedAdminOrder(null); }} submit={() => void resolveDispute()} submitLabel="İtirazı kapat"><Field label="Çözüm notu" value={adminNote} onChange={setAdminNote} /></Modal> : null}
    {modal === "hakedis" && selectedAdminOrder ? <Modal title={`${selectedAdminOrder.siparisNo} hakedişi`} close={() => { setModal(""); setSelectedAdminOrder(null); }} submit={() => void completeSettlement()} submitLabel="Hakedişi tamamla"><Field label="Aktarım referansı" value={adminNote} onChange={setAdminNote} /></Modal> : null}
  </main>;
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) { return <button className={active ? "active" : ""} aria-pressed={active} onClick={onClick}>{children}</button>; }
function OrderRow({ order, lines, actions, showSettlement = false }: { order: TedarikciSiparis; lines: SiparisKalemi[]; actions: React.ReactNode; showSettlement?: boolean }) {
  return <section className="supplier-marketplace__order-row"><div className="supplier-marketplace__order-title"><Truck /><div><strong>{order.tedarikciUnvani}</strong><span>{order.siparisNo} · {statusLabels[order.durum] ?? order.durum}</span></div></div><ul>{lines.map((line) => <li key={line.id}><span>{line.ad} · {line.miktar} {line.birim}</span><b>{money(line.toplamTutar, order.paraBirimi)}</b></li>)}</ul>{showSettlement ? <div className="supplier-marketplace__settlement"><span>Satış {money(order.genelToplam, order.paraBirimi)}</span><span>Kesintiler {money(order.komisyonTutari + order.komisyonKdvTutari + order.tevkifatTutari + order.odemeHizmetiBedeli, order.paraBirimi)}</span><strong>Hakediş {money(order.tedarikciHakEdisi, order.paraBirimi)}</strong></div> : null}{order.kargoTakipNo ? <p>{order.kargoFirmasi} · {order.kargoTakipNo}</p> : null}{actions ? <div className="supplier-marketplace__order-actions">{actions}</div> : null}</section>;
}
function Field({ label, value, onChange, type = "text", required = true }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean }) { return <label><span>{label}</span><input required={required} type={type} value={value} onChange={(event) => onChange(event.target.value)} /></label>; }
function Modal({ title, close, submit, submitLabel, children, wide = false }: { title: string; close: () => void; submit: () => void; submitLabel: string; children: React.ReactNode; wide?: boolean }) { return <div className="supplier-marketplace__modal" role="dialog" aria-modal="true" aria-label={title}><form className={wide ? "supplier-marketplace__modal-form--wide" : undefined} onSubmit={(event) => { event.preventDefault(); submit(); }}><header className="supplier-marketplace__modal-header"><h2>{title}</h2><button type="button" aria-label="Kapat" onClick={close}><X /></button></header><div className="supplier-marketplace__modal-body">{children}</div><footer className="supplier-marketplace__modal-footer"><button className="supplier-marketplace__submit">{submitLabel}</button></footer></form></div>; }
function Empty({ text }: { text: string }) { return <div className="supplier-marketplace__empty"><PackageSearch /><strong>{text}</strong></div>; }
function money(value: number, currency: string) { return new Intl.NumberFormat("tr-TR", { style: "currency", currency }).format(value); }
