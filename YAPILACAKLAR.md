# Systemcel — Öncelikli Yapılacaklar

> Son güncelleme: 21 Eylül 2026
> Durum kontrolü: `7ea166e` yerel sürümü, mevcut kod/testler ve önceki pilot kayıtları. Bu düzenlemede testler yeniden çalıştırılmadı; canlı ortam yeniden doğrulanmadı.
> `[x]` yalnız belirtilen kapsamın tamamlandığını gösterir. Kodun ve testin bulunması, canlı kabulün tamamlandığı anlamına gelmez.
> 21 Eylül kullanıcı kararı: tedarikçi pazaryeri, sevkiyat ve mal kabul ilk ücretli yayına dahildir. Satış belgesi, canlı sürüm/geri dönüş ve Jev gerçek kullanım kontrolleri listeye eklendi.
> Kural: Tamamlanan paketler bu dosyada ayrıntılı günlük olarak tutulmaz; yalnız kısa özet bırakılır.
> PayTR başvurusu yapıldı ve değerlendirmede; sağlayıcı erişimi, testler ve gerçek tahsilat kabulü bekliyor.
> Ayrıntılı kapanış planı: [`docs/design/2026-09-11-yayin-ve-yazilim-kapanis-plani.md`](docs/design/2026-09-11-yayin-ve-yazilim-kapanis-plani.md)

## 1. P0 — Yayın öncesi temel işler

### 1. Aday sürüm, hesap ve temel kullanım kabulü

- [ ] Aday commit, başarılı CI ve canlı sürümü eşleştir; önceki doğrulanmış sürüme şema uyumlu geri dönüşü izole ortamda prova et. Otomatik yayın kodu hazır; çalışmış olması ayrıca kanıtlanmalı.
- [ ] Sıfırdan yeni kimlikle Clerk kayıt → provision → kolay kurulum smoke testi çalıştır.
- [ ] Canlı SMTP'yi yapılandır; doğrulanmış göndericiden kontrollü gelen kutusuna genel ve fiyat/yenileme bildirimi teslimini kanıtla.
- [ ] Maskeli DeepSeek istemcisinin aday sürümde bulunduğunu ve gerçek oturumla işletmeye bağlı asistan yanıtını doğrula; başka işletmenin verisinin yanıta karışmadığını kontrol et.
- [ ] Jev önerilerini gerçek kullanıcı senaryolarında doğrula: düşük güvenli öneriyi incelemeye yönlendirme, kullanıcı onayı, yanlış öneriyi reddetme ve tekrar işlemde mükerrer kayıt oluşmaması. Yeni karar akışları için kod/test mevcut; canlı kabul açık.
- [ ] İşletme ve muhasebeci sohbetlerinde kontrollü dosya gönderme/indirme kabulünü tamamla; dosya ve hedef kullanıcı onayı bekliyor.
- [ ] Kritik akışlarda kalan klavye sırası, focus, Escape ve boş/yükleniyor/hata durumlarını doğrula. Plan penceresinin odak kapanı ve geri dönüş testi mevcut; kalan kapsamı tamamla.
- [ ] Fiziksel iOS/Safari smoke'unu sınırlı pilot sırasında çalıştır; WebKit emülasyonunu gerçek cihaz kanıtı sayma.

### 2. Yedek, izleme ve veri işleme

- [ ] Hazır `rclone crypt` akışını bağımsız nesne depolamaya bağla; üç ardışık uzak yedek ve yalnız uzak kopyadan bağımsız restore ile RPO/RTO'yu ölç.
- [ ] Hazır Oracle metric collector'ını kalıcı izleme hedefine bağla; VM dışı HTTPS probe ile alarm ve düzelme mesajlarını iki kanalda doğrula.
- [ ] DeepSeek hesabında model geliştirme için veri kullanımını kapat; sağlayıcıyı, Çin'de veri işleme/saklama ihtimalini ve yurt dışı aktarım dayanağını alt işleyen/KVKK metinlerinde hukuk onayıyla güncelle.

### 3. Ücretli yayın — sağlayıcı ve belge doğrulamaları

Yasal hazırlık kaydında işe başlama tarihi 14 Eylül 2026. PayTR başvurusu yapıldı; erişim ve gerçek ödeme kabulü açık.

