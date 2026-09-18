# Systemcel yayın ve yazılım kapanış planı

Tarih: 11 Eylül 2026. Durum: yazılım uygulaması başladı; dış doğrulama kapıları açık.

Bu plan yeni kullanıcı kaydı, tenant izolasyonu, sunucu dışı yedek, Oracle alarmları ve gerçek cihaz/kullanıcı pilotunu kapatır; kod taramasında görülen diğer yazılım işlerini de sıraya koyar. Şirket kuruluşu, başvurular ve hukuk işlemleri kapsam dışıdır. Bunlara bağlı teknik entegrasyonlar ayrıca gösterilir.

Ürün kararlarının kaynağı [ürün kapsamı](../product-scope.md), mevcut operasyon listesi [YAPILACAKLAR.md](../../YAPILACAKLAR.md)'dir. Bu belge yeni özelliklerin lansmana alınmış olduğu anlamına gelmez.

## 1. Başlangıç durumu ve kanıt sınırı

- Son kayıtlı canlı altyapı doğrulaması 8 Eylül tarihlidir. DNS, HTTPS ve health/readiness başarılı; gerçek yeni Clerk kimliğiyle uçtan uca kayıt hâlâ açık. Bu plan hazırlanırken canlı ortam yeniden test edilmedi.
- Oracle günlük yerel yedeği ve ayrı veritabanına geri yükleme kanıtı var. Otomatik sunucu dışı aktarım yok; Windows DPAPI kopyası başka bir kullanıcı profiliyle kurtarma kanıtı değildir.
- CI test/build yapıyor; Oracle'a otomatik yayın yapmıyor. Geçiş kaydı `oracle-production-candidate` etiketini bildiriyor. Etiket değiştirmek tek başına kabul ölçütü değildir.
- Stok işlem çubuğu, rapor alanları ve bu plan `48d76c7` commit'inde kaydedildi. Bu commit henüz tek başına canlıya dağıtım veya yayın kapısı kanıtı değildir.
- Aşağıdaki kod/test referansları implementasyonun varlığını gösterir. Testlerin bu turda yeniden geçtiği veya özelliğin canlıda doğrulandığı anlamına gelmez.

Kaynaklar: [Geçiş durumu](../../deployment/oracle-free/MIGRATION-STATUS.md), [CI](../../.github/workflows/ci.yml), [release runbook](../runbooks/release.md).

### 11 Eylül 2026 uygulama kaydı

Şirket veya sağlayıcı hesabı gerektirmeyen ilk yazılım paketi uygulandı. Aşağıdaki durumlar kod/test tamamlanması ile gerçek ortam kabulünü bilinçli olarak ayırır:

| Paket | Yerel yazılım durumu | Kalan gerçek ortam kabulü |
|---|---|---|
| K0/K8 | Tam SHA'ya bağlı kanıt şeması, üretici/doğrulayıcı, public smoke ve dağıtım yapmayan kontrollü release-bundle workflow'u eklendi. Developer API adresi `https://systemcel.app/api/v1` olarak düzeltildi. | Workflow artifact'ının aday SHA için üretilmesi, Oracle'a kontrollü dağıtım ve izole rollback provası. |
| K3 | `rclone crypt` zorunlu uzak aktarım; kilit, retry, immutable paket, indirerek checksum kontrolü, en son yazılan tamamlanma işareti, atomik başarı durumu ve uzak kanıta bağlı güvenli yerel retention eklendi. Bash syntax ve sahte-rclone hata/idempotency fixture'ları geçti. | Nesne depolama ve anahtar/IAM kararı; üç ardışık gerçek uzak yedek; yalnız uzak kopyadan bağımsız restore ve ölçülen RPO/RTO. |
| K4 | CPU, RAM, disk, readiness, PostgreSQL bağlantısı, container restart ve son doğrulanmış uzak yedek yaşını üreten Prometheus metin collector'ı ile systemd timer eklendi. Sahte Docker/readiness fixture'ı geçti. | Kalıcı metric backend/agent, VM dışı HTTPS probe, gerçek alarm ve düzelme teslimi, sorumlu/alıcı tanımı. |
| K5 | Genel bildirim outbox'ına SMTP adaptörü bağlandı. Alıcı aktif kullanıcı ve aynı işletmedeki aktif üyelikle çözülüyor; snapshot uyuşmazlığı reddediliyor; eski deneme sender'ıyla mükerrer e-posta engellendi. | Gönderici alan adı ve canlı SMTP kimlik bilgileri; kontrollü gelen kutusunda teslim kanıtı. Güvenli tenant-kullanıcı chat eşleştirmesi olmadığı için genel Telegram kanalı etkinleştirilmedi. |
| K10 | Fiyat artışı için değişmez dönem/tutar/metin/alıcı ve iki-kanal outbox kanıtı eklendi. Uygulama içi ve e-posta teslimleri yenilemeden en az 30 gün önce tamamlanmamışsa checkout eski dönem fiyatına düşüyor; dönem sonu iptal tahsilatı engelleniyor. Migration, aylık/yıllık, uzlaştırma sonrası yenileme ve tenant/alıcı regresyonları eklendi. | Fiyat değişikliği planlayan yönetim/job bağlantısı ile gerçek ödeme sağlayıcısının zamanlanmış tahsilat ve mutabakat yolunda aynı kuralın K11 ile doğrulanması. |
| K1/K2/K7/K9 | Kanıt şablonu ve kabul alanları hazır; mevcut otomatik regresyonlar korunuyor. | Gerçek Clerk/OAuth hesapları, iki kontrollü tenant, fiziksel iPhone/Safari ve pilot katılımcıları. |
| K6 | Oracle'a DeepSeek anahtarı ve `deepseek-flash` seçimi eklendi; doğrudan sağlayıcı smoke isteği ile public health/readiness 11 Eylül'de geçti. Yerel aday istemci işletme verilerini maskeliyor, tenant için anahtarlı anonim `user_id` üretiyor ve kullanıcıya ham sağlayıcı hatası göstermiyor. | Aday kodun commit/yayın işlemi, gerçek oturumla tenant bağlı AI cevabı, sohbet dosyası uçtan uca testi; DeepSeek hesap veri kullanım tercihi ile alt işleyen/yurt dışı aktarım metninin kapatılması. |
| K11 | Bu turda uygulanmadı. | Şirket ve ödeme sağlayıcısı hesabı açıldıktan sonra gerçek adapter, sandbox/gerçek tahsilat, iade ve satış belgesi mutabakatı. |

