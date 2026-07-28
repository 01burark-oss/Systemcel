import React from "react";
import {
  Banknote,
  Barcode,
  Check,
  CreditCard,
  Minus,
  PackageOpen,
  Plus,
  Search,
  ShoppingCart,
  Trash2,
  WalletCards
} from "lucide-react";
import { jsonOku } from "../../shared/json";
import type {
  HizliSatisSepetSatiri,
  HizliSatisSonucu,
  UrunListeKaydi,
  UrunStokEkranVerisi
} from "./types";
import "./hizli-satis.css";

interface HizliSatisSayfasiProps {
  yenileAnahtari: number;
}

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

export function HizliSatisSayfasi({ yenileAnahtari }: HizliSatisSayfasiProps) {
  const barkodRef = React.useRef<HTMLInputElement | null>(null);
  const [ekran, setEkran] = React.useState<UrunStokEkranVerisi | null>(null);
  const [arama, setArama] = React.useState("");
  const [sepet, setSepet] = React.useState<HizliSatisSepetSatiri[]>([]);
  const [odemeYontemi, setOdemeYontemi] = React.useState("Nakit");
  const [islemAnahtari, setIslemAnahtari] = React.useState(() => yeniIslemAnahtari());
  const [islemde, setIslemde] = React.useState(false);
  const [yukleniyor, setYukleniyor] = React.useState(true);
  const [mesaj, setMesaj] = React.useState("");
  const [hata, setHata] = React.useState("");

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

    const product = urunler.find((row) => row.barkod.trim() === barcode);
    if (!product) {
      setHata("Bu barkoda ait aktif bir ürün bulunamadı.");
      return;
    }

    sepeteEkle(product);
    setArama("");
    window.setTimeout(() => barkodRef.current?.focus(), 20);
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
      <section className="pos-summary" aria-label="Hızlı satış özeti">
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
        <div className={`pos-notice ${hata ? "error" : "success"}`} role="status">
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
              autoComplete="off"
            />
            <button type="button" onClick={barkoduIsle}><Barcode size={18} />Barkodu ekle</button>
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
              <h2>Sepet</h2>
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
                <strong>Sepet boş</strong>
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
              {islemde ? "Satış kaydediliyor..." : "Satışı tamamla"}
            </button>
            <small>Satış tamamlandığında stok otomatik düşer ve tutar gelir kaydı olarak eklenir.</small>
          </footer>
        </aside>
      </section>
    </main>
  );
}
