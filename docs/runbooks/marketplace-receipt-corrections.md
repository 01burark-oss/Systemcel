# Kesinleşmiş mal kabulü düzeltme

Bu yerel paket, kesinleşmiş ve muhasebeleşmiş bir kabulün kısmen veya tamamen geri alınmasını kaydeder. İlk kabul, fatura, tahsilat ve sağlayıcı aktarım kayıtları korunur. Paket canlıya yayımlanmadı.

## İşlem

1. Yönetici siparişin mal kabul kaydını inceler. Tamamlanmış sipariş de incelemeye açılabilir.
2. Kalan kabulü aşmayan pozitif miktar ve gerekçeyle düzeltme ister. Miktar en fazla üç ondalıklıdır. Aynı siparişte aynı anda tek karar bekler.
3. Farklı bir aktif yönetici gerekçeli onay veya ret verir. İlk kişi kendi isteğine karar veremez. İki kullanıcı işlemi ve zamanları saklanır.
4. Onayda, tek seri işlem içinde alıcının kullanılabilir stokundan negatif hareket, iki işletmenin carisinde karşı hareket ve pazaryeri defterinde ters kayıtlar oluşur. Siparişin etkin kabul miktarı azalır; ilk kabul miktarı değişmez.
5. Düzeltme, asıl alıcı/satıcı faturalarını ve belge için gereken net/KDV/brüt tutarı saklar. Kesilmiş faturaların tutarı, satırları veya durumu değişmez. Resmî iade/düzeltme belgesi D3/K6 teyidini bekler.

## Ödeme ve mal konumu

Hakedişin yeni net tutarı azalır; `OdenenTutar` yalnız gerçekten tamamlanmış aktarımı gösterir. Ödenmiş tutarın yeni hakedişi aşan kısmı ayrıca `TedarikcidenGeriAlinacakTutar` olarak görünür. Geç gelen sağlayıcı başarı sonucu da gerçek aktarım olarak kaydedilir ve fark incelemede tutulur. Belirsiz talimat tamamlanmış sayılmaz ve tekrar gönderilmez.

Cari/vadeli kabulde sağlayıcı hakedişi hiç oluşmamışsa düzeltme yeni hakediş veya pazaryeri para defteri oluşturmaz. Stok ve cari yine ters hareketlerle düzelir. Hiç ödeme alınmamışsa `ParaDurumu=Uygulanmaz`; faturaya sonradan işletme tarafından ödeme işlendiğinde ödeme incelemesi açık bırakılır. Olmayan bir ödeme için iade kaydı üretilmez.

Bekleyen düzeltme, ilgili aktarımı ve aynı ödeme üzerindeki iadeyi gönderimden alıkoyar. Onaylı düzeltmenin para, belge ve stok konumu incelemeleri de bitmeden aktarım veya itiraz kapanışı yapılmaz. Ret, kendi bekleme nedenini kaldırır; diğer ödeme/itiraz engelleri sürer.

`ParaDurumu=IncelemeBekliyor`, `BelgeDurumu=BelgeBekliyor`, `StokDurumu=IadeKonumuBekliyor` ayrı işlerdir. Ters stok kaydı, malların tedarikçiye fiziksel olarak ulaştığı anlamına gelmez; tedarikçi kataloğuna stok eklenmez. Önceki ret stokunun kayıp/iade/yeniden sevk kararı bu düzeltmenin yerine geçmez.

Sağlayıcı geri alma/iade yöntemi ve resmî belge yöntemi kesinleşmeden bu kayıtları otomatik uzlaştıran veya para gönderen bir yol yoktur. K5'in yerel işlem temeli hazır olabilir; gerçek sağlayıcı/belge ve depo kabulü açık kalır.

## Tekrar ve tutar sınırları

İstek anahtarı işletme içinde tekildir. Aynı anahtar, kayıt, miktar, gerekçe ve kullanıcıyla tekrar aynı sonucu verir; farklı içerik çakışmadır. Kararın aynı kullanıcı/not/sonuçla tekrarı yeni hareket oluşturmaz. Seri işlem ve benzersiz anahtar eşzamanlı kararları korur.

Veritabanı eşzamanlı karar nedeniyle işlemi geri alırsa API 409 ve yeniden dene yanıtı verir; teknik sağlayıcı ayrıntısı gösterilmez. Aynı düzeltmenin işlem anahtarı ve karar notu korunarak yeniden denenir, yeni anahtar üretilmez.

Birden çok kısmi düzeltmede tutar, ilk kabulün toplamı üzerinden birikimli yuvarlanır. Son düzeltme ilk kabulü tamamen kapatırken kuruş kaybı oluşmaz. Eski kabulün muhasebe bağlantısı yoksa veya stok kullanılmış/rezerve edilmişse işlem reddedilir; eksik referans veya negatif stokla sessizce ilerlenmez.

Komisyon ve komisyon KDV'si ilk komisyon matrahından, tevkifat ilk tevkifat matrahından paylaştırılır. Bu matrahlar KDV hariçtir; farklı KDV oranlı ürünlerde brüt tutar oranıyla kesinti dağıtılmaz. Tedarikçiye yüklenen ödeme hizmeti bedeli brüt tutardan paylaştırılır; platformun gerçekten ödediği hizmet bedeli geri alınmış sayılmaz. Çok küçük miktarda kuruş yuvarlaması negatif hakediş azaltımı çıkarırsa işlem mali inceleme için durur.

PostgreSQL migration: `MarketplaceReceiptCorrections`. SQLite eski şema kurulumu düzeltme tablosunu ve hareket bağlantılarını iki kez çalıştırılabilir biçimde ekler. Üretim veritabanına bu koşuda migration uygulanmadı.
