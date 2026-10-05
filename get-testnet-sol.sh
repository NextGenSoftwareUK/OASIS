#!/bin/bash

# Script to get testnet SOL from Solana devnet faucet
# Usage: ./get-testnet-sol.sh [wallet_address]

set -e

WALLET_ADDRESS="${1:-268i3KPxGjA4Eif6BK2HLfpbXqGuSbQxvCfw5e5Ka9F5Qk2bgVm}"
FAUCET_URL="https://faucet.solana.com"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}💰 Requesting Testnet SOL${NC}"
echo "================================"
echo ""
echo -e "${BLUE}Wallet Address:${NC} $WALLET_ADDRESS"
echo -e "${BLUE}Faucet URL:${NC} $FAUCET_URL"
echo ""

# Validate Solana address format (Base58, allow longer addresses)
if ! [[ "$WALLET_ADDRESS" =~ ^[1-9A-HJ-NP-Za-km-z]{32,}$ ]]; then
    echo -e "${RED}❌ Invalid Solana address format${NC}"
    echo -e "${YELLOW}Address length: ${#WALLET_ADDRESS} characters${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Address format looks valid (${#WALLET_ADDRESS} chars)${NC}"

echo -e "${YELLOW}📡 Requesting SOL from faucet...${NC}"
echo ""

# Method 1: Try the official Solana faucet API
RESPONSE=$(curl -s -X POST "$FAUCET_URL" \
    -H "Content-Type: application/json" \
    -d "{\"account\":\"$WALLET_ADDRESS\"}" 2>&1)

if echo "$RESPONSE" | grep -q "success\|Success\|signature"; then
    echo -e "${GREEN}✅ Successfully requested SOL!${NC}"
    echo "$RESPONSE"
    echo ""
    echo -e "${BLUE}💡 Check your balance in a few seconds${NC}"
    echo ""
    echo "To check balance, run:"
    echo "  solana balance $WALLET_ADDRESS --url devnet"
    exit 0
fi

# Method 2: Try alternative faucet endpoints
echo -e "${YELLOW}Trying alternative faucet methods...${NC}"
echo ""

# Try solana-faucet.com
ALT_RESPONSE=$(curl -s -X POST "https://api.solana-faucet.com/request" \
    -H "Content-Type: application/json" \
    -d "{\"address\":\"$WALLET_ADDRESS\",\"network\":\"devnet\"}" 2>&1)

if echo "$ALT_RESPONSE" | grep -q "success\|Success\|signature"; then
    echo -e "${GREEN}✅ Successfully requested SOL from alternative faucet!${NC}"
    echo "$ALT_RESPONSE"
    exit 0
fi

# If both fail, provide manual instructions
echo -e "${YELLOW}⚠️  Automated faucet request failed${NC}"
echo ""
echo -e "${BLUE}📋 Manual Methods to Get Testnet SOL:${NC}"
echo ""
echo "1. 🌐 Official Solana Faucet (Web):"
echo "   https://faucet.solana.com"
echo "   Paste your address: $WALLET_ADDRESS"
echo ""
echo "2. 🔧 Solana CLI (if installed):"
echo "   solana airdrop 2 $WALLET_ADDRESS --url devnet"
echo ""
echo "3. 🐍 Python Script (if you have solana-py):"
echo "   python3 -c \"from solana.rpc.api import Client; client = Client('https://api.devnet.solana.com'); print(client.request_airdrop('$WALLET_ADDRESS', 2_000_000_000))\""
echo ""
echo "4. 📱 Discord Faucet:"
echo "   Join: https://discord.gg/solana"
echo "   Use: !faucet $WALLET_ADDRESS"
echo ""
echo "5. 🌍 Alternative Faucets:"
echo "   - https://solfaucet.com/"
echo "   - https://faucet.sonic.game/"
echo ""
echo -e "${BLUE}Your wallet address:${NC}"
echo "$WALLET_ADDRESS"
echo ""

