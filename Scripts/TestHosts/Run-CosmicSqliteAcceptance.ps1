#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work = Join-Path $taskRoot 'TestResults/SQLiteCurrent'

# This runner owns the global build lease. Do not wrap it in another lease.
# Historical reconciliation cases require exact preserved failed fixtures and
# must never run as ordinary regression or as automatic database cleanup.
Push-Location $taskRoot
try {
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC SQLite production acceptance' -Operation {
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    foreach ($name in 'integration-current.db','scoped-visibility.pending.json','scoped-visibility.pending.json.read-baseline.json') {
        if (Test-Path -LiteralPath (Join-Path $work $name)) { throw "Preserve and reconcile the existing owned fixture: $name" }
    }
    $roots = @(
        'OASIS Architecture/NextGenSoftware.OASIS.API.Core',
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage',
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS',
        'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests',
        'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests'
    )
    function SourceSnapshot {
        @($roots | ForEach-Object {
            Get-ChildItem -LiteralPath (Join-Path $taskRoot $_) -Recurse -File |
                Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in '.cs','.csproj','.props','.targets','.xml','.json','.sql' }
        } | Sort-Object FullName | Get-FileHash | Select-Object Path,Hash) | ConvertTo-Json -Compress
    }
    $before = SourceSnapshot
    foreach ($root in $roots) {
        $projects = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot $root) -File -Filter '*.csproj')
        if ($projects.Count -ne 1) { throw "One existing project is required at $root" }
        & dotnet build $projects[0].FullName --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if ($LASTEXITCODE) { throw "Producer/consumer build failed at $root" }
    }
    $identities = @()
    foreach ($consumer in $roots[3..4]) {
        foreach ($index in 0..2) {
            $root = $roots[$index]
            $name = [IO.Path]::GetFileName($root) + '.dll'
            $producer = Join-Path (Join-Path $taskRoot $root) "bin/Debug/net10.0/$name"
            $copy = Join-Path (Join-Path $taskRoot $consumer) "bin/Debug/net10.0/$name"
            $hash = (Get-FileHash -LiteralPath $producer).Hash
            if ($hash -ne (Get-FileHash -LiteralPath $copy).Hash) { throw "Stale consumer: $copy" }
            $identities += @{producer=$producer;consumer=$copy;sha256=$hash}
        }
    }
    $reports = @()
    foreach ($kind in 'native','unit') {
        $root = if ($kind -eq 'native') { $roots[3] } else { $roots[4] }
        $project = (Get-ChildItem -LiteralPath (Join-Path $taskRoot $root) -File -Filter '*.csproj').FullName
        $report = "cosmic-production-$kind-current.trx"
        $started = [DateTime]::UtcNow
        $filter = if ($kind -eq 'native') { @('--filter','FullyQualifiedName!~SQLiteScopedGraphReconciliation') } else { @() }
        & dotnet test $project --no-build --no-restore /m:2 /nr:false @filter --logger "trx;LogFileName=$report" --results-directory $work -v:q
        if ($LASTEXITCODE) { throw "SQLite $kind production tests failed; preserve their report and owned fixture." }
        $file = Get-Item -LiteralPath (Join-Path $work $report)
        if ($file.LastWriteTimeUtc -lt $started) { throw 'Stale production test report.' }
        [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw
        $counts = $xml.TestRun.ResultSummary.Counters
        if ([int]$counts.total -lt 1 -or [int]$counts.passed -ne [int]$counts.total -or [int]$counts.notExecuted -ne 0) {
            throw 'Complete fresh production cohort with no skips is required.'
        }
        $reports += @{kind=$kind;total=[int]$counts.total;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash;path=$file.FullName}
    }
    if ($before -cne (SourceSnapshot)) { throw 'Core/Shared/SQLite source changed during acceptance; retain results as an older cohort.' }
    foreach ($name in 'integration-current.db','integration-current.db-wal','integration-current.db-shm','scoped-visibility.pending.json') {
        if (Test-Path -LiteralPath (Join-Path $work $name)) { throw "Owned fixture teardown is incomplete: $name" }
    }
    @{status='passed';sourceUnchanged=$true;identities=$identities;reports=$reports;excludedHistoricalClass='SQLiteScopedGraphReconciliation'} |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $work 'cosmic-production-current.json')
}

} finally { Pop-Location }
