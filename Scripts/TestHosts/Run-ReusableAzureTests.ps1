$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runtime='C:\Users\david\.cache\oasis-runtimes\azurite-3.37.0\node_modules\azurite\dist\src\blob\main.js'
if(!(Test-Path -LiteralPath $runtime -PathType Leaf)) {throw 'Retained Azurite3.37.0 runtime is missing. Do not silently reinstall.'}
if(Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue) {throw 'Owned Azure fixture port10000 is occupied.'}
if(Get-NetTCPConnection -LocalPort 10003 -State Listen -ErrorAction SilentlyContinue) {throw 'Reserved Azure outage port10003 is occupied.'}
$work=Join-Path $root 'TestResults/AzureCurrent'
New-Item -ItemType Directory -Force $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$process=$null
$previous=$env:OASIS_AZURE_NATIVE_TEST
try {
    Push-Location $root
    $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests.csproj'
    foreach($producer in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj','Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.csproj',$project)) {
        & dotnet build $producer --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE) {throw "Cached build failed: $producer"}
    }
    $node=(Get-Command node.exe).Source
    $process=Start-Process -FilePath $node -ArgumentList @($runtime,'--blobHost','127.0.0.1','--blobPort','10000','--inMemoryPersistence','--extentMemoryLimit','64','--disableTelemetry','--silent') -WorkingDirectory $work -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'azurite.out.log') -RedirectStandardError (Join-Path $work 'azurite.err.log')
    $ready=$false
    for($i=0;$i -lt 60;$i++) {
        if($process.HasExited) {throw 'Owned Azurite exited before readiness.'}
        if(Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue) {$ready=$true;break}
        Start-Sleep -Milliseconds 250
    }
    if(!$ready) {throw 'Owned Azurite readiness timed out.'}
    $env:OASIS_AZURE_NATIVE_TEST='1'
    $started=[DateTime]::UtcNow
    & dotnet test $project --no-build --no-restore --logger 'trx;LogFileName=azure-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE) {throw 'Azure native test failed.'}
    $reportFile=Get-Item -LiteralPath (Join-Path $work 'azure-native-current.trx') -ErrorAction Stop
    if($reportFile.LastWriteTimeUtc -lt $started){throw 'Azure native report is stale; no verification claim permitted.'}
    [xml]$report=Get-Content -LiteralPath $reportFile.FullName -Raw
    $counts=$report.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 72 -or [int]$counts.passed -ne 72 -or [int]$counts.notExecuted -ne 0) {throw 'Expected seventy-two native tests passed, no skips.'}
}
finally {
    $env:OASIS_AZURE_NATIVE_TEST=$previous
    if($null -ne $process -and !$process.HasExited) {
        Stop-Process -Id $process.Id -Force
        if(!$process.WaitForExit(10000)){throw 'Owned Azurite did not exit; do not restart the fixture.'}
    }
    Pop-Location
    $lease.Dispose()
}
