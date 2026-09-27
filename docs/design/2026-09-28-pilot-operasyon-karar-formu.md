# Pilot operasyon karar formu — D4

**Karar sahipleri:** Burak, depo sorumlusu ve mali müşavir. Bu formdaki boş alanlar onaylanmış kural değildir. İlk gerçek pilotta K7 çift onay, K8 çevrimdışı mal kabul ve K9 itiraz/risk davranışını kodlamak için birlikte doldurulacaktır. Karar tarihi, pilot kapsamı ve onaylayan kişiler aynı sürümde saklanmalıdır.

## 1. Pilot sınırı

| Karar | Yazılı değer | Sorumlu |
|---|---|---|
| Pilottaki işletmeler, depolar, tedarikçiler ve ürün kategorileri | Bekliyor | Burak + depo |
| Aynı anda açık kalabilecek sipariş/teslimat sayısı veya toplam tutar sınırı | Bekliyor | Burak + mali müşavir |
| Pilot başlangıç/bitiş tarihi ve durdurma yetkilisi | Bekliyor | Burak |
| Hata, fiziksel hasar, fazla/eksik teslim ve para farkında insan inceleme yolu | Bekliyor | Üçlü karar |

## 2. Çift onay — K7

| Karar | Yazılı değer | Sorumlu |
|---|---|---|
| Tek onayla kesinleşebilecek azami teslim/kabul tutarı ve para birimi | Bekliyor | Burak + mali müşavir |
| Tutar düşük olsa da ikinci onay isteyen risk işaretleri | Bekliyor | Depo + mali müşavir |
| İlk ve ikinci onayı verebilen roller; aynı kişinin iki rolü olsa da ikinci imza verip veremeyeceği | Bekliyor | Burak + depo |
| İkinci onay beklerken stok, fatura/cari ve hakedişin durumu | Bekliyor | Depo + mali müşavir |
| Miktar/belge/karar değişince onayın ne zaman geçersiz olacağı ve kaç saat bekleyeceği | Bekliyor | Üçlü karar |

Uygulama kabulü: iki farklı yetkili gerekirse ikinci geçerli onaydan önce kesin stok ve hakediş oluşmaz; tekrar istek ikinci kayıt üretmez. Yetki kaybı, karar değişikliği ve eşzamanlı iki onay ayrı denenir.

## 3. Çevrimdışı mal kabul — K8

| Karar | Yazılı değer | Sorumlu |
|---|---|---|
| İlk pilotta çevrimdışı kabul gerekli mi; hangi depo/cihazlarda | Bekliyor | Depo + Burak |
| Bağlantısız taslağın azami süresi ve cihazda saklanabilecek veri/fotoğraf | Bekliyor | Depo + Burak |
| Süresi dolmuş, cihazı değişmiş veya çevrimiçi kararla çakışmış taslağın inceleme sorumlusu | Bekliyor | Depo |
| Bağlantı dönmeden stok/fatura/ödeme etkisi gösterilip gösterilmeyeceği | Bekliyor | Depo + mali müşavir |

Uygulama kabulü: süre ve yetki sunucu saatiyle doğrulanır; tekrar senkronizasyon çift kabul, stok veya hakediş yaratmaz. Gerçek cihazda bağlantı kesilip açılarak test edilir.

## 4. İtiraz, süre ve risk — K9

| Karar | Yazılı değer | Sorumlu |
|---|---|---|
| Alıcı ve tedarikçi ilk yanıt/kanıt sunma süresi; başlangıç olayı ve saat dilimi | Bekliyor | Mali müşavir + Burak |
| Süre aşımında hatırlatma, yöneticiye yükseltme ve nihai inceleme süresi | Bekliyor | Burak + depo |
| Eksik sevk, olağandışı kabul/red ve tekrarlı asılsız itiraz için ölçülebilir sinyaller | Bekliyor | Depo + mali müşavir |
| Riskli kayıtta yalnız inceleme, geçici limit, aktarım durdurma veya çift onay seçeneklerinden hangisi | Bekliyor | Burak + mali müşavir |
| Yanlış işaretlemeyi kaldırma, itiraz etme ve denetim izi sorumlusu | Bekliyor | Üçlü karar |

Uygulama kabulü: süre aşımı doğru inceleme kuyruğuna gider; risk nedeni görünür ve kararı aktör/zaman/gerekçeyle kaydedilir. Otomatik kalıcı yaptırım uygulanmaz.

## 5. Karar kaydı

| Kayıt | Durum |
|---|---|
| Karar tarihi (UTC) | Bekliyor |
| Pilot için geçerli aday SHA/sürüm | Bekliyor |
| Burak onayı | Bekliyor |
| Depo sorumlusu onayı | Bekliyor |
| Mali müşavir onayı | Bekliyor |
| Karar değişikliği tarihi ve eski kuralların hangi kayıtlar için geçerli kalacağı | Bekliyor |

Onay gelmeden K7–K9'un eşik/süre/risk değerleri koda sabitlenmez ve bu maddeler kapanmış sayılmaz.
