# Systemcel Developer API v1

Developer API, Büyüme ve Kurumsal işletme planlarında salt okunur entegrasyonlar için sunulur. Anahtarlar **Ayarlar > Geliştirici API** bölümünden oluşturulur. Oluşturulan anahtar yalnız bir kez gösterilir; kaybedilirse geri getirilemez, iptal edilip yenisi oluşturulmalıdır.

## Kimlik doğrulama

Her istekte anahtarı ayrı başlıkta gönderin:

```bash
curl "https://app.systemcel.app/api/v1/invoices?page=1&pageSize=50" \
  -H "X-Systemcel-Api-Key: sys_live_ORNEK_PREFIX_GIZLI_DEGER"
```

Anahtarı URL, sorgu parametresi, uygulama günlüğü veya istemci tarafı koda koymayın. Sunucudaki secret manager ya da şifreli ortam değişkeninde saklayın. Anahtar işletmeye sabitlenmiştir; başka bir işletmenin verisine erişemez. İptal edilen, süresi dolan veya plan hakkını kaybeden anahtar hemen reddedilir.

## Kapsamlar

| Kapsam | Uç nokta |
| --- | --- |
| `summary:read` | `GET /api/v1/business` |
| `accounts:read` | `GET /api/v1/accounts` |
| `products:read` | `GET /api/v1/products` |
| `invoices:read` | `GET /api/v1/invoices` |
| `bank:read` | `GET /api/v1/bank-transactions` |

`read:all` tüm salt-okunur uç noktaları kapsar. En az ayrıcalık için yalnız gereken kapsamları seçin. Bu MVP hiçbir `write`, `create`, `update` veya `delete` kapsamı sunmaz; finansal kayıt mutasyonu desteklenmez.

## Sayfalama ve hız sınırı

Liste uç noktalarında `page` varsayılan `1`, `pageSize` varsayılan `50` değeridir. `page` en fazla `10000`, `pageSize` en fazla `100` olabilir. Yanıt şekli:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 50,
  "total": 0
}
```

Her API anahtarı dakikada 60 istekle sınırlandırılır. Sınır aşımında `429` ve `application/problem+json` döner. Kimlik doğrulama hataları, anahtar prefix’inin var olup olmadığını açıklamayan tek biçimli `401` yanıtıdır.

## OpenAPI

Makine tarafından okunabilir sözleşme: [`openapi/developer-api-v1.yaml`](openapi/developer-api-v1.yaml).
