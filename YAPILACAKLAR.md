# Systemcel — Öncelikli Yapılacaklar

> Son güncelleme: 25 Eylül 2026
> Son doğrulanmış canlı aday: `6a1ad80`; [CI #35939579315](https://github.com/01burark-oss/Systemcel/actions/runs/35939579315) ve [production deploy #35940792670](https://github.com/01burark-oss/Systemcel/actions/runs/35940792670) başarılı. Oracle aday SHA eşleşmesi ve public production smoke geçti. Gerçek kullanıcıyla AI/Telegram kabulü ayrıca açık.
> `[x]` yalnız belirtilen kapsamın tamamlandığını gösterir. Kodun ve testin bulunması, canlı kabulün tamamlandığı anlamına gelmez.
> 21 Eylül kullanıcı kararı: tedarikçi pazaryeri, sevkiyat ve mal kabul ilk ücretli yayına dahildir. Satış belgesi, canlı sürüm/geri dönüş ve Jev gerçek kullanım kontrolleri listeye eklendi.
> Kural: Tamamlanan paketler bu dosyada ayrıntılı günlük olarak tutulmaz; yalnız kısa özet bırakılır.
> PayTR, 24 Eylül yanıtında mevcut Sanal POS teklifinin pazaryeri için geçerli olduğunu ve teklif dışında kurulum/yıllık/aylık/işlem başı ek ücret olmadığını bildirdi. Hesap yetkileri, sözleşme koşulları, test erişimi ve gerçek tahsilat kabulü açık.
> Ayrıntılı kapanış planı: [`docs/design/2026-09-11-yayin-ve-yazilim-kapanis-plani.md`](docs/design/2026-09-11-yayin-ve-yazilim-kapanis-plani.md)

### Dış onay ve canlı kabul sırası

1. Kullanıcının PayTR görüşmesinden aktardığı onaya kadar bekleme ve 7 günlük valör dönemi bilgisini yazılı teyit ettir; onay sonrası aktarım gününü ve hesapta kalma koşullarını netleştir. Chargeback kanıt/bloke/rezerv ayrıntıları, alt satıcı belgeleri ve abonelik yetkisi için yazılı yanıt al; pazaryeri hesabının test erişimini doğrula. PayTR, chargeback sorumluluğunun pazaryerinde olduğunu ve test bilgilerinin mağaza başvurusu test moduna geçince verileceğini e-postayla bildirdi.
2. Mali müşavir ve hukukla şahıs işletmesi MERSİS/KEP gerekliliğini, Systemcel abonelik satış belgesi yöntemini, PSP sözleşmesini ve DeepSeek yurt dışı veri aktarımı metinlerini karara bağla.
3. Canlı SMTP, şifreli Oracle Object Storage yedeği, üç Oracle VM alarmı ve UptimeRobot HTTPS kontrolü açıldı. İlk zamanlanmış yedek ve uzak geri yükleme geçti; üç ardışık günlük yedeği, uygulama içi posta teslimini, gerçek kesinti/düzelme bildirimini ve ikinci alarm alıcısını doğrula. DeepSeek API veri işleme ve yurt dışı aktarım koşulları için hukuk incelemesi açık.
4. Sağlayıcı erişimi açılınca gerçek checkout, imzalı webhook, kısmi hakediş, iade, ters ibraz ve günlük mutabakatı sandbox ve sınırlı canlı işlemle doğrula.
5. Tek tedarik zinciri ve sınırlı depo pilotunda gerçek mobil cihaz, yetkili kullanıcı, muhasebe mutabakatı ve geri alma provasını tamamla; hukuk/finans, operasyon ve güvenlik onaylarıyla yayın kararı ver.

### PayTR yanıtı — 24 Eylül 2026

- [x] PayTR, gönderdiği Sanal POS teklifinin pazaryeri için geçerli olduğunu ve teklif dışında kurulum, yıllık, aylık veya işlem başı ek ücret uygulanmayacağını e-postayla bildirdi. Teklifteki komisyon oranları ayrıca geçerlidir; sözleşme ve mağaza yetkileri henüz doğrulanmadı.
- [x] [Resmi pazaryeri dokümanı](https://dev.paytr.com/platform-transfer-talebi), aynı sepette birden fazla satıcıyı, farklı satıcı komisyonlarını ve parçalı iadeyi anlatıyor. [Transfer API'si](https://dev.paytr.com/platform-transfer-talebi/transfer-talimatinin-verilmesi) sipariş ve benzersiz transfer numarasıyla tutar/IBAN talimatı alıyor; çok satıcılı örnekte her satıcı için ayrı `trans_id`, `submerchant_amount` ve `total_amount` ile talimat veriliyor. Satıcı hakedişi ve farklı komisyonları Systemcel hesaplamalı. Aynı gün transfer talebi yapılamıyor; en erken ertesi gün ve istenen aktarım gününde saat 10.00'a kadar talimat gerekiyor. [Transfer sonucu bildirimi](https://dev.paytr.com/platform-transfer-talebi/transfer-talimatinin-sonucunun-alinmasi) isteğe bağlıdır: tamamlanan `trans_id` değerleri mağazanın panelde tanımladığı URL'ye imzalı POST ile gönderilir; `OK` yanıtı alınmazsa PayTR bildirimi tekrarlar. API talimatına başarılı yanıt ile transferin tamamlanması ayrı aşamalardır. [İade API'si](https://dev.paytr.com/iade-api) tutarın bir kısmını iade etmeyi destekliyor. Bunlar genel teknik doküman kanıtıdır, bizim mağazanın canlı yetki veya sözleşme kabulü değildir.
- [x] [Pazaryeri Durum Sorgu API'si](https://dev.paytr.com/durum-sorgu) sipariş bazında `submerchant_payments` ve iadeleri döndürüyor; [Pazaryeri Ödeme Detay servisi](https://dev.paytr.com/odeme-rapor-servisi/odeme-detayi) günlük alt satıcı transferlerini raporluyor. [Geri dönen transferleri listeleme](https://dev.paytr.com/platform-transfer-talebi/geri-donen-odemeleri-listele), [alt hesaptan yeniden gönderme](https://dev.paytr.com/platform-transfer-talebi/geri-donen-odemeleri-hesaptan-gonder) ve [sonuç callback'i](https://dev.paytr.com/platform-transfer-talebi/geri-donen-odemeleri-hesaptan-gonder-2) ayrıca belgelenmiş. Bunlar transfer bildirimine ek durum ve mutabakat yollarıdır; gerçek mağaza yanıt şeması test modunda doğrulanmalıdır.
- [x] [Kayıtlı kartla tekrarlayan ödeme API'si](https://dev.paytr.com/direkt-api/kart-saklama-api/kayitli-kart-tekrarlayan-odeme) teknik olarak mevcut; işlem Non3D yürür ve mağazada Non3D yetkisi gerekir. Systemcel mağazasına bu yetkinin tanınıp tanınmadığı hâlâ açık.
- [x] PayTR'ın 24 Eylül ikinci yanıtına göre transfer talimatı, ödeme istenen gün en geç 10.00'da Transfer API'sine gönderilmeli; daha geç gelen talimatlar ertesi gün işleme alınır. İtiraz ve chargeback sorumluluğu pazaryerine aittir. Test bilgileri mağaza başvurusu alınıp mağaza test moduna geçince iletilir. Bunlar genel e-posta açıklamalarıdır; ayrıntılı sözleşme ve mağaza yetkisi hâlâ doğrulanmalıdır.
- [x] Kullanıcının PayTR görüşmesinden aktardığına göre alt satıcı transferi pazaryeri onay verene kadar bekliyor; net bir azami süre söylenmedi. Örneğin 7 günlük valörle çalışılıyorsa 7. gün onay verilmediğinde ödeme bir sonraki 7 günlük döneme kalıyor. Bu sözlü bilgi henüz PayTR e-postasında veya mağaza sözleşmesinde doğrulanmadı; "koruma/emanet hesabı" niteliği olarak yorumlanmamalı.
- [x] 25 Eylül'de Clerk production üzerinden `gonca.aydin@paytr.com` adresine Systemcel kullanıcı daveti gönderildi; Clerk daveti beklemede ve 25 Ekim'de sona erecek. Ayrı `PayTR Test İşletmesi` oluşturuldu, ancak ücretsiz planın 1/1 kullanıcı sınırı bu işletmeye ikinci kullanıcı davetini engelledi. PayTR'a mevcut işletme verilerine erişim verilmedi.
- [ ] Gonca Hanım'ın Clerk davetini kabul edip kendi Systemcel test alanına giriş yaptığını doğrula. Hazırlanan ortak demo işletmesine erişim gerekiyorsa kullanıcı kapasitesini meşru plan üzerinden açıp yalnız o işletmeye sınırlı davet gönder; ayrı test hesabının erişim ve iptal akışını doğrula.
- [ ] PayTR'dan şu noktaları yazılı teyit et: onay bekleyen tahsilatın hangi hesapta ve hangi koşullarda tutulduğu; 7 günlük dönem örneğinde onay 8. gün verilirse aktarımın tam günü, dönemlerin nasıl tekrarlandığı ve varsa sözleşmesel üst sınır; transfer sonucu bildirimi, `submerchant_payments` sorgusu ve günlük rapordaki durumların gerçek mağazadaki kesinlik anlamı; teslim edilmeme itirazında hangi kanıtların incelendiği, bloke/rezerv ve mali yük; alt satıcı doğrulama belgeleri, güvenli evrak yükleme bağlantısı ve entegrasyon desteği; Systemcel abonelikleri için aynı mağazada kart saklama ve Non3D yetkisi. Mağaza başvurusunun test moduna geçişini ve test bilgilerinin teslimini takip et. Yanıtları sözleşme ve hesap yetkileriyle karşılaştır. Transfer dokümanındaki stopaj hesabı ve PayTR komisyonunun faturalandırılmasını mali müşavirle ayrıca doğrula.
- [x] PayTR canlı mod ön kontrolü için iletişim sayfasına işletme unvanı, vergi bilgileri, açık adres, telefon ve e-posta eklendi. Herkese açık pazaryeri satış, teslimat/kargo, iptal/iade koşulları ile sepet ve altbilgi bağlantıları kodlandı. Ürün kartı KDV dahil fiyatı ve varsa satıcıya özgü sevkiyat/iade koşullarını gösteriyor; demo tedarikçiler herkese açık katalogdan dışlanıyor. Web lint/build, API Release build ve hedefli tarayıcı kontrolü geçti.
- [ ] Bu sayfalar canlıya yayımlandıktan sonra PayTR'ın adres, iletişim, satış, teslimat ve iade bağlantılarını erişim olmadan görebildiğini doğrula. Gerçek canlı katalogda test/demo ürün veya kategori bulunmadığını ayrıca kontrol et; satış koşullarının son metnini hukuk/finansla onayla. Bu çalışma, PayTR canlı ödeme yetkisi veya gerçek ödeme entegrasyonu anlamına gelmez.

### Sıradaki yazılım işleri

1. **P0:** Kategori bazlı sevk/kabul kuralının gerçek kategori ve depo pilotuyla kabulünü tamamla. Yapılandırılabilir lot/seri, tartım toleransı ve ek belge doğrulaması kodu/testi yerelde tamamlandı.
2. **P0:** Tutar/risk eşiği ve karar yetkisi netleşince çift onay, süreli çevrimdışı taslak ve çakışma inceleme akışını tamamla.
3. **P0:** Ret farkının stok uzlaştırmasını pilotta doğrula; sonradan değişen kararda ters stok, cari, fatura ve hakediş hareketlerini üret. Kayıp/iade/yeniden sevk için yönetici ekranı, idempotency, denetim izi ve açık ret stoku varken itirazı kapatmama kuralı yerelde tamamlandı.
4. **P0:** İtiraz SLA'sı, risk puanı ve manuel inceleme kuyruğunu sözleşme eşikleriyle uygula. Yönetim kuyruğunda itiraz yaşı ve ilk yanıt süresinin salt okunur hesabı yerelde tamamlandı; kabul/itiraz eşzamanlılığı izole PostgreSQL'de doğrulandı.
5. **P1:** İletişim filtresinin insan incelemesini, şube/transfer stok maliyetini, yarım kalan eski aktarım tekrarını ve aynı dövizli banka adaylarını gerçek pilot verisiyle kabul et. Kod ve hedefli testler yerelde tamamlandı; canlı kabul açık.
6. **P2:** Çoklu şube/kur konsolidasyonu, OAuth/webhook portalı ve müşteri sağlığı/destek SLA otomasyonunu ayrı kabul senaryolarıyla geliştir.

### 24 Eylül 2026 ekran düzeltmeleri (yerel)

- [x] Net kâr dönem filtresindeki yerel seçim listesini kartın kendi menüsüyle değiştir; seçeneklerin kart tarafından kesilmesini önle. Açık/koyu tema ve 820/1366 px tarayıcı kontrolü geçti.
- [x] Ödeme dağılımı toplamı `0,00 TL` iken büyük düz renkli daire yerine `Henüz ödeme yok` durumunu göster; ödeme varsa pasta grafiğini koru. Birim ve tarayıcı kontrolleri geçti.

Bu düzeltmeler için canlı tarayıcı kabulü yayın sonrası ayrıca yapılmalıdır.

### 25 Eylül 2026 yerel doğrulama

- [x] Ret stok uzlaştırması yönetici ekranında masaüstü açık/koyu tema ve 320/360 px mobil görünümde doğrulandı; hedefli tarayıcı akışı 5/5 geçti. Bekleyen ret stoku bulunan itiraz, yeniden teslim dışındaki kararla kapatılamıyor; servis testleri 31/31 geçti.
- [x] Muhasebeci iletişim inceleme kuyruğunda eşzamanlı yönetici kararının tek sonuç üretmesi hedefli servis testlerinde doğrulandı (2/2). İncelemedeki istekler artık ikinci başvuruyu engelliyor; müşteri ekranı inceleme durumunu gösteriyor ve henüz açılmamış sohbete yönlendirmiyor. Hedefli web testleri 5/5, ilgili servis testi 1/1, lint ve web build geçti. Muhasebeci pazaryeri özelliği hâlen bayrakla kapalı; canlı kullanıcı kabulü açık.
- [x] QR mal kabulü ile itiraz açma yarışı ayrı SQLite bağlantılarıyla ve izole PostgreSQL'de geçti. PostgreSQL'de eşzamanlı sevk testi de geçti (2/2); geçici test konteyneri kaldırıldı.
- [x] Yayın öncesi yerel doğrulamada web lint, tip kontrolü, 160 test ve üretim derlemesi; .NET Release derlemesi ve 385 test geçti. Yeni PostgreSQL yarış testleri izole veritabanında 2/2 geçti ve PostgreSQL CI işine eklendi. Tam Playwright paketi 307 başarılı, 473 proje koşuluyla atlanan senaryoyla geçti. Uzak yedek, monitoring collector/alarm ve deploy-bundle smoke testleri de geçti.
- Aşağıdaki canlı yapılandırma değişiklikleri önceki yayımlı sürüm üzerinde yapıldı; uygulama bildirim akışı ve çoklu işletme AI kabulü ayrıca açık.

### 25 Eylül 2026 canlı işletim kontrolü

- Zoho hesabına giriş açıldı; `merhaba@systemcel.app` ana adresi ve `destek@systemcel.app` takma adı görüldü. `destek@systemcel.app` adresine gönderilen Zoho parola sıfırlama iletileri gelen kutusunda doğrulandı. Bu, Systemcel uygulamasından SMTP teslimi kanıtı değildir.
- Oracle Cloud Shell'de SSH anahtarı olmadığından VM erişimi `Permission denied (publickey)` döndü; kullanıcının İndirilenler klasöründeki mevcut anahtarla yetkili SSH erişimi doğrulandı. Yeni erişim anahtarı açılmadı.
- Kullanıcı onayıyla Zoho üretim uygulama parolası oluşturuldu ve Oracle VM'nin yalnız sahibi tarafından okunabilen `.env` dosyasına kaydedildi. `smtp.zoho.eu:587` STARTTLS ile VM'den gönderilen kontrollü test, `merhaba@systemcel.app` adresinden `01burark@gmail.com` gelen kutusuna 25 Eylül 02:23'te ulaştı. Uygulama yeniden başlatıldı ve readiness başarılı. 25 Eylül'de kendi aktif işletme üyeliğine bağlı idempotent bir test satırı canlı bildirim outbox'ına eklendi; uygulama `Eposta` satırını `TeslimEdildi` durumuna taşıdı ve Gmail gelen kutusunda 15:50'de görüldü. Fiyat değişikliği ve yenileme olaylarının kendi iş akışından teslim kabulü açık.
- Canlı Jev arızasının somut nedeni VM'de `TYPESAFE_API_KEY`/`Jev__ApiKey` bulunmamasıydı. Kullanıcı onayıyla yeni TypeSafe anahtarı oluşturuldu, doğru `criteria` alanlı tanı isteği başarıyla yanıtlandı, anahtar VM'ye kaydedildi ve uygulama readiness geçti. Gerçek kullanıcı oturumundaki işletme sorusu artık işletme verisine dayalı yanıt verdi; çoklu işletme/tenant ayrımı ve öneri karar akışlarının canlı kabulü açık. `criteria` alanını sabitleyen .NET testi dâhil Jev testleri 9/9 geçti.
- `systemcel-monitoring.timer` etkin; readiness ve üç konteyner çalışıyor. Şifreli Oracle Object Storage yedeği sonrası collector'da `systemcel_offsite_backup_state_valid=1` doğrulandı. Metriklerin kalıcı dış hedefe aktarımı ve eşik alarmları açık.
- Oracle Frankfurt'ta yalnız `systemcel-free` VM'sine tanımlı, silme yetkisi olmayan bucket erişimiyle private `systemcel-backups-2026` hedefi kuruldu. `systemcel-backup.service` elle ve 25 Eylül 03:02 UTC'deki ilk zamanlanmış çalışmasında başarılı oldu; `systemcel-20260925T030214Z` paketi ve tamamlanma işareti doğrulandı. Şifre çözme kurtarma bilgisi VM'nin okuyamadığı Oracle Vault `systemcel-recovery` içinde Active durumda. Yalnız uzak paketin izole PostgreSQL konteynerine geri yüklemesi 73 tabloyla geçti; bu örnekte indirme ve restore 4 saniye sürdü. Üç ardışık günlük çalıştırma ve farklı makinede tam felaket kurtarma provası açık.
- OneDrive hedefinde boş alan `0 B` olduğundan yedek hedefi olarak kullanılmadı. Deneme için verilen rclone Microsoft erişimi hesaptan geri alındı ve VM'deki kullanılmayan OneDrive remote'u kaldırıldı.
- Disk >%70/%85, doğrulanmış uzak yedek yaşı >26/36 saat ve collector durması için beş dakikalık yerel SMTP alarm timer'ı 25 Eylül'de etkinleştirildi. Sahte metrik ve ayrı durum dosyasıyla birleşik kritik/toparlanma iletileri gönderildi; ikisi de Gmail'de görüldü. Kritik ileti önce Spam'e düştü ve “Spam değil” olarak işaretlendi; Gmail, bu gönderenden sonraki iletileri Gelen Kutusu'na alacağını bildirdi. Yerel rota VM kapalıyken çalışmaz; kalıcı dış metrik hedefi ve ikinci kanal ayrıca açık.
- Oracle `instance_status > 0` (5 dakika), `CpuUtilization > 95` (10 dakika) ve `MemoryUtilization > 90` (5 dakika) kritik alarmları yalnız Systemcel VM'si için etkin; üçünün de mevcut durumu `Ok`. `01burark@gmail.com` aboneliği Active ve kontrollü Oracle test iletisi gelen kutusunda doğrulandı. UptimeRobot ücretsiz 5 dakikalık `https://systemcel.app/api/health/ready` dış kontrolü etkin ve durumu `Up`; test amaçlı UP ve DOWN bildirimleri 25 Eylül 15:06'da Gmail'e ulaştı. Yerel disk/yedek alarmının kontrollü kritik ve toparlanma iletileri de Gmail'de görüldü. Gerçek kesinti/düzelme olayı ve ikinci alıcı/kanal açık.

### 24 Eylül 2026 canlı işletim kontrolü (yerel)

- DeepSeek Chat hesabındaki “Herkes için modeli geliştir” seçeneği kapatıldı ve kapalı konumu ekranda doğrulandı. Bu hesap tercihi API veri işleme sözleşmesi ile KVKK/hukuk onayını kapatmaz.
- Gerçek oturumda işletmeyle ilgili AI sorusu “Sorunun işletmeyle ilgili olup olmadığını şu anda doğrulayamıyorum” yanıtını verdi. Jev yönlendirmesi canlıda kullanılamıyor; sağlayıcıda son 24 saatte başarılı istek görünmedi. Anahtar/istek ve sunucu logu doğrulaması açık.
- Jev istemcisinde ağ hatası, sağlayıcı zaman aşımı ve bozuk JSON yanıtı artık güvenli `karar_yok` sonucuna dönüyor; kullanıcı isteğini iptal ederse iptal korunuyor. Yerel hedefli testler 9/9 geçti; canlı Jev arızasının nedeni henüz belirlenmedi.
- Oracle `systemcel-free` örneği çalışıyor, CPU/RAM metrikleri geliyor. Başlangıçta OCI boot volume yedek listesi boştu ve otomatik yedek politikası atanmamıştı. 24 Eylül'de `systemcel-free-manual-2026-09-24` adlı tam yedek oluşturuldu; `Available`, 15/47 GB ve 30 gün saklama (24 Ekim 2026 bitiş) doğrulandı. Bu tek seferlik, çökme tutarlı volume anlık görüntüsü; uygulamanın ayrı PostgreSQL/uygulama verisi yedek zamanlayıcısı, bağımsız uzak hedefi ve geri yükleme kanıtının yerine geçmez.
- Oracle ana sayfasında root compartment için alarm görünmüyor. Kalıcı alarm/VM dışı probe ve iki kanalda teslim kanıtı açık.
- Oracle Instance Run Command ile sır değerlerini yazdırmayan salt okunur tanı komutu istendi; komut `Accepted` durumunda kaldı ve yanıt üretmedi. Agent eklentisi IAM politikasını kontrol etmeyi öneriyor; ayrıca VM'nin Ubuntu 24.04 imajı [Oracle'ın Run Command için listelediği hazır imajlar](https://docs.oracle.com/en-us/iaas/Content/Compute/Tasks/runningcommands.htm) arasında yok. IAM erişimi genişletilmedi. VM içindeki yedek timer, `rclone` ve Jev anahtarının canlı varlığı bu yolla doğrulanamadı; desteklenen alternatif erişim gerekli.
- Telegram uygulama ayarında bot bağlantısı yok; kullanıcı isteğiyle bu turun dışında bırakıldı, gerçek sohbet/AI kabulü açık. Gmail gelen kutusundaki eski manuel teslim testi, güncel uygulama SMTP ve yenileme bildirimi kabulü sayılmıyor. Zoho yönetim oturumu mevcut Zoho parolasıyla doğrulama bekliyor.

## 1. P0 — Yayın öncesi temel işler

### 1. Aday sürüm, hesap ve temel kullanım kabulü

- [ ] Önceki doğrulanmış sürüme şema uyumlu geri dönüşü izole ortamda prova et. Aday `5e43c7c`, başarılı CI #35803872143 ve canlı deploy #35804918141 aynı SHA'da eşleşti; public smoke geçti.
- [ ] Sıfırdan yeni kimlikle Clerk kayıt → provision → kolay kurulum smoke testi çalıştır.
- [ ] Canlı SMTP ve genel uygulama outbox'ı kontrollü testle Gmail'e teslim edildi. Fiyat değişikliği ve yenileme bildiriminin kendi olay akışından teslimini ayrıca kanıtla.
- [ ] Gerçek oturumla işletmeye bağlı asistan sorusu 25 Eylül'de Jev ve DeepSeek üzerinden yanıtlandı. Başka işletmenin verisinin yanıta karışmadığını kontrollü ikinci tenant ile doğrula. Sınırsız AI paketlerindeki 15 mesaj/saat sınırı kaldırıldı; işletmeyle ilgili soru ve takip sorusu kararını yalnız Jev veriyor. Jev kullanılamazsa asistan işletme verisiyle yanıt üretmiyor ve mesaj hakkı tüketmiyor. Bağlam belirteci kullanıcıya ve işletmeye bağlı; ilgili .NET testleri, CI ve önceki yayın geçti. Canlı çoklu tenant kabulü açık.
- [ ] Jev önerilerini gerçek kullanıcı senaryolarında doğrula: düşük güvenli öneriyi incelemeye yönlendirme, kullanıcı onayı, yanlış öneriyi reddetme ve tekrar işlemde mükerrer kayıt oluşmaması. Yeni karar akışları için kod/test mevcut; canlı kabul açık.
- [ ] İşletme ve muhasebeci sohbetlerinde kontrollü dosya gönderme/indirme kabulünü tamamla; dosya ve hedef kullanıcı onayı bekliyor.
- [ ] Kritik akışlarda kalan klavye sırası, focus, Escape ve boş/yükleniyor/hata durumlarını doğrula. Plan penceresinin odak kapanı ve geri dönüşü, bildirim hata/yeniden deneme, yönetim inceleme durumları ve mobil mal kabulün ilgili Playwright akışları 25 Eylül'de geçti; diğer rotaların kapsamı açık.
- [ ] Fiziksel iOS/Safari smoke'unu sınırlı pilot sırasında çalıştır; WebKit emülasyonunu gerçek cihaz kanıtı sayma.

### 2. Yedek, izleme ve veri işleme

- [x] `rclone crypt` akışını private Oracle Object Storage'a bağla, kurtarma parolası/salt'ını ayrı Vault'ta sakla; elle ve ilk zamanlanmış systemd yedeğini, yalnız uzak paketten izole PostgreSQL restore'unu doğrula. 25 Eylül'deki zamanlanmış pakette 73 tablo ve 4 saniyelik indirme/restore süresi ölçüldü. [Kurtarma denemesi](deployment/oracle-free/scripts/verify-offsite-restore.sh).
- [ ] Üç ardışık günlük uzak yedeği ve farklı makineden tam felaket kurtarma provasını gör; gerçek işlem bazlı RPO ile kabul RTO'sunu ölç, 14 günlük uzak saklama kuralını kararlaştır.
- [x] Oracle VM altyapı, CPU ve bellek alarmlarını aç; Gmail aboneliğini onayla ve kontrollü test bildiriminin teslimini doğrula. UptimeRobot ile VM dışı `/api/health/ready` kontrolünü etkinleştir; durum `Up`, test UP/DOWN e-postaları Gmail'e ulaştı.
- [ ] Yerel collector metriklerini kalıcı dış hedefe bağla. Disk ve yedek yaşı için yerel SMTP alarmı, sahte kritik/normal metriklerle Gmail tesliminde doğrulandı; alarmın smoke testi CI altyapı işine eklendi. Canlı VM'de kurulu kopyanın sonraki kod değişiklikleriyle güncellenmesi yayın adımı olarak takip edilmeli. Gerçek kesinti/düzelme mesajlarını ikinci bağımsız alıcı veya kanalla doğrula; Telegram kullanıcı isteğiyle kapsam dışında.
- [x] DeepSeek Chat hesabında model geliştirme için veri kullanımı kapatıldı; ayarın kapalı olduğu 24 Eylül 2026'da ekranda doğrulandı.
- [ ] DeepSeek API veri işleme koşullarını, Çin'de veri işleme/saklama ihtimalini ve yurt dışı aktarım dayanağını alt işleyen/KVKK metinlerinde hukuk onayıyla güncelle. Chat hesap ayarı tek başına bu işi kapatmaz.

### 3. Ücretli yayın — sağlayıcı ve belge doğrulamaları

Yasal hazırlık kaydında işe başlama tarihi 14 Eylül 2026. PayTR pazaryeri teklif kapsamını e-postayla doğruladı; hesap erişimi ve gerçek ödeme kabulü açık.

- [ ] Sunulan belgelerde bulunmayan MERSİS/ticaret sicil ve KEP bilgisinin şahıs işletmesi için gerekliliğini doğrula; varsa yasal metinlere ekle.
- [ ] PayTR başvuru sonucunu ve mağazaya tanımlanan ödeme yeteneklerini doğrula; test mağazası/erişim açıldıktan sonra gerçek sağlayıcı adapter'ını ve sandbox sözleşme testlerini tamamla.
- [ ] Canlı checkout, imzalı webhook, yenileme, başarısız tahsilat, iade ve mutabakatı gerçek sağlayıcıda doğrula; 30 günlük fiyat korumasının gerçek tahsilat yolunda da uygulandığını kanıtla.
- [ ] Systemcel'in kendi abonelik satış belgesi sürecini netleştir: manuel veya otomatik düzenleme yöntemini seç; tahsilat–belge referansı, müşteriye sunma ve iade/iptal bağlantısını doğrula. Uygulamada müşterilerin kestiği faturalar bu işten ayrıdır.
- [ ] Tedarikçi ödemeleri için mağazaya tanımlanan pazaryeri transfer yetkisini, teslimat sonrası bekletme koşullarını ve kısmi hakediş/iade davranışını sözleşme ve sandbox'ta doğrula. Genel API dokümanını canlı hesap yetkisi sayma; abonelik tekrarlayan tahsilatını ayrıca doğrula. Ayrıntılı kabul maddeleri aşağıdaki tedarikçi bölümünde.

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
- [x] Ürün kategorisine göre yapılandırılabilir zorunlu sevk alanı kuralları eklendi; yerel servis testleri geçti. Pilot kategori yapılandırması ve canlı kabul ayrıca açık.
- [x] Sevkiyat QR'ı opak rastgele kodla üretildi; fiyat veya hassas işletme verisi QR içeriğine yazılmıyor, çözümleme işletme erişimine bağlı.
- [x] e-Belge adapter'ına tenant ve idempotency bağlı e-İrsaliye gönderme, durum sorgulama ve yanıt alma sözleşmesi eklendi; gerçek sağlayıcı erişimi açılana kadar yapılandırılmamış adapter güvenli hata döndürüyor.
- [x] Kağıt irsaliye veya entegrasyonsuz tedarikçi için dosya imzası/boyutu doğrulanan fotoğraf/PDF yükleme ve manuel belge numarası yedeği eklendi.

### Depo mal kabulü

- [x] `Depo sorumlusu` ve `Mal kabul onaylayıcısı` rolleri davet/rol ekranında şube-depo kapsamıyla tanımlandı; API bu kapsamı uygular ve tedarikçi işletme alıcı adına kabul veremez.
- [x] QR çözümleme, kodla mal kabul ve sipariş kaleminde sevk/kabul/ret miktarlarının gösterimi eklendi.
- [ ] Kamera ile QR okutma, irsaliye eşleştirme ve beklenen/gelen miktar karşılaştırmasını gerçek mobil cihazda tamamla.
- [x] QR etiketi bazında tam/kısmi kabul, ret, ret nedeni ve not kaydı eklendi; tekrar işlem aynı kabulü çoğaltmıyor.
- [x] Mal kabul kaydına QR etiketi, alıcı işletme ve şube/depo rolü erişimine bağlı, dosya imzası ve boyutu doğrulanan JPG/PNG/WEBP/PDF kanıt yükleme eklendi; dosya yolu kabul denetim izinde saklanıyor.
- [x] Tartım, sıcaklık, lot/seri, son kullanma tarihi ve ek belge için kategoriye bağlı sevk/kabul doğrulaması eklendi; ağırlık toleransı yapılandırılabiliyor. Yerel servis testleri geçti; gerçek kategori ve cihaz kabulü açık.
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
- [ ] Taraflara yanıt süresi ve yönetici inceleme SLA'sı tanımla; süre dolunca otomatik para aktarımı yerine risk kuralına göre üst inceleme veya sözleşmesel karar uygula. Yönetim kuyruğunda itiraz yaşı ve yanıtlanan ilk şikâyetin yanıt süresi salt okunur hesaplanıyor; eşik, otomatik üst inceleme ve sözleşmesel karar açık.
- [ ] Sürekli asılsız itiraz, sürekli eksik sevk ve olağandışı kabul/red örüntüleri için alıcı/tedarikçi risk puanı üret.
- [ ] Riskli hesaplarda daha uzun bloke, çift onay, işlem limiti veya manuel inceleme uygula; otomatik kalıcı yaptırım verme.

### Muhasebe, belge ve stok bağlantıları

- [x] Alıcı stok girişi, alış faturası ve borç carisi her kısmi kabulde yalnız kabul edilen miktar/tutar üzerinden oluşturuluyor.
- [x] Tedarikçi stok çıkışı peşin/vadeli ödeme türünden bağımsız olarak sevk edilen miktar kadar rezervasyondan ve stoktan düşüyor; kaynak stok hareketi sevkiyat referansıyla yazılıyor.
- [x] Ret kaydı başına yönetici kararıyla kayıp, tedarikçiye fiziksel iade veya yeniden sevk stok uzlaştırması; idempotency, denetim alanları, migration ve yönetici ekranı eklendi. Bekleyen ret stoku varken itirazın yeniden teslim dışındaki kararla kapanması engellendi. Geçmiş `YenidenTeslim` kayıtları manuel incelemeye ayrıldı. Yerel servis/tarayıcı testleri geçti; pilot operasyon kabulü açık.
- [ ] Kısmi kabul/red için e-İrsaliye yanıtı ve gerekiyorsa iade e-İrsaliyesi/e-Fatura süreçlerini e-Belge adapter'ına bağla.
- [ ] Sonradan değişen kabul kararlarında silme yerine ters stok, cari, fatura ve hakediş kayıtları üret.
- [ ] Aynı sipariş, irsaliye, fatura, cari, stok hareketi, PSP tahsilatı ve hakediş arasında izlenebilir referans zinciri kur. Yeni stok, cari ve tahsilat hareketlerine sipariş/kabul, stok çıkışına sevkiyat yabancı anahtarları eklendi; fatura eşlemesi, PSP dağıtımı ve hakediş yönetim inceleme ekranında aynı sipariş altında gösteriliyor. Eski hareketlerin güvenli geriye dönük eşlemesi ve canlı PSP referans doğrulaması açık (23 Eylül 2026).

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
- [ ] Yeniden sevkin gerçek depo pilotundaki kabulünü tamamla. Yeniden sevk akışı, ayrı SQLite bağlantılarında tek başarılı sevk/tek stok düşümü ve izole PostgreSQL'de eşzamanlı sevk yarışı doğrulandı.
- [x] Yabancı işletmenin QR erişimi, aynı kabulün tekrar gönderimi, tam kabulde tek stok/muhasebe işlemi ve rette hakediş blokesi için regresyon testleri eklendi.
- [x] Depo/mal kabul rolü sınırı, yabancı tenant ve aynı işlem anahtarıyla mükerrer QR regresyonları eklendi.
- [x] Farklı işlem anahtarıyla aynı QR'ın ikinci kez kabul edilmesi regresyon testinde reddediliyor.
- [x] Aynı QR'ın farklı işlem anahtarlarıyla eşzamanlı kabulünde tek stok ve muhasebe etkisi ayrı SQLite bağlantılarıyla; QR kabulü ile itiraz açma yarışında tutarlı durum ve hakediş hem SQLite hem izole PostgreSQL'de doğrulandı.
- [ ] PSP tahsilat/serbest bırakma/iade webhook tekrarları, zaman aşımı ve günlük mutabakat farkını sağlayıcı sözleşme testleriyle doğrula.
- [ ] Mobil mal kabul akışını çevrimdışı taslak, gerçek cihaz ve depo eldiveniyle kullanılabilir hedef boyutlarıyla tamamla. 320 px Chromium ve mobil WebKit'te kamera izni reddi, QR koduyla kabul, iki ret nedeni ve alan hizası 25 Eylül Playwright koşusunda yeniden geçti. Emülasyon fiziksel cihaz kanıtı değildir.
- [x] Pazaryeri API'si `Pazaryeri:Aktif` ve `Pazaryeri:PilotIsletmeIdleri` yapılandırmasıyla tamamen kapatılabilir veya işletme izin listesine sınırlandırılabilir duruma getirildi.
- [ ] Canlı yapılandırmada tek tedarik zinciri ve sınırlı depo pilot işletmelerini seç; kısmi kabul ve muhasebe mutabakatı kanıtlanmadan izin listesini genişletme.
- [ ] Pilot çıkışında hukuk/finans, operasyon, muhasebe, güvenlik ve geri alma runbook onaylarını yayın kaydına ekle.

## 3. P0 — Son kabul ve yayın kararı

- [ ] Sınırlı gerçek kullanıcı pilotunu tamamla; hata oranı, aktivasyon ve destek yüküne göre genel yayın kararı ver. Önceki işletme/muhasebeci senaryo pilotları tamamlandı; bu madde genel yayın kararını kapatır.

## 4. P1 — Yayından sonra iyileştirmeler

Tedarikçi pazaryeri, sevkiyat ve mal kabul 21 Eylül kullanıcı kararıyla P0'a alındı. Aşağıdaki işler genel yayın sonrasındaki sıradır; ilk yayında kullanılan depo/rol, stok ve bildirim parçaları P0 kabulüne dahildir.

1. [ ] Üyelik/rol/sahiplik: mevcut davet, rol değiştirme, üye kaldırma ve sahiplik devrini gerçek hesaplarla doğrula; açık sekmede yetki kaldırma ve arayüz kabulünü tamamla.
2. [ ] Bildirim operasyonu: işletme/kullanıcıya bağlı Telegram eşleştirmesi ve başarısız bildirimleri kontrollü yeniden gönderme görünürlüğü. Yöneticiye başarısız teslim listesi ve kontrollü yeniden deneme eklendi. Telegram eşleştirmesi ve bildirim gönderimi kullanıcı/işletme bazında saklanıyor; kodlu eşleştirme ve ayrılma test edildi, QR yerel üretiliyor. Bağlı özel sohbette Systemcel AI soruları aktif üyelik ve son bağlanan işletme kapsamında çalışıyor; mobil bağlantı ekranı açıldı. Kod, test, CI ve yayın geçti; eski global bot komutları çoklu işletme bağlamına taşınmadı. Canlı botla gerçek sohbet ve kullanıcı kabulü açık (24 Eylül 2026). Canlı SMTP teslimi P0'da.
3. [ ] Hata ve kullanım görünürlüğü: mevcut istek kimliği/log altyapısını kalıcı hata izlemeye bağla; aktivasyon ve ürün dönüşüm ölçümlerini tamamla. Altyapı alarmları P0'da.
4. [ ] Banka eşleştirme: mevcut CSV, aday önerisi ve insan onayını gerçek anonimleştirilmiş dosyalarla doğrula; kısmi/toplu eşleştirme kapsamını netleştir. TRY dışı hareketler aynı para birimindeki fatura/ödeme/cari kayıtlarıyla aday eşleştiriliyor; kur çevrimi yapılmıyor. Yerel hedefli testler 8/8 geçti; gerçek dosya kabulü açık.
5. [ ] Eski veri aktarımı: mevcut önizleme/uygulama ve akıllı alan eşleştirmesini gerçek formatlar, yarım kalan aktarım ve tekrar denemeyle doğrula; masaüstü aracı sürümle ve imzala. Satır hatasında taslak korunuyor ve başarılı satırlar tekrar uygulanmıyor; yerel hedefli testler 19/19 geçti. Gerçek veri kabulü ve masaüstü araç yayını açık.
6. [ ] Stok defteri: mevcut depo, rezervasyon, transfer, sayım ve ters kaydı pilotta doğrula; kalan konum, maliyet ve mutabakat ihtiyaçlarını tamamla. Şubeler arası alış geçmişi ve transferde taşınan hareketli ortalama maliyet brüt kâr hesabında düzeltildi; hedefli testler 6/6 geçti. Pilot maliyet mutabakatı açık.
7. [ ] Genel e-Belge kapsamını tamamla: UBL-TR, e-Fatura/e-Arşiv, webhook/polling, iptal/itiraz ve mutabakat. Tedarikçi sevk/kabulü için gereken e-İrsaliye ve iade bağlantıları P0'dadır; bu madde kalan genel kapsamdır.
8. [ ] Pazaryeri iletişim güvenliği: iletişim tespiti ve 30 dakika kuralının mevcut kapsamını doğrula; yanlış pozitif ve insan incelemesini tamamla. Kesin iletişim bilgisi engelleniyor; belirsiz sayısal/sosyal eşleşme yönetici inceleme kuyruğuna alınıyor. Karar ekranı, denetim izi ve mobil/dark görsel kontrol yerelde tamamlandı; canlı gerçek mesaj kabulü açık.
9. [ ] Frontend: mevcut rota bazlı lazy-load'u koru; bundle/CSS ve API yanıt sürelerini ölçerek gerekli modül ve ortak durum bileşeni düzenlemelerini yap. Pazaryeri CSS'i rota ile yüklenir hale getirildi; ilk global CSS 702,85 KB'den 676,97 KB'ye (gzip 115,84 KB'den 112,43 KB'ye) indi. Diğer modüller ve API süreleri açık (23 Eylül 2026).
10. [ ] Mevcut şube/kur temelinin üzerine konsolidasyon, entegrasyon ve Pro muhasebeci otomasyonlarında kalan kapsamı netleştir; P2 işleriyle birlikte planla.

## 5. P2 — Sonraki ürün derinliği

- [ ] Sektör/NACE tabanlı mevzuat ve teşvik bildirimleri; yalnız doğrulanmış kaynaklarla.
- [ ] Gelişmiş stok maliyetleme, performans ve 100 bin+ hareket testleri. Stok bakiyesi veritabanında toplama taşındı ve 100 bin hareketlik SQLite testi eklendi. Brüt kâr hesabında tekrarlı fatura taraması, dönem dışı kayıtlar ve çift stok sorgusu düzeltildi; pazaryeri mal kabulü alış faturasıyla iki kez maliyete eklenmiyor. Şubeler arası alış geçmişi ve transfer maliyeti düzeltildi; gerçek pilot ölçümü ve diğer maliyet yöntemleri açık.
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
- PayTR pazaryeri teklif kapsamı ve ek sabit ücret olmaması e-postayla doğrulandı (24 Eylül kullanıcı tarafından paylaşılan yanıt); mağaza yetkisi ve gerçek tahsilat henüz denenmedi.
- Canlı uygulama Oracle üzerinde çalışır; genel yayın kararı verilmeden ödeme sağlayıcısı `Fake` kalır.
- Canlı AI sağlayıcısı DeepSeek, model `deepseek-flash`tır; anahtar yalnız Oracle `.env` dosyasında tutulur.
- PostgreSQL yalnız `systemcel_app` kullanıcısı ve uygulama trusted source'u üzerinden erişilir.
- OneDrive dışı geri alınabilir geliştirme önbelleği: `C:\Users\Windows\AppData\Local\SystemcelCacheBackups\20260810-1615`
- `YAPILACAKLAR.md` kullanıcı isteği gereği commit edilmez.
