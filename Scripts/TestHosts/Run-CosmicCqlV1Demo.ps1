#Requires -Version 7.0
param([switch]$Serve,[switch]$IncludeRegression)

$ErrorActionPreference = 'Stop'
if ($Serve -and $IncludeRegression) { throw 'Choose interactive serve or bounded regression verification.' }
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

        function Get-DemoSourceSnapshot {
            $roots = @(
                'Examples/CosmicCqlDemo',
                'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Cql',
                'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Models/Data',
                'OASIS Architecture/NextGenSoftware.OASIS.API.Core',
                'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage',
                'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS',
                'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS',
                'Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS',
                'Providers/Storage/Neo4j.Driver.OASIS',
                'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests'
            )
            @($roots | ForEach-Object {
                Get-ChildItem -LiteralPath (Join-Path $taskRoot $_) -Recurse -File |
                    Where-Object {
                        $_.FullName -notmatch '\\(bin|obj)\\' -and
                        ($_.Extension -in '.cs', '.csproj', '.props', '.snk' -or $_.Name -eq 'windowsZones.xml')
                    }
            } | Get-FileHash | Select-Object Path,Hash) | ConvertTo-Json -Compress
        }
        $sourceBefore = Get-DemoSourceSnapshot

        # Dependencies are already restored in the retained provider environment.
        # The reusable runner builds with --no-restore and verifies producer copies.
        $started = [DateTime]::UtcNow
        if ($IncludeRegression) {
            & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CqlV1Web4
        } else {
            & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CqlV1Demo
        }
        if ($LASTEXITCODE) { throw 'Native CQL v1 walkthrough failed.' }

        $sourceAfter = Get-DemoSourceSnapshot
        @{ before=$sourceBefore; after=$sourceAfter; unchanged=($sourceBefore -ceq $sourceAfter) } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'v1-source-identities.json')
        if ($sourceBefore -cne $sourceAfter) { throw 'Demo sources changed during verification.' }

        $proof = Get-Item -LiteralPath (Join-Path $work 'v1-demo-result.json') -ErrorAction Stop
        if ($proof.LastWriteTimeUtc -lt $started) { throw 'Demo result is stale; no verification claim permitted.' }
        $report = Get-Content -LiteralPath (Join-Path $work 'v1-demo-result.json') -Raw | ConvertFrom-Json
        if ($report.status -ne 'passed' -or $report.providers.Count -ne 3 -or $report.walkthroughRequests -ne 93 -or $report.localSdkQueries -ne 93) {
            throw 'Three-provider demo proof is incomplete.'
        }

        # Capture the tested producer/consumer identities before releasing the
        # global build lease. Another task may rebuild Shared immediately after it.
        $consumer = Join-Path $taskRoot 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0'
        $producers = @(
            'OASIS Architecture/NextGenSoftware.OASIS.API.Core',
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS',
            'Providers/Storage/Neo4j.Driver.OASIS'
        )
        $names = @(
            'NextGenSoftware.OASIS.API.Core',
            'NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage',
            'NextGenSoftware.OASIS.API.Providers.MongoDBOASIS',
            'NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS',
            'NextGenSoftware.OASIS.API.Providers.Neo4jOASIS',
            'Neo4j.Driver'
        )
        $identities = for ($index = 0; $index -lt $names.Count; $index++) {
            $producer = Join-Path $taskRoot "$($producers[$index])/bin/Debug/net10.0/$($names[$index]).dll"
            $copy = Join-Path $consumer "$($names[$index]).dll"
            $expected = (Get-FileHash -LiteralPath $producer -Algorithm SHA256).Hash
            if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ne $expected) {
                throw "Demo consumer differs from tested producer: $producer"
            }
            @{ producer = $producer; consumer = $copy; sha256 = $expected }
        }
        @{ capturedUtc = [DateTime]::UtcNow.ToString('O'); buildLeaseHeld = $true; identities = @($identities) } |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'v1-binary-identities.json')

        Write-Host "PASS: all three native providers; $($report.settledOperations) settled operations."
        Write-Host 'Owned services stopped and fixture removed. Spaced C# walkthrough output:'
        Write-Host (Join-Path $work 'v1-walkthrough.txt')
        if (!$Serve) { Get-Content -LiteralPath (Join-Path $work 'v1-walkthrough.txt') }
    }
}
finally {
    [Environment]::SetEnvironmentVariable('OASIS_CQL_V1_DEMO_MODE', $priorMode)
}
