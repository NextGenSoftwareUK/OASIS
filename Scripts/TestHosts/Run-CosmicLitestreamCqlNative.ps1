param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/LitestreamCql'
$results=Join-Path $root 'TestResults/LitestreamCqlCurrent'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Litestream native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $oldConnection=$env:OASIS_LITESTREAM_CQL_CONNECTION;$oldState=$env:OASIS_LITESTREAM_CQL_STATE
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original source setup unresolved; preserve without replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw "Original Mongo fixture unresolved."}
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.IntegrationTests'
        & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
        if($LASTEXITCODE){throw 'Cached restore failed.'}
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        $env:OASIS_LITESTREAM_CQL_CONNECTION='Data Source='+(Join-Path $state 'source.db')
        $env:OASIS_LITESTREAM_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~LitestreamCqlNativeTests --results-directory $results --logger 'trx;LogFileName=litestream-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Native Litestream acceptance failed; retain source/intent.'}
        [xml]$report=Get-Content (Join-Path $results 'litestream-cql-native.trx') -Raw
        if($report.TestRun.ResultSummary.Counters.passed -ne '1' -or $report.TestRun.ResultSummary.Counters.total -ne '1'){throw 'One native pass without skips required.'}

    } finally {
        $env:OASIS_LITESTREAM_CQL_CONNECTION=$oldConnection;$env:OASIS_LITESTREAM_CQL_STATE=$oldState
        $lease.Dispose()
    }
}
