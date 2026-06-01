import React from "react";
import { Mail } from "lucide-react";
import { helpTopics } from "../../shared/HelpDropdown";

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
        body: "Systemcel; gelir, gider, fatura, cari hesap, stok, tahsilat ve rapor akışlarını aynı çalışma alanında toplar. Amaç tek tek dosya, mesaj ve tablo kovalamadan finansal görünürlüğü tek ekrandan sağlamaktır."
      },
      {
        title: "Deneme süresi nasıl işler?",
        body: "Ücretsiz başlatıldığında kullanıcı ürünü ödeme yapmadan dener. Deneme sonunda uygun plan seçilirse kayıtlar aynı işletme altında devam eder; tekrar kurulum yapmaya gerek kalmaz."
      },
      {
        title: "AI mesaj hakkı ne anlama gelir?",
        body: "AI hakkı, asistanla yapılan otomatik yorumlama, özetleme ve işlem hazırlama isteklerini ifade eder. Örneğin dönem özeti, kayıt açıklaması veya finansal yorum üretimi bu haklardan düşebilir."
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
    lead: "İlk kurulumun amacı işletme alanını doğru bilgilerle açmak ve ilk kayıtları güvenli şekilde denemektir.",
    subsections: [
      {
        title: "İşletme alanı oluşturma",
        body: "Her şey işletme kaydıyla başlar. İşletme adı, iletişim bilgileri ve temel finans tercihleri girildiğinde Systemcel bu alanı gelir-gider, fatura, cari ve rapor modüllerinin merkezi olarak kullanır."
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
        body: "Kurulumdan sonra küçük bir gelir veya gider kaydı açmak en iyi kontroldür. Bu kayıt dashboard, rapor ve nakit akışı ekranlarında görünüyorsa temel akış hazır demektir."
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
        body: "Gelir işletmeye para girişini, gider ise para çıkışını temsil eder. Dashboard üzerindeki toplam gelir, toplam gider ve net kar hesapları bu ayrıma göre oluşur."
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
        body: "Fatura ödenmedi, bekliyor, ödendi veya vadesi geçti gibi durumlarla izlenir. Bu durumlar dashboard üzerinde bekleyen tahsilat ve yaklaşan ödeme görünürlüğünü besler."
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
    lead: "GİB e-Arşiv akışı, portal işlemlerini uygulamadan yönetilebilir hale getirmek için tasarlanır.",
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
        title: "SMS onayı",
        body: "GİB tarafında gerekli olduğunda SMS doğrulaması istenir. Bu adım kullanıcı kontrolünde tamamlanır; sistem onay kodu olmadan resmi işlemi tamamlamaz."
      },
      {
        title: "Hata durumları",
        body: "Portal erişimi, eksik müşteri bilgisi, yanlış vergi numarası veya bağlantı kesintisi gibi durumlarda işlem tamamlanmayabilir. Kullanıcı hatayı düzelttikten sonra aynı faturadan devam edebilir."
      },
      {
        title: "Güvenli kullanım",
        body: "GİB bilgileri yalnızca fatura işlemi için kullanılır. Ekip içinde bu ayarlara kimlerin erişeceği işletmenin kullanıcı yönetimiyle sınırlandırılmalıdır."
      }
    ]
  },
  telegram: {
    lead: "Telegram bildirimleri, uygulamaya girmeden önemli finans hareketlerini takip etmek için ek bir kanal sağlar.",
    subsections: [
      {
        title: "Bağlantı mantığı",
        body: "Kullanıcı Telegram botunu bağladığında Systemcel belirlenen sohbet kanalına özet ve uyarı mesajları gönderebilir. Bağlantı işletme bazında düşünülür."
      },
      {
        title: "Hangi bildirimler gider?",
        body: "Günlük özet, bekleyen tahsilat, vadesi yaklaşan fatura, kritik stok veya dönem raporu gibi önemli olaylar Telegram'a taşınabilir. Amaç kullanıcıyı sadece aksiyon gereken konularda uyarmaktır."
      },
      {
        title: "Fotoğraf ve belge akışı",
        body: "Kullanıcı fiş veya belgeyi Telegram üzerinden ilettiğinde bu içerik kayıt hazırlama sürecinde kullanılabilir. Son kayıt yine kontrol edilerek uygulama verisine eklenmelidir."
      },
      {
        title: "Bildirim kalabalığını azaltma",
        body: "Her hareket için bildirim göndermek yerine özet ve eşik mantığı kullanılmalıdır. Böylece kullanıcı önemli durumları kaçırmadan mesaj yorgunluğu yaşamaz."
      },
      {
        title: "Bağlantı kesilirse",
        body: "Telegram bağlantısı koparsa uygulama içindeki kayıtlar ve raporlar çalışmaya devam eder. Sadece dış bildirim kanalı durur; tekrar bağlandığında bildirim akışı devam eder."
      }
    ]
  },
  raporlar: {
    lead: "Raporlar, girilen kayıtları dönemsel finansal kararlara dönüştürür.",
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
  abonelik: {
    lead: "Abonelik alanı, işletme veya muhasebeci kullanımına göre plan seçimini ve limitleri açık hale getirir.",
    subsections: [
      {
        title: "İşletme ve muhasebeci ayrımı",
        body: "İşletme planları kendi finansını yöneten ekipler içindir. Muhasebeci planları ise birden fazla müşteri portföyünü aynı hesap altında yönetmek için tasarlanır."
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
        body: "İptal akışında kullanıcıya mevcut dönem, veri erişimi ve devam eden haklar net gösterilmelidir. Böylece ödeme durumu ile veri yönetimi birbirine karışmaz."
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

export function YardimSayfasi() {
  const [activeId, setActiveId] = React.useState(getActiveTopicId);
  const [activeSubsectionId, setActiveSubsectionId] = React.useState(() => window.location.hash.replace("#", ""));

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
                  <a href={topic.href} aria-current={isActive ? "true" : undefined}>
                    {topic.title}
                  </a>
                  {isActive ? (
                    <div className="help-sidebar__sublist">
                      {article.subsections.map((subsection, index) => (
                        <a
                          className={activeSubsectionId === `${id}-${index}` ? "is-active" : undefined}
                          href={`#${id}-${index}`}
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

      <section className="help-contact">
        <Mail size={26} />
        <div>
          <h2>Cevabını bulamadın mı?</h2>
          <p>Destek talebi, demo veya satış görüşmesi için ekibe ulaş.</p>
        </div>
        <a href="mailto:iletisim@systemcel.com">İletişime geç</a>
      </section>
    </main>
  );
}
