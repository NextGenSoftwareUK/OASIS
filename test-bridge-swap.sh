#!/bin/bash

# Test script for OASIS Bridge API swap functionality
# Tests creating swap orders between different chains

set -e

API_URL="${NEXT_PUBLIC_OASIS_API_URL:-https://localhost:5004}"
TOKEN_FILE="${HOME}/.oasis_token"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🧪 Testing OASIS Bridge API${NC}"
echo "================================"
echo ""

# Load token
if [ -f "$TOKEN_FILE" ]; then
    export OASIS_TOKEN=$(cat "$TOKEN_FILE")
    echo -e "${GREEN}✅ Token loaded from $TOKEN_FILE${NC}"
else
    echo -e "${YELLOW}⚠️  No token found. Please run ./auth-oasis.sh first${NC}"
    exit 1
fi

# Extract avatar ID from token
AVATAR_ID=$(echo "$OASIS_TOKEN" | cut -d'.' -f2 | base64 -d 2>/dev/null | jq -r '.id // empty' 2>/dev/null || echo "")

if [ -z "$AVATAR_ID" ]; then
    echo -e "${RED}❌ Could not determine avatar ID from token${NC}"
    exit 1
fi

echo -e "${BLUE}Avatar ID: $AVATAR_ID${NC}"
echo ""

# Test 1: Get supported networks
echo -e "${BLUE}Test 1: Get Supported Networks${NC}"
echo "-----------------------------------"
NETWORKS_RESPONSE=$(curl -k -s -X GET "$API_URL/api/v1/networks")
echo "$NETWORKS_RESPONSE" | jq '.' 2>/dev/null || echo "$NETWORKS_RESPONSE"
echo ""

# Test 2: Get exchange rate (SOL to ETH)
echo -e "${BLUE}Test 2: Get Exchange Rate (SOL → ETH)${NC}"
echo "-----------------------------------"
RATE_RESPONSE=$(curl -k -s -X GET "$API_URL/api/v1/exchange-rate?fromToken=SOL&toToken=ETH")
echo "$RATE_RESPONSE" | jq '.' 2>/dev/null || echo "$RATE_RESPONSE"
echo ""

# Test 3: Create a swap order (SOL to ETH)
echo -e "${BLUE}Test 3: Create Swap Order (SOL → ETH)${NC}"
echo "-----------------------------------"
echo "Note: This creates an order but may not execute without proper bridge setup"
echo "      and testnet tokens. The order will be created but execution may fail."
echo ""

# Get a Solana wallet address for testing
SOLANA_WALLET=$(curl -k -s -X GET "$API_URL/api/wallet/avatar/$AVATAR_ID/wallets" \
    -H "Authorization: Bearer $OASIS_TOKEN" \
    -H "Content-Type: application/json" | \
    jq -r '.result.SolanaOASIS[0].walletAddress // empty' 2>/dev/null || echo "")

if [ -z "$SOLANA_WALLET" ]; then
    echo -e "${YELLOW}⚠️  No Solana wallet found. Using placeholder address${NC}"
    SOLANA_WALLET="28Q1LC3udtXJx67UshEdro44Z5haDut5HQusxuGZpuPxRcinkNh"
fi

# Get an Ethereum wallet address for testing
ETH_WALLET=$(curl -k -s -X GET "$API_URL/api/wallet/avatar/$AVATAR_ID/wallets" \
    -H "Authorization: Bearer $OASIS_TOKEN" \
    -H "Content-Type: application/json" | \
    jq -r '.result.EthereumOASIS[0].walletAddress // empty' 2>/dev/null || echo "")

if [ -z "$ETH_WALLET" ]; then
    echo -e "${YELLOW}⚠️  No Ethereum wallet found. Using placeholder address${NC}"
    ETH_WALLET="0x5ac4431ce49f6ef3a8211c5551070145a56a9c52"
fi

echo -e "${BLUE}From (Solana): $SOLANA_WALLET${NC}"
echo -e "${BLUE}To (Ethereum): $ETH_WALLET${NC}"
echo ""

SWAP_REQUEST=$(cat <<EOF
{
  "userId": "$AVATAR_ID",
  "fromToken": "SOL",
  "toToken": "ETH",
  "amount": 0.1,
  "fromNetwork": "Solana",
  "toNetwork": "Ethereum",
  "destinationAddress": "$ETH_WALLET"
}
EOF
)

echo "Request:"
echo "$SWAP_REQUEST" | jq '.' 2>/dev/null || echo "$SWAP_REQUEST"
echo ""

SWAP_RESPONSE=$(curl -k -s -X POST "$API_URL/api/v1/orders" \
    -H "Authorization: Bearer $OASIS_TOKEN" \
    -H "Content-Type: application/json" \
    -d "$SWAP_REQUEST")

echo "Response:"
echo "$SWAP_RESPONSE" | jq '.' 2>/dev/null || echo "$SWAP_RESPONSE"
echo ""

# Extract order ID if successful
ORDER_ID=$(echo "$SWAP_RESPONSE" | jq -r '.orderId // empty' 2>/dev/null || echo "")

if [ -n "$ORDER_ID" ] && [ "$ORDER_ID" != "null" ]; then
    echo -e "${GREEN}✅ Swap order created! Order ID: $ORDER_ID${NC}"
    echo ""
    
    # Test 4: Check order balance
    echo -e "${BLUE}Test 4: Check Order Balance${NC}"
    echo "-----------------------------------"
    BALANCE_RESPONSE=$(curl -k -s -X GET "$API_URL/api/v1/orders/$ORDER_ID/check-balance" \
        -H "Authorization: Bearer $OASIS_TOKEN" \
        -H "Content-Type: application/json")
    echo "$BALANCE_RESPONSE" | jq '.' 2>/dev/null || echo "$BALANCE_RESPONSE"
    echo ""
else
    echo -e "${RED}❌ Failed to create swap order${NC}"
    echo "Response: $SWAP_RESPONSE"
fi

echo ""
echo -e "${GREEN}✅ Bridge API test complete!${NC}"

