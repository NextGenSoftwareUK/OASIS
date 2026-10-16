#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/Neo4jCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$started=$false
$prior=$env:OASIS_OWNED_NEO4J_URI
$priorWork=$env:OASIS_OWNED_NEO4J_WORK
$freeBefore=(Get-PSDrive C).Free
try {
    $state=(& wsl -d Ubuntu -u root -- podman inspect oasis-neo4j --format '{{.State.Status}} {{.Image}}') -join ''
    if($LASTEXITCODE -or $state -ne 'created 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd' -and $state -ne 'exited 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'){
        throw 'Exact retained stopped Neo4j container required; no download/replacement attempted.'
    }
    if(Get-NetTCPConnection -State Listen -LocalPort 17687 -ErrorAction SilentlyContinue){throw 'Owned Bolt port is already occupied.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 17688 -ErrorAction SilentlyContinue){throw 'Owned simulated outage port is occupied.'}
    $provider=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.csproj'
    $project=Join-Path $taskRoot 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'
    $sqlite=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj'
    foreach($path in $sqlite,$provider,$project){
        & dotnet build $path --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Neo4j producer/consumer build failed.'}
    }
    & wsl -d Ubuntu -u root -- podman start oasis-neo4j
    if($LASTEXITCODE){throw 'Retained Neo4j service failed to start.'}
    $started=$true
    $deadline=[DateTime]::UtcNow.AddSeconds(60)
    do {
        & wsl -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only 'RETURN 1;' 2>$null | Out-Null
        $ready=$LASTEXITCODE -eq 0
        if(!$ready){Start-Sleep -Milliseconds 500}
    } while(!$ready -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready){throw 'Owned Neo4j did not become ready; no test submitted.'}
    $env:OASIS_OWNED_NEO4J_URI='bolt://127.0.0.1:17687'
    $env:OASIS_OWNED_NEO4J_WORK=$work
    $testStarted=[DateTime]::UtcNow
    & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter FullyQualifiedName~Neo4jNativeAvatarTests --logger 'trx;LogFileName=neo4j-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Native Neo4j test failed; preserve database for inspection.'}
    $report=Get-Item -LiteralPath (Join-Path $work 'neo4j-native-current.trx')
    if($report.LastWriteTimeUtc -lt $testStarted){throw 'Native Neo4j report is stale.'}
    [xml]$evidence=Get-Content -LiteralPath $report.FullName
    $counts=$evidence.SelectSingleNode("//*[local-name()='Counters']")
    if(!$counts -or [int]$counts.total -ne 62 -or [int]$counts.passed -ne 62 -or [int]$counts.notExecuted -ne 0){throw 'Exactly sixty-two fresh native cases including credential selectors and V2 nearby routing, no skips, required.'}
    Write-Output "Free bytes before=$freeBefore after=$((Get-PSDrive C).Free)"
} finally {
    try {
        if($started){
            & wsl -d Ubuntu -u root -- podman stop --time 20 oasis-neo4j
            if($LASTEXITCODE){throw 'Owned Neo4j shutdown failed; do not recreate or delete its data.'}
        }
    } finally {
        [Environment]::SetEnvironmentVariable('OASIS_OWNED_NEO4J_URI',$prior)
        [Environment]::SetEnvironmentVariable('OASIS_OWNED_NEO4J_WORK',$priorWork)
        $lease.Dispose()
    }
}