- [ ] Sunulan belgelerde bulunmayan MERSİS/ticaret sicil ve KEP bilgisinin şahıs işletmesi için gerekliliğini doğrula; varsa yasal metinlere ekle.
- [ ] PayTR başvuru sonucunu ve mağazaya tanımlanan ödeme yeteneklerini doğrula; test mağazası/erişim açıldıktan sonra gerçek sağlayıcı adapter'ını ve sandbox sözleşme testlerini tamamla.
- [ ] Canlı checkout, imzalı webhook, yenileme, başarısız tahsilat, iade ve mutabakatı gerçek sağlayıcıda doğrula; 30 günlük fiyat korumasının gerçek tahsilat yolunda da uygulandığını kanıtla.
- [ ] Systemcel'in kendi abonelik satış belgesi sürecini netleştir: manuel veya otomatik düzenleme yöntemini seç; tahsilat–belge referansı, müşteriye sunma ve iade/iptal bağlantısını doğrula. Uygulamada müşterilerin kestiği faturalar bu işten ayrıdır.
- [ ] Tedarikçi ödemeleri için sağlayıcının alt üye işyeri, bloke ve kısmi hakediş desteğini doğrula; abonelik başvurusunu pazaryeri ödeme yetkisi olarak kabul etme. Ayrıntılı kabul maddeleri aşağıdaki tedarikçi bölümünde.

## 2. P0 — Tedarikçi pazaryeri, sevkiyat ve mal kabul

Uygulama sırası: **sağlayıcı yetenekleri → depo yetkileri ve kabul kaydı → kısmi stok/cari/fatura → kısmi hakediş ve itiraz → ekran/bildirim → entegrasyon testleri ve sınırlı depo pilotu**. Sağlayıcı sonucu beklenirken veri modeli, arayüz ve Fake testleri ilerleyebilir; gerçek para kabulü kapanmış sayılmaz.

Mevcut Fake ödeme akışında tam kabul için muhasebe/hakediş kodu mevcut; ret geldiğinde ilgili tedarikçi siparişinin hakedişi blokeleniyor. Gerçek PSP kabulü ile kalem bazlı kısmi aktarım ve muhasebe işleri açık. Kod/test kanıtı: [tedarikçi servisi](CashTracker.Infrastructure/Services/TedarikciPazaryeriService.cs), [modeller](CashTracker.Core/Models/PazaryeriSiparisModels.cs), [servis testleri](CashTracker.Tests/TedarikciPazaryeriServiceTests.cs), [arayüz](Systemcel.Web/src/screens/tedarikci-pazaryeri/TedarikciPazaryeriSayfasi.tsx).

### Sabit ürün kararları

- Klasik kargo takibi zorunlu olmayacak; tedarikçinin kendi aracı, distribütör, 3PL, soğuk zincir ve bölge deposu aynı sevk modeliyle desteklenir.
- Tedarikçinin veya sürücünün “teslim ettim” beyanı tek başına hakediş açmaz; hakedişin kaynağı alıcının yetkili depo/şube kullanıcısının dijital mal kabul kaydıdır.
- Mal kabul sipariş bazında değil kalem ve miktar bazında yapılır; kabul edilen miktar stok, fatura, cari ve hakedişe yansır.
- Eksik, fazla, hasarlı, yanlış, kalite reddi ve sıcaklık/parti uyuşmazlığı ayrı nedenler olarak tutulur.
- Güvenli ödeme yalnız lisanslı PSP'nin alt üye işyeri/blokeli hakediş modeliyle çalışır; para Systemcel hesabında tutulmaz.
- PSP hazır değilse vadeli/cari akış açıkça ayrı bir ödeme seçeneğidir; güvenli ödeme gibi sunulmaz.

### Sipariş ve sevk modeli

- [x] Kalem miktarlarından kısmi/tam sevk ve kısmi/tam kabul durumları; ret durumunda itiraz geçişi uygulandı.
- [x] `Sevke hazır` / `Mal kabul bekliyor` durumları, arayüz görünürlüğü ve geçiş zinciri tamamlandı.
- [x] Tek siparişe birden fazla sevkiyat; sevkiyat başına araç, çıkış deposu ve planlanan teslim tarihi desteği eklendi.
- [x] Belge numarası, plaka, sürücü/taşıyıcı, çıkış deposu, planlanan teslim zamanı ve açıklama alanları eklendi.
- [x] Sevkiyatın e-İrsaliye UUID/sevk tarihi, varış deposu ve randevu alanları; API, arayüz ve kalıcı veri modeliyle tamamlandı.
- [x] Lot, son kullanma tarihi, sıcaklık aralığı ve miktarı etiketlere bölme temeli eklendi.
- [x] Seri numarası, ağırlık ve palet/koli alanları ile temel aralık doğrulamaları tamamlandı.
- [ ] Ürün kategorisine göre zorunlu sevk alanı kurallarını tanımla.
- [x] Sevkiyat QR'ı opak rastgele kodla üretildi; fiyat veya hassas işletme verisi QR içeriğine yazılmıyor, çözümleme işletme erişimine bağlı.
- [x] e-Belge adapter'ına tenant ve idempotency bağlı e-İrsaliye gönderme, durum sorgulama ve yanıt alma sözleşmesi eklendi; gerçek sağlayıcı erişimi açılana kadar yapılandırılmamış adapter güvenli hata döndürüyor.
- [x] Kağıt irsaliye veya entegrasyonsuz tedarikçi için dosya imzası/boyutu doğrulanan fotoğraf/PDF yükleme ve manuel belge numarası yedeği eklendi.

