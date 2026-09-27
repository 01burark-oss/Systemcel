[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$CandidateSha,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$EnvironmentName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

function New-Observation([string]$Blocker) {
    [ordered]@{
        status = "pending"
        observedAtUtc = $null
        actualResult = $null
        evidencePath = $null
        blocker = $Blocker
    }
}

function New-Check(
    [string]$Scenario,
    [string]$Role,
    [string]$Expected,
    [string]$Blocker,
    [string]$Device = $null
) {
    $check = [ordered]@{
        scenario = $Scenario
        role = $Role
        actorLabel = $null
        expectedResult = $Expected
        status = "pending"
        observedAtUtc = $null
        actualResult = $null
        evidencePath = $null
        blocker = $Blocker
    }
    if (-not [string]::IsNullOrWhiteSpace($Device)) { $check.device = $Device }
    $check
}

function New-Gate(
    [string]$Id,
    [string]$Title,
    [string]$Scenario,
    [string]$Expected,
    [string]$Blocker,
    [bool]$ExternalRequired = $true
) {
    [ordered]@{
        id = $Id
        checklistRefs = @($Id)
        title = $Title
        externalRequired = $ExternalRequired
        status = "pending"
        checks = @((New-Check $Scenario "responsible-actor" $Expected $Blocker))
    }
}

$externalAccount = "Kontrollü gerçek hesap ve sağlayıcı erişimi gerekli."
$physicalDevice = "Fiziksel cihaz ve cihaz sahibi gerekli; WebKit emülasyonu yeterli değildir."
$pilotParticipants = "Pilot katılımcıları, takvim ve destek kanalı gerekli."

$document = [ordered]@{
    schemaVersion = 2
    candidateSha = $CandidateSha.ToLowerInvariant()
    environment = $EnvironmentName
    recordedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    release = [ordered]@{
        sourceSha = $CandidateSha.ToLowerInvariant()
        deployedSha = $null
        bundle = New-Observation "SHA-sabitli yayın paketi henüz oluşturulmadı veya doğrulanmadı."
        health = New-Observation "Aday sürümün hedef ortam sağlık kaydı henüz alınmadı."
    }
    gates = @(
        [ordered]@{
            id = "release"; checklistRefs = @("R1", "A7"); title = "Sürüm ve kanıt envanteri"; externalRequired = $false; status = "pending"
            checks = @(
                (New-Check "candidate-sha" "developer" "Kaynak, paket ve hedef ortam aynı tam commit SHA ile ilişkilidir." "Paket ve hedef ortam SHA kaydı gerekli."),
                (New-Check "public-health" "developer" "Liveness, readiness ve public smoke aynı SHA için geçer." "Hedef ortam public smoke kaydı gerekli.")
            )
        },
        [ordered]@{
            id = "onboarding"; checklistRefs = @("A2"); title = "Gerçek Clerk kaydı ve kurulum"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "email-signup-onboarding" "business-owner" "Kayıt, doğrulama, rol, kurulum ve ana ekran tek kullanıcı/işletme üretir." $externalAccount),
                (New-Check "oauth-resume-idempotency" "accountant" "OAuth dönüşü, yenileme ve tekrar callback mükerrer kayıt üretmez." $externalAccount),
                (New-Check "signout-session-expiry" "business-owner" "Çıkış ve süresi dolmuş oturum sonrası korunan API reddedilir." $externalAccount)
            )
        },
        [ordered]@{
            id = "tenant-access"; checklistRefs = @("A1", "A2", "P1-1"); title = "İşletme ve üyelik sınırları"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "tenant-a-to-b-denial" "owner-staff-accountant" "Liste, kayıt, rapor, dosya ve sohbet erişimleri tenant B verisini sunucuda reddeder." $externalAccount),
                (New-Check "revoked-membership" "staff" "Yetkisi kaldırılan açık sekme, dosya bağlantısı ve sohbet bağlantısı yeniden reddedilir." $externalAccount),
                (New-Check "invite-and-ownership-boundaries" "owner" "Davet, kapasite, rol ve sahiplik kuralları yarış ve tekrar kullanımda korunur." $externalAccount)
            )
        },
        [ordered]@{
            id = "ai-chat-ui"; checklistRefs = @("A2", "A3", "A4"); title = "AI, sohbet dosyası ve kritik arayüz"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "ai-provider-and-tenant-context" "business-owner" "Gerçek yanıt/kota/timeout görünür; tenant B verisi yanıta girmez." "AI sağlayıcı erişimi ve iki kontrollü tenant gerekli."),
                (New-Check "chat-file-roundtrip" "business-owner-accountant" "Küçük dosya iki rolde gönderilip indirilir; bozuk/büyük/yetkisiz dosya reddedilir." "Kontrollü hesaplar ve zararsız test dosyası gerekli."),
                (New-Check "critical-ui-keyboard-states" "keyboard-user" "Kritik akışlarda sıra, odak dönüşü, Escape ve yükleme/boş/hata durumları kullanılabilir." "Aday sürümde kontrollü kullanıcı oturumu gerekli.")
            )
        },
        [ordered]@{
            id = "physical-ios"; checklistRefs = @("A1", "A4"); title = "Fiziksel iPhone ve Safari"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "iphone-safari-critical-flow" "business-owner" "Kayıt, klavye, işletme değişimi, dosya/kamera, rapor, modal ve ağ dönüşü geçer." $physicalDevice "MODEL / iOS / Safari sürümünü girin")
            )
        },
        [ordered]@{
            id = "rollback"; checklistRefs = @("A5", "A7"); title = "Tekrarlanabilir yayın ve geri dönüş"; externalRequired = $false; status = "pending"
            checks = @(
                (New-Check "immutable-release-bundle" "developer" "Tam SHA, manifest ve SHA-256 içeren paket yeniden üretilebilir." "Release bundle workflow çıktısı gerekli."),
                (New-Check "schema-compatible-rollback" "developer" "Önceki doğrulanmış sürüme dönüş izole ortamda readiness ve smoke ile geçer." "İzole rollback provası ve önceki doğrulanmış SHA gerekli.")
            )
        },
        [ordered]@{
            id = "pilot"; checklistRefs = @("A1", "A7"); title = "Sınırlı gerçek kullanıcı pilotu"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "pilot-roster-and-support" "pilot-coordinator" "Anonim katılımcı etiketleri, cihazlar, veri sınırı ve destek kanalı kayıtlıdır." $pilotParticipants),
                (New-Check "pilot-critical-tasks" "business-and-accountant" "Kritik görevler desteklenen roller/cihazlarda en az bir kez tamamlanır; ham sayılar tutulur." $pilotParticipants),
                (New-Check "pilot-release-decision" "pilot-coordinator" "Kritik açık hata yoktur; son 48 saat, yedek ve alarm durumu değerlendirilir." $pilotParticipants)
            )
        },
        (New-Gate "R1" "Blog ve katalog yayını" "blog-catalog-production" "Üç yazı, boş/dolu katalog ve public API aday SHA'da canlı doğrulanır." "CI, deploy ve canlı sayfa kanıtı gerekli."),
        (New-Gate "K1" "PayTR adaptörü" "provider-payment-payout-refund" "Gerçek test ortamı tahsilat, iade ve aktarım referansları uygulama kayıtlarıyla eşleşir." "PayTR test erişimi ve gerçek adapter kabulü gerekli."),
        (New-Gate "K2" "Ödeme bildirimleri" "verified-replay-safe-events" "İmzalı tahsilat/aktarım olayları tekrar veya sıra değişiminde tek muhasebe etkisi üretir." "Sağlayıcı callback ve sorgu kanıtı gerekli."),
        (New-Gate "K3" "Günlük ödeme mutabakatı" "provider-ledger-reconciliation" "Tahsilat, iade ve transfer farkı tespit edilir; etkilenen aktarım durur ve inceleme açılır." "Gerçek test raporu ve kasıtlı fark denemesi gerekli."),
        (New-Gate "K4" "Abonelik ve satış belgesi" "renewal-price-document" "Yenileme ve 30 günlük fiyat koruması gerçek tahsilatta geçer; satış belgesi referansı izlenir." "PayTR yetkisi, belge yöntemi ve teslim kanıtı gerekli."),
        (New-Gate "K5" "Kabul kararı düzeltmesi" "receipt-correction-accounting" "Kısmi/tam ve aktarım öncesi/sonrası düzeltme tek ters kayıt zinciriyle uzlaşır." "Düzeltme kodu ve muhasebe kabul kanıtı gerekli."),
        (New-Gate "K6" "Pilot e-belgesi ve eski referanslar" "document-history-linkage" "Pilot belgeleri sağlayıcı durumuyla, eski kesin kayıtlar siparişten hakedişe kadar eşleşir." "Gerçek e-belge sağlayıcısı ve eski veri önizlemesi gerekli."),
        (New-Gate "K7" "Çift onay" "distinct-approver-threshold" "Belirlenen risk/eşik üzerindeki kabul iki ayrı yetkili olmadan kesinleşmez." "D4 eşiği ve iki yetkiliyle kabul gerekli."),
        (New-Gate "K8" "Çevrimdışı mal kabul" "offline-reconciliation" "Taslak süre/çakışma kontrolünden geçer; tekrarda çift stok veya hakediş oluşmaz." "D4 ihtiyaç kararı ve fiziksel cihaz kanıtı gerekli."),
        (New-Gate "K9" "İtiraz süreleri ve risk" "dispute-sla-risk-review" "Süre aşımı ve risk sinyali gerekçeli yönetici incelemesine gider." "D4 süre/risk kuralları ve örnek olay kanıtı gerekli."),
        (New-Gate "A1" "Gerçek tedarikçi ve depo pilotu" "supplier-warehouse-pilot" "Gerçek siparişten mal kabul, iade ve muhasebe mutabakatına kadar fiziksel depo akışı doğrulanır." "Yetkili tedarikçi, depo, gerçek ürün ve pilot kanıtı gerekli."),
        (New-Gate "A2" "Hesap, yetki ve AI kabulü" "tenant-ai-account-acceptance" "Gerçek kayıt, işletme ayrımı, AI öneri onayı ve maskeden ad geri getirme doğru işletmede doğrulanır." "İki kontrollü işletme hesabı ve gerçek AI sağlayıcı erişimi gerekli."),
        (New-Gate "A3" "Olay bildirimi ve dosya teslimi" "event-delivery-file-access" "Yenileme/fiyat bildirimi doğru alıcıya ulaşır; sohbet dosyası yetkisi korunur." "Kontrollü alıcı ve iki rol hesabı gerekli."),
        (New-Gate "A4" "Kritik arayüz kabulü" "keyboard-visual-physical-ios" "Klavye, odak, boş/yükleniyor/hata ve kritik finans ekranları fiziksel cihaz dahil doğrulanır." "Kontrollü hesap ve fiziksel iPhone/Safari kanıtı gerekli."),
        (New-Gate "A5" "Tam kurtarma" "offsite-full-restore" "Üç ardışık günlük yedek, başka makinede tam kurtarma ve ölçülen RPO/RTO doğrulanır." "Bağımsız ortam ve kabul hedefleri gerekli."),
        (New-Gate "A6" "Dış izleme ve ikinci alarm" "off-vm-alert-recovery" "Kalıcı dış metrik, ikinci alıcı, kesinti ve düzelme mesajları doğrulanır." "İkinci alıcı/kanal ve izole test hedefi gerekli."),
        (New-Gate "A7" "Ücretli yayın kararı" "paid-release-signoff" "Gerekli P0, pilot, gerçek para ve geri dönüş kanıtları değerlendirilip yayın kararı kaydedilir." "Burak'ın yayın kararı gerekli."),
        (New-Gate "D1" "PayTR sözleşme kapsamı" "merchant-capabilities" "Mağazanın API, transfer, valör ve tekrarlayan ödeme yetkileri yazılı doğrulanır." "PayTR ve yetkili hesap yanıtı gerekli."),
        (New-Gate "D2" "Gerçek katalog ve inceleme" "real-supplier-catalog" "Yetkili gerçek tedarikçinin ürünleri anonim görünür; inceleme erişimi sınırlandırılır." "Tedarikçi ürünleri ve inceleme hesabı gerekli."),
        (New-Gate "D3" "Hukuk ve satış belgesi kararı" "marketplace-legal-document" "Pazaryeri metinleri, PSP ve abonelik belgesi yöntemi yetkililerce onaylanır." "Hukuk/mali müşavir kararı gerekli."),
        (New-Gate "D4" "Pilot operasyon kararları" "pilot-rules" "Kategori, yetki, çift onay, çevrimdışı ve itiraz kuralları yazılıdır." "Depo/operasyon ve hukuk/finans kararı gerekli."),
        (New-Gate "D5" "AI veri işleme" "ai-processing-approval" "API işleme ve aktarım şartları ile alt işleyen metni sürümlü onaylanır." "Hukuk ve işletme sahibi kararı gerekli.")
    )
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$parent = Split-Path -Parent $resolvedOutput
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}
$document | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedOutput -Encoding utf8NoBOM
Write-Output "Release evidence template created: $resolvedOutput"
