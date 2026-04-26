import React from "react";
import { createRoot } from "react-dom/client";
import {
  Banknote,
  CalendarDays,
  Check,
  CreditCard,
  Eraser,
  Landmark,
  Plus,
  RefreshCw,
  Save,
  Search,
  Trash2,
  WalletCards
} from "lucide-react";
import "./styles.css";

type Tur = "gelir" | "gider";
type OdemeYontemi = "nakit" | "krediKarti" | "onlineOdeme" | "havale";

interface Kayit {
  id: number;
  tarih: string;
  tur: Tur;
  tutar: number;
  odemeYontemi: OdemeYontemi;
  kalem: string;
  aciklama: string;
}

interface OdemeSecenek {
  deger: OdemeYontemi;
  etiket: string;
}

interface StokUrun {
  id: number;
  ad: string;
  birim: string;
}

interface EkranVerisi {
  aktifIsletme: string;
  kayitlar: Kayit[];
  gelirKalemleri: string[];
  giderKalemleri: string[];
  stokUrunleri: StokUrun[];
  odemeYontemleri: OdemeSecenek[];
}

interface FormDurumu {
  id: number | null;
  tarih: string;
  tur: Tur;
  tutar: string;
  odemeYontemi: OdemeYontemi;
  kalem: string;
  aciklama: string;
  stokAktif: boolean;
  stokUrunId: number;
  stokMiktar: string;
}

const bosForm = (): FormDurumu => ({
  id: null,
  tarih: simdiInputDegeri(),
  tur: "gelir",
  tutar: "",
  odemeYontemi: "nakit",
  kalem: "",
  aciklama: "",
  stokAktif: false,
  stokUrunId: 0,
  stokMiktar: "1"
});

function simdiInputDegeri() {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

function paraBiç(tutar: number) {
  return `${tutar.toLocaleString("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })} TL`;
}

function tarihBiç(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(date);
}

function odemeEtiketi(value: OdemeYontemi) {
  switch (value) {
    case "krediKarti":
      return "Kredi Kartı";
    case "onlineOdeme":
      return "Online Ödeme";
    case "havale":
      return "Havale";
    default:
      return "Nakit";
  }
}

function odemeIkonu(value: OdemeYontemi) {
  switch (value) {
    case "krediKarti":
      return <CreditCard size={17} />;
    case "onlineOdeme":
      return <WalletCards size={17} />;
    case "havale":
      return <Landmark size={17} />;
    default:
      return <Banknote size={17} />;
  }
}

async function jsonOku<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { "Content-Type": "application/json" },
    ...init
  });

  const text = await response.text();
  const payload = text ? JSON.parse(text) : null;
  if (!response.ok) {
    throw new Error(payload?.mesaj ?? payload?.detail ?? "İşlem tamamlanamadı.");
  }

  return payload as T;
}

