# Systemcel yasal metin yayım kaydı

> Hukuk onayı kullanıcı tarafından 31 Ağustos 2026'da bildirildi. Vergi levhası ve noter belgeleri 17 Eylül 2026'da ürün kimlik alanları için kaynak olarak alındı. Yalnız kamuya açık hizmet sağlayıcı bilgileri ürüne işlendi; T.C. kimlik numarası, imza örnekleri ve vekil kişilerin kimlik bilgileri yayımlanmadı.
>
> Teknik sürüm tarihi: 17 Eylül 2026

## 1. Hizmet sağlayıcı bilgileri

| Alan | Bilgi / durum |
|---|---|
| Hizmet sağlayıcı | Burak Özmen |
| İşletme türü | Şahıs işletmesi |
| Vergi dairesi / vergi numarası | Küçükyalı Vergi Dairesi / 7020714272 |
| İşe başlama tarihi | 14 Eylül 2026 |
| Ana faaliyet | 621000 — Bilgisayar programlama faaliyetleri |
| MERSİS / ticaret sicil | Sunulan belgelerde yer almıyor; yayımdan önce gerekliyse ayrıca doğrulanacak |
| Açık adres | Bağlarbaşı Mahallesi, Hür Sokak No: 2, İç Kapı No: 9, Maltepe/İstanbul |
| KEP | Sunulan belgelerde yer almıyor; varsa ayrıca eklenecek |
| Resmî e-posta | `destek@systemcel.app` |
| Destek kanalı | `destek@systemcel.app` |
| Ödeme kuruluşu | PayTR başvurusu henüz açılmadı; üye işyeri bilgisi başvuru sonrasında eklenecek |

Üretim alan adı ve bütün kullanıcı iletişimleri için teknik tercih `systemcel.app` olarak tekleştirilmiştir.

## 2. Yayınlanacak metin seti ve sürümleme

| Metin | Teknik kaynak | Durum |
|---|---|---|
| Kullanım şartları | `Systemcel.Web/src/auth/legalTexts.ts` / `terms` | Hizmet sağlayıcı, vergi, adres ve iletişim bilgileri eklendi |
| Gizlilik politikası | `Systemcel.Web/src/auth/legalTexts.ts` / `privacy` | Veri sorumlusu, vergi, adres ve iletişim bilgileri eklendi |
| KVKK aydınlatma | `Systemcel.Web/src/auth/legalTexts.ts` / `kvkk` | Veri sorumlusu kimliği ve başvuru iletişimi eklendi |
| Abonelik, yenileme, iptal ve iade | `Systemcel.Web/src/auth/legalTexts.ts` / `subscription` | Hizmet sağlayıcı, vergi, adres ve destek bilgileri eklendi |
| Çerez politikası | `/cerezler` ve bu dosyadaki envanter | Teknik envanter hazır |
| Ödeme açık onayı | `BillingApi` sürüm `abonelik-onayi-2026-08-v2` | Metin/hash/IP-UA kanıtı veritabanında |

Yayındaki bir metin anlamlı biçimde değiştiğinde sürümü değiştirilmeli; yeni kabul gereken değişiklikler eski kabulin üzerine yazılmamalıdır. Ödeme onayı için tam metnin SHA-256 özeti, kullanıcı referansı, işletme, fiyat/KDV, zaman, IP özeti ve user-agent özeti `AbonelikOnayi` kaydında saklanır.

## 3. Açık rıza ile aydınlatmayı ayırma kuralı

- KVKK aydınlatma metni bilgi verme yükümlülüğüdür; tek başına “açık rıza” kutusu gibi sunulmamalıdır.
- Sözleşmenin kurulması/ifası, hukuki yükümlülük ve meşru menfaat kapsamındaki zorunlu işlemler pazarlama rızasına bağlanmamalıdır.
- Pazarlama, reklam, hassas veri veya zorunlu olmayan yurt dışı aktarım gibi ayrı rıza gerektirebilecek bir özellik eklenirse amaç bazlı, boş varsayılan ve geri alınabilir ayrı bir tercih oluşturulmalıdır.
- Google Analytics yalnız kullanıcının ayrı ve boş varsayılan tercihiyle yüklenir. Ret halinde analitik script'i yüklenmez.

