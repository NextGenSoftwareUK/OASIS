[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Start','Stop')][string]$Action,
    [Parameter(Mandatory)][string]$StateDirectory,
    [int]$Port = 5055,
    [int]$DelayMilliseconds = 0,
    [string]$PythonPath
)
$ErrorActionPreference = 'Stop'
$state = [IO.Path]::GetFullPath($StateDirectory)
$pidPath = Join-Path $state "host-$Port.pid"
if ($Action -eq 'Stop') {
    if (Test-Path -LiteralPath $pidPath) {
        $hostPid = [int](Get-Content -Raw -LiteralPath $pidPath)
        Stop-Process -Id $hostPid -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $pidPath -Force -ErrorAction SilentlyContinue
    }
    return
}
New-Item -ItemType Directory -Path $state -Force | Out-Null
$root = Split-Path -Parent $PSScriptRoot
$hostScript = Join-Path $PSScriptRoot 'TestHosts/geohotspot_resilience_host.py'
if (!$PythonPath) {
    $command = Get-Command python -ErrorAction SilentlyContinue
    if ($command -and $command.Source -notlike '*WindowsApps*') { $PythonPath = $command.Source }
    else {
        $bundled = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
        if (Test-Path -LiteralPath $bundled) { $PythonPath = $bundled }
    }
}
if (!$PythonPath -or !(Test-Path -LiteralPath $PythonPath)) { throw 'Python 3 is required for the disposable local GeoHotSpot resilience host. Pass -PythonPath when it is not on PATH.' }
function Quote([string]$value) { '"' + $value.Replace('"','\"') + '"' }
$arguments = "$(Quote $hostScript) --port $Port --database $(Quote (Join-Path $state 'state.sqlite3')) --hotspot-id $((Get-Content -Raw (Join-Path $state 'hotspot-id.txt')).Trim()) --tokens $(Quote (Join-Path $state 'tokens.json')) --delay-ms $DelayMilliseconds"
$process = Start-Process $PythonPath -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $state "host-$Port.out.log") -RedirectStandardError (Join-Path $state "host-$Port.err.log")
Set-Content -LiteralPath $pidPath -Value $process.Id -NoNewline
