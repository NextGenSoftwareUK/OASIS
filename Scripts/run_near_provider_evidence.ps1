[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
foreach($name in @('OASIS_NEAR_REUSABLE_RPC_URL','OASIS_NEAR_REUSABLE_CHAIN_ID','OASIS_NEAR_REUSABLE_CONTRACT_ID','OASIS_NEAR_REUSABLE_SIGNER_PATH')){
    if([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))){throw "Required retained NEAR environment setting missing: $name. No new sandbox will be initialized."}
}
$repoRoot = Split-Path -Parent $PSScriptRoot
$bridgeRoot = Join-Path $repoRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge'
$cacheRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/near'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

Push-Location $bridgeRoot
try {
    if (Test-Path -LiteralPath 'node_modules/near-api-js/package.json') {
        npm ls --omit=dev --all *> $null
        if ($LASTEXITCODE -ne 0) { throw "Existing NEAR SDK dependencies are invalid; repair this shared cache before running evidence." }
    }
    else {
        throw 'Retained near-api-js dependencies are missing. Provision once explicitly; this verification runner never installs packages.'
    }
}
finally { Pop-Location }

$repoWsl = (wsl.exe wslpath -a ($repoRoot -replace '\\', '/')).Trim()
$cacheWsl = (wsl.exe wslpath -a ($cacheRoot -replace '\\', '/')).Trim()
$runnerWsl = "$repoWsl/Scripts/run_near_provider_evidence.sh"
wsl.exe env "OASIS_REPO_ROOT_WSL=$repoWsl" "OASIS_NEAR_CACHE_ROOT=$cacheWsl" `
    "OASIS_NEAR_REUSABLE_RPC_URL=$env:OASIS_NEAR_REUSABLE_RPC_URL" "OASIS_NEAR_REUSABLE_CHAIN_ID=$env:OASIS_NEAR_REUSABLE_CHAIN_ID" `
    "OASIS_NEAR_REUSABLE_CONTRACT_ID=$env:OASIS_NEAR_REUSABLE_CONTRACT_ID" "OASIS_NEAR_REUSABLE_SIGNER_PATH=$env:OASIS_NEAR_REUSABLE_SIGNER_PATH" bash $runnerWsl
if ($LASTEXITCODE -ne 0) { throw "NEAR provider evidence failed." }
