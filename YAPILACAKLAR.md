# Systemcel — Kompakt Yapılacaklar

> Son güncelleme: 8 Eylül 2026
> Kural: Tamamlanan paketler bu dosyada ayrıntılı günlük olarak tutulmaz; yalnız kısa özet bırakılır.
> PayTR başvurusu, test mağazası, sandbox ve gerçek kart tahsilatı şirket kuruluşundan önce açılmaz.

## 1. Sabit kararlar

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

## 2. P0 — Açık yayın kapıları

### Teknik olarak şimdi yapılabilir

- [x] Oracle Docker/Caddy/PostgreSQL düzeninde app, Caddy ve PostgreSQL sağlık ve ağ izolasyonu doğrulandı; genel HTTP kapısı 8 Eylül 2026'da geçti.
- [ ] Sıfırdan yeni kimlikle Clerk kayıt → provision → kolay kurulum smoke testi çalıştır.
- [ ] Gerçek SMTP teslimini doğrula; fiyat/yenileme bildirimlerinin gönderim kanıtını kaydet.
- [x] Muhasebeci pilotunu; profil görseli yükleme, yönetici onayı, müşteri eşleşmesi ve çalışma alanı geçişiyle tamamla.
- [x] Fake ödeme zincirini işletme ve muhasebeci rollerinde doğrula.
- [ ] Fiziksel iOS/Safari smoke'unu sınırlı pilot sırasında çalıştır.

### Kullanıcı, maliyet veya dış koordinasyon gerektirir

- [x] Oracle yedeği ayrı veritabanına geri yüklendi; checksum ve geri yükleme kontrolü 2 Eylül 2026'da geçti.
- [ ] Günlük Oracle yedeğini kullanıcı profiline bağlı olmayan, taşınabilir ve otomatik sunucu dışı hedefe aktar.
- [ ] Sınırlı gerçek kullanıcı pilotunu tamamla; hata oranı, aktivasyon ve destek yüküne göre genel yayın kararı ver.

### Şirket kuruluşundan sonra

- [ ] Şirket unvanı, MERSİS/vergi bilgisi, adres, KEP/e-posta ve destek kanalını yasal metinlere ekle.
- [x] Hukuk onayı kullanıcı tarafından 31 Ağustos 2026'da bildirildi; yayıma alınacak kimlik ve iletişim alanları ayrı kapıda kalıyor.
- [ ] PayTR başvuru/test mağazası/sandbox sözleşme testlerini tamamla.
- [ ] Canlı checkout, imzalı webhook, yenileme, başarısız tahsilat, iade ve mutabakatı gerçek sağlayıcıda doğrula.
- [ ] Fiyat artışı için 30 günlük bildirim snapshot'ı, teslim kanıtı ve bildirim başarısızsa zamlı tahsilatı durdurma kuralını uygula.

## 3. P0 — Tamamlanan teknik temel

- [x] PostgreSQL migration zinciri boş ve eski şemada veri koruyarak doğrulandı.
- [x] Aylık/yıllık fiyat kataloğu, KDV, lansman kontenjanı ve yenileme referansı tek sunucu kaynağına alındı.
- [x] Sağlayıcıdan bağımsız checkout, imzalı Fake webhook, idempotency ve abonelik durum makinesi kuruldu.
- [x] Abonelik özeti, plan hakları, ödeme geçmişi, açık onay penceresi ve dönem sonu iptal ekranı tamamlandı.
- [x] Fatura, kullanıcı, işletme, gelir-gider, cari, ürün/hizmet ve muhasebeci müşteri limitleri API'de transaction-safe uygulanıyor.
- [x] AES-256-GCM, tenant sınırları, rate limit, güvenlik başlıkları, dar CORS, dosya imza/boyut ve ZIP bombası kontrolleri tamamlandı.
- [x] Oracle secret'ları, private PostgreSQL ağı, liveness/readiness, container durumu ve günlük yedek timer'ı doğrulandı.
- [ ] Oracle CPU, RAM, disk, container restart ve yedek yaşı alarmlarını `docs/runbooks/monitoring.md` eşiklerine göre kur.
- [x] Mobil kayıt/çıkış, sohbet arşiv yarışı ve eski mavi tema regresyonları kapatıldı.
- [x] Landing plan/rol/dönem seçimi uygulamaya taşınıyor; aylık kartlar ilk 3 ay ve sonraki fiyatı, yıllık kartlar toplam tutar ve gerçek tasarrufu gösteriyor; muhasebeci kartları masaüstünde merkez, mobilde tek sütun.
- [x] CI; .NET, Vitest, Playwright cihaz matrisi, lint, typecheck, PostgreSQL smoke, Docker build, zafiyet ve secret taramasını çalıştırıyor.
- [x] Mantıksal PostgreSQL yedeği izole PostgreSQL 18'e geri yüklenerek doğrulandı; release/rollback/monitoring runbook'ları hazır.
- [x] İşletme canlı pilotu; gelir-gider, ürün-stok, hızlı satış, cari, tahsilat, fatura, rapor, GİB ayarı, Telegram, abonelik ve dönem sonu iptaliyle tamamlandı.
- [x] Pilot sırasında bulunan hızlı satış limit atlama ve cari kart üstüne yazma yarışları regresyon testleriyle kapatıldı.
- [x] Canlı yönetici erişimi yapılandırıldı; pilot muhasebeci başvurusu onaylandı.