### Depo mal kabulü

- [x] `Depo sorumlusu` ve `Mal kabul onaylayıcısı` rolleri davet/rol ekranında şube-depo kapsamıyla tanımlandı; API bu kapsamı uygular ve tedarikçi işletme alıcı adına kabul veremez.
- [x] QR çözümleme, kodla mal kabul ve sipariş kaleminde sevk/kabul/ret miktarlarının gösterimi eklendi.
- [ ] Kamera ile QR okutma, irsaliye eşleştirme ve beklenen/gelen miktar karşılaştırmasını gerçek mobil cihazda tamamla.
- [x] QR etiketi bazında tam/kısmi kabul, ret, ret nedeni ve not kaydı eklendi; tekrar işlem aynı kabulü çoğaltmıyor.
- [x] Mal kabul kaydına QR etiketi, alıcı işletme ve şube/depo rolü erişimine bağlı, dosya imzası ve boyutu doğrulanan JPG/PNG/WEBP/PDF kanıt yükleme eklendi; dosya yolu kabul denetim izinde saklanıyor.
- [ ] Tartım, sıcaklık, lot/seri ve son kullanma tarihi kontrolünü ürün kategorisine göre açılabilir doğrulama adımları yap.
- [x] Kabul kaydına sunucudan doğrulanan kullanıcı, işletme, şube/depo, cihaz, IP, tarih-saat ve SHA-256 belge karmasıyla denetim izi eklendi.
- [ ] Yüksek tutar/risk eşiğinde iki yetkili onayı; küçük ve düzenli teslimatlarda tek yetkili onayı uygula.
- [ ] Çevrimdışı depolar için süreli ve imzalı taslak oluştur; ağ geldiğinde sunucu zamanıyla uzlaştır, çakışmayı manuel incelemeye düşür.
- [x] Kabul tamamlanmadan alıcı stoku artırılmıyor; her kısmi kabulde yalnız kabul edilen miktar kadar stok girişi yapılıyor.

### Güvenli ödeme ve kısmi hakediş

- [x] Siparişte ödeme alma, teslimata kadar hakedişi bloke tutma ve teslimat sonrası PSP serbest bırakma sözleşmesi hazırlandı; gerçek sağlayıcı adapter'ı kapalıdır.
- [x] Serbest bırakılabilir hakediş her kabul kaydının kabul edilen miktar/tutarı üzerinden hesaplanıyor.
- [x] Kısmi kabulde kabul edilen net tutar aktarılıyor; reddedilen tutar blokede kalıyor ve yönetici kararıyla alıcıya kısmen veya tamamen iade edilebiliyor.
- [x] Komisyon, komisyon KDV'si, tevkifat ve ödeme hizmeti bedeli kısmi kabul oranında kuruşa yuvarlanıyor; son kabul kuruş farkını kapatıyor.
- [x] `Bloke → Kısmen serbest → Serbest bırakıldı → İade edildi` durumları PSP işlem kimliği ve idempotency anahtarıyla saklanıyor.
- [ ] Ters ibraz durumunu gerçek PSP webhook sözleşmesi açıldığında ekle.
- [ ] Tahsilat, hakediş, iade ve ters ibraz webhook'larını imza doğrulamalı, tekrar çalıştırılabilir ve tenant bağlı işle.
- [ ] PSP bakiyesi ile Systemcel ödeme/hakediş kayıtlarını günlük otomatik mutabakata al; farkta yeni aktarımı durdurup yönetici uyarısı üret.
- [x] Serbest bırakma başarısızsa mal kabul geri alınmıyor; hata ve kabul kanıtı saklanıyor, sipariş `Hakediş bekliyor` kuyruğunda güvenli yeniden denemeye açık kalıyor.
- [ ] PSP sözleşmesi, alt üye işyeri doğrulaması, koruma hesabı ve chargeback/rezerv şartları hukuk ve finans onayından geçmeden canlı ödeme açma.

### İtiraz, fark ve kötüye kullanım

