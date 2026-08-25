import React from "react";
import {
  Banknote,
  Barcode,
  Camera,
  Check,
  CreditCard,
  Loader2,
  Minus,
  PackageOpen,
  Plus,
  Search,
  ScanBarcode,
  ShoppingCart,
  ReceiptText,
  Trash2,
  WalletCards
} from "lucide-react";
import { jsonOku } from "../../shared/json";
import { useI18n } from "../../shared/i18n";
import type {
  HizliSatisSepetSatiri,
  HizliSatisSonucu,
  UrunListeKaydi,
  UrunStokEkranVerisi
} from "./types";
import "./hizli-satis.css";

interface HizliSatisSayfasiProps {
  yenileAnahtari: number;
  onKayitOlusturuldu?: () => void;
}

interface FisOcrSonucu {
  merchant: string;
  receiptDate?: string | null;
  paymentMethod: string;
  receiptTotal?: number | null;
  items: Array<{ rawName: string; amount: number; candidateKalem: string }>;
}

interface GelirGiderEkranOzeti {
  giderKalemleri: string[];
  odemeYontemleri: Array<{ deger: string; etiket: string }>;
}

interface GiderTaslagi {
  tarih: string;
  tutar: string;
  odemeYontemi: string;
  kalem: string;
  aciklama: string;
}

interface BarcodeDetectorLike {
  detect(source: ImageBitmapSource): Promise<Array<{ rawValue: string }>>;
}

type BarcodeDetectorConstructor = new (options?: { formats?: string[] }) => BarcodeDetectorLike;

const odemeYontemleri = [
  { value: "Nakit", label: "Nakit", icon: Banknote },
  { value: "KrediKarti", label: "Kredi kartı", icon: CreditCard },
  { value: "Havale", label: "Havale / EFT", icon: WalletCards },
  { value: "OnlineOdeme", label: "Online ödeme", icon: Check }
];

