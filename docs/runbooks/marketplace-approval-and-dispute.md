# Mal kabulün ikinci onayı ve itiraz takibi

3 Ekim 2026 yerel uygulama kaydı. Bu paket henüz yayımlanmadı; PayTR yetkisi veya canlı ödeme ayarı değiştirmez.

## İkinci onay

Tedarikçi siparişinin KDV dahil toplamı 10.000 TL'yi aşıyorsa ikinci kişi gerekir. Eşik etiket veya kabul edilen parçanın tutarına uygulanmaz; siparişi bölmek onayı kaldırmaz. Kur karşılığı kararlaştırılmadığı için TRY dışındaki siparişler de ikinci onay bekler. Reddedilen miktar, belge uyuşmazlığı veya miktar düzeltmesi varsa tutardan bağımsız ikinci onay gerekir.

İlk kayıt `OnayBekliyor` olarak saklanır ve aynı etiket tekrar kullanılamaz. Kabul/ret sayaçları, stok, fatura, cari ve hakediş kesin etkileri ikinci onaya kadar oluşturulmaz. İlk onayı veren hesabın ikinci onayı yasaktır. İkinci onay, işlem notu, kullanıcı ve UTC zamanı ile kalıcı kaydedilir. Aynı onay tekrar gönderilirse ilk kayıt korunur; iki eşzamanlı istek iki hareket oluşturamaz.

İlk kabul mevcut aktif depo/mal kabul üyeliğiyle yapılır. İkinci kişi aktif kullanıcı olmalı ve alıcının işletme sahibi veya `mal_kabul_onaylayicisi` olmalıdır; atanmış şube/depo sınırı uygulanır. Platform yöneticisi farklı bir kişi olarak onaylayabilir. Sıradan `yonetici` veya `depo_sorumlusu` rolü tek başına ikinci onay yetkisi vermez. Gerçek depo yetkilisi ile Burak veya belirlediği ikinci kişinin hesap/rol eşlemesi pilot kurulum girdisidir.

Bekleyen onay varken yönetim ekranında ret stoku uzlaştırması ve itiraz kararı engellenir. Sunucu da aynı engelleri uygular. Teslim edildi durumunu elle seçmek, yüksek tutarlı veya bekleyen onaylı kabulü atlayamaz. Sonradan kesinleşmiş kabulün ters muhasebe kayıtlarıyla değiştirilmesi ayrı K5 işidir; formdaki miktar düzeltmesi seçeneği K5'i tamamlamaz.

## İtiraz hedefleri

İlk yanıt bir iş günü, kanıtların toplanması iki iş günü içinde hedeflenir. İstanbul saatinde başvuru saatinin karşılığı korunur; cumartesi, pazar ve yapılandırılan tatil tarihleri atlanır. Bunlar iç operasyon hedefleridir; müşterinin yasal başvuru haklarını sınırlamaz.

Yönetim ekranındaki ilk yanıt ve kanıt aşamaları ayrı notlarla işaretlenir. Tedarikçinin şikâyete ilk yanıtı da yanıt zamanını kaydeder; yanıt düzenlenmesi ilk tarihi değiştirmez. Açık itiraza ek açıklama eklemek süreyi yeniden başlatmaz. İtiraz kapatılıp yeni bir itiraz açılırsa yeni tur başlar.

Mevcut arka plan görevi beş dakikada bir hedef aşımını denetler. Gecikme yönetim kuyruğuna ve durum geçmişine bir kez yazılır. Geç tamamlanan aşama gecikmeyi silmez. İtiraz çözülmeden taraflardan biri otomatik haklı sayılmaz. Açık itiraz, çözülememiş şikâyet veya ikinci onay bekleyen kayıt varsa ilgili siparişin aktarım talimatı gönderilemez; kabul kaynaklı talimatlar için de aynı kural geçerlidir.

## Pilot yapılandırması

`Pazaryeri:ItirazYukseltmeKullaniciRef`, Burak'ın doğrulanmış oturum sağlayıcısı kullanıcı referansıdır. Boş varsayılanla yönetim kuyruğu/geçmiş kaydı çalışır; kişisel bildirim üretilmez. Değer tanımlandığında alıcı işletme kapsamında uygulama içi bildirim saklanır ve `/app/tedarikci-pazaryeri` yoluna gider. Burak'ın ilgili işletmedeki bildirim görünürlüğü ve platform yönetim yetkisi gerçek hesapla doğrulanmalıdır. Bu özellik e-posta veya telefon bildirimi göndermez.