- [x] Eksik, hasarlı, yanlış ürün, kalite ve diğer şikâyet kategorileri; tedarikçi yanıtı, alıcı sonuçlandırması ve doğrulanmış sipariş değerlendirmesi eklendi.
- [x] Teslim edilmedi, sıcaklık ve belge uyuşmazlığı kategorileri mevcut şikâyet akışına eklendi.
- [x] Bir etikette itiraz açıkken aynı siparişin uyuşmazlık dışındaki etiketleri kabul edilebiliyor ve yalnız onların hakedişi serbest bırakılıyor; sipariş yönetici kararı verilene kadar `İtirazlı` kalıyor.
- [x] Yönetici inceleme paketinde sipariş, sevkiyat, irsaliye dosyası/UUID, mal kabul, fotoğraf kanıtı, kullanıcı izi, durum geçmişi ve taraf açıklamaları tek ekranda gösteriliyor.
- [x] Yönetici kararları `tedarikçiye aktar`, `alıcıya iade`, `kısmi paylaş`, `yeniden teslim` olarak gerekçe, durum geçmişi ve ödeme/hakediş kayıtlarıyla uygulanıyor.
- [ ] Taraflara yanıt süresi ve yönetici inceleme SLA'sı tanımla; süre dolunca otomatik para aktarımı yerine risk kuralına göre üst inceleme veya sözleşmesel karar uygula.
- [ ] Sürekli asılsız itiraz, sürekli eksik sevk ve olağandışı kabul/red örüntüleri için alıcı/tedarikçi risk puanı üret.
- [ ] Riskli hesaplarda daha uzun bloke, çift onay, işlem limiti veya manuel inceleme uygula; otomatik kalıcı yaptırım verme.

### Muhasebe, belge ve stok bağlantıları

- [x] Alıcı stok girişi, alış faturası ve borç carisi her kısmi kabulde yalnız kabul edilen miktar/tutar üzerinden oluşturuluyor.
- [x] Tedarikçi stok çıkışı peşin/vadeli ödeme türünden bağımsız olarak sevk edilen miktar kadar rezervasyondan ve stoktan düşüyor; kaynak stok hareketi sevkiyat referansıyla yazılıyor.
- [ ] Kabul farklarını kayıp, iade veya yeniden sevk kararıyla stokta uzlaştır.
- [ ] Kısmi kabul/red için e-İrsaliye yanıtı ve gerekiyorsa iade e-İrsaliyesi/e-Fatura süreçlerini e-Belge adapter'ına bağla.
- [ ] Sonradan değişen kabul kararlarında silme yerine ters stok, cari, fatura ve hakediş kayıtları üret.
- [ ] Aynı sipariş, irsaliye, fatura, cari, stok hareketi, PSP tahsilatı ve hakediş arasında izlenebilir referans zinciri kur.

### Ekranlar ve bildirimler

- [x] Alıcı sipariş ekranına `Beklenen sevkiyatlar`, `Mal kabul`, `Fark/itiraz` ve `Blokedeki ödemeler` operasyon görünümleri eklendi.
- [x] Tedarikçi ekranında sevk oluşturma, QR etiketi, belge numarası ve kalemlerin kabul/ret miktarları mevcut.
- [x] Tedarikçi sevkiyat ekranında belge dosyası ekleme ve sipariş bazında kesinti/net hakediş görünümü tamamlandı.
- [x] Tedarikçi satış görünümünde her mal kabulün brüt tutarı, serbest bırakılan net tutarı, aktarım referansı/hatası ve ret miktarı ayrı hareket listesinde gösteriliyor.
- [x] Yöneticiye geciken mal kabul, açık itiraz, geciken hakediş ve başarısız aktarım operasyon kuyruğu eklendi.
- [ ] Mutabakat farkı ve riskli işlem kuyruklarını gerçek PSP mutabakatı/risk puanı hazır olduğunda ekle.
- [x] Sevke hazır/kısmi sevk, randevu yaklaşması, mal kabul bekleme, kısmi kabul, itiraz, hakediş serbest bırakma ve aktarım başarısız olayları mevcut uygulama bildirimi/outbox hattına bağlandı.
- [x] Yönetim görünümüne zamanında teslim, kabul, eksik/hasar, itiraz, ortalama kabul ve hakediş süresi metrikleri eklendi.

### Test ve yayın kapıları

