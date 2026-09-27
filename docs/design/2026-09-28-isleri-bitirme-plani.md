# Systemcel kalan işleri bitirme planı — 28 Eylül 2026

Amaç: ilk ücretli yayında tedarikçi pazaryeri, sevkiyat ve mal kabulü de çalışır duruma getirmek; kalan işleri kod/komut satırı, tarayıcı ve dış karar olarak ayırmak.

Bu plan 28 Eylül'de yerel kaynaklar ve [YAPILACAKLAR.md](../../YAPILACAKLAR.md) içindeki 25–26 Eylül kayıtlarıyla hazırlandı. Sonraki uygulama ve doğrulamalar Bölüm 11'de tarihleriyle kaydedilir. Public smoke, canlı ödeme/yedek ayarlarını veya sunucudaki SHA'yı tek başına doğrulamaz.

Mevcut R/K/A/D/P1 numaraları korunur. Aşağıdaki B numaraları, aynı işlerin tarayıcıda yürütülecek parçalarıdır; ayrı özellikler değildir.

**Bitirme sırası:** mevcut yerel paketi hazırla → PayTR erişimini ve operasyon kararlarını netleştirirken bağımsız kodu tamamla → ödeme ve muhasebeyi test ortamında bağla → gerçek hesap/cihaz kabulünü yap → dar pilotu tamamla → aynı sürümü doğrulayarak ücretli yayını aç.

## 1. Nerede duruyoruz?

| Alan | Eldeki kanıt | Bu planda kalan |
|---|---|---|
| Yerel sürüm | HEAD `8cc986c`; beş takip edilen web dosyasında değişiklik ve yeni `blogPosts.ts` var. Yapılacaklar dosyası da yerel değişmiş. | Blog/katalog paketinin son kontrolü ve yayın kabulü. |
| Canlı sürüm | 26 Eylül kaydında yerel/uzak/canlı SHA aynı; başarılı CI ve deploy bağlantıları var. | Uygulamaya başlarken yeniden sürüm tespiti; sonraki her adayda aynı SHA'nın doğrulanması. |
| Gerçek ödeme | Son canlı kayıtta `Unconfigured`; PayTR adaptörü bulunmuyor. Test mağazası açıldığı bildirilmiş. | Panel yetkileri, gerçek adaptör, bildirim, transfer, iade, mutabakat, abonelik belgesi. |
| Pazaryeri | Sevkiyat, kısmi kabul/red, stok/cari/fatura/hakediş temeli ve itiraz ekranları mevcut. | Kabul kararının sonradan düzeltilmesi, gerçek para/e-belge bağlantısı, gerçek tedarikçi pilotu. |
| Katalog | Son canlı kontrolde tedarikçi ve ürün sayısı sıfır. | Yetkili gerçek tedarikçinin gerçek ürünleri. Boş ekranın düzelmesi katalog doldurmaz. |
| AI ve posta | Gerçek AI yanıtı ve genel outbox e-postasının Gmail teslimi kaydedilmiş. | İki işletme arasında veri ayrımı, öneri onayları, yenileme/fiyat olayı teslimi ve dosya yetkileri. |
| Yedek ve alarm | Şifreli uzak yedek, örnek veritabanı restore'u ve temel alarm teslimleri mevcut. | Üç ardışık günlük yedek, başka makinede tam uygulama kurtarma, kalıcı dış metrikler ve ikinci alarm kanalı. |

Eski belgeler yeni iş çıkarma kaynağı olarak kullanılmayacak. Örneğin `MIGRATION-STATUS.md` otomatik yayın/uzak yedek öncesini; bazı runbook ve kapsam metinleri `Fake` ödeme dönemini anlatıyor. Bunlar bugünkü eksikleri doğrudan temsil etmiyor. Güncel kapsamda şirket kuruluşunu, SMTP kurulumunu, AI anahtarını veya Oracle geçişini yeniden başlatmaya gerek yok.

## 2. Kod ve komut satırıyla yapılacaklar

Bu bölümün sahibi Codex. SSH, sağlayıcı API'si ve otomatik testle yapılabilen işler de burada; bunlar için panelde elle işlem yapmak gerekmiyor.

### C0 — Durum belgelerini ve kabul kayıtlarını birleştirme

Somut uyuşmazlık: `scripts/New-SystemcelReleaseEvidence.ps1` ve `scripts/release-evidence.schema.json` içindeki K1 kayıt/kurulum, K2 işletme ayrımı, K6 AI/sohbet/UI, K7 fiziksel iPhone, K8 geri dönüş, K9 pilot anlamına geliyor. Güncel yapılacaklar listesindeki aynı numaralar ödeme ve pazaryeri geliştirmelerini anlatıyor.

- `YAPILACAKLAR.md` maddelerini uygulama planının sabit kimliği olarak kullan. Tarihsel K numaralarını doğrudan yeni K numaralarıyla eşitleme.
- Ürün kapsamı, yayın/izleme rehberi ve sürüm kanıtı şablonundaki eski ödeme/şirket durumu ifadelerini gerçek yapılandırmayla karşılaştır. Tarihsel kayıtları silmeden açıkça tarihsel işaretle veya güncel kayda yönlendir.
- Kanıt üretici ve doğrulayıcı arasındaki sözleşmeyi koruyarak eski kabul kimliklerini yeni R/K/A/D listesine eşleştir. Gerekirse şema sürümünü artır; eski kanıtların anlamını değiştirme.
- Her maddeyi `kod eksik`, `uygulandı/kabul bekliyor`, `dış girdi bekliyor`, `doğrulandı` durumlarından biriyle izle. Yayın dışına alınması ancak kapsam kararıyla mümkün olsun.

**Çıktı:** tekrarlanmayan iş listesi ve hangi kanıtın hangi işi kapattığını gösteren eşleme. Kod değişmediyse uygulama testlerini bu belge işi için yeniden çalıştırma.

### R1 — Yerelde hazır blog ve katalog paketini kapatma

1. `App.tsx`, `PublicContentPage.tsx`, `PublicSupplierMarketplace.tsx`, ilgili iki CSS ve yeni `blogPosts.ts` dosyasını birlikte incele.
2. Üç yazının Türkçe/İngilizce içeriğini, doğrudan URL ile açılmasını, yenilemeyi, geri dönüşü ve bulunamayan yazı durumunu kontrol et. Katalogda boş, dolu, arama sonucu yok, yükleniyor ve hata durumlarını ayır.
3. Masaüstü/dar mobil ve açık/koyu temada taşma, odak, okunabilirlik ve tıklanabilir bağlantıları denetle.
4. Geliştirirken hedefli kontrol; release push öncesinde tam yayın kontrolü. Sonrasında commit/push talimatıyla yayınla, aynı SHA'nın canlı sayfalarını doğrula.