Yerel doğrulama kaydı: release-evidence Pester testleri 3/3; bildirim, fiyat koruma, migration ve lifecycle hedefli testleri 56/56; yedek/collector Bash fixture'ları başarılıdır. DeepSeek istemcisi, AI finans bağlamı ve OCR güvenlik testleri 10/10 geçti. Oracle ortam değişkeni uygulama konteynerine aktarıldı; `deepseek-flash` doğrudan sağlayıcı smoke isteği, public health ve readiness 200 döndü. Bu kayıt gerçek kullanıcı oturumundaki Systemcel AI akışını veya aday kodun canlıya yayımlandığını kanıtlamaz. Gerçek SMTP, Clerk, iPhone, pilot ve ödeme doğrulamaları yapılmadı.

## 2. Öncelik ve sıra

| Paket | Öncelik | İşin türü | Başlamadan gereken | Sorumlu |
|---|---|---|---|---|
| K0 — Sürüm ve kanıt envanteri | İlk adım | Yayın hazırlığı | Yerel/uzak/canlı sürümlere okuma erişimi | Geliştirici |
| K1 — Yeni Clerk kaydı ve kurulum | Pilot öncesi P0 | Gerçek entegrasyon doğrulaması | K0, kontrollü yeni hesaplar | Geliştirici + hesap sahibi |
| K2 — Tenant, üyelik ve oturum sınırları | Pilot öncesi P0 | Güvenlik doğrulaması | K1, birbirinden bağımsız iki işletme | Geliştirici |
| K3 — Taşınabilir otomatik yedek | Pilot öncesi P0 | Operasyon kodu + kurtarma provası | Yedek hedefi ve anahtar saklama kararı | Geliştirici + altyapı sahibi |
| K4 — Alarmlar ve hata görünürlüğü | Pilot öncesi P0 | Operasyon entegrasyonu | Bildirim alıcısı, K3'ün yedek durum sözleşmesi | Geliştirici + operasyon sorumlusu |
| K5 — Bildirim kanalları ve SMTP | İlan edilen kanallar için P0 | Eksik adaptör + teslim doğrulaması | Kontrollü alıcılar ve sağlayıcı erişimi | Geliştirici |
| K6 — AI, sohbet dosyası ve kritik UI | Lansmanda sunulan özellikler için P0 | Entegrasyon/akış doğrulaması | K1–K2, AI erişimi ve test dosyası | Geliştirici |
| K7 — Fiziksel iPhone/Safari | Pilot öncesi P0 | Gerçek cihaz doğrulaması | K1–K2, K5–K6'nın aday sürümü | Cihaz sahibi + geliştirici |
| K8 — Tekrarlanabilir yayın ve geri dönüş | Pilot öncesi P0 | Dağıtım otomasyonu | K0, K3–K4 | Geliştirici |
| K9 — Sınırlı gerçek kullanıcı pilotu | Genel yayın öncesi P0 | Kullanıcı kabulü | K1–K8'in gerekli kapıları | Pilot koordinatörü + kullanıcılar |
| K10 — Ücretli abonelik bildirim koruması | Gerçek ücretli yayın öncesi P0 | Yeni iş kuralı + veri kaydı | K5; geliştirme Fake ile başlayabilir | Geliştirici |
| K11 — Gerçek ödeme ve satış belgesi bağlantısı | Gerçek ücretli yayın öncesi P0 | Sağlayıcı entegrasyonu | K10, şirket/sağlayıcı erişim kapısı | Geliştirici + ödeme operasyonu |

