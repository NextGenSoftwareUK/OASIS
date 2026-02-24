#!/bin/bash

# Deploy OASIS contract to Sui (Sui Move)
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

echo -e "${GREEN}=== Deploying OASIS Contract to Sui ${NETWORK} ===${NC}\n"

# Check for Sui CLI
if ! command -v sui &> /dev/null; then
    echo -e "${RED}❌ Sui CLI not found${NC}"
    echo "Install it: cargo install --locked --git https://github.com/MystenLabs/sui.git --branch main sui"
    exit 1
fi

# Set network
if [ "$NETWORK" == "testnet" ]; then
    sui client switch --env testnet
    RPC_URL="https://fullnode.testnet.sui.io:443"
else
    sui client switch --env mainnet
    RPC_URL="https://fullnode.mainnet.sui.io:443"
fi

echo -e "${YELLOW}Network: ${NETWORK}${NC}"
echo -e "${YELLOW}RPC URL: ${RPC_URL}${NC}\n"

# Get active address
ACTIVE_ADDRESS=$(sui client active-address)
if [ -z "$ACTIVE_ADDRESS" ]; then
    echo -e "${YELLOW}⚠️  No active address. Creating new address...${NC}"
    sui client new-address ed25519
    ACTIVE_ADDRESS=$(sui client active-address)
fi

echo -e "${YELLOW}Active Address: ${ACTIVE_ADDRESS}${NC}\n"

# Check balance
BALANCE=$(sui client gas | head -1 | awk '{print $1}' || echo "0")
echo -e "${YELLOW}Account Balance: ${BALANCE} SUI${NC}\n"

# Navigate to contract directory
CONTRACT_DIR="Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/contracts"
if [ ! -d "$CONTRACT_DIR" ]; then
    echo -e "${RED}❌ Contract directory not found: ${CONTRACT_DIR}${NC}"
    exit 1
fi

cd "$CONTRACT_DIR"

# Build contract
echo -e "${YELLOW}Building Sui Move contract...${NC}"
sui move build

# Publish package
echo -e "\n${YELLOW}Publishing package to Sui ${NETWORK}...${NC}"
PUBLISH_OUTPUT=$(sui client publish --gas-budget 100000000 --json)

# Extract package ID
PACKAGE_ID=$(echo "$PUBLISH_OUTPUT" | jq -r '.objectChanges[] | select(.type == "published") | .packageId' 2>/dev/null || echo "")

if [ -z "$PACKAGE_ID" ]; then
    # Try alternative parsing
    PACKAGE_ID=$(echo "$PUBLISH_OUTPUT" | grep -oP '"packageId":\s*"\K[^"]+' | head -1 || echo "")
fi

if [ -z "$PACKAGE_ID" ]; then
    echo -e "${YELLOW}⚠️  Could not parse package ID from output${NC}"
    echo "Publish output:"
    echo "$PUBLISH_OUTPUT"
    echo -e "\n${YELLOW}Please extract the package ID manually from the output above${NC}"
    read -p "Enter Package ID: " PACKAGE_ID
fi

echo -e "\n${GREEN}✅ OASIS contract deployed successfully!${NC}"
echo -e "${GREEN}   Package ID: ${PACKAGE_ID}${NC}"
echo -e "${GREEN}   Explorer: https://suiexplorer.com/object/${PACKAGE_ID}?network=${NETWORK}${NC}"

# Save to deployed-addresses.json
cd - > /dev/null
DEPLOYED_FILE="deployed-addresses.json"
if [ ! -f "$DEPLOYED_FILE" ]; then
    echo "{}" > "$DEPLOYED_FILE"
fi

# Update JSON
echo -e "\n${YELLOW}📝 Update deployed-addresses.json with:${NC}"
echo "  \"SuiOASIS\": {"
echo "    \"${NETWORK}\": {"
echo "      \"packageId\": \"${PACKAGE_ID}\","
echo "      \"address\": \"${ACTIVE_ADDRESS}\","
echo "      \"network\": \"${NETWORK}\","
echo "      \"deployedAt\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\""
echo "    }"
echo "  }"

echo -e "\n${YELLOW}📝 Update OASIS_DNA.json with:${NC}"
echo "  \"SuiOASIS\": {"
echo "    \"RpcEndpoint\": \"${RPC_URL}\","
echo "    \"ContractAddress\": \"${PACKAGE_ID}\","
echo "    \"Network\": \"${NETWORK}\""
echo "  }"


