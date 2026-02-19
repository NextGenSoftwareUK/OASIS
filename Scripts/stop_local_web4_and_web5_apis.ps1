param(
    [switch]$UsePortFallback
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stateFile = Join-Path $scriptDir ".local_api_processes.json"

function Stop-ByPid {
    param([int]$ProcessId, [string]$Name)

    try
    {
        $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($null -ne $process)
        {
            Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
            Write-Host "Stopped $Name (pid: $ProcessId)"
            return $true
        }
    }
    catch
    {
    }

    Write-Host "$Name not running (pid: $ProcessId)"
    return $false
}

function Stop-ByPorts {
    param([int[]]$Ports)

    $stoppedAny = $false
    foreach ($port in $Ports)
    {
        try
        {
            $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
            foreach ($connection in $connections)
            {
                $pid = [int]$connection.OwningProcess
                if ($pid -gt 0)
                {
                    try
                    {
                        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                        Write-Host "Stopped process on port $port (pid: $pid)"
                        $stoppedAny = $true
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }
    }

    return $stoppedAny
}

$stopped = $false
if (Test-Path $stateFile)
{
    try
    {
        $state = Get-Content $stateFile -Raw | ConvertFrom-Json
        foreach ($entry in $state.processes)
        {
            if (Stop-ByPid -ProcessId ([int]$entry.pid) -Name ([string]$entry.name))
            {
                $stopped = $true
            }
        }
    }
    finally
    {
        Remove-Item $stateFile -Force -ErrorAction SilentlyContinue
    }
}
else
{
    Write-Host "No state file found: $stateFile"
}

if ($UsePortFallback)
{
    if (Stop-ByPorts -Ports @(5003, 5004, 5055, 5056))
    {
        $stopped = $true
    }
}

if (-not $stopped)
{
    Write-Host "No local WEB4/WEB5 API processes were stopped."
}

