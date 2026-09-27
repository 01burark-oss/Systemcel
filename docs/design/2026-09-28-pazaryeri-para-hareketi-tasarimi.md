# Pazaryeri para hareketi ve mal kabul düzeltmesi — uygulama kararı

28 Eylül 2026 kod incelemesi. İlk ücretli yayının K1–K5 işleri için uygulanacak teknik sınırlar. Bu dosya ödeme sağlayıcısı yetkisini veya hukuk/muhasebe kararını vermez.

## Somut mevcut durum

- `TedarikciPazaryeriService.ReceiveShipmentQrAsync` seri hale getirilmiş veritabanı işlemi başlatıyor ve `ApplyAcceptedReceiptAsync` çağrısını bu işlem içinde yapıyor. Bu yordam kabul edilen miktarı fatura, cari, stok ve uygun durumda hakedişe işliyor.
- `ApplyAcceptedReceiptAsync` içinde `_paymentGateway.ReleaseAsync` çağrısı var. `FinalizeAcceptedDeliveryAsync` ve `CompleteSettlementAsync` de sağlayıcı aktarımını veritabanı işlemi kapanmadan çağırabiliyor. İade yollarında aynı desen `RefundAsync` için bulunuyor.
- Fake gateway aynı idempotency anahtarı için aynı referansı hesaplıyor; bu gerçek sağlayıcıda aktarımın tamamlandığını veya veritabanı işlemi geri alınırsa geri döneceğini kanıtlamıyor.
- `PaymentReconciliationService` mevcut haliyle abonelik durumlarını `IPaymentReconciliationProvider` üzerinden karşılaştırıyor. Pazaryeri tahsilat, iade ve alt satıcı transferi için günlük mutabakatı yapmıyor.
- Üretimde son doğrulanan ödeme modu `Unconfigured`; bu tasarım gerçek para açılmadan uygulanabilir.

## Değişmez kurallar

1. Dış sağlayıcı sonucu ile yerel defter kaydı ayrı aşamalardır. API'nin talimatı kabul etmesi, para hareketinin tamamlandığı anlamına gelmez.
2. Tek sipariş/işlem için tekrar deneme aynı değişmez idempotency anahtarını taşır. Sonuç bilinmiyorsa önce durum sorgulanır; yeni para talimatı üretilmez.
3. Kabulün asıl kararı silinmez. Her düzeltme ayrı, gerekçeli ve zaman/aktör/kaynak kararla bağlantılı kayıttır. Aynı düzeltme isteği tekrar gelirse ikinci stok/cari/fatura hareketi oluşmaz.
4. Stok ve yerel muhasebe düzeltmesi sağlayıcıdan geri ödeme geldiği varsayımıyla yazılmaz. Bekleyen iade veya aktarım farkı ayrı durum olarak tutulur.
5. Resmî e-belge kesildiyse eski belge tutarı sessizce değiştirilmez; D3/K6 kapsamında uygun iade/düzeltme belgesinin sağlayıcı durumu beklenir.
6. K3 mutabakat farkı etkilenen yeni aktarımı durdurur. Yönetici gerekçeli karar verene kadar sistem otomatik olarak farkı sıfırlamaz.

## Uygulama sırası

### 1. Kalıcı para talimatı

`PazaryeriParaTalimatı` benzeri kalıcı kayıtla tahsilat/iade/aktarım türü, kaynak sipariş ve mal kabul/düzeltme, sağlayıcı, tutar/para birimi, değişmez idempotency anahtarı, durum, yanıt referansı, deneme sayısı ve zamanlar saklanacak. `(sağlayıcı, tür, idempotency anahtarı)` benzersiz olacak. Yerel iş kararı ve bekleyen talimat aynı veritabanı işleminde kaydedilecek; sağlayıcı çağrısı commit sonrasında başlayacak. Eşzamanlı worker claim'i ve süresi dolan claim'in sorgulanarak geri alınması gerekiyor.

Durumlar: `Hazir` → `Gonderiliyor` → `SonucBekliyor` → `Tamamlandi` veya `IncelemeGerekli`. Kesin başarısızlık kaydı ayrı olacak. Zaman aşımı `Basarisiz` sayılmayacak; önce sağlayıcı sorgusu yapılacak. Bildirim ile sorgu aynı talimatı tamamlayabilir, ama yalnız bir kez defter etkisi üretir.

