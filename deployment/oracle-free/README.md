# Oracle Always Free canlı dağıtımı

Bu klasör Systemcel'in güncel canlı Oracle Cloud Always Free ARM64 dağıtımını içerir. DigitalOcean kaynakları silinmiştir; geri dönüş hedefi değildir. DNS ve alarm teslim sağlayıcısı altyapı sahibinin ayrı yapılandırmasıdır.

## Hazırlanan sunucu

- Ubuntu 24.04 ARM64
- 2 OCPU, 6 GB RAM, 46,6 GB disk ve 2 GB swap
- Reserved public IP: `89.168.102.124`
- OCI NSG ve UFW üzerinde yalnızca TCP `22`, `80`, `443`
- Docker, Compose, Fail2ban ve Git
- Çalışma dizini: `/opt/systemcel`

Public IP rezerve edilmiştir: `89.168.102.124`.

## Mimari

- `caddy`: TLS sonlandırma ve ters proxy
- `app`: Systemcel API ve derlenmiş web arayüzü
- `db`: internete kapalı PostgreSQL 18
- `app_data`: yüklemeler, sohbet ekleri ve çalışma zamanı dosyaları
- `postgres_data`: PostgreSQL verisi

Uygulama portu yalnızca sunucunun `127.0.0.1:8080` adresine bağlanır. PostgreSQL için host portu yayımlanmaz.
Container içindeki `HOME` ve `SYSTEMCEL_APPDATA` aynı kalıcı volume'e yönlendirilir; aylık rapor çıktıları da böylece geçici container katmanında kalmaz.

## İlk kurulum

```bash
cd /opt/systemcel/repo/deployment/oracle-free
cp .env.example .env
chmod 600 .env
```

`.env` içine production Clerk değerleri, sabit şifreleme anahtarı ve güçlü PostgreSQL parolası girilmelidir. Şirket ve gerçek sağlayıcı kapısı açılana kadar `SYSTEMCEL_PAYMENT_PROVIDER=Fake` kullanılmalıdır.

Canlı değer `CADDY_SITE_ADDRESS=systemcel.app` olmalıdır; Caddy alan adı sunucuya çözüldüğünde sertifikayı otomatik alır. Doğrudan IP yalnız arıza ayırma amacıyla ve TLS alan adı doğrulaması korunarak kullanılmalıdır.

## Dağıtım ve doğrulama

```bash
chmod +x scripts/*.sh
./scripts/deploy.sh
curl --fail http://127.0.0.1:8080/api/health/ready
curl --fail https://systemcel.app/api/health/ready
./scripts/smoke.sh http://127.0.0.1:8080
```

2 Eylül 2026 veri taşıması ve geri yükleme kontrolü tamamlanmıştır. Güncel kanıt ve açık kapılar için `MIGRATION-STATUS.md` kullanılır; eski sağlayıcıdan yeniden veri alınmaz.

## Şifreli sunucu dışı yedekleme

Günlük systemd zamanlayıcısı 03:00 UTC (06:00 Türkiye), en fazla beş dakika rastgele gecikmeyle çalışır. Servis uygulama yazmalarını kısa süre durdurur, doğrulanmış PostgreSQL + appdata + SHA-256 üçlüsünü üretir ve `rclone crypt` hedefe yollar. Hedef depolama sağlayıcısından bağımsızdır; şifreleme VM'den çıkmadan uygulanır.

Altyapı sahibi önce bir rclone depolama remote'u, onun üzerinde `crypt` remote'u oluşturmalıdır. Crypt parolası, salt parolası, sabit `SYSTEMCEL_SECRET_ENCRYPTION_KEY` ve gerekli kurulum sırları VM'den ve eski Windows profilinden bağımsız, erişim kontrollü kurtarma kaydında tutulmalıdır. Aktarım kimliğine mümkünse yalnız yazma/listeleme yetkisi verin; retention silme yetkisini ayrı tutun.

