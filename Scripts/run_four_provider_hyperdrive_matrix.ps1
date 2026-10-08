<#
.SYNOPSIS
Runs real isolated MongoDB, SQLite, IPFS and Ethereum provider evidence.
.DESCRIPTION
Uses a disposable MongoDB replica set, temporary SQLite databases, an offline loopback
Kubo daemon, and a loopback Ganache EVM with deterministic development-only accounts.
No public chain, paid gas, Railway database, or shared provider is contacted.
#>
[CmdletBinding()]
param(
    [string]$ArtifactsDirectory = 'artifacts/four-provider-matrix',
    [string]$WorkingPath = 'TestResults/HyperDriveProviderMatrix'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifacts = [IO.Path]::GetFullPath((Join-Path $root $ArtifactsDirectory))
$work = [IO.Path]::GetFullPath((Join-Path $root $WorkingPath))
$kuboVersion = '0.43.1'
$kuboSha256 = 'C25871516DEC4D97CE1D160A26C1E3FDAD294CB0EEA065C0C0343BCFCA86C524'
$kuboApiPort = 5001
$ganachePort = 8545
$started = [Collections.Generic.List[int]]::new()

function Wait-LoopbackPort([int]$Port, [int]$Seconds = 60) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) { return }
        Start-Sleep -Milliseconds 500
    }
    throw "Loopback port $Port did not open within $Seconds seconds."
}

function Invoke-ProviderTest([string]$Name, [string]$Project) {
    $resultPath = Join-Path $artifacts $Name
    New-Item -ItemType Directory -Force $resultPath | Out-Null
    & dotnet test $Project -c Release --logger "trx;LogFileName=$Name.trx" --results-directory $resultPath -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "$Name provider evidence failed with exit code $LASTEXITCODE." }
}

foreach ($port in @($kuboApiPort, $ganachePort)) {
    if (Test-NetConnection 127.0.0.1 -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue) {
        throw "Loopback port $port is already in use; refusing to test against an unowned runtime."
    }
}

New-Item -ItemType Directory -Force $artifacts, $work | Out-Null
try {
    $downloads = Join-Path $work 'downloads'
    $kuboRoot = Join-Path $work 'kubo'
    $kuboRepo = Join-Path $kuboRoot 'repo'
    $kuboZip = Join-Path $downloads "kubo_v$($kuboVersion)_windows-amd64.zip"
    New-Item -ItemType Directory -Force $downloads, $kuboRoot | Out-Null
    if (!(Test-Path -LiteralPath $kuboZip -PathType Leaf)) {
        Invoke-WebRequest "https://github.com/ipfs/kubo/releases/download/v$kuboVersion/kubo_v$($kuboVersion)_windows-amd64.zip" -OutFile $kuboZip -MaximumRedirection 8
    }
    if ((Get-FileHash $kuboZip -Algorithm SHA256).Hash -ne $kuboSha256) { throw 'The pinned Kubo archive checksum did not match.' }
    $ipfs = Join-Path $kuboRoot 'kubo/ipfs.exe'
    if (!(Test-Path -LiteralPath $ipfs -PathType Leaf)) { Expand-Archive $kuboZip -DestinationPath $kuboRoot -Force }
    $env:IPFS_PATH = $kuboRepo
    if (!(Test-Path -LiteralPath (Join-Path $kuboRepo 'config'))) { & $ipfs init --profile=test }
    & $ipfs config Addresses.API "/ip4/127.0.0.1/tcp/$kuboApiPort"
    & $ipfs config Addresses.Gateway '/ip4/127.0.0.1/tcp/8080'
    $kubo = Start-Process $ipfs -ArgumentList @('daemon', '--offline') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $artifacts 'kubo.out.log') -RedirectStandardError (Join-Path $artifacts 'kubo.err.log')
    $started.Add($kubo.Id)
    Wait-LoopbackPort $kuboApiPort

    $ganache = Start-Process npx.cmd -ArgumentList @('--yes', 'ganache', '--server.host', '127.0.0.1', '--server.port', "$ganachePort", '--chain.chainId', '31337', '--wallet.deterministic') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $artifacts 'ganache.out.log') -RedirectStandardError (Join-Path $artifacts 'ganache.err.log')
    $started.Add($ganache.Id)
    Wait-LoopbackPort $ganachePort

    $env:IPFSOASIS_TEST_API = "http://127.0.0.1:$kuboApiPort"
    $env:ETHEREUMOASIS_TEST_RPC = "http://127.0.0.1:$ganachePort"
    $env:ETHEREUMOASIS_TEST_CHAIN_ID = '31337'
    $env:ETHEREUMOASIS_TEST_PRIVATE_KEY = '0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d'

    Invoke-ProviderTest 'sqlite' 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests.csproj'
    Invoke-ProviderTest 'ipfs' 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests.csproj'
    Invoke-ProviderTest 'ethereum' 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests.csproj'
    & (Join-Path $PSScriptRoot 'run_hosted_mongo_release_evidence.ps1') -ArtifactsDirectory (Join-Path $ArtifactsDirectory 'mongo')
    if ($LASTEXITCODE -ne 0) { throw "MongoDB provider evidence failed with exit code $LASTEXITCODE." }

    [ordered]@{
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        providers = @('MongoDBOASIS', 'SQLLiteDBOASIS', 'IPFSOASIS', 'EthereumOASIS')
        ethereumNetwork = 'Disposable Ganache chain 31337 (free development ETH; never mainnet)'
        ipfsNetwork = "Offline Kubo $kuboVersion on loopback"
        status = 'PASS'
    } | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $artifacts 'summary.json')
}
finally {
    foreach ($processId in $started) { Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue }
    foreach ($port in @($kuboApiPort, $ganachePort)) {
        Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique |
            ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    }
    Remove-Item Env:IPFSOASIS_TEST_API, Env:ETHEREUMOASIS_TEST_RPC, Env:ETHEREUMOASIS_TEST_CHAIN_ID, Env:ETHEREUMOASIS_TEST_PRIVATE_KEY -ErrorAction SilentlyContinue
}
