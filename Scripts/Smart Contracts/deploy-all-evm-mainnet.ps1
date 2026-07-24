# Deploy OASIS contracts to ALL EVM mainnets (PowerShell)
# ⚠️  WARNING: This will deploy to MAINNET and cost real money!
# Make sure you've tested on testnet first!

param(
    [switch]$SkipConfirmation = $false
)

$ErrorActionPreference = "Stop"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
Write-Host "⚠️  MAINNET DEPLOYMENT WARNING ⚠️" -ForegroundColor Red
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
Write-Host "This will deploy contracts to MAINNET networks" -ForegroundColor Yellow
Write-Host "This will cost REAL MONEY in gas fees!" -ForegroundColor Yellow
Write-Host ""

if (-not $SkipConfirmation) {
    $tested = Read-Host "Have you tested on testnet first? (yes/no)"
    if ($tested -ne "yes") {
        Write-Host "❌ Please test on testnet first!" -ForegroundColor Red
        exit 1
    }
    
    $confirm = Read-Host "Are you sure you want to deploy to MAINNET? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host ""
Write-Host "=== Deploying OASIS Contracts to ALL EVM Mainnets ===" -ForegroundColor Green
Write-Host ""

# Check for private key
if ([string]::IsNullOrEmpty($env:DEPLOYER_PRIVATE_KEY)) {
    Write-Host "❌ DEPLOYER_PRIVATE_KEY environment variable not set" -ForegroundColor Red
    Write-Host "Please set it: `$env:DEPLOYER_PRIVATE_KEY='your-private-key'"
    exit 1
}

# List of mainnet chains to deploy
$chains = @(
    "ethereum",
    "arbitrum",
    "optimism",
    "base",
    "polygon",
    "bnb",
    "fantom",
    "avalanche",
    "rootstock",
    "zkSync",
    "linea",
    "scroll"
)

$successful = @()
$failed = @()

# Deploy to each chain
foreach ($chain in $chains) {
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host "Deploying to: $chain MAINNET" -ForegroundColor Yellow
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host ""
    
    try {
        node scripts/deploy-evm-chain.js $chain mainnet
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
    
    Start-Sleep -Seconds 5
}

# Summary
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "=== Mainnet Deployment Summary ===" -ForegroundColor Green
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
Write-Host "2. Update OASIS_DNA.json with mainnet addresses"
Write-Host "3. Verify contracts on block explorers"
Write-Host "4. Run integration tests on mainnet"
Write-Host "5. Update documentation with contract addresses"


