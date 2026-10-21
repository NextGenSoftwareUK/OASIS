#Requires -Version 7.0
param([switch]$NoBuild,[switch]$Serve)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project=Join-Path $taskRoot 'Examples/CosmicCqlDemo/CosmicCqlDemo.csproj'
$provider=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC CQL coffee demo' -Operation {
    if(!$NoBuild){
        & dotnet restore $project --no-dependencies /m:2 /nr:false /p:UseSharedCompilation=false
        if($LASTEXITCODE){throw 'Demo dependency restore failed.'}
        & dotnet build $provider --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
        if($LASTEXITCODE){throw 'Existing SQLite producer build failed.'}
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
        if($LASTEXITCODE){throw 'Demo build failed.'}
    }
    $output=Join-Path $taskRoot 'Examples/CosmicCqlDemo/bin/Debug/net10.0'
    $producers=@(
        @('OASIS Architecture/NextGenSoftware.OASIS.API.Core','NextGenSoftware.OASIS.API.Core.dll'),
        @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage','NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll'),
        @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS','NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.dll')
    )
    $identities=foreach($entry in $producers){
        $producer=Join-Path $taskRoot ($entry[0]+'/bin/Debug/net10.0/'+$entry[1])
        $consumer=Join-Path $output $entry[1]
        $expected=(Get-FileHash -LiteralPath $producer -Algorithm SHA256).Hash
        if((Get-FileHash -LiteralPath $consumer -Algorithm SHA256).Hash -ne $expected){throw ('Stale demo runtime copy: '+$entry[1])}
        @{name=$entry[1];producer=$producer;consumer=$consumer;sha256=$expected}
    }
    if($Serve){ & dotnet run --project $project --no-build --no-restore -- --serve }
    else { & dotnet run --project $project --no-build --no-restore }
    if($LASTEXITCODE){throw 'CQL demo failed. Preserve any existing owned fixture for review.'}
    $work=Join-Path $taskRoot 'TestResults/CosmicCqlDemo'
    $resultName=if($Serve){'api-demo-result.json'}else{'demo-result.json'}
    $artifactName=if($Serve){'api-demo-artifacts.json'}else{'demo-artifacts.json'}
    $report=Get-Content -LiteralPath (Join-Path $work $resultName) -Raw | ConvertFrom-Json
    if($report.Status -ne 'passed' -or $report.SeededRows -ne 5 -or $report.Steps.Count -ne 3 -or !$report.NoOpSkipped){throw 'Demo proof is incomplete.'}
    foreach($name in @('demo.db','demo.db-wal','demo.db-shm','demo-manifest.json')){
        if(Test-Path -LiteralPath (Join-Path $work $name)){throw 'Successful owned demo teardown is incomplete.'}
    }
    @{assemblies=$identities;reportSha256=(Get-FileHash -LiteralPath (Join-Path $work $resultName)).Hash;fixtureRemoved=$true} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work $artifactName) -Encoding utf8
}