- [x] Tam kabul, çoklu sevkiyat, kısmi kabul, eksik/tam ret, hasar, kısmi iade ve kısmi hakediş servis testleri eklendi.
- [x] Sipariş miktarını aşan sevkiyat transaction içinde reddediliyor; stok ve rezervasyon değişmediği regresyon testinde doğrulandı.
- [ ] Yeniden sevk ve eşzamanlı sevkiyat yarışları için kalan servis/integrasyon kapsamını tamamla.
- [x] Yabancı işletmenin QR erişimi, aynı kabulün tekrar gönderimi, tam kabulde tek stok/muhasebe işlemi ve rette hakediş blokesi için regresyon testleri eklendi.
- [x] Depo/mal kabul rolü sınırı, yabancı tenant ve aynı işlem anahtarıyla mükerrer QR regresyonları eklendi.
- [x] Farklı işlem anahtarıyla aynı QR'ın ikinci kez kabul edilmesi regresyon testinde reddediliyor.
- [ ] Eşzamanlı kabul/itiraz yarışının kalan regresyon kapsamını tamamla.
- [ ] PSP tahsilat/serbest bırakma/iade webhook tekrarları, zaman aşımı ve günlük mutabakat farkını sağlayıcı sözleşme testleriyle doğrula.
- [ ] Mobil mal kabul akışını kamera izni, çevrimdışı taslak, en küçük ekran ve depo eldiveniyle kullanılabilir hedef boyutlarıyla Playwright'ta doğrula.
- [x] Pazaryeri API'si `Pazaryeri:Aktif` ve `Pazaryeri:PilotIsletmeIdleri` yapılandırmasıyla tamamen kapatılabilir veya işletme izin listesine sınırlandırılabilir duruma getirildi.
- [ ] Canlı yapılandırmada tek tedarik zinciri ve sınırlı depo pilot işletmelerini seç; kısmi kabul ve muhasebe mutabakatı kanıtlanmadan izin listesini genişletme.
- [ ] Pilot çıkışında hukuk/finans, operasyon, muhasebe, güvenlik ve geri alma runbook onaylarını yayın kaydına ekle.

## 3. P0 — Son kabul ve yayın kararı

- [ ] Sınırlı gerçek kullanıcı pilotunu tamamla; hata oranı, aktivasyon ve destek yüküne göre genel yayın kararı ver. Önceki işletme/muhasebeci senaryo pilotları tamamlandı; bu madde genel yayın kararını kapatır.

## 4. P1 — Yayından sonra iyileştirmeler

Tedarikçi pazaryeri, sevkiyat ve mal kabul 21 Eylül kullanıcı kararıyla P0'a alındı. Aşağıdaki işler genel yayın sonrasındaki sıradır; ilk yayında kullanılan depo/rol, stok ve bildirim parçaları P0 kabulüne dahildir.

1. [ ] Üyelik/rol/sahiplik: mevcut davet, rol değiştirme, üye kaldırma ve sahiplik devrini gerçek hesaplarla doğrula; açık sekmede yetki kaldırma ve arayüz kabulünü tamamla.
2. [ ] Bildirim operasyonu: işletme/kullanıcıya bağlı Telegram eşleştirmesi ve başarısız bildirimleri kontrollü yeniden gönderme görünürlüğü. Canlı SMTP teslimi P0'da.
3. [ ] Hata ve kullanım görünürlüğü: mevcut istek kimliği/log altyapısını kalıcı hata izlemeye bağla; aktivasyon ve ürün dönüşüm ölçümlerini tamamla. Altyapı alarmları P0'da.
4. [ ] Banka eşleştirme: mevcut CSV, aday önerisi ve insan onayını gerçek anonimleştirilmiş dosyalarla doğrula; kısmi/toplu eşleştirme kapsamını netleştir.
5. [ ] Eski veri aktarımı: mevcut önizleme/uygulama ve akıllı alan eşleştirmesini gerçek formatlar, yarım kalan aktarım ve tekrar denemeyle doğrula; masaüstü aracı sürümle ve imzala.
6. [ ] Stok defteri: mevcut depo, rezervasyon, transfer, sayım ve ters kaydı pilotta doğrula; kalan konum, maliyet ve mutabakat ihtiyaçlarını tamamla.
7. [ ] Genel e-Belge kapsamını tamamla: UBL-TR, e-Fatura/e-Arşiv, webhook/polling, iptal/itiraz ve mutabakat. Tedarikçi sevk/kabulü için gereken e-İrsaliye ve iade bağlantıları P0'dadır; bu madde kalan genel kapsamdır.
8. [ ] Pazaryeri iletişim güvenliği: iletişim tespiti ve 30 dakika kuralının mevcut kapsamını doğrula; yanlış pozitif ve insan incelemesini tamamla.
9. [ ] Frontend: mevcut rota bazlı lazy-load'u koru; bundle/CSS ve API yanıt sürelerini ölçerek gerekli modül ve ortak durum bileşeni düzenlemelerini yap.
10. [ ] Mevcut şube/kur temelinin üzerine konsolidasyon, entegrasyon ve Pro muhasebeci otomasyonlarında kalan kapsamı netleştir; P2 işleriyle birlikte planla.