## 4. Pilot özellik matrisi

### İşletme

- [x] Giriş, geri tuşu ve işletme değiştirme; sıfırdan kayıt/çıkış ayrı smoke'ta kalıyor.
- [x] Kolay kurulum ve işletme profili.
- [ ] Dashboard çalışıyor; canlı AI anahtarı eksik olduğu için asistan yanıt testi bekliyor.
- [x] Gelir, gider, kasa hareketi ve tahsilat.
- [x] Cari hesap ve hareketler.
- [x] Ürün/hizmet, stok, hızlı satış ve raporlar.
- [x] Fatura taslağı ve onay; güvenlik gereği gerçek GİB gönderimi yapılmadı.
- [x] Muhasebeci bulma, talep, bağlantı ve sohbet.
- [ ] Sohbette dosya yükleme; zararsız pilot dosyasının gönderimi için işlem anı onayı bekliyor.
- [x] Telegram bağlantı ekranı ve eşleme verisi; üçüncü kişiye gerçek mesaj gönderilmedi.
- [x] Ayarlar, çoklu işletme, plan, açık onay, Fake ödeme, geçmiş ve dönem sonu iptal.

### Muhasebeci

- [x] Kayıt, kolay kurulum, profil görseli, başvuru ve yönetici onayı.
- [x] Müşteri listesi, pazaryeri talebi kabulü ve 1/10 kapasite sayacı.
- [x] Müşteri çalışma alanına geçiş; `Okuma + rapor` yazma sınırı API'de doğrulandı.
- [x] Müşteri verileri, rapor erişimi, talep ve sohbet.
- [ ] Müşteri sohbetinde dosya yükleme.
- [x] Pazaryeri profili ve işletme eşleşmesi.
- [x] Standart aylık seçim, açık onay, Fake ödeme, plan dönemi/hakları ve ödeme geçmişi.
- [x] Pro ve Standart + ek müşteri kredisi fiyatları ile Fake checkout varyasyonları kalıcı testlerle doğrulandı.
- [x] Yabancı işletme kimliğiyle okuma, aktif etme, yeniden adlandırma ve silme girişimleri regresyon testiyle reddediliyor.

### Ortak kalite kapıları

- [x] 320/360/375/390/430 mobil, 768 tablet, 1366/1920 masaüstü, WebKit ve reduced-motion Playwright matrisi geçti.
- [ ] Klavye sırası, focus, modal kapanı, Escape, boş/loading/hata durumları.
- [x] Konsol hatası, yatay taşma ve eski mavi tema kontrolü; canlı konsol temiz.
- [x] Oluşturulan pilot verileri `PILOT` etiketiyle ayrıldı ve pilot raporuna kaydedildi.

### Canlı pilotta kalan somut engeller

- Oracle'a canlı AI anahtarı eklenmeli.
- Gerçek SMTP teslim kanıtı ve sıfırdan yeni kimlik smoke'u alınmalı.

## 5. P1 — P0 sonrasında öncelik

