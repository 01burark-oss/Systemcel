import React from "react";
import {
  Archive,
  CalendarDays,
  CheckCircle2,
  ChevronUp,
  ExternalLink,
  FileCode2,
  FileDown,
  FileText,
  FolderOpen,
  PackageCheck,
  Printer,
  RefreshCw
} from "lucide-react";
import type { UstBarDurumu } from "../../shared/chrome";
import { jsonOku } from "../../shared/json";
import type { RaporlarEkranVerisi, RaporPaket, RaporYazdirFormu } from "./types";

interface RaporlarSayfasiProps {
  onIsletmeDegistir: (id: number) => void;
  ustBar: UstBarDurumu | null;
  ustBarIslemde: boolean;
  yenileAnahtari: number;
}

interface ApiMesaj {
  mesaj: string;
}

interface KlasorSecim {
  yol: string;
}

function bugun() {
  return new Date().toISOString().slice(0, 10);
}

function ayBasi() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function ayDegeri() {
  return new Date().toISOString().slice(0, 7);
}

function secimDegistir(secili: string[], deger: string) {
  return secili.includes(deger)
    ? secili.filter((item) => item !== deger)
    : [...secili, deger];
}

function etiketBic(value: string) {
  return value
    .replaceAll("Yonetici Ozeti", "Yönetici Özeti")
    .replaceAll("KDV Ozeti", "KDV Özeti")
    .replaceAll("Aylik", "Aylık")
    .replaceAll("Gunluk", "Günlük")
    .replaceAll("Haftalik", "Haftalık")
    .replaceAll("Ozel Tarih Araligi", "Özel Tarih Aralığı")
    .replaceAll("Son 30 Gun", "Son 30 Gün")
    .replaceAll("Son 1 Yil", "Son 1 Yıl")
    .replaceAll("Bu Yil", "Bu Yıl")
    .replaceAll("Bugun", "Bugün")
    .replaceAll("Ozeti", "Özeti")
    .replaceAll("Ozet", "Özet")
    .replaceAll("Donemi", "Dönemi")
    .replaceAll("Donem", "Dönem");
}

function bosYazdirFormu(): RaporYazdirFormu {
  return {
    sablon: "yoneticiOzeti",
    aralikKodu: "monthly",
    baslangic: ayBasi(),
    bitis: bugun(),
    notMetni: ""
  };
}