## 5. P2 — Sonraki ürün derinliği

- [ ] Sektör/NACE tabanlı mevzuat ve teşvik bildirimleri; yalnız doğrulanmış kaynaklarla.
- [ ] Gelişmiş stok maliyetleme, performans ve 100 bin+ hareket testleri.
- [ ] OAuth, webhook abonelikleri ve geliştirici portalı; API anahtarlı, yetki kapsamlı salt okunur v1 mevcut.
- [ ] Çoklu şube konsolidasyonu, kur farkı ve çoklu para birimi raporlaması.
- [ ] Gelişmiş müşteri sağlık skoru, dönem sonu görevleri ve destek SLA otomasyonu.

## 6. Yapıldı — teknik temel ve kayıtlı doğrulamalar

- [x] PostgreSQL migration zinciri boş ve eski şemada veri koruyarak doğrulandı.
- [x] Aylık/yıllık fiyat kataloğu, KDV, lansman kontenjanı ve yenileme referansı tek sunucu kaynağına alındı.
- [x] Sağlayıcıdan bağımsız checkout, imzalı Fake webhook, idempotency ve abonelik durum makinesi kuruldu.
- [x] Abonelik özeti, plan hakları, ödeme geçmişi, açık onay penceresi ve dönem sonu iptal ekranı tamamlandı.
- [x] Fatura, kullanıcı, işletme, gelir-gider, cari, ürün/hizmet ve muhasebeci müşteri limitleri API'de transaction-safe uygulanıyor.
- [x] AES-256-GCM, tenant sınırları, rate limit, güvenlik başlıkları, dar CORS, dosya imza/boyut ve ZIP bombası kontrolleri tamamlandı.
- [x] Oracle secret'ları, private PostgreSQL ağı, liveness/readiness, container durumu ve günlük yedek timer'ı doğrulandı.
- [x] Oracle CPU, RAM, disk, readiness, PostgreSQL bağlantısı, container restart ve uzak yedek yaşı için Prometheus metrik collector'ı ve systemd timer'ı eklendi; sahte servis fixture'ı geçti.
- [x] Şifreli sunucu dışı yedek aktarımı için `rclone crypt`, kilit, retry, checksum, tamamlanma işareti ve uzak başarıya bağlı güvenli yerel retention eklendi; hata/idempotency fixture'ları geçti.
- [x] Genel bildirim outbox'ına tenant bağlı SMTP adaptörü eklendi; alıcı/snapshot uyuşmazlığı ve mükerrer deneme e-postası regresyonları kapatıldı.
- [x] Fiyat artışı için değişmez snapshot ve iki kanal teslim kanıtı eklendi; 30 gün şartı sağlanmazsa aylık/yıllık ve uzlaştırma sonrası yenilemede eski fiyat korunuyor.
- [x] SHA-sabitli release bundle, yayın kanıt şeması/doğrulayıcısı ve canonical Developer API public smoke'u eklendi.
- [x] Mobil kayıt/çıkış, sohbet arşiv yarışı ve eski mavi tema regresyonları kapatıldı.
- [x] Landing plan/rol/dönem seçimi uygulamaya taşınıyor; aylık kartlar ilk 3 ay ve sonraki fiyatı, yıllık kartlar toplam tutar ve gerçek tasarrufu gösteriyor; muhasebeci kartları masaüstünde merkez, mobilde tek sütun.
- [x] CI; .NET, Vitest, Playwright cihaz matrisi, lint, typecheck, PostgreSQL smoke, Docker build, zafiyet ve secret taramasını çalıştırıyor.
- [x] Mantıksal PostgreSQL yedeği izole PostgreSQL 18'e geri yüklenerek doğrulandı; release/rollback/monitoring runbook'ları hazır.
- [x] İşletme canlı pilotu; gelir-gider, ürün-stok, hızlı satış, cari, tahsilat, fatura, rapor, GİB ayarı, Telegram, abonelik ve dönem sonu iptaliyle tamamlandı.
- [x] Pilot sırasında bulunan hızlı satış limit atlama ve cari kart üstüne yazma yarışları regresyon testleriyle kapatıldı.
- [x] Canlı yönetici erişimi yapılandırıldı; pilot muhasebeci başvurusu onaylandı.
- [x] Oracle'a DeepSeek anahtarı ve `deepseek-flash` model seçimi eklendi; sağlayıcı smoke isteği ile public health/readiness 11 Eylül 2026'da geçti. Tenant bağlı gerçek kullanıcı AI akışı ayrı kabul kapısıdır.
- [x] Maskeli DeepSeek istemcisi ve işletmeye bağlı finansal bağlam testleri kaynak kodda mevcut; canlı oturum kabulü açık.
- [x] CI başarılı aday SHA'sını Oracle'a taşıyan otomatik yayın workflow'u, kısıtlı SSH geçidi ve dağıtım/yedek ön kontrolleri eklendi. Kanıt: [workflow](.github/workflows/deploy-production.yml), [dağıtım betiği](deployment/oracle-free/scripts/deploy-bundle.sh).
- [x] Üyelik daveti, kapasite, e-posta doğrulama, rol değiştirme, üye kaldırma ve sahiplik devri temeli/testleri mevcut. Kanıt: [üyelik testleri](CashTracker.Tests/MembershipEntitlementAuditTests.cs).
- [x] Depo, rezervasyon, transfer, sayım ve ters stok kaydı temeli/testleri mevcut. Kanıt: [stok testleri](CashTracker.Tests/GelismisStokServiceTests.cs).
- [x] Banka CSV aktarımı, aday eşleştirme, insan onayı ve işletme sınırı testleri mevcut. Kanıt: [banka testleri](CashTracker.Tests/BankaMutabakatServiceTests.cs).
- [x] Eski veri aktarımında önizleme/uygulama, akıllı alan eşleştirme ve tekrar işlem testleri mevcut. Kanıt: [aktarım testleri](CashTracker.Tests/ExternalDataMigrationTests.cs).
- [x] API anahtarı ve yetki kapsamı olan salt okunur geliştirici API v1 mevcut. Kanıt: [API testleri](CashTracker.Tests/DeveloperApiServiceTests.cs).
- [x] Rota bazlı lazy-load, istek kimliği ve log kapsamı mevcut. Kanıt: [App.tsx](Systemcel.Web/src/App.tsx), [istek görünürlüğü testleri](CashTracker.Tests/RequestObservabilityTests.cs).
- [x] Plan penceresinde klavye odak kapanı, Escape ve tetikleyiciye odak dönüşü için test eklendi. Kanıt: [erişilebilirlik testleri](Systemcel.Web/e2e/workspace-accessibility.spec.ts).
- [x] Jev karar akışları ve düşük güvenli kararları incelemeye yönlendirme kodu/testleri eklendi. Kanıt: [Jev testleri](CashTracker.Tests/JevDecisionServiceTests.cs), `d892fc0`, `c58ab35`, `dcb4343`.
- [x] Oracle Docker/Caddy/PostgreSQL düzeninde app, Caddy ve PostgreSQL sağlık ve ağ izolasyonu doğrulandı; genel HTTP kapısı 8 Eylül 2026'da geçti.
- [x] Muhasebeci pilotunu; profil görseli yükleme, yönetici onayı, müşteri eşleşmesi ve çalışma alanı geçişiyle tamamla.
- [x] Fake ödeme zincirini işletme ve muhasebeci rollerinde doğrula.
- [x] Oracle yedeği ayrı veritabanına geri yüklendi; checksum ve geri yükleme kontrolü 2 Eylül 2026'da geçti.
- [x] Hizmet sağlayıcı adı, şahıs işletmesi türü, vergi dairesi/numarası, açık adres ve destek e-postasını Türkçe/İngilizce yasal metinlere ekle; vergi levhasındaki işe başlama tarihi ve faaliyet kodunu yayım kaydına işle.
- [x] Hukuk onayı kullanıcı tarafından 31 Ağustos 2026'da bildirildi; doğrulanan kimlik ve iletişim alanları 17 Eylül 2026'da ürün metinlerine işlendi.
- [x] PayTR başvurusu yapıldı — kullanıcı 21 Eylül 2026'da doğruladı; başvuru değerlendirmesi sürüyor.

