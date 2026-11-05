#Requires -Version 7.0
[CmdletBinding()]
param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$BuildOnly) {
    foreach ($name in @('COSMIC_MINIO_HOSTED_SOURCE','COSMIC_MINIO_HOSTED_ACCESS_KEY','COSMIC_MINIO_HOSTED_SECRET_KEY')) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { throw "Required native configuration absent: $name" }
    }
    $descriptor = Get-Item -LiteralPath $env:COSMIC_MINIO_HOSTED_SOURCE
    if ($descriptor.PSIsContainer -or $descriptor.Length -lt 1 -or $descriptor.Length -gt 16384) { throw 'Bounded source descriptor required.' }
}
$state = Join-Path $taskRoot 'LocalState/COSMIC/RecoveryLaptopMinIO20250907'
$phasePrefix = if ($BuildOnly) { 'hosted-build-' } else { 'hosted-native-' }
$phase = Join-Path $state ($phasePrefix + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $phase | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-minio-hosted-native -Operation {
    Push-Location $taskRoot
    try {
        $project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests.csproj'
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $phase 'build.log')
        if ($LASTEXITCODE) { throw "Native fixture build failed; preserve $phase" }
        if (!$BuildOnly) {
            $coreRevision = (& git -C (Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core') rev-parse HEAD).Trim()
            if ($LASTEXITCODE) { throw 'Core source revision could not be observed.' }
            $sourceHash = (Get-FileHash -LiteralPath $env:COSMIC_MINIO_HOSTED_SOURCE -Algorithm SHA256).Hash
            @{ coreRevision = $coreRevision; sourceSha256 = $sourceHash; startedUtc = [DateTime]::UtcNow.ToString('O'); operation = 'read-only-existing-source' } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $phase 'intent.json')
            dotnet test $project --no-build --filter 'FullyQualifiedName~MinioHostedNativeSourceTests' /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $phase --logger 'trx;LogFileName=native.trx' *> (Join-Path $phase 'test.log')
            if ($LASTEXITCODE) { throw "Native qualification failed; preserve $phase" }
            [xml]$report = Get-Content -LiteralPath (Join-Path $phase 'native.trx') -Raw
            $counters = $report.TestRun.ResultSummary.Counters
            if ($counters.total -ne '1' -or $counters.passed -ne '1' -or $counters.failed -ne '0' -or $counters.notExecuted -ne '0') { throw 'Exactly one native pass, zero skips required.' }
            if ((Get-FileHash -LiteralPath $env:COSMIC_MINIO_HOSTED_SOURCE -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Source admission descriptor changed.' }
            @{ coreRevision = $coreRevision; sourceSha256 = $sourceHash; trxSha256 = (Get-FileHash -LiteralPath (Join-Path $phase 'native.trx') -Algorithm SHA256).Hash; passed = 1; finishedUtc = [DateTime]::UtcNow.ToString('O') } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $phase 'ack.json')
        }
    }
    finally { Pop-Location }
}
Write-Output "MinIO hosted native phase evidence: $phase"
