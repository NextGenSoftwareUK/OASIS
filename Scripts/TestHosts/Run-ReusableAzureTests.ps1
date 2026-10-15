$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runtime='C:\Users\david\.cache\oasis-runtimes\azurite-3.37.0\node_modules\azurite\dist\src\blob\main.js'
if(!(Test-Path -LiteralPath $runtime -PathType Leaf)) {throw 'Retained Azurite3.37.0 runtime is missing. Do not silently reinstall.'}
if(Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue) {throw 'Owned Azure fixture port10000 is occupied.'}
$work=Join-Path $root 'TestResults/AzureCurrent'
New-Item -ItemType Directory -Force $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$process=$null
$previous=$env:OASIS_AZURE_NATIVE_TEST
try {
    Push-Location $root
    $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests.csproj'
    foreach($producer in @('Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.csproj',$project)) {
        & dotnet build $producer --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
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
    & dotnet test $project --no-build --no-restore --filter FullyQualifiedName~AzureNativeStorageTests --logger 'trx;LogFileName=azure-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE) {throw 'Azure native test failed.'}
    [xml]$report=Get-Content (Join-Path $work 'azure-native-current.trx') -Raw
    if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1') {throw 'Expected exactly one native test passed.'}
}
finally {
    $env:OASIS_AZURE_NATIVE_TEST=$previous
    if($null -ne $process -and !$process.HasExited) {Stop-Process -Id $process.Id -Force}
    Pop-Location
    $lease.Dispose()
}
