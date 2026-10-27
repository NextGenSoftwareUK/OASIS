#Requires -Version 7.0
[CmdletBinding()]
param([switch]$CqlWeb4,[switch]$CqlPoint,[switch]$ReconcileCore,[switch]$CoreStorage,[switch]$RequireCleanShutdown,[string]$EnvironmentPath)
$ErrorActionPreference='Stop'
if(@(@($CqlWeb4,$CqlPoint,$CoreStorage,$ReconcileCore)|Where-Object {$_}).Count -gt 1){throw 'Select one native mode.'}
$taskSolidRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskSolidWork=if($EnvironmentPath){[IO.Path]::GetFullPath($EnvironmentPath)}else{Join-Path $taskSolidRoot 'LocalState/COSMIC/SolidCurrent'}
$taskSolidResults=Join-Path $taskSolidRoot 'TestResults/SolidCurrent'
New-Item -ItemType Directory -Path $taskSolidResults -Force|Out-Null
if(!(Test-Path -LiteralPath $taskSolidWork -PathType Container)){throw 'Retained SOLID work directory required.'}
$taskSolidLease=[IO.File]::Open((Join-Path $taskSolidWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$taskSolidStarted=$false;$taskSolidPrior=$env:OASIS_SOLID_COLD_READONLY_WORK
$taskSolidReport=Join-Path $taskSolidResults $(if($CqlWeb4){'solid-cql-web4-lifecycle.json'}elseif($CqlPoint){'solid-cql-lifecycle.json'}elseif($ReconcileCore){'solid-core-cleanup-lifecycle.json'}elseif($CoreStorage){'solid-core-lifecycle.json'}else{'solid-readiness-current.json'})
$taskSolidTrxName=if($CqlWeb4){'solid-cql-web4-native.trx'}elseif($CqlPoint){'solid-cql-native.trx'}elseif($ReconcileCore){'solid-core-cleanup.trx'}elseif($CoreStorage){'solid-core-current.trx'}else{'solid-ready-read-current.trx'}
$taskSolidTrx=Join-Path $taskSolidResults $taskSolidTrxName
$taskSolidSamples=[Collections.Generic.List[object]]::new()
$taskSolidEvidence=[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('O');scope=$(if($CqlWeb4){'native private Pod WEB4 rows and measured query receipt; controlled identity/outbox'}elseif($CqlPoint){'native private Pod CQL/sharedmanager/LINQ/demo with exact owned fixture cleanup'}elseif($ReconcileCore){'exact original owned fixture readback and conditional cleanup; deleted children never replayed'}elseif($CoreStorage){'current-source shared storage baseline in explicitly selected owned Pod'}else{'frozen-output readiness and one authenticated read; not current-source acceptance'});freeBefore=(Get-PSDrive C).Free;socketReadyMs=$null;testExit=$null;stopExit=$null;finalState=$null;samples=$taskSolidSamples}
try{
    if((Test-Path $taskSolidReport) -or (Test-Path $taskSolidTrx)){throw 'Preserve original readiness evidence before another run.'}
    $taskSolidPending=@(Get-ChildItem -LiteralPath $taskSolidWork -Filter '*pending*.json')
    if($taskSolidPending -and (!$ReconcileCore -or @($taskSolidPending|Where-Object Name -notin @('core-async.pending.json','core-sync.pending.json')).Count)){throw 'Original pending SOLID fixtures require reconciliation.'}
    foreach($taskSolidName in @('private-local-client.dpapi','private-local-pod.dpapi')){
        if(!(Test-Path -LiteralPath (Join-Path $taskSolidWork $taskSolidName) -PathType Leaf)){throw 'Retained identity missing; never reprovision here.'}
    }
    $taskSolidRaw=& wsl -d Ubuntu -u root -- podman inspect oasis-solid
    if($LASTEXITCODE -ne 0){throw 'Retained container unavailable; no creation/download.'}
    $taskSolidContainer=($taskSolidRaw|ConvertFrom-Json)[0]
    $taskSolidBinding=@($taskSolidContainer.HostConfig.PortBindings.'13000/tcp')
    $taskSolidMounts=@($taskSolidContainer.Mounts)
    $taskSolidInitRequested=$taskSolidContainer.Config.CreateCommand -contains '--init'
    if($RequireCleanShutdown -and !$taskSolidInitRequested){throw 'Approved init definition required for shutdown verification.'}
    $taskSolidAllowedState=$taskSolidContainer.State.Status -eq 'exited' -or ($RequireCleanShutdown -and $taskSolidContainer.State.Status -in @('created','configured'))
    if($taskSolidContainer.Image -ne '58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a' -or
        !$taskSolidAllowedState -or $taskSolidBinding.Count -ne 1 -or
        $taskSolidBinding[0].HostIp -ne '127.0.0.1' -or $taskSolidBinding[0].HostPort -ne '13000' -or
        $taskSolidMounts.Count -ne 1 -or $taskSolidMounts[0].Name -ne 'oasis-solid-data' -or $taskSolidMounts[0].Destination -ne '/data' -or
        ($taskSolidContainer.Config.Entrypoint -join ' ') -ne 'node bin/server.js'){
        throw 'Retained SOLID identity or lifecycle differs; inspect without replacement.'
    }
    $taskSolidProject=Join-Path $taskSolidRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.csproj'
    if($CqlWeb4 -or $CqlPoint -or $CoreStorage -or $ReconcileCore){
        if($CoreStorage){
        foreach($taskSolidMode in @('async','sync')){
            if(Test-Path -LiteralPath (Join-Path $taskSolidRoot "TestResults/ProviderConformanceCurrent/SOLIDOASIS/$taskSolidMode/report.json")){
                throw 'Preserve prior SOLID baseline evidence before a new dispatch.'
            }
        }
        }
        $taskSolidProvider=Join-Path $taskSolidRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.csproj'
        foreach($taskSolidBuild in @($taskSolidProvider,$taskSolidProject)){
            & dotnet build $taskSolidBuild --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'SOLID current-source build failed.'}
        }
    }
    $taskSolidDll=Join-Path (Split-Path $taskSolidProject) 'bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.dll'
    $taskSolidEvidence.frozenTestSha256=(Get-FileHash -LiteralPath $taskSolidDll).Hash
    $taskSolidTimer=[Diagnostics.Stopwatch]::StartNew()
    & wsl -d Ubuntu -u root -- podman start oasis-solid
    if($LASTEXITCODE -ne 0){throw 'Retained SOLID start failed.'}
    $taskSolidStarted=$true
    $taskSolidEvidence.initRequested=$taskSolidInitRequested
    $taskSolidEvidence.processes=@(& wsl -d Ubuntu -u root -- podman top oasis-solid pid comm)
    if($LASTEXITCODE -ne 0){throw 'Runtime process observation failed.'}
    $taskSolidObserved=& (Join-Path $PSScriptRoot 'Wait-ReusableSolidListener.ps1')
    $taskSolidEvidence.socketReadyMs=$taskSolidTimer.ElapsedMilliseconds
    foreach($taskSolidSample in $taskSolidObserved.Samples){$taskSolidSamples.Add($taskSolidSample)}
    $env:OASIS_SOLID_COLD_READONLY_WORK=$taskSolidWork
    $taskSolidFilter=if($CqlWeb4){'Category=SolidCqlWeb4Native'}elseif($CqlPoint){'Category=SolidCqlNative'}elseif($ReconcileCore){'Category=SolidCoreFixtureCleanup'}elseif($CoreStorage){'Category=SolidCoreStorageConformance'}else{'FullyQualifiedName~SolidColdStartReadOnlyTests.RetainedIssuerDiscoveryThenAuthenticatedAclRead'}
    & dotnet test $taskSolidProject --no-build --no-restore /m:2 /nr:false --filter $taskSolidFilter --logger "trx;LogFileName=$taskSolidTrxName" --results-directory $taskSolidResults -v:q
    $taskSolidEvidence.testExit=$LASTEXITCODE
    if($LASTEXITCODE -ne 0){throw 'Discovery/authenticated-read diagnostic failed; preserve original evidence.'}
    [xml]$taskSolidResult=Get-Content $taskSolidTrx -Raw
    $taskSolidCounts=$taskSolidResult.TestRun.ResultSummary.Counters
    $taskSolidExpected=if($CoreStorage){2}else{1}
    if([int]$taskSolidCounts.total -ne $taskSolidExpected -or [int]$taskSolidCounts.passed -ne $taskSolidExpected -or [int]$taskSolidCounts.notExecuted -ne 0){throw 'All selected native cases must pass with no skips.'}
}finally{
    $env:OASIS_SOLID_COLD_READONLY_WORK=$taskSolidPrior
    try{
        if($taskSolidStarted){
            $taskSolidStopTimer=[Diagnostics.Stopwatch]::StartNew()
            & wsl -d Ubuntu -u root -- podman stop --time 30 oasis-solid
            $taskSolidEvidence.stopExit=$LASTEXITCODE;$taskSolidEvidence.stopMs=$taskSolidStopTimer.ElapsedMilliseconds
            $taskSolidFinal=& wsl -d Ubuntu -u root -- podman inspect oasis-solid --format '{{.State.Status}} {{.State.ExitCode}} {{.State.OOMKilled}}'
            $taskSolidEvidence.finalState="$taskSolidFinal".Trim()
            $taskSolidEvidence.freeAfter=(Get-PSDrive C).Free
            $taskSolidBytes=[Text.Encoding]::UTF8.GetBytes(($taskSolidEvidence|ConvertTo-Json -Depth 6))
            $taskSolidStream=[IO.File]::Open($taskSolidReport,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$taskSolidStream.Write($taskSolidBytes);$taskSolidStream.Flush($true)}finally{$taskSolidStream.Dispose()}
            if($RequireCleanShutdown -and ($taskSolidEvidence.stopExit -ne 0 -or $taskSolidEvidence.finalState -notmatch '^exited (0|143) false$')){
                throw 'SOLID did not stop with normal exit/SIGTERM; preserved shutdown evidence is not a pass.'
            }
        }
    }finally{$taskSolidLease.Dispose()}
}
