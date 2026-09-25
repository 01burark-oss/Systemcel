import React from "react";
import { ArrowLeft, ArrowRight, PackageSearch, Search, Store } from "lucide-react";
import "./marketing.css";
import "./public-supplier-marketplace.css";

type PublicProduct = {
  id: number;
  ad: string;
  aciklama: string;
  kategori: string;
  birim: string;
  birimFiyat: number;
  kdvOrani: number;
  paraBirimi: string;
  tedarikciUnvani: string;
  tedarikciSehri: string;
  sevkiyatBolgeleri: string;
  iadeKosullari: string;
  tahminiTeslimatGun: number;
};

const orderHref = "/giris?returnUrl=%2Fapp%2Ftedarikci-pazaryeri";

export function PublicSupplierMarketplace() {
  const [products, setProducts] = React.useState<PublicProduct[]>([]);
  const [query, setQuery] = React.useState("");
  const [status, setStatus] = React.useState<"loading" | "ready" | "error">("loading");
  const language = window.localStorage.getItem("systemcel.language") === "en" ? "en" : "tr";
  const tr = language === "tr";

  React.useEffect(() => {
    document.title = tr ? "Tedarikçi Pazaryeri | Systemcel" : "Supplier Marketplace | Systemcel";
    const controller = new AbortController();
    fetch("/api/public/tedarikci-pazaryeri/urunler", { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("catalog unavailable");
        return response.json() as Promise<{ urunler: PublicProduct[] }>;
      })
      .then((data) => { setProducts(data.urunler); setStatus("ready"); })
      .catch((error: unknown) => { if (!(error instanceof DOMException && error.name === "AbortError")) setStatus("error"); });
    return () => controller.abort();
  }, [tr]);

  const normalizedQuery = query.trim().toLocaleLowerCase(tr ? "tr" : "en");
  const visibleProducts = products.filter((product) =>
    `${product.ad} ${product.kategori} ${product.tedarikciUnvani} ${product.tedarikciSehri}`
      .toLocaleLowerCase(tr ? "tr" : "en").includes(normalizedQuery)
  );

  return (
    <main className="marketing-page public-supplier-marketplace">
      <header className="marketing-public-header">
        <a className="marketing-brand" href="/" aria-label="Systemcel ana sayfa"><span className="marketing-brand-mark" aria-hidden="true"><i /><i /><i /><i /></span><strong>systemcel</strong></a>
        <a className="public-supplier-marketplace__back" href="/"><ArrowLeft size={17} />{tr ? "Ana sayfa" : "Home"}</a>
      </header>
      <section className="public-supplier-marketplace__hero marketing-grid-bg">
        <div className="public-supplier-marketplace__wrap">
          <span className="marketing-eyebrow"><i />SYSTEMCEL</span>
          <h1>{tr ? "Tedarikçi pazaryeri" : "Supplier marketplace"}</h1>
          <p>{tr ? "Doğrulanmış tedarikçilerin yayındaki ürünlerini inceleyin. Sipariş vermek için giriş yapabilirsiniz." : "Browse products from verified suppliers. Sign in when you are ready to order."}</p>
        </div>
      </section>
      <section className="public-supplier-marketplace__body public-supplier-marketplace__wrap" aria-label={tr ? "Ürün kataloğu" : "Product catalog"}>
        <div className="public-supplier-marketplace__toolbar">
          <div><span>{tr ? "KATALOG" : "CATALOG"}</span><h2>{tr ? "Ürünleri keşfedin" : "Explore products"}</h2></div>
          <label className="public-supplier-marketplace__search"><Search size={19} aria-hidden="true" /><input type="search" aria-label={tr ? "Ürün ara" : "Search products"} value={query} onChange={(event) => setQuery(event.target.value)} placeholder={tr ? "Ürün, kategori veya tedarikçi ara" : "Search products, categories or suppliers"} /></label>
        </div>
        {status === "loading" ? <p className="public-supplier-marketplace__notice" role="status">{tr ? "Ürünler yükleniyor…" : "Loading products…"}</p> : null}
        {status === "error" ? <p className="public-supplier-marketplace__notice" role="alert">{tr ? "Ürünler şu anda gösterilemiyor. Lütfen daha sonra tekrar deneyin." : "Products are unavailable right now. Please try again later."}</p> : null}
        {status === "ready" && visibleProducts.length === 0 ? <div className="public-supplier-marketplace__empty"><PackageSearch size={32} aria-hidden="true" /><p>{query ? (tr ? "Aramanıza uygun ürün bulunamadı." : "No products match your search.") : (tr ? "Henüz yayında ürün yok. Daha sonra tekrar bakın." : "No products are listed yet. Check back later.")}</p></div> : null}
        {status === "ready" && visibleProducts.length > 0 ? <div className="public-supplier-marketplace__grid">{visibleProducts.map((product) => (
          <article className="public-supplier-marketplace__card" key={product.id}>
            <div className="public-supplier-marketplace__card-top"><span className="public-supplier-marketplace__icon"><Store size={22} aria-hidden="true" /></span><span className="public-supplier-marketplace__category">{product.kategori}</span></div>
            <div><h3>{product.ad}</h3><p>{product.aciklama}</p></div>
            <div className="public-supplier-marketplace__supplier"><strong>{product.tedarikciUnvani}</strong>{product.tedarikciSehri ? <span>{product.tedarikciSehri}</span> : null}</div>
            <div className="public-supplier-marketplace__terms">
              {product.sevkiyatBolgeleri ? <span>{tr ? "Sevkiyat bölgesi" : "Shipping area"}: {product.sevkiyatBolgeleri}</span> : null}
              {product.tahminiTeslimatGun > 0 ? <span>{tr ? `Tahmini teslimat: ${product.tahminiTeslimatGun} gün` : `Estimated delivery: ${product.tahminiTeslimatGun} days`}</span> : null}
              {product.iadeKosullari ? <span>{tr ? "Satıcının iade koşulları" : "Seller return terms"}: {product.iadeKosullari}</span> : null}
            </div>
            <div className="public-supplier-marketplace__card-foot"><div><strong>{new Intl.NumberFormat(tr ? "tr-TR" : "en-US", { style: "currency", currency: product.paraBirimi || "TRY" }).format(product.birimFiyat * (1 + product.kdvOrani / 100))}</strong><small>/ {product.birim} · {tr ? "KDV dahil" : "VAT included"}</small></div><a href={orderHref}>{tr ? "Sipariş ver" : "Order"}<ArrowRight size={16} aria-hidden="true" /></a></div>
          </article>
        ))}</div> : null}
      </section>
      <nav className="public-supplier-marketplace__policies public-supplier-marketplace__wrap" aria-label={tr ? "Pazaryeri koşulları" : "Marketplace policies"}>
        <a href="/pazaryeri-satis-kosullari">{tr ? "Satış koşulları" : "Sale terms"}</a>
        <a href="/teslimat-ve-kargo">{tr ? "Teslimat ve kargo" : "Delivery and shipping"}</a>
        <a href="/iptal-ve-iade">{tr ? "İptal ve iade" : "Cancellation and returns"}</a>
        <a href="/iletisim">{tr ? "İletişim" : "Contact"}</a>
      </nav>
    </main>
  );
}