1. Hatırlatma ve bildirim omurgası: outbox, idempotency, retry/dead-letter, sessiz saat, e-posta/Telegram tercihleri.
2. e-Belge sağlayıcı adapter'ı: UBL-TR, e-Fatura/e-Arşiv, webhook/polling, iptal/itiraz ve mutabakat.
3. Stok hareket defteri: depo/konum, rezervasyon, transfer, sayım, ters kayıt, maliyet ve mutabakat.
4. Tedarikçi zinciri: sevk/e-İrsaliye, depo mal kabulü, kısmi kabul, itiraz ve kabul edilen miktar kadar hakediş.
5. Pazaryeri iletişim güvenliği: iletişim tespiti, 30 dakika kısıt, yanlış pozitif ve insan incelemesi.
6. Banka hareketleri ve insan onaylı cari/fatura eşleştirme.
7. Kullanıcı/rol/sahiplik devri ve üyelik yönetimi.
8. Eski veri aktarım sihirbazı ve imzalı masaüstü araç dağıtımı.
9. Yapılandırılmış log, correlation ID, hata izleme ve ürün dönüşüm metrikleri.
10. Frontend modülerleştirme, lazy-load, ortak durum bileşenleri ve CSS parçalama.
11. Çoklu şube/para birimi, entegrasyon API'leri ve gerçek Pro muhasebeci otomasyonları.

### Tedarikçi zinciri — sevk, mal kabul ve hakediş

#### Sabit ürün kararları

- Klasik kargo takibi zorunlu olmayacak; tedarikçinin kendi aracı, distribütör, 3PL, soğuk zincir ve bölge deposu aynı sevk modeliyle desteklenir.
- Tedarikçinin veya sürücünün “teslim ettim” beyanı tek başına hakediş açmaz; hakedişin kaynağı alıcının yetkili depo/şube kullanıcısının dijital mal kabul kaydıdır.
- Mal kabul sipariş bazında değil kalem ve miktar bazında yapılır; kabul edilen miktar stok, fatura, cari ve hakedişe yansır.
- Eksik, fazla, hasarlı, yanlış, kalite reddi ve sıcaklık/parti uyuşmazlığı ayrı nedenler olarak tutulur.
- Güvenli ödeme yalnız lisanslı PSP'nin alt üye işyeri/blokeli hakediş modeliyle çalışır; para Systemcel hesabında tutulmaz.
- PSP hazır değilse vadeli/cari akış açıkça ayrı bir ödeme seçeneğidir; güvenli ödeme gibi sunulmaz.

#### Sipariş ve sevk modeli

- [ ] Sipariş durumlarını `Sipariş verildi → Tedarikçi onayladı → Sevke hazır → Kısmen sevk edildi/Sevk edildi → Mal kabul bekliyor → Kısmen kabul/Tam kabul/İtirazlı → Tamamlandı` olarak kalem toplamlarından türet.
- [ ] Tek siparişe birden fazla sevkiyat, farklı araç/depo ve farklı teslim tarihi bağlanabilmesini sağla.
- [ ] Sevkiyat kaydına e-İrsaliye numarası/UUID, sevk tarihi, araç plakası, sürücü/taşıyıcı, çıkış ve varış deposu, randevu zamanı ve açıklama alanlarını ekle.
- [ ] Seri/lot/parti, son kullanma tarihi, ağırlık, sıcaklık aralığı ve palet/koli bilgisini kategoriye göre isteğe bağlı destekle.
- [ ] Sevkiyat QR'ı üret; QR yalnız sipariş/sevkiyat kimliği taşısın, fiyat veya hassas işletme verisi içermesin.
- [ ] e-Belge adapter'ına e-İrsaliye gönderme, durum sorgulama ve e-İrsaliye yanıtı alma sözleşmesini ekle.
- [ ] Kağıt irsaliye veya entegrasyonsuz tedarikçi için belge fotoğrafı/PDF ve manuel numara girişi yedeği bırak.

#### Depo mal kabulü

