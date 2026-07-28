# ADR-001 — Ödeme sağlayıcısı seçimi

- **Tarih:** 22 Temmuz 2026
- **Durum:** PayTR seçildi; entegrasyon şirket kuruluşu ve üye işyeri onayına kadar beklemede.
- **Karar sahibi:** Systemcel

## Bağlam

Systemcel, işletme ve muhasebeci planları için aylık/yıllık tahsilat, yenileme,
iptal, iade ve ödeme geçmişi akışlarına ihtiyaç duyacak. Fiyatlar yalnızca
sunucudaki `SubscriptionPlanCatalog` kaynağından hesaplanmalı; istemcinin
gönderdiği tutar kabul edilmemeli.

Şirket kuruluşu ve PayTR üye işyeri hesabı henüz tamamlanmadığından, canlı kart
tahsilatı ve gerçek webhook anahtarları bu aşamada eklenmeyecek.

## Karar

Ödeme sağlayıcısı olarak **PayTR** kullanılacak.

Entegrasyon aşağıdaki sınırlarla geliştirilecek:

- Sağlayıcı ayrıntıları `IPaymentProvider` arkasında tutulacak; uygulama
  servisleri doğrudan PayTR istemcisine bağımlı olmayacak.
- Checkout isteğinde gelen `plan`, `audience` ve `billing` değerleri sunucuda
  doğrulanacak; fiyat, para birimi ve dönem katalogdan tekrar çözülecek.
- Ödeme sonucu yalnızca imzası doğrulanmış webhook ile kesinleşecek.
- Webhook olay kimliği tekil tutulacak; tekrar gelen bildirim ikinci tahsilat
  veya ikinci abonelik oluşturmayacak.
- PayTR mağaza anahtarları, secret anahtarı ve callback imza bilgileri yalnızca
  ortam değişkenlerinde saklanacak; repoya, istemci bundle'ına veya loglara
  yazılmayacak.

## Şimdiden kesinleşen ürün kuralları

- İşletmeler için mevcut kredi kartsız **30 günlük deneme** korunur.
- Ücretsiz muhasebeci planı checkout gerektirmez.
- Ücretli planların aylık/yıllık fiyatı mevcut plan kataloğundan gelir.
- Canlı ödeme, PayTR sandbox ve üye işyeri erişimleri hazır olmadan açılmaz.

## Entegrasyon öncesi netleştirilecek kurallar

Bu kararlar tahmin edilerek kodlanmayacak; PayTR entegrasyonu başlamadan önce
ürün ve hukuk tarafında onaylanacak:

1. Ücretli muhasebeci planlarında deneme uygulanıp uygulanmayacağı.
2. Aylık/yıllık yükseltme, düşürme ve dönem ortası fark hesaplama kuralı.
3. İptalin anında mı yoksa dönem sonunda mı etkili olacağı.
4. Başarısız tahsilatta tolerans süresi ve yeniden deneme takvimi.
5. İade/cayma akışı ile satış belgesi ve e-Arşiv sorumlulukları.
6. KDV gösterimi, şirket unvanı ve resmi iletişim bilgileri.
7. Standart muhasebeci planındaki müşteri başı fiyatlandırmanın varsa yıllık
   tahsilat davranışı.

## Sonuçlar

P0.3 kapsamında PayTR için checkout, callback/webhook doğrulama, idempotent
ödeme olayı kaydı ve abonelik yaşam döngüsü eklenecek. Şirket kuruluşu, PayTR
başvurusu ve sandbox/canlı erişim bilgileri hazır olmadan gerçek tahsilat
entegrasyonunun tamamlandığı iddia edilmeyecek.
