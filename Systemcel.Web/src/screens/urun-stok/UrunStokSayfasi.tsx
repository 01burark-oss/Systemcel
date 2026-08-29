import React from "react";
import {
  AlertTriangle,
  Barcode,
  Box,
  PackagePlus,
  Plus,
  Save,
  Search,
  Trash2,
  TrendingUp,
  WalletCards,
  X
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type {
  StokHareketFormu,
  UrunFormu,
  UrunListeKaydi,
  UrunStokEkranVerisi
} from "./types";

interface UrunStokSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

interface KimlikliMesaj {
  mesaj: string;
  id: number;
}

function bugun() {
  return new Date().toISOString().slice(0, 10);
}

function bosUrunFormu(): UrunFormu {
  return {
    id: 0,
    tip: "Urun",
    ad: "",
    barkod: "",
    birim: "Adet",
    kdvOrani: "20",
    alisFiyati: "0",
    satisFiyati: "0",
    kritikStok: "0",
    aktif: true
  };
}

function bosStokFormu(): StokHareketFormu {
  return {
    miktar: "0",
    birimMaliyet: "",
    tarih: bugun(),
    aciklama: ""
  };
}

function sayiyaCevir(value: string) {
  const normalized = value.replace(",", ".").trim();
  const parsed = Number(normalized);
  if (!Number.isFinite(parsed)) {
    throw new Error("Sayısal alanları kontrol edin.");
  }

  return parsed;
}

function formdanKayit(row: UrunListeKaydi): UrunFormu {
  return {
    id: row.id,
    tip: row.tip,
    ad: row.ad,
    barkod: row.barkod,
    birim: row.birim || "Adet",
    kdvOrani: String(row.kdvOrani).replace(".", ","),
    alisFiyati: String(row.alisFiyati).replace(".", ","),
    satisFiyati: String(row.satisFiyati).replace(".", ","),
    kritikStok: String(row.kritikStok).replace(".", ","),
    aktif: row.aktif
  };
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

function tarihBic(tarih: string) {
  const value = new Date(tarih);
  if (Number.isNaN(value.getTime())) {
    return tarih;
  }

  return value.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}

function etiketBic(value: string) {
  switch (value) {
    case "Urun":
      return "Ürün";
    case "Alis":
      return "Alış";
    case "Satis":
      return "Satış";
    case "Giris":
      return "Giriş";
    case "Cikis":
      return "Çıkış";
    default:
      return value;
  }
}

export function UrunStokSayfasi({ yenileAnahtari }: UrunStokSayfasiProps) {
  const pageRef = React.useRef<HTMLElement | null>(null);
  const barcodeInputRef = React.useRef<HTMLInputElement | null>(null);
  const productNameInputRef = React.useRef<HTMLInputElement | null>(null);
  const workbenchRef = React.useRef<HTMLElement | null>(null);
  const [ekran, setEkran] = React.useState<UrunStokEkranVerisi | null>(null);
  const [seciliId, setSeciliId] = React.useState<number | null>(null);
  const [urunFormu, setUrunFormu] = React.useState<UrunFormu>(() => bosUrunFormu());
  const [stokFormu, setStokFormu] = React.useState<StokHareketFormu>(() => bosStokFormu());
  const [arama, setArama] = React.useState("");
  const [tipFiltresi, setTipFiltresi] = React.useState("Tumu");
  const [durumFiltresi, setDurumFiltresi] = React.useState("Aktif");
  const [, setDurum] = React.useState("Ürün/Stok yükleniyor...");
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const [barkodPaneliAcik, setBarkodPaneliAcik] = React.useState(false);
  const [barkodDegeri, setBarkodDegeri] = React.useState("");
  const [barkodMesaji, setBarkodMesaji] = React.useState("");
  const [aktifIslemPaneli, setAktifIslemPaneli] = React.useState<"urun" | "stok">("urun");
  const [islemPaneliAcik, setIslemPaneliAcik] = React.useState(false);
  const seciliIdRef = React.useRef<number | null>(null);

  const seciliUrun = React.useMemo(
    () => ekran?.urunler.find((row) => row.id === seciliId) ?? null,
    [ekran, seciliId]
  );

  const ozette = React.useMemo(() => {
    const urunler = ekran?.urunler ?? [];
    const aktifUrunler = urunler.filter((row) => row.aktif && row.tip === "Urun");
    const kritik = aktifUrunler.filter((row) => row.mevcutStok <= row.kritikStok).length;
    const stokDegeri = aktifUrunler.reduce((total, row) => total + row.mevcutStok * row.satisFiyati, 0);
    const bugunkuHareket = (ekran?.sonHareketler ?? []).filter((row) => row.tarih.slice(0, 10) === bugun()).length;
    return { aktifUrun: aktifUrunler.length, kritik, stokDegeri, bugunkuHareket };
  }, [ekran]);

  const filtreliUrunler = React.useMemo(() => {
    const query = arama.trim().toLocaleLowerCase("tr-TR");
    return (ekran?.urunler ?? []).filter((row) => {
      const matchesSearch =
        !query ||
        row.ad.toLocaleLowerCase("tr-TR").includes(query) ||
        row.barkod.toLocaleLowerCase("tr-TR").includes(query);
      const matchesType = tipFiltresi === "Tumu" || row.tip === tipFiltresi;
      const matchesState =
        durumFiltresi === "Tumu" ||
        (durumFiltresi === "Aktif" ? row.aktif : !row.aktif);
      return matchesSearch && matchesType && matchesState;
    });
  }, [arama, durumFiltresi, ekran, tipFiltresi]);

  const formuSifirla = React.useCallback((barkod = "") => {
    seciliIdRef.current = null;
    setSeciliId(null);
    setUrunFormu({ ...bosUrunFormu(), barkod });
    setStokFormu(bosStokFormu());
    setAktifIslemPaneli("urun");
  }, []);

  const yeniUrunFormunuAc = React.useCallback(() => {
    formuSifirla();
    setIslemPaneliAcik(true);
    window.requestAnimationFrame(() => {
      workbenchRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
      window.setTimeout(() => productNameInputRef.current?.focus(), 280);
    });
  }, [formuSifirla]);

  const kaydiSec = React.useCallback((row: UrunListeKaydi) => {
    seciliIdRef.current = row.id;
    setSeciliId(row.id);
    setUrunFormu(formdanKayit(row));
    setStokFormu(bosStokFormu());
    setIslemPaneliAcik(true);
    setDurum(`${row.ad || "Kayıt"} seçildi.`);
  }, []);

  const yenile = React.useCallback(async (tercihId?: number | null) => {
    setHata("");
    setDurum("Ürün/Stok yükleniyor...");
    const data = await jsonOku<UrunStokEkranVerisi>("/api/ekran/urun-stok");
    setEkran(data);

    const hedefId = tercihId === undefined
      ? seciliIdRef.current
      : tercihId;
    const hedef = data.urunler.find((row) => row.id === hedefId) ?? null;
    if (hedef) {
      kaydiSec(hedef);
      setDurum(`${data.urunler.length} kayıt hazır.`);
      return;
    }

    formuSifirla();
    if (tercihId === null) {
      setIslemPaneliAcik(false);
    }
    setDurum("Kayıtlı ürün/hizmet bulunamadı. Yeni kart oluşturabilirsiniz.");
  }, [formuSifirla, kaydiSec]);

  React.useEffect(() => {
    pageRef.current?.scrollTo({ top: 0, left: 0 });
    yenile().catch((error: Error) => {
      setDurum("");
      setHata(error.message);
    });
  }, [yenile, yenileAnahtari]);

  React.useEffect(() => {
    if (!barkodPaneliAcik) {
      return;
    }

    const handle = window.setTimeout(() => barcodeInputRef.current?.focus(), 60);
    return () => window.clearTimeout(handle);
  }, [barkodPaneliAcik]);

  function urunAlaniniGuncelle<K extends keyof UrunFormu>(alan: K, deger: UrunFormu[K]) {
    setUrunFormu((current) => ({ ...current, [alan]: deger }));
  }

  function stokAlaniniGuncelle<K extends keyof StokHareketFormu>(alan: K, deger: StokHareketFormu[K]) {
    setStokFormu((current) => ({ ...current, [alan]: deger }));
  }

  function urunPayload() {
    if (!urunFormu.ad.trim()) {
      throw new Error("Ad alanı zorunludur.");
    }

    return {
      tip: urunFormu.tip,
      ad: urunFormu.ad,
      barkod: urunFormu.barkod,
      birim: urunFormu.birim,
      kdvOrani: sayiyaCevir(urunFormu.kdvOrani),
      alisFiyati: sayiyaCevir(urunFormu.alisFiyati),
      satisFiyati: sayiyaCevir(urunFormu.satisFiyati),
      kritikStok: sayiyaCevir(urunFormu.kritikStok),
      aktif: urunFormu.aktif
    };
  }

  async function urunKaydet() {
    try {
      setIslemde(true);
      setHata("");
      const payload = urunPayload();
      const result = urunFormu.id > 0
        ? await jsonOku<KimlikliMesaj>(`/api/ekran/urun-stok/urunler/${urunFormu.id}`, {
            method: "PUT",
            body: JSON.stringify({ ...payload, id: urunFormu.id })
          })
        : await jsonOku<KimlikliMesaj>("/api/ekran/urun-stok/urunler", {
            method: "POST",
            body: JSON.stringify(payload)
          });

      await yenile(result.id);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Ürün/hizmet kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  async function urunSil() {
    if (!seciliId) {
      return;
    }

    if (!window.confirm("Ürün/hizmet ve stok hareketleri silinsin mi?")) {
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<{ mesaj: string }>(`/api/ekran/urun-stok/urunler/${seciliId}`, {
        method: "DELETE"
      });
      await yenile(null);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Ürün/hizmet silinemedi.");
    } finally {
      setIslemde(false);
    }
  }

  async function stokIsle() {
    if (!seciliId) {
      setHata("Önce bir ürün seçin.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const miktar = sayiyaCevir(stokFormu.miktar);
      if (miktar === 0) {
        throw new Error("Miktar sıfır olamaz.");
      }
      const birimMaliyet = stokFormu.birimMaliyet.trim() ? sayiyaCevir(stokFormu.birimMaliyet) : undefined;
      if (miktar > 0 && (birimMaliyet === undefined || birimMaliyet <= 0)) {
        throw new Error("Stok girişi için KDV hariç birim maliyet girin.");
      }

      const result = await jsonOku<{ mesaj: string; mevcutStok: number }>(`/api/ekran/urun-stok/urunler/${seciliId}/hareketler`, {
        method: "POST",
        body: JSON.stringify({
          miktar,
          birimMaliyet,
          tarih: stokFormu.tarih,
          aciklama: stokFormu.aciklama
        })
      });

      setStokFormu(bosStokFormu());
      await yenile(seciliId);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Stok hareketi eklenemedi.");
    } finally {
      setIslemde(false);
    }
  }

  async function barkoduIsle() {
    const barcode = barkodDegeri.trim();
    if (!barcode) {
      setBarkodMesaji("Barkod okutun veya yazın.");
      barcodeInputRef.current?.focus();
      return;
    }

    try {
      setIslemde(true);
      setBarkodMesaji("Barkod kontrol ediliyor...");
      const existing = await jsonOku<UrunListeKaydi>(`/api/ekran/urun-stok/barkod?deger=${encodeURIComponent(barcode)}`);
      kaydiSec(existing);
      setAktifIslemPaneli("urun");
      setBarkodPaneliAcik(false);
      setBarkodDegeri("");
      setDurum(`Barkod mevcut kayda ait: ${existing.ad}`);
    } catch {
      formuSifirla(barcode);
      setBarkodPaneliAcik(false);
      setBarkodDegeri("");
      setDurum("Barkod alındı. Ürün adını ve fiyat bilgilerini girip kaydedin.");
    } finally {
      setIslemde(false);
      setBarkodMesaji("");
    }
  }

  return (
    <main ref={pageRef} className="stock-page">
      <section className="stock-stats">
        <div className="stock-stat">
          <span className="stock-stat__icon blue"><Box size={24} /></span>
          <div><small>Ürün</small><strong>{ozette.aktifUrun}</strong></div>
        </div>
        <div className="stock-stat">
          <span className="stock-stat__icon amber"><AlertTriangle size={24} /></span>
          <div><small>Kritik stok</small><strong>{ozette.kritik}</strong></div>
        </div>
        <div className="stock-stat">
          <span className="stock-stat__icon green"><WalletCards size={24} /></span>
          <div><small>Stok değeri</small><strong>{paraBic(ozette.stokDegeri)}</strong></div>
        </div>
        <div className="stock-stat">
          <span className="stock-stat__icon purple"><TrendingUp size={24} /></span>
          <div><small>Bugünkü hareket</small><strong>+{ozette.bugunkuHareket}</strong></div>
        </div>
      </section>

      <section className={`stock-layout ${islemPaneliAcik ? "stock-layout--panel-open" : "stock-layout--panel-closed"}`}>
        <div className="stock-left">
          <div className="stock-card stock-card--list">
            <div className="stock-card__header">
              <h2>Ürünler</h2>
              <div className="stock-list-tools">
                <label className="stock-search">
                  <Search size={17} />
                  <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Ürün adı, barkod ara..." />
                </label>
                <select value={tipFiltresi} onChange={(event) => setTipFiltresi(event.target.value)}>
                  <option value="Tumu">Tüm Tipler</option>
                  {(ekran?.tipSecenekleri ?? []).map((secenek) => <option key={secenek.deger} value={secenek.deger}>{etiketBic(secenek.etiket)}</option>)}
                </select>
                <select value={durumFiltresi} onChange={(event) => setDurumFiltresi(event.target.value)}>
                  <option value="Aktif">Aktif Ürünler</option>
                  <option value="Pasif">Pasif Ürünler</option>
                  <option value="Tumu">Tüm Durumlar</option>
                </select>
                <button
                  type="button"
                  className="stock-btn stock-btn--primary stock-new-product"
                  onClick={yeniUrunFormunuAc}
                  disabled={islemde}
                >
                  <Plus size={17} />
                  Yeni ürün
                </button>
              </div>
            </div>

            <div className="stock-table-wrap">
              <table className="stock-table">
                <thead>
                  <tr>
                    <th>Tip</th>
                    <th>Ürün Adı</th>
                    <th>Barkod</th>
                    <th>Birim</th>
                    <th>KDV %</th>
                    <th>Satış</th>
                    <th>Kritik</th>
                    <th>Stok</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {filtreliUrunler.map((row) => {
                    const kritik = row.tip === "Urun" && row.mevcutStok <= row.kritikStok;
                    return (
                      <tr
                        key={row.id}
                        className={seciliId === row.id ? "secili" : ""}
                        onClick={() => kaydiSec(row)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            kaydiSec(row);
                          }
                        }}
                        tabIndex={0}
                        aria-label={`${row.ad} ürün kartını aç`}
                      >
                        <td><span className={`stock-type ${row.tip === "Hizmet" ? "service" : ""}`}><Box size={15} />{etiketBic(row.tip)}</span></td>
                        <td>{row.ad || "-"}</td>
                        <td>{row.barkod || "-"}</td>
                        <td>{row.birim || "-"}</td>
                        <td>{sayiBic(row.kdvOrani)}</td>
                        <td>{paraBic(row.satisFiyati)}</td>
                        <td>{row.tip === "Urun" ? sayiBic(row.kritikStok) : "-"}</td>
                        <td>{row.tip === "Urun" ? sayiBic(row.mevcutStok) : "-"}</td>
                        <td><span className={`stock-pill ${kritik ? "critical" : row.aktif ? "active" : "passive"}`}>{kritik ? "Kritik" : row.aktif ? "Aktif" : "Pasif"}</span></td>
                      </tr>
                    );
                  })}
                  {filtreliUrunler.length === 0 && (
                    <tr>
                      <td className="bos" colSpan={9}>
                        <div className="stock-empty">
                          <Box size={22} />
                          <strong>Henüz ürün bulunmuyor.</strong>
                          <span>İlk ürün veya hizmet kartınızı oluşturarak başlayın.</span>
                          <button type="button" onClick={yeniUrunFormunuAc}>
                            <Plus size={15} />
                            İlk ürünü ekle
                          </button>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          <div className="stock-card stock-card--history">
            <div className="stock-card__header compact">
              <h2>Son hareketler</h2>
            </div>
            <div className="stock-table-wrap stock-table-wrap--history">
              <table className="stock-table stock-table--history">
                <thead>
                  <tr>
                    <th>Tarih</th>
                    <th>Ürün</th>
                    <th>Hareket Tipi</th>
                    <th>Miktar</th>
                    <th>Açıklama</th>
                    <th>Kaynak</th>
                  </tr>
                </thead>
                <tbody>
                  {(ekran?.sonHareketler ?? []).map((row) => (
                    <tr key={row.id}>
                      <td>{tarihBic(row.tarih)}</td>
                      <td>{row.urunAdi}</td>
                      <td><span className={`stock-pill ${row.miktar >= 0 ? "active" : "critical"}`}>{etiketBic(row.hareketTipi)}</span></td>
                      <td>{row.miktar > 0 ? "+" : ""}{sayiBic(row.miktar)}</td>
                      <td>{row.aciklama || "-"}</td>
                      <td>{row.kaynak || "-"}</td>
                    </tr>
                  ))}
                  {(ekran?.sonHareketler.length ?? 0) === 0 && (
                    <tr>
                      <td className="bos" colSpan={6}>
                        <div className="stock-empty stock-empty--compact">
                          <PackagePlus size={20} />
                          <strong>Henüz stok hareketi yok.</strong>
                          <span>Ürün giriş ve çıkışları burada listelenecek.</span>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {islemPaneliAcik ? <aside ref={workbenchRef} className="stock-side stock-side--drawer">
          <section className="stock-card stock-workbench">
            <header className="stock-workbench__header">
              <div>
                <span>Hızlı işlem</span>
                <h2>Ürün işlemi</h2>
              </div>
              <button
                type="button"
                className="side-panel-close"
                onClick={() => setIslemPaneliAcik(false)}
                aria-label="Ürün işlemini kapat"
              >
                <X size={18} />
              </button>
            </header>

            <div className="stock-workbench__tabs" role="tablist" aria-label="Ürün ve stok işlemleri">
              <button
                type="button"
                role="tab"
                aria-selected={aktifIslemPaneli === "urun"}
                className={aktifIslemPaneli === "urun" ? "active" : ""}
                onClick={() => setAktifIslemPaneli("urun")}
              >
                <Box size={17} />
                Ürün kartı
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={aktifIslemPaneli === "stok"}
                className={aktifIslemPaneli === "stok" ? "active" : ""}
                onClick={() => setAktifIslemPaneli("stok")}
              >
                <PackagePlus size={17} />
                Stok hareketi
              </button>
            </div>

            {aktifIslemPaneli === "urun" ? (
              <div className="stock-workbench__panel" role="tabpanel">
                <div className="stock-form-grid">
                  <label className="stock-field">
                    <span>Tip</span>
                    <select value={urunFormu.tip} onChange={(event) => urunAlaniniGuncelle("tip", event.target.value)}>
                      {(ekran?.tipSecenekleri ?? []).map((secenek) => <option key={secenek.deger} value={secenek.deger}>{etiketBic(secenek.etiket)}</option>)}
                    </select>
                  </label>
                  <label className="stock-field">
                    <span>Birim</span>
                    <select value={urunFormu.birim} onChange={(event) => urunAlaniniGuncelle("birim", event.target.value)}>
                      {(ekran?.birimSecenekleri ?? []).map((secenek) => <option key={secenek.deger} value={secenek.deger}>{secenek.etiket}</option>)}
                    </select>
                  </label>
                  <label className="stock-field">
                    <span>Ad</span>
                    <input ref={productNameInputRef} value={urunFormu.ad} onChange={(event) => urunAlaniniGuncelle("ad", event.target.value)} placeholder="Ürün veya hizmet adı" />
                  </label>
                  <label className="stock-field">
                    <span>Barkod</span>
                    <input value={urunFormu.barkod} onChange={(event) => urunAlaniniGuncelle("barkod", event.target.value)} placeholder="Barkod numarası" />
                  </label>
                  <label className="stock-field">
                    <span>Alış fiyatı</span>
                    <input inputMode="decimal" value={urunFormu.alisFiyati} onChange={(event) => urunAlaniniGuncelle("alisFiyati", event.target.value)} />
                  </label>
                  <label className="stock-field">
                    <span>Satış fiyatı</span>
                    <input inputMode="decimal" value={urunFormu.satisFiyati} onChange={(event) => urunAlaniniGuncelle("satisFiyati", event.target.value)} />
                  </label>
                  <label className="stock-field">
                    <span>KDV %</span>
                    <input inputMode="decimal" value={urunFormu.kdvOrani} onChange={(event) => urunAlaniniGuncelle("kdvOrani", event.target.value)} />
                  </label>
                  <label className="stock-field">
                    <span>Kritik stok</span>
                    <input inputMode="decimal" value={urunFormu.kritikStok} onChange={(event) => urunAlaniniGuncelle("kritikStok", event.target.value)} />
                  </label>
                  <label className="stock-check">
                    <input type="checkbox" checked={urunFormu.aktif} onChange={(event) => urunAlaniniGuncelle("aktif", event.target.checked)} />
                    <span>
                      <strong>Aktif ürün</strong>
                      <small>Listelerde ve işlemlerde kullanılabilir.</small>
                    </span>
                  </label>
                  <button type="button" className="stock-btn stock-btn--barcode stock-btn--barcode-inline" onClick={() => setBarkodPaneliAcik(true)} disabled={islemde}>
                    <Barcode size={17} />
                    Barkod ile ürün ekle
                  </button>
                </div>

                <div className="stock-actions">
                  <button type="button" className="stock-btn" onClick={() => formuSifirla()} disabled={islemde}><Plus size={16} />Yeni</button>
                  <button type="button" className="stock-btn stock-btn--danger" onClick={() => void urunSil()} disabled={islemde || !seciliId}><Trash2 size={16} />Sil</button>
                  <button type="button" className="stock-btn stock-btn--primary" onClick={() => void urunKaydet()} disabled={islemde}><Save size={16} />Kaydet</button>
                </div>
              </div>
            ) : (
              <div className="stock-workbench__panel" role="tabpanel">
                <div className={`stock-selection-summary ${seciliUrun?.tip === "Urun" ? "ready" : ""}`}>
                  <span>
                    <small>Seçili ürün</small>
                    <strong>{seciliUrun?.ad || "Önce listeden bir ürün seçin"}</strong>
                  </span>
                  <span>
                    <small>Mevcut stok</small>
                    <strong>{sayiBic(seciliUrun?.mevcutStok ?? 0)}</strong>
                  </span>
                </div>

                <div className="stock-movement-form">
                  <div className="stock-movement-form__primary">
                    <label className="stock-field">
                      <span>Miktar (+/-)</span>
                      <input inputMode="decimal" data-allow-negative="true" value={stokFormu.miktar} onChange={(event) => stokAlaniniGuncelle("miktar", event.target.value)} disabled={!seciliId || seciliUrun?.tip !== "Urun"} />
                    </label>
                    <label className="stock-field stock-field--date">
                      <span>Tarih</span>
                      <input className="stock-date-input" type="date" value={stokFormu.tarih} onChange={(event) => stokAlaniniGuncelle("tarih", event.target.value)} disabled={!seciliId || seciliUrun?.tip !== "Urun"} />
                    </label>
                  </div>
                  <p className="stock-movement-form__hint">
                    Pozitif değer stok girişi, negatif değer stok çıkışı yapar. Giriş maliyeti KDV hariçtir.
                  </p>
                  <label className="stock-field stock-field--grow">
                    <span>Birim maliyet (KDV hariç)</span>
                    <input inputMode="decimal" value={stokFormu.birimMaliyet} onChange={(event) => stokAlaniniGuncelle("birimMaliyet", event.target.value)} disabled={!seciliId || seciliUrun?.tip !== "Urun" || stokFormu.miktar.trim().startsWith("-")} placeholder="Sadece stok girişi" />
                  </label>
                  <label className="stock-field stock-field--grow stock-field--wide">
                    <span>Açıklama</span>
                    <input value={stokFormu.aciklama} onChange={(event) => stokAlaniniGuncelle("aciklama", event.target.value)} disabled={!seciliId || seciliUrun?.tip !== "Urun"} placeholder="Hareket açıklaması" />
                  </label>
                  <div className="stock-movement-form__actions">
                    <button type="button" className="stock-btn stock-btn--primary stock-btn--movement" onClick={() => void stokIsle()} disabled={islemde || !seciliId || seciliUrun?.tip !== "Urun"}>
                      <PackagePlus size={16} />
                      Stok hareketini kaydet
                    </button>
                  </div>
                </div>
              </div>
            )}
          </section>
        </aside> : null}
      </section>

      {hata ? (
        <div className="stock-feedback">
          <p className="stock-feedback__error">{hata}</p>
        </div>
      ) : null}

      {barkodPaneliAcik && (
        <div className="stock-modal" role="dialog" aria-modal="true" aria-labelledby="stock-barcode-dialog-title">
          <div className="stock-modal__card">
            <h2 id="stock-barcode-dialog-title">Barkod okut</h2>
            <input
              ref={barcodeInputRef}
              aria-label="Barkod"
              value={barkodDegeri}
              onChange={(event) => setBarkodDegeri(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void barkoduIsle();
                }
              }}
              placeholder="Barkod"
            />
            {barkodMesaji ? <small>{barkodMesaji}</small> : null}
            <div className="stock-modal__actions">
              <button type="button" className="stock-btn" onClick={() => setBarkodPaneliAcik(false)}>Vazgeç</button>
              <button type="button" className="stock-btn stock-btn--primary" onClick={() => void barkoduIsle()}>Tamam</button>
            </div>
          </div>
        </div>
      )}

    </main>
  );
}
