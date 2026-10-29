param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/UpstashCql'
$results=Join-Path $root 'TestResults/UpstashCqlCurrent'
$resource=Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Upstash'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Upstash native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $priorUrl=$env:OASIS_UPSTASH_CQL_URL;$priorToken=$env:OASIS_UPSTASH_CQL_TOKEN;$priorState=$env:OASIS_UPSTASH_CQL_STATE
    $decoded=$null;$credential=$null
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original native write outcome unresolved; preserve without replay.'}
        $metadata=Get-Content (Join-Path $resource 'resource.json') -Raw|ConvertFrom-Json
        if([DateTimeOffset]::UtcNow -ge [DateTimeOffset]$metadata.ExpiresByUtc -or $metadata.Lifecycle -notlike '*official scratch*'){throw 'Unexpired existing official free scratch admission required; no provisioning.'}
        $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.IntegrationTests'
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        $decoded=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $resource 'credentials.dpapi')),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        $credential=[Text.Encoding]::UTF8.GetString($decoded)|ConvertFrom-Json
        $uri=[Uri]$credential.Url
        if($uri.Scheme -ne 'https' -or !$uri.Host.EndsWith('.upstash.io',[StringComparison]::OrdinalIgnoreCase) -or !$uri.IsDefaultPort -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or [string]::IsNullOrWhiteSpace($credential.Token)){throw 'Protected original Upstash REST identity required.'}
        $env:OASIS_UPSTASH_CQL_URL=$credential.Url;$env:OASIS_UPSTASH_CQL_TOKEN=$credential.Token;$env:OASIS_UPSTASH_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~UpstashCqlNativeTests --results-directory $results --logger 'trx;LogFileName=upstash-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Hosted native acceptance failed; preserve source/intent, no automatic replay.'}
        [xml]$report=Get-Content (Join-Path $results 'upstash-cql-native.trx') -Raw
        if($report.TestRun.ResultSummary.Counters.passed -ne '1' -or $report.TestRun.ResultSummary.Counters.total -ne '1'){throw 'One native pass without skips required.'}
    } finally {
        $env:OASIS_UPSTASH_CQL_URL=$priorUrl;$env:OASIS_UPSTASH_CQL_TOKEN=$priorToken;$env:OASIS_UPSTASH_CQL_STATE=$priorState
        if($decoded){[Array]::Clear($decoded,0,$decoded.Length)};$credential=$null
        $lease.Dispose()
    }
}
