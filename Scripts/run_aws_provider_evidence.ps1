<#
.SYNOPSIS
Runs AWSOASIS integration evidence against Amazon's official DynamoDB Local runtime.
.DESCRIPTION
Downloads DynamoDB Local and its published SHA-256 checksum from AWS, verifies the
archive, starts an isolated in-memory instance, and runs the AWSOASIS integration
suite through AWSSDK.DynamoDBv2. No AWS account or billable service is used.
#>
[CmdletBinding()]
param(
    [string]$ArtifactsDirectory = 'artifacts/aws-provider-evidence',
    [string]$WorkingPath = 'TestResults/AWSProviderEvidence',
    [int]$Port = 18000
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifacts = [IO.Path]::GetFullPath((Join-Path $root $ArtifactsDirectory))
$work = [IO.Path]::GetFullPath((Join-Path $root $WorkingPath))
$download = Join-Path $work 'dynamodb_local_latest.zip'
$checksumFile = "$download.sha256"
$runtime = Join-Path $work 'runtime'
$stdout = Join-Path $artifacts 'dynamodb-local.out.log'
$stderr = Join-Path $artifacts 'dynamodb-local.err.log'
$process = $null
$previousEndpoint = $env:DYNAMODB_SERVICE_URL
$lease = $null

function Wait-LoopbackPort([int]$TargetPort, [int]$Seconds = 60) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-NetConnection 127.0.0.1 -Port $TargetPort -InformationLevel Quiet -WarningAction SilentlyContinue) { return }
        Start-Sleep -Milliseconds 500
    }
    throw "DynamoDB Local did not open loopback port $TargetPort within $Seconds seconds."
}

if (Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) {
    throw "Loopback port $Port is already in use; refusing to run against an unowned service."
}

New-Item -ItemType Directory -Force $artifacts, $work | Out-Null
if (Get-NetTCPConnection -LocalPort 18001 -State Listen -ErrorAction SilentlyContinue) {
    throw 'Reserved AWS outage fixture port18001 is occupied; refusing to contact an unowned service.'
}
try {
    $lease = [IO.File]::Open((Join-Path $work 'environment.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $jar = Join-Path $runtime 'DynamoDBLocal.jar'
    $runtimeManifest = Join-Path $runtime 'runtime-source-sha256.txt'
    if (Test-Path -LiteralPath $runtime) {
        if (!(Test-Path -LiteralPath $jar -PathType Leaf) -or !(Test-Path -LiteralPath (Join-Path $runtime 'DynamoDBLocal_lib') -PathType Container)) {
            throw 'Existing DynamoDB runtime is incomplete; refusing to delete or overwrite it.'
        }
        if (!(Test-Path -LiteralPath $runtimeManifest -PathType Leaf)) {
            throw 'Existing DynamoDB runtime lacks verified archive provenance; review it rather than reinstalling automatically.'
        }
        $actual = (Get-Content -LiteralPath $runtimeManifest -Raw).Trim()
        if ($actual -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Invalid retained runtime provenance.' }
    }
    else {
    if (!(Test-Path -LiteralPath $download -PathType Leaf)) {
        Invoke-WebRequest 'https://d1ni2b6xgvw0s0.cloudfront.net/dynamodb_local_latest.zip' -OutFile $download
    }
    if (!(Test-Path -LiteralPath $checksumFile -PathType Leaf)) {
        Invoke-WebRequest 'https://d1ni2b6xgvw0s0.cloudfront.net/dynamodb_local_latest.zip.sha256' -OutFile $checksumFile
    }
    $expected = ((Get-Content -LiteralPath $checksumFile -Raw).Trim() -split '\s+')[0].ToUpperInvariant()
    $actual = (Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($expected -notmatch '^[0-9A-F]{64}$' -or $actual -ne $expected) {
        throw "The official DynamoDB Local archive checksum did not match (expected $expected, actual $actual)."
    }

    New-Item -ItemType Directory -Force $runtime | Out-Null
    Expand-Archive -LiteralPath $download -DestinationPath $runtime -Force
    $jar = Join-Path $runtime 'DynamoDBLocal.jar'
    if (!(Test-Path -LiteralPath $jar -PathType Leaf)) { throw 'The verified DynamoDB Local archive did not contain DynamoDBLocal.jar.' }
    Set-Content -LiteralPath $runtimeManifest -Value $actual -Encoding ascii
    # These exact files belong to this runner; retain the runtime, not duplicate archives.
    Remove-Item -LiteralPath $download, $checksumFile -Force
    }

    # Rebuild producers before the test consumer, reusing restored assets and existing outputs.
    # A missing cache is an explicit setup error; routine verification must not redownload dependencies.
    $testProject = 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj'
    foreach ($project in @(
        'OASIS Architecture/NextGenSoftware.OASIS.Common/NextGenSoftware.OASIS.Common.csproj',
        'OASIS Architecture/NextGenSoftware.OASIS.API.DNA/NextGenSoftware.OASIS.API.DNA.csproj',
        'OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj',
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj',
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.csproj',
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj',
        $testProject
    )) {
        & dotnet build (Join-Path $root $project) -c Release -f net10.0 --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if ($LASTEXITCODE -ne 0) { throw "Cached producer/consumer build failed: $project. Restore missing dependencies once into the shared cache before retrying." }
    }

    $process = Start-Process java -ArgumentList @(
        "-Djava.library.path=$runtime/DynamoDBLocal_lib",
        '-jar', $jar,
        '-inMemory', '-sharedDb', '-disableTelemetry',
        '-port', "$Port"
    ) -WorkingDirectory $runtime -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Wait-LoopbackPort $Port

    $env:DYNAMODB_SERVICE_URL = "http://127.0.0.1:$Port"
    $testsStarted=[DateTime]::UtcNow
    & dotnet test (Join-Path $root $testProject) -c Release --no-build --no-restore `
        /m:2 /nr:false /p:UseSharedCompilation=false --logger 'trx;LogFileName=aws-provider.trx' --results-directory $artifacts -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "AWSOASIS provider evidence failed with exit code $LASTEXITCODE." }
    $reportFile=Get-Item -LiteralPath (Join-Path $artifacts 'aws-provider.trx') -ErrorAction Stop
    if($reportFile.LastWriteTimeUtc -lt $testsStarted){throw 'AWS native report is stale; no verification claim permitted.'}
    [xml]$report = Get-Content -LiteralPath $reportFile.FullName -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int]$counters.total -ne 31 -or [int]$counters.passed -ne 31 -or [int]$counters.notExecuted -ne 0) {
        throw 'Expected exactly seventeen passing native AWS tests without skips.'
    }

    [ordered]@{
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        provider = 'AWSOASIS'
        client = 'AWSSDK.DynamoDBv2'
        runtime = 'Official Amazon DynamoDB Local'
        runtimeArchiveSha256 = $actual
        tests = [int]$counters.total
        skipped = 0
        status = 'PASS'
    } | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $artifacts 'summary.json')
}
finally {
    $env:DYNAMODB_SERVICE_URL = $previousEndpoint
    if ($null -ne $process) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { if ($null -ne $process -and $_ -eq $process.Id) { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue } }
    if ($null -ne $lease) { $lease.Dispose() }
}