K1–K2 ile K3–K4 bağımsız ilerleyebilir. K5–K6 geliştirmeleri de bu sırada hazırlanabilir. Genel ücretli yayın için K9, K10 ve K11 birlikte kapanmalıdır. Sağlayıcı erişimi beklenirken Fake ile teknik doğrulama yapılabilir.

## 3. İstenen beş yayın kapısının uygulaması

Bu bölüm kayıt/kurulum, tenant izolasyonu, yedekleme, alarmlar ve cihaz/kullanıcı pilotunu açar. Ek yazılım paketleri sonraki bölümdedir.

### K1 — Yeni Clerk kullanıcısıyla kayıt ve kolay kurulum

Mevcut mobil auth testleri Clerk/API yanıtlarını taklit ediyor; gerçek sağlayıcı, cookie ve callback zincirini kanıtlamıyor. Kaynak: [mobile-auth.spec.ts](../../Systemcel.Web/e2e/mobile-auth.spec.ts), [ClerkAuthenticationSetup.cs](../../Systemcel.Api/ClerkAuthenticationSetup.cs).

1. İlk kez kullanılacak işletme ve muhasebeci kimliklerini belirle. E-posta doğrulaması ile Google OAuth senaryolarını kapsa.
2. Kayıt → doğrulama/OAuth dönüşü → yerel kullanıcı ve işletme oluşturma → rol seçimi → kolay kurulum → ana ekran akışını çalıştır.
3. Kurulumu yarıda bırakıp devam et; sayfayı yenile; çift tıklama/tekrar callback sonrasında ikinci kullanıcı, işletme veya abonelik oluşmadığını denetle.
4. Çıkış → geri tuşu → korunan API isteği; oturum süresi dolması ve yeniden giriş senaryolarını çalıştır.
5. Eski Clerk kimliğinin taşınmasını yeni hesap akışından ayrı doğrula; eski kullanıcıya ait işletmenin yeni kullanıcıya bağlanmadığını kontrol et. Geçiş allowlist'ini ancak mevcut kullanıcı bağlantıları doğrulandıktan sonra kapat.

Kapanış: iki hesap tipinde gerçek oturumla kurulum tamamlanır; yeniden giriş aynı hesaba döner; tekrar istekler mükerrer kayıt üretmez; çıkış sonrası yetkisiz API isteği reddedilir. Kanıt UTC zamanı, commit SHA, rol ve anonim test hesabı etiketiyle tutulur; token/parola kaydedilmez.

### K2 — Tenant izolasyonu ve ekip yetkileri

Tenant ve üyelik kontrolleri zaten var. [IsletmeServiceTenantTests.cs](../../CashTracker.Tests/IsletmeServiceTenantTests.cs), [MembershipEntitlementAuditTests.cs](../../CashTracker.Tests/MembershipEntitlementAuditTests.cs) ve [SecurityHardeningTests.cs](../../CashTracker.Tests/SecurityHardeningTests.cs) korunarak gerçek kimlik/API matrisi tamamlanır.

1. Birbirinden bağımsız A/B işletmeleri, A'nın sahibi/personeli ve salt okunur bağlı muhasebeci için erişim matrisi çıkar.
2. Liste, tek kayıt, oluşturma/güncelleme/silme, aktif işletme değiştirme, rapor indirme, sohbet eki ve SignalR konuşma katılımını kapsa. URL/body işletme kimliği değiştirilerek yalnız bu kontrollü hesaplar arasında dene.
3. Yetkisi kaldırılan kullanıcının açık sekmesini, doğrudan dosya bağlantısını ve devam eden sohbet bağlantısını yeniden kontrol et. Arayüzde düğmenin gizlenmesi yerine sunucudaki reddi doğrula.
4. Davet kodu akışında yanlış e-posta, doğrulanmamış e-posta, tekrar kullanım, süre aşımı ve kapasite yarışını dene. Rol değişimi, üyeyi kaldırma, tekrar davet ve sahiplik devrini mevcut kurallara göre kontrol et.
5. Bir sınır ihlali bulunursa önce dar bir başarısız regresyon oluştur; ilgili API/servisi düzelt ve aynı matrisi yeniden çalıştır.

