[CmdletBinding()]
param(
    [string]$BaseUrl = "https://systemcel.app",
    [string]$AllowedOrigin = "https://systemcel.app"
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd("/")
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
    Write-Output "[FAIL] $Message"
}

function Add-Pass([string]$Message) {
    Write-Output "[PASS] $Message"
}

function Get-Response([string]$Path, [string]$Method = "GET", [hashtable]$Headers = @{}) {
    Invoke-WebRequest -Uri "$base$Path" -Method $Method -Headers $Headers -UseBasicParsing
}

function Assert-Status([object]$Response, [int]$Expected, [string]$Name) {
    if ([int]$Response.StatusCode -ne $Expected) {
        Add-Failure "$Name status $($Response.StatusCode), expected $Expected"
    }
    else {
        Add-Pass "$Name status $Expected"
    }
}

function Assert-Header([object]$Response, [string]$Name) {
    if ([string]::IsNullOrWhiteSpace([string]$Response.Headers[$Name])) {
        Add-Failure "Missing response header: $Name"
    }
    else {
        Add-Pass "Response header present: $Name"
    }
}

$root = Get-Response "/"
Assert-Status $root 200 "landing"
@(
    "Strict-Transport-Security",
    "X-Content-Type-Options",
    "X-Frame-Options",
    "Referrer-Policy",
    "Permissions-Policy",
    "Content-Security-Policy"
) | ForEach-Object { Assert-Header $root $_ }

$live = Get-Response "/api/health/live"
Assert-Status $live 200 "liveness"
if ([string]$live.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Liveness response is not JSON"
}
else {
    $liveBody = $live.Content | ConvertFrom-Json
    if ($liveBody.durum -eq "canli") { Add-Pass "Liveness payload" } else { Add-Failure "Unexpected liveness payload" }
}

$ready = Get-Response "/api/health/ready"
Assert-Status $ready 200 "readiness"
if ([string]$ready.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Readiness response is not JSON"
}
else {
    $readyBody = $ready.Content | ConvertFrom-Json
    if ($readyBody.durum -eq "hazir" -and $readyBody.veritabani -eq "PostgreSql") {
        Add-Pass "Readiness payload and PostgreSQL connection"
    }
    else {
        Add-Failure "Unexpected readiness payload"
    }
}

$plans = Get-Response "/api/public/planlar"
Assert-Status $plans 200 "public plans"
if ([string]$plans.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Public plans response is not JSON"
}
else {
    $parsedPlans = $plans.Content | ConvertFrom-Json
    $planBody = @($parsedPlans | ForEach-Object { $_ })
    $invalidPrice = @($planBody | Where-Object { [decimal]$_.aylikTutar -le 0 })
    if ($planBody.Count -eq 5 -and $invalidPrice.Count -eq 0) {
        Add-Pass "Five paid monthly plans"
    }
    else {
        Add-Failure "Expected five paid plans with positive monthly prices"
    }
}

$untrusted = Get-Response "/api/health/live" "OPTIONS" @{
    Origin = "https://untrusted.invalid"
    "Access-Control-Request-Method" = "GET"
}
if ([string]::IsNullOrWhiteSpace([string]$untrusted.Headers["Access-Control-Allow-Origin"])) {
    Add-Pass "Untrusted CORS origin rejected"
}
else {
    Add-Failure "Untrusted CORS origin accepted"
}

$trusted = Get-Response "/api/health/live" "OPTIONS" @{
    Origin = $AllowedOrigin
    "Access-Control-Request-Method" = "GET"
}
if ([string]$trusted.Headers["Access-Control-Allow-Origin"] -eq $AllowedOrigin) {
    Add-Pass "Configured CORS origin accepted"
}
else {
    Add-Failure "Configured CORS origin was not accepted"
}

if ($failures.Count -gt 0) {
    throw "Staging gate failed with $($failures.Count) error(s)."
}

Write-Output "Staging gate passed for $base"
