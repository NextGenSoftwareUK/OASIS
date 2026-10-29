#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $taskRoot
try {
    & "$PSScriptRoot/Invoke-WithProviderBuildLease.ps1" -Owner cosmic-sqlite-migration-fence -Operation {
        $work = Join-Path $taskRoot 'TestResults/DevelopmentSyncCurrent'
        New-Item -ItemType Directory -Force -Path $work | Out-Null
        Get-PSDrive C | Select-Object Free
        $project = 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
        foreach($producer in @('OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj',
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj', $project)) {
            dotnet build $producer --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q *> (Join-Path $work 'native-fence-runner-build.log')
            if($LASTEXITCODE -ne 0){throw "Fence producer build failed: $producer"}
        }
        $prior = $env:OASIS_OWNED_MIGRATION_FENCE_ROOT
        try {
            $env:OASIS_OWNED_MIGRATION_FENCE_ROOT = $taskRoot
            dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~NativeRecordFenceBlocksV2WritesAndDeliveryAndSurvivesRestart' --logger 'trx;LogFileName=native-fence-sqlite.trx' --results-directory $work
            if($LASTEXITCODE -ne 0){throw 'Native SQLite migration fence verification failed; preserve original fixture outcomes.'}
            [xml]$report = Get-Content (Join-Path $work 'native-fence-sqlite.trx')
            if($report.TestRun.ResultSummary.Counters.total -ne '2' -or $report.TestRun.ResultSummary.Counters.passed -ne '2'){throw 'Expected both explicit native fence cases.'}
        }
        finally {$env:OASIS_OWNED_MIGRATION_FENCE_ROOT = $prior}
        Get-PSDrive C | Select-Object Free
    }
}
finally { Pop-Location }
