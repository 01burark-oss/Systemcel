# ADR-001 — Ödeme sağlayıcısı seçimi

- **Tarih:** 22 Temmuz 2026
- **Son güncelleme:** 1 Ağustos 2026
- **Durum:** PayTR seçildi; sağlayıcıdan bağımsız hazırlık ve sahte sağlayıcı testleri şirket kuruluşundan önce, gerçek PayTR doğrulaması üye işyeri hesabından sonra yapılacak.
- **Karar sahibi:** Systemcel

## Bağlam

Systemcel, işletme ve muhasebeci planları için aylık/yıllık tahsilat, yenileme,
iptal, iade ve ödeme geçmişi akışlarına ihtiyaç duyacak. Fiyatlar yalnızca
sunucudaki `SubscriptionPlanCatalog` kaynağından hesaplanmalı; istemcinin
gönderdiği tutar kabul edilmemeli.

Şirket kuruluşu ve PayTR üye işyeri hesabı henüz tamamlanmadığından, canlı kart
tahsilatı ve gerçek webhook anahtarları bu aşamada eklenmeyecek.

## Karar

Ödeme sağlayıcısı olarak **PayTR** kullanılacak.

Entegrasyon aşağıdaki sınırlarla geliştirilecek:

- Sağlayıcı ayrıntıları `IPaymentProvider` arkasında tutulacak; uygulama
  servisleri doğrudan PayTR istemcisine bağımlı olmayacak.
- Checkout isteğinde gelen `plan`, `audience` ve `billing` değerleri sunucuda
  doğrulanacak; fiyat, para birimi ve dönem katalogdan tekrar çözülecek.
- Ödeme sonucu yalnızca imzası doğrulanmış webhook ile kesinleşecek.
- Webhook olay kimliği tekil tutulacak; tekrar gelen bildirim ikinci tahsilat
  veya ikinci abonelik oluşturmayacak.
- PayTR mağaza anahtarları, secret anahtarı ve callback imza bilgileri yalnızca
  ortam değişkenlerinde saklanacak; repoya, istemci bundle'ına veya loglara
  yazılmayacak.

## Kesinleşen ürün ve tahsilat kuralları

- İşletmeler için ücretli planlarda **30 günlük kartlı deneme** uygulanır.
- Muhasebeciler için ücretli planlarda **14 günlük kartlı deneme** uygulanır.
- Muhasebeciler için kalıcı ücretsiz plan sunulmaz. Mevcut ücretsiz kayıtlar veri kaybetmeden kısıtlı geçiş durumuna alınır.
- Ücretli planların aylık/yıllık fiyatı mevcut plan kataloğundan gelir.
- İlk kartlı deneme ve checkout yalnızca aylık faturalamayla başlatılır. Kullanıcı aboneliği başladıktan sonra yıllık döneme geçebilir.
- Deneme başlangıcında karttan ücret çekilmez. Deneme bitiş tarihi, ilk çekilecek aylık plan tutarı, KDV ve iptal yolu ayrı onay metninde gösterilir.
- Muhasebeci Standart plana 10 müşteri dahildir. 11. müşteri ve sonrası için muhasebecinin açıkça satın aldığı her `+1 müşteri kredisi` kapasiteyi bir artırır; kredi ana abonelikle birlikte yinelenir ve aylıkta kredi başına KDV hariç 50 TL'dir.
- Yıllık plana sonradan geçildiğinde müşteri kredileri de Standart planla aynı %16 yıllık avantajla kredi başına KDV hariç 504 TL/yıl olarak yinelenir.
- Deneme bitmeden 7 ve 3 gün önce uygulama içi ve e-posta hatırlatması gönderilir.
- Deneme sırasında iptal edilen hesap deneme sonuna kadar kullanılır; deneme sonunda çekim yapılmaz.
- Aylık plandan daha yüksek bir plana geçiş anında uygulanır. Kullanılmayan dönem bedeli gün bazında kredi olarak düşülür, yeni planın kalan dönem farkı tahsil edilir.
- Yıllık plandan daha yüksek bir yıllık plana geçiş anında uygulanır ve aynı gün bazlı mahsup kuralı kullanılır.
- Aylıktan yıllığa geçiş anında uygulanır; kullanılmayan aylık bedel yıllık toplamdan kredi olarak düşülür.
- Daha düşük plana veya yıllıktan aylığa geçiş mevcut dönemin sonunda uygulanır. Dönem ortasında nakit iade yapılmaz.
- İptal dönem sonunda etkili olur. Kullanıcı ödenmiş/deneme döneminin sonuna kadar erişimini korur; sonraki yenileme yapılmaz.
- Başarısız tahsilatta 7 günlük tolerans süresi uygulanır. Otomatik denemeler 1, 3 ve 5. günlerde yapılır; başarı olmazsa hesap veri silinmeden kısıtlı moda alınır.
- Plan fiyatları katalogda KDV hariç tutulur. Checkout ve onay metni KDV tutarını ve tahsil edilecek toplamı ayrı gösterir.
- Canlı ödeme, PayTR sandbox ve üye işyeri erişimleri hazır olmadan açılmaz.

## Şirket ve sağlayıcı onayı bekleyen sınırlar

- İade/cayma metninin nihai hukuk dili ile satış belgesi ve e-Arşiv sorumlulukları.
- Şirket unvanı, vergi bilgileri, resmî iletişim kanalları ve sözleşme tarafı bilgileri.
- PayTR'nin tekrarlayan ödeme/abonelik ürününde mağazaya tanımladığı kesin API ve webhook alanları.
- Dönem içinde müşteri kredisi azaltılırken aktif müşteri sayısının yeni kapasiteyi aşması halinde uygulanacak kullanıcı deneyimi ve sağlayıcı mahsup ayrıntısı.

## Sonuçlar

P0.3 kapsamında PayTR için checkout, callback/webhook doğrulama, idempotent
ödeme olayı kaydı ve abonelik yaşam döngüsü eklenecek. Şirket kuruluşu, PayTR
başvurusu ve sandbox/canlı erişim bilgileri hazır olmadan gerçek tahsilat
entegrasyonunun tamamlandığı iddia edilmeyecek.
