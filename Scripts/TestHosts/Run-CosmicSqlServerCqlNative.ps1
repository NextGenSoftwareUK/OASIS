param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/SqlServerCql'
$results=Join-Path $root 'TestResults/SqlServerCqlCurrent'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
$tool='C:/Program Files/Microsoft SQL Server/170/Tools/Binn/SqlLocalDB.exe'
$instance='CosmicCql'
$instanceDirectory=Join-Path $env:LOCALAPPDATA 'Microsoft/Microsoft SQL Server Local DB/Instances/CosmicCql'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC SQL Server native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $priorConnection=$env:OASIS_SQLSERVER_CQL_CONNECTION;$priorState=$env:OASIS_SQLSERVER_CQL_STATE
    $started=$false;$passed=$false
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Source setup unresolved; do not replay.'}
        $admission=Join-Path $state 'instance.json';$pending=Join-Path $state 'instance.pending.json'
        if(Test-Path $pending){throw 'Instance creation unresolved; inspect original outcome.'}
        $versions=(& $tool versions)-join ' '
        if($LASTEXITCODE -or $versions -notmatch '17\.0\.4025\.3'){throw 'Existing pinned SQL Server2025 runtime required.'}
        if(!(Test-Path $admission)){
            if(Test-Path $instanceDirectory){throw 'Unowned instance exists; no adoption.'}
            [IO.File]::WriteAllText($pending,(@{instance=$instance;version='17.0.4025.3';directory=$instanceDirectory}|ConvertTo-Json))
            & $tool create $instance 17.0 *> (Join-Path $results 'instance-create.log')
            if($LASTEXITCODE){throw 'Instance creation failed; retain intent.'}
            Move-Item -LiteralPath $pending -Destination $admission
        }
        $record=Get-Content $admission -Raw|ConvertFrom-Json
        if($record.instance -ne $instance -or $record.version -ne '17.0.4025.3' -or $record.directory -ne $instanceDirectory -or !(Test-Path $instanceDirectory)){throw 'Original instance admission differs.'}
        $info=(& $tool info $instance)-join "`n"
        if($LASTEXITCODE -or $info -notmatch 'State:\s+Stopped'){throw 'Expected stopped owned instance; no process adoption.'}
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests'
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        $started=$true
        & $tool start $instance *> (Join-Path $results 'instance-start.log')
        if($LASTEXITCODE){throw 'Owned LocalDB start failed.'}
        $env:OASIS_SQLSERVER_CQL_CONNECTION='Server=(localdb)\CosmicCql;Database=cosmic_cql_demo;Integrated Security=true;Encrypt=false;Pooling=false;Connect Timeout=10'
        $env:OASIS_SQLSERVER_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~SqlServerCqlNativeTests --results-directory $results --logger 'trx;LogFileName=sqlserver-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Native acceptance failed; retain original source/journals.'}
        [xml]$report=Get-Content (Join-Path $results 'sqlserver-cql-native.trx') -Raw
        if($report.TestRun.ResultSummary.Counters.passed -ne '1' -or $report.TestRun.ResultSummary.Counters.total -ne '1'){throw 'One native pass without skips required.'}
        $passed=$true
    } finally {
        $env:OASIS_SQLSERVER_CQL_CONNECTION=$priorConnection;$env:OASIS_SQLSERVER_CQL_STATE=$priorState
        try {if($started -and $passed){& $tool stop $instance *> (Join-Path $results 'instance-stop.log');if($LASTEXITCODE){throw 'Owned instance stop failed.'}}}
        finally {$lease.Dispose()}
    }
}
