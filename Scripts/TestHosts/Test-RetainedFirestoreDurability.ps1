#requires -Version 7.5
param([switch]$AllVersionExports,[switch]$CollectionIdentities,[switch]$IndependentExportVersions,[switch]$RunBatchRevisions,[switch]$RunEncryptedReplay,[switch]$RunPreparationClaims,[switch]$RunConditionalGraphs,[switch]$ResumeExportedProof,[switch]$ReconcileRetainedExport,[switch]$InspectRetainedExport,[switch]$VerifyRetainedCheckpoint,[switch]$RunHistoryTests,[switch]$RunRegressionTests,[switch]$ReconcileRoleFixture,[int]$ContinueOwnedEmulatorId)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/GoogleCurrent'
$historyReportName=if($AllVersionExports){'google-all-version-export-history-native.trx'}elseif($CollectionIdentities){'google-collection-id-history-native-current.trx'}elseif($IndependentExportVersions){'google-export-history-native-current.trx'}else{'google-history-native-current.trx'}
$regressionReportName=if($AllVersionExports){'google-all-version-export-regression-native.trx'}elseif($CollectionIdentities){'google-collection-id-regression-native-current.trx'}elseif($IndependentExportVersions){'google-export-regression-native-current.trx'}else{'google-native-current.trx'}
if($AllVersionExports -and (!$RunHistoryTests -or !$RunRegressionTests -or !$VerifyRetainedCheckpoint -or $CollectionIdentities -or $IndependentExportVersions -or $RunBatchRevisions -or $RunEncryptedReplay -or $RunPreparationClaims -or $RunConditionalGraphs -or $ResumeExportedProof -or $ReconcileRetainedExport -or $InspectRetainedExport -or $ReconcileRoleFixture -or $ContinueOwnedEmulatorId)){throw 'All-version export verification requires only completed checkpoint and both native suites.'}
if($CollectionIdentities -and (!$RunHistoryTests -or !$RunRegressionTests -or !$VerifyRetainedCheckpoint -or $IndependentExportVersions -or $RunBatchRevisions -or $RunEncryptedReplay -or $RunPreparationClaims -or $RunConditionalGraphs -or $ResumeExportedProof -or $ReconcileRetainedExport -or $InspectRetainedExport -or $ReconcileRoleFixture -or $ContinueOwnedEmulatorId)){throw 'Collection identity verification requires only completed checkpoint and both native suites.'}
if($IndependentExportVersions -and (!$RunHistoryTests -or !$RunRegressionTests -or !$VerifyRetainedCheckpoint)){throw 'Independent export verification requires the completed retained checkpoint and both native suites.'}
if($RunHistoryTests -and $RunRegressionTests -and !$ContinueOwnedEmulatorId){
    foreach($reportName in @($historyReportName,$regressionReportName)){
        if(Test-Path -LiteralPath (Join-Path $work $reportName)){throw 'Preserve and hash-verify previous Google history/regression reports before a fresh verification run.'}
    }
}
$jar='C:\Users\david\.cache\firebase\emulators\cloud-firestore-emulator-v1.22.0.jar'
$export=Join-Path $work 'retained-export'
$metadata=Join-Path $export 'firestore_export/firestore_export.overall_export_metadata'
$intent=Join-Path $work 'durability.pending.json'
$uri='http://127.0.0.1:18080/v1/projects/oasis-google-native/databases/(default)/documents/_oasis_history_durability/6cb0ff2b-85a0-49d4-ad12-264c97cb070a'
if(!(Test-Path -LiteralPath $jar -PathType Leaf)){throw 'Retained official runtime required; no download.'}
if($VerifyRetainedCheckpoint){
    if($ReconcileRetainedExport -or $ResumeExportedProof -or (Test-Path -LiteralPath $intent)){throw 'Completed checkpoint mode cannot replace pending-original recovery.'}
    $ReconcileRetainedExport=$true
}
if($RunHistoryTests -and !$VerifyRetainedCheckpoint -and !$ContinueOwnedEmulatorId){throw 'History suite requires explicit verified checkpoint mode or owned-runtime continuation.'}
if($RunRegressionTests -and !$RunHistoryTests){throw 'Regression suite requires the durable history runner.'}
if($RunConditionalGraphs -and (!$RunHistoryTests -or $ReconcileRoleFixture)){throw 'Atomic graph mode requires history-runner admission without reconciliation mode.'}
if($RunPreparationClaims -and (!$RunHistoryTests -or $ReconcileRoleFixture)){throw 'Preparation mode requires history-runner admission without reconciliation mode.'}
if($RunEncryptedReplay -and (!$RunHistoryTests -or $ReconcileRoleFixture)){throw 'Encrypted runtime mode requires history-runner admission without reconciliation mode.'}
if($RunBatchRevisions -and (!$RunHistoryTests -or $ReconcileRoleFixture)){throw 'Batch revision mode requires history-runner admission without reconciliation mode.'}
if($ReconcileRoleFixture -and (!$ContinueOwnedEmulatorId -or !$RunHistoryTests -or $RunRegressionTests)){throw 'Exact role-fixture reconciliation requires the same owned live history runtime only.'}
if($ResumeExportedProof -and $ReconcileRetainedExport){throw 'Choose one explicit recovery mode.'}
if($InspectRetainedExport -and !$ReconcileRetainedExport){throw 'Inspection requires explicit retained export mode.'}
if($ContinueOwnedEmulatorId){
    if(!$RunHistoryTests -or $ReconcileRetainedExport -or $ResumeExportedProof -or $VerifyRetainedCheckpoint){throw 'Owned-runtime continuation requires only native test mode.'}
    $native=Get-CimInstance Win32_Process -Filter "ProcessId=$ContinueOwnedEmulatorId"
    if(!$native -or $native.Name -ne 'java.exe' -or !$native.CommandLine.Contains($jar) -or !$native.CommandLine.Contains('--project_id oasis-google-native') -or !$native.CommandLine.Contains('--port 18080')){throw 'Owned retained emulator command identity differs.'}
    if((Get-NetTCPConnection -LocalPort 18080 -State Listen).OwningProcess -ne $ContinueOwnedEmulatorId){throw 'Owned retained emulator listener differs.'}
    if(!(Test-Path -LiteralPath (Join-Path $work 'history-durability-ready.json'))){throw 'No completed checkpoint admission.'}
}elseif($ReconcileRetainedExport){
    if(!(Test-Path -LiteralPath $metadata)){throw 'Acknowledged retained export required.'}
    if(!$VerifyRetainedCheckpoint){
        if(!(Test-Path -LiteralPath $intent)){throw 'Known original intent required.'}
        $pending=Get-Content -LiteralPath $intent -Raw|ConvertFrom-Json -DateKind String
        if($pending.uri -cne $uri -or $pending.proof -cne 'owned-retained-firestore-durability-v1'){throw 'Retained intent identity differs.'}
    }
    if(Get-ChildItem -LiteralPath $work -Filter 'history-native-*.pending.json'){throw 'Reconcile retained native history originals before any restart.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 18080,18081,18082,18083 -ErrorAction SilentlyContinue){throw 'Reserved emulator ports occupied.'}
}elseif($ResumeExportedProof){
    if(!(Test-Path -LiteralPath $intent) -or !(Test-Path -LiteralPath $metadata)){throw 'Known original intent and acknowledged export required.'}
    if((Get-NetTCPConnection -LocalPort 18080 -State Listen).OwningProcess -ne 57580){throw 'Known original emulator must still own the listener.'}
}else{
    if((Test-Path -LiteralPath $intent) -or (Test-Path -LiteralPath $export)){throw 'Existing durability evidence requires reconciliation; do not overwrite/recreate.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 18080,18081,18082,18083 -ErrorAction SilentlyContinue){throw 'Reserved emulator ports occupied.'}
}
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null;$safeToStop=$true
$oldEndpoint=$env:FIRESTORE_EMULATOR_HOST;$oldHistoryWork=$env:OASIS_FIRESTORE_HISTORY_WORK
function ConvertFrom-FirestoreProofJson([string]$Json){
    # PowerShell's default date coercion loses the exact native revision when formatted into a URI.
    $document=ConvertFrom-Json -InputObject $Json -DateKind String
    if($document.updateTime -isnot [string] -or $document.updateTime -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?Z$'){
        throw 'Firestore proof requires an exact native RFC3339 updateTime string; no date coercion or guessed revision.'
    }
    return $document
}
function Start-RetainedFirestore([bool]$Import){
    $arguments=@('-Xmx256m','-jar',$jar,'--host','127.0.0.1','--port','18080','--webchannel_port','18081','--websocket_port','18082','--project_id','oasis-google-native','--single_project_mode','--single_project_mode_error')
    if($Import){$arguments+=@('--import-data',$metadata)}
    $process=Start-Process java -ArgumentList $arguments -WorkingDirectory $work -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'durability-current.log') -RedirectStandardError (Join-Path $work 'durability-current.err')
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    do{
        if($process.HasExited){throw 'Owned emulator exited during readiness.'}
        $listener=Get-NetTCPConnection -LocalPort 18080 -State Listen -ErrorAction SilentlyContinue
        if($listener){if($listener.OwningProcess -ne $process.Id){throw 'Emulator listener ownership mismatch.'};return $process}
        Start-Sleep -Milliseconds 200
    }while([DateTime]::UtcNow -lt $deadline)
    throw 'Emulator readiness timeout; inspect owned process before restarting.'
}
try{
    $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests.csproj'
    if($RunHistoryTests){
        if($RunRegressionTests){
            foreach($dependency in @('LocalFileOASIS','SQLLiteDBOASIS')){
                $build=Join-Path $root "Providers/Storage/NextGenSoftware.OASIS.API.Providers.$dependency/NextGenSoftware.OASIS.API.Providers.$dependency.csproj"
                & dotnet build $build --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
                if($LASTEXITCODE){throw 'Cached regression dependency build failed.'}
            }
        }
        foreach($build in @((Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.csproj'),$project)){
            & dotnet build $build --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Cached native history build failed.'}
        }
    }
    if($RunHistoryTests){& (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput (Join-Path (Split-Path $project) 'bin/Debug/net10.0')}
    if($ContinueOwnedEmulatorId){
        $owned=Get-Process -Id $ContinueOwnedEmulatorId;$safeToStop=$false
        # The checkpoint was already reconciled on this exact process; never reimport/replay it.
        try{Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec 15|Out-Null;throw 'Reconciled checkpoint unexpectedly exists.'}
        catch{if([int]$_.Exception.Response.StatusCode -ne 404){throw}}
    }else{
    if($ReconcileRetainedExport){
        $owned=Start-RetainedFirestore $true
        $created=ConvertFrom-FirestoreProofJson (Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec 15).Content
        if($created.name -cne 'projects/oasis-google-native/databases/(default)/documents/_oasis_history_durability/6cb0ff2b-85a0-49d4-ad12-264c97cb070a' -or $created.fields.proof.stringValue -cne 'owned-retained-firestore-durability-v1'){throw 'Imported original proof identity differs.'}
    }elseif($ResumeExportedProof){
        $owned=Get-Process -Id 57580 -ErrorAction Stop
        if($owned.ProcessName -ne 'java'){throw 'Known emulator process differs.'}
        $created=ConvertFrom-FirestoreProofJson (Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec 15).Content
        if($created.fields.proof.stringValue -ne 'owned-retained-firestore-durability-v1'){throw 'Original proof record differs.'}
    }else{
    $owned=Start-RetainedFirestore $false
    $body=@{name='projects/oasis-google-native/databases/(default)/documents/_oasis_history_durability/6cb0ff2b-85a0-49d4-ad12-264c97cb070a';fields=@{proof=@{stringValue='owned-retained-firestore-durability-v1'}}}|ConvertTo-Json -Depth 5 -Compress
    $bytes=[Text.Encoding]::UTF8.GetBytes((@{uri=$uri;state='before-create-only-durability-write';proof='owned-retained-firestore-durability-v1'}|ConvertTo-Json -Compress))
    $file=[IO.File]::Open($intent,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$file.Write($bytes,0,$bytes.Length);$file.Flush($true)}finally{$file.Dispose()}
    $safeToStop=$false
    $created=Invoke-RestMethod -Uri ($uri+'?currentDocument.exists=false') -Method Patch -ContentType 'application/json' -Body $body -TimeoutSec 15
    if($created.fields.proof.stringValue -ne 'owned-retained-firestore-durability-v1'){throw 'Native create acknowledgement differs.'}
    $exportBody=@{database='projects/oasis-google-native/databases/(default)';export_directory=$export;export_name='firestore_export'}|ConvertTo-Json -Compress
    New-Item -ItemType Directory -Path $export | Out-Null
    Invoke-RestMethod -Uri 'http://127.0.0.1:18080/emulator/v1/projects/oasis-google-native:export' -Method Post -ContentType 'application/json' -Body $exportBody -TimeoutSec 30 | Out-Null
    }
    if(!(Test-Path -LiteralPath $metadata -PathType Leaf)){throw 'No native export metadata; keep original emulator alive.'}
    $safeToStop=$true
    if(!$ReconcileRetainedExport){
        Stop-Process -Id $owned.Id;$owned.WaitForExit(5000)|Out-Null
        $owned=Start-RetainedFirestore $true
    }
    $read=ConvertFrom-FirestoreProofJson (Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec 15).Content
    if($read.name -ne $created.name -or $read.fields.proof.stringValue -ne $created.fields.proof.stringValue){throw 'Restart/import did not preserve original native record.'}
    if([string]::IsNullOrWhiteSpace([string]$read.updateTime)){throw 'Imported REST record lacks an observed updateTime; retain export/intent and do not attempt conditional deletion.'}
    if($InspectRetainedExport){$read|ConvertTo-Json -Depth 8;return}
    # Retained emulator1.22.0 query transcoder turns Timestamp into zero (see diagnostic).
    # Use the canonical Commit protocol with exact observed revision in its typed JSON body,
    # matching the production SDK's commit boundary; never retry with an unguarded delete.
    $commitBody=@{writes=@(@{delete=$read.name;currentDocument=@{updateTime=$read.updateTime}})}|ConvertTo-Json -Depth 6 -Compress
    $ack=ConvertFrom-Json -InputObject (Invoke-WebRequest -Uri 'http://127.0.0.1:18080/v1/projects/oasis-google-native/databases/(default)/documents:commit' -Method Post -ContentType 'application/json' -Body $commitBody -TimeoutSec 15).Content -DateKind String
    if(@($ack.writeResults).Count -ne 1 -or [string]::IsNullOrWhiteSpace($ack.commitTime)){throw 'Conditional cleanup acknowledgement malformed; retain intent/export.'}
    $absent=$false
    try{Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 15 | Out-Null}catch{if([int]$_.Exception.Response.StatusCode -eq 404){$absent=$true}else{throw}}
    if(!$absent){throw 'Exact owned fixture deletion not independently confirmed.'}
    $result=@{status='PASS';originalName=$created.name;exportMetadata=$metadata;restartReadback=$true;conditionalCleanup=$true;retainedSnapshotContainsKnownProofFixture=$true}
    $result|ConvertTo-Json -Compress|Write-Output
    if(Test-Path -LiteralPath $intent){Remove-Item -LiteralPath $intent}
    }
    if($RunHistoryTests){
        $gate=Join-Path $work 'history-durability-ready.json'
        if(!$ContinueOwnedEmulatorId){
            $bytes=[Text.Encoding]::UTF8.GetBytes(($result|ConvertTo-Json -Compress))
            $file=[IO.File]::Open($gate,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$file.Write($bytes,0,$bytes.Length);$file.Flush($true)}finally{$file.Dispose()}
        }
        $env:FIRESTORE_EMULATOR_HOST='127.0.0.1:18080';$env:OASIS_FIRESTORE_HISTORY_WORK=$work
        $safeToStop=$false;$started=[DateTime]::UtcNow
        if($ReconcileRoleFixture){
            & dotnet test $project --no-build --no-restore --filter 'TestCategory=RoleFixtureReconciliation' --logger 'trx;LogFileName=google-role-reconciliation-current.trx' --results-directory $work -v:q
            if($LASTEXITCODE -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Original role fixture reconciliation incomplete; preserve same live runtime.'}
            return
        }
        if($RunBatchRevisions){
            & dotnet test $project --no-build --no-restore --filter 'TestCategory=BatchRevisionNative' --logger 'trx;LogFileName=google-batch-revision-native-current.trx' --results-directory $work /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Native batch revision tests failed; preserve original live emulator and fixtures.'}
            $report=Get-Item -LiteralPath (Join-Path $work 'google-batch-revision-native-current.trx')
            [xml]$trx=Get-Content -LiteralPath $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
            if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 4 -or [int]$counts.passed -ne 4 -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete4/4 native batch/import revision report required.'}
            if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Original batch revision fixtures remain unresolved.'}
            if(!$RunEncryptedReplay -and !$RunPreparationClaims -and !$RunRegressionTests -and !$RunConditionalGraphs){$safeToStop=$true;return}
        }
        if($RunEncryptedReplay){
            & dotnet test $project --no-build --no-restore --filter 'TestCategory=RuntimeEncryptedNative' --logger 'trx;LogFileName=google-encrypted-replay-native-current.trx' --results-directory $work /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Native encrypted runtime tests failed; preserve original live emulator and fixtures.'}
            $report=Get-Item -LiteralPath (Join-Path $work 'google-encrypted-replay-native-current.trx')
            [xml]$trx=Get-Content -LiteralPath $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
            if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 4 -or [int]$counts.passed -ne 4 -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete4/4 encrypted runtime report required.'}
            if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Original encrypted runtime fixtures remain unresolved.'}
            if(!$RunPreparationClaims -and !$RunRegressionTests -and !$RunConditionalGraphs){$safeToStop=$true;return}
        }
        if($RunPreparationClaims){
            & dotnet test $project --no-build --no-restore --filter 'TestCategory=PreparationNative' --logger 'trx;LogFileName=google-preparation-native-current.trx' --results-directory $work /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Native preparation tests failed; preserve original live emulator and fixtures.'}
            $report=Get-Item -LiteralPath (Join-Path $work 'google-preparation-native-current.trx')
            [xml]$trx=Get-Content -LiteralPath $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
            if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 7 -or [int]$counts.passed -ne 7 -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete7/7 native preparation report required.'}
            if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Original preparation fixtures remain unresolved.'}
            if(!$RunRegressionTests -and !$RunConditionalGraphs){$safeToStop=$true;return}
        }
        if($RunConditionalGraphs){
            & dotnet test $project --no-build --no-restore --filter 'TestCategory=AtomicGraphNative' --logger 'trx;LogFileName=google-conditional-native-current.trx' --results-directory $work /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Native atomic graph tests failed; preserve original live emulator and fixtures.'}
            $report=Get-Item -LiteralPath (Join-Path $work 'google-conditional-native-current.trx')
            [xml]$trx=Get-Content -LiteralPath $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
            if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 4 -or [int]$counts.passed -ne 4 -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete4/4 native atomic graph report required.'}
            if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Original atomic graph fixtures remain unresolved.'}
            if(!$RunRegressionTests){$safeToStop=$true;return}
        }
        & dotnet test $project --no-build --no-restore --filter 'TestCategory=HistoryNative' --logger "trx;LogFileName=$historyReportName" --results-directory $work -v:q
        if($LASTEXITCODE){throw 'Native history suite failed; retain live emulator for original reconciliation.'}
        $report=Get-Item -LiteralPath (Join-Path $work $historyReportName)
        if($report.LastWriteTimeUtc -lt $started){throw 'Stale native report.'}
        [xml]$trx=Get-Content -LiteralPath $report.FullName -Raw
        $counts=$trx.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 83 -or [int]$counts.passed -ne 83 -or [int]$counts.notExecuted -ne 0){throw 'Expected exactly83native history/lifecycle/search/role/identity/count/export cases, no skips.'}
        if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Native history originals remain; do not stop.'}
        if($RunRegressionTests){
            $started=[DateTime]::UtcNow
            & dotnet test $project --no-build --no-restore --filter 'TestCategory!=HistoryNative&TestCategory!=RoleFixtureReconciliation&TestCategory!=IdentityTransactionInspection&TestCategory!=AtomicGraphNative&TestCategory!=PreparationNative&TestCategory!=RuntimeEncryptedNative&TestCategory!=BatchRevisionNative' --logger "trx;LogFileName=$regressionReportName" --results-directory $work -v:q
            if($LASTEXITCODE){throw 'Native regression failed; retain original live emulator for inspection.'}
            $report=Get-Item -LiteralPath (Join-Path $work $regressionReportName)
            if($report.LastWriteTimeUtc -lt $started){throw 'Stale native regression report.'}
            [xml]$trx=Get-Content -LiteralPath $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
            if([int]$counts.total -ne 79 -or [int]$counts.passed -ne 79 -or [int]$counts.notExecuted -ne 0){throw 'Expected all79native regression cases, no skips.'}
        }
        if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Unresolved native intents remain after regression; preserve live emulator.'}
        $safeToStop=$true
    }
}finally{
    $env:FIRESTORE_EMULATOR_HOST=$oldEndpoint;$env:OASIS_FIRESTORE_HISTORY_WORK=$oldHistoryWork
    if($owned -and !$owned.HasExited -and $safeToStop){Stop-Process -Id $owned.Id;$owned.WaitForExit(5000)|Out-Null}
    elseif($owned -and !$owned.HasExited){Write-Warning "Original uncertain outcome retained in live owned emulator PID $($owned.Id); do not restart/reset."}
    $lease.Dispose()
}
