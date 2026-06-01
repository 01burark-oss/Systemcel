import React from "react";
import { CalendarDays, Plus, RefreshCw, Save, Trash2 } from "lucide-react";
import { BusinessSelector } from "../../shared/BusinessSelector";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type {
  CariDetay,
  CariEkranVerisi,
  CariHareketFormu,
  CariKartFormu,
  CariSecenek
} from "./types";

interface CariHesaplarSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

interface KimlikliMesaj {
  mesaj: string;
  id: number;
}

function ilkSecenek(secenekler: CariSecenek[] | undefined, yedek: string) {
  return secenekler?.[0]?.deger ?? yedek;
}

function bosKartFormu(ekran?: CariEkranVerisi | null): CariKartFormu {
  return {
    id: 0,
    tip: ilkSecenek(ekran?.tipSecenekleri, "Musteri"),
    unvan: "",
    telefon: "",
    eposta: "",
    vergiNoTc: "",
    vergiDairesi: "",
    adres: "",
    aktif: true
  };
}

function bosHareketFormu(ekran?: CariEkranVerisi | null): CariHareketFormu {
  return {
    hareketTipi: ilkSecenek(ekran?.hareketTipleri, "Borc"),
    tutar: "",
    tarih: new Date().toISOString().slice(0, 10),
    aciklama: ""
  };
}

function paraBic(tutar: number) {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(tutar);
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
    case "Musteri":
      return "Müşteri";
    case "Tedarikci":
      return "Tedarikçi";
    case "HerIkisi":
      return "Her İkisi";
    case "Borc":
      return "Borç";
    case "Alacak":
      return "Alacak";
    default:
      return value;
  }
}

