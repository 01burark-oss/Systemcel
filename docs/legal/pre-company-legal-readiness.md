# Systemcel şirket öncesi yasal hazırlık dosyası

> Durum: Teknik taslak — hukuk görüşü değildir. Canlı tahsilat ve genel yayın öncesinde şirket bilgileri doldurulmalı ve Türkiye'de yetkili hukuk danışmanı tarafından onaylanmalıdır.
>
> Teknik sürüm tarihi: 1 Ağustos 2026

## 1. Şirket kuruluşundan sonra doldurulacak tek kaynak alanları

| Alan | Yer tutucu |
|---|---|
| Ticaret unvanı | `[ŞİRKET UNVANI]` |
| Vergi dairesi / vergi numarası | `[VERGİ DAİRESİ / VERGİ NO]` |
| MERSİS / ticaret sicil | `[MERSİS / SİCİL]` |
| Açık adres | `[AÇIK ADRES]` |
| KEP | `[KEP]` |
| Resmî e-posta | `[RESMÎ E-POSTA]` |
| Destek kanalı | `destek@systemcel.app` (kuruluş sonrası doğrulanacak) |
| Ödeme kuruluşu | `[ÖDEME KURULUŞU VE ÜYE İŞYERİ BİLGİSİ]` |

Üretim alan adı ve bütün kullanıcı iletişimleri için teknik tercih `systemcel.app` olarak tekleştirilmiştir.

## 2. Yayınlanacak metin seti ve sürümleme

| Metin | Teknik kaynak | Şirket öncesi durum |
|---|---|---|
| Kullanım şartları | `Systemcel.Web/src/auth/legalTexts.ts` / `terms` | Taslak |
| Gizlilik politikası | `Systemcel.Web/src/auth/legalTexts.ts` / `privacy` | Taslak |
| KVKK aydınlatma | `Systemcel.Web/src/auth/legalTexts.ts` / `kvkk` | Veri sorumlusu alanları bekliyor |
| Abonelik, yenileme, iptal ve iade | `Systemcel.Web/src/auth/legalTexts.ts` / `subscription` | Taslak, checkout'tan bağlantılı |
| Çerez politikası | `/cerezler` ve bu dosyadaki envanter | Teknik envanter hazır |
| Ödeme açık onayı | `BillingApi` sürüm `abonelik-onayi-2026-08-v2` | Metin/hash/IP-UA kanıtı veritabanında |

Yayındaki bir metin anlamlı biçimde değiştiğinde sürümü değiştirilmeli; yeni kabul gereken değişiklikler eski kabulin üzerine yazılmamalıdır. Ödeme onayı için tam metnin SHA-256 özeti, kullanıcı referansı, işletme, fiyat/KDV, zaman, IP özeti ve user-agent özeti `AbonelikOnayi` kaydında saklanır.

## 3. Açık rıza ile aydınlatmayı ayırma kuralı

- KVKK aydınlatma metni bilgi verme yükümlülüğüdür; tek başına “açık rıza” kutusu gibi sunulmamalıdır.
- Sözleşmenin kurulması/ifası, hukuki yükümlülük ve meşru menfaat kapsamındaki zorunlu işlemler pazarlama rızasına bağlanmamalıdır.
- Pazarlama, reklam, hassas veri veya zorunlu olmayan yurt dışı aktarım gibi ayrı rıza gerektirebilecek bir özellik eklenirse amaç bazlı, boş varsayılan ve geri alınabilir ayrı bir tercih oluşturulmalıdır.
- Mevcut üründe reklam/pazarlama çerezi veya analitik SDK bulunmadığından zorunlu olmayan çerez onay bandı açılmamıştır.

## 4. Çerez ve tarayıcı depolama envanteri

| Sağlayıcı / anahtar | Tür | Amaç | Zorunluluk | Süre / kontrol |
|---|---|---|---|---|
| Clerk oturum tanımlayıcıları | Güvenli oturum çerezi / tarayıcı verisi | Kimlik doğrulama, oturum ve saldırı önleme | Zorunlu | Clerk üretim ayarı ve hukuk incelemesiyle kesinleştirilecek |
| `systemcel.language` | localStorage | Dil tercihi | Tercih | Kullanıcı temizleyene kadar |
| `systemcel.accountTypeIntent` | localStorage | Kayıt/kurulum hedef rolünü koruma | İşlevsel | Kurulum tamamlanınca silinir |
| Uygulamanın geçici UI tercihleri | localStorage / memory | Ekran tercihleri ve güvenli yönlendirme | İşlevsel | İlgili akış bitince veya kullanıcı temizleyince |

Yeni analitik, reklam, A/B testi veya üçüncü taraf widget eklenmeden önce bu tablo güncellenmeli; zorunlu olmayan depolama kullanıcı tercihinden önce başlatılmamalıdır.

## 5. Hukuk danışmanına verilecek karar listesi

1. Ürünün B2B/B2C kullanıcı ayrımı ve tüketici mevzuatının hangi senaryolarda uygulanacağı.
2. Dijital hizmette cayma hakkı, hizmetin hemen ifası ve iade yaklaşımının nihai dili.
3. Deneme sonunda otomatik tahsilat ve 7/3 günlük hatırlatmanın yeterliliği.
4. KVKK hukuki sebepleri, veri saklama süreleri, yurt dışı aktarım mekanizması ve veri işleyen sözleşmeleri.
5. Clerk, DigitalOcean, e-posta, ödeme ve isteğe bağlı GİB/Telegram/AI sağlayıcıları için aktarım ve alt işleyen listesi.
6. Muhasebeci–işletme çalışma alanında veri sorumlusu/veri işleyen rollerinin sınırı.
7. Destek, güvenlik olayı, veri sahibi başvurusu, hesap kapatma ve kayıt silme süreleri.

## 6. Canlı yayın kapısı

- Tüm köşeli parantezli alanlar doldurulmadan metinler “nihai” işaretlenmez.
- Hukuk onayı, onaylanan dosyanın sürümü ve tarihiyle kayda geçirilir.
- Canlı ödeme sağlayıcısının adı, tahsilat/iade yolu ve iletişim bilgileri checkout metniyle karşılaştırılır.
- Test hesabında kayıt → yasal metin → deneme/ödeme onayı → iptal → kabul kanıtı uçtan uca doğrulanır.
- Yayımdan sonra kullanıcıya gösterilen metin ile veritabanındaki metin özeti aynı olmalıdır.
