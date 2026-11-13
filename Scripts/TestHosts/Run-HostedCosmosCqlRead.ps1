#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$ConfigurationPath,[switch]$Mongo,[switch]$Serve)
if($Serve -and !$Mongo){throw 'The hosted browser preset requires explicit -Mongo federation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$configuration=[IO.Path]::GetFullPath($ConfigurationPath)
if(!(Test-Path -LiteralPath $configuration -PathType Leaf)){throw 'Existing non-secret hosted configuration required.'}
if([string]::IsNullOrWhiteSpace($env:OASIS_HOSTED_COSMOS_AUTH_KEY)){throw 'Set the hosted auth key in the protected process environment; never pass it on the command line.'}
$work=Join-Path $root 'TestResults/HostedCosmosCurrent'
foreach($name in @('read-only-report.json','read-only-report.json.next','hosted-read-current.trx')){
    if(Test-Path -LiteralPath (Join-Path $work $name)){throw 'Preserve previous hosted outcome before another run.'}
}
if($Mongo -and (Test-Path (Join-Path $root 'LocalState/COSMIC/CosmosCqlCurrent/mongo.pending.json'))){throw 'Reconcile the original local Mongo fixture before hosted pairing.'}
$project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests.csproj'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-hosted-read -Operation {
    $priorConfig=$env:OASIS_HOSTED_COSMOS_CONFIG;$priorExecute=$env:OASIS_HOSTED_COSMOS_EXECUTE
    $priorMongo=$env:OASIS_HOSTED_COSMOS_MONGO;$priorServe=$env:OASIS_COSMOS_MONGO_SERVE
    $mongoProcess=$null
    try{
        $builds=@('Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.csproj',$project)
        if($Mongo){$builds=@('Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj')+$builds}
        foreach($build in $builds){
            $buildPath=if([IO.Path]::IsPathRooted($build)){$build}else{Join-Path $root $build}
            & dotnet build $buildPath --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Hosted read producer/consumer build failed.'}
        }
        if($Mongo){
            $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $data=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Retained Mongo data required; no replacement environment.'}
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5092 -ErrorAction SilentlyContinue){throw 'Owned Mongo/teaching port is occupied.'}
            if(((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo8.3.11 runtime required.'}
            New-Item -ItemType Directory -Path $work -Force|Out-Null
            $mongoProcess=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $work 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $ready=$false
            for($attempt=0;$attempt -lt 40;$attempt++){
                if($mongoProcess.HasExited){throw 'Owned Mongo exited.'}
                $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listener){if(@($listener|Where-Object OwningProcess -ne $mongoProcess.Id).Count){throw 'Mongo listener ownership changed.'};$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$ready){throw 'Mongo readiness deadline exceeded.'}
        }
        $env:OASIS_HOSTED_COSMOS_MONGO=if($Mongo){'1'}else{$null}
        $env:OASIS_COSMOS_MONGO_SERVE=if($Serve){'1'}else{$null}
        $env:OASIS_HOSTED_COSMOS_CONFIG=$configuration;$env:OASIS_HOSTED_COSMOS_EXECUTE='1'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~HostedCosmosReadTests.ReadExistingHostedRecordsThroughSharedQueries' --logger 'trx;LogFileName=hosted-read-current.trx' --results-directory $work /m:2 /nr:false /p:UseSharedCompilation=false
        if($LASTEXITCODE){throw 'Hosted read acceptance incomplete; preserve original evidence, no retry/fallback.'}
        [xml]$report=Get-Content -LiteralPath (Join-Path $work 'hosted-read-current.trx') -Raw
        $counts=$report.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'One complete unskipped hosted acceptance case required.'}
        if(!(Test-Path -LiteralPath (Join-Path $work 'read-only-report.json'))){throw 'Hosted structured evidence missing.'}
    }finally{
        $env:OASIS_HOSTED_COSMOS_CONFIG=$priorConfig;$env:OASIS_HOSTED_COSMOS_EXECUTE=$priorExecute
        $env:OASIS_HOSTED_COSMOS_MONGO=$priorMongo;$env:OASIS_COSMOS_MONGO_SERVE=$priorServe
        if($mongoProcess){if(!$mongoProcess.HasExited){$mongoProcess.Kill();if(!$mongoProcess.WaitForExit(30000)){throw 'Owned Mongo teardown failed.'}};$mongoProcess.Dispose()}
    }
}
