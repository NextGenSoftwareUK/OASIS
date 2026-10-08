[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$bridgeRoot = Join-Path $repoRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge'
$cacheRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/near'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

Push-Location $bridgeRoot
try {
    npm ci --ignore-scripts
    if ($LASTEXITCODE -ne 0) { throw "Failed to restore the integrity-locked official near-api-js bridge." }
}
finally { Pop-Location }

$repoWsl = (wsl.exe wslpath -a ($repoRoot -replace '\\', '/')).Trim()
$cacheWsl = (wsl.exe wslpath -a ($cacheRoot -replace '\\', '/')).Trim()
$runnerWsl = "$repoWsl/Scripts/run_near_provider_evidence.sh"
wsl.exe env "OASIS_REPO_ROOT_WSL=$repoWsl" "OASIS_NEAR_CACHE_ROOT=$cacheWsl" bash $runnerWsl
if ($LASTEXITCODE -ne 0) { throw "NEAR provider evidence failed." }