**Bağımlılık:** yerel hazırlık için yok; yayın için commit/push talimatı. **Kapanış:** yazıların tamamı canlıda açılır, katalog gerçek API verisini gösterir. Gerçek ürün ekleme D2/B3'te kalır.

### K1 — Gerçek PayTR adaptörü

Mevcut temel: `IPaymentProvider`, `IMarketplacePaymentGateway`, Fake/Unconfigured uygulamaları ve `Systemcel.Api/Program.cs` kayıtları. Yeni bir ödeme mimarisi kurmak gerekmiyor.

1. Mevcut ödeme ve pazaryeri gateway arayüzlerinden ilerle; abonelik ve ürün siparişinin yaşam döngüsünü ayrı tut.
2. B1'de doğrulanan mağaza yeteneklerine göre ödeme başlatma, durum sorgusu, kısmi/tam iade ve satıcı transferi işlemlerini uygula. Sağlayıcıya giden tutarı sunucudaki sipariş/fiyat kaydından hesapla.
3. İç ödeme/sipariş kimliği ile sağlayıcı işlem referansını kalıcı eşleştir. Zaman aşımında sonucu belirsiz işlemi sorgulayarak çöz; körlemesine ikinci tahsilat veya transfer yapma.
4. Anahtarları sunucu sırrı olarak bağla; ham kart verisini uygulama kayıtlarına veya loglara alma. Yapılandırma eksikken ödeme kapalı kalsın.
5. Test mağazasında başarılı/başarısız ödeme, geri dönüş sayfasına gelmeyen kullanıcı, zaman aşımı ve yeniden deneme senaryolarını çalıştır.

**Şimdi yapılabilir:** arayüz bağlantıları ve hata/tekrar işleme tasarımı. **Gerekli girdi:** D1/B1 API erişimi ve mağaza yetkileri. **Kapanış:** gerçek test işlemi uygulamadaki tek ödeme kaydıyla eşleşir; Fake testi tek başına yeterli değildir.

### K2 — Sağlayıcı bildirimleri

Mevcut temel: `BillingApi` bildirim yolu, lifecycle olay işleme ve `OdemeOlayi` tekrar koruması. Eksik, gerçek PayTR sözleşmesinin bu temele bağlanması.

1. Tahsilat ve transfer bildirimlerini doğrulanmış sözleşmeye göre ayrı işle. İade veya ters ibraz için bildirim varmış gibi davranma; sunulmayan olaylarda desteklenen sorgu/rapor yolunu kullan.
2. İmzayı doğrula; sipariş, tutar ve işletme ilişkisini sunucu kaydından bul. İstemcinin bildirdiği işletme kimliğine güvenme.
3. Tekrar gönderilen, geç gelen ve sırası değişen olaylar için kalıcı tekrar işleme koruması kur. Kayıt/işleme garantisi sağlanmadan sağlayıcıya başarı dönme.
4. Geçersiz imza, başka işletmeye ait referans, miktar/tutar uyuşmazlığı ve işlem sırasında kesinti senaryolarını test et. İşlenemeyen olaylar inceleme kuyruğuna düşsün.

**Bağımlılık:** K1 ve B1 callback ayarları. **Kapanış:** aynı sağlayıcı sonucu kaç kez gelirse gelsin tek tahsilat, tek muhasebe etkisi ve tek hakediş oluşur.

### K3 — Günlük ödeme mutabakatı

Mevcut temel: `PaymentReconciliationService` ve `PaymentReconciliationHostedService`. Yeni bir zamanlayıcı yazmadan bu servislerin gerçek sağlayıcı kayıtlarını karşılaştırması sağlanacak. Farkta durdurma ve yönetici incelemesinin tam mevcut kapsamı uygulama başında doğrulanacak; sağlayıcı bağlantısının yokluğu bu mekanizmaların tamamının yokluğu anlamına gelmez.

1. Tahsilat, iade, aktarım talimatı, tamamlanan aktarım ve geri dönen para hareketini ayrı durumlar olarak karşılaştır.
2. Sağlayıcının günlük rapor/sorgusunu kalıcı bir işten çalıştır; kesilirse son başarılı noktadan devam et. Saat dilimi ve gün sınırını açık tanımla.
3. Eksik/gecikmiş/mükerrer işlem, tutar ve komisyon farkı için yönetici inceleme kaydı aç. Fark çözülene kadar etkilenen yeni aktarımı engelle; durdurmanın kapsamını görünür tut.
4. Yönetici düzeltmesini gerekçe, aktör, zaman ve önceki/sonraki durumla kaydet. Kayıt silerek fark kapatma.

**Bağımlılık:** K1–K2, D1 ücret/aktarım kuralları. **Kapanış:** test raporuyla uygulama toplamları eşleşir; kasıtlı fark yeni para hareketini durdurur ve çözümden sonra kontrollü devam eder.

### K4 — Abonelik, yenileme ve satış belgesi

Mevcut temel: `SubscriptionLifecycleService` ve `SubscriptionPriceProtectionService`; 30 günlük teslim kanıtı ve bildirim outbox'ları kodda mevcut. Bunlar yeniden yazılacak özellik değil, gerçek tahsilatla birlikte kabulü tamamlanacak kurallar.

1. Mevcut onay, dönem, iptal, kampanya ve fiyat koruması temelini kullan. İlk tahsilat, yenileme, başarısız ödeme, dönem sonu iptal ve iade senaryolarını PayTR'a bağla.
2. E-posta ve uygulama içi fiyat bildiriminin gerekli kanıtı oluşmadan zamlı tutarı sağlayıcıya gönderme. 30 gün sınırını saat kontrollü testle doğrula; yıllık peşin dönem ve kampanya bitişini kapsa.
3. D3'te seçilen yöntemle Systemcel'in kendi abonelik satış belgesini tahsilata bağla. Manuel yöntem seçilirse belge numarası/durumu, sorumlu iş kuyruğu ve müşterinin belgeye ulaşma yolu bulunsun. Otomatik yöntemde tekrar bildirim tek belge üretsin.
4. İade/iptal ile satış belgesi arasındaki bağlantıyı koru. Müşterinin uygulamada kendi adına kestiği faturayı Systemcel abonelik belgesi yerine sayma.
5. Kart saklama/Non3D yetkisi yoksa otomatik yenileme çalışıyor kabul etme. Alternatif kullanıcı onaylı ödeme akışı ancak ürün ve sözleşme kararıyla seçilsin.