### 2. K1–K3 sağlayıcı bağlantısı

PayTR test mağazasındaki yetkilere göre ayrı tahsilat/iade/transfer yolları bağlanacak. İmzalı callback yalnız doğrulanmış olay sözleşmesi için kullanılacak; desteklenmeyen olaylarda sorgu/rapor yoluna gidilecek. Günlük pazaryeri mutabakatı sağlayıcıdaki tahsilat, iade, transfer talimatı, tamamlanan/geri dönen transferi Systemcel kayıtlarıyla karşılaştıracak. Fark yönetici kuyruğuna ve aktarım durdurma bayrağına gidecek. Mevcut abonelik mutabakatı korunacak.

### 3. K5 düzeltme

Yeni düzeltme kaydı ilk mal kabul referansını, önceki/sonraki kabul miktarını, gerekçeyi, aktörü, işlem anahtarını, hesaplanan stok/cari/fatura farkını ve para/belge uzlaştırma durumunu tutacak. Düzeltme miktarı kalan kabulü aşamaz; aynı anahtar/fark tekrar sonucu döndürür, farklı içerik reddedilir. Kaynak `TedarikciMalKabul` hareketleri silinmez; karşı işaretli hareketler düzeltme ID'siyle eklenir.

- **Henüz aktarılmadı:** bekleyen hakediş düşürülür; iade gerekiyorsa ayrı talimat oluşturulur; stok/cari/fatura ters kayıtlarıyla toplamlar karşılaştırılır.
- **Talimat gönderildi, sonuç bilinmiyor:** önce sağlayıcı sorgusu; düzeltme incelemede kalır. Yeni transfer gönderilmez.
- **Aktarım tamamlandı:** gerçek geri alma/mahsup sözleşmesine göre açık alacak/borç ve iade işi kaydedilir. Sağlayıcı parayı geri göndermeden `IadeEdildi` veya `Uzlasti` yazılmaz.
- **Belge resmileşti:** ilgili iade/düzeltme belgesinin numarası ve durumu bağlanır; belirsiz veya reddedilen belge incelemede kalır.

İlk kod dilimi, sağlayıcıya dokunmadan cari ödeme modunda ters kayıtları ve tekrar/eşzamanlılık korumasını doğrulayabilir. Ancak K5 kapanışı için yukarıdaki ücretli üç durum da gerekir; dar dilim P0'ı tamamlandı yapmaz.

## Gerekli kabul testleri

- Kısmi kabulü kısmen/tamamen geri alma; art arda iki düzeltme; aynı anahtarın tekrar gönderimi; aynı anahtarla farklı tutar; iki yetkilinin aynı anda düzeltmesi.
- Kabul düzeltmesi ile hakediş worker'ının eşzamanlı çalışması; düzeltme sırasında ağ zaman aşımı; sağlayıcıda başarılı ama yerelde commit başarısız sonucu.
- Başarılı/başarısız/bekleyen iade; aktarım hiç yapılmadı/bilinmiyor/tamamlandı; günlük raporda eksik ve mükerrer hareket.
- Stok, cari, fatura, tahsilat ve hakediş toplamlarının önceki + ters kayıtlarla eşleşmesi; başka işletmenin kaydına erişimin reddi.
- SQLite davranış testi yanında PostgreSQL eşzamanlılık ve migration kontrolü. Fake sağlayıcı sonuçları gerçek PayTR test yanıtı yerine geçmez.

## Karar bekleyen noktalar

- D1: mağazada pazaryeri transferi, geç onay, geri dönen aktarım ve transfer sonrası geri alma/mahsup yolları; API olay ve rapor sözleşmesi.
- D3/K6: resmî satış/iade belgesinin yöntemi ve zamanlaması.
- D4: düzeltme yetkilisi ve çift onay/risk eşiği.

Bu bilgiler gelmeden gerçek para için keskin durum veya alan uydurulmayacak. Mevcut `Unconfigured` canlı modu açık kalacak.
