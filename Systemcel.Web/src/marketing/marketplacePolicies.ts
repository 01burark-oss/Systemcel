import type { AuthLanguage, LegalTextContent } from "../auth/legalTexts";

export type MarketplacePolicyKey = "marketplaceSale" | "marketplaceDelivery" | "marketplaceReturns";

export const marketplacePolicies: Record<AuthLanguage, Record<MarketplacePolicyKey, LegalTextContent>> = {
  tr: {
    marketplaceSale: {
      linkLabel: "Pazaryeri satış koşulları",
      title: "Pazaryeri satış koşulları",
      updatedAt: "25 Eylül 2026",
      updatedAtLabel: "Son güncelleme",
      intro: "Systemcel pazaryerinde farklı tedarikçilerden verilen siparişlere ilişkin temel bilgiler.",
      closeLabel: "Kapat",
      note: "Siparişe özgü ürün, satıcı, miktar, vergi ve toplam tutar bilgileri sipariş ekranında gösterilir. Kanundan doğan haklarınız saklıdır.",
      sections: [
        { title: "Pazaryeri ve satıcı", text: "Systemcel, tedarikçilerle alıcıları bir araya getiren pazaryeri platformudur. Ürünün satıcısı, ürün kartında ve sipariş kaydında adı gösterilen tedarikçidir. Farklı tedarikçilerin ürünleri tek sepetten alınabilir; siparişler tedarikçi bazında ayrı izlenir." },
        { title: "Ürün ve fiyat", text: "Ürün açıklaması, birimi, fiyatı ve KDV oranı ürün kaydında gösterilir. Sipariş ekranında miktar ve toplam tutarı kontrol edin. Stok, fiyat ve ürün koşulları sipariş verilene kadar değişebilir. Siparişe eklenebilecek teslimat veya başka bir bedel varsa, ödeme onayından önce ayrıca gösterilmelidir." },
        { title: "Sipariş ve ödeme", text: "Sipariş vermek için Systemcel işletme hesabına giriş gerekir. Siparişin durumu uygulamadaki pazaryeri alanından takip edilir. Ödeme yöntemi ve varsa ödeme hizmeti sağlayıcısı, ödeme adımında gösterilir. Platformda henüz sunulmayan bir ödeme yöntemine ilişkin taahhütte bulunulmaz." },
        { title: "İletişim", text: "Platform işletmecisi Burak Özmen (şahıs işletmesi), Küçükyalı Vergi Dairesi, VKN 7020714272. Adres: Bağlarbaşı Mahallesi, Hür Sokak No: 2, İç Kapı No: 9, Maltepe/İstanbul. Telefon: 0530 065 58 88. E-posta: destek@systemcel.app. Ürüne ve sevkiyata ilişkin sorularınız için siparişte adı geçen tedarikçiye de başvurabilirsiniz." }
      ]
    },
    marketplaceDelivery: {
      linkLabel: "Teslimat ve kargo koşulları",
      title: "Teslimat ve kargo koşulları",
      updatedAt: "25 Eylül 2026",
      updatedAtLabel: "Son güncelleme",
      intro: "Pazaryeri siparişlerinde sevkiyat tedarikçi bazında yürütülür.",
      closeLabel: "Kapat",
      note: "Tahmini teslim süresi kesin teslim tarihi değildir. Siparişe özgü sevkiyat bilgileri için ürün ve sipariş ekranını esas alın.",
      sections: [
        { title: "Sevkiyat", text: "Her tedarikçi kendi ürünlerinin sevkiyatını yürütür. Aynı sepetteki ürünler farklı zamanlarda ve farklı taşıyıcılarla teslim edilebilir. Tedarikçi sevkiyat bölgesi veya tahmini teslim süresi bildirmişse bunlar ürün kartında gösterilir. Güncel sevkiyat durumunu sipariş ekranından takip edebilirsiniz." },
        { title: "Teslimat adresi ve takip", text: "Siparişten önce teslimat adresinizi kontrol edin. Tedarikçi kargo veya sevkiyat bilgisi paylaştığında, sipariş kaydında görünür. Teslimat bedeli uygulanıyorsa tutarı ödeme onayından önce ayrıca gösterilmelidir." },
        { title: "Eksik veya hasarlı teslimat", text: "Teslim aldığınız ürünlerin miktarını ve durumunu kontrol edin. Eksik, hasarlı ya da yanlış ürün için uygulamadaki mal kabul ve itiraz akışını kullanın veya destek@systemcel.app adresine sipariş numaranızla yazın. Teslimat bildirimi tek başına alıcının mal kabulü sayılmaz." }
      ]
    },
    marketplaceReturns: {
      linkLabel: "İptal ve iade koşulları",
      title: "İptal ve iade koşulları",
      updatedAt: "25 Eylül 2026",
      updatedAtLabel: "Son güncelleme",
      intro: "Sipariş iptali, teslimat itirazı ve iade taleplerinin nasıl ele alındığı.",
      closeLabel: "Kapat",
      note: "İşletmeler arası alımlarda tüketiciye özgü cayma hakkı her siparişe otomatik olarak uygulanmaz. Emredici yasal haklar ve siparişe özgü satıcı koşulları saklıdır.",
      sections: [
        { title: "Sipariş iptali", text: "Sevkiyat başlamadan önce uygulamadaki sipariş ekranından iptal talebinde bulunabilirsiniz. Sevkiyat başladıktan sonra siparişin durumuna göre iade veya itiraz sürecini kullanın. Siparişin yalnız belirli kalemlerinde sorun varsa bunları ayrı ayrı bildirin." },
        { title: "İade ve itiraz", text: "Ürün size ulaştığında eksik, hasarlı veya siparişten farklı olan miktarları mal kabul ekranında belirtin. Diğer iade taleplerinizi sipariş ekranından veya destek@systemcel.app üzerinden iletin. Tedarikçi ürün için iade koşulu bildirmişse ürün kartında gösterilir; geçerli yasal haklarınızı sınırlamaz." },
        { title: "İnceleme ve geri ödeme", text: "Talep, sipariş ve teslimat kayıtlarıyla birlikte incelenir. Onaylanan iade tutarı ve ödeme yöntemi, ilgili siparişin ödeme akışına göre belirlenir. İade onaylanmadan kesin geri ödeme tarihi veya tutarı taahhüt edilmez. Sonucu sipariş kaydından takip edebilir, destek@systemcel.app adresinden bilgi isteyebilirsiniz." }
      ]
    }
  },
  en: {
    marketplaceSale: { linkLabel: "Marketplace sale terms", title: "Marketplace sale terms", updatedAt: "September 25, 2026", updatedAtLabel: "Last updated", intro: "Key information about ordering from different suppliers on Systemcel.", closeLabel: "Close", note: "Review the seller, product, quantity, tax and total shown for your order. Statutory rights remain unaffected.", sections: [
      { title: "Platform and seller", text: "Systemcel connects business buyers with suppliers. The seller is the supplier named on the product and order. Orders from multiple suppliers are tracked separately." },
      { title: "Products and prices", text: "Product details, unit price and VAT rate are shown on the listing. Review quantities and totals before ordering. Any delivery or other charge must be shown before payment confirmation." },
      { title: "Orders and payments", text: "A Systemcel business account is required to order. Track order status in the marketplace. Available payment methods are shown at checkout." },
      { title: "Contact", text: "Platform operator: Burak Özmen (sole proprietorship), Küçükyalı Tax Office, tax number 7020714272. Address: Bağlarbaşı Mahallesi, Hür Sokak No. 2, Apt. 9, Maltepe, Istanbul, Türkiye. Phone: +90 530 065 58 88. Email: destek@systemcel.app." }
    ] },
    marketplaceDelivery: { linkLabel: "Delivery and shipping", title: "Delivery and shipping", updatedAt: "September 25, 2026", updatedAtLabel: "Last updated", intro: "Suppliers handle shipment for their own orders.", closeLabel: "Close", note: "Estimated delivery time is not a guaranteed date. Check the listing and order for details.", sections: [
      { title: "Shipping", text: "Each supplier ships its own products. Items in one basket may arrive separately. Shipping regions and estimated delivery times are shown on the listing when supplied by the seller." },
      { title: "Address and tracking", text: "Check your delivery address before ordering. Tracking details appear in the order when the supplier provides them. Any delivery charge must be shown before payment confirmation." },
      { title: "Missing or damaged goods", text: "Check the quantity and condition on arrival. Report missing, damaged or incorrect goods through the receipt and dispute flow, or email destek@systemcel.app with your order number. A supplier's delivery notice alone does not constitute buyer acceptance." }
    ] },
    marketplaceReturns: { linkLabel: "Cancellation and returns", title: "Cancellation and returns", updatedAt: "September 25, 2026", updatedAtLabel: "Last updated", intro: "How cancellations, delivery disputes and returns are handled.", closeLabel: "Close", note: "Consumer withdrawal rights do not automatically apply to every business purchase. Mandatory legal rights remain unaffected.", sections: [
      { title: "Cancellation", text: "You may request cancellation from the order screen before shipment starts. After shipment, use the return or dispute flow according to the order status. Report issues with individual items separately." },
      { title: "Returns and disputes", text: "Report missing, damaged or incorrect quantities in the receipt flow. Send other return requests from the order screen or to destek@systemcel.app. Supplier return conditions, when provided, are shown on the listing and do not limit statutory rights." },
      { title: "Review and refund", text: "Requests are reviewed against order and delivery records. The amount and route of any approved refund depend on the order's payment flow. Follow the result in the order or contact support." }
    ] }
  }
};