**Bağımlılık:** D1, D3, K1–K3; bildirim kabulü B5. **Kapanış:** ödeme, abonelik dönemi, bildirim ve satış belgesi aynı referanslarla izlenir; iptal edilen dönemde yeni tahsilat oluşmaz.

### K5 — Sonradan değişen mal kabul kararını düzeltme

Başlangıç noktaları: `CashTracker.Infrastructure/Services/TedarikciPazaryeriService.cs` içindeki `ReceiveShipmentQrAsync`, `ReconcileRejectedReceiptStockAsync` ve stok defterinin `TersKayitKaynakIslemId` ilişkisi. Kabul sonrası karar düzeltme yolu dar taramada bulunmadı; ret stokunu uzlaştırma bunun yerine geçmiyor.

1. Kabul kaydını değiştirmek yerine gerekçeli bir düzeltme işlemi tasarla; ilk karar ve düzeltmenin bağlantısını koru.
2. Kısmi/tam düzeltmenin stok, cari, fatura ve hakediş etkisini aynı işlem bütünlüğünde ters kayıtlarla üret.
3. Para henüz aktarılmadıysa bekleyen hakedişi düzelt. Aktarılmışsa sağlayıcının izin verdiği iade/geri tahsil/mutabakat yolunu ayrı yönet; yerel kayıtla para geri gelmiş gibi gösterme.
4. Aynı düzeltmenin tekrar gönderilmesini ve iki yetkilinin eşzamanlı kararını kontrol et. Negatif stok, fazla iade veya ikinci hakediş oluşmasını engelle.
5. Ekranda ilk karar, düzeltme gerekçesi ve maddi etkiler birlikte görülsün. Ret stokunun iade/yeniden sevk uzlaştırmasını ayrı akış olarak koru.

**Şimdi yapılabilir:** bütün yerel muhasebe ve eşzamanlılık davranışı. **Bağımlılık:** gerçek para kısmında K1–K3/D3. **Kapanış:** kısmi, tam, aktarım öncesi/sonrası, tekrar ve eşzamanlı işlemler doğru bakiye verir; PostgreSQL üzerinde kritik yarış senaryoları geçer.

### K6 — Pilot e-belgesi ve eski referanslar

Mevcut temel: sipariş/sevkiyat/kabul belge referansları ve yerel fatura eşleme. `Program.cs` sevk irsaliyesi için `UnconfiguredSevkIrsaliyesiAdapter` kullanıyor; gerçek sağlayıcı burada tamamlanacak.

1. Sipariş → sevkiyat → kabul → fatura/cari → tahsilat/hakediş bağlantısının eski kayıtlardaki eksiklerini raporla.
2. Önce değişiklik yapmayan eşleştirme önizlemesi üret. Yalnız kesin eşleşmeleri yeniden çalıştırılabilir bir aktarım ile bağla; belirsizleri yönetici incelemesine ayır.
3. Seçilen gerçek e-belge sağlayıcısıyla pilotun gerektirdiği kısmi kabul/red, iade irsaliyesi/faturası ve durum takibini uygula. Taslak oluşturmayı resmî gönderim kabulü sayma.
4. Belge reddi, kısmi iade ve mükerrer cevapta stok/cari/ödeme referanslarını karşılaştır.

**Şimdi yapılabilir:** referans raporu ve güvenli eski veri eşleştirme. **Gerekli girdi:** e-belge sağlayıcısı/test erişimi ve D3 belge kararı. **Kapanış:** pilot belgeleri sağlayıcı durumu ve uygulama kayıtlarıyla eşleşir. Genel e-belge genişlemesi P1-6'da; pilot için gereken K6 açık P0'dır.

### K7–K9 — Operasyon kararından sonra tamamlanacak kod

| Madde | Çözüm adımları | Bağımlılık ve kapanış |
|---|---|---|
| K7: Çift onay | Tutar/risk eşiği için ayrı onay durumu; iki farklı yetkili; aynı kişinin ikinci onayını engelleme; karar/miktar değişince eski onayı geçersiz kılma; süre ve yetki değişimini denetleme. | D4 eşikleri ve roller. İkinci geçerli onaydan önce stok/hakedişin kesinleşme davranışı tanımlı ve test edilmiş olur. |
| K8: Çevrimdışı kabul | Sunucunun verdiği süreli/imzalı yetki bağlamıyla yerel taslak; sunucu zamanı ve sürüm denetimi; bağlantı gelince uzlaştırma; süresi dolan veya çakışan kaydı incelemeye alma. | D4 ihtiyaç, taslak süresi, cihaz/veri saklama kararı. Tekrar senkronizasyonda çift stok/hakediş yok; gerçek telefonda kopma/dönüş kanıtı var. |
| K9: İtiraz süreleri/risk | İtiraz durumları ve son yanıt tarihlerini kalıcı tutma; zamanlanmış hatırlatma/escalation; tekrarlanan sorunlar için açıklanabilir risk nedenleri; yönetici inceleme ve denetim kaydı. | D4 süreler/kurallar. Süre aşımı doğru kuyruğa düşer; bloke/limit/çift onay gerekçesi izlenir; otomatik kalıcı yaptırım verilmez. |

Bu üç madde mevcut listede P0. Pilot gereksinimi yazılı kararla daraltılmadıkça kendiliğinden ertelenmiş sayılmaz. Küçük/düzenli teslimatta tek onay koşulu da açık tanımlanmalı.

### A2–A4 — Kabul testlerinden çıkacak kod düzeltmeleri

- İki ayrı işletmede API, AI bağlamı ve dosya indirme sınırlarını hedefli testlerle doğrula. Ekranda veri gizlemek tek başına yetki kontrolü değildir.
- AI için düşük güven, öneriyi onaylama/reddetme, tekrar uygulama, kota ve sağlayıcı hatalarını kapsa. Dış modele giden veri maskeli kalırken cevapta adlar doğru işletmenin yerel eşlemesinden geri gelsin.
- Yenileme/fiyat olayını kendi akışından üret; mesaj şablonunu elle göndererek olayı kapatma. İşletme ve muhasebeci sohbet dosyalarında yetkisiz indirme ve yeniden denemeyi kapsa.
- Net kâr dönem menüsü, sıfır ödeme grafiği ve kabul ekranı düzeltmelerinin aday sürümde bulunduğunu doğrula; mevcut düzeltmeleri kanıtsız yeniden yazma.
- B4/B5/B7'de gerçek hata bulunursa yalnız o hatayı düzelt, uygun regresyonu çalıştır ve aynı tarayıcı senaryosunu yeniden dene.

