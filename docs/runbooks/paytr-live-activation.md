# PayTR ilk abonelik ödemesini canlıya açma

Bu kılavuz tek seferlik iFrame abonelik ödemesi içindir. Kayıtlı kart, otomatik yenileme ve pazaryeri tahsilat/aktarım yetkileri ayrı kabul gerektirir. Mağazanın canlı moda alınması tek başına Systemcel'de ödeme açmaz.

## Açılıştan önce tamamlanacak kapılar

1. İletişim, abonelik, teslimat, satış ve iptal/iade koşullarını canlı sitede doğrula. Aylık kurucu fiyatı ilk üç aylık dönem; yıllık peşin fiyat satın alınan 12 ayın tamamı içindir. Tek seferlik kart ödemesi kart saklama veya otomatik yenileme başlatmaz.
2. PayTR panelindeki Canlı Mod sürecini tamamla; yalnız fiilen tamamlanan entegrasyon/test/bildirim adımlarını onayla. Kimlik veya yeni sözleşme adımı kullanıcıya aittir. İlk iFrame ödemesinin ayrı değerlendirilmesine ilişkin mağaza yanıtını kaydet. Non3D risk kabulünü bu başvuruya dahil etme.
3. Test iade çelişkisini yazılı olarak çöz: 100 TL talebin API kabulü varken sorgu `returns=[]` ve panelde test işlemlerinin iade edilemeyeceği açıklaması var. Yeni talimat gönderme veya kaydı elle tamamlandı yapma. Testte iade kaydı oluşamıyorsa gerçek kabul yöntemi PayTR tarafından açıklanmalı.
4. Canlı iade yanıt sözleşmesini PayTR'dan yazılı doğrula. Adaptör iki ayrı biçimi destekler: `zero` yalnız sayı/metin `is_test=0`, `absent` yalnız alanın hiç bulunmaması. Sunucuda doğrulanmış biçim seçilmeden canlı iade gönderimi ve yeni canlı checkout kapalıdır; başlangıç değeri `unconfirmed`. Bunlar alternatif sözleşmelerdir, otomatik biçim tahmini yapılmaz. API kabulü iade tamamlandı anlamına gelmez; canlı durum sorgusunda aynı referans/tutarın görünmesi gerekir.
5. Denetlenebilir bir pilot işletme ve ödeme/iade sorumlusu belirle. Gerçek kart ödemesinin ve gerçek iadenin son onayını kullanıcı gerçekleştirir. Tutar ve sipariş önceden gösterilir.

## Üretim hazırlığı

- Test hostu `paytr-test.systemcel.app` ve onun veritabanı üretimden ayrıdır. Üretim `SYSTEMCEL_PUBLIC_BASE_URL=https://systemcel.app` kullanır. Test hostundan alınan başarılı bildirim üretim ödemesini doğrulamaz.
- Yayın CI'sı, dağıtım SHA'sı, public smoke ve güncel yedek doğrulanır. Açılış adayı için tam Playwright, web kontrolleri, Release .NET ve operasyon smoke kontrolleri tamamlanır.
- Merchant ID/Key/Salt yalnız sunucudaki mevcut güvenli sır yönetimi üzerinden hazırlanır. Sohbete, Git'e, loglara veya ekran kanıtına yazılmaz. Test sunucusunun sır dosyası üretim dosyasının üzerine kopyalanmaz.
- Üretimin güvenilir proxy IP'lerini ağ yapılandırmasından doğrula; istemci IP'sini keyfi header'dan kabul etme.
- Mağaza bildirim adresi üretime taşınacaksa mevcut test adresini kaydet; açık test siparişleri ve üretim geçişi için PayTR'ın bildirim yönlendirmesini teyit et. Üretim hedefi `https://systemcel.app/api/odeme/paytr/bildirim` olmalıdır. Form/dönüş/health yönlendirmeleri de gerçek hostta erişilebilir olmalı.

## Kapalı yapılandırma ve pilot geçişi

Hazırlık sırasında `SYSTEMCEL_PAYMENT_PROVIDER=Unconfigured` ve `SYSTEMCEL_PAYTR_LIVE_ENABLED=false` korunur. Mağaza onayı ve yukarıdaki kapılar tamamlandığında yalnız pilot kabul için:

```dotenv
SYSTEMCEL_PAYMENT_PROVIDER=PayTR
SYSTEMCEL_PUBLIC_BASE_URL=https://systemcel.app
SYSTEMCEL_PAYTR_TEST_MODE=false
SYSTEMCEL_PAYTR_LIVE_ENABLED=true
SYSTEMCEL_PAYTR_LIVE_BUSINESS_IDS=<onaylı pozitif pilot işletme kimlikleri>
SYSTEMCEL_PAYTR_LIVE_CHECKOUT_PAUSED=false
SYSTEMCEL_PAYTR_LIVE_REFUND_RESPONSE_MODE=<PayTR yazılı yanıtına göre zero veya absent>
```

