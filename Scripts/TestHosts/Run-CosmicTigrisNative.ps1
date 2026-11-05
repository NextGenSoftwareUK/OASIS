#Requires -Version 7.0
[CmdletBinding()]
param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$BuildOnly) {
    foreach ($name in @('COSMIC_TIGRIS_CQL_SOURCE','COSMIC_TIGRIS_CQL_ACCESS_KEY','COSMIC_TIGRIS_CQL_SECRET_KEY')) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { throw "Required native configuration absent: $name" }
    }
    $descriptor = Get-Item -LiteralPath $env:COSMIC_TIGRIS_CQL_SOURCE
    if ($descriptor.PSIsContainer -or $descriptor.Length -lt 1 -or $descriptor.Length -gt 16384) { throw 'Bounded source descriptor required.' }
}
$state = Join-Path $taskRoot 'LocalState/COSMIC/RecoveryLaptopTigrisCql'
$phasePrefix = if ($BuildOnly) { 'build-' } else { 'native-' }
$phase = Join-Path $state ($phasePrefix + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $phase | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-tigris-native -Operation {
    Push-Location $taskRoot
    try {
        $project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests/NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests.csproj'
        dotnet build $project /p:EnableTigrisCqlNativeTests=true /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $phase 'build.log')
        if ($LASTEXITCODE) { throw "Native fixture build failed; preserve $phase" }
        if (!$BuildOnly) {
            $sourceHash = (Get-FileHash -LiteralPath $env:COSMIC_TIGRIS_CQL_SOURCE -Algorithm SHA256).Hash
            @{ sourceSha256 = $sourceHash; startedUtc = [DateTime]::UtcNow.ToString('O'); operation = 'read-only-existing-source' } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $phase 'intent.json')
            dotnet test $project --no-build /p:EnableTigrisCqlNativeTests=true --filter 'FullyQualifiedName~TigrisCqlNativeTests' /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $phase --logger 'trx;LogFileName=native.trx' *> (Join-Path $phase 'test.log')
            if ($LASTEXITCODE) { throw "Native qualification failed; preserve $phase" }
            [xml]$report = Get-Content -LiteralPath (Join-Path $phase 'native.trx') -Raw
            $counters = $report.TestRun.ResultSummary.Counters
            if ($counters.total -ne '1' -or $counters.passed -ne '1' -or $counters.failed -ne '0' -or $counters.notExecuted -ne '0') { throw 'Exactly one native pass, zero skips required.' }
            if ((Get-FileHash -LiteralPath $env:COSMIC_TIGRIS_CQL_SOURCE -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Source admission descriptor changed.' }
            @{ sourceSha256 = $sourceHash; trxSha256 = (Get-FileHash -LiteralPath (Join-Path $phase 'native.trx') -Algorithm SHA256).Hash; passed = 1; finishedUtc = [DateTime]::UtcNow.ToString('O') } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $phase 'ack.json')
        }
    }
    finally { Pop-Location }
}
Write-Output "Tigris phase evidence: $phase"