### A5 — Tam kurtarma ve geri dönüş

1. Uzak yedek kurulumunu tekrar yapma. Son üç ardışık günlük paketin zamanını, bütünlüğünü ve saklama politikasını doğrula; eksik gün varsa nedeni gider, gerekli ardışık kanıtı tamamla.
2. Üretimden bağımsız bir makinede yalnız kurtarma girdileriyle veritabanı, uygulama dosyaları, şifre çözme anahtarları ve doğru uygulama sürümünü ayağa kaldır. Dış ödeme/posta işlerini kapalı tut.
3. Kayıt sayıları, örnek dosyalar, ilişkiler, oturum ve kritik okuma/işlem senaryolarını kontrol et. Sadece 73 tabloyu açabilmek tam kurtarma kabulü değildir.
4. Gerçek veri kaybı aralığı ve kurtarma süresini ölç; kararlaştırılan RPO/RTO hedefleriyle karşılaştır. 14 günlük uzak saklamayı doğrula.
5. Bir önceki doğrulanmış uygulama sürümüne şema uyumlu geri dönüşü izole ortamda prova et. Eski migration'a otomatik dönüş yapma; uyumsuzlukta ileri düzeltme/veri kurtarma yolunu kaydet.

**Gerekli girdi:** bağımsız makine/erişim ve Burak'ın kabul edeceği veri kaybı/kurtarma hedefi. **Kapanış:** başka makinede kullanılabilen uygulama, ölçülmüş süreler ve tekrar uygulanabilir kurtarma kaydı.

### A6 — Kalıcı izleme ve alarm kanıtı

1. Mevcut collector ve uygulama metriklerini VM dışında kalıcı hedefe aktar. Erişim ve maliyet kararı olmadan yeni ücretli servis seçme.
2. Sunucudaki monitoring/alert betiklerini aday sürümle karşılaştır; runbook'taki sinyallerin veri kaynağını ve eşiklerini bağla.
3. Readiness, 5xx, disk, yeniden başlatma, veritabanı bağlantısı ve yedek yaşı için eksik veri durumunu da izle.
4. B6'da ikinci bağımsız kanal/alıcıyı ayarla. Harcanabilir ayrı bir test hedefinde gerçek kesinti/düzelme yarat; VM'den bağımsız alarmın ulaştığını kanıtla. Canlı veritabanını durdurarak veya diski doldurarak test yapma.
5. VM'nin gerçekten kapanması senaryosu ayrıca gerekiyorsa bakım penceresi ve geri dönüş hazırlığıyla çalıştır; test hedefinin kanıtını canlı VM kesintisi gibi sunma.

**Kapanış:** dış metrik geçmişi var; birincil ve ikinci kanal alarm/düzelme kanıtı mevcut; VM içindeki SMTP tek alarm yolu değil. Telegram bu iş için yeniden açılmaz.

## 3. Computer use ile yapılacaklar

Bu bölüm tarayıcıdaki hesap/panel ayarlarını ve gerçek kullanıcı kabulünü kapsar. API veya CLI ile güvenilir biçimde yapılabilen alt adım o yoldan yürütülebilir. Tarayıcıdan form doldurmak sağlayıcı onayını, fiziksel depo kontrolünü veya hukuk kararını yerine getirmez.

| İş | Panelde uygulanacak adımlar | Bitti sayılması için |
|---|---|---|
| **B1 — PayTR erişimi (D1)** | Yetkili oturumda test/canlı durumu, kimlik doğrulama, API erişimi, pazaryeri transferi ve kart saklama/Non3D yetkilerini ayrı kontrol et. Test callback adreslerini K2 ile eşleştir. Eksik yetki/sözleşme soruları için tek talep taslağı hazırla. Anahtarları konuşmaya veya ekran kanıtına alma. | Yetki matrisi ve güvenli yapılandırma hazır; bilinmeyenler açıkça kaydedilmiş. Panelde görünmeyen ticari koşullar yazılı sağlayıcı yanıtı bekler. |
| **B2 — PayTR işlem kabulü (K1–K4)** | Gerçek test checkout'unu kullan; başarılı/başarısız ödeme ve dönüşü gör. İade/transfer sonuçlarını panelden uygulama kayıtlarıyla karşılaştır. Yenileme yetkisini ayrı doğrula. | İşlem referansı, tutar ve son durum iki tarafta aynı. Canlı para provası ancak onaylanan tutar/hesapla; test mağazası sonucu canlı para kanıtı değildir. |
| **B3 — Gerçek katalog ve inceleme hesabı (D2)** | Yetkili tedarikçinin gerçek şirket/ödeme/ürün/stok/fiyat/sevk/iade bilgilerini gir; yayımlamadan kontrol et. Anonim ziyaretçiyle ürün, fiyat ve satış/iletişim bağlantılarını aç. Ayrı inceleme hesabının girişini, sınırlı erişimini ve iş bitince iptalini doğrula. | Gerçek ürün anonim katalogda görünür; hesap müşteri verisine erişmez; inceleme tamamlandığında erişim kaldırılır. Mevcut kullanıcı kapasitesi aşılmaz. |
| **B4 — Clerk, roller ve AI (A2, P1-1)** | Kontrollü yeni kimlikle kayıt → işletme → kolay kurulum. İkinci işletmeyle veri ayrımı; üye daveti/rol değişimi/kaldırma, sahiplik devri. Açık sekmede rol kaldırma. AI'da onay/ret ve adların doğru dönüşü. | Ayrı tarayıcı oturumlarında beklenen erişim var; kaldırılan erişim API/dosya yolundan da çalışmaz; AI diğer işletmeden veri getirmez. |
| **B5 — Bildirim ve dosya (A3)** | Kontrollü alıcıyla yenileme/fiyat olayını başlat; uygulama içi kaydı ve posta kutusundaki teslimi karşılaştır. İşletme ve muhasebeci rollerinde dosya yükle/indir, erişimi kaldırıp tekrar dene. | Doğru alıcıya tek mantıksal bildirim, olayla eşleşen teslim kaydı ve yetkili dosya erişimi. Spam/teslim sorunu görünür. Üçüncü kişiye gönderim açık talimatla. |
| **B6 — Oracle/izleme panelleri (A5–A6)** | Uzak paket geçmişi/saklama, metrik hedefi, dış HTTPS izleyici, ikinci kanal/alıcı ve alarm kurallarını kontrol et. Test kesintisi/düzelmesini panel ve alıcı tarafında karşılaştır. | Panel ayarı ile kodun beklediği hedef aynı; teslim ve toparlanma kaydı var. Paket listesi tam kurtarma provası yerine geçmez. |
| **B7 — Son arayüz kabulü (A4, R1)** | Açık/koyu tema; masaüstü ve dar mobil; uzun liste/kaydırma; hover/focus/seçili/disabled; yükleniyor/boş/hata; giriş/çıkış/yetki değişimi. Klavyeyle Tab, Enter, Escape; modal kapanınca odak dönüşü. | Kırpılan kenar/gölge, düşük kontrast, taşan kontrol ve çalışmayan eylem yok. Kontrol edilen SHA/ekran/durum kaydedilmiş. |
| **B8 — Yayın sonrası doğrulama (R1, A7)** | CI/deploy sonucunu aynı aday için izle; canlı blog/katalog ve kritik kullanıcı akışlarını tekrar aç. Sağlayıcı modu ve pilot izin listesini kontrol et. | CI SHA = deploy SHA = canlı SHA; public smoke ve gerçek oturum kontrolü başarılı. Yalnız yeşil CI ekranı yeterli değil. |

