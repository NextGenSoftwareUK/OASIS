$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$jar='C:\Users\david\.cache\firebase\emulators\cloud-firestore-emulator-v1.22.0.jar'
if(!(Test-Path -LiteralPath $jar -PathType Leaf)) {throw 'Retained official Firestore1.22.0 runtime missing; do not silently reinstall.'}
foreach($port in @(18080,18081,18082,18083)) {if(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {throw "Owned Firestore port$port occupied."}}
$work=Join-Path $root 'TestResults/GoogleCurrent'
New-Item -ItemType Directory -Force $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$process=$null;$previous=$env:FIRESTORE_EMULATOR_HOST
try {
    Push-Location $root
    $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests.csproj'
    foreach($producer in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj','Providers/Cloud/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.csproj',$project)) {
        & dotnet build $producer --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE) {throw "Cached build failed: $producer"}
    }
    $process=Start-Process java -ArgumentList @('-Xmx256m','-jar',$jar,'--host','127.0.0.1','--port','18080','--webchannel_port','18081','--websocket_port','18082','--project_id','oasis-google-native','--single_project_mode','--single_project_mode_error') -WorkingDirectory $work -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'firestore.out.log') -RedirectStandardError (Join-Path $work 'firestore.err.log')
    $ready=$false
    for($i=0;$i -lt 60;$i++) {
        if($process.HasExited) {throw 'Owned Firestore exited before readiness.'}
        if(Get-NetTCPConnection -LocalPort 18080 -State Listen -ErrorAction SilentlyContinue) {$ready=$true;break}
        Start-Sleep -Milliseconds 250
    }
    if(!$ready) {throw 'Owned Firestore readiness timed out.'}
    $env:FIRESTORE_EMULATOR_HOST='127.0.0.1:18080'
    $started=[DateTime]::UtcNow
    & dotnet test $project --no-build --no-restore --logger 'trx;LogFileName=google-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE) {throw 'Google native test failed.'}
    $reportFile=Get-Item -LiteralPath (Join-Path $work 'google-native-current.trx') -ErrorAction Stop
    if($reportFile.LastWriteTimeUtc -lt $started){throw 'Google native report is stale; no verification claim permitted.'}
    [xml]$report=Get-Content -LiteralPath $reportFile.FullName -Raw
    $counts=$report.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 33 -or [int]$counts.passed -ne 33 -or [int]$counts.notExecuted -ne 0) {throw 'Expected all thirty-three native tests passed, no skips.'}
}
finally {
    $env:FIRESTORE_EMULATOR_HOST=$previous
    if($null -ne $process -and !$process.HasExited) {Stop-Process -Id $process.Id -Force}
    Pop-Location;$lease.Dispose()
}
