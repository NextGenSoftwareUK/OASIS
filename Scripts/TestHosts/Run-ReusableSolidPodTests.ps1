$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
# Reuse-only: never create/start a container here. Operator must start the pinned retained server.
$state=& wsl -d Ubuntu -u root -- podman inspect oasis-solid --format '{{.Image}} {{.State.Running}}'
if($LASTEXITCODE -ne 0 -or "$state".Trim() -ne '58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a true'){
    throw 'Pinned retained oasis-solid container is not running; no creation/start/download/reset attempted.'
}
$work=Join-Path $taskRoot 'TestResults/SolidCurrent'
New-Item -ItemType Directory -Path $work -Force | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
$journal=Join-Path $work 'native-pending.json'
if(Test-Path -LiteralPath $journal){throw 'Pending original Solid fixture requires reconciliation; never replay/reset.'}
foreach($pendingName in @('private-native-pending.json','private-acl-pending.json')){
    if(Test-Path -LiteralPath (Join-Path $work $pendingName)){throw 'Pending private Solid original requires reconciliation; never replay/reset.'}
}
foreach($requiredName in @('private-local-client.dpapi','private-local-pod.dpapi')){
    if(!(Test-Path -LiteralPath (Join-Path $work $requiredName))){throw 'Provision the retained synthetic identity once with Initialize-ReusableSolidIdentity.ps1 before running private authentication tests.'}
}
$keyPath=Join-Path $work 'storage-key.dpapi'
if(!(Test-Path -LiteralPath $keyPath -PathType Leaf)){
    throw 'Original retained Solid storage key required; restore the protected key, never generate a replacement over existing data.'
}
$project=Join-Path $taskRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.csproj'
if(!(Test-Path -LiteralPath (Join-Path (Split-Path $project) 'obj/project.assets.json'))){throw 'Restore this existing test project once using bounded workers; no per-run restore.'}
foreach($build in @((Join-Path $taskRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.csproj'),$project)){
    & dotnet build $build --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE -ne 0){throw 'Current Solid producer/test build failed.'}
}
New-Item -ItemType Directory -Path $work -Force | Out-Null
Add-Type -AssemblyName System.Security
$names=@('OASIS_SOLID_NATIVE_ROOT','OASIS_SOLID_NATIVE_KEY_PATH','OASIS_SOLID_NATIVE_JOURNAL');$prior=@{}
foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try{
    $env:OASIS_SOLID_NATIVE_ROOT='http://localhost:13000/native-fixture/'
    $env:OASIS_SOLID_NATIVE_KEY_PATH=$keyPath;$env:OASIS_SOLID_NATIVE_JOURNAL=$journal
    $started=[DateTime]::UtcNow
    & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~RetainedSolidPodTests' --logger 'trx;LogFileName=solid-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE -ne 0){throw 'Native Solid tests failed; preserve original pending journal and key.'}
    $report=Get-Item -LiteralPath (Join-Path $work 'solid-native-current.trx')
    [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
    $counts=$evidence.TestRun.ResultSummary.Counters
    if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 6 -or [int]$counts.passed -ne 6 -or [int]$counts.notExecuted -ne 0 -or (Test-Path -LiteralPath $journal) -or (Test-Path -LiteralPath (Join-Path $work 'private-native-pending.json'))){
        throw 'Six fresh successful native cases and cleared original fixture journals are required.'
    }
}finally{foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name],'Process')}}
# Container belongs to its operator, not this reuse-only runner: do not stop/remove/reset it.
} finally { $lease.Dispose() }
