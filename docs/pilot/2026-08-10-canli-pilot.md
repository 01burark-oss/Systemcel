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

## Kalan engeller

- AI ekranı canlı anahtar bekliyor.
- Muhasebeci profil görseli yükleme, tarayıcı uzantısının dosya erişimi kapalı olduğu için tamamlanamadı.
- Muhasebeci başvuru onayı için `SYSTEMCEL_ADMIN_CLERK_USER_IDS` tanımlı değil.
- Bu iki engel kalkınca muhasebeci müşteri kabulü, çalışma alanı geçişi, dosya/sohbet ve muhasebeci Fake ödeme zinciri yeniden test edilecek.

## Otomatik doğrulama

- Web TypeScript kontrolü: geçti.
- Vitest: 13/13 geçti.
- .NET: 121/121 geçti.