## 7. Önceki pilotların kabul kaydı

### İşletme

- [x] Giriş, geri tuşu ve işletme değiştirme; sıfırdan kayıt/çıkış ayrı smoke'ta kalıyor.
- [x] Kolay kurulum ve işletme profili.
- [x] Dashboard ve DeepSeek sağlayıcı smoke'u önceki kayıtlarda doğrulandı. Gerçek oturumla AI kabulü P0'da açık.
- [x] Gelir, gider, kasa hareketi ve tahsilat.
- [x] Cari hesap ve hareketler.
- [x] Ürün/hizmet, stok, hızlı satış ve raporlar.
- [x] Fatura taslağı ve onay; güvenlik gereği gerçek GİB gönderimi yapılmadı.
- [x] Muhasebeci bulma, talep, bağlantı ve sohbet.
- Açık — Sohbette dosya yükleme: P0 dosya kabulü kapsamında, dosya ve hedef onayı bekliyor.
- [x] Telegram bağlantı ekranı ve eşleme verisi; üçüncü kişiye gerçek mesaj gönderilmedi.
- [x] Ayarlar, çoklu işletme, plan, açık onay, Fake ödeme, geçmiş ve dönem sonu iptal.

### Muhasebeci

- [x] Kayıt, kolay kurulum, profil görseli, başvuru ve yönetici onayı.
- [x] Müşteri listesi, pazaryeri talebi kabulü ve 1/10 kapasite sayacı.
- [x] Müşteri çalışma alanına geçiş; `Okuma + rapor` yazma sınırı API'de doğrulandı.
- [x] Müşteri verileri, rapor erişimi, talep ve sohbet.
- Açık — Müşteri sohbetinde dosya yükleme: P0 dosya kabulü kapsamında.
- [x] Pazaryeri profili ve işletme eşleşmesi.
- [x] Standart aylık seçim, açık onay, Fake ödeme, plan dönemi/hakları ve ödeme geçmişi.
- [x] Pro ve Standart + ek müşteri kredisi fiyatları ile Fake checkout varyasyonları kalıcı testlerle doğrulandı.
- [x] Yabancı işletme kimliğiyle okuma, aktif etme, yeniden adlandırma ve silme girişimleri regresyon testiyle reddediliyor.

