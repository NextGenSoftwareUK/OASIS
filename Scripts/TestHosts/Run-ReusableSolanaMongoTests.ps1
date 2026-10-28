#Requires -Version 7.0
[CmdletBinding()]
param([switch]$ArchiveCompletedDemo,[switch]$Serve)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/DemoBreadthCurrent'
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
$data=Join-Path $root 'LocalState/COSMIC/mongo-data'
$source=Join-Path $root 'LocalState/COSMIC/SolanaMetadataCurrent'
if(!(Test-Path (Join-Path $data 'WiredTiger')) -or !(Test-Path (Join-Path $source 'index.db')) -or !(Test-Path (Join-Path $source 'seed.acknowledged.json'))){throw 'Retained Mongo data and acknowledged Solana source/index required; no replacement initialization.'}
if(Test-Path (Join-Path $source 'seed.pending.json')){throw 'Preserve pending original chain submission.'}
if(Test-Path (Join-Path $root 'LocalState/COSMIC/MongoSolanaCurrent/fixture.pending.json')){throw 'Preserve/reconcile exact original Mongo fixture; never replay creates.'}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-native-mongo-solana -Operation {
    $owned=$null;$prior=$env:OASIS_SOLANA_MONGO_FEDERATION_ROOT;$priorServe=$env:OASIS_SOLANA_MIXED_SERVE
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 27029,18899,5089,5091 -ErrorAction SilentlyContinue){throw 'Owned Mongo/Solana demo port occupied.'}
        if(((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b'){throw 'Retained Mongo8.3.11 runtime required.'}
        foreach($project in @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',
            'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.csproj')){
            & dotnet build (Join-Path $root $project) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Native mixed producer build failed.'}
        }
        $owned=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mixed-solana-mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready=$false
        for($attempt=0;$attempt -lt 40;$attempt++){
            if($owned.HasExited){throw 'Owned Mongo exited.'}
            $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if($listener){if(@($listener|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener ownership differs.'};$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Mongo listener readiness deadline exceeded.'}
        $env:OASIS_SOLANA_MONGO_FEDERATION_ROOT=$root
        $env:OASIS_SOLANA_MIXED_SERVE=if($Serve){'1'}else{$null}
        & (Join-Path $PSScriptRoot 'Run-ReusableSolanaTests.ps1') -IndexedMetadataDemo -ArchiveCompletedDemo:$ArchiveCompletedDemo
    }finally{
        $env:OASIS_SOLANA_MONGO_FEDERATION_ROOT=$prior
        $env:OASIS_SOLANA_MIXED_SERVE=$priorServe
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo did not stop.'}};$owned.Dispose()}
    }
}
