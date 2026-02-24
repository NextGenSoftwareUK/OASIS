#!/bin/bash

# Master OASIS Deployment Script
# This script provides a menu-driven interface for deploying contracts

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

clear
echo -e "${CYAN}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║     OASIS Multi-Chain Contract Deployment Master        ║${NC}"
echo -e "${CYAN}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check prerequisites
check_prerequisites() {
    echo -e "${YELLOW}Checking prerequisites...${NC}"
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        echo -e "${RED}❌ Node.js not found. Please install Node.js v16+${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Node.js: $(node --version)${NC}"
    
    # Check for private key
    if [ -z "$DEPLOYER_PRIVATE_KEY" ]; then
        echo -e "${YELLOW}⚠️  DEPLOYER_PRIVATE_KEY not set${NC}"
        read -p "Enter deployer private key (or press Enter to skip): " PRIVATE_KEY
        if [ ! -z "$PRIVATE_KEY" ]; then
            export DEPLOYER_PRIVATE_KEY="$PRIVATE_KEY"
        else
            echo -e "${RED}❌ Private key required${NC}"
            exit 1
        fi
    fi
    echo -e "${GREEN}✅ Deployer private key configured${NC}"
    
    # Check Hardhat config
    if [ ! -f "hardhat.config.js" ]; then
        echo -e "${YELLOW}⚠️  hardhat.config.js not found${NC}"
        if [ -f "scripts/hardhat.config.template.js" ]; then
            echo "Copying template..."
            cp scripts/hardhat.config.template.js hardhat.config.js
            echo -e "${YELLOW}⚠️  Please edit hardhat.config.js with your RPC URLs${NC}"
        fi
    fi
    
    echo ""
}

# Show menu
show_menu() {
    echo -e "${CYAN}Select deployment option:${NC}"
    echo ""
    echo -e "${GREEN}EVM Chains:${NC}"
    echo "  1) Deploy to ALL EVM testnets"
    echo "  2) Deploy to ALL EVM mainnets ⚠️"
    echo "  3) Deploy to individual EVM chain (testnet)"
    echo "  4) Deploy to individual EVM chain (mainnet) ⚠️"
    echo ""
    echo -e "${GREEN}Move Chains:${NC}"
    echo "  5) Deploy to ALL Move chains (testnet)"
    echo "  6) Deploy to ALL Move chains (mainnet) ⚠️"
    echo "  7) Deploy to Aptos (testnet/mainnet)"
    echo "  8) Deploy to Sui (testnet/mainnet)"
    echo ""
    echo -e "${GREEN}Utilities:${NC}"
    echo "  9) Check deployment status"
    echo " 10) Update OASIS_DNA.json from deployments"
    echo " 11) Exit"
    echo ""
}

# Deploy individual EVM chain
deploy_individual_evm() {
    local network_type=$1
    
    echo -e "${CYAN}Available EVM chains:${NC}"
    if [ "$network_type" == "testnet" ]; then
        echo "  sepolia, arbitrumSepolia, optimismSepolia, baseSepolia, amoy,"
        echo "  bnbTestnet, fantomTestnet, fuji, rootstockTestnet,"
        echo "  zkSyncTestnet, lineaTestnet, scrollSepolia"
    else
        echo "  ethereum, arbitrum, optimism, base, polygon, bnb, fantom,"
        echo "  avalanche, rootstock, zkSync, linea, scroll"
    fi
    echo ""
    read -p "Enter chain name: " chain_name
    
    if [ "$network_type" == "testnet" ]; then
        node scripts/deploy-evm-chain.js "$chain_name"
    else
        node scripts/deploy-evm-chain.js "$chain_name" mainnet
    fi
}

# Main menu loop
main() {
    check_prerequisites
    
    while true; do
        show_menu
        read -p "Enter choice [1-11]: " choice
        
        case $choice in
            1)
                echo -e "${YELLOW}Deploying to ALL EVM testnets...${NC}"
                ./scripts/deploy-all-evm-testnet.sh
                ;;
            2)
                echo -e "${RED}⚠️  MAINNET DEPLOYMENT - This will cost real money!${NC}"
                read -p "Are you sure? (yes/no): " confirm
                if [ "$confirm" == "yes" ]; then
                    ./scripts/deploy-all-evm-mainnet.sh
                fi
                ;;
            3)
                deploy_individual_evm "testnet"
                ;;
            4)
                echo -e "${RED}⚠️  MAINNET DEPLOYMENT - This will cost real money!${NC}"
                read -p "Are you sure? (yes/no): " confirm
                if [ "$confirm" == "yes" ]; then
                    deploy_individual_evm "mainnet"
                fi
                ;;
            5)
                echo -e "${YELLOW}Deploying to ALL Move chains (testnet)...${NC}"
                ./scripts/deploy-all-move.sh testnet
                ;;
            6)
                echo -e "${RED}⚠️  MAINNET DEPLOYMENT - This will cost real money!${NC}"
                read -p "Are you sure? (yes/no): " confirm
                if [ "$confirm" == "yes" ]; then
                    ./scripts/deploy-all-move.sh mainnet
                fi
                ;;
            7)
                read -p "Deploy to testnet or mainnet? (testnet/mainnet): " network
                ./scripts/deploy-aptos.sh "$network"
                ;;
            8)
                read -p "Deploy to testnet or mainnet? (testnet/mainnet): " network
                ./scripts/deploy-sui.sh "$network"
                ;;
            9)
                node scripts/check-deployment-status.js
                ;;
            10)
                node scripts/update-dna-from-deployments.js
                ;;
            11)
                echo -e "${GREEN}Exiting...${NC}"
                exit 0
                ;;
            *)
                echo -e "${RED}Invalid choice${NC}"
                ;;
        esac
        
        echo ""
        read -p "Press Enter to continue..."
        clear
    done
}

main