### Ortak kalite kapıları

- [x] 320/360/375/390/430 mobil, 768 tablet, 1366/1920 masaüstü, WebKit ve reduced-motion Playwright matrisi geçti.
- Açık — Kalan klavye, odak ve boş/yükleniyor/hata durumları: P0 arayüz kabulü kapsamında.
- [x] Konsol hatası, yatay taşma ve eski mavi tema kontrolü; canlı konsol temiz.
- [x] Oluşturulan pilot verileri `PILOT` etiketiyle ayrıldı ve pilot raporuna kaydedildi.

Kalan dosya, AI ve ortak arayüz işleri yukarıdaki P0 maddelerinin kabul kapsamıdır; ayrı iş olarak iki kez sayılmaz.

## 8. Sabit ürün kararları ve fiyatlar

- Lansmanda ücretsiz deneme kapalı; abonelik açık onay ve anlık tahsilatla başlar.
- Lansman fiyatı ilk 50 yeni hesapta aylık planda ilk 3 ay geçerlidir.
- Yıllık toplu ödemede lansman fiyatı 12 ay için uygulanır; yenileme liste fiyatından yapılır.
- Peşin ödenmiş dönem değişmez; sonraki yenilemede o tarihteki liste fiyatı uygulanır.
- Fiyat değişikliği en az 30 gün önce e-posta ve uygulama içinden bildirilir; dönem sonu iptal yolu açık kalır.
- Ek muhasebeci müşteri kredileri kampanya dışıdır ve güncel liste fiyatından yinelenir.
- Muhasebecisini getirip meslek doğrulamasını tamamlatan işletmeye, lansman döneminden sonraki ilk normal plan ayı hediye edilir.

### Fiyatlar — KDV hariç

| Plan | Lansman aylık | Normal aylık | Lansman yıllık toplam | Normal yıllık |
|---|---:|---:|---:|---:|
| İşletme Başlangıç | ₺490 | ₺690 | ₺6.144 | ₺6.624 |
| İşletme Büyüme | ₺990 | ₺1.290 | ₺11.880 | ₺15.480 |
| İşletme Kurumsal | ₺1.990 | ₺2.490 | ₺22.704 | ₺23.904 |
| Muhasebeci Standart | ₺699 | ₺899 | ₺8.557,92 | ₺9.061,92 |
| Muhasebeci Pro | ₺1.199 | ₺1.499 | ₺14.353,92 | ₺15.109,92 |

## 9. Operasyon notları

- Canlı alan: `https://systemcel.app`
- PayTR başvurusu değerlendirmede; gerçek tahsilat henüz denenmedi (21 Eylül kullanıcı beyanı).
- Canlı uygulama Oracle üzerinde çalışır; genel yayın kararı verilmeden ödeme sağlayıcısı `Fake` kalır.
- Canlı AI sağlayıcısı DeepSeek, model `deepseek-flash`tır; anahtar yalnız Oracle `.env` dosyasında tutulur.
- PostgreSQL yalnız `systemcel_app` kullanıcısı ve uygulama trusted source'u üzerinden erişilir.
- OneDrive dışı geri alınabilir geliştirme önbelleği: `C:\Users\Windows\AppData\Local\SystemcelCacheBackups\20260810-1615`
- `YAPILACAKLAR.md` kullanıcı isteği gereği commit edilmez.
