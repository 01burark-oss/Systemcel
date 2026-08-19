# Systemcel — Kompakt Yapılacaklar

> Son güncelleme: 17 Ağustos 2026
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

- [ ] DigitalOcean staging'de API ve web'i ayrı bileşenlere ayır; PostgreSQL yalnız API'ye açık kalsın.
- [ ] Sıfırdan yeni kimlikle Clerk kayıt → provision → kolay kurulum smoke testi çalıştır.
- [ ] Gerçek SMTP teslimini doğrula; fiyat/yenileme bildirimlerinin gönderim kanıtını kaydet.
- [x] Muhasebeci pilotunu; profil görseli yükleme, yönetici onayı, müşteri eşleşmesi ve çalışma alanı geçişiyle tamamla.
- [x] Fake ödeme zincirini işletme ve muhasebeci rollerinde doğrula.
- [ ] Fiziksel iOS/Safari smoke'unu sınırlı pilot sırasında çalıştır.

### Kullanıcı, maliyet veya dış koordinasyon gerektirir

- [ ] DigitalOcean PITR ile yeni PostgreSQL cluster oluştur, uygulamayı yeni cluster'a geçir ve geri dönüşü dene.
- [ ] Sınırlı gerçek kullanıcı pilotunu tamamla; hata oranı, aktivasyon ve destek yüküne göre genel yayın kararı ver.

### Şirket kuruluşundan sonra

- [ ] Şirket unvanı, MERSİS/vergi bilgisi, adres, KEP/e-posta ve destek kanalını yasal metinlere ekle.
- [ ] KVKK, gizlilik, kullanım, mesafeli hizmet, abonelik ve iptal/iade metinlerini hukuk onayından geçir.
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
- [x] DigitalOcean encrypted secret, dar PostgreSQL kullanıcısı/trusted source, liveness/readiness ve CPU/RAM/restart alarmları doğrulandı.
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
- [ ] Pro planı ve ek müşteri kredisi varyasyonları.
- [ ] Başka tenant verisine URL/kimlik değiştirerek erişememe.

### Ortak kalite kapıları

- [x] 320/360/375/390/430 mobil, 768 tablet, 1366/1920 masaüstü, WebKit ve reduced-motion Playwright matrisi geçti.
- [ ] Klavye sırası, focus, modal kapanı, Escape, boş/loading/hata durumları.
- [x] Konsol hatası, yatay taşma ve eski mavi tema kontrolü; canlı konsol temiz.
- [x] Oluşturulan pilot verileri `PILOT` etiketiyle ayrıldı ve pilot raporuna kaydedildi.

### Canlı pilotta kalan somut engeller

- DigitalOcean'a canlı AI anahtarı eklenmeli.
- Gerçek SMTP teslim kanıtı ve sıfırdan yeni kimlik smoke'u alınmalı.

## 5. P1 — P0 sonrasında öncelik

1. Hatırlatma ve bildirim omurgası: outbox, idempotency, retry/dead-letter, sessiz saat, e-posta/Telegram tercihleri.
2. e-Belge sağlayıcı adapter'ı: UBL-TR, e-Fatura/e-Arşiv, webhook/polling, iptal/itiraz ve mutabakat.
3. Stok hareket defteri: depo/konum, rezervasyon, transfer, sayım, ters kayıt, maliyet ve mutabakat.
4. Pazaryeri iletişim güvenliği: iletişim tespiti, 30 dakika kısıt, yanlış pozitif ve insan incelemesi.
5. Banka hareketleri ve insan onaylı cari/fatura eşleştirme.
6. Kullanıcı/rol/sahiplik devri ve üyelik yönetimi.
7. Eski veri aktarım sihirbazı ve imzalı masaüstü araç dağıtımı.
8. Yapılandırılmış log, correlation ID, hata izleme ve ürün dönüşüm metrikleri.
9. Frontend modülerleştirme, lazy-load, ortak durum bileşenleri ve CSS parçalama.
10. Çoklu şube/para birimi, entegrasyon API'leri ve gerçek Pro muhasebeci otomasyonları.

## 6. P2 — Sonraki ürün derinliği

- Sektör/NACE tabanlı mevzuat ve teşvik bildirimleri; yalnız doğrulanmış kaynaklarla.
- Gelişmiş stok maliyetleme, performans ve 100 bin+ hareket testleri.
- API anahtarı/OAuth, webhook abonelikleri ve geliştirici portalı.
- Çoklu şube konsolidasyonu, kur farkı ve çoklu para birimi raporlaması.
- Gelişmiş müşteri sağlık skoru, dönem sonu görevleri ve destek SLA otomasyonu.

## 7. Operasyon notları

- Canlı alan: `https://systemcel.app`
- Şirket öncesi ödeme sağlayıcısı: `Fake`
- Canlı uygulama ortamı şu an `Staging`; genel yayın kararı verilmeden `Production` yapılmaz.
- PostgreSQL yalnız `systemcel_app` kullanıcısı ve uygulama trusted source'u üzerinden erişilir.
- OneDrive dışı geri alınabilir geliştirme önbelleği: `C:\Users\Windows\AppData\Local\SystemcelCacheBackups\20260810-1615`
- `YAPILACAKLAR.md` kullanıcı isteği gereği commit edilmez.
