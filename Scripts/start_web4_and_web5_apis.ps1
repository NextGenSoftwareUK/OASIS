# Start WEB4 (ONODE) and WEB5 (STAR) APIs locally in serial. Saves process state to .local_api_processes.json so stop_web4_and_web5_apis.ps1 can stop them.
# Use -NoWait to leave APIs running in the background (e.g. for run_web4_web5_harnesses.ps1).

param(
    [string]$Configuration = "Release",
    [string]$Web4OasisApiBaseUrl = "http://localhost:5555",
    [string]$Web5StarApiBaseUrl = "http://localhost:5556",
    [int]$StartupTimeoutSeconds = 360,
    [switch]$NoWait
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$web4Project = Join-Path $scriptDir "..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
$web5Project = Join-Path $scriptDir "..\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\NextGenSoftware.OASIS.STAR.WebAPI.csproj"
$stateFile = Join-Path $scriptDir ".local_api_processes.json"
$startedProcesses = @()

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
    param([Parameter(Mandatory = $true)][string]$BaseUrl)

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
        [Parameter(Mandatory = $true)]$Process,
        [int]$TimeoutSeconds = 360
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
        if ($Process.HasExited)
        {
            throw "API process exited while waiting for endpoint (exit code $($Process.ExitCode)). Candidates: $($candidates -join ', ')"
        }

        foreach ($candidate in $candidates)
        {
            if (Test-TcpUrl -BaseUrl $candidate)
            {
                return $candidate
            }
        }

        Start-Sleep -Milliseconds 750
    }

    throw "Timed out waiting for API endpoint. Candidates: $($candidates -join ', ')"
}

function Start-LocalApi {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Urls
    )

    if (!(Test-Path $ProjectPath))
    {
        throw "Project not found: $ProjectPath"
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
    $script:startedProcesses += $process
    return $process
}

function Stop-StartedProcesses {
    foreach ($process in $script:startedProcesses)
    {
        if ($null -ne $process -and !$process.HasExited)
        {
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
}

function Save-ProcessState {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedWeb4,
        [Parameter(Mandatory = $true)][string]$ResolvedWeb5
    )

    $payload = [ordered]@{
        startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        web4Url = $ResolvedWeb4
        web5Url = $ResolvedWeb5
        processes = @(
            @{ name = "WEB4_OASIS_API"; pid = $web4Process.Id; project = $web4Project },
            @{ name = "WEB5_STAR_API"; pid = $web5Process.Id; project = $web5Project }
        )
    }
    $json = $payload | ConvertTo-Json -Depth 5
    Set-Content -Path $stateFile -Value $json -Encoding UTF8
}

function Remove-ProcessState {
    if (Test-Path $stateFile)
    {
        try { Remove-Item $stateFile -Force -ErrorAction SilentlyContinue } catch {}
    }
}

try
{
    $resolvedWeb4Target = Normalize-ApiBaseUrl -Url $Web4OasisApiBaseUrl
    $resolvedWeb5Target = Normalize-ApiBaseUrl -Url $Web5StarApiBaseUrl

    Write-Host "Starting local WEB4 OASIS API (serial: first)..."
    $web4Process = Start-LocalApi -ProjectPath $web4Project -Urls $resolvedWeb4Target
    $resolvedWeb4 = Wait-ForAnyUrl -BaseUrls @($resolvedWeb4Target, "http://localhost:5003") -Process $web4Process -TimeoutSeconds $StartupTimeoutSeconds
    Write-Host "WEB4 OASIS API ready at: $resolvedWeb4 (pid: $($web4Process.Id))"

    Write-Host "Starting local WEB5 STAR API (serial: second)..."
    $web5Process = Start-LocalApi -ProjectPath $web5Project -Urls $resolvedWeb5Target
    $resolvedWeb5 = Wait-ForAnyUrl -BaseUrls @($resolvedWeb5Target, "http://localhost:5055") -Process $web5Process -TimeoutSeconds $StartupTimeoutSeconds
    Write-Host "WEB5 STAR API ready at: $resolvedWeb5 (pid: $($web5Process.Id))"
    Save-ProcessState -ResolvedWeb4 $resolvedWeb4 -ResolvedWeb5 $resolvedWeb5

    Write-Host ""
    Write-Host "Local APIs are running for ODOOM/OQUAKE testing."
    Write-Host "WEB4 OASIS API: $resolvedWeb4"
    Write-Host "WEB5 STAR API:  $resolvedWeb5"

    if ($NoWait)
    {
        Write-Host "NoWait enabled; leaving APIs running in background."
        return
    }

    Write-Host "Press Ctrl+C to stop both APIs."
    Wait-Process -Id $web4Process.Id, $web5Process.Id
}
finally
{
    if (-not $NoWait)
    {
        Write-Host "Stopping local API processes..."
        Stop-StartedProcesses
        Remove-ProcessState
    }
}
