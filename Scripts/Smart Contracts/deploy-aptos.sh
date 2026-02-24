#!/bin/bash

# Deploy OASIS contract to Aptos (Move)
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

echo -e "${GREEN}=== Deploying OASIS Contract to Aptos ${NETWORK} ===${NC}\n"

# Check for Aptos CLI
if ! command -v aptos &> /dev/null; then
    echo -e "${RED}❌ Aptos CLI not found${NC}"
    echo "Install it: curl -fsSL https://aptos.dev/scripts/install_cli.py | python3"
    exit 1
fi

# Set network
if [ "$NETWORK" == "testnet" ]; then
    aptos config set-global-config --config-type testnet
    RPC_URL="https://fullnode.testnet.aptoslabs.com"
else
    aptos config set-global-config --config-type mainnet
    RPC_URL="https://fullnode.mainnet.aptoslabs.com"
fi

echo -e "${YELLOW}Network: ${NETWORK}${NC}"
echo -e "${YELLOW}RPC URL: ${RPC_URL}${NC}\n"

# Get account address
ACCOUNT_ADDRESS=$(aptos config show-profiles | grep -A 5 "default" | grep "account" | awk '{print $2}' || echo "")
if [ -z "$ACCOUNT_ADDRESS" ]; then
    echo -e "${YELLOW}⚠️  No default account found. Initializing...${NC}"
    aptos init --network $NETWORK
    ACCOUNT_ADDRESS=$(aptos config show-profiles | grep -A 5 "default" | grep "account" | awk '{print $2}')
fi

echo -e "${YELLOW}Account Address: ${ACCOUNT_ADDRESS}${NC}\n"

# Navigate to contract directory
CONTRACT_DIR="Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AptosOASIS/contracts"
if [ ! -d "$CONTRACT_DIR" ]; then
    echo -e "${RED}❌ Contract directory not found: ${CONTRACT_DIR}${NC}"
    exit 1
fi

cd "$CONTRACT_DIR"

# Compile contract
echo -e "${YELLOW}Compiling Move contract...${NC}"
aptos move compile --named-addresses oasis=$ACCOUNT_ADDRESS

# Publish module
echo -e "\n${YELLOW}Publishing module to Aptos ${NETWORK}...${NC}"
aptos move publish \
    --named-addresses oasis=$ACCOUNT_ADDRESS \
    --assume-yes

# Get published module address
MODULE_ADDRESS="${ACCOUNT_ADDRESS}::oasis::oasis"

echo -e "\n${GREEN}✅ OASIS contract deployed successfully!${NC}"
echo -e "${GREEN}   Module Address: ${MODULE_ADDRESS}${NC}"
echo -e "${GREEN}   Explorer: https://explorer.aptoslabs.com/account/${ACCOUNT_ADDRESS}?network=${NETWORK}${NC}"

# Save to deployed-addresses.json
cd - > /dev/null
DEPLOYED_FILE="deployed-addresses.json"
if [ ! -f "$DEPLOYED_FILE" ]; then
    echo "{}" > "$DEPLOYED_FILE"
fi

# Update JSON (requires jq or manual edit)
echo -e "\n${YELLOW}📝 Update deployed-addresses.json with:${NC}"
echo "  \"AptosOASIS\": {"
echo "    \"${NETWORK}\": {"
echo "      \"address\": \"${MODULE_ADDRESS}\","
echo "      \"accountAddress\": \"${ACCOUNT_ADDRESS}\","
echo "      \"network\": \"${NETWORK}\","
echo "      \"deployedAt\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\""
echo "    }"
echo "  }"

echo -e "\n${YELLOW}📝 Update OASIS_DNA.json with:${NC}"
echo "  \"AptosOASIS\": {"
echo "    \"RpcEndpoint\": \"${RPC_URL}\","
echo "    \"ContractAddress\": \"${ACCOUNT_ADDRESS}\","
echo "    \"Network\": \"${NETWORK}\""
echo "  }"


