#Requires -Version 7.0
# Controlled tests only: this runner never contacts a public source or replays acceptance.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.ZcashOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.ZcashOASIS.UnitTests.csproj'
$results = Join-Path $root 'TestResults/ZcashPublicSourceCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
Push-Location $root
try {
    & (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-zcash-controls' -Operation {
        dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $results 'restore.log')
        if ($LASTEXITCODE) { throw 'Zcash cached restore failed; inspect restore.log.' }
        dotnet test $project --no-restore --filter 'FullyQualifiedName~ZcashPublicBlockMetadataTests' /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $results --logger 'trx;LogFileName=controlled.trx' *> (Join-Path $results 'controlled.log')
        if ($LASTEXITCODE) { throw 'Zcash controlled tests failed; inspect controlled.log.' }
    }
}
finally { Pop-Location }
