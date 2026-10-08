[CmdletBinding()]
param(
    [string]$ArtifactsDirectory = 'artifacts/avalanche-provider-evidence',
    [int]$Port = 8545
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifacts = [IO.Path]::GetFullPath((Join-Path $root $ArtifactsDirectory))
$contractRoot = Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS'
$testProject = Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests.csproj'
$ganache = $null

if (Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) {
    throw "Loopback port $Port is already in use; refusing to test against an unowned runtime."
}

New-Item -ItemType Directory -Force $artifacts | Out-Null
try {
    $ganache = Start-Process npx.cmd -ArgumentList @('--yes', 'ganache', '--server.host', '127.0.0.1', '--server.port', "$Port", '--chain.chainId', '43114', '--wallet.deterministic') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $artifacts 'ganache.out.log') -RedirectStandardError (Join-Path $artifacts 'ganache.err.log')
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 500
        $ready = Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue
    } until ($ready -or [DateTime]::UtcNow -ge $deadline)
    if (!$ready) { throw 'The disposable EVM runtime did not become ready.' }

    $env:LOCAL_CHAIN_ID = '43114'
    Push-Location $contractRoot
    try { $deployment = (& npx.cmd hardhat run scripts/deploy.js --network localhost 2>&1 | Out-String) }
    finally { Pop-Location }
    $contractAddress = [regex]::Match($deployment, '0x[a-fA-F0-9]{40}').Value
    if ([string]::IsNullOrWhiteSpace($contractAddress)) { throw "Contract deployment did not return an address.`n$deployment" }

    $env:OASIS_AVALANCHE_RPC_URL = "http://127.0.0.1:$Port"
    $env:OASIS_AVALANCHE_PRIVATE_KEY = '0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d'
    $env:OASIS_AVALANCHE_CONTRACT_ADDRESS = $contractAddress
    & dotnet test $testProject -c Release --logger 'trx;LogFileName=avalanche.trx' --results-directory $artifacts -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Avalanche provider evidence failed with exit code $LASTEXITCODE." }
}
finally {
    if ($null -ne $ganache) { Stop-Process -Id $ganache.Id -Force -ErrorAction SilentlyContinue }
    Remove-Item Env:LOCAL_CHAIN_ID, Env:OASIS_AVALANCHE_RPC_URL, Env:OASIS_AVALANCHE_PRIVATE_KEY, Env:OASIS_AVALANCHE_CONTRACT_ADDRESS -ErrorAction SilentlyContinue
}