Kapanış: yabancı verinin gövde, dosya veya gerçek zamanlı kanal üzerinden sızdığı hiçbir senaryo kalmaz; reddedilen yazmalar veri değiştirmez; izinli işlemler çalışır. Davet kodu korunur; kullanıcı adı/parola ile ekip hesabı tasarımı bu planın işi değildir.

### K3 — Otomatik, taşınabilir sunucu dışı yedek

[backup.sh](../../deployment/oracle-free/scripts/backup.sh) PostgreSQL dump, appdata arşivi ve checksum üretir. [restore.sh](../../deployment/oracle-free/scripts/restore.sh) gerçek veritabanı/appdata içeriğini değiştirir ve sabit volume kullanır; canlı checkout üzerinden prova amacıyla çalıştırılmamalıdır.

1. VM'den bağımsız nesne depolama hedefi, saklama süresi, kapasite/maliyet sınırı ve kurtarma anahtarının sahipliğini belirle.
2. Mevcut doğrulanmış üçlü yedeği, Windows profiline bağlı olmayan şifreli bir pakete dönüştür. Kurtarma için uygulamanın sabit şifreleme anahtarı ve gerekli kurulum sırlarının ayrı, erişim kontrollü kurtarma kaydı da bulunmalı.
3. Yüklemeyi zamanlanmış işin parçası yap: tek çalışan kilidi, yarım yüklemeyi başarılı saymama, tekrar deneme, paket kimliği ve uzak checksum doğrulaması ekle. Son başarılı **uzak** yedek zamanı yerel yedekten ayrı ölçülsün.
4. Uzak kopya başarısızken tek kurtarılabilir kopyanın retention temizliğiyle kaybolmasını engelle; disk doluluğu alarmıyla birlikte ele al. Uzak saklama/silme yetkilerini mümkün olduğunca aktarım kimliğinden ayır.
5. PostgreSQL dump ile appdata arşivinin farklı anlarda alınmasını ele al: ilk provada mevcut `--quiesce` yolunu kullan; periyodik kesintisiz yöntemi eşzamanlı dosya yükleme/silme altında doğrula. Tutarlılık kanıtlanmadan kesintisiz yedek için tam kurtarma iddiası yapma.
6. Ayrı makine veya tamamen ayrı Compose proje/volume/DB adları kullanan kurtarma düzeneği hazırla. Paketi yalnız uzak hedeften indir; bağımsız anahtarla çöz; PostgreSQL 18 ve appdata'yı yükle. Dış mesaj/ödeme işleri kapalı kalsın.
7. Satır sayıları ve kontrol toplamları yanında örnek rapor/sohbet dosyalarını ve şifreli ayarların açılmasını doğrula. VM ve eski Windows profili olmadan geçen süreyi kaydet.

Önerilen pilot hedefi: en fazla 24 saat veri kaybı aralığı (RPO), en fazla 4 saat tam kurtarma (RTO), 14 günlük uzak günlük kopya. Bunlar ölçülmüş değerler veya verilmiş hizmet taahhüdü değildir. Ücretli yayın öncesinde kabul edilebilir veri kaybı kararı tekrar alınmalı; 24 saat kabul edilmiyorsa daha sık yedek veya sürekli WAL arşivleme/ana dönebilme tasarımı ayrı iş olur.

Kapanış: en az üç ardışık otomatik uzak yedek başarılı; başarısız aktarım alarmı ulaşmış; son paket bağımsız ortamda tam geri yüklenmiş; ölçülen RPO/RTO hedefi karşılıyor.

Yöntem tercihi: mevcut dump+appdata paketinin otomatik şifreli aktarımıyla başla. Manuel indirme taşınabilirlik ve süreklilik ihtiyacını karşılamıyor. Sürekli arşivleme daha küçük veri kaybı penceresi sağlar, ancak işletim yükü getirir; hedef gerektirirse geçilir. Sağlayıcı seçimi bütçe/erişim kararı olarak açık bırakılmıştır.

### K4 — Oracle alarmları ve gözlemlenebilirlik

[Monitoring runbook](../runbooks/monitoring.md) eşikleri tanımlar; [RequestObservabilityMiddleware.cs](../../Systemcel.Api/Services/RequestObservabilityMiddleware.cs) istek kimliği, sayaç ve süre ölçümü üretir. Kalan iş ölçümlerin dışarı toplanması, alarm kuralları ve teslim kanıtıdır.

