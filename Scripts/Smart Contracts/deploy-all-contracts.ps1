# OASIS Multi-Chain Contract Deployment Script (PowerShell)
# This script helps deploy OASIS contracts to all supported chains
# 
# IMPORTANT: This script requires manual intervention for:
# - Private key input (never hardcode!)
# - Gas fee confirmation
# - Contract verification

param(
    [string]$DeployerPrivateKey = "",
    [string]$Network = "all",
    [switch]$Testnet = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== OASIS Multi-Chain Contract Deployment ===" -ForegroundColor Green
Write-Host ""

# Check prerequisites
function Check-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow
    
    # Check Node.js
    try {
        $nodeVersion = node --version
        Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
    } catch {
        Write-Host "❌ Node.js not found. Please install Node.js v16+" -ForegroundColor Red
        exit 1
    }
    
    # Check for private key
    if ([string]::IsNullOrEmpty($DeployerPrivateKey)) {
        $envKey = $env:DEPLOYER_PRIVATE_KEY
        if ([string]::IsNullOrEmpty($envKey)) {
            Write-Host "⚠️  DEPLOYER_PRIVATE_KEY not set" -ForegroundColor Yellow
            $DeployerPrivateKey = Read-Host "Enter deployer private key (or press Enter to skip)"
            if ([string]::IsNullOrEmpty($DeployerPrivateKey)) {
                Write-Host "❌ Private key required for deployment" -ForegroundColor Red
                exit 1
            }
        } else {
            $DeployerPrivateKey = $envKey
        }
    }
    
    Write-Host "✅ Prerequisites check complete" -ForegroundColor Green
    Write-Host ""
}

# Deploy to EVM chain
function Deploy-EVMChain {
    param(
        [string]$ChainName,
        [string]$NetworkName,
        [string]$RpcUrl,
        [string]$ChainId
    )
    
    Write-Host "Deploying to $ChainName..." -ForegroundColor Yellow
    
    $deployScript = "scripts/deploy-$NetworkName.js"
    
    if (-not (Test-Path $deployScript)) {
        $scriptContent = @"
const hre = require("hardhat");
const fs = require("fs");

async function main() {
    const OASIS = await hre.ethers.getContractFactory("OASIS");
    const oasis = await OASIS.deploy();
    await oasis.waitForDeployment();
    
    const address = await oasis.getAddress();
    console.log("OASIS deployed to:", address);
    console.log("Network:", "$ChainName");
    console.log("Chain ID:", $ChainId);
    
    // Save address to file
    let addresses = {};
    if (fs.existsSync('deployed-addresses.json')) {
        addresses = JSON.parse(fs.readFileSync('deployed-addresses.json', 'utf8'));
    }
    addresses["$NetworkName"] = {
        chain: "$ChainName",
        address: address,
        chainId: $ChainId,
        deployedAt: new Date().toISOString()
    };
    fs.writeFileSync('deployed-addresses.json', JSON.stringify(addresses, null, 2));
}

main()
    .then(() => process.exit(0))
    .catch((error) => {
        console.error(error);
        process.exit(1);
    });
"@
        New-Item -ItemType Directory -Force -Path "scripts" | Out-Null
        $scriptContent | Out-File -FilePath $deployScript -Encoding UTF8
    }
    
    Write-Host "⚠️  Manual step required:" -ForegroundColor Yellow
    Write-Host "1. Update hardhat.config.js with $ChainName network"
    Write-Host "2. Run: npx hardhat run $deployScript --network $NetworkName"
    Write-Host ""
}

# Main deployment flow
function Main {
    Check-Prerequisites
    
    $chains = @{
        "ethereum" = @{ Name = "Ethereum"; Rpc = "https://eth.llamarpc.com"; ChainId = "1" }
        "arbitrum" = @{ Name = "Arbitrum"; Rpc = "https://arb1.arbitrum.io/rpc"; ChainId = "42161" }
        "optimism" = @{ Name = "Optimism"; Rpc = "https://mainnet.optimism.io"; ChainId = "10" }
        "base" = @{ Name = "Base"; Rpc = "https://mainnet.base.org"; ChainId = "8453" }
        "polygon" = @{ Name = "Polygon"; Rpc = "https://polygon-rpc.com"; ChainId = "137" }
        "bnb" = @{ Name = "BNB Chain"; Rpc = "https://bsc-dataseed.binance.org"; ChainId = "56" }
        "fantom" = @{ Name = "Fantom"; Rpc = "https://rpc.ftm.tools"; ChainId = "250" }
        "avalanche" = @{ Name = "Avalanche"; Rpc = "https://api.avax.network/ext/bc/C/rpc"; ChainId = "43114" }
        "zksync" = @{ Name = "zkSync"; Rpc = "https://mainnet.era.zksync.io"; ChainId = "324" }
        "linea" = @{ Name = "Linea"; Rpc = "https://rpc.linea.build"; ChainId = "59144" }
        "scroll" = @{ Name = "Scroll"; Rpc = "https://rpc.scroll.io"; ChainId = "534352" }
    }
    
    if ($Network -eq "all") {
        Write-Host "Deploying to all EVM chains..." -ForegroundColor Yellow
        foreach ($chain in $chains.GetEnumerator()) {
            Deploy-EVMChain -ChainName $chain.Value.Name -NetworkName $chain.Key -RpcUrl $chain.Value.Rpc -ChainId $chain.Value.ChainId
        }
    } elseif ($chains.ContainsKey($Network)) {
        $chain = $chains[$Network]
        Deploy-EVMChain -ChainName $chain.Name -NetworkName $Network -RpcUrl $chain.Rpc -ChainId $chain.ChainId
    } else {
        Write-Host "Available networks: $($chains.Keys -join ', ')" -ForegroundColor Yellow
        Write-Host "Usage: .\deploy-all-contracts.ps1 -Network <network-name>" -ForegroundColor Yellow
    }
}

Main


