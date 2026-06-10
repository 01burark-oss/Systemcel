import { Camera } from "lucide-react";

interface MuhasebeciProfilOnizlemeProps {
  resimUrl: string;
  unvan: string;
  konum: string;
  deneyimYili: string | number;
  ucretBilgisi: string;
  uzmanliklar: string;
  musteriTipleri: string;
  kisaAciklama: string;
}

export function MuhasebeciProfilOnizleme({
  resimUrl,
  unvan,
  konum,
  deneyimYili,
  ucretBilgisi,
  uzmanliklar,
  musteriTipleri,
  kisaAciklama
}: MuhasebeciProfilOnizlemeProps) {
  return (
    <section className="accountant-profile-preview" aria-label="Profil önizleme">
      <div className="accountant-profile-preview__image">
        {resimUrl ? <img src={resimUrl} alt="" /> : <Camera size={22} />}
      </div>
      <div className="accountant-profile-preview__content">
        <span>Profil önizleme</span>
        <strong>{unvan.trim() || "Muhasebe ofisi"}</strong>
        <p>{kisaAciklama.trim() || "Kısa açıklama burada görünecek."}</p>
        <div>
          <small>{konum.trim() || "Konum"}</small>
          <small>{Number(deneyimYili || 0)} yıl deneyim</small>
          <small>{ucretBilgisi.trim() || "Ücret bilgisi"}</small>
        </div>
        <div>
          <em>{uzmanliklar.trim() || "Uzmanlıklar"}</em>
          <em>{musteriTipleri.trim() || "Müşteri tipi"}</em>
        </div>
      </div>
    </section>
  );
}