1. Sunucu dışından HTTPS/readiness kontrolü kur; VM tamamen kapandığında da alarm üretebilsin.
2. CPU, RAM, disk, PostgreSQL bağlantısı, container restart, HTTP 5xx ve son başarılı uzak yedek yaşını topla. Mevcut uygulama metriklerini yeniden yazmadan kalıcı ölçüm hedefine bağla.
3. Alarm alıcısı, kritik durumda ikinci kanal, tekrar bildirim aralığı ve düzelme mesajı tanımla. Kritik altyapı alarmını yalnız aynı VM'nin SMTP işine bağlama.
4. Mevcut eşikleri uygula: disk %70/%85; restart 15 dakikada 2 / 10 dakikada 3; yedek yaşı 26/36 saat; 5xx 10 dakika %2 / 5 dakika %5; readiness 5 dakikada 2 hata / art arda 5 hata.
5. Runbook'ta sayısal olmayan CPU/RAM için başlangıç önerisi: CPU 15 dakika %80 / 10 dakika %95; RAM 10 dakika %80 / 5 dakika %90. Gerçek pilot ölçümleriyle ayarla; bunları gözlemlenmiş yük gibi sunma.
6. Uygulama hata takibi ve destek kaydı aynı istek kimliğiyle bulunabilsin. Log/metric etiketlerine token, dosya içeriği veya yüksek sayıda kullanıcı kimliği ekleme.
7. Test metriği/izole arıza ile alarm ve düzelme teslimini kanıtla; canlı diski doldurarak test etme.

Kapanış: her kritik sinyalin veri kaynağı, eşiği ve sorumlusu kayıtlı; alarm ve düzelme mesajı alıcıya ulaşmış; VM dışı kontrol ile uzak yedek yaşı görünür.

### K7 ve K9 — Fiziksel iPhone/Safari ve gerçek kullanıcı pilotu

K7: cihaz modeli, iOS/Safari sürümü ve test edilen SHA'yı kaydet. Gerçek kayıt/OAuth, klavye açıkken form, çıkış/geri tuşu, işletme değiştirme, kamera/dosya seçimi, sohbet eki, rapor indirme ve modal odağını test et. Dar ekran, açık/koyu tema, ağ kesilip geri gelmesi ve uygulamaya dönüşü kapsa. WebKit emülasyonu bu kayıt yerine geçmez. Gerçek ödeme sağlanınca checkout dönüşünü aynı cihazda tekrar test et.

K9 için başlangıç önerisi: 3 işletme + 2 muhasebeciyle en az 7 günlük pilot. Sayı/süre plan varsayımıdır; seçilmiş katılımcı veya takvim taahhüdü değildir. Başlamadan katılımcı, cihaz, kullanılacak test verisi, destek kanalı ve hata kayıt biçimi belirlenir.

Pilot görevleri: kayıt/kurulum, ekip daveti, cari ve gelir-gider, stok/hızlı satış, fatura taslağı, rapor, muhasebeci bağlantısı, sohbet/dosya, bildirim ve mevcut ödeme modu. Operasyon alarmlarıyla uygulama hataları aynı dönemde izlenir.

Genel yayın kabulü: açık veri kaybı/tenant ihlali/çift tahsilat/hesaba erişememe hatası yok; kritik görevlerin tamamı desteklenen roller ve cihazlarda en az bir kez başarılı; başarısız görevler düzeltilip tekrar denenmiş; son 48 saatte tekrarlayan kritik hata yok; yedek ve alarmlar sağlıklı. Küçük örneklemde yalnız yüzdeye dayanılmaz; tamamlayan/başlayan hesap sayısı, görev hataları ve destek süresi ham sayılarıyla raporlanır. Ücretli tahsilatın kabulü K10–K11'i ayrıca gerektirir.

## 4. Beş kapıya eklenen yazılım işleri

### K0 ve K8 — Sürüm, yayın ve geri dönüş

- İncelenecek yerel değişiklikler, remote commit, CI sonucu ve canlı sürümü eşleştir. GitHub'a push edilmiş olmak canlıya çıktığı anlamına gelmez.
- Oracle için salt okunur repo erişimi veya sürümlenmiş dağıtım paketi yolunu tamamla. Manuel, SHA sabitlenmiş yayın ilk kabul için yeterli olabilir; sonraki adım aynı yordamı kontrollü workflow ile çalıştırmaktır.
- Aynı anda iki deploy çalışmasını engelle; yedek ön kontrolü, migration değerlendirmesi, readiness ve gerçek kullanıcı smoke adımlarını bağla. Oracle ARM64 üzerinde çalıştırma kanıtını koru; CI Docker build'i tek başına bunu kanıtlamaz.
- Önceki doğrulanmış uygulama sürümüne şema uyumlu geri dönüşü izole ortamda prova et. Şema değişikliği için ileri düzeltme/veri kurtarma kararını kaydet; eski migration'a otomatik düşürme yapma.
- Kapanış: hangi commit'in nasıl dağıtıldığı ve nasıl geri alınacağı tekrar edilebilir; son UI düzeltmeleri aday sürümde görünür; tüm yayın kanıtları aynı SHA'ya bağlıdır.

