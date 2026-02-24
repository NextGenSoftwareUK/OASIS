#!/bin/bash

# Deploy OASIS contracts to ALL Move-based chains (Aptos and Sui)
# Supports both testnet and mainnet

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

NETWORK=${1:-testnet}  # Default to testnet

if [ "$NETWORK" != "testnet" ] && [ "$NETWORK" != "mainnet" ]; then
    echo -e "${RED}❌ Invalid network. Use 'testnet' or 'mainnet'${NC}"
    exit 1
fi

if [ "$NETWORK" == "mainnet" ]; then
    echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${RED}⚠️  MAINNET DEPLOYMENT WARNING ⚠️${NC}"
    echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    read -p "Are you sure you want to deploy to MAINNET? (yes/no): " confirm
    if [ "$confirm" != "yes" ]; then
        echo -e "${YELLOW}Cancelled.${NC}"
        exit 0
    fi
fi

echo -e "${GREEN}=== Deploying OASIS Contracts to ALL Move Chains (${NETWORK}) ===${NC}\n"

SUCCESSFUL=()
FAILED=()

# Deploy Aptos
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Deploying to Aptos ${NETWORK}...${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"

if ./scripts/deploy-aptos.sh "$NETWORK"; then
    SUCCESSFUL+=("Aptos")
    echo -e "${GREEN}✅ Successfully deployed to Aptos${NC}\n"
else
    FAILED+=("Aptos")
    echo -e "${RED}❌ Failed to deploy to Aptos${NC}\n"
fi

sleep 2

# Deploy Sui
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Deploying to Sui ${NETWORK}...${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"

if ./scripts/deploy-sui.sh "$NETWORK"; then
    SUCCESSFUL+=("Sui")
    echo -e "${GREEN}✅ Successfully deployed to Sui${NC}\n"
else
    FAILED+=("Sui")
    echo -e "${RED}❌ Failed to deploy to Sui${NC}\n"
fi

# Summary
echo -e "\n${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}=== Move Chain Deployment Summary ===${NC}"
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
echo "2. Update OASIS_DNA.json with addresses"
echo "3. Run integration tests"
if [ "$NETWORK" == "testnet" ]; then
    echo "4. Deploy to mainnet after testnet verification"
fi


