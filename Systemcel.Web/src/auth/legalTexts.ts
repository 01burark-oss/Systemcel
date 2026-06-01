export type AuthLanguage = "tr" | "en";
export type LegalTextKey = "terms" | "privacy" | "kvkk";

export type LegalTextSection = {
  title: string;
  text: string;
};

export type LegalTextContent = {
  linkLabel: string;
  title: string;
  updatedAt: string;
  updatedAtLabel: string;
  intro: string;
  note: string;
  closeLabel: string;
  sections: LegalTextSection[];
};

export const legalTexts: Record<AuthLanguage, Record<LegalTextKey, LegalTextContent>> = {
  tr: {
    terms: {
      linkLabel: "Kullanım Şartları",
      title: "Systemcel Kullanıcı Sözleşmesi ve Kullanım Şartları",
      updatedAt: "24 Mayıs 2026",
      updatedAtLabel: "Son güncelleme",
      closeLabel: "Yasal metni kapat",
      intro:
        "Bu metin, Systemcel web uygulamasından yararlanan kullanıcılar ile Systemcel arasındaki temel kullanım koşullarını düzenlemek amacıyla hazırlanmıştır.",
      note:
        "Bu metinler taslak niteliğindedir. Canlı kullanıma geçmeden önce şirket bilgileri ve hukuki hükümler hukuk danışmanı tarafından gözden geçirilmelidir.",
      sections: [
        {
          title: "1. Hizmetin Kapsamı",
          text:
            "Systemcel; gelir, gider, cari hesap, ürün/stok, fatura, tahsilat/ödeme, raporlama ve desteklenen entegrasyon modüllerini tek çalışma alanında sunan bir finans yönetimi yazılımıdır."
        },
        {
          title: "2. Hesap Oluşturma ve Güvenlik",
          text:
            "Kullanıcı, hesap oluştururken verdiği bilgilerin doğru ve güncel olmasından sorumludur. Hesap bilgilerinin gizliliği, parola güvenliği ve yetkisiz erişim risklerinin önlenmesi kullanıcı sorumluluğundadır."
        },
        {
          title: "3. Veri ve İçerik Sorumluluğu",
          text:
            "Uygulamaya girilen finansal kayıtların, belgelerin, işletme bilgilerinin ve entegrasyon verilerinin doğruluğu kullanıcıya aittir. Systemcel bu verileri hizmetin sunulması, güvenli şekilde işletilmesi ve kullanıcı taleplerinin karşılanması amacıyla işler."
        },
        {
          title: "4. Yasaklı Kullanımlar",
          text:
            "Hizmet; hukuka aykırı işlem yapmak, üçüncü kişilerin haklarını ihlal etmek, güvenlik önlemlerini aşmaya çalışmak, yetkisiz erişim sağlamak veya hizmetin sürekliliğini bozmak amacıyla kullanılamaz."
        },
        {
          title: "5. Ücretlendirme ve Abonelik",
          text:
            "Ücretli planlar, deneme süreleri, kullanım limitleri ve ödeme koşulları seçilen pakete göre belirlenir. Plan değişikliği, iptal ve yenileme koşulları ilgili abonelik ekranlarında ayrıca gösterilir."
        },
        {
          title: "6. Sorumluluk Sınırları",
          text:
            "Systemcel, hizmeti makul teknik önlemlerle kesintisiz ve güvenli biçimde sunmayı hedefler. Bununla birlikte kullanıcı tarafından girilen verilerin doğruluğundan, mevzuata uygun muhasebe kayıtlarının tutulmasından ve ticari kararların sonuçlarından kullanıcı sorumludur."
        }
      ]
    },
    privacy: {
      linkLabel: "Gizlilik Politikası",
      title: "Systemcel Gizlilik Politikası",
      updatedAt: "24 Mayıs 2026",
      updatedAtLabel: "Son güncelleme",
      closeLabel: "Yasal metni kapat",
      intro:
        "Bu politika, Systemcel web uygulamasında işlenen kişisel veriler ve gizlilik yaklaşımı hakkında kullanıcıları bilgilendirmek amacıyla hazırlanmıştır.",
      note:
        "Bu metinler taslak niteliğindedir. Canlı kullanıma geçmeden önce şirket bilgileri ve hukuki hükümler hukuk danışmanı tarafından gözden geçirilmelidir.",
      sections: [
        {
          title: "1. İşlenen Veri Kategorileri",
          text:
            "Hesap bilgileri, iletişim bilgileri, işletme bilgileri, finansal kayıtlar, fatura ve cari işlem verileri, kullanım günlükleri, cihaz bilgileri ve entegrasyon ayarları hizmetin sunumu kapsamında işlenebilir."
        },
        {
          title: "2. Verilerin Kullanım Amaçları",
          text:
            "Veriler; hesap oluşturma, kimlik doğrulama, hizmetin işletilmesi, abonelik haklarının yönetilmesi, güvenlik kontrollerinin yapılması, destek taleplerinin karşılanması ve yasal yükümlülüklerin yerine getirilmesi amaçlarıyla kullanılır."
        },
        {
          title: "3. Üçüncü Taraf Hizmetler",
          text:
            "Kimlik doğrulama, bulut altyapısı, ödeme, destek ve kullanıcı tarafından etkinleştirilen entegrasyon hizmetleri kapsamında sınırlı veri paylaşımı yapılabilir. Bu paylaşım yalnızca hizmetin gerektirdiği ölçüde gerçekleştirilir."
        },
        {
          title: "4. Güvenlik Önlemleri",
          text:
            "Systemcel; erişim kontrolü, güvenli bağlantı, şifreleme, kayıt izleme ve operasyonel güvenlik uygulamaları gibi makul teknik ve idari önlemlerle verilerin korunmasını hedefler."
        },
        {
          title: "5. Saklama ve Silme",
          text:
            "Veriler, hizmetin sunulması için gerekli süre boyunca ve ilgili mevzuatta öngörülen saklama yükümlülükleri çerçevesinde muhafaza edilir. Saklama süresi sona eren veriler silinir, yok edilir veya anonim hale getirilir."
        }
      ]
    },
    kvkk: {
      linkLabel: "KVKK Aydınlatma Metni",
      title: "Kişisel Verilerin İşlenmesine İlişkin Aydınlatma Metni",
      updatedAt: "24 Mayıs 2026",
      updatedAtLabel: "Son güncelleme",
      closeLabel: "Yasal metni kapat",
      intro:
        "Bu aydınlatma metni, 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında kişisel verilerin işlenmesine ilişkin temel bilgileri sunmak amacıyla hazırlanmıştır.",
      note:
        "Bu metinler taslak niteliğindedir. Canlı kullanıma geçmeden önce şirket bilgileri ve hukuki hükümler hukuk danışmanı tarafından gözden geçirilmelidir.",
      sections: [
        {
          title: "1. Veri Sorumlusu",
          text:
            "Systemcel hizmetini sunan veri sorumlusuna ilişkin unvan, adres ve iletişim bilgileri canlı kullanım öncesinde şirket bilgileri kesinleştiğinde bu metne eklenecektir."
        },
        {
          title: "2. İşlenen Kişisel Veriler",
          text:
            "Kimlik, iletişim, müşteri işlem, finansal işlem, işlem güvenliği, abonelik, destek talebi ve entegrasyon ayarlarına ilişkin kişisel veriler işlenebilir."
        },
        {
          title: "3. İşleme Amaçları",
          text:
            "Kişisel veriler; üyelik oluşturma, hizmet sunumu, işletme kayıtlarının yönetimi, abonelik ve ödeme süreçleri, güvenlik, destek, yasal yükümlülüklerin yerine getirilmesi ve kullanıcı deneyiminin iyileştirilmesi amaçlarıyla işlenir."
        },
        {
          title: "4. Hukuki Sebepler",
          text:
            "Kişisel veriler; sözleşmenin kurulması veya ifası, hukuki yükümlülüklerin yerine getirilmesi, meşru menfaat ve gerekli hallerde açık rıza hukuki sebeplerine dayanılarak işlenir."
        },
        {
          title: "5. Aktarım",
          text:
            "Kişisel veriler; kimlik doğrulama, bulut altyapısı, ödeme, destek ve isteğe bağlı entegrasyon sağlayıcılarına hizmetin gerektirdiği ölçüde aktarılabilir. Yurt dışı aktarım süreçleri ilgili mevzuata uygun şekilde yürütülür."
        },
        {
          title: "6. İlgili Kişi Hakları",
          text:
            "İlgili kişiler, KVKK'nın 11. maddesi kapsamındaki haklarını kullanmak için ileride ilan edilecek resmi iletişim kanalları üzerinden başvuru yapabilir."
        }
      ]
    }
  },
  en: {
    terms: {
      linkLabel: "Terms of Use",
      title: "Systemcel User Agreement and Terms of Use",
      updatedAt: "May 24, 2026",
      updatedAtLabel: "Last updated",
      closeLabel: "Close legal text",
      intro:
        "This text sets out the basic terms of use between Systemcel and users who access the Systemcel web application.",
      note:
        "These texts are drafts. Before production use, company information and legal clauses should be reviewed by legal counsel.",
      sections: [
        {
          title: "1. Scope of Service",
          text:
            "Systemcel is financial management software that provides income, expense, account, inventory, invoice, collection/payment, reporting and supported integration modules in a single workspace."
        },
        {
          title: "2. Account Creation and Security",
          text:
            "The user is responsible for providing accurate and up-to-date information during account creation. The user is also responsible for keeping account credentials confidential and preventing unauthorized access."
        },
        {
          title: "3. Data and Content Responsibility",
          text:
            "The accuracy of financial records, documents, business information and integration data entered into the application is the responsibility of the user. Systemcel processes this data to provide, operate securely and support the service."
        },
        {
          title: "4. Prohibited Use",
          text:
            "The service may not be used for unlawful transactions, infringement of third-party rights, attempts to bypass security controls, unauthorized access or disruption of service continuity."
        },
        {
          title: "5. Fees and Subscriptions",
          text:
            "Paid plans, trial periods, usage limits and payment terms are determined by the selected package. Plan changes, cancellation and renewal terms are shown separately in the relevant subscription screens."
        },
        {
          title: "6. Limitation of Liability",
          text:
            "Systemcel aims to provide the service continuously and securely with reasonable technical measures. However, the user is responsible for the accuracy of entered data, accounting compliance and the consequences of business decisions."
        }
      ]
    },
    privacy: {
      linkLabel: "Privacy Policy",
      title: "Systemcel Privacy Policy",
      updatedAt: "May 24, 2026",
      updatedAtLabel: "Last updated",
      closeLabel: "Close legal text",
      intro:
        "This policy informs users about personal data processed in the Systemcel web application and Systemcel's privacy approach.",
      note:
        "These texts are drafts. Before production use, company information and legal clauses should be reviewed by legal counsel.",
      sections: [
        {
          title: "1. Data Categories",
          text:
            "Account information, contact information, business information, financial records, invoice and account transaction data, usage logs, device information and integration settings may be processed as part of the service."
        },
        {
          title: "2. Purposes of Use",
          text:
            "Data is used to create accounts, verify identity, operate the service, manage subscription rights, perform security checks, respond to support requests and comply with legal obligations."
        },
        {
          title: "3. Third-Party Services",
          text:
            "Limited data may be shared for identity verification, cloud infrastructure, payment, support and user-enabled integration services. Such sharing is limited to what is necessary for the service."
        },
        {
          title: "4. Security Measures",
          text:
            "Systemcel aims to protect data with reasonable technical and administrative measures such as access control, secure connections, encryption, logging and operational security practices."
        },
        {
          title: "5. Retention and Deletion",
          text:
            "Data is retained for as long as required to provide the service and to meet applicable retention obligations. Data whose retention period has expired is deleted, destroyed or anonymized."
        }
      ]
    },
    kvkk: {
      linkLabel: "KVKK Notice",
      title: "Notice on the Processing of Personal Data",
      updatedAt: "May 24, 2026",
      updatedAtLabel: "Last updated",
      closeLabel: "Close legal text",
      intro:
        "This notice provides basic information about the processing of personal data under Turkish Personal Data Protection Law No. 6698.",
      note:
        "These texts are drafts. Before production use, company information and legal clauses should be reviewed by legal counsel.",
      sections: [
        {
          title: "1. Data Controller",
          text:
            "The title, address and contact details of the data controller providing the Systemcel service will be added to this text before production use once company details are finalized."
        },
        {
          title: "2. Personal Data Processed",
          text:
            "Identity, contact, customer transaction, financial transaction, transaction security, subscription, support request and integration setting data may be processed."
        },
        {
          title: "3. Purposes of Processing",
          text:
            "Personal data is processed for membership creation, service delivery, management of business records, subscription and payment processes, security, support, legal obligations and improvement of user experience."
        },
        {
          title: "4. Legal Grounds",
          text:
            "Personal data is processed based on legal grounds such as establishment or performance of a contract, compliance with legal obligations, legitimate interests and, where required, explicit consent."
        },
        {
          title: "5. Transfer",
          text:
            "Personal data may be transferred to identity verification, cloud infrastructure, payment, support and optional integration providers to the extent required by the service. Cross-border transfer processes are carried out in accordance with applicable law."
        },
        {
          title: "6. Data Subject Rights",
          text:
            "Data subjects may exercise their rights under Article 11 of the KVKK through official contact channels to be announced later."
        }
      ]
    }
  }
};
