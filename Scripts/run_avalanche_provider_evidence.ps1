[CmdletBinding()]
param(
    [string]$ArtifactsDirectory = 'artifacts/avalanche-provider-evidence',
    [int]$Port = 8545,
    [ValidateSet('Avalanche', 'Base')]
    [string]$Provider = 'Avalanche'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifacts = [IO.Path]::GetFullPath((Join-Path $root $ArtifactsDirectory))
$contractRoot = Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS'
$providerSettings = if ($Provider -eq 'Base') {
    @{
        ChainId = '8453'
        Prefix = 'OASIS_BASE'
        Project = 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.BaseOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.BaseOASIS.IntegrationTests.csproj'
        ResultName = 'base'
    }
} else {
    @{
        ChainId = '43114'
        Prefix = 'OASIS_AVALANCHE'
        Project = 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests.csproj'
        ResultName = 'avalanche'
    }
}
$testProject = Join-Path $root $providerSettings.Project
$ganache = $null

if (Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) {
    throw "Loopback port $Port is already in use; refusing to test against an unowned runtime."
}

New-Item -ItemType Directory -Force $artifacts | Out-Null
try {
    $ganache = Start-Process npx.cmd -ArgumentList @('--yes', 'ganache', '--server.host', '127.0.0.1', '--server.port', "$Port", '--chain.chainId', $providerSettings.ChainId, '--wallet.deterministic') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $artifacts 'ganache.out.log') -RedirectStandardError (Join-Path $artifacts 'ganache.err.log')
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 500
        $ready = Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue
    } until ($ready -or [DateTime]::UtcNow -ge $deadline)
    if (!$ready) { throw 'The disposable EVM runtime did not become ready.' }

    $env:LOCAL_CHAIN_ID = $providerSettings.ChainId
    Push-Location $contractRoot
    try { $deployment = (& npx.cmd hardhat run scripts/deploy.js --network localhost 2>&1 | Out-String) }
    finally { Pop-Location }
    $contractAddress = [regex]::Match($deployment, '0x[a-fA-F0-9]{40}').Value
    if ([string]::IsNullOrWhiteSpace($contractAddress)) { throw "Contract deployment did not return an address.`n$deployment" }

    [Environment]::SetEnvironmentVariable("$($providerSettings.Prefix)_RPC_URL", "http://127.0.0.1:$Port")
    [Environment]::SetEnvironmentVariable("$($providerSettings.Prefix)_PRIVATE_KEY", '0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d')
    [Environment]::SetEnvironmentVariable("$($providerSettings.Prefix)_CONTRACT_ADDRESS", $contractAddress)
    & dotnet test $testProject -c Release --logger "trx;LogFileName=$($providerSettings.ResultName).trx" --results-directory $artifacts -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "$Provider provider evidence failed with exit code $LASTEXITCODE." }
}
finally {
    if ($null -ne $ganache) { Stop-Process -Id $ganache.Id -Force -ErrorAction SilentlyContinue }
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    Remove-Item Env:LOCAL_CHAIN_ID -ErrorAction SilentlyContinue
    [Environment]::SetEnvironmentVariable("$($providerSettings.Prefix)_RPC_URL", $null)
    [Environment]::SetEnvironmentVariable("$($providerSettings.Prefix)_PRIVATE_KEY", $null)
    [Environment]::SetEnvironmentVariable("$($providerSettings.Prefix)_CONTRACT_ADDRESS", $null)
}