function App() {
  const [ekran, setEkran] = React.useState<EkranVerisi | null>(null);
  const [form, setForm] = React.useState<FormDurumu>(() => bosForm());
  const [arama, setArama] = React.useState("");
  const [durum, setDurum] = React.useState("Yükleniyor...");
  const [hata, setHata] = React.useState("");
  const [kaydediliyor, setKaydediliyor] = React.useState(false);

  const yenile = React.useCallback(async () => {
    setHata("");
    setDurum("Kayıtlar yükleniyor...");
    const data = await jsonOku<EkranVerisi>("/api/ekran/gelir-gider");
    setEkran(data);
    setDurum(`${data.kayitlar.length} kayıt hazır.`);
  }, []);

  React.useEffect(() => {
    yenile().catch((error: Error) => {
      setDurum("");
      setHata(error.message);
    });
  }, [yenile]);

  const kalemler = form.tur === "gelir" ? ekran?.gelirKalemleri ?? [] : ekran?.giderKalemleri ?? [];
  const stokKullanilabilir = form.id === null && form.tur === "gider" && (ekran?.stokUrunleri.length ?? 0) > 0;

  const filtreliKayitlar = React.useMemo(() => {
    const query = arama.trim().toLocaleLowerCase("tr-TR");
    if (!ekran || !query) return ekran?.kayitlar ?? [];

    return ekran.kayitlar.filter((kayit) => {
      const metin = [
        tarihBiç(kayit.tarih),
        kayit.tur === "gelir" ? "gelir" : "gider",
        odemeEtiketi(kayit.odemeYontemi),
        paraBiç(kayit.tutar),
        kayit.kalem,
        kayit.aciklama
      ]
        .join(" ")
        .toLocaleLowerCase("tr-TR");
      return metin.includes(query);
    });
  }, [arama, ekran]);

  function formuTemizle() {
    setForm(bosForm());
    setHata("");
    setDurum("Yeni kayıt hazır.");
  }

  function kayitSec(kayit: Kayit) {
    setForm({
      id: kayit.id,
      tarih: kayit.tarih,
      tur: kayit.tur,
      tutar: kayit.tutar.toString().replace(".", ","),
      odemeYontemi: kayit.odemeYontemi,
      kalem: kayit.kalem,
      aciklama: kayit.aciklama,
      stokAktif: false,
      stokUrunId: 0,
      stokMiktar: "1"
    });
    setHata("");
    setDurum("Kayıt düzenleniyor.");
  }

  function formGuncelle(patch: Partial<FormDurumu>) {
    setForm((current) => {
      const next = { ...current, ...patch };
      if (patch.tur === "gelir") {
        next.stokAktif = false;
        next.stokUrunId = 0;
        next.stokMiktar = "1";
      }
      return next;
    });
  }

  function tutarOku() {
    const normalized = form.tutar.trim().replace(/\s/g, "").replace(",", ".");
    const amount = Number(normalized);
    if (!Number.isFinite(amount) || amount <= 0) {
      throw new Error("Tutar sıfırdan büyük olmalıdır.");
    }
    return amount;
  }

  async function kaydet() {
    try {
      setKaydediliyor(true);
      setHata("");
      const body = {
        tarih: form.tarih,
        tur: form.tur,
        tutar: tutarOku(),
        odemeYontemi: form.odemeYontemi,
        kalem: form.kalem,
        aciklama: form.aciklama,
        stokGiris: {
          aktif: stokKullanilabilir && form.stokAktif,
          urunId: form.stokUrunId,
          miktar: Number(form.stokMiktar.replace(",", "."))
        }
      };

      if (!body.kalem.trim()) throw new Error("Kalem seçin.");

      if (form.id === null) {
        await jsonOku("/api/ekran/gelir-gider/kayitlar", {
          method: "POST",
          body: JSON.stringify(body)
        });
      } else {
        await jsonOku(`/api/ekran/gelir-gider/kayitlar/${form.id}`, {
          method: "PUT",
          body: JSON.stringify({ ...body, id: form.id })
        });
      }

      await yenile();
      formuTemizle();
      setDurum("Kayıt başarıyla kaydedildi.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Kayıt kaydedilemedi.");
    } finally {
      setKaydediliyor(false);
    }
  }

  async function sil() {
    if (form.id === null) return;
    if (!window.confirm("Seçili kaydı silmek istiyor musunuz?")) return;

    try {
      setKaydediliyor(true);
      setHata("");
      await jsonOku(`/api/ekran/gelir-gider/kayitlar/${form.id}`, { method: "DELETE" });
      await yenile();
      formuTemizle();
      setDurum("Kayıt silindi.");
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Kayıt silinemedi.");
    } finally {
      setKaydediliyor(false);
    }
  }

  return (
    <main className="sayfa">
      <section className="liste-karti kart">
        <div className="kart-baslik">
          <div>
            <h1>Tüm Kayıtlar Listesi</h1>
            <p>{ekran?.aktifIsletme ? `İşletme: ${ekran.aktifIsletme}` : "Kayıtlar hazırlanıyor"}</p>
          </div>
          <label className="arama-kutusu" aria-label="Kayıt ara">
            <Search size={19} />
            <input value={arama} onChange={(event) => setArama(event.target.value)} placeholder="Ara..." />
          </label>
        </div>

        <div className="tablo-kapsayici">
          <table>
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Tür</th>
                <th>Yöntem</th>
                <th className="sayi">Tutar</th>
                <th>Kalem</th>
                <th>Açıklama</th>
              </tr>
            </thead>
            <tbody>
              {filtreliKayitlar.map((kayit) => (
                <tr
                  key={kayit.id}
                  className={form.id === kayit.id ? "secili" : ""}
                  onClick={() => kayitSec(kayit)}
                >
                  <td>{tarihBiç(kayit.tarih)}</td>
                  <td>
                    <span className={`tur ${kayit.tur}`}>{kayit.tur === "gelir" ? "↑ Gelir" : "↓ Gider"}</span>
                  </td>
                  <td>{odemeEtiketi(kayit.odemeYontemi)}</td>
                  <td className={`sayi tutar ${kayit.tur}`}>{paraBiç(kayit.tutar)}</td>
                  <td>{kayit.kalem}</td>
                  <td className="aciklama">{kayit.aciklama}</td>
                </tr>
              ))}
              {filtreliKayitlar.length === 0 && (
                <tr>
                  <td className="bos" colSpan={6}>
                    Kayıt bulunamadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="form-karti kart">
        <div className="form-baslik">
          <h2>İşlem Kayıt Formu</h2>
          <span>{form.id === null ? "Yeni kayıt" : "Düzenleme"}</span>
        </div>

        <div className="form-alani">
          <label className="satir">
            <span>Tarih</span>
            <div className="input-ikonlu">
              <CalendarDays size={18} />
              <input type="datetime-local" value={form.tarih} onChange={(e) => formGuncelle({ tarih: e.target.value })} />
            </div>
          </label>

          <div className="satir">
            <span>Tür</span>
            <div className="segmented">
              <button className={form.tur === "gelir" ? "aktif" : ""} onClick={() => formGuncelle({ tur: "gelir", kalem: "" })}>
                Gelir
              </button>
              <button className={form.tur === "gider" ? "aktif" : ""} onClick={() => formGuncelle({ tur: "gider", kalem: "" })}>
                Gider
              </button>
            </div>
          </div>

          <label className="satir">
            <span>Tutar</span>
            <div className="tutar-alani">
              <strong>TL</strong>
              <input value={form.tutar} onChange={(e) => formGuncelle({ tutar: e.target.value })} placeholder="tutar girin" inputMode="decimal" />
            </div>
          </label>

          <div className="satir">
            <span>Yöntem</span>
            <div className="odeme-grid">
              {(ekran?.odemeYontemleri ?? []).map((odeme) => (
                <button
                  key={odeme.deger}
                  className={form.odemeYontemi === odeme.deger ? "aktif" : ""}
                  onClick={() => formGuncelle({ odemeYontemi: odeme.deger })}
                >
                  {form.odemeYontemi === odeme.deger ? <Check size={16} /> : odemeIkonu(odeme.deger)}
                  {odeme.etiket}
                </button>
              ))}
            </div>
          </div>

          <label className="satir">
            <span>Kalem</span>
            <select value={form.kalem} onChange={(e) => formGuncelle({ kalem: e.target.value })}>
              <option value="">Kalem seçin</option>
              {kalemler.map((kalem) => (
                <option key={kalem} value={kalem}>
                  {kalem}
                </option>
              ))}
            </select>
          </label>

          <div className="satir stok-satiri">
            <span>Stok Girişi</span>
            <div className="stok-alani">
              <label className="kontrol">
                <input
                  type="checkbox"
                  checked={stokKullanilabilir && form.stokAktif}
                  disabled={!stokKullanilabilir}
                  onChange={(e) => formGuncelle({ stokAktif: e.target.checked })}
                />
                Bu gider stoklu ürün alımı
              </label>
              <select
                value={form.stokUrunId}
                disabled={!stokKullanilabilir || !form.stokAktif}
                onChange={(e) => formGuncelle({ stokUrunId: Number(e.target.value) })}
              >
                <option value={0}>Ürün seçin</option>
                {(ekran?.stokUrunleri ?? []).map((urun) => (
                  <option key={urun.id} value={urun.id}>
                    {urun.ad} ({urun.birim})
                  </option>
                ))}
              </select>
              <input
                value={form.stokMiktar}
                disabled={!stokKullanilabilir || !form.stokAktif}
                onChange={(e) => formGuncelle({ stokMiktar: e.target.value })}
                inputMode="decimal"
              />
              <small>
                {form.id !== null
                  ? "Düzenlenen kayıtta stok girişi yapılmaz."
                  : form.tur !== "gider"
                    ? "Stok girişi sadece gider kaydı için kullanılır."
                    : "Kaydedince gider kaydı ve stok girişi birlikte oluşur."}
              </small>
            </div>
          </div>

          <label className="satir aciklama-satiri">
            <span>Açıklama</span>
            <textarea value={form.aciklama} onChange={(e) => formGuncelle({ aciklama: e.target.value })} />
          </label>
        </div>

        <div className="mesaj-alani">
          {hata ? <p className="hata">{hata}</p> : <p>{durum}</p>}
        </div>

        <div className="aksiyonlar">
          <button onClick={() => yenile().catch((error: Error) => setHata(error.message))} disabled={kaydediliyor}>
            <RefreshCw size={17} />
            Yenile
          </button>
          <button onClick={sil} disabled={kaydediliyor || form.id === null}>
            <Trash2 size={17} />
            Sil
          </button>
          <button onClick={formuTemizle} disabled={kaydediliyor}>
            <Plus size={17} />
            Yeni
          </button>
          <button className="birincil" onClick={kaydet} disabled={kaydediliyor}>
            {kaydediliyor ? <Eraser size={17} /> : <Save size={17} />}
            Kaydet
          </button>
        </div>
      </section>
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<App />);