Bu örnek doğrudan çalıştırılacak dosya değildir. Gerçek kimlikler, sırlar ve proxy ayarları güvenli sunucu yapılandırmasına girilir. Boş/geçersiz canlı izin listesi, eksik anahtarlar, güvensiz URL veya kapalı canlı anahtarı sağlayıcıyı devre dışı bırakır. Yanıt sözleşmesi doğrulanmamışsa yeni checkout ve iade gönderimi kapalı kalır; mevcut bildirim/sorgu sağlayıcısı korunur. Test izin listesi canlı erişim vermez. Tüm müşterileri açan joker değer yoktur. Oracle Compose `.env` dosyasını doğrudan uygulamaya iletir; örnek ayar yeni seçeneği kapalı tutar.

Yönetici iade ekranı canlı/test modunu sunucudan alır. Canlı gönderimde işletme ve gerçek tutar gösterilir; ayrı teyit `canliIadeOnayi=true` ile iletilir. Sunucu eski, teyitsiz ekran isteklerini reddeder. Pilot listesi onay, gönderim ve sorguda ayrıca denetlenir. Belirsiz sonuçta gönder düğmesi yeniden açılmaz.

## İlk satış belgesi

İlk pilotta Systemcel'in kendi yazılım satışı için belge, mali müşavirin belirlediği mevcut e-belge sisteminde düzenlenir. Otomatik e-Fatura entegrasyonu varmış gibi belge numarası üretilmez. Ödeme başarı bildirimi kesinleşince sorumlu şu alanları kontrollü kayda geçirir: işletme/müşteri, fatura bilgileri, ödeme/sipariş referansı, satış tarihi, aylık/yıllık dönem, net/KDV/toplam, belge sistemi ve belge numarası, düzenleyen kişi/tarih. İade belgesi ve ilk satış bağlantısı da aynı kayda eklenir. Sistem ve belge yöntemi belirlenmeden bu adım tamamlanmış sayılmaz.

Kabul kaydı şablonu: [paytr-subscription-acceptance.md](paytr-subscription-acceptance.md).

## Kontrollü kabul

1. Pilot dışı işletmenin teklif/checkout isteğinin reddedildiğini doğrula. Pilot için `test_mode=0` ve doğru toplamı kontrol et; otomatik yenileme veya kart saklama sunma.
2. Kullanıcı kontrollü gerçek ödemeyi tamamlar. Başarı yalnız imzalı bildirimin kalıcı kaydından sonra uygulanır; tarayıcı dönüşü ödeme kanıtı değildir.
3. Sipariş/tutar/para birimi ve canlı mod sağlayıcı sorgusuyla eşleşir. Bildirim tekrarı ikinci abonelik/olay üretmez. Kesin başarısız ödeme planı değiştirmez; belirsiz sonuç yeni çekimle tekrar denenmez.
4. Gerçek iade, yetkili inceleme ve kullanıcı son onayıyla tek referanstan gönderilir. API kabulü iade tamamlandı sayılmaz; sorgudaki aynı referans ve tutar eşleşmesi gerekir. Önce kısmi, sonra kalan tutar kabulü; yeni aboneliğin yanlışlıkla kapatılmaması doğrulanır.
5. Tüm kanıtlar ve sorumlu onayı kaydedildikten sonra izin listesi onaylı müşterilerle genişletilir. Pazaryeri/otomatik yenileme kapıları bu kabulden bağımsız açık kalır.

## Geri dönüş

Yeni ödeme başlatmayı durdurmak için `SYSTEMCEL_PAYTR_LIVE_CHECKOUT_PAUSED=true` uygulanır ve uygulama yapılandırması yeniden yüklenir. Teklif/checkout isteği reddedilir; aynı anahtarlar, canlı izin listesi, `PayTR` sağlayıcısı ve canlı açma anahtarı korunarak başlamış siparişlerin bildirim/sorguları işlenir. Daha önce düzenlenen ödeme formu PayTR'da hâlâ tamamlanabilir; duraklatma mevcut siparişi iptal etmez. Açık sipariş varken sağlayıcıyı `Unconfigured` yapmak, canlı anahtarını kapatmak veya eski sürüme dönmek bildirim işlemesini de durdurabileceğinden bunlar ancak açık işlemler uzlaştırıldıktan sonra yapılır. PayTR'ın yeniden bildirim davranışı ve açık talimatlar korunur; veri silinmez ve belirsiz ödeme/iade tekrar gönderilmez.
