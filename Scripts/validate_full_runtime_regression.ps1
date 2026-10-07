[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$ArtifactsDirectory = 'artifacts/full-runtime-regression'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactsPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactsDirectory))
$repoPrefix = [IO.Path]::GetFullPath($repoRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (!$artifactsPath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Regression artifacts must remain inside the OASIS repository: '$artifactsPath'."
}

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Assert-TestReport {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][int]$MinimumExecuted
    )
    $path = Join-Path $artifactsPath $Name
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required regression report is missing: '$path'."
    }
    [xml]$report = Get-Content -LiteralPath $path -Raw
    $counters = @($report.TestRun.ResultSummary.Counters)[0]
    $executed = [int]$counters.executed
    $failed = [int]$counters.failed + [int]$counters.error + [int]$counters.timeout + [int]$counters.aborted
    if ($executed -lt $MinimumExecuted -or $failed -ne 0) {
        throw "Regression report '$Name' executed $executed tests with $failed failures; required at least $MinimumExecuted and zero failures."
    }
}

$suites = @(
    @{ Project = 'ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.csproj'; Report = 'onode-core.trx'; Minimum = 163 },
    @{ Project = 'ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests/NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests.csproj'; Report = 'onode-core-integration.trx'; Minimum = 42 },
    @{ Project = 'ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'; Report = 'onode-webapi.trx'; Minimum = 83 },
    @{ Project = 'STAR ODK/NextGenSoftware.OASIS.STAR.STARDNA.UnitTests/NextGenSoftware.OASIS.STAR.STARDNA.UnitTests.csproj'; Report = 'star-dna.trx'; Minimum = 1 }
)

foreach ($suite in $suites) {
    $reportPath = Join-Path $artifactsPath $suite.Report
    Remove-Item -LiteralPath $reportPath -Force -ErrorAction SilentlyContinue
    Invoke-DotNet @('test', (Join-Path $repoRoot $suite.Project), '--configuration', $Configuration,
        '--logger', "trx;LogFileName=$($suite.Report)", '--results-directory', $artifactsPath, '--nologo')
    Assert-TestReport -Name $suite.Report -MinimumExecuted $suite.Minimum
}

Invoke-DotNet @('build', (Join-Path $repoRoot 'Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.csproj'),
    '--configuration', $Configuration, '--nologo')
Invoke-DotNet @('build', (Join-Path $repoRoot 'Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.csproj'),
    '--configuration', $Configuration, '--framework', 'netstandard2.1', '--nologo')
Invoke-DotNet @('build', (Join-Path $repoRoot 'Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.csproj'),
    '--configuration', $Configuration, '--framework', 'net8.0', '--nologo')
Invoke-DotNet @('build', (Join-Path $repoRoot 'STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj'),
    '--configuration', $Configuration, '--nologo')

Write-Host "Full-runtime regression validation passed. Evidence: $artifactsPath"
