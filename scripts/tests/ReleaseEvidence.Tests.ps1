$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$script:Generator = Join-Path $RepoRoot "scripts/New-SystemcelReleaseEvidence.ps1"
$script:Validator = Join-Path $RepoRoot "scripts/Test-SystemcelReleaseEvidence.ps1"
$script:Sha = "48d76c7fc191d433fcbb57d087521fdc529c22ec"

Describe "Systemcel release evidence" {
    It "creates a valid template without claiming external checks passed" {
        $path = Join-Path $TestDrive "evidence.json"

        & $Generator -CandidateSha $Sha -EnvironmentName "staging" -OutputPath $path
        { & $Validator -Path $path } | Should Not Throw

        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $evidence.candidateSha | Should Be $Sha
        $evidence.schemaVersion | Should Be 2
        (@($evidence.gates).id -join ",") | Should Be "release,onboarding,tenant-access,ai-chat-ui,physical-ios,rollback,pilot,R1,K1,K2,K3,K4,K5,K6,K7,K8,K9,A1,A2,A3,A4,A5,A6,A7,D1,D2,D3,D4,D5"
        (@($evidence.gates | Where-Object id -eq "onboarding").checklistRefs -join ",") | Should Be "A2"
        @($evidence.gates | Where-Object id -eq "K1").Count | Should Be 1
        (@($evidence.gates | Where-Object id -eq "K1").checklistRefs -join ",") | Should Be "K1"
        @($evidence.gates | ForEach-Object checks | Where-Object status -eq "passed").Count | Should Be 0
        (@($evidence.gates | Where-Object externalRequired).status -join ",") | Should Not Match "passed"
    }

    It "rejects a passed real-world check without timestamp, result and evidence reference" {
        $path = Join-Path $TestDrive "unsupported-pass.json"
        & $Generator -CandidateSha $Sha -EnvironmentName "production" -OutputPath $path
        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $evidence.gates[1].checks[0].status = "passed"
        $evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8NoBOM

        $threw = $false
        try { & $Validator -Path $path } catch { $threw = $true }
        $threw | Should Be $true
    }

    It "accepts a physical-device pass only when device and evidence details are recorded" {
        $path = Join-Path $TestDrive "iphone.json"
        & $Generator -CandidateSha $Sha -EnvironmentName "production" -OutputPath $path
        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $check = $evidence.gates | Where-Object id -eq "physical-ios" | Select-Object -ExpandProperty checks -First 1
        $check.status = "passed"
        $check.observedAtUtc = "2026-09-11T08:30:00Z"
        $check.actualResult = "Kritik akış tamamlandı."
        $check.evidencePath = "private://pilot/physical-ios/iphone-01"
        $check.blocker = $null
        $check.actorLabel = "pilot-owner-01"
        $check.device = "iPhone 15 / iOS 20 / Safari"
        $evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8NoBOM

        { & $Validator -Path $path } | Should Not Throw
    }

    It "validates legacy evidence without treating its K IDs as current checklist items" {
        $path = Join-Path $TestDrive "legacy.json"
        & $Generator -CandidateSha $Sha -EnvironmentName "production" -OutputPath $path
        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $evidence.schemaVersion = 1
        $evidence.gates = @($evidence.gates | Select-Object -First 7)
        $legacyIds = @("K0", "K1", "K2", "K6", "K7", "K8", "K9")
        for ($index = 0; $index -lt $legacyIds.Count; $index++) {
            $evidence.gates[$index].id = $legacyIds[$index]
            $evidence.gates[$index].PSObject.Properties.Remove("checklistRefs")
        }
        $evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8NoBOM
        $output = & $Validator -Path $path 3>&1
        ($output | Out-String) | Should Match "legacy v1"
    }

    It "rejects a current gate linked to a different checklist item" {
        $path = Join-Path $TestDrive "wrong-reference.json"
        & $Generator -CandidateSha $Sha -EnvironmentName "production" -OutputPath $path
        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        ($evidence.gates | Where-Object id -eq "K1").checklistRefs = @("A2")
        $evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8NoBOM

        $output = & (Get-Command pwsh).Source -NoProfile -File $Validator -Path $path 2>&1 | Out-String
        $LASTEXITCODE | Should Be 1
        $output | Should Match "Gate K1 checklistRefs must be K1"
    }
}
