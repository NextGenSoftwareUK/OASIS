#Requires -Version 7.0
param([switch]$Serve)

$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work = Join-Path $taskRoot 'TestResults/CosmicCqlDemo'
$priorMode = $env:OASIS_CQL_V1_DEMO_MODE

# Reuse the existing native environment. This runner never installs MongoDB,
# Neo4j, a container or a second SDK, and never resets a retained failed fixture.
New-Item -ItemType Directory -Force -Path $work | Out-Null
if (Test-Path -LiteralPath (Join-Path $work 'v1-session.json')) {
    throw 'A previous demo readiness file remains. Finish or inspect that run before starting another.'
}

if ($Serve) {
    if (Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue) {
        throw 'Loopback demo port 5088 is occupied; preserve the current owner.'
    }
    Write-Host 'Starting MongoDB, SQLite and Neo4j using the retained native environment.'
    Write-Host 'After verification, open http://127.0.0.1:5088/demo (or /swagger).'
    Write-Host 'Use Finish demo in the page for verified settlement and cleanup.'
}

try {
    $env:OASIS_CQL_V1_DEMO_MODE = if ($Serve) { 'serve' } else { 'console' }

    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'CQL v1 three-provider developer demo' -Operation {
        $project = Join-Path $taskRoot 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'

        # Restore the existing pinned graph in the shared cache, then build and
        # verify producer copies through the existing owned-service runner.
        & dotnet restore $project --no-dependencies --disable-parallel -v:q
        if ($LASTEXITCODE) { throw 'Pinned demo dependency restore failed.' }

        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CqlV1Demo
        if ($LASTEXITCODE) { throw 'Native demo failed. Preserve its journal and data.' }

        $report = Get-Content -LiteralPath (Join-Path $work 'v1-demo-result.json') -Raw | ConvertFrom-Json
        if ($report.status -ne 'passed' -or $report.providers.Count -ne 3 -or $report.walkthroughRequests -ne 9) {
            throw 'Three-provider demo proof is incomplete.'
        }

        Write-Host "PASS: all three native providers; $($report.settledOperations) settled operations."
        Write-Host 'Owned services stopped and fixture removed. Spaced C# walkthrough output:'
        Write-Host (Join-Path $work 'v1-walkthrough.txt')
        if (!$Serve) { Get-Content -LiteralPath (Join-Path $work 'v1-walkthrough.txt') }
    }
}
finally {
    [Environment]::SetEnvironmentVariable('OASIS_CQL_V1_DEMO_MODE', $priorMode)
}