B1–B8'de oturum açma, MFA veya kimlik doğrulama kullanıcı müdahalesi isteyebilir. Bunlar yalnız ilgili adımı bekletir; bağımsız kod işleri devam eder. Bu oturumdaki computer use tarayıcı yüzeyleriyle sınırlıdır; fiziksel iPhone/Safari, kamera ve depo koşullarını B7 emülasyonuyla kapanmış sayamayız.

## 4. Kod veya computer use ile tek başına kapatılamayanlar

| Girdi | Kim sağlar? | Gereken somut sonuç | Açtığı işler |
|---|---|---|---|
| **D1 — PayTR ticari kapsamı** | Burak + PayTR | Transfer onayı/bekletme, 7 günlük valörün kesin takvimi, geç onay ve üst süre, alt satıcı belgeleri, iade/chargeback/bloke/rezerv ve tekrarlayan ödeme yetkileri için yazılı kapsam. | K1–K4, B1–B2. Teknik doküman mağazaya tanınan hakkı kanıtlamaz. |
| **D2 — Gerçek tedarikçi/alıcı/depo** | Burak + yetkili tedarikçi/depo | En az 1 tedarikçi, 1 alıcı, 1 depo; gerçek ürün ve fiyatlar; satış/yayın yetkisi; sevk/iade bilgileri; kontrollü hesaplar. | B3, A1. Sahte satış ilanı ekleyerek tamamlanmaz. |
| **D3 — Hukuk ve belge yöntemi** | Burak + mali müşavir/hukuk | Gerekliyse MERSİS/KEP bilgileri; pazaryeri/satış/iade metni ve PSP sözleşmesi kabulü; abonelik satış belgesi yöntemi, komisyon faturası ve vergi uygulaması kararı; pilot e-belge erişimi. | K4, K6, A7. Bunlar bu planda hukuki hüküm olarak belirlenmiyor. |
| **D4 — Pilot operasyonu** | Burak + depo/operasyon + hukuk/finans | Kategoriler, yetkililer, çift onay eşiği, çevrimdışı gereksinim/taslak süresi, itiraz/inceleme süreleri; K7–K9'un pilot kapsamı. | K7–K9 ve A1. Karar çıkmadan eşik uydurulmaz veya P0 düşürülmez. |
| **D5 — AI veri işleme kabulü** | Burak + hukuk; teknik akış Codex | Gerçek API sözleşmesi, işleme/saklama/aktarım durumu ve alt işleyen metinlerinin sürümlü kabulü. | AI'nın ücretli yayın kabulü. Sohbet hesabı ayarı API sözleşmesi yerine geçmez. |
| **Fiziksel cihaz ve depo** | Burak veya pilot kullanıcı | Cihaz/sürüm kaydı; gerçek kamera/QR, Safari, klavye, dosya seçme, ağ kopması/dönüş ve sevk/kabul denemeleri. | A1, A4, gerekiyorsa K8. |
| **Kurtarma ve alarm tercihleri** | Burak | Kabul edilebilir veri kaybı (RPO), toparlanma süresi (RTO), bağımsız makine ve kalıcı metrik hedefi; ikinci alıcı/kanal. | A5–A6. 24 saat/4 saat gibi eski öneriler verilmiş taahhüt sayılmaz. |
| **Ücretli yayın kararı** | Burak; teknik/operasyon/hukuk kanıtlarıyla | Açık P0 kalmadığı, canlı para provası ve destek sorumluluğunun değerlendirildiği yayın kaydı. | A7. |

Codex eksik veri şablonlarını ve sağlayıcıya sorulacak soruları tek dosyada hazırlayabilir, mevcut belgelerden alanları doldurabilir ve panelde uygulayabilir. Sağlayıcıya/tedarikçiye mesaj gönderme bu plan talebinin parçası değildir; gönderim ayrıca açık talimat gerektirir.

## 5. Gerçek pilot nasıl yürütülecek? — A1

1. D2/D4 verileriyle izin listesini 1 gerçek tedarikçi, 1 alıcı ve 1 depoyla sınırla. Depo/şube rollerini ve kategoriye göre lot/seri, tartım toleransı, sıcaklık ve belge kurallarını ayarla.
2. Aynı siparişte tam ve kısmi sevkiyat, birden fazla sevkiyat ve miktar farkını dene. Sürücünün teslim beyanının tek başına hakediş açmadığını doğrula.
3. Yetkili alıcıyla kabul/red ve kanıt yüklemesi yap; itiraz, yönetici kararı, yeniden sevk, iade ve kabul düzeltmesini çalıştır.
4. Her adım sonunda stok miktarı, cari bakiye, fatura, komisyon, net hakediş ve sağlayıcı durumunu birlikte karşılaştır. Tutarları yalnız ekran görüntüsünden değil kayıt/rapor üzerinden de uzlaştır.
5. Fiziksel telefonda kamera/QR, Safari ve ağ kopması/dönüşünü dene; aynı isteği tekrar göndererek çift etki olmadığını gözle.
6. Katılımcıların gerçekten tamamladığı görevleri, hataları ve destek ihtiyacını kaydet. Kod veya iş kuralı değişirse ilgili akışı aday sürümde yeniden doğrula.

