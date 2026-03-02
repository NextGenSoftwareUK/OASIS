#!/bin/bash

# Deploy OASIS contracts to ALL EVM mainnets
# ⚠️  WARNING: This will deploy to MAINNET and cost real money!
# Make sure you've tested on testnet first!

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${RED}⚠️  MAINNET DEPLOYMENT WARNING ⚠️${NC}"
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}This will deploy contracts to MAINNET networks${NC}"
echo -e "${YELLOW}This will cost REAL MONEY in gas fees!${NC}\n"

read -p "Have you tested on testnet first? (yes/no): " tested
if [ "$tested" != "yes" ]; then
    echo -e "${RED}❌ Please test on testnet first!${NC}"
    exit 1
fi

read -p "Are you sure you want to deploy to MAINNET? (yes/no): " confirm
if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}Cancelled.${NC}"
    exit 0
fi

echo -e "\n${GREEN}=== Deploying OASIS Contracts to ALL EVM Mainnets ===${NC}\n"

# Check for private key
if [ -z "$DEPLOYER_PRIVATE_KEY" ]; then
    echo -e "${RED}❌ DEPLOYER_PRIVATE_KEY environment variable not set${NC}"
    echo "Please set it: export DEPLOYER_PRIVATE_KEY=your-private-key"
    exit 1
fi

# List of mainnet chains to deploy
CHAINS=(
    "ethereum"
    "arbitrum"
    "optimism"
    "base"
    "polygon"
    "bnb"
    "fantom"
    "avalanche"
    "rootstock"
    "zkSync"
    "linea"
    "scroll"
)

SUCCESSFUL=()
FAILED=()

# Deploy to each chain
for chain in "${CHAINS[@]}"; do
    echo -e "\n${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Deploying to: ${chain} MAINNET${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
    
    if node scripts/deploy-evm-chain.js "$chain" mainnet; then
        SUCCESSFUL+=("$chain")
        echo -e "${GREEN}✅ Successfully deployed to ${chain}${NC}\n"
    else
        FAILED+=("$chain")
        echo -e "${RED}❌ Failed to deploy to ${chain}${NC}\n"
    fi
    
    # Small delay between deployments
    sleep 5
done

# Summary
echo -e "\n${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}=== Mainnet Deployment Summary ===${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"

echo -e "${GREEN}✅ Successful Deployments (${#SUCCESSFUL[@]}):${NC}"
for chain in "${SUCCESSFUL[@]}"; do
    echo "   - $chain"
done

if [ ${#FAILED[@]} -gt 0 ]; then
    echo -e "\n${RED}❌ Failed Deployments (${#FAILED[@]}):${NC}"
    for chain in "${FAILED[@]}"; do
        echo "   - $chain"
    done
fi

echo -e "\n${YELLOW}Next Steps:${NC}"
echo "1. Review deployed-addresses.json for all contract addresses"
echo "2. Update OASIS_DNA.json with mainnet addresses"
echo "3. Verify contracts on block explorers"
echo "4. Run integration tests on mainnet"
echo "5. Update documentation with contract addresses"


