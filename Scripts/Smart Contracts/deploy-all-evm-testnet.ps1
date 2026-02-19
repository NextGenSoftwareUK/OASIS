# Deploy OASIS contracts to ALL EVM testnets (PowerShell)
# This script deploys contracts to all supported EVM testnet networks

param(
    [switch]$SkipConfirmation = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Deploying OASIS Contracts to ALL EVM Testnets ===" -ForegroundColor Green
Write-Host ""

# Check for private key
if ([string]::IsNullOrEmpty($env:DEPLOYER_PRIVATE_KEY)) {
    Write-Host "❌ DEPLOYER_PRIVATE_KEY environment variable not set" -ForegroundColor Red
    Write-Host "Please set it: `$env:DEPLOYER_PRIVATE_KEY='your-private-key'"
    exit 1
}

# List of testnet chains to deploy
$chains = @(
    "sepolia",
    "arbitrumSepolia",
    "optimismSepolia",
    "baseSepolia",
    "amoy",
    "bnbTestnet",
    "fantomTestnet",
    "fuji",
    "rootstockTestnet",
    "zkSyncTestnet",
    "lineaTestnet",
    "scrollSepolia"
)

$successful = @()
$failed = @()

# Deploy to each chain
foreach ($chain in $chains) {
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host "Deploying to: $chain" -ForegroundColor Yellow
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host ""
    
    try {
        node scripts/deploy-evm-chain.js $chain
        if ($LASTEXITCODE -eq 0) {
            $successful += $chain
            Write-Host "✅ Successfully deployed to $chain" -ForegroundColor Green
        } else {
            $failed += $chain
            Write-Host "❌ Failed to deploy to $chain" -ForegroundColor Red
        }
    } catch {
        $failed += $chain
        Write-Host "❌ Failed to deploy to $chain: $_" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 2
}

# Summary
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "=== Deployment Summary ===" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host ""

Write-Host "✅ Successful Deployments ($($successful.Count)):" -ForegroundColor Green
foreach ($chain in $successful) {
    Write-Host "   - $chain"
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ Failed Deployments ($($failed.Count)):" -ForegroundColor Red
    foreach ($chain in $failed) {
        Write-Host "   - $chain"
    }
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review deployed-addresses.json for all contract addresses"
Write-Host "2. Update OASIS_DNA.json with testnet addresses"
Write-Host "3. Run integration tests on testnets"
Write-Host "4. Deploy to mainnet after testnet verification"


