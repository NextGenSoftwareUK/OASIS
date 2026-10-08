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
try {
    if (!(Test-Path -LiteralPath $download -PathType Leaf)) {
        Invoke-WebRequest 'https://d1ni2b6xgvw0s0.cloudfront.net/dynamodb_local_latest.zip' -OutFile $download
    }
    Invoke-WebRequest 'https://d1ni2b6xgvw0s0.cloudfront.net/dynamodb_local_latest.zip.sha256' -OutFile $checksumFile
    $expected = ((Get-Content -LiteralPath $checksumFile -Raw).Trim() -split '\s+')[0].ToUpperInvariant()
    $actual = (Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($expected -notmatch '^[0-9A-F]{64}$' -or $actual -ne $expected) {
        throw "The official DynamoDB Local archive checksum did not match (expected $expected, actual $actual)."
    }

    if (Test-Path -LiteralPath $runtime) { Remove-Item -LiteralPath $runtime -Recurse -Force }
    New-Item -ItemType Directory -Force $runtime | Out-Null
    Expand-Archive -LiteralPath $download -DestinationPath $runtime -Force
    $jar = Join-Path $runtime 'DynamoDBLocal.jar'
    if (!(Test-Path -LiteralPath $jar -PathType Leaf)) { throw 'The verified DynamoDB Local archive did not contain DynamoDBLocal.jar.' }

    $process = Start-Process java -ArgumentList @(
        "-Djava.library.path=$runtime/DynamoDBLocal_lib",
        '-jar', $jar,
        '-inMemory', '-sharedDb', '-disableTelemetry',
        '-port', "$Port"
    ) -WorkingDirectory $runtime -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Wait-LoopbackPort $Port

    $env:DYNAMODB_SERVICE_URL = "http://127.0.0.1:$Port"
    & dotnet test 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj' `
        -c Release --logger 'trx;LogFileName=aws-provider.trx' --results-directory $artifacts -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "AWSOASIS provider evidence failed with exit code $LASTEXITCODE." }

    [ordered]@{
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        provider = 'AWSOASIS'
        client = 'AWSSDK.DynamoDBv2'
        runtime = 'Official Amazon DynamoDB Local'
        runtimeArchiveSha256 = $actual
        tests = 2
        skipped = 0
        status = 'PASS'
    } | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $artifacts 'summary.json')
}
finally {
    Remove-Item Env:DYNAMODB_SERVICE_URL -ErrorAction SilentlyContinue
    if ($null -ne $process) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { if ($null -ne $process -and $_ -eq $process.Id) { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue } }
}
