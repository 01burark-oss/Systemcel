# ADR-002 — PostgreSQL zaman damgası sözleşmesi

- **Tarih:** 1 Ağustos 2026
- **Durum:** Kabul edildi

## Bağlam

Mevcut PostgreSQL migration zincirindeki `DateTime` kolonları `timestamp without time zone`
olarak oluşturuluyor. Npgsql 6 ve sonrası, `DateTimeKind.Utc` değerlerini bu kolon tipine
varsayılan davranışla yazmayı reddediyor. Ödeme yaşam döngüsünün gerçek PostgreSQL
doğrulamasında checkout kaydı bu nedenle çalışma zamanında başarısız oldu.

## Karar

- Uygulama, Npgsql veri kaynağı kurulmadan önce
  `Npgsql.EnableLegacyTimestampBehavior` uyumluluk anahtarını etkinleştirir.
- `CashTrackerDbContext`, eklenen veya değiştirilen `DateTime` değerlerini kayıttan önce
  `DateTimeKind.Unspecified` olarak normalize eder.
- Bu davranış, mevcut migration zincirinin veri tipi sözleşmesini korumak içindir; saat
  dilimi dönüşümü yaptığı anlamına gelmez.
- Yeni bir `timestamp with time zone` geçişi ancak ayrı, veri dönüşümü içeren bir migration
  ve geriye dönük uyumluluk planıyla yapılır.

## Doğrulama

- Boş PostgreSQL 18 veritabanında 13 migration baştan sona uygulandı.
- Eski abonelik ve mükerrer deneme verisi içeren temsili veritabanı son migration'a taşındı;
  abonelik verisi korundu ve mükerrer deneme kaydı tekilleştirildi.
- Gerçek PostgreSQL üzerinde idempotent checkout, kart doğrulama, abonelik özeti, ödeme
  geçmişi ve dönem sonu iptal akışları çalıştırıldı.
- Entegrasyon testi, kaydedilen ödeme zaman damgasının `Unspecified` türünde olduğunu
  doğrular.

## Sonuçlar

Bu karar mevcut şemayla güvenli çalışma sağlar. Gelecekte kolonlar `timestamp with time
zone` tipine geçirilecekse normalizasyon ve uyumluluk anahtarı aynı paket içinde yeniden
değerlendirilmelidir.
