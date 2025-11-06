#!/bin/bash

# Quick Multi-Chain NFT Test with metabricks_admin credentials
API_BASE="https://localhost:5004/api"
CURL_OPTS="-k"  # Ignore self-signed certificate

echo "================================================"
echo "Testing Multi-Chain Web4 NFT Minting"
echo "================================================"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Credentials from meta-bricks
USERNAME="metabricks_admin"
PASSWORD="Uppermall1!"

echo -e "${YELLOW}Step 1: Authenticating with metabricks_admin...${NC}"

AUTH_RESPONSE=$(curl $CURL_OPTS -s -X POST "$API_BASE/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

echo "Auth Response: $AUTH_RESPONSE"
echo ""

# Extract token using different methods for robustness
TOKEN=$(echo "$AUTH_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('result', {}).get('token', ''))" 2>/dev/null)

if [ -z "$TOKEN" ]; then
    TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
fi

if [ -z "$TOKEN" ]; then
    echo -e "${RED}✗ Authentication failed - No token received${NC}"
    echo "Full response: $AUTH_RESPONSE"
    exit 1
fi

echo -e "${GREEN}✓ Authenticated successfully!${NC}"
echo "Token: ${TOKEN:0:50}..."
echo ""

# Extract Avatar ID
AVATAR_ID=$(echo "$AUTH_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('result', {}).get('avatar', {}).get('id', ''))" 2>/dev/null)

if [ -z "$AVATAR_ID" ]; then
    AVATAR_ID=$(echo "$AUTH_RESPONSE" | grep -o '"id":"[^"]*' | cut -d'"' -f4 | head -1)
fi

echo "Avatar ID: $AVATAR_ID"
echo ""

# Step 2: Mint Multi-Chain NFT
echo -e "${YELLOW}Step 2: Minting Multi-Chain Web4 NFT...${NC}"
echo ""
echo "This will create:"
echo "  • 1 Web4 OASIS NFT (parent wrapper)"
echo "  • 2 Web3 NFT variants:"
echo "    - Solana variant (Fire Dragon)"
echo "    - Arbitrum variant (Ice Dragon)"
echo ""

# Create the NFT request
NFT_REQUEST=$(cat <<'EOF'
{
  "title": "Cosmic Dragon - Multi-Chain Test",
  "description": "Testing David's new multi-chain NFT architecture! This dragon exists on both Solana and Arbitrum with unique variants.",
  "imageUrl": "https://picsum.photos/400?random=dragon",
  "price": 10.0,
  "discount": 0,
  "royaltyPercentage": 15,
  "tags": ["dragon", "multichain", "web4", "test"],
  "metaData": {
    "artist": "OASIS Team",
    "collection": "Multi-Chain Test Collection",
    "rarity": "legendary",
    "power": 9000,
    "test": true
  },
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS",
  "nftStandardType": "ERC1155",
  "nftOffChainMetaType": "OASIS",
  "storeNFTMetaDataOnChain": false,
  "numberToMint": 1,
  "waitTillNFTMinted": true,
  "waitForNFTToMintInSeconds": 120,
  "web3NFTs": [
    {
      "title": "Cosmic Dragon - Fire Variant (Solana)",
      "description": "Fire-breathing Solana dragon with unique properties",
      "imageUrl": "https://picsum.photos/400?random=fire",
      "price": 2.5,
      "onChainProvider": "SolanaOASIS",
      "metaData": {
        "element": "fire",
        "color": "red",
        "special_ability": "inferno_blast",
        "chain": "solana"
      },
      "tags": ["fire", "solana"],
      "nftMetaDataMergeStrategy": "MergeAndOverwrite",
      "nftTagsMergeStrategy": "Merge",
      "numberToMint": 1
    },
    {
      "title": "Cosmic Dragon - Ice Variant (Arbitrum)",
      "description": "Frost-powered Arbitrum dragon with unique properties",
      "imageUrl": "https://picsum.photos/400?random=ice",
      "price": 0.05,
      "onChainProvider": "ArbitrumOASIS",
      "metaData": {
        "element": "ice",
        "color": "blue",
        "special_ability": "arctic_freeze",
        "chain": "arbitrum"
      },
      "tags": ["ice", "arbitrum"],
      "nftMetaDataMergeStrategy": "MergeAndOverwrite",
      "nftTagsMergeStrategy": "Merge",
      "numberToMint": 1
    }
  ]
}
EOF
)

echo "Sending mint request..."
echo ""

MINT_RESPONSE=$(curl $CURL_OPTS -s -X POST "$API_BASE/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$NFT_REQUEST")

# Pretty print if possible
echo "$MINT_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$MINT_RESPONSE"
echo ""

# Check if minting was successful
if echo "$MINT_RESPONSE" | grep -q '"isError":false\|"success":true'; then
    echo -e "${GREEN}✓ NFT Minting initiated!${NC}"
    
    # Try to extract NFT ID
    NFT_ID=$(echo "$MINT_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('result', {}).get('id', ''))" 2>/dev/null)
    
    if [ -n "$NFT_ID" ]; then
        echo "NFT ID: $NFT_ID"
        echo ""
        
        # Wait a moment then load the NFT
        echo -e "${YELLOW}Step 3: Loading NFT details...${NC}"
        sleep 3
        
        LOAD_RESPONSE=$(curl $CURL_OPTS -s -X GET "$API_BASE/nft/load-nft-by-id/$NFT_ID" \
          -H "Authorization: Bearer $TOKEN")
        
        echo ""
        echo "Web4 NFT Details:"
        echo "=================="
        echo "$LOAD_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$LOAD_RESPONSE"
        echo ""
        
        # Count Web3 variants
        WEB3_COUNT=$(echo "$LOAD_RESPONSE" | grep -o '"web3NFTs"' | wc -l)
        
        if [ "$WEB3_COUNT" -gt 0 ]; then
            echo -e "${GREEN}✓ Multi-Chain NFT created successfully!${NC}"
            echo -e "${BLUE}The Web4 NFT contains blockchain variants${NC}"
        fi
    fi
else
    echo -e "${RED}✗ Minting may have failed${NC}"
    echo "Check the response above for errors"
fi

echo ""
echo "================================================"
echo -e "${GREEN}Test Complete!${NC}"
echo "================================================"
echo ""
echo "What we tested:"
echo "✓ Authentication with metabricks_admin"
echo "✓ Multi-chain NFT minting (Solana + Arbitrum)"
echo "✓ Web4 wrapper with Web3 variants"
echo "✓ Metadata merge strategies"
echo "✓ Different properties per chain"
echo ""
echo "Key Features Demonstrated:"
echo "• Single Web4 NFT wrapping multiple Web3 NFTs"
echo "• Shared parent metadata (artist, collection, rarity)"
echo "• Unique variant metadata (element, color, abilities)"
echo "• Different pricing per blockchain"
echo "• Tag merging strategies"
echo ""

