import React from "react";
import { Loader2, Mail, MessageCircle, Send } from "lucide-react";
import { helpTopics } from "../../shared/HelpDropdown";
import { jsonOku } from "../../shared/json";
import { useI18n } from "../../shared/i18n";

type HelpSubsection = {
  title: string;
  body: string;
  bullets?: string[];
};

type HelpArticle = {
  lead: string;
  subsections: HelpSubsection[];
};

const helpContent: Record<string, HelpArticle> = {
  sss: {
    lead: "Kullanıcıların ilk gün en çok takıldığı konuları kısa cevaplarla toparladık.",
    subsections: [
      {
        title: "Systemcel neyi çözer?",
        body: "Systemcel; gelir, gider, fatura, cari hesap, stok, tahsilat ve raporları tek yerde toplar. Böylece dosya, mesaj ve tablolar arasında aramadan finans durumunuzu tek ekranda görürsünüz."
      },
      {
        title: "Deneme süresi nasıl işler?",
        body: "Ücretsiz başlatıldığında kullanıcı ürünü ödeme yapmadan dener. Deneme sonunda uygun plan seçilirse kayıtlar aynı işletme altında devam eder; tekrar kurulum yapmaya gerek kalmaz."
      },
      {
        title: "Systemcel AI hakkı neleri kapsar?",
        body: "Planınızdaki mesaj hakkı, asistana gönderdiğiniz soruları ve özet isteklerini kapsar. Akıllı eşleştirme, fiş ve fatura kontrolleri, veri taşıma önerileri ve günlük öncelikler de Systemcel AI paketine dahildir."
      },
      {
        title: "GİB bilgisi olmadan kullanabilir miyim?",
        body: "Evet. Gelir-gider, cari, stok, rapor ve fatura taslaklarını GİB bağlantısı olmadan da kullanabilirsin. GİB e-Arşiv sadece uygulama üzerinden resmi e-Arşiv faturası kesmek istediğinde gerekir."
      },
      {
        title: "Telegram bağlamazsam sistem eksik çalışır mı?",
        body: "Hayır. Telegram isteğe bağlıdır. Bağlandığında finansal özetleri ve önemli durumları mesaj olarak alırsın; bağlamazsan uygulama içindeki ekranlar aynı şekilde çalışmaya devam eder."
      },
      {
        title: "Yanlış kayıt girersem ne olur?",
        body: "Kayıtlar düzenlenebilir yapıdadır. Tutar, kategori, ödeme yöntemi, açıklama veya cari bağlantı yanlış girildiyse ilgili modülden kayıt açılıp düzeltilir; raporlar güncel veriye göre yeniden hesaplanır."
      },
      {
        title: "Muhasebeciler müşterileri nasıl ayırır?",
        body: "Muhasebeci kullanımında her müşteri ayrı işletme/çalışma alanı gibi düşünülür. Böylece müşteri verileri, raporları ve dönem takibi birbirine karışmadan yönetilir."
      }
    ]
  },
  "ilk-kurulum": {
    lead: "İlk kurulumda işletme bilgilerinizi ve günlük kullanım tercihlerinizi belirlersiniz.",
    subsections: [
      {
        title: "İşletme alanı oluşturma",
        body: "Önce işletme adını, iletişim bilgilerini ve temel finans tercihlerini girin. Gelir-gider, fatura, cari hesap ve raporlar bu işletmeye bağlı tutulur."
      },
      {
        title: "İlk kullanıcı ve yetki mantığı",
        body: "İlk kullanıcı genellikle işletme sahibi veya finans sorumlusudur. Daha sonra ekip üyeleri eklenirse her kullanıcının hangi işletmeye eriştiği ve hangi işlemleri yapabileceği ayrı şekilde yönetilir."
      },
      {
        title: "Başlangıç ayarları",
        body: "Para birimi, kategori listeleri, ödeme yöntemleri ve GİB bilgileri gibi ayarlar ürünün günlük kayıt mantığını belirler. Bu bilgiler başta doğru kurulursa raporlar daha temiz oluşur."
      },
      {
        title: "İlk test kaydı",
        body: "Kurulumdan sonra deneme amaçlı bir gelir veya gider ekleyin. Kayıt ana ekranda, raporlarda ve nakit akışında görünüyorsa kurulumu tamamladınız."
      }
    ]
  },
  "gelir-gider": {
    lead: "Gelir-gider modülü, kasaya giren ve kasadan çıkan hareketlerin düzenli şekilde kaydedilmesini sağlar.",
    subsections: [
      {
        title: "Kayıt mantığı",
        body: "Her hareket bir tutar, tarih, ödeme yöntemi, kategori ve açıklamayla kaydedilir. Bu alanlar sonradan rapor filtrelerinde kullanıldığı için ne kadar düzenli girilirse finansal görünürlük o kadar netleşir."
      },
      {
        title: "Gelir ve gider ayrımı",
        body: "Gelir işletmeye para girişini, gider ise para çıkışını gösterir. Ana ekrandaki toplam gelir, toplam gider ve net kâr bu ayrıma göre hesaplanır."
      },
      {
        title: "Kategori ve ödeme yöntemi",
        body: "Kategori hareketin nedenini; ödeme yöntemi ise paranın hangi kanaldan geçtiğini gösterir. Örneğin kira gideri nakit, satış geliri kredi kartı veya havale olarak işaretlenebilir."
      },
      {
        title: "Cari veya fatura bağlantısı",
        body: "Bir hareket müşteri, tedarikçi veya faturayla ilişkiliyse ilgili kayıtla bağlanır. Böylece tek hareket hem kasa akışına hem de cari hesap geçmişine yansır."
      },
      {
        title: "Düzeltme ve takip",
        body: "Yanlış girilen hareketler düzenlenebilir. Düzenleme yapıldığında dönem özetleri, ödeme yöntemi dağılımı ve rapor sonuçları güncel tutara göre yeniden okunur."
      }
    ]
  },
  faturalar: {
    lead: "Faturalar modülü, satış ve alış faturalarını oluşturmak, ödeme durumunu izlemek ve tahsilatla bağlamak için kullanılır.",
    subsections: [
      {
        title: "Satış ve alış faturası",
        body: "Satış faturası müşteriye kesilen geliri, alış faturası tedarikçiden gelen gideri temsil eder. Bu ayrım hem cari hesap hem de rapor tarafında doğru sınıflandırma sağlar."
      },
      {
        title: "Satır düzeni",
        body: "Fatura satırlarında ürün, hizmet, miktar, birim fiyat ve toplam tutar yönetilir. Satırlar doğru girildiğinde stok, gelir ve müşteri borcu daha anlamlı takip edilir."
      },
      {
        title: "Ödeme durumu",
        body: "Fatura; bekliyor, ödendi veya vadesi geçti gibi durumlarla izlenir. Ana ekranda bekleyen tahsilatları ve yaklaşan ödemeleri görebilirsiniz."
      },
      {
        title: "Tahsilata bağlama",
        body: "Bir fatura ödendiğinde ödeme hareketi faturaya bağlanır. Böylece hem fatura kapanır hem de gelir-gider kaydı aynı işlemden üretilmiş olur."
      },
      {
        title: "Vade takibi",
        body: "Vade tarihi girilen faturalar gecikme ve yaklaşan tahsilat listelerinde görünür. Bu sayede kullanıcı hangi faturanın öncelikli takip edilmesi gerektiğini kaçırmaz."
      }
    ]
  },
  "gib-e-arsiv": {
    lead: "Systemcel'de e-Arşiv fatura taslağı hazırlayabilir ve gönderim adımlarını uygulamadan yönetebilirsiniz.",
    subsections: [
      {
        title: "Portal bilgileri",
        body: "GİB portal kullanıcı bilgileri ayar alanında saklanır ve fatura kesme akışında kullanılır. Bu bilgiler girilmeden resmi e-Arşiv gönderimi yapılamaz."
      },
      {
        title: "Taslak oluşturma",
        body: "Systemcel'deki fatura bilgileriyle önce taslak hazırlanır. Kullanıcı müşteri bilgilerini, satırları ve tutarları kontrol ettikten sonra gönderim adımına geçer."
      },
      {
        title: "Müşteri bilgi teyidi",
        body: "İşletme isterse taslak bilgilerini müşterinin cari kartındaki telefona bir bağlantıyla gönderir. Müşteri bilgileri teyit eder veya düzeltme ister; bu yanıt resmi e-belge onayı değildir."
      },
      {
        title: "GİB SMS kodu",
        body: "Resmi kesim için gereken kod, GİB Portal'da kayıtlı işletme telefonuna gider. Systemcel bu kod olmadan resmi gönderimi tamamlamaz."
      },
      {
        title: "Hata durumları",
        body: "Portal erişimi, eksik müşteri bilgisi, yanlış vergi numarası veya bağlantı kesintisi gibi durumlarda işlem tamamlanmayabilir. Kullanıcı hatayı düzelttikten sonra aynı faturadan devam edebilir."
      },
      {
        title: "Güvenli kullanım",
        body: "GİB bilgileri yalnızca fatura işlemi için kullanılır. Mevcut sürümde ekran veya işlem bazlı ekip yetkisi bulunmadığı için bu bilgileri yalnız işletme hesabı yetkilileri yönetmelidir."
      }
    ]
  },
  telegram: {
    lead: "İsterseniz finans özetlerini ve önemli uyarıları Telegram'dan alabilirsiniz.",
    subsections: [
      {
        title: "Bağlantı mantığı",
        body: "Kullanıcı Telegram botunu bağladığında Systemcel belirlenen sohbet kanalına özet ve uyarı mesajları gönderebilir. Bağlantı işletme bazında düşünülür."
      },
      {
        title: "Hangi bildirimler gider?",
        body: "Günlük özet, bekleyen tahsilat, vadesi yaklaşan fatura, kritik stok veya dönem raporu gibi bildirimleri Telegram'dan alabilirsiniz."
      },
      {
        title: "Fotoğraf ve belge akışı",
        body: "Kullanıcı fiş veya belgeyi Telegram üzerinden ilettiğinde bu içerik kayıt hazırlama sürecinde kullanılabilir. Son kayıt yine kontrol edilerek uygulama verisine eklenmelidir."
      },
      {
        title: "Bildirim kalabalığını azaltma",
        body: "Bildirimleri yalnızca önemli durumlar için açarak gereksiz mesajları azaltabilirsiniz."
      },
      {
        title: "Bağlantı kesilirse",
        body: "Telegram bağlantısı koparsa uygulama içindeki kayıtlar ve raporlar çalışmaya devam eder. Sadece dış bildirim kanalı durur; tekrar bağlandığında bildirim akışı devam eder."
      }
    ]
  },
  raporlar: {
    lead: "Raporlar, seçtiğiniz dönemde gelirlerinizi, giderlerinizi ve nakit durumunuzu gösterir.",
    subsections: [
      {
        title: "Dönem seçimi",
        body: "Raporlar gün, hafta, ay veya özel tarih aralığına göre okunur. Tarih aralığı değiştikçe toplam gelir, toplam gider, net kar ve dağılımlar yeniden hesaplanır."
      },
      {
        title: "Gelir-gider dağılımı",
        body: "Kategori ve ödeme yöntemi bazlı dağılımlar paranın nereden geldiğini ve nereye gittiğini gösterir. Bu görünüm gereksiz giderleri veya güçlü gelir kanallarını fark etmeyi kolaylaştırır."
      },
      {
        title: "Nakit akışı",
        body: "Nakit akışı raporu tahsilat ve ödeme zamanlamasını gösterir. Karlı görünen bir işletmenin neden nakit sıkışıklığı yaşadığını anlamak için bu ekran önemlidir."
      },
      {
        title: "Çıktı alma",
        body: "Raporlar paylaşılabilir çıktı veya dönem özeti olarak hazırlanabilir. Muhasebeci, yönetici veya ekip içi değerlendirme için aynı verinin sade hali kullanılır."
      },
      {
        title: "AI yorumları",
        body: "AI asistanı raporları özetleyip dikkat edilmesi gereken noktaları kullanıcı diline çevirebilir. Bu yorum karar desteğidir; nihai kontrol kullanıcıdadır."
      }
    ]
  },
  "muhasebeci-baglantisi": {
    lead: "Çalıştığınız muhasebeciyi Systemcel'e davet ederek kayıtları ve görüşmeleri aynı yerde yönetebilirsiniz.",
    subsections: [
      {
        title: "Muhasebeciyi davet etme",
        body: "Muhasebeci bağlantısı ekranından davet bağlantısı oluşturun ve çalıştığınız muhasebeciye gönderin. Muhasebeciniz kendi hesabıyla giriş yaparak daveti kabul eder."
      },
      {
        title: "Yetki seçimi",
        body: "Davet oluştururken Okuma ve rapor ya da Tam işlem yetkisini seçin. Okuma ve rapor yetkisi kayıtları görüntülemeyi; Tam işlem yetkisi kayıtları görüntüleyip düzenlemeyi sağlar."
      },
      {
        title: "Birlikte çalışma",
        body: "Bağlantı kurulduktan sonra belgeleri ve görüşmeleri aynı çalışma alanında yönetebilirsiniz. Muhasebeciniz bağlı müşterilerini kendi panelinde ayrı ayrı görür."
      }
    ]
  },
  "tedarikci-pazaryeri": {
    lead: "Tedarikçi pazaryerinde farklı satıcılardan ürün alabilir ve her teslimatı ayrı takip edebilirsiniz.",
    subsections: [
      {
        title: "Ürün arama ve sepet",
        body: "Ürün, kategori veya tedarikçi adına göre arama yapın. Farklı tedarikçilerin ürünlerini aynı sepete ekleyebilirsiniz."
      },
      {
        title: "Sipariş takibi",
        body: "Siparişten sonra her tedarikçinin onay, hazırlama, kargo, teslimat ve fatura durumunu ayrı izleyebilirsiniz. Gerekirse yalnızca seçtiğiniz tedarikçinin siparişini iptal edebilirsiniz."
      },
      {
        title: "Alım talebi ve teklifler",
        body: "Aradığınız ürün katalogda yoksa alım talebi oluşturun. Tedarikçilerin fiyat, teslimat süresi ve minimum sipariş bilgilerini karşılaştırarak bir teklifi kabul edin."
      },
      {
        title: "Tedarikçi hesabı",
        body: "Tedarikçiler profil başvurusu yapabilir, ürünlerini yayınlayabilir, siparişleri hazırlayıp kargoya verebilir ve satış hakedişlerini takip edebilir."
      }
    ]
  },
  abonelik: {
    lead: "Abonelik alanı, işletme veya muhasebeci kullanımına göre plan seçimini ve limitleri açık hale getirir.",
    subsections: [
      {
        title: "İşletme ve muhasebeci ayrımı",
        body: "İşletme planları kendi finansını yöneten ekipler içindir. Muhasebeci planları birden fazla müşteriyi aynı hesaptan yönetmek içindir."
      },
      {
        title: "Plan limitleri",
        body: "Kullanıcı sayısı, müşteri sayısı, AI hakkı ve bazı otomasyon özellikleri plana göre değişebilir. Plan seçerken sadece bugünkü ihtiyaç değil, yakın dönem büyüme de düşünülmelidir."
      },
      {
        title: "Plan değiştirme",
        body: "İhtiyaç arttığında daha yüksek plana geçmek verileri taşıma gerektirmez. Mevcut kayıtlar aynı işletme altında kalır; sadece haklar ve limitler güncellenir."
      },
      {
        title: "Ücretsiz plandan ücretliye geçiş",
        body: "Ücretsiz kullanım sırasında girilen veriler korunur. Ücretli plan seçildiğinde kullanıcı aynı çalışma alanından devam eder."
      },
      {
        title: "İptal ve erişim",
        body: "Aboneliği dönem sonunda iptal edebilirsiniz. Erişiminizin biteceği tarih iptal ekranında gösterilir."
      }
    ]
  },
  guvenlik: {
    lead: "Güvenlik tarafında amaç işletme verisini doğru kullanıcıyla, doğru yetkiyle ve kontrollü bağlantılarla çalıştırmaktır.",
    subsections: [
      {
        title: "İşletme bazlı veri ayrımı",
        body: "Her işletmenin kayıtları kendi çalışma alanında tutulur. Bu yapı özellikle muhasebeci kullanımında müşterilerin birbirine karışmasını engeller."
      },
      {
        title: "Kullanıcı erişimi",
        body: "Ekip üyeleri yalnızca kendilerine açılan işletme ve modüllerde işlem yapmalıdır. Kullanıcı ayrımı, finansal veriye gereksiz erişimi azaltır."
      },
      {
        title: "Hassas bilgiler",
        body: "GİB portal bilgileri, iletişim kanalları ve ödeme bağlantıları gibi alanlar hassas kabul edilir. Bu alanlar yalnızca yetkili kişiler tarafından güncellenmelidir."
      },
      {
        title: "Kayıt bütünlüğü",
        body: "Finansal kayıtlar değiştirildiğinde rapor sonuçları da değişir. Bu yüzden düzenleme yetkisi kontrollü verilmeli, kritik işlemlerde kullanıcı dikkatli yönlendirilmelidir."
      },
      {
        title: "Güvenli alışkanlıklar",
        body: "Güçlü şifre, kişiye özel kullanıcı hesabı, ortak bilgisayarda oturum kapatma ve GİB bilgilerini sınırlı paylaşma günlük güvenliği artırır."
      }
    ]
  }
};

