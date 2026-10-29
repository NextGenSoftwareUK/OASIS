param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/LitestreamCql'
$results=Join-Path $root 'TestResults/LitestreamCqlCurrent'
$exe='C:/Users/david/AppData/Local/OASIS/tools/litestream/0.5.17/litestream.exe'
if((Get-FileHash $exe -Algorithm SHA256).Hash -ne '271ECB4EC49A4895A98A8FDD884113034DACC9D75B4A8D7A1B02322BAE7FC7B7'){throw 'Original pinned replication runtime required.'}
$lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
$oldState=$env:OASIS_LITESTREAM_CQL_STATE;$oldExec=$env:OASIS_LITESTREAM_REPLICATION_EXEC
try {
    $pending=Join-Path $state 'replication.pending.json'
    $receipt=Join-Path $state 'replication.json'
    $restored=Join-Path $state 'restored.db'
    $source=Join-Path $state 'source.db'
    $replica=Join-Path $state 'file-replica'
    $config=Join-Path $state 'replication.yml'
    if(!(Test-Path (Join-Path $state 'demo-seed.json')) -or !(Test-Path $source)){throw 'Original native source required.'}
    if(Test-Path $pending){throw 'Original replication outcome unresolved; inspect without replay.'}
    $project=(Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.IntegrationTests').Replace('\','/')
    $resultPath=$results.Replace('\','/')
    $env:OASIS_LITESTREAM_CQL_STATE=$state
    if(!(Test-Path $receipt)){
        foreach($path in @($replica,$restored,(Join-Path $state 'replication-probe.pending.json'),(Join-Path $state 'replication-probe.json'))){if(Test-Path $path){throw 'Preserve original replica/restore/probe; no adoption.'}}
        $intent=[Text.Encoding]::UTF8.GetBytes((@{source=$source;replica=$replica;restored=$restored;runtime='0.5.17'}|ConvertTo-Json))
        $file=[IO.File]::Open($pending,'CreateNew','Write','Read');try{$file.Write($intent);$file.Flush($true)}finally{$file.Dispose()}
        $sourceYaml=$source.Replace('\','/').Replace("'","''")
        $replicaYaml=$replica.Replace('\','/').Replace("'","''")
        [IO.File]::WriteAllText($config,"dbs:`n  - path: '$sourceYaml'`n    replica:`n      type: file`n      path: '$replicaYaml'`n")
        $env:OASIS_LITESTREAM_REPLICATION_EXEC='1'
        $command="dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~WriteOwnedProbeDuringNativeReplication --results-directory $resultPath --logger trx"
        & $exe replicate -config $config -exec $command *> (Join-Path $results 'replication.log')
        if($LASTEXITCODE){throw 'Actual sidecar replication failed; preserve source/replica/probe outcomes.'}
        if(!(Test-Path (Join-Path $state 'replication-probe.json'))){throw 'Live probe acknowledgement missing.'}
        & $exe restore -config $config -o $restored $source *> (Join-Path $results 'restore.log')
        if($LASTEXITCODE){throw 'Native restore failed; preserve outcomes without replay.'}
    } elseif(!(Test-Path $restored) -or !(Test-Path $replica)) {throw 'Original restore/replica missing; never recreate implicitly.'}
    & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~RestoredReplicaPreservesOriginalHolonsAndLiveProbe --results-directory $results --logger 'trx;LogFileName=litestream-restore.trx' *> (Join-Path $results 'restore-test.log')
    if($LASTEXITCODE){throw 'Restored records/CQL verification failed; preserve originals.'}
    [xml]$report=Get-Content (Join-Path $results 'litestream-restore.trx') -Raw
    if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'One actual restored-source pass without skips required.'}
    if(Test-Path $pending){Move-Item -LiteralPath $pending -Destination $receipt}
} finally {
    $env:OASIS_LITESTREAM_CQL_STATE=$oldState;$env:OASIS_LITESTREAM_REPLICATION_EXEC=$oldExec
    $lease.Dispose()
}
