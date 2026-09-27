# Systemcel ürün kapsamı

Bu dosya güncel ürün kapsamının tek belge kaynağıdır. `plan.md` tarihsel/legacy planlama kaydıdır; `YAPILACAKLAR.md` yalnız uygulanabilir operasyon checklistidir. İlk ücretli yayında ücretsiz deneme yoktur: abonelik, açık onay ve başarılı anlık tahsilatla başlar. Son doğrulanan canlı ödeme sağlayıcısı `Unconfigured` durumundadır; gerçek tahsilat kapalıdır. `Fake` yalnız yerel ve kontrollü test akışlarında kullanılır. PayTR erişimi ve ücretli yayın kapıları `YAPILACAKLAR.md` içinde açıktır.

## Fiyat kaynağı

Fiyatların tek teknik kaynağı `CashTracker.Core/Models/SubscriptionPlanCatalog.cs` içindeki `SubscriptionPlanCatalog` sınıfıdır. Dokümantasyondaki fiyat tabloları kopya kabul edilir ve katalog değiştiğinde güncellenmelidir; istemcinin gönderdiği tutar kabul edilmez. Katalog fiyatları KDV hariçtir.

Kurucu kampanyası katalogdaki `kurucu-100-2026` koduyla ilk 50 işletme hesabında aylık 3 dönem için uygulanır. İşletme planları Başlangıç, Büyüme ve Kurumsal; muhasebeci planları Standart ve Pro'dur. Muhasebeci Standart 10 müşteriyi içerir; ek müşteri kredisi katalog kurallarıyla hesaplanır.

## Lansman kapsamı

Aktif kapsam işletme ve muhasebeci çalışma alanları, tenant izolasyonu, gelir-gider ve cari akışları, ürün/stok, hızlı satış, fatura taslağı, raporlar, muhasebeci eşleşmesi, abonelik özeti, ödeme geçmişi, dönem sonu iptal, Fake ödeme ve katalogdan çözülen plan haklarıdır.

Teknik kapanış; yeni Clerk kimliğiyle kayıt ve kurulum, olay bazlı posta teslimi, iki işletme arasında AI ayrımı, sınırlı gerçek tedarikçi pilotu, fiziksel iOS/Safari kontrolü, tam kurtarma provası ve gerçek PayTR tahsilatını kapsar. Genel SMTP teslimi, gerçek AI yanıtı ve şifreli sunucu dışı yedek temeli doğrulanmıştır; kalan kabul kanıtları `YAPILACAKLAR.md` içindedir. Hizmet sağlayıcının doğrulanan vergi ve adres bilgileri yasal metinlerde yayımlanır; MERSİS/ticaret sicil ve KEP gerekliliği ayrı karar kapısıdır. Gerçek GİB veya Telegram işlemleri yalnız kullanıcı tarafından açıkça yetkilendirilen kontrollü pilotta yapılır.

## Yayın ortamı

Güncel canlı barındırma Oracle Cloud Always Free VM'dir. `deployment/oracle-free/compose.yaml` uygulama, Caddy ve PostgreSQL 18 container'larını çalıştırır; Caddy HTTPS'i sonlandırır, PostgreSQL internete açılmaz. DigitalOcean staging uygulaması ve veritabanı silinmiştir ve canlı geri dönüş hedefi değildir.
