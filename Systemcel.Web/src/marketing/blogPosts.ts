export type BlogLanguage = "tr" | "en";

type BlogCopy = {
  title: string;
  intro: string;
  sections: { title: string; paragraphs: string[] }[];
  source?: { label: string; href: string };
};

export type BlogPost = {
  slug: string;
  tr: BlogCopy;
  en: BlogCopy;
};

export const blogPosts: BlogPost[] = [
  {
    slug: "on-muhasebede-tek-veri-kaynagi",
    tr: {
      title: "Ön muhasebede tek veri kaynağı neden önemli?",
      intro: "Aynı satışın fatura, cari hesap, stok ve banka kayıtlarında farklı görünmesi küçük bir işletmede bile kararları zorlaştırır. Önce hangi kaydın doğru olduğunu bulmak gerekir.",
      sections: [
        {
          title: "Bir satış, birden fazla iz bırakır",
          paragraphs: [
            "Müşteriye yapılan satış yalnızca gelir satırı değildir. Ürün stoktan çıkar, müşterinin cari bakiyesi değişir, fatura düzenlenir ve ödeme daha sonra bankaya düşebilir. Bu olayları ayrı tablolarda, birbirinden kopuk biçimde izlerseniz aynı işlemi birkaç kez girersiniz. Bir yerde düzeltme yaptığınızda diğer kayıtların eski kalması da kolaydır.",
            "Örneğin satış faturası kesildiği hâlde ödeme henüz gelmemişse, gelir ile tahsilatı aynı şey gibi görmek nakit durumunu olduğundan iyi gösterir. Tersine, bankaya gelen tutarın hangi faturaya ait olduğu belirsizse müşterinin açık bakiyesi yanlış kalabilir. Her kaydın kaynağını ve bağlantısını bilmek, rakamların neden farklı olduğunu açıklamayı kolaylaştırır."
          ]
        },
        {
          title: "Günlük kontrolde neye bakmalı?",
          paragraphs: [
            "Önce satış, fatura ve tahsilatın aynı işlemle ilişkilendirildiğinden emin olun. Ardından stok hareketinin gerçekten teslim edilen miktarı gösterip göstermediğini kontrol edin. İade veya kısmi ödeme varsa önceki kaydı sessizce değiştirmek yerine yeni hareketi açıkça kaydedin. Böylece geçmişte ne olduğu izlenebilir.",
            "Haftalık olarak açık cari bakiyeleri banka hareketleriyle, stok miktarını fiziksel sayımla karşılaştırın. Uyuşmazlık çıktığında yalnız toplamı düzeltmek yerine farkı yaratan belgeyi bulun. Tek veri kaynağının değeri, bütün sayıların aynı ekranda durmasından çok, aralarındaki ilişkinin korunmasıdır."
          ]
        },
        {
          title: "Muhasebeciyle aynı kayda bakmak",
          paragraphs: [
            "Muhasebeci ay sonunda başka bir Excel dosyası, işletme başka bir liste kullanıyorsa aynı faturanın iki ayrı açıklaması oluşabilir. İlgili belgeyi, işlem tarihini ve ödeme durumunu birlikte görmek soruları azaltır. Yine de ön muhasebe kayıtları ile yasal muhasebe kayıtlarının aynı şey olmadığını unutmayın; dönem sonu sınıflandırma ve beyan kararları için mali müşavirinizle çalışın."
          ]
        }
      ]
    },
    en: {
      title: "Why one source of truth matters in bookkeeping",
      intro: "When the same sale looks different in invoices, customer accounts, inventory, and bank records, even a small business must first work out which number is right.",
      sections: [
        { title: "One sale leaves several records", paragraphs: [
          "A sale is more than a revenue entry. An item leaves inventory, the customer's balance changes, an invoice is issued, and payment may reach the bank later. Tracking these events in disconnected sheets means entering the same transaction repeatedly and risking a missed update when one entry changes.",
          "An issued invoice is not the same as collected cash. If an incoming bank payment cannot be matched to its invoice, the customer's open balance may also be wrong. Links between records make differences easier to explain."
        ] },
        { title: "What to check regularly", paragraphs: [
          "Check that the sale, invoice, and payment refer to the same transaction. Confirm that inventory reflects the quantity actually delivered. Record a return or partial payment as a visible new event instead of quietly overwriting history.",
          "Compare open customer balances with bank movements and stock figures with physical counts each week. When totals differ, find the document behind the difference. A shared source of truth is useful because those relationships survive, not simply because numbers sit on one screen."
        ] },
        { title: "Working with your accountant", paragraphs: [
          "Sharing the document, transaction date, and payment status reduces the confusion caused by separate spreadsheets. Bookkeeping records are still distinct from statutory accounts; work with your accountant on period-end classification and filing decisions."
        ] }
      ]
    }
  },
  {
    slug: "e-arsiv-fatura-akisi",
    tr: {
      title: "e-Arşiv fatura akışını düzenlemek",
      intro: "Fatura hazırlığı ile resmî düzenleme aynı adım değildir. Alıcı bilgisini, tutarı ve onay durumunu ayrı ayrı izlemek hataları azaltır.",
      sections: [
        { title: "Taslağı hazırlayın, bilgileri kontrol edin", paragraphs: [
          "Fatura taslağında alıcının kimlik veya vergi bilgileri, mal ya da hizmet kalemleri, miktar, fiyat ve vergi bilgileri kontrol edilmelidir. Özellikle aynı isimli müşterilerde yanlış cari hesabın seçilmesi, sonradan düzeltmesi zor bir belgeye yol açabilir. Taslağı resmî belgeyle karıştırmamak için durumunu açıkça gösterin.",
          "Systemcel'deki müşteri teyidi, alıcının taslaktaki bilgileri gözden geçirmesine yarar. Bu teyit resmî e-Arşiv fatura onayı değildir ve tek başına fatura kesmez. Teyit geldikten sonra belgeyi düzenleme sorumluluğu işletmede kalır."
        ] },
        { title: "Resmî kesim adımını ayrı izleyin", paragraphs: [
          "GİB Portal bağlantısıyla çalışan akışta taslak portala gönderilir; gerekiyorsa portalda kayıtlı işletme telefonuna gelen SMS koduyla işlem tamamlanır. Kod istenmiş olması veya müşterinin taslağı onaylaması, belgenin düzenlendiği anlamına gelmez. İşlem sonucunu ve belge durumunu ayrıca kontrol edin.",
          "Bir hata dönerse aynı faturayı körlemesine yeniden göndermeyin. Önce GİB tarafındaki sonucu ve uygulamadaki işlem kaydını karşılaştırın. Çift belge riski özellikle ağ bağlantısı kesildiğinde ya da yanıt geciktiğinde ortaya çıkar."
        ] },
        { title: "Belgeyi ödeme ve cari hesapla eşleştirin", paragraphs: [
          "Kesilen belgeyi ilgili satış ve müşteri bakiyesiyle ilişkilendirin. Ödeme daha sonra geldiyse tahsilatı ayrı tarihle kaydedin. İptal veya iade ihtiyacında mevcut belgeyi silerek geçmişi değiştirmeyin; uygulanacak resmî yöntemi mali müşavirinizle teyit edin. Güncel kurallar ve teknik ayrıntılar için GİB'in e-Belge duyurularını izleyin."
        ] }
      ],
      source: { label: "GİB e-Belge portalı", href: "https://ebelge.gib.gov.tr/" }
    },
    en: {
      title: "Organizing the e-Archive invoice flow",
      intro: "Preparing an invoice and officially issuing it are separate steps. Track buyer details, amounts, and approval status independently.",
      sections: [
        { title: "Prepare the draft and check its details", paragraphs: [
          "Check the buyer's identity or tax details, the goods or services, quantity, price, and tax information before issuing an invoice. Choosing the wrong customer account can be hard to correct later. Keep the draft status distinct from the official document status.",
          "Systemcel's customer confirmation lets the buyer review draft details. It is not official e-Archive approval and does not issue an invoice by itself. The business remains responsible for the formal issue step."
        ] },
        { title: "Track the official issue separately", paragraphs: [
          "With the GİB Portal connection, the draft goes to the portal; when required, the process is completed with an SMS code sent to the business phone registered there. Requesting a code or receiving customer confirmation does not prove that the invoice was issued. Check the final result and document status.",
          "If a request fails, inspect the portal result and the application's transaction record before retrying. An interrupted connection or delayed response could otherwise create a duplicate document."
        ] },
        { title: "Link the document to payment", paragraphs: [
          "Connect the issued document to its sale and customer balance. Record a later payment on its actual date. For cancellation or returns, confirm the proper official procedure with your accountant instead of deleting the history. Follow GİB's e-Document updates for current rules."
        ] }
      ],
      source: { label: "GİB e-Document portal", href: "https://ebelge.gib.gov.tr/" }
    }
  },
  {
    slug: "muhasebeciyle-dijital-calisma",
    tr: {
      title: "Muhasebeciyle dijital çalışma alanı",
      intro: "“Şu faturayı tekrar gönderir misiniz?” sorusu her ay yeniden soruluyorsa sorun çoğu zaman belgenin kendisinde değil, hangi işlemle ilgili olduğunun kaybolmasındadır.",
      sections: [
        { title: "Talebi belgenin yanında tutun", paragraphs: [
          "E-posta zincirinde dosya adı, tarih ve işlem açıklaması birbirinden kopabilir. Muhasebecinin istediği veriyi ilgili dönem ve kayıtla birlikte görmek, yanlış dosyayı gönderme ihtimalini azaltır. İşletme de talebin kimden geldiğini ve hangi bilgiye yanıt verdiğini takip edebilir.",
          "Örneğin banka hareketiyle eşleşmeyen bir tahsilat için yalnız dekontu iletmek yetmeyebilir. Hangi müşteri, hangi fatura ve hangi tutar olduğunu aynı görüşmede belirtmek, sorunun bir turda çözülmesini kolaylaştırır."
        ] },
        { title: "Erişimi kişiye göre verin", paragraphs: [
          "Muhasebeciyle çalışmak için ortak şifre kullanmak gerekmez. Kişiye özel davet ve uygun yetki, erişimin kimde olduğunu görmeyi ve iş ilişkisi bittiğinde kaldırmayı mümkün kılar. Finansal belgeleri sohbetlere veya kişisel e-posta kutularına dağınık biçimde kopyalamamak da günlük veri yönetimini kolaylaştırır."
        ] },
        { title: "Ay sonunda küçük bir kontrol listesi", paragraphs: [
          "Dönem kapanmadan önce eksik alış ve satış faturalarını, açıklamasız banka hareketlerini, açık cari bakiyeleri ve stok farklarını gözden geçirin. Muhasebecinizin istediği belgeleri aynı dönem altında toplayın; karar gerektiren konuları mesajla netleştirin. Bir çalışma alanı görüşmeyi düzenler, ancak mali müşavirin uzman değerlendirmesinin yerini almaz."
        ] }
      ]
    },
    en: {
      title: "A digital workspace with your accountant",
      intro: "If someone asks for the same invoice every month, the problem may be the missing link between the document and the transaction it explains.",
      sections: [
        { title: "Keep a request with its record", paragraphs: [
          "File names, dates, and transaction notes can get separated in email threads. Keeping a data request near its period and record reduces the chance of sending the wrong file. The business can also see who asked and which information answered the question.",
          "For an unmatched bank payment, a receipt alone may not be enough. Mentioning the customer, invoice, and amount in the same discussion helps resolve the issue sooner."
        ] },
        { title: "Give each person their own access", paragraphs: [
          "Working with an accountant should not require a shared password. A personal invitation and appropriate access make it possible to see who can enter and to remove access when the engagement ends. Avoiding scattered copies of financial documents also makes daily data management easier."
        ] },
        { title: "A short month-end review", paragraphs: [
          "Before closing a period, review missing purchase and sales invoices, unexplained bank movements, open customer balances, and stock differences. Gather the documents your accountant requests under the same period, and discuss decisions in context. A workspace organizes the conversation; it does not replace professional accounting judgment."
        ] }
      ]
    }
  }
];

export function findBlogPost(slug: string) {
  return blogPosts.find((post) => post.slug === slug);
}
