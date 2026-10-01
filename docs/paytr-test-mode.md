# PayTR abonelik test kabulü

Bu dilim tek çekim abonelik checkout'unu, PayTR iFrame sonuç bildirimini, abonelik ödemelerinin durum sorgusunu ve test ödemelerinin kısmi/tam iadesini kapsar. Pazaryeri siparişleri, alt satıcı transfer talimatı, kart saklama ve otomatik yenileme bu sağlayıcıya bağlı değildir. Tarayıcının başarılı dönüşü ödeme kaydı üretmez; yalnız doğrulanmış ve veritabanına işlenmiş bildirim bunu yapar.

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
4. Sağlayıcının işlem durumunu uygulamadaki ödeme ve abonelik kayıtlarıyla karşılaştır. Durum sorgusu kayıtlı PayTR sipariş kimliği (`merchant_oid`) ile günde bir kez karşılaştırma yapar. Yanlış imza, farklı tutar ve mükerrer bildirimde ikinci abonelik/para olayı oluşmadığını doğrula. Tarayıcı dönüşü callback'ten önce gelirse uygulama sonucu “kontrol ediliyor” göstermelidir.
5. PayTR mağaza panelindeki API uyarılarını ve işlem durumunu incele. Başarılı callback ve uygulama kaydı eşleşmedikçe canlı geçiş isteme.

Paneldeki mağaza bakiyesini bankaya aktarma ayarı alt satıcıya Platform Transfer yetkisi değildir. Pazaryeri ve otomatik yenileme, mağaza özelinde yazılı PayTR kapsamı ve ayrı test kabulü gerektirir.

## Sipariş durum sorgusu ve mutabakat

