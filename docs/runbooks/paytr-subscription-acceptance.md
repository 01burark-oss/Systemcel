# Systemcel abonelik ödeme kabul kaydı

Bu kayıt yalnız Systemcel'in kendi yazılım bedeli içindir. Gerçek kabul sırasında doldurulur; boş alanlar tamamlanmış kabul sayılmaz. Kart bilgisi, Key/Salt veya müşteri kimlik belgesi bu dosyaya yazılmaz. Gerçek kişisel veriler erişimi sınırlı operasyon kaydında tutulur; Git'e eklenmez.

| Alan | Gerçek kabul kaydına girilecek bilgi |
|---|---|
| Aday | Commit SHA, başarılı CI/dağıtım bağlantısı, public smoke |
| Yetki | PayTR'ın tek seferlik 3D dönem satışına ilişkin yanıtı |
| İade sözleşmesi | PayTR yanıtının tarihi/referansı ve doğrulanmış `zero`/`absent` seçimi |
| Sorumlular | Burak, ödeme/iade yetkilisi, belge sorumlusu |
| Pilot | İşletme kimliği; diğer işletmelerin checkout/iade isteği reddedildi mi? |
| Satış | Aylık/yıllık dönem, plan, net tutar/KDV/toplam; kullanıcıya gösterilen ve onaylanan tutar |
| Ödeme | Kalıcı yerel ödeme kimliği, sağlayıcı sipariş referansı, canlı sorgu sonucu, imzalı bildirim zamanı |
| Tekrar/başarısızlık | Bildirim tekrarı tek olay/abonelik üretti mi; başarısız ödeme planı korudu mu? |
| Belge | Mali müşavirin seçtiği sistem, satış belge numarası/tarihi, ödeme bağlantısı |
| Kısmi iade | Onaylayan/zaman, talimat/referans, kullanıcı teyidi, tek gönderim, sorguda eşleşen tutar/ref |
| Kalan iade | Önceki iade sonrası kalan tutar, ayrı onay/ref, sorguda toplam ve tam iade durumu |
| İade belgesi | Satış belgesine bağlı gerçek belge numarası/tarihi ve sorumlu |
| Dönem iptali | Aylık bitiş; yıllık başlangıca göre ay sonu/kalan tam aylar ve ödenen toplam üzerinden hesap |
| Duraklatma | Yeni checkout kapandı mı; başlamış bildirim/sorgu ve gerekli iade korunuyor mu? |
| Son kabul | Kanıtların yeri, Burak'ın kabul tarihi; izin listesinin genişletilmesi kararı |

Gerçek kart işlemini kullanıcı tamamlar. İade API'sinin `success` yanıtı tek başına tamamlanma kanıtı değildir. Sonuç belirsizse aynı veya yeni referansla ikinci kez gönderilmez; sorgu ve destek kaydıyla uzlaştırılır. Yeni ödenmiş aboneliğin eski ödeme iadesinden etkilenmediği ayrıca kontrol edilir.
