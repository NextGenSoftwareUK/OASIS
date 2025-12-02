#!/bin/bash

# Script to check Solana wallet balance
# Usage: ./check-solana-balance.sh [address] [network]

set -e

ADDRESS="${1:-2gQvjrtBEoQanfCiAud8hs2tTBF9fba1MkZ2pNtUy5SB}"
NETWORK="${2:-devnet}"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# RPC endpoints
case $NETWORK in
    devnet)
        RPC_URL="https://api.devnet.solana.com"
        ;;
    testnet)
        RPC_URL="https://api.testnet.solana.com"
        ;;
    mainnet|mainnet-beta)
        RPC_URL="https://api.mainnet-beta.solana.com"
        ;;
    *)
        RPC_URL="https://api.devnet.solana.com"
        NETWORK="devnet"
        ;;
esac

echo -e "${BLUE}🔍 Checking Solana Balance${NC}"
echo "========================="
echo ""
echo -e "${BLUE}Address:${NC} $ADDRESS"
echo -e "${BLUE}Network:${NC} $NETWORK"
echo -e "${BLUE}RPC URL:${NC} $RPC_URL"
echo ""

# Check balance
RESPONSE=$(curl -s -X POST "$RPC_URL" \
    -H "Content-Type: application/json" \
    -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"getBalance\",\"params\":[\"$ADDRESS\"]}")

# Parse response
ERROR=$(echo "$RESPONSE" | jq -r '.error // empty' 2>/dev/null)
BALANCE_LAMPORTS=$(echo "$RESPONSE" | jq -r '.result.value // 0' 2>/dev/null)

if [ -n "$ERROR" ]; then
    ERROR_MSG=$(echo "$RESPONSE" | jq -r '.error.message // "Unknown error"' 2>/dev/null)
    echo -e "${RED}❌ Error: $ERROR_MSG${NC}"
    echo ""
    echo "Full response:"
    echo "$RESPONSE" | jq '.'
    exit 1
fi

# Convert lamports to SOL (1 SOL = 1,000,000,000 lamports)
BALANCE_SOL=$(echo "scale=9; $BALANCE_LAMPORTS / 1000000000" | bc)

echo -e "${GREEN}✅ Balance:${NC}"
echo -e "  ${GREEN}$BALANCE_SOL SOL${NC}"
echo -e "  ${YELLOW}$BALANCE_LAMPORTS lamports${NC}"
echo ""

# Check if address has received any transactions
echo -e "${BLUE}📊 Additional Info:${NC}"
echo ""

# Get transaction count
TX_COUNT_RESPONSE=$(curl -s -X POST "$RPC_URL" \
    -H "Content-Type: application/json" \
    -d "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"getTransactionCount\",\"params\":[\"$ADDRESS\",{\"commitment\":\"confirmed\"}]}")

TX_COUNT=$(echo "$TX_COUNT_RESPONSE" | jq -r '.result // 0' 2>/dev/null)
echo -e "  Transaction Count: ${BLUE}$TX_COUNT${NC}"

# Check if address is valid (get account info)
ACCOUNT_INFO_RESPONSE=$(curl -s -X POST "$RPC_URL" \
    -H "Content-Type: application/json" \
    -d "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"getAccountInfo\",\"params\":[\"$ADDRESS\",{\"encoding\":\"base58\"}]}")

ACCOUNT_EXISTS=$(echo "$ACCOUNT_INFO_RESPONSE" | jq -r '.result.value != null' 2>/dev/null)

if [ "$ACCOUNT_EXISTS" = "true" ]; then
    echo -e "  Account Status: ${GREEN}✅ Active${NC}"
else
    echo -e "  Account Status: ${YELLOW}⚠️  Not initialized (no transactions yet)${NC}"
fi

echo ""
echo -e "${BLUE}🔗 View on Explorer:${NC}"
echo "  https://explorer.solana.com/address/$ADDRESS?cluster=$NETWORK"

