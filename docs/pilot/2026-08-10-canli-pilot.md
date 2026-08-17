# Canlı pilot — 10 Ağustos 2026

## Kapsam

- Ortam: `https://systemcel.app` / Staging
- Ödeme: Fake sağlayıcı; gerçek kart veya dış ödeme kullanılmadı.
- Veri: İşletme, ürün, stok, gelir-gider, cari ve tahsilat kayıtları `PILOT` etiketiyle oluşturuldu.
- Güvenlik sınırı: Gerçek GİB gönderimi, Telegram mesajı, mikrofon/kamera ve üçüncü kişiye işlem yapılmadı.

## Geçen işletme akışları

- Giriş, kolay kurulum, işletme profili ve iki çalışma alanı arasında veri ayrımı.
- Gelir-gider, ürün/hizmet, stok girişi, hızlı satış, cari hareket ve tahsilat.
- Fatura taslağı, onay, ödeme geçmişi ve rapor ZIP'i.
- Büyüme aylık planı için açık onay, Fake checkout, hakların açılması ve dönem sonu iptal.
- GİB ve Telegram ayar ekranları, muhasebeci pazaryeri, sohbetler ve yönetim erişim sınırı.
- 390 px mobil ve masaüstü görünüm; sayfa taşması yok, konsolda hata/uyarı yok.

## Bulunan ve düzeltilen sorunlar

- Ücretsiz planda Hızlı Satış, fatura ve varsayılan cari kart limitlerini atlayabiliyordu. Hak kontrolleri seri transaction içine alındı.
- Cari ekranının ilk yükleme yarışı, yeni kaydı otomatik seçilen mevcut kartın üzerine yazabiliyordu. İlk kartı kendiliğinden düzenleme kaldırıldı.
- Stok hareketi, rapor ayı, hızlı not, ücretsiz plan durumu ve GİB saklama açıklamasındaki Türkçe/teknik metinler sadeleştirildi.
- Tahsilat ekranındaki işlem oluşturmayan `Taslak` ve `Onayla` kopyaları kaldırıldı; tek gerçek kayıt eylemi bırakıldı.
- Muhasebeci kolay kurulumunda profil görseli, hesap rolü kaydedilmeden önce yüklendiği için sunucudan `403` alıyordu. Tamamlanmamış kolay kurulumlarda güvenli görsel yüklemeye izin verildi ve regresyon testi eklendi.
- Başarılı profil görseli yüklemesinden sonra önceki hata metni ekranda kalıyordu; yeni denemede hata durumu temizleniyor.

## 17 Ağustos takibi

- Profil görseli canlı ortamda yüklendi, muhasebeci kolay kurulumu tamamlandı ve başvuru gönderildi.
- Canlı yönetici kimliği yapılandırıldı; başvuru yönetim ekranından onaylandı ve pazaryeri profili yayınlandı.
- Pilot işletme pazaryerinden talep gönderdi; muhasebeci talebi `Okuma + rapor` yetkisiyle kabul etti ve kapasite sayacı `1 / 10` oldu.
- Aylık Muhasebeci Standart planı açık onayla seçildi. Fake sağlayıcıda ₺699,00 + ₺139,80 KDV olmak üzere ₺838,80 ödeme başarıyla tamamlandı.
- Abonelik özeti `Aktif`, dönem `17 Ağustos 2026 — 17 Eylül 2026`, haklar `100 AI mesajı / 1 kullanıcı / 10 müşteri` ve ödeme geçmişi `1 işlem` olarak doğrulandı.
- Muhasebeci müşteri çalışma alanına geçti; dashboard ve üç gelir-gider kaydını gördü, `Okuma + rapor` bağlamında kayıt ekleme girişimi API tarafından reddedildi.
- İşletmenin gönderdiği canlı pilot mesajı muhasebeci sohbetinde doğru konuşma ve okunmamış sayaçla görüntülendi.
- Canlı testte okuma yetkili formun düzenlenebilir görünmesi ve API hata metnindeki bozuk Türkçe bulundu. Form alanları salt okunur bağlamda devre dışı bırakıldı, hata metni UTF-8 Türkçeyle düzeltildi.

## Kalan engeller

- AI ekranı canlı anahtar bekliyor.
- Sohbete zararsız pilot dosyası yükleme, dosya ve hedef için işlem anı kullanıcı onayı bekliyor.
- Pro plan ve ek müşteri kredisi varyasyonları henüz canlı Fake ödeme ile denenmedi.

## Otomatik doğrulama

- Web TypeScript kontrolü: geçti.
- Vitest: 13/13 geçti.
- .NET: 121/121 geçti.
- Playwright mobil rota senaryosunun çoklu sayfa süresi CI için gerçek kapsama göre ayarlandı.
- CI cihaz matrisinin kullandığı Chromium ve WebKit tarayıcıları workflow'da birlikte kuruluyor.
- Yüksek önem dereceli `nanoid` bildirimi güvenli `3.3.18` sürümüne yükseltilerek kapatıldı.
- Profil yükleme düzeltmelerinin son CI çalışması tüm işlerde geçti: https://github.com/01burark-oss/Systemcel/actions/runs/31428871299
- 17 Ağustos takip düzeltmeleri için web lint, TypeScript ve 13/13 Vitest yerelde geçti; .NET tam test paketi yeniden çalıştırıldı.