function yeniIslemAnahtari() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `hizli-satis-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function paraBic(value: number) {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value);
}

function sayiBic(value: number) {
  return new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2
  }).format(value);
}

export function HizliSatisSayfasi({ yenileAnahtari, onKayitOlusturuldu }: HizliSatisSayfasiProps) {
  const { t } = useI18n();
  const barkodRef = React.useRef<HTMLInputElement | null>(null);
  const barkodKameraRef = React.useRef<HTMLInputElement | null>(null);
  const fisKameraRef = React.useRef<HTMLInputElement | null>(null);
  const giderKayitRef = React.useRef(false);
  const [ekran, setEkran] = React.useState<UrunStokEkranVerisi | null>(null);
  const [arama, setArama] = React.useState("");
  const [sepet, setSepet] = React.useState<HizliSatisSepetSatiri[]>([]);
  const [odemeYontemi, setOdemeYontemi] = React.useState("Nakit");
  const [islemAnahtari, setIslemAnahtari] = React.useState(() => yeniIslemAnahtari());
  const [islemde, setIslemde] = React.useState(false);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");
  const [taramaDurumu, setTaramaDurumu] = React.useState<"idle" | "loading" | "success" | "error" | "unsupported">("idle");
  const [taramaMesaji, setTaramaMesaji] = React.useState("");
  const [fisOcrHazir, setFisOcrHazir] = React.useState<boolean | null>(null);
  const [fisSonucu, setFisSonucu] = React.useState<FisOcrSonucu | null>(null);
  const [fisIslemde, setFisIslemde] = React.useState(false);
  const [giderTaslagi, setGiderTaslagi] = React.useState<GiderTaslagi | null>(null);
  const [giderKalemleri, setGiderKalemleri] = React.useState<string[]>([]);
  const [giderOdemeYontemleri, setGiderOdemeYontemleri] = React.useState<Array<{ deger: string; etiket: string }>>([]);
  const [giderKaydediliyor, setGiderKaydediliyor] = React.useState(false);

  const urunler = React.useMemo(
    () => (ekran?.urunler ?? []).filter((row) => row.aktif && row.tip === "Urun"),
    [ekran]
  );

  const filtreliUrunler = React.useMemo(() => {
    const query = arama.trim().toLocaleLowerCase("tr-TR");
    if (!query) return urunler;
    return urunler.filter((row) =>
      row.ad.toLocaleLowerCase("tr-TR").includes(query) ||
      row.barkod.toLocaleLowerCase("tr-TR").includes(query)
    );
  }, [arama, urunler]);

  const sepetAdedi = React.useMemo(
    () => sepet.reduce((total, row) => total + row.miktar, 0),
    [sepet]
  );

  const sepetToplami = React.useMemo(
    () => sepet.reduce((total, row) => total + row.satisFiyati * row.miktar, 0),
    [sepet]
  );

  const stoktaUrunSayisi = React.useMemo(
    () => urunler.filter((row) => row.mevcutStok > 0).length,
    [urunler]
  );

  const urunleriYukle = React.useCallback(async () => {
    setYukleniyor(true);
    setHata("");
    try {
      const data = await jsonOku<UrunStokEkranVerisi>("/api/ekran/urun-stok");
      setEkran(data);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Ürünler yüklenemedi.");
    } finally {
      setYukleniyor(false);
    }
  }, []);

  React.useEffect(() => {
    void urunleriYukle();
  }, [urunleriYukle, yenileAnahtari]);

  React.useEffect(() => {
    jsonOku<{ fisOcrHazir: boolean }>("/api/ekran/mobil-tarama/durum")
      .then((result) => setFisOcrHazir(result.fisOcrHazir))
      .catch(() => setFisOcrHazir(false));
  }, []);

  React.useEffect(() => {
    const handle = window.setTimeout(() => barkodRef.current?.focus(), 100);
    return () => window.clearTimeout(handle);
  }, []);

  function sepeteEkle(product: UrunListeKaydi) {
    setHata("");
    setMesaj("");
    if (product.mevcutStok <= 0) {
      setHata(`${product.ad} stokta bulunmuyor.`);
      return;
    }
    if (product.satisFiyati <= 0) {
      setHata(`${product.ad} için satış fiyatı girilmemiş.`);
      return;
    }

    const existing = sepet.find((row) => row.id === product.id);
    const nextQuantity = (existing?.miktar ?? 0) + 1;
    if (nextQuantity > product.mevcutStok) {
      setHata(`${product.ad} için en fazla ${sayiBic(product.mevcutStok)} adet satabilirsiniz.`);
      return;
    }

    setSepet((current) => existing
      ? current.map((row) => row.id === product.id ? { ...row, miktar: nextQuantity } : row)
      : [...current, { ...product, miktar: 1 }]);
    setMesaj(`${product.ad} sepete eklendi.`);
  }

  function barkoduIsle() {
    const barcode = arama.trim();
    if (!barcode) {
      setHata("Barkod okutun veya ürün arayın.");
      barkodRef.current?.focus();
      return;
    }

    barkodlaUrunEkle(barcode);
  }

  function barkodlaUrunEkle(barcode: string) {
    const product = urunler.find((row) => row.barkod.trim() === barcode.trim());
    if (!product) {
      setHata("Bu barkoda ait aktif bir ürün bulunamadı.");
      return false;
    }

    sepeteEkle(product);
    setArama("");
    window.setTimeout(() => barkodRef.current?.focus(), 20);
    return true;
  }

  async function barkodFotografiSecildi(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    setTaramaDurumu("loading");
    setTaramaMesaji("Barkod okunuyor...");
    setHata("");
    try {
      let barcode = await barkoduYerelOku(file);
      if (!barcode) {
        const form = new FormData();
        form.append("file", file);
        const result = await jsonOku<{ barkod: string }>("/api/ekran/mobil-tarama/barkod", { method: "POST", body: form });
        barcode = result.barkod;
      }

      if (!barkodlaUrunEkle(barcode)) {
        setTaramaDurumu("error");
        setTaramaMesaji(`${barcode} barkodu okundu ancak aktif ürünlerde bulunamadı.`);
        return;
      }
      setTaramaDurumu("success");
      setTaramaMesaji(`${barcode} barkodu okundu ve sepete eklendi.`);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Barkod okunamadı. Barkodu kadraja yaklaştırıp tekrar deneyin.";
      setTaramaDurumu(/platformda aktif değil|platformda aktif degil|desteklenmiyor/i.test(errorMessage) ? "unsupported" : "error");
      setTaramaMesaji(errorMessage);
    }
  }

  async function fisFotografiSecildi(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    setFisIslemde(true);
    setFisSonucu(null);
    setGiderTaslagi(null);
    setHata("");
    try {
      const form = new FormData();
      form.append("file", file);
      const [result, ledger] = await Promise.all([
        jsonOku<FisOcrSonucu>("/api/ekran/mobil-tarama/fis-ocr", { method: "POST", body: form }),
        jsonOku<GelirGiderEkranOzeti>("/api/ekran/gelir-gider")
      ]);
      setFisSonucu(result);
      setGiderKalemleri(ledger.giderKalemleri);
      setGiderOdemeYontemleri(ledger.odemeYontemleri);
      setGiderTaslagi(fisTaslagiOlustur(result, ledger.giderKalemleri, ledger.odemeYontemleri));
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Fiş okunamadı. Fotoğrafı daha aydınlık bir ortamda tekrar çekin.");
    } finally {
      setFisIslemde(false);
    }
  }

  function giderTaslaginiGuncelle(patch: Partial<GiderTaslagi>) {
    setGiderTaslagi((current) => current ? { ...current, ...patch } : current);
  }

  async function giderOlarakKaydet() {
    if (!giderTaslagi || giderKayitRef.current) return;

    try {
      const tarih = giderTaslagi.tarih.trim();
      if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(tarih) || !Number.isFinite(Date.parse(tarih)))
        throw new Error("Geçerli bir fiş tarihi girin.");
      const tutar = Number(giderTaslagi.tutar.trim().replace(/\s/g, "").replace(",", "."));
      if (!Number.isFinite(tutar) || tutar <= 0)
        throw new Error("Fiş toplamı sıfırdan büyük olmalıdır.");
      if (!giderTaslagi.kalem.trim())
        throw new Error("Gider kalemi seçin.");

      giderKayitRef.current = true;
      setGiderKaydediliyor(true);
      setHata("");
      await jsonOku("/api/ekran/gelir-gider/kayitlar", {
        method: "POST",
        body: JSON.stringify({
          tarih,
          tur: "gider",
          tutar,
          odemeYontemi: giderTaslagi.odemeYontemi,
          kalem: giderTaslagi.kalem,
          aciklama: giderTaslagi.aciklama,
          stokGiris: { aktif: false, urunId: 0, miktar: 1 }
        })
      });
      setFisSonucu(null);
      setGiderTaslagi(null);
      setMesaj("Fiş gider olarak kaydedildi. Finansal özet yenilendi.");
      onKayitOlusturuldu?.();
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Fiş gider olarak kaydedilemedi.");
    } finally {
      giderKayitRef.current = false;
      setGiderKaydediliyor(false);
    }
  }

  function miktariDegistir(id: number, fark: number) {
    setHata("");
    setSepet((current) => current
      .map((row) => {
        if (row.id !== id) return row;
        const nextQuantity = row.miktar + fark;
        if (nextQuantity > row.mevcutStok) {
          setHata(`${row.ad} için yeterli stok yok.`);
          return row;
        }
        return { ...row, miktar: nextQuantity };
      })
      .filter((row) => row.miktar > 0));
  }

  async function satisiTamamla() {
    if (sepet.length === 0) {
      setHata("Satışı tamamlamak için sepete ürün ekleyin.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      setMesaj("Satış kaydediliyor...");
      const result = await jsonOku<HizliSatisSonucu>("/api/ekran/urun-stok/hizli-satis", {
        method: "POST",
        body: JSON.stringify({
          islemAnahtari,
          odemeYontemi,
          satirlar: sepet.map((row) => ({
            urunHizmetId: row.id,
            miktar: row.miktar
          }))
        })
      });

      setSepet([]);
      setArama("");
      setIslemAnahtari(yeniIslemAnahtari());
      setMesaj(`${result.faturaNo} numaralı satış tamamlandı. ${paraBic(result.toplam)} gelir olarak kaydedildi.`);
      await urunleriYukle();
      window.setTimeout(() => barkodRef.current?.focus(), 20);
    } catch (error) {
      setMesaj("");
      setHata(error instanceof Error ? error.message : "Satış kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  return (
    <main className="pos-page">
      <section className="pos-summary" aria-label={t("quickSale.title")}>
        <article>
          <span><PackageOpen size={20} /></span>
          <div><small>Satışa açık ürün</small><strong>{stoktaUrunSayisi}</strong></div>
        </article>
        <article>
          <span><ShoppingCart size={20} /></span>
          <div><small>Sepetteki adet</small><strong>{sayiBic(sepetAdedi)}</strong></div>
        </article>
        <article className="pos-summary__total">
          <span><WalletCards size={20} /></span>
          <div><small>Sepet toplamı</small><strong>{paraBic(sepetToplami)}</strong></div>
        </article>
      </section>

      {(hata || mesaj) ? (
        <div className={`pos-notice ${hata ? "error" : "success"}`} role={hata ? "alert" : "status"}>
          {hata || mesaj}
        </div>
      ) : null}

      <section className="pos-layout">
        <div className="pos-catalog">
          <header className="pos-section-header">
            <div>
              <span>Stoktaki ürünler</span>
              <h2>Ürün seç</h2>
              <p>Ürün kartına dokunun veya barkodu okutun.</p>
            </div>
            <b>{filtreliUrunler.length} ürün</b>
          </header>

          <div className="pos-search">
            <Search size={20} />
            <input
              ref={barkodRef}
              value={arama}
              onChange={(event) => {
                setArama(event.target.value);
                setHata("");
              }}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  barkoduIsle();
                }
              }}
              placeholder="Ürün adı veya barkod ara"
              aria-label={t("quickSale.search")}
              autoComplete="off"
            />
            <button type="button" onClick={barkoduIsle}><Barcode size={18} />Barkodu ekle</button>
          </div>

          <div className="pos-capture-stack">
            <div className="pos-capture-actions" aria-label="Kamerayla tarama">
              <input ref={barkodKameraRef} type="file" accept="image/jpeg,image/png,image/webp" capture="environment" onChange={barkodFotografiSecildi} aria-label="Barkod fotoğrafı" hidden />
              <button type="button" onClick={() => barkodKameraRef.current?.click()} disabled={taramaDurumu === "loading"}>
                {taramaDurumu === "loading" ? <Loader2 size={18} className="spin" /> : <ScanBarcode size={18} />}
                <span><strong>Barkod tara</strong><small>Kamerayı ürün koduna tutun</small></span>
              </button>
              <input ref={fisKameraRef} type="file" accept="image/jpeg,image/png,image/webp" capture="environment" onChange={fisFotografiSecildi} aria-label="Fiş fotoğrafı" hidden />
              <button type="button" onClick={() => fisKameraRef.current?.click()} disabled={fisOcrHazir !== true || fisIslemde}>
                {fisIslemde ? <Loader2 size={18} className="spin" /> : <Camera size={18} />}
                <span><strong>Fiş oku</strong><small>{fisOcrHazir === null ? "Servis kontrol ediliyor" : fisOcrHazir ? "Gider fişini fotoğraflayın" : "OCR yapılandırması gerekli"}</small></span>
              </button>
            </div>
            <p className="pos-capture-help">Kamera izni reddedilirse tarayıcı ayarlarından izin verin veya galeriden net bir fotoğraf seçin.</p>

            {taramaMesaji ? <p className={`pos-scan-feedback ${taramaDurumu}`} role={taramaDurumu === "error" ? "alert" : "status"}>{taramaMesaji}</p> : null}
            {fisOcrHazir === false ? <p className="pos-scan-feedback error" role="alert">Fiş okuma servisi hazır değil. Yönetici ReceiptOcr API anahtarını yapılandırmalı.</p> : null}
            {fisSonucu && giderTaslagi ? (
              <article className="pos-receipt-preview pos-receipt-form" aria-label="Okunan fiş">
                <span><ReceiptText size={19} /></span>
                <div>
                  <strong>{fisSonucu.merchant || "Satıcı okunamadı"}</strong>
                  <small>{fisSonucu.receiptDate || "Tarih yok"} · {fisSonucu.paymentMethod || "Ödeme yöntemi yok"}</small>
                </div>
                <b>{paraBic(fisSonucu.receiptTotal ?? 0)}</b>
                <p>{fisSonucu.items.length} satır okundu. Kayıt oluşturmadan önce tutarları kontrol edin.</p>
                <div className="pos-receipt-form__fields">
                  <label><span>Tarih</span><input aria-label="Fiş tarihi" type="datetime-local" value={giderTaslagi.tarih} onChange={(event) => giderTaslaginiGuncelle({ tarih: event.target.value })} /></label>
                  <label><span>Toplam</span><input aria-label="Fiş toplamı" inputMode="decimal" value={giderTaslagi.tutar} onChange={(event) => giderTaslaginiGuncelle({ tutar: event.target.value })} /></label>
                  <label><span>Ödeme yöntemi</span><select aria-label="Fiş ödeme yöntemi" value={giderTaslagi.odemeYontemi} onChange={(event) => giderTaslaginiGuncelle({ odemeYontemi: event.target.value })}>{giderOdemeYontemleri.map((option) => <option key={option.deger} value={option.deger}>{option.etiket}</option>)}</select></label>
                  <label><span>Gider kalemi</span><select aria-label="Fiş gider kalemi" value={giderTaslagi.kalem} onChange={(event) => giderTaslaginiGuncelle({ kalem: event.target.value })}><option value="">Kalem seçin</option>{giderKalemleri.map((category) => <option key={category} value={category}>{category}</option>)}</select></label>
                  <label className="pos-receipt-form__description"><span>Açıklama</span><textarea aria-label="Fiş açıklaması" value={giderTaslagi.aciklama} onChange={(event) => giderTaslaginiGuncelle({ aciklama: event.target.value })} /></label>
                </div>
                <button type="button" className="pos-receipt-form__save" onClick={() => void giderOlarakKaydet()} disabled={giderKaydediliyor}>
                  {giderKaydediliyor ? <Loader2 size={17} className="spin" /> : <Check size={17} />}
                  {giderKaydediliyor ? "Gider kaydediliyor..." : "Gider olarak kaydet"}
                </button>
              </article>
            ) : null}
          </div>

          <div className="pos-products">
            {filtreliUrunler.map((product) => {
              const cartRow = sepet.find((row) => row.id === product.id);
              const unavailable = product.mevcutStok <= 0 || product.satisFiyati <= 0;
              return (
                <button
                  key={product.id}
                  type="button"
                  className={`pos-product-card${cartRow ? " selected" : ""}`}
                  onClick={() => sepeteEkle(product)}
                  disabled={unavailable}
                >
                  <span className="pos-product-card__icon"><PackageOpen size={22} /></span>
                  <span className="pos-product-card__body">
                    <strong>{product.ad}</strong>
                    <small>{product.barkod || "Barkod yok"}</small>
                  </span>
                  <span className={`pos-product-card__stock${product.mevcutStok <= product.kritikStok ? " critical" : ""}`}>
                    {product.mevcutStok > 0 ? `${sayiBic(product.mevcutStok)} ${product.birim}` : "Stok yok"}
                  </span>
                  <strong className="pos-product-card__price">{paraBic(product.satisFiyati)}</strong>
                  <span className="pos-product-card__add">{cartRow ? `${sayiBic(cartRow.miktar)} sepette` : <><Plus size={16} />Sepete ekle</>}</span>
                </button>
              );
            })}

            {!yukleniyor && filtreliUrunler.length === 0 ? (
              <div className="pos-empty">
                <PackageOpen size={28} />
                <strong>{urunler.length === 0 ? "Satışa açık ürün bulunamadı" : "Aramanızla eşleşen ürün yok"}</strong>
                <p>{urunler.length === 0 ? "Önce Ürün / Stok ekranından ürün ve başlangıç stoğu ekleyin." : "Arama metnini veya barkodu kontrol edin."}</p>
                {urunler.length === 0 ? <a href="/app/urun-stok">Ürün / Stok ekranına git</a> : null}
              </div>
            ) : null}
          </div>
        </div>

        <aside className="pos-cart">
          <header className="pos-section-header">
            <div>
              <span>Aktif satış</span>
              <h2>{t("quickSale.title")}</h2>
              <p>{sepet.length > 0 ? `${sepet.length} farklı ürün` : "Henüz ürün eklenmedi."}</p>
            </div>
            {sepet.length > 0 ? (
              <button type="button" className="pos-cart__clear" onClick={() => setSepet([])}>
                <Trash2 size={16} />Temizle
              </button>
            ) : null}
          </header>

          <div className="pos-cart__lines">
            {sepet.map((row) => (
              <article key={row.id}>
                <div className="pos-cart__line-title">
                  <strong>{row.ad}</strong>
                  <small>{paraBic(row.satisFiyati)} / {row.birim}</small>
                </div>
                <div className="pos-cart__quantity">
                  <button type="button" onClick={() => miktariDegistir(row.id, -1)} aria-label={`${row.ad} miktarını azalt`}><Minus size={15} /></button>
                  <strong>{sayiBic(row.miktar)}</strong>
                  <button type="button" onClick={() => miktariDegistir(row.id, 1)} disabled={row.miktar >= row.mevcutStok} aria-label={`${row.ad} miktarını artır`}><Plus size={15} /></button>
                </div>
                <strong className="pos-cart__line-total">{paraBic(row.satisFiyati * row.miktar)}</strong>
                <button type="button" className="pos-cart__remove" onClick={() => setSepet((current) => current.filter((item) => item.id !== row.id))} aria-label={`${row.ad} ürününü sepetten çıkar`}><Trash2 size={15} /></button>
              </article>
            ))}

            {sepet.length === 0 ? (
              <div className="pos-cart__empty">
                <ShoppingCart size={31} />
                <strong>{t("quickSale.emptyCart")}</strong>
                <p>Soldaki ürünlerden seçim yaparak satışa başlayın.</p>
              </div>
            ) : null}
          </div>

          <footer className="pos-checkout">
            <fieldset>
              <legend>Ödeme yöntemi</legend>
              <div className="pos-payment-options">
                {odemeYontemleri.map((option) => {
                  const Icon = option.icon;
                  return (
                    <button
                      key={option.value}
                      type="button"
                      className={odemeYontemi === option.value ? "active" : ""}
                      onClick={() => setOdemeYontemi(option.value)}
                      aria-pressed={odemeYontemi === option.value}
                    >
                      <Icon size={16} />{option.label}
                    </button>
                  );
                })}
              </div>
            </fieldset>

            <div className="pos-checkout__total">
              <span>Ödenecek toplam</span>
              <strong>{paraBic(sepetToplami)}</strong>
            </div>
            <button type="button" className="pos-checkout__submit" onClick={() => void satisiTamamla()} disabled={islemde || sepet.length === 0}>
              <Check size={19} />
              {islemde ? t("support.saving") : t("quickSale.complete")}
            </button>
            <small>Satış tamamlandığında stok otomatik düşer ve tutar gelir kaydı olarak eklenir.</small>
          </footer>
        </aside>
      </section>
    </main>
  );
}

async function barkoduYerelOku(file: File) {
  const Detector = (window as typeof window & { BarcodeDetector?: BarcodeDetectorConstructor }).BarcodeDetector;
  if (Detector && typeof createImageBitmap === "function") {
    const bitmap = await createImageBitmap(file);
    try {
      const results = await new Detector({ formats: ["ean_13", "ean_8", "upc_a", "upc_e", "code_128", "code_39", "qr_code"] }).detect(bitmap);
      const detected = results.find((item) => item.rawValue.trim())?.rawValue.trim();
      if (detected) return detected;
    } catch {
      // ZXing handles browsers whose BarcodeDetector exists but rejects a format.
    } finally {
      bitmap.close();
    }
  }

  if (typeof URL.createObjectURL !== "function") return "";
  const imageUrl = URL.createObjectURL(file);
  try {
    const { BrowserMultiFormatReader } = await import("@zxing/browser");
    const result = await new BrowserMultiFormatReader().decodeFromImageUrl(imageUrl);
    return result.getText().trim();
  } catch {
    return "";
  } finally {
    URL.revokeObjectURL(imageUrl);
  }
}

function fisTaslagiOlustur(
  result: FisOcrSonucu,
  categories: string[],
  paymentMethods: Array<{ deger: string; etiket: string }>
): GiderTaslagi {
  const candidate = result.items.map((item) => item.candidateKalem.trim()).find((item) =>
    categories.some((category) => category.localeCompare(item, "tr-TR", { sensitivity: "base" }) === 0));
  const category = candidate
    ? categories.find((item) => item.localeCompare(candidate, "tr-TR", { sensitivity: "base" }) === 0) ?? ""
    : "";
  const normalizedPayment = normalizePaymentMethod(result.paymentMethod);
  const paymentMethod = paymentMethods.find((item) => item.deger === normalizedPayment)?.deger
    ?? paymentMethods[0]?.deger
    ?? "nakit";
  const date = result.receiptDate?.slice(0, 10);
  const description = [result.merchant.trim(), ...result.items.map((item) => item.rawName.trim())]
    .filter(Boolean)
    .join(" | ")
    .slice(0, 500);
  return {
    tarih: date ? `${date}T12:00` : "",
    tutar: result.receiptTotal && result.receiptTotal > 0 ? String(result.receiptTotal).replace(".", ",") : "",
    odemeYontemi: paymentMethod,
    kalem: category,
    aciklama: description
  };
}

function normalizePaymentMethod(value: string) {
  const normalized = value.trim().toLowerCase().replace(/[^a-z0-9]/g, "");
  if (normalized === "kredikarti") return "krediKarti";
  if (normalized === "onlineodeme") return "onlineOdeme";
  if (normalized === "havale") return "havale";
  return "nakit";
}
