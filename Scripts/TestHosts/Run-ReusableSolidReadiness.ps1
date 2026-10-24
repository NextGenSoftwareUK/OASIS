#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$taskSolidRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskSolidWork=Join-Path $taskSolidRoot 'TestResults/SolidCurrent'
if(!(Test-Path -LiteralPath $taskSolidWork -PathType Container)){throw 'Retained SOLID work directory required.'}
$taskSolidLease=[IO.File]::Open((Join-Path $taskSolidWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$taskSolidStarted=$false;$taskSolidPrior=$env:OASIS_SOLID_COLD_READONLY_WORK
$taskSolidReport=Join-Path $taskSolidWork 'solid-readiness-current.json'
$taskSolidTrx=Join-Path $taskSolidWork 'solid-ready-read-current.trx'
$taskSolidSamples=[Collections.Generic.List[object]]::new()
$taskSolidEvidence=[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('O');scope='frozen-output readiness and one authenticated read; not current-source acceptance';freeBefore=(Get-PSDrive C).Free;socketReadyMs=$null;testExit=$null;stopExit=$null;finalState=$null;samples=$taskSolidSamples}
try{
    if((Test-Path $taskSolidReport) -or (Test-Path $taskSolidTrx)){throw 'Preserve original readiness evidence before another run.'}
    if(Get-ChildItem -LiteralPath $taskSolidWork -Filter '*pending*.json'){throw 'Original pending SOLID fixtures require reconciliation.'}
    foreach($taskSolidName in @('private-local-client.dpapi','private-local-pod.dpapi')){
        if(!(Test-Path -LiteralPath (Join-Path $taskSolidWork $taskSolidName) -PathType Leaf)){throw 'Retained identity missing; never reprovision here.'}
    }
    $taskSolidRaw=& wsl -d Ubuntu -u root -- podman inspect oasis-solid
    if($LASTEXITCODE -ne 0){throw 'Retained container unavailable; no creation/download.'}
    $taskSolidContainer=($taskSolidRaw|ConvertFrom-Json)[0]
    $taskSolidBinding=@($taskSolidContainer.HostConfig.PortBindings.'13000/tcp')
    $taskSolidMounts=@($taskSolidContainer.Mounts)
    if($taskSolidContainer.Image -ne '58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a' -or
        $taskSolidContainer.State.Status -ne 'exited' -or $taskSolidBinding.Count -ne 1 -or
        $taskSolidBinding[0].HostIp -ne '127.0.0.1' -or $taskSolidBinding[0].HostPort -ne '13000' -or
        $taskSolidMounts.Count -ne 1 -or $taskSolidMounts[0].Name -ne 'oasis-solid-data' -or $taskSolidMounts[0].Destination -ne '/data' -or
        ($taskSolidContainer.Config.Entrypoint -join ' ') -ne 'node bin/server.js'){
        throw 'Retained SOLID identity or lifecycle differs; inspect without replacement.'
    }
    $taskSolidProject=Join-Path $taskSolidRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.csproj'
    $taskSolidDll=Join-Path (Split-Path $taskSolidProject) 'bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.dll'
    $taskSolidEvidence.frozenTestSha256=(Get-FileHash -LiteralPath $taskSolidDll).Hash
    $taskSolidTimer=[Diagnostics.Stopwatch]::StartNew()
    & wsl -d Ubuntu -u root -- podman start oasis-solid
    if($LASTEXITCODE -ne 0){throw 'Retained SOLID start failed.'}
    $taskSolidStarted=$true
    $taskSolidObserved=& (Join-Path $PSScriptRoot 'Wait-ReusableSolidListener.ps1')
    $taskSolidEvidence.socketReadyMs=$taskSolidTimer.ElapsedMilliseconds
    foreach($taskSolidSample in $taskSolidObserved.Samples){$taskSolidSamples.Add($taskSolidSample)}
    $env:OASIS_SOLID_COLD_READONLY_WORK=$taskSolidWork
    & dotnet test $taskSolidProject --no-build --no-restore /m:2 /nr:false --filter 'FullyQualifiedName~SolidColdStartReadOnlyTests.RetainedIssuerDiscoveryThenAuthenticatedAclRead' --logger 'trx;LogFileName=solid-ready-read-current.trx' --results-directory $taskSolidWork -v:q
    $taskSolidEvidence.testExit=$LASTEXITCODE
    if($LASTEXITCODE -ne 0){throw 'Discovery/authenticated-read diagnostic failed; preserve original evidence.'}
    [xml]$taskSolidResult=Get-Content $taskSolidTrx -Raw
    $taskSolidCounts=$taskSolidResult.TestRun.ResultSummary.Counters
    if([int]$taskSolidCounts.total -ne 1 -or [int]$taskSolidCounts.passed -ne 1 -or [int]$taskSolidCounts.notExecuted -ne 0){throw 'Exactly one successful diagnostic required.'}
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
        }
    }finally{$taskSolidLease.Dispose()}
}
