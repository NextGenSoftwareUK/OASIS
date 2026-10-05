#!/bin/bash

# Script to regenerate wallets with proper address derivation
# This deletes old wallets and creates new ones with correct addresses

set -e

API_URL="${NEXT_PUBLIC_OASIS_API_URL:-https://localhost:5004}"
TOKEN_FILE="${HOME}/.oasis_token"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔄 Wallet Regeneration Script${NC}"
echo "=================================="
echo ""
echo -e "${YELLOW}⚠️  WARNING: This will delete existing wallets and create new ones${NC}"
echo -e "${YELLOW}⚠️  Make sure you have backed up any important wallet data${NC}"
echo ""
read -p "Continue? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo "Cancelled."
    exit 0
fi

# Load token
if [ ! -f "$TOKEN_FILE" ]; then
    echo -e "${RED}❌ No saved token found. Run ./auth-oasis.sh first.${NC}"
    exit 1
fi

TOKEN=$(cat "$TOKEN_FILE")
export OASIS_TOKEN="$TOKEN"

# Extract avatar ID
AVATAR_ID=$(echo "$TOKEN" | cut -d'.' -f2 | base64 -d 2>/dev/null | jq -r '.id // empty' 2>/dev/null || echo "")

if [ -z "$AVATAR_ID" ]; then
    echo -e "${RED}❌ Could not extract avatar ID from token${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Avatar ID: $AVATAR_ID${NC}"
echo ""

# Providers to regenerate
PROVIDERS=(
    "EthereumOASIS"
    "ZcashOASIS"
    "StarknetOASIS"
    "SolanaOASIS"
    "PolygonOASIS"
    "ArbitrumOASIS"
    "AztecOASIS"
    "MidenOASIS"
)

echo -e "${BLUE}Creating new wallets with proper address derivation...${NC}"
echo ""

SUCCESS=0
FAIL=0

