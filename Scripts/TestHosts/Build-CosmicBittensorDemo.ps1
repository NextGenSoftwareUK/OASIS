#Requires -Version 7.0
param([ValidateSet('core','api','demo')][string]$StartAt = 'core')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
$results = Join-Path $root 'TestResults/BittensorPublicSourceCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$core = 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/NextGenSoftware.OASIS.API.Core.UnitTests.csproj'
$api = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
$demo = 'Examples/CosmicCqlDemo/CosmicCqlDemo.csproj'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-bittensor-integration' -Operation {
    $startBuild = $false
    foreach ($entry in @(@('core',$core),@('api',$api),@('demo',$demo))) {
        $label = $entry[0]; $project = $entry[1]
        if ($label -eq $StartAt) { $startBuild = $true }
        if (!$startBuild) { continue }
        dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $results "$label-integration-restore.log")
        if ($LASTEXITCODE) { throw "$label cached restore failed." }
        dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly *> (Join-Path $results "$label-integration-build.log")
        if ($LASTEXITCODE) { throw "$label integration build failed." }
    }
    dotnet test $core --no-build --no-restore --filter 'FullyQualifiedName~StorageQueryTariffBittensorTests' --results-directory $results --logger 'trx;LogFileName=core-admission.trx' *> (Join-Path $results 'core-admission.log')
    if ($LASTEXITCODE) { throw 'Core Bittensor read admission tests failed.' }
    dotnet test $api --no-build --no-restore --filter 'FullyQualifiedName~CqlBittensorHostedAdmissionTests' --results-directory $results --logger 'trx;LogFileName=http-admission.trx' *> (Join-Path $results 'http-admission.log')
    if ($LASTEXITCODE) { throw 'HTTP Bittensor admission tests failed.' }
}
