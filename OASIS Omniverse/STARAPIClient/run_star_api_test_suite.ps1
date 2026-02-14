param(
    [switch]$SkipHarness,
    [bool]$KillStaleTestHosts = $true,
    [string]$Configuration = "Release",
    [int]$RealLocalStartupTimeoutSeconds = 600,
    [ValidateSet("fake", "real-local", "real-live")]
    [string]$HarnessMode = "fake",
    [string]$Web5StarApiBaseUrl = "http://localhost:5055",
    [string]$Web4OasisApiBaseUrl = "http://localhost:5056",
    [string]$RealLiveWeb5StarApiBaseUrl = "https://oasisweb4.one/star/api",
    [string]$RealLiveWeb4OasisApiBaseUrl = "https://oasisweb4.one/api",
    [string]$Username = "",
    [string]$Password = "",
    [string]$ApiKey = "",
    [string]$AvatarId = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$unitProject = Join-Path $scriptDir "TestProjects/NextGenSoftware.OASIS.STARAPI.Client.UnitTests/NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj"
$integrationProject = Join-Path $scriptDir "TestProjects/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj"
$harnessProject = Join-Path $scriptDir "TestProjects/NextGenSoftware.OASIS.STARAPI.Client.TestHarness/NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj"
$localWeb5Project = Join-Path $scriptDir "..\..\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\NextGenSoftware.OASIS.STAR.WebAPI.csproj"
$localWeb4Project = Join-Path $scriptDir "..\..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
$resultsRoot = Join-Path $scriptDir "TestResults"
$unitResultsDir = Join-Path $resultsRoot "Unit"
$integrationResultsDir = Join-Path $resultsRoot "Integration"
$harnessResultsDir = Join-Path $resultsRoot "Harness"
$unitTrx = "starapi-unit-tests.trx"
$integrationTrx = "starapi-integration-tests.trx"
$unitJunit = Join-Path $unitResultsDir "starapi-unit-tests.junit.xml"
$integrationJunit = Join-Path $integrationResultsDir "starapi-integration-tests.junit.xml"
$harnessJunit = Join-Path $harnessResultsDir "starapi-harness.junit.xml"
$startedApiProcesses = @()

function Kill-StaleTestHosts {
    Write-Host "Killing stale testhost processes..."
    try { Get-Process testhost -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue } catch {}
    try { Get-Process testhost.net -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue } catch {}
    try { Get-Process vstest.console -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue } catch {}
}

function Normalize-ApiBaseUrl {
    param([Parameter(Mandatory = $true)][string]$Url)
    $normalized = $Url.Trim().TrimEnd("/")
    if ($normalized.EndsWith("/api", [System.StringComparison]::OrdinalIgnoreCase))
    {
        return $normalized.Substring(0, $normalized.Length - 4)
    }
    return $normalized
}

function Test-TcpUrl {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl
    )

    $uri = [System.Uri]::new($BaseUrl)
    $tcpClient = $null
    try
    {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $async = $tcpClient.BeginConnect($uri.Host, $uri.Port, $null, $null)
        $connected = $async.AsyncWaitHandle.WaitOne(1200)
        if ($connected -and $tcpClient.Connected)
        {
            $tcpClient.EndConnect($async) | Out-Null
            return $true
        }
        return $false
    }
    catch
    {
        return $false
    }
    finally
    {
        if ($null -ne $tcpClient)
        {
            try { $tcpClient.Close() } catch {}
            try { $tcpClient.Dispose() } catch {}
        }
    }
}

function Wait-ForAnyUrl {
    param(
        [Parameter(Mandatory = $true)][string[]]$BaseUrls,
        [Parameter(Mandatory = $false)]$Process = $null,
        [int]$TimeoutSeconds = 180
    )

    $candidates = @()
    foreach ($url in $BaseUrls)
    {
        if (![string]::IsNullOrWhiteSpace($url))
        {
            $candidates += (Normalize-ApiBaseUrl -Url $url)
        }
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        if ($null -ne $Process -and $Process.HasExited)
        {
            throw "API process exited early while waiting for endpoint (exit code $($Process.ExitCode)). Candidates: $($candidates -join ', ')"
        }

        foreach ($candidate in $candidates)
        {
            if (Test-TcpUrl -BaseUrl $candidate)
            {
                return $candidate
            }
        }

        if ($null -ne $Process -and $Process.HasExited)
        {
            throw "API process exited while waiting for endpoint (exit code $($Process.ExitCode)). Candidates: $($candidates -join ', ')"
        }

        Start-Sleep -Milliseconds 750
    }

    throw "Timed out waiting for API endpoint to become available. Candidates: $($candidates -join ', ')"
}

function Start-LocalApi {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Urls
    )

    if (!(Test-Path $ProjectPath))
    {
        throw "Local API project not found: $ProjectPath"
    }

    $args = @(
        "run",
        "--no-launch-profile",
        "--project", "`"$ProjectPath`"",
        "-c", $Configuration,
        "--urls", "`"$Urls`""
    ) -join " "

    $projectWorkingDir = Split-Path -Parent $ProjectPath
    $process = Start-Process -FilePath "dotnet" -ArgumentList $args -WorkingDirectory $projectWorkingDir -PassThru
    $script:startedApiProcesses += $process
    return $process
}

