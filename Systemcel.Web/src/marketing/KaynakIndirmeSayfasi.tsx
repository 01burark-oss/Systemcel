import { ArrowDownToLine, FileCheck2, FileSpreadsheet } from "lucide-react";
import systemcelBrand from "../assets/systemcel-brand.svg";
import "./kaynak-indirme.css";

interface KaynakBilgisi {
  etiket: string;
  baslik: string;
  aciklama: string;
  dosya: string;
  tur: "PDF" | "Excel";
  not: string;
}

const kaynaklar: Record<string, KaynakBilgisi> = {
  ai: {
    etiket: "AI",
    baslik: "Defterine sorulacak 20 doğru soru",
    aciklama: "Kayıtlı finansal veriyi hedefleyen, dayanağı görülebilir soru örnekleri.",
    dosya: "systemcel-defterine-sorulacak-20-soru.pdf",
    tur: "PDF",
    not: "AI yalnız sisteme girilmiş veriyi kullanabilir; nihai ticari veya finansal karar vermez."
  },
  nakit: {
    etiket: "NAKİT",
    baslik: "13 haftalık nakit akışı şablonu",
    aciklama: "Tahsilat ve ödemeleri haftalara dağıtan, formüllü ve düzenlenebilir çalışma dosyası.",
    dosya: "systemcel-13-haftalik-nakit-akisi.xlsx",
    tur: "Excel",
    not: "Şablon banka hesabınıza bağlanmaz; yalnızca sizin girdiğiniz kalemleri hesaplar."
  },
  defter: {
    etiket: "DEFTER",
    baslik: "Ay sonu kapanış kontrol listesi",
    aciklama: "Belgelerden cari mutabakata, kasadan devir notlarına kadar pratik kapanış sırası.",
    dosya: "systemcel-ay-sonu-kapanis-kontrol-listesi.pdf",
    tur: "PDF",
    not: "İşletme içi kontrol kaynağıdır; vergisel kapsamınızı mali müşavirinizle doğrulayın."
  },
  takvim: {
    etiket: "TAKVİM",
    baslik: "Eylül 2026 beyanname takvimi",
    aciklama: "GİB 2026 takvimindeki Eylül son günlerinin işletmeler için sadeleştirilmiş görünümü.",
    dosya: "systemcel-eylul-2026-beyanname-takvimi.pdf",
    tur: "PDF",
    not: "Her tarih her mükellefe uygulanmaz; canlı GİB duyurularını ve mali müşavirinizi esas alın."
  },
  "50": {
    etiket: "İLK 50",
    baslik: "Kampanya detayları ve fiyat karşılaştırması",
    aciklama: "Büyüme planının ilk 50 yıllık fiyatı, KDV karşılığı, koşullar ve kayıt bağlantısı.",
    dosya: "systemcel-ilk-50-kampanya-detaylari.pdf",
    tur: "PDF",
    not: "İlk 50: yıllık ₺11.880 + KDV. Kontenjan sonrası: yıllık ₺15.480 + KDV."
  }
};

export function KaynakIndirmeSayfasi({ kod }: { kod: string }) {
  const kaynak = kaynaklar[kod.toLocaleLowerCase("tr-TR")];

  if (!kaynak) {
    return (
      <main className="resource-download-page">
        <section className="resource-download-card is-missing">
          <img src={systemcelBrand} alt="Systemcel" />
          <h1>Kaynak bulunamadı</h1>
          <p>Bağlantıyı kontrol edin veya kaynağı yeniden isteyin.</p>
          <a href="/">systemcel.app’e dön</a>
        </section>
      </main>
    );
  }

  const Icon = kaynak.tur === "Excel" ? FileSpreadsheet : FileCheck2;
  const fileUrl = `/kaynaklar/dosyalar/${kaynak.dosya}`;

  return (
    <main className="resource-download-page">
      <section className="resource-download-card">
        <header>
          <img src={systemcelBrand} alt="Systemcel" />
          <span>Ücretsiz çalışma kaynağı</span>
        </header>
        <div className="resource-download-body">
          <div className="resource-download-icon"><Icon size={30} /></div>
          <p className="resource-download-kicker">{kaynak.etiket} · {kaynak.tur}</p>
          <h1>{kaynak.baslik}</h1>
          <p className="resource-download-description">{kaynak.aciklama}</p>
          <a className="resource-download-button" href={fileUrl} download>
            <ArrowDownToLine size={20} /> Dosyayı indir
          </a>
          <p className="resource-download-note">{kaynak.not}</p>
        </div>
        <footer>systemcel.app · Eylül 2026</footer>
      </section>
    </main>
  );
}
