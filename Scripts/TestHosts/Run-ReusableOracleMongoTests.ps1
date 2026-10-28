#Requires -Version 7.0
[CmdletBinding()]
param([switch]$ReconcileManagerFailure,[switch]$Serve)
if($Serve -and $ReconcileManagerFailure){throw 'Interactive demo is unavailable during original-fixture reconciliation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/DemoBreadthCurrent'
if(!$ReconcileManagerFailure -and (Test-Path (Join-Path $root 'TestResults/OracleCurrent/oracle-cql-native-current.trx'))){throw 'Preserve/review prior CQL report before starting services.'}
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
$data=Join-Path $root 'LocalState/COSMIC/mongo-data'
if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Retained Mongo data required; no replacement initialization.'}
foreach($name in @('fixture.pending.json','mongo.pending.json')){
    if(!$ReconcileManagerFailure -and (Test-Path (Join-Path $root ('LocalState/COSMIC/OracleCqlCurrent/'+$name)))){throw 'Reconcile original Oracle/Mongo fixture before writes.'}
}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-native-mongo-oracle -Operation {
    $owned=$null;$prior=$env:OASIS_ORACLE_MONGO_DEMO;$priorServe=$env:OASIS_ORACLE_MONGO_SERVE
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 27029,5090 -ErrorAction SilentlyContinue){throw 'Owned Mongo/Oracle demo port occupied.'}
        if(((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b'){throw 'Retained Mongo8.3.11 runtime required.'}
        foreach($project in @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.csproj')){
            & dotnet build (Join-Path $root $project) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Native mixed producer build failed.'}
        }
        $owned=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mixed-oracle-mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready=$false
        for($attempt=0;$attempt -lt 40;$attempt++){
            if($owned.HasExited){throw 'Owned Mongo exited.'}
            $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if($listener){if(@($listener|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener ownership differs.'};$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Mongo listener readiness deadline exceeded.'}
        $env:OASIS_ORACLE_MONGO_DEMO='1'
        $env:OASIS_ORACLE_MONGO_SERVE=if($Serve){'1'}else{$null}
        if($ReconcileManagerFailure){ & (Join-Path $PSScriptRoot 'Run-ReusableOracleTests.ps1') -CqlReconcile }
        else { & (Join-Path $PSScriptRoot 'Run-ReusableOracleTests.ps1') -Cql }
    }finally{
        $env:OASIS_ORACLE_MONGO_DEMO=$prior
        $env:OASIS_ORACLE_MONGO_SERVE=$priorServe
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo did not stop.'}};$owned.Dispose()}
    }
}