**Çıkış koşulu:** veri karışması, çift tahsilat/hakediş, açıklanamayan muhasebe farkı veya hesaba erişememe gibi açık kritik hata yok; kapsam içindeki görevler ilgili rollerde tamamlanmış; A2–A6 kanıtları hazır. Pilot süresi/katılımcı artışı operasyon kararıdır; eski bir planın önerdiği süre otomatik taahhüt değildir.

## 6. Yayın sonrası işler de kaybolmayacak

P1/P2 kapsamı ilk ücretli yayını kendiliğinden büyütmez. Yayında açıkça vaat edilen bir işlevin gerçek hatası bulunursa ilgili düzeltme P0'a alınır.

| Madde | Kodla çözüm | Tarayıcı / dış girdi ve kapanış |
|---|---|---|
| **P1-1 Üyelik/sahiplik** | Mevcut modeli koru; gerçek kabulde bulunan yetki sorununa regresyon ekle. | B4'te davet/rol/kaldırma/devir; açık sekme ve dosya erişimi doğrulanır. Pilotun zorunlu rolleri A2'de önce kapanır. |
| **P1-2 Hata/kullanım takibi** | İstek kimliğini kalıcı hata takibine bağla; aktivasyon, dönüşüm, API süreleri ve destek olaylarını ölç. Bundle/CSS'i ölçülen darboğaza göre düzelt. | Hedef servis/erişim seçimi; dashboard ve kontrollü hata görünürlüğü. A6 altyapı metrikleriyle aynı bağlantıyı yeniden kurma. |
| **P1-3 Banka eşleştirme** | Gerçek dosyalarda parser ve eşleştirmeyi doğrula; karar verilirse kısmi/toplu eşleştirmeyi ekle; tekrar işlem korumasını sürdür. | Anonimleştirilmiş gerçek banka dosyaları ve kapsam kararı. Yanlış öneri, mükerrer satır ve para birimi uyuşmazlığı doğru ele alınır. |
| **P1-4 Eski veri aktarımı** | Mevcut önizleme/alan eşleme/taslak/tekrar korumasını gerçek formatlara uyarla; masaüstü aracı sürümle, dağıtım ve imza sürecini tamamla. | Gerçek kaynak dosyalar, gerekiyorsa imza sertifikası. Yarım aktarım devam eder; başarılı satır tekrar eklenmez. |
| **P1-5 Stok maliyeti** | Hareketli ortalama ve mevcut depo/rezervasyon/transfer/sayım temelini gerçek mutabakat farklarına göre tamamla. | Gerçek ürün/konum/maliyet verisiyle ekran ve defter toplamları karşılaştırılır. K5 muhasebe işi tekrar açılmaz. |
| **P1-6 Genel e-belge** | Seçilen kapsamda UBL-TR, e-Fatura/e-Arşiv, durum/webhook/sorgu, iptal/itiraz ve mutabakatı gerçek adaptörde tamamla. | Sağlayıcı sözleşmesi/test erişimi ve yetkili belge kabulü. Pilotun dar K6 kapsamının üzerine eklenir. |
| **P1-7 İletişim incelemesi** | Mevcut kesin engel/belirsiz insan incelemesi akışını yanlış pozitiflerle düzelt; 30 dakika kuralını saat kontrollü doğrula. | Anonim gerçek mesaj seti ve yönetici kararı. Kapalı muhasebeci pazaryeri bayrağını test için yanlışlıkla canlı açma. |
| **P2 Sektör/NACE bildirimleri** | Doğrulanmış kaynak toplama, sürüm/tarih, kategori eşleme ve bildirim tercihleri. | Kaynak doğruluğu ve uzman içerik kontrolü; yanlış/eski bildirim düzeltme yolu. |
| **P2 Ek maliyet/100 bin+ hareket** | İstenen maliyet yöntemlerini ayrı tanımla; PostgreSQL'de gerçekçi veri/indeks/sorgu yükünü ölç, darboğazı düzelt. | Gerçek hacim ve kabul süresi hedefi. Mevcut SQLite testini üretim performansı sayma. |
| **P2 Geliştirici platformu** | Mevcut salt okunur v1 üzerinde OAuth, webhook aboneliği, imza/tekrar deneme/revoke ve portalı ayrı paket olarak tasarla. | Entegratör senaryoları ve kontrollü uygulama kaydı; scope ve revoke kabulü. |
| **P2 Şube/kur raporları** | Konsolidasyon kuralları, dönem kuru/kur farkı ve para birimi raporlarını tanımla; hesaplama ve izlenebilirliği uygula. | Mali müşavirden rapor kuralları ve gerçek örnekler. ECB kurunu çekebilmek rapor kabulü değildir. |
| **P2 Sağlık skoru/dönem sonu/SLA** | Ölçülebilir sinyaller, görev şablonları, zamanlanmış işler ve yöneticinin düzeltebildiği kurallar. | İşletme/muhasebeci görev ve destek süreleri kararı; örnek ay kapanışıyla kabul. |
| **Telegram — ertelendi** | Kullanıcı yeniden istediğinde mevcut kod üzerinden eski global komutları işletme bağlamına taşıma. | Gerçek bot sohbeti/AI kabulü. Şimdiki çalışma sırasına alınmaz. |

## 7. Uygulama sırası ve bağımlılıklar

| Aşama | Kod/komut satırı | Aynı sırada ilerleyebilen panel/dış iş | Aşamadan çıkış |
|---|---|---|---|
| **1. Açık yerel paketi toparla** | C0, R1 son inceleme ve hedefli kontroller; mevcut değişiklikleri koru. | B1 PayTR yetki envanteri; D2–D5 bilgi listeleri. | R1 yayın adayı ve gerçek dış engeller belli. |
| **2. Beklemeden geliştir** | K5; K6 referans önizlemesi; K1–K3 altyapısı; A2/A3 hedefli testler; A5/A6 hazırlığı. | Gerçek tedarikçi bilgileri, operasyon kararları, metrik/kurtarma hedefi. | Sağlayıcı olmadan tamamlanabilen kod ve kanıt araçları hazır. |
| **3. Sağlayıcıyı bağla** | K1 → K2 → K3; K4; K6 sağlayıcı bağlantısı. | B1/B2 test işlemleri; D1/D3 eksik yetki/kararları. | Test ödemesi, transfer/iade ve muhasebe mutabakatı aynı referanslarda doğru. |
| **4. Pilot kurallarını tamamla** | D4'e göre K7–K9; bulunan hatalar; A5 tam kurtarma ve A6 dış metrikler. | B3 gerçek katalog, B4/B5 hesap/posta/dosya, B6 alarm kabulü. | Gerekli operasyon kodu ve işletim kanıtları hazır. |
| **5. Gerçek kullanım** | Bulunan gerçek hataları gider, yalnız ilgili akışı tekrar doğrula. | A1 dar pilot, fiziksel iPhone/depo, B7 arayüz matrisi. | Açık kritik hata ve açıklanamayan para/stok farkı yok. |
| **6. Ücretli yayın** | CI ile aynı release kontrolleri; istenen commit/push; aynı SHA için CI/deploy/public smoke. | B8 canlı oturum, kontrollü gerçek para/iade kabulü ve Burak'ın A7 kararı. | Aday, canlı sürüm, para kabulü ve geri dönüş kaydı tutarlı. |
| **7. Yayın sonrası** | P1'i gerçek destek/hata etkisine göre; P2'yi ayrı ürün paketleriyle yürüt. | Gerçek kullanıcı verisi ve gerektiğinde ürün kararları. | İlk yayın kapsamı sürekli yeni özelliklerle genişletilmez. |