export function RaporlarSayfasi({ yenileAnahtari }: RaporlarSayfasiProps) {
  const [ekran, setEkran] = React.useState<RaporlarEkranVerisi | null>(null);
  const [donem, setDonem] = React.useState(ayDegeri());
  const [klasor, setKlasor] = React.useState("");
  const [formatlar, setFormatlar] = React.useState<string[]>([]);
  const [icerikler, setIcerikler] = React.useState<string[]>([]);
  const [sonPaket, setSonPaket] = React.useState<RaporPaket | null>(null);
  const [yazdirFormu, setYazdirFormu] = React.useState<RaporYazdirFormu>(() => bosYazdirFormu());
  const [hata, setHata] = React.useState("");
  const [durum, setDurum] = React.useState("Raporlar yükleniyor...");
  const [islemde, setIslemde] = React.useState(false);

  const yenile = React.useCallback(async () => {
    setHata("");
    setDurum("Raporlar yükleniyor...");
    const data = await jsonOku<RaporlarEkranVerisi>("/api/ekran/raporlar");
    setEkran(data);
    setDonem((current) => current || data.varsayilanDonem);
    setKlasor((current) => current || data.varsayilanKlasor);
    setFormatlar(data.formatlar.filter((row) => row.secili).map((row) => row.deger));
    setIcerikler(data.icerikler.filter((row) => row.secili).map((row) => row.deger));
    setSonPaket(data.sonPaket);
    setDurum(data.sonPaket?.varMi ? `${data.sonPaket.ad} hazır.` : "");
  }, []);

  React.useEffect(() => {
    yenile().catch((error: Error) => {
      setHata(error.message);
      setDurum("");
    });
  }, [yenile, yenileAnahtari]);

  const yazdirFormuGuncelle = <K extends keyof RaporYazdirFormu>(key: K, value: RaporYazdirFormu[K]) => {
    setYazdirFormu((current) => ({ ...current, [key]: value }));
  };

  const klasorSec = async () => {
    try {
      setIslemde(true);
      setHata("");
      const result = await jsonOku<KlasorSecim>("/api/ekran/raporlar/klasor-sec", {
        method: "POST",
        body: JSON.stringify({ yol: klasor })
      });
      if (result.yol) {
        setKlasor(result.yol);
        setDurum("Kayıt klasörü seçildi.");
      }
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Klasör seçilemedi.");
    } finally {
      setIslemde(false);
    }
  };

  const paketOlustur = async () => {
    try {
      if (formatlar.length === 0) {
        throw new Error("En az bir dış aktarım formatı seçin.");
      }

      setIslemde(true);
      setHata("");
      setDurum("Rapor paketi oluşturuluyor...");
      const result = await jsonOku<RaporPaket>("/api/ekran/raporlar/paket", {
        method: "POST",
        body: JSON.stringify({ donem, klasor, formatlar, icerikler })
      });
      setSonPaket(result);
      setDurum(`${result.ad} oluşturuldu.`);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Rapor paketi oluşturulamadı.");
    } finally {
      setIslemde(false);
    }
  };

  const paketAksiyonu = async (aksiyon: "ac" | "klasor") => {
    try {
      if (!sonPaket?.yol) {
        throw new Error("Önce rapor paketi oluşturun.");
      }

      setIslemde(true);
      setHata("");
      const result = await jsonOku<ApiMesaj>(`/api/ekran/raporlar/paket/${aksiyon}`, {
        method: "POST",
        body: JSON.stringify({ yol: sonPaket.yol })
      });
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Rapor paketi açılamadı.");
    } finally {
      setIslemde(false);
    }
  };

  const yazdirAksiyonu = async (aksiyon: "pdf" | "html" | "yazdir") => {
    try {
      setIslemde(true);
      setHata("");
      const endpoint =
        aksiyon === "yazdir"
          ? "/api/ekran/raporlar/yazdir"
          : `/api/ekran/raporlar/yazdir/${aksiyon}`;
      const result = await jsonOku<ApiMesaj>(endpoint, {
        method: "POST",
        body: JSON.stringify(yazdirFormu)
      });
      setDurum(result.mesaj);
    } catch (error) {
      setHata(error instanceof Error ? error.message : "Rapor aksiyonu tamamlanamadı.");
    } finally {
      setIslemde(false);
    }
  };

  return (
    <main className="reports-page">
      <section className="reports-layout">
        <div className="reports-left">
          <section className="reports-card reports-package-card">
            <div className="reports-card__header">
              <div>
                <span className="reports-eyebrow">
                  <FileText size={18} />
                  Aylık Muhasebe Paketi
                </span>
                <h2>Rapor paketini oluştur</h2>
                <p>Paket içeriği otomatik hazırlanır; formatları ihtiyaca göre seçin.</p>
              </div>
              <button className="reports-icon-btn" disabled={islemde} type="button" onClick={yenile}>
                <RefreshCw size={19} />
              </button>
            </div>

            <div className="reports-form-grid reports-form-grid--package">
              <label className="reports-field">
                <span>Dönem</span>
                <input value={donem} onChange={(event) => setDonem(event.target.value)} type="month" />
              </label>
              <label className="reports-field reports-field--folder">
                <span>Kayıt Klasörü</span>
                <input value={klasor} onChange={(event) => setKlasor(event.target.value)} />
              </label>
              <button className="reports-btn" disabled={islemde} type="button" onClick={klasorSec}>
                <FolderOpen size={17} />
                Klasör Seç
              </button>
            </div>

            <SelectionGroup
              items={ekran?.formatlar ?? []}
              selected={formatlar}
              title="Dışa Aktarım Formatı"
              onToggle={(deger) => setFormatlar((current) => secimDegistir(current, deger))}
            />

            <SelectionGroup
              items={ekran?.icerikler ?? []}
              selected={icerikler}
              title="İçerik Filtresi"
              onToggle={(deger) => setIcerikler((current) => secimDegistir(current, deger))}
            />

            <div className="reports-actions reports-actions--package">
              <button className="reports-btn" disabled={!sonPaket?.varMi || islemde} type="button" onClick={() => paketAksiyonu("klasor")}>
                <FolderOpen size={17} />
                Klasörü Aç
              </button>
              <button className="reports-btn reports-btn--primary" disabled={islemde} type="button" onClick={paketOlustur}>
                <PackageCheck size={18} />
                Paketi Oluştur
              </button>
            </div>
          </section>

          <section className="reports-card reports-preview-card">
            <div className="reports-card__header">
              <div>
                <span className="reports-eyebrow">
                  <Archive size={18} />
                  Son Rapor / Önizleme
                </span>
                <h2>{sonPaket?.varMi ? sonPaket.ad : `${donem || ekran?.varsayilanDonem || "Bu ay"} paketi henüz oluşturulmadı`}</h2>
                <p>{sonPaket?.varMi ? `${sonPaket.donem} dönemi - ${sonPaket.olusturmaZamani}` : "Yukarıdaki seçenekleri belirleyip paket oluşturabilirsiniz."}</p>
              </div>
              <CheckCircle2 className={sonPaket?.varMi ? "reports-ok" : "reports-muted-icon"} size={24} />
            </div>

            <div className="reports-preview-box">
              <FileText size={42} />
              <div>
                <strong>{sonPaket?.varMi ? sonPaket.yol : "Rapor paketi bekleniyor"}</strong>
                <span>{sonPaket?.varMi ? sonPaket.klasor : "Excel, HTML ve ZIP çıktısı seçilen klasöre kaydedilecek."}</span>
              </div>
            </div>

            <div className="reports-actions">
              <button className="reports-btn" disabled={!sonPaket?.varMi || islemde} type="button" onClick={() => paketAksiyonu("ac")}>
                <ExternalLink size={17} />
                Paketi Aç
              </button>
              <button className="reports-btn" disabled={!sonPaket?.varMi || islemde} type="button" onClick={() => paketAksiyonu("klasor")}>
                <FolderOpen size={17} />
                Klasörde Göster
              </button>
              <button className="reports-btn reports-btn--primary" disabled={islemde} type="button" onClick={paketOlustur}>
                <RefreshCw size={17} />
                Yeniden Oluştur
              </button>
            </div>
          </section>
        </div>

        <aside className="reports-side">
          <section className="reports-card reports-print-card">
            <div className="reports-card__header reports-card__header--compact">
              <div>
                <h2>Yazdır / Dışa Aktar</h2>
                <p>Mevcut yazdırma raporlarını Raporlar içinden üret.</p>
              </div>
              <ChevronUp size={21} />
            </div>

            <div className="reports-print-form">
              <label className="reports-field reports-field--full">
                <span>Şablon</span>
                <select value={yazdirFormu.sablon} onChange={(event) => yazdirFormuGuncelle("sablon", event.target.value)}>
                  {(ekran?.yazdirmaSablonlari ?? []).map((option) => (
                    <option key={option.deger} value={option.deger}>
                      {etiketBic(option.etiket)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="reports-field reports-field--full">
                <span>Tarih Aralığı</span>
                <select value={yazdirFormu.aralikKodu} onChange={(event) => yazdirFormuGuncelle("aralikKodu", event.target.value)}>
                  {(ekran?.tarihAraliklari ?? []).map((option) => (
                    <option key={option.deger} value={option.deger}>
                      {etiketBic(option.etiket)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="reports-field">
                <span>Başlangıç</span>
                <input
                  disabled={yazdirFormu.aralikKodu !== "custom"}
                  value={yazdirFormu.baslangic}
                  onChange={(event) => yazdirFormuGuncelle("baslangic", event.target.value)}
                  type="date"
                />
              </label>
              <label className="reports-field">
                <span>Bitiş</span>
                <input
                  disabled={yazdirFormu.aralikKodu !== "custom"}
                  value={yazdirFormu.bitis}
                  onChange={(event) => yazdirFormuGuncelle("bitis", event.target.value)}
                  type="date"
                />
              </label>
              <label className="reports-field reports-field--full">
                <span>Not</span>
                <textarea
                  value={yazdirFormu.notMetni}
                  onChange={(event) => yazdirFormuGuncelle("notMetni", event.target.value)}
                  placeholder="Rapor notu giriniz..."
                />
              </label>
            </div>

            <div className="reports-print-actions">
              <button className="reports-btn reports-btn--primary" disabled={islemde} type="button" onClick={() => yazdirAksiyonu("pdf")}>
                <FileDown size={17} />
                PDF Kaydet
              </button>
              <button className="reports-btn" disabled={islemde} type="button" onClick={() => yazdirAksiyonu("html")}>
                <FileCode2 size={17} />
                HTML
              </button>
              <button className="reports-btn reports-btn--success" disabled={islemde} type="button" onClick={() => yazdirAksiyonu("yazdir")}>
                <Printer size={17} />
                Yazdır
              </button>
            </div>

            <div className="reports-info-box">
              <CalendarDays size={20} />
              <p>PDF ve yazdırma işlemleri mevcut rapor şablonlarını kullanır. Paketleme ise muhasebe aktarım dosyalarını ZIP olarak hazırlar.</p>
            </div>
          </section>
        </aside>
      </section>

      {(hata || durum) && (
        <p className="reports-feedback">
          {hata ? <span className="reports-feedback__error">{hata}</span> : durum}
        </p>
      )}
    </main>
  );
}

function SelectionGroup({
  items,
  onToggle,
  selected,
  title
}: {
  items: { deger: string; etiket: string }[];
  onToggle: (deger: string) => void;
  selected: string[];
  title: string;
}) {
  return (
    <div className="reports-selection">
      <h3>{title}</h3>
      <div>
        {items.map((item) => (
          <button
            key={item.deger}
            className={selected.includes(item.deger) ? "selected" : ""}
            type="button"
            onClick={() => onToggle(item.deger)}
          >
            <CheckCircle2 size={16} />
            {etiketBic(item.etiket)}
          </button>
        ))}
      </div>
    </div>
  );
}