Durum sorgusu PayTR'ın [resmi Durum Sorgu API'sini](https://dev.paytr.com/durum-sorgu) kullanır. Uygulama `https://www.paytr.com/odeme/durum-sorgu` adresine yalnız `merchant_id`, yerel ödeme kaydındaki `merchant_oid` ve imzayı (`merchant_id + merchant_oid + merchant_salt` metninin `merchant_key` ile HMAC-SHA256 Base64 özeti) içeren POST gönderir. Yanıtın başarılı olması halinde `payment_amount` ve `payment_total` alanları TL birimindeki ondalık tutarlardır; `TL`, uygulamada `TRY` olarak gösterilir. `test_mode` ödeme ortamını, `returns[].return_amount` ise iade toplamını verir.

PayTR yanıt kodu `004`, başarılı tahsilat bulunmadığı anlamına gelir; bu yanıt tek başına başarısız ödeme kanıtı sayılmaz. Durum sorgusu yeni tahsilat başlatmaz, abonelik hakkı vermez veya iade/transfer talimatı oluşturmaz. Her gün çalışan mutabakat, sağlayıcı kaydını yerel sipariş, tutar, para birimi, test modu ve iade bilgisiyle karşılaştırır. Yeni veya süresi dolmamış bir checkout için `004` fark oluşturmaz. Yerel kayıtta tamamlanmış/iade edilmiş ödeme sağlayıcıda bulunamazsa ya da süresi dolmuş checkout'un sonucu belirsizse inceleme kaydı oluşur. Tamamlanmış durumla uyuşmayan bir sağlayıcı tahsilatı da otomatik uygulanmaz.

Farklar, yerel checkout anahtarı ve PayTR sipariş kimliğiyle `payment.reconciliation.mismatch` / `IncelemeGerekli` olayı olarak kaydedilir. Aynı fark aynı gün tekrar yazılmaz. İmzalı bildirimler aynı sipariş için çelişkili kesin sonuçlar verirse ikinci sonucu otomatik uygulamak yerine inceleme olayı kaydedilir; ödeme ve abonelik planı olduğu gibi kalır.

PayTR test mağazasında salt okunur sorguyla 828 TL başarılı deneme siparişinin `test_mode=1` ve iade toplamı `0` olduğu görüldü. Eski 960 TL başarısız sipariş sorgusunda `004` döndü; bu sonuç başarısız bildirimle tutarlıdır, ancak sorgu yanıtı tek başına başarısızlık kanıtı değildir. Bu sorgular yeni ödeme veya sağlayıcı tarafında değişiklik yapmadı.

Windows Application Control xUnit'in test DLL'sini yüklemesini engelledi; hedefli doğrulama bu nedenle Linux ARM64'te yapıldı. Resmi .NET SDK ile son Release koşusunda 6 sınıf, 74 test geçti; 0 başarısız ve 0 atlanan test oldu. Muhasebeci ödeme akışı, fiyat koruması ve çelişkili bildirimler bu koşuya dahildir. Sorgunun 15 saniyelik sınırı yanıt gövdesini okumayı da kapsar; gövde okuması takıldığında iptal edildiği testle doğrulandı. Release API yayımlaması da başarılıdır.

Gerçek C# sağlayıcısı PayTR'da 828 TL başarılı siparişi buldu; eski 960 TL başarısız sipariş için `004` döndü. `PaymentReconciliationService`, izole PostgreSQL test veritabanında iki kez çalıştırıldı: her çalıştırmada 1 ödeme kontrol edildi, 0 fark bulundu ve 0 inceleme kaydı eklendi. Ödeme ile abonelik kayıtları korundu.

Yeni API yalnız izole PayTR test uygulamasına yayımlandı; önceki imajın geri dönüş etiketi korundu. Canlı üretim `main` sürümü `1e4c8e4` değişmeden kaldı ve sağlayıcı `Unconfigured` durumundadır. Bu doğrulama K1–K3 kapsamını veya tam CI'ı tamamlanmış saymaz.

## Kısmi ve tam abonelik iadesi

Test adaptörü PayTR'ın [İade API'sini](https://dev.paytr.com/iade-api) kullanır. İstek tutarı iki ondalıkla ödeme para birimi cinsinden gönderilir; imza `merchant_id + merchant_oid + return_amount + merchant_salt` metninin `merchant_key` ile HMAC-SHA256 Base64 özetidir. Her kalıcı talimatın alfa numerik `reference_no` değeri iade isteğine eklenir ve durum sorgusundaki iade kaydıyla eşleştirilir. Bu referansın sağlayıcıda tekrar göndermeyi güvenli yaptığı varsayılmaz.

`IPaymentRefundService.RequestAsync`, işletme ve ödeme ilişkisini, abonelik işlem tipini ve kalan iade tutarını veritabanı işlemi içinde kontrol eder. `OdemeIadeTalimati` ödeme kaydına bağlıdır; aynı anahtar/tutar aynı kaydı döndürür, aynı anahtarla farklı tutar reddedilir. Hazır ve sonuç bekleyen tutarlar ayrılır; toplam ödeme aşılmaz. Muhasebeci hizmeti ve pazaryeri iadeleri bu servisle gönderilmez.

Gönderim, `DispatchAsync` ile açıkça başlatılır; servis sağlayıcı çağrısından önce `Gonderiliyor` durumunu kalıcılaştırır. Hem servis hem PayTR adaptörü siparişin test ödemesi olduğunu sorguyla doğrular. Canlı ödeme veya doğrulanamayan sipariş için iade gönderilmez. HTTP isteği ve yanıt gövdesi okuması toplam 15 saniyeyle, yanıt boyutu 64 KiB ile sınırlıdır; sağlayıcının ham hata metni saklanmaz.

İade isteğinin başarılı yanıtı tek başına yerel iadeyi tamamlamaz. Sipariş sorgusunda aynı referans ve aynı tutar bulunduğunda talimat `Tamamlandi` olur ve bir `payment.refund.confirmed / Islendi` olayı yazılır. Ağ hatası, iptal veya belirsiz yanıt `SonucBekliyor` durumunda kalır; aynı talimat otomatik ya da tekrar `DispatchAsync` çağrısıyla gönderilmez. Günlük mutabakat bu kayıtları yalnız sorguyla uzlaştırır. Yanlış tutar veya mükerrer referans incelemeye bırakılır. `Tamamlandi`, PayTR iade kaydının eşleşmesini ifade eder; bankadaki valör/hesaba geçiş kabulü değildir.

Kısmi iade ödeme toplamını ve abonelik hakkını değiştirmez; tamamlanan iade toplamı ayrı talimatlardan hesaplanır. Toplam iade ödeme tutarına ulaştığında ödeme `IadeEdildi` olur ve yalnız o ödemenin oluşturduğu abonelik etkilenir. Eski bir ödemenin iadesi yeni aktif dönemi kapatmaz. Yükseltme iadesi, ancak hâlâ geçerli ve daha önce iade edilmemiş önceki dönemi geri açabilir.

Bu dilim dışarıya açık bir iade endpoint'i veya müşteri düğmesi eklemez; test kabulü ve yetkili sunucu işi servis üzerinden yürütülür. Üretim sağlayıcısı ve gerçek tahsilat ayarı kapalı kalır. Müşteriye sunulacak iade hakkı ve satış belgesi yöntemi D3 kapsamında ayrıca kararlaştırılır.

1 Ekim iade paketinin son kontrolünde resmi Linux ARM64 SDK ile `Release --no-incremental` derlemesi 0 uyarı/0 hatayla tamamlandı. İade, durum sorgusu, mutabakat ve ödeme yaşam döngüsündeki 83 hedefli test geçti; başarısız/atlanan test yok. Migration/model kontrolü temiz ve Release API publish başarılı. Kaynak arşivi mevcut doğrulama dizinine açıldığında eski dosya zamanları artımlı derlemeyi yanıltabildiğinden son kontrol zorunlu yeniden derleme kullandı.

Gerçek PayTR test kabulünde 828 TL test siparişi için 100 TL iade API yanıtı; sipariş, tutar, test işareti ve referans bakımından doğrulandı. Ardından sipariş sorgusu `returns=[]` döndürdü. Bu nedenle talimat `SonucBekliyor` kaldı; yerel tamamlanan iade veya abonelik değişikliği oluşturulmadı. Aynı talimat ikinci kez gönderilmedi; kalan 728 TL iade aşamasına geçilmedi. Sağlayıcıda kısmi/tam iade mutabakatı henüz tamamlanmış değildir. Test iadelerinin sorguda kalıcı iade kaydı üretip üretmediği ve beklenen gecikme PayTR'dan teyit edilmelidir; yalnız API'nin başarılı yanıtı bu kabulü kapatmaz.

## Ödeme formunun zaman aşımı ve son doğrulama

`CreateCheckoutUrlAsync`, HTTP yanıt başlıklarını ve gövdesini aynı 15 saniyelik süre içinde okur. `ResponseHeadersRead` kullanıldığında `HttpClient.Timeout` gövde okumasını kapsamadığından, istemcinin iptaline bağlı ayrı bir zaman sınırı gerekir. İstemci isteği iptal ettiğinde bu iptal korunur.

Takılan gövde testi düzeltmeden önce 18 saniyelik dış test sınırında başarısız oldu; sağlayıcı kendi süresinde durmadı. Düzeltmeden sonra dokuz sınıftaki 101 hedefli test geçti; başarısız/atlanan test yok. Son kaynak resmi Linux ARM64 SDK ile `Release --no-incremental` derlendi: 0 uyarı/0 hata. EF model kontrolü bekleyen değişiklik bulmadı; bu kontrol veritabanı bağlantısı olmadan tasarım zamanı bağlamıyla çalıştı. Release API publish, eksik analyzer paketi restore edildikten sonra tamamlandı. Tam CI çalıştırılmadı.

1 Ekim 2026 panel kontrolünde mağaza test modunda, kimlik doğrulaması tamamlanmış ve kalıcı bildirim adresi doğru görüldü. 25 Eylül–1 Ekim API uyarı listesi boştu. Saat 04.18.20'de (Türkiye saati) iade sorgusu, Platform Transfer test yöntemi/yetkisi, kart saklama/Non3D ve harcama itirazı konuları tek teknik destek talebinde gönderildi. Panel teslimi doğruladı; yanıt bekleniyor. Anahtarlar mesaja veya eklere eklenmedi.

Pazaryeri aktarımı için [Platform Transfer API](https://dev.paytr.com/platform-transfer-talebi/transfer-talimatinin-verilmesi), bankadaki alıcının adı ve IBAN'ını ister; belgelenmiş bir `test_mode` alanı bulunmaz. [Tekrarlayan kart ödemesi](https://dev.paytr.com/direkt-api/kart-saklama-api/kayitli-kart-tekrarlayan-odeme), kart saklama ve mağazanın Non3D yetkisini gerektirir. Bu mağaza için yazılı test kapsamı alınmadan mevcut abonelik test ayarı bu işlemlere uygulanmaz. Pazaryeri gateway'i `Unconfigured` kalır; mevcut kaynakta gerçek pazaryeri checkout'u, PayTR transfer bildirimi/mutabakatı, kayıtlı kart ve otomatik tahsilat bulunmaz. Bunlar tamamlanmış iş sayılmaz.

Canlı geçiş ekranı iletişim/adres bilgileri, teslimat, satış ve iptal/iade koşullarının erişilebilir olmasını istiyor. Bu koşullar ve satış belgesi yöntemi Burak ile mali müşavir tarafından kararlaştırılmalıdır. Mağazanın kimlik doğrulaması bu kararların veya canlı onayın yerine geçmez.