Aynı sırada ilerlemek çok sayıda ajan veya Windows'ta eşzamanlı test süreci demek değildir. Bir dar kapsamlı worker arama/test/basit düzenleme yapabilir; ödeme, muhasebe, güvenlik ve son karar ana ajanda kalır. `npm test` ile .NET Release kontrolleri sırayla çalışır.

**Kritik yol:** D1 → K1/K2/K3/K4 → B2 → A1/A7. Buna D2/K5/K6 ve D4'te gerekli görülen K7–K9 pilot öncesi katılır. A5/A6 paralel hazırlanır ama ücretli yayından önce kapanır.

Takvim için henüz güvenilir toplam gün sayısı yok: mağaza yetkileri, e-belge erişimi, D4 kapsamı ve gerçek pilot müsaitliği açık. Bunların bekleme süresi yazılım süresi gibi gösterilmeyecek. Aşama 1 sonunda her paket için aktif geliştirme tahmini ve dış bekleme ayrı yazılacak; API testine ulaşmadan kesin bitiş tarihi verilmeyecek.

## 8. Her işin kapanış kaydı

Her kabul kaydında şu bilgiler bulunur: mevcut madde ID'si, aday SHA, ortam, UTC zamanı, anonim rol/senaryo, beklenen sonuç, gerçek sonuç, kanıt yolu, kalan engel. Ödeme için sağlayıcı referansı; cihaz için model/sürüm; kurtarma için paket/checksum ve ölçülen süre eklenir. Sır, kart bilgisi, token veya müşteri belgesi açık repoya konmaz.

- Kod eksikliği: uygulanmış değişiklik ve davranışı doğrulayan en küçük anlamlı test.
- Panel ayarı: ilgili hesap/ortamın ayarı ve gerçekten çalıştığını gösteren sonuç.
- Gerçek kabul: gerçek hesap/sağlayıcı/cihaz kanıtı; mock/emülasyon ayrı etiketlenir.
- Dış karar: sorumlusu, karar tarihi, kapsadığı sürüm ve metin.
- Yayın: aynı aday için başarılı CI + deploy + canlı SHA + public smoke + gerekli kullanıcı akışı.

Bir hata çıkınca önce başarısız adım/log/kanıt incelenir, belirli hata yerelde yeniden üretilir ve nedeni düzeltilir. Deneme amaçlı tekrar CI çalıştırma veya yeni commit gönderme kapanış yöntemi değildir. Devam eden adayın CI/deploy sonucu alınmadan yeni release push yapılmaz.

## 9. Uygulama ve yayın kontrol komutları

Kaynak: `.github/workflows/ci.yml`, `Systemcel.Web/package.json` ve `AGENTS.md`. Aşağıdaki komutlar planın kontrol listesidir; 28 Eylül'de çalıştırılanların sonuçları Bölüm 11'de kayıtlıdır. Geliştirme sırasında ilgili hedefli test yeterli; release push öncesinde CI ile aynı kapsam gerekir. İşleme başlarken workflow değişmişse komutlar yeniden eşleştirilir.

`Systemcel.Web` dizininde, sırayla:

```powershell
npm ci
npm run lint
npm run typecheck
npm test
npm run build
npx playwright install chromium webkit
npm run test:e2e
npm audit --audit-level=high
```

Depo kökünde, web testleri bittikten sonra:

```powershell
dotnet restore .\CashTracker.sln /p:Configuration=Release /p:RestoreUseStaticGraphEvaluation=true
dotnet list .\CashTracker.sln package --vulnerable --include-transitive
dotnet build .\CashTracker.sln --no-restore --configuration Release --disable-parallel /p:BuildInParallel=false
dotnet test .\CashTracker.Tests\CashTracker.Tests.csproj --no-build --configuration Release --logger 'console;verbosity=minimal'
```

Paket taramasının yalnız çıkış koduna bakma; CI'daki gibi açık paket bulgusunu da başarısızlık say. `.NET` ve Vitest'i eşzamanlı çalıştırma; `maxWorkers: 1` korunur.

CI'nın PostgreSQL hizmeti/ortamıyla EF pending-model-changes ve migration kontrolü, `FullyQualifiedName~PostgreSql_` testleri, API health/public plan kontrolleri ayrıca çalışır. Normal `.NET` test komutunun geçmesi bu ortamın çalıştığını kanıtlamaz. Tam geçmiş gitleaks kontrolü, Docker build ve CI güvenlik kontrolleri de release kanıtına dahildir.

Operasyon smoke'ları Bash/Python ortamında:

```bash
bash deployment/oracle-free/tests/backup-offsite-smoke.sh
bash deployment/oracle-free/tests/collect-monitoring-smoke.sh
python3 deployment/oracle-free/tests/alert-monitoring-smoke.py
bash deployment/oracle-free/tests/deploy-bundle-smoke.sh
```

Yayın kanıtında gerçek aday SHA ile mevcut araçlar kullanılır; C0 eşlemesi düzeltilmeden eski kimliklere yeni anlam yüklenmez:

```powershell
pwsh ./scripts/New-SystemcelReleaseEvidence.ps1 -CandidateSha <40-karakter-SHA> -EnvironmentName production -OutputPath ./artifacts/release-evidence.json
pwsh ./scripts/Test-SystemcelReleaseEvidence.ps1 -Path ./artifacts/release-evidence.json
pwsh ./scripts/Test-SystemcelPublic.ps1 -BaseUrl https://systemcel.app -CandidateSha <40-karakter-SHA> -EnvironmentName production -EvidencePath ./artifacts/public-smoke.json
```