function Stop-StartedApis {
    foreach ($process in $script:startedApiProcesses)
    {
        if ($null -ne $process -and !$process.HasExited)
        {
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Title failed with exit code $LASTEXITCODE" }
}

Write-Host "Running WEB5 STAR API test suite..."
Write-Host "Configuration: $Configuration"
Write-Host "Kill stale test hosts: $KillStaleTestHosts"
Write-Host "Harness mode: $HarnessMode"

New-Item -ItemType Directory -Path $unitResultsDir -Force | Out-Null
New-Item -ItemType Directory -Path $integrationResultsDir -Force | Out-Null
New-Item -ItemType Directory -Path $harnessResultsDir -Force | Out-Null

if ($KillStaleTestHosts) { Kill-StaleTestHosts }

Invoke-Step -Title "Unit Tests" -Action {
    dotnet test $unitProject -c $Configuration --results-directory $unitResultsDir --logger "trx;LogFileName=$unitTrx" --logger "junit;LogFilePath=$unitJunit"
}

if ($KillStaleTestHosts) { Kill-StaleTestHosts }

Invoke-Step -Title "Integration Tests" -Action {
    dotnet test $integrationProject -c $Configuration --results-directory $integrationResultsDir --logger "trx;LogFileName=$integrationTrx" --logger "junit;LogFilePath=$integrationJunit"
}

if (-not $SkipHarness)
{
    try
    {
        $resolvedWeb5 = Normalize-ApiBaseUrl -Url $Web5StarApiBaseUrl
        $resolvedWeb4 = Normalize-ApiBaseUrl -Url $Web4OasisApiBaseUrl

        if ($HarnessMode -eq "real-live")
        {
            $liveWeb5 = Normalize-ApiBaseUrl -Url $RealLiveWeb5StarApiBaseUrl
            $liveWeb4 = Normalize-ApiBaseUrl -Url $RealLiveWeb4OasisApiBaseUrl
            if (-not $PSBoundParameters.ContainsKey("Web5StarApiBaseUrl")) { $resolvedWeb5 = $liveWeb5 }
            if (-not $PSBoundParameters.ContainsKey("Web4OasisApiBaseUrl")) { $resolvedWeb4 = $liveWeb4 }
        }
        elseif ($HarnessMode -eq "real-local")
        {
            Write-Host ""
            Write-Host "Starting local WEB4 OASIS API (sequential startup)..."
            $web4Process = Start-LocalApi -ProjectPath $localWeb4Project -Urls $resolvedWeb4
            $resolvedWeb4 = Wait-ForAnyUrl -BaseUrls @($resolvedWeb4, "http://localhost:5003") -Process $web4Process -TimeoutSeconds $RealLocalStartupTimeoutSeconds
            Write-Host "Starting local WEB5 STAR API (sequential startup)..."
            $web5Process = Start-LocalApi -ProjectPath $localWeb5Project -Urls $resolvedWeb5
            $resolvedWeb5 = Wait-ForAnyUrl -BaseUrls @($resolvedWeb5, "http://localhost:5055") -Process $web5Process -TimeoutSeconds $RealLocalStartupTimeoutSeconds
            Write-Host "Resolved local WEB5 STAR API URL: $resolvedWeb5"
            Write-Host "Resolved local WEB4 OASIS API URL: $resolvedWeb4"
        }

        Write-Host ""
        Write-Host "Setting Test Harness environment variables..."
        $env:STARAPI_WEB5_BASE_URL = $resolvedWeb5
        $env:STARAPI_WEB4_BASE_URL = $resolvedWeb4
        $env:STARAPI_USERNAME = $Username
        $env:STARAPI_PASSWORD = $Password
        $env:STARAPI_API_KEY = $ApiKey
        $env:STARAPI_AVATAR_ID = $AvatarId
        $env:STARAPI_HARNESS_MODE = $HarnessMode
        $env:STARAPI_HARNESS_USE_FAKE_SERVER = if ($HarnessMode -eq "fake") { "true" } else { "false" }
        $env:STARAPI_HARNESS_JUNIT_PATH = $harnessJunit

        Invoke-Step -Title "Console Test Harness" -Action {
            dotnet run --project $harnessProject -c $Configuration
        }
    }
    finally
    {
        if ($HarnessMode -eq "real-local")
        {
            Write-Host "Stopping locally started API processes..."
            Stop-StartedApis
        }
    }
}
else
{
    Write-Host ""
    Write-Host "Console Test Harness skipped."
}

Write-Host ""
Write-Host "WEB5 STAR API test suite completed successfully." -ForegroundColor Green
Write-Host "Artifacts:"
Write-Host " - Unit: $unitResultsDir"
Write-Host " - Integration: $integrationResultsDir"
Write-Host " - Harness: $harnessResultsDir"

