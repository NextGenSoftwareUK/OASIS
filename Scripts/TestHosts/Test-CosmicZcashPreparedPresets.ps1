#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
$results = Join-Path $root 'TestResults/ZcashPublicSourceCurrent'
$project = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
# The common planner accepts only query inputs. Preserve the publisher's full
# audit review and export its two public input fields without changing either.
$review = Get-Content -LiteralPath (Join-Path $results 'prepared-review.json') -Raw | ConvertFrom-Json
$inputs = @{ profiles=$review.profiles; examples=$review.examples } | ConvertTo-Json -Depth 64 -Compress
$inputPath = Join-Path $results 'prepared-query-inputs.json'
if (Test-Path -LiteralPath $inputPath) {
    if ((Get-Content -LiteralPath $inputPath -Raw) -cne $inputs) { throw 'Retained prepared query inputs differ.' }
}
else {
    $stream = [IO.File]::Open($inputPath, [IO.FileMode]::CreateNew)
    try { $stream.Write([Text.Encoding]::UTF8.GetBytes($inputs)); $stream.Flush($true) }
    finally { $stream.Dispose() }
}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-zcash-prepared' -Operation {
    dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q -clp:ErrorsOnly *> (Join-Path $results 'prepared-build.log')
    if ($LASTEXITCODE) { throw 'Prepared-preset consumer build failed.' }
    $previous = $env:COSMIC_CQL_PUBLIC_REVIEW
    try {
        $env:COSMIC_CQL_PUBLIC_REVIEW = $inputPath
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ActualZcashCandidatePlansFourPresetsWithShippingCapabilities' --results-directory $results --logger 'trx;LogFileName=prepared-presets.trx' *> (Join-Path $results 'prepared-presets.log')
        if ($LASTEXITCODE) { throw 'Prepared presets failed actual shipping compiler/capability planning.' }
    }
    finally { $env:COSMIC_CQL_PUBLIC_REVIEW = $previous }
}