Bu son örneklerdeki `<40-karakter-SHA>` çalıştırmadan önce gerçek commit ile değiştirilir. Şablon doğrulaması, bekleyen senaryoları yapılmış saymaz. Public smoke da gerçek hesap/pilot kabulünün yerine geçmez.

## 10. PayTR için doğrulanan teknik kaynaklar

28 Eylül incelemesinde resmî doküman pazaryeri transferinin talimat ve sonuç aşamalarını ayrı tarif ediyor: [Platform Transfer Talebi](https://dev.paytr.com/platform-transfer-talebi). Bu ayrım K1–K3 tasarımına yansıtılmalı.

[iFrame API 1. adım](https://dev.paytr.com/iframe-api/iframe-api-1-adim) `merchant_oid` için en fazla 64 karakterlik alfa sayısal kimlik, sunucudan token isteği ve iFrame gösterimini tarif ediyor. [iFrame API 2. adım](https://dev.paytr.com/iframe-api/iframe-api-2-adim) sonuç bildirimini form POST olarak, imza doğrulamasını ve yalnız `OK` yanıtını tarif ediyor; başarılı tarayıcı dönüşünü tahsilat kanıtı saymıyor. Mevcut `PaymentCheckoutRequest` gerekli alıcı IP'sini sağlayıcıya taşımıyor, `BillingApi` webhook'u JSON gövdesi bekliyor, web arayüzü checkout URL'sine doğrudan yönlendiriyor ve `IMarketplacePaymentGateway.CollectAsync` eşzamanlı başarılı tahsilat varsayıyor. Bu sözleşmeler PayTR test mağazasıyla uyarlanmadan sağlayıcıyı DI'ya bağlamak güvenli değil; ortak hesap anahtarını/iade/aktarım durumunu kalıcı eşleştiren tasarım ve gerçek callback kabulü K1–K3'ün parçasıdır.

Kayıtlı kartla tekrarlayan ödeme Non3D çalışıyor ve mağazada bu yetkinin açık olmasını gerektiriyor: [Kayıtlı Kart Tekrarlayan Ödeme](https://dev.paytr.com/direkt-api/kart-saklama-api/kayitli-kart-tekrarlayan-odeme). Genel dokümanın bulunması Systemcel mağazasındaki yetkiyi kanıtlamaz.

Diğer teknik başlangıç kaynağı: [Durum Sorgu API](https://dev.paytr.com/durum-sorgu). Uygulama sırasında kullanılacak her alan/olay/rapor sözleşmesi ayrıca doğrulanacak; bu plan PayTR'ın mağazaya özel ticari koşullarını varsaymıyor.

## 11. 28 Eylül uygulama kaydı

| İş | Yapılan ve kanıt | Kalan |
|---|---|---|
| C0 | Sürüm kanıtı şablonu v2'ye geçirildi. Güncel R1/K1–K9/A1–A7/D1–D5 kendi anlamıyla ayrı bekleyen kapı olarak yer alıyor. Eski v1 dosyaları tarihsel şemasıyla doğrulanıyor ve eski K numaraları için uyarı veriliyor. Kimlik eşlemesi doğrulayıcıda sabitlendi; 5 hedefli Pester testi geçti. Ürün kapsamı ve release/monitoring rehberindeki yanlış `Fake` canlı durumu düzeltildi. | Gerekli dış kabul gözlemleri henüz bekliyor. |
| R1 yerel hazırlık | Web lint, typecheck, build; masaüstü blog 3/3 ve katalog 2/2; dar mobil Chromium/WebKit blog+katalog 10/10. 320 px blog/yazı ve açık/koyu boş katalog ekranları görsel incelendi; yatay taşma yok. | Tam release testi, istenen commit/push, CI/deploy ve canlı SHA/yazı doğrulaması. Gerçek katalog D2. |
| Mevcut canlı kamu yüzeyi | 28 Eylül `Test-SystemcelPublic.ps1` landing, güvenlik başlıkları, live/ready, PostgreSQL, plan/config, API sınırı ve CORS kontrollerini geçti. Yerel ve GitHub `main` aynı `8cc986c` SHA'sında. | Oracle deploy SHA'sı 28 Eylül'de yeniden okunmadı; gerçek oturum/ödeme kabulü yapılmadı. |
| B1 PayTR paneli | Resmî PayTR sitesindeki Mağaza Paneli giriş ekranına ulaşıldı. | Tarayıcıda oturum bulunmadığı için mağaza yetki/kapsamı okunamadı; kullanıcı oturum açınca sürdürülecek. |
| D4 karar sahibi | Burak, pilot kurallarını depo sorumlusu ve mali müşavirle belirleyeceğini bildirdi. [Karar formu](2026-09-28-pilot-operasyon-karar-formu.md) hazırlandı; boş değerler onaylanmış kural sayılmıyor. | Çift onay eşiği, çevrimdışı taslak süresi, itiraz/inceleme kuralları yazılı değer olarak açık. |
| K6 eski referans önizlemesi | Salt okunur [pazaryeri referans denetimi](../../scripts/marketplace-reference-audit.sql) sevkiyat, kabul, ödeme dağıtımı ve aktarım bağlantılarındaki yapısal tutarsızlıkları kişi/işlem referansı basmadan sayıyor. | PostgreSQL üzerinde izole snapshot veya salt okunur rolle henüz çalıştırılmadı; belirsiz eski kayıtların elle incelemesi ve gerçek e-belge bağlantısı açık. |

Web için tam yayın komutları 28 Eylül'de sırayla geçti: `npm ci`, lint, typecheck, build, 161 Vitest, tam Playwright (367 geçti, 493 koşul gereği atlandı), `npm audit` (0 bulgu). .NET restore, bağımlılık taraması, Release build ve testleri de sırayla geçti: 389 test geçti, 2 PostgreSQL eşzamanlılık testi veritabanı yokluğundan atlandı, 0 başarısızlık. PostgreSQL'e bağlı CI kontrolleri ve gerçek dağıtım henüz tamamlanmadı.

K5'in yerel kodu ve gerçek ödeme bağlantısı bu kayıtla tamamlanmış sayılmaz. Kabul edilen miktar stok/fatura/cariye, uygun durumda ise sağlayıcı aktarımına etki ediyor; bu yüzden düzeltme, ayrı ters kayıt ve dış para durumu uzlaştırmasıyla uygulanacak. PayTR yetkisi ve gerçek işlem sözleşmesi gelene kadar aktarılmış paranın geri alınmış olduğu varsayılmayacak.
