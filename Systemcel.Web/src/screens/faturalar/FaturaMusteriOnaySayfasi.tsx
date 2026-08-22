import React from "react";
import { AlertCircle, CheckCircle2, FileCheck2, LoaderCircle } from "lucide-react";
import systemcelBrand from "../../assets/systemcel-brand.svg";
import { jsonOku } from "../../shared/json";
import "./fatura-musteri-onay.css";

interface PublicOnayDetayi {
  durum: string;
  isletmeAdi: string;
  cariUnvan: string;
  cariVergiNoMaskeli: string;
  cariAdres: string;
  faturaNo: string;
  faturaTarihi: string;
  faturaToplami: number;
  paraBirimi: string;
  sonGecerlilikAt: string;
  yanitAt: string | null;
  aciklama: string;
}

interface FaturaMusteriOnaySayfasiProps {
  token: string;
}

function paraBic(value: number, currency: string) {
  return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currency || "TRY" }).format(value);
}

function tarihBic(value: string) {
  return new Date(value).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });
}

export function FaturaMusteriOnaySayfasi({ token }: FaturaMusteriOnaySayfasiProps) {
  const [detay, setDetay] = React.useState<PublicOnayDetayi | null>(null);
  const [hata, setHata] = React.useState("");
  const [aciklama, setAciklama] = React.useState("");
  const [duzeltmeAcik, setDuzeltmeAcik] = React.useState(false);
  const [islemde, setIslemde] = React.useState(false);

  React.useEffect(() => {
    jsonOku<PublicOnayDetayi>(`/api/public/fatura-onaylari/${encodeURIComponent(token)}`)
      .then(setDetay)
      .catch((error: Error) => setHata(error.message));
  }, [token]);

  async function yanitla(bilgilerDogru: boolean) {
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<PublicOnayDetayi>(
        `/api/public/fatura-onaylari/${encodeURIComponent(token)}/yanit`,
        {
          method: "POST",
          body: JSON.stringify({ bilgilerDogru, aciklama })
        }
      );
      setDetay(result);
      setDuzeltmeAcik(false);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Yanıtınız kaydedilemedi.");
    } finally {
      setIslemde(false);
    }
  }

  const bekliyor = detay?.durum === "Bekliyor";
  const olumlu = detay?.durum === "Onaylandi";

  return (
    <main className="customer-confirmation-page">
      <section className="customer-confirmation-shell">
        <header>
          <img src={systemcelBrand} alt="Systemcel" />
          <span>Güvenli bilgi teyidi</span>
        </header>

        {hata && !detay ? (
          <div className="customer-confirmation-state is-error">
            <AlertCircle size={30} />
            <h1>Bağlantı açılamadı</h1>
            <p>{hata}</p>
          </div>
        ) : !detay ? (
          <div className="customer-confirmation-state">
            <LoaderCircle className="is-spinning" size={30} />
            <p>Fatura taslağı yükleniyor…</p>
          </div>
        ) : (
          <>
            <div className={`customer-confirmation-intro ${olumlu ? "is-success" : ""}`}>
              {olumlu ? <CheckCircle2 size={30} /> : <FileCheck2 size={30} />}
              <div>
                <p>{detay.isletmeAdi}</p>
                <h1>{bekliyor ? "Bilgilerinizi kontrol edin" : "Teyit durumu"}</h1>
                <span>{detay.aciklama}</span>
              </div>
            </div>

            <dl className="customer-confirmation-details">
              <div><dt>Müşteri</dt><dd>{detay.cariUnvan}</dd></div>
              <div><dt>Vergi / T.C. No</dt><dd>{detay.cariVergiNoMaskeli}</dd></div>
              <div className="is-full"><dt>Adres</dt><dd>{detay.cariAdres}</dd></div>
              <div><dt>Taslak no</dt><dd>{detay.faturaNo}</dd></div>
              <div><dt>Tarih</dt><dd>{tarihBic(detay.faturaTarihi)}</dd></div>
              <div className="is-total"><dt>Toplam</dt><dd>{paraBic(detay.faturaToplami, detay.paraBirimi)}</dd></div>
            </dl>

            {bekliyor ? (
              <div className="customer-confirmation-actions">
                {duzeltmeAcik ? (
                  <label>
                    <span>Hangi bilgi düzeltilmeli?</span>
                    <textarea
                      autoFocus
                      value={aciklama}
                      onChange={(event) => setAciklama(event.target.value)}
                      maxLength={500}
                      placeholder="Örn. Fatura adresimiz değişti…"
                    />
                  </label>
                ) : null}
                <div>
                  <button className="is-primary" disabled={islemde} onClick={() => yanitla(true)}>
                    <CheckCircle2 size={19} /> Bilgilerim doğru
                  </button>
                  <button
                    disabled={islemde}
                    onClick={() => duzeltmeAcik ? yanitla(false) : setDuzeltmeAcik(true)}
                  >
                    <AlertCircle size={19} /> {duzeltmeAcik ? "Düzeltme isteğini gönder" : "Düzeltme gerekiyor"}
                  </button>
                </div>
              </div>
            ) : null}

            {hata ? <p className="customer-confirmation-error">{hata}</p> : null}
            <footer>
              Bu teyit, müşteri bilgilerinin ve taslak özetinin kontrolüdür. Resmi e-Fatura/e-Arşiv onayı değildir; belge işletmenin GİB işlemi sonrasında kesilir.
            </footer>
          </>
        )}
      </section>
    </main>
  );
}