### K5 — Genel bildirim kanalları ve gerçek teslim

Somut eksik: [Program.cs](../../Systemcel.Api/Program.cs) genel outbox için e-posta ve Telegram'ı `YapilandirilmamisBildirimAdapter` ile kaydediyor. SMTP değişkenlerini doldurmak tek başına bu iki kanalı etkinleştirmez. [BildirimService.cs](../../CashTracker.Infrastructure/Services/BildirimService.cs) ve [BildirimDeliveryTests.cs](../../CashTracker.Tests/BildirimDeliveryTests.cs) kayıt, okundu, tercih, claim, retry/dead-letter temelinin zaten bulunduğunu gösteriyor.

- Genel bildirimler için gerçek e-posta/Telegram adaptörlerini mevcut outbox'a bağla; mevcut tahsilat/abonelik sender'larıyla mükerrer gönderimi engelle.
- Gönderici doğrulaması ve kontrollü gelen kutusu teslimini denetle. SMTP kabulü, alıcıya ulaşma ve uygulamada okundu olaylarını ayrı anlamlarla kaydet.
- Tercih kapalı, geceyi aşan sessiz saat, yeniden başlatma, süre aşmış claim, bağlantı hatası, yeniden deneme ve dead-letter'dan kontrollü tekrar işleme senaryolarını tamamla.
- Uygulama içi bildirim kaydı/okundu akışı mevcut; uygulama adapter'ının no-op olması tek başına bildirimin çalışmadığı kanıtı değildir. Kabul kontrolü gerçek panelde görünme ve okundu durumudur.
- Kapanış: açık kanalların her birinde doğru alıcıya tek mantıksal bildirim, kapalı kanalda gönderim yok; arıza görünür. Genel Telegram kanalı yayın kapsamına alınmıyorsa kullanıcıya aktif teslimat vaat edilmemesi ayrıca doğrulanır.

### K6 — AI, dosyalar ve ortak arayüz kabulü

- AI: mevcut istemci/kota/finansal bağlam kodunu kullanarak gerçek yanıt, kota dolumu, timeout ve sağlayıcı arızasını doğrula; A'nın yanıtında B verisi bulunmadığını denetle. İlgili kanıt: [AiAssistantFinancialContextTests.cs](../../CashTracker.Tests/AiAssistantFinancialContextTests.cs), [AiUsageQuotaServiceTests.cs](../../CashTracker.Tests/AiUsageQuotaServiceTests.cs). Lansmanda erişime açık AI için anahtar eklemekten sonra başarılı yanıt da gerekir.
- Sohbet: kontrollü küçük dosyayı iki rolde gönder/indir; bozuk veya aşırı büyük dosya, tekrar deneme, yetkisiz indirme ve bağlantı yenilenmesini kapsa. [SohbetMerkeziApi.cs](../../Systemcel.Api/Api/SohbetMerkeziApi.cs) ve güvenlik testleri zaten var; eksik olan gerçek kullanıcı uçtan uca kanıtıdır.
- Erişilebilirlik: mevcut [workspace-accessibility.spec.ts](../../Systemcel.Web/e2e/workspace-accessibility.spec.ts) üzerine kritik kayıt, ekip daveti, ödeme, sohbet ve rapor akışlarında klavye sırası, focus dönüşü, Escape, loading/boş/hata durumlarının kalan kapsamını çıkar. Aynı testleri baştan yazma.
- Doküman/ürün uyumu: [developer-api.md](../developer-api.md) ve [OpenAPI](../openapi/developer-api-v1.yaml) içindeki `app.systemcel.app` taban adresini doğrulanmış `systemcel.app` ile uyumlu hâle getir; bağlantıyı smoke'a ekle. P1/P2 listelerindeki mevcut özellikleri geliştirme, doğrulama ve gelecek kapsam olarak ayrıştır.
- Kapanış: ilan edilen kritik akışlarda engelleyici UI/entegrasyon hatası yok; bulunmuş her gerçek hata için uygun doğrulama kanıtı var.

### K10 — Ücretli abonelik bildirimleri ve fiyat artışı koruması

Somut eksik: [SubscriptionReminderSenders.cs](../../CashTracker.Infrastructure/Payments/SubscriptionReminderSenders.cs) hâlâ `SendTrialEndingAsync` ve deneme bitişi metni içeriyor. [SubscriptionLifecycleService.cs](../../CashTracker.Infrastructure/Payments/SubscriptionLifecycleService.cs) bu eski hatırlatmayı çağırıyor. Bu kod, lansmandaki denemesiz abonelik için yenileme/fiyat artışı teslim kanıtı sayılamaz.

