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

## Kalan engeller

- AI ekranı canlı anahtar bekliyor.
- Muhasebeci başvuru onayı için `SYSTEMCEL_ADMIN_CLERK_USER_IDS` tanımlı değil.
- Canlı düzeltme yayımlandıktan sonra profil yükleme ve başvuru gönderimi yeniden test edilecek; yönetici erişimi açılınca müşteri kabulü, çalışma alanı geçişi, dosya/sohbet ve muhasebeci Fake ödeme zinciri tamamlanacak.

## Otomatik doğrulama

- Web TypeScript kontrolü: geçti.
- Vitest: 13/13 geçti.
- .NET: 121/121 geçti.
- Playwright mobil rota senaryosunun çoklu sayfa süresi CI için gerçek kapsama göre ayarlandı.
- CI cihaz matrisinin kullandığı Chromium ve WebKit tarayıcıları workflow'da birlikte kuruluyor.
- Yüksek önem dereceli `nanoid` bildirimi güvenli `3.3.18` sürümüne yükseltilerek kapatıldı.
