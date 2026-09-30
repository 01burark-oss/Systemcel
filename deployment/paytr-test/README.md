# Kalıcı PayTR test ortamı

Bu paket mevcut Oracle makinesinde ayrı `systemcel-paytr-test` Compose projesini hazırlar. Üretim veritabanını, appdata alanını, Clerk hesabını veya ödeme sağlayıcısını kullanmaz. Kaynak kod, PR #6'nın PayTR test adaptörünü içermelidir. Sunucu kurulumu yapılmadan bu paket kabul tamamlandı anlamına gelmez.

Test uygulaması Development modundadır; oturumsuz API yalnız ayrı admin gateway üzerinden sunucunun 127.0.0.1:8190 adresine bağlanır. Bu porta sadece SSH tüneliyle erişilir. İnternete bağlanan gateway yalnız POST callback, GET ödeme formu/dönüşü ve sabit health yanıtını açar; diğer yollar 404 döner. Veritabanının host portu yoktur. Admin gateway üretimin edge ağına bağlı değildir. PayTR her durumda test modunda ve yalnız işletme 1 için seçilir.

## Sunucu ön koşulları

- Yönetici erişimi; mevcut `status/deploy` GitHub anahtarı yeterli değildir.
- Docker Compose 2.30 veya üstü; `env_file: format: raw` sırların içindeki dolar işaretlerinin değiştirilmesini önler.
- En az 1.5 GiB boş RAM ve 5 GiB boş disk; uygulama görüntüsü derlemesi için ayrıca kapasite kontrolü.
- `systemcel_edge` mevcut üretim Caddy ağı; adı sunucuda doğrulanmalıdır.
- `10.253.77.0/24` ağı mevcut host/VPC/Docker ağlarıyla çakışmamalıdır.
- 8089 ve 8190 loopback portları boş olmalıdır.
- Name.com'da `paytr-test.systemcel.app` A kaydı doğrulanmış Oracle public IP'sine gider. Mevcut ana alan kayıtları korunur.

## Kurulum

Kaynağı ayrı checkout'a yerleştir; üretim checkout'unda `compose up` çalıştırma. `deployment/paytr-test/init-config.sh` yalnız yetkili, etkileşimli SSH oturumunda çalıştırılır. Kullanıcı PayTR Key ve Salt'ı kendisi girer; sırları OCI Run Command scriptine veya çıktısına koyma. Dosyalar 0600 ile yazılır, Git tarafından yok sayılır. Çıktıda Docker'ın çözülmüş config/env değerlerini yazdırma.

```bash
cd /opt/systemcel-paytr-test/deployment/paytr-test
bash init-config.sh
docker compose config --quiet
docker compose build app
docker compose up -d
curl --fail http://127.0.0.1:8089/health
```

Üretim Caddyfile'ının bir yedeğini al. Kaynak Caddyfile'ın sonundaki `import /data/paytr-test/*.caddy` satırı test sitesini isteğe bağlı olarak yükler; boş dizinde ana siteyi etkilemez. `edge-site.Caddyfile` dosyasını üretim Caddy container'ının kalıcı `/data/paytr-test/site.caddy` konumuna yerleştir. Çalışan Caddy container'ında `caddy validate` ile doğrula, sonra `caddy reload` yap. Hata varsa yedeği geri koy; uygulama/veritabanı container'larını yeniden başlatma. Caddy HTTP/HTTPS portları ve sertifika yaşam döngüsü mevcut frontend'de kalır. Import satırı üretim dalında yayımlanmadan sonraki üretim dağıtımına karşı kalıcılık tamamlanmış sayılmaz.

Sıfırdan etkileşimli kuruluma alternatif olarak `configure-from-stdin.py`, yetkili SSH bağlantısının stdin kanalından `customerIp`, `key` ve `salt` alanlarını kabul eder. Sırları komut argümanlarına veya terminal çıktısına koyma. Yalnız IP ile ilk çağrı boş sır dosyası ve ayrı veritabanı parolası oluşturur; sonraki çağrı Key/Salt'ı kaydeder. Dolu sır dosyası üzerine yazılmaz. Yapılandırma sınırı `python3 tests/configuration-smoke.py` ile sahte değerler ve geçici dosyalar üzerinden doğrulanır.

Yerel test ekranı için yetkili SSH kimliğiyle:

```powershell
ssh -N -L 8590:127.0.0.1:8190 ubuntu@VERIFIED_ORACLE_HOST
```

Tarayıcıda `http://127.0.0.1:8590/app/abonelik` açılır. İzinli test işletmesi 1 seçilir. Public dönüş sayfasındaki abonelik bağlantısı public uygulama API'sini açmaz; SSH tünelindeki yerel ekrandan sonucu kontrol et.

## Kabul ve geri dönüş

PayTR panelindeki Bildirim URL'si ancak HTTPS sertifikası ve gateway kontrolü geçince `https://paytr-test.systemcel.app/api/odeme/paytr/bildirim` yapılır. Başarılı ödeme, kesin başarısızlık/zaman aşımı, kalıcı tek olay kaydı ve plan hakları birlikte doğrulanır. İnternetten `/app/abonelik`, `/api/abonelik/ozet` ve `/setup` 404; bozuk imza 400 dönmelidir.

Yeniden başlatmadan sonra test container'ları, test verileri ve sertifika erişimi tekrar kontrol edilir. İlk kurulumda fiziksel Oracle reboot yapılmaz; test stack'i durdur/aç ile lifecycle prova edilir. Üretim public smoke önce/sonra aynı sürümde geçmelidir.

Geri dönüşte yalnız bu Compose projesini `docker compose stop` ile durdur; volume silme. Test site kaydını kaldırıp Caddy'yi doğrula/reload et ve PayTR callback'i doğrulanmış önceki test adresine döndür. `docker compose down -v` kullanma. Sırlar kapatılırken süreç/dosya saklama kararını kullanıcıyla tamamla.

Kaynaklar: [Docker Compose env_file](https://docs.docker.com/reference/compose-file/services/#env_file), [Caddy reverse_proxy](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy), [Oracle Run Command](https://docs.oracle.com/en-us/iaas/Content/Compute/Tasks/runningcommands.htm).
