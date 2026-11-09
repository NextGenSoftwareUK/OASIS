#Requires -Version 7.0
[CmdletBinding()]
param([switch]$ArchiveCompletedDemo, [switch]$Mongo, [switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$profile = if($Mongo){'NativeImportMongoCurrent'}else{'NativeImportCqlCurrent'}
$work = Join-Path $root ('TestResults/' + $profile)
$journal = Join-Path $root ('LocalState/COSMIC/' + $profile + '/fixture.pending.json')
if(Test-Path -LiteralPath $journal){throw 'Reconcile the original fixture; never replay writes.'}
$files = if($Mongo){@('source.db','unused-adapter.db')}else{@('source.db','target.db')}
foreach($name in $files){foreach($suffix in @('','-wal','-shm','-journal')){
    if(Test-Path -LiteralPath (Join-Path $work ($name+$suffix))){throw 'Preserve original owned database outcome before another run.'}
}}

function Preserve-CompletedEvidence([string]$Path){
    $archive = Join-Path $work 'Archive'
    New-Item -ItemType Directory -Path $archive -Force | Out-Null
    $hash = (Get-FileHash -LiteralPath $Path).Hash
    $destination = Join-Path $archive ($hash+'-'+[IO.Path]::GetFileName($Path))
    if(Test-Path -LiteralPath $destination){
        if((Get-FileHash -LiteralPath $destination).Hash -ne $hash){throw 'Existing archive differs; preserve both outcomes.'}
        Remove-Item -LiteralPath $Path # Identical completed content already retained.
    }else{Move-Item -LiteralPath $Path -Destination $destination}
    if((Get-FileHash -LiteralPath $destination).Hash -ne $hash){throw 'Archived evidence differs.'}
}

$report = Join-Path $work 'report.json'
if(Test-Path -LiteralPath $report){
    if(!$ArchiveCompletedDemo){throw 'Review completed evidence or specify ArchiveCompletedDemo.'}
    $document = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $completed = if($Mongo){$document.workflow}else{$document}
    if(!$completed.Cleaned -or !$completed.DistinctActorIds -or !$completed.SourcePreserved -or
       $completed.Created -ne 3 -or $completed.Unchanged -ne 3 -or $completed.Updated -ne 1){throw 'Only a completed verified fixture can be archived.'}
    if($Mongo -and (!$document.exactCleanupVerified -or $document.physicalTargetRows -ne 6)){throw 'Cross-provider cleanup proof missing.'}
    Preserve-CompletedEvidence $report
}
$trx = Join-Path $work 'native-import-cql.trx'
if($VerifyOnly -and (Test-Path -LiteralPath $trx)){
    if(!$ArchiveCompletedDemo){throw 'Preserve prior native report before another run.'}
    [xml]$previous = Get-Content -LiteralPath $trx -Raw
    $counts = $previous.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Only a completed unskipped native pass can be archived.'}
    Preserve-CompletedEvidence $trx
}

# One build lease and one retained service; never allocate a second environment.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-native-import-demo -Operation {
    $project = Join-Path $root 'Examples/CosmicCqlDemo/CosmicCqlDemo.csproj'
    $tests = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
    $owned = $null
    $prior = $env:OASIS_NATIVE_IMPORT_MONGO
    try{
        if(!(Test-Path (Join-Path $root 'Examples/CosmicCqlDemo/obj/project.assets.json'))){
            & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Reusable cached demo metadata unavailable; no automatic download.'}
        }
        if($Mongo){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Mongo producer build failed.'}
        }
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Native import demo build failed.'}
        if($VerifyOnly){
            & dotnet build $tests --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Native test consumer build failed.'}
        }
        if($Mongo){
            $runtime = 'C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $data = Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Retained Mongo data required; no replacement initialization.'}
            if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Owned Mongo port occupied.'}
            if(((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo8.3.11 runtime required.'}
            New-Item -ItemType Directory -Path $work -Force | Out-Null
            $owned = Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $work 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $ready = $false
            for($attempt=0;$attempt -lt 40;$attempt++){
                if($owned.HasExited){throw 'Owned Mongo exited.'}
                $listener = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listener){if(@($listener|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Mongo listener ownership changed.'};$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$ready){throw 'Mongo readiness deadline exceeded.'}
        }
        if($VerifyOnly){
            $env:OASIS_NATIVE_IMPORT_MONGO = if($Mongo){'1'}else{$null}
            $method = if($Mongo){'NativeSqliteDataImportsIntoMongoAndQueriesWithStablePrivateIdentity'}else{'NativeSqliteDataImportsSavesAndQueriesWithStablePrivateIdentity'}
            & dotnet test $tests --no-build --no-restore --filter ('FullyQualifiedName~NativeImportCqlDemoNativeTests.'+$method) --logger 'trx;LogFileName=native-import-cql.trx' --results-directory $work /m:2 /nr:false /p:UseSharedCompilation=false
        }else{
            $mode = if($Mongo){'--native-import-mongo'}else{'--native-import'}
            & dotnet run --project $project --no-build --no-restore -- $mode
        }
        if($LASTEXITCODE){throw 'Native import acceptance incomplete; preserve original journal.'}
        if((Test-Path -LiteralPath $journal) -or !(Test-Path -LiteralPath $report)){throw 'Verified cleanup/report required.'}
        Write-Output ('Completed report SHA256 '+(Get-FileHash -LiteralPath $report).Hash)
    }finally{
        $env:OASIS_NATIVE_IMPORT_MONGO = $prior
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo teardown failed.'}};$owned.Dispose()}
    }
}
