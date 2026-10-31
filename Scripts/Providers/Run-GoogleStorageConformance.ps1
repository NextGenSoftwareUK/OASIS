#Requires -Version 7.5
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/GoogleCurrent'
$jar=Join-Path $env:USERPROFILE '.cache/firebase/emulators/cloud-firestore-emulator-v1.22.0.jar'
if(!(Test-Path -LiteralPath $jar) -or (Get-FileHash -LiteralPath $jar).Hash -ne '9B6498B7F62714D67F48F59B3818883CD682DBCD46B9F59511DE81C97BB5166C'){throw 'Retained pinned Firestore1.22.0 runtime required; no download.'}
if((Test-Path -LiteralPath (Join-Path $work 'durability.pending.json')) -or (Test-Path -LiteralPath (Join-Path $work 'retained-export'))){throw 'Legacy durability state requires its existing owner; canonical scope does not replace or reset it.'}
$ownerPath=Join-Path $work 'core-runtime-owner.json'
if(Test-Path -LiteralPath $ownerPath){throw 'Review the original canonical runtime owner before another run; never replace an uncertain live emulator.'}
foreach($mode in @('async','sync')){
    $report=Join-Path $taskRoot ('TestResults/ProviderConformanceCurrent/GoogleCloudOASIS/'+$mode+'/report.json')
    if((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next'))){throw 'Preserve original Google canonical fixture/report before another run.'}
}
& (Join-Path $taskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Google Firestore canonical native breadth' -Operation {
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $environmentLease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $owned=$null;$safeToStop=$true;$prior=$env:FIRESTORE_EMULATOR_HOST;$priorScope=$env:OASIS_GOOGLE_CORE_SCOPE
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 18080,18081,18082,18083 -ErrorAction SilentlyContinue){throw 'Existing Firestore ports are in use; no duplicate runtime or process stop.'}
        $projects=@(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.csproj',
            'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests.csproj')
        foreach($relative in $projects){
            $project=Join-Path $taskRoot $relative
            & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Cached Google metadata unavailable; no automatic install.'}
            & dotnet build $project --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Google native producer/consumer build failed.'}
        }
        $googleOutput=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
        . (Join-Path $taskRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $taskRoot -ConsumerOutput $googleOutput -VerifySourceDocuments
        foreach($name in @('NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS','NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests')){
            Write-Output ($name+': sources='+(Assert-SourceDocuments (Join-Path $googleOutput ($name+'.dll')) $taskRoot))
        }
        $owned=Start-Process java -ArgumentList @('-Xmx256m','-jar',$jar,'--host','127.0.0.1','--port','18080','--webchannel_port','18081','--websocket_port','18082','--project_id','oasis-google-core-conformance','--single_project_mode','--single_project_mode_error') -WorkingDirectory $work -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'core-native-current.log') -RedirectStandardError (Join-Path $work 'core-native-current.err')
        [ordered]@{ProcessId=$owned.Id;Jar=$jar;Project='oasis-google-core-conformance';State='starting-synthetic-scope';LegacyCheckpointRestored=$false} | ConvertTo-Json | Set-Content -LiteralPath $ownerPath
        $deadline=[DateTime]::UtcNow.AddSeconds(20);$ready=$false
        do{
            if($owned.HasExited){throw 'Owned emulator exited during startup; inspect its bounded logs.'}
            $listener=Get-NetTCPConnection -State Listen -LocalPort 18080 -ErrorAction SilentlyContinue
            if($listener){
                if($listener.OwningProcess -ne $owned.Id){throw 'Emulator listener ownership differs.'}
                $ready=$true;break
            }
            Start-Sleep -Milliseconds 200
        }while([DateTime]::UtcNow -lt $deadline)
        if(!$ready){throw 'Owned Firestore startup timed out.'}
        $env:FIRESTORE_EMULATOR_HOST='127.0.0.1:18080';$env:OASIS_GOOGLE_CORE_SCOPE='oasis-google-core-conformance'
        $safeToStop=$false # A failed mutation must keep its volatile original available for inspection.
        $results=Join-Path $taskRoot 'TestResults/ProviderConformanceCurrent'
        & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'TestCategory=GoogleCoreStorageConformance' --logger 'trx;LogFileName=google-core-current.trx' --results-directory $results -v:q
        $testExit=$LASTEXITCODE
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers GoogleCloudOASIS
        if($LASTEXITCODE){throw 'Google report registration failed; original runtime remains live.'}
        & python -B (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'Google matrix update failed; original runtime remains live.'}
        if($testExit){throw 'Google native conformance failed; preserve live emulator, original IDs and reports, no reset/replay.'}
        [xml]$trx=Get-Content (Join-Path $results 'google-core-current.trx')
        if($trx.TestRun.ResultSummary.Counters.total -ne 2 -or $trx.TestRun.ResultSummary.Counters.passed -ne 2 -or $trx.TestRun.ResultSummary.Counters.notExecuted -ne 0){throw 'Both actual native Google modes required, no skips.'}
        $safeToStop=$true
    }finally{
        $env:FIRESTORE_EMULATOR_HOST=$prior;$env:OASIS_GOOGLE_CORE_SCOPE=$priorScope
        try{
        if($owned -and !$owned.HasExited -and $safeToStop){Stop-Process -Id $owned.Id;if(!$owned.WaitForExit(10000)){throw 'Owned Firestore still live; do not restart it.'}}
        if($owned){[ordered]@{ProcessId=$owned.Id;Jar=$jar;Project='oasis-google-core-conformance';State=if($owned.HasExited){'stopped-after-bounded-run'}else{'preserved-live-unknown-outcome'};LegacyCheckpointRestored=$false} | ConvertTo-Json | Set-Content -LiteralPath $ownerPath}
        }finally{$environmentLease.Dispose()}
    }
}