- [ ] `Depo sorumlusu` ve `Mal kabul onaylayıcısı` rollerini şube/depo kapsamıyla tanımla; sürücü ve tedarikçi alıcı adına kabul veremesin.
- [ ] Mobil uyumlu mal kabul ekranında QR okutma, irsaliye eşleştirme ve beklenen/gelen/kabul/red miktarlarını yan yana göster.
- [ ] Her kalem için tam kabul, kısmi kabul ve red işlemlerini; neden, not ve fotoğraf kanıtıyla kaydet.
- [ ] Tartım, sıcaklık, lot/seri ve son kullanma tarihi kontrolünü ürün kategorisine göre açılabilir doğrulama adımları yap.
- [ ] Kabul kaydına kullanıcı, işletme, şube/depo, cihaz, IP, tarih-saat ve belge karması ekleyerek değiştirilemez denetim izi oluştur.
- [ ] Yüksek tutar/risk eşiğinde iki yetkili onayı; küçük ve düzenli teslimatlarda tek yetkili onayı uygula.
- [ ] Çevrimdışı depolar için süreli ve imzalı taslak oluştur; ağ geldiğinde sunucu zamanıyla uzlaştır, çakışmayı manuel incelemeye düşür.
- [ ] Kabul tamamlanmadan alıcı stoklarını artırma; yalnız kabul edilen miktar kadar stok girişi yap.

#### Güvenli ödeme ve kısmi hakediş

- [x] Siparişte ödeme alma, teslimata kadar hakedişi bloke tutma ve teslimat sonrası PSP serbest bırakma sözleşmesi hazırlandı; gerçek sağlayıcı adapter'ı kapalıdır.
- [ ] Sipariş toplamı yerine her sevkiyat kaleminin kabul edilen miktarı üzerinden serbest bırakılabilir hakediş hesapla.
- [ ] Kısmi kabulde kabul edilen tutarı aktar; eksik/hasarlı/reddedilen tutarı blokede bırak veya karar sonucunda iade et.
- [ ] Komisyon, komisyon KDV'si, tevkifat, ödeme hizmeti bedeli ve tedarikçi net hakedişini her kısmi aktarımda oransal ve kuruş mutabakatlı dağıt.
- [ ] `Blokede → Kısmen serbest → Serbest bırakıldı → İade edildi/Ters ibraz` durumlarını PSP işlem kimliği ve idempotency anahtarıyla sakla.
- [ ] Tahsilat, hakediş, iade ve ters ibraz webhook'larını imza doğrulamalı, tekrar çalıştırılabilir ve tenant bağlı işle.
- [ ] PSP bakiyesi ile Systemcel ödeme/hakediş kayıtlarını günlük otomatik mutabakata al; farkta yeni aktarımı durdurup yönetici uyarısı üret.
- [ ] Serbest bırakma başarısızsa mal kabulü geri alma; siparişi `Hakediş bekliyor` durumunda tut ve güvenli yeniden deneme sağla.
- [ ] PSP sözleşmesi, alt üye işyeri doğrulaması, koruma hesabı ve chargeback/rezerv şartları hukuk ve finans onayından geçmeden canlı ödeme açma.

#### İtiraz, fark ve kötüye kullanım

- [ ] İtiraz türlerini `teslim edilmedi`, `eksik`, `hasarlı`, `yanlış ürün`, `kalite`, `sıcaklık`, `belge uyuşmazlığı` olarak yapılandır.
- [ ] İtiraz açıldığında yalnız ilgili sevkiyat/kalem tutarını bloke et; uyuşmazlık olmayan hakedişi gereksiz yere tutma.
- [ ] İnceleme paketinde sipariş, sevkiyat, irsaliye/e-İrsaliye yanıtı, mal kabul kaydı, fotoğraflar, kullanıcı izi ve taraf açıklamalarını tek ekranda göster.
- [ ] Yönetici kararlarını `tedarikçiye aktar`, `alıcıya iade`, `kısmi paylaş`, `yeniden teslim` olarak gerekçe ve denetim iziyle uygula.
- [ ] Taraflara yanıt süresi ve yönetici inceleme SLA'sı tanımla; süre dolunca otomatik para aktarımı yerine risk kuralına göre üst inceleme veya sözleşmesel karar uygula.
- [ ] Sürekli asılsız itiraz, sürekli eksik sevk ve olağandışı kabul/red örüntüleri için alıcı/tedarikçi risk puanı üret.
- [ ] Riskli hesaplarda daha uzun bloke, çift onay, işlem limiti veya manuel inceleme uygula; otomatik kalıcı yaptırım verme.

#### Muhasebe, belge ve stok bağlantıları

