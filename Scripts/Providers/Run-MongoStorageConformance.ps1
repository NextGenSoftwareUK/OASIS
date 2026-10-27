#Requires -Version 7.0
[CmdletBinding()]
param([switch]$InspectOriginal,[switch]$CleanupOriginal)
$ErrorActionPreference='Stop'
if($InspectOriginal -and $CleanupOriginal){throw 'Select read-only inspection or reviewed exact cleanup separately.'}
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$nativeWork=Join-Path $taskRoot 'TestResults/SubscriptionUsageCurrent'
$data=Join-Path $nativeWork 'mongo-data'
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
if(!(Test-Path -LiteralPath (Join-Path $data 'WiredTiger'))){throw 'Retained initialized Mongo data required; no new database-server environment or replica initialization.'}
if(!(Test-Path -LiteralPath $runtime) -or (& $runtime --version | Select-Object -First 1) -ne 'db version v8.3.11'){throw 'Retained pinned Mongo8.3.11 runtime required; no download.'}
if(!$InspectOriginal -and !$CleanupOriginal){foreach($mode in @('async','sync')){
    $report=Join-Path $taskRoot ('TestResults/ProviderConformanceCurrent/MongoDBOASIS/'+$mode+'/report.json')
    if((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next'))){throw 'Review original conformance fixture/report before another run.'}
}}
$results=Join-Path $taskRoot 'TestResults/ProviderConformanceCurrent'
$reportName=if($CleanupOriginal){'mongo-core-original-cleanup-current.trx'}elseif($InspectOriginal){'mongo-core-read-count-current.trx'}else{'mongo-core-current.trx'}
if(Test-Path -LiteralPath (Join-Path $results $reportName)){throw 'Preserve the original Mongo conformance TRX.'}
& (Join-Path $taskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Mongo canonical native storage breadth' -Operation {
    # Shared lifecycle lock is the one already used by retained Mongo/CQL tests.
    $serverLease=[IO.File]::Open((Join-Path $nativeWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $owned=$null;$prior=$env:OASIS_MONGO_CONFORMANCE_URI
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Retained Mongo server is in use; coordinate its existing owner rather than stop or duplicate it.'}
        $projects=@(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',
            'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests.csproj')
        foreach($relative in $projects){
            $project=Join-Path $taskRoot $relative
            & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Required cached metadata unavailable; no automatic download or second environment.'}
            & dotnet build $project --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Mongo conformance producer/consumer build failed.'}
        }
        $mongoOutput=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
        . (Join-Path $taskRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $taskRoot -ConsumerOutput $mongoOutput -VerifySourceDocuments
        foreach($assembly in @('NextGenSoftware.OASIS.API.Providers.MongoDBOASIS','NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests')){
            Write-Output ($assembly+': sources='+(Assert-SourceDocuments (Join-Path $mongoOutput ($assembly+'.dll')) $taskRoot))
        }
        $arguments=@('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $nativeWork 'mongo-current.log'),'--logappend')
        $owned=Start-Process -FilePath $runtime -ArgumentList $arguments -WindowStyle Hidden -PassThru
        $deadline=[DateTime]::UtcNow.AddSeconds(30)
        do{
            if($owned.HasExited){throw 'Owned retained Mongo exited; preserve its data/logs, do not reset.'}
            $listeners=@(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue)
            if($listeners.Count){
                if(@($listeners | Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener owner differs from the process started by this runner.'}
                break
            }
            Start-Sleep -Milliseconds 200
        }while([DateTime]::UtcNow -lt $deadline)
        if(!$listeners.Count){throw 'Retained Mongo did not open its owned port.'}
        $env:OASIS_MONGO_CONFORMANCE_URI='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
        $filter=if($CleanupOriginal){'TestCategory=MongoConformanceOriginalCleanup'}elseif($InspectOriginal){'TestCategory=MongoConformanceReadOnly'}else{'TestCategory=MongoCoreStorageConformance'}
        & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $results -v:q
        $testExit=$LASTEXITCODE
        if(!$InspectOriginal -and !$CleanupOriginal){
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers MongoDBOASIS
        if($LASTEXITCODE){throw 'Mongo native report registration failed.'}
        & python -B (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'Mongo matrix generation failed.'}
        }
        if($testExit){throw 'Canonical Mongo storage failed; preserve exact fixtures, no automatic rerun or cleanup.'}
        [xml]$trx=Get-Content (Join-Path $results $reportName)
        $expected=if($CleanupOriginal){1}else{2}
        if($trx.TestRun.ResultSummary.Counters.total -ne $expected -or $trx.TestRun.ResultSummary.Counters.passed -ne $expected -or $trx.TestRun.ResultSummary.Counters.notExecuted -ne 0){throw 'Expected every selected native schedule, no skips.'}
    }
    finally{
        $env:OASIS_MONGO_CONFORMANCE_URI=$prior
        try{
            if($owned -and !$owned.HasExited){
                # Ask the exact owned server to flush and shut down; do not turn a baseline into a crash test.
                $framework=Get-ChildItem -LiteralPath 'C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App' -Directory | Where-Object Name -Like '10.0.*' | Sort-Object { [version]$_.Name } | Select-Object -Last 1
                if(!$framework){throw 'Retained .NET10 logging assembly unavailable; owned Mongo remains live for explicit inspection.'}
                Add-Type -Path (Join-Path $framework.FullName 'Microsoft.Extensions.Logging.Abstractions.dll')
                foreach($assembly in @('MongoDB.Bson.dll','MongoDB.Driver.dll')){Add-Type -Path (Join-Path $mongoOutput $assembly)}
                $client=[MongoDB.Driver.MongoClient]::new('mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000')
                $shutdown=[MongoDB.Driver.BsonDocumentCommand[MongoDB.Bson.BsonDocument]]::new([MongoDB.Bson.BsonDocument]::Parse('{"shutdown":1,"force":true,"timeoutSecs":5}'))
                try{$client.GetDatabase('admin').RunCommand[MongoDB.Bson.BsonDocument]($shutdown,$null,[Threading.CancellationToken]::None) | Out-Null}
                catch{if(!$owned.WaitForExit(10000)){throw 'Owned graceful Mongo shutdown failed; preserve live process/data and inspect, no forced stop or replacement.'}}
                if(!$owned.WaitForExit(10000)){throw 'Owned Mongo is still live; do not remove data or start a replacement.'}
            }
        }finally{$serverLease.Dispose()}
    }
}