- Ücretli dönem başlangıcı, yenileme, tahsilat hatası, dönem sonu iptal ve fiyat artışı olaylarını ayrı sözleşmelerle tanımla; eski kullanıcı kayıtlarını veri kaybetmeden ele al.
- Fiyat artışı için abonelik/dönem/eski-yeni tutar/yürürlük tarihi/metin sürümü/kanal kanıtı içeren değişmez snapshot sakla. Genel outbox payload'ını tek başına bu kanıtın yerine koyma.
- Mevcut ürün kuralındaki 30 gün ve e-posta + uygulama içi bildirim koşullarını zaman kontrollü testlerle uygula. Bildirim eksik/geç/başarısız olduğunda yeni fiyatı tahsil etme; uygulanacak erteleme/eski fiyat davranışını açıkça belirle.
- Kontrolü hem tahsilat isteği oluşturulurken hem yenileme işinde uygula. Sadece kullanıcı arayüzündeki uyarıyla yetinme; sağlayıcıya zamanlanmış tahsilat varsa onu da uzlaştır.
- Kapanış: zaman sınırı, başarısız teslim, yinelenen olay, yıllık peşin dönem ve iptal testleri geçer; bildirim şartı oluşmadan zamlı ödeme isteği çıkmaz. Bu yazılım şimdi Fake ile geliştirilebilir.

### K11 — Gerçek ödeme entegrasyonu ve abonelik satış belgesi

Somut eksik: `IPaymentProvider` kaydı yalnız Fake/Unconfigured seçiyor; PayTR erişimi alındığında yalnız anahtar girilerek canlıya geçilemez.

- Gerçek sağlayıcı adapter'ını mevcut fiyatlama, açık onay, idempotency ve lifecycle üzerine kur. Checkout, imza doğrulaması, tekrar/gecikmiş/sırasız webhook, timeout sonrası belirsiz sonuç, plan değişimi, ek kredi, yenileme, iptal, iade ve mutabakat senaryolarını çalıştır.
- Şirket/sağlayıcı erişimi öncesinde sözleşme arayüzü ve Fake regresyonları hazırlanabilir. Kesin alanlar, tekrarlayan ödeme yeteneği ve sandbox testleri sağlayıcının mağazaya tanımladığı erişime bağlıdır.
- Systemcel'in kendi abonelik tahsilatına ait satış belgesi akışını, müşterilerin uygulamada oluşturduğu faturadan ayrı incele. Ödeme geçmişi tek başına bu akışın kanıtı değildir; dar taramada tam otomatik belge üretimi doğrulanmadı.
- İlk ücretli yayın için belgelenmiş manuel belge düzenleme + ödeme/belge referansı mutabakatı veya otomatik sağlayıcı bağlantısı seçilir. Otomasyon seçilirse çift webhook'ta tek belge, iade/iptal bağlantısı ve hata kuyruğu eklenir. Müşteriye belge sunma yolu ücretli yayın öncesinde belli olmalıdır.
- Kapanış: sandbox matrisi ve kontrollü gerçek tahsilat/iade/mutabakat başarılı; K10 koruması gerçek adapter'da da geçerli; tahsilatın hangi satış belgesine bağlı olduğu izlenebilir.

## 5. Mevcut yazılımı tekrar yapmadan sonraki işler