- [ ] Alıcı stok girişini, alış faturasını ve borç carisini yalnız kabul edilen miktar/tutar üzerinden oluştur.
- [ ] Tedarikçi stok çıkışını sevkte rezervasyondan düş; kabul farklarını kayıp, iade veya yeniden sevk kararıyla uzlaştır.
- [ ] Kısmi kabul/red için e-İrsaliye yanıtı ve gerekiyorsa iade e-İrsaliyesi/e-Fatura süreçlerini e-Belge adapter'ına bağla.
- [ ] Sonradan değişen kabul kararlarında silme yerine ters stok, cari, fatura ve hakediş kayıtları üret.
- [ ] Aynı sipariş, irsaliye, fatura, cari, stok hareketi, PSP tahsilatı ve hakediş arasında izlenebilir referans zinciri kur.

#### Ekranlar ve bildirimler

- [ ] Alıcıya `Beklenen sevkiyatlar`, `Mal kabul`, `Fark/itiraz` ve `Blokedeki ödemeler` görünümlerini ekle.
- [ ] Tedarikçiye sevk oluşturma, belge ekleme, kabul sonucu, bloke/serbest hakediş ve fark kapatma ekranlarını ekle.
- [ ] Yöneticiye geciken mal kabul, açık itiraz, başarısız aktarım, mutabakat farkı ve riskli işlem kuyrukları ekle.
- [ ] Sevk edildi, randevu yaklaştı, mal kabul bekliyor, kısmi kabul, red, itiraz, hakediş serbest ve aktarım başarısız olaylarını uygulama içi bildirim/outbox hattına bağla.
- [ ] Operasyon raporlarına zamanında teslim, kabul oranı, eksik/hasar oranı, itiraz oranı, ortalama kabul süresi ve hakediş süresi metriklerini ekle.

#### Test ve yayın kapıları

- [ ] Tam kabul, çoklu sevkiyat, kısmi kabul, fazla teslim, eksik teslim, tam red, hasar, yeniden sevk ve iade senaryoları için servis/integrasyon testleri yaz.
- [ ] Yetkisiz depo kullanıcısı, yabancı tenant, mükerrer QR, aynı kabulün iki kez gönderimi ve eşzamanlı kabul/itiraz yarışlarını regresyon testine al.
- [ ] PSP tahsilat/serbest bırakma/iade webhook tekrarları, zaman aşımı ve günlük mutabakat farkını sağlayıcı sözleşme testleriyle doğrula.
- [ ] Mobil mal kabul akışını kamera izni, çevrimdışı taslak, en küçük ekran ve depo eldiveniyle kullanılabilir hedef boyutlarıyla Playwright'ta doğrula.
- [ ] Özelliği bayrak arkasında tek tedarik zinciri ve sınırlı depo pilotuyla aç; kısmi kabul ve muhasebe mutabakatı kanıtlanmadan genelleştirme.
- [ ] Pilot çıkışında hukuk/finans, operasyon, muhasebe, güvenlik ve geri alma runbook onaylarını yayın kaydına ekle.

## 6. P2 — Sonraki ürün derinliği

- Sektör/NACE tabanlı mevzuat ve teşvik bildirimleri; yalnız doğrulanmış kaynaklarla.
- Gelişmiş stok maliyetleme, performans ve 100 bin+ hareket testleri.
- API anahtarı/OAuth, webhook abonelikleri ve geliştirici portalı.
- Çoklu şube konsolidasyonu, kur farkı ve çoklu para birimi raporlaması.
- Gelişmiş müşteri sağlık skoru, dönem sonu görevleri ve destek SLA otomasyonu.

## 7. Operasyon notları

- Canlı alan: `https://systemcel.app`
- Şirket öncesi ödeme sağlayıcısı: `Fake`
- Canlı uygulama Oracle üzerinde çalışır; genel yayın kararı verilmeden ödeme sağlayıcısı `Fake` kalır.
- PostgreSQL yalnız `systemcel_app` kullanıcısı ve uygulama trusted source'u üzerinden erişilir.
- OneDrive dışı geri alınabilir geliştirme önbelleği: `C:\Users\Windows\AppData\Local\SystemcelCacheBackups\20260810-1615`
- `YAPILACAKLAR.md` kullanıcı isteği gereği commit edilmez.
