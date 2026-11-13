#Requires -Version 7.0
[CmdletBinding()]
param([switch]$Native)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = 'Providers/AI/TestProjects/NextGenSoftware.OASIS.API.Providers.BittensorOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.BittensorOASIS.UnitTests.csproj'
$results = Join-Path $root 'TestResults/BittensorPublicSourceCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
Push-Location $root
try {
    & (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-bittensor-cql' -Operation {
        if ($Native) {
            $sourceState = Join-Path $root 'LocalState/COSMIC/HostedBittensorPublicSourceCurrent'
            $oldSource = $env:COSMIC_BITTENSOR_PUBLIC_SOURCE
            $oldIntent = $env:COSMIC_BITTENSOR_CQL_INTENT
            try {
                $env:COSMIC_BITTENSOR_PUBLIC_SOURCE = Join-Path $sourceState 'source.json'
                $env:COSMIC_BITTENSOR_CQL_INTENT = Join-Path $sourceState 'native-cql.intent.json'
                dotnet test $project --no-build --no-restore --filter 'TestCategory=NativeBittensorPublic' /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $results --logger 'trx;LogFileName=native-cql.trx' *> (Join-Path $results 'native-cql.log')
                if ($LASTEXITCODE -ne 0) { throw 'Bittensor native CQL gate failed; preserve intent and inspect evidence.' }
            }
            finally {
                $env:COSMIC_BITTENSOR_PUBLIC_SOURCE = $oldSource
                $env:COSMIC_BITTENSOR_CQL_INTENT = $oldIntent
            }
            return
        }
        # A missing cached dependency fails visibly rather than downloading a new environment.
        dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $results 'restore.log')
        if ($LASTEXITCODE -ne 0) { throw 'Bittensor cached restore failed; inspect restore.log.' }
        dotnet test $project --no-restore --filter 'TestCategory!=NativeBittensorPublic' /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $results --logger 'trx;LogFileName=adapter.trx' *> (Join-Path $results 'adapter.log')
        if ($LASTEXITCODE -ne 0) { throw 'Bittensor adapter tests failed; inspect adapter.log.' }
    }
}
finally { Pop-Location }