```bash
sudo install -d -m 0750 /etc/systemcel
sudo rclone config --config /etc/systemcel/rclone.conf
sudo install -m 0600 backup-offsite.env.example /etc/systemcel/backup-offsite.env
sudoedit /etc/systemcel/backup-offsite.env
sudo install -m 644 systemcel-backup.service systemcel-backup.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now systemcel-backup.timer
```

Gerçek hedefe yazmadan kuru kontrol ve ardından kontrollü ilk çalıştırma:

```bash
sudo -u root env RCLONE_CONFIG=/etc/systemcel/rclone.conf rclone config show systemcel-crypt | grep 'type = crypt'
sudo systemctl start systemcel-backup.service
sudo systemctl status systemcel-backup.service --no-pager
sudo journalctl -u systemcel-backup.service --no-pager -n 50
sudo jq . /var/lib/systemcel-backup/offsite-last-success.json
```

Betik tek-çalışan kilidi kullanır, işlemleri tekrar dener, uzak dosyaları yeniden indirerek checksum kontrolü yapar ve `COMPLETED.json` işaretini en son yazar. Yalnız bundan sonra yerel son-başarı durumu atomik güncellenir. Tamamlanma işareti olmayan paketler kurtarılabilir başarı sayılmaz. Loglara rclone yapılandırması ya da sırlar yazılmaz.

Canlı hedef Oracle Frankfurt'taki private `systemcel-backups-2026` Object Storage bucket'ıdır. VM yalnız bu bucket'taki nesneleri oluşturabilir, listeleyebilir ve okuyabilir; silme yetkisi yoktur. `systemcel-oracle` rclone remote'u instance principal kullanır ve `no_check_bucket=true` ayarlıdır. `systemcel-crypt` şifreleme remote'u `/etc/systemcel/rclone.conf` içinde 0600 izinle tutulur. Şifreleme parolası ve salt, VM'nin okuyamadığı Oracle Vault `systemcel-recovery` içindeki `systemcel-backup-rclone-recovery-2026` secret'ında kurtarma amacıyla ayrıca saklanır. Yeni VM ile felaket kurtarmada bucket IAM iznini yeni instance OCID'sine açıkça taşıyın; mevcut dinamik grup yalnız `systemcel-free` VM'siyle eşleşir. Sırları komut satırına veya loga yazmayın.

Uzak paketten canlı veritabanına dokunmadan izole geri yükleme denemesi:

```bash
sudo env RCLONE_CONFIG=/etc/systemcel/rclone.conf \
  SYSTEMCEL_OFFSITE_REMOTE=systemcel-crypt:systemcel-production \
  bash ./scripts/verify-offsite-restore.sh
```

Bu komut son uzak paketi doğrudan nesne depolamadan indirir, tamamlanma işaretini ve SHA-256 manifestini doğrular, uygulama arşivini okur ve PostgreSQL dump'ını ağsız geçici bir `postgres:18-alpine` konteynerine geri yükler. Çıktıda tablo sayısı, indirme ve geri yükleme süresi ile paket yaşı yer alır. Paket yaşı tek başına gerçek işlem bazlı RPO ölçümü değildir. Belirli bir paketi sınamak için paket kimliğini son argüman olarak verin. Geçici konteyner ve indirilen dosyalar işlem sonunda silinir.

Sunucu dışı aktarım başarısızsa yeni yerel paketler 14 günü geçse de silinmez. Bu veri kaybına karşı güvenli varsayılan diski doldurabilir; disk alarmı bu nedenle zorunludur. Uzak retention (pilot başlangıcı: 14 gün) depolama tarafı lifecycle kuralıyla, tamamlanmış paketlere uygulanmalıdır.

Yerel-only yedek veya mevcut bir yerel paketi tekrar aktarmak için:

```bash
./scripts/backup.sh --quiesce
sudo --preserve-env=RCLONE_CONFIG,SYSTEMCEL_OFFSITE_REMOTE ./scripts/backup-offsite.sh --transfer-only
```

Kurulum ve zamanlayıcı kontrolü:

```bash
sudo systemctl list-timers systemcel-backup.timer
```

