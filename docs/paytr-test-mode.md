# PayTR abonelik test kabulü

Bu dilim yalnız tek çekim abonelik checkout'unu ve PayTR iFrame sonuç bildirimini kapsar. Pazaryeri siparişleri, alt satıcı transferi, iade, kart saklama ve otomatik yenileme bu sağlayıcıya bağlı değildir. Tarayıcının başarılı dönüşü ödeme kaydı üretmez; yalnız doğrulanmış ve veritabanına işlenmiş bildirim bunu yapar.

## Kapalı varsayılan ve yapılandırma

`SYSTEMCEL_PAYMENT_PROVIDER` varsayılan olarak `Unconfigured` kalır. PayTR test sağlayıcısını seçmek için sunucunun gizli yapılandırmasında aşağıdakilerin **tamamı** gerekir:

| Değişken | Amaç |
|---|---|
| `SYSTEMCEL_PAYMENT_PROVIDER=PayTR` | Abonelik test checkout'u sağlayıcısını seçer. |
| `SYSTEMCEL_PAYTR_MERCHANT_ID` | Mağazaya özel sayısal kimlik. |
| `SYSTEMCEL_PAYTR_MERCHANT_KEY`, `SYSTEMCEL_PAYTR_MERCHANT_SALT` | İmza sırları; Git'e, sohbet kaydına veya istemciye yazılmaz. |
| `SYSTEMCEL_PUBLIC_BASE_URL` | PayTR'nin erişebildiği sabit HTTPS kök adresi. |
| `SYSTEMCEL_PAYTR_TEST_MODE=true` | Test dışındaki değerde sağlayıcı kapalı kalır. |
| `SYSTEMCEL_PAYTR_TEST_BUSINESS_IDS` | Virgülle ayrılmış, izinli dahili işletme kimlikleri; boşsa sağlayıcı kapalı kalır. |
| `SYSTEMCEL_PAYTR_TRUSTED_PROXY_IPS` | Gerekirse ters proxy'nin virgülle ayrılmış IP adresleri. Yalnız bu IP'lerden gelen `X-Forwarded-For` başlığına bakılır. |

İzinli olmayan işletmelerin abonelik teklif/checkout isteği reddedilir. PayTR seçildiğinde bu ilk dilimde ücretsiz deneme devre dışıdır. Canlı kart tahsilatı için bu test dilimini kullanmayın; `test_mode=false` sağlayıcıyı kapalı tutar.

## Test sırası

1. İzole, erişilebilir HTTPS test kurulumu ve izinli test işletmesini hazırla. `SYSTEMCEL_PUBLIC_BASE_URL` değerinin gerçek alan adıyla eşleştiğini ve istemci IP'sinin doğru aktarıldığını doğrula.
2. PayTR panelindeki Bildirim URL formunda protokolü `https://` seçip adres alanına `<test-alani>/api/odeme/paytr/bildirim` yaz; kaydedilen tam URL'nin `https://<test-alani>/api/odeme/paytr/bildirim` olduğunu doğrula. Bu adres oturum istemez; yalnız form POST kabul eder. Başarılı işlenmiş bildirimde gövde tam olarak `OK` olur.
3. Test işletmesiyle abonelik teklifini aç. Ad, adres ve telefon alanlarını doldur. Checkout'un ürettiği iFrame'de başarılı ve başarısız test işlemlerini ayrı sipariş kimlikleriyle çalıştır.
4. Sağlayıcının işlem durumunu uygulamadaki ödeme ve abonelik kayıtlarıyla karşılaştır. Yanlış imza, farklı tutar ve mükerrer bildirimde ikinci abonelik/para olayı oluşmadığını doğrula. Tarayıcı dönüşü callback'ten önce gelirse uygulama sonucu “kontrol ediliyor” göstermelidir.
5. PayTR mağaza panelindeki API uyarılarını ve işlem durumunu incele. Başarılı callback ve uygulama kaydı eşleşmedikçe canlı geçiş isteme.

Paneldeki mağaza bakiyesini bankaya aktarma ayarı alt satıcıya Platform Transfer yetkisi değildir. Pazaryeri ve otomatik yenileme, mağaza özelinde yazılı PayTR kapsamı ve ayrı test kabulü gerektirir.
