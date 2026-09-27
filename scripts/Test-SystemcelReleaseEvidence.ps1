[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$Path
)

$ErrorActionPreference = "Stop"
$document = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
$isLegacy = $document.schemaVersion -eq 1
$schemaPath = Join-Path $PSScriptRoot $(if ($isLegacy) { "release-evidence-v1.schema.json" } else { "release-evidence.schema.json" })
if (-not (Test-Json -LiteralPath $Path -SchemaFile $schemaPath)) {
    throw "Release evidence does not match $schemaPath."
}
$errors = [System.Collections.Generic.List[string]]::new()
$allowedStatuses = @("pending", "passed", "failed", "blocked", "not-applicable")
$requiredGateIds = if ($isLegacy) { @("K0", "K1", "K2", "K6", "K7", "K8", "K9") } else { @("release", "onboarding", "tenant-access", "ai-chat-ui", "physical-ios", "rollback", "pilot", "R1", "K1", "K2", "K3", "K4", "K5", "K6", "K7", "K8", "K9", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "D1", "D2", "D3", "D4", "D5") }
$physicalGateId = if ($isLegacy) { "K7" } else { "physical-ios" }
$expectedChecklistRefs = @{
    "release" = @("R1", "A7"); "onboarding" = @("A2"); "tenant-access" = @("A1", "A2", "P1-1")
    "ai-chat-ui" = @("A2", "A3", "A4"); "physical-ios" = @("A1", "A4")
    "rollback" = @("A5", "A7"); "pilot" = @("A1", "A7")
    "R1" = @("R1"); "K1" = @("K1"); "K2" = @("K2"); "K3" = @("K3")
    "K4" = @("K4"); "K5" = @("K5"); "K6" = @("K6"); "K7" = @("K7")
    "K8" = @("K8"); "K9" = @("K9"); "A1" = @("A1"); "A2" = @("A2")
    "A3" = @("A3"); "A4" = @("A4"); "A5" = @("A5")
    "A6" = @("A6"); "A7" = @("A7"); "D1" = @("D1"); "D2" = @("D2")
    "D3" = @("D3"); "D4" = @("D4"); "D5" = @("D5")
}

function Add-Error([string]$Message) { $script:errors.Add($Message) }
function Test-FullSha([string]$Value) { return $Value -match '^[0-9a-f]{40}$' }
function Test-Observation([object]$Observation, [string]$Label) {
    if ($allowedStatuses -notcontains [string]$Observation.status) { Add-Error "$Label has an invalid status."; return }
    if ($Observation.status -in @("passed", "failed")) {
        if ([string]::IsNullOrWhiteSpace([string]$Observation.observedAtUtc)) { Add-Error "$Label needs observedAtUtc when executed." }
        if ([string]::IsNullOrWhiteSpace([string]$Observation.actualResult)) { Add-Error "$Label needs actualResult when executed." }
        if ([string]::IsNullOrWhiteSpace([string]$Observation.evidencePath)) { Add-Error "$Label needs evidencePath when executed." }
    }
    if ($Observation.status -in @("pending", "blocked") -and [string]::IsNullOrWhiteSpace([string]$Observation.blocker)) {
        Add-Error "$Label needs a blocker while pending or blocked."
    }
}

if ($document.schemaVersion -notin @(1, 2)) { Add-Error "schemaVersion must be 1 or 2." }
if (-not (Test-FullSha ([string]$document.candidateSha))) { Add-Error "candidateSha must be a lowercase full commit SHA." }
if ([string]$document.release.sourceSha -ne [string]$document.candidateSha) { Add-Error "release.sourceSha must equal candidateSha." }
if ($null -ne $document.release.deployedSha -and -not (Test-FullSha ([string]$document.release.deployedSha))) {
    Add-Error "release.deployedSha must be null or a lowercase full commit SHA."
}
try { [DateTimeOffset]::Parse([string]$document.recordedAtUtc) | Out-Null } catch { Add-Error "recordedAtUtc must be an ISO-8601 timestamp." }
Test-Observation $document.release.bundle "release/bundle"
Test-Observation $document.release.health "release/health"

$gateIds = @($document.gates | ForEach-Object { [string]$_.id })
foreach ($gateId in $requiredGateIds) {
    if (@($gateIds | Where-Object { $_ -eq $gateId }).Count -ne 1) { Add-Error "Gate $gateId must occur exactly once." }
}
if (@($gateIds | Select-Object -Unique).Count -ne $gateIds.Count) { Add-Error "Gate IDs must be unique." }

foreach ($gate in @($document.gates)) {
    if (-not $isLegacy -and @($gate.checklistRefs).Count -eq 0) { Add-Error "Gate $($gate.id) needs checklistRefs." }
    if (-not $isLegacy -and $expectedChecklistRefs.ContainsKey([string]$gate.id)) {
        $actualRefs = @($gate.checklistRefs | Sort-Object) -join ","
        $expectedRefs = @($expectedChecklistRefs[[string]$gate.id] | Sort-Object) -join ","
        if ($actualRefs -ne $expectedRefs) { Add-Error "Gate $($gate.id) checklistRefs must be $expectedRefs." }
    }
    if ($allowedStatuses -notcontains [string]$gate.status) { Add-Error "Gate $($gate.id) has an invalid status." }
    if (@($gate.checks).Count -eq 0) { Add-Error "Gate $($gate.id) must contain checks." }

    foreach ($check in @($gate.checks)) {
        $label = "$($gate.id)/$($check.scenario)"
        if ([string]::IsNullOrWhiteSpace([string]$check.scenario)) { Add-Error "$label needs a scenario." }
        if ([string]::IsNullOrWhiteSpace([string]$check.expectedResult)) { Add-Error "$label needs an expectedResult." }
        Test-Observation $check $label
        if ($check.status -in @("passed", "failed") -and [string]::IsNullOrWhiteSpace([string]$check.actorLabel)) {
            Add-Error "$label needs an anonymous actorLabel when executed."
        }
        if ($gate.id -eq $physicalGateId -and $check.status -eq "passed" -and [string]::IsNullOrWhiteSpace([string]$check.device)) {
            Add-Error "$label needs physical device and OS/browser details when passed."
        }
    }

    if ($gate.status -eq "passed" -and @($gate.checks | Where-Object status -notin @("passed", "not-applicable")).Count -gt 0) {
        Add-Error "Gate $($gate.id) cannot pass while a check is incomplete."
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    throw "Release evidence validation failed with $($errors.Count) error(s)."
}

Write-Output "Release evidence is structurally valid: $([System.IO.Path]::GetFullPath($Path))"
if ($isLegacy) { Write-Warning "This is a legacy v1 evidence file. Its K1-K9 IDs do not match the current YAPILACAKLAR.md items; create a v2 template for current release acceptance." }