`backup.sh` tamamlanmamış yerel çıktıları yayınlamaz; dump listesini, arşivi ve checksum'ları oluşturma sırasında doğrular. Aynı diskteki yedek tek başına felaket kurtarma sayılmaz.

Planlı bakımda tutarlı bir kopya almak için uygulama yazmalarını kısa süreli durduran seçenek kullanılabilir:

```bash
./scripts/backup.sh --quiesce
```

Bu seçenek çalışıyorsa `app` ve `caddy` servislerini durdurur, yedek tamamlandığında yeniden başlatır. Otomatik servis, dosya ve veritabanı tutarlılığı için güvenli varsayılan olarak bu yolu kullanır. Kesintisiz yöntem eşzamanlı yükleme/silme altında ayrıca kanıtlanmadan tam kurtarma olarak sunulmamalıdır.

## Yerel monitoring collector

Collector CPU, RAM, deployment diski, yerel readiness, PostgreSQL bağlantıları, container durumu/restart sayacı ve son doğrulanmış uzak yedek yaşını Prometheus text formatında atomik üretir:

```bash
sudo install -m 644 systemcel-monitoring.service systemcel-monitoring.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now systemcel-monitoring.timer
./scripts/collect-monitoring.sh --stdout
sudo cat /var/lib/systemcel-monitoring/systemcel.prom
```

Disk ve uzak yedek yaşı için sunucu içi e-posta uyarısını açmadan önce `.env` içinde `SYSTEMCEL_ALERT_EMAIL` alıcısını ayarlayın. Servis aynı SMTP ayarlarını kullanır; kritik durumda 30 dakikada bir tekrar, normale dönüşte tek bildirim gönderir. Collector üç dakikadan uzun süre güncellenmezse ayrıca uyarır:

```bash
sudo install -Dm755 scripts/alert-monitoring.py /usr/local/lib/systemcel/alert-monitoring.py
sudo install -m 644 systemcel-monitoring-alert.service systemcel-monitoring-alert.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now systemcel-monitoring-alert.timer
sudo systemctl start systemcel-monitoring-alert.service
```

Textfile yolu bir node exporter/ajan tarafından kalıcı metric hedefine alınmalıdır. VM tamamen kapandığında bu collector çalışamayacağı için HTTPS/readiness probe'u ayrı bir VM dışı serviste kurulmalıdır. Eşikler, kuru test ve teslim kanıtı [monitoring runbook](../../docs/runbooks/monitoring.md) içindedir.

Geri yükleme betiği bilerek etkileşimli ve yıkıcı işlem uyarılıdır:

```bash
./scripts/restore.sh \
  /opt/systemcel/backups/systemcel-db-TARIH.dump \
  /opt/systemcel/backups/systemcel-appdata-TARIH.tar.gz \
  /opt/systemcel/backups/systemcel-TARIH.sha256
```

Üç dosya aynı klasörde ve aynı zaman damgasıyla bulunmalıdır. Betik yıkıcı işleme başlamadan önce manifesti, dump yapısını ve arşiv yollarını doğrular; ardından `app` ile `caddy` servislerini kapatır. PostgreSQL geri yüklemesi hata verirse web servisleri kapalı kalır ve bozuk/eksik veri trafik almaz. Başarılı işlem sonunda veritabanı sorgusu ve yerel smoke testi otomatik çalışır.

## Canlı işletim kapıları

Tamamlanan taşıma kanıtı `MIGRATION-STATUS.md` dosyasındadır. Açık işletim kapıları şunlardır:

1. Yeni Clerk kimliğiyle kayıt, gerçek oturum, provision, kolay kurulum ve tenant izolasyonu.
2. Gerçek SMTP teslimi ve kontrollü bildirim kanıtı.
3. Şifreli, taşınabilir ve otomatik sunucu dışı yedek aktarımı.
4. CPU, RAM, disk, container restart ve yedek yaşı alarmları.
5. Sınırlı gerçek kullanıcı ve fiziksel iPhone/Safari pilotu.
