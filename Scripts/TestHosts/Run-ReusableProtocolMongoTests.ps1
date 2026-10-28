#Requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('ActivityPub','IPFS','SOLID')][string]$Provider='ActivityPub',[switch]$Reconcile,[switch]$ArchiveCompletedDemo)
$ErrorActionPreference='Stop'
if($Reconcile -and $Provider -ne 'ActivityPub'){throw 'Protocol fixture reconciliation requires its retained provider journal; no new protocol writes are allowed in this mode.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/DemoBreadthCurrent'
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
$data=Join-Path $root 'LocalState/COSMIC/mongo-data'
$project=Join-Path $root 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests.csproj'
if($Provider -eq 'IPFS'){$project=Join-Path $root 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests.csproj'}
if($Provider -eq 'SOLID'){$project=Join-Path $root 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.csproj'}
$report=if($Reconcile){'native-mongo-mastodon-reconcile.trx'}else{'native-mongo-mastodon.trx'}
if($Provider -eq 'ActivityPub' -and (Test-Path (Join-Path $results $report))){throw 'Preserve previous native federation evidence before rerunning.'}
if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Retained Mongo data required; never initialize a replacement.'}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-native-protocol-mongo -Operation {
    $owned=$null
    $names=@('OASIS_PROTOCOL_MONGO_ROOT','OASIS_PROTOCOL_MONGO_RECONCILE','OASIS_ACTIVITYPUB_PUBLIC_LIVE','OASIS_CQL_POINT_SERVE')
    $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 27029,5088 -ErrorAction SilentlyContinue){throw 'Owned demo ports occupied; preserve other operations.'}
        if(((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b'){throw 'Expected retained Mongo8.3.11 runtime.'}
        foreach($build in @((Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj'),$project)){
            & dotnet build $build --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Native federation producer/consumer build failed.'}
        }
        $owned=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'protocol-mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready=$false
        for($attempt=0;$attempt -lt 40;$attempt++){
            if($owned.HasExited){throw 'Owned Mongo exited.'}
            $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if($listener){if(@($listener|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener ownership differs.'};$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Mongo readiness deadline exceeded.'}
        $env:OASIS_PROTOCOL_MONGO_ROOT=$root;$env:OASIS_PROTOCOL_MONGO_RECONCILE=if($Reconcile){'1'}else{'0'}
        $env:OASIS_ACTIVITYPUB_PUBLIC_LIVE='1';$env:OASIS_CQL_POINT_SERVE='0'
        if($Provider -eq 'IPFS'){
            & (Join-Path $PSScriptRoot 'Run-ReusableIpfsTests.ps1') -CqlKeySet -ArchiveCompletedDemo:$ArchiveCompletedDemo
        }elseif($Provider -eq 'SOLID'){
            & (Join-Path $PSScriptRoot 'Run-ReusableSolidReadiness.ps1') -CqlKeySet -ArchiveCompletedDemo:$ArchiveCompletedDemo -RequireCleanShutdown
        }else{
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~PublicMastodonKeySetTests' --logger "trx;LogFileName=$report" --results-directory $results -v:q
        if($LASTEXITCODE){throw 'Native federation acceptance failed; preserve journal and report.'}
        [xml]$proof=Get-Content (Join-Path $results $report) -Raw
        if([int]$proof.TestRun.ResultSummary.Counters.passed -ne 1 -or [int]$proof.TestRun.ResultSummary.Counters.total -ne 1){throw 'Exactly one native federation case required.'}
        }
    }finally{
        foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])}
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo did not stop.'}};$owned.Dispose()}
    }
}
