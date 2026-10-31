[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$runner = (Resolve-Path (Join-Path $PSScriptRoot 'TestHosts/run_antelope_seeds.sh')).Path
$wslRunner = (& wsl.exe wslpath -a ($runner -replace '\\', '/')).Trim()
if ([string]::IsNullOrWhiteSpace($wslRunner)) { throw 'Could not resolve the SEEDS evidence runner in WSL.' }
& wsl.exe bash $wslRunner
if ($LASTEXITCODE -ne 0) { throw "SEEDS official Antelope evidence failed with exit code $LASTEXITCODE." }
