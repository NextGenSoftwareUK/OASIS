#Requires -Version 7.5
[CmdletBinding()]
param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw 'Serve requires explicit federation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/TonCql'
$results=Join-Path $root 'TestResults/TonCqlCurrent'
New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-ton-retained-demo -Operation {
    $ownership=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $names=@('NODE_PATH','OASIS_TON_NODE','OASIS_TON_WORKER','OASIS_TON_BINDING','OASIS_TON_CODE_HASH','OASIS_TON_CQL_MODE',
        'OASIS_TON_CQL_STATE','OASIS_TON_CQL_STORAGE_KEY','OASIS_MONGO_REPLICA_SET_CONNECTION','OASIS_TON_MONGO_SERVE')
    $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
    $mongo=$null
    function ArchivePassed([string]$path) {
        if(!(Test-Path $path)){return}
        [xml]$old=Get-Content $path -Raw
        if($old.TestRun.ResultSummary.Counters.total -ne '1' -or $old.TestRun.ResultSummary.Counters.passed -ne '1'){
            throw 'Preserve failed/incomplete prior acceptance report.'
        }
        $archive=Join-Path $results 'Archive';New-Item -ItemType Directory -Force -Path $archive | Out-Null
        $target=Join-Path $archive ((Get-FileHash $path).Hash+'-'+[IO.Path]::GetFileName($path))
        if(Test-Path $target){throw 'Preserve existing archive without overwrite.'}
        Move-Item -LiteralPath $path -Destination $target
    }
    try {
        foreach($pending in @('demo-setup.pending.json','native/pending.json','mongo.pending.json')){
            if(Test-Path (Join-Path $state $pending)){throw 'Retained operation/fixture needs reconciliation; no retry/reseed.'}
        }
        if($Federated -and (Get-NetTCPConnection -State Listen -LocalPort 27029,5109 -ErrorAction SilentlyContinue)){
            throw 'Paired endpoint occupied; do not adopt another process.'
        }
        $sdk=Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge'
        if(!(Test-Path (Join-Path $sdk 'node_modules/@ton/sandbox/package.json'))){throw 'Restore pinned SDK once before running.'}
        $env:NODE_PATH=Join-Path $sdk 'node_modules'
        $env:OASIS_TON_NODE=(Get-Command node.exe).Source
        $env:OASIS_TON_WORKER=Join-Path $sdk 'native-worker.cjs'
        & node (Join-Path $PSScriptRoot 'compile-cosmic-ton.cjs') *> (Join-Path $results 'retained-contract-build.log')
        if($LASTEXITCODE){throw 'Native contract compilation failed.'}
        $env:OASIS_TON_BINDING=Join-Path $results 'compiled/TonHolonStore_TonHolonStore.cjs'
        $env:OASIS_TON_CODE_HASH=((& node -e 'const{Cell}=require("@ton/core");const fs=require("node:fs");console.log(Cell.fromBoc(fs.readFileSync(process.argv[1]))[0].hash().toString("hex"))' (Join-Path $results 'compiled/TonHolonStore_TonHolonStore.code.boc')) -join '').Trim()
        if($LASTEXITCODE){throw 'Could not bind native code identity.'}
        $keyPath=Join-Path $state 'storage-key.dpapi'
        if(!(Test-Path $keyPath)){
            if((Test-Path (Join-Path $state 'demo-seed.json')) -or (Test-Path (Join-Path $state 'native'))){throw 'Original source key missing; never replace it.'}
            $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
            try {
                $protected=[Security.Cryptography.ProtectedData]::Protect($key,$null,'CurrentUser')
                $file=[IO.File]::Open($keyPath,'CreateNew','Write','None')
                try{$file.Write($protected);$file.Flush($true)}finally{$file.Dispose()}
            } finally {[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
        }
        $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,'CurrentUser')
        try {
            if($key.Length -ne 32){throw 'Original private key has wrong size.'}
            $env:OASIS_TON_CQL_STORAGE_KEY=[Convert]::ToBase64String($key)
        } finally {[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
        $env:OASIS_TON_CQL_MODE='owned-local-tvm-demo';$env:OASIS_TON_CQL_STATE=$state
        $project=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests.csproj'
        $assets=Join-Path (Split-Path $project) 'obj/project.assets.json'
        if(!(Test-Path $assets) -or (Get-Content $assets -Raw) -notmatch 'NextGenSoftware.OASIS.API.Providers.MongoDBOASIS'){
            & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q *> (Join-Path $results 'retained-restore.log')
            if($LASTEXITCODE){throw 'Cache-only consumer restore failed.'}
        }
        $targets=@(Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/NextGenSoftware.OASIS.API.Providers.TONOASIS.csproj')
        if($Federated){$targets+=Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj'}
        foreach($target in $targets+@($project)){
            & dotnet build $target --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly *> (Join-Path $results ([IO.Path]::GetFileNameWithoutExtension($target)+'-retained-build.log'))
            if($LASTEXITCODE){throw 'Retained demo producer/consumer build failed.'}
        }
        if(!$Federated -or !(Test-Path (Join-Path $state 'demo-seed.json'))){
            $report=Join-Path $results 'ton-retained-native.trx';ArchivePassed $report
            & dotnet test $project --no-build --no-restore --filter FullyQualifiedName~TonRetainedCqlTests --logger 'trx;LogFileName=ton-retained-native.trx' --results-directory $results -v:q *> (Join-Path $results 'ton-retained-native.log')
            if($LASTEXITCODE){throw 'Retained native proof failed; preserve source/intents.'}
            [xml]$proof=Get-Content $report -Raw
            if($proof.TestRun.ResultSummary.Counters.total -ne '1' -or $proof.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one real native proof required.'}
        }
        if($Federated){
            $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $data=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Original Mongo environment missing; no reset.'}
            if(((& $runtime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo runtime differs.'}
            $mongo=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'ton-mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $ready=$false
            for($i=0;$i -lt 40;$i++){
                if($mongo.HasExited){throw 'Owned Mongo exited during startup.'}
                $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listeners){if(@($listeners|Where-Object OwningProcess -ne $mongo.Id).Count){throw 'Mongo listener owner differs.'};$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$ready){throw 'Owned Mongo startup deadline exceeded.'}
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_TON_MONGO_SERVE=if($Serve){'1'}else{'0'}
            $report=Join-Path $results 'ton-mongo-federation.trx';ArchivePassed $report
            & dotnet test $project --no-build --no-restore --filter FullyQualifiedName~TonMongoCqlNativeTests --logger 'trx;LogFileName=ton-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'ton-mongo-federation.log')
            if($LASTEXITCODE){throw 'Native federation failed; preserve exact fixture.'}
            [xml]$proof=Get-Content $report -Raw
            if($proof.TestRun.ResultSummary.Counters.total -ne '1' -or $proof.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one executed paired proof required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Temporary Mongo cleanup incomplete.'}
        }
    } finally {
        try {
            if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}};$mongo.Dispose()}
        } finally {
            foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])}
            $ownership.Dispose()
        }
    }
}