## 4. Çerez ve tarayıcı depolama envanteri

| Sağlayıcı / anahtar | Tür | Amaç | Zorunluluk | Süre / kontrol |
|---|---|---|---|---|
| Clerk oturum tanımlayıcıları | Güvenli oturum çerezi / tarayıcı verisi | Kimlik doğrulama, oturum ve saldırı önleme | Zorunlu | Clerk üretim ayarı ve hukuk incelemesiyle kesinleştirilecek |
| `systemcel.language` | localStorage | Dil tercihi | Tercih | Kullanıcı temizleyene kadar |
| `systemcel.accountTypeIntent` | localStorage | Kayıt/kurulum hedef rolünü koruma | İşlevsel | Kurulum tamamlanınca silinir |
| Uygulamanın geçici UI tercihleri | localStorage / memory | Ekran tercihleri ve güvenli yönlendirme | İşlevsel | İlgili akış bitince veya kullanıcı temizleyince |
| `systemcel.analyticsConsent` | localStorage | Google Analytics izin veya ret tercihini saklama | Tercih | Kullanıcı tarayıcı verisini temizleyene kadar |
| Google Analytics (`_ga` ve sağlayıcının ürettiği ilişkili anahtarlar) | Çerez / tarayıcı verisi | İzin veren kullanıcılar için ziyaret ölçümü | Zorunlu değil | Google yapılandırması ve kullanıcı tercihiyle sınırlı |

Yeni reklam, A/B testi veya üçüncü taraf widget eklenmeden önce bu tablo güncellenmeli; zorunlu olmayan depolama kullanıcı tercihinden önce başlatılmamalıdır.

## 5. Hukuk danışmanına verilecek karar listesi

1. Ürünün B2B/B2C kullanıcı ayrımı ve tüketici mevzuatının hangi senaryolarda uygulanacağı.
2. Dijital hizmette cayma hakkı, hizmetin hemen ifası ve iade yaklaşımının nihai dili.
3. Anlık ücretli abonelikte açık onay, KDV, yenileme ve dönem sonu iptal dilinin yeterliliği.
4. KVKK hukuki sebepleri, veri saklama süreleri, yurt dışı aktarım mekanizması ve veri işleyen sözleşmeleri.
5. Clerk, Oracle Cloud, Google Analytics, e-posta, ödeme ve isteğe bağlı GİB/Telegram/AI sağlayıcıları için aktarım ve alt işleyen listesi.
6. Muhasebeci–işletme çalışma alanında veri sorumlusu/veri işleyen rollerinin sınırı.
7. Destek, güvenlik olayı, veri sahibi başvurusu, hesap kapatma ve kayıt silme süreleri.

DeepSeek canlı AI sağlayıcısı olarak seçildiğinde, genel “AI sağlayıcısı” ifadesi tek başına kapanış kanıtı sayılmaz. Sağlayıcının Çin'deki veri işleme/saklama açıklaması, model geliştirme veri kullanımını kapatma tercihi, veri işleyen sözleşmesi ve KVKK yurt dışı aktarım dayanağı hukuk danışmanıyla doğrulanmalı; onaylanan sağlayıcı adı ve amaç/sınır alt işleyen listesine eklenmelidir.

## 6. Canlı yayın kapısı

- MERSİS/ticaret sicil ve KEP bilgisi gerekiyorsa resmî kaynaktan doğrulanmadan metinler “nihai” işaretlenmez.
- Hukuk onayı, onaylanan dosyanın sürümü ve tarihiyle kayda geçirilir.
- Canlı ödeme sağlayıcısının adı, tahsilat/iade yolu ve iletişim bilgileri checkout metniyle karşılaştırılır.
- Test hesabında kayıt → yasal metin → ödeme onayı → iptal → kabul kanıtı uçtan uca doğrulanır.
- Yayımdan sonra kullanıcıya gösterilen metin ile veritabanındaki metin özeti aynı olmalıdır.