`Pazaryeri:IsGunuTatilTarihleri`, `yyyy-MM-dd` biçiminde tarih listesidir. Tatiller kendiliğinden harici bir takvimden alınmaz. Pilotun kapsadığı resmî/işletme tatilleri kuruluma eklenmeli; boş liste yalnız hafta sonlarını atlar. Yapılandırmayı değiştirince API yeniden başlatılır.

## Şema ve yayın

`20261002232846_MarketplaceReceiptApprovalAndDisputeTargets` migration'ı yeni alanları ve onay indeksini ekler. Önceden muhasebeleşmiş kabul kayıtları `Onaylandi` kalır; eski hareketler tekrar işlenmez. Eski açık itirazın başlangıcı gerçek durum geçişinden, ilk yanıtı mevcut itiraz turuna ait şikâyet yanıtından alınır. Geçiş bulunmazsa siparişin güncelleme tarihi kullanılır. SQLite şema uyarlaması da aynı yaklaşımı uygular.

Yayından önce [release.md](release.md) ve [agent-release-notes.md](agent-release-notes.md) kapsamındaki tam CI eşdeğeri kontroller gerekir. Yerel testler gerçek PayTR aktarımı veya pilot kabulü kanıtı değildir. Yayın sonrasında iki gerçek yetkiliyle ilk kayıt/ikinci onay, stok-cari-fatura mutabakatı, itiraz hedefleri ve Burak bildirimi doğrulanmalıdır.

## Yerel doğrulama

Tam .NET Release koşusu 548 başarılı, 4 PostgreSQL bağlantısı gerektiren koşullu atlama ile tamamlandı. Bu dört testten biri yeni ikinci onay yarışıdır; yayın öncesi PostgreSQL CI işinde çalışmalıdır. SQLite üzerinde iki eşzamanlı onay, farklı işletme/rol/şube/pasif kullanıcı, 10.000 TL sınırı, etiket bölme, belge/miktar/ret istisnaları, tekrar işlem, açık şikâyet ve bekleyen onayın mevcut aktarımı tutması doğrulandı. Eski itirazın gerçek tur tarihleriyle taşınması iki şema yolunda kontrol edildi.

Son incelemede, itiraz sonuçlandırılırken önceki şikâyet kapanış notunun üzerine yazılması önlendi; yönetim kararı ayrı durum geçmişinde tutulur. Bu düzeltmeden sonraki hedefli kabul/itiraz/aktarım/şema/API koşusu 73 başarılı, 4 PostgreSQL koşullu atlama ile geçti. Son TRX `CashTracker.Tests/TestResults/marketplace-approval-final-targeted.trx` yolundadır.

Web 177 test, typecheck, lint ve build geçti. Etkilenen pazaryeri tarayıcı dosyası dört projede 23 başarılı ve 21 beklenen proje atlamasıyla tamamlandı. Son görsel akışlar 8/8 geçti. Açık/koyu tema, masaüstü/320 px, odak, hover, hata/yüklenme, seçili istisnalar ve itiraz modalinin kaydırma/taşma kontrolleri yapıldı. 28 görüntü ve .NET günlüğü `artifacts/marketplace-approval-20261003/` içinde; TRX `CashTracker.Tests/TestResults/marketplace-approval-full.trx` yolunda. EF model kontrolü bekleyen değişiklik bulmadı; yeni migration SQL'i aynı kanıt klasöründe üretildi. Üretim verisine uygulanmadı.

Son duyuru renk düzeltmesinde dört tarayıcı akışı tekrar geçti. Tarayıcıdaki gerçek RGBA renkleriyle ölçülen metin kontrastı açık temada 18,24:1, koyu temada 5,35:1; iki ölçüm de testteki 4,5:1 alt sınırını geçti. Son API Release derlemesi sıfır uyarı/hata ile tamamlandı. Yeni onay ve itiraz zamanları API'de UTC işaretli döner.
