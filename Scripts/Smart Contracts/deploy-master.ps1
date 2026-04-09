# Master OASIS Deployment Script (PowerShell)
# This script provides a menu-driven interface for deploying contracts

$ErrorActionPreference = "Stop"

function Show-Menu {
    Clear-Host
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║     OASIS Multi-Chain Contract Deployment Master        ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "EVM Chains:" -ForegroundColor Green
    Write-Host "  1) Deploy to ALL EVM testnets"
    Write-Host "  2) Deploy to ALL EVM mainnets ⚠️"
    Write-Host "  3) Deploy to individual EVM chain (testnet)"
    Write-Host "  4) Deploy to individual EVM chain (mainnet) ⚠️"
    Write-Host ""
    Write-Host "Move Chains:" -ForegroundColor Green
    Write-Host "  5) Deploy to ALL Move chains (testnet)"
    Write-Host "  6) Deploy to ALL Move chains (mainnet) ⚠️"
    Write-Host "  7) Deploy to Aptos (testnet/mainnet)"
    Write-Host "  8) Deploy to Sui (testnet/mainnet)"
    Write-Host ""
    Write-Host "Utilities:" -ForegroundColor Green
    Write-Host "  9) Check deployment status"
    Write-Host " 10) Update OASIS_DNA.json from deployments"
    Write-Host " 11) Exit"
    Write-Host ""
}

function Check-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow
    
    # Check Node.js
    try {
        $nodeVersion = node --version
        Write-Host "✅ Node.js: $nodeVersion" -ForegroundColor Green
    } catch {
        Write-Host "❌ Node.js not found. Please install Node.js v16+" -ForegroundColor Red
        exit 1
    }
    
    # Check for private key
    if ([string]::IsNullOrEmpty($env:DEPLOYER_PRIVATE_KEY)) {
        Write-Host "⚠️  DEPLOYER_PRIVATE_KEY not set" -ForegroundColor Yellow
        $privateKey = Read-Host "Enter deployer private key (or press Enter to skip)"
        if (![string]::IsNullOrEmpty($privateKey)) {
            $env:DEPLOYER_PRIVATE_KEY = $privateKey
        } else {
            Write-Host "❌ Private key required" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "✅ Deployer private key configured" -ForegroundColor Green
    
    Write-Host ""
}

function Deploy-IndividualEVM {
    param([string]$NetworkType)
    
    Write-Host "Available EVM chains:" -ForegroundColor Cyan
    if ($NetworkType -eq "testnet") {
        Write-Host "  sepolia, arbitrumSepolia, optimismSepolia, baseSepolia, amoy,"
        Write-Host "  bnbTestnet, fantomTestnet, fuji, rootstockTestnet,"
        Write-Host "  zkSyncTestnet, lineaTestnet, scrollSepolia"
    } else {
        Write-Host "  ethereum, arbitrum, optimism, base, polygon, bnb, fantom,"
        Write-Host "  avalanche, rootstock, zkSync, linea, scroll"
    }
    Write-Host ""
    $chainName = Read-Host "Enter chain name"
    
    if ($NetworkType -eq "testnet") {
        node scripts/deploy-evm-chain.js $chainName
    } else {
        node scripts/deploy-evm-chain.js $chainName mainnet
    }
}

# Main menu loop
Check-Prerequisites

while ($true) {
    Show-Menu
    $choice = Read-Host "Enter choice [1-11]"
    
    switch ($choice) {
        "1" {
            Write-Host "Deploying to ALL EVM testnets..." -ForegroundColor Yellow
            & ./scripts/deploy-all-evm-testnet.ps1
        }
        "2" {
            Write-Host "⚠️  MAINNET DEPLOYMENT - This will cost real money!" -ForegroundColor Red
            $confirm = Read-Host "Are you sure? (yes/no)"
            if ($confirm -eq "yes") {
                & ./scripts/deploy-all-evm-mainnet.ps1 -SkipConfirmation
            }
        }
        "3" {
            Deploy-IndividualEVM "testnet"
        }
        "4" {
            Write-Host "⚠️  MAINNET DEPLOYMENT - This will cost real money!" -ForegroundColor Red
            $confirm = Read-Host "Are you sure? (yes/no)"
            if ($confirm -eq "yes") {
                Deploy-IndividualEVM "mainnet"
            }
        }
        "5" {
            Write-Host "Deploying to ALL Move chains (testnet)..." -ForegroundColor Yellow
            & ./scripts/deploy-all-move.sh testnet
        }
        "6" {
            Write-Host "⚠️  MAINNET DEPLOYMENT - This will cost real money!" -ForegroundColor Red
            $confirm = Read-Host "Are you sure? (yes/no)"
            if ($confirm -eq "yes") {
                & ./scripts/deploy-all-move.sh mainnet
            }
        }
        "7" {
            $network = Read-Host "Deploy to testnet or mainnet? (testnet/mainnet)"
            & ./scripts/deploy-aptos.sh $network
        }
        "8" {
            $network = Read-Host "Deploy to testnet or mainnet? (testnet/mainnet)"
            & ./scripts/deploy-sui.sh $network
        }
        "9" {
            node scripts/check-deployment-status.js
        }
        "10" {
            node scripts/update-dna-from-deployments.js
        }
        "11" {
            Write-Host "Exiting..." -ForegroundColor Green
            exit 0
        }
        default {
            Write-Host "Invalid choice" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Read-Host "Press Enter to continue"
}