const getTopicId = (href: string) => href.split("#")[1] ?? "sss";
const topicIds = helpTopics.map((topic) => getTopicId(topic.href));

function getActiveTopicId() {
  const hash = window.location.hash.replace("#", "");
  return topicIds.find((id) => hash === id || hash.startsWith(`${id}-`)) ?? "sss";
}

interface DestekTalebi {
  id: number;
  isletmeId: number;
  isletmeAdi: string;
  konu: string;
  kategori: string;
  aciklama: string;
  oncelik: string;
  durum: string;
  yoneticiYaniti: string;
  createdAt: string;
  updatedAt: string;
}

interface DestekTalebiListesi {
  talepler: DestekTalebi[];
}

function yeniIdempotencyKey() {
  return globalThis.crypto?.randomUUID?.() ?? `destek-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function talepDurumu(durum: string) {
  const anahtar = durum.trim().toLowerCase();
  if (anahtar.includes("coz") || anahtar.includes("tamam")) return "Çözüldü";
  if (anahtar.includes("yanıt") || anahtar.includes("incele") || anahtar.includes("islem")) return "İşlemde";
  return "Açık";
}

function tarih(value: string) {
  if (!value) return "-";
  return new Date(value).toLocaleString("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function kategoriEtiketi(kategori: string) {
  return kategori === "Diger" ? "Diğer" : kategori;
}

export function YardimSayfasi() {
  const { t } = useI18n();
  const [activeId, setActiveId] = React.useState(getActiveTopicId);
  const [activeSubsectionId, setActiveSubsectionId] = React.useState(() => window.location.hash.replace("#", ""));
  const [talepler, setTalepler] = React.useState<DestekTalebi[]>([]);
  const [talepYukleniyor, setTalepYukleniyor] = React.useState(true);
  const [talepGonderiliyor, setTalepGonderiliyor] = React.useState(false);
  const [talepHatasi, setTalepHatasi] = React.useState("");
  const [talepMesaji, setTalepMesaji] = React.useState("");
  const [konu, setKonu] = React.useState("");
  const [kategori, setKategori] = React.useState("Diger");
  const [aciklama, setAciklama] = React.useState("");
  const idempotencyRef = React.useRef(yeniIdempotencyKey());

  const talepleriYukle = React.useCallback(async () => {
    setTalepYukleniyor(true);
    setTalepHatasi("");
    try {
      const sonuc = await jsonOku<DestekTalebiListesi>("/api/ekran/destek-talepleri");
      setTalepler(sonuc.talepler ?? []);
    } catch (error) {
      setTalepHatasi(error instanceof Error ? error.message : "Destek talepleri yüklenemedi.");
    } finally {
      setTalepYukleniyor(false);
    }
  }, []);

  const helpLinkeGit = React.useCallback((event: React.MouseEvent<HTMLAnchorElement>, targetId: string) => {
    event.preventDefault();
    const nextTopicId = topicIds.find((id) => targetId === id || targetId.startsWith(`${id}-`)) ?? "sss";
    const nextSubsectionId = targetId === nextTopicId ? `${nextTopicId}-0` : targetId;
    setActiveId(nextTopicId);
    setActiveSubsectionId(nextSubsectionId);
    window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}#${targetId}`);
    document.getElementById(targetId)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  React.useEffect(() => {
    document.title = "Systemcel Yardım | Destek Merkezi";

    const page = document.querySelector<HTMLElement>(".help-page");
    let frame = 0;

    const syncActiveFromScroll = () => {
      const marker = Math.min(window.innerHeight * 0.34, 260);
      let nextTopicId = topicIds[0];

      for (const topicId of topicIds) {
        const article = document.getElementById(topicId);
        if (!article) {
          continue;
        }

        const rect = article.getBoundingClientRect();
        if (rect.top <= marker) {
          nextTopicId = topicId;
        }
        if (rect.top <= marker && rect.bottom > marker) {
          nextTopicId = topicId;
          break;
        }
      }

      const activeArticle = document.getElementById(nextTopicId);
      const subsections = Array.from(activeArticle?.querySelectorAll<HTMLElement>(".help-subsections section") ?? []);
      let nextSubsectionId = subsections[0]?.id ?? nextTopicId;

      for (const subsection of subsections) {
        if (subsection.getBoundingClientRect().top <= marker + 48) {
          nextSubsectionId = subsection.id;
        }
      }

      setActiveId((current) => (current === nextTopicId ? current : nextTopicId));
      setActiveSubsectionId((current) => (current === nextSubsectionId ? current : nextSubsectionId));
    };

    const requestSync = () => {
      window.cancelAnimationFrame(frame);
      frame = window.requestAnimationFrame(syncActiveFromScroll);
    };

    const syncActiveFromHash = () => {
      const hash = window.location.hash.replace("#", "");
      const nextTopicId = getActiveTopicId();
      setActiveId(nextTopicId);
      setActiveSubsectionId(hash || `${nextTopicId}-0`);
      window.setTimeout(requestSync, 80);
    };

    syncActiveFromHash();
    page?.addEventListener("scroll", requestSync, { passive: true });
    window.addEventListener("resize", requestSync);
    window.addEventListener("hashchange", syncActiveFromHash);

    return () => {
      window.cancelAnimationFrame(frame);
      page?.removeEventListener("scroll", requestSync);
      window.removeEventListener("resize", requestSync);
      window.removeEventListener("hashchange", syncActiveFromHash);
    };
  }, []);

  React.useEffect(() => {
    talepleriYukle().catch(() => undefined);
  }, [talepleriYukle]);

  async function talepGonder(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const temizKonu = konu.trim();
    const temizAciklama = aciklama.trim();
    if (!temizKonu || !temizAciklama) {
      setTalepHatasi("Konu ve açıklama alanlarını doldur.");
      return;
    }

    setTalepGonderiliyor(true);
    setTalepHatasi("");
    setTalepMesaji("");
    try {
      const yeniTalep = await jsonOku<DestekTalebi>("/api/ekran/destek-talepleri", {
        method: "POST",
        headers: { "Idempotency-Key": idempotencyRef.current },
        body: JSON.stringify({ konu: temizKonu, kategori, aciklama: temizAciklama })
      });
      setTalepler((onceki) => [yeniTalep, ...onceki.filter((item) => item.id !== yeniTalep.id)]);
      setKonu("");
      setAciklama("");
      setTalepMesaji("Talebin kaydedildi. Durumu ve yanıtı burada takip edebilirsin.");
      idempotencyRef.current = yeniIdempotencyKey();
    } catch (error) {
      setTalepHatasi(error instanceof Error ? error.message : "Talep kaydedilemedi. Aynı talebi yeniden deneyebilirsin.");
    } finally {
      setTalepGonderiliyor(false);
    }
  }

  return (
    <main className="help-page">
      <section className="help-hero">
        <span>Yardım Merkezi</span>
        <h1>Systemcel'i kurarken ve kullanırken yanında</h1>
        <p>Soldaki başlıklardan ilerle; her bölüm çalışma mantığını, kullanıcıların takılabileceği noktaları ve doğru kullanım şeklini açıklar.</p>
      </section>

      <section className="help-layout" aria-label="Yardım dokümanı">
        <aside className="help-sidebar" aria-label="Yardım başlıkları">
          <span>Başlıklar</span>
          <nav>
            {helpTopics.map((topic) => {
              const id = getTopicId(topic.href);
              const isActive = activeId === id;
              const article = helpContent[id];

              return (
                <div className={`help-sidebar__group ${isActive ? "is-active" : ""}`} key={topic.href}>
                  <a href={topic.href} onClick={(event) => helpLinkeGit(event, id)} aria-current={isActive ? "true" : undefined}>
                    {topic.title}
                  </a>
                  {isActive ? (
                    <div className="help-sidebar__sublist">
                      {article.subsections.map((subsection, index) => (
                        <a
                          className={activeSubsectionId === `${id}-${index}` ? "is-active" : undefined}
                          href={`#${id}-${index}`}
                          onClick={(event) => helpLinkeGit(event, `${id}-${index}`)}
                          key={`${id}-${subsection.title}`}
                          aria-current={activeSubsectionId === `${id}-${index}` ? "location" : undefined}
                        >
                          {subsection.title}
                        </a>
                      ))}
                    </div>
                  ) : null}
                </div>
              );
            })}
          </nav>
        </aside>

        <div className="help-content">
          {helpTopics.map((topic) => {
            const id = getTopicId(topic.href);
            const article = helpContent[id];

            return (
              <article className="help-article" id={id} key={topic.href}>
                <span>{topic.title}</span>
                <h2>{topic.title === "SSS" ? "Sık sorulan sorular" : topic.title}</h2>
                <p className="help-article__lead">{article.lead}</p>

                <div className="help-subsections">
                  {article.subsections.map((subsection, index) => (
                    <section id={`${id}-${index}`} key={`${id}-${subsection.title}`}>
                      <h3>{subsection.title}</h3>
                      <p>{subsection.body}</p>
                      {subsection.bullets ? (
                        <ul>
                          {subsection.bullets.map((item) => (
                            <li key={item}>{item}</li>
                          ))}
                        </ul>
                      ) : null}
                    </section>
                  ))}
                </div>
              </article>
            );
          })}
        </div>
      </section>

      <section className="help-support" aria-labelledby="destek-talebi-baslik">
        <div className="help-support__intro">
          <MessageCircle size={24} aria-hidden="true" />
          <div>
            <span>Uygulama içi destek</span>
            <h2 id="destek-talebi-baslik">{t("support.title")}</h2>
            <p>Talebini buradan kaydet; güncel durumunu ve verilen yanıtı aynı yerde gör.</p>
          </div>
        </div>

        <form className="help-support__form" onSubmit={talepGonder}>
          <label>
            <span>{t("support.subject")}</span>
            <input value={konu} onChange={(event) => setKonu(event.target.value)} maxLength={120} placeholder="Örn. Fatura taslağında hata" required />
          </label>
          <label>
            <span>{t("support.category")}</span>
            <select value={kategori} onChange={(event) => setKategori(event.target.value)}>
              <option value="Teknik">Teknik</option>
              <option value="Faturalama">Faturalama</option>
              <option value="Hesap">Hesap</option>
              <option value="Diger">Diğer</option>
            </select>
          </label>
          <label className="help-support__field--full">
            <span>{t("support.description")}</span>
            <textarea value={aciklama} onChange={(event) => setAciklama(event.target.value)} maxLength={4000} rows={4} placeholder="Ne olduğunu ve hangi adımda takıldığını yaz." required />
          </label>
          <div className="help-support__submit">
            <span>Öncelik planına göre sistem tarafından belirlenir.</span>
            <button type="submit" disabled={talepGonderiliyor}>
              {talepGonderiliyor ? <Loader2 className="spin" size={16} /> : <Send size={16} />}
              {talepGonderiliyor ? t("support.saving") : t("support.submit")}
            </button>
          </div>
        </form>

        {talepHatasi ? <p className="help-support__feedback help-support__feedback--error" role="alert">{talepHatasi}</p> : null}
        {talepMesaji ? <p className="help-support__feedback" role="status">{talepMesaji}</p> : null}

        <div className="help-support__requests" aria-live="polite">
          <div className="help-support__requests-title"><h3>{t("support.requests")}</h3><button type="button" onClick={() => talepleriYukle()} disabled={talepYukleniyor}>{t("admin.refresh")}</button></div>
          {talepYukleniyor ? <p className="help-support__state"><Loader2 className="spin" size={17} /> {t("admin.loading")}</p> : talepler.length === 0 ? <p className="help-support__state">{t("support.empty")}</p> : <div className="help-support__request-list">{talepler.map((talep) => <article key={talep.id} className="help-support__request"><div className="help-support__request-heading"><div><strong>{talep.konu}</strong><span>{kategoriEtiketi(talep.kategori)} · {tarih(talep.createdAt)}</span></div><span className={`help-support__status help-support__status--${talepDurumu(talep.durum).toLocaleLowerCase("tr-TR")}`}>{talepDurumu(talep.durum)}</span></div><p>{talep.aciklama}</p>{talep.yoneticiYaniti ? <div className="help-support__answer"><strong>{t("support.reply")}</strong><p>{talep.yoneticiYaniti}</p></div> : null}</article>)}</div>}
        </div>
      </section>

      <section className="help-contact">
        <Mail size={26} />
        <div>
          <h2>Cevabını bulamadın mı?</h2>
          <p>Destek talebi, demo veya satış görüşmesi için ekibe ulaş.</p>
        </div>
        <a href="mailto:merhaba@systemcel.app">İletişime geç</a>
      </section>
    </main>
  );
}
