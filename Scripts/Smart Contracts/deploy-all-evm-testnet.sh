#!/bin/bash

# Deploy OASIS contracts to ALL EVM testnets
# This script deploys contracts to all supported EVM testnet networks

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}=== Deploying OASIS Contracts to ALL EVM Testnets ===${NC}\n"

# Check for private key
if [ -z "$DEPLOYER_PRIVATE_KEY" ]; then
    echo -e "${RED}❌ DEPLOYER_PRIVATE_KEY environment variable not set${NC}"
    echo "Please set it: export DEPLOYER_PRIVATE_KEY=your-private-key"
    exit 1
fi

# List of testnet chains to deploy
CHAINS=(
    "sepolia"
    "arbitrumSepolia"
    "optimismSepolia"
    "baseSepolia"
    "amoy"
    "bnbTestnet"
    "fantomTestnet"
    "fuji"
    "rootstockTestnet"
    "zkSyncTestnet"
    "lineaTestnet"
    "scrollSepolia"
)

SUCCESSFUL=()
FAILED=()

# Deploy to each chain
for chain in "${CHAINS[@]}"; do
    echo -e "\n${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Deploying to: ${chain}${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
    
    if node scripts/deploy-evm-chain.js "$chain"; then
        SUCCESSFUL+=("$chain")
        echo -e "${GREEN}✅ Successfully deployed to ${chain}${NC}\n"
    else
        FAILED+=("$chain")
        echo -e "${RED}❌ Failed to deploy to ${chain}${NC}\n"
    fi
    
    # Small delay between deployments
    sleep 2
done

# Summary
echo -e "\n${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}=== Deployment Summary ===${NC}"
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
echo "2. Update OASIS_DNA.json with testnet addresses"
echo "3. Run integration tests on testnets"
echo "4. Deploy to mainnet after testnet verification"