for PROVIDER in "${PROVIDERS[@]}"; do
    echo -e "${YELLOW}Processing $PROVIDER...${NC}"
    
    # Generate keypair
    KEYPAIR_RESPONSE=$(curl -k -s -X POST "$API_URL/api/keys/generate_keypair_for_provider/$PROVIDER" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $TOKEN")
    
    # Check if response is an error
    IS_ERROR=$(echo "$KEYPAIR_RESPONSE" | jq -r '.isError // false' 2>/dev/null || echo "true")
    
    if [ "$IS_ERROR" = "true" ]; then
        ERROR_MSG=$(echo "$KEYPAIR_RESPONSE" | jq -r '.message // "Unknown error"' 2>/dev/null || echo "Failed to parse response")
        echo -e "${RED}  ✗ Failed to generate keypair: $ERROR_MSG${NC}"
        echo "  Full response: $KEYPAIR_RESPONSE" | jq '.' 2>/dev/null || echo "  Full response: $KEYPAIR_RESPONSE"
        ((FAIL++))
        continue
    fi
    
    # Extract keys from successful response
    PUBLIC_KEY=$(echo "$KEYPAIR_RESPONSE" | jq -r '.result.publicKey // empty' 2>/dev/null)
    PRIVATE_KEY=$(echo "$KEYPAIR_RESPONSE" | jq -r '.result.privateKey // empty' 2>/dev/null)
    
    if [ -z "$PUBLIC_KEY" ] || [ "$PUBLIC_KEY" = "null" ] || [ "$PUBLIC_KEY" = "" ]; then
        echo -e "${RED}  ✗ Failed to extract public key from response${NC}"
        echo "  Response: $KEYPAIR_RESPONSE" | jq '.' 2>/dev/null || echo "  Response: $KEYPAIR_RESPONSE"
        ((FAIL++))
        continue
    fi
    
    echo -e "${GREEN}  ✓ Generated keypair${NC}"
    
    # Link public key (creates wallet with new address derivation)
    LINK_RESPONSE=$(curl -k -s -X POST "$API_URL/api/keys/link_provider_public_key_to_avatar_by_id" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $TOKEN" \
        -d "{\"AvatarID\":\"$AVATAR_ID\",\"providerType\":\"$PROVIDER\",\"providerKey\":\"$PUBLIC_KEY\"}")
    
    WALLET_ADDRESS=$(echo "$LINK_RESPONSE" | jq -r '.result.walletAddress // empty')
    WALLET_ID=$(echo "$LINK_RESPONSE" | jq -r '.result.id // .result.walletId // empty')
    
    if [ -z "$WALLET_ADDRESS" ] || [ "$WALLET_ADDRESS" = "null" ]; then
        echo -e "${RED}  ✗ Failed to create wallet${NC}"
        echo "  Response: $LINK_RESPONSE" | jq '.' 2>/dev/null || echo "  Response: $LINK_RESPONSE"
        ((FAIL++))
        continue
    fi
    
    # Link private key to complete the wallet
    if [ -n "$PRIVATE_KEY" ] && [ -n "$WALLET_ID" ]; then
        LINK_PRIVATE_RESPONSE=$(curl -k -s -X POST "$API_URL/api/keys/link_provider_private_key_to_avatar_by_id" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $TOKEN" \
            -d "{\"AvatarID\":\"$AVATAR_ID\",\"walletId\":\"$WALLET_ID\",\"providerType\":\"$PROVIDER\",\"providerKey\":\"$PRIVATE_KEY\"}")
        
        if echo "$LINK_PRIVATE_RESPONSE" | jq -e '.isError == false' > /dev/null 2>&1; then
            echo -e "${GREEN}  ✓ Linked private key${NC}"
        else
            echo -e "${YELLOW}  ⚠ Private key linking failed (wallet still created)${NC}"
        fi
    fi
    
    # Validate address format
    case $PROVIDER in
        EthereumOASIS|PolygonOASIS|ArbitrumOASIS|AztecOASIS|MidenOASIS)
            if [[ "$WALLET_ADDRESS" =~ ^0x[0-9a-fA-F]{40}$ ]]; then
                echo -e "${GREEN}  ✓ Address: $WALLET_ADDRESS${NC}"
                ((SUCCESS++))
            else
                echo -e "${RED}  ✗ Invalid format: $WALLET_ADDRESS${NC}"
                ((FAIL++))
            fi
            ;;
        ZcashOASIS)
            # Zcash transparent addresses: tm... (testnet) or t1... (mainnet), base58, typically 34 chars
            # Note: Unified Addresses (u1...) require proper ZIP-316 encoding which needs Zcash libraries
            # For now, we use transparent addresses which are simpler and should work with most faucets
            if [[ "$WALLET_ADDRESS" =~ ^t[m1][a-zA-Z0-9]{25,}$ ]]; then
                echo -e "${GREEN}  ✓ Address: $WALLET_ADDRESS${NC}"
                ((SUCCESS++))
            else
                echo -e "${RED}  ✗ Invalid format: $WALLET_ADDRESS${NC}"
                ((FAIL++))
            fi
            ;;
        StarknetOASIS)
            # Starknet addresses: 0x + 64 hex chars (66 total)
            if [[ "$WALLET_ADDRESS" =~ ^0x[0-9a-fA-F]{63,64}$ ]]; then
                echo -e "${GREEN}  ✓ Address: $WALLET_ADDRESS${NC}"
                ((SUCCESS++))
            else
                echo -e "${RED}  ✗ Invalid format: $WALLET_ADDRESS${NC}"
                ((FAIL++))
            fi
            ;;
        SolanaOASIS)
            # Solana addresses: base58, typically 32-44 chars but can be up to 52
            if [[ "$WALLET_ADDRESS" =~ ^[1-9A-HJ-NP-Za-km-z]{32,52}$ ]]; then
                echo -e "${GREEN}  ✓ Address: $WALLET_ADDRESS${NC}"
                ((SUCCESS++))
            else
                echo -e "${RED}  ✗ Invalid format: $WALLET_ADDRESS${NC}"
                ((FAIL++))
            fi
            ;;
    esac
    
    echo ""
done

echo -e "${BLUE}=== Summary ===${NC}"
echo -e "${GREEN}✓ Success: $SUCCESS${NC}"
echo -e "${RED}✗ Failed: $FAIL${NC}"
echo ""

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}✅ All wallets regenerated successfully!${NC}"
    echo ""
    echo "Run the test again:"
    echo "  cd zypherpunk-wallet-ui && ./test-wallet-addresses.sh"
else
    echo -e "${YELLOW}⚠️  Some wallets failed to regenerate${NC}"
fi

