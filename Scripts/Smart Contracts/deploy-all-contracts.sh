#!/bin/bash

# OASIS Multi-Chain Contract Deployment Script
# This script helps deploy OASIS contracts to all supported chains
# 
# IMPORTANT: This script requires manual intervention for:
# - Private key input (never hardcode!)
# - Gas fee confirmation
# - Contract verification

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== OASIS Multi-Chain Contract Deployment ===${NC}\n"

# Check prerequisites
check_prerequisites() {
    echo "Checking prerequisites..."
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        echo -e "${RED}❌ Node.js not found. Please install Node.js v16+${NC}"
        exit 1
    fi
    
    # Check Hardhat
    if ! command -v npx hardhat &> /dev/null; then
        echo -e "${YELLOW}⚠️  Hardhat not found. Installing...${NC}"
        npm install --save-dev hardhat @nomicfoundation/hardhat-toolbox
    fi
    
    # Check for private key
    if [ -z "$DEPLOYER_PRIVATE_KEY" ]; then
        echo -e "${YELLOW}⚠️  DEPLOYER_PRIVATE_KEY not set in environment${NC}"
        echo "Please set it: export DEPLOYER_PRIVATE_KEY=your-private-key"
        read -p "Enter deployer private key (or press Enter to skip): " PRIVATE_KEY
        if [ ! -z "$PRIVATE_KEY" ]; then
            export DEPLOYER_PRIVATE_KEY="$PRIVATE_KEY"
        else
            echo -e "${RED}❌ Private key required for deployment${NC}"
            exit 1
        fi
    fi
    
    echo -e "${GREEN}✅ Prerequisites check complete${NC}\n"
}

# Deploy to EVM chain
deploy_evm_chain() {
    local CHAIN_NAME=$1
    local NETWORK_NAME=$2
    local RPC_URL=$3
    local CHAIN_ID=$4
    
    echo -e "${YELLOW}Deploying to ${CHAIN_NAME}...${NC}"
    
    # Create deployment script if it doesn't exist
    if [ ! -f "scripts/deploy-${NETWORK_NAME}.js" ]; then
        cat > "scripts/deploy-${NETWORK_NAME}.js" << EOF
const hre = require("hardhat");

async function main() {
    const OASIS = await hre.ethers.getContractFactory("OASIS");
    const oasis = await OASIS.deploy();
    await oasis.waitForDeployment();
    
    const address = await oasis.getAddress();
    console.log("OASIS deployed to:", address);
    console.log("Network:", "${CHAIN_NAME}");
    console.log("Chain ID:", ${CHAIN_ID});
    
    // Save address to file
    const fs = require('fs');
    const addresses = JSON.parse(fs.readFileSync('deployed-addresses.json', 'utf8') || '{}');
    addresses["${NETWORK_NAME}"] = {
        chain: "${CHAIN_NAME}",
        address: address,
        chainId: ${CHAIN_ID},
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
EOF
    fi
    
    # Update hardhat config if needed
    # (This would need to be done manually or via script)
    
    echo -e "${YELLOW}⚠️  Manual step required:${NC}"
    echo "1. Update hardhat.config.js with ${CHAIN_NAME} network"
    echo "2. Run: npx hardhat run scripts/deploy-${NETWORK_NAME}.js --network ${NETWORK_NAME}"
    echo ""
}

# Main deployment flow
main() {
    check_prerequisites
    
    echo "Select deployment option:"
    echo "1) Deploy to all EVM chains"
    echo "2) Deploy to specific chain"
    echo "3) Check deployment status"
    echo "4) Exit"
    read -p "Choice [1-4]: " choice
    
    case $choice in
        1)
            echo -e "${YELLOW}Deploying to all EVM chains...${NC}"
            deploy_evm_chain "Ethereum" "ethereum" "https://eth.llamarpc.com" "1"
            deploy_evm_chain "Arbitrum" "arbitrum" "https://arb1.arbitrum.io/rpc" "42161"
            deploy_evm_chain "Optimism" "optimism" "https://mainnet.optimism.io" "10"
            deploy_evm_chain "Base" "base" "https://mainnet.base.org" "8453"
            deploy_evm_chain "Polygon" "polygon" "https://polygon-rpc.com" "137"
            deploy_evm_chain "BNB Chain" "bnb" "https://bsc-dataseed.binance.org" "56"
            deploy_evm_chain "Fantom" "fantom" "https://rpc.ftm.tools" "250"
            deploy_evm_chain "Avalanche" "avalanche" "https://api.avax.network/ext/bc/C/rpc" "43114"
            deploy_evm_chain "zkSync" "zkSync" "https://mainnet.era.zksync.io" "324"
            deploy_evm_chain "Linea" "linea" "https://rpc.linea.build" "59144"
            deploy_evm_chain "Scroll" "scroll" "https://rpc.scroll.io" "534352"
            ;;
        2)
            echo "Available chains:"
            echo "1) Ethereum  2) Arbitrum  3) Optimism  4) Base"
            echo "5) Polygon  6) BNB Chain  7) Fantom  8) Avalanche"
            echo "9) zkSync  10) Linea  11) Scroll"
            read -p "Select chain [1-11]: " chain_choice
            # Implementation for specific chain deployment
            ;;
        3)
            echo "Checking deployment status..."
            if [ -f "deployed-addresses.json" ]; then
                cat deployed-addresses.json
            else
                echo "No deployments found. Run deployment first."
            fi
            ;;
        4)
            echo "Exiting..."
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid choice${NC}"
            exit 1
            ;;
    esac
}

main


