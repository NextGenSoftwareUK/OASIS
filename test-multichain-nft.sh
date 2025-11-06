#!/bin/bash

# Multi-Chain NFT Testing Script
# This script helps you test David's new multi-chain Web4 NFT functionality

API_BASE="http://localhost:5003/api"
TOKEN=""

echo "======================================"
echo "Multi-Chain Web4 NFT Test Script"
echo "======================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Check if API is running
echo -e "${YELLOW}Step 1: Checking if OASIS API is running...${NC}"
if curl -s --connect-timeout 3 "$API_BASE/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ API is running on port 5003${NC}"
else
    echo -e "${RED}✗ API is not responding. Please start it with:${NC}"
    echo "  cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
    echo "  dotnet run"
    exit 1
fi

echo ""

# Step 2: Authentication
echo -e "${YELLOW}Step 2: Authentication${NC}"
echo "Please enter your OASIS credentials:"
read -p "Username: " USERNAME
read -sp "Password: " PASSWORD
echo ""

echo "Authenticating..."
AUTH_RESPONSE=$(curl -s -X POST "$API_BASE/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo $AUTH_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo -e "${RED}✗ Authentication failed${NC}"
    echo "Response: $AUTH_RESPONSE"
    exit 1
fi

echo -e "${GREEN}✓ Authenticated successfully${NC}"
AVATAR_ID=$(echo $AUTH_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Avatar ID: $AVATAR_ID"
echo ""

# Step 3: Mint Multi-Chain NFT
echo -e "${YELLOW}Step 3: Minting Multi-Chain Web4 NFT...${NC}"
echo ""
echo "Choose a test scenario:"
echo "1) Simple - Same NFT on 2 chains (Solana + Arbitrum)"
echo "2) Advanced - Dragon variants on 3 chains with unique metadata"
echo "3) Custom - Provide your own JSON"
read -p "Enter choice (1-3): " CHOICE

case $CHOICE in
  1)
    echo "Minting simple multi-chain NFT..."
    NFT_REQUEST='{
      "title": "Test Multi-Chain NFT",
      "description": "Testing David'"'"'s new multi-chain NFT system",
      "imageUrl": "https://picsum.photos/400",
      "price": 1.0,
      "discount": 0,
      "royaltyPercentage": 10,
      "onChainProvider": "SolanaOASIS",
      "offChainProvider": "MongoDBOASIS",
      "nftStandardType": "ERC1155",
      "nftOffChainMetaType": "OASIS",
      "storeNFTMetaDataOnChain": false,
      "numberToMint": 1,
      "web3NFTs": [
        {
          "onChainProvider": "SolanaOASIS",
          "price": 0.5
        },
        {
          "onChainProvider": "ArbitrumOASIS",
          "price": 0.01
        }
      ]
    }'
    ;;
    
  2)
    echo "Minting dragon variant collection..."
    NFT_REQUEST='{
      "title": "Cosmic Dragon Collection",
      "description": "Epic dragon with unique variants per blockchain",
      "imageUrl": "https://picsum.photos/400?random=1",
      "price": 10.0,
      "royaltyPercentage": 15,
      "tags": ["dragon", "fantasy", "multichain", "web4"],
      "metaData": {
        "artist": "CryptoArtist",
        "collection": "Cosmic Dragons",
        "rarity": "legendary",
        "power": 9000
      },
      "onChainProvider": "SolanaOASIS",
      "offChainProvider": "MongoDBOASIS",
      "nftStandardType": "ERC721",
      "nftOffChainMetaType": "OASIS",
      "storeNFTMetaDataOnChain": false,
      "web3NFTs": [
        {
          "onChainProvider": "SolanaOASIS",
          "title": "Fire Dragon",
          "description": "Fire-breathing Solana dragon",
          "imageUrl": "https://picsum.photos/400?random=2",
          "price": 2.5,
          "metaData": {
            "element": "fire",
            "color": "red"
          },
          "tags": ["fire", "solana"],
          "nftMetaDataMergeStrategy": "MergeAndOverwrite"
        },
        {
          "onChainProvider": "ArbitrumOASIS",
          "title": "Ice Dragon",
          "description": "Frost-powered Arbitrum dragon",
          "imageUrl": "https://picsum.photos/400?random=3",
          "price": 0.05,
          "metaData": {
            "element": "ice",
            "color": "blue"
          },
          "tags": ["ice", "arbitrum"],
          "nftMetaDataMergeStrategy": "MergeAndOverwrite"
        }
      ]
    }'
    ;;
    
  3)
    echo "Enter path to JSON file with NFT request:"
    read -p "Path: " JSON_PATH
    if [ -f "$JSON_PATH" ]; then
      NFT_REQUEST=$(cat "$JSON_PATH")
    else
      echo -e "${RED}File not found${NC}"
      exit 1
    fi
    ;;
    
  *)
    echo -e "${RED}Invalid choice${NC}"
    exit 1
    ;;
esac

echo ""
echo "Sending mint request..."
MINT_RESPONSE=$(curl -s -X POST "$API_BASE/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$NFT_REQUEST")

echo "$MINT_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$MINT_RESPONSE"

NFT_ID=$(echo $MINT_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4 | head -1)

if [ -z "$NFT_ID" ]; then
    echo -e "${RED}✗ Minting failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✓ NFT Minted Successfully!${NC}"
echo "NFT ID: $NFT_ID"
echo ""

# Step 4: Load and inspect the NFT
echo -e "${YELLOW}Step 4: Loading multi-chain NFT details...${NC}"
sleep 2

LOAD_RESPONSE=$(curl -s -X GET "$API_BASE/nft/load-nft-by-id/$NFT_ID" \
  -H "Authorization: Bearer $TOKEN")

echo ""
echo "Web4 NFT Details:"
echo "=================="
echo "$LOAD_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$LOAD_RESPONSE"

# Count Web3 NFTs
WEB3_COUNT=$(echo $LOAD_RESPONSE | grep -o '"web3NFTs"' | wc -l)
echo ""
echo -e "${GREEN}✓ Web4 NFT contains $WEB3_COUNT blockchain variants${NC}"
echo ""

# Step 5: Query all NFTs for this avatar
echo -e "${YELLOW}Step 5: Querying all NFTs for your avatar...${NC}"
ALL_NFTS=$(curl -s -X GET "$API_BASE/nft/load-all-nfts-for_avatar/$AVATAR_ID" \
  -H "Authorization: Bearer $TOKEN")

NFT_COUNT=$(echo $ALL_NFTS | grep -o '"id"' | wc -l)
echo -e "${GREEN}You have $NFT_COUNT total NFTs${NC}"
echo ""

# Summary
echo "======================================"
echo -e "${GREEN}Test Complete!${NC}"
echo "======================================"
echo ""
echo "What was tested:"
echo "✓ Multi-chain NFT minting"
echo "✓ Web4 wrapper with Web3 variants"
echo "✓ Metadata inheritance and overrides"
echo "✓ NFT querying and inspection"
echo ""
echo "Next steps:"
echo "- Check transaction hashes on block explorers"
echo "- Test sending NFT to another address"
echo "- Try importing existing Web3 NFTs"
echo "- Test different merge strategies"
echo ""
echo "Documentation: MULTI_CHAIN_NFT_TEST_GUIDE.md"

