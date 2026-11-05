param([Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$SourceConfig)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source=[IO.Path]::GetFullPath($SourceConfig)
if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw 'An existing owned Algolia source descriptor is required.' }
if ([string]::IsNullOrWhiteSpace($env:COSMIC_ALGOLIA_CQL_API_KEY)) { throw 'Set the source-scoped Algolia search credential in the process environment.' }
$sourceHash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$results=Join-Path $root ('LocalState/COSMIC/RecoveryLaptopAlgoliaCql/native-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($results)
@{sourceSha256=$sourceHash;nativeWrites=$false;sourceDescriptor=$source;phase='owned-read-only-qualification'} |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $results 'intent.json') -Encoding utf8
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-algolia-owned-read-qualification' -Operation {
    $priorSource=$env:COSMIC_ALGOLIA_CQL_SOURCE
    try {
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS.IntegrationTests'
        & dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly -v:q *> (Join-Path $results 'build.log')
        if ($LASTEXITCODE) { throw 'Algolia native fixture build failed; preserve original output.' }
        $env:COSMIC_ALGOLIA_CQL_SOURCE=$source
        & dotnet test $project --no-build --no-restore --filter 'TestCategory=AlgoliaCqlNative' /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $results --logger 'trx;LogFileName=native.trx' *> (Join-Path $results 'native.log')
        if ($LASTEXITCODE) { throw 'Algolia native qualification incomplete; preserve first outcome without automatic retry.' }
        $trx=Join-Path $results 'native.trx'
        [xml]$report=Get-Content -LiteralPath $trx -Raw
        $counts=$report.SelectSingleNode("//*[local-name()='Counters']")
        if ($counts.total -ne '1' -or $counts.passed -ne '1' -or $counts.notExecuted -ne '0') { throw 'One native pass with zero skips is required.' }
        if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Original Algolia source descriptor changed.' }
        @{sourceSha256=$sourceHash;nativeWrites=$false;passed=1;skipped=0;trxSha256=(Get-FileHash -LiteralPath $trx -Algorithm SHA256).Hash} |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $results 'acceptance.json') -Encoding utf8
        Write-Output "Read-only Algolia qualification passed. Evidence: $results"
    }
    finally { $env:COSMIC_ALGOLIA_CQL_SOURCE=$priorSource }
}