| Alan | Kodda bulunan temel | Sonraki gerçek iş | Öncelik |
|---|---|---|---|
| Ekip/rol/sahiplik | [UyelikApi](../../Systemcel.Api/Api/UyelikApi.cs), üyelik/kapasite/sahiplik regresyonları | K2'de gerçek rol matrisi ve UI kabulü; yeni kimlik modeli kararı ayrıca tartışılır | K2; yeni model plan dışı |
| Bildirim omurgası | Kalıcı kayıt, okundu, tercih/sessiz saat, outbox/claim/retry/dead-letter | K5 kanal bağlantıları, tekrar işleme görünürlüğü ve K10 olayları | P0/P1, kapsamına göre |
| Stok defteri | [GelismisStokService](../../CashTracker.Infrastructure/Services/GelismisStokService.cs), depo/transfer/sayım/ters kayıt testleri | Pilot verisiyle kabul; rezervasyon/maliyet/mutabakat gereksinimini mevcut kodla farklandır; 100 bin hareket performans provası | P1/P2 |
| Banka eşleştirme | [BankaMutabakatService](../../CashTracker.Infrastructure/Services/BankaMutabakatService.cs), CSV/idempotency/tenant testleri | Gerçek anonimleştirilmiş dosya çeşitleri ve kısmi/toplu eşleştirme kapsamını netleştir; otomatik banka bağlantısı ayrı adapter | P1 |
| GİB/e-belge | Portal ayarı, taslak/SMS akışı | Tam e-belge sağlayıcısı sözleşmesi, UBL-TR, durum/webhook, iptal/itiraz ve mutabakat; gerçek işlem kanıtı | P1; lansman vaadi genişlerse P0 |
| Geliştirici API | Salt okunur v1, API key/tenant/scope/revoke/pagination testleri | Önce taban URL düzeltmesi ve canlı okuma smoke'u; OAuth/webhook portalı ayrı ürün genişlemesi | URL P0, genişleme P2 |
| Eski veri aktarımı | [ExternalDataMigrationService](../../Systemcel.Api/Import/ExternalDataMigrationService.cs), aktarım testleri | Kullanıcının gerçek formatlarıyla prova, yarım aktarım/tekrar işleme kanıtı; dağıtılacak masaüstü araç için sürümleme/imza | P1 |
| Şube/kur/Pro kapsamı | [SubeKurService](../../CashTracker.Infrastructure/Services/SubeKurService.cs), şube/kur testleri | Gerçek Pro otomasyonlarının ve konsolidasyonun mevcut davranıştan farkını tanımla; eksik senaryoları ondan sonra geliştir | P1/P2 |
| Pazaryeri iletişim kuralı | Sohbet/bağlantı akışları | İletişim tespiti, 30 dakika kuralı ve insan incelemesi için mevcut davranış envanteri + yanlış pozitif kabul seti | P1 keşif; eksikliği henüz kesinleştirilmedi |
| Performans ve frontend | `App.tsx` route lazy-load; istek metrikleri ve performans testleri | Pilot p95/API süreleri, bundle/CSS ölçümü; yalnız ölçülen darboğazları böl/parçala | P1 |

Bu başlıkların tamamı genel yayını erteleyecek yeni zorunluluk değildir. Kapsamda açıkça sunulan bir özellik çalışmıyorsa önce o vaatle ilgili düzeltme P0'a çekilir; yeni OAuth, banka bağlantısı veya tam e-belge ürünü kendiliğinden lansman kapsamına eklenmez.

## 6. Uygulama takvimi önerisi

Bir geliştirici, hazır test hesapları ve erişimler varsayımıyla kaba sıralama; canlı prova bulguları görülmeden teslim tarihi değildir:

- 1. hafta: K0, K1–K2; K3 kurtarma/aktarım düzeneği ve K4 alarm kurulumu. K5 adaptör ve olay farklarının uygulamasına başla.
- 2. hafta: K3 bağımsız kurtarma provası, K4 alarm teslimleri, K5–K6, K7 gerçek cihaz ve K8 sürüm provası. Kritik hata çıkarsa pilot başlangıcı kayar.
- 3. hafta: kapılar kapanmışsa K9'un en az 7 günlük pilotu; hata düzeltmeleri ve ölçüm. K10 ayrı iş paketi olarak ilerler.
- K11: şirket/sağlayıcı erişimi ve K10 tamamlandıktan sonra ayrıca tahminlenir; sağlayıcı bekleme süresi takvime gizlenmez.
- P1/P2: pilot sonuçlarıyla yeniden sıralanır; önce destek yükü ve hata oranını düşüren işler alınır.

Önce karara bağlanacak dış girdiler: yedek hedefi/bütçesi ve anahtar sorumlusu; alarm alıcısı; kontrollü hesaplar ve test dosyası; AI/SMTP erişimleri; fiziksel iPhone ve pilot katılımcıları; ücretli yayın için kabul edilebilir veri kaybı ve satış belgesi yöntemi. Bu girdiler kod taramasını ve yerel geliştirme planlamasını engellemez; ilgili canlı adımın tamamlanmasını etkiler.

## 7. Kapanış kaydı ve yayın kararı

Her paket için tek kayıt tutulur: paket ID, aday commit SHA, ortam, UTC zaman, anonim senaryo/rol, beklenen sonuç, gerçekleşen sonuç, kanıt yolu ve varsa kalan engel. Fiziksel cihaz işlerinde cihaz/sürüm; yedekte uzak paket kimliği/checksum ve kurtarma süresi eklenir. Hassas kanıtlar herkese açık repoya konmaz.

Kontrol kutusu yalnız kanıtla kapanır. Kodun mevcut olması, mock testinin geçmesi, health 200 veya commit/push tek başına canlı kabul değildir. Bu tur üstteki yerel yazılım paketlerini ve kanıt araçlarını üretmiştir; dış erişim, gerçek cihaz ve pilot gerektiren yayın kapıları tamamlanmış olarak işaretlenmemiştir.
