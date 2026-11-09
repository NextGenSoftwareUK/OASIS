#Requires -Version 7.0
param([switch]$MultiRecord,[switch]$ServePointDemo,[switch]$ArchiveCompletedReport)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/ActivityPubCurrent'
$state=Join-Path $root 'LocalState/COSMIC/ActivityPubCurrent'
New-Item -ItemType Directory -Path $work,$state -Force|Out-Null
$lease=[IO.File]::Open((Join-Path $state 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$priorLive=$env:OASIS_ACTIVITYPUB_PUBLIC_LIVE;$priorServe=$env:OASIS_CQL_POINT_SERVE
try{
    if(Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue){throw 'Point demo listener occupied; stop it before another demo.'}
    $reportName=if($MultiRecord){'activitypub-cql-keyset-native.trx'}else{'activitypub-cql-interactive-native.trx'}
    $trx=Join-Path $work $reportName
    if(Test-Path -LiteralPath $trx){
        if(!$ArchiveCompletedReport){throw 'Preserve prior report; use ArchiveCompletedReport only for a completed pass.'}
        [xml]$report=Get-Content -LiteralPath $trx -Raw
        if([int]$report.TestRun.ResultSummary.Counters.passed -ne 1 -or [int]$report.TestRun.ResultSummary.Counters.total -ne 1){throw 'Prior report is not a completed native pass.'}
        $archive=Join-Path $work 'Archive';New-Item -ItemType Directory -Path $archive -Force|Out-Null
        $hash=(Get-FileHash -LiteralPath $trx).Hash
        $target=Join-Path $archive ($hash+'-'+$reportName)
        if(Test-Path -LiteralPath $target){throw 'Existing archive must not be overwritten.'}
        Move-Item -LiteralPath $trx -Destination $target
        if((Get-FileHash -LiteralPath $target).Hash -ne $hash){throw 'Archive hash mismatch.'}
    }
    $project=Join-Path $root 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests'
    foreach($item in @((Join-Path $root 'Providers/Network/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS'),$project)){
        & dotnet build $item --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Current ActivityPub demo build failed.'}
    }
    $env:OASIS_ACTIVITYPUB_PUBLIC_LIVE='1';$env:OASIS_CQL_POINT_SERVE=if($ServePointDemo){'1'}else{'0'}
    & dotnet test $project --no-build --no-restore --filter $(if($MultiRecord){'FullyQualifiedName~PublicMastodonKeySetTests'}else{'Category=ActivityPubCqlInteractiveNative'}) --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Public status interactive native verification failed; preserve evidence.'}
    [xml]$selected=Get-Content -LiteralPath $trx -Raw
    $counts=$selected.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Exactly one completed native interactive case must pass.'}
}finally{$env:OASIS_ACTIVITYPUB_PUBLIC_LIVE=$priorLive;$env:OASIS_CQL_POINT_SERVE=$priorServe;$lease.Dispose()}