export function CariHesaplarSayfasi({
  onIsletmeDegistir,
  ustBar,
  ustBarIslemde,
  yenileAnahtari
}: CariHesaplarSayfasiProps) {
  const pageRef = React.useRef<HTMLElement | null>(null);
  const [ekran, setEkran] = React.useState<CariEkranVerisi | null>(null);
  const [seciliId, setSeciliId] = React.useState<number | null>(null);
  const [detay, setDetay] = React.useState<CariDetay | null>(null);
  const [kartFormu, setKartFormu] = React.useState<CariKartFormu>(() => bosKartFormu());
  const [hareketFormu, setHareketFormu] = React.useState<CariHareketFormu>(() => bosHareketFormu());
  const [durum, setDurum] = React.useState("Cari hesaplar yükleniyor...");
  const [hata, setHata] = React.useState("");
  const [islemde, setIslemde] = React.useState(false);
  const ekranRef = React.useRef<CariEkranVerisi | null>(null);
  const seciliIdRef = React.useRef<number | null>(null);

  const formuSifirla = React.useCallback((ekranVerisi?: CariEkranVerisi | null) => {
    seciliIdRef.current = null;
    setSeciliId(null);
    setDetay(null);
    const kaynak = ekranVerisi ?? ekranRef.current;
    setKartFormu(bosKartFormu(kaynak));
    setHareketFormu(bosHareketFormu(kaynak));
  }, []);

  const detayYukle = React.useCallback(async (id: number, ekranVerisi?: CariEkranVerisi | null) => {
    const data = await jsonOku<CariDetay>(`/api/ekran/cari-hesaplar/${id}`);
    seciliIdRef.current = id;
    setSeciliId(id);
    setDetay(data);
    setKartFormu(data.kart);
    const kaynak = ekranVerisi ?? ekranRef.current;
    setHareketFormu(() => ({
      hareketTipi: ilkSecenek(kaynak?.hareketTipleri, "Borc"),
      tutar: "",
      tarih: new Date().toISOString().slice(0, 10),
      aciklama: ""
    }));
  }, []);

  const yenile = React.useCallback(async (tercihId?: number | null) => {
    setHata("");
    setDurum("Cari hesaplar yükleniyor...");
    const data = await jsonOku<CariEkranVerisi>("/api/ekran/cari-hesaplar");
    ekranRef.current = data;
    setEkran(data);

    const hedefId = tercihId === undefined
      ? seciliIdRef.current ?? data.kartlar[0]?.id ?? null
      : tercihId ?? data.kartlar[0]?.id ?? null;
    if (hedefId) {
      await detayYukle(hedefId, data);
      setDurum(`${data.kartlar.length} cari kart hazır.`);
      return;
    }

    formuSifirla(data);
    setDurum("");
  }, [detayYukle, formuSifirla]);

  React.useEffect(() => {
    pageRef.current?.scrollTo({ top: 0, left: 0 });
    yenile().catch((error: Error) => {
      setDurum("");
      setHata(error.message);
    });
  }, [yenileAnahtari, yenile]);

  function kartAlaniniGuncelle<K extends keyof CariKartFormu>(alan: K, deger: CariKartFormu[K]) {
    setKartFormu((current) => ({ ...current, [alan]: deger }));
  }

  function hareketAlaniniGuncelle<K extends keyof CariHareketFormu>(alan: K, deger: CariHareketFormu[K]) {
    setHareketFormu((current) => ({ ...current, [alan]: deger }));
  }

  async function kartKaydet() {
    try {
      setIslemde(true);
      setHata("");

      if (!kartFormu.unvan.trim()) {
        throw new Error("Unvan alanı zorunludur.");
      }

      const body = {
        tip: kartFormu.tip,
        unvan: kartFormu.unvan,
        telefon: kartFormu.telefon,
        eposta: kartFormu.eposta,
        vergiNoTc: kartFormu.vergiNoTc,
        vergiDairesi: kartFormu.vergiDairesi,
        adres: kartFormu.adres,
        aktif: kartFormu.aktif
      };

      const result = kartFormu.id > 0
        ? await jsonOku<KimlikliMesaj>(`/api/ekran/cari-hesaplar/${kartFormu.id}`, {
            method: "PUT",
            body: JSON.stringify({ ...body, id: kartFormu.id })
          })
        : await jsonOku<KimlikliMesaj>("/api/ekran/cari-hesaplar", {
            method: "POST",
            body: JSON.stringify(body)
          });

      await yenile(result.id);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Cari kart kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  async function kartSil() {
    if (!seciliId) {
      return;
    }

    if (!window.confirm("Seçili cari kart ve hareketleri silinsin mi?")) {
      return;
    }

    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<{ mesaj: string }>(`/api/ekran/cari-hesaplar/${seciliId}`, {
        method: "DELETE"
      });
      await yenile(null);
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Cari kart silinemedi.");
    } finally {
      setIslemde(false);
    }
  }

  async function hareketEkle() {
    if (!seciliId) {
      setHata("Önce bir cari kart seçin.");
      return;
    }

    try {
      setIslemde(true);
      setHata("");

      const tutar = Number(hareketFormu.tutar.replace(",", "."));
      if (!Number.isFinite(tutar) || tutar <= 0) {
        throw new Error("Tutar sıfırdan büyük olmalıdır.");
      }

      const result = await jsonOku<{ mesaj: string }>(`/api/ekran/cari-hesaplar/${seciliId}/hareketler`, {
        method: "POST",
        body: JSON.stringify({
          hareketTipi: hareketFormu.hareketTipi,
          tutar,
          tarih: hareketFormu.tarih,
          aciklama: hareketFormu.aciklama
        })
      });

      await detayYukle(seciliId);
      setHareketFormu(bosHareketFormu(ekranRef.current));
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Cari hareket eklenemedi.");
    } finally {
      setIslemde(false);
    }
  }

  return (
    <main ref={pageRef} className="cari-page">
      <section className="cari-hero">
        <div>
          <h1>Cari Hesaplar</h1>
          <p>{ekran?.aktifIsletme ? `Aktif işletme: ${ekran.aktifIsletme}` : "Cari kartlar hazırlanıyor."}</p>
        </div>

        <div className="cari-hero__actions">
          <button type="button" className="ghost-refresh" onClick={() => yenile().catch((error: Error) => setHata(error.message))} disabled={islemde}>
            <RefreshCw size={18} />
          </button>
          <BusinessSelector
            aktifIsletmeId={ustBar?.aktifIsletmeId}
            isletmeler={ustBar?.isletmeler ?? []}
            disabled={ustBarIslemde}
            onChange={onIsletmeDegistir}
          />
        </div>
      </section>

      <section className="cari-layout">
        <div className="cari-card cari-card--list">
          <div className="cari-card__header">
            <div>
              <h2>Cari Liste</h2>
              <p>{ekran ? `${ekran.kartlar.length} kayıt listeleniyor.` : "Liste yükleniyor."}</p>
            </div>
          </div>

          <div className="cari-table-wrap">
            <table className="cari-table cari-table--list">
              <colgroup>
                <col style={{ width: "12%" }} />
                <col style={{ width: "18%" }} />
                <col style={{ width: "22%" }} />
                <col style={{ width: "18%" }} />
                <col style={{ width: "18%" }} />
                <col style={{ width: "12%" }} />
              </colgroup>
              <thead>
                <tr>
                  <th>Id</th>
                  <th>Tip</th>
                  <th>Unvan</th>
                  <th>Telefon</th>
                  <th>Vergi No</th>
                  <th>Aktif</th>
                </tr>
              </thead>
              <tbody>
                {(ekran?.kartlar ?? []).map((kart) => (
                  <tr
                    key={kart.id}
                    className={seciliId === kart.id ? "secili" : ""}
                    onClick={() => {
                      void detayYukle(kart.id);
                      setDurum(`${kart.unvan || "Cari kart"} seçildi.`);
                    }}
                  >
                    <td>{kart.id}</td>
                    <td>{etiketBic(kart.tip)}</td>
                    <td>{kart.unvan || "-"}</td>
                    <td>{kart.telefon || "-"}</td>
                    <td>{kart.vergiNo || "-"}</td>
                    <td className="cari-table__check">
                      <input type="checkbox" checked={kart.aktif} readOnly />
                    </td>
                  </tr>
                ))}
                {(ekran?.kartlar.length ?? 0) === 0 && (
                  <tr>
                    <td className="bos" colSpan={6} aria-label="Liste boş" />
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="cari-side">
          <div className="cari-card cari-card--form">
            <div className="cari-card__header">
              <div>
                <h2>Cari Kart</h2>
                <p>{seciliId ? "Seçili kart düzenleniyor." : "Yeni cari kart oluşturun."}</p>
              </div>
              <strong className="cari-balance">Bakiye: {paraBic(detay?.bakiye ?? 0)}</strong>
            </div>

            <div className="cari-form-grid">
              <label className="cari-field">
                <span>Tip</span>
                <select value={kartFormu.tip} onChange={(event) => kartAlaniniGuncelle("tip", event.target.value)}>
                  {(ekran?.tipSecenekleri ?? []).map((secenek) => (
                    <option key={secenek.deger} value={secenek.deger}>
                      {etiketBic(secenek.etiket)}
                    </option>
                  ))}
                </select>
              </label>

              <label className="cari-field">
                <span>Unvan</span>
                <input value={kartFormu.unvan} onChange={(event) => kartAlaniniGuncelle("unvan", event.target.value)} />
              </label>

              <label className="cari-field">
                <span>Telefon</span>
                <input value={kartFormu.telefon} onChange={(event) => kartAlaniniGuncelle("telefon", event.target.value)} />
              </label>

              <label className="cari-field">
                <span>E-posta</span>
                <input value={kartFormu.eposta} onChange={(event) => kartAlaniniGuncelle("eposta", event.target.value)} />
              </label>

              <label className="cari-field">
                <span>Vergi/TC No</span>
                <input value={kartFormu.vergiNoTc} onChange={(event) => kartAlaniniGuncelle("vergiNoTc", event.target.value)} />
              </label>

              <label className="cari-field">
                <span>Vergi Dairesi</span>
                <input value={kartFormu.vergiDairesi} onChange={(event) => kartAlaniniGuncelle("vergiDairesi", event.target.value)} />
              </label>

              <label className="cari-field cari-field--full">
                <span>Adres</span>
                <textarea value={kartFormu.adres} onChange={(event) => kartAlaniniGuncelle("adres", event.target.value)} />
              </label>

              <label className="cari-check">
                <input
                  type="checkbox"
                  checked={kartFormu.aktif}
                  onChange={(event) => kartAlaniniGuncelle("aktif", event.target.checked)}
                />
                Aktif
              </label>
            </div>

            <div className="cari-actions">
              <button type="button" className="cari-btn" onClick={() => formuSifirla()} disabled={islemde}>
                <Plus size={16} />
                Yeni
              </button>
              <button type="button" className="cari-btn cari-btn--primary" onClick={() => void kartKaydet()} disabled={islemde}>
                <Save size={16} />
                Kaydet
              </button>
              <button type="button" className="cari-btn cari-btn--danger" onClick={() => void kartSil()} disabled={islemde || !seciliId}>
                <Trash2 size={16} />
                Sil
              </button>
            </div>
          </div>
        </div>

        <div className="cari-card cari-card--movement">
            <div className="cari-card__header">
              <div>
                <h2>Hareketler</h2>
                <p>{seciliId ? "Seçili cari kartın hareketleri." : "Hareket eklemek için kart seçin."}</p>
              </div>
            </div>

            <div className="cari-movement-form">
              <label className="cari-field">
                <span>Tip</span>
                <select
                  value={hareketFormu.hareketTipi}
                  onChange={(event) => hareketAlaniniGuncelle("hareketTipi", event.target.value)}
                  disabled={!seciliId}
                >
                  {(ekran?.hareketTipleri ?? []).map((secenek) => (
                    <option key={secenek.deger} value={secenek.deger}>
                      {etiketBic(secenek.etiket)}
                    </option>
                  ))}
                </select>
              </label>

              <label className="cari-field">
                <span>Tutar</span>
                <input
                  value={hareketFormu.tutar}
                  onChange={(event) => hareketAlaniniGuncelle("tutar", event.target.value)}
                  disabled={!seciliId}
                  inputMode="decimal"
                />
              </label>

              <label className="cari-field cari-field--date">
                <span>Tarih</span>
                <div className="cari-input-icon">
                  <CalendarDays size={16} />
                  <input
                    type="date"
                    value={hareketFormu.tarih}
                    onChange={(event) => hareketAlaniniGuncelle("tarih", event.target.value)}
                    disabled={!seciliId}
                  />
                </div>
              </label>

              <label className="cari-field cari-field--wide">
                <span>Açıklama</span>
                <input
                  value={hareketFormu.aciklama}
                  onChange={(event) => hareketAlaniniGuncelle("aciklama", event.target.value)}
                  disabled={!seciliId}
                />
              </label>

              <button type="button" className="cari-btn cari-btn--primary" onClick={() => void hareketEkle()} disabled={islemde || !seciliId}>
                <Plus size={16} />
                Hareket Ekle
              </button>
            </div>

            <div className="cari-table-wrap cari-table-wrap--movement">
              <table className="cari-table cari-table--movement">
                <colgroup>
                  <col style={{ width: "14%" }} />
                  <col style={{ width: "18%" }} />
                  <col style={{ width: "36%" }} />
                  <col style={{ width: "14%" }} />
                  <col style={{ width: "18%" }} />
                </colgroup>
                <thead>
                  <tr>
                    <th>Id</th>
                    <th>Tarih</th>
                    <th>Açıklama</th>
                    <th>Tip</th>
                    <th className="sayi">Tutar</th>
                  </tr>
                </thead>
                <tbody>
                  {(detay?.hareketler ?? []).map((hareket) => (
                    <tr key={hareket.id}>
                      <td>{hareket.id}</td>
                      <td>{tarihBic(hareket.tarih)}</td>
                      <td>{hareket.aciklama || hareket.kaynak || "-"}</td>
                      <td>{etiketBic(hareket.hareketTipi)}</td>
                      <td className="sayi">{paraBic(hareket.tutar)}</td>
                    </tr>
                  ))}
                  {(detay?.hareketler.length ?? 0) === 0 && (
                    <tr>
                      <td className="bos" colSpan={5}>
                        Hareket bulunamadı.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
        </div>
      </section>

        {hata && (
          <div className="cari-feedback">
            <p className="cari-feedback__error">{hata}</p>
          </div>
        )}
    </main>
  );
}
